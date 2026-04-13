namespace Hexalith.Memories.Server.Graph;

using System.Diagnostics;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

using NFalkorDB;

using StackExchange.Redis;

/// <summary>
/// Traverses causal chains in FalkorDB, returning rich node context with edge metadata.
/// Singleton service — stateless and thread-safe.
/// </summary>
public sealed partial class GraphTraversalService
{
    private static readonly TimeSpan GraphOperationTimeout = TimeSpan.FromSeconds(10);
    private const int MaxSnippetLength = 200;

    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ILogger<GraphTraversalService> _logger;

    public GraphTraversalService(
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<GraphTraversalService> logger)
    {
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _logger = logger;
    }

    /// <summary>
    /// Traverses causal chains from a starting memory unit, returning nodes with edge metadata.
    /// </summary>
    public async Task<TraversalResult> TraverseAsync(
        string tenantId,
        string startNodeId,
        int depth,
        string? caseId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(startNodeId);

        FalkorDB falkor = new(_falkorDb.GetDatabase());
        string graphId = tenantId;

        (string query, IDictionary<string, object> parameters) =
            _graphQueryBuilder.BuildTraverseWithEdges(startNodeId, depth, caseId);

        long startTimestamp = Stopwatch.GetTimestamp();
        List<TraversalNode> nodes;
        try
        {
            ResultSet resultSet = await falkor.QueryAsync(graphId, query, parameters)
                .WaitAsync(GraphOperationTimeout, cancellationToken)
                .ConfigureAwait(false);

            nodes = [];
            foreach (Record record in resultSet)
            {
                TraversalNode node = ParseTraversalNode(record);
                nodes.Add(node);
            }
        }
        catch (RedisServerException ex) when (IsGraphNotFoundError(ex))
        {
            LogGraphNotFound(_logger, tenantId);
            return new TraversalResult(startNodeId, depth, [], 0);
        }

        long elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).Milliseconds;
        LogTraversalComplete(_logger, tenantId, startNodeId, depth, nodes.Count, elapsedMs);

        return new TraversalResult(startNodeId, depth, nodes, nodes.Count);
    }

    private static TraversalNode ParseTraversalNode(Record record)
    {
        string nodeId = record.GetValue<string>("nodeId");
        string ingestedAtStr = record.GetValue<string>("ingestedAt");
        string content = record.GetValue<string>("content");
        string sourceUri = record.GetValue<string>("sourceUri");
        string sourceTypeStr = record.GetValue<string>("sourceType");
        long hopDistance = record.GetValue<long>("hopDistance");

        DateTimeOffset ingestedAt = DateTimeOffset.Parse(ingestedAtStr, System.Globalization.CultureInfo.InvariantCulture);
        SourceType sourceType = ParseSourceType(sourceTypeStr);
        string snippet = TruncateContent(content);

        List<TraversalEdgeInfo> edges = ParseEdges(record);

        return new TraversalNode(nodeId, snippet, sourceUri, sourceType, ingestedAt, (int)hopDistance, edges);
    }

    private static List<TraversalEdgeInfo> ParseEdges(Record record)
    {
        List<TraversalEdgeInfo> edges = [];

        object edgesRaw = record.GetValue<object>("edges");
        if (edgesRaw is not IEnumerable<object> edgeCollection)
        {
            return edges;
        }

        foreach (object edgeObj in edgeCollection)
        {
            if (edgeObj is not IDictionary<string, object> edgeMap)
            {
                continue;
            }

            string? edgeTypeStr = edgeMap.TryGetValue("edgeType", out object? etVal) ? etVal?.ToString() : null;
            string? originStr = edgeMap.TryGetValue("origin", out object? orVal) ? orVal?.ToString() : null;
            string? connectedId = edgeMap.TryGetValue("connectedId", out object? ciVal) ? ciVal?.ToString() : null;
            string? direction = edgeMap.TryGetValue("direction", out object? dirVal) ? dirVal?.ToString() : null;
            float confidence = edgeMap.TryGetValue("confidence", out object? confVal) && confVal is double confDbl
                ? (float)confDbl
                : 0f;

            if (edgeTypeStr is null || connectedId is null || direction is null)
            {
                continue;
            }

            EdgeType edgeType = ParseEdgeType(edgeTypeStr);
            EdgeOrigin origin = ParseEdgeOrigin(originStr ?? "explicit");

            edges.Add(new TraversalEdgeInfo(edgeType, confidence, origin, connectedId, direction));
        }

        return edges;
    }

    internal static EdgeType ParseEdgeType(string cypherLabel) => cypherLabel switch
    {
        "CAUSED_BY" => EdgeType.CausedBy,
        "CORRELATED_WITH" => EdgeType.CorrelatedWith,
        "REFERENCES" => EdgeType.References,
        "CONTAINS" => EdgeType.Contains,
        "ANNOTATES" => EdgeType.Annotates,
        _ => throw new ArgumentOutOfRangeException(nameof(cypherLabel), cypherLabel, $"Unknown edge type label: {cypherLabel}"),
    };

    internal static EdgeOrigin ParseEdgeOrigin(string value) => value switch
    {
        "explicit" => EdgeOrigin.Explicit,
        "inferred" => EdgeOrigin.Inferred,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown edge origin: {value}"),
    };

    internal static SourceType ParseSourceType(string value) => value switch
    {
        "file" => SourceType.File,
        "url" => SourceType.Url,
        "event" => SourceType.Event,
        "command" => SourceType.Command,
        "projection" => SourceType.Projection,
        "discussion" => SourceType.Discussion,
        "annotation" => SourceType.Annotation,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown source type: {value}"),
    };

    internal static string TruncateContent(string content)
    {
        if (content.Length <= MaxSnippetLength)
        {
            return content;
        }

        int lastSpace = content.LastIndexOf(' ', MaxSnippetLength);
        int cutoff = lastSpace > 0 ? lastSpace : MaxSnippetLength;
        return content[..cutoff] + "...";
    }

    private static bool IsGraphNotFoundError(RedisServerException ex)
        => ex.Message.Contains("Graph not found", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("ERR Invalid graph operation", StringComparison.OrdinalIgnoreCase);

    [LoggerMessage(Level = LogLevel.Information, Message = "Traversal complete: tenant={TenantId}, startNode={StartNodeId}, depth={Depth}, nodeCount={NodeCount}, latencyMs={ElapsedMs}")]
    private static partial void LogTraversalComplete(ILogger logger, string tenantId, string startNodeId, int depth, int nodeCount, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FalkorDB graph not found for tenant {TenantId} — returning empty traversal")]
    private static partial void LogGraphNotFound(ILogger logger, string tenantId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Traversal failed: tenant={TenantId}, startNode={StartNodeId}")]
    private static partial void LogTraversalError(ILogger logger, string tenantId, string startNodeId, Exception exception);
}
