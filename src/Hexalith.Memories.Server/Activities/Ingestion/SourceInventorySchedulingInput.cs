namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Exact source producer and its independently determined scheduling transition.</summary>
internal sealed record SourceInventorySchedulingInput(string TenantId, string SourceUri, string WorkflowInstanceId, string State);
