namespace Hexalith.Memories.Contracts.V1;

/// <summary>A bounded exact-source inventory page. Completeness is independent of deduplication and search results.</summary>
/// <param name="TenantId">The authorized tenant.</param>
/// <param name="SourceUri">The exact canonical source URI.</param>
/// <param name="Version">The immutable snapshot version, changed by every durable inventory mutation.</param>
/// <param name="CoverageComplete">Whether legacy coverage has been explicitly reconciled by the owner.</param>
/// <param name="AdmissionClosed">Whether new producers are fenced.</param>
/// <param name="Permanent">Whether the fence permanently prohibits re-admission.</param>
/// <param name="Entries">At most 100 retained workflow/output obligations.</param>
/// <param name="NextCursor">A scope/version-bound continuation, or null at the end.</param>
/// <param name="IncompleteReason">Why owner reconciliation is required when coverage is incomplete.</param>
public sealed record SourceInventoryPage(string TenantId, string SourceUri, string Version, bool CoverageComplete,
    bool AdmissionClosed, bool Permanent, IReadOnlyList<SourceInventoryEntry> Entries, string? NextCursor, string? IncompleteReason = null);
