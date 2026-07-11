// <copyright file="TenantDeletionWorkflowTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Tenants;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

public class TenantDeletionWorkflowTests
{
    private const string TestTenantId = "tenant-1";
    private const string TestWorkflowInstanceId = "delete-tenant-1-123456";

    [Fact]
    public async Task DeletionWorkflow_HappyPath_CallsAllActivitiesInOrder()
    {
        TenantDeletionInput input = new(TestTenantId);
        TestLogger logger = new();
        WorkflowContext context = CreateContext(logger);
        SetupHappyPath(context);
        TenantDeletionWorkflow workflow = new();

        TenantDeletionResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(TenantStatus.Active);
        result.Message.ShouldContain("deleted successfully");
        result.DeletedAxes.ShouldNotBeNull();
        result.DeletedAxes.ShouldContain("syntactic");
        result.DeletedAxes.ShouldContain("semantic");
        result.DeletedAxes.ShouldContain("graph");
        result.DeletedAxes.ShouldContain("state");

        // Verify activity call order
        Received.InOrder(() =>
        {
            context.CallActivityAsync<TenantInfo?>(nameof(GetTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>());
            context.CallActivityAsync<bool>(nameof(UpdateTenantStatusActivity), Arg.Any<TenantStatusUpdateInput>(), Arg.Any<WorkflowTaskOptions>());
            context.CallActivityAsync<bool>(nameof(DeleteRediSearchActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>());
            context.CallActivityAsync<bool>(nameof(DeleteRedisVectorActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>());
            context.CallActivityAsync<BatchedGraphDeletionResult>(nameof(DeleteFalkorDbBatchActivity), Arg.Any<BatchedGraphDeletionInput>(), Arg.Any<WorkflowTaskOptions>());
            context.CallActivityAsync<bool>(nameof(DeleteFalkorDbGraphFinalizerActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>());
            context.CallActivityAsync<bool>(nameof(DeleteTenantDataKeysActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>());
            context.CallActivityAsync<bool>(nameof(RemoveTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>());
        });

        logger.Messages.ShouldContain(message => message.Contains("GraphBatchCompleted", StringComparison.Ordinal));
        await context.Received().CallActivityAsync<bool>(
            nameof(UpdateTenantStatusActivity),
            Arg.Is<TenantStatusUpdateInput>(i =>
                i.TenantId == TestTenantId
                && i.Status == TenantStatus.Deleting
                && i.WorkflowInstanceId == TestWorkflowInstanceId),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task DeletionWorkflow_TenantNotFound_ReturnsSuccess()
    {
        TenantDeletionInput input = new(TestTenantId);
        WorkflowContext context = CreateContext();

        // GetTenantRegistryActivity returns null (tenant already deleted)
        context.CallActivityAsync<TenantInfo?>(
                nameof(GetTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>())
            .Returns((TenantInfo?)null);

        TenantDeletionWorkflow workflow = new();
        TenantDeletionResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(TenantStatus.Active);
        result.Message.ShouldContain("already deleted");

        // Should NOT call any deletion activities
        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(DeleteRediSearchActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task DeletionWorkflow_BatchedLoop_TerminatesWhenComplete()
    {
        TenantDeletionInput input = new(TestTenantId);
        WorkflowContext context = CreateContext();

        // Setup tenant as Active
        TenantInfo tenant = new(TestTenantId, "Test", TenantStatus.Active, DateTimeOffset.UtcNow);
        context.CallActivityAsync<TenantInfo?>(
                nameof(GetTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(tenant);
        context.CallActivityAsync<bool>(nameof(UpdateTenantStatusActivity), Arg.Any<TenantStatusUpdateInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteRediSearchActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteRedisVectorActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);

        // 3 batches not complete, then 4th is complete
        context.CallActivityAsync<BatchedGraphDeletionResult>(
                nameof(DeleteFalkorDbBatchActivity), Arg.Any<BatchedGraphDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(
                new BatchedGraphDeletionResult(1500, 500, false),
                new BatchedGraphDeletionResult(1000, 500, false),
                new BatchedGraphDeletionResult(500, 500, false),
                new BatchedGraphDeletionResult(0, 500, true));

        context.CallActivityAsync<bool>(nameof(DeleteFalkorDbGraphFinalizerActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteTenantDataKeysActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(RemoveTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);

        TenantDeletionWorkflow workflow = new();
        TenantDeletionResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(TenantStatus.Active);

        // Verify batch activity was called 4 times (first batch + 3 more in loop)
        await context.Received(4).CallActivityAsync<BatchedGraphDeletionResult>(
            nameof(DeleteFalkorDbBatchActivity), Arg.Any<BatchedGraphDeletionInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task DeletionWorkflow_PartialFailure_SetsStatusToFailed()
    {
        TenantDeletionInput input = new(TestTenantId);
        WorkflowContext context = CreateContext();

        TenantInfo tenant = new(TestTenantId, "Test", TenantStatus.Active, DateTimeOffset.UtcNow);
        context.CallActivityAsync<TenantInfo?>(
                nameof(GetTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(tenant);
        context.CallActivityAsync<bool>(nameof(UpdateTenantStatusActivity), Arg.Any<TenantStatusUpdateInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteRediSearchActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);

        // RedisVector deletion fails
        context.CallActivityAsync<bool>(nameof(DeleteRedisVectorActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Throws(CreateTaskFailedException("Redis Vector deletion failed"));

        TenantDeletionWorkflow workflow = new();
        TenantDeletionResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(TenantStatus.Failed);
        result.DeletedAxes.ShouldNotBeNull();
        result.DeletedAxes.ShouldContain("syntactic");
        result.DeletedAxes.ShouldNotContain("semantic");

        // Verify status was updated to Failed
        await context.Received().CallActivityAsync<bool>(
            nameof(UpdateTenantStatusActivity),
            Arg.Is<TenantStatusUpdateInput>(i =>
                i.Status == TenantStatus.Failed
                && i.WorkflowInstanceId == TestWorkflowInstanceId),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task DeletionWorkflow_WhenStatusUpdateToFailedAlsoFails_ReturnsCompensationFailed()
    {
        TenantDeletionInput input = new(TestTenantId);
        WorkflowContext context = CreateContext();

        TenantInfo tenant = new(TestTenantId, "Test", TenantStatus.Active, DateTimeOffset.UtcNow);
        context.CallActivityAsync<TenantInfo?>(
                nameof(GetTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(tenant);
        context.CallActivityAsync<bool>(
                nameof(UpdateTenantStatusActivity),
                Arg.Is<TenantStatusUpdateInput>(i =>
                    i.Status == TenantStatus.Deleting
                    && i.WorkflowInstanceId == TestWorkflowInstanceId),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteRediSearchActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteRedisVectorActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Throws(CreateTaskFailedException("Redis Vector deletion failed"));
        context.CallActivityAsync<bool>(
                nameof(UpdateTenantStatusActivity),
                Arg.Is<TenantStatusUpdateInput>(i =>
                    i.Status == TenantStatus.Failed
                    && i.WorkflowInstanceId == TestWorkflowInstanceId),
                Arg.Any<WorkflowTaskOptions>())
            .Throws(CreateTaskFailedException("Status update failed"));

        TenantDeletionWorkflow workflow = new();

        TenantDeletionResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(TenantStatus.CompensationFailed);
        result.DeletedAxes.ShouldNotBeNull();
        result.DeletedAxes.ShouldContain("syntactic");
    }

    [Fact]
    public async Task DeletionWorkflow_AlreadyDeleting_ContinuesIdempotently()
    {
        TenantDeletionInput input = new(TestTenantId);
        TestLogger logger = new();
        WorkflowContext context = CreateContext(logger);

        // Tenant is already in Deleting status (replay safety)
        TenantInfo tenant = new(TestTenantId, "Test", TenantStatus.Deleting, DateTimeOffset.UtcNow);
        context.CallActivityAsync<TenantInfo?>(
                nameof(GetTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(tenant);

        // First batch returns complete immediately
        context.CallActivityAsync<BatchedGraphDeletionResult>(
                nameof(DeleteFalkorDbBatchActivity), Arg.Any<BatchedGraphDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new BatchedGraphDeletionResult(0, 0, true));

        context.CallActivityAsync<bool>(nameof(DeleteRediSearchActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteRedisVectorActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteFalkorDbGraphFinalizerActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteTenantDataKeysActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(RemoveTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);

        TenantDeletionWorkflow workflow = new();
        TenantDeletionResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(TenantStatus.Active);

        // Should NOT call UpdateTenantStatusActivity to set Deleting (already Deleting)
        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(UpdateTenantStatusActivity),
            Arg.Is<TenantStatusUpdateInput>(i => i.Status == TenantStatus.Deleting),
            Arg.Any<WorkflowTaskOptions>());
        logger.Messages.ShouldContain(message => message.Contains("DeletionResumed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DeletionWorkflow_ProvisioningTenant_ReturnsError()
    {
        TenantDeletionInput input = new(TestTenantId);
        WorkflowContext context = CreateContext();

        TenantInfo tenant = new(TestTenantId, "Test", TenantStatus.Provisioning, DateTimeOffset.UtcNow);
        context.CallActivityAsync<TenantInfo?>(
                nameof(GetTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(tenant);

        TenantDeletionWorkflow workflow = new();
        TenantDeletionResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(TenantStatus.Provisioning);
        result.Message.ShouldContain("provisioning");

        // Should NOT call any deletion activities
        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(DeleteRediSearchActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task DeletionWorkflow_BatchLoopMaxIterations_SetsStatusToFailed()
    {
        TenantDeletionInput input = new(TestTenantId);
        TestLogger logger = new();
        WorkflowContext context = CreateContext(logger);

        TenantInfo tenant = new(TestTenantId, "Test", TenantStatus.Active, DateTimeOffset.UtcNow);
        context.CallActivityAsync<TenantInfo?>(
                nameof(GetTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(tenant);
        context.CallActivityAsync<bool>(nameof(UpdateTenantStatusActivity), Arg.Any<TenantStatusUpdateInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteRediSearchActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteRedisVectorActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);

        // Batch activity never completes — always returns remaining nodes (simulates a stalled deletion)
        context.CallActivityAsync<BatchedGraphDeletionResult>(
                nameof(DeleteFalkorDbBatchActivity), Arg.Any<BatchedGraphDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new BatchedGraphDeletionResult(1000, 0, false));

        TenantDeletionWorkflow workflow = new();
        TenantDeletionResult result = await workflow.RunAsync(context, input);

        // Verify workflow exits with Failed status due to max iterations
        result.Status.ShouldBe(TenantStatus.Failed);
        result.Message.ShouldContain("maximum iterations");
        result.Message.ShouldContain("1000 nodes remain");
        result.DeletedAxes.ShouldNotBeNull();
        result.DeletedAxes.ShouldContain("syntactic");
        result.DeletedAxes.ShouldContain("semantic");

        // Verify status was updated to Failed
        await context.Received().CallActivityAsync<bool>(
            nameof(UpdateTenantStatusActivity),
            Arg.Is<TenantStatusUpdateInput>(i =>
                i.Status == TenantStatus.Failed
                && i.WorkflowInstanceId == TestWorkflowInstanceId),
            Arg.Any<WorkflowTaskOptions>());
        logger.Messages.ShouldContain(message => message.Contains("DeletionFailed", StringComparison.Ordinal));
    }

    private static WorkflowContext CreateContext(ILogger? logger = null)
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.InstanceId.Returns(TestWorkflowInstanceId);
        context.CreateReplaySafeLogger<TenantDeletionWorkflow>()
            .Returns(logger ?? Substitute.For<ILogger>());
        return context;
    }

    private static WorkflowTaskFailedException CreateTaskFailedException(string message)
    {
        // WorkflowTaskFailedException has non-public constructors; create uninitialized instance
        var ex = (WorkflowTaskFailedException)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(WorkflowTaskFailedException));
        return ex;
    }

    private static void SetupHappyPath(WorkflowContext context)
    {
        TenantInfo tenant = new(TestTenantId, "Test Tenant", TenantStatus.Active, DateTimeOffset.UtcNow);

        context.CallActivityAsync<TenantInfo?>(
                nameof(GetTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(tenant);
        context.CallActivityAsync<bool>(nameof(UpdateTenantStatusActivity), Arg.Any<TenantStatusUpdateInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteRediSearchActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteRedisVectorActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);

        // First batch returns complete (small tenant — no loop)
        context.CallActivityAsync<BatchedGraphDeletionResult>(
                nameof(DeleteFalkorDbBatchActivity), Arg.Any<BatchedGraphDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new BatchedGraphDeletionResult(0, 0, true));

        context.CallActivityAsync<bool>(nameof(DeleteFalkorDbGraphFinalizerActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteTenantDataKeysActivity), Arg.Any<TenantDeletionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(RemoveTenantRegistryActivity), Arg.Any<string>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
    }

    private sealed class TestLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
