// <copyright file="TenantMismatchMonitor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Logging;

/// <summary>Observability sink for <c>TENANT_MISMATCH</c> detections.
/// <para>
/// Under Hexalith.Memories' physical isolation model a tenant mismatch between a requested tenantId
/// and the <c>tenantId</c> field persisted on a record is structurally impossible. If one is detected at
/// runtime it indicates either data corruption or an isolation breach and must be surfaced to operators
/// immediately. A Critical log entry with structured fields is emitted and a process-wide counter is
/// incremented; callers resolve the user-facing response to a standard 404 so no internal state leaks.
/// </para>
/// <para>
/// No full metrics library is pulled in for this single signal (anti-pattern #6 in story 5.4). The
/// <see cref="MismatchCount"/> property can be inspected by tests or exposed via a health check/endpoint
/// later without touching call sites.
/// </para>
/// </summary>
public static partial class TenantMismatchMonitor
{
    private static long _mismatchCount;

    /// <summary>Gets the total number of tenant mismatches observed since the process started.</summary>
    public static long MismatchCount => Interlocked.Read(ref _mismatchCount);

    /// <summary>Resets the counter. Intended for unit-test isolation only; never call from production code.</summary>
    public static void ResetForTests() => Interlocked.Exchange(ref _mismatchCount, 0);

    /// <summary>Records a tenant mismatch detection: logs Critical and increments the counter.</summary>
    /// <param name="logger">The logger associated with the detecting component (e.g. <c>CaseService</c>).</param>
    /// <param name="requestedTenantId">The tenant identifier presented by the caller.</param>
    /// <param name="actualTenantId">The tenant identifier persisted on the record.</param>
    /// <param name="resourceType">The resource type (e.g. <c>MemoryUnit</c>, <c>Case</c>).</param>
    /// <param name="resourceId">The resource identifier on which the mismatch was observed.</param>
    public static void RecordMismatch(
        ILogger logger,
        string requestedTenantId,
        string actualTenantId,
        string resourceType,
        string resourceId)
    {
        _ = Interlocked.Increment(ref _mismatchCount);
        LogTenantMismatch(logger, requestedTenantId, actualTenantId, resourceType, resourceId);
    }

    [LoggerMessage(
        EventId = 5400,
        Level = LogLevel.Critical,
        Message = "TENANT_MISMATCH: request for tenant {RequestedTenantId} returned {ResourceType} {ResourceId} owned by tenant {ActualTenantId} — possible isolation breach or data corruption.")]
    private static partial void LogTenantMismatch(
        ILogger logger,
        string requestedTenantId,
        string actualTenantId,
        string resourceType,
        string resourceId);
}
