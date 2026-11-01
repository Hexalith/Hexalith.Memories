// <copyright file="DaprApplicationTokenMiddlewareTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using Hexalith.Memories.ServiceDefaults.Security;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using Shouldly;

/// <summary>Tests the sidecar-to-application DAPR token boundary.</summary>
[Collection(DaprTokenEnvironmentCollection.Name)]
public sealed class DaprApplicationTokenMiddlewareTests
{
    private static readonly IConfiguration EmptyHostConfiguration = new ConfigurationBuilder().Build();

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    [InlineData("/ready")]
    public async Task PodProbe_WithoutToken_ReachesEndpoint(string path)
    {
        using EnvironmentScope _ = new("expected-app-token");
        bool reached = false;
        var middleware = new DaprApplicationTokenMiddleware(
            _ =>
            {
                reached = true;
                return Task.CompletedTask;
            },
            EmptyHostConfiguration,
            HostEnvironment(Environments.Production));
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        await middleware.InvokeAsync(context);

        reached.ShouldBeTrue();
    }

    [Fact]
    public async Task DaprHealthOperation_WithoutToken_IsRejected()
    {
        using EnvironmentScope _ = new("expected-app-token");
        bool reached = false;
        var middleware = new DaprApplicationTokenMiddleware(
            _ =>
            {
                reached = true;
                return Task.CompletedTask;
            },
            EmptyHostConfiguration,
            HostEnvironment(Environments.Production));
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/health";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        reached.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ApiOperation_WithMatchingToken_ReachesEndpoint()
    {
        const string Token = "expected-app-token";
        using EnvironmentScope _ = new(Token);
        bool reached = false;
        var middleware = new DaprApplicationTokenMiddleware(
            _ =>
            {
                reached = true;
                return Task.CompletedTask;
            },
            EmptyHostConfiguration,
            HostEnvironment(Environments.Production));
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/search";
        context.Request.Headers[DaprApplicationTokenMiddleware.DaprApiTokenHeader] = Token;

        await middleware.InvokeAsync(context);

        reached.ShouldBeTrue();
    }

    [Fact]
    public async Task HostConfiguration_EmptyAppApiToken_IgnoresProcessEnvironment()
    {
        using EnvironmentScope _ = new("leaked-process-token");
        bool reached = false;
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey] = string.Empty,
            })
            .Build();
        var middleware = new DaprApplicationTokenMiddleware(
            _ =>
            {
                reached = true;
                return Task.CompletedTask;
            },
            configuration,
            HostEnvironment(Environments.Development));
        DefaultHttpContext context = new();
        context.Request.Path = "/dapr/subscribe";

        await middleware.InvokeAsync(context);

        reached.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ProductionHost_EmptyOverride_RequestWithoutToken_IsRejected()
    {
        using EnvironmentScope _ = new("expected-app-token");
        bool reached = false;
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey] = string.Empty,
            })
            .Build();
        var middleware = new DaprApplicationTokenMiddleware(
            _ =>
            {
                reached = true;
                return Task.CompletedTask;
            },
            configuration,
            HostEnvironment(Environments.Production));
        DefaultHttpContext context = new();
        context.Request.Path = "/dapr/subscribe";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        reached.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Theory]
    [InlineData("override-token", false)]
    [InlineData("expected-app-token", true)]
    public async Task ProductionHost_NonEmptyOverride_DoesNotReplaceProcessToken(string suppliedToken, bool expectedReached)
    {
        using EnvironmentScope _ = new("expected-app-token");
        bool reached = false;
        var middleware = new DaprApplicationTokenMiddleware(
            _ =>
            {
                reached = true;
                return Task.CompletedTask;
            },
            Configuration(DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey, "override-token"),
            HostEnvironment(Environments.Production));
        DefaultHttpContext context = new();
        context.Request.Path = "/dapr/subscribe";
        context.Request.Headers[DaprApplicationTokenMiddleware.DaprApiTokenHeader] = suppliedToken;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        reached.ShouldBe(expectedReached);
        context.Response.StatusCode.ShouldBe(
            expectedReached ? StatusCodes.Status200OK : StatusCodes.Status401Unauthorized);
    }

    [Theory]
    [InlineData("override-token", true)]
    [InlineData("process-token", false)]
    public async Task HostConfiguration_NonEmptyOverride_ReplacesProcessToken(string suppliedToken, bool expectedReached)
    {
        using EnvironmentScope _ = new("process-token");
        bool reached = false;
        var middleware = new DaprApplicationTokenMiddleware(
            _ =>
            {
                reached = true;
                return Task.CompletedTask;
            },
            Configuration(DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey, "override-token"),
            HostEnvironment(Environments.Development));
        DefaultHttpContext context = new();
        context.Request.Path = "/dapr/subscribe";
        context.Request.Headers[DaprApplicationTokenMiddleware.DaprApiTokenHeader] = suppliedToken;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        reached.ShouldBe(expectedReached);
        context.Response.StatusCode.ShouldBe(
            expectedReached ? StatusCodes.Status200OK : StatusCodes.Status401Unauthorized);
    }

    [Theory]
    [InlineData(DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey, null)]
    [InlineData(DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey + ":Child", "child-value")]
    public async Task HostConfiguration_NullOverrideUnderPresentSection_EnforcesProcessToken(string key, string? value)
    {
        using EnvironmentScope _ = new("expected-app-token");
        bool reached = false;
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hexalith:Dapr:Unrelated"] = "present",
                [key] = value,
            })
            .Build();
        var middleware = new DaprApplicationTokenMiddleware(
            _ =>
            {
                reached = true;
                return Task.CompletedTask;
            },
            configuration,
            HostEnvironment(Environments.Development));
        DefaultHttpContext context = new();
        context.Request.Path = "/dapr/subscribe";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        reached.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task HostConfiguration_OverrideAddedAfterConstruction_IsNotHonored()
    {
        using EnvironmentScope _ = new("expected-app-token");
        bool reached = false;
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();
        var middleware = new DaprApplicationTokenMiddleware(
            _ =>
            {
                reached = true;
                return Task.CompletedTask;
            },
            configuration,
            HostEnvironment(Environments.Development));
        configuration[DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey] = string.Empty;
        DefaultHttpContext context = new();
        context.Request.Path = "/dapr/subscribe";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        reached.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    private static IConfiguration Configuration(string key, string? value)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })
            .Build();

    private static IHostEnvironment HostEnvironment(string environmentName)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName = environmentName;
        return environment;
    }

    private sealed class EnvironmentScope : IDisposable
    {
        private readonly string? _original;

        public EnvironmentScope(string value)
        {
            _original = Environment.GetEnvironmentVariable(
                DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable);
            Environment.SetEnvironmentVariable(
                DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable,
                value);
        }

        public void Dispose()
            => Environment.SetEnvironmentVariable(
                DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable,
                _original);
    }
}
