// <copyright file="TenantLifecycleEndpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Dapr;
using Dapr.Client;
using Dapr.Workflow;
using Grpc.Core;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Tests.Authentication;
using Hexalith.Memories.Server.Tests.EventStoreIntegration;
using Hexalith.Memories.Server.Workflows;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using Shouldly;

/// <summary>Tests tenant lifecycle HTTP results against the registered workflow and state-store seams.</summary>
public sealed class TenantLifecycleEndpointTests : IDisposable
{
    private const string TenantId = "tenant-a";
    private const string StateStore = "statestore";
    private const string StateKey = "tenant-registry-" + TenantId;
    private const string InternalError = "private sidecar diagnostic";

    private readonly EventStoreWebAppFactory _factory = new();
    private readonly IDaprWorkflowClient _workflowClient = Substitute.For<IDaprWorkflowClient>();
    private readonly WebApplicationFactory<Program> _host;

    public TenantLifecycleEndpointTests()
    {
        _host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton(_workflowClient)));
    }

    [Theory]
    [InlineData("provision", "provision", false)]
    [InlineData("provision", "provision", true)]
    [InlineData("deletion", "delete", false)]
    [InlineData("deletion", "delete", true)]
    public async Task GetStatus_WorkflowSidecarUnavailable_ReturnsServiceUnavailable(
        string route, string workflowPrefix, bool grpc)
    {
        StubActiveTenant();
        string instanceId = $"{workflowPrefix}-{TenantId}-{Guid.NewGuid():N}";
        Exception failure = grpc
            ? new RpcException(new Status(StatusCode.Unavailable, InternalError))
            : new DaprException(InternalError);
        _workflowClient.GetWorkflowStateAsync(instanceId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<WorkflowState>(failure));
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/tenants/{TenantId}/{route}-status/{instanceId}",
            TestContext.Current.CancellationToken);

        await AssertErrorAsync(response, HttpStatusCode.ServiceUnavailable, "DAPR_UNAVAILABLE");
        await _workflowClient.Received(1).GetWorkflowStateAsync(instanceId, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDeletionStatus_InboxRetry_ReportsSubsequentSidecarOutage()
    {
        StubActiveTenant();
        string instanceId = $"delete-{TenantId}-{Guid.NewGuid():N}";
        _workflowClient.GetWorkflowStateAsync(instanceId, true, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<WorkflowState>(
                    new RpcException(new Status(StatusCode.Unknown, "inbox key was declared in metadata"))),
                Task.FromException<WorkflowState>(
                    new RpcException(new Status(StatusCode.Unavailable, InternalError))));
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/tenants/{TenantId}/deletion-status/{instanceId}",
            TestContext.Current.CancellationToken);

        await AssertErrorAsync(response, HttpStatusCode.ServiceUnavailable, "DAPR_UNAVAILABLE");
        await _workflowClient.Received(2).GetWorkflowStateAsync(instanceId, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStatus_SidecarNeverBecomesReady_CancelsReadAndReturnsServiceUnavailable()
    {
        StubActiveTenant();
        string instanceId = $"provision-{TenantId}-{Guid.NewGuid():N}";
        TaskCompletionSource<WorkflowState> pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken dependencyToken = default;
        _workflowClient.GetWorkflowStateAsync(instanceId, true, Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                dependencyToken = call.Arg<CancellationToken>();
                return pendingRead.Task;
            });
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/tenants/{TenantId}/provision-status/{instanceId}",
            TestContext.Current.CancellationToken);

        await AssertErrorAsync(response, HttpStatusCode.ServiceUnavailable, "DAPR_UNAVAILABLE");
        dependencyToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Theory]
    [InlineData("provision", "provision")]
    [InlineData("deletion", "delete")]
    public async Task GetStatus_RegistrySidecarUnavailable_ReturnsServiceUnavailable(string route, string workflowPrefix)
    {
        _factory.DaprClient.GetStateAsync<StoredTenantRegistryEntry?>(
                StateStore, StateKey, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StoredTenantRegistryEntry?>(new DaprException(
                InternalError, new RpcException(new Status(StatusCode.Unavailable, InternalError)))));
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/tenants/{TenantId}/{route}-status/{workflowPrefix}-{TenantId}-{Guid.NewGuid():N}",
            TestContext.Current.CancellationToken);

        await AssertErrorAsync(response, HttpStatusCode.ServiceUnavailable, "DAPR_UNAVAILABLE");
        _workflowClient.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("provision", "provision")]
    [InlineData("deletion", "delete")]
    public async Task GetStatus_NonAvailabilityRegistryFailure_IsNotReportedAsSidecarOutage(
        string route, string workflowPrefix)
    {
        _factory.DaprClient.GetStateAsync<StoredTenantRegistryEntry?>(
                StateStore, StateKey, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StoredTenantRegistryEntry?>(new DaprException(
                InternalError, new RpcException(new Status(StatusCode.PermissionDenied, InternalError)))));
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/tenants/{TenantId}/{route}-status/{workflowPrefix}-{TenantId}-{Guid.NewGuid():N}",
            TestContext.Current.CancellationToken);

        await AssertErrorAsync(response, HttpStatusCode.InternalServerError, "UNHANDLED_EXCEPTION");
        _workflowClient.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("provision", "provision", "tenant-b", "PROVISIONING_STATUS_NOT_FOUND")]
    [InlineData("deletion", "delete", "tenant-b", "DELETION_STATUS_NOT_FOUND")]
    [InlineData("provision", "provision", "tenant-a-b", "PROVISIONING_STATUS_NOT_FOUND")]
    [InlineData("deletion", "delete", "tenant-a-b", "DELETION_STATUS_NOT_FOUND")]
    public async Task GetStatus_WrongWorkflowTenant_RejectsBeforeWorkflowLookup(
        string route, string workflowPrefix, string workflowTenant, string expectedCode)
    {
        StubActiveTenant();
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/tenants/{TenantId}/{route}-status/{workflowPrefix}-{workflowTenant}-{Guid.NewGuid():N}",
            TestContext.Current.CancellationToken);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, expectedCode);
        _workflowClient.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("provision", "provision")]
    [InlineData("deletion", "delete")]
    public async Task GetStatus_UnauthorizedTenant_RejectsBeforeRegistryOrWorkflowLookup(
        string route, string workflowPrefix)
    {
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/tenants/tenant-b/{route}-status/{workflowPrefix}-tenant-b-{Guid.NewGuid():N}",
            TestContext.Current.CancellationToken);

        await AssertErrorAsync(response, HttpStatusCode.Forbidden, "TENANT_FORBIDDEN");
        _factory.DaprClient.ReceivedCalls().ShouldBeEmpty();
        _workflowClient.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Delete_ScheduleOutage_PreservesServiceUnavailableAndDeletionOwnership(
        bool grpc, bool rollbackFails)
    {
        StubActiveTenant();
        StoredTenantRegistryEntry persisted = new TenantRegistryEntry(
            new TenantInfo(TenantId, TenantId, TenantStatus.Active, DateTimeOffset.UtcNow),
            WorkflowInstanceId: null);
        string etag = "active";
        _factory.DaprClient.GetStateAndETagAsync<StoredTenantRegistryEntry?>(
                StateStore, StateKey, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ((StoredTenantRegistryEntry?)persisted, etag));
        _factory.DaprClient.TrySaveStateAsync(
                StateStore, StateKey, Arg.Any<StoredTenantRegistryEntry>(),
                Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.ArgAt<string>(3).ShouldBe(etag);
                if (rollbackFails && etag == "deleting")
                {
                    return Task.FromException<bool>(new IOException(InternalError));
                }

                persisted = call.Arg<StoredTenantRegistryEntry>();
                etag = persisted.Tenant.Status == TenantStatus.Deleting ? "deleting" : "restored";
                return Task.FromResult(true);
            });

        Exception failure = grpc
            ? new RpcException(new Status(StatusCode.Unavailable, InternalError))
            : new DaprException(InternalError, new IOException(InternalError));
        string? attemptedInstanceId = null;
        _workflowClient.ScheduleNewWorkflowAsync(
                nameof(TenantDeletionWorkflow), Arg.Any<string>(), Arg.Any<TenantDeletionInput>())
            .Returns(call =>
            {
                attemptedInstanceId = call.ArgAt<string>(1);
                persisted.WorkflowInstanceId.ShouldBe(attemptedInstanceId);
                persisted.Tenant.Status.ShouldBe(TenantStatus.Deleting);
                return Task.FromException<string>(failure);
            });
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.DeleteAsync(
            $"/api/v1/tenants/{TenantId}", TestContext.Current.CancellationToken);

        await AssertErrorAsync(response, HttpStatusCode.ServiceUnavailable, "DAPR_UNAVAILABLE");
        attemptedInstanceId.ShouldNotBeNullOrWhiteSpace();
        await _factory.DaprClient.Received(grpc ? 0 : 1).TrySaveStateAsync(
            StateStore, StateKey, Arg.Is<StoredTenantRegistryEntry>(entry => entry.Tenant.Status == TenantStatus.Active),
            "deleting", cancellationToken: Arg.Any<CancellationToken>());
        bool claimRemains = grpc || rollbackFails;
        persisted.Tenant.Status.ShouldBe(claimRemains ? TenantStatus.Deleting : TenantStatus.Active);
        persisted.WorkflowInstanceId.ShouldBe(claimRemains ? attemptedInstanceId : null);
        var warnings = _factory.AuditLogs.Captures
            .Where(log => log.Message.StartsWith("Tenant deletion rollback", StringComparison.Ordinal)).ToArray();
        warnings.Length.ShouldBe(rollbackFails ? 1 : 0);
        if (rollbackFails)
        {
            warnings[0].Message.ShouldContain(nameof(IOException));
            warnings[0].Message.ShouldNotContain(InternalError);
            warnings[0].Exception.ShouldBeNull();
        }
    }

    [Fact]
    public async Task Delete_WorkflowLookupOutage_LeavesExistingDeletionOwnerInPlace()
    {
        string instanceId = $"delete-{TenantId}-{Guid.NewGuid():N}";
        TenantRegistryEntry entry = new(
            new TenantInfo(TenantId, TenantId, TenantStatus.Deleting, DateTimeOffset.UtcNow), instanceId);
        _factory.DaprClient.GetStateAsync<StoredTenantRegistryEntry?>(
                StateStore, StateKey, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(entry);
        _workflowClient.GetWorkflowStateAsync(instanceId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<WorkflowState>(
                new RpcException(new Status(StatusCode.Unavailable, InternalError))));
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.DeleteAsync(
            $"/api/v1/tenants/{TenantId}", TestContext.Current.CancellationToken);

        await AssertErrorAsync(response, HttpStatusCode.ServiceUnavailable, "DAPR_UNAVAILABLE");
        await _factory.DaprClient.DidNotReceive().TrySaveStateAsync(
            StateStore, StateKey, Arg.Any<StoredTenantRegistryEntry>(), Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>());
        await _workflowClient.DidNotReceive().ScheduleNewWorkflowAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantDeletionInput>());
    }

    [Fact]
    public async Task Delete_UnauthorizedTenant_RejectsBeforeRegistryOrWorkflowLookup()
    {
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.DeleteAsync(
            "/api/v1/tenants/tenant-b", TestContext.Current.CancellationToken);

        await AssertErrorAsync(response, HttpStatusCode.Forbidden, "TENANT_FORBIDDEN");
        _factory.DaprClient.ReceivedCalls().ShouldBeEmpty();
        _workflowClient.ReceivedCalls().ShouldBeEmpty();
    }

    public void Dispose()
    {
        _host.Dispose();
        _factory.Dispose();
    }

    private void StubActiveTenant()
    {
        TenantRegistryEntry entry = new(new TenantInfo(TenantId, TenantId, TenantStatus.Active, DateTimeOffset.UtcNow), WorkflowInstanceId: null);
        _factory.DaprClient.GetStateAsync<StoredTenantRegistryEntry?>(
                StateStore, StateKey, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(entry);
    }

    private HttpClient CreateAuthorizedClient()
    {
        HttpClient client = _host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", ServerTestBearerToken.Create(tenants: [TenantId]));
        return client;
    }

    private static async Task AssertErrorAsync(
        HttpResponseMessage response, HttpStatusCode statusCode, string errorCode)
    {
        response.StatusCode.ShouldBe(statusCode);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(
            MemoriesJsonContext.Options, cancellationToken);
        error.ShouldNotBeNull().Code.ShouldBe(errorCode);
        error.Message.ShouldNotContain(InternalError);
        if (errorCode == "DAPR_UNAVAILABLE")
        {
            error.Suggestion.ShouldContain("/ready");
        }
    }
}
