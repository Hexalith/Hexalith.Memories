// <copyright file="BackendHealthResponseWriter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.ServiceDefaults.Health;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>Serializes a <see cref="HealthReport"/> to the Story 8.1 V1 JSON schema
/// (camelCase; <c>schemaVersion: 1</c>; per-entry <c>affectedCapabilities</c>). Installed
/// via <c>HealthCheckOptions.ResponseWriter</c> on each of the <c>/health</c>, <c>/alive</c>,
/// and <c>/ready</c> endpoints so probes observe an operator-friendly, stable schema in
/// place of ASP.NET Core's default plain-text writer.</summary>
public static class BackendHealthResponseWriter
{
    /// <summary>V1 payload schema version. Additive field changes keep this at <c>1</c>;
    /// rename / removal bumps the value and requires a migration note in
    /// <c>docs/dev/health-checks.md</c>.</summary>
    public const int SchemaVersion = 1;

    /// <summary>Writes <paramref name="report"/> as UTF-8 JSON on
    /// <paramref name="context"/>'s response body, using the shared
    /// <see cref="MemoriesJsonContext.Options"/> (camelCase + combined resolver).</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="report">The aggregated health report.</param>
    /// <returns>A task that completes once the body is flushed.</returns>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            schemaVersion = SchemaVersion,
            status = report.Status.ToString(),
            totalDurationMs = (int)report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                static kv => kv.Key,
                static kv => new
                {
                    status = kv.Value.Status.ToString(),
                    description = kv.Value.Description ?? string.Empty,
                    durationMs = (int)kv.Value.Duration.TotalMilliseconds,
                    affectedCapabilities = kv.Value.Status == HealthStatus.Healthy
                        ? Array.Empty<string>()
                        : BackendCapabilityCatalog.GetCapabilities(kv.Key).ToArray(),
                }),
        };

        string json = JsonSerializer.Serialize(payload, MemoriesJsonContext.Options);
        return context.Response.WriteAsync(json);
    }
}
