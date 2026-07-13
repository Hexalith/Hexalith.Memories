// <copyright file="ImportEndpointStatusTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using Dapr.Workflow;

using Hexalith.Memories.Server.Endpoints;

using Shouldly;

/// <summary>Tests restore workflow status projection.</summary>
[Trait("Category", "Unit")]
public sealed class ImportEndpointStatusTests
{
    [Theory]
    [InlineData(WorkflowRuntimeStatus.Completed, "reindexing", "Completed")]
    [InlineData(WorkflowRuntimeStatus.Failed, "restoring-data-plane", "Failed")]
    [InlineData(WorkflowRuntimeStatus.Canceled, "cleaning-up", "Canceled")]
    [InlineData(WorkflowRuntimeStatus.Terminated, "reindexing", "Terminated")]
    [InlineData(WorkflowRuntimeStatus.Running, "reindexing", "reindexing")]
    [InlineData(WorkflowRuntimeStatus.Pending, null, "Pending")]
    public void ResolveReportedStatus_TerminalRuntimeStateTakesPrecedenceOverCustomProgress(
        WorkflowRuntimeStatus runtimeStatus,
        string? customStatus,
        string expected)
        => ImportEndpoints.ResolveReportedStatus(runtimeStatus, customStatus).ShouldBe(expected);
}
