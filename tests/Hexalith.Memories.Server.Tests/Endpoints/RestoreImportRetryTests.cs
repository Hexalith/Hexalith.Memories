// <copyright file="RestoreImportRetryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;

using Dapr.Client;
using Dapr.Common.Serialization;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Import;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Tests.Authentication;
using Hexalith.Memories.Server.Tests.EventStoreIntegration;
using Hexalith.Memories.Server.Workflows;

using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;
using Shouldly;

public sealed class RestoreImportRetryTests : IDisposable
{
    private const string Tenant = "tenant-restore";
    private const string Owner = "restore-owner";
    private const string StagingKey = "duplicate-staging";
    private const string Payload = """
        {
          "manifest": {"schemaVersion":1,"scope":"tenant","tenantId":"tenant-restore"},
          "tenant": {}, "cases": [], "memoryUnits": [], "edges": [],
          "statistics": {"memoryUnitCount":0,"edgeCount":0,"caseCount":0}
        }
        """;

    private readonly EventStoreWebAppFactory _factory = new();
    private readonly IImportStagingStore _staging = Substitute.For<IImportStagingStore>();
    private readonly IDaprWorkflowClient _workflows = Substitute.For<IDaprWorkflowClient>();

    [Theory]
    [InlineData("pending", HttpStatusCode.Conflict, "RESTORE_TARGET_BUSY")]
    [InlineData("unavailable", HttpStatusCode.ServiceUnavailable, "DAPR_UNAVAILABLE")]
    [InlineData("running", HttpStatusCode.Accepted, null)]
    [InlineData("different", HttpStatusCode.Conflict, "RESTORE_TARGET_BUSY")]
    public async Task PostImport_AdoptsOnlyConfirmedIdenticalWorkflow(
        string ownerState,
        HttpStatusCode expectedStatus,
        string? expectedError)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TenantRegistryEntry entry = new(new TenantInfo(Tenant, Tenant, TenantStatus.Active, DateTimeOffset.UtcNow), null);
        _factory.DaprClient.GetStateAsync<StoredTenantRegistryEntry?>(
            "statestore", $"tenant-registry-{Tenant}", Arg.Any<ConsistencyMode?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(entry);
        _staging.StageAsync(Tenant, Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(StagingKey);
        _staging.OpenReadAsync(StagingKey, Arg.Any<CancellationToken>())
            .Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(Payload)));
        _staging.AcquireRestoreLeaseAsync(StagingKey, Tenant, Arg.Is<string?>(caseId => caseId == null), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RestoreLeaseResult(false, Owner, ownerState != "different"));

        _workflows.GetWorkflowStateAsync(Owner, false, Arg.Any<CancellationToken>()).Returns(_ => ownerState switch
        {
            "running" => Task.FromResult(CreateRunningWorkflow()),
            "unavailable" => Task.FromException<WorkflowState>(new Dapr.DaprException("Test state outage")),
            _ => Task.FromResult<WorkflowState>(null!),
        });

        using var host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IImportStagingStore>();
            services.RemoveAll<IDaprWorkflowClient>();
            services.AddSingleton(_staging);
            services.AddSingleton(_workflows);
        }));
        using HttpClient client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", ServerTestBearerToken.Create(tenants: [Tenant]));
        using StringContent content = new(Payload, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync($"/api/v1/tenants/{Tenant}/import", content, cancellationToken);

        response.StatusCode.ShouldBe(expectedStatus);
        if (expectedError is not null)
        {
            (await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options, cancellationToken))
                .ShouldNotBeNull().Code.ShouldBe(expectedError);
            response.Headers.Location.ShouldBeNull();
        }
        else
        {
            string location = MemoriesRoutes.RestoreStatusLocation(Tenant, Owner);
            response.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe(location);
            (await response.Content.ReadFromJsonAsync<RestoreAcceptedResponse>(MemoriesJsonContext.Options, cancellationToken))
                .ShouldNotBeNull().InstanceId.ShouldBe(Owner);
        }

        await _staging.Received(1).DeleteAsync(StagingKey, CancellationToken.None);
        await _workflows.Received(ownerState == "different" ? 0 : 1)
            .GetWorkflowStateAsync(Owner, false, Arg.Any<CancellationToken>());
        await _workflows.DidNotReceiveWithAnyArgs().ScheduleNewWorkflowAsync(default!, default!, default!, default, default);
    }

    public void Dispose() => _factory.Dispose();

    private static WorkflowState CreateRunningWorkflow()
    {
        Type metadataType = typeof(WorkflowState).Assembly.GetType("Dapr.Workflow.Client.WorkflowMetadata", throwOnError: true)!;
        DateTime now = DateTime.UtcNow;
        object metadata = Activator.CreateInstance(metadataType, Owner, nameof(RestoreWorkflow),
            WorkflowRuntimeStatus.Running, now, now, new JsonDaprSerializer(MemoriesJsonContext.Options))!;
        return (WorkflowState)Activator.CreateInstance(typeof(WorkflowState), BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null, args: [metadata], culture: null)!;
    }
}
