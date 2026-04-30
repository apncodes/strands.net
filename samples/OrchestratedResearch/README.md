# OrchestratedResearch

Three orchestration patterns demonstrated back to back in a single run: sequential pipeline, parallel fan-out, and agent-as-tool delegation. Use this as a reference for choosing between the patterns.

## SDK concepts demonstrated

**`PipelineOrchestrator`** (Part A) — chains agents sequentially. Each stage receives the previous stage's output as its input prompt. `StreamAsync` on the pipeline yields `PipelineStageEvent` wrappers that carry the stage index and name alongside the inner `StreamEvent`, so you can attribute each streamed token to the correct stage.

**`ParallelOrchestrator`** (Part B) — sends the same prompt to multiple agents concurrently via `Task.WhenAll` and collects results in order. The researchers here have different system prompts so they produce independent specialist views of the same question.

**`AgentTool`** (Part C) — wraps a child agent as an `ITool` with a `prompt` parameter. The orchestrator agent calls the researcher tool by name; the tool invokes the child agent and returns its text. The parent never calls the child's `InvokeAsync` directly.

## Scenario

- Part A: a researcher produces bullet-point notes on the CAP theorem; a writer turns them into a paragraph. The pipeline streams both stages with colour-coded output.
- Part B: two researchers answer the same question simultaneously — one specialises in cloud cost optimisation, the other in developer productivity.
- Part C: an orchestrator agent delegates a software architecture question to a researcher sub-agent via tool call, then summarises the result for a non-technical audience.

## How to run

```bash
dotnet run --project samples/OrchestratedResearch
```

## Where you'd use these patterns

- **`PipelineOrchestrator`** — content pipelines (research → draft → edit → publish), data transformation chains, multi-stage reasoning.
- **`ParallelOrchestrator`** — any scenario where independent viewpoints or data sources can be gathered concurrently: market research, multi-source summarisation, ensemble scoring.
- **`AgentTool`** — building hierarchical agent systems where a coordinator delegates subtasks to specialists without tight coupling.
