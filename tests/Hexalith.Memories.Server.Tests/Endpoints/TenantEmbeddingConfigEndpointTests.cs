// <copyright file="TenantEmbeddingConfigEndpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Shouldly;

public class TenantEmbeddingConfigEndpointTests
{
    [Fact]
    public void EmbeddingProviderDefaults_Validate_ValidConfig_ShouldNotThrow()
    {
        // Integration test for endpoint validation path
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();
        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void EmbeddingConfigChangeException_ShouldContainAllFields()
    {
        // Verify exception carries all data needed for 409 response
        TenantEmbeddingConfig current = EmbeddingProviderDefaults.Google();
        TenantEmbeddingConfig proposed = current with { Model = "different-model" };
        string[] affectedFields = ["model"];

        EmbeddingConfigChangeException ex = new("test-tenant", current, proposed, affectedFields);

        ex.TenantId.ShouldBe("test-tenant");
        ex.CurrentConfig.ShouldNotBeNull();
        ex.CurrentConfig.Model.ShouldBe("gemini-embedding-001");
        ex.ProposedConfig.ShouldNotBeNull();
        ex.ProposedConfig.Model.ShouldBe("different-model");
        ex.AffectedFields.ShouldContain("model");
        ex.Message.ShouldContain("reindex");
    }

    [Fact]
    public void ConflictResponse_ShouldSerializeWithCorrectStructure()
    {
        // Verify the conflict response shape matches AC #4 spec
        TenantEmbeddingConfig current = EmbeddingProviderDefaults.Google();
        TenantEmbeddingConfig proposed = current with { Dimensions = 3072 };

        var conflictBody = new
        {
            error = "EmbeddingConfigChangeRequired",
            message = "Embedding configuration change requires reindex",
            currentConfig = current,
            proposedConfig = proposed,
            affectedFields = new[] { "dimensions" },
        };

        string json = JsonSerializer.Serialize(conflictBody, MemoriesJsonContext.Options);
        using JsonDocument doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("error").GetString().ShouldBe("EmbeddingConfigChangeRequired");
        doc.RootElement.TryGetProperty("currentConfig", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("proposedConfig", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("affectedFields", out _).ShouldBeTrue();
    }

    [Fact]
    public void EmbeddingConfigResponse_OllamaOidcConfig_ShouldSerializeAllMetadataWithoutSecretValues()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama();

        string json = JsonSerializer.Serialize(config, MemoriesJsonContext.Options);
        using JsonDocument doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("provider").GetString().ShouldBe("ollama");
        doc.RootElement.GetProperty("model").GetString().ShouldBe("qwen3-embedding:4b");
        doc.RootElement.GetProperty("dimensions").GetInt32().ShouldBe(2560);
        doc.RootElement.GetProperty("rateLimitPerMinute").GetInt32().ShouldBe(6000);
        doc.RootElement.GetProperty("reindexRequired").GetBoolean().ShouldBeFalse();
        doc.RootElement.GetProperty("baseUrl").GetString().ShouldBe("https://llm.tache.ai");
        doc.RootElement.GetProperty("authMode").GetString().ShouldBe("oidc-client-credentials");
        doc.RootElement.GetProperty("oidcTokenEndpoint").GetString().ShouldBe("https://auth.tache.ai/realms/tache/protocol/openid-connect/token");
        doc.RootElement.GetProperty("oidcClientId").GetString().ShouldBe("memories-embedding");
        doc.RootElement.GetProperty("oidcScope").GetString().ShouldBe("openid");
        doc.RootElement.GetProperty("apiSecretKeyName").GetString().ShouldBe("memories-embedding-client-secret");
        json.ShouldNotContain("\"client_secret\":");
        json.ShouldNotContain("\"clientSecret\":");
        json.ShouldNotContain("\"oidcClientSecret\":");
        json.ShouldNotContain("\"oidc_client_secret\":");
        json.ShouldNotContain("super-secret-client-secret");
    }

    [Fact]
    public void ConflictResponse_OllamaOidcConfig_ShouldSerializeProposedMetadataWithoutSecretValues()
    {
        TenantEmbeddingConfig current = EmbeddingProviderDefaults.Ollama();
        TenantEmbeddingConfig proposed = current with { BaseUrl = "https://other-llm.tache.ai" };

        var conflictBody = new
        {
            error = "EmbeddingConfigChangeRequired",
            message = "Embedding configuration change requires reindex",
            currentConfig = current,
            proposedConfig = proposed,
            affectedFields = new[] { "baseUrl" },
        };

        string json = JsonSerializer.Serialize(conflictBody, MemoriesJsonContext.Options);
        using JsonDocument doc = JsonDocument.Parse(json);

        JsonElement proposedConfig = doc.RootElement.GetProperty("proposedConfig");
        proposedConfig.GetProperty("baseUrl").GetString().ShouldBe("https://other-llm.tache.ai");
        proposedConfig.GetProperty("authMode").GetString().ShouldBe("oidc-client-credentials");
        proposedConfig.GetProperty("oidcTokenEndpoint").GetString().ShouldBe("https://auth.tache.ai/realms/tache/protocol/openid-connect/token");
        proposedConfig.GetProperty("oidcClientId").GetString().ShouldBe("memories-embedding");
        proposedConfig.GetProperty("oidcScope").GetString().ShouldBe("openid");
        proposedConfig.GetProperty("apiSecretKeyName").GetString().ShouldBe("memories-embedding-client-secret");
        doc.RootElement.GetProperty("affectedFields")[0].GetString().ShouldBe("baseUrl");
        json.ShouldNotContain("\"client_secret\":");
        json.ShouldNotContain("\"clientSecret\":");
        json.ShouldNotContain("\"oidcClientSecret\":");
        json.ShouldNotContain("\"oidc_client_secret\":");
        json.ShouldNotContain("super-secret-client-secret");
    }

    [Fact]
    public void ConflictResponse_AffectedFields_ShouldSerializeAsArray()
    {
        // Verify affectedFields serializes properly
        var conflictBody = new
        {
            error = "EmbeddingConfigChangeRequired",
            affectedFields = new[] { "provider", "dimensions" },
        };

        string json = JsonSerializer.Serialize(conflictBody, MemoriesJsonContext.Options);
        using JsonDocument doc = JsonDocument.Parse(json);

        JsonElement fields = doc.RootElement.GetProperty("affectedFields");
        fields.GetArrayLength().ShouldBe(2);
        fields[0].GetString().ShouldBe("provider");
        fields[1].GetString().ShouldBe("dimensions");
    }
}
