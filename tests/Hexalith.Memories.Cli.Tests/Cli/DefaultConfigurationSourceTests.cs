// <copyright file="DefaultConfigurationSourceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System;

using Hexalith.Memories.Cli.Configuration;

using Shouldly;

/// <summary>spec-infrastructure-dependency-abstraction (F3, Decision D30) — the tier-4 default endpoint is
/// config-sourced (env-overridable) with the built-in literal preserved as the fallback.</summary>
public sealed class DefaultConfigurationSourceTests
{
    [Fact]
    public void TryResolve_WithoutOverride_UsesBuiltInDefault()
    {
        DefaultConfigurationSource source = new(_ => null);

        source.TryResolve(out Uri? endpoint, out string? apiToken).ShouldBeTrue();

        endpoint.ShouldBe(DefaultConfigurationSource.DefaultEndpoint);
        endpoint.ShouldBe(new Uri("http://127.0.0.1:5000/"));
        apiToken.ShouldBeNull();
    }

    [Fact]
    public void TryResolve_WithConfiguredOverride_UsesOverride()
    {
        DefaultConfigurationSource source = new(name =>
            name == DefaultConfigurationSource.DefaultEndpointVariableName ? "https://memories.internal:9443/" : null);

        source.TryResolve(out Uri? endpoint, out _).ShouldBeTrue();

        endpoint.ShouldBe(new Uri("https://memories.internal:9443/"));
    }

    [Fact]
    public void TryResolve_WithInvalidOverride_FallsBackToBuiltInDefault()
    {
        DefaultConfigurationSource source = new(name =>
            name == DefaultConfigurationSource.DefaultEndpointVariableName ? "not a uri" : null);

        source.TryResolve(out Uri? endpoint, out _).ShouldBeTrue();

        endpoint.ShouldBe(DefaultConfigurationSource.DefaultEndpoint);
    }
}
