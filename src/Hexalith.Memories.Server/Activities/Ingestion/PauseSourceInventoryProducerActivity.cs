namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;

/// <summary>Explicit owned-test pause before an admitted producer materializes any memory unit.</summary>
internal sealed class PauseSourceInventoryProducerActivity(IConfiguration configuration, IHostEnvironment environment) : WorkflowActivity<string, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, string input)
    {
        if (!environment.IsProduction() && configuration.GetValue("Memories:Testing:EnableSourceInventoryQualification", false)
            && configuration.GetValue("Memories:Testing:UseFakeEmbedding", false))
        {
            int delay = Math.Clamp(configuration.GetValue("Memories:Testing:SourceInventoryAdmissionDelayMilliseconds", 0), 0, 5000);
            if (delay > 0) { await Task.Delay(delay).ConfigureAwait(false); }
        }
        return true;
    }
}
