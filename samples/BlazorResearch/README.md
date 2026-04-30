# BlazorResearch

A Blazor Server research portal where three specialist agents — technology, market, and trends — run simultaneously and feed their findings into a synthesis that streams back to the browser in real time. No JavaScript written; all interactivity is driven by Blazor's SignalR channel and `IAsyncEnumerable`.

## SDK concepts demonstrated

**Parallel agent fan-out** — `Task.WhenAll` launches all three analyst agents concurrently. Each agent completes independently; `InvokeAsync(StateHasChanged)` is called from the background task as each one finishes, so the cards in the UI flip from spinner to result the moment that agent is done — not when all three are done.

**Streaming into a live UI** — The synthesis agent uses `StreamAsync`, which returns `IAsyncEnumerable<StreamEvent>`. Each `TextDeltaEvent` is appended to a string and triggers a `StateHasChanged`, producing the character-by-character streaming effect without polling or websocket code.

**Structured output extraction** — After synthesis, `GetStructuredOutputAsync<ResearchReport>` extracts a typed `record` (topic, maturity level, key finding, outlook, confidence score) from the free-form text. The SDK retries automatically on parse failure with a self-correcting prompt.

**Source-generated tools** — `ResearchTool.Search` is decorated with `[Tool]`. The Roslyn generator emits `ResearchTool_Search_Tool` at compile time. It is registered in DI as `ITool` and injected into the Razor component — no reflection at runtime.

## Scenario

You enter a research topic (or pick from the chips). Three agents independently research it from different angles. Their results appear as they arrive. A fourth agent reads all three and writes an executive synthesis that streams into view. Finally, a structured summary card is extracted and rendered.

## How to run

```bash
dotnet run --project samples/BlazorResearch
```

Open **http://localhost:5050** — requires AWS credentials configured for Bedrock.

## Where you'd use these patterns

- **Internal research tools** — competitive intelligence portals where multiple analysts (pricing, technical, regulatory) work in parallel and a senior agent synthesises.
- **Medical or legal summarisation** — parallel agents covering different document sections, streamed summary to the reviewer's screen.
- **Any Blazor app needing background AI** — the `InvokeAsync(StateHasChanged)` pattern is directly reusable anywhere you run agent work off the UI thread.
