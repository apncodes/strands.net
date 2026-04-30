# FileAgent

A streaming agent with read and write access to a sandboxed workspace directory. Shows the built-in file tools, coloured tool call indicators in the terminal, and automatic context-window trimming when the conversation grows long.

## SDK concepts demonstrated

**`FileReadTool` / `FileWriteTool`** — built-in tools that scope all file access to a base directory. Any path that attempts to escape the sandbox (e.g. `../../etc/passwd`) is rejected with an error result. No configuration beyond the base path is needed.

**`SlidingWindowStrategy`** — when the accumulated token count exceeds `MaxContextTokens`, the strategy drops the oldest messages from the middle of the conversation while preserving the system prompt and the most recent message. The agent never sees a "context full" error; trimming happens transparently before each model call.

**`ToolCallStartEvent` / `ToolCallResultEvent`** — the streaming loop handles these events separately from `TextDeltaEvent` to display coloured tool indicators (`▶ FileRead`, `◀ <preview>`) inline with the streamed response.

**`ModelException`** — caught at the top level and printed with the HTTP status code. This is the standard pattern for surfacing Bedrock throttling or service errors without crashing the process.

## Scenario

A temporary workspace is seeded with three markdown note files. The agent reads and writes files in response to your prompt, streaming its response and tool calls to the console. The workspace is deleted on exit.

## How to run

```bash
dotnet run --project samples/FileAgent
dotnet run --project samples/FileAgent -- "summarise note1.md in one sentence"
```

Try: `"Read note1.md and note2.md, then write a combined summary to summary.md"`

## Where you'd use these patterns

- **Coding assistants** — `FileReadTool` + `FileWriteTool` scoped to a project directory is the foundation of any agent that reads source files and writes patches or documentation.
- **Document processing pipelines** — the sandbox model is important in production: the agent can only touch files you explicitly permit, making it safe to deploy alongside real data.
- **Long-running research agents** — `SlidingWindowStrategy` + `MaxContextTokens` is the right pairing for any agent that accumulates many turns over a long session.
