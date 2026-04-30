# Strands.Core

The [Strands Agents](https://strandsagents.com) framework for .NET — model-driven agentic AI built natively in C# 13.

```bash
dotnet add package Strands.Core
dotnet add package Strands.Models.Bedrock
```

```csharp
using Strands.Core;
using Strands.Models.Bedrock;

var agent = new Agent(
    model: new BedrockModel("us-east-1"),
    systemPrompt: "You are a helpful assistant.",
    tools: [new CalculatorTool_Calculate_Tool(new CalculatorTool())]
);

// Single invocation
var result = await agent.InvokeAsync("What is 42 multiplied by 1764?");
Console.WriteLine(result.Message);

// Streaming
await foreach (var evt in agent.StreamAsync("Explain recursion"))
    if (evt is TextDeltaEvent delta)
        Console.Write(delta.Delta);
```

Full documentation and samples: [github.com/apncodes/strands.net](https://github.com/apncodes/strands.net)
