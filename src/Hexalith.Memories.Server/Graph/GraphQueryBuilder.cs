namespace Hexalith.Memories.Server.Graph;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Generates parameterized Cypher queries for all FalkorDB operations.
/// Stateless and thread-safe — registered as singleton.
/// </summary>
/// <remarks>
/// This is a safety interface implementation (Decision D9) that structurally prevents
/// Cypher injection. All values are passed via $parameter placeholders — no raw values
/// appear in query strings.
/// <para>
/// Known FalkorDB limitation: relationship types cannot be parameterized in Cypher.
/// Edge type labels are derived from the <see cref="EdgeType"/> enum via a closed switch
/// expression and validated before interpolation.
/// </para>
/// </remarks>
public sealed class GraphQueryBuilder : IGraphQueryBuilder
{
    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildMergeMemoryUnitNode(
        string memoryUnitId,
        string caseId,
        string content,
        string contentHash,
        string sourceUri,
        SourceType sourceType,
        string embeddingProvider,
        int embeddingDimensions,
        string ingestedBy,
        DateTimeOffset ingestedAt,
        string metadataJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(embeddingProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(ingestedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataJson);

        const string query = "MERGE (m:MemoryUnit {id: $id}) SET m.caseId = $caseId, m.content = $content, m.contentHash = $contentHash, m.sourceUri = $sourceUri, m.sourceType = $sourceType, m.embeddingProvider = $provider, m.embeddingDimensions = $dims, m.indexedAt = $indexedAt, m.ingestedBy = $ingestedBy, m.ingestedAt = $ingestedAt, m.lastUpdated = $lastUpdated, m.metadataJson = $metadataJson";

        Dictionary<string, object> parameters = new()
        {
            ["id"] = memoryUnitId,
            ["caseId"] = caseId,
            ["content"] = content,
            ["contentHash"] = contentHash,
            ["sourceUri"] = sourceUri,
            ["sourceType"] = ToCamelCase(sourceType),
            ["provider"] = embeddingProvider,
            ["dims"] = embeddingDimensions,
            ["indexedAt"] = ingestedAt.ToString("o"),
            ["ingestedBy"] = ingestedBy,
            ["ingestedAt"] = ingestedAt.ToString("o"),
            ["lastUpdated"] = ingestedAt.ToString("o"),
            ["metadataJson"] = metadataJson,
        };

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildMergeCaseNode(string caseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);

        const string query = "MERGE (c:Case {id: $caseId})";

        Dictionary<string, object> parameters = new()
        {
            ["caseId"] = caseId,
        };

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildMergeEdge(
        string sourceNodeId,
        string targetNodeId,
        EdgeType edgeType,
        float confidence,
        EdgeOrigin origin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceNodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetNodeId);

        string edgeLabel = ToUpperSnakeCase(edgeType);
        (string sourceLabel, string targetLabel) = GetNodeLabels(edgeType);

        string query = $"MATCH (s:{sourceLabel} {{id: $sourceId}}), (t:{targetLabel} {{id: $targetId}}) MERGE (s)-[r:{edgeLabel}]->(t) SET r.confidence = $confidence, r.origin = $origin";

        Dictionary<string, object> parameters = new()
        {
            ["sourceId"] = sourceNodeId,
            ["targetId"] = targetNodeId,
            ["confidence"] = confidence,
            ["origin"] = ToCamelCase(origin),
        };

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildMergeStubNode(string memoryUnitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);

        const string query = "MERGE (m:MemoryUnit {id: $id})";

        Dictionary<string, object> parameters = new()
        {
            ["id"] = memoryUnitId,
        };

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildCheckMemoryUnitExists(string memoryUnitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);

        const string query = "MATCH (m:MemoryUnit {id: $id}) RETURN m.id";

        Dictionary<string, object> parameters = new()
        {
            ["id"] = memoryUnitId,
        };

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildDeleteMemoryUnitNode(string memoryUnitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);

        const string query = "MATCH (m:MemoryUnit {id: $id}) DETACH DELETE m";

        Dictionary<string, object> parameters = new()
        {
            ["id"] = memoryUnitId,
        };

        return (query, parameters);
    }

    /// <summary>
    /// Converts an <see cref="EdgeType"/> enum value to UPPER_SNAKE_CASE Cypher relationship label.
    /// Uses an explicit switch expression on the closed set — no regex, no ToString interpolation.
    /// </summary>
    private static string ToUpperSnakeCase(EdgeType edgeType) => edgeType switch
    {
        EdgeType.CausedBy => "CAUSED_BY",
        EdgeType.CorrelatedWith => "CORRELATED_WITH",
        EdgeType.Contains => "CONTAINS",
        EdgeType.References => "REFERENCES",
        EdgeType.Annotates => "ANNOTATES",
        _ => throw new ArgumentOutOfRangeException(nameof(edgeType), edgeType, $"Unknown edge type: {edgeType}"),
    };

    private static (string SourceLabel, string TargetLabel) GetNodeLabels(EdgeType edgeType) => edgeType switch
    {
        EdgeType.Contains => ("Case", "MemoryUnit"),
        EdgeType.CausedBy or EdgeType.CorrelatedWith or EdgeType.References or EdgeType.Annotates => ("MemoryUnit", "MemoryUnit"),
        _ => throw new ArgumentOutOfRangeException(nameof(edgeType), edgeType, $"Unknown edge type: {edgeType}"),
    };

    private static string ToCamelCase<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        string name = value.ToString();
        return string.IsNullOrEmpty(name)
            ? string.Empty
            : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
