namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Bounded durable source admission and provenance, retained through cleanup and restart.</summary>
/// <param name="TenantId">Exact tenant ownership.</param>
/// <param name="SourceUri">Exact source ownership.</param>
/// <param name="Version">Optimistic inventory generation.</param>
/// <param name="CoverageComplete">Whether complete source coverage has independent authority.</param>
/// <param name="AdmissionClosed">Whether further producers are fenced.</param>
/// <param name="Permanent">Whether admission can never reopen.</param>
/// <param name="Entries">Retained producer and output obligations.</param>
/// <param name="CoverageEvidenceId">Independent source coverage evidence.</param>
/// <param name="IncompleteReason">The retained reason coverage is incomplete.</param>
/// <param name="LastReopenedFromVersion">The exact transient fence already reopened.</param>
/// <param name="FreshCandidateWorkflows">New admissions eligible for one atomic empty-key coverage initialization; absent on legacy data and consumed by first candidate retention.</param>
internal sealed record SourceInventorySnapshot(string TenantId, string SourceUri, string Version,
    bool CoverageComplete = false, bool AdmissionClosed = false, bool Permanent = false,
    IReadOnlyList<SourceInventoryEntry>? Entries = null, string? CoverageEvidenceId = null, string? IncompleteReason = null,
    string? LastReopenedFromVersion = null,
    IReadOnlyList<string>? FreshCandidateWorkflows = null);
