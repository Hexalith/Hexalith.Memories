// <copyright file="MemoryUnitDeletionProjectionWorkflowTests.cs" company="ITANEO">
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
/// Story 21.2 AC1: memory-unit deletion projection cleanup must delete the syntactic/vector/graph
/// read models before recording activity, and surface projection failure as a failed workflow
/// instance so the accepted EventStore deletion event stays replayable instead of silently diverging.
/// </summary>
public class MemoryUnitDeletionProjectionWorkflowTests
{
    private static readonly MemoryUnitDeletionProjectionInput Input = new(
        "tenant-1", "case-001", "mu-001", ["ann-001", "ann-002"]);

    [Fact]
    public async Task RunAsync_HappyPath_ShouldDeleteProjectionsThenRecordActivity()
    {
        WorkflowContext context = CreateContext();
        context.CallActivityAsync<bool>(nameof(DeleteMemoryUnitProjectionActivity), Arg.Any<MemoryUnitDeletionProjectionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(RecordCaseActivityActivity), Arg.Any<CaseActivityInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        MemoryUnitDeletionProjectionWorkflow workflow = new();

        bool result = await workflow.RunAsync(context, Input);

        result.ShouldBeTrue();
        Received.InOrder(() =>
        {
            context.CallActivityAsync<bool>(nameof(DeleteMemoryUnitProjectionActivity), Input, Arg.Any<WorkflowTaskOptions>());
            context.CallActivityAsync<bool>(
                nameof(RecordCaseActivityActivity),
                Arg.Is<CaseActivityInput>(i =>
                    i!.TenantId == Input.TenantId
                    && i.CaseId == Input.CaseId
                    && i.EventType == CaseActivityEventType.MemoryUnitDeleted
                    && i.MemoryUnitId == Input.MemoryUnitId),
                Arg.Any<WorkflowTaskOptions>());
        });
    }

    [Fact]
    public async Task RunAsync_ProjectionDeleteFails_ShouldRethrowAndSkipActivityRecord()
    {
        WorkflowContext context = CreateContext();
        context.CallActivityAsync<bool>(nameof(DeleteMemoryUnitProjectionActivity), Arg.Any<MemoryUnitDeletionProjectionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<bool>(WorkflowTestHelpers.CreateTaskFailedException()));
        MemoryUnitDeletionProjectionWorkflow workflow = new();

        // Deletion cleanup is idempotent and retried by policy; after retries are exhausted the
        // instance fails visibly and the accepted event remains the rebuild source — no activity
        // record is written for a deletion that did not complete.
        _ = await Should.ThrowAsync<WorkflowTaskFailedException>(() => workflow.RunAsync(context, Input));

        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(RecordCaseActivityActivity), Arg.Any<CaseActivityInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    private static WorkflowContext CreateContext()
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.InstanceId.Returns("memory-unit-delete-mu-001");
        return context;
    }
}
