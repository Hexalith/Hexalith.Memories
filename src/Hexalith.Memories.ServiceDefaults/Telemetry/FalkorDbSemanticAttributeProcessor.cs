// <copyright file="FalkorDbSemanticAttributeProcessor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.ServiceDefaults.Telemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using OpenTelemetry;

/// <summary>
/// Story 8.5 ADR-8.5-001 (h) Path A — rewrites <c>db.system</c> / <c>db.system.name</c> tags on
/// Redis-protocol spans whose <c>server.address</c> (or <c>net.peer.name</c> fallback) resolves to
/// one of the configured FalkorDB hostnames. Without this rewrite, APM backends (Honeycomb,
/// Datadog, Grafana Tempo) misclassify FalkorDB graph queries as generic Redis commands because the upstream
/// <c>OpenTelemetry.Instrumentation.StackExchangeRedis</c> package cannot distinguish FalkorDB
/// from Redis at the wire-protocol level.
/// </summary>
/// <remarks>
/// Register INSIDE the <c>WithTracing</c> lambda via
/// <c>tracing.AddProcessor&lt;FalkorDbSemanticAttributeProcessor&gt;()</c>, placed AFTER both
/// <c>AddGuardedRedisInstrumentation</c> calls but BEFORE <c>AddOpenTelemetryExporters</c>. This
/// placement guarantees processor-vs-exporter order: the rewrite fires before any exporter sees
/// the activity, so downstream APM tooling observes the corrected tags.
/// </remarks>
public sealed class FalkorDbSemanticAttributeProcessor : BaseProcessor<Activity>
{
    /// <summary>
    /// Default FalkorDB resource hostname assigned by the Aspire AppHost. The processor rewrites
    /// any Redis-source activity whose <c>server.address</c> / <c>net.peer.name</c> matches this
    /// value to <c>db.system=falkordb</c>.
    /// </summary>
    public const string DefaultFalkorDbHostname = "falkordb";

    /// <summary>The OpenTelemetry semantic-convention tag for the database system (legacy).</summary>
    internal const string DbSystemTag = "db.system";

    /// <summary>The OpenTelemetry semantic-convention tag for the database system (new bank).</summary>
    internal const string DbSystemNameTag = "db.system.name";

    /// <summary>The FalkorDB semantic value shipped on the rewritten tags.</summary>
    internal const string FalkorDbSystemValue = "falkordb";

    /// <summary>StackExchange.Redis OTEL source name — activities from other sources are ignored.</summary>
    internal const string RedisSourceName = "OpenTelemetry.Instrumentation.StackExchangeRedis";

    private readonly HashSet<string> _falkorDbHostnames;

    /// <summary>Initializes a new instance using the default <see cref="DefaultFalkorDbHostname"/>.</summary>
    public FalkorDbSemanticAttributeProcessor()
        : this([DefaultFalkorDbHostname])
    {
    }

    /// <summary>Initializes a new instance with an explicit FalkorDB hostname for test overrides.</summary>
    /// <param name="falkorDbHostname">The FalkorDB resource hostname to match against
    /// <c>server.address</c> / <c>net.peer.name</c>. MUST NOT be null or empty.</param>
    public FalkorDbSemanticAttributeProcessor(string falkorDbHostname)
        : this([falkorDbHostname])
    {
    }

    /// <summary>Initializes a new instance with explicit FalkorDB hostnames / aliases.</summary>
    /// <param name="falkorDbHostnames">The FalkorDB hostnames that should be treated as graph
    /// backends when seen in <c>server.address</c> or <c>net.peer.name</c>.</param>
    public FalkorDbSemanticAttributeProcessor(IEnumerable<string> falkorDbHostnames)
    {
        ArgumentNullException.ThrowIfNull(falkorDbHostnames);

        _falkorDbHostnames = new(StringComparer.OrdinalIgnoreCase);
        foreach (string hostname in falkorDbHostnames)
        {
            if (!string.IsNullOrWhiteSpace(hostname))
            {
                _ = _falkorDbHostnames.Add(hostname.Trim());
            }
        }

        if (_falkorDbHostnames.Count == 0)
        {
            throw new ArgumentException(
                "At least one FalkorDB hostname must be provided.",
                nameof(falkorDbHostnames));
        }
    }

    /// <inheritdoc/>
    public override void OnEnd(Activity data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (!string.Equals(data.Source.Name, RedisSourceName, StringComparison.Ordinal))
        {
            return;
        }

        string? serverAddress = GetStringTag(data, "server.address")
            ?? GetStringTag(data, "net.peer.name");

        if (string.IsNullOrEmpty(serverAddress))
        {
            return;
        }

        // Review P17: some instrumentation flavours emit `server.address` with a port suffix
        // (e.g. "falkordb:6379"). Strip it before the allow-list comparison so the hostname-only
        // configured set still matches port-carrying addresses.
        string candidate = serverAddress.Trim();
        int portColon = candidate.LastIndexOf(':');
        if (portColon > 0 && portColon < candidate.Length - 1 && IsAllDigits(candidate.AsSpan(portColon + 1)))
        {
            candidate = candidate[..portColon];
        }

        if (!_falkorDbHostnames.Contains(candidate))
        {
            return;
        }

        // SetTag with an existing key replaces the prior value; both legacy and new tag banks
        // are updated so consumers on either semantic-conventions version see the same shape.
        _ = data.SetTag(DbSystemTag, FalkorDbSystemValue);
        _ = data.SetTag(DbSystemNameTag, FalkorDbSystemValue);
    }

    private static string? GetStringTag(Activity activity, string tagName)
        => activity.GetTagItem(tagName) as string;

    private static bool IsAllDigits(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return false;
        }

        foreach (char c in span)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
