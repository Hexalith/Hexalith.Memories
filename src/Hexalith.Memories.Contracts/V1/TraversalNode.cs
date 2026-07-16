namespace Hexalith.Memories.Contracts.V1;

/// <summary>A node in a causal chain traversal result, including summary content and incident edges.</summary>
public sealed record TraversalNode(
    string MemoryUnitId,
    string ContentSnippet,
    string SourceUri,
    SourceType SourceType,
    DateTimeOffset IngestedAt,
    int HopDistance,
    IReadOnlyList<TraversalEdgeInfo> Edges);
