#!/usr/bin/env bash
# DurableWorkflow deploy script
# Deploys three Lambda functions and a Step Functions state machine using raw AWS CLI.
# No SAM, no CDK, no CloudFormation — just the AWS CLI and dotnet.
#
# Prerequisites:
#   - .NET 10 SDK
#   - AWS CLI configured with credentials
#   - Bedrock access enabled (Claude Haiku cross-region inference profile)
#   - An IAM role for Lambda execution (see README for required permissions)
#
# Usage:
#   chmod +x deploy.sh
#   LAMBDA_ROLE_ARN=arn:aws:iam::123456789:role/my-lambda-role ./deploy.sh
#   LAMBDA_ROLE_ARN=arn:aws:iam::123456789:role/my-lambda-role REGION=us-west-2 ./deploy.sh

set -euo pipefail

REGION="${REGION:-us-east-1}"
LAMBDA_ROLE_ARN="${LAMBDA_ROLE_ARN:?LAMBDA_ROLE_ARN is required. Set it to your Lambda execution role ARN.}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== DurableWorkflow Deploy ==="
echo "Region:    $REGION"
echo "Role:      $LAMBDA_ROLE_ARN"
echo ""

# ── Step 1: Build and package each Lambda ─────────────────────────────────────

build_lambda() {
    local name=$1
    echo "Building $name..."
    dotnet publish "$SCRIPT_DIR/$name/$name.csproj" \
        --configuration Release \
        --runtime linux-x64 \
        --output "$SCRIPT_DIR/$name/publish" \
        --verbosity quiet

    cp "$SCRIPT_DIR/$name/publish/$name" "$SCRIPT_DIR/$name/bootstrap"
    (cd "$SCRIPT_DIR/$name" && zip -j "$name.zip" bootstrap)
    echo "  → $name.zip ($(du -sh "$SCRIPT_DIR/$name/$name.zip" | cut -f1))"
}

build_lambda PlanAgent
build_lambda ExecuteAgent
build_lambda SummarizeAgent

# ── Step 2: Deploy Lambda functions ───────────────────────────────────────────

deploy_lambda() {
    local name=$1
    local zip_path="$SCRIPT_DIR/$name/$name.zip"

    if aws lambda get-function --function-name "durable-workflow-$name" --region "$REGION" &>/dev/null; then
        echo "Updating $name..."
        aws lambda update-function-code \
            --function-name "durable-workflow-$name" \
            --zip-file "fileb://$zip_path" \
            --region "$REGION" \
            --output text --query 'FunctionArn'
        aws lambda wait function-updated \
            --function-name "durable-workflow-$name" \
            --region "$REGION"
    else
        echo "Creating $name..."
        aws lambda create-function \
            --function-name "durable-workflow-$name" \
            --runtime provided.al2023 \
            --handler bootstrap \
            --role "$LAMBDA_ROLE_ARN" \
            --zip-file "fileb://$zip_path" \
            --memory-size 512 \
            --timeout 60 \
            --region "$REGION" \
            --output text --query 'FunctionArn'
        aws lambda wait function-active \
            --function-name "durable-workflow-$name" \
            --region "$REGION"
    fi
}

deploy_lambda PlanAgent
deploy_lambda ExecuteAgent
deploy_lambda SummarizeAgent

# ── Step 3: Get Lambda ARNs ────────────────────────────────────────────────────

PLAN_ARN=$(aws lambda get-function --function-name "durable-workflow-PlanAgent" \
    --region "$REGION" --query 'Configuration.FunctionArn' --output text)
EXECUTE_ARN=$(aws lambda get-function --function-name "durable-workflow-ExecuteAgent" \
    --region "$REGION" --query 'Configuration.FunctionArn' --output text)
SUMMARIZE_ARN=$(aws lambda get-function --function-name "durable-workflow-SummarizeAgent" \
    --region "$REGION" --query 'Configuration.FunctionArn' --output text)

