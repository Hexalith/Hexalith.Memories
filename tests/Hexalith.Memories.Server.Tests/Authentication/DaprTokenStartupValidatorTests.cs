// <copyright file="DaprTokenStartupValidatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using Hexalith.Memories.ServiceDefaults.Security;

using Microsoft.Extensions.Hosting;

using NSubstitute;

using Shouldly;

/// <summary>
/// Story 26.1 — guards the production fail-closed startup gate: an instance must refuse to start when
/// either DAPR authentication token is absent in Production, and must stay permissive elsewhere.
/// Shares a collection with the middleware tests so concurrent APP_API_TOKEN mutation cannot race.
/// </summary>
[Collection("DaprTokenEnvironment")]
public sealed class DaprTokenStartupValidatorTests
{
    [Fact]
    public async Task StartAsync_InProduction_WithMissingTokens_Throws()
    {
        using TokenEnvironment _ = new(appToken: null, daprToken: null);
        var validator = new DaprTokenStartupValidator(HostEnvironment("Production"));

        await Should.ThrowAsync<InvalidOperationException>(
            () => validator.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartAsync_InProduction_WithBothTokens_DoesNotThrow()
    {
        using TokenEnvironment _ = new(appToken: "app-token", daprToken: "dapr-token");
        var validator = new DaprTokenStartupValidator(HostEnvironment("Production"));

        await Should.NotThrowAsync(
            () => validator.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartAsync_OutsideProduction_WithMissingTokens_DoesNotThrow()
    {
        using TokenEnvironment _ = new(appToken: null, daprToken: null);
        var validator = new DaprTokenStartupValidator(HostEnvironment("Development"));

        await Should.NotThrowAsync(
            () => validator.StartAsync(TestContext.Current.CancellationToken));
    }

    private static IHostEnvironment HostEnvironment(string environmentName)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName = environmentName;
        return environment;
    }

    private sealed class TokenEnvironment : IDisposable
    {
        private readonly string? _originalApp;
        private readonly string? _originalDapr;

        public TokenEnvironment(string? appToken, string? daprToken)
        {
            _originalApp = Environment.GetEnvironmentVariable("APP_API_TOKEN");
            _originalDapr = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
            Environment.SetEnvironmentVariable("APP_API_TOKEN", appToken);
            Environment.SetEnvironmentVariable("DAPR_API_TOKEN", daprToken);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("APP_API_TOKEN", _originalApp);
            Environment.SetEnvironmentVariable("DAPR_API_TOKEN", _originalDapr);
        }
    }
}
