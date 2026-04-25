namespace Hexalith.Memories.Contracts.V1;

/// <summary>Represents a fully indexed memory unit in the knowledge graph.</summary>
public sealed record MemoryUnit
{
    public required string Id { get; init; }

    public required string TenantId { get; init; }

    public required string CaseId { get; init; }

    public required string Content { get; init; }

    public required string ContentHash { get; init; }

    public required string SourceUri { get; init; }

    public required SourceType SourceType { get; init; }

    public required string IngestedBy { get; init; }

    public required DateTimeOffset IngestedAt { get; init; }

    public required DateTimeOffset LastUpdated { get; init; }

    public required MemoryUnitStatus Status { get; init; }

    // Pinned to StringComparer.Ordinal (decision S6-D3 — re-review 2026-04-25) to match
    // IngestionInput.Metadata + IndexInput.Metadata. MemoryUnit is also a contract-boundary record
    // carrying CloudEvent metadata back to consumers; without pinning, a round-trip through a
    // case-insensitive dictionary would silently change lookup semantics.
    public Dictionary<string, MetadataField> Metadata
    {
        get => field ??= new Dictionary<string, MetadataField>(StringComparer.Ordinal);
        init => field = value switch
        {
            null => new Dictionary<string, MetadataField>(StringComparer.Ordinal),
            Dictionary<string, MetadataField> existing when ReferenceEquals(existing.Comparer, StringComparer.Ordinal) => existing,
            _ => new Dictionary<string, MetadataField>(value, StringComparer.Ordinal),
        };
    }

    public string? EmbeddingProvider { get; init; }

    /// <summary>
    /// The embedding model identifier (e.g. "gemini-embedding-001") used to generate this memory unit's vector.
    /// Nullable because memory units indexed before FR70 (Story 5.5) do not carry this field; legacy reads return null.
    /// </summary>
    public string? EmbeddingModel { get; init; }

    public int? EmbeddingDimensions { get; init; }

    public string? Classification { get; init; }

    public FailureDetails? FailureDetails { get; init; }
}
