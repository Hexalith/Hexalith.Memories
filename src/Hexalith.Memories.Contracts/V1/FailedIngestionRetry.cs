namespace Hexalith.Memories.Contracts.V1;

/// <summary>Scheduler-captured failed-attempt lineage; ordinary ingestion cannot supply this internal durable execution context.</summary>
/// <param name="PreviousWorkflowInstanceId">The independently observed failed producer.</param>
/// <param name="MemoryUnitId">The stable business output retained across independent workflow attempts.</param>
public sealed record FailedIngestionRetry(string PreviousWorkflowInstanceId, string MemoryUnitId);
