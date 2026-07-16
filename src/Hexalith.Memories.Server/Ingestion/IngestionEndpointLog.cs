// <copyright file="IngestionEndpointLog.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;

/// <summary>
/// Structured log events for Story 6.1 URL + directory ingestion surfaces. Event IDs 6101-6108 are
/// pinned for dashboard/alert wiring — do NOT reuse these IDs elsewhere.
/// </summary>
internal static partial class IngestionEndpointLog
{
    /// <summary>Redacts a URL to scheme + host + path (drops query and fragment) for log safety.</summary>
    internal static string RedactUrl(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
    }

    /// <summary>Best-effort redaction for raw URL input that may not parse.</summary>
    internal static string RedactUrl(string? url)
        => Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
            ? RedactUrl(uri)
            : "(invalid-url)";

    [LoggerMessage(
        EventId = 6101,
        Level = LogLevel.Information,
        Message = "URL ingestion scheduled for tenant {TenantId}, case {CaseId}, instance {InstanceId}, url {RedactedUrl}")]
    internal static partial void LogUrlIngestionScheduled(
        ILogger logger,
        string tenantId,
        string caseId,
        string instanceId,
        string redactedUrl);

    [LoggerMessage(
        EventId = 6102,
        Level = LogLevel.Warning,
        Message = "URL ingestion rejected for tenant {TenantId}, case {CaseId}, url {RedactedUrl}: {ErrorCode}")]
    internal static partial void LogUrlIngestionRejected(
        ILogger logger,
        string tenantId,
        string caseId,
        string redactedUrl,
        string errorCode);

    [LoggerMessage(
        EventId = 6103,
        Level = LogLevel.Information,
        Message = "Directory batch scheduled for tenant {TenantId}, case {CaseId}, batch {BatchId}: discovered={Discovered}, enqueued={Enqueued}, skipped={SkippedCount}")]
    internal static partial void LogDirectoryBatchScheduled(
        ILogger logger,
        string tenantId,
        string caseId,
        string batchId,
        int discovered,
        int enqueued,
        int skippedCount);

    [LoggerMessage(
        EventId = 6104,
        Level = LogLevel.Warning,
        Message = "Directory batch rejected for tenant {TenantId}, case {CaseId}, batch {BatchId}, path {DirectoryPath}: {ErrorCode}")]
    internal static partial void LogDirectoryBatchRejected(
        ILogger logger,
        string tenantId,
        string caseId,
        string? batchId,
        string errorCode,
        string directoryPath);

    [LoggerMessage(
        EventId = 6105,
        Level = LogLevel.Information,
        Message = "URL fetch started for memory unit {MemoryUnitId}, url {RedactedUrl}")]
    internal static partial void LogUrlFetchStarted(
        ILogger logger,
        string memoryUnitId,
        string redactedUrl);

    [LoggerMessage(
        EventId = 6106,
        Level = LogLevel.Information,
        Message = "URL fetch completed for memory unit {MemoryUnitId}, status {HttpStatus}, bytes {ByteCount}, elapsedMs {ElapsedMs}, finalUrl {FinalRedactedUrl}")]
    internal static partial void LogUrlFetchCompleted(
        ILogger logger,
        string memoryUnitId,
        int httpStatus,
        long byteCount,
        long elapsedMs,
        string finalRedactedUrl);

    [LoggerMessage(
        EventId = 6107,
        Level = LogLevel.Warning,
        Message = "URL fetch failed for memory unit {MemoryUnitId}: {ErrorCode} (httpStatus={HttpStatus}, elapsedMs={ElapsedMs})")]
    internal static partial void LogUrlFetchFailed(
        ILogger logger,
        string memoryUnitId,
        string errorCode,
        int? httpStatus,
        long elapsedMs);

    [LoggerMessage(
        EventId = 6108,
        Level = LogLevel.Information,
        Message = "Directory file skipped for batch {BatchId}: {Path} ({Reason})")]
    internal static partial void LogDirectoryFileSkipped(
        ILogger logger,
        string batchId,
        string path,
        string reason);
}
