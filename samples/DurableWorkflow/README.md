# DurableWorkflow — Step Functions + Strands Agents .NET

This sample demonstrates how to build durable, multi-step agentic workflows on AWS using **AWS Step Functions** to orchestrate three separate **Strands Agents .NET** Lambda functions. Each step is an independent agent with its own tool set. State flows automatically between steps via the Step Functions execution context.

## Architecture

```
Input: { "topic": "serverless AI agents on AWS" }
         │
         ▼
┌─────────────────────┐
│     PlanAgent       │  Lambda 1
│                     │  Receives topic, produces a structured
│  [ValidateTopic]    │  research plan with 3 focus areas
│  tool               │
└─────────┬───────────┘
          │ ResearchPlan { topic, focusAreas[], objective }
          ▼
┌─────────────────────┐
│    ExecuteAgent     │  Lambda 2
│                     │  Receives plan, researches each focus
│  [Research]         │  area using a tool, returns findings
│  tool               │
│                     │  ← Retry (2x) + Catch on failure
└─────────┬───────────┘
          │ ResearchFindings { topic, objective, focusAreas[], findings }
          ▼
┌─────────────────────┐
│   SummarizeAgent    │  Lambda 3
│                     │  Receives findings, synthesizes an
│  (no tools)         │  executive summary
└─────────┬───────────┘
          │ WorkflowResult { topic, summary, focusAreas[] }
          ▼
Output: executive summary
```

Each Lambda is a completely independent process. If the workflow is interrupted between steps — Lambda timeout, transient error, infrastructure failure — Step Functions resumes from the last successful step. The agent code has zero knowledge of the orchestration layer.

## How state passes between steps

Step Functions passes the **entire output** of each Lambda as the **input** to the next Lambda. The `ResultPath: "$"` setting in the ASL definition replaces the current state with the Lambda's return value.

```
PlanAgent returns:
  { "topic": "...", "focusAreas": [...], "objective": "..." }

Step Functions passes this directly to ExecuteAgent as its input.

ExecuteAgent returns:
  { "topic": "...", "objective": "...", "focusAreas": [...], "findings": "..." }

Step Functions passes this directly to SummarizeAgent as its input.
```

No shared database, no message queue, no session manager. The state machine is the state store.

## Prerequisites

- .NET 10 SDK
- AWS CLI configured with credentials
- Amazon Bedrock access enabled (`us.anthropic.claude-haiku-4-5-20251001-v1:0`)
- An IAM role for Lambda execution with these permissions:
  - `bedrock:InvokeModel` and `bedrock:InvokeModelWithResponseStream` on `*`
  - `logs:CreateLogGroup`, `logs:CreateLogStream`, `logs:PutLogEvents`

## Deploy

```bash
chmod +x deploy.sh

# Set your Lambda execution role ARN
export LAMBDA_ROLE_ARN=arn:aws:iam::YOUR_ACCOUNT:role/YOUR_LAMBDA_ROLE

# Deploy to us-east-1 (default)
./deploy.sh

# Or specify a region
REGION=us-west-2 ./deploy.sh
```

The script:
1. Builds and packages each Lambda (`dotnet publish` → zip)
2. Creates or updates the three Lambda functions via `aws lambda create-function`
3. Substitutes Lambda ARNs into `statemachine.asl.json`
4. Creates a minimal Step Functions execution role
5. Creates or updates the state machine via `aws stepfunctions create-state-machine`

## Trigger an execution

```bash
aws stepfunctions start-execution \
  --state-machine-arn YOUR_STATE_MACHINE_ARN \
  --input '{"topic": "serverless AI agents on AWS"}' \
  --region us-east-1
```

## Check execution status

```bash
# List recent executions
aws stepfunctions list-executions \
  --state-machine-arn YOUR_STATE_MACHINE_ARN \
  --region us-east-1

# Get execution details and output
aws stepfunctions describe-execution \
  --execution-arn YOUR_EXECUTION_ARN \
  --region us-east-1
```

## Clean up

```bash
# Delete the state machine
aws stepfunctions delete-state-machine \
  --state-machine-arn YOUR_STATE_MACHINE_ARN \
  --region us-east-1

# Delete the Lambda functions
for fn in PlanAgent ExecuteAgent SummarizeAgent; do
  aws lambda delete-function --function-name "durable-workflow-$fn" --region us-east-1
done

# Delete the Step Functions IAM role
aws iam delete-role-policy --role-name durable-workflow-sfn-role --policy-name InvokeLambda
aws iam delete-role --role-name durable-workflow-sfn-role
```

---

## Architecture decision: Why not AWS SAM?

This sample uses raw AWS CLI commands instead of AWS SAM (`template.yaml`). This was a deliberate choice. Here's the reasoning.

### What SAM provides

SAM is a CloudFormation extension that simplifies serverless application deployment. Its main benefits for Step Functions are:

- **`DefinitionSubstitutions`** — substitutes Lambda ARNs into the ASL definition at deploy time
- **Policy templates** — shorthand for common IAM policies
- **Stack lifecycle** — creates/updates/deletes all resources as a unit via CloudFormation

