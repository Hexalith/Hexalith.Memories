// <copyright file="EventStoreIntegrationLog.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using Microsoft.Extensions.Logging;

/// <summary>Source-generated <see cref="LoggerMessage"/> emitters for Story 9.1 + 9.3 + 16.1. EventId bank <c>9100-9199</c>
/// is pinned for this sub-system. Ranges:
/// <list type="bullet">
///   <item><description>9100-9109 — Information / startup (Story 9.1)</description></item>
///   <item><description>9110-9119 — Warning (drops, unknown-source, tenant-deleting, case cap) (Story 9.1)</description></item>
///   <item><description>9120-9129 — Error (workflow scheduling failures, envelope parse) (Story 9.1)</description></item>
///   <item><description>9130-9139 — Story 9.3 happy-path information (observations recorded, snapshots served)</description></item>
///   <item><description>9140-9149 — Story 9.3 Warning / Debug (observation-store write failures, regex bypass, config change, drops)</description></item>
///   <item><description>9150-9159 — Story 16.1 Warning (projection-binding provider failure, snapshot tenant mismatch, null bindings)</description></item>
/// </list>
/// </summary>
internal static partial class EventStoreIntegrationLog
{
    [LoggerMessage(
        EventId = 9102,
        Level = LogLevel.Information,
        Message = "EventStore ingestion: tenant {TenantId} is provisioning, event {CloudEventId} will be retried.")]
    public static partial void TenantProvisioning(ILogger logger, string tenantId, string cloudEventId);

    [LoggerMessage(
        EventId = 9110,
        Level = LogLevel.Warning,
        Message = "EventStore ingestion: no tenant mapping for source {Source} (cloudEventId={CloudEventId}).")]
    public static partial void UnknownSource(ILogger logger, string source, string cloudEventId);

    [LoggerMessage(
        EventId = 9111,
        Level = LogLevel.Warning,
        Message = "EventStore ingestion: tenant {TenantId} is deleting, event {CloudEventId} dropped.")]
    public static partial void TenantDeleting(ILogger logger, string tenantId, string cloudEventId);

    [LoggerMessage(
        EventId = 9112,
        Level = LogLevel.Warning,
        Message = "EventStore ingestion: tenant {TenantId} not found, event {CloudEventId} dropped.")]
    public static partial void TenantNotFound(ILogger logger, string tenantId, string cloudEventId);

    [LoggerMessage(
        EventId = 9113,
        Level = LogLevel.Warning,
        Message = "EventStore ingestion: auto-create disabled for tenant {TenantId}, event {CloudEventId} dropped.")]
    public static partial void AutoCreateDisabled(ILogger logger, string tenantId, string cloudEventId);

    [LoggerMessage(
        EventId = 9114,
        Level = LogLevel.Warning,
        Message = "EventStore ingestion: case cap exceeded for tenant {TenantId}, event {CloudEventId} dropped.")]
    public static partial void CaseCapExceeded(ILogger logger, string tenantId, string cloudEventId);

    [LoggerMessage(
        EventId = 9120,
        Level = LogLevel.Error,
        Message = "EventStore ingestion: workflow scheduling failed for event {CloudEventId} ({ExceptionType}).")]
    public static partial void WorkflowScheduleFailed(ILogger logger, string cloudEventId, string exceptionType);

    [LoggerMessage(
        EventId = 9121,
        Level = LogLevel.Error,
        Message = "EventStore ingestion: invalid CloudEvents envelope — {Reason} (cloudEventId={CloudEventId}).")]
    public static partial void InvalidEnvelope(ILogger logger, string reason, string cloudEventId);

    [LoggerMessage(
        EventId = 9122,
        Level = LogLevel.Error,
        Message = "EventStore ingestion: preflight release failed for event {CloudEventId} ({ExceptionType}).")]
    public static partial void PreflightReleaseFailed(ILogger logger, string cloudEventId, string exceptionType);

    [LoggerMessage(
        EventId = 9126,
        Level = LogLevel.Error,
        Message = "EventStore ingestion: route resolution failed for event {CloudEventId} ({ExceptionType}).")]
    public static partial void RouteResolutionFailed(ILogger logger, string cloudEventId, string exceptionType);

