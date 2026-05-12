using Microsoft.Extensions.Logging;
using ResponsibleAiSample.Tools;
using StrandsAgents.Core;
using StrandsAgents.Models.Bedrock;

// ─────────────────────────────────────────────────────────────────────────────
// Responsible AI Sample
//
// This sample demonstrates all five responsible tool design principles:
//   1. Least Privilege    — FileAccessTool restricts access to allowed directories
//   2. Input Validation   — ContentFetchTool uses [ToolParameterValidation] constraints
//   3. Clear Documentation — All tools have descriptive [Tool] attributes
//   4. Error Handling     — Both tools return descriptive errors instead of throwing
//   5. Audit Logging      — AuditLogHookHandler records every tool call (metadata only)
//
// Additionally demonstrates:
//   - Bedrock Guardrails in enforcing mode (blocks harmful content)
//   - Tool result guardrail evaluation (screens external content before feeding to model)
//   - GuardrailViolationEvent hook for monitoring violations
// ─────────────────────────────────────────────────────────────────────────────

// RESPONSIBLE AI PRINCIPLE: Audit Logging
// Set up a logger — AuditLogHookHandler will write structured entries here.
// In production, replace with your preferred ILogger implementation.
using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger("ResponsibleAiSample");

// RESPONSIBLE AI PRINCIPLE: Guardrails
// Configure Bedrock Guardrails in enforcing mode.
// Replace "your-guardrail-id" with your actual Bedrock Guardrail ID.
// The guardrail will:
//   - Block harmful input before it reaches the model
//   - Block harmful model output before it reaches the caller
//   - Screen tool results (via IGuardrailEvaluator) before feeding them back to the model
var guardrailConfig = new BedrockGuardrailConfig(
    GuardrailId: "your-guardrail-id",
    GuardrailVersion: "1")
{
    Trace = true,                    // Enable trace for debugging
    RedactInput = true,              // Replace blocked input in conversation history
    RedactOutput = true,             // Replace blocked output in conversation history
    EvaluateLatestMessageOnly = true // Only evaluate the latest message (reduces cost)
};

// Set up the hook registry for event handling
var hooks = new HookRegistry();

// RESPONSIBLE AI PRINCIPLE: Audit Logging
// Register the audit log handler — logs tool name, call ID, elapsed time, and error status.
// Deliberately does NOT log tool input or output content to avoid capturing PII.
var auditHandler = new AuditLogHookHandler(loggerFactory.CreateLogger<AuditLogHookHandler>());
auditHandler.Register(hooks);

// Register a GuardrailViolationEvent handler to monitor guardrail interventions
hooks.Register<GuardrailViolationEvent>(evt =>
{
    logger.LogWarning(
        "Guardrail intervention: GuardrailId={GuardrailId} Action={Action} Source={Source}",
        evt.GuardrailId, evt.Action, evt.Source);
    return Task.CompletedTask;
});

// Create the Bedrock model with guardrail config and hooks
// BedrockModel implements both IModel and IGuardrailEvaluator, so it is passed
// as both the model provider and the guardrail evaluator for tool result screening.
var bedrockModel = new BedrockModel(
    region: "us-east-1",
    modelId: "us.anthropic.claude-haiku-4-5-20251001-v1:0",
    guardrailConfig: guardrailConfig,
    hooks: hooks);

// RESPONSIBLE AI PRINCIPLE: Clear Documentation
// Tools are documented via [Tool] attributes — the model receives these descriptions
// to understand what each tool does and when to use it.
var fileAccessTool = new FileAccessTool();
var contentFetchTool = new ContentFetchTool();

// Create the agent, passing BedrockModel as both model and guardrailEvaluator.
// This enables tool result screening: after each tool call, the result content
// is evaluated by the guardrail before being fed back to the model.
var agent = new Agent(
    model: bedrockModel,
    systemPrompt: "You are a helpful assistant. Use the available tools to answer questions. " +
                  "Always respect security boundaries and report any access errors to the user.",
    tools:
    [
        new FileAccessTool_ReadFile_Tool(fileAccessTool),
        new ContentFetchTool_FetchContent_Tool(contentFetchTool)
    ],
    hooks: hooks,
    guardrailEvaluator: bedrockModel);  // BedrockModel screens tool results via ApplyGuardrail

Console.WriteLine("Responsible AI Sample");
Console.WriteLine("=====================");
Console.WriteLine("This sample demonstrates responsible tool design principles.");
Console.WriteLine("Note: Replace 'your-guardrail-id' with a real Bedrock Guardrail ID to enable guardrail enforcement.");
Console.WriteLine();

// Example interaction demonstrating tool result guardrail evaluation:
// ContentFetchTool fetches external content, which is then screened by the guardrail
// before being fed back to the model.
Console.Write("Enter a prompt (or press Enter for a demo): ");
var userInput = Console.ReadLine();

if (string.IsNullOrWhiteSpace(userInput))
    userInput = "Please fetch the content from https://example.com and summarize it.";

Console.WriteLine($"\nPrompt: {userInput}");
Console.WriteLine("\nResponse:");

try
{
    var result = await agent.InvokeAsync(userInput);

    if (result.StopReason == StopReason.GuardrailBlocked)
    {
        Console.WriteLine("[Guardrail blocked this interaction]");
        Console.WriteLine(result.Message);
    }
    else
    {
        Console.WriteLine(result.Message);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}
