// <copyright file="ConsistencyEndpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Consistency;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Tests.Authentication;
using Hexalith.Memories.Server.Tests.Infrastructure;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Workflows;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 8.2 — HTTP-contract coverage for the five consistency endpoints, including
/// schedule/status happy paths now that the workflow client is abstracted behind
/// <see cref="IConsistencyWorkflowService"/>.
/// </summary>
public sealed class ConsistencyEndpointTests : IDisposable
{
    private const string StoreName = "statestore";
    private const string ValidUlid = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9";

    private readonly ConsistencyEndpointFactory _factory = new();

    [Fact]
    public async Task PostVerify_UnknownTenant_Returns404()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/tenants/unknown-tenant/consistency/verify",
            new { },
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(cancellationToken: CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");
    }

    [Fact]
    public async Task PostVerify_MalformedTenantIdFormat_Returns403BeforeEndpoint()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/tenants/bad~tenant/consistency/verify",
            new { },
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(cancellationToken: CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_FORBIDDEN");
    }

    [Fact]
    public async Task PostVerify_BatchSizeOutOfRange_Returns400WithInvalidBatchSize()
    {
        _factory.StubTenantActive("acme-consistency");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/tenants/acme-consistency/consistency/verify",
            new ConsistencyVerificationRequest("acme-consistency", BatchSize: 9),
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(cancellationToken: CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_BATCH_SIZE");
    }

    [Fact]
    public async Task PostVerify_ActiveTenant_Returns202WithLocationAndWorkflowId()
    {
        _factory.StubTenantActive("acme-consistency");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/tenants/acme-consistency/consistency/verify",
            new ConsistencyVerificationRequest("acme-consistency", BatchSize: 250),
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString().ShouldStartWith("/api/v1/tenants/acme-consistency/consistency/verify/");

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken.None));
        string workflowInstanceId = body.RootElement.GetProperty("workflowInstanceId").GetString().ShouldNotBeNull();
        response.Headers.Location!.ToString().ShouldEndWith($"/{workflowInstanceId}");

