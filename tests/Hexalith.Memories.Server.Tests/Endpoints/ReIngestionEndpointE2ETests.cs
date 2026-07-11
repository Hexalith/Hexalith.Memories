// <copyright file="ReIngestionEndpointE2ETests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Tests.Authentication;
using Hexalith.Memories.Server.Tests.EventStoreIntegration;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 23.4 API-level coverage for failed non-URL re-ingestion outcomes. These tests exercise
/// the real minimal API routes and JSON contracts while replacing the coordinator dependencies.
/// </summary>
public sealed class ReIngestionEndpointE2ETests : IDisposable
{
    private const string StoreName = "statestore";
    private const string TenantId = "tenant-a";
    private const string CaseId = "case-1";

    private readonly EventStoreWebAppFactory _factory = new();
    private readonly IFailedUnitsRegistry _registry = Substitute.For<IFailedUnitsRegistry>();
    private readonly IWorkflowPayloadStore _payloadStore = Substitute.For<IWorkflowPayloadStore>();
    private readonly IIngestionWorkflowScheduler _scheduler = Substitute.For<IIngestionWorkflowScheduler>();

    public ReIngestionEndpointE2ETests()
    {
        _factory.FailedUnitsRegistryOverride = _registry;
        _factory.WorkflowPayloadStoreOverride = _payloadStore;
        _factory.IngestionWorkflowSchedulerOverride = _scheduler;
    }

    [Fact]
    public async Task PostReIngest_WhenNonUrlSourcePayloadUnavailable_ReturnsActionableErrorWithoutClaim()
    {
        StubTenantActive();
        _registry.GetAsync(TenantId, "mu-legacy", Arg.Any<CancellationToken>())
            .Returns(CreateRecord("mu-legacy", SourceType.File));
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.PostAsync(
            $"/api/v1/tenants/{TenantId}/cases/{CaseId}/memory-units/mu-legacy/re-ingest",
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse error = await ReadJsonAsync<ErrorResponse>(response);
        error.Code.ShouldBe("NON_URL_REINGESTION_UNAVAILABLE");
        error.Message.ShouldBe("Cannot re-ingest this non-URL failed unit because the original source content is unavailable.");
        error.Suggestion.ShouldBe("Re-ingest from the original file or event source if available, or ingest the content again.");

        await _registry.DidNotReceive().RemoveAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _scheduler.DidNotReceive().ScheduleAsync(
            Arg.Any<string>(),
            Arg.Any<IngestionInput>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostBulkReIngest_WithMixedOutcomes_ReturnsScheduledUnsupportedMissingAndConflictCounts()
    {
        StubTenantActive();
        StubCaseExists();
        _registry.GetAsync(TenantId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<string>(1) switch
            {
                "mu-ok" => CreateRecord("mu-ok", SourceType.Url),
                "mu-unsupported" => CreateRecord("mu-unsupported", SourceType.File),
                "mu-conflict" => CreateRecord("mu-conflict", SourceType.Url),
                _ => null,
            });
        _registry.RemoveAsync(TenantId, CaseId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<string>(2) == "mu-ok");
        _scheduler
            .ScheduleAsync("mu-ok", Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>())
            .Returns("wf-mu-ok");
        using HttpClient client = CreateAuthorizedClient();
        ReIngestRequest request = new(["mu-ok", "mu-unsupported", "mu-missing", "mu-conflict"]);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{TenantId}/cases/{CaseId}/failed-units/re-ingest",
            request,
            MemoriesJsonContext.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        BulkReIngestionResponse bulk = await ReadJsonAsync<BulkReIngestionResponse>(response);
        bulk.Scheduled.ShouldBe(1);
        bulk.Unsupported.ShouldBe(1);
        bulk.NotFound.ShouldBe(1);
        bulk.Conflicted.ShouldBe(1);
        bulk.Errored.ShouldBe(0);
        bulk.Units.Select(unit => unit.Outcome).ShouldBe(
        [
            "scheduled",
            "unsupported-source-payload",
            "not-found",
            "conflict",
        ]);
        bulk.Units[0].NewWorkflowInstanceId.ShouldBe("wf-mu-ok");
        bulk.Units[1].ErrorCode.ShouldBe("NON_URL_REINGESTION_UNAVAILABLE");

        await _scheduler.Received(1).ScheduleAsync(
            "mu-ok",
            Arg.Any<IngestionInput>(),
            Arg.Any<CancellationToken>());
        await _registry.DidNotReceive().RemoveAsync(
            TenantId,
            CaseId,
            "mu-unsupported",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    public void Dispose() => _factory.Dispose();

    private HttpClient CreateAuthorizedClient()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ServerTestBearerToken.Create(tenants: [TenantId]));
        return client;
    }

    private void StubTenantActive()
    {
        TenantRegistryEntry entry = new(
            new TenantInfo(TenantId, TenantId, TenantStatus.Active, DateTimeOffset.UtcNow),
            WorkflowInstanceId: null);

        _factory.DaprClient
            .GetStateAsync<StoredTenantRegistryEntry?>(
                StoreName,
                Arg.Is<string>(key => key == $"tenant-registry-{TenantId}"),
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(entry);
    }

    private void StubCaseExists()
        => _factory.RedisDatabase
            .HashGetAllAsync(
                Arg.Is<RedisKey>(key => key.ToString() == $"{TenantId}:case:{CaseId}"),
                Arg.Any<CommandFlags>())
            .Returns(
            [
                new HashEntry("id", CaseId),
                new HashEntry("tenantId", TenantId),
                new HashEntry("name", "Retry Case"),
                new HashEntry("status", nameof(CaseStatus.Active)),
                new HashEntry("createdAt", "2026-07-05T10:00:00+00:00"),
                new HashEntry("lastUpdated", "2026-07-05T10:00:00+00:00"),
            ]);

    private static FailedUnitRecord CreateRecord(string memoryUnitId, SourceType sourceType)
        => new(
            TenantId,
            CaseId,
            memoryUnitId,
            sourceType == SourceType.Url ? $"https://example.test/{memoryUnitId}" : $"file:///{memoryUnitId}.txt",
            sourceType,
            "operator@example.com",
            sourceType == SourceType.Url ? null : "text/plain",
            "embedding",
            "PROVIDER_500",
            "provider failed",
            1,
            LastRetryAt: null,
            DateTimeOffset.Parse("2026-07-05T10:05:00+00:00"),
            SourcePayloadReference: null,
            Metadata: null);

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
        where T : class
    {
        string body = await response.Content.ReadAsStringAsync();
        T? value = JsonSerializer.Deserialize<T>(body, MemoriesJsonContext.Options);
        value.ShouldNotBeNull();
        return value;
    }
}
