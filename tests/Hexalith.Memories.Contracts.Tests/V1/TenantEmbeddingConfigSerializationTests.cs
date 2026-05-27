// <copyright file="TenantEmbeddingConfigSerializationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TenantEmbeddingConfigSerializationTests
{
    [Fact]
    public void RoundTrip_AllFieldsPopulated_ShouldProduceIdenticalJson()
    {
        TenantEmbeddingConfig original = CreateFullConfig();

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantEmbeddingConfig? deserialized = JsonSerializer.Deserialize<TenantEmbeddingConfig>(json, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json);
    }

    [Fact]
    public void RoundTrip_ReindexRequiredTrue_ShouldPreserve()
    {
        TenantEmbeddingConfig original = CreateFullConfig() with { ReindexRequired = true };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"reindexRequired\":true");

        TenantEmbeddingConfig? deserialized = JsonSerializer.Deserialize<TenantEmbeddingConfig>(json, MemoriesJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.ReindexRequired.ShouldBeTrue();
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        TenantEmbeddingConfig config = CreateFullConfig();

        string json = JsonSerializer.Serialize(config, MemoriesJsonContext.Options);

        json.ShouldContain("\"baseUrl\":");
        json.ShouldContain("\"authMode\":");
        json.ShouldContain("\"oidcTokenEndpoint\":");
        json.ShouldContain("\"oidcClientId\":");
        json.ShouldContain("\"oidcScope\":");
        json.ShouldContain("\"provider\":");
        json.ShouldContain("\"model\":");
        json.ShouldContain("\"dimensions\":");
        json.ShouldContain("\"rateLimitPerMinute\":");
        json.ShouldContain("\"apiSecretKeyName\":");
        json.ShouldContain("\"reindexRequired\":");
    }

    [Fact]
    public void RoundTrip_AllFieldValues_ShouldPreserve()
    {
        TenantEmbeddingConfig original = CreateFullConfig();

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantEmbeddingConfig? deserialized = JsonSerializer.Deserialize<TenantEmbeddingConfig>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Provider.ShouldBe("google");
        deserialized.Model.ShouldBe("gemini-embedding-001");
        deserialized.Dimensions.ShouldBe(768);
        deserialized.RateLimitPerMinute.ShouldBe(1500);
        deserialized.ApiSecretKeyName.ShouldBe("google-embedding-api-key");
        deserialized.ReindexRequired.ShouldBeFalse();
    }

    [Fact]
    public void RoundTrip_OllamaOidcFields_ShouldPreserveAllValues()
    {
        TenantEmbeddingConfig original = new()
        {
            Provider = "ollama",
            Model = "qwen3-embedding:4b",
            Dimensions = 2560,
            RateLimitPerMinute = 6000,
            ApiSecretKeyName = "memories-embedding-client-secret",
            ReindexRequired = false,
            BaseUrl = "https://llm.tache.ai",
            AuthMode = "oidc-client-credentials",
            OidcTokenEndpoint = "https://auth.tache.ai/realms/tache/protocol/openid-connect/token",
            OidcClientId = "memories-embedding",
            OidcScope = "openid",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantEmbeddingConfig? deserialized = JsonSerializer.Deserialize<TenantEmbeddingConfig>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.BaseUrl.ShouldBe("https://llm.tache.ai");
        deserialized.AuthMode.ShouldBe("oidc-client-credentials");
        deserialized.OidcTokenEndpoint.ShouldBe("https://auth.tache.ai/realms/tache/protocol/openid-connect/token");
        deserialized.OidcClientId.ShouldBe("memories-embedding");
        deserialized.OidcScope.ShouldBe("openid");
    }

    [Fact]
    public void Deserialize_LegacyGoogleJson_ShouldDefaultNewFields()
    {
        const string Json = """
            {
              "provider": "google",
              "model": "gemini-embedding-001",
              "dimensions": 768,
              "rateLimitPerMinute": 1500,
              "apiSecretKeyName": "google-embedding-api-key",
              "reindexRequired": false
            }
            """;

        TenantEmbeddingConfig? deserialized = JsonSerializer.Deserialize<TenantEmbeddingConfig>(Json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Provider.ShouldBe("google");
        deserialized.BaseUrl.ShouldBeNull();
        deserialized.AuthMode.ShouldBe("api-key");
        deserialized.OidcTokenEndpoint.ShouldBeNull();
        deserialized.OidcClientId.ShouldBeNull();
        deserialized.OidcScope.ShouldBeNull();
    }

    private static TenantEmbeddingConfig CreateFullConfig() => new()
    {
        Provider = "google",
        Model = "gemini-embedding-001",
        Dimensions = 768,
        RateLimitPerMinute = 1500,
        ApiSecretKeyName = "google-embedding-api-key",
        ReindexRequired = false,
        BaseUrl = "https://example.test/embeddings",
        AuthMode = "api-key",
        OidcTokenEndpoint = null,
        OidcClientId = null,
        OidcScope = null,
    };
}
