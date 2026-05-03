// <copyright file="OllamaEmbeddingEndToEndTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Ingestion;

using Shouldly;

using StackExchange.Redis;

/// <summary>Story 13.7 Ollama provider end-to-end coverage through the Aspire topology.</summary>
[Collection("OllamaAspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class OllamaEmbeddingEndToEndTests : IAsyncLifetime
{
    private readonly string _clientSecret = $"example-{Guid.NewGuid():N}";
    private AspireIngestionPipelineFixture? _fixture;
    private OllamaOidcFakeServer? _fakeServer;

    public async Task InitializeAsync()
    {
        _fakeServer = await OllamaOidcFakeServer.StartAsync(_clientSecret);
        _fixture = new AspireIngestionPipelineFixture(
            EmbeddingProviderTestMode.OllamaOidcFake,
            new EmbeddingProviderSecret(OllamaOidcFakeServer.SecretName, _clientSecret));
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync();
        }

        if (_fakeServer is not null)
        {
            await _fakeServer.DisposeAsync();
        }
    }

    [Fact]
    public async Task Story13_7_AC2_OllamaEmbeddingEndToEnd_ShouldIndexAndSearchWith2560Dimensions()
    {
        AspireIngestionPipelineFixture fixture = _fixture!;
        OllamaOidcFakeServer fakeServer = _fakeServer!;
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-ollama-{unique[..12]}";
        string caseId = $"case-ollama-{unique[..12]}";
        string canary = $"ollama-canary-{unique}";
        string sourceUri = $"file:///{unique}.txt";

        await fixture.ProvisionActiveTenantAsync(
            tenantId,
            displayName: $"Ollama {unique}",
            vectorDimensions: OllamaOidcFakeServer.OllamaDimensions);
        await ConfigureOllamaTenantAsync(fixture, fakeServer, tenantId);
        await ResetSemanticIndexesAsync(fixture, tenantId);
        await CreateCaseAsync(fixture, tenantId, caseId);

        IngestionInput input = new()
        {
            TenantId = tenantId,
            CaseId = caseId,
            SourceUri = sourceUri,
            ContentBytes = Encoding.UTF8.GetBytes($"Story 13.7 Ollama provider path with syntactic canary {canary}."),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };

        using HttpResponseMessage ingestResponse = await fixture.MemoriesClient.PostAsJsonAsync(
            "/api/ingest",
            input,
            MemoriesJsonContext.Options);

        ingestResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        AcceptedResponse accepted = (await ingestResponse.Content.ReadFromJsonAsync<AcceptedResponse>(
            MemoriesJsonContext.Options))!;
        accepted.InstanceId.ShouldNotBeNullOrWhiteSpace();
        (string semanticKey, string memoryUnitId) = await WaitForSemanticHashAsync(
            fixture,
            fakeServer,
            tenantId,
            caseId,
            accepted.InstanceId);

        IDatabase db = fixture.RedisConnection.GetDatabase();
        RedisValue provider = await db.HashGetAsync(semanticKey, "embeddingProvider");
        RedisValue model = await db.HashGetAsync(semanticKey, "embeddingModel");
        RedisValue dimensions = await db.HashGetAsync(semanticKey, "embeddingDimensions");
        provider.ToString().ShouldBe($"{EmbeddingProviderDefaults.OllamaProviderName}:{OllamaOidcFakeServer.DefaultModel}");
        model.ToString().ShouldBe(OllamaOidcFakeServer.DefaultModel);
        dimensions.ToString().ShouldBe(OllamaOidcFakeServer.OllamaDimensions.ToString(System.Globalization.CultureInfo.InvariantCulture));

        using HttpResponseMessage searchResponse = await fixture.MemoriesClient.GetAsync(
            $"/api/search?tenantId={tenantId}&query={canary}&axis=hybrid&axes=syntactic,semantic&maxResults=5");

        searchResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        HybridSearchResult? search = await searchResponse.Content.ReadFromJsonAsync<HybridSearchResult>(
            MemoriesJsonContext.Options);
        search.ShouldNotBeNull();
        search.Results.Select(result => result.MemoryUnitId).ShouldContain(memoryUnitId);
        search.Results.Single(result => result.MemoryUnitId == memoryUnitId).ContentSnippet.ShouldContain(canary);
        fakeServer.TokenRequestCount.ShouldBeGreaterThanOrEqualTo(1);
        fakeServer.EmbedRequestCount.ShouldBeGreaterThanOrEqualTo(2);
        string logs = string.Join(Environment.NewLine, fixture.GetLogEntriesSince(0).Select(entry => entry.Message));
        logs.ShouldNotContain(_clientSecret);
        logs.ShouldNotContain("client_" + "secret=");
        logs.ShouldNotContain("Bearer " + "eyJ");
    }

    private static async Task ConfigureOllamaTenantAsync(
        AspireIngestionPipelineFixture fixture,
        OllamaOidcFakeServer fakeServer,
        string tenantId)
    {
        TenantEmbeddingConfig config = new()
        {
            Provider = EmbeddingProviderDefaults.OllamaProviderName,
            Model = OllamaOidcFakeServer.DefaultModel,
            Dimensions = OllamaOidcFakeServer.OllamaDimensions,
            RateLimitPerMinute = 6000,
            ApiSecretKeyName = OllamaOidcFakeServer.SecretName,
            BaseUrl = fakeServer.OllamaBaseUrl.ToString(),
            AuthMode = EmbeddingProviderDefaults.OidcClientCredentialsAuthMode,
            OidcTokenEndpoint = fakeServer.OidcTokenEndpoint.ToString(),
            OidcClientId = OllamaOidcFakeServer.ClientId,
            OidcScope = OllamaOidcFakeServer.Scope,
        };

        using HttpResponseMessage response = await fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/tenants/{tenantId}/embedding-config?forceReindex=true",
            config,
            MemoriesJsonContext.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task CreateCaseAsync(AspireIngestionPipelineFixture fixture, string tenantId, string caseId)
    {
        using HttpResponseMessage response = await fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            new CreateCaseInput(tenantId, caseId, "Story 13.7 Ollama provider test case."),
            MemoriesJsonContext.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private static async Task ResetSemanticIndexesAsync(AspireIngestionPipelineFixture fixture, string tenantId)
    {
        IDatabase db = fixture.RedisConnection.GetDatabase();
        await DropIndexIfExistsAsync(db, $"{tenantId}:memories:vec");
        await DropIndexIfExistsAsync(db, $"{tenantId}:memories:vec:nl");
    }

    private static async Task DropIndexIfExistsAsync(IDatabase db, string indexName)
    {
        try
        {
            _ = await db.ExecuteAsync("FT.DROPINDEX", indexName);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown Index name", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    private static async Task<(string SemanticKey, string MemoryUnitId)> WaitForSemanticHashAsync(
        AspireIngestionPipelineFixture fixture,
        OllamaOidcFakeServer fakeServer,
        string tenantId,
        string caseId,
        string instanceId)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(3);
        IServer redisServer = fixture.RedisConnection.GetServer(fixture.RedisConnection.GetEndPoints().Single());
        IDatabase db = fixture.RedisConnection.GetDatabase();
        string lastWorkflowPayload = string.Empty;

        while (DateTimeOffset.UtcNow < deadline)
        {
            string[] semanticKeys = redisServer.Keys(pattern: $"{tenantId}:vec:*")
                .Select(key => key.ToString())
                .Where(key => !key.Contains(":vec:nl:", StringComparison.Ordinal))
                .ToArray();

            foreach (string semanticKey in semanticKeys)
            {
                RedisValue storedCaseId = await db.HashGetAsync(semanticKey, "caseId");
                RedisValue memoryUnitId = await db.HashGetAsync(semanticKey, "memoryUnitId");
                RedisValue dimensions = await db.HashGetAsync(semanticKey, "embeddingDimensions");
                if (storedCaseId.ToString() == caseId &&
                    !memoryUnitId.IsNullOrEmpty &&
                    dimensions.ToString() == OllamaOidcFakeServer.OllamaDimensions.ToString(System.Globalization.CultureInfo.InvariantCulture))
                {
                    return (semanticKey, memoryUnitId.ToString());
                }
            }

            using HttpResponseMessage workflowResponse = await fixture.MemoriesClient.GetAsync($"/api/ingest/{instanceId}");
            if (workflowResponse.StatusCode == HttpStatusCode.OK)
            {
                lastWorkflowPayload = await workflowResponse.Content.ReadAsStringAsync();
                if (WorkflowReachedTerminalStatus(lastWorkflowPayload))
                {
                    break;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        string allTenantKeys = string.Join(
            ", ",
            redisServer.Keys(pattern: $"{tenantId}:*").Select(key => key.ToString()).Order(StringComparer.Ordinal));
        string logs = string.Join(Environment.NewLine, fixture.GetLogEntriesSince(0).TakeLast(40).Select(entry => entry.Message));
        throw new TimeoutException(
            $"Ollama semantic hash for tenant '{tenantId}' and case '{caseId}' was not written. " +
            $"TokenRequests={fakeServer.TokenRequestCount}, EmbedRequests={fakeServer.EmbedRequestCount}. " +
            $"Redis keys: {allTenantKeys}. Last workflow payload: {lastWorkflowPayload}. Recent logs: {logs}");
    }

    private static bool WorkflowReachedTerminalStatus(string payload)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        if (document.RootElement.TryGetProperty("runtimeStatus", out JsonElement runtimeStatus))
        {
            if (runtimeStatus.ValueKind == JsonValueKind.String &&
                (string.Equals(runtimeStatus.GetString(), "Failed", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(runtimeStatus.GetString(), "Completed", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (runtimeStatus.ValueKind == JsonValueKind.Number &&
                runtimeStatus.TryGetInt32(out int ordinal) &&
                ordinal is 3 or 5)
            {
                return true;
            }
        }

        if (document.RootElement.TryGetProperty("isWorkflowCompleted", out JsonElement completed) &&
            completed.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        return false;
    }

    private sealed record AcceptedResponse(string InstanceId);
}
