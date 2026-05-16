using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using StrandsAgents.Core;
using StrandsAgents.Models.Bedrock;
using System.Text.Json.Serialization;

// SummarizeAgent — Step 3 (final) of the DurableWorkflow state machine.
//
// Receives the ResearchFindings produced by ExecuteAgent and uses an agent
// to synthesize a concise executive summary. Returns the final WorkflowResult
// as the state machine's output.

var handler = async (ResearchFindings input, ILambdaContext context) =>
{
    context.Logger.LogInformation($"SummarizeAgent: synthesizing findings for '{input.Topic}'");

    var agent = new Agent(
        model: new BedrockModel(
            region: Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1",
            modelId: "us.anthropic.claude-haiku-4-5-20251001-v1:0"),
        systemPrompt: """
            You are a research synthesis assistant. Given research findings, produce a clear,
            concise executive summary (3-5 sentences) that captures the key insights and
            actionable conclusions. Write for a technical audience.
            """);

    var result = await agent.InvokeAsync(
        $"Synthesize these research findings into an executive summary.\n\n" +
        $"Topic: {input.Topic}\n" +
        $"Objective: {input.Objective}\n" +
        $"Focus areas: {string.Join(", ", input.FocusAreas)}\n\n" +
        $"Findings:\n{input.Findings}");

    context.Logger.LogInformation($"SummarizeAgent: summary complete");

    return new WorkflowResult
    {
        Topic = input.Topic,
        Summary = result.Message,
        FocusAreas = input.FocusAreas
    };
};

await LambdaBootstrapBuilder
    .Create(handler, new SourceGeneratorLambdaJsonSerializer<WorkflowJsonContext>())
    .Build()
    .RunAsync();

// ── Shared data contracts ─────────────────────────────────────────────────────

public record ResearchFindings
{
    public string Topic { get; init; } = "";
    public string Objective { get; init; } = "";
    public string[] FocusAreas { get; init; } = [];
    public string Findings { get; init; } = "";
}

public record WorkflowResult
{
    public string Topic { get; init; } = "";
    public string Summary { get; init; } = "";
    public string[] FocusAreas { get; init; } = [];
}

[JsonSerializable(typeof(ResearchFindings))]
[JsonSerializable(typeof(WorkflowResult))]
[JsonSerializable(typeof(string))]
public partial class WorkflowJsonContext : JsonSerializerContext { }