    // ------------------------------------------------------------------------------------------------
    // Story 9.3 — Handler registry + mismatch detection (bank 9130-9149).
    // ------------------------------------------------------------------------------------------------

    [LoggerMessage(
        EventId = 9130,
        Level = LogLevel.Debug,
        Message = "Observation recorded for tenant {TenantId}, aggregate {AggregateType}, eventType {EventType}.")]
    public static partial void ObservedEventTypeRecorded(ILogger logger, string tenantId, string aggregateType, string eventType);

    [LoggerMessage(
        EventId = 9131,
        Level = LogLevel.Information,
        Message = "Handler registry snapshot served with {HandlersCount} handler row(s).")]
    public static partial void HandlerRegistrySnapshotServed(ILogger logger, int handlersCount);

    [LoggerMessage(
        EventId = 9132,
        Level = LogLevel.Information,
        Message = "Handler mismatch detected for tenant {TenantId}: category={Category}, severity={Severity}, subject={Subject}.")]
    public static partial void HandlerMismatchDetected(ILogger logger, string tenantId, string category, string severity, string subject);

    [LoggerMessage(
        EventId = 9140,
        Level = LogLevel.Warning,
        Message = "Observation-store write failed for tenant {TenantId} ({ExceptionType}) — fail-open, ingestion unaffected.")]
    public static partial void ObservedEventTypeStoreWriteFailed(ILogger logger, string tenantId, string exceptionType);

    [LoggerMessage(
        EventId = 9141,
        Level = LogLevel.Warning,
        Message = "Regex bypassed for pathological event type (reason={Reason}, truncatedEventType={TruncatedEventType}).")]
    public static partial void RegexSkippedForPathologicalEventType(ILogger logger, string reason, string truncatedEventType);

    [LoggerMessage(
        EventId = 9142,
        Level = LogLevel.Warning,
        Message = "Observation aggregates set at 1024 cap for tenant {TenantId} (cardinality={Cardinality}); SADD skipped for this aggregateType until TTL reset.")]
    public static partial void ObservationAggregatesSetCardinalityWarning(ILogger logger, string tenantId, long cardinality);

    [LoggerMessage(
        EventId = 9143,
        Level = LogLevel.Information,
        Message = "Observation writes kill-switch transition: enabled={Enabled}.")]
    public static partial void ObservationWritesConfigChanged(ILogger logger, bool enabled);

    [LoggerMessage(
        EventId = 9144,
        Level = LogLevel.Warning,
        Message = "Observation dropped for tenant {TenantId} (reason={Reason}).")]
    public static partial void ObservationDropped(ILogger logger, string tenantId, string reason);

    [LoggerMessage(
        EventId = 9146,
        Level = LogLevel.Warning,
        Message = "Per-tenant observation read failed for tenant {TenantId} ({ExceptionType}) — partial snapshot returned.")]
    public static partial void TenantObservationReadFailed(ILogger logger, string tenantId, string exceptionType);

    // ------------------------------------------------------------------------------------------------
    // Story 16.1 — Projection-binding cross-check (bank 9150-9159).
    // ------------------------------------------------------------------------------------------------

    [LoggerMessage(
        EventId = 9150,
        Level = LogLevel.Warning,
        Message = "Projection binding provider failed for tenant {TenantId} ({ExceptionType}) — projection-binding cross-check skipped; existing handler-mismatch diagnostics preserved.")]
    public static partial void ProjectionBindingProviderFailed(ILogger logger, string tenantId, string exceptionType);

    [LoggerMessage(
        EventId = 9151,
        Level = LogLevel.Warning,
        Message = "Projection binding snapshot tenant mismatch: requested {RequestedTenantId} but provider returned {SnapshotTenantId}; projection-binding cross-check skipped.")]
    public static partial void ProjectionBindingSnapshotTenantMismatched(ILogger logger, string requestedTenantId, string snapshotTenantId);

    [LoggerMessage(
        EventId = 9152,
        Level = LogLevel.Warning,
        Message = "Projection binding snapshot for tenant {TenantId} reported Authoritative posture but returned null Bindings list — treating as Unavailable; projection-binding cross-check skipped.")]
    public static partial void ProjectionBindingSnapshotNullBindings(ILogger logger, string tenantId);
}
