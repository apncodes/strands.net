using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using StrandsAgents.Core;
using StrandsAgents.Models.Bedrock;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlanAgent;

// PlanAgent — Step 1 of the DurableWorkflow state machine.
//
// Receives the user's topic from Step Functions input, uses an agent to
// produce a structured research plan, and returns it as output.
// Step Functions passes this output as input to ExecuteAgent.

var handler = async (WorkflowInput input, ILambdaContext context) =>
{
    context.Logger.LogInformation($"PlanAgent: planning research for topic '{input.Topic}'");

    var agent = new Agent(
        model: new BedrockModel(
            region: Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1",
            modelId: "us.anthropic.claude-haiku-4-5-20251001-v1:0"),
        systemPrompt: """
            You are a research planning assistant. Given a topic, produce a concise research plan
            with exactly 3 focus areas. Respond with ONLY a JSON object in this format:
            {"topic": "...", "focusAreas": ["area1", "area2", "area3"], "objective": "..."}
            """,
        toolProviders: [new PlanningTools()]);

    var result = await agent.InvokeAsync(
        $"Create a research plan for: {input.Topic}");

    // Extract JSON from the agent response
    var json = ExtractJson(result.Message);
    context.Logger.LogInformation($"PlanAgent: plan produced — {json}");

    return JsonSerializer.Deserialize<ResearchPlan>(json, WorkflowJsonContext.Default.ResearchPlan)
        ?? new ResearchPlan { Topic = input.Topic, FocusAreas = ["Overview"], Objective = "Research the topic" };
};

await LambdaBootstrapBuilder
    .Create(handler, new SourceGeneratorLambdaJsonSerializer<WorkflowJsonContext>())
    .Build()
    .RunAsync();

static string ExtractJson(string text)
{
    var start = text.IndexOf('{');
    var end = text.LastIndexOf('}');
    return start >= 0 && end > start ? text[start..(end + 1)] : "{}";
}

namespace PlanAgent
{
    public partial class PlanningTools
    {
        [Tool("Validates that a research topic is well-formed and researchable")]
        public string ValidateTopic(string topic) =>
            topic.Length < 3 ? "Topic too short — please provide more detail" : $"Topic '{topic}' is valid and researchable";
    }
}

// ── Shared data contracts ─────────────────────────────────────────────────────
// These records define the Step Functions state that flows between Lambda steps.
// Each Lambda receives the output of the previous step as its input.

public record WorkflowInput(string Topic);

public record ResearchPlan
{
    public string Topic { get; init; } = "";
    public string[] FocusAreas { get; init; } = [];
    public string Objective { get; init; } = "";
}

[JsonSerializable(typeof(WorkflowInput))]
[JsonSerializable(typeof(ResearchPlan))]
[JsonSerializable(typeof(string))]
public partial class WorkflowJsonContext : JsonSerializerContext { }
