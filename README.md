# Strands Agents .NET

> **The Strands Agents framework — built for .NET.**
> Model-driven agentic AI for C# developers, built on the same principles as [AWS Strands Agents](https://strandsagents.com).

[![NuGet](https://img.shields.io/nuget/v/Strands.Core?label=NuGet&color=blue)](https://www.nuget.org/packages/Strands.Core)
[![License](https://img.shields.io/badge/license-Apache--2.0-green)](LICENSE)

---

## Quickstart

```bash
dotnet add package Strands.Core
dotnet add package Strands.Models.Bedrock
dotnet add package Strands.Tools
```

```csharp
using Strands.Core;
using Strands.Models.Bedrock;

var agent = new Agent(
    model: new BedrockModel("us-east-1"),
    systemPrompt: "You are a helpful assistant.",
    tools: [new CalculatorTool_Calculate_Tool(new CalculatorTool())]
);

var result = await agent.InvokeAsync("What is 42 multiplied by 1764?");
Console.WriteLine(result.Message); // The answer is 74,088
```

Prerequisites: .NET 10 SDK, AWS credentials with Bedrock access enabled.

---

## Why Strands.NET

.NET is the dominant runtime in enterprise — Azure, AWS Lambda, Windows services, ASP.NET APIs. Until now, the Strands agentic framework was Python-only. This project brings the same model-driven loop, tool system, and multi-agent patterns to every .NET developer without requiring them to learn a new language or host a Python sidecar. The API is idiomatic C# 13 throughout: generics instead of string tags, `IAsyncEnumerable` instead of async generators, `Task.WhenAll` instead of `asyncio.gather`.

---

## Features

- **Model-driven event loop** — the LLM decides which tools to call; the SDK executes them and loops until `EndTurn`
- **Tool system** — decorate any method with `[Tool]`; the Roslyn source generator emits a compile-time `ITool` wrapper with zero runtime reflection
- **Streaming** — `StreamAsync` returns `IAsyncEnumerable<StreamEvent>` end to end with `[EnumeratorCancellation]` on every boundary
- **Hook system** — type-safe `Register<TEvent>` callbacks for `BeforeToolCall`, `AfterToolCall`, `BeforeModelCall`, `AfterModelCall`
- **Human-in-the-loop** — set `e.Interrupt = true` in any `BeforeToolCallEvent` hook to pause before sensitive actions
- **Structured output** — `GetStructuredOutputAsync<T>()` extracts typed records with automatic JSON retry
- **Session management** — `InMemorySessionManager` or `FileSessionManager`; bring your own via `ISessionManager`
- **Context window trimming** — `SlidingWindowStrategy` or `SummarizingConversationManager` for long-running agents
- **OpenTelemetry** — `ActivitySource` named `"Strands.Agent"` emits traces and metrics with zero config
- **DI integration** — `AddBedrockModel()`, `AddStrandsAgent()` for native ASP.NET Core / Worker Service wiring
- **Multi-agent graph** — `GraphBuilder` with conditional routing; `PipelineOrchestrator`; `ParallelOrchestrator`
- **Agent as tool** — wrap any `IAgent` as an `ITool` with `agent.AsTool()` for hierarchical orchestration
- **MCP** — connect any Model Context Protocol server (stdio or SSE) via `McpToolProvider`
- **A2A protocol** — expose agents over HTTP with `MapA2AEndpoint`; call remote agents with `A2AAgent` (cross-framework, cross-language)
- **Provider-agnostic** — swap `BedrockModel` for `AnthropicModel` or `OpenAICompatibleModel` in one line
- **AgentCore Runtime** *(optional)* — `MapAgentCoreEndpoints()` deploys any agent to Amazon Bedrock AgentCore Runtime in one line; managed Memory, Browser, and Code Interpreter tools available via `Strands.AgentCore`

---

## Why .NET gets its own implementation

These aren't translations — they're the patterns .NET developers already know, applied to agentic AI.

| Capability | Strands Python | Strands Agents .NET |
|---|---|---|
| Type safety | Runtime type hints | Compile-time generics |
| Streaming | `async_generator` | `IAsyncEnumerable<T>` |
| Hook registration | String event names | `Register<TEvent>` — compiler-checked |
| Tool schema | `inspect.signature()` at runtime | Roslyn source generator at compile time |
| Parallel execution | `asyncio.gather` | `Task.WhenAll` |
| DI integration | Manual wiring | `AddBedrockModel()` + `AddStrandsAgent()` |
| Enterprise hosting | WSGI / ASGI | `IHostedService` / Lambda / Azure Functions |
| MCP | ✓ | ✓ |
| A2A protocol | ✓ | ✓ (interoperable with Python agents) |
| Graph orchestration | ✓ | ✓ with parallel-node support |

---

## Multi-agent patterns

### Sequential pipeline

```csharp
var pipeline = new PipelineOrchestrator([researchAgent, writerAgent, reviewerAgent]);
var result = await pipeline.InvokeAsync("Write a report on quantum computing");
```

### Parallel fan-out

```csharp
var results = await new ParallelOrchestrator([techAgent, marketAgent, riskAgent])
    .RunAsync("Analyse this topic from your specialist perspective");
// All three run concurrently via Task.WhenAll
```

### Graph with conditional routing

```csharp
var graph = new GraphBuilder()
    .AddNode("triage",    triageAgent)
    .AddNode("billing",   billingAgent)
    .AddNode("technical", techAgent)
    .AddConditionalEdge("triage", r =>
        r.Message.Contains("billing") ? "billing" : "technical")
    .Build();
```

### Agent as tool

```csharp
var researchTool = researchAgent.AsTool("researcher", "Research a topic and return a summary");
var writerAgent  = new Agent(model, tools: [researchTool]);
```

---

## AgentCore Runtime deployment (optional)

Deploy any Strands.NET agent to Amazon Bedrock AgentCore Runtime with one line. Your agent code is unchanged.

```bash
dotnet add package Strands.AgentCore
```

```csharp
// Your agent — exactly as you configured it
builder.Services
    .AddBedrockModel("us-east-1")
    .AddStrandsAgent("You are a helpful assistant.");

// One line makes it deployable to AgentCore Runtime
var app = builder.Build();
app.MapAgentCoreEndpoints();  // POST /invocations + GET /health
app.UseAgentCorePort(8080);   // AgentCore Runtime expects port 8080
app.Run();
```

Python equivalent: `BedrockAgentCoreApp()` + `@app.entrypoint` + `app.run()`

---

## Samples

| Sample | What it shows |
|---|---|
| [CliAgent](samples/CliAgent/) | Multi-turn streaming REPL — the minimal working agent |
| [AspNetAgent](samples/AspNetAgent/) | `/chat` endpoint with session continuity and SSE streaming |
| [MultiAgentPipeline](samples/MultiAgentPipeline/) | Sequential pipeline + parallel fan-out with timestamps |
| [OrchestratedResearch](samples/OrchestratedResearch/) | All three orchestration patterns side by side |
| [SupportTriage](samples/SupportTriage/) | Graph routing, hooks, and structured output extraction |
| [CustomerServiceApi](samples/CustomerServiceApi/) | Production-shaped REST API with session persistence |
| [FinanceAssistant](samples/FinanceAssistant/) | 4-agent parallel swarm with typed report extraction |
| [PersistentAssistant](samples/PersistentAssistant/) | Cross-run memory with automatic summarization |
| [DistributedAgents](samples/DistributedAgents/) | A2A cross-process agent communication |
| [ChatUI](samples/ChatUI/) | Browser chat UI with SSE streaming and tool badges |
| [BlazorResearch](samples/BlazorResearch/) | Blazor Server portal with live parallel agent cards |
| [AgentCoreSample](samples/AgentCoreSample/) | Deploy any agent to AgentCore Runtime — `MapAgentCoreEndpoints()` in one line |

---

## Relationship to AWS Strands

Strands Agents .NET implements the same model-driven agentic architecture as [AWS Strands Agents](https://strandsagents.com), which is open source under Apache 2.0. The core concepts — event loop, tool system, hooks, multi-agent orchestration — are shared; the implementation is native C# 13 throughout. This project is not affiliated with or endorsed by AWS, but is fully compatible with the Strands ecosystem including the A2A protocol for cross-language agent communication.

---

## Contributing

PRs, issues, and feedback are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines. The biggest areas of need are additional model providers (Gemini, Ollama), more built-in tools, and real-world samples.

---

## License

Apache 2.0. See [LICENSE](LICENSE).
