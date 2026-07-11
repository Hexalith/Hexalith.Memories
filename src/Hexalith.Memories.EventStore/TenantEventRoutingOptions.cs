// <copyright file="TenantEventRoutingOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Configuration options for the EventStore pub/sub subscription binding.
/// Bound from the <c>EventStoreIntegration:Routing</c> configuration section via <c>IOptions&lt;T&gt;</c>.</summary>
public sealed class TenantEventRoutingOptions
{
    /// <summary>Gets or sets the name of the DAPR pub/sub component the subscription uses.
    /// Matches the component <c>metadata.name</c> in <c>deploy/dapr/components/pubsub.yaml</c>. Required.</summary>
    public string PubSubName { get; set; } = "pubsub";

    /// <summary>Gets or sets the topic the server subscribes to. MVP: single topic per deployment. Required.</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>Gets the map of CloudEvents <c>source</c> prefix → tenant id. Longest-prefix wins, case-insensitive.
    /// Unknown sources are dropped with a warning at the subscription endpoint.</summary>
    public Dictionary<string, string> SourceToTenantMap { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets a value indicating whether the routed (index) tenants in
    /// <see cref="SourceToTenantMap"/> are auto-provisioned at startup when missing. Default: <c>false</c>
    /// (production registers index tenants explicitly via <c>POST /api/v1/tenants</c> and the routing validator
    /// fail-fasts on unknown tenants). Dev/single-process hosts that own a well-known curated index (e.g. the
    /// Tenants AppHost provisioning <c>tenants-index</c>) set this <c>true</c> so the index exists before the
    /// first event arrives.</summary>
    public bool AutoProvisionRoutedTenants { get; set; }

    /// <summary>Gets or sets a value indicating whether the router may lazily create a case on first event per
    /// <c>(tenantId, aggregateType)</c>. Default: <c>true</c> for development parity with PRD §534 "zero-code"
    /// zero-config story; production overrides to <c>false</c> per ADR 9.1-C.</summary>
    public bool AutoCreateCases { get; set; } = true;

    /// <summary>Gets or sets the token-replacement template used when auto-creating a case. Allowed tokens:
    /// <c>{aggregateType}</c>, <c>{tenantId}</c>. Raw <c>string.Format</c> is not used.</summary>
    public string CaseNameTemplate { get; set; } = "events:{aggregateType}";

    /// <summary>Gets or sets the hard cap on auto-created cases per tenant. Once exceeded the router returns
    /// <see cref="TenantEventRouteResolutionStatus.CaseCapExceeded"/> without calling case creation.</summary>
    public int MaxAutoCreatedCasesPerTenant { get; set; } = 100;

    /// <summary>Gets or sets a value indicating whether endpoint-level preflight dedup reservation is enabled.
    /// When disabled only the workflow-level permanent dedup key is the safety net (Story 1.6 posture).</summary>
    public bool PreflightDedupEnabled { get; set; } = true;

    /// <summary>Gets or sets the TTL on preflight dedup reservation keys. Must be aligned with the configured
    /// DAPR resiliency policy max-duration — see <c>docs/dev/eventstore-integration.md</c>.</summary>
    public TimeSpan PreflightDedupTtl { get; set; } = TimeSpan.FromHours(24);
}
