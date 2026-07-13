// <copyright file="BackupRestoreFidelityIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Restore;

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

using CaseRecord = Hexalith.Memories.Contracts.V1.Case;

/// <summary>
/// Story 26.2 (AC7) — proves export → import fidelity end to end against the Aspire-hosted Redis Stack +
/// FalkorDB + Dapr sidecar: snapshot the backing stores, export the tenant, wipe the data plane (keeping the
/// provisioned indexes), restore the export, then assert every syntactic memory-unit hash field, every case +
/// members hash, and every graph edge round-trips, and that a re-derived semantic vector exists (byte-equal
/// under the fixture's deterministic embedding provider) for every restored unit.
/// <para>
/// This tier is Docker-dependent; it cannot run in the sandbox (no container runtime) and is validated in CI /
/// an operator cluster. The Docker-free contract tests in Hexalith.Memories.Server.Tests cover the same
/// schema-version / scope / edge-identity / dangling-stub / idempotency logic.
/// </para>
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class BackupRestoreFidelityIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    /// <summary>Initializes a new instance of the <see cref="BackupRestoreFidelityIntegrationTests"/> class.</summary>
    /// <param name="fixture">The Aspire integration fixture.</param>
    public BackupRestoreFidelityIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Ingest ≥3 units across ≥2 cases with a non-CONTAINS edge, snapshot, export, wipe, restore, and assert
    /// every hash and every edge round-trips with re-derived vectors present.
    /// </summary>
    [Fact]
    public async Task ExportThenImport_RestoresEveryHashAndEdge()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync($"tenant-restore-{Guid.NewGuid():N}");
        string firstCaseId = await CreateCaseAsync(tenantId, "Restore fidelity case one");
        string secondCaseId = await CreateCaseAsync(tenantId, "Restore fidelity case two");

        await IngestMemoryUnitAsync(tenantId, firstCaseId, "restore fidelity first memory");
        await IngestMemoryUnitAsync(tenantId, firstCaseId, "restore fidelity second memory");
        await IngestMemoryUnitAsync(tenantId, secondCaseId, "restore fidelity third memory");
        await WaitForContainsEdgeAsync(tenantId, firstCaseId, expectedCount: 2);
        await WaitForContainsEdgeAsync(tenantId, secondCaseId, expectedCount: 1);

        IReadOnlyList<string> firstCaseUnits = await GetCaseMemoryUnitIdsAsync(tenantId, firstCaseId);
        firstCaseUnits.Count.ShouldBeGreaterThanOrEqualTo(2);

        // Seed a non-CONTAINS (REFERENCES) edge with a promotion audit trail so the fidelity assertion covers
        // an edge that is present in edges[] (CONTAINS edges are rebuilt from caseId, not exported).
        await SeedReferencesEdgeAsync(tenantId, firstCaseUnits[0], firstCaseUnits[1]);

        // --- Snapshot the backing stores ---
        Dictionary<string, Dictionary<string, string>> muSnapshot = await SnapshotHashesAsync($"{tenantId}:mu:*");
        Dictionary<string, Dictionary<string, string>> caseSnapshot = await SnapshotHashesAsync($"{tenantId}:case:*");
        Dictionary<string, byte[]> vecSnapshot = await SnapshotVectorBytesAsync($"{tenantId}:vec:*");
        HashSet<string> edgeSnapshot = await SnapshotEdgesAsync(tenantId);
        muSnapshot.Count.ShouldBeGreaterThanOrEqualTo(3);
        edgeSnapshot.ShouldContain(e => e.Contains("REFERENCES", StringComparison.Ordinal));

        // --- Export the tenant ---
        string exportJson = await ExportTenantAsync(tenantId);

        // --- Wipe the data plane but keep the provisioned indexes (simulates same-tenant-id DR into a clean store) ---
        await WipeDataPlaneAsync(tenantId);
        (await SnapshotHashesAsync($"{tenantId}:mu:*")).ShouldBeEmpty();

        // --- Restore ---
        await RestoreTenantAsync(tenantId, exportJson);

        // --- Assert every syntactic memory-unit hash field round-trips ---
        Dictionary<string, Dictionary<string, string>> muRestored = await SnapshotHashesAsync($"{tenantId}:mu:*");
        muRestored.Count.ShouldBe(muSnapshot.Count);
        foreach ((string key, Dictionary<string, string> originalFields) in muSnapshot)
        {
            muRestored.ShouldContainKey(key);
            Dictionary<string, string> restoredFields = muRestored[key];
            foreach ((string field, string value) in originalFields)
            {
                restoredFields.ShouldContainKey(field);
                restoredFields[field].ShouldBe(value, $"memory unit hash '{key}' field '{field}' did not round-trip");
            }
        }

        // --- Assert every case + members hash round-trips ---
        Dictionary<string, Dictionary<string, string>> caseRestored = await SnapshotHashesAsync($"{tenantId}:case:*");
        foreach ((string key, Dictionary<string, string> originalFields) in caseSnapshot)
        {
            caseRestored.ShouldContainKey(key);
            Dictionary<string, string> restoredFields = caseRestored[key];
            foreach ((string field, string value) in originalFields)
            {
                restoredFields.ShouldContainKey(field);
                restoredFields[field].ShouldBe(value, $"case hash '{key}' field '{field}' did not round-trip");
            }
        }

        // --- Assert every graph edge round-trips (source, target, type, confidence, origin, audit) ---
        HashSet<string> edgeRestored = await SnapshotEdgesAsync(tenantId);
        foreach (string edge in edgeSnapshot)
        {
            edgeRestored.ShouldContain(edge, $"graph edge '{edge}' was not restored");
        }

        // --- Assert re-derived semantic vectors exist and are byte-identical (deterministic fixture provider) ---
        Dictionary<string, byte[]> vecRestored = await SnapshotVectorBytesAsync($"{tenantId}:vec:*");
        foreach (string muKey in muSnapshot.Keys)
        {
            string memoryUnitId = muKey.Split(':').Last();
            vecRestored.Keys.ShouldContain(k => k.StartsWith($"{tenantId}:vec:{memoryUnitId}", StringComparison.Ordinal));
        }

        foreach ((string vecKey, byte[] originalBytes) in vecSnapshot)
        {
            vecRestored.ShouldContainKey(vecKey);
            vecRestored[vecKey].ShouldBe(originalBytes, $"semantic vector '{vecKey}' was not byte-identical after restore");
        }
    }

    private async Task<string> CreateCaseAsync(string tenantId, string caseName)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/cases",
            new CreateCaseInput("ignored", caseName, null),
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        CaseRecord? createdCase = await response.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();
        return createdCase.Id;
    }

    private async Task IngestMemoryUnitAsync(string tenantId, string caseId, string content)
    {
        IngestionInput input = new()
        {
            TenantId = tenantId,
            CaseId = caseId,
            SourceUri = $"file:///{Guid.NewGuid():N}.txt",
            ContentBytes = Encoding.UTF8.GetBytes(content),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/v1/ingest",
            input,
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    private async Task WaitForContainsEdgeAsync(string tenantId, string caseId, int expectedCount)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(2);
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        while (DateTimeOffset.UtcNow < deadline)
        {
            ResultSet result = await falkor.SelectGraph(tenantId).QueryAsync(
                "MATCH (:Case {id: $caseId})-[r:CONTAINS]->(:MemoryUnit) RETURN count(r) AS cnt",
                new Dictionary<string, object> { ["caseId"] = caseId });

            if (ReadCount(result) >= expectedCount)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException($"Case '{caseId}' did not reach {expectedCount} CONTAINS edges in time.");
    }

    private async Task<IReadOnlyList<string>> GetCaseMemoryUnitIdsAsync(string tenantId, string caseId)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        ResultSet result = await falkor.SelectGraph(tenantId).QueryAsync(
            "MATCH (:Case {id: $caseId})-[:CONTAINS]->(m:MemoryUnit) RETURN m.id AS muId",
            new Dictionary<string, object> { ["caseId"] = caseId });

        List<string> ids = [];
        foreach (Record record in result)
        {
            ids.Add(record.GetValue<string>("muId"));
        }

        return ids;
    }

    private async Task SeedReferencesEdgeAsync(string tenantId, string sourceId, string targetId)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        _ = await falkor.SelectGraph(tenantId).QueryAsync(
            "MATCH (s:MemoryUnit {id: $sourceId}), (t:MemoryUnit {id: $targetId}) " +
            "MERGE (s)-[r:REFERENCES]->(t) SET r.createdAt = $createdAt, r.confidence = $confidence, " +
            "r.origin = $origin, r.verifiedBy = $verifiedBy, r.previousConfidence = $previousConfidence",
            new Dictionary<string, object>
            {
                ["sourceId"] = sourceId,
                ["targetId"] = targetId,
                // Tenant export intentionally excludes the newest 500 ms to absorb cross-pod clock drift.
                // Stamp this already-committed fixture edge outside that advisory snapshot window.
                ["createdAt"] = DateTimeOffset.UtcNow.AddSeconds(-1).ToString("o"),
                ["confidence"] = 0.75f,
                ["origin"] = "inferred",
                ["verifiedBy"] = "integration-reviewer",
                ["previousConfidence"] = 0.4f,
            });
    }

    private async Task<Dictionary<string, Dictionary<string, string>>> SnapshotHashesAsync(string pattern)
    {
        IServer server = _fixture.RedisConnection.GetServer(_fixture.RedisConnection.GetEndPoints().Single());
        IDatabase db = _fixture.RedisConnection.GetDatabase();
        Dictionary<string, Dictionary<string, string>> snapshot = [];

        foreach (RedisKey key in server.Keys(pattern: pattern))
        {
            // Story 26.2 review (decision D1): the case-activity feed ({id}:activity, a Redis STREAM) and its
            // {id}:activity:summary hash are operational read-models, NOT part of the backup fidelity contract,
            // so they are excluded from the snapshot (mirrors CaseService.ListCasesAsync). Skipping the stream
            // key also avoids a WRONGTYPE error from HGETALL against a non-hash key.
            if (key.ToString().Contains(":activity", StringComparison.Ordinal))
            {
                continue;
            }

            HashEntry[] entries = await db.HashGetAllAsync(key);
            Dictionary<string, string> fields = new(StringComparer.Ordinal);
            foreach (HashEntry entry in entries)
            {
                fields[entry.Name.ToString()] = entry.Value.ToString();
            }

            snapshot[key.ToString()] = fields;
        }

        return snapshot;
    }

    private async Task<Dictionary<string, byte[]>> SnapshotVectorBytesAsync(string pattern)
    {
        IServer server = _fixture.RedisConnection.GetServer(_fixture.RedisConnection.GetEndPoints().Single());
        IDatabase db = _fixture.RedisConnection.GetDatabase();
        Dictionary<string, byte[]> snapshot = [];

        foreach (RedisKey key in server.Keys(pattern: pattern))
        {
            RedisValue embedding = await db.HashGetAsync(key, "embedding");
            if (!embedding.IsNull)
            {
                snapshot[key.ToString()] = (byte[])embedding!;
            }
        }

        return snapshot;
    }

    private async Task<HashSet<string>> SnapshotEdgesAsync(string tenantId)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        ResultSet result = await falkor.SelectGraph(tenantId).QueryAsync(
            "MATCH (a)-[r]->(b) RETURN a.id AS sourceId, b.id AS targetId, type(r) AS edgeType, " +
            "r.confidence AS confidence, r.origin AS origin, r.verifiedBy AS verifiedBy, " +
            "r.previousConfidence AS previousConfidence");

        HashSet<string> edges = new(StringComparer.Ordinal);
        foreach (Record record in result)
        {
            edges.Add(string.Join(
                '|',
                Convert.ToString(record.GetValue<object?>("sourceId"), CultureInfo.InvariantCulture),
                Convert.ToString(record.GetValue<object?>("targetId"), CultureInfo.InvariantCulture),
                Convert.ToString(record.GetValue<object?>("edgeType"), CultureInfo.InvariantCulture),
                Convert.ToString(record.GetValue<object?>("confidence"), CultureInfo.InvariantCulture),
                Convert.ToString(record.GetValue<object?>("origin"), CultureInfo.InvariantCulture),
                Convert.ToString(record.GetValue<object?>("verifiedBy"), CultureInfo.InvariantCulture),
                Convert.ToString(record.GetValue<object?>("previousConfidence"), CultureInfo.InvariantCulture)));
        }

        return edges;
    }

    private async Task<string> ExportTenantAsync(string tenantId)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync($"/api/v1/tenants/{tenantId}/export");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string json = await response.Content.ReadAsStringAsync();
        json.ShouldNotBeNullOrWhiteSpace();
        return json;
    }

    private async Task WipeDataPlaneAsync(string tenantId)
    {
        IServer server = _fixture.RedisConnection.GetServer(_fixture.RedisConnection.GetEndPoints().Single());
        IDatabase db = _fixture.RedisConnection.GetDatabase();

        foreach (string pattern in new[] { $"{tenantId}:mu:*", $"{tenantId}:vec:*", $"{tenantId}:case:*" })
        {
            foreach (RedisKey key in server.Keys(pattern: pattern))
            {
                await db.KeyDeleteAsync(key);
            }
        }

        // Detach-delete every graph node but keep the (now-empty) tenant graph and its indexes intact.
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        _ = await falkor.SelectGraph(tenantId).QueryAsync("MATCH (n) DETACH DELETE n");
    }

    private async Task RestoreTenantAsync(string tenantId, string exportJson)
    {
        int logStartIndex = _fixture.LogEntryCount;
        using StringContent content = new(exportJson, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync($"/api/v1/tenants/{tenantId}/import", content);
        string responseBody = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            // Aspire forwards child-process stdout/stderr asynchronously; allow the exception log to reach
            // the fixture before composing a failure diagnostic.
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        string recentLogs = string.Join(
            Environment.NewLine,
            _fixture.GetLogEntriesSince(logStartIndex)
                .Select(entry => $"[{entry.Level}] {entry.Category}: {entry.Message}"));
        response.StatusCode.ShouldBe(
            HttpStatusCode.Accepted,
            $"{responseBody}{Environment.NewLine}{recentLogs}");

        RestoreAcceptedResponse? accepted = System.Text.Json.JsonSerializer.Deserialize<RestoreAcceptedResponse>(
            responseBody,
            MemoriesJsonContext.Options);
        accepted.ShouldNotBeNull();

        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(3);
        string lastStatusResponse = "No restore status response was received.";
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage statusResponse = await _fixture.MemoriesClient.GetAsync(
                $"/api/v1/tenants/{tenantId}/restore/{accepted.InstanceId}");
            string statusBody = await statusResponse.Content.ReadAsStringAsync();
            lastStatusResponse = $"HTTP {(int)statusResponse.StatusCode}: {statusBody}";
            if (statusResponse.StatusCode == HttpStatusCode.OK)
            {
                RestoreStatusResponse? status = System.Text.Json.JsonSerializer.Deserialize<RestoreStatusResponse>(
                    statusBody,
                    MemoriesJsonContext.Options);
                if (status is not null)
                {
                    if (string.Equals(status.Status, "completed", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(status.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    if (status.Status.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        string failureLogs = string.Join(
                            Environment.NewLine,
                            _fixture.GetLogEntriesSince(logStartIndex)
                                .TakeLast(80)
                                .Select(entry => $"[{entry.Level}] {entry.Category}: {entry.Message}"));
                        throw new InvalidOperationException(
                            $"Restore workflow failed: {status.Status}. {lastStatusResponse}{Environment.NewLine}{failureLogs}");
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        await Task.Delay(TimeSpan.FromSeconds(1));
        string timeoutLogs = string.Join(
            Environment.NewLine,
            _fixture.GetLogEntriesSince(logStartIndex)
                .TakeLast(80)
                .Select(entry => $"[{entry.Level}] {entry.Category}: {entry.Message}"));
        throw new TimeoutException(
            $"Restore workflow did not complete in time. Last status: {lastStatusResponse}{Environment.NewLine}{timeoutLogs}");
    }

    private static long ReadCount(ResultSet result)
    {
        result.Count.ShouldBe(1);
        using IEnumerator<Record> enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<long>("cnt");
    }
}
