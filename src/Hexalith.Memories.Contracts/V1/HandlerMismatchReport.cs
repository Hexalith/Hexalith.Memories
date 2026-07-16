// <copyright file="HandlerMismatchReport.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

/// <summary>Story 9.3 — tenant-scoped mismatch report returned by
/// <c>GET /api/v1/tenants/{tenantId}/handlers/mismatches</c>. Experimental surface (HXL002).</summary>
public sealed record HandlerMismatchReport
{
    [JsonPropertyName("tenantId")]
    public required string TenantId { get; init; }

    [JsonPropertyName("asOf")]
    public required string AsOf { get; init; }

    /// <summary>Observation-window width in hours (24h for 9.3 — hardcoded; see deferred-work
    /// <c>Story-9.3-ObservationWindowConfig</c>).</summary>
    [JsonPropertyName("windowHours")]
    public required int WindowHours { get; init; }

    [JsonPropertyName("mismatches")]
    public required IReadOnlyList<HandlerMismatch> Mismatches { get; init; }

    /// <summary>Story 9.3 Finding EE — positive-confirmation UX. Populated even when <see cref="Mismatches"/> is empty.</summary>
    [JsonPropertyName("summary")]
    public required HandlerMismatchReportSummary Summary { get; init; }

    /// <summary>Story 9.3 Finding L — computed property for automated monitors short-circuiting without
    /// enumerating <see cref="Mismatches"/>.</summary>
    [JsonIgnore]
    public bool HasWarnings => Mismatches.Any(m => m.Severity == HandlerMismatchSeverity.Warning);

    [JsonIgnore]
    public bool HasInfo => Mismatches.Any(m => m.Severity == HandlerMismatchSeverity.Info);
}

/// <summary>Story 9.3 — detector-reported summary metadata for positive-confirmation UX.</summary>
public sealed record HandlerMismatchReportSummary
{
    [JsonPropertyName("routesConfigured")]
    public required int RoutesConfigured { get; init; }

    [JsonPropertyName("observationsChecked")]
    public required int ObservationsChecked { get; init; }
}

/// <summary>Story 9.3 — individual mismatch row.</summary>
public sealed record HandlerMismatch
{
    [JsonPropertyName("category")]
    public required HandlerMismatchCategory Category { get; init; }

    [JsonPropertyName("severity")]
    public required HandlerMismatchSeverity Severity { get; init; }

    /// <summary>Identifier of the thing that's mismatched — an <c>eventType</c>, a <c>sourcePrefix</c>, or a stem.</summary>
    [JsonPropertyName("subject")]
    public required string Subject { get; init; }

    /// <summary>Free-form description of where the mismatch was detected (per-category template).</summary>
    [JsonPropertyName("context")]
    public required string Context { get; init; }

    /// <summary>Actionable operator next-step including a runbook URL (Story 9.3 Finding A).</summary>
    [JsonPropertyName("suggestion")]
    public required string Suggestion { get; init; }
}

/// <summary>Story 9.3 — mismatch category; camelCase-serialised.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<HandlerMismatchCategory>))]
public enum HandlerMismatchCategory
{
    /// <summary>An event was observed for which no routing-map entry matched.</summary>
    UnhandledEventType,

    /// <summary>A configured routing-map entry has received zero events in the window.</summary>
    StaleHandler,

    /// <summary>Two or more versions of the same event-name stem are observed concurrently.</summary>
    VersionMismatch,

    /// <summary>A configured route lacks an authoritative runtime projection binding.</summary>
    ProjectionBindingMissing,
}

/// <summary>Story 9.3 — mismatch severity; 2-valued (ADR-9.3-004 enum minimalism).</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<HandlerMismatchSeverity>))]
public enum HandlerMismatchSeverity
{
    /// <summary>Low-priority observation; never paging-worthy.</summary>
    Info,

    /// <summary>Operator should review; still not blocking ingestion.</summary>
    Warning,
}
