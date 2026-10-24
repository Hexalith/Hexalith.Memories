namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Bounded durable source admission and provenance, retained through cleanup and restart.</summary>
internal sealed record SourceInventorySnapshot(string TenantId, string SourceUri, string Version,
    bool CoverageComplete = false, bool AdmissionClosed = false, bool Permanent = false,
    IReadOnlyList<SourceInventoryEntry>? Entries = null, string? CoverageEvidenceId = null, string? IncompleteReason = null,
    string? LastReopenedFromVersion = null);
