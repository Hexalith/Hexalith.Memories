// <copyright file="DaprApplicationTokenMiddlewareTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using Hexalith.Memories.ServiceDefaults.Security;

using Microsoft.AspNetCore.Http;

using Shouldly;

/// <summary>Tests the sidecar-to-application DAPR token boundary.</summary>
[Collection("DaprTokenEnvironment")]
public sealed class DaprApplicationTokenMiddlewareTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    [InlineData("/ready")]
    public async Task PodProbe_WithoutToken_ReachesEndpoint(string path)
    {
        using EnvironmentScope _ = new("expected-app-token");
        bool reached = false;
        var middleware = new DaprApplicationTokenMiddleware(_ =>
        {
            reached = true;
            return Task.CompletedTask;
        });
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
        var middleware = new DaprApplicationTokenMiddleware(_ =>
        {
            reached = true;
            return Task.CompletedTask;
        });
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
        var middleware = new DaprApplicationTokenMiddleware(_ =>
        {
            reached = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/search";
        context.Request.Headers[DaprApplicationTokenMiddleware.DaprApiTokenHeader] = Token;

        await middleware.InvokeAsync(context);

        reached.ShouldBeTrue();
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
