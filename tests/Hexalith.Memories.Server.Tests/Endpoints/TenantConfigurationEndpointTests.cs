// <copyright file="TenantConfigurationEndpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Tenants;

using Microsoft.AspNetCore.Http;

using Shouldly;

/// <summary>
/// Story 5.5 AC3 tests. Endpoint behavior is wired through minimal-API delegates in
/// <c>Program.cs</c>; these tests verify the composable pieces (input validation, shape of
/// <see cref="TenantStatusGuard.ToHttpResult(ErrorResponse)"/>, contract serialization) so the
/// delegate stays thin and the behavior is protected.
/// </summary>
public class TenantConfigurationEndpointTests
{
    // ToHttpResult mutation-guard (Task 5.2) — parameterized over every TENANT_* code.
    // Protects 5-4's fix: any change that routes a non-not-found code to 404, or vice versa, fails.
    [Theory]
    [InlineData("TENANT_NOT_FOUND", StatusCodes.Status404NotFound)]
    [InlineData("TENANT_DELETING", StatusCodes.Status409Conflict)]
    [InlineData("TENANT_PROVISIONING", StatusCodes.Status409Conflict)]
    [InlineData("TENANT_FAILED", StatusCodes.Status409Conflict)]
    [InlineData("TENANT_UNAVAILABLE", StatusCodes.Status409Conflict)]
    public void ToHttpResult_RoutesStatusCodesCorrectly(string code, int expectedStatus)
    {
        ErrorResponse error = new(code, $"{code} message", "suggestion");

        IResult result = TenantStatusGuard.ToHttpResult(error);

        result.ShouldNotBeNull();
        int actualStatus = result switch
        {
            Microsoft.AspNetCore.Http.HttpResults.NotFound<ErrorResponse> => StatusCodes.Status404NotFound,
            Microsoft.AspNetCore.Http.HttpResults.Conflict<ErrorResponse> => StatusCodes.Status409Conflict,
            _ => -1,
        };
        actualStatus.ShouldBe(expectedStatus);
    }

    [Fact]
    public void TenantUpdateInput_SerializesAndDeserializesDisplayNameRoundTrip()
    {
        TenantUpdateInput input = new("New Display Name");

        string json = JsonSerializer.Serialize(input, MemoriesJsonContext.Options);
        TenantUpdateInput? deserialized = JsonSerializer.Deserialize<TenantUpdateInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.DisplayName.ShouldBe("New Display Name");
        json.ShouldContain("\"displayName\"");
    }

    [Fact]
    public void TenantSummary_SerializesWithAllRequiredFields()
    {
        TenantSummary summary = new()
        {
            Id = "acme",
            DisplayName = "Acme Corp",
            Status = TenantStatus.Active,
            CreatedAt = new DateTimeOffset(2026, 4, 14, 0, 0, 0, TimeSpan.Zero),
            MemoryUnitCount = 42L,
            IndexSizes = new TenantIndexSizes(100, 100, 50),
            IndexStatus = new TenantIndexStatus(IndexHealth.Ready, IndexHealth.Ready, IndexHealth.Ready),
            ReindexRequired = false,
            LastActivityAt = new DateTimeOffset(2026, 4, 13, 12, 0, 0, TimeSpan.Zero),
        };

        string json = JsonSerializer.Serialize(summary, MemoriesJsonContext.Options);
        TenantSummary? deserialized = JsonSerializer.Deserialize<TenantSummary>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe("acme");
        deserialized.MemoryUnitCount.ShouldBe(42L);
        deserialized.IndexSizes.RediSearchKeyCount.ShouldBe(100L);
        deserialized.IndexStatus.FalkorDb.ShouldBe(IndexHealth.Ready);
        deserialized.ReindexRequired.ShouldBeFalse();
        deserialized.LastActivityAt.ShouldNotBeNull();
    }

    [Fact]
    public void TenantSummary_NullableCountsSerializeAsNull()
    {
        TenantSummary summary = new()
        {
            Id = "acme",
            DisplayName = "Acme",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            MemoryUnitCount = null,
            IndexSizes = new TenantIndexSizes(null, null, null),
            IndexStatus = new TenantIndexStatus(IndexHealth.Unknown, IndexHealth.Unknown, IndexHealth.Unknown),
            ReindexRequired = false,
            LastActivityAt = null,
        };

        string json = JsonSerializer.Serialize(summary, MemoriesJsonContext.Options);
        json.ShouldContain("\"memoryUnitCount\":null");
        json.ShouldContain("\"lastActivityAt\":null");
        json.ShouldContain("\"rediSearchKeyCount\":null");
    }

    [Fact]
    public void IndexHealth_SerializesAsCamelCaseString()
    {
        string json = JsonSerializer.Serialize(new TenantIndexStatus(
            IndexHealth.Ready,
            IndexHealth.Missing,
            IndexHealth.Unknown), MemoriesJsonContext.Options);

        json.ShouldContain("\"rediSearch\":\"ready\"");
        json.ShouldContain("\"redisVector\":\"missing\"");
        json.ShouldContain("\"falkorDb\":\"unknown\"");
    }

    [Fact]
    public void TenantConfigurationView_EmbedsFullEmbeddingConfig_NotProjected()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();
        TenantConfigurationView view = new()
        {
            Id = "acme",
            DisplayName = "Acme",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            EmbeddingConfig = config,
            IndexStatus = new TenantIndexStatus(IndexHealth.Ready, IndexHealth.Ready, IndexHealth.Ready),
        };

        string json = JsonSerializer.Serialize(view, MemoriesJsonContext.Options);
        // apiSecretKeyName is non-sensitive and should appear (Amendment C).
        json.ShouldContain("\"apiSecretKeyName\":\"google-embedding-api-key\"");
        json.ShouldContain("\"provider\":\"google\"");
        json.ShouldContain("\"model\":\"gemini-embedding-001\"");
    }
}
