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

    private static DaprClient CreateDaprClientWithSecret()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetSecretAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["google-embedding-api-key"] = TestApiKey });
        return daprClient;
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
