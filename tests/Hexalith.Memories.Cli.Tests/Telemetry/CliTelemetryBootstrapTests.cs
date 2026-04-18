// <copyright file="CliTelemetryBootstrapTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Telemetry;

using System;
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
