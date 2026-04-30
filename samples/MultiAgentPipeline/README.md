# MultiAgentPipeline

## SDK concepts demonstrated

**`PipelineOrchestrator`** (Part A) — chains Researcher → Writer → Reviewer sequentially. Each stage receives the previous stage's output as its prompt. `StreamAsync` yields `PipelineStageEvent` wrappers carrying the stage index and name alongside the inner event, so output can be attributed to each agent as it streams.

**`Task.WhenAll` parallel fan-out with timestamps** (Part B) — two specialist researchers launch simultaneously. Timestamps printed as each one finishes prove they ran in parallel: both `+1.2s` and `+1.4s` will appear before a sequential run would even finish the first agent. This is `Task.WhenAll` doing what it says.

**Role specialisation via system prompt** — the same `Agent` constructor produces a Researcher, a Writer, and a Reviewer purely by varying the system prompt. No subclasses, no configuration files.

## Scenario

Part A runs a clean sequential pipeline on a fixed topic and streams all three stages to the console. Part B launches two researchers with different angles simultaneously, shows their completion timestamps, then feeds combined findings into the sequential Writer → Reviewer chain.

## How to run

```bash
dotnet run --project samples/MultiAgentPipeline
```

## Where you'd use these patterns

- **Content pipelines** — research → draft → edit → publish, where each stage hands off to the next.
- **Parallel data gathering** — run multiple specialist agents concurrently (legal, financial, technical) and combine their findings into a single downstream step.
