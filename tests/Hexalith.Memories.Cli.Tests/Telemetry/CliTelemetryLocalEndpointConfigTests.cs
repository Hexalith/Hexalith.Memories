// <copyright file="CliTelemetryLocalEndpointConfigTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Telemetry;

using System;

using Hexalith.Memories.Cli.Execution;

using Shouldly;

/// <summary>spec-infrastructure-dependency-abstraction (F4, Decision D30) — the local development OTLP
/// fallback endpoint is config-sourced (env-overridable) with the built-in literal preserved as the
/// default. Uses an injected environment reader so no process-wide env var is mutated.</summary>
public sealed class CliTelemetryLocalEndpointConfigTests
{
    private static void NoWarn(string message)
    {
    }

    [Fact]
    public void ResolveEndpoint_LocalFallback_WithoutOverride_UsesBuiltInLiteral()
    {
        Uri? endpoint = CliTelemetryBootstrap.ResolveEndpoint(
            endpoint: null, telemetryFlag: true, warn: NoWarn, readEnvironment: _ => null);

        endpoint.ShouldBe(new Uri(CliTelemetryBootstrap.LocalDevelopmentOtlpEndpoint, UriKind.Absolute));
    }

    [Fact]
    public void ResolveEndpoint_LocalFallback_WithConfiguredOverride_UsesOverride()
    {
        Uri? endpoint = CliTelemetryBootstrap.ResolveEndpoint(
            endpoint: null,
            telemetryFlag: true,
            warn: NoWarn,
            readEnvironment: name =>
                name == CliTelemetryBootstrap.LocalDevelopmentOtlpEndpointEnvVar ? "http://localhost:4321" : null);

        endpoint.ShouldBe(new Uri("http://localhost:4321", UriKind.Absolute));
    }

    [Fact]
    public void ResolveEndpoint_LocalFallback_WithInvalidOverride_FallsBackToLiteralAndWarns()
    {
        List<string> warnings = [];
        Uri? endpoint = CliTelemetryBootstrap.ResolveEndpoint(
            endpoint: null,
            telemetryFlag: true,
            warn: warnings.Add,
            readEnvironment: name =>
                name == CliTelemetryBootstrap.LocalDevelopmentOtlpEndpointEnvVar ? "not a uri" : null);

        endpoint.ShouldBe(new Uri(CliTelemetryBootstrap.LocalDevelopmentOtlpEndpoint, UriKind.Absolute));
        warnings.ShouldContain(w =>
            w.Contains(CliTelemetryBootstrap.LocalDevelopmentOtlpEndpointEnvVar, StringComparison.Ordinal));
    }
}
