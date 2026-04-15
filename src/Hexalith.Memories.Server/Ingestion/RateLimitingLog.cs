// <copyright file="RateLimitingLog.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;

/// <summary>
/// Structured log events for Story 6.2 per-tenant rate limiting and concurrency gating. Event IDs
/// 6201-6206 are pinned for dashboard/alert wiring — do NOT reuse these IDs elsewhere.
/// </summary>
internal static partial class RateLimitingLog
{
    [LoggerMessage(
        EventId = 6201,
        Level = LogLevel.Warning,
        Message = "Rate limit exceeded locally for tenant {TenantId} (actor refused consume).")]
    internal static partial void LogRateLimitExceededLocally(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = 6202,
        Level = LogLevel.Warning,
        Message = "Provider rate limit received for tenant {TenantId}, Retry-After={RetryAfterSeconds}s.")]
    internal static partial void LogProviderRateLimitReceived(
        ILogger logger,
        string tenantId,
        int retryAfterSeconds);

    [LoggerMessage(
        EventId = 6203,
        Level = LogLevel.Information,
        Message = "Rate limit actor updated for tenant {TenantId} — remaining={Remaining}, windowStart={WindowStart}.")]
    internal static partial void LogRateLimitActorUpdated(
        ILogger logger,
        string tenantId,
        int remaining,
        DateTime windowStart);

    [LoggerMessage(
        EventId = 6204,
        Level = LogLevel.Debug,
        Message = "Extraction gate acquired for tenant {TenantId} — available={AvailableCount}.")]
    internal static partial void LogExtractionGateAcquired(
        ILogger logger,
        string tenantId,
        int availableCount);

    [LoggerMessage(
        EventId = 6205,
        Level = LogLevel.Information,
        Message = "Extraction gate contended for tenant {TenantId} — queueDepth={QueueDepth}.")]
    internal static partial void LogExtractionGateContended(
        ILogger logger,
        string tenantId,
        int queueDepth);

    [LoggerMessage(
        EventId = 6206,
        Level = LogLevel.Warning,
        Message = "Extraction gate acquisition timed out for tenant {TenantId} after {TimeoutSeconds}s.")]
    internal static partial void LogExtractionGateTimeout(
        ILogger logger,
        string tenantId,
        int timeoutSeconds);
}
