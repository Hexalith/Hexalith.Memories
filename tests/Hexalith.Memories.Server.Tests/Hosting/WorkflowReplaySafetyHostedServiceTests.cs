// <copyright file="WorkflowReplaySafetyHostedServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Hosting;

using System.Reflection;

using Dapr.Workflow;

using Hexalith.Memories.Server.Hosting;

using Shouldly;

public class WorkflowReplaySafetyHostedServiceTests
{
    [Fact]
    public void WorkflowStateMetadataField_StillExists_OnCurrentSdkSurface()
    {
        // Decision D2 (committed-branch review 2026-04-24): TryGetWorkflowName drills into
        // WorkflowState's private _metadata field to read WorkflowMetadata.Name. This test fails
        // fast if a future Dapr.Workflow SDK renames or removes the field — surfacing the drift
        // at the unit-test level BEFORE the production startup probe logs Critical 9173 and fails
        // open.
        FieldInfo? metadata = typeof(WorkflowState)
            .GetField("_metadata", BindingFlags.Instance | BindingFlags.NonPublic);

        metadata.ShouldNotBeNull(
            "Dapr.Workflow SDK change detected: WorkflowState._metadata private field no longer exists. "
            + "WorkflowReplaySafetyHostedService.TryGetWorkflowName must be updated to the new surface "
            + "or the gate will silently fail open via Critical event 9173 in production.");

        metadata.FieldType.FullName.ShouldBe(
            "Dapr.Workflow.Client.WorkflowMetadata",
            "The private field that TryGetWorkflowName drills through has changed type. "
            + "Update WorkflowReplaySafetyHostedService.MetadataField + TryGetWorkflowName.");

        // WorkflowMetadata.Name is expected to be a public instance property; if this assertion
        // breaks, the drill-through path must follow the new accessor shape.
        PropertyInfo? name = metadata.FieldType
            .GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
        name.ShouldNotBeNull(
            "Dapr.Workflow.Client.WorkflowMetadata.Name is no longer a public instance property. "
            + "WorkflowReplaySafetyHostedService.TryGetWorkflowName must be updated.");
        name.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void IsActive_TerminalStates_ReturnFalse()
    {
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Completed).ShouldBeFalse();
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Failed).ShouldBeFalse();
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Terminated).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_NonTerminalStates_ReturnTrue()
    {
        // Running + Suspended + Pending should all delay startup for the tracked workflow family.
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Running).ShouldBeTrue();
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Suspended).ShouldBeTrue();
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Pending).ShouldBeTrue();
    }

    [Fact]
    public void ShouldCountWorkflow_OnlyTracksIngestionWorkflow()
    {
        WorkflowReplaySafetyHostedService.ShouldCountWorkflow(
            workflowName: "IngestionWorkflow",
            exists: true,
            status: WorkflowRuntimeStatus.Running).ShouldBeTrue();

        WorkflowReplaySafetyHostedService.ShouldCountWorkflow(
            workflowName: "ConsistencyVerificationWorkflow",
            exists: true,
            status: WorkflowRuntimeStatus.Running).ShouldBeFalse();

        WorkflowReplaySafetyHostedService.ShouldCountWorkflow(
            workflowName: "IngestionWorkflow",
            exists: false,
            status: WorkflowRuntimeStatus.Running).ShouldBeFalse();
    }

    [Fact]
    public void ShouldFailOpenForUnreadableWorkflowName_ActiveStatesRequireReadableName()
    {
        WorkflowReplaySafetyHostedService.ShouldFailOpenForUnreadableWorkflowName(
            workflowName: null,
            exists: true,
            status: WorkflowRuntimeStatus.Running).ShouldBeTrue();

        WorkflowReplaySafetyHostedService.ShouldFailOpenForUnreadableWorkflowName(
            workflowName: "  ",
            exists: true,
            status: WorkflowRuntimeStatus.Pending).ShouldBeTrue();

        WorkflowReplaySafetyHostedService.ShouldFailOpenForUnreadableWorkflowName(
            workflowName: "IngestionWorkflow",
            exists: true,
            status: WorkflowRuntimeStatus.Running).ShouldBeFalse();

        WorkflowReplaySafetyHostedService.ShouldFailOpenForUnreadableWorkflowName(
            workflowName: null,
            exists: true,
            status: WorkflowRuntimeStatus.Completed).ShouldBeFalse();
    }

    [Fact]
    public void Timeouts_MatchDocumentedEnvelope()
    {
        // Improvement X split: 5s poll, 5min total, 10s per-call (Improvement Z).
        WorkflowReplaySafetyHostedService.PollInterval.ShouldBe(TimeSpan.FromSeconds(5));
        WorkflowReplaySafetyHostedService.TotalTimeout.ShouldBe(TimeSpan.FromMinutes(5));
        WorkflowReplaySafetyHostedService.PerQueryTimeout.ShouldBe(TimeSpan.FromSeconds(10));
    }
}
