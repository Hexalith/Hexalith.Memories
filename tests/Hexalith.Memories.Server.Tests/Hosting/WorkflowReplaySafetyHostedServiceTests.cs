// <copyright file="WorkflowReplaySafetyHostedServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Hosting;

using Dapr.Workflow;

using Hexalith.Memories.Server.Hosting;

using Shouldly;

public class WorkflowReplaySafetyHostedServiceTests
{
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
    public void Timeouts_MatchDocumentedEnvelope()
    {
        // Improvement X split: 5s poll, 5min total, 10s per-call (Improvement Z).
        WorkflowReplaySafetyHostedService.PollInterval.ShouldBe(TimeSpan.FromSeconds(5));
        WorkflowReplaySafetyHostedService.TotalTimeout.ShouldBe(TimeSpan.FromMinutes(5));
        WorkflowReplaySafetyHostedService.PerQueryTimeout.ShouldBe(TimeSpan.FromSeconds(10));
    }
}
