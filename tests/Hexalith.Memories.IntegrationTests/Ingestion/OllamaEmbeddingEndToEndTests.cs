// <copyright file="OllamaEmbeddingEndToEndTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;

using Shouldly;

using StackExchange.Redis;

/// <summary>Story 13.7 Ollama provider end-to-end coverage through the Aspire topology.</summary>
[Collection("OllamaAspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class OllamaEmbeddingEndToEndTests : IAsyncLifetime
{
    /// <summary>
    /// Story 14.4 AC #6: the ingestion path must invoke the Ollama embed endpoint at least once
    /// for the raw payload and once for the natural-language description, so two embed calls is
    /// the floor. Named constant prevents the assertion from drifting silently if a refactor
    /// changes the call shape — a regression that drops one of the embeddings becomes a clear
    /// failure pointing back to this expectation.
    /// </summary>
    private const int MinimumRawAndNaturalLanguageEmbeddings = 2;

    /// <summary>
    /// Story 14.4 AC #6: at least one OIDC token request is required to obtain the bearer used
    /// by both embed calls. Cached tokens may collapse multiple ingestions to a single request,
    /// so the floor is one rather than per-embed.
    /// </summary>
    private const int MinimumTokenRequests = 1;

    private readonly string _clientSecret = $"example-{Guid.NewGuid():N}";
    private AspireIngestionPipelineFixture? _fixture;
    private OllamaOidcFakeServer? _fakeServer;

    public async ValueTask InitializeAsync()
    {
        _fakeServer = await OllamaOidcFakeServer.StartAsync(_clientSecret);
        try
        {
            _fixture = new AspireIngestionPipelineFixture(
                EmbeddingProviderTestMode.OllamaOidcFake,
                new EmbeddingProviderSecret(OllamaOidcFakeServer.SecretName, _clientSecret));
            await _fixture.InitializeAsync();
        }
        catch
        {
            // xUnit does not call DisposeAsync after InitializeAsync throws, so the Kestrel
            // listener inside _fakeServer would leak its loopback port until process exit.
            await _fakeServer.DisposeAsync();
            _fakeServer = null;
            _fixture = null;
            throw;
        }
    }

    public async ValueTask DisposeAsync()
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
        string sourceUri = $"event://enterprise.claims/{unique}";

        await fixture.ProvisionActiveTenantAsync(
            tenantId,
            displayName: $"Ollama {unique}",
            vectorDimensions: OllamaOidcFakeServer.OllamaDimensions);
        await ConfigureOllamaTenantAsync(fixture, fakeServer, tenantId);
        await CreateCaseAsync(fixture, tenantId, caseId);

        IngestionInput input = new()
        {
            TenantId = tenantId,
            CaseId = caseId,
            SourceUri = sourceUri,
            ContentBytes = Encoding.UTF8.GetBytes(
                $$"""
                {"eventId":"{{unique}}","eventType":"ClaimSubmitted","description":"Story 13.7 Ollama provider path with syntactic canary {{canary}}."}
                """),
            ContentType = "application/json",
            SourceType = SourceType.Event,
            IngestedBy = "integration@test.local",
            Metadata =
            {
                ["cloudevent.type"] = new MetadataField("ClaimSubmitted", MetadataOrigin.Human, 1.0f),
                ["cloudevent.subject"] = new MetadataField($"claims/{unique}", MetadataOrigin.Human, 1.0f),
                ["event.aggregateType"] = new MetadataField("Claim", MetadataOrigin.Human, 1.0f),
            },
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
            fakeServer,
            tenantId,
            caseId,
            sourceUri,
            accepted.InstanceId);

        IDatabase db = fixture.RedisConnection.GetDatabase();
        RedisValue provider = await db.HashGetAsync(semanticKey, "embeddingProvider");
        RedisValue model = await db.HashGetAsync(semanticKey, "embeddingModel");
        RedisValue dimensions = await db.HashGetAsync(semanticKey, "embeddingDimensions");
        provider.ToString().ShouldBe($"{EmbeddingProviderDefaults.OllamaProviderName}:{OllamaOidcFakeServer.DefaultModel}");
        model.ToString().ShouldBe(OllamaOidcFakeServer.DefaultModel);
        dimensions.ToString().ShouldBe(OllamaOidcFakeServer.OllamaDimensions.ToString(System.Globalization.CultureInfo.InvariantCulture));

        // Story 14.4 / 13.7-RV2: build query strings via Uri.EscapeDataString so interpolated values
        // (even though current values are GUID-N hex) cannot inject reserved URL characters.
        string searchUri =
            $"/api/search?tenantId={Uri.EscapeDataString(tenantId)}" +
            $"&query={Uri.EscapeDataString(canary)}" +
            "&axis=hybrid&axes=syntactic,semantic&maxResults=5";
        using HttpResponseMessage searchResponse = await fixture.MemoriesClient.GetAsync(searchUri);

        searchResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        HybridSearchResult? search = await searchResponse.Content.ReadFromJsonAsync<HybridSearchResult>(
            MemoriesJsonContext.Options);
        search.ShouldNotBeNull();
        search.Results.Select(result => result.MemoryUnitId).ShouldContain(memoryUnitId);
        search.Results.Single(result => result.MemoryUnitId == memoryUnitId).ContentSnippet.ShouldContain(canary);
        fakeServer.TokenRequestCount.ShouldBeGreaterThanOrEqualTo(MinimumTokenRequests);
        fakeServer.EmbedRequestCount.ShouldBeGreaterThanOrEqualTo(MinimumRawAndNaturalLanguageEmbeddings);
        string logs = string.Join(Environment.NewLine, fixture.GetLogEntriesSince(0).Select(entry => entry.Message));
        // Assert the actual secret/token literals never appear, plus the form-encoded leak shape.
        // The fake mints a deterministic opaque token, so a 'Bearer eyJ' (JWT prefix) substring
        // would never match — exercise the actual token value instead.
        logs.ShouldNotContain(_clientSecret);
        logs.ShouldNotContain($"client_secret={_clientSecret}");
        logs.ShouldNotContain(OllamaOidcFakeServer.AccessToken);
        logs.ShouldNotContain($"Bearer {OllamaOidcFakeServer.AccessToken}");
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

    private async Task<(string SemanticKey, string MemoryUnitId)> WaitForSemanticHashAsync(
        OllamaOidcFakeServer fakeServer,
        string tenantId,
        string caseId,
        string sourceUri,
        string instanceId)
    {
        AspireIngestionPipelineFixture fixture = _fixture!;
        // Story 14.4 AC #3: targeted wait. The workflow status payload is the source of the
        // memoryUnitId; once available, the Redis probe is a direct HGET against that known key.
        using CancellationTokenSource cts = new(TimeSpan.FromMinutes(3));
        CancellationToken ct = cts.Token;
        IDatabase db = fixture.RedisConnection.GetDatabase();
        string lastWorkflowPayload = string.Empty;
        WorkflowTerminalStatus terminalStatus = WorkflowTerminalStatus.NotReached;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var workflowRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/ingest/{instanceId}");
                workflowRequest.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    AspireIngestionPipelineFixture.MintServerBearer(tenantId));
                using HttpResponseMessage workflowResponse = await fixture.MemoriesClient.SendAsync(workflowRequest, ct).ConfigureAwait(false);
                if (workflowResponse.StatusCode == HttpStatusCode.OK)
                {
                    lastWorkflowPayload = await workflowResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    terminalStatus = WorkflowReachedTerminalStatus(lastWorkflowPayload);
                    string? targetedMemoryUnitId = TryExtractMemoryUnitId(lastWorkflowPayload) ?? instanceId;
                    if (!string.IsNullOrEmpty(targetedMemoryUnitId))
                    {
                        string targetedKey = IndexSchemaDefinitions.BuildSemanticKey(tenantId, targetedMemoryUnitId);
                        string? matchedMemoryUnitId = await TryMatchSemanticHashAsync(db, tenantId, targetedKey, caseId, sourceUri).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(matchedMemoryUnitId))
                        {
                            return (targetedKey, matchedMemoryUnitId);
                        }

                        string targetedChunkKey = IndexSchemaDefinitions.BuildSemanticChunkKey(tenantId, targetedMemoryUnitId, sequence: 0);
                        matchedMemoryUnitId = await TryMatchSemanticHashAsync(db, tenantId, targetedChunkKey, caseId, sourceUri).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(matchedMemoryUnitId))
                        {
                            return (targetedChunkKey, matchedMemoryUnitId);
                        }
                    }

                    if (terminalStatus != WorkflowTerminalStatus.NotReached)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        const string allTenantKeys = "omitted; targeted wait does not enumerate the Redis keyspace";
        string logs = string.Join(Environment.NewLine, fixture.GetLogEntriesSince(0).TakeLast(40).Select(entry => entry.Message));
        string redactedPayload = Redact(lastWorkflowPayload);
        string redactedLogs = Redact(logs);
        string detail =
            $"tenant '{tenantId}' case '{caseId}'. " +
            $"TokenRequests={fakeServer.TokenRequestCount}, EmbedRequests={fakeServer.EmbedRequestCount}. " +
            $"Redis keys: {allTenantKeys}. Last workflow payload: {redactedPayload}. Recent logs: {redactedLogs}";

        throw terminalStatus switch
        {
            WorkflowTerminalStatus.Failed => new InvalidOperationException(
                $"Ollama ingestion workflow reached terminal Failed before semantic hash was written for {detail}"),
            WorkflowTerminalStatus.Completed => new InvalidOperationException(
                $"Ollama ingestion workflow reported Completed but no Ollama semantic hash was written for {detail}"),
            _ => new TimeoutException(
                $"Ollama semantic hash was not written within wait budget for {detail}"),
        };
    }

    private static async Task<string?> TryMatchSemanticHashAsync(
        IDatabase db,
        string tenantId,
        string semanticKey,
        string caseId,
        string sourceUri)
    {
        RedisValue storedCaseId = await db.HashGetAsync(semanticKey, "caseId").ConfigureAwait(false);
        RedisValue memoryUnitId = await db.HashGetAsync(semanticKey, "memoryUnitId").ConfigureAwait(false);
        RedisValue dimensions = await db.HashGetAsync(semanticKey, "embeddingDimensions").ConfigureAwait(false);
        if (storedCaseId.ToString() != caseId ||
            memoryUnitId.IsNullOrEmpty ||
            dimensions.ToString() != OllamaOidcFakeServer.OllamaDimensions.ToString(System.Globalization.CultureInfo.InvariantCulture))
        {
            return null;
        }

        RedisValue storedSourceUri = await db.HashGetAsync($"{tenantId}:mu:{memoryUnitId}", "sourceUri").ConfigureAwait(false);
        return storedSourceUri.ToString() == sourceUri ? memoryUnitId.ToString() : null;
    }

    /// <summary>
    /// Extracts the memoryUnitId from the workflow status payload's serializedOutput when the
    /// workflow has progressed far enough to materialize an <see cref="IngestionResult"/>.
    /// Returns null on any parsing failure so the caller can keep polling the workflow status.
    /// </summary>
    private static string? TryExtractMemoryUnitId(string workflowPayload)
    {
        if (string.IsNullOrWhiteSpace(workflowPayload))
        {
            return null;
        }

        try
        {
            using JsonDocument outer = JsonDocument.Parse(workflowPayload);
            if (!outer.RootElement.TryGetProperty("serializedOutput", out JsonElement serialized) ||
                serialized.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string? raw = serialized.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            using JsonDocument inner = JsonDocument.Parse(raw);
            if (inner.RootElement.ValueKind != JsonValueKind.Object ||
                !inner.RootElement.TryGetProperty("memoryUnitId", out JsonElement idElement) ||
                idElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string? id = idElement.GetString();
            return string.IsNullOrWhiteSpace(id) ? null : id;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string Redact(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text
            .Replace(_clientSecret, "[REDACTED_SECRET]", StringComparison.Ordinal)
            .Replace(OllamaOidcFakeServer.AccessToken, "[REDACTED_TOKEN]", StringComparison.Ordinal);
    }

    private static WorkflowTerminalStatus WorkflowReachedTerminalStatus(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return WorkflowTerminalStatus.NotReached;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(payload);
        }
        catch (JsonException)
        {
            // A non-JSON status body is not a terminal signal — keep polling.
            return WorkflowTerminalStatus.NotReached;
        }

        using (document)
        {
            if (document.RootElement.TryGetProperty("runtimeStatus", out JsonElement runtimeStatus))
            {
                if (runtimeStatus.ValueKind == JsonValueKind.String)
                {
                    string? value = runtimeStatus.GetString();
                    if (string.Equals(value, "Failed", StringComparison.OrdinalIgnoreCase))
                    {
                        return WorkflowTerminalStatus.Failed;
                    }

                    if (string.Equals(value, "Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        return WorkflowTerminalStatus.Completed;
                    }
                }

                if (runtimeStatus.ValueKind == JsonValueKind.Number &&
                    runtimeStatus.TryGetInt32(out int ordinal))
                {
                    // Durable Task runtime status: 3 = Completed, 5 = Failed. Some DAPR payloads
                    // also expose booleans below while runtimeStatus remains numeric/non-terminal.
                    if (ordinal == 3)
                    {
                        return WorkflowTerminalStatus.Completed;
                    }

                    if (ordinal == 5)
                    {
                        return WorkflowTerminalStatus.Failed;
                    }
                }
            }

            if (document.RootElement.TryGetProperty("isWorkflowCompleted", out JsonElement completed) &&
                completed.ValueKind == JsonValueKind.True)
            {
                return WorkflowTerminalStatus.Completed;
            }
        }

        return WorkflowTerminalStatus.NotReached;
    }

    private enum WorkflowTerminalStatus
    {
        NotReached,
        Completed,
        Failed,
    }

    private sealed record AcceptedResponse(string InstanceId);
}
