// <copyright file="CliTelemetryBootstrap.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Execution;

using System;
using System.Linq;

using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Story 7.5 — opt-in CLI telemetry bootstrap. Wires the OpenTelemetry SDK with HttpClient
/// instrumentation + OTLP exporter ONLY when the env var <c>HEXALITH_MEMORIES_OTEL_ENDPOINT</c> OR the
/// global <c>--telemetry</c> flag is set. ADR-7.5-005: CLI telemetry is opt-in; server telemetry is
/// always-on.
/// </summary>
public static class CliTelemetryBootstrap
{
    /// <summary>Env var name that enables CLI telemetry and supplies the OTLP endpoint.</summary>
    public const string OtlpEndpointEnvVar = "HEXALITH_MEMORIES_OTEL_ENDPOINT";

    /// <summary>Local Aspire dashboard OTLP endpoint used when <c>--telemetry</c> is passed without an explicit env var.</summary>
    public const string LocalDevelopmentOtlpEndpoint = "http://localhost:18889";

    /// <summary>Marker service type used to detect "already registered" on idempotency checks.</summary>
    private sealed class TelemetryRegisteredMarker
    {
    }

    /// <summary>
    /// Registers the OpenTelemetry SDK on the given collection when telemetry is enabled.
    /// Idempotent: subsequent calls are no-ops once the marker is registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="telemetryFlag">Value of the <c>--telemetry</c> global flag.</param>
    /// <returns><c>true</c> if telemetry was registered on this call; <c>false</c> if disabled or already registered.</returns>
    public static bool TryRegister(IServiceCollection services, bool telemetryFlag)
    {
        ArgumentNullException.ThrowIfNull(services);

        string? endpoint = Environment.GetEnvironmentVariable(OtlpEndpointEnvVar);
        Uri? otlpEndpoint = ResolveEndpoint(endpoint, telemetryFlag);
        if (otlpEndpoint is null)
        {
            return false;
        }

        // Idempotency guard: if already registered, skip.
        if (services.Any(sd => sd.ServiceType == typeof(TelemetryRegisteredMarker)
            || sd.ServiceType == typeof(TracerProvider)
            || (sd.ImplementationType is not null && typeof(TracerProvider).IsAssignableFrom(sd.ImplementationType))))
        {
            return false;
        }

        services.AddSingleton<TelemetryRegisteredMarker>();

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("Hexalith.Memories.Cli"))
            .WithTracing(tracing =>
            {
                tracing.AddSource(MemoriesActivitySource.SourceName);
                tracing.AddHttpClientInstrumentation();
                tracing.AddOtlpExporter(o => o.Endpoint = otlpEndpoint);
            });

        return true;
    }

    internal static Uri? ResolveEndpoint(string? endpoint, bool telemetryFlag)
    {
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            return new Uri(endpoint, UriKind.Absolute);
        }

        return telemetryFlag ? new Uri(LocalDevelopmentOtlpEndpoint, UriKind.Absolute) : null;
    }
}
