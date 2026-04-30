# FinanceAssistant

Four specialist analyst agents — price, fundamentals, news, and risk — run concurrently on a single stock ticker. Their findings are synthesised into a narrative and then extracted into a typed `EquityReport` record.

## SDK concepts demonstrated

**`ParallelOrchestrator`** — wraps `Task.WhenAll` over multiple `IAgent` instances. Each agent receives the same prompt but has a different system prompt and a single domain-specific tool. Results are collected in order once all four complete.

**Role specialisation via system prompt** — one codebase produces four genuinely different analysts purely by varying the system prompt. The price analyst focuses on momentum and volume; the fundamentals analyst on P/E and revenue; each uses only the tool relevant to its domain. This is the primary pattern for building analyst swarms.

**Source-generated tools** — `FinanceDataTool` has four `[Tool]`-decorated methods (`GetQuote`, `GetFinancials`, `GetHeadlines`, `GetRiskMetrics`). The source generator emits four independent `ITool` wrappers, each with its own JSON schema, from a single class.

**`GetStructuredOutputAsync<T>`** — after synthesis, a final agent extracts a typed `EquityReport` (ticker, recommendation, price target, thesis, risks, confidence score) from the free-form narrative. The SDK retries up to three times with a self-correcting prompt if JSON parsing fails.

## Scenario

You pass a ticker on the command line (default `NVDA`). Four agents run in parallel, each calling its dedicated data tool and producing a specialist assessment. A synthesis agent integrates the four views into an investment thesis. A structured extraction agent converts that thesis into a printable report card.

## How to run

```bash
dotnet run --project samples/FinanceAssistant           # NVDA (default)
dotnet run --project samples/FinanceAssistant -- AAPL
dotnet run --project samples/FinanceAssistant -- MSFT
```

## Where you'd use these patterns

- **Equity research platforms** — run domain-specific analyst agents in parallel and surface a blended view faster than any single-agent chain.
- **Due diligence tools** — same swarm pattern applied to M&A screening: legal, financial, technical, and market risk agents running concurrently.
- **Any structured report pipeline** — `GetStructuredOutputAsync<T>` is reusable wherever you need a model's free-form output normalised into a typed object for downstream processing.
