// <copyright file="MemoriesClientConsistencyTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.ClientRest;

using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>
/// Story 8.2 — <see cref="MemoriesClient"/> consistency methods (Task 5). Mirrors the
/// <see cref="MemoriesClientTests"/> <c>TestDelegatingHandler</c> pattern.
/// </summary>
public class MemoriesClientConsistencyTests
{
    private static readonly Uri Endpoint = new("http://127.0.0.1:5000/");

    [Fact]
    public async Task StartConsistencyVerificationAsync_202Response_ReturnsStatusUrl()
    {
        const string TenantId = "tenant-1";
        const string InstanceId = "verify-consistency-tenant-1-abc123";
        Uri expected = new(Endpoint, $"api/tenants/{TenantId}/consistency/verify/{InstanceId}");

        MemoriesClient client = CreateClient(HttpStatusCode.Accepted, location: new Uri($"/api/tenants/{TenantId}/consistency/verify/{InstanceId}", UriKind.Relative));

        Uri result = await client.StartConsistencyVerificationAsync(
            TenantId,
            new ConsistencyVerificationRequest(TenantId),
            CancellationToken.None);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task GetConsistencyVerificationStatusAsync_200Response_DeserializesWorkflowState()
    {
        const string InstanceId = "verify-consistency-tenant-1-abc";
        ConsistencyVerificationStatus state = new(
            InstanceId,
            "Completed",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            new ConsistencyWorkflowProgress("completed", 1, 1),
            new ConsistencyVerificationResult(
                "tenant-1",
                TotalUnits: 2,
                ConsistentCount: 1,
                InconsistentCount: 1,
                Discrepancies:
                [
                    new ConsistencyDiscrepancy(
                        "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
                        SyntacticPresent: true,
                        SemanticPresent: false,
                        GraphPresent: true,
                        ConsistencyRepairRecommendation.ReIndexSemantic),
                ],
                TotalDiscrepancyCount: 1,
                TruncatedAt: null,
                EnumerationTruncated: false,
                StartedAt: DateTimeOffset.UtcNow.AddMinutes(-2),
                CompletedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
                Duration: TimeSpan.FromMinutes(1)));

        string body = JsonSerializer.Serialize(state, MemoriesJsonContext.Options);
        MemoriesClient client = CreateClient(HttpStatusCode.OK, body);

        ConsistencyVerificationStatus? result = await client.GetConsistencyVerificationStatusAsync(
            "tenant-1", InstanceId, CancellationToken.None);

        result.ShouldNotBeNull();
        result.InstanceId.ShouldBe(InstanceId);
        result.Status.ShouldBe("Completed");
        result.Result.ShouldNotBeNull();
        result.Result.InconsistentCount.ShouldBe(1);
    }

    [Fact]
    public async Task InspectConsistencyAsync_200Response_DeserializesInspectionResult()
    {
        const string TenantId = "tenant-1";
        const string MemoryUnitId = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9";

        ConsistencyInspectionResult expected = new(
            TenantId,
            MemoryUnitId,
            SyntacticPresent: true,
            SemanticPresent: true,
            GraphPresent: false,
            SyntacticDetail: new ConsistencySyntacticDetail(
                "hash", DateTimeOffset.UtcNow, "file:///x", "file", "case-1", "gemini", "gemini-embedding-001"),
            SemanticDetail: new ConsistencySemanticDetail(768, $"{TenantId}:vec:{MemoryUnitId}"),
            GraphDetail: null,
            Recommendation: ConsistencyRepairRecommendation.ReIndexGraph,
            CheckedAt: DateTimeOffset.UtcNow);

        string body = JsonSerializer.Serialize(expected, MemoriesJsonContext.Options);
        MemoriesClient client = CreateClient(HttpStatusCode.OK, body);

        ConsistencyInspectionResult result = await client.InspectConsistencyAsync(
            TenantId, MemoryUnitId, CancellationToken.None);

        result.Recommendation.ShouldBe(ConsistencyRepairRecommendation.ReIndexGraph);
        result.SyntacticDetail.ShouldNotBeNull();
        result.GraphDetail.ShouldBeNull();
    }

    [Fact]
    public async Task InspectConsistencyAsync_404Response_ThrowsMemoriesRemoteExceptionWithCode()
    {
        string body = JsonSerializer.Serialize(
            new ErrorResponse("MEMORY_UNIT_NOT_FOUND", "not found", "run verify"),
            MemoriesJsonContext.Options);
        MemoriesClient client = CreateClient(HttpStatusCode.NotFound, body);

        MemoriesRemoteException ex = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.InspectConsistencyAsync("tenant-1", "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9", CancellationToken.None));

        ex.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ex.Error.Code.ShouldBe("MEMORY_UNIT_NOT_FOUND");
    }

    [Fact]
    public async Task StartConsistencyRepairAsync_202Response_ReturnsStatusUrl()
    {
        const string TenantId = "tenant-1";
        const string InstanceId = "repair-consistency-tenant-1-xyz789";
        Uri expected = new(Endpoint, $"api/tenants/{TenantId}/consistency/repair/{InstanceId}");
        MemoriesClient client = CreateClient(HttpStatusCode.Accepted, location: new Uri($"/api/tenants/{TenantId}/consistency/repair/{InstanceId}", UriKind.Relative));

        Uri result = await client.StartConsistencyRepairAsync(
            TenantId,
            new ConsistencyRepairRequest(TenantId, IncludeUnrepairable: true),
            CancellationToken.None);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task GetConsistencyRepairStatusAsync_200Response_DeserializesWorkflowState()
    {
        const string InstanceId = "repair-consistency-tenant-1-xyz";
        ConsistencyRepairStatus state = new(
            InstanceId,
            "Running",
            DateTimeOffset.UtcNow.AddMinutes(-2),
            DateTimeOffset.UtcNow,
            new ConsistencyWorkflowProgress("repairing", 1, 2),
            null);
        string body = JsonSerializer.Serialize(state, MemoriesJsonContext.Options);
        MemoriesClient client = CreateClient(HttpStatusCode.OK, body);

        ConsistencyRepairStatus? result = await client.GetConsistencyRepairStatusAsync(
            "tenant-1", InstanceId, CancellationToken.None);

        result.ShouldNotBeNull();
        result.InstanceId.ShouldBe(InstanceId);
        result.Status.ShouldBe("Running");
        result.Progress.ShouldNotBeNull();
        result.Progress.CurrentPhase.ShouldBe("repairing");
    }

    private static MemoriesClient CreateClient(HttpStatusCode status, string body = "", Uri? location = null)
    {
        var handler = new TestDelegatingHandler((_, _) =>
        {
            HttpResponseMessage response = new(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            if (location is not null)
            {
                response.Headers.Location = location;
            }

            return Task.FromResult(response);
        });
        var httpClient = new HttpClient(handler) { BaseAddress = Endpoint };
        IOptions<MemoriesClientOptions> options = Options.Create(new MemoriesClientOptions { Endpoint = Endpoint });
        return new MemoriesClient(httpClient, options, NullLogger<MemoriesClient>.Instance);
    }
}
