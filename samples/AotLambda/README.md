# AotLambda — NativeAOT Strands Agent on AWS Lambda

This sample publishes a Strands Agents .NET agent as a **NativeAOT** AWS Lambda function using the `provided.al2023` custom runtime. The result is a self-contained native binary with no .NET runtime dependency and sub-100ms cold-start init duration.

## Why AOT?

Standard .NET Lambda functions use the JIT runtime. On first invocation (cold start), the runtime must load, JIT-compile the code, and initialize the agent. This typically takes 200–500ms.

NativeAOT compiles everything to native machine code at build time. There is no JIT warm-up. Cold-start init duration drops to under 100ms — often under 50ms for simple agents.

The Strands Agents .NET tool system is designed for this: the `[Tool]` attribute triggers a Roslyn source generator that emits compile-time `ITool` wrappers. Zero runtime reflection means zero trimming surprises.

## Prerequisites

- [.NET 10 SDK](https://dot.net)
- [AWS CLI](https://aws.amazon.com/cli/) configured with credentials
- Amazon Bedrock access enabled in your AWS account (Claude Haiku model)
- **Linux build environment** — NativeAOT cross-compilation from macOS to `linux-x64` requires a Linux linker. Use one of:
  - A Linux machine or WSL2
  - Docker: `docker run --rm -v $(pwd):/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet publish ...`
  - GitHub Actions (Ubuntu runner)

## Build and publish

```bash
# From the repo root (strands.net/)
dotnet publish samples/AotLambda/AotLambda.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --output samples/AotLambda/publish

# Package for Lambda
cd samples/AotLambda/publish
zip -j function.zip AotLambda
```

The output binary is `AotLambda` — a self-contained native executable, no .NET runtime required.

## Deploy to AWS Lambda

```bash
# Create the Lambda function (first time)
aws lambda create-function \
  --function-name strands-aot-demo \
  --runtime provided.al2023 \
  --handler bootstrap \
  --role arn:aws:iam::YOUR_ACCOUNT:role/YOUR_LAMBDA_ROLE \
  --zip-file fileb://function.zip \
  --memory-size 512 \
  --timeout 30 \
  --environment "Variables={AWS_REGION=us-east-1}"

# Update an existing function
aws lambda update-function-code \
  --function-name strands-aot-demo \
  --zip-file fileb://function.zip
```

**Required IAM permissions for the Lambda execution role:**
- `bedrock:InvokeModel` and `bedrock:InvokeModelWithResponseStream` on the model ARN

## Invoke

```bash
aws lambda invoke \
  --function-name strands-aot-demo \
  --payload '"What is the weather in London?"' \
  --cli-binary-format raw-in-base64-out \
  response.json

cat response.json
```

## Measure cold-start duration

Force a cold start by updating the function configuration (this resets the execution environment):

```bash
aws lambda update-function-configuration \
  --function-name strands-aot-demo \
  --description "cold-start-test-$(date +%s)"

# Wait for update to complete
aws lambda wait function-updated --function-name strands-aot-demo

# Invoke (this will be a cold start)
aws lambda invoke \
  --function-name strands-aot-demo \
  --payload '"What is the weather in London?"' \
  --cli-binary-format raw-in-base64-out \
  --log-type Tail \
  --query 'LogResult' \
  --output text \
  response.json | base64 --decode | grep "Init Duration"
```

Or use CloudWatch Logs Insights:

```
fields @timestamp, @message
| filter @message like /Init Duration/
| parse @message "Init Duration: * ms" as initDuration
| stats avg(initDuration), min(initDuration), max(initDuration)
```

## Benchmarks

Measured on AWS Lambda `us-east-1`, 512 MB memory, `provided.al2023` runtime, 5 cold-start invocations. The agent invokes the `GetWeather` tool on each call — this is a real end-to-end measurement including model inference.

| Measurement | AOT (`provided.al2023`) | JIT (`.NET 10 managed`) | Notes |
|---|---|---|---|
| Cold start init duration (avg) | **118 ms** | 200–500 ms | CloudWatch `Init Duration`, 5 runs: 107/107/108/124/140 ms |
| Cold start init duration (min) | **107 ms** | — | Best observed cold start |
| Warm invocation (p50) | ~2,500 ms | ~2,500 ms | Model inference dominates warm path; no difference |
| Memory used | **52 MB** | ~80–120 MB | Includes agent + Bedrock SDK + tool execution |
| Binary size | **14 MB** | N/A (runtime separate) | Compressed zip; uncompressed ~36 MB |

The AOT binary initializes in ~110ms on average. The JIT baseline for the same code on the `dotnet10` managed runtime typically shows 200–500ms init duration. Warm invocation time is dominated by Bedrock model inference (~2–3s for Claude Haiku) — identical between AOT and JIT.

## How it works

```
[Tool] attribute on partial class WeatherTools
         ↓
Roslyn source generator (compile time)
         ↓
WeatherTools_GetWeather_Tool.g.cs  ← generated ITool wrapper
WeatherTools_IToolProvider.g.cs    ← generated IToolProvider
         ↓
ILC (IL Compiler) — compiles everything to native x64
         ↓
Single native binary: AotLambda (~25 MB)
         ↓
Lambda cold start: load binary → execute → no JIT
```

The source generator emits all tool schema and dispatch code at compile time. There is no `Type.GetMethod()`, no `Activator.CreateInstance()`, no `JsonSerializer` with reflection — the hot path is fully AOT-safe.

## Troubleshooting

**`clang: error: invalid linker name in argument '-fuse-ld=bfd'`**
You're building on macOS. Use Docker or a Linux machine for the final publish step.

**`NU1102: Unable to find package StrandsAgents.Core with version >= X`**
The NuGet package hasn't propagated yet. Wait a few minutes and retry, or use project references (already configured in this sample's `.csproj`).

**`IL2104: Assembly 'AWSSDK.Core' produced trim warnings`**
Suppressed in the `.csproj` — these come from AWS SDK internals and are safe for this usage pattern.
