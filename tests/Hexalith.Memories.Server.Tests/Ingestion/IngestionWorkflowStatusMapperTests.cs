// <copyright file="IngestionWorkflowStatusMapperTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Reflection;

using Dapr.Common.Serialization;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Shouldly;

public sealed class IngestionWorkflowStatusMapperTests
{
    private static readonly JsonDaprSerializer Serializer = new(MemoriesJsonContext.Options);
    private static readonly Type WorkflowMetadataType = Type.GetType(
        "Dapr.Workflow.Client.WorkflowMetadata, Dapr.Workflow",
        throwOnError: true)!;

    [Fact]
    public void TryMap_NullWorkflowState_ReturnsFalse()
    {
        bool mapped = IngestionWorkflowStatusMapper.TryMap("wf-1", null, out IngestionWorkflowStatus? status);

        mapped.ShouldBeFalse();
        status.ShouldBeNull();
    }

    [Fact]
    public void TryMap_UnreadableInput_ReturnsFalse()
    {
        WorkflowState state = CreateWorkflowState("wf-1", WorkflowRuntimeStatus.Running, inputJson: "{");

        bool mapped = IngestionWorkflowStatusMapper.TryMap("wf-1", state, out IngestionWorkflowStatus? status);

        mapped.ShouldBeFalse();
        status.ShouldBeNull();
    }

    [Fact]
    public void TryMap_RunningWorkflow_ProjectsSafeInputAndRuntimeFields()
    {
        WorkflowState state = CreateWorkflowState(
            "wf-running",
            WorkflowRuntimeStatus.Running,
            input: CreateInput("tenant-a", "case-1"));

        bool mapped = IngestionWorkflowStatusMapper.TryMap("wf-running", state, out IngestionWorkflowStatus? status);

        mapped.ShouldBeTrue();
        status.ShouldNotBeNull();
        status.InstanceId.ShouldBe("wf-running");
        status.TenantId.ShouldBe("tenant-a");
        status.CaseId.ShouldBe("case-1");
        status.RuntimeStatus.ShouldBe("Running");
        status.MemoryUnitId.ShouldBeNull();
        status.MemoryUnitStatus.ShouldBeNull();
        status.FailureSummary.ShouldBeNull();
    }

    [Fact]
    public void TryMap_CompletedIndexedOutput_ProjectsSafeCompletionFields()
    {
        WorkflowState state = CreateWorkflowState(
            "wf-complete",
            WorkflowRuntimeStatus.Completed,
            input: CreateInput("tenant-a", "case-1"),
            output: new IngestionResult(
                "mu-1",
                MemoryUnitStatus.Indexed,
                DateTimeOffset.Parse("2026-07-04T10:05:00+00:00"),
                WasDuplicate: false,
                ConsistencyNote: null));

        bool mapped = IngestionWorkflowStatusMapper.TryMap("wf-complete", state, out IngestionWorkflowStatus? status);

        mapped.ShouldBeTrue();
        status.ShouldNotBeNull();
        status.MemoryUnitId.ShouldBe("mu-1");
        status.MemoryUnitStatus.ShouldBe(MemoryUnitStatus.Indexed);
        status.FailureSummary.ShouldBeNull();
    }

    [Fact]
    public void TryMap_CompletedFailedOutput_ProjectsSafeFailedStatus()
    {
        WorkflowState state = CreateWorkflowState(
            "wf-failed-output",
            WorkflowRuntimeStatus.Completed,
            input: CreateInput("tenant-a", "case-1"),
            output: new IngestionResult(
                "mu-2",
                MemoryUnitStatus.Failed,
                DateTimeOffset.Parse("2026-07-04T10:05:00+00:00"),
                WasDuplicate: false,
                ConsistencyNote: "missing backend"));

        bool mapped = IngestionWorkflowStatusMapper.TryMap("wf-failed-output", state, out IngestionWorkflowStatus? status);

        mapped.ShouldBeTrue();
        status.ShouldNotBeNull();
        status.MemoryUnitId.ShouldBe("mu-2");
        status.MemoryUnitStatus.ShouldBe(MemoryUnitStatus.Failed);
        status.FailureSummary.ShouldBe("Workflow completed with failed ingestion status.");
    }

    [Fact]
    public void TryMap_OutputDeserializationFailure_DoesNotLeakRawOutput()
    {
        WorkflowState state = CreateWorkflowState(
            "wf-bad-output",
            WorkflowRuntimeStatus.Completed,
            input: CreateInput("tenant-a", "case-1"),
            outputJson: "{\"memoryUnitId\":");

        bool mapped = IngestionWorkflowStatusMapper.TryMap("wf-bad-output", state, out IngestionWorkflowStatus? status);

        mapped.ShouldBeTrue();
        status.ShouldNotBeNull();
        status.MemoryUnitId.ShouldBeNull();
        status.MemoryUnitStatus.ShouldBeNull();
        status.FailureSummary.ShouldBe("Workflow output could not be projected safely.");
    }

    private static IngestionInput CreateInput(string tenantId, string caseId) => new()
    {
        TenantId = tenantId,
        CaseId = caseId,
        SourceUri = "test://source",
        ContentType = "text/plain",
        SourceType = SourceType.Event,
        IngestedBy = "operator-1",
    };

    private static WorkflowState CreateWorkflowState(
        string instanceId,
        WorkflowRuntimeStatus status,
        IngestionInput? input = null,
        IngestionResult? output = null,
        string? inputJson = null,
        string? outputJson = null)
    {
        DateTime created = new(2026, 7, 4, 10, 0, 0, DateTimeKind.Utc);
        object metadata = Activator.CreateInstance(
            WorkflowMetadataType,
            instanceId,
            "IngestionWorkflow",
            status,
            created,
            created.AddMinutes(5),
            Serializer)!;
        WorkflowMetadataType.GetProperty("SerializedInput")!
            .SetValue(metadata, inputJson ?? (input is null ? null : Serializer.Serialize(input)));
        WorkflowMetadataType.GetProperty("SerializedOutput")!
            .SetValue(metadata, outputJson ?? (output is null ? null : Serializer.Serialize(output)));

        return (WorkflowState)Activator.CreateInstance(
            typeof(WorkflowState),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [metadata],
            culture: null)!;
    }
}
