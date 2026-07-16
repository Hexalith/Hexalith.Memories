// <copyright file="TenantProvisioningWorkflowTests.cs" company="ITANEO">
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
public class TenantProvisioningWorkflowTests
{
    private const string TestInstanceId = "provision-tenant-1-123456";

    [Fact]
    public async Task RunAsync_HappyPath_ShouldPassWorkflowInstanceIdToRegistryActivities()
    {
        TenantProvisioningInput input = new("tenant-1", "Tenant One") { VectorDimensions = 768 };
        WorkflowContext context = CreateContext();
        SetupHappyPath(context, input);
        TenantProvisioningWorkflow workflow = new();

        TenantProvisioningResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(TenantStatus.Active);
        result.ErrorCode.ShouldBeNull();

        await context.Received().CallActivityAsync<TenantInfo>(
            nameof(InitializeTenantRegistryActivity),
            Arg.Is<InitializeTenantRegistryInput>(i =>
                i.TenantId == input.TenantId
                && i.DisplayName == input.DisplayName
                && i.WorkflowInstanceId == TestInstanceId),
            Arg.Any<WorkflowTaskOptions>());

        await context.Received().CallActivityAsync<bool>(
            nameof(UpdateTenantStatusActivity),
            Arg.Is<TenantStatusUpdateInput>(i =>
                i.TenantId == input.TenantId
                && i.Status == TenantStatus.Active
                && i.WorkflowInstanceId == TestInstanceId),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_WhenProvisioningFails_ShouldMarkFailedWithWorkflowInstanceId()
    {
        TenantProvisioningInput input = new("tenant-1", "Tenant One") { VectorDimensions = 768 };
        WorkflowContext context = CreateContext();
        TenantInfo tenantInfo = new(input.TenantId, input.DisplayName, TenantStatus.Provisioning, DateTimeOffset.UtcNow);
        context.CallActivityAsync<TenantInfo>(
                nameof(InitializeTenantRegistryActivity),
                Arg.Any<InitializeTenantRegistryInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(tenantInfo);
        context.CallActivityAsync<bool>(nameof(ProvisionRediSearchActivity), Arg.Any<TenantProvisioningInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(ProvisionRedisVectorActivity), Arg.Any<TenantProvisioningInput>(), Arg.Any<WorkflowTaskOptions>())
            .Throws(CreateTaskFailedException());
        context.CallActivityAsync<bool>(nameof(DeleteRediSearchIndexActivity), Arg.Any<TenantProvisioningInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(UpdateTenantStatusActivity), Arg.Any<TenantStatusUpdateInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        TenantProvisioningWorkflow workflow = new();

        TenantProvisioningResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(TenantStatus.Failed);
        await context.Received().CallActivityAsync<bool>(
            nameof(UpdateTenantStatusActivity),
            Arg.Is<TenantStatusUpdateInput>(i =>
                i.TenantId == input.TenantId
                && i.Status == TenantStatus.Failed
                && i.WorkflowInstanceId == TestInstanceId),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_InvalidTenantId_ShouldReturnFailedWithErrorCode()
    {
        TenantProvisioningInput input = new("tenant with spaces", "Tenant One") { VectorDimensions = 768 };
        WorkflowContext context = CreateContext();
        TenantProvisioningWorkflow workflow = new();

        TenantProvisioningResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(TenantStatus.Failed);
        result.ErrorCode.ShouldBe("INVALID_TENANT_ID");
        await context.DidNotReceive().CallActivityAsync<TenantInfo>(
            nameof(InitializeTenantRegistryActivity),
            Arg.Any<InitializeTenantRegistryInput>(),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_MissingDisplayName_ShouldReturnInvalidInputErrorCode()
    {
        TenantProvisioningInput input = new("tenant-1", "") { VectorDimensions = 768 };
        WorkflowContext context = CreateContext();
        TenantProvisioningWorkflow workflow = new();

        TenantProvisioningResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(TenantStatus.Failed);
        result.ErrorCode.ShouldBe("INVALID_INPUT");
    }

    private static WorkflowContext CreateContext()
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.InstanceId.Returns(TestInstanceId);
        context.CreateReplaySafeLogger<TenantProvisioningWorkflow>()
            .Returns(Substitute.For<ILogger>());
        context.NewGuid().Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        return context;
    }

    private static WorkflowTaskFailedException CreateTaskFailedException()
    {
        var ex = (WorkflowTaskFailedException)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(WorkflowTaskFailedException));
        return ex;
    }

    private static void SetupHappyPath(WorkflowContext context, TenantProvisioningInput input)
    {
        TenantInfo tenantInfo = new(input.TenantId, input.DisplayName, TenantStatus.Provisioning, DateTimeOffset.UtcNow);

        context.CallActivityAsync<TenantInfo>(
                nameof(InitializeTenantRegistryActivity),
                Arg.Any<InitializeTenantRegistryInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(tenantInfo);
        context.CallActivityAsync<bool>(nameof(ProvisionRediSearchActivity), Arg.Any<TenantProvisioningInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(ProvisionRedisVectorActivity), Arg.Any<TenantProvisioningInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(ProvisionFalkorDbActivity), Arg.Any<TenantProvisioningInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(VerifyTenantActivity), Arg.Any<TenantProvisioningInput>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(UpdateTenantStatusActivity), Arg.Any<TenantStatusUpdateInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
    }
}
