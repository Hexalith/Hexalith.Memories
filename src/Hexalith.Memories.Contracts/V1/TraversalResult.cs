namespace Hexalith.Memories.Contracts.V1;

/// <summary>The result of a causal chain traversal from a starting memory unit.</summary>
public sealed record TraversalResult(
    string StartNodeId,
    int Depth,
    IReadOnlyList<TraversalNode> Nodes,
    int TotalNodeCount);
