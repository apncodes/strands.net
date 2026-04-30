# Strands.AgentCore

Deploy any [Strands.NET](https://github.com/apncodes/strands.net) agent to [Amazon Bedrock AgentCore Runtime](https://aws.amazon.com/bedrock/agentcore/) with one line. Optionally use AgentCore managed services — Memory, Browser, and Code Interpreter — as tools your agent can invoke.

```bash
dotnet add package Strands.AgentCore
```

## Your agent code is unchanged

```csharp
using Strands.AgentCore.Hosting;
using Strands.AgentCore.Extensions;
using Strands.Extensions.DI;

var builder = WebApplication.CreateBuilder(args);

// ── Agent configuration — identical whether running locally or on AgentCore Runtime ──
builder.Services
    .AddBedrockModel("us-east-1")
    .AddAgentCoreBrowser()                  // optional managed browser
    .AddAgentCoreCodeInterpreter()          // optional managed code execution
    .AddAgentCoreSessionManager(            // optional managed session storage
        Environment.GetEnvironmentVariable("AGENTCORE_MEMORY_ID") ?? "")
    .AddStrandsAgent("You are a helpful assistant.");

// ── AgentCore hosting — one line makes this deployable to AgentCore Runtime ──
var app = builder.Build();
app.MapAgentCoreEndpoints();  // POST /invocations + GET /health
app.UseAgentCorePort(8080);   // AgentCore Runtime expects port 8080
app.Run();
```

Python equivalent: `BedrockAgentCoreApp()` + `@app.entrypoint` + `app.run()`

## Test locally before deploying

```bash
dotnet run

# Non-streaming
curl -X POST http://localhost:8080/invocations \
  -H "Content-Type: application/json" \
  -d '{"prompt": "What is 42 multiplied by 1764?"}'

# Streaming
curl -X POST http://localhost:8080/invocations \
  -H "Content-Type: application/json" \
  -H "Accept: text/event-stream" \
  -d '{"prompt": "Explain quantum computing in 3 sentences"}'

# Health check (required by AgentCore Runtime)
curl http://localhost:8080/health
```

## What this package provides

| Component | What it does |
|---|---|
| `MapAgentCoreEndpoints()` | Registers `POST /invocations` + `GET /health` on your `WebApplication` |
| `UseAgentCorePort(8080)` | Binds to port 8080 — required by AgentCore Runtime |
| `AgentCoreSessionManager` | Persists conversation history to AgentCore Memory |
| `AgentCoreMemoryTool` | Agent-initiated explicit memory operations |
| `AgentCoreBrowserTool` | Managed browser sandbox for JS-rendered pages |
| `AgentCoreCodeInterpreterTool` | Managed code execution sandbox |

## Additive and optional

This package adds zero changes to `Strands.Core`, `Strands.Models.Bedrock`, or any other package. An agent built with `Strands.Core` compiles and runs identically with or without `Strands.AgentCore` installed.
