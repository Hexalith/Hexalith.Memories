// <copyright file="DirectoryIngestionIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.AppHost;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class DirectoryIngestionIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public DirectoryIngestionIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task DirectoryIngestion_MixedFiles_ShouldIndexSupportedAndSkipUnsupported()
    {
        IngestionIntegrationTestDriver driver = new(_fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-directory-{unique[..10]}";
        string caseId = await driver.CreateTenantAndCaseAsync(tenantId);
        string directory = Path.Combine(RepositoryRootLocator.Resolve(), "test-data", $"story-26-3-{unique}");
        Directory.CreateDirectory(directory);

        try
        {
            string[] supported = ["document.md", "sample.pdf", "notes.txt", "page.html", "payload.json"];
            string[] unsupported = ["program.exe", "archive.iso"];
            foreach (string file in supported)
            {
                string destination = Path.Combine(directory, file);
                if (Path.GetExtension(file).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(
                        Path.Combine(
                            RepositoryRootLocator.Resolve(),
                            "tests",
                            "Hexalith.Memories.Server.Tests",
                            "Fixtures",
                            "sample.pdf"),
                        destination);
                    continue;
                }

                string content = Path.GetExtension(file) switch
                {
                    ".md" => $"# Directory ingestion\n\nCanary {unique}.",
                    ".html" => $"<html><body>Directory canary {unique}.</body></html>",
                    ".json" => $$"""{"canary":"{{unique}}"}""",
                    _ => $"Directory ingestion canary {unique} from {file}.",
                };
                await File.WriteAllTextAsync(destination, content);
            }

            foreach (string file in unsupported)
            {
                await File.WriteAllTextAsync(Path.Combine(directory, file), $"Unsupported canary {unique}.");
            }

            using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
                "/api/v1/ingest/directory",
                new DirectoryIngestionRequest
                {
                    TenantId = tenantId,
                    CaseId = caseId,
                    DirectoryPath = directory,
                    IngestedBy = "integration@test.local",
                    Recursive = false,
                },
                MemoriesJsonContext.Options);
            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            DirectoryIngestionOutcome? outcome = await response.Content.ReadFromJsonAsync<DirectoryIngestionOutcome>(
                MemoriesJsonContext.Options);
            outcome.ShouldNotBeNull();
            outcome.Discovered.ShouldBe(7);
            outcome.Enqueued.ShouldBe(5);
            outcome.Skipped.Count.ShouldBe(2);
            outcome.Skipped.ShouldAllBe(item => item.Reason == "UNSUPPORTED_EXTENSION");
            outcome.Skipped.Select(item => Path.GetExtension(item.Path)).Order().ShouldBe([".exe", ".iso"]);
            outcome.InstanceIds.Count.ShouldBe(5);
            outcome.InstanceIds.Distinct(StringComparer.Ordinal).Count().ShouldBe(5);

            BatchStatusResponse batch = await WaitForBatchAsync(outcome.BatchId, tenantId);
            batch.Counts.Indexed.ShouldBe(5);
            batch.Counts.Failed.ShouldBe(0);
            batch.Instances.ShouldAllBe(instance => instance.Status == "indexed" && instance.MemoryUnitId != null);
            (await driver.ListRedisKeysAsync($"{tenantId}:mu:*")).Length.ShouldBe(5);
            (await driver.ListRedisKeysAsync($"{tenantId}:vec:*")).Length.ShouldBe(5);
            foreach (BatchInstanceStatus instance in batch.Instances)
            {
                (await driver.CountGraphNodesAsync(tenantId, caseId, instance.SourceUri)).ShouldBe(1);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact(Skip = "26.3-DIRECTORY-CROSS-TENANT-PERF: This latency-isolation claim needs controlled concurrent load and repeatable timing bounds unavailable in the functional integration lane. Owner: performance test maintainers. Unskip when: the performance lane provisions two tenants and publishes an accepted latency budget.")]
    public void DirectoryIngestion_CrossTenantIsolation_ShouldNotSerialize()
    {
        // Scenario (Story 6.1 AC11):
        //   Schedule a 100-file batch for t1, simultaneously schedule a single-file ingest for t2,
        //   assert t2 latency stays within 2× single-tenant baseline. Coarse assertion; true load
        //   isolation / chaos tests are deferred to Phase 2.
    }

    private async Task<BatchStatusResponse> WaitForBatchAsync(string batchId, string tenantId)
    {
        using CancellationTokenSource cts = new(IngestionIntegrationTestDriver.DefaultTimeout);
        BatchStatusResponse? last = null;
        while (!cts.IsCancellationRequested)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/ingest/batches/{batchId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                AspireIngestionPipelineFixture.MintServerBearer(tenantId));
            using HttpResponseMessage response = await _fixture.MemoriesClient.SendAsync(request, cts.Token);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                last = await response.Content.ReadFromJsonAsync<BatchStatusResponse>(
                    MemoriesJsonContext.Options,
                    cts.Token);
                if (last is not null && last.Counts.Indexed + last.Counts.Failed == last.Enqueued)
                {
                    return last;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
        }

        throw new TimeoutException($"Directory batch '{batchId}' did not converge. Last response: {last}");
    }
}
