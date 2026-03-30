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

    public required int EmbeddingDimensions { get; init; }

    public Dictionary<string, MetadataField> Metadata
    {
        get => field ??= [];
        init => field = value ?? [];
    }

    public string? CausationId { get; init; }

    public string? CorrelationId { get; init; }
}
