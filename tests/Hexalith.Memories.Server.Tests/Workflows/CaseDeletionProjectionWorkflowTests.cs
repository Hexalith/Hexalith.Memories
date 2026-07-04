// <copyright file="CaseDeletionProjectionWorkflowTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Workflows;

using NSubstitute;

using Shouldly;

/// <summary>
/// Story 21.2 AC1: case deletion projection cleanup must set the "deleting" status guard before
/// removing read models, and surface projection failure as a failed workflow instance so the case
/// stays observably "deleting" (blocking concurrent ingestion) rather than silently diverging.
/// </summary>
public class CaseDeletionProjectionWorkflowTests
{
    private static readonly CaseDeletionProjectionInput Input = new(
        "tenant-1", "case-001", ["mu-001", "mu-002"]);

    [Fact]
    public async Task RunAsync_HappyPath_ShouldMarkDeletingBeforeDeletingProjections()
    {
        WorkflowContext context = CreateContext();
        context.CallActivityAsync<bool>(nameof(MarkCaseDeletingActivity), Arg.Any<CaseProjectionCleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteCaseProjectionActivity), Arg.Any<CaseDeletionProjectionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        CaseDeletionProjectionWorkflow workflow = new();

        bool result = await workflow.RunAsync(context, Input);

        result.ShouldBeTrue();
        Received.InOrder(() =>
        {
            context.CallActivityAsync<bool>(
                nameof(MarkCaseDeletingActivity),
                Arg.Is<CaseProjectionCleanupInput>(i => i.TenantId == Input.TenantId && i.CaseId == Input.CaseId),
                Arg.Any<WorkflowTaskOptions>());
            context.CallActivityAsync<bool>(nameof(DeleteCaseProjectionActivity), Input, Arg.Any<WorkflowTaskOptions>());
        });
    }

    [Fact]
    public async Task RunAsync_MarkDeletingFails_ShouldRethrowWithoutDeletingProjections()
    {
        WorkflowContext context = CreateContext();
        context.CallActivityAsync<bool>(nameof(MarkCaseDeletingActivity), Arg.Any<CaseProjectionCleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<bool>(WorkflowTestHelpers.CreateTaskFailedException()));
        CaseDeletionProjectionWorkflow workflow = new();

        _ = await Should.ThrowAsync<WorkflowTaskFailedException>(() => workflow.RunAsync(context, Input));

        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(DeleteCaseProjectionActivity), Arg.Any<CaseDeletionProjectionInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_ProjectionDeleteFails_ShouldRethrowAndLeaveCaseInDeletingState()
    {
        WorkflowContext context = CreateContext();
        context.CallActivityAsync<bool>(nameof(MarkCaseDeletingActivity), Arg.Any<CaseProjectionCleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(DeleteCaseProjectionActivity), Arg.Any<CaseDeletionProjectionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<bool>(WorkflowTestHelpers.CreateTaskFailedException()));
        CaseDeletionProjectionWorkflow workflow = new();

        // The failed instance keeps the case in the observable "deleting" guard state; the accepted
        // deletion event remains replayable so a retry/rebuild converges instead of diverging.
        _ = await Should.ThrowAsync<WorkflowTaskFailedException>(() => workflow.RunAsync(context, Input));

        await context.Received(1).CallActivityAsync<bool>(
            nameof(MarkCaseDeletingActivity), Arg.Any<CaseProjectionCleanupInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    private static WorkflowContext CreateContext()
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.InstanceId.Returns("case-delete-case-001");
        return context;
    }
}
