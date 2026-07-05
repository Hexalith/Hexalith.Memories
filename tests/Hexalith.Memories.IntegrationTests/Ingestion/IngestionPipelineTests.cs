// <copyright file="IngestionPipelineTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Infrastructure;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

/// <summary>End-to-end tests for the ingestion workflow running inside the full Aspire topology.</summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class IngestionPipelineTests
{
    private const int MaxPayloadBytes = 1024 * 1024;

    private readonly AspireIngestionPipelineFixture _fixture;

    /// <summary>Initializes a new instance of the <see cref="IngestionPipelineTests"/> class.</summary>
    /// <param name="fixture">The shared Aspire topology fixture.</param>
    public IngestionPipelineTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task PostIngest_ShouldIndexMemoryUnitAcrossAllBackends()
    {
        // Arrange
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        string caseId = $"case-{Guid.NewGuid():N}";
        string sourceUri = $"file:///{Guid.NewGuid():N}.txt";
        IngestionInput input = new()
        {
            TenantId = tenantId,
            CaseId = caseId,
            SourceUri = sourceUri,
            ContentBytes = "Aspire integration test content for workflow orchestration."u8.ToArray(),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
            Metadata = new Dictionary<string, MetadataField>
            {
                ["priority"] = new("urgent", MetadataOrigin.Human, 1.0f),
            },
        };

        // Act
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PostAsJsonAsync("/api/ingest", input, MemoriesJsonContext.Options);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        AcceptedResponse? accepted = await response.Content
            .ReadFromJsonAsync<AcceptedResponse>(MemoriesJsonContext.Options);
        accepted.ShouldNotBeNull();
        accepted.InstanceId.ShouldNotBeNullOrWhiteSpace();

        (string syntacticKey, string semanticKey) = await WaitForBackendWritesAsync(tenantId, caseId, sourceUri);

        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();
        RedisValue ingestedBy = await redisDb.HashGetAsync(syntacticKey, "ingestedBy");
        RedisValue indexedSourceUri = await redisDb.HashGetAsync(syntacticKey, "sourceUri");
        RedisValue metadataJson = await redisDb.HashGetAsync(syntacticKey, "metadataJson");

        ingestedBy.ToString().ShouldBe(input.IngestedBy);
        indexedSourceUri.ToString().ShouldBe(sourceUri);
        metadataJson.ToString().ShouldContain("priority");

        bool semanticExists = await redisDb.KeyExistsAsync(semanticKey);
        semanticExists.ShouldBeTrue();
        IndexSchemaDefinitions.TryParseSemanticChunkKey(tenantId, semanticKey, out string parsedMemoryUnitId, out int chunkSequence)
            .ShouldBeTrue();
        chunkSequence.ShouldBe(0);
        parsedMemoryUnitId.ShouldBe(syntacticKey[(syntacticKey.LastIndexOf(':') + 1)..]);
        RedisValue storedChunkSequence = await redisDb.HashGetAsync(semanticKey, "chunkSequence");
        RedisValue storedChunkStartOffset = await redisDb.HashGetAsync(semanticKey, "chunkStartOffset");
        RedisValue storedChunkEndOffset = await redisDb.HashGetAsync(semanticKey, "chunkEndOffset");
        ((int)storedChunkSequence).ShouldBe(0);
        ((int)storedChunkStartOffset).ShouldBe(0);
        ((int)storedChunkEndOffset).ShouldBeGreaterThan(0);

        using var stateRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/ingest/{accepted.InstanceId}");
        stateRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            AspireIngestionPipelineFixture.MintServerBearer(tenantId));
        using HttpResponseMessage stateResponse = await _fixture.MemoriesClient
            .SendAsync(stateRequest);
        stateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostIngest_PayloadLargerThanOneMegabyte_ShouldReturnBadRequest()
    {
        IngestionInput input = new()
        {
            TenantId = $"tenant-{Guid.NewGuid():N}",
            CaseId = $"case-{Guid.NewGuid():N}",
            SourceUri = $"file:///{Guid.NewGuid():N}.txt",
            ContentBytes = new byte[MaxPayloadBytes + 1],
            ContentType = "application/octet-stream",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };

        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PostAsJsonAsync("/api/ingest", input, MemoriesJsonContext.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_INPUT");
        error.Message.ShouldContain("1 MB");
    }

    private async Task<(string SyntacticKey, string SemanticKey)> WaitForBackendWritesAsync(
        string tenantId,
        string caseId,
        string sourceUri)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(2);
        IServer redisServer = _fixture.RedisConnection.GetServer(_fixture.RedisConnection.GetEndPoints().Single());
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        while (DateTimeOffset.UtcNow < deadline)
        {
            string[] syntacticKeys = redisServer
                .Keys(pattern: $"{tenantId}:mu:*")
                .Select(key => key.ToString())
                .ToArray();
            string[] semanticKeys = redisServer
                .Keys(pattern: $"{tenantId}:vec:*")
                .Select(key => key.ToString())
                .ToArray();
            long graphCount = await CountGraphNodesAsync(falkor, tenantId, caseId, sourceUri);

            if (syntacticKeys.Length == 1 && semanticKeys.Length == 1 && graphCount == 1)
            {
                return (syntacticKeys[0], semanticKeys[0]);
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException(
            $"The ingestion pipeline did not index data for tenant '{tenantId}' within the allotted time.");
    }

    private static async Task<long> CountGraphNodesAsync(
        FalkorDB falkor,
        string tenantId,
        string caseId,
        string sourceUri)
    {
        ResultSet result = await falkor.QueryAsync(
            tenantId,
            "MATCH (m:MemoryUnit {caseId: $caseId, sourceUri: $sourceUri}) RETURN count(m) as cnt",
            new Dictionary<string, object>
            {
                ["caseId"] = caseId,
                ["sourceUri"] = sourceUri,
            }).ConfigureAwait(false);

        result.Count.ShouldBe(1);
        var enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<long>("cnt");
    }

    private sealed record AcceptedResponse(string InstanceId);
}
