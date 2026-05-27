// <copyright file="EmbeddingClientConfigTests.cs" company="ITANEO">
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

public class EmbeddingClientConfigTests
{
    private const string TenantId = "test-tenant";

    [Fact]
    public async Task GenerateAsync_ShouldIncludeOutputDimensionalityInRequest()
    {
        // Arrange
        string? capturedBody = null;
        float[] vector = CreateVector(768);
        TestDelegatingHandler handler = new((req, _) =>
        {
            capturedBody = req.Content!.ReadAsStringAsync().Result;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreateEmbeddingResponse(vector), Encoding.UTF8, "application/json"),
            });
        });
        IHttpClientFactory factory = CreateHttpClientFactory(handler);
        DaprClient daprClient = CreateDaprClientWithSecret("google-embedding-api-key");
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(factory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act
        await client.GenerateAsync("test text", TenantId, config, CancellationToken.None);

        // Assert — request JSON includes exact "output_dimensionality" field (snake_case!)
        capturedBody.ShouldNotBeNull();
        capturedBody.ShouldContain("\"output_dimensionality\":768");
    }

    [Fact]
    public async Task GenerateAsync_ShouldUseConfiguredEndpointUrl()
    {
        // Arrange
        string? capturedUrl = null;
        float[] vector = CreateVector(768);
        TestDelegatingHandler handler = new((req, _) =>
        {
            capturedUrl = req.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreateEmbeddingResponse(vector), Encoding.UTF8, "application/json"),
            });
        });
        IHttpClientFactory factory = CreateHttpClientFactory(handler);
        DaprClient daprClient = CreateDaprClientWithSecret("google-embedding-api-key");
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(factory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act
        await client.GenerateAsync("test text", TenantId, config, CancellationToken.None);

        // Assert — URL uses v1beta and correct model name
        capturedUrl.ShouldBe(
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent");
    }

    [Fact]
    public async Task GenerateAsync_ShouldValidateResponseDimensionsFromConfig()
    {
        // Arrange — config expects 3072 but API returns 768
        float[] shortVector = CreateVector(768);
        IHttpClientFactory factory = CreateSimpleHttpClientFactory(HttpStatusCode.OK, CreateEmbeddingResponse(shortVector));
        DaprClient daprClient = CreateDaprClientWithSecret("google-embedding-api-key");
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Dimensions = 3072 };

        EmbeddingClient client = new(factory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act & Assert
        EmbeddingApiException ex = await Should.ThrowAsync<EmbeddingApiException>(
            () => client.GenerateAsync("test", TenantId, config, CancellationToken.None));
        ex.Message.ShouldContain("3072");
        ex.Message.ShouldContain("768");
    }

    [Fact]
    public async Task GenerateAsync_TwoConcurrentTenants_ShouldRetrieveCorrectApiKeys()
    {
        // Arrange
        float[] vector = CreateVector(768);
        IHttpClientFactory factory = CreateSimpleHttpClientFactory(HttpStatusCode.OK, CreateEmbeddingResponse(vector));
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetSecretAsync("secretstore", "key-tenant-a", Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["key-tenant-a"] = "api-key-a" });
        daprClient.GetSecretAsync("secretstore", "key-tenant-b", Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["key-tenant-b"] = "api-key-b" });

        TenantEmbeddingConfig configA = EmbeddingProviderDefaults.Google() with { ApiSecretKeyName = "key-tenant-a" };
        TenantEmbeddingConfig configB = EmbeddingProviderDefaults.Google() with { ApiSecretKeyName = "key-tenant-b" };

        EmbeddingClient client = new(factory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act
        await client.GenerateAsync("text-a", "tenant-a", configA, CancellationToken.None);
        await client.GenerateAsync("text-b", "tenant-b", configB, CancellationToken.None);

        // Assert — each request used the correct API key
        await daprClient.Received(1).GetSecretAsync("secretstore", "key-tenant-a", Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
        await daprClient.Received(1).GetSecretAsync("secretstore", "key-tenant-b", Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ShouldCacheApiKeyBySecretKeyName()
    {
        // Arrange
        float[] vector = CreateVector(768);
        IHttpClientFactory factory = CreateSimpleHttpClientFactory(HttpStatusCode.OK, CreateEmbeddingResponse(vector));
        DaprClient daprClient = CreateDaprClientWithSecret("google-embedding-api-key");
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        EmbeddingClient client = new(factory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act — call twice with same config
        await client.GenerateAsync("text1", TenantId, config, CancellationToken.None);
        await client.GenerateAsync("text2", TenantId, config, CancellationToken.None);

        // Assert — secret retrieved only once
        await daprClient.Received(1).GetSecretAsync("secretstore", config.ApiSecretKeyName, Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_UnauthorizedResponse_ShouldRefreshCachedSecretAndRetry()
    {
        // Arrange
        int requestCount = 0;
        string? firstApiKey = null;
        string? secondApiKey = null;
        float[] vector = CreateVector(768);
        TestDelegatingHandler handler = new((req, _) =>
        {
            requestCount++;
            string apiKey = req.Headers.GetValues("x-goog-api-key").Single();

            if (requestCount == 1)
            {
                firstApiKey = apiKey;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("stale key", Encoding.UTF8, "text/plain"),
                });
            }

            secondApiKey = apiKey;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreateEmbeddingResponse(vector), Encoding.UTF8, "application/json"),
            });
        });
        IHttpClientFactory factory = CreateHttpClientFactory(handler);
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetSecretAsync(
                "secretstore",
                "google-embedding-api-key",
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new Dictionary<string, string> { ["google-embedding-api-key"] = "old-key" }),
                Task.FromResult(new Dictionary<string, string> { ["google-embedding-api-key"] = "new-key" }));

        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();
        EmbeddingClient client = new(factory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act
        float[] result = await client.GenerateAsync("test text", TenantId, config, CancellationToken.None);

        // Assert
        result.Length.ShouldBe(768);
        requestCount.ShouldBe(2);
        firstApiKey.ShouldBe("old-key");
        secondApiKey.ShouldBe("new-key");
        await daprClient.Received(2).GetSecretAsync(
            "secretstore",
            "google-embedding-api-key",
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_UnsupportedProvider_ShouldThrowArgumentException()
    {
        // Arrange
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Provider = "openai" };

        EmbeddingClient client = new(factory, daprClient, CreateConfiguration(), CreateHostEnvironment());

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => client.GenerateAsync("test text", TenantId, config, CancellationToken.None));
        await daprClient.DidNotReceive().GetSecretAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_FakeEmbedding_ShouldUseDimensionsFromConfig()
    {
        // Arrange
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Dimensions = 1536 };

        EmbeddingClient client = new(factory, daprClient, CreateConfiguration(useFakeEmbedding: true), CreateHostEnvironment());

        // Act
        float[] result = await client.GenerateAsync("test", TenantId, config, CancellationToken.None);

        // Assert — fake embedding uses configured dimensions, not hardcoded 768
        result.Length.ShouldBe(1536);
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
        => JsonSerializer.Serialize(new { embedding = new { values } });

    private static IHttpClientFactory CreateHttpClientFactory(TestDelegatingHandler handler)
    {
        HttpClient httpClient = new(handler);
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("EmbeddingClient").Returns(httpClient);
        return factory;
    }

    private static IHttpClientFactory CreateSimpleHttpClientFactory(HttpStatusCode statusCode, string responseBody)
    {
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("EmbeddingClient").Returns(_ =>
        {
            TestDelegatingHandler handler = new((_, _) =>
                Task.FromResult(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
                }));
            return new HttpClient(handler);
        });
        return factory;
    }

    private static DaprClient CreateDaprClientWithSecret(string secretKeyName)
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetSecretAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { [secretKeyName] = "test-api-key" });
        return daprClient;
    }

    private static IConfiguration CreateConfiguration(bool useFakeEmbedding = false)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memories:Testing:UseFakeEmbedding"] = useFakeEmbedding.ToString(),
            })
            .Build();

    private static IHostEnvironment CreateHostEnvironment()
    {
        IHostEnvironment env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Development");
        return env;
    }

    internal sealed class TestDelegatingHandler : DelegatingHandler
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
