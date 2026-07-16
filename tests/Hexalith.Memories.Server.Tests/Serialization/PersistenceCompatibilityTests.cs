// <copyright file="PersistenceCompatibilityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Serialization;

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tenants;

using Shouldly;

/// <summary>Golden legacy-payload coverage for the server-owned persistence boundary.</summary>
public sealed class PersistenceCompatibilityTests
{
    [Fact]
    public void ServerOnlyPayloads_AreRegisteredInThePersistenceSourceGenerationContext()
    {
        Type[] expectedTypes =
        [
            typeof(BatchedGraphDeletionInput),
            typeof(BatchedGraphDeletionResult),
            typeof(CounterTransitionInput),
            typeof(ExtractionInput),
            typeof(ExtractionResult),
            typeof(FailedUnitInput),
            typeof(FetchUrlInput),
            typeof(UrlFetchResult),
            typeof(IndexInput),
            typeof(IndexResult),
            typeof(NaturalLanguageDescriptionInput),
            typeof(NaturalLanguageDescriptionResult),
            typeof(NaturalLanguageIndexInput),
            typeof(QueueNaturalLanguageEmbeddingRetryInput),
            typeof(FailedNaturalLanguageEmbeddingRecord),
            typeof(NaturalLanguageEmbeddingRetryInput),
            typeof(NaturalLanguageEmbeddingRetryResult),
            typeof(StoredTenantEmbeddingConfig),
            typeof(StoredFusionWeights),
            typeof(StoredTenantRegistryEntry),
            typeof(StoredCaseMember),
            typeof(StoredFailureDetails),
            typeof(StoredWorkflowPayloadReference),
            typeof(Dictionary<string, StoredMetadataField>),
        ];

        foreach (Type type in expectedTypes)
        {
            MemoriesPersistenceJsonSourceGenerationContext.Default.GetTypeInfo(type).ShouldNotBeNull(
                $"{type.Name} must be owned by the server persistence source-generation context.");
        }
    }

    [Fact]
    public void TenantConfiguration_LegacyJson_MapsAndRewritesWithoutShapeDrift()
    {
        const string Json = """
            {"provider":"ollama","model":"qwen3-embedding:4b","dimensions":2560,"rateLimitPerMinute":6000,"apiSecretKeyName":"embedding-secret","reindexRequired":true,"baseUrl":"https://llm.example","authMode":"oidc-client-credentials","oidcTokenEndpoint":"https://auth.example/token","oidcClientId":"memories","oidcScope":"openid"}
            """;

        StoredTenantEmbeddingConfig stored = Deserialize<StoredTenantEmbeddingConfig>(Json);
        TenantEmbeddingConfig contract = PersistenceModelMapper.ToContract(stored);
        StoredTenantEmbeddingConfig rewritten = PersistenceModelMapper.ToStored(contract);

        AssertEquivalentJson(Json, JsonSerializer.Serialize(rewritten, MemoriesPersistenceJsonContext.Options));
    }

    [Fact]
    public void TenantConfiguration_PreOidcJson_DefaultsAuthModeWithoutLosingValues()
    {
        const string Json = """
            {"provider":"google","model":"gemini-embedding-001","dimensions":768,"rateLimitPerMinute":1500,"apiSecretKeyName":"google-key","reindexRequired":false}
            """;

        StoredTenantEmbeddingConfig stored = Deserialize<StoredTenantEmbeddingConfig>(Json);
        TenantEmbeddingConfig contract = PersistenceModelMapper.ToContract(stored);

        contract.AuthMode.ShouldBe("api-key");
        contract.Provider.ShouldBe("google");
        contract.Model.ShouldBe("gemini-embedding-001");
        contract.Dimensions.ShouldBe(768);
        contract.RateLimitPerMinute.ShouldBe(1500);
        contract.ApiSecretKeyName.ShouldBe("google-key");
    }

    [Fact]
    public void FusionWeights_LegacyJson_MapsAndRewritesWithoutShapeDrift()
    {
        const string Json = """
            {"syntacticWeight":0.1,"semanticWeight":0.2,"graphWeight":0.3,"nlWeight":0.4}
            """;

        StoredFusionWeights stored = Deserialize<StoredFusionWeights>(Json);
        FusionWeights contract = PersistenceModelMapper.ToContract(stored);
        AssertEquivalentJson(
            Json,
            JsonSerializer.Serialize(PersistenceModelMapper.ToStored(contract), MemoriesPersistenceJsonContext.Options));
    }

    [Fact]
    public void FusionWeights_LegacyJsonWithMissingFields_KeepsDurableFallbacks()
    {
        const string Json = """
            {}
            """;

        StoredFusionWeights stored = Deserialize<StoredFusionWeights>(Json);
        FusionWeights contract = PersistenceModelMapper.ToContract(stored);

        contract.SyntacticWeight.ShouldBe(0.4);
        contract.SemanticWeight.ShouldBe(0.4);
        contract.GraphWeight.ShouldBe(0.2);
        contract.NlWeight.ShouldBe(0.2);
    }

