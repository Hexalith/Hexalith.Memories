// <copyright file="EmbeddingClientTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Net;
using System.Text;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

public class EmbeddingClientTests
{
    private const string TenantId = "test-tenant";
    private const string TestApiKey = "test-api-key";
    private const string TestClientSecret = "test-client-secret";
    private const string TestAccessToken = "test-access-token";
    private const string TestText = "Hello world";

    [Fact]
    public async Task GenerateAsync_SuccessfulResponse_ReturnsVectorWith768Dimensions()
    {
        // Arrange
        float[] expectedVector = CreateVector(768);
        string responseJson = CreateEmbeddingResponse(expectedVector);
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, responseJson);
        DaprClient daprClient = CreateDaprClientWithSecret();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act
        float[] result = await client.GenerateAsync(TestText, TenantId, config, CancellationToken.None);

        // Assert
        result.Length.ShouldBe(768);
        result[0].ShouldBe(expectedVector[0]);
        result[767].ShouldBe(expectedVector[767]);
    }

    [Fact]
    public async Task GenerateAsync_Http429Response_ThrowsEmbeddingRateLimitException()
    {
        // Arrange
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.TooManyRequests, "rate limited");
        DaprClient daprClient = CreateDaprClientWithSecret();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act & Assert
        EmbeddingRateLimitException ex = await Should.ThrowAsync<EmbeddingRateLimitException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        ex.TenantId.ShouldBe(TenantId);
    }

    [Fact]
    public async Task GenerateAsync_Http500Response_ThrowsEmbeddingApiException()
    {
        // Arrange
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.InternalServerError, "server error");
        DaprClient daprClient = CreateDaprClientWithSecret();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act & Assert
        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        ex.StatusCode.ShouldBe(500);
        ex.ResponseBody.ShouldBe("server error");
        ex.TenantId.ShouldBe(TenantId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GenerateAsync_NullOrEmptyText_ThrowsArgumentException(string? text)
    {
        // Arrange
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, "{}");
        DaprClient daprClient = CreateDaprClientWithSecret();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => client.GenerateAsync(text!, TenantId, config, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsync_CallsDaprSecretStoreWithCorrectParameters()
    {
        // Arrange
        float[] vector = CreateVector(768);
        string responseJson = CreateEmbeddingResponse(vector);
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, responseJson);
        DaprClient daprClient = CreateDaprClientWithSecret();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act
        await client.GenerateAsync(TestText, TenantId, config, CancellationToken.None);

        // Assert
        await daprClient.Received(1).GetSecretAsync(
            "secretstore",
            "google-embedding-api-key",
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_DaprSecretStoreUnavailable_ThrowsEmbeddingApiExceptionWithActionableMessage()
    {
        // Arrange
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, "{}");
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetSecretAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DAPR sidecar not running"));
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act & Assert
        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        ex.Message.ShouldContain("secretstore");
        ex.Message.ShouldContain("secretstore.yaml");
        ex.TenantId.ShouldBe(TenantId);
        ex.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task GenerateAsync_MalformedJsonResponse_ThrowsEmbeddingApiException()
    {
        // Arrange
        string malformedJson = """{"unexpected": "shape"}""";
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, malformedJson);
        DaprClient daprClient = CreateDaprClientWithSecret();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act & Assert
        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        ex.Message.ShouldContain("embedding.values");
        ex.Message.ShouldContain(malformedJson);
    }

    [Fact]
    public async Task GenerateAsync_WrongDimensionCount_ThrowsEmbeddingApiException()
    {
        // Arrange
        float[] shortVector = CreateVector(100);
        string responseJson = CreateEmbeddingResponse(shortVector);
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, responseJson);
        DaprClient daprClient = CreateDaprClientWithSecret();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act & Assert
        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        ex.Message.ShouldContain("768");
        ex.Message.ShouldContain("100");
    }

    [Fact]
    public async Task GenerateAsync_HttpTimeout_ThrowsTaskCanceledException()
    {
        // Arrange
        DaprClient daprClient = CreateDaprClientWithSecret();
        TestDelegatingHandler handler = new(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        HttpClient httpClient = new(handler) { Timeout = TimeSpan.FromMilliseconds(50) };
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("EmbeddingClient").Returns(httpClient);
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act & Assert
        await Should.ThrowAsync<TaskCanceledException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsync_InvalidJsonResponseBody_ThrowsEmbeddingApiException()
    {
        // Arrange
        string invalidJson = "not json at all";
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, invalidJson);
        DaprClient daprClient = CreateDaprClientWithSecret();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act & Assert
        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        ex.Message.ShouldContain("invalid JSON");
        ex.Message.ShouldContain(invalidJson);
    }

    [Fact]
    public async Task GenerateAsync_FakeEmbeddingEnabled_ShouldBypassSecretStoreAndHttp()
    {
        // Arrange
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ =>
            throw new InvalidOperationException("HTTP client should not be called when fake embedding is enabled."));
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(useFakeEmbedding: true), CreateHostEnvironment());

        // Act
        float[] result = await client.GenerateAsync(TestText, TenantId, config, CancellationToken.None);

        // Assert
        result.Length.ShouldBe(768);
        result[0].ShouldNotBe(0f);
        await daprClient.DidNotReceive().GetSecretAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PrimeApiKeyAsync_ReusesCachedSecretAcrossCalls()
    {
        // Arrange
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, "{}");
        DaprClient daprClient = CreateDaprClientWithSecret();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act
        await client.PrimeApiKeyAsync(TenantId, config, CancellationToken.None);
        await client.PrimeApiKeyAsync(TenantId, config, CancellationToken.None);

        // Assert
        await daprClient.Received(1).GetSecretAsync(
            "secretstore",
            "google-embedding-api-key",
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_FakeEmbeddingEnabledOutsideDevelopment_ThrowsInvalidOperationException()
    {
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, "{}");
        DaprClient daprClient = Substitute.For<DaprClient>();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => new EmbeddingClient(
                httpClientFactory,
                daprClient,
                CreateConfiguration(useFakeEmbedding: true),
                CreateHostEnvironment(Environments.Production)));

        ex.Message.ShouldContain("Fake embeddings are only supported");
    }

    [Fact]
    public async Task GenerateAsync_Ollama_SendsNativeRequestWithBearerToken()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        TestDelegatingHandler handler = new(async (request, _) =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return CreateJsonResponse(HttpStatusCode.OK, CreateOllamaResponse(CreateVector(2560)));
        });
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(handler);
        DaprClient daprClient = CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret);
        IOidcTokenProvider tokenProvider = CreateTokenProvider(TestAccessToken);
        TenantEmbeddingConfig config = CreateOllamaConfig();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment(), tokenProvider);

        // Act
        await client.GenerateAsync(TestText, TenantId, config, CancellationToken.None);

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Method.ShouldBe(HttpMethod.Post);
        capturedRequest.RequestUri!.ToString().ShouldBe("https://llm.tache.ai/api/embed");
        capturedRequest.Headers.Authorization.ShouldNotBeNull();
        capturedRequest.Headers.Authorization.Scheme.ShouldBe("Bearer");
        capturedRequest.Headers.Authorization.Parameter.ShouldBe(TestAccessToken);
        capturedRequest.Headers.Contains("x-goog-api-key").ShouldBeFalse();

        using JsonDocument body = JsonDocument.Parse(capturedBody!);
        body.RootElement.GetProperty("model").GetString().ShouldBe("qwen3-embedding:4b");
        body.RootElement.GetProperty("input").GetString().ShouldBe(TestText);
        body.RootElement.TryGetProperty("content", out _).ShouldBeFalse();
        body.RootElement.TryGetProperty("output_dimensionality", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateAsync_Ollama_SuccessfulResponse_ReturnsVectorWithConfiguredDimensions()
    {
        // Arrange
        float[] expectedVector = CreateVector(2560);
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, CreateOllamaResponse(expectedVector));
        DaprClient daprClient = CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret);
        IOidcTokenProvider tokenProvider = CreateTokenProvider(TestAccessToken);
        TenantEmbeddingConfig config = CreateOllamaConfig();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment(), tokenProvider);

        // Act
        float[] result = await client.GenerateAsync(TestText, TenantId, config, CancellationToken.None);

        // Assert
        result.Length.ShouldBe(2560);
        result[0].ShouldBe(expectedVector[0]);
        result[2559].ShouldBe(expectedVector[2559]);
    }

    [Fact]
    public async Task GenerateAsync_Ollama_WrongDimensionCount_ThrowsEmbeddingApiException()
    {
        // Arrange
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, CreateOllamaResponse(CreateVector(12)));
        DaprClient daprClient = CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret);
        IOidcTokenProvider tokenProvider = CreateTokenProvider(TestAccessToken);
        TenantEmbeddingConfig config = CreateOllamaConfig();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment(), tokenProvider);

        // Act & Assert
        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        ex.Message.ShouldContain("2560");
        ex.Message.ShouldContain("12");
        ex.Message.ShouldNotContain("Google");
    }

    [Theory]
    [InlineData("""{}""", "missing 'embeddings' array")]
    [InlineData("""{"embeddings":[]}""", "'embeddings' array is empty")]
    [InlineData("""{"embeddings":[null]}""", "first 'embeddings' item must be an array")]
    [InlineData("""{"embeddings":[{}]}""", "first 'embeddings' item must be an array")]
    [InlineData("""{"embeddings":[["bad"]]}""", "invalid JSON or non-numeric vector values")]
    [InlineData("not json at all", "invalid JSON or non-numeric vector values")]
    public async Task GenerateAsync_Ollama_MalformedResponse_ThrowsEmbeddingApiException(string responseBody, string expectedSubMessage)
    {
        // Arrange
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, responseBody);
        DaprClient daprClient = CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret);
        IOidcTokenProvider tokenProvider = CreateTokenProvider(TestAccessToken);
        TenantEmbeddingConfig config = CreateOllamaConfig();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment(), tokenProvider);

        // Act & Assert
        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        ex.Message.ShouldContain("Malformed embedding API response");
        ex.Message.ShouldContain(expectedSubMessage);
    }

    [Fact]
    public async Task GenerateAsync_Ollama_MultipleEmbeddings_ReturnsFirstVector()
    {
        // Arrange
        float[] firstVector = CreateVector(2560);
        float[] secondVector = CreateVector(2560).Select(value => value + 1f).ToArray();
        string responseJson = JsonSerializer.Serialize(new { embeddings = new[] { firstVector, secondVector } });
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.OK, responseJson);
        DaprClient daprClient = CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret);
        IOidcTokenProvider tokenProvider = CreateTokenProvider(TestAccessToken);
        TenantEmbeddingConfig config = CreateOllamaConfig();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment(), tokenProvider);

        // Act
        float[] result = await client.GenerateAsync(TestText, TenantId, config, CancellationToken.None);

        // Assert
        result[0].ShouldBe(firstVector[0]);
        result[2559].ShouldBe(firstVector[2559]);
    }

    [Fact]
    public async Task GenerateAsync_Ollama_Unauthorized_InvalidatesTokenAndRetriesOnce()
    {
        // Arrange
        int requestCount = 0;
        List<string> authorizationHeaders = [];
        TestDelegatingHandler handler = new((request, _) =>
        {
            requestCount++;
            authorizationHeaders.Add(request.Headers.Authorization!.Parameter!);
            return Task.FromResult(requestCount == 1
                ? CreateJsonResponse(HttpStatusCode.Unauthorized, "stale token")
                : CreateJsonResponse(HttpStatusCode.OK, CreateOllamaResponse(CreateVector(2560))));
        });
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(handler);
        DaprClient daprClient = CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret);
        IOidcTokenProvider tokenProvider = Substitute.For<IOidcTokenProvider>();
        tokenProvider.GetAccessTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("old-token");
        tokenProvider.InvalidateAndRefreshAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("new-token");
        TenantEmbeddingConfig config = CreateOllamaConfig();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment(), tokenProvider);

        // Act
        float[] result = await client.GenerateAsync(TestText, TenantId, config, CancellationToken.None);

        // Assert
        result.Length.ShouldBe(2560);
        requestCount.ShouldBe(2);
        authorizationHeaders.ShouldBe(["old-token", "new-token"]);
        await tokenProvider.Received(1).InvalidateAndRefreshAsync(
            config.OidcTokenEndpoint!,
            config.OidcClientId!,
            TestClientSecret,
            config.OidcScope,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GenerateAsync_Ollama_UnauthorizedTwice_ThrowsEmbeddingApiException(HttpStatusCode statusCode)
    {
        // Arrange
        int requestCount = 0;
        TestDelegatingHandler handler = new((_, _) =>
        {
            requestCount++;
            return Task.FromResult(CreateJsonResponse(statusCode, "auth failed"));
        });
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(handler);
        DaprClient daprClient = CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret);
        IOidcTokenProvider tokenProvider = CreateTokenProvider(TestAccessToken, refreshedAccessToken: "new-token");
        TenantEmbeddingConfig config = CreateOllamaConfig();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment(), tokenProvider);

        // Act & Assert
        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        ex.StatusCode.ShouldBe((int)statusCode);
        requestCount.ShouldBe(2);
        await tokenProvider.Received(1).InvalidateAndRefreshAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_Ollama_NonAuthFailure_DoesNotInvalidateToken()
    {
        // Arrange
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(HttpStatusCode.InternalServerError, "server error");
        DaprClient daprClient = CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret);
        IOidcTokenProvider tokenProvider = CreateTokenProvider(TestAccessToken);
        TenantEmbeddingConfig config = CreateOllamaConfig();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment(), tokenProvider);

        // Act & Assert
        await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        await tokenProvider.DidNotReceive().InvalidateAndRefreshAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_Ollama_RetryRebuildsSameBodyWithRefreshedToken()
    {
        // Arrange
        int requestCount = 0;
        List<string> requestBodies = [];
        List<string> authorizationHeaders = [];
        TestDelegatingHandler handler = new(async (request, _) =>
        {
            requestCount++;
            requestBodies.Add(await request.Content!.ReadAsStringAsync());
            authorizationHeaders.Add(request.Headers.Authorization!.Parameter!);
            return requestCount == 1
                ? CreateJsonResponse(HttpStatusCode.Forbidden, "expired")
                : CreateJsonResponse(HttpStatusCode.OK, CreateOllamaResponse(CreateVector(2560)));
        });
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(handler);
        DaprClient daprClient = CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret);
        IOidcTokenProvider tokenProvider = CreateTokenProvider("old-token", refreshedAccessToken: "new-token");
        TenantEmbeddingConfig config = CreateOllamaConfig();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment(), tokenProvider);

        // Act
        await client.GenerateAsync(TestText, TenantId, config, CancellationToken.None);

        // Assert
        requestBodies.Count.ShouldBe(2);
        requestBodies[1].ShouldBe(requestBodies[0]);
        authorizationHeaders.ShouldBe(["old-token", "new-token"]);
    }

    [Fact]
    public async Task GenerateAsync_Ollama_ApiKeyAuthMode_ThrowsActionableException()
    {
        // Arrange
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantEmbeddingConfig config = CreateOllamaConfig() with { AuthMode = EmbeddingProviderDefaults.ApiKeyAuthMode };

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment(), CreateTokenProvider(TestAccessToken));

        // Act & Assert
        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        ex.Message.ShouldContain("api-key");
        ex.Message.ShouldContain("ollama");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("ftp://host/")]
    [InlineData("/relative/path")]
    public async Task GenerateAsync_Ollama_InvalidBaseUrl_ThrowsArgumentException(string baseUrl)
    {
        // Arrange
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        DaprClient daprClient = CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret);
        IOidcTokenProvider tokenProvider = CreateTokenProvider(TestAccessToken);
        TenantEmbeddingConfig config = CreateOllamaConfig() with { BaseUrl = baseUrl };

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment(), tokenProvider);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsync_Ollama_MissingOidcTokenProvider_ThrowsActionableException()
    {
        // Arrange
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        DaprClient daprClient = CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret);
        TenantEmbeddingConfig config = CreateOllamaConfig();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act & Assert
        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        ex.Message.ShouldContain("IOidcTokenProvider");
        ex.Message.ShouldContain("Ollama");
    }

    [Fact]
    public async Task GenerateAsync_UnsupportedProvider_ListsSupportedProviders()
    {
        // Arrange
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Provider = "openai" };

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act & Assert
        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            () => client.GenerateAsync(TestText, TenantId, config, CancellationToken.None));
        ex.Message.ShouldContain("google");
        ex.Message.ShouldContain("ollama");
    }

    [Theory]
    [InlineData("google:gemini-embedding-001", "google", "gemini-embedding-001")]
    [InlineData("ollama:nomic-embed-text", "ollama", "nomic-embed-text")]
    [InlineData("ollama:qwen3-embedding:4b", "ollama", "qwen3-embedding:4b")]
    [InlineData("ollama:library/model:tag", "ollama", "library/model:tag")]
    public void ParseEmbeddingProvider_PreservesModelAfterFirstColon(string value, string expectedProvider, string expectedModel)
    {
        // Act
        EmbeddingProviderIdentifier result = EmbeddingClient.ParseEmbeddingProviderIdentifier(value);

        // Assert
        result.Provider.ShouldBe(expectedProvider);
        result.Model.ShouldBe(expectedModel);
    }

    [Theory]
    [InlineData("google")]
    [InlineData(":gemini-embedding-001")]
    [InlineData("ollama:")]
    [InlineData("openai:text-embedding-3-small")]
    public void ParseEmbeddingProvider_MalformedValue_ThrowsActionableConfigurationError(string value)
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingClient.ParseEmbeddingProviderIdentifier(value));
        ex.Message.ShouldContain("EmbeddingProvider");
        ex.Message.ShouldContain("google");
        ex.Message.ShouldContain("ollama");
        ex.Message.ShouldNotContain(TestAccessToken);
        ex.Message.ShouldNotContain(TestClientSecret);
    }

    [Fact]
    public async Task GenerateAsync_Ollama_RedactsSecretsTokensAndInputFromAuthFailure()
    {
        // Arrange
        string sensitiveInput = "private case note that should not appear in exception text";
        TestDelegatingHandler handler = new((request, _) =>
        {
            string presentedToken = request.Headers.Authorization!.Parameter!;
            string body = $$"""{"error":"{{presentedToken}} {{TestClientSecret}} {{sensitiveInput}}"}""";
            return Task.FromResult(CreateJsonResponse(HttpStatusCode.Unauthorized, body));
        });
        IHttpClientFactory httpClientFactory = CreateHttpClientFactory(handler);
        DaprClient daprClient = CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret);
        IOidcTokenProvider tokenProvider = CreateTokenProvider(TestAccessToken, refreshedAccessToken: "new-token");
        TenantEmbeddingConfig config = CreateOllamaConfig();

        EmbeddingClient client = new(httpClientFactory, daprClient, CreateConfiguration(), CreateHostEnvironment(), tokenProvider);

        // Act & Assert
        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync(sensitiveInput, TenantId, config, CancellationToken.None));
        ex.Message.ShouldNotContain(TestAccessToken);
        ex.Message.ShouldNotContain("new-token");
        ex.Message.ShouldNotContain(TestClientSecret);
        ex.Message.ShouldNotContain(sensitiveInput);
        ex.ResponseBody.ShouldNotBeNull();
        ex.ResponseBody.ShouldNotContain(TestAccessToken);
        ex.ResponseBody.ShouldNotContain("new-token");
        ex.ResponseBody.ShouldNotContain(TestClientSecret);
        ex.ResponseBody.ShouldNotContain(sensitiveInput);
    }

    private static float[] CreateVector(int dimensions)
    {
        float[] vector = new float[dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            vector[i] = i * 0.001f;
        }

        return vector;
    }

    private static string CreateEmbeddingResponse(float[] values)
    {
        return JsonSerializer.Serialize(new
        {
            embedding = new { values },
        });
    }

    private static string CreateOllamaResponse(float[] values)
        => JsonSerializer.Serialize(new { embeddings = new[] { values } });

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string responseBody)
        => new(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
        };

    private static TenantEmbeddingConfig CreateOllamaConfig()
        => EmbeddingProviderDefaults.Ollama() with
        {
            BaseUrl = "https://llm.tache.ai/",
            OidcTokenEndpoint = "https://auth.tache.ai/realms/tache/protocol/openid-connect/token",
            OidcClientId = "memories-embedding",
            OidcScope = "openid",
        };

    private static IHttpClientFactory CreateHttpClientFactory(HttpStatusCode statusCode, string responseBody)
    {
        TestDelegatingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            }));
        HttpClient httpClient = new(handler);
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("EmbeddingClient").Returns(httpClient);
        return factory;
    }

    private static IHttpClientFactory CreateHttpClientFactory(TestDelegatingHandler handler)
    {
        HttpClient httpClient = new(handler);
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("EmbeddingClient").Returns(httpClient);
        return factory;
    }

    private static DaprClient CreateDaprClientWithSecret()
        => CreateDaprClientWithSecret("google-embedding-api-key", TestApiKey);

    private static DaprClient CreateDaprClientWithSecret(string secretKeyName, string secretValue)
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetSecretAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { [secretKeyName] = secretValue });
        return daprClient;
    }

    private static IOidcTokenProvider CreateTokenProvider(string accessToken, string? refreshedAccessToken = null)
    {
        IOidcTokenProvider tokenProvider = Substitute.For<IOidcTokenProvider>();
        tokenProvider.GetAccessTokenAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(accessToken);
        tokenProvider.InvalidateAndRefreshAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(refreshedAccessToken ?? accessToken);
        return tokenProvider;
    }

    private static IConfiguration CreateConfiguration(bool useFakeEmbedding = false)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memories:Testing:UseFakeEmbedding"] = useFakeEmbedding.ToString(),
            })
            .Build();

    private static IHostEnvironment CreateHostEnvironment(string environmentName = "Development")
    {
        IHostEnvironment hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns(environmentName);
        return hostEnvironment;
    }

    private sealed class TestDelegatingHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public TestDelegatingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
