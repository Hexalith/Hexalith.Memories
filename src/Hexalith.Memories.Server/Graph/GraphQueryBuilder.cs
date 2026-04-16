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
    /// <summary>
    /// Test-only overload used by integration tests that do not distinguish between provider and model.
    /// Passes <paramref name="embeddingIdentifier"/> as both <c>embeddingProvider</c> and <c>embeddingModel</c>.
    /// Production call sites MUST use the 12-argument overload with distinct provider/model values.
    /// </summary>
    public (string Query, IDictionary<string, object> Parameters) BuildMergeMemoryUnitNode(
        string memoryUnitId,
        string caseId,
        string content,
        string contentHash,
        string sourceUri,
        SourceType sourceType,
        string embeddingIdentifier,
        int embeddingDimensions,
        string ingestedBy,
        DateTimeOffset ingestedAt,
        string metadataJson)
        => BuildMergeMemoryUnitNode(
            memoryUnitId,
            caseId,
            content,
            contentHash,
            sourceUri,
            sourceType,
            embeddingIdentifier,
            embeddingIdentifier,
            embeddingDimensions,
            ingestedBy,
            ingestedAt,
            metadataJson);

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildMergeMemoryUnitNode(
        string memoryUnitId,
        string caseId,
        string content,
        string contentHash,
        string sourceUri,
        SourceType sourceType,
        string embeddingProvider,
        string embeddingModel,
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
        ArgumentException.ThrowIfNullOrWhiteSpace(embeddingModel);
        ArgumentException.ThrowIfNullOrWhiteSpace(ingestedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataJson);

        const string query = "MERGE (m:MemoryUnit {id: $id}) SET m.caseId = $caseId, m.content = $content, m.contentHash = $contentHash, m.sourceUri = $sourceUri, m.sourceType = $sourceType, m.embeddingProvider = $provider, m.embeddingModel = $model, m.embeddingDimensions = $dims, m.indexedAt = $indexedAt, m.ingestedBy = $ingestedBy, m.ingestedAt = $ingestedAt, m.lastUpdated = $lastUpdated, m.metadataJson = $metadataJson";

        Dictionary<string, object> parameters = new()
        {
            ["id"] = memoryUnitId,
            ["caseId"] = caseId,
            ["content"] = content,
            ["contentHash"] = contentHash,
            ["sourceUri"] = sourceUri,
            ["sourceType"] = ToCamelCase(sourceType),
            ["provider"] = embeddingProvider,
            ["model"] = embeddingModel,
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
    public (string Query, IDictionary<string, object> Parameters) BuildMergeCaseNode(
        string caseId, string name, string tenantId, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        const string query = "MERGE (c:Case {id: $caseId}) SET c.name = $name, c.tenantId = $tenantId, c.createdAt = $createdAt";

        Dictionary<string, object> parameters = new()
        {
            ["caseId"] = caseId,
            ["name"] = name,
            ["tenantId"] = tenantId,
            ["createdAt"] = createdAt.ToString("o"),
        };

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildCountCaseMemoryUnits(string caseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);

        const string query = "MATCH (c:Case {id: $caseId})-[:CONTAINS]->(m) RETURN count(m) AS count";

        Dictionary<string, object> parameters = new()
        {
            ["caseId"] = caseId,
        };

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildListCaseMemoryUnitIds(string caseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);

        const string query = "MATCH (c:Case {id: $caseId})-[:CONTAINS]->(m:MemoryUnit) RETURN m.id AS memoryUnitId";

        Dictionary<string, object> parameters = new()
        {
            ["caseId"] = caseId,
        };

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildDeleteCaseNode(string caseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);

        const string query = "MATCH (c:Case {id: $caseId}) DETACH DELETE c";

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

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildCountMemoryUnits()
    {
        const string query = "MATCH (m:MemoryUnit) RETURN COUNT(m) AS count";

        Dictionary<string, object> parameters = [];

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildTraverseFromNode(
        string startNodeId, int depth)
        => BuildTraverseFromNode(startNodeId, depth, caseId: null);

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildTraverseFromNode(
        string startNodeId, int depth, string? caseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startNodeId);
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(depth, 10);

        // Depth is interpolated as literal — Cypher does not support parameterized path length.
        // Same pattern as edge type labels in BuildMergeEdge: validated closed set.
        string whereClause = string.IsNullOrWhiteSpace(caseId) ? "" : " WHERE n.caseId = $caseId";
        string query = $"MATCH p = (start:MemoryUnit {{id: $startId}})-[*0..{depth}]-(n:MemoryUnit){whereClause} RETURN DISTINCT n.id AS nodeId, min(length(p)) AS hopDistance";

        Dictionary<string, object> parameters = new()
        {
            ["startId"] = startNodeId,
        };

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            parameters["caseId"] = caseId;
        }

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildTraverseWithEdges(
        string startNodeId, int depth)
        => BuildTraverseWithEdges(startNodeId, depth, caseId: null);

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildTraverseWithEdges(
        string startNodeId, int depth, string? caseId)
        => BuildTraverseWithEdges(startNodeId, depth, caseId, edgeTypes: null);

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildTraverseWithEdges(
        string startNodeId, int depth, string? caseId, IReadOnlyList<EdgeType>? edgeTypes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startNodeId);
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(depth, 10);

        // Default to semantic types when no filter specified (AC #4).
        IReadOnlyList<EdgeType> effectiveTypes = (edgeTypes is null || edgeTypes.Count == 0)
            ? EdgeTypeTaxonomy.SemanticTypes
            : edgeTypes.Distinct().ToList();

        // Edge type labels are interpolated as literals — Cypher does not support parameterized
        // relationship types. This is safe because labels are derived from the closed EdgeType enum
        // via the validated ToUpperSnakeCase switch. Same safety pattern as BuildMergeEdge.
        string edgeLabels = string.Join("|", effectiveTypes.Select(ToUpperSnakeCase));

        // Depth is interpolated as literal — Cypher does not support parameterized path length.
        string whereClause = string.IsNullOrWhiteSpace(caseId)
            ? string.Empty
            : " WHERE (n.caseId = $caseId OR n.content IS NULL) AND start.caseId = $caseId AND ALL(node IN nodes(p) WHERE (node:MemoryUnit AND (node.caseId = $caseId OR node.content IS NULL)) OR (node:Case AND node.id = $caseId))";
        string edgeWhereClause = string.IsNullOrWhiteSpace(caseId)
            ? " WHERE m.id <> n.id"
            : " WHERE m.id <> n.id AND ((m:MemoryUnit AND (m.caseId = $caseId OR m.content IS NULL)) OR (m:Case AND m.id = $caseId))";
        string query = $"MATCH p = (start:MemoryUnit {{id: $startId}})-[:{edgeLabels}*0..{depth}]-(n:MemoryUnit){whereClause} WITH DISTINCT n, min(length(p)) AS hopDistance OPTIONAL MATCH (n)-[r:{edgeLabels}]-(m){edgeWhereClause} RETURN n.id AS nodeId, n.ingestedAt AS ingestedAt, n.content AS content, n.sourceUri AS sourceUri, n.sourceType AS sourceType, hopDistance, collect(DISTINCT {{edgeType: type(r), confidence: r.confidence, origin: r.origin, connectedId: m.id, direction: CASE WHEN startNode(r) = n THEN 'outgoing' ELSE 'incoming' END, verifiedBy: r.verifiedBy, previousConfidence: r.previousConfidence}}) AS edges ORDER BY n.ingestedAt ASC";

        Dictionary<string, object> parameters = new()
        {
            ["startId"] = startNodeId,
        };

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            parameters["caseId"] = caseId;
        }

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildUpdateEdgeConfidence(
        string sourceNodeId,
        string targetNodeId,
        EdgeType edgeType,
        float newConfidence,
        string verifiedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceNodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetNodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(verifiedBy);
        if (!float.IsFinite(newConfidence))
        {
            throw new ArgumentOutOfRangeException(nameof(newConfidence), newConfidence, "Confidence must be a finite value.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(newConfidence, 0f);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newConfidence, 1f);

        // Edge label is interpolated — safe because derived from the closed EdgeType enum
        // via the validated ToUpperSnakeCase switch. Same safety pattern as BuildMergeEdge.
        string edgeLabel = ToUpperSnakeCase(edgeType);
        (string sourceLabel, string targetLabel) = GetNodeLabels(edgeType);

        string query = $"MATCH (s:{sourceLabel} {{id: $sourceId}})-[r:{edgeLabel}]->(t:{targetLabel} {{id: $targetId}}) SET r.previousConfidence = r.confidence, r.confidence = $newConfidence, r.verifiedBy = $verifiedBy RETURN r.confidence AS newConfidence, r.previousConfidence AS previousConfidence";

        Dictionary<string, object> parameters = new()
        {
            ["sourceId"] = sourceNodeId,
            ["targetId"] = targetNodeId,
            ["newConfidence"] = newConfidence,
            ["verifiedBy"] = verifiedBy,
        };

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildCountAnnotations(string memoryUnitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);

        const string query = "MATCH (a:MemoryUnit)-[:ANNOTATES]->(m:MemoryUnit {id: $memoryUnitId}) RETURN count(a) AS count";

        Dictionary<string, object> parameters = new()
        {
            ["memoryUnitId"] = memoryUnitId,
        };

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildListAnnotationIds(string memoryUnitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);

        const string query = "MATCH (a:MemoryUnit)-[:ANNOTATES]->(m:MemoryUnit {id: $memoryUnitId}) RETURN a.id AS annotationId";

        Dictionary<string, object> parameters = new()
        {
            ["memoryUnitId"] = memoryUnitId,
        };

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildCountAllNodes()
    {
        const string query = "MATCH (n) RETURN count(n) AS count";

        Dictionary<string, object> parameters = [];

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildBatchDeleteNodes(int batchSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        const string query = "MATCH (n) WITH n LIMIT $batchSize DETACH DELETE n RETURN count(n) AS deleted";

        Dictionary<string, object> parameters = new()
        {
            ["batchSize"] = batchSize,
        };

        return (query, parameters);
    }

    /// <inheritdoc/>
    public (string Query, IDictionary<string, object> Parameters) BuildBatchCountAnnotations(IReadOnlyList<string> memoryUnitIds)
    {
        ArgumentNullException.ThrowIfNull(memoryUnitIds);

        const string query = "UNWIND $ids AS muId OPTIONAL MATCH (a:MemoryUnit)-[:ANNOTATES]->(m:MemoryUnit {id: muId}) RETURN muId, count(a) AS count";

        Dictionary<string, object> parameters = new()
        {
            ["ids"] = memoryUnitIds,
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
