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
    /// <remarks>spec-infrastructure-dependency-abstraction (F4, Decision D30): the local dev fallback is
    /// config-sourced from <see cref="LocalDevelopmentOtlpEndpointEnvVar"/>; the literal remains only as the
    /// documented, overridable default (identical effective value when unset).</remarks>
    public const string LocalDevelopmentOtlpEndpoint = "http://localhost:18889";

    /// <summary>Env var that overrides the local development OTLP fallback endpoint.</summary>
    public const string LocalDevelopmentOtlpEndpointEnvVar = "HEXALITH_MEMORIES_OTEL_LOCAL_ENDPOINT";

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
        => ResolveEndpoint(endpoint, telemetryFlag, WriteStderr, Environment.GetEnvironmentVariable);

    internal static Uri? ResolveEndpoint(string? endpoint, bool telemetryFlag, Action<string> warn)
        => ResolveEndpoint(endpoint, telemetryFlag, warn, Environment.GetEnvironmentVariable);

    internal static Uri? ResolveEndpoint(string? endpoint, bool telemetryFlag, Action<string> warn, Func<string, string?> readEnvironment)
    {
        ArgumentNullException.ThrowIfNull(warn);
        ArgumentNullException.ThrowIfNull(readEnvironment);

        Uri localEndpoint = ResolveLocalDevelopmentEndpoint(readEnvironment, warn);

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? parsedEndpoint)
                && IsAcceptableOtlpScheme(parsedEndpoint))
            {
                return parsedEndpoint;
            }

            warn(
                $"[Hexalith.Memories.Cli] Ignoring {OtlpEndpointEnvVar}='{endpoint}': " +
                "value is not an http(s) absolute URI. " +
                (telemetryFlag
                    ? $"Falling back to {localEndpoint}."
                    : "Telemetry disabled."));

            return telemetryFlag ? localEndpoint : null;
        }

        return telemetryFlag ? localEndpoint : null;
    }

    // spec-infrastructure-dependency-abstraction (F4, Decision D30; review P9): the local dev OTLP
    // fallback is config-sourced from an env var; invalid overrides warn (matching the primary OTLP var)
    // and fall back to the documented literal default.
    private static Uri ResolveLocalDevelopmentEndpoint(Func<string, string?> readEnvironment, Action<string> warn)
    {
        string? configured = readEnvironment(LocalDevelopmentOtlpEndpointEnvVar);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return new Uri(LocalDevelopmentOtlpEndpoint, UriKind.Absolute);
        }

        string trimmed = configured.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? parsed)
            && IsAcceptableOtlpScheme(parsed))
        {
            return parsed;
        }

        warn(
            $"[Hexalith.Memories.Cli] Ignoring {LocalDevelopmentOtlpEndpointEnvVar}='{trimmed}': " +
            $"value is not an http(s) absolute URI. Falling back to {LocalDevelopmentOtlpEndpoint}.");
        return new Uri(LocalDevelopmentOtlpEndpoint, UriKind.Absolute);
    }

    private static bool IsAcceptableOtlpScheme(Uri uri)
        => uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static void WriteStderr(string message) => Console.Error.WriteLine(message);
}
