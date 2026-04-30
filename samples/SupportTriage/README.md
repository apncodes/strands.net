# SupportTriage

A support ticket router built as an agent graph. A triage agent reads each ticket and decides which specialist to hand it to. The specialist looks up the account in a CRM tool and writes the response. A hook fires before every CRM lookup so you can log, audit, or block it.

## SDK concepts demonstrated

**`GraphBuilder` with `AddConditionalEdge`** — the triage agent's response is parsed for a `ROUTE: <category>` prefix. The conditional edge function reads that prefix and returns the name of the next node (`billing`, `technical`, `account`, or `general`). Nodes with no outgoing edge terminate the graph and return their result.

**`HookRegistry`** — per-ticket hooks are created fresh for each run so accumulated state (token counts) doesn't carry over. `BeforeToolCallEvent` fires before the CRM lookup; uncommenting two lines turns auto-approval into a human-in-the-loop gate. `AfterModelCallEvent` aggregates token usage across every model call in the graph run.

**`GetStructuredOutputAsync<T>`** — after the specialist responds, a separate agent extracts a typed `TicketResolution` (category, next action, resolved flag) from the free-form text. This agent runs in an isolated message list — it doesn't pollute the specialist's conversation history.

**Source-generated tools** — `TicketLookupTool.LookupTicket` is decorated with `[Tool]`. The generated `TicketLookupTool_LookupTicket_Tool` carries its JSON schema as a compile-time string literal.

## Scenario

Three sample tickets are processed in sequence — a billing dispute, a crash report, and an account change request. Each goes through triage → specialist routing → CRM lookup → response → structured extraction. The terminal shows which route was taken, the specialist's reply, and the extracted resolution fields.

## How to run

```bash
dotnet run --project samples/SupportTriage
```

To require manual operator approval before each CRM lookup, uncomment the HITL block in `Program.cs`:

```csharp
// Console.Write("  Approve? [Y/n]: ");
// if (Console.ReadLine()?.Trim().ToLower() == "n") e.Interrupt = true;
```

## Where you'd use these patterns

- **Tier-1 support automation** — triage + specialist routing handles the majority of common ticket types without human involvement; the hook pattern makes it easy to escalate edge cases.
- **Compliance workflows** — the `BeforeToolCallEvent` interrupt pattern is the standard mechanism for requiring human sign-off before any action that touches sensitive data or triggers a real side effect.
- **Any multi-step classification pipeline** — graph routing isn't limited to support; it applies to document classification, intake forms, or any flow where an initial model call determines which downstream agent runs.
