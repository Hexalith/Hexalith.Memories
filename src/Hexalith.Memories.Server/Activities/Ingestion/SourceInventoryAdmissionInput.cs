namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Admission identity captured before a child ingestion workflow is scheduled.</summary>
internal sealed record SourceInventoryAdmissionInput(string WorkflowInstanceId, IngestionInput Input);
