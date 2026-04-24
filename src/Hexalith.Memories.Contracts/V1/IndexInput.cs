namespace Hexalith.Memories.Contracts.V1;

/// <summary>Shared input for all three indexing activities (syntactic, semantic, graph).</summary>
public sealed record IndexInput
{
    public required string MemoryUnitId { get; init; }

    public required string TenantId { get; init; }

    public required string CaseId { get; init; }

    public required string Content { get; init; }

    public required string ContentHash { get; init; }

    public required string SourceUri { get; init; }

    public required SourceType SourceType { get; init; }

    public required string IngestedBy { get; init; }

    public required DateTimeOffset IngestedAt { get; init; }

    public required float[] EmbeddingVector { get; init; }

    public required string EmbeddingProvider { get; init; }

    /// <summary>The embedding model identifier (e.g. "gemini-embedding-001"). Required: every new ingestion must supply the model that generated the vector (FR70, Story 5.5).</summary>
    public required string EmbeddingModel { get; init; }

    public required int EmbeddingDimensions { get; init; }

    // Pinned to StringComparer.Ordinal (decision D6 — committed-branch review 2026-04-24) to
    // match IngestionInput.Metadata and guarantee consistent lookups across the ingestion pipeline.
    public Dictionary<string, MetadataField> Metadata
    {
        get => field ??= new Dictionary<string, MetadataField>(StringComparer.Ordinal);
        init => field = value is null
            ? new Dictionary<string, MetadataField>(StringComparer.Ordinal)
            : new Dictionary<string, MetadataField>(value, StringComparer.Ordinal);
    }

    public string? CausationId { get; init; }

    public string? CorrelationId { get; init; }
}