        await _factory.WorkflowService.Received(1).ScheduleVerificationAsync(
            Arg.Is<string>(id => id == workflowInstanceId),
            Arg.Is<ConsistencyVerificationInput>(input => input.TenantId == "acme-consistency" && input.BatchSize == 250),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVerifyStatus_InstanceIdMismatch_Returns404WithConsistencyVerifyNotFound()
    {
        _factory.StubTenantActive("acme-consistency");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/v1/tenants/acme-consistency/consistency/verify/repair-consistency-acme-consistency-abcd",
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(cancellationToken: CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("CONSISTENCY_VERIFY_NOT_FOUND");
    }

    [Fact]
    public async Task GetVerifyStatus_KnownInstance_Returns200WithTypedStatus()
    {
        _factory.StubTenantActive("acme-consistency");
        _factory.WorkflowService
            .GetVerificationStatusAsync("verify-consistency-acme-consistency-abc123", Arg.Any<CancellationToken>())
            .Returns(new ConsistencyVerificationStatus(
                "verify-consistency-acme-consistency-abc123",
                "Completed",
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow,
                new ConsistencyWorkflowProgress("completed", 1, 1),
                new ConsistencyVerificationResult(
                    "acme-consistency",
                    TotalUnits: 2,
                    ConsistentCount: 1,
                    InconsistentCount: 1,
                    Discrepancies:
                    [
                        new ConsistencyDiscrepancy(
                            ValidUlid,
                            SyntacticPresent: true,
                            SemanticPresent: false,
                            GraphPresent: true,
                            ConsistencyRepairRecommendation.ReIndexSemantic),
                    ],
                    TotalDiscrepancyCount: 1,
                    TruncatedAt: null,
                    EnumerationTruncated: false,
                    StartedAt: DateTimeOffset.UtcNow.AddMinutes(-2),
                    CompletedAt: DateTimeOffset.UtcNow,
                    Duration: TimeSpan.FromSeconds(3))));

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/v1/tenants/acme-consistency/consistency/verify/verify-consistency-acme-consistency-abc123",
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ConsistencyVerificationStatus? status = await response.Content.ReadFromJsonAsync<ConsistencyVerificationStatus>(
            MemoriesJsonContext.Options,
            cancellationToken: CancellationToken.None);
        status.ShouldNotBeNull();
        status.Progress.ShouldNotBeNull();
        status.Result.ShouldNotBeNull();
        status.Result.InconsistentCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetInspect_UnknownOpaqueMemoryUnit_Returns404WithExactValueRecoveryGuidance()
    {
        const string opaqueId = "wf-file-instance-7";
        _factory.StubTenantActive("acme-consistency");
        _factory.InspectionService
            .InspectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Memory unit not found"));

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/tenants/acme-consistency/consistency/inspect/{opaqueId}",
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(cancellationToken: CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("MEMORY_UNIT_NOT_FOUND");
        error.Suggestion.ShouldContain("exact non-blank MemoryUnitId", Shouldly.Case.Sensitive);
        error.Suggestion.ShouldNotContain("ULID", Shouldly.Case.Insensitive);
        error.Suggestion.ShouldNotContain("GUID", Shouldly.Case.Insensitive);
        await _factory.InspectionService.Received(1).InspectAsync(
            "acme-consistency",
            opaqueId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetInspect_InvalidCallableInput_Returns400WithExactValueRecoveryGuidance()
    {
        const string opaqueId = "Wf-File_Instance-7";
        _factory.StubTenantActive("acme-consistency");
        _factory.InspectionService
            .InspectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Memory unit identifier is invalid."));

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/tenants/acme-consistency/consistency/inspect/{opaqueId}",
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(cancellationToken: CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_MEMORY_UNIT_ID");
        error.Suggestion.ShouldContain("exact non-blank MemoryUnitId", Shouldly.Case.Sensitive);
        error.Suggestion.ShouldNotContain("ULID", Shouldly.Case.Insensitive);
        error.Suggestion.ShouldNotContain("GUID", Shouldly.Case.Insensitive);
    }

    [Fact]
    public async Task GetInspect_KnownOpaqueMemoryUnit_ForwardsAndReturnsExactIdentifier()
    {
        const string opaqueId = "Wf File%?#[]*";
        _factory.StubTenantActive("acme-consistency");
        ConsistencyInspectionResult stubResult = new(
            "acme-consistency",
            opaqueId,
            SyntacticPresent: true,
            SemanticPresent: true,
            GraphPresent: true,
            SyntacticDetail: new ConsistencySyntacticDetail(
                "hash", DateTimeOffset.UtcNow, "file:///sample.md", "file", "case-1", "gemini", "gemini-embedding-001"),
            SemanticDetail: new ConsistencySemanticDetail(768, IndexSchemaDefinitions.BuildSemanticKey("acme-consistency", opaqueId)),
            GraphDetail: new ConsistencyGraphDetail(2, 1, 1),
            Recommendation: ConsistencyRepairRecommendation.NoOp,
            CheckedAt: DateTimeOffset.UtcNow);
        _factory.InspectionService
            .InspectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(stubResult);

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/tenants/acme-consistency/consistency/inspect/{Uri.EscapeDataString(opaqueId)}",
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ConsistencyInspectionResult? body = await response.Content
            .ReadFromJsonAsync<ConsistencyInspectionResult>(
                MemoriesJsonContext.Options,
                cancellationToken: CancellationToken.None);
        body.ShouldNotBeNull();
        body.Recommendation.ShouldBe(ConsistencyRepairRecommendation.NoOp);
        body.SyntacticDetail.ShouldNotBeNull();
        body.SemanticDetail.ShouldNotBeNull();
        body.GraphDetail.ShouldNotBeNull();
        body.MemoryUnitId.ShouldBe(opaqueId);
        await _factory.InspectionService.Received(1).InspectAsync(
            "acme-consistency",
            opaqueId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetInspect_BackendUnavailable_Preserves503ErrorMapping()
    {
        const string opaqueId = "wf-file-instance-7";
        _factory.StubTenantActive("acme-consistency");
        _factory.InspectionService
            .InspectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/tenants/acme-consistency/consistency/inspect/{opaqueId}",
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(cancellationToken: CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("BACKEND_UNAVAILABLE");
    }

    [Fact]
    public async Task PostRepair_BatchSizeOutOfRange_Returns400WithInvalidBatchSize()
    {
        _factory.StubTenantActive("acme-consistency");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/tenants/acme-consistency/consistency/repair",
            new ConsistencyRepairRequest("acme-consistency", BatchSize: 10_000),
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(cancellationToken: CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_BATCH_SIZE");
    }

    [Fact]
    public async Task PostRepair_ActiveTenant_Returns202WithLocationAndWorkflowId()
    {
        _factory.StubTenantActive("acme-consistency");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/tenants/acme-consistency/consistency/repair",
            new ConsistencyRepairRequest("acme-consistency", BatchSize: 125, IncludeUnrepairable: true),
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString().ShouldStartWith("/api/v1/tenants/acme-consistency/consistency/repair/");

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken.None));
        string workflowInstanceId = body.RootElement.GetProperty("workflowInstanceId").GetString().ShouldNotBeNull();
        response.Headers.Location!.ToString().ShouldEndWith($"/{workflowInstanceId}");

        await _factory.WorkflowService.Received(1).ScheduleRepairAsync(
            Arg.Is<string>(id => id == workflowInstanceId),
            Arg.Is<ConsistencyRepairInput>(input => input.TenantId == "acme-consistency" && input.BatchSize == 125 && input.IncludeUnrepairable),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRepairStatus_KnownInstance_Returns200WithTypedStatus()
    {
        _factory.StubTenantActive("acme-consistency");
        _factory.WorkflowService
            .GetRepairStatusAsync("repair-consistency-acme-consistency-abc123", Arg.Any<CancellationToken>())
            .Returns(new ConsistencyRepairStatus(
                "repair-consistency-acme-consistency-abc123",
                "Completed",
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow,
                new ConsistencyWorkflowProgress("completed", 1, 1),
                new ConsistencyRepairResult(
                    "acme-consistency",
                    TotalDiscrepancies: 1,
                    RepairedCount: 1,
                    UnrepairableCount: 0,
                    Actions:
                    [
                        new RepairActionRecord(
                            ValidUlid,
                            ConsistencyRepairRecommendation.ReIndexSemantic,
                            Succeeded: true,
                            FailureReason: null,
                            BeforeState: new Dictionary<string, string>(),
                            AfterState: new Dictionary<string, string>()),
                    ],
                    PassesExecuted: 1,
                    StartedAt: DateTimeOffset.UtcNow.AddMinutes(-2),
                    CompletedAt: DateTimeOffset.UtcNow,
                    Duration: TimeSpan.FromSeconds(4))));

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/v1/tenants/acme-consistency/consistency/repair/repair-consistency-acme-consistency-abc123",
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ConsistencyRepairStatus? status = await response.Content.ReadFromJsonAsync<ConsistencyRepairStatus>(
            MemoriesJsonContext.Options,
            cancellationToken: CancellationToken.None);
        status.ShouldNotBeNull();
        status.Progress.ShouldNotBeNull();
        status.Result.ShouldNotBeNull();
        status.Result.RepairedCount.ShouldBe(1);
    }

    [Fact]
    public async Task ConsistencyEndpoints_DoNotEmitAccessTelemetryAuditEvents()
    {
        _factory.StubTenantActive("acme-consistency");
        _factory.InspectionService
            .InspectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ConsistencyInspectionResult(
                "acme-consistency",
                ValidUlid,
                SyntacticPresent: true,
                SemanticPresent: true,
                GraphPresent: true,
                SyntacticDetail: new ConsistencySyntacticDetail(
                    "hash", DateTimeOffset.UtcNow, "file:///sample.md", "file", "case-1", "gemini", "gemini-embedding-001"),
                SemanticDetail: new ConsistencySemanticDetail(768, IndexSchemaDefinitions.BuildSemanticKey("acme-consistency", ValidUlid)),
                GraphDetail: new ConsistencyGraphDetail(1, 2, 1),
                Recommendation: ConsistencyRepairRecommendation.NoOp,
                CheckedAt: DateTimeOffset.UtcNow));

        int capturedBefore = _factory.AuditLogs.AccessTelemetryCaptures.Count;
        using HttpClient client = _factory.CreateClient();

        _ = await client.PostAsJsonAsync(
            "/api/v1/tenants/acme-consistency/consistency/verify",
            new ConsistencyVerificationRequest("acme-consistency"),
            CancellationToken.None);
        _ = await client.GetAsync($"/api/v1/tenants/acme-consistency/consistency/inspect/{ValidUlid}", CancellationToken.None);
        _ = await client.PostAsJsonAsync(
            "/api/v1/tenants/acme-consistency/consistency/repair",
            new ConsistencyRepairRequest("acme-consistency", IncludeUnrepairable: true),
            CancellationToken.None);

        _factory.AuditLogs.AccessTelemetryCaptures.Count.ShouldBe(capturedBefore);
    }

    public void Dispose() => _factory.Dispose();

    /// <summary>
    /// Consistency-specific <see cref="WebApplicationFactory{TEntryPoint}"/> — mirrors the
    /// telemetry factory shape but adds overrides for <see cref="IConsistencyInspectionService"/>.
    /// </summary>
    private sealed class ConsistencyEndpointFactory : WebApplicationFactory<Program>
    {
        public DaprClient DaprClient { get; } = Substitute.For<DaprClient>();

        public IConsistencyInspectionService InspectionService { get; } = Substitute.For<IConsistencyInspectionService>();

        public IConsistencyWorkflowService WorkflowService { get; } = Substitute.For<IConsistencyWorkflowService>();

        public CapturingAuditLoggerProvider AuditLogs { get; } = new();

        public void StubTenantActive(string tenantId)
        {
            TenantRegistryEntry entry = new(
                new TenantInfo(tenantId, tenantId, TenantStatus.Active, DateTimeOffset.UtcNow),
                WorkflowInstanceId: null);

            DaprClient
                .GetStateAsync<StoredTenantRegistryEntry?>(
                    StoreName,
                    Arg.Is<string>(k => k.Contains(tenantId, StringComparison.Ordinal)),
                    Arg.Any<ConsistencyMode?>(),
                    Arg.Any<IReadOnlyDictionary<string, string>?>(),
                    Arg.Any<CancellationToken>())
                .Returns(entry);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:redis", "localhost:0,abortConnect=false,connectTimeout=1");
            builder.UseSetting("ConnectionStrings:falkordb", "localhost:0,abortConnect=false,connectTimeout=1");

            builder.ConfigureTestServices(services =>
            {
                services.AddKeyedSingleton<IConnectionMultiplexer>(
                    "redis",
                    (_, _) => Substitute.For<IConnectionMultiplexer>());
                services.AddKeyedSingleton<IConnectionMultiplexer>(
                    "falkordb",
                    (_, _) => Substitute.For<IConnectionMultiplexer>());

                services.RemoveAll<DaprClient>();
                services.AddSingleton<DaprClient>(DaprClient);

                services.RemoveAll<IConsistencyInspectionService>();
                services.AddSingleton<IConsistencyInspectionService>(InspectionService);

                services.RemoveAll<IConsistencyWorkflowService>();
                services.AddSingleton<IConsistencyWorkflowService>(WorkflowService);

                // Endpoint tests drive services directly; infrastructure services would resolve DAPR/backends at startup.
                services.RemoveInfrastructureHostedServices();

                services.AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider>(AuditLogs);
            });
        }

        protected override void ConfigureClient(HttpClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            base.ConfigureClient(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                ServerTestBearerToken.Create(tenants:
                [
                    "acme-consistency",
                    "unknown-tenant",
                ]));
        }
    }
}
