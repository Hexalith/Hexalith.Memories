// <copyright file="EmbeddingClientBatchTests.cs" company="ITANEO">
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

using Shouldly;

public class EmbeddingClientBatchTests
{
    private const string TenantId = "test-tenant";
    private const string TestApiKey = "test-api-key";
    private const string TestClientSecret = "test-client-secret";
    private const string TestAccessToken = "test-access-token";

    [Fact]
    public async Task GenerateBatchAsync_Google_UsesBatchEndpointAndPreservesOrder()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        float[] first = CreateVector(768, 0f);
        float[] second = CreateVector(768, 10f);
        TestDelegatingHandler handler = new(async (request, _) =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return CreateJsonResponse(HttpStatusCode.OK, CreateGoogleBatchResponse(first, second));
        });
        EmbeddingClient client = new(
            CreateHttpClientFactory(handler),
            CreateDaprClientWithSecret("google-embedding-api-key", TestApiKey),
            CreateConfiguration(),
            CreateHostEnvironment());

        IReadOnlyList<float[]> result = await client.GenerateBatchAsync(
            ["first text", "second text"],
            TenantId,
            EmbeddingProviderDefaults.Google(),
            CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0][0].ShouldBe(first[0]);
        result[1][0].ShouldBe(second[0]);
        capturedRequest.ShouldNotBeNull();
        capturedRequest.RequestUri!.ToString().ShouldBe(
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:batchEmbedContents");
        capturedRequest.Headers.GetValues("x-goog-api-key").ShouldBe([TestApiKey]);

        using JsonDocument body = JsonDocument.Parse(capturedBody!);
        JsonElement requests = body.RootElement.GetProperty("requests");
        requests.GetArrayLength().ShouldBe(2);
        requests[0].GetProperty("model").GetString().ShouldBe("models/gemini-embedding-001");
        requests[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString().ShouldBe("first text");
        requests[0].GetProperty("output_dimensionality").GetInt32().ShouldBe(768);
        requests[1].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString().ShouldBe("second text");
    }

    [Fact]
    public async Task GenerateBatchAsync_Ollama_UsesArrayInputAndPreservesOrder()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        float[] first = CreateVector(2560, 0f);
        float[] second = CreateVector(2560, 10f);
        TestDelegatingHandler handler = new(async (request, _) =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return CreateJsonResponse(HttpStatusCode.OK, CreateOllamaResponse(first, second));
        });
        EmbeddingClient client = new(
            CreateHttpClientFactory(handler),
            CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret),
            CreateConfiguration(),
            CreateHostEnvironment(),
            CreateTokenProvider(TestAccessToken));

        IReadOnlyList<float[]> result = await client.GenerateBatchAsync(
            ["first text", "second text"],
            TenantId,
            CreateOllamaConfig(),
            CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0][0].ShouldBe(first[0]);
        result[1][0].ShouldBe(second[0]);
        capturedRequest.ShouldNotBeNull();
        capturedRequest.RequestUri!.ToString().ShouldBe("https://llm.tache.ai/api/embed");
        capturedRequest.Headers.Authorization.ShouldNotBeNull();
        capturedRequest.Headers.Authorization.Parameter.ShouldBe(TestAccessToken);

        using JsonDocument body = JsonDocument.Parse(capturedBody!);
        body.RootElement.GetProperty("model").GetString().ShouldBe("qwen3-embedding:4b");
        JsonElement input = body.RootElement.GetProperty("input");
        input.ValueKind.ShouldBe(JsonValueKind.Array);
        input[0].GetString().ShouldBe("first text");
        input[1].GetString().ShouldBe("second text");
    }

    [Fact]
    public async Task GenerateBatchAsync_MixedCaseProvider_ResolvesStrategyCaseInsensitively()
    {
        HttpRequestMessage? capturedRequest = null;
        TestDelegatingHandler handler = new((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(CreateJsonResponse(
                HttpStatusCode.OK,
                CreateGoogleBatchResponse(CreateVector(768, 0f))));
        });
        EmbeddingClient client = new(
            CreateHttpClientFactory(handler),
            CreateDaprClientWithSecret("google-embedding-api-key", TestApiKey),
            CreateConfiguration(),
            CreateHostEnvironment());

        IReadOnlyList<float[]> result = await client.GenerateBatchAsync(
            ["first text"],
            TenantId,
            EmbeddingProviderDefaults.Google() with { Provider = "GOOGLE" },
            CancellationToken.None);

        result.Count.ShouldBe(1);
        capturedRequest.ShouldNotBeNull();
        capturedRequest.RequestUri!.ToString().ShouldContain(":batchEmbedContents");
    }

    [Fact]
    public async Task GenerateBatchAsync_ResponseCountMismatch_ThrowsEmbeddingApiException()
    {
        EmbeddingClient client = new(
            CreateHttpClientFactory(HttpStatusCode.OK, CreateGoogleBatchResponse(CreateVector(768, 0f))),
            CreateDaprClientWithSecret("google-embedding-api-key", TestApiKey),
            CreateConfiguration(),
            CreateHostEnvironment());

        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateBatchAsync(
                ["first text", "second text"],
                TenantId,
                EmbeddingProviderDefaults.Google(),
                CancellationToken.None));

        ex.Message.ShouldContain("Expected 2 embeddings but received 1");
    }

    [Fact]
    public async Task GenerateBatchAsync_FakeEmbedding_ReturnsDeterministicVectorPerInput()
    {
        EmbeddingClient client = new(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<DaprClient>(),
            CreateConfiguration(useFakeEmbedding: true),
            CreateHostEnvironment());

        IReadOnlyList<float[]> first = await client.GenerateBatchAsync(
            ["alpha", "beta"],
            TenantId,
            EmbeddingProviderDefaults.Google(),
            CancellationToken.None);
        IReadOnlyList<float[]> second = await client.GenerateBatchAsync(
            ["alpha", "beta"],
            TenantId,
            EmbeddingProviderDefaults.Google(),
            CancellationToken.None);

        first.Count.ShouldBe(2);
        first[0].ShouldBe(second[0]);
        first[1].ShouldBe(second[1]);
        first[0].ShouldNotBe(first[1]);
    }

    [Fact]
    public async Task GenerateBatchAsync_EmptyInput_ThrowsBeforeSecretLookup()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        EmbeddingClient client = new(
            Substitute.For<IHttpClientFactory>(),
            daprClient,
            CreateConfiguration(),
            CreateHostEnvironment());

        await Should.ThrowAsync<ArgumentException>(
            () => client.GenerateBatchAsync([], TenantId, EmbeddingProviderDefaults.Google(), CancellationToken.None));
        await daprClient.DidNotReceive().GetSecretAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateBatchAsync_NullItem_ThrowsBeforeSecretLookup()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        EmbeddingClient client = new(
            Substitute.For<IHttpClientFactory>(),
            daprClient,
            CreateConfiguration(),
            CreateHostEnvironment());

        await Should.ThrowAsync<ArgumentException>(
            () => client.GenerateBatchAsync(
                ["valid text", null!],
                TenantId,
                EmbeddingProviderDefaults.Google(),
                CancellationToken.None));
        await daprClient.DidNotReceive().GetSecretAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateBatchAsync_ErrorBody_RedactsEverySubmittedInput()
    {
        string firstText = "private first case note";
        string secondText = "private second case note";
        string response = $$"""{"error":"{{firstText}} {{secondText}} {{TestApiKey}}"}""";
        EmbeddingClient client = new(
            CreateHttpClientFactory(HttpStatusCode.InternalServerError, response),
            CreateDaprClientWithSecret("google-embedding-api-key", TestApiKey),
            CreateConfiguration(),
            CreateHostEnvironment());

        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateBatchAsync(
                [firstText, secondText],
                TenantId,
                EmbeddingProviderDefaults.Google(),
                CancellationToken.None));

        ex.ResponseBody.ShouldNotBeNull();
        ex.ResponseBody.ShouldNotContain(firstText);
        ex.ResponseBody.ShouldNotContain(secondText);
        ex.ResponseBody.ShouldNotContain(TestApiKey);
    }

    [Fact]
    public async Task GenerateBatchAsync_Ollama_ResponseCountMismatch_ThrowsEmbeddingApiException()
    {
        EmbeddingClient client = new(
            CreateHttpClientFactory(HttpStatusCode.OK, CreateOllamaResponse(CreateVector(2560, 0f))),
            CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret),
            CreateConfiguration(),
            CreateHostEnvironment(),
            CreateTokenProvider(TestAccessToken));

        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateBatchAsync(
                ["first text", "second text"],
                TenantId,
                CreateOllamaConfig(),
                CancellationToken.None));

        ex.Message.ShouldContain("Expected 2 embeddings but received 1");
        ex.Message.ShouldNotContain("Google");
    }

    [Fact]
    public async Task GenerateBatchAsync_Google_DimensionMismatch_ThrowsEmbeddingApiException()
    {
        EmbeddingClient client = new(
            CreateHttpClientFactory(HttpStatusCode.OK, CreateGoogleBatchResponse(CreateVector(100, 0f), CreateVector(100, 10f))),
            CreateDaprClientWithSecret("google-embedding-api-key", TestApiKey),
            CreateConfiguration(),
            CreateHostEnvironment());

        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateBatchAsync(
                ["first text", "second text"],
                TenantId,
                EmbeddingProviderDefaults.Google(),
                CancellationToken.None));

        ex.Message.ShouldContain("768");
        ex.Message.ShouldContain("100");
    }

    [Fact]
    public async Task GenerateBatchAsync_Ollama_DimensionMismatch_ThrowsEmbeddingApiException()
    {
        EmbeddingClient client = new(
            CreateHttpClientFactory(HttpStatusCode.OK, CreateOllamaResponse(CreateVector(12, 0f), CreateVector(12, 10f))),
            CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret),
            CreateConfiguration(),
            CreateHostEnvironment(),
            CreateTokenProvider(TestAccessToken));

        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateBatchAsync(
                ["first text", "second text"],
                TenantId,
                CreateOllamaConfig(),
                CancellationToken.None));

        ex.Message.ShouldContain("2560");
        ex.Message.ShouldContain("12");
    }

    [Fact]
    public async Task GenerateBatchAsync_Google_MalformedJson_ThrowsEmbeddingApiException()
    {
        EmbeddingClient client = new(
            CreateHttpClientFactory(HttpStatusCode.OK, """{"unexpected":"shape"}"""),
            CreateDaprClientWithSecret("google-embedding-api-key", TestApiKey),
            CreateConfiguration(),
            CreateHostEnvironment());

        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateBatchAsync(
                ["first text", "second text"],
                TenantId,
                EmbeddingProviderDefaults.Google(),
                CancellationToken.None));

        ex.Message.ShouldContain("embeddings");
    }

    [Fact]
    public async Task GenerateBatchAsync_Ollama_NonNumericVector_ThrowsEmbeddingApiException()
    {
        EmbeddingClient client = new(
            CreateHttpClientFactory(HttpStatusCode.OK, """{"embeddings":[["bad"],[0.1]]}"""),
            CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret),
            CreateConfiguration(),
            CreateHostEnvironment(),
            CreateTokenProvider(TestAccessToken));

        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateBatchAsync(
                ["first text", "second text"],
                TenantId,
                CreateOllamaConfig(),
                CancellationToken.None));

        ex.Message.ShouldContain("invalid JSON or non-numeric vector values");
    }

    [Fact]
    public async Task GenerateBatchAsync_TooManyRequests_ThrowsRateLimitExceptionWithRetryAfter()
    {
        TestDelegatingHandler handler = new((_, _) =>
        {
            HttpResponseMessage response = new(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("rate limited", Encoding.UTF8, "text/plain"),
            };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(42));
            return Task.FromResult(response);
        });
        EmbeddingClient client = new(
            CreateHttpClientFactory(handler),
            CreateDaprClientWithSecret("google-embedding-api-key", TestApiKey),
            CreateConfiguration(),
            CreateHostEnvironment());

        EmbeddingRateLimitException ex = await Should.ThrowAsync<EmbeddingRateLimitException>(
            () => client.GenerateBatchAsync(
                ["first text", "second text"],
                TenantId,
                EmbeddingProviderDefaults.Google(),
                CancellationToken.None));

        ex.TenantId.ShouldBe(TenantId);
        ex.RetryAfterSeconds.ShouldBe(42);
    }

    [Fact]
    public async Task GenerateBatchAsync_CallerCancellationDuringSend_ThrowsOperationCanceledException()
    {
        using CancellationTokenSource cts = new();
        TestDelegatingHandler handler = new((_, _) =>
        {
            cts.Cancel();
            return Task.FromCanceled<HttpResponseMessage>(cts.Token);
        });
        EmbeddingClient client = new(
            CreateHttpClientFactory(handler),
            CreateDaprClientWithSecret("google-embedding-api-key", TestApiKey),
            CreateConfiguration(),
            CreateHostEnvironment());

        await Should.ThrowAsync<OperationCanceledException>(
            () => client.GenerateBatchAsync(
                ["first text", "second text"],
                TenantId,
                EmbeddingProviderDefaults.Google(),
                cts.Token));
    }

    [Fact]
    public async Task GenerateBatchAsync_ResponseContentReadFailure_WrappedInEmbeddingApiException()
    {
        TestDelegatingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ThrowingContent(new IOException("simulated batch response stream reset")),
            }));
        EmbeddingClient client = new(
            CreateHttpClientFactory(handler),
            CreateDaprClientWithSecret("google-embedding-api-key", TestApiKey),
            CreateConfiguration(),
            CreateHostEnvironment());

        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateBatchAsync(
                ["first text", "second text"],
                TenantId,
                EmbeddingProviderDefaults.Google(),
                CancellationToken.None));

        ex.InnerException.ShouldBeOfType<HttpRequestException>();
        ex.Message.ShouldContain("Google");
    }

    [Fact]
    public async Task GenerateBatchAsync_UnsupportedProvider_ThrowsBeforeSecretLookup()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        EmbeddingClient client = new(
            Substitute.For<IHttpClientFactory>(),
            daprClient,
            CreateConfiguration(),
            CreateHostEnvironment());

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            () => client.GenerateBatchAsync(
                ["first text"],
                TenantId,
                EmbeddingProviderDefaults.Google() with { Provider = "openai" },
                CancellationToken.None));

        ex.Message.ShouldContain("google");
        ex.Message.ShouldContain("ollama");
        await daprClient.DidNotReceive().GetSecretAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateBatchAsync_WhitespaceItem_ThrowsBeforeSecretLookup(string badItem)
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        EmbeddingClient client = new(
            Substitute.For<IHttpClientFactory>(),
            daprClient,
            CreateConfiguration(),
            CreateHostEnvironment());

        await Should.ThrowAsync<ArgumentException>(
            () => client.GenerateBatchAsync(
                ["valid text", badItem],
                TenantId,
                EmbeddingProviderDefaults.Google(),
                CancellationToken.None));
        await daprClient.DidNotReceive().GetSecretAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateBatchAsync_Google_Unauthorized_RefreshesApiKeyAndRetriesOnce()
    {
        int requestCount = 0;
        List<string> apiKeys = [];
        float[] first = CreateVector(768, 0f);
        float[] second = CreateVector(768, 10f);
        TestDelegatingHandler handler = new((request, _) =>
        {
            requestCount++;
            apiKeys.Add(request.Headers.GetValues("x-goog-api-key").Single());
            return Task.FromResult(requestCount == 1
                ? CreateJsonResponse(HttpStatusCode.Unauthorized, "stale key")
                : CreateJsonResponse(HttpStatusCode.OK, CreateGoogleBatchResponse(first, second)));
        });
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetSecretAsync(
                "secretstore",
                "google-embedding-api-key",
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new Dictionary<string, string> { ["google-embedding-api-key"] = "old-key" }),
                Task.FromResult(new Dictionary<string, string> { ["google-embedding-api-key"] = "new-key" }));
        EmbeddingClient client = new(
            CreateHttpClientFactory(handler),
            daprClient,
            CreateConfiguration(),
            CreateHostEnvironment());

        IReadOnlyList<float[]> result = await client.GenerateBatchAsync(
            ["first text", "second text"],
            TenantId,
            EmbeddingProviderDefaults.Google(),
            CancellationToken.None);

        result.Count.ShouldBe(2);
        requestCount.ShouldBe(2);
        apiKeys.ShouldBe(["old-key", "new-key"]);
        await daprClient.Received(2).GetSecretAsync(
            "secretstore",
            "google-embedding-api-key",
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateBatchAsync_Ollama_Unauthorized_RefreshesTokenAndRetriesOnce()
    {
        int requestCount = 0;
        List<string> authorizationHeaders = [];
        List<string> requestBodies = [];
        float[] first = CreateVector(2560, 0f);
        float[] second = CreateVector(2560, 10f);
        TestDelegatingHandler handler = new(async (request, _) =>
        {
            requestCount++;
            authorizationHeaders.Add(request.Headers.Authorization!.Parameter!);
            requestBodies.Add(await request.Content!.ReadAsStringAsync());
            return requestCount == 1
                ? CreateJsonResponse(HttpStatusCode.Forbidden, "expired")
                : CreateJsonResponse(HttpStatusCode.OK, CreateOllamaResponse(first, second));
        });
        IOidcTokenProvider tokenProvider = Substitute.For<IOidcTokenProvider>();
        tokenProvider.GetAccessTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("old-token");
        tokenProvider.InvalidateAndRefreshAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("new-token");
        EmbeddingClient client = new(
            CreateHttpClientFactory(handler),
            CreateDaprClientWithSecret("memories-embedding-client-secret", TestClientSecret),
            CreateConfiguration(),
            CreateHostEnvironment(),
            tokenProvider);

        IReadOnlyList<float[]> result = await client.GenerateBatchAsync(
            ["first text", "second text"],
            TenantId,
            CreateOllamaConfig(),
            CancellationToken.None);

        result.Count.ShouldBe(2);
        requestCount.ShouldBe(2);
        authorizationHeaders.ShouldBe(["old-token", "new-token"]);
        requestBodies[1].ShouldBe(requestBodies[0]);
        await tokenProvider.Received(1).InvalidateAndRefreshAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    private static float[] CreateVector(int dimensions, float offset)
    {
        float[] vector = new float[dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            vector[i] = offset + (i * 0.001f);
        }

        return vector;
    }

    private static string CreateGoogleBatchResponse(params float[][] values)
        => JsonSerializer.Serialize(new
        {
            embeddings = values.Select(static value => new { values = value }).ToArray(),
        });

    private static string CreateOllamaResponse(params float[][] values)
        => JsonSerializer.Serialize(new { embeddings = values });

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string responseBody)
        => new(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
        };

    private static IHttpClientFactory CreateHttpClientFactory(HttpStatusCode statusCode, string responseBody)
    {
        TestDelegatingHandler handler = new((_, _) => Task.FromResult(CreateJsonResponse(statusCode, responseBody)));
        return CreateHttpClientFactory(handler);
    }

    private static IHttpClientFactory CreateHttpClientFactory(TestDelegatingHandler handler)
    {
        HttpClient httpClient = new(handler);
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("EmbeddingClient").Returns(httpClient);
        return factory;
    }

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

    private static IOidcTokenProvider CreateTokenProvider(string accessToken)
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
            .Returns(accessToken);
        return tokenProvider;
    }

    private static TenantEmbeddingConfig CreateOllamaConfig()
        => EmbeddingProviderDefaults.Ollama() with
        {
            BaseUrl = "https://llm.tache.ai/",
            OidcTokenEndpoint = "https://auth.tache.ai/realms/tache/protocol/openid-connect/token",
            OidcClientId = "memories-embedding",
            OidcScope = "openid",
        };

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
}
