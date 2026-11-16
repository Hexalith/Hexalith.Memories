// <copyright file="DegradationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Search;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

using StackExchange.Redis;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed partial class DegradationIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public DegradationIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    private async Task<HybridSearchResult> GetHybridSearchAsync(
        string tenantId,
        string caseId,
        string memoryUnitId,
        string query)
    {
        string path = $"/api/v1/search?tenantId={tenantId}&caseId={caseId}&query={query}" +
            $"&axis=hybrid&axes=syntactic,semantic,graph&graphStartNodeId={memoryUnitId}&depth=1";
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<HybridSearchResult>(MemoriesJsonContext.Options)
            ?? throw new InvalidOperationException("Hybrid search returned no response body.");
    }

    private async Task<IReadOnlyDictionary<string, string>> ReadHashSnapshotAsync(string key)
    {
        IDatabase database = _fixture.RedisConnection.GetDatabase();
        (await database.KeyTypeAsync(key)).ShouldBe(RedisType.Hash);
        HashEntry[] entries = await database.HashGetAllAsync(key);
        entries.ShouldNotBeEmpty();
        return entries.ToDictionary(
            entry => entry.Name.ToString(),
            entry => entry.Value.ToString(),
            StringComparer.Ordinal);
    }

    private static void AssertHashSnapshot(
        IReadOnlyDictionary<string, string> expected,
        IReadOnlyDictionary<string, string> actual)
    {
        actual.Count.ShouldBe(expected.Count);
        foreach ((string field, string value) in expected)
        {
            actual.ShouldContainKey(field);
            actual[field].ShouldBe(value);
        }
    }
}
