namespace Hexalith.Memories.Contracts.V1;

/// <summary>Represents a directed relationship between two memory units in the knowledge graph.</summary>
public sealed record GraphEdge(
    string Id,
    string SourceId,
    string TargetId,
    EdgeType EdgeType,
    float Confidence,
    EdgeOrigin Origin,
    DateTimeOffset CreatedAt);
