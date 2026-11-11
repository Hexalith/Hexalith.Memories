namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Dapr.Workflow;
using Hexalith.Memories.Server.Activities.Cases;

/// <summary>Dispatches the retained registered annotation producer through the real workflow worker.</summary>
internal sealed class RetainedAnnotationSchedulingWorkflow : Workflow<AnnotationProjectionInput, string>
{
    /// <inheritdoc />
    public override Task<string> RunAsync(WorkflowContext context, AnnotationProjectionInput input)
        => context.CallActivityAsync<string>(nameof(ScheduleAnnotationIngestionActivity), input);
}
