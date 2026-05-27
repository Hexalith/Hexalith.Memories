namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Represents one edge incident on a traversal node, describing the relationship to another node.</summary>
public sealed record TraversalEdgeInfo(
    EdgeType EdgeType,
    float Confidence,
    EdgeOrigin Origin,
    string ConnectedNodeId,
    string Direction)
{
    /// <summary>Gets the identity of the person who last verified this edge, if promoted.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VerifiedBy { get; init; }

    /// <summary>Gets the confidence value before the most recent promotion, if promoted.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? PreviousConfidence { get; init; }
}
