// <copyright file="CliTelemetryBootstrapTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Telemetry;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Memories.Cli.Execution;

using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Trace;

using Shouldly;

/// <summary>Story 7.5 Task 10.1 — asserts opt-in gating + idempotency of CLI telemetry bootstrap.</summary>
public sealed class CliTelemetryBootstrapTests
{
    [Fact]
    public void ResolveEndpoint_FlagTrue_EnvVarUnset_UsesLocalDevelopmentEndpoint()
    {
        Uri? endpoint = CliTelemetryBootstrap.ResolveEndpoint(endpoint: null, telemetryFlag: true);

        endpoint.ShouldNotBeNull();
        endpoint.ShouldBe(new Uri(CliTelemetryBootstrap.LocalDevelopmentOtlpEndpoint, UriKind.Absolute));
    }

    [Fact]
    public void ResolveEndpoint_EnvVarSet_PrefersEnvVar()
    {
        Uri? endpoint = CliTelemetryBootstrap.ResolveEndpoint("https://collector.internal:4318", telemetryFlag: true);

        endpoint.ShouldNotBeNull();
        endpoint.ShouldBe(new Uri("https://collector.internal:4318", UriKind.Absolute));
    }

    [Fact]
    public void ResolveEndpoint_InvalidEnvVar_FlagFalse_DisablesTelemetry()
    {
        Uri? endpoint = CliTelemetryBootstrap.ResolveEndpoint("not a valid uri", telemetryFlag: false);

        endpoint.ShouldBeNull();
    }

    [Fact]
    public void ResolveEndpoint_InvalidEnvVar_FlagTrue_FallsBackToLocalDevelopmentEndpoint()
    {
        Uri? endpoint = CliTelemetryBootstrap.ResolveEndpoint("not a valid uri", telemetryFlag: true);

        endpoint.ShouldNotBeNull();
        endpoint.ShouldBe(new Uri(CliTelemetryBootstrap.LocalDevelopmentOtlpEndpoint, UriKind.Absolute));
    }

    [Theory]
    [InlineData("file:///tmp/x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("mailto:ops@example.com")]
    [InlineData("ftp://collector.internal")]
    public void ResolveEndpoint_NonHttpAbsoluteUri_FlagFalse_DisablesTelemetry(string value)
    {
        Uri? endpoint = CliTelemetryBootstrap.ResolveEndpoint(value, telemetryFlag: false);

        endpoint.ShouldBeNull();
    }

    [Theory]
    [InlineData("file:///tmp/x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("mailto:ops@example.com")]
    [InlineData("ftp://collector.internal")]
    public void ResolveEndpoint_NonHttpAbsoluteUri_FlagTrue_FallsBackToLocalDevelopmentEndpoint(string value)
    {
        Uri? endpoint = CliTelemetryBootstrap.ResolveEndpoint(value, telemetryFlag: true);

        endpoint.ShouldNotBeNull();
        endpoint.ShouldBe(new Uri(CliTelemetryBootstrap.LocalDevelopmentOtlpEndpoint, UriKind.Absolute));
    }

    [Fact]
    public void ResolveEndpoint_InvalidEnvVar_EmitsWarning()
    {
        List<string> warnings = [];

        Uri? endpoint = CliTelemetryBootstrap.ResolveEndpoint(
            "not a valid uri",
            telemetryFlag: true,
            warn: warnings.Add);

        endpoint.ShouldNotBeNull();
        warnings.ShouldHaveSingleItem();
        warnings[0].ShouldContain(CliTelemetryBootstrap.OtlpEndpointEnvVar);
        warnings[0].ShouldContain("not a valid uri");
    }

    [Fact]
    public void ResolveEndpoint_NonHttpScheme_EmitsWarning_EvenWithFlagFalse()
    {
        List<string> warnings = [];

        Uri? endpoint = CliTelemetryBootstrap.ResolveEndpoint(
            "file:///tmp/x",
            telemetryFlag: false,
            warn: warnings.Add);

        endpoint.ShouldBeNull();
        warnings.ShouldHaveSingleItem();
        warnings[0].ShouldContain("file:///tmp/x");
        warnings[0].ShouldContain("Telemetry disabled");
    }

    [Fact]
    public void ResolveEndpoint_ValidHttpsUri_DoesNotWarn()
    {
        List<string> warnings = [];

        Uri? endpoint = CliTelemetryBootstrap.ResolveEndpoint(
            "https://collector.internal:4318",
            telemetryFlag: false,
            warn: warnings.Add);

        endpoint.ShouldNotBeNull();
        warnings.ShouldBeEmpty();
    }

    [Fact]
    public void TryRegister_FlagFalse_EnvVarUnset_DoesNothing()
    {
        // Isolate from ambient env var.
        string? previous = Environment.GetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar, null);
            var services = new ServiceCollection();
            bool registered = CliTelemetryBootstrap.TryRegister(services, telemetryFlag: false);
            registered.ShouldBeFalse();
            services.Any(s => s.ServiceType == typeof(TracerProvider)).ShouldBeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar, previous);
        }
    }

    [Fact]
    public void TryRegister_FlagTrue_RegistersTracer()
    {
        string? previous = Environment.GetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar, null);
            var services = new ServiceCollection();
            bool registered = CliTelemetryBootstrap.TryRegister(services, telemetryFlag: true);
            registered.ShouldBeTrue();
            using ServiceProvider provider = services.BuildServiceProvider();
            provider.GetService<TracerProvider>().ShouldNotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar, previous);
        }
    }

    [Fact]
    public void TryRegister_EnvVarSet_RegistersTracer()
    {
        string? previous = Environment.GetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar, "http://localhost:18889");
            var services = new ServiceCollection();
            bool registered = CliTelemetryBootstrap.TryRegister(services, telemetryFlag: false);
            registered.ShouldBeTrue();
            using ServiceProvider provider = services.BuildServiceProvider();
            provider.GetService<TracerProvider>().ShouldNotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar, previous);
        }
    }

    [Fact]
    public void TryRegister_CalledTwice_IsIdempotent()
    {
        string? previous = Environment.GetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar, null);
            var services = new ServiceCollection();
            bool first = CliTelemetryBootstrap.TryRegister(services, telemetryFlag: true);
            bool second = CliTelemetryBootstrap.TryRegister(services, telemetryFlag: true);
            first.ShouldBeTrue();
            second.ShouldBeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar, previous);
        }
    }

    [Fact]
    public void TryRegister_PreRegisteredTracerProvider_IsNoOp()
    {
        string? previous = Environment.GetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar, null);
            var services = new ServiceCollection();
            using TracerProvider existing = Sdk.CreateTracerProviderBuilder().Build();
            services.AddSingleton(existing);

            bool registered = CliTelemetryBootstrap.TryRegister(services, telemetryFlag: true);

            registered.ShouldBeFalse();
            services.Count(s => s.ServiceType == typeof(TracerProvider)).ShouldBe(1);
        }
        finally
        {
            Environment.SetEnvironmentVariable(CliTelemetryBootstrap.OtlpEndpointEnvVar, previous);
        }
    }
}
