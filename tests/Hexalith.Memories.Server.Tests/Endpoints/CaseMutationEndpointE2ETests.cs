// <copyright file="CaseMutationEndpointE2ETests.cs" company="ITANEO">
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
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Tests.Authentication;
using Hexalith.Memories.Server.Tests.EventStoreIntegration;
using Hexalith.Memories.Server.Workflows;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

using CaseRecord = Hexalith.Memories.Contracts.V1.Case;

/// <summary>
/// Story 21.2 API-level coverage for the case mutation path. These tests drive the real HTTP
/// endpoint and middleware through <see cref="EventStoreWebAppFactory"/> while replacing only the
/// EventStore command gateway and projection scheduler seams.
/// </summary>
public sealed class CaseMutationEndpointE2ETests : IDisposable
{
    private const string StoreName = "statestore";
    private const string TenantId = "tenant-a";

    private readonly EventStoreWebAppFactory _factory = new();
    private readonly List<string> _operationLog = [];
    private readonly CapturingCommandStore _commandStore;
    private readonly CapturingProjectionWorkflowScheduler _scheduler;

    public CaseMutationEndpointE2ETests()
    {
        _commandStore = new CapturingCommandStore(_operationLog);
        _scheduler = new CapturingProjectionWorkflowScheduler(_operationLog);
        _factory.MemoriesCommandStore = _commandStore;
        _factory.CaseProjectionWorkflowScheduler = _scheduler;
    }

    [Fact]
    public async Task PostCase_AcceptsEventStoreCommandBeforeSchedulingProjectionWorkflow()
    {
        StubTenantActive(TenantId);
        using HttpClient client = CreateAuthorizedClient();
        CreateCaseInput input = new("ignored-body-tenant", "Claims Pilot", "First investigation case");

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{TenantId}/cases",
            input,
            MemoriesJsonContext.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? created = await response.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        created.ShouldNotBeNull();
        created.TenantId.ShouldBe(TenantId);
        created.Name.ShouldBe(input.Name);
        created.Description.ShouldBe(input.Description);

        CreateCaseCommand command = _commandStore.AcceptedCommands
            .ShouldHaveSingleItem()
            .ShouldBeOfType<CreateCaseCommand>();
        command.TenantId.ShouldBe(TenantId);
        command.CaseId.ShouldBe(created.Id);
        command.Name.ShouldBe(input.Name);
        command.Description.ShouldBe(input.Description);

        ScheduledProjectionWorkflow scheduled = _scheduler.ScheduledWorkflows.ShouldHaveSingleItem();
        scheduled.WorkflowName.ShouldBe(nameof(CaseCreationProjectionWorkflow));
        scheduled.InstanceId.ShouldBe($"case-create-{created.Id}");
        ProjectCaseCreatedInput projectionInput = scheduled.Input.ShouldBeOfType<ProjectCaseCreatedInput>();
        projectionInput.TenantId.ShouldBe(TenantId);
        projectionInput.CaseId.ShouldBe(created.Id);

        _operationLog.ShouldBe(
        [
            $"accept:{CreateCaseCommand.CommandType}",
            $"schedule:{nameof(CaseCreationProjectionWorkflow)}",
        ]);

        await _factory.RedisDatabase.DidNotReceive().HashSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task PostCase_WhenBodyInvalid_Returns400WithoutEventStoreCommand()
    {
        StubTenantActive(TenantId);
        using HttpClient client = CreateAuthorizedClient();
        CreateCaseInput input = new("ignored-body-tenant", string.Empty, "Missing name");

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{TenantId}/cases",
            input,
            MemoriesJsonContext.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("INVALID_CASE_NAME");
        _commandStore.AcceptedCommands.ShouldBeEmpty();
        _scheduler.ScheduledWorkflows.ShouldBeEmpty();
    }

    [Fact]
    public async Task PostCase_WhenEventStoreCommandAcceptFails_Returns500WithoutProjectionWorkflow()
    {
        _factory.MemoriesCommandStore = new FailingCommandStore();
        StubTenantActive(TenantId);
        using HttpClient client = CreateAuthorizedClient();
        CreateCaseInput input = new("ignored-body-tenant", "Gateway Failure", null);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{TenantId}/cases",
            input,
            MemoriesJsonContext.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _scheduler.ScheduledWorkflows.ShouldBeEmpty();
        await _factory.RedisDatabase.DidNotReceive().HashSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
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

    private void StubTenantActive(string tenantId)
    {
        TenantRegistryEntry entry = new(
            new TenantInfo(tenantId, tenantId, TenantStatus.Active, DateTimeOffset.UtcNow),
            WorkflowInstanceId: null);

        _factory.DaprClient
            .GetStateAsync<StoredTenantRegistryEntry?>(
                StoreName,
                Arg.Is<string>(key => key == $"tenant-registry-{tenantId}"),
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(entry);
    }

    private static async Task<ErrorResponse> ReadErrorResponseAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        ErrorResponse? error = JsonSerializer.Deserialize<ErrorResponse>(body, MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        return error;
    }

    private sealed class CapturingCommandStore(List<string> operationLog) : IMemoriesCommandStore
    {
        public List<object> AcceptedCommands { get; } = [];

        public Task<string> AcceptAsync<TCommand>(
            string tenantId,
            TCommand command,
            string actorId,
            CancellationToken cancellationToken)
            where TCommand : IMemoriesCommandContract
        {
            tenantId.ShouldNotBeNullOrWhiteSpace();
            actorId.ShouldNotBeNullOrWhiteSpace();
            AcceptedCommands.Add(command);
            operationLog.Add($"accept:{TCommand.CommandType}");
            return Task.FromResult($"accepted-{AcceptedCommands.Count}");
        }
    }

    private sealed class FailingCommandStore : IMemoriesCommandStore
    {
        public Task<string> AcceptAsync<TCommand>(
            string tenantId,
            TCommand command,
            string actorId,
            CancellationToken cancellationToken)
            where TCommand : IMemoriesCommandContract
            => Task.FromException<string>(new InvalidOperationException("EVENTSTORE_UNAVAILABLE"));
    }

    private sealed class CapturingProjectionWorkflowScheduler(List<string> operationLog) : ICaseProjectionWorkflowScheduler
    {
        public List<ScheduledProjectionWorkflow> ScheduledWorkflows { get; } = [];

        public Task<string> ScheduleAsync(
            string workflowName,
            string instanceId,
            object input,
            CancellationToken cancellationToken)
        {
            ScheduledWorkflows.Add(new ScheduledProjectionWorkflow(workflowName, instanceId, input));
            operationLog.Add($"schedule:{workflowName}");
            return Task.FromResult(instanceId);
        }
    }

    private sealed record ScheduledProjectionWorkflow(string WorkflowName, string InstanceId, object Input);
}
