// <copyright file="DaprTokenStartupValidatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using Hexalith.Memories.ServiceDefaults;
using Hexalith.Memories.ServiceDefaults.Security;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using Shouldly;

/// <summary>
/// Story 26.1 — guards the production fail-closed startup gate: an instance must refuse to start when
/// either DAPR authentication token is absent in Production, and must stay permissive elsewhere.
/// Epic 27 retro B1 — Production must also refuse to start when the test-host application-token
/// override is configured, because that override would otherwise disable the token check.
/// Shares a collection with the middleware tests so concurrent APP_API_TOKEN mutation cannot race.
/// </summary>
[Collection(DaprTokenEnvironmentCollection.Name)]
public sealed class DaprTokenStartupValidatorTests
{
    private static readonly IConfiguration EmptyConfiguration = new ConfigurationBuilder().Build();

    [Fact]
    public async Task StartAsync_InProduction_WithMissingTokens_Throws()
    {
        using TokenEnvironment _ = new(appToken: null, daprToken: null);
        var validator = new DaprTokenStartupValidator(HostEnvironment("Production"), EmptyConfiguration);

        await Should.ThrowAsync<InvalidOperationException>(
            () => validator.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartAsync_InProduction_WithBothTokens_DoesNotThrow()
    {
        using TokenEnvironment _ = new(appToken: "app-token", daprToken: "dapr-token");
        var validator = new DaprTokenStartupValidator(HostEnvironment("Production"), EmptyConfiguration);

        await Should.NotThrowAsync(
            () => validator.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartAsync_OutsideProduction_WithMissingTokens_DoesNotThrow()
    {
        using TokenEnvironment _ = new(appToken: null, daprToken: null);
        var validator = new DaprTokenStartupValidator(HostEnvironment("Development"), EmptyConfiguration);

        await Should.NotThrowAsync(
            () => validator.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartAsync_InProductionHost_WithBothTokensAndEmptyHostConfigurationOverride_Throws()
    {
        // Same host shape as Server and MCP: WebApplicationBuilder plus AddServiceDefaults, which
        // registers the validator. WebHost.UseSetting writes host configuration directly, so the
        // empty value stays visible (a generic HostBuilder chains host configuration and hides it).
        using TokenEnvironment _ = new(appToken: "app-token", daprToken: "dapr-token");
        WebApplicationBuilder builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.WebHost.UseTestServer();
        builder.WebHost.UseSetting(DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey, string.Empty);
        builder.AddServiceDefaults(configureRedisInstrumentation: false);
        await using WebApplication app = builder.Build();

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => app.StartAsync(TestContext.Current.CancellationToken));

        exception.Message.ShouldContain(DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey);
        exception.Message.ShouldContain("Hexalith__Dapr__ApplicationTokenOverride");
    }

    [Fact]
    public async Task StartAsync_InProduction_WithBothTokensAndNonEmptyOverride_ThrowsWithoutEchoingValue()
    {
        const string OverrideValue = "override-token-value";
        using TokenEnvironment _ = new(appToken: "app-token", daprToken: "dapr-token");
        var validator = new DaprTokenStartupValidator(
            HostEnvironment("Production"),
            Configuration(DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey, OverrideValue));

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => validator.StartAsync(TestContext.Current.CancellationToken));

        exception.Message.ShouldContain(DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey);
        exception.Message.ShouldNotContain(OverrideValue);
    }

    [Theory]
    [InlineData(DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey, null)]
    [InlineData(DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey + ":Child", "child-value")]
    public async Task StartAsync_InProduction_WithNullOverrideUnderPresentSection_DoesNotThrow(string key, string? value)
    {
        using TokenEnvironment _ = new(appToken: "app-token", daprToken: "dapr-token");
        var validator = new DaprTokenStartupValidator(HostEnvironment("Production"), Configuration(key, value));

        await Should.NotThrowAsync(
            () => validator.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartAsync_OutsideProduction_WithEmptyOverride_DoesNotThrow()
    {
        using TokenEnvironment _ = new(appToken: null, daprToken: null);
        var validator = new DaprTokenStartupValidator(
            HostEnvironment("Development"),
            Configuration(DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey, string.Empty));

        await Should.NotThrowAsync(
            () => validator.StartAsync(TestContext.Current.CancellationToken));
    }

    private static IConfiguration Configuration(string key, string? value)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hexalith:Dapr:Unrelated"] = "present",
                [key] = value,
            })
            .Build();

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
