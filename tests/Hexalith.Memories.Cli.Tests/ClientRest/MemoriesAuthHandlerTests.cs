// <copyright file="MemoriesAuthHandlerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.ClientRest;

using System.Net;

using Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public class MemoriesAuthHandlerTests
{
    private const string IngressEndpoint = "https://ingress.example.com/";
    private const string LocalhostEndpoint = "http://127.0.0.1:5000/";
    private const string TokenValue = "t";
    private static readonly string DockerServiceEndpoint = $"{Uri.UriSchemeHttp}://memories-server:5000/";
    public static object?[][] NoTokenEndpoints =>
    [
        [null, IngressEndpoint],
        [null, LocalhostEndpoint],
        [null, DockerServiceEndpoint],
    ];

    public static object?[][] HeaderInvariantEndpoints =>
    [
        [null, IngressEndpoint],
        [null, LocalhostEndpoint],
        [null, DockerServiceEndpoint],
        [TokenValue, IngressEndpoint],
        [TokenValue, LocalhostEndpoint],
    ];

    // Row 1: no token + ingress → zero auth headers.
    [Theory]
    [MemberData(nameof(NoTokenEndpoints))]
    public async Task SendAsync_WithoutToken_AttachesNoAuthHeaders(string? token, string endpoint)
    {
        // Arrange
        (MemoriesAuthHandler authHandler, TestDelegatingHandler innerHandler) = BuildPipeline(token);
        using var client = new HttpClient(authHandler) { BaseAddress = new Uri(endpoint) };

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/tenants");
        using HttpResponseMessage response = await client.SendAsync(request);

        // Assert
        HttpRequestMessage captured = innerHandler.Requests.ShouldHaveSingleItem();
        captured.Headers.Authorization.ShouldBeNull();
        captured.Headers.Contains("dapr-api-token").ShouldBeFalse();
    }

    // Row 3: token + HTTPS ingress → Authorization: Bearer token; no dapr-api-token.
    [Fact]
    public async Task SendAsync_TokenAndHttpsEndpoint_AttachesBearerOnly()
    {
        // Arrange
        (MemoriesAuthHandler authHandler, TestDelegatingHandler innerHandler) = BuildPipeline(TokenValue);
        using var client = new HttpClient(authHandler) { BaseAddress = new Uri(IngressEndpoint) };

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/tenants");
        using HttpResponseMessage response = await client.SendAsync(request);

        // Assert
        HttpRequestMessage captured = innerHandler.Requests.ShouldHaveSingleItem();
        captured.Headers.Authorization.ShouldNotBeNull();
        captured.Headers.Authorization!.Scheme.ShouldBe("Bearer");
        captured.Headers.Authorization.Parameter.ShouldBe(TokenValue);
        captured.Headers.Contains("dapr-api-token").ShouldBeFalse();
    }

    // Row 4: token + localhost http → dapr-api-token only; no Bearer.
    [Fact]
    public async Task SendAsync_TokenAndLocalhostHttpEndpoint_AttachesDaprHeaderOnly()
    {
        // Arrange
        (MemoriesAuthHandler authHandler, TestDelegatingHandler innerHandler) = BuildPipeline(TokenValue);
        using var client = new HttpClient(authHandler) { BaseAddress = new Uri(LocalhostEndpoint) };

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/tenants");
        using HttpResponseMessage response = await client.SendAsync(request);

        // Assert
        HttpRequestMessage captured = innerHandler.Requests.ShouldHaveSingleItem();
        captured.Headers.Authorization.ShouldBeNull();
        captured.Headers.Contains("dapr-api-token").ShouldBeTrue();
        captured.Headers.GetValues("dapr-api-token").ShouldHaveSingleItem().ShouldBe(TokenValue);
    }

    [Fact]
    public async Task SendAsync_TokenAndRemoteHttpEndpoint_ThrowsInsteadOfSendingPlaintextToken()
    {
        (MemoriesAuthHandler authHandler, TestDelegatingHandler innerHandler) = BuildPipeline(TokenValue);
        using var client = new HttpClient(authHandler) { BaseAddress = new Uri(DockerServiceEndpoint) };

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/tenants");

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => client.SendAsync(request));

        exception.Message.ShouldContain("Refusing to send API token over http://");
        innerHandler.Requests.ShouldBeEmpty();
    }

    // Critical negative invariant: both headers must NEVER be present simultaneously.
    [Theory]
    [MemberData(nameof(HeaderInvariantEndpoints))]
    public async Task SendAsync_AllScenarios_NeverAttachesBothHeaders(string? token, string endpoint)
    {
        // Arrange
        (MemoriesAuthHandler authHandler, TestDelegatingHandler innerHandler) = BuildPipeline(token);
        using var client = new HttpClient(authHandler) { BaseAddress = new Uri(endpoint) };

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/tenants");
        using HttpResponseMessage response = await client.SendAsync(request);

        // Assert
        HttpRequestMessage captured = innerHandler.Requests.ShouldHaveSingleItem();
        bool hasAuthorization = captured.Headers.Authorization is not null;
        bool hasDaprHeader = captured.Headers.Contains("dapr-api-token");
        (hasAuthorization && hasDaprHeader).ShouldBeFalse("auth handler must never attach both headers");
    }

    private static (MemoriesAuthHandler AuthHandler, TestDelegatingHandler InnerHandler) BuildPipeline(string? token)
    {
        var innerHandler = new TestDelegatingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        IOptionsMonitor<MemoriesClientOptions> monitor = Substitute.For<IOptionsMonitor<MemoriesClientOptions>>();
        monitor.CurrentValue.Returns(new MemoriesClientOptions { ApiToken = token });
        var authHandler = new MemoriesAuthHandler(monitor) { InnerHandler = innerHandler };
        return (authHandler, innerHandler);
    }
}
