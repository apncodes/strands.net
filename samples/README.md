# Strands .NET SDK — Samples

## Prerequisites

- **.NET 10 SDK** — <https://dot.net>
- **AWS credentials** with Bedrock model access enabled in `us-east-1`

```bash
export AWS_REGION=us-east-1
export AWS_ACCESS_KEY_ID=...
export AWS_SECRET_ACCESS_KEY=...
```

Enable model access for `us.anthropic.claude-haiku-4-5-20251001-v1:0` in the
[Bedrock console](https://console.aws.amazon.com/bedrock/home#/modelaccess).

## Samples

| Sample | SDK features |
|--------|-------------|
| [CliAgent](CliAgent/) | `StreamAsync`, `CalculatorTool`, event handling |
| [AspNetAgent](AspNetAgent/) | `InvokeAsync`, SSE via `StreamAsync`, minimal API |
| [DiAgent](DiAgent/) | DI extensions, `FileReadTool`, `SlidingWindowStrategy`, sessions |
| [FileAgent](FileAgent/) | `FileReadTool`, `FileWriteTool`, context window trimming |
| [MultiAgentPipeline](MultiAgentPipeline/) | `AsTool()` — agent wrapped as a tool |
| [OrchestratedResearch](OrchestratedResearch/) | `PipelineOrchestrator`, `ParallelOrchestrator`, `AgentTool` |
| [SupportTriage](SupportTriage/) | `GraphBuilder`, conditional routing, hooks, `GetStructuredOutputAsync` |
| [CustomerServiceApi](CustomerServiceApi/) | Multi-session REST API, `FileSessionManager`, SSE |
| [FinanceAssistant](FinanceAssistant/) | 4-agent `ParallelOrchestrator`, source-generated tools, structured output |
| [PersistentAssistant](PersistentAssistant/) | `SummarizingConversationManager`, `FileSessionManager`, `AgentState` |
| [DistributedAgents](DistributedAgents/) | `A2AAgent`, `MapA2AEndpoint`, `AgentTool` — two processes |
| [ChatUI](ChatUI/) | Browser chat UI, SSE streaming, tool badges — open http://localhost:5000 |
| [BlazorResearch](BlazorResearch/) | Blazor Server, parallel agents, live card updates — open http://localhost:5050 |

Each sample folder has its own README with the run command and any non-obvious details.
