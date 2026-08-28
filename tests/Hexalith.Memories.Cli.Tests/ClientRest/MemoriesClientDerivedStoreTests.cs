// <copyright file="MemoriesClientDerivedStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.ClientRest;

using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Contracts.V1.DerivedStores;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

public class MemoriesClientDerivedStoreTests
{
    private static readonly Uri Endpoint = new("http://127.0.0.1:5000/");

    [Fact]
    public async Task PutDiagnosticEntryAsync_UsesTenantClassAndResourceRoute()
    {
        var handler = new TestDelegatingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        MemoriesClient client = CreateClient(handler);

        await client.PutDiagnosticEntryAsync(
            "tenant-a",
            DiagnosticStoreClass.VectorIndex,
            "probe-1",
            new DiagnosticStoreEntry("probe-1", "digest-1"),
            TestContext.Current.CancellationToken);

        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Put);
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldBe("/api/v1/tenants/tenant-a/diagnostics/derived-stores/VectorIndex/probe-1");
    }

    [Fact]
    public async Task GetDiagnosticEntryAsync_ForeignTenantMiss_ReturnsNull()
    {
        var handler = new TestDelegatingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        MemoriesClient client = CreateClient(handler);

        DiagnosticStoreEntry? result = await client.GetDiagnosticEntryAsync(
            "tenant-b",
            DiagnosticStoreClass.VectorIndex,
            "probe-1",
            TestContext.Current.CancellationToken);

        result.ShouldBeNull();
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldContain("/tenants/tenant-b/");
    }

    [Fact]
    public async Task StartOrRejoinDerivedStoreCorrectionAsync_ReadsDurableStatus()
    {
        var expected = new DerivedStoreCorrectionStatus(
            "derived-correction-abc",
            DerivedStoreCorrectionState.Pending,
            "association-1",
            "intake-1",
            "correction-1",
            5,
            "case-prior",
            "case-corrected",
            0,
            0,
            false,
            DateTimeOffset.UtcNow.AddHours(1),
            null,
            null);
        string json = JsonSerializer.Serialize(expected, MemoriesJsonContext.Options);
        var handler = new TestDelegatingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        }));
        MemoriesClient client = CreateClient(handler);

        DerivedStoreCorrectionStatus actual = await client.StartOrRejoinDerivedStoreCorrectionAsync(
            "tenant-a",
            new StartDerivedStoreCorrectionRequest("association-1", "intake-1", "correction-1", 5, "case-corrected"),
            TestContext.Current.CancellationToken);

        actual.ShouldBe(expected);
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldBe("/api/v1/tenants/tenant-a/derived-stores/corrections");
    }

    [Fact]
    public async Task GetIngestionWorkflowStatusAsync_ReadsCanonicalTerminalIdentity()
    {
        var expected = new IngestionWorkflowStatus(
            "workflow-1",
            "tenant-a",
            "case-1",
            "Completed",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            "unit-1",
            MemoryUnitStatus.Indexed,
            null);
        string json = JsonSerializer.Serialize(expected, MemoriesJsonContext.Options);
        var handler = new TestDelegatingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        }));
        MemoriesClient client = CreateClient(handler);

        IngestionWorkflowStatus actual = await client.GetIngestionWorkflowStatusAsync(
            "workflow-1",
            TestContext.Current.CancellationToken);

        actual.ShouldBe(expected);
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldBe("/api/v1/ingest/workflow-1");
    }

    private static MemoriesClient CreateClient(TestDelegatingHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = Endpoint };
        return new MemoriesClient(
            httpClient,
            Options.Create(new MemoriesClientOptions { Endpoint = Endpoint }),
            NullLogger<MemoriesClient>.Instance);
    }
}
