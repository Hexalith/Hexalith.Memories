namespace Hexalith.Memories.Contracts.V1;

/// <summary>Content-free durable provenance for one admitted source workflow, including historical cases and deleted outputs.</summary>
/// <param name="CaseId">The case captured at admission.</param>
/// <param name="WorkflowInstanceId">The durable producer handle, or null for a reconciled historical canonical output.</param>
/// <param name="IdempotencyTokenHash">SHA-256 of an explicit retry identity, when supplied.</param>
/// <param name="MemoryUnitId">The canonical output when publication has completed.</param>
/// <param name="State">Admitted, Completed, or Deleted; unknown work remains admitted.</param>
/// <param name="TerminalVerified">Whether independently observed terminal workflow evidence is durably retained.</param>
/// <param name="SchedulingState">Preparing, Scheduling, Scheduled or Unscheduled; unknown legacy admission cannot prove non-scheduling.</param>
/// <param name="CandidateMemoryUnitId">The producer artifact identity retained before materialization.</param>
/// <param name="SourcePayloadReference">The exact claim-check identity retained before preparing source bytes.</param>
/// <param name="CandidateArtifactsDeleted">Whether a distinct discarded candidate and its retained payload/failed artifacts were physically reconciled.</param>
/// <param name="RefreshProtocolVersion">The supported explicit refresh protocol captured at admission, or null for ordinary ingestion.</param>
/// <param name="SourceRevisionNumber">The authoritative refresh revision, or null for ordinary ingestion.</param>
/// <param name="RefreshIdentityDigest">The immutable refresh content/revision binding, or null for ordinary ingestion.</param>
public sealed record SourceInventoryEntry(string CaseId, string? WorkflowInstanceId, string? IdempotencyTokenHash = null,
    string? MemoryUnitId = null, string State = "Admitted", bool TerminalVerified = false, string SchedulingState = "Unknown", string? CandidateMemoryUnitId = null,
    WorkflowPayloadReference? SourcePayloadReference = null, string? RefreshProtocolVersion = null,
    long? SourceRevisionNumber = null, string? RefreshIdentityDigest = null, bool CandidateArtifactsDeleted = false);
