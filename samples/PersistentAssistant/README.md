# PersistentAssistant

A personal assistant that picks up exactly where you left off on every restart. When the conversation grows long, it summarises old messages with the model and keeps only a compact memory — so the context window never fills up no matter how long you use it.

## SDK concepts demonstrated

**`FileSessionManager`** — saves the full `AgentSession` (messages + state snapshot) as JSON to `~/.strands/persistent-assistant/`. On the next run, the session is loaded and messages are replayed into the conversation manager before the agent starts.

**`SummarizingConversationManager`** — tracks message count and, when the threshold is exceeded, calls the model to produce a summary of the oldest messages, then replaces them with that summary. The threshold here is set to 8 for demo purposes; in production you'd use 40+.

**`AgentState`** — a key-value bag that travels with the session. This sample stores `user.name` extracted from natural language ("My name is...") and restores it on resume so the agent greets you by name on the next run.

**Manual session control** — the agent is created without a `sessionManager` parameter so we control the session ID. After every turn we call `await sessionManager.SaveAsync(SessionId, ...)` explicitly, giving us a stable, predictable session file rather than a new GUID each turn.

## Scenario

First run: the assistant greets you as a new user. You can tell it your name, preferences, or anything you want it to remember. Type `quit` to exit — the session is saved. Second run: it loads your history, greets you by name, and continues the conversation. After 8 messages the oldest turns are summarised silently in the background.

## How to run

```bash
dotnet run --project samples/PersistentAssistant            # start or resume
dotnet run --project samples/PersistentAssistant -- --reset # wipe and restart
```

Session is saved at `~/.strands/persistent-assistant/persistent-session.json`.

## Where you'd use these patterns

- **Personal productivity assistants** — any CLI or desktop tool where the user expects the AI to remember previous sessions without a database.
- **Long-running customer conversations** — agents embedded in support or sales workflows where a single conversation can span days or weeks.
- **Cost-controlled agents** — `SummarizingConversationManager` is the primary mechanism for keeping token spend bounded on long-lived sessions without losing semantic continuity.
