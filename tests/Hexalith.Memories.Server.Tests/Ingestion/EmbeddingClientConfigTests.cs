// <copyright file="EmbeddingClientConfigTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Shouldly;

/// <summary>
/// ATDD acceptance tests for refactored EmbeddingClient with configurable provider (Story 1.7, AC #2, #3).
/// TDD Red Phase: These tests validate that EmbeddingClient uses tenant config for endpoint URL,
/// model, dimensions, and output_dimensionality. Remove Skip attributes once Story 1.7 is implemented.
/// </summary>
public class EmbeddingClientConfigTests
{
    private const string TenantId = "test-tenant";

    [Fact(Skip = "TDD Red Phase — Story 1.7: EmbeddingClient not yet refactored for config")]
    public async Task GenerateAsync_ShouldIncludeOutputDimensionalityInRequest()
    {
        // Arrange — AC #2: output_dimensionality field required by gemini-embedding-001
        // Setup mock HTTP handler to capture request body
        // Setup config with dimensions=768

        // Act
        // await client.GenerateAsync("test text", TenantId, config, CancellationToken.None);

        // Assert — request JSON includes exact "output_dimensionality" field (snake_case!)
        // Google API silently ignores unknown fields and returns 3072-dim vectors if name is wrong
        // capturedRequestBody.ShouldContain("\"output_dimensionality\":768");
        throw new NotImplementedException("TDD Red Phase — implement output_dimensionality in request JSON");
    }

    [Fact(Skip = "TDD Red Phase — Story 1.7: EmbeddingClient not yet refactored for config")]
    public async Task GenerateAsync_ShouldUseConfiguredEndpointUrl()
    {
        // Arrange — AC #2: endpoint URL from config
        // gemini-embedding-001 uses /v1beta/ not /v1/
        // Setup mock handler to capture request URL

        // Act
        // await client.GenerateAsync("test text", TenantId, config, CancellationToken.None);

        // Assert — URL uses v1beta and correct model name
        // capturedUrl.ShouldBe(
        //     "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent");
        throw new NotImplementedException("TDD Red Phase — implement configurable endpoint URL");
    }

    [Fact(Skip = "TDD Red Phase — Story 1.7: EmbeddingClient not yet refactored for config")]
    public async Task GenerateAsync_ShouldValidateResponseDimensionsFromConfig()
    {
        // Arrange — AC #2: expected dimensions from config, not hardcoded 768
        // Setup config with dimensions=3072
        // Mock handler returns 768-dim vector (wrong!)

        // Act & Assert
        // await Should.ThrowAsync<EmbeddingApiException>(
        //     () => client.GenerateAsync("test", TenantId, config, CancellationToken.None));
        throw new NotImplementedException("TDD Red Phase — implement config-driven dimension validation");
    }

    [Fact(Skip = "TDD Red Phase — Story 1.7: EmbeddingClient not yet refactored for config")]
    public async Task GenerateAsync_TwoConcurrentTenants_ShouldRetrieveCorrectApiKeys()
    {
        // Arrange — AC #2: singleton with ConcurrentDictionary keyed by apiSecretKeyName
        // Tenant A: apiSecretKeyName = "key-tenant-a"
        // Tenant B: apiSecretKeyName = "key-tenant-b"
        // Mock DaprClient to return different keys per secret name

        // Act — call for both tenants
        // await client.GenerateAsync("text-a", "tenant-a", configA, CancellationToken.None);
        // await client.GenerateAsync("text-b", "tenant-b", configB, CancellationToken.None);

        // Assert — each request used the correct API key from the ConcurrentDictionary
        // daprClient.Received(1).GetSecretAsync("secretstore", "key-tenant-a", ...);
        // daprClient.Received(1).GetSecretAsync("secretstore", "key-tenant-b", ...);
        throw new NotImplementedException("TDD Red Phase — implement per-tenant API key caching");
    }

    [Fact(Skip = "TDD Red Phase — Story 1.7: EmbeddingClient not yet refactored for config")]
    public async Task GenerateAsync_ShouldCacheApiKeyBySecretKeyName()
    {
        // Arrange — singleton caches keys: second call should NOT hit DAPR secrets
        // Both calls use same apiSecretKeyName

        // Act — call twice with same config
        // await client.GenerateAsync("text1", TenantId, config, CancellationToken.None);
        // await client.GenerateAsync("text2", TenantId, config, CancellationToken.None);

        // Assert — secret retrieved only once
        // daprClient.Received(1).GetSecretAsync("secretstore", config.ApiSecretKeyName, ...);
        throw new NotImplementedException("TDD Red Phase — implement API key caching in ConcurrentDictionary");
    }
}
