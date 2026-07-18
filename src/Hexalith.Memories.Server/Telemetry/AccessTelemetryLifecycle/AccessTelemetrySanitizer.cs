// <copyright file="AccessTelemetrySanitizer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

/// <summary>Synchronously maps typed logger state to the ratified privacy-safe V1 record.</summary>
internal sealed class AccessTelemetrySanitizer
{
    private static readonly IReadOnlyDictionary<string, int> SuccessEventIds = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["search"] = 7501,
        ["ingest"] = 7502,
        ["traverse"] = 7503,
        ["case-access"] = 7504,
        ["delete"] = 7505,
        ["tenant-lifecycle"] = 7506,
        ["tenant-config"] = 7507,
        ["case-member"] = 7508,
        ["annotation"] = 7509,
    };

    private static readonly IReadOnlyDictionary<string, HashSet<string>> AllowedSourceKeys =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["search"] = new(StringComparer.Ordinal) { "axis", "axes", "attributeFilterCount", "explain", "graphWeight", "maxResults", "metadataFilterCount", "nlWeight", "offset", "query", "semanticWeight", "sourceType", "subject", "syntacticWeight", "tokenBudget" },
            ["ingest"] = new(StringComparer.Ordinal) { "aggregateType", "bytes", "cloudEventId", "cloudEventType", "contentType", "eventOutcome", "sourceType" },
            ["traverse"] = new(StringComparer.Ordinal) { "depth", "edgeTypes", "startNodeId", "tokenBudget" },
            ["case-access"] = new(StringComparer.Ordinal) { "memoryUnitId", "sourceUri" },
            ["delete"] = new(StringComparer.Ordinal) { "memoryUnitIdPrefix", "operation" },
            ["tenant-lifecycle"] = new(StringComparer.Ordinal) { "operation", "state", "workflowInstanceIdPrefix" },
            ["tenant-config"] = new(StringComparer.Ordinal) { "changedFields", "fieldCount", "forceReindex", "operation" },
            ["case-member"] = new(StringComparer.Ordinal) { "memberIdPrefix", "operation" },
            ["annotation"] = new(StringComparer.Ordinal) { "memoryUnitIdPrefix", "operation" },
        };

    private readonly byte[] _markerKey;
    private readonly string _markerKeyId;
    private readonly TimeProvider _timeProvider;
    private readonly MonotonicRecordIdGenerator _recordIds;
    private readonly TimeSpan _retention;

    /// <summary>Initializes a sanitizer with already-loaded marker-key material.</summary>
    public AccessTelemetrySanitizer(
        byte[] markerKey,
        string markerKeyId,
        TimeProvider timeProvider,
        MonotonicRecordIdGenerator recordIds,
        TimeSpan retention)
    {
        ArgumentNullException.ThrowIfNull(markerKey);
        if (markerKey.Length < 32)
        {
            throw new ArgumentException("Marker key must contain at least 256 bits.", nameof(markerKey));
        }

        _markerKey = markerKey.ToArray();
        _markerKeyId = markerKeyId ?? throw new ArgumentNullException(nameof(markerKeyId));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _recordIds = recordIds ?? throw new ArgumentNullException(nameof(recordIds));
        _retention = retention;
    }

    /// <summary>Attempts to sanitize one exact logger tuple.</summary>
    public bool TrySanitize(
        LogLevel level,
        EventId eventId,
        AccessTelemetryEvent source,
        out AccessTelemetryRecord? record,
        out AccessTelemetryReason reason)
    {
        record = null;
        reason = AccessTelemetryReason.SchemaMismatch;
        try
        {
            if (!ValidateTuple(level, eventId.Id, source) || source.SchemaVersion != 1 ||
                source.DurationMs is < 0 or > 86_400_000 || !TryReadTimestamp(source.Timestamp, out DateTimeOffset emittedAt))
            {
                return false;
            }

            if (!AllowedSourceKeys[source.OperationType].IsSupersetOf(source.QueryParams.Keys))
            {
                return false;
            }
            ValidateSourceValueTypes(source.OperationType, source.QueryParams);

            bool rejectedTenant = string.Equals(source.TenantId, "__rejected__", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(source.TenantId);
            string tenantMarker = rejectedTenant ? "__rejected__" : CreateMarker("tenant", source.TenantId);
            string? userMarker = rejectedTenant || string.IsNullOrWhiteSpace(source.User) ? null : CreateMarker("user", source.User);
            string? caseMarker = rejectedTenant || string.IsNullOrWhiteSpace(source.CaseId) ? null : CreateMarker("case", source.CaseId);
            IReadOnlyDictionary<string, object?> queryParams = TransformQueryParams(source, rejectedTenant);
            ValidateCaseAndResult(source, caseMarker);

            DateTimeOffset expiresAt = emittedAt.Add(_retention);
            AccessTelemetryRecord candidate = new()
            {
                AcceptedAtUtc = FormatTimestamp(_timeProvider.GetUtcNow()),
                CaseMarker = caseMarker,
                DurationMs = source.DurationMs,
                EmittedAtUtc = FormatTimestamp(emittedAt),
                EnvelopeHash = string.Empty,
                ErrorCode = MapErrorCode(source.Outcome, source.ErrorCode),
                EventId = eventId.Id,
                ExpiresAtUtc = FormatTimestamp(expiresAt),
                MarkerKeyId = _markerKeyId,
                OperationType = source.OperationType,
                Outcome = source.Outcome,
                QueryParams = queryParams,
                RecordId = _recordIds.NewId(),
                ResultCount = source.ResultCount,
                SchemaVersion = 1,
                SpanId = rejectedTenant ? null : source.SpanId,
                TenantMarker = tenantMarker,
                TraceId = rejectedTenant ? null : source.TraceId,
                UserMarker = userMarker,
            };
            record = candidate with { EnvelopeHash = AccessTelemetryCanonicalizer.CalculateEnvelopeHash(candidate) };
            _ = AccessTelemetryCanonicalizer.CanonicalizeRecord(record);
            reason = AccessTelemetryReason.None;
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            record = null;
            reason = AccessTelemetryReason.SchemaMismatch;
            return false;
        }
    }

    private static string BucketByteLength(long length)
        => length switch
        {
            0 => "0",
            <= 64 * 1024 => "1-64KiB",
            <= 1024 * 1024 => "64KiB-1MiB",
            <= 10 * 1024 * 1024 => "1-10MiB",
            _ => "10MiB+",
        };

    private static string BucketFieldCount(int count)
        => count switch
        {
            <= 0 => "0",
            1 => "1",
            <= 3 => "2-3",
            <= 8 => "4-8",
            _ => "9+",
        };

    private static string BucketQueryLength(int length)
        => length switch
        {
            0 => "0",
            <= 32 => "1-32",
            <= 128 => "33-128",
            <= 256 => "129-256",
            <= 1024 => "257-1024",
            _ => "1025+",
        };

    private static string ClassifyAxis(string? axis)
        => axis switch
        {
            "syntactic" or "semantic" or "graph" or "natural-language" or "hybrid" or
            "graph-scoped-syntactic" or "graph-scoped-semantic" => axis,
            "nl" => "natural-language",
            _ => "unknown",
        };

    private static string ClassifyContentKind(string? contentType)
    {
        if (contentType is null)
        {
            return "unknown";
        }

        return contentType.Split('/', 2, StringSplitOptions.TrimEntries)[0].ToLowerInvariant() switch
        {
            "text" => contentType.Contains("plain", StringComparison.OrdinalIgnoreCase) ? "text" : "document",
            "application" => "document",
            "image" => "image",
            "audio" => "audio",
            _ => "unknown",
        };
    }

    private static string ClassifySourceKind(string? value)
        => value?.ToLowerInvariant() switch
        {
            "file" or "upload" or "directory" => "file",
            "url" => "url",
            "event" => "event",
            "command" => "command",
            "projection" => "projection",
            "discussion" => "discussion",
            "annotation" => "annotation",
            _ => "unknown",
        };

    private static string ClassifyEventOutcome(string? value)
        => value?.ToLowerInvariant() switch
        {
            null => "not-applicable",
            "accepted" => "accepted",
            "duplicate" => "duplicate",
            "rejected" => "rejected",
            _ => "unknown",
        };

    private static string ClassifyWorkflowState(string? value)
        => value?.ToLowerInvariant() switch
        {
            null => "not-applicable",
            "pending" => "pending",
            "running" => "running",
            "completed" => "completed",
            "failed" => "failed",
            "terminated" => "terminated",
            _ => "unknown",
        };

    private static string ClassifyUri(string? sourceUri)
    {
        if (!Uri.TryCreate(sourceUri, UriKind.Absolute, out Uri? uri))
        {
            return "unknown";
        }

        return uri.Scheme switch
        {
            "http" or "https" => "url",
            "file" => "file",
            _ => "other",
        };
    }

    private static string ClassifyWeightProfile(IReadOnlyDictionary<string, object?> values)
    {
        string[] keys = ["syntacticWeight", "semanticWeight", "graphWeight", "nlWeight"];
        object?[] supplied = keys.Where(values.ContainsKey).Select(key => values[key]).ToArray();
        if (supplied.Length == 0 || supplied.All(static value => value is null))
        {
            return "configured";
        }

        double[] weights = supplied.Select(ReadDouble).ToArray();
        return weights.All(static value => double.IsFinite(value) && value >= 0) && weights.Sum() > 0
            ? "request-override"
            : "invalid";
    }

    private static string FormatTimestamp(DateTimeOffset value)
    {
        long milliseconds = value.ToUnixTimeMilliseconds();
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime.ToString(
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture);
    }

    private static bool GetBool(IReadOnlyDictionary<string, object?> values, string name, bool defaultValue)
    {
        if (!values.TryGetValue(name, out object? value) || value is null)
        {
            return defaultValue;
        }

        return value is bool result ? result : throw new AccessTelemetryContractException("query_params_value_invalid");
    }

    private static int GetInt(IReadOnlyDictionary<string, object?> values, string name, int defaultValue)
    {
        if (!values.TryGetValue(name, out object? value) || value is null)
        {
            return defaultValue;
        }

        return value is int result
            ? result
            : throw new AccessTelemetryContractException("query_params_value_invalid");
    }

    private static long GetLong(IReadOnlyDictionary<string, object?> values, string name, long defaultValue)
    {
        if (!values.TryGetValue(name, out object? value) || value is null)
        {
            return defaultValue;
        }

        return value is long result
            ? result
            : throw new AccessTelemetryContractException("query_params_value_invalid");
    }

    private static string? GetString(IReadOnlyDictionary<string, object?> values, string name)
    {
        if (!values.TryGetValue(name, out object? value) || value is null)
        {
            return null;
        }

        return value is string result ? result : throw new AccessTelemetryContractException("query_params_value_invalid");
    }

    private static string MapErrorCode(string outcome, string? source)
    {
        if (outcome == "ok")
        {
            if (source is not null)
            {
                throw new AccessTelemetryContractException("error_code_invalid");
            }

            return null!;
        }

        string code = source?.Trim().ToUpperInvariant() ?? string.Empty;
        if (code.Length > 128 || code.Length == 0 || code == "UNKNOWN")
        {
            return "unknown";
        }

        if (code.StartsWith("INVALID_", StringComparison.Ordinal) || code.StartsWith("MISSING_", StringComparison.Ordinal) ||
            code is "PAGINATION_LIMIT_EXCEEDED" or "BATCH_TOO_LARGE" or "NESTED_ANNOTATION_NOT_ALLOWED")
        {
            return "invalid_input";
        }

        if (code == "NOT_FOUND" || code.EndsWith("_NOT_FOUND", StringComparison.Ordinal) || code == "UNKNOWN_SOURCE")
        {
            return "not_found";
        }

        if (code is "FORBIDDEN" or "AUTO_CREATE_DISABLED" or "DIRECTORY_INGESTION_DISABLED" || code.EndsWith("_FORBIDDEN", StringComparison.Ordinal))
        {
            return "forbidden";
        }

        if (code is "CONFLICT" or "CASE_MISMATCH" or "MEMBER_LIMIT_EXCEEDED" or "CASE_CAP_EXCEEDED" or "MEMORY_UNIT_NOT_INDEXED" ||
            code.EndsWith("_CONFLICT", StringComparison.Ordinal) || code.EndsWith("_DELETING", StringComparison.Ordinal) || code.EndsWith("_PROVISIONING", StringComparison.Ordinal))
        {
            return "conflict";
        }

        if (code is "CANCELLED" or "REQUEST_CANCELLED")
        {
            return "cancelled";
        }

        if (code is "DAPR_UNAVAILABLE" or "BACKEND_UNAVAILABLE" or "ALL_BACKENDS_UNAVAILABLE" or "GRAPH_UNAVAILABLE" or
            "GRAPH_TIMEOUT" or "LOOKUP_BACKEND_UNAVAILABLE" or "BATCH_TRACKING_UNAVAILABLE" or "TENANT_UNAVAILABLE" or
            "TENANT_FAILED" or "HYBRID_DEGRADED" || code.EndsWith("_TIMEOUT", StringComparison.Ordinal))
        {
            return "dependency_unavailable";
        }

        return code switch
        {
            "RATE_LIMITED" or "TOO_MANY_REQUESTS" or "HTTP_429" => "rate_limited",
            "SCHEDULE_FAILED" or "BATCH_SCHEDULING_FAILED" => "internal_dependency_failure",
            "UNHANDLED_EXCEPTION" or "HTTP_500" or "HTTP_502" or "HTTP_503" => "internal_failure",
            _ => "unknown",
        };
    }

    private static double ReadDouble(object? value)
        => value is double result ? result : double.NaN;

    private static void ValidateSourceValueTypes(
        string operation,
        IReadOnlyDictionary<string, object?> values)
    {
        switch (operation)
        {
            case "search":
                RequireSourceType<string>(values, "axis", "axes", "query", "sourceType", "subject");
                RequireSourceType<int>(values, "attributeFilterCount", "maxResults", "metadataFilterCount", "offset", "tokenBudget");
                RequireSourceType<bool>(values, "explain");
                RequireSourceType<double>(values, "graphWeight", "nlWeight", "semanticWeight", "syntacticWeight");
                break;
            case "ingest":
                RequireSourceType<string>(values, "aggregateType", "cloudEventId", "cloudEventType", "contentType", "eventOutcome", "sourceType");
                RequireSourceType<long>(values, "bytes");
                break;
            case "traverse":
                RequireSourceType<int>(values, "depth", "tokenBudget");
                RequireSourceType<string>(values, "edgeTypes", "startNodeId");
                break;
            case "case-access":
                RequireSourceType<string>(values, "memoryUnitId", "sourceUri");
                break;
            case "delete":
                RequireSourceType<string>(values, "memoryUnitIdPrefix", "operation");
                break;
            case "tenant-lifecycle":
                RequireSourceType<string>(values, "operation", "state", "workflowInstanceIdPrefix");
                break;
            case "tenant-config":
                RequireSourceType<string[]>(values, "changedFields");
                RequireSourceType<int>(values, "fieldCount");
                RequireSourceType<bool>(values, "forceReindex");
                RequireSourceType<string>(values, "operation");
                break;
            case "case-member":
                RequireSourceType<string>(values, "memberIdPrefix", "operation");
                break;
            case "annotation":
                RequireSourceType<string>(values, "memoryUnitIdPrefix", "operation");
                break;
            default:
                throw new AccessTelemetryContractException("operation_invalid");
        }
    }

    private static void RequireSourceType<T>(
        IReadOnlyDictionary<string, object?> values,
        params string[] keys)
    {
        foreach (string key in keys)
        {
            if (values.TryGetValue(key, out object? value) && value is not null && value is not T)
            {
                throw new AccessTelemetryContractException("query_params_value_invalid");
            }
        }
    }

    private static IReadOnlyDictionary<string, object?> TransformQueryParams(AccessTelemetryEvent source, bool rejectedTenant)
        => source.OperationType switch
        {
            "search" => TransformSearch(source, rejectedTenant),
            "ingest" => TransformIngest(source, rejectedTenant),
            "traverse" => TransformTraverse(source, rejectedTenant),
            "case-access" => TransformCaseAccess(source),
            "delete" => TransformDelete(source),
            "tenant-lifecycle" => TransformTenantLifecycle(source),
            "tenant-config" => TransformTenantConfig(source),
            "case-member" => TransformCaseMember(source),
            "annotation" => TransformAnnotation(source),
            _ => throw new AccessTelemetryContractException("operation_invalid"),
        };

    private static IReadOnlyDictionary<string, object?> TransformCaseAccess(AccessTelemetryEvent source)
    {
        string? uri = GetString(source.QueryParams, "sourceUri");
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["accessKind"] = uri is null ? "memory-unit-id" : "source-uri",
            ["projection"] = "detail",
            ["sourceKind"] = uri is null ? "not-applicable" : ClassifyUri(uri),
        };
    }

    private static IReadOnlyDictionary<string, object?> TransformDelete(AccessTelemetryEvent source)
    {
        string operation = GetString(source.QueryParams, "operation") ?? throw new AccessTelemetryContractException("operation_invalid");
        if (operation is not ("memory-unit-delete" or "case-delete" or "tenant-delete"))
        {
            throw new AccessTelemetryContractException("operation_invalid");
        }

        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["cascade"] = operation is "case-delete" or "tenant-delete",
            ["targetKind"] = operation switch { "case-delete" => "case", "tenant-delete" => "tenant", _ => "memory-unit" },
        };
    }

    private static IReadOnlyDictionary<string, object?> TransformIngest(AccessTelemetryEvent source, bool rejectedTenant)
        => new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["caseScope"] = rejectedTenant ? "rejected-or-unknown" : source.CaseId is null ? "tenant" : "case",
            ["contentKind"] = ClassifyContentKind(GetString(source.QueryParams, "contentType")),
            ["contentLengthBucket"] = BucketByteLength(GetLong(source.QueryParams, "bytes", 0)),
            ["eventOutcome"] = ClassifyEventOutcome(GetString(source.QueryParams, "eventOutcome")),
            ["sourceKind"] = ClassifySourceKind(GetString(source.QueryParams, "sourceType")),
        };

    private static IReadOnlyDictionary<string, object?> TransformSearch(AccessTelemetryEvent source, bool rejectedTenant)
    {
        string? query = GetString(source.QueryParams, "query");
        string? subject = GetString(source.QueryParams, "subject");
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["axis"] = ClassifyAxis(GetString(source.QueryParams, "axis")),
            ["caseScope"] = rejectedTenant ? "rejected-or-unknown" : source.CaseId is null ? "all-authorized" : "single",
            ["explain"] = GetBool(source.QueryParams, "explain", false),
            ["queryLengthBucket"] = BucketQueryLength(query?.Length ?? 0),
            ["subjectPresent"] = !string.IsNullOrWhiteSpace(subject),
            ["weightProfile"] = ClassifyWeightProfile(source.QueryParams),
        };
    }

    private static IReadOnlyDictionary<string, object?> TransformTenantConfig(AccessTelemetryEvent source)
    {
        string configKind = GetString(source.QueryParams, "operation") switch
        {
            "embedding-config-update" => "embedding",
            "display-name-update" => "display-name",
            _ => throw new AccessTelemetryContractException("operation_invalid"),
        };
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["action"] = "update",
            ["changedFieldCountBucket"] = BucketFieldCount(GetInt(source.QueryParams, "fieldCount", 0)),
            ["configKind"] = configKind,
            ["forceReindex"] = GetBool(source.QueryParams, "forceReindex", false),
        };
    }

    private static IReadOnlyDictionary<string, object?> TransformTenantLifecycle(AccessTelemetryEvent source)
        => new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["action"] = GetString(source.QueryParams, "operation") switch
            {
                "tenant-create" => "provision",
                "tenant-provision-status" => "provision-status",
                "tenant-deletion-status" => "deletion-status",
                _ => throw new AccessTelemetryContractException("operation_invalid"),
            },
            ["workflowState"] = ClassifyWorkflowState(GetString(source.QueryParams, "state")),
        };

    private static IReadOnlyDictionary<string, object?> TransformCaseMember(AccessTelemetryEvent source)
        => new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["action"] = GetString(source.QueryParams, "operation") switch
            {
                "case-member-add" => "add",
                "case-member-remove" => "remove",
                _ => throw new AccessTelemetryContractException("operation_invalid"),
            },
            ["role"] = "unknown",
        };

    private static IReadOnlyDictionary<string, object?> TransformAnnotation(AccessTelemetryEvent source)
    {
        if (GetString(source.QueryParams, "operation") != "annotation-create")
        {
            throw new AccessTelemetryContractException("operation_invalid");
        }

        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["action"] = "create",
            ["annotationKind"] = "unknown",
        };
    }

    private static IReadOnlyDictionary<string, object?> TransformTraverse(AccessTelemetryEvent source, bool rejectedTenant)
    {
        int depth = GetInt(source.QueryParams, "depth", -1);
        string depthBucket = depth switch { 0 => "0", >= 1 and <= 5 => depth.ToString(CultureInfo.InvariantCulture), >= 6 and <= 10 => "6-10", _ => "invalid" };
        string? edgeTypes = GetString(source.QueryParams, "edgeTypes");
        int edgeTypeCount = edgeTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(17).Count() ?? 0;
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["caseScope"] = rejectedTenant ? "rejected-or-unknown" : source.CaseId is null ? "all-authorized" : "single",
            ["depthBucket"] = depthBucket,
            ["direction"] = "out",
            ["edgeTypeCount"] = Math.Min(edgeTypeCount, 16),
            ["includeGaps"] = false,
        };
    }

    private static bool TryReadTimestamp(string value, out DateTimeOffset timestamp)
    {
        bool parsed = DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
            out timestamp);
        return parsed && (value.EndsWith('Z') || value.Contains('+') || value.LastIndexOf('-') > 9);
    }

    private static bool ValidateTuple(LogLevel level, int eventId, AccessTelemetryEvent source)
    {
        if (!SuccessEventIds.TryGetValue(source.OperationType, out int successId) || source.EventId != eventId)
        {
            return false;
        }

        return source.Outcome switch
        {
            "ok" => eventId == successId && level == LogLevel.Information,
            "partial" => source.OperationType == "search" && eventId == 7501 && level == LogLevel.Information,
            "error" => eventId == successId + 10 && level == LogLevel.Warning,
            _ => false,
        };
    }

    private static void ValidateCaseAndResult(AccessTelemetryEvent source, string? caseMarker)
    {
        bool rejected = source.TenantId == "__rejected__" || string.IsNullOrWhiteSpace(source.TenantId);
        if (!rejected && source.OperationType is "case-access" or "case-member" or "annotation" && caseMarker is null)
        {
            throw new AccessTelemetryContractException("case_marker_required");
        }

        bool resultRequired = source.OperationType is "search" or "traverse" or "case-access" && source.Outcome is "ok" or "partial";
        if (resultRequired != source.ResultCount.HasValue || source.ResultCount is < 0 or > 1_000_000)
        {
            throw new AccessTelemetryContractException("result_count_invalid");
        }

        string? deleteOperation = source.OperationType == "delete" ? GetString(source.QueryParams, "operation") : null;
        if (!rejected && source.OperationType == "delete" &&
            ((deleteOperation == "tenant-delete") != (caseMarker is null)))
        {
            throw new AccessTelemetryContractException("case_marker_invalid");
        }

        if (!rejected && source.OperationType is "tenant-lifecycle" or "tenant-config" && caseMarker is not null)
        {
            throw new AccessTelemetryContractException("case_marker_invalid");
        }
    }

    private string CreateMarker(string domain, string value)
    {
        byte[] data = Encoding.UTF8.GetBytes(string.Concat(domain, "\0", value.Trim()));
        return Convert.ToHexString(HMACSHA256.HashData(_markerKey, data)).ToLowerInvariant();
    }
}
