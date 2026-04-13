namespace Hexalith.Memories.Server.Graph;

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

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
    private readonly IConnectionMultiplexer _redis;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ILogger<GraphTraversalService> _logger;

    public GraphTraversalService(
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<GraphTraversalService> logger)
    {
        _falkorDb = falkorDb;
        _redis = redis;
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
        IReadOnlyList<EdgeType>? edgeTypes,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(startNodeId);

        FalkorDB falkor = new(_falkorDb.GetDatabase());
        string graphId = tenantId;

        (string query, IDictionary<string, object> parameters) =
            _graphQueryBuilder.BuildTraverseWithEdges(startNodeId, depth, caseId, edgeTypes);

        long startTimestamp = Stopwatch.GetTimestamp();
        List<TraversalNode> nodes;
        List<TraversalGapMarker> gapMarkers;
        try
        {
            ResultSet resultSet = await falkor.QueryAsync(graphId, query, parameters)
                .WaitAsync(GraphOperationTimeout, cancellationToken)
                .ConfigureAwait(false);

            nodes = [];
            gapMarkers = [];
            foreach (Record record in resultSet)
            {
                if (!TryGetRequiredString(record, "nodeId", out string? nodeId))
                {
                    continue;
                }

                string? contentRaw = TryGetOptionalString(record, "content");
                if (string.IsNullOrWhiteSpace(contentRaw))
                {
                    FallbackTraversalNodeData? fallback = await LoadFallbackTraversalNodeDataAsync(graphId, nodeId, cancellationToken)
                        .ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(fallback?.Content))
                    {
                        TraversalNode? node = await ParseTraversalNodeAsync(graphId, record, fallback.Content, cancellationToken, fallback)
                            .ConfigureAwait(false);
                        if (node is not null)
                        {
                            nodes.Add(node);
                            continue;
                        }
                    }

                    // Stub node — gap marker (FR49). Stub nodes created by BuildMergeStubNode
                    // have ONLY the id property; content is absent/null in FalkorDB.
                    long gapHop = TryGetRecordValue(record, "hopDistance", out long parsedGapHop)
                        ? parsedGapHop
                        : 0;
                    List<TraversalEdgeInfo> gapEdges = TryGetRecordValue(record, "edges", out object? gapEdgesRaw)
                        ? ParseEdgeCollection(gapEdgesRaw)
                        : [];
                    gapMarkers.Add(new TraversalGapMarker(nodeId, checked((int)gapHop), gapEdges));
                }
                else
                {
                    TraversalNode? node = await ParseTraversalNodeAsync(graphId, record, contentRaw, cancellationToken)
                        .ConfigureAwait(false);
                    if (node is not null)
                    {
                        nodes.Add(node);
                    }
                }
            }
        }
        catch (RedisServerException ex) when (IsGraphNotFoundError(ex))
        {
            LogGraphNotFound(_logger, tenantId);
            return new TraversalResult(startNodeId, depth, [], 0);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogTraversalError(_logger, tenantId, startNodeId, ex);
            throw;
        }

        long elapsedMs = (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        LogTraversalComplete(_logger, tenantId, startNodeId, depth, nodes.Count, elapsedMs);

        return new TraversalResult(startNodeId, depth, nodes, nodes.Count) { GapMarkers = gapMarkers };
    }

    /// <summary>
    /// Promotes the confidence of an existing edge in the knowledge graph (FR51).
    /// Returns null if the edge or graph does not exist.
    /// </summary>
    public async Task<ConfidencePromotionResult?> PromoteEdgeConfidenceAsync(
        string tenantId,
        ConfidencePromotionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(request);

        FalkorDB falkor = new(_falkorDb.GetDatabase());
        string graphId = tenantId;

        (string query, IDictionary<string, object> parameters) =
            _graphQueryBuilder.BuildUpdateEdgeConfidence(
                request.SourceNodeId,
                request.TargetNodeId,
                request.EdgeType,
                request.NewConfidence,
                request.VerifiedBy);

        ResultSet resultSet;
        try
        {
            resultSet = await falkor.QueryAsync(graphId, query, parameters)
                .WaitAsync(GraphOperationTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RedisServerException ex) when (IsGraphNotFoundError(ex))
        {
            LogEdgeNotFound(_logger, tenantId, request.SourceNodeId, request.TargetNodeId, request.EdgeType);
            return null;
        }

        foreach (Record record in resultSet)
        {
            if (TryConvertToSingle(TryGetRecordValueOrDefault<object>(record, "previousConfidence"), out float previousConfidence) &&
                TryConvertToSingle(TryGetRecordValueOrDefault<object>(record, "newConfidence"), out float newConfidence))
            {
                LogConfidencePromoted(_logger, tenantId, request.SourceNodeId, request.TargetNodeId, request.EdgeType, previousConfidence, newConfidence, request.VerifiedBy);
                return new ConfidencePromotionResult(
                    request.SourceNodeId,
                    request.TargetNodeId,
                    request.EdgeType,
                    previousConfidence,
                    newConfidence,
                    request.VerifiedBy);
            }
        }

        LogEdgeNotFound(_logger, tenantId, request.SourceNodeId, request.TargetNodeId, request.EdgeType);
        return null;
    }

    internal static List<TraversalEdgeInfo> ParseEdgeCollection(object? edgesRaw)
    {
        List<TraversalEdgeInfo> edges = [];

        if (edgesRaw is null || edgesRaw is string || edgesRaw is not IEnumerable edgeCollection)
        {
            return edges;
        }

        foreach (object? edgeObj in edgeCollection)
        {
            if (!TryReadEdgeFields(edgeObj, out string? edgeTypeStr, out float confidence, out string? originStr, out string? connectedId, out string? direction, out string? verifiedBy, out float? previousConfidence) ||
                string.IsNullOrWhiteSpace(edgeTypeStr) ||
                string.IsNullOrWhiteSpace(connectedId) ||
                string.IsNullOrWhiteSpace(direction) ||
                !TryParseEdgeType(edgeTypeStr, out EdgeType edgeType))
            {
                continue;
            }

            EdgeOrigin origin = TryParseEdgeOrigin(originStr, out EdgeOrigin parsedOrigin)
                ? parsedOrigin
                : EdgeOrigin.Explicit;

            edges.Add(new TraversalEdgeInfo(edgeType, confidence, origin, connectedId, direction)
            {
                VerifiedBy = verifiedBy,
                PreviousConfidence = previousConfidence,
            });
        }

        return edges;
    }

    private async Task<TraversalNode?> ParseTraversalNodeAsync(
        string tenantId,
        Record record,
        string content,
        CancellationToken cancellationToken,
        FallbackTraversalNodeData? fallbackData = null)
    {
        if (!TryGetRequiredString(record, "nodeId", out string? nodeId))
        {
            return null;
        }

        long hopDistance = TryGetRecordValue(record, "hopDistance", out long parsedHopDistance)
            ? parsedHopDistance
            : 0;

        string? sourceUri = TryGetOptionalString(record, "sourceUri");
        string? sourceTypeValue = TryGetOptionalString(record, "sourceType");
        string? ingestedAtValue = TryGetOptionalString(record, "ingestedAt");

        SourceType? sourceType = TryParseSourceType(sourceTypeValue, out SourceType parsedSourceType)
            ? parsedSourceType
            : null;
        DateTimeOffset? ingestedAt = TryParseDateTimeOffset(ingestedAtValue, out DateTimeOffset parsedIngestedAt)
            ? parsedIngestedAt
            : null;

        if (string.IsNullOrWhiteSpace(sourceUri) ||
            sourceType is null ||
            ingestedAt is null)
        {
            FallbackTraversalNodeData? fallback = fallbackData ?? await LoadFallbackTraversalNodeDataAsync(tenantId, nodeId, cancellationToken)
                .ConfigureAwait(false);

            sourceUri = string.IsNullOrWhiteSpace(sourceUri) ? fallback?.SourceUri : sourceUri;
            sourceType ??= fallback?.SourceType;
            ingestedAt ??= fallback?.IngestedAt;
        }

        if (string.IsNullOrWhiteSpace(sourceUri) ||
            sourceType is null ||
            ingestedAt is null)
        {
            LogTraversalNodeSkipped(_logger, tenantId, nodeId);
            return null;
        }

        List<TraversalEdgeInfo> edges = TryGetRecordValue(record, "edges", out object? edgesRaw)
            ? ParseEdgeCollection(edgesRaw)
            : [];

        return new TraversalNode(
            nodeId!,
            TruncateContent(content),
            sourceUri!,
            sourceType.Value,
            ingestedAt.Value,
            checked((int)hopDistance),
            edges);
    }

    private async Task<FallbackTraversalNodeData?> LoadFallbackTraversalNodeDataAsync(
        string tenantId,
        string nodeId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        HashEntry[] entries = await _redis.GetDatabase()
            .HashGetAllAsync($"{tenantId}:mu:{nodeId}")
            .ConfigureAwait(false);

        if (entries.Length == 0)
        {
            return null;
        }

        Dictionary<string, string> fields = [];
        foreach (HashEntry entry in entries)
        {
            fields[entry.Name.ToString()] = entry.Value.ToString();
        }

        SourceType? sourceType = fields.TryGetValue("sourceType", out string? sourceTypeValue) && TryParseSourceType(sourceTypeValue, out SourceType parsedSourceType)
            ? parsedSourceType
            : null;
        DateTimeOffset? ingestedAt = fields.TryGetValue("ingestedAt", out string? ingestedAtValue) && TryParseDateTimeOffset(ingestedAtValue, out DateTimeOffset parsedIngestedAt)
            ? parsedIngestedAt
            : null;

        return new FallbackTraversalNodeData(
            GetNonEmptyField(fields, "content"),
            GetNonEmptyField(fields, "sourceUri"),
            sourceType,
            ingestedAt);
    }

    private static T? TryGetRecordValueOrDefault<T>(Record record, string key)
        => TryGetRecordValue(record, key, out T? value) ? value : default;

    private static bool TryGetRecordValue<T>(Record record, string key, out T? value)
    {
        try
        {
            value = record.GetValue<T>(key);
            return value is not null;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    private static bool TryGetRequiredString(Record record, string key, [NotNullWhen(true)] out string? value)
    {
        value = TryGetOptionalString(record, key);
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string? TryGetOptionalString(Record record, string key)
        => TryGetRecordValue(record, key, out object? value)
            ? value?.ToString()
            : null;

    private static bool TryReadEdgeFields(
        object? edgeValue,
        out string? edgeType,
        out float confidence,
        out string? origin,
        out string? connectedId,
        out string? direction,
        out string? verifiedBy,
        out float? previousConfidence)
    {
        edgeType = null;
        origin = null;
        connectedId = null;
        direction = null;
        verifiedBy = null;
        previousConfidence = null;
        confidence = 0f;

        if (edgeValue is null || !TryNormalizeEdgeMap(edgeValue, out Dictionary<string, object?> edgeMap))
        {
            return false;
        }

        edgeType = TryGetMapString(edgeMap, "edgeType");
        origin = TryGetMapString(edgeMap, "origin");
        connectedId = TryGetMapString(edgeMap, "connectedId");
        direction = TryGetMapString(edgeMap, "direction");
        verifiedBy = TryGetMapString(edgeMap, "verifiedBy");
        _ = TryConvertToSingle(edgeMap.TryGetValue("confidence", out object? confidenceValue) ? confidenceValue : null, out confidence);
        if (TryConvertToSingle(edgeMap.TryGetValue("previousConfidence", out object? pcValue) ? pcValue : null, out float pcFloat))
        {
            previousConfidence = pcFloat;
        }

        return true;
    }

    private static bool TryNormalizeEdgeMap(object edgeValue, out Dictionary<string, object?> edgeMap)
    {
        edgeMap = [];

        if (edgeValue is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                string? key = entry.Key?.ToString();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    edgeMap[key] = entry.Value;
                }
            }

            return edgeMap.Count > 0;
        }

        if (edgeValue is IEnumerable sequence && edgeValue is not string)
        {
            List<object?> values = [];
            foreach (object? item in sequence)
            {
                values.Add(item);
            }

            if (TryCreateEdgeMapFromSequence(values, out edgeMap))
            {
                return true;
            }
        }

        foreach (PropertyInfo property in edgeValue.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || !IsKnownEdgeFieldName(property.Name))
            {
                continue;
            }

            edgeMap[property.Name] = property.GetValue(edgeValue);
        }

        return edgeMap.Count > 0;
    }

    private static bool TryCreateEdgeMapFromSequence(
        IReadOnlyList<object?> values,
        out Dictionary<string, object?> edgeMap)
    {
        edgeMap = [];

        if (values.Count == 7)
        {
            edgeMap["edgeType"] = values[0];
            edgeMap["confidence"] = values[1];
            edgeMap["origin"] = values[2];
            edgeMap["connectedId"] = values[3];
            edgeMap["direction"] = values[4];
            edgeMap["verifiedBy"] = values[5];
            edgeMap["previousConfidence"] = values[6];
            return true;
        }

        if (values.Count == 5)
        {
            edgeMap["edgeType"] = values[0];
            edgeMap["confidence"] = values[1];
            edgeMap["origin"] = values[2];
            edgeMap["connectedId"] = values[3];
            edgeMap["direction"] = values[4];
            return true;
        }

        if (values.Count % 2 != 0)
        {
            return false;
        }

        bool foundKnownField = false;
        for (int i = 0; i < values.Count; i += 2)
        {
            string? key = values[i]?.ToString();
            if (!string.IsNullOrWhiteSpace(key) && IsKnownEdgeFieldName(key))
            {
                edgeMap[key] = values[i + 1];
                foundKnownField = true;
            }
        }

        return foundKnownField;
    }

    private static bool IsKnownEdgeFieldName(string name)
        => string.Equals(name, "edgeType", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "confidence", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "origin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "connectedId", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "direction", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "verifiedBy", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "previousConfidence", StringComparison.OrdinalIgnoreCase);

    private static string? TryGetMapString(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out object? value)
            ? value?.ToString()
            : null;

    private static bool TryConvertToSingle(object? value, out float result)
    {
        switch (value)
        {
            case float floatValue:
                result = floatValue;
                return true;
            case double doubleValue:
                result = (float)doubleValue;
                return true;
            case decimal decimalValue:
                result = (float)decimalValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case long longValue:
                result = longValue;
                return true;
            case RedisValue redisValue when redisValue.HasValue &&
                float.TryParse(redisValue.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float redisParsed):
                result = redisParsed;
                return true;
            case not null when float.TryParse(value.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float parsed):
                result = parsed;
                return true;
            default:
                result = 0f;
                return false;
        }
    }

    private static bool TryParseDateTimeOffset(string? value, out DateTimeOffset result)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result);

    private static bool TryParseEdgeType(string? value, out EdgeType edgeType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            edgeType = default;
            return false;
        }

        try
        {
            edgeType = ParseEdgeType(value);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            edgeType = default;
            return false;
        }
    }

    private static bool TryParseEdgeOrigin(string? value, out EdgeOrigin origin)
        => Enum.TryParse(value, ignoreCase: true, out origin)
        && Enum.IsDefined(origin);

    private static bool TryParseSourceType(string? value, out SourceType sourceType)
        => Enum.TryParse(value, ignoreCase: true, out sourceType)
        && Enum.IsDefined(sourceType);

    private static string? GetNonEmptyField(IReadOnlyDictionary<string, string> fields, string fieldName)
        => fields.TryGetValue(fieldName, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    private sealed record FallbackTraversalNodeData(
        string? Content,
        string? SourceUri,
        SourceType? SourceType,
        DateTimeOffset? IngestedAt);

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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping traversal node {NodeId} in tenant {TenantId} because required context is missing")]
    private static partial void LogTraversalNodeSkipped(ILogger logger, string tenantId, string nodeId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Traversal failed: tenant={TenantId}, startNode={StartNodeId}")]
    private static partial void LogTraversalError(ILogger logger, string tenantId, string startNodeId, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Confidence promoted: tenant={TenantId}, source={SourceNodeId}, target={TargetNodeId}, edgeType={EdgeType}, {PreviousConfidence} → {NewConfidence}, verifiedBy={VerifiedBy}")]
    private static partial void LogConfidencePromoted(ILogger logger, string tenantId, string sourceNodeId, string targetNodeId, EdgeType edgeType, float previousConfidence, float newConfidence, string verifiedBy);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Edge not found for confidence promotion: tenant={TenantId}, source={SourceNodeId}, target={TargetNodeId}, edgeType={EdgeType}")]
    private static partial void LogEdgeNotFound(ILogger logger, string tenantId, string sourceNodeId, string targetNodeId, EdgeType edgeType);
}
