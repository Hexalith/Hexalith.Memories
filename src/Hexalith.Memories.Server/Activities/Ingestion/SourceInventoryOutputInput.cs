namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Content-free terminal provenance passed to the durable inventory activity.</summary>
internal sealed record SourceInventoryOutputInput(string TenantId, string SourceUri, string WorkflowInstanceId, string CaseId, string MemoryUnitId);
