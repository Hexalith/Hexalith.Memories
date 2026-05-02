// <copyright file="OidcTokenProviderTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

public class OidcTokenProviderTests
{
    private const string ClientId = "memories-embedding";
    private const string ClientSecret = "super-secret-client-value";
    private const string TokenEndpoint = "https://keycloak.example/realms/memories/protocol/openid-connect/token";

    [Fact]
    public async Task GetAccessTokenAsync_CacheMiss_PostsClientCredentialsForm()
    {
        // Arrange
        ScriptedTokenHandler handler = new((_, _) => TokenResponse("token-1", 3600));
        OidcTokenProvider provider = CreateProvider(handler);

        // Act
        string token = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, "embeddings.write", CancellationToken.None);

        // Assert
        token.ShouldBe("token-1");
        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Post);
        handler.Requests[0].RequestUri!.AbsoluteUri.ShouldBe(TokenEndpoint);
        handler.Requests[0].ContentType.ShouldBe("application/x-www-form-urlencoded");
        handler.Requests[0].Form["grant_type"].ShouldBe("client_credentials");
        handler.Requests[0].Form["client_id"].ShouldBe(ClientId);
        handler.Requests[0].Form["client_secret"].ShouldBe(ClientSecret);
        handler.Requests[0].Form["scope"].ShouldBe("embeddings.write");
    }

    [Fact]
    public async Task GetAccessTokenAsync_CacheHit_DoesNotSendSecondHttpRequest()
    {
        // Arrange
        ScriptedTokenHandler handler = new((_, _) => TokenResponse("token-1", 3600));
        OidcTokenProvider provider = CreateProvider(handler);

        // Act
        string first = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);
        string second = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);

        // Assert
        first.ShouldBe("token-1");
        second.ShouldBe("token-1");
        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].Form.ContainsKey("scope").ShouldBeFalse();
    }

    [Fact]
    public async Task GetAccessTokenAsync_DifferentScopes_DoNotReuseCachedToken()
    {
        // Arrange
        int callCount = 0;
        ScriptedTokenHandler handler = new((_, _) =>
        {
            callCount++;
            return TokenResponse($"token-{callCount}", 3600);
        });
        OidcTokenProvider provider = CreateProvider(handler);

        // Act
        string readToken = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, "read", CancellationToken.None);
        string writeToken = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, "write", CancellationToken.None);
        string cachedReadToken = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, " read ", CancellationToken.None);

        // Assert
        readToken.ShouldBe("token-1");
        writeToken.ShouldBe("token-2");
        cachedReadToken.ShouldBe("token-1");
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].Form["scope"].ShouldBe("read");
        handler.Requests[1].Form["scope"].ShouldBe("write");
    }

    [Fact]
    public async Task GetAccessTokenAsync_ExpiredEntry_FetchesNewToken()
    {
        // Arrange
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero));
        int callCount = 0;
        ScriptedTokenHandler handler = new((_, _) =>
        {
            callCount++;
            return TokenResponse($"token-{callCount}", 60);
        });
        OidcTokenProvider provider = CreateProvider(handler, timeProvider);

        // Act
        string first = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromSeconds(31));
        string second = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);

        // Assert
        first.ShouldBe("token-1");
        second.ShouldBe("token-2");
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task InvalidateAndRefreshAsync_EvictsAndFetchesExactlyOnce()
    {
        // Arrange
        int callCount = 0;
        ScriptedTokenHandler handler = new((_, _) =>
        {
            callCount++;
            return TokenResponse($"token-{callCount}", 3600);
        });
        OidcTokenProvider provider = CreateProvider(handler);

        // Act
        string first = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);
        string refreshed = await provider.InvalidateAndRefreshAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);
        string cached = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);

        // Assert
        first.ShouldBe("token-1");
        refreshed.ShouldBe("token-2");
        cached.ShouldBe("token-2");
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task InvalidateAndRefreshAsync_OnlyEvictsMatchingScopeKey()
    {
        // Arrange
        int callCount = 0;
        ScriptedTokenHandler handler = new((_, _) =>
        {
            callCount++;
            return TokenResponse($"token-{callCount}", 3600);
        });
        OidcTokenProvider provider = CreateProvider(handler);

        // Act
        string readToken = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, "read", CancellationToken.None);
        string writeToken = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, "write", CancellationToken.None);
        string refreshedReadToken = await provider.InvalidateAndRefreshAsync(TokenEndpoint, ClientId, ClientSecret, "read", CancellationToken.None);
        string cachedWriteToken = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, "write", CancellationToken.None);

        // Assert
        readToken.ShouldBe("token-1");
        writeToken.ShouldBe("token-2");
        refreshedReadToken.ShouldBe("token-3");
        cachedWriteToken.ShouldBe("token-2");
        handler.Requests.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ConcurrentSameKey_SendsSingleRequest()
    {
        // Arrange
        TaskCompletionSource<HttpResponseMessage> responseGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ScriptedTokenHandler handler = new((_, _) => responseGate.Task);
        OidcTokenProvider provider = CreateProvider(handler);

        // Act
        Task<string> first = provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);
        Task<string> second = provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);
        await handler.WaitForRequestsAsync(1);
        responseGate.SetResult(TokenResponse("shared-token", 3600));
        string[] tokens = await Task.WhenAll(first, second);

        // Assert
        tokens.ShouldAllBe(t => t == "shared-token");
        handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ConcurrentDifferentKeys_DoNotBlockEachOther()
    {
        // Arrange
        TaskCompletionSource<HttpResponseMessage> slowResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ScriptedTokenHandler handler = new((request, _) =>
            request.RequestUri!.AbsoluteUri.Contains("realm-a", StringComparison.Ordinal)
                ? slowResponse.Task
                : Task.FromResult(TokenResponse("fast-token", 3600)));
        OidcTokenProvider provider = CreateProvider(handler);

        // Act
        Task<string> slow = provider.GetAccessTokenAsync(
            "https://keycloak.example/realms/realm-a/protocol/openid-connect/token",
            "client-a",
            ClientSecret,
            null,
            CancellationToken.None);
        Task<string> fast = provider.GetAccessTokenAsync(
            "https://keycloak.example/realms/realm-b/protocol/openid-connect/token",
            "client-b",
            ClientSecret,
            null,
            CancellationToken.None);

        string fastToken = await fast;
        slow.IsCompleted.ShouldBeFalse();
        slowResponse.SetResult(TokenResponse("slow-token", 3600));
        string slowToken = await slow;

        // Assert
        fastToken.ShouldBe("fast-token");
        slowToken.ShouldBe("slow-token");
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAccessTokenAsync_CancelledWaiter_DoesNotCancelSharedAcquisition()
    {
        // Arrange
        TaskCompletionSource<HttpResponseMessage> responseGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ScriptedTokenHandler handler = new((_, _) => responseGate.Task);
        OidcTokenProvider provider = CreateProvider(handler);
        using CancellationTokenSource cancelledWaiter = new();

        // Act
        Task<string> first = provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);
        await handler.WaitForRequestsAsync(1);
        Task<string> second = provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, cancelledWaiter.Token);
        await cancelledWaiter.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => second);
        responseGate.SetResult(TokenResponse("shared-token", 3600));
        string firstToken = await first;
        string cachedToken = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);

        // Assert
        firstToken.ShouldBe("shared-token");
        cachedToken.ShouldBe("shared-token");
        handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetAccessTokenAsync_NonSuccess_ThrowsTypedExceptionWithoutCaching()
    {
        // Arrange
        int callCount = 0;
        string longBody = new('x', 1200);
        ScriptedTokenHandler handler = new((_, _) =>
        {
            callCount++;
            return callCount == 1
                ? new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    Content = new StringContent(longBody, Encoding.UTF8, "application/json"),
                }
                : TokenResponse("token-after-failure", 3600);
        });
        OidcTokenProvider provider = CreateProvider(handler);

        // Act
        OidcTokenAcquisitionException ex = await Should.ThrowAsync<OidcTokenAcquisitionException>(
            () => provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None));
        string token = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);

        // Assert
        ex.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        ex.ResponseBodyPreview.Length.ShouldBe(1024);
        ex.TokenEndpoint.ShouldBe(TokenEndpoint);
        ex.ClientId.ShouldBe(ClientId);
        ex.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        token.ShouldBe("token-after-failure");
        handler.Requests.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData("""{"expires_in":3600,"token_type":"Bearer"}""")]
    [InlineData("""{"access_token":"token-1","token_type":"Bearer"}""")]
    [InlineData("""{"access_token":"token-1","expires_in":0,"token_type":"Bearer"}""")]
    [InlineData("""{"access_token":"token-1","expires_in":3600,"token_type":"Basic"}""")]
    [InlineData("""not-json""")]
    public async Task GetAccessTokenAsync_MalformedSuccess_ThrowsTypedExceptionWithoutCaching(string responseBody)
    {
        // Arrange
        int callCount = 0;
        ScriptedTokenHandler handler = new((_, _) =>
        {
            callCount++;
            return callCount == 1
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
                }
                : TokenResponse("token-after-malformed-response", 3600);
        });
        OidcTokenProvider provider = CreateProvider(handler);

        // Act
        OidcTokenAcquisitionException ex = await Should.ThrowAsync<OidcTokenAcquisitionException>(
            () => provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None));
        string token = await provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);

        // Assert
        ex.StatusCode.ShouldBeNull();
        token.ShouldBe("token-after-malformed-response");
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAccessTokenAsync_Cancellation_PropagatesOperationCanceledException()
    {
        // Arrange
        ScriptedTokenHandler handler = new(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromMinutes(1), ct);
            return TokenResponse("never-used", 3600);
        });
        OidcTokenProvider provider = CreateProvider(handler);
        using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(50));

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, cts.Token));
    }

    [Fact]
    public async Task LogsAndExceptions_DoNotContainClientSecretOrAccessToken()
    {
        // Arrange
        string token = "token-material-that-must-not-leak";
        string endpointWithQuery = TokenEndpoint + "?client_secret=" + ClientSecret;
        CapturingLogger<OidcTokenProvider> logger = new();
        ScriptedTokenHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(
                $$"""{"error":"invalid_client","access_token":"{{token}}","client_secret":"{{ClientSecret}}"}""",
                Encoding.UTF8,
                "application/json"),
        });
        OidcTokenProvider provider = CreateProvider(handler, logger: logger);

        // Act
        OidcTokenAcquisitionException ex = await Should.ThrowAsync<OidcTokenAcquisitionException>(
            () => provider.GetAccessTokenAsync(endpointWithQuery, ClientId, ClientSecret, null, CancellationToken.None));

        // Assert
        string logText = string.Join(Environment.NewLine, logger.Entries.Select(e => e.Message));
        logText.ShouldNotContain(ClientSecret);
        logText.ShouldNotContain(token);
        ex.Message.ShouldNotContain(ClientSecret);
        ex.Message.ShouldNotContain(token);
        ex.ResponseBodyPreview.ShouldNotContain(ClientSecret);
        ex.ResponseBodyPreview.ShouldNotContain(token);
        ex.TokenEndpoint.ShouldBe(TokenEndpoint);
        handler.Requests[0].RequestUri!.AbsoluteUri.ShouldBe(TokenEndpoint);
    }

    private static OidcTokenProvider CreateProvider(
        ScriptedTokenHandler handler,
        FakeTimeProvider? timeProvider = null,
        CapturingLogger<OidcTokenProvider>? logger = null)
        => new(
            new HttpClient(handler),
            timeProvider ?? new FakeTimeProvider(new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero)),
            logger ?? new CapturingLogger<OidcTokenProvider>());

    private static HttpResponseMessage TokenResponse(string token, int expiresIn)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    access_token = token,
                    expires_in = expiresIn,
                    token_type = "Bearer",
                }),
                Encoding.UTF8,
                "application/json"),
        };

    private sealed class ScriptedTokenHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
        private readonly TaskCompletionSource _requestObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ScriptedTokenHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            : this((request, _) => Task.FromResult(handler(request)))
        {
        }

        public ScriptedTokenHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
            : this((request, ct) => Task.FromResult(handler(request, ct)))
        {
        }

        public ScriptedTokenHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public List<CapturedRequest> Requests { get; } = [];

        public async Task WaitForRequestsAsync(int count)
        {
            while (Requests.Count < count)
            {
                await _requestObserved.Task.ConfigureAwait(false);
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri,
                request.Content?.Headers.ContentType?.MediaType,
                ParseForm(body)));
            _requestObserved.TrySetResult();
            return await _handler(request, cancellationToken).ConfigureAwait(false);
        }

        private static Dictionary<string, string> ParseForm(string body)
            => body.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .ToDictionary(
                    pair => WebUtility.UrlDecode(pair[0]),
                    pair => pair.Length > 1 ? WebUtility.UrlDecode(pair[1]) : string.Empty,
                    StringComparer.Ordinal);
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri? RequestUri,
        string? ContentType,
        IReadOnlyDictionary<string, string> Form);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId, formatter(state, exception)));
    }
}
