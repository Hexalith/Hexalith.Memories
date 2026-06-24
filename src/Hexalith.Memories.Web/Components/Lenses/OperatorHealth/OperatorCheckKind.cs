// <copyright file="OperatorCheckKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.OperatorHealth;

/// <summary>
/// Operator health check derived from a named Evidence Packet field.
/// </summary>
/// <remarks>
/// Story 17.4 — operator health is contract/state display only. This lens adds no live infrastructure
/// probes and no provider-specific health categories; checks the canonical contract does not expose
/// (queue/backlog, rate-limit, consistency repair, last-checked time) are recorded as deferred gaps and
/// render an unavailable boundary rather than an invented status.
/// </remarks>
public enum OperatorCheckKind
{
    /// <summary>Tenant isolation and authorization status (Scope.IsolationStatus).</summary>
    TenantIsolation = 0,

    /// <summary>Retrieval backend health (Evidence.Degraded / AllEnabledAxesUnavailable / OmittedDetails.Reason).</summary>
    RetrievalBackend,

    /// <summary>Retrieval axis availability (Evidence.UnavailableAxes).</summary>
    AxisAvailability,

    /// <summary>Graph context availability (Graph.Available / GapMarkers).</summary>
    GraphContext,

    /// <summary>Authorization of the requested scope (State / OmittedDetails.Reason).</summary>
    Authorization,

    /// <summary>Detail completeness under compression or redaction (OmittedDetails.Reason).</summary>
    DetailCompleteness,
}
