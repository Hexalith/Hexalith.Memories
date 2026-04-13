namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Classifies edge types and provides default filter sets for graph traversal.
/// Semantic edges express content relationships (causal, correlative, referential).
/// Structural edges express organizational relationships (containment, annotation).
/// </summary>
public static class EdgeTypeTaxonomy
{
    /// <summary>Semantic edge types: the default set for graph traversal queries.</summary>
    public static readonly IReadOnlyList<EdgeType> SemanticTypes = [EdgeType.CausedBy, EdgeType.CorrelatedWith, EdgeType.References];

    /// <summary>Structural edge types: excluded from default traversal to avoid noise.</summary>
    public static readonly IReadOnlyList<EdgeType> StructuralTypes = [EdgeType.Contains, EdgeType.Annotates];

    /// <summary>All defined edge types.</summary>
    public static readonly IReadOnlyList<EdgeType> AllTypes = [EdgeType.CausedBy, EdgeType.CorrelatedWith, EdgeType.References, EdgeType.Contains, EdgeType.Annotates];

    /// <summary>Returns the category (Structural or Semantic) for the given edge type.</summary>
    public static EdgeTypeCategory GetCategory(EdgeType edgeType) => edgeType switch
    {
        EdgeType.CausedBy => EdgeTypeCategory.Semantic,
        EdgeType.CorrelatedWith => EdgeTypeCategory.Semantic,
        EdgeType.References => EdgeTypeCategory.Semantic,
        EdgeType.Contains => EdgeTypeCategory.Structural,
        EdgeType.Annotates => EdgeTypeCategory.Structural,
        _ => throw new ArgumentOutOfRangeException(nameof(edgeType), edgeType, $"Unknown edge type: {edgeType}"),
    };
}