### Why SAM is not the right fit here

**1. Prerequisite burden for a sample**

SAM requires a separate install (`pip install aws-sam-cli`). A developer cloning this repo to understand the pattern now needs Python, pip, and SAM CLI before running anything. This sample only needs the AWS CLI and .NET SDK — the same tools required by the AotLambda sample.

**2. Abstraction obscures the pattern**

The purpose of this sample is to show the *Step Functions + Lambda orchestration pattern*, not to teach SAM. SAM's `template.yaml` adds a layer of indirection between the developer and what's actually being deployed. The `deploy.sh` script makes every resource creation explicit — you can read it and know exactly what AWS resources exist.

**3. Heavyweight for a demo**

SAM creates a CloudFormation stack, an S3 bucket for artifacts, and manages stack lifecycle. For a sample that a developer runs once to understand the pattern, this is significant overhead. The raw CLI approach creates exactly the resources needed and nothing else.

**4. Inconsistent with the project philosophy**

Strands Agents .NET is built around "don't over-engineer." Using SAM for a sample that can be deployed with 50 lines of bash contradicts that.

**5. ARN substitution is trivial without SAM**

The one concrete thing SAM provides here — substituting Lambda ARNs into the ASL definition — is handled by three `sed` substitutions in `deploy.sh`. This is not a reason to add a tool dependency.

### When SAM would be the right choice

- You're building a production application that needs to be deployed repeatedly across environments
- You need API Gateway triggers, EventBridge schedules, or other SAM-specific event sources
- You want CloudFormation stack lifecycle management (rollback, drift detection)
- Your team already uses SAM and consistency matters more than minimalism

None of these apply to a sample demonstrating a pattern.

### AWS documentation alignment

The [official AWS Step Functions tutorial](https://docs.aws.amazon.com/step-functions/latest/dg/tutorial-creating-lambda-state-machine.html) for Lambda integration uses direct AWS CLI commands, not SAM. SAM is recommended for building serverless *applications*, not for demonstrating *patterns*.

---

## Comparison with Microsoft Agent Framework (MAF) DurableTask

The most common question about this sample: "Why not use MAF's built-in `Microsoft.Agents.AI.DurableTask` instead?"

### MAF DurableTask approach

MAF ships a `Microsoft.Agents.AI.DurableTask` integration that provides workflow durability inside the framework. A durable workflow in MAF looks roughly like:

```csharp
// MAF — durability is built into the framework
var workflow = new WorkflowBuilder()
    .AddStep("plan", planAgent)
    .AddStep("execute", executeAgent)
    .AddStep("summarize", summarizeAgent)
    .Build();

await workflow.RunAsync(input);
// Framework handles checkpointing, retry, and resume internally
```

### Strands Agents .NET approach (this sample)

```csharp
// Strands Agents .NET — each agent is a plain Lambda function
// Step Functions handles orchestration, checkpointing, and retry
var agent = new Agent(model, systemPrompt, toolProviders: [...]);
var result = await agent.InvokeAsync(input);
return result; // Step Functions passes this to the next Lambda
```

### Trade-off table

| Dimension | MAF DurableTask | Strands Agents .NET + Step Functions |
|---|---|---|
| Durability mechanism | Built into the framework | AWS Step Functions (platform) |
| Setup complexity | Add NuGet package | Deploy state machine + Lambda functions |
| Observability | Framework logs | Step Functions console, CloudWatch, X-Ray |
| Retry configuration | Code | ASL JSON (declarative, no redeploy) |
| State storage | Framework-managed | Step Functions execution history |
| Max workflow duration | Hours (configurable) | 1 year (Standard) / 5 min (Express) |
| Cost model | Lambda execution time only | Lambda + Step Functions state transitions |
| Cloud target | Azure-native (Durable Task) | AWS-native |
| Portability | Tied to MAF | Step Functions is independent of the agent framework |
| Visual debugging | DevUI | Step Functions console execution graph |
| Human-in-the-loop | Callbacks | `waitForTaskToken` integration pattern |

### When to pick each

**Pick MAF DurableTask if:**
- You deploy to Azure
- You want workflow durability built into the framework with minimal infrastructure setup
- Your team prefers code-first workflow definition over declarative ASL
- You're already using MAF and want consistency

**Pick Strands Agents .NET + Step Functions if:**
- You deploy to AWS
- You want platform-native observability (Step Functions console, CloudWatch, X-Ray)
- You want to change retry/timeout configuration without redeploying agent code
- You want workflows that can run for hours or days (Standard Workflows)
- You prefer separating orchestration concerns from agent logic
- Your durability requirements are met by the platform you're already running on

### The key philosophical difference

MAF's approach: the framework owns durability. Strands Agents .NET's approach: the platform owns durability, the framework stays lightweight. Neither is wrong — they reflect different priorities. MAF optimizes for developer convenience; Strands Agents .NET optimizes for operational transparency and AWS-native integration.

Both support MCP and A2A, so agents from either framework can interoperate.
