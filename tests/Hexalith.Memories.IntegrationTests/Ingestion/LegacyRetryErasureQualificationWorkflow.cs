namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Text.Json;
using Dapr.Workflow;

/// <summary>Synthetic historical producer retaining raw input while awaiting a distant timer.</summary>
public sealed class LegacyRetryErasureQualificationWorkflow : Workflow<JsonElement, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowContext context, JsonElement input)
    {
        context.SetCustomStatus(input);
        await context.CreateTimer(TimeSpan.FromHours(1), CancellationToken.None);
        return true;
    }
}
