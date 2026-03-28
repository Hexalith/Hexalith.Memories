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

    public Dictionary<string, MetadataField> Metadata
    {
        get => field ??= [];
        init => field = value ?? [];
    }

    public string? EmbeddingProvider { get; init; }

    public int? EmbeddingDimensions { get; init; }

    public string? Classification { get; init; }

    public FailureDetails? FailureDetails { get; init; }
}
