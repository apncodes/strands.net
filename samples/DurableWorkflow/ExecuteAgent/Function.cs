using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using StrandsAgents.Core;
using StrandsAgents.Models.Bedrock;
using System.Text.Json.Serialization;
using ExecuteAgent;

// ExecuteAgent — Step 2 of the DurableWorkflow state machine.
//
// Receives the ResearchPlan produced by PlanAgent, uses an agent with a
// research tool to gather findings for each focus area, and returns a
// ResearchFindings record. Step Functions passes this to SummarizeAgent.
//
// This step has a Retry + Catch block in the state machine definition —
// if this Lambda fails, Step Functions retries up to 2 times with backoff
// before routing to a Failure state. The agent code itself is unchanged;
// durability is handled entirely by the platform.

var handler = async (ResearchPlan input, ILambdaContext context) =>
{
    context.Logger.LogInformation($"ExecuteAgent: researching {input.FocusAreas.Length} focus areas for '{input.Topic}'");

    var agent = new Agent(
        model: new BedrockModel(
            region: Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1",
            modelId: "us.anthropic.claude-haiku-4-5-20251001-v1:0"),
        systemPrompt: """
            You are a research execution assistant. Given a research plan, gather key findings
            for each focus area. Be concise — 2-3 sentences per area. Use the research tool
            to look up information for each focus area.
            """,
        toolProviders: [new ResearchTools()]);

    var focusAreasList = string.Join(", ", input.FocusAreas);
    var result = await agent.InvokeAsync(
        $"Research these focus areas for '{input.Topic}': {focusAreasList}. " +
        $"Objective: {input.Objective}");

    context.Logger.LogInformation($"ExecuteAgent: research complete, {result.Message.Length} chars");

    return new ResearchFindings
    {
        Topic = input.Topic,
        Objective = input.Objective,
        FocusAreas = input.FocusAreas,
        Findings = result.Message
    };
};

await LambdaBootstrapBuilder
    .Create(handler, new SourceGeneratorLambdaJsonSerializer<WorkflowJsonContext>())
    .Build()
    .RunAsync();

namespace ExecuteAgent
{
    public partial class ResearchTools
    {
        [Tool("Look up research information for a specific topic or focus area")]
        public string Research(string query) =>
            // In a real implementation this would call a search API, knowledge base, or RAG pipeline.
            // For this demo it returns structured placeholder findings that demonstrate the pattern.
            $"Research findings for '{query}': This area involves key developments in technology, " +
            $"market dynamics, and emerging trends. Current state shows significant activity with " +
            $"multiple stakeholders driving innovation. Key considerations include scalability, " +
            $"cost efficiency, and integration with existing systems.";
    }
}

// ── Shared data contracts ─────────────────────────────────────────────────────

public record ResearchPlan
{
    public string Topic { get; init; } = "";
    public string[] FocusAreas { get; init; } = [];
    public string Objective { get; init; } = "";
}

public record ResearchFindings
{
    public string Topic { get; init; } = "";
    public string Objective { get; init; } = "";
    public string[] FocusAreas { get; init; } = [];
    public string Findings { get; init; } = "";
}

[JsonSerializable(typeof(ResearchPlan))]
[JsonSerializable(typeof(ResearchFindings))]
[JsonSerializable(typeof(string))]
public partial class WorkflowJsonContext : JsonSerializerContext { }
