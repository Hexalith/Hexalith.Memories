// <copyright file="SearchEndpointDegradationLog.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using System;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>
/// Structured warning log events emitted when search endpoints translate backend failures into
/// degraded (per-axis) or total-failure (503) responses. Event IDs are pinned for dashboard wiring:
/// 5601 = single-axis backend unavailable, 5602 = graph backend unavailable,
/// 5603 = hybrid total failure.
/// </summary>
internal static partial class SearchEndpointDegradationLog
{
    internal static string DescribeFailureReason(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is RedisServerException redisException
            && GetTransientRedisReason(redisException) is { } transientReason)
        {
            return transientReason;
        }

        return exception.GetType().Name;
    }

    /// <summary>
    /// Returns <c>true</c> when the Redis server error message indicates a transient, recoverable
    /// condition (warming up, memory pressure, or temporary busy). These conditions are treated as
    /// backend-unavailable rather than missing-data. Messages matching "no such index" or
    /// "Unknown Index name" are explicitly NOT transient — they indicate missing indices, which
    /// search services handle as empty results.
    /// </summary>
    /// <param name="exception">The Redis server exception to classify.</param>
    /// <returns><c>true</c> for transient LOADING/BUSY/OOM states; <c>false</c> otherwise.</returns>
    internal static bool IsTransientRedisError(RedisServerException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return GetTransientRedisReason(exception) is not null;
    }

    private static string? GetTransientRedisReason(RedisServerException exception)
    {
        string message = exception.Message;
        if (RediSearchErrorClassifier.IsMissingIndexError(exception))
        {
            return null;
        }

        if (message.Contains("LOADING", StringComparison.OrdinalIgnoreCase))
        {
            return "LOADING";
        }

        if (message.Contains("BUSY", StringComparison.OrdinalIgnoreCase))
        {
            return "BUSY";
        }

        return message.Contains("OOM", StringComparison.OrdinalIgnoreCase)
            ? "OOM"
            : null;
    }

    [LoggerMessage(
        EventId = 5601,
        Level = LogLevel.Warning,
        Message = "Search backend {Axis} unavailable for tenant {TenantId}: {Reason} ({DegradationType})")]
    internal static partial void LogBackendUnavailable(
        ILogger logger,
        string axis,
        string tenantId,
        string reason,
        string degradationType);

    [LoggerMessage(
        EventId = 5602,
        Level = LogLevel.Warning,
        Message = "Graph backend {Axis} unavailable for tenant {TenantId}, startNode={StartNodeId}: {Reason} ({DegradationType})")]
    internal static partial void LogGraphUnavailable(
        ILogger logger,
        string axis,
        string tenantId,
        string? startNodeId,
        string reason,
        string degradationType);

    [LoggerMessage(
        EventId = 5603,
        Level = LogLevel.Warning,
        Message = "Hybrid search total failure for tenant {TenantId}: {Reason}; unavailable ({UnavailableAxes}); enabled={EnabledAxes} ({DegradationType})")]
    internal static partial void LogHybridTotalFailure(
        ILogger logger,
        string tenantId,
        string unavailableAxes,
        string enabledAxes,
        string reason,
        string degradationType);
}
