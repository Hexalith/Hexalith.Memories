// <copyright file="TenantEmbeddingConfigEndpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using Shouldly;

/// <summary>
/// ATDD acceptance tests for tenant embedding configuration REST endpoints (Story 1.7, AC #1, #4).
/// TDD Red Phase: These tests define expected HTTP behavior for GET/PUT endpoints.
/// Remove Skip attributes once endpoints are implemented.
/// </summary>
public class TenantEmbeddingConfigEndpointTests
{
    [Fact(Skip = "TDD Red Phase — Story 1.7: REST endpoints not yet implemented")]
    public void GetEmbeddingConfig_UnconfiguredTenant_ShouldReturnDefaultConfig()
    {
        // Arrange — AC #1: GET /api/tenants/{tenantId}/embedding-config
        // Setup TenantConfigurationActor mock returning default

        // Act
        // var response = await httpClient.GetAsync("/api/tenants/test-tenant/embedding-config");

        // Assert
        // response.StatusCode.ShouldBe(HttpStatusCode.OK);
        // var config = await response.Content.ReadFromJsonAsync<TenantEmbeddingConfig>();
        // config.Provider.ShouldBe("google");
        // config.Model.ShouldBe("gemini-embedding-001");
        // config.Dimensions.ShouldBe(768);
        // config.RateLimitPerMinute.ShouldBe(1500);
        throw new NotImplementedException("TDD Red Phase — implement GET endpoint");
    }

    [Fact(Skip = "TDD Red Phase — Story 1.7: REST endpoints not yet implemented")]
    public void PutEmbeddingConfig_ConfigChangeWithoutForceReindex_ShouldReturn409Conflict()
    {
        // Arrange — AC #4: change not silently applied without acknowledgment
        // Existing config: gemini-embedding-001/768
        // New config: different-model/3072
        // forceReindex query param = false (or absent)

        // Act
        // var response = await httpClient.PutAsJsonAsync(
        //     "/api/tenants/test-tenant/embedding-config",
        //     newConfig);

        // Assert — 409 with structured error body
        // response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        // var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        // error.GetProperty("error").GetString().ShouldBe("EmbeddingConfigChangeRequired");
        // error.TryGetProperty("currentConfig", out _).ShouldBeTrue();
        // error.TryGetProperty("proposedConfig", out _).ShouldBeTrue();
        // error.TryGetProperty("affectedFields", out _).ShouldBeTrue();
        throw new NotImplementedException("TDD Red Phase — implement PUT endpoint with 409 conflict");
    }

    [Fact(Skip = "TDD Red Phase — Story 1.7: REST endpoints not yet implemented")]
    public void PutEmbeddingConfig_WithForceReindex_ShouldReturn200()
    {
        // Arrange — AC #4: forceReindex=true bypasses warning
        // New config with changed provider/model/dimensions

        // Act
        // var response = await httpClient.PutAsJsonAsync(
        //     "/api/tenants/test-tenant/embedding-config?forceReindex=true",
        //     newConfig);

        // Assert
        // response.StatusCode.ShouldBe(HttpStatusCode.OK);
        throw new NotImplementedException("TDD Red Phase — implement PUT endpoint with forceReindex");
    }

    [Fact(Skip = "TDD Red Phase — Story 1.7: REST endpoints not yet implemented")]
    public void PutEmbeddingConfig_RateLimitOnlyChange_ShouldReturn200WithoutForceReindex()
    {
        // Arrange — rateLimitPerMinute change doesn't require forceReindex
        // Same provider, model, dimensions; only rateLimitPerMinute differs

        // Act
        // var response = await httpClient.PutAsJsonAsync(
        //     "/api/tenants/test-tenant/embedding-config",
        //     configWithNewRateLimit);

        // Assert
        // response.StatusCode.ShouldBe(HttpStatusCode.OK);
        throw new NotImplementedException("TDD Red Phase — implement PUT endpoint non-breaking change");
    }
}