    [Fact]
    public void TenantRegistry_LegacyJson_MapsAndRewritesWithoutShapeDrift()
    {
        const string Json = """
            {"tenant":{"id":"tenant-a","displayName":"Tenant A","status":"deleting","createdAt":"2026-07-01T12:00:00+00:00","embeddingProvider":"ollama","embeddingModel":"qwen3-embedding:4b"},"workflowInstanceId":"delete-tenant-a-1","lastUpdated":"2026-07-02T12:00:00+00:00"}
            """;

        StoredTenantRegistryEntry stored = Deserialize<StoredTenantRegistryEntry>(Json);
        TenantRegistryEntry contract = PersistenceModelMapper.ToContract(stored);
        AssertEquivalentJson(
            Json,
            JsonSerializer.Serialize(PersistenceModelMapper.ToStored(contract), MemoriesPersistenceJsonContext.Options));
    }

    [Fact]
    public void TenantRegistry_NullEmbedding_RewritesWithoutAddingExplicitNullKeys()
    {
        // Story 25.4: an embedding-unconfigured tenant persisted its registry row with the embedding keys ABSENT.
        // StoredTenantInfo must keep that legacy shape (WhenWritingNull) rather than emitting explicit nulls.
        const string Json = """
            {"tenant":{"id":"tenant-b","displayName":"Tenant B","status":"active","createdAt":"2026-07-01T12:00:00+00:00"},"workflowInstanceId":"provision-tenant-b-1","lastUpdated":"2026-07-02T12:00:00+00:00"}
            """;

        StoredTenantRegistryEntry stored = Deserialize<StoredTenantRegistryEntry>(Json);
        TenantRegistryEntry contract = PersistenceModelMapper.ToContract(stored);
        AssertEquivalentJson(
            Json,
            JsonSerializer.Serialize(PersistenceModelMapper.ToStored(contract), MemoriesPersistenceJsonContext.Options));
    }

    [Fact]
    public void CaseMember_LegacyJson_MapsAndRewritesWithoutShapeDrift()
    {
        const string Json = """
            {"memberId":"operator-a","memberType":"user","addedAt":"2026-07-01T12:00:00+00:00"}
            """;

        StoredCaseMember stored = Deserialize<StoredCaseMember>(Json);
        CaseMember contract = PersistenceModelMapper.ToContract(stored);
        AssertEquivalentJson(
            Json,
            JsonSerializer.Serialize(PersistenceModelMapper.ToStored(contract), MemoriesPersistenceJsonContext.Options));
    }

    [Fact]
    public void FailedUnitNestedPayloads_LegacyJson_MapAndRewriteWithoutShapeDrift()
    {
        const string FailureJson = """
            {"stage":"semantic","errorCode":"EMBEDDING_FAILED","retryCount":2,"errorMessage":"unavailable","lastRetryAt":"2026-07-01T12:00:00+00:00"}
            """;
        const string ReferenceJson = """
            {"id":"payload-1","sha256Hash":"abc123","byteLength":42,"contentKind":1,"tenantId":"tenant-a","memoryUnitId":"unit-a"}
            """;
        const string MetadataJson = """
            {"origin":{"value":"upload","origin":"human","confidence":1}}
            """;

        StoredFailureDetails failure = Deserialize<StoredFailureDetails>(FailureJson);
        StoredWorkflowPayloadReference reference = Deserialize<StoredWorkflowPayloadReference>(ReferenceJson);
        Dictionary<string, StoredMetadataField> metadata = Deserialize<Dictionary<string, StoredMetadataField>>(MetadataJson);

        AssertEquivalentJson(
            FailureJson,
            JsonSerializer.Serialize(
                PersistenceModelMapper.ToStored(PersistenceModelMapper.ToContract(failure)),
                MemoriesPersistenceJsonContext.Options));
        AssertEquivalentJson(
            ReferenceJson,
            JsonSerializer.Serialize(
                PersistenceModelMapper.ToStored(PersistenceModelMapper.ToContract(reference)),
                MemoriesPersistenceJsonContext.Options));
        AssertEquivalentJson(
            MetadataJson,
            JsonSerializer.Serialize(
                PersistenceModelMapper.ToStored(PersistenceModelMapper.ToContract(metadata)),
                MemoriesPersistenceJsonContext.Options));
    }

    private static T Deserialize<T>(string json)
        where T : class
    {
        T? value = JsonSerializer.Deserialize<T>(json, MemoriesPersistenceJsonContext.Options);
        value.ShouldNotBeNull();
        return value;
    }

    private static void AssertEquivalentJson(string expected, string actual)
    {
        JsonNode? expectedNode = JsonNode.Parse(expected);
        JsonNode? actualNode = JsonNode.Parse(actual);
        JsonNode.DeepEquals(expectedNode, actualNode).ShouldBeTrue($"Expected {expected}, actual {actual}.");
    }
}
