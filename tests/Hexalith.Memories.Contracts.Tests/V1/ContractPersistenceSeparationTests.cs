// <copyright file="ContractPersistenceSeparationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Reflection;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Guards the V1 CLR/storage separation while retaining the legacy V1 JSON shape.</summary>
public sealed class ContractPersistenceSeparationTests
{
    [Fact]
    public void LegacyBackendNamedJson_RoundTripsThroughAxisNamedClrMembers()
    {
        const string Json = """
            {
              "sizes": {
                "rediSearchKeyCount": 11,
                "redisVectorKeyCount": 12,
                "falkorDbNodeCount": 13
              },
              "status": {
                "rediSearch": "ready",
                "redisVector": "degraded",
                "falkorDb": "missing"
              },
              "semantic": {
                "embeddingDimensions": 768,
                "vectorHashKey": "tenant-a:vec:unit-a"
              }
            }
            """;

        using JsonDocument document = JsonDocument.Parse(Json);
        TenantIndexSizes? sizes = document.RootElement.GetProperty("sizes")
            .Deserialize<TenantIndexSizes>(MemoriesJsonContext.Options);
        TenantIndexStatus? status = document.RootElement.GetProperty("status")
            .Deserialize<TenantIndexStatus>(MemoriesJsonContext.Options);
        ConsistencySemanticDetail? semantic = document.RootElement.GetProperty("semantic")
            .Deserialize<ConsistencySemanticDetail>(MemoriesJsonContext.Options);

        sizes.ShouldNotBeNull();
        sizes.SyntacticKeyCount.ShouldBe(11L);
        sizes.SemanticKeyCount.ShouldBe(12L);
        sizes.GraphNodeCount.ShouldBe(13L);
        status.ShouldNotBeNull();
        status.Syntactic.ShouldBe(IndexHealth.Ready);
        status.Semantic.ShouldBe(IndexHealth.Degraded);
        status.Graph.ShouldBe(IndexHealth.Missing);
        semantic.ShouldNotBeNull();
        semantic.EmbeddingDimensions.ShouldBe(768);
        semantic.SemanticIndexKey.ShouldBe("tenant-a:vec:unit-a");

        string sizesJson = JsonSerializer.Serialize(sizes, MemoriesJsonContext.Options);
        string statusJson = JsonSerializer.Serialize(status, MemoriesJsonContext.Options);
        string semanticJson = JsonSerializer.Serialize(semantic, MemoriesJsonContext.Options);
        sizesJson.ShouldContain("\"rediSearchKeyCount\":11", Shouldly.Case.Sensitive);
        sizesJson.ShouldContain("\"redisVectorKeyCount\":12", Shouldly.Case.Sensitive);
        sizesJson.ShouldContain("\"falkorDbNodeCount\":13", Shouldly.Case.Sensitive);
        sizesJson.ShouldNotContain("syntacticKeyCount", Shouldly.Case.Sensitive);
        statusJson.ShouldContain("\"rediSearch\":\"ready\"", Shouldly.Case.Sensitive);
        statusJson.ShouldContain("\"redisVector\":\"degraded\"", Shouldly.Case.Sensitive);
        statusJson.ShouldContain("\"falkorDb\":\"missing\"", Shouldly.Case.Sensitive);
        statusJson.ShouldNotContain("\"syntactic\"", Shouldly.Case.Sensitive);
        semanticJson.ShouldContain("\"vectorHashKey\":\"tenant-a:vec:unit-a\"", Shouldly.Case.Sensitive);
        semanticJson.ShouldNotContain("semanticIndexKey", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void ContractsAssembly_ExcludesServerOnlyPayloadsAndBackendNamedClrMembers()
    {
        Assembly contractsAssembly = typeof(MemoriesJsonContext).Assembly;
        HashSet<string> publicTypeNames = contractsAssembly.ExportedTypes
            .Select(static type => type.Name)
            .ToHashSet(StringComparer.Ordinal);
        string[] serverOnlyTypes =
        [
            "BatchedGraphDeletionInput",
            "BatchedGraphDeletionResult",
            "CounterTransitionInput",
            "ExtractionInput",
            "ExtractionResult",
            "FailedUnitInput",
            "FetchUrlInput",
            "UrlFetchResult",
            "IndexInput",
            "IndexResult",
            "NaturalLanguageDescriptionInput",
            "NaturalLanguageDescriptionResult",
            "NaturalLanguageIndexInput",
            "QueueNaturalLanguageEmbeddingRetryInput",
            "FailedNaturalLanguageEmbeddingRecord",
            "NaturalLanguageEmbeddingRetryInput",
            "NaturalLanguageEmbeddingRetryResult",
        ];

        foreach (string typeName in serverOnlyTypes)
        {
            publicTypeNames.ShouldNotContain(typeName);
        }

        typeof(TenantIndexSizes).GetProperty("RediSearchKeyCount").ShouldBeNull();
        typeof(TenantIndexSizes).GetProperty("RedisVectorKeyCount").ShouldBeNull();
        typeof(TenantIndexSizes).GetProperty("FalkorDbNodeCount").ShouldBeNull();
        typeof(TenantIndexStatus).GetProperty("RediSearch").ShouldBeNull();
        typeof(TenantIndexStatus).GetProperty("RedisVector").ShouldBeNull();
        typeof(TenantIndexStatus).GetProperty("FalkorDb").ShouldBeNull();
        typeof(ConsistencySemanticDetail).GetProperty("VectorHashKey").ShouldBeNull();
        typeof(TenantProvisioningResult).GetProperty("CompensatedBackends").ShouldBeNull();
        typeof(TenantDeletionResult).GetProperty("DeletedBackends").ShouldBeNull();
        bool referencesAnotherHexalithAssembly = contractsAssembly.GetReferencedAssemblies()
            .Any(static reference =>
                reference.Name is not null && reference.Name.StartsWith("Hexalith.", StringComparison.Ordinal));
        referencesAnotherHexalithAssembly.ShouldBeFalse();
    }
}
