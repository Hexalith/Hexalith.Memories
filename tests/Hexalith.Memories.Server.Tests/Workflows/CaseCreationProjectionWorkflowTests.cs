// <copyright file="CaseCreationProjectionWorkflowTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Workflows;

using NSubstitute;

using Shouldly;

/// <summary>
/// Story 21.2 AC1: case-created projection fan-out must run Redis hash, FalkorDB node, and case
/// activity projections in order, and compensate completed projections when a later boundary fails
/// so the EventStore event remains the replayable source of truth without silent divergence.
/// </summary>
public class CaseCreationProjectionWorkflowTests
{
    private static readonly ProjectCaseCreatedInput Input = new(
        "tenant-1", "case-001", "Test Case", "A description", DateTimeOffset.Parse("2026-07-04T10:00:00+00:00"));

    [Fact]
    public async Task RunAsync_HappyPath_ShouldProjectHashGraphAndActivityInOrder()
    {
        WorkflowContext context = CreateContext();
        SetupHappyPath(context);
        CaseCreationProjectionWorkflow workflow = new();

        bool result = await workflow.RunAsync(context, Input);

        result.ShouldBeTrue();
        Received.InOrder(() =>
        {
            context.CallActivityAsync<bool>(nameof(ProjectCaseHashActivity), Input, Arg.Any<WorkflowTaskOptions>());
            context.CallActivityAsync<bool>(nameof(ProjectCaseGraphActivity), Input, Arg.Any<WorkflowTaskOptions>());
            context.CallActivityAsync<bool>(
                nameof(RecordCaseActivityActivity),
                Arg.Is<CaseActivityInput>(i =>
                    i.TenantId == Input.TenantId
                    && i.CaseId == Input.CaseId
                    && i.EventType == CaseActivityEventType.CaseCreated),
                Arg.Any<WorkflowTaskOptions>());
        });
        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(CleanupCaseProjectionActivity), Arg.Any<CaseProjectionCleanupInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_HashProjectionFails_ShouldRethrowWithoutCompensation()
    {
        WorkflowContext context = CreateContext();
        context.CallActivityAsync<bool>(nameof(ProjectCaseHashActivity), Arg.Any<ProjectCaseCreatedInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<bool>(WorkflowTestHelpers.CreateTaskFailedException()));
        CaseCreationProjectionWorkflow workflow = new();

        _ = await Should.ThrowAsync<WorkflowTaskFailedException>(() => workflow.RunAsync(context, Input));

        // Nothing was projected yet, so nothing must be compensated — the EventStore event stays
        // pending replay/rebuild instead of triggering a destructive cleanup.
        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(CleanupCaseProjectionActivity), Arg.Any<CaseProjectionCleanupInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(ProjectCaseGraphActivity), Arg.Any<ProjectCaseCreatedInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_GraphProjectionFails_ShouldCompensateCompletedProjectionsAndRethrow()
    {
        WorkflowContext context = CreateContext();
        context.CallActivityAsync<bool>(nameof(ProjectCaseHashActivity), Arg.Any<ProjectCaseCreatedInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(ProjectCaseGraphActivity), Arg.Any<ProjectCaseCreatedInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<bool>(WorkflowTestHelpers.CreateTaskFailedException()));
        context.CallActivityAsync<bool>(nameof(CleanupCaseProjectionActivity), Arg.Any<CaseProjectionCleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        CaseCreationProjectionWorkflow workflow = new();

        _ = await Should.ThrowAsync<WorkflowTaskFailedException>(() => workflow.RunAsync(context, Input));

        await context.Received(1).CallActivityAsync<bool>(
            nameof(CleanupCaseProjectionActivity),
            Arg.Is<CaseProjectionCleanupInput>(i => i.TenantId == Input.TenantId && i.CaseId == Input.CaseId),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_ActivityRecordFails_ShouldCompensateCompletedProjectionsAndRethrow()
    {
        WorkflowContext context = CreateContext();
        context.CallActivityAsync<bool>(nameof(ProjectCaseHashActivity), Arg.Any<ProjectCaseCreatedInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(ProjectCaseGraphActivity), Arg.Any<ProjectCaseCreatedInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(RecordCaseActivityActivity), Arg.Any<CaseActivityInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<bool>(WorkflowTestHelpers.CreateTaskFailedException()));
        context.CallActivityAsync<bool>(nameof(CleanupCaseProjectionActivity), Arg.Any<CaseProjectionCleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        CaseCreationProjectionWorkflow workflow = new();

        _ = await Should.ThrowAsync<WorkflowTaskFailedException>(() => workflow.RunAsync(context, Input));

        // Activity-stream failure after Redis + FalkorDB commits rolls the projections back and
        // surfaces the failed instance instead of leaving a partially projected case.
        await context.Received(1).CallActivityAsync<bool>(
            nameof(CleanupCaseProjectionActivity),
            Arg.Is<CaseProjectionCleanupInput>(i => i.TenantId == Input.TenantId && i.CaseId == Input.CaseId),
            Arg.Any<WorkflowTaskOptions>());
    }

    private static WorkflowContext CreateContext()
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.InstanceId.Returns("case-create-case-001");
        return context;
    }

    private static void SetupHappyPath(WorkflowContext context)
    {
        context.CallActivityAsync<bool>(nameof(ProjectCaseHashActivity), Arg.Any<ProjectCaseCreatedInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(ProjectCaseGraphActivity), Arg.Any<ProjectCaseCreatedInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(RecordCaseActivityActivity), Arg.Any<CaseActivityInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
    }
}
