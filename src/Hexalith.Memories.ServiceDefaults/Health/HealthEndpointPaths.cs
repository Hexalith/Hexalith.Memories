// <copyright file="HealthEndpointPaths.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.ServiceDefaults.Health;

/// <summary>Single source of truth for the three health-probe paths. Both the endpoint
/// mapping (see <see cref="Extensions.MapDefaultEndpoints"/>) and the trace-exclusion
/// filter (see <see cref="Extensions.ShouldTraceHttpRequest"/>) MUST reference these
/// constants — renaming the physical path then becomes a one-line change, and the
/// Story 7.5 trace-exclusion invariant cannot silently drift from the endpoint routes.</summary>
public static class HealthEndpointPaths
{
    /// <summary>Aggregate endpoint that surfaces every registered health check.</summary>
    public const string Health = "/health";

    /// <summary>Liveness endpoint — runs only checks tagged <c>"live"</c>.</summary>
    public const string Alive = "/alive";

    /// <summary>Readiness endpoint — runs only checks tagged <c>"ready"</c>.</summary>
    public const string Ready = "/ready";
}
