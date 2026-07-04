// <copyright file="AnnotationProjectionWorkflowTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Workflows;

using NSubstitute;

using Shouldly;

/// <summary>
/// Story 21.2 AC1: annotation projection fan-out must project the graph stub/edge, run the
/// ingestion workflow as an observed child workflow, and record activity in order, compensating the graph stub when a later
/// boundary fails so no orphaned FalkorDB stub survives a failed annotation request.
/// </summary>
public class AnnotationProjectionWorkflowTests
{
    private static readonly AnnotationProjectionInput Input = new(
        "tenant-1",
        "case-001",
        "ann-001",
        "mu-001",
        "annotation:mu-001:ann-001",
        "Correction text",
        "correction",
        "annotator@test.local",
        new Dictionary<string, MetadataField>
        {
            ["_system.annotation_target"] = new MetadataField("mu-001", MetadataOrigin.Human, 1.0f),
        });

    [Fact]
    public async Task RunAsync_HappyPath_ShouldProjectGraphRunIngestionAndRecordActivityInOrder()
    {
        WorkflowContext context = CreateContext();
        SetupHappyPath(context);
        AnnotationProjectionWorkflow workflow = new();

        string result = await workflow.RunAsync(context, Input);

        result.ShouldBe(Input.AnnotationMemoryUnitId);
        Received.InOrder(() =>
        {
            context.CallActivityAsync<bool>(nameof(ProjectAnnotationGraphActivity), Input, Arg.Any<WorkflowTaskOptions>());
            context.CallChildWorkflowAsync<IngestionResult>(
                nameof(IngestionWorkflow),
                Arg.Is<IngestionInput>(i =>
                    i.TenantId == Input.TenantId
                    && i.CaseId == Input.CaseId
                    && i.SourceUri == Input.SourceUri
                    && i.SourceType == SourceType.Annotation
                    && i.CausationId == Input.TargetMemoryUnitId),
                Arg.Is<ChildWorkflowTaskOptions>(o => o.InstanceId == Input.AnnotationMemoryUnitId));
            context.CallActivityAsync<bool>(
                nameof(RecordCaseActivityActivity),
                Arg.Is<CaseActivityInput>(i =>
                    i.TenantId == Input.TenantId
                    && i.CaseId == Input.CaseId
                    && i.EventType == CaseActivityEventType.AnnotationCreated
                    && i.MemoryUnitId == Input.AnnotationMemoryUnitId),
                Arg.Any<WorkflowTaskOptions>());
        });
        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_GraphProjectionFails_ShouldRethrowWithoutCompensation()
    {
        WorkflowContext context = CreateContext();
        context.CallActivityAsync<bool>(nameof(ProjectAnnotationGraphActivity), Arg.Any<AnnotationProjectionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<bool>(WorkflowTestHelpers.CreateTaskFailedException()));
        AnnotationProjectionWorkflow workflow = new();

        _ = await Should.ThrowAsync<WorkflowTaskFailedException>(() => workflow.RunAsync(context, Input));

        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.DidNotReceive().CallChildWorkflowAsync<IngestionResult>(
            nameof(IngestionWorkflow), Arg.Any<IngestionInput>(), Arg.Any<ChildWorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_IngestionChildWorkflowFails_ShouldCompensateGraphStubAndRethrow()
    {
        WorkflowContext context = CreateContext();
        context.CallActivityAsync<bool>(nameof(ProjectAnnotationGraphActivity), Arg.Any<AnnotationProjectionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallChildWorkflowAsync<IngestionResult>(
                nameof(IngestionWorkflow),
                Arg.Any<IngestionInput>(),
                Arg.Any<ChildWorkflowTaskOptions>())
            .Returns(Task.FromException<IngestionResult>(WorkflowTestHelpers.CreateTaskFailedException()));
        context.CallActivityAsync<bool>(nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        AnnotationProjectionWorkflow workflow = new();

        _ = await Should.ThrowAsync<WorkflowTaskFailedException>(() => workflow.RunAsync(context, Input));

        await context.Received(1).CallActivityAsync<bool>(
            nameof(CleanupGraphActivity),
            Arg.Is<CleanupInput>(i => i.MemoryUnitId == Input.AnnotationMemoryUnitId && i.TenantId == Input.TenantId),
            Arg.Any<WorkflowTaskOptions>());
        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(RecordCaseActivityActivity), Arg.Any<CaseActivityInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_ActivityRecordFails_ShouldCompensateGraphStubAndRethrow()
    {
        WorkflowContext context = CreateContext();
        context.CallActivityAsync<bool>(nameof(ProjectAnnotationGraphActivity), Arg.Any<AnnotationProjectionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallChildWorkflowAsync<IngestionResult>(
                nameof(IngestionWorkflow),
                Arg.Any<IngestionInput>(),
                Arg.Any<ChildWorkflowTaskOptions>())
            .Returns(CreateIngestionResult());
        context.CallActivityAsync<bool>(nameof(RecordCaseActivityActivity), Arg.Any<CaseActivityInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<bool>(WorkflowTestHelpers.CreateTaskFailedException()));
        context.CallActivityAsync<bool>(nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        AnnotationProjectionWorkflow workflow = new();

        _ = await Should.ThrowAsync<WorkflowTaskFailedException>(() => workflow.RunAsync(context, Input));

        await context.Received(1).CallActivityAsync<bool>(
            nameof(CleanupGraphActivity),
            Arg.Is<CleanupInput>(i => i.MemoryUnitId == Input.AnnotationMemoryUnitId && i.TenantId == Input.TenantId),
            Arg.Any<WorkflowTaskOptions>());
    }

    private static WorkflowContext CreateContext()
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.InstanceId.Returns("annotation-project-ann-001");
        return context;
    }

    private static void SetupHappyPath(WorkflowContext context)
    {
        context.CallActivityAsync<bool>(nameof(ProjectAnnotationGraphActivity), Arg.Any<AnnotationProjectionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallChildWorkflowAsync<IngestionResult>(
                nameof(IngestionWorkflow),
                Arg.Any<IngestionInput>(),
                Arg.Any<ChildWorkflowTaskOptions>())
            .Returns(CreateIngestionResult());
        context.CallActivityAsync<bool>(nameof(RecordCaseActivityActivity), Arg.Any<CaseActivityInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
    }

    private static IngestionResult CreateIngestionResult()
        => new(
            Input.AnnotationMemoryUnitId,
            MemoryUnitStatus.Indexed,
            DateTimeOffset.Parse("2026-07-04T10:00:00+00:00"),
            WasDuplicate: false,
            ConsistencyNote: null);
}
