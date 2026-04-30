# CliAgent

## SDK concepts demonstrated

**Multi-turn `StreamAsync` REPL** — a single `Agent` instance persists across the loop, so conversation history accumulates naturally. Each turn calls `StreamAsync`, which returns `IAsyncEnumerable<StreamEvent>`. The loop prints `TextDeltaEvent` deltas as they arrive — tokens appear on screen before the model finishes.

**Source-generated tool** — `CalculatorTool_Calculate_Tool` is emitted at compile time by the Roslyn source generator with the JSON schema baked in as a string literal.

**`CancellationToken` + `CancelKeyPress`** — Ctrl+C cancels the current stream cleanly and exits the loop.

## Scenario

Type anything. Ask follow-up questions — the agent remembers the full conversation. Ask for arithmetic and watch it call the calculator tool mid-stream. Type `exit` or press Ctrl+C to quit.

## How to run

```bash
dotnet run --project samples/CliAgent
```

Prerequisites: .NET 10, AWS credentials configured for Bedrock.

## Where you'd use these patterns

- **CLI utilities** — any terminal tool where you want an AI layer that accumulates context across turns.
- **Developer REPLs** — code review, log analysis, or deployment assistants that need multi-turn back-and-forth.
