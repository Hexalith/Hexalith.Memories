namespace Hexalith.Memories.Contracts.V1;

/// <summary>Represents one edge incident on a traversal node, describing the relationship to another node.</summary>
public sealed record TraversalEdgeInfo(
    EdgeType EdgeType,
    float Confidence,
    EdgeOrigin Origin,
    string ConnectedNodeId,
    string Direction);