echo ""
echo "Lambda ARNs:"
echo "  Plan:      $PLAN_ARN"
echo "  Execute:   $EXECUTE_ARN"
echo "  Summarize: $SUMMARIZE_ARN"

# ── Step 4: Substitute ARNs into the state machine definition ─────────────────
# This is what SAM's DefinitionSubstitutions does — we do it with sed.

ASL=$(sed \
    -e "s|\${PlanAgentArn}|$PLAN_ARN|g" \
    -e "s|\${ExecuteAgentArn}|$EXECUTE_ARN|g" \
    -e "s|\${SummarizeAgentArn}|$SUMMARIZE_ARN|g" \
    "$SCRIPT_DIR/statemachine.asl.json")

# ── Step 5: Create or update the state machine ────────────────────────────────

STATE_MACHINE_NAME="durable-workflow-research"

# The state machine needs an IAM role that allows it to invoke Lambda.
# We create a minimal inline role here for the demo.
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
SFN_ROLE_NAME="durable-workflow-sfn-role"

if ! aws iam get-role --role-name "$SFN_ROLE_NAME" &>/dev/null; then
    echo ""
    echo "Creating Step Functions execution role..."
    aws iam create-role \
        --role-name "$SFN_ROLE_NAME" \
        --assume-role-policy-document '{
            "Version": "2012-10-17",
            "Statement": [{
                "Effect": "Allow",
                "Principal": {"Service": "states.amazonaws.com"},
                "Action": "sts:AssumeRole"
            }]
        }' --output text --query 'Role.Arn'

    aws iam put-role-policy \
        --role-name "$SFN_ROLE_NAME" \
        --policy-name "InvokeLambda" \
        --policy-document "{
            \"Version\": \"2012-10-17\",
            \"Statement\": [{
                \"Effect\": \"Allow\",
                \"Action\": \"lambda:InvokeFunction\",
                \"Resource\": [\"$PLAN_ARN\", \"$EXECUTE_ARN\", \"$SUMMARIZE_ARN\"]
            }]
        }"
    sleep 10  # IAM propagation
fi

SFN_ROLE_ARN=$(aws iam get-role --role-name "$SFN_ROLE_NAME" \
    --query 'Role.Arn' --output text)

EXISTING_ARN=$(aws stepfunctions list-state-machines --region "$REGION" \
    --query "stateMachines[?name=='$STATE_MACHINE_NAME'].stateMachineArn" \
    --output text)

if [ -n "$EXISTING_ARN" ]; then
    echo ""
    echo "Updating state machine..."
    aws stepfunctions update-state-machine \
        --state-machine-arn "$EXISTING_ARN" \
        --definition "$ASL" \
        --role-arn "$SFN_ROLE_ARN" \
        --region "$REGION" \
        --output text --query 'updateDate'
    STATE_MACHINE_ARN="$EXISTING_ARN"
else
    echo ""
    echo "Creating state machine..."
    STATE_MACHINE_ARN=$(aws stepfunctions create-state-machine \
        --name "$STATE_MACHINE_NAME" \
        --definition "$ASL" \
        --role-arn "$SFN_ROLE_ARN" \
        --type EXPRESS \
        --region "$REGION" \
        --output text --query 'stateMachineArn')
fi

echo ""
echo "=== Deploy complete ==="
echo ""
echo "State machine ARN:"
echo "  $STATE_MACHINE_ARN"
echo ""
echo "To trigger an execution:"
echo "  aws stepfunctions start-execution \\"
echo "    --state-machine-arn $STATE_MACHINE_ARN \\"
echo "    --input '{\"topic\": \"serverless AI agents on AWS\"}' \\"
echo "    --region $REGION"
echo ""
echo "To check execution status:"
echo "  aws stepfunctions list-executions \\"
echo "    --state-machine-arn $STATE_MACHINE_ARN \\"
echo "    --region $REGION"
