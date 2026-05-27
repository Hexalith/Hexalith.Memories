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

using NSubstitute;

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
    public async Task GetAccessTokenAsync_CancelledLeader_DoesNotPoisonSharedAcquisition()
    {
        // Story 14.3 AC1: a caller cancellation must not cancel the in-flight HTTP fetch shared
        // by other waiters. The leader cancels mid-fetch and the second same-key waiter still
        // receives the original token without a second HTTP request.
        TaskCompletionSource<HttpResponseMessage> responseGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ScriptedTokenHandler handler = new((_, _) => responseGate.Task);
        OidcTokenProvider provider = CreateProvider(handler);
        using CancellationTokenSource leaderCts = new();

        Task<string> leader = provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, leaderCts.Token);
        await handler.WaitForRequestsAsync(1);
        Task<string> waiter = provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);
        await leaderCts.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => leader);
        responseGate.SetResult(TokenResponse("shared-token", 3600));

        string waiterToken = await waiter;
        waiterToken.ShouldBe("shared-token");
        handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task InvalidateAndRefreshAsync_ConcurrentForcedCallers_CollapseToOneRequest()
    {
        // Story 14.3 AC4: concurrent forced refresh callers for the same key must not each fire
        // a token endpoint request.
        TaskCompletionSource<HttpResponseMessage> responseGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ScriptedTokenHandler handler = new((_, _) => responseGate.Task);
        OidcTokenProvider provider = CreateProvider(handler);

        Task<string> first = provider.InvalidateAndRefreshAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);
        Task<string> second = provider.InvalidateAndRefreshAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);
        Task<string> third = provider.InvalidateAndRefreshAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None);
        await handler.WaitForRequestsAsync(1);
        responseGate.SetResult(TokenResponse("forced-token", 3600));
        string[] tokens = await Task.WhenAll(first, second, third);

        tokens.ShouldAllBe(t => t == "forced-token");
        handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task InvalidateAndRefreshAsync_DoesNotJoinNormalInflightFetchWithStaleSecret()
    {
        // A forced refresh may carry a rotated client_secret after a 401/403 retry; it must not
        // join an older normal cache-miss fetch that was started with stale credentials.
        TaskCompletionSource<HttpResponseMessage> normalResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<HttpResponseMessage> forcedResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int requestCount = 0;
        ScriptedTokenHandler handler = new((_, _) =>
            Interlocked.Increment(ref requestCount) == 1
                ? normalResponse.Task
                : forcedResponse.Task);
        OidcTokenProvider provider = CreateProvider(handler);

        Task<string> normal = provider.GetAccessTokenAsync(TokenEndpoint, ClientId, "stale-secret", null, CancellationToken.None);
        await handler.WaitForRequestsAsync(1);
        Task<string> forced = provider.InvalidateAndRefreshAsync(TokenEndpoint, ClientId, "rotated-secret", null, CancellationToken.None);
        await handler.WaitForRequestsAsync(2);

        forcedResponse.SetResult(TokenResponse("rotated-token", 3600));
        normalResponse.SetResult(TokenResponse("stale-token", 3600));

        string forcedToken = await forced;
        string normalToken = await normal;

        forcedToken.ShouldBe("rotated-token");
        normalToken.ShouldBe("stale-token");
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].Form["client_secret"].ShouldBe("stale-secret");
        handler.Requests[1].Form["client_secret"].ShouldBe("rotated-secret");
    }

    [Fact]
    public async Task GetAccessTokenAsync_AlreadyCancelled_DoesNotStartDetachedFetch()
    {
        ScriptedTokenHandler handler = new((_, _) => TokenResponse("never-used", 3600));
        OidcTokenProvider provider = CreateProvider(handler);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, cts.Token));

        handler.Requests.Count.ShouldBe(0);
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
        // Arrange — handler completes immediately when its own cancellation token cancels, so the
        // detached fetch returns promptly when the test cancels (even though the public surface
        // already throws OperationCanceledException via Task.WaitAsync(ct)).
        TaskCompletionSource<HttpResponseMessage> gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ScriptedTokenHandler handler = new((_, _) => gate.Task);
        OidcTokenProvider provider = CreateProvider(handler);
        using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(50));

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, cts.Token));

        // Release the gate so the detached HTTP task does not leak past the test.
        gate.TrySetResult(TokenResponse("never-used", 3600));
    }

    [Fact]
    public async Task GetAccessTokenAsync_HttpRequestException_WrappedInOidcAcquisitionException()
    {
        // Story 14.3 AC5: transport failures cross the provider boundary as OidcTokenAcquisitionException.
        ScriptedTokenHandler handler = new((_, _) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("simulated transport failure")));
        CapturingLogger<OidcTokenProvider> logger = new();
        OidcTokenProvider provider = CreateProvider(handler, logger: logger);

        OidcTokenAcquisitionException ex = await Should.ThrowAsync<OidcTokenAcquisitionException>(
            () => provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None));

        ex.StatusCode.ShouldBeNull();
        ex.InnerException.ShouldBeOfType<HttpRequestException>();
        ex.TokenEndpoint.ShouldBe(TokenEndpoint);
        ex.ClientId.ShouldBe(ClientId);
        ex.Message.ShouldNotContain(ClientSecret);
        ex.ResponseBodyPreview.ShouldNotContain(ClientSecret);
        string logText = string.Join(Environment.NewLine, logger.Entries.Select(e => e.Message));
        logText.ShouldNotContain(ClientSecret);
    }

    [Fact]
    public async Task GetAccessTokenAsync_IOException_WrappedInOidcAcquisitionException()
    {
        ScriptedTokenHandler handler = new((_, _) =>
            Task.FromException<HttpResponseMessage>(new IOException("simulated socket reset")));
        OidcTokenProvider provider = CreateProvider(handler);

        OidcTokenAcquisitionException ex = await Should.ThrowAsync<OidcTokenAcquisitionException>(
            () => provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None));

        ex.StatusCode.ShouldBeNull();
        ex.InnerException.ShouldBeOfType<IOException>();
        ex.Message.ShouldNotContain(ClientSecret);
    }

    [Fact]
    public async Task GetAccessTokenAsync_TimeoutException_WrappedInOidcAcquisitionException()
    {
        // Simulates HttpClient.Timeout by raising TaskCanceledException while the caller's CT is
        // not cancelled — this is the case the provider must surface as a typed transport error
        // rather than as a caller-cancellation OperationCanceledException.
        ScriptedTokenHandler handler = new((_, _) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("simulated request timeout")));
        OidcTokenProvider provider = CreateProvider(handler);

        OidcTokenAcquisitionException ex = await Should.ThrowAsync<OidcTokenAcquisitionException>(
            () => provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None));

        ex.StatusCode.ShouldBeNull();
        ex.InnerException.ShouldBeOfType<TaskCanceledException>();
        ex.Message.ShouldContain("timed out");
        ex.Message.ShouldNotContain(ClientSecret);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ResponseContentReadFailure_WrappedInOidcAcquisitionException()
    {
        ScriptedTokenHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ThrowingContent(new IOException("simulated response stream reset")),
        });
        OidcTokenProvider provider = CreateProvider(handler);

        OidcTokenAcquisitionException ex = await Should.ThrowAsync<OidcTokenAcquisitionException>(
            () => provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None));

        ex.StatusCode.ShouldBeNull();
        ex.InnerException.ShouldBeOfType<HttpRequestException>();
        ex.Message.ShouldNotContain(ClientSecret);
    }

    [Fact]
    public async Task LogsAndExceptions_DoNotContainClientSecretOrAccessToken()
    {
        // Arrange — exercise the response-body redaction for token-shaped JSON properties; the
        // endpoint itself is sanitized (no embedded credentials, no query, no fragment) since
        // those shapes are now rejected synchronously by ValidateAndCreateKey.
        string token = "token-material-that-must-not-leak";
        CapturingLogger<OidcTokenProvider> logger = new();
        ScriptedTokenHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(
                $$"""{"error":"invalid_client","access_token":"{{token}}","client_secret":"{{ClientSecret}}","refresh_token":"{{token}}","id_token":"{{token}}"}""",
                Encoding.UTF8,
                "application/json"),
        });
        OidcTokenProvider provider = CreateProvider(handler, logger: logger);

        // Act
        OidcTokenAcquisitionException ex = await Should.ThrowAsync<OidcTokenAcquisitionException>(
            () => provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None));

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

    [Fact]
    public async Task SanitizePreview_OverlappingTokenFields_AllRedacted()
    {
        // Multiple sensitive JSON properties of varying length, including overlapping bearer
        // values, must all be redacted by the response preview.
        string longToken = new('A', 600);
        string shortToken = "ttt";
        string bearerToken = "bearer-token-material-that-must-not-leak";
        ScriptedTokenHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                $$"""{"access_token":"{{longToken}}","refresh_token":"{{longToken}}","id_token":"{{shortToken}}","client_secret":"{{ClientSecret}}","error_description":"Bearer {{bearerToken}}"}""",
                Encoding.UTF8,
                "application/json"),
        });
        OidcTokenProvider provider = CreateProvider(handler);

        OidcTokenAcquisitionException ex = await Should.ThrowAsync<OidcTokenAcquisitionException>(
            () => provider.GetAccessTokenAsync(TokenEndpoint, ClientId, ClientSecret, null, CancellationToken.None));

        ex.ResponseBodyPreview.ShouldNotContain(longToken);
        ex.ResponseBodyPreview.ShouldNotContain(ClientSecret);
        ex.ResponseBodyPreview.ShouldNotContain(bearerToken);
        ex.ResponseBodyPreview.ShouldContain("\"access_token\":\"[redacted]\"");
        ex.ResponseBodyPreview.ShouldContain("\"refresh_token\":\"[redacted]\"");
        ex.ResponseBodyPreview.ShouldContain("\"id_token\":\"[redacted]\"");
        ex.ResponseBodyPreview.ShouldContain("\"client_secret\":\"[redacted]\"");
        ex.ResponseBodyPreview.ShouldContain("Bearer [redacted]");
    }

    [Theory]
    [InlineData("https://user:pw@keycloak.example/realms/memories/protocol/openid-connect/token")]
    [InlineData("https://only-user@keycloak.example/realms/memories/protocol/openid-connect/token")]
    public async Task GetAccessTokenAsync_TokenEndpointWithUserInfo_ThrowsArgumentExceptionWithoutEchoingCredentials(string endpoint)
    {
        ScriptedTokenHandler handler = new((_, _) => TokenResponse("never-fetched", 3600));
        OidcTokenProvider provider = CreateProvider(handler);

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            () => provider.GetAccessTokenAsync(endpoint, ClientId, ClientSecret, null, CancellationToken.None));

        ex.Message.ShouldNotContain("user:pw");
        ex.Message.ShouldNotContain("only-user");
        ex.Message.ShouldContain("user-info");
        handler.Requests.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetAccessTokenAsync_TokenEndpointWithQuery_ThrowsArgumentExceptionWithoutEchoingQueryValues()
    {
        ScriptedTokenHandler handler = new((_, _) => TokenResponse("never-fetched", 3600));
        OidcTokenProvider provider = CreateProvider(handler);
        string endpoint = TokenEndpoint + "?client_secret=" + ClientSecret;

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            () => provider.GetAccessTokenAsync(endpoint, ClientId, ClientSecret, null, CancellationToken.None));

        ex.Message.ShouldNotContain(ClientSecret);
        ex.Message.ShouldContain("query string");
        handler.Requests.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetAccessTokenAsync_TokenEndpointWithFragment_ThrowsArgumentException()
    {
        ScriptedTokenHandler handler = new((_, _) => TokenResponse("never-fetched", 3600));
        OidcTokenProvider provider = CreateProvider(handler);
        string endpoint = TokenEndpoint + "#frag";

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            () => provider.GetAccessTokenAsync(endpoint, ClientId, ClientSecret, null, CancellationToken.None));

        ex.Message.ShouldContain("fragment");
        handler.Requests.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData("http://localhost/realms/memories/protocol/openid-connect/token")]
    [InlineData("http://localhost:8080/realms/memories/protocol/openid-connect/token")]
    [InlineData("http://127.0.0.1:8080/realms/memories/protocol/openid-connect/token")]
    [InlineData("http://[::1]/realms/memories/protocol/openid-connect/token")]
    [InlineData("http://[::1]:8080/realms/memories/protocol/openid-connect/token")]
    public async Task GetAccessTokenAsync_LoopbackHttpTokenEndpoint_SendsTokenRequest(string endpoint)
    {
        ScriptedTokenHandler handler = new((_, _) => TokenResponse("loopback-token", 3600));
        OidcTokenProvider provider = CreateProvider(handler);

        string token = await provider.GetAccessTokenAsync(endpoint, ClientId, ClientSecret, null, CancellationToken.None);

        token.ShouldBe("loopback-token");
        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].RequestUri!.AbsoluteUri.ShouldBe(endpoint);
    }

    // Story 15.4 D2 pinning: .NET `Uri` canonicalizes alternative literal forms of loopback to the
    // allowed `127.0.0.1` / `[::1]` host values. These forms are intentionally accepted; pinning
    // them with tests means a future refactor that introduces a stricter literal-string match will
    // surface in CI rather than silently break local operator setups.
    [Theory]
    [InlineData("http://2130706433/realms/memories/protocol/openid-connect/token")] // decimal IPv4 form of 127.0.0.1
    [InlineData("http://127.0.0.001/realms/memories/protocol/openid-connect/token")] // octal-style leading zeros for 127.0.0.1
    [InlineData("http://[0:0:0:0:0:0:0:1]/realms/memories/protocol/openid-connect/token")] // expanded IPv6 loopback
    [InlineData("http://[::0001]/realms/memories/protocol/openid-connect/token")] // padded compressed IPv6 loopback
    public async Task GetAccessTokenAsync_UriCanonicalizedLoopbackForms_SendsTokenRequest(string endpoint)
    {
        ScriptedTokenHandler handler = new((_, _) => TokenResponse("canonicalized-loopback-token", 3600));
        OidcTokenProvider provider = CreateProvider(handler);

        string token = await provider.GetAccessTokenAsync(endpoint, ClientId, ClientSecret, null, CancellationToken.None);

        token.ShouldBe("canonicalized-loopback-token");
        handler.Requests.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("http://auth.tache.ai/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://10.0.0.5/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://172.16.0.5/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://192.168.1.20/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://169.254.169.254/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://host.docker.internal/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://localtest.me/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://keycloak.internal/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://127.0.0.2/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://[::ffff:127.0.0.1]/realms/tache/protocol/openid-connect/token")] // Story 15.4 P2: IPv4-mapped IPv6 is not the literal [::1].
    [InlineData("http://[::ffff:7f00:1]/realms/tache/protocol/openid-connect/token")] // Story 15.4 P2: compressed IPv4-mapped IPv6 form.
    [InlineData("http://localhost./realms/tache/protocol/openid-connect/token")] // Story 15.4 P3: trailing-dot host is not the literal "localhost".
    public async Task GetAccessTokenAsync_NonLoopbackHttpTokenEndpoint_ThrowsBeforeSendingRequest(string endpoint)
    {
        ScriptedTokenHandler handler = new((_, _) => TokenResponse("never-fetched", 3600));
        OidcTokenProvider provider = CreateProvider(handler);

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            () => provider.GetAccessTokenAsync(endpoint, ClientId, ClientSecret, null, CancellationToken.None));

        AssertSanitizedTransportPolicyMessage(ex, "tokenEndpoint", endpoint);
        handler.Requests.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetAccessTokenAsync_NonLoopbackHttpTokenEndpointWithSecretLikePath_DoesNotEchoEndpoint()
    {
        ScriptedTokenHandler handler = new((_, _) => TokenResponse("never-fetched", 3600));
        OidcTokenProvider provider = CreateProvider(handler);
        const string endpoint = "http://auth.tache.ai/realms/Bearer%20abc.def.ghi/client-secret-value/token";

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            () => provider.GetAccessTokenAsync(endpoint, ClientId, ClientSecret, null, CancellationToken.None));

        AssertSanitizedTransportPolicyMessage(ex, "tokenEndpoint", endpoint);
        ex.Message.ShouldNotContain("Bearer");
        ex.Message.ShouldNotContain("abc.def.ghi");
        ex.Message.ShouldNotContain("client-secret-value");
        handler.Requests.Count.ShouldBe(0);
    }

    [Fact]
    public void Constructor_NullHttpClientFactory_ThrowsArgumentNullException()
    {
        // Story 14.3 AC2: factory-driven HttpClient lifetime; null factory should fail loudly.
        Should.Throw<ArgumentNullException>(() => new OidcTokenProvider(
            null!,
            new FakeTimeProvider(),
            Substitute.For<ILogger<OidcTokenProvider>>()));
    }

    private static void AssertSanitizedTransportPolicyMessage(
        ArgumentException ex,
        string parameterName,
        string endpoint)
    {
        ex.ParamName.ShouldBe(parameterName);
        ex.Message.ShouldContain("HTTPS");
        ex.Message.ShouldContain("loopback");
        ex.Message.ShouldContain("localhost");
        ex.Message.ShouldContain("127.0.0.1");
        ex.Message.ShouldContain("[::1]");
        ex.Message.ShouldNotContain(endpoint);

        // Story 15.4 P4: strengthen leak guard. ShouldNotContain(endpoint) only catches the full
        // URL; a regression that echoes just the host, path, or known credential markers would
        // still pass. Probe each segment explicitly. The literal allowlist tokens shared with the
        // policy message (`localhost`, `127.0.0.1`, `[::1]`) are excluded from the host-leak guard
        // because they appear in the legitimate policy text.
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? parsed))
        {
            string host = parsed.Host;
            if (!string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(host, "127.0.0.1", StringComparison.Ordinal) &&
                !string.Equals(host, "[::1]", StringComparison.Ordinal))
            {
                ex.Message.ShouldNotContain(host);
            }

            if (!string.IsNullOrEmpty(parsed.AbsolutePath) && parsed.AbsolutePath != "/")
            {
                ex.Message.ShouldNotContain(parsed.AbsolutePath);
            }
        }

        ex.Message.ShouldNotContain("Bearer");
        ex.Message.ShouldNotContain("client_secret");
        ex.Message.ShouldNotContain("client-secret");
    }

    private static OidcTokenProvider CreateProvider(
        ScriptedTokenHandler handler,
        FakeTimeProvider? timeProvider = null,
        CapturingLogger<OidcTokenProvider>? logger = null)
    {
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(OidcTokenProvider.HttpClientName).Returns(_ => new HttpClient(handler, disposeHandler: false));
        return new OidcTokenProvider(
            factory,
            timeProvider ?? new FakeTimeProvider(new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero)),
            logger ?? new CapturingLogger<OidcTokenProvider>());
    }

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
        private readonly object _gate = new();
        private readonly List<TaskCompletionSource> _waiters = [];

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
            while (true)
            {
                TaskCompletionSource? toAwait = null;
                lock (_gate)
                {
                    if (Requests.Count >= count)
                    {
                        return;
                    }

                    toAwait = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    _waiters.Add(toAwait);
                }

                await toAwait.Task.ConfigureAwait(false);
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            CapturedRequest captured = new(
                request.Method,
                request.RequestUri,
                request.Content?.Headers.ContentType?.MediaType,
                ParseForm(body));

            TaskCompletionSource[] toRelease;
            lock (_gate)
            {
                Requests.Add(captured);
                toRelease = [.. _waiters];
                _waiters.Clear();
            }

            foreach (TaskCompletionSource waiter in toRelease)
            {
                waiter.TrySetResult();
            }

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

    private sealed class ThrowingContent : HttpContent
    {
        private readonly Exception _exception;

        public ThrowingContent(Exception exception)
        {
            _exception = exception;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => Task.FromException(_exception);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return true;
        }
    }

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
