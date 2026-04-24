// <copyright file="NaturalLanguageEmbeddingRetryHostedServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using Dapr.Workflow;

using Hexalith.Memories.Server.NaturalLanguage;

using Shouldly;

public class NaturalLanguageEmbeddingRetryHostedServiceTests
{
    [Fact]
    public void GetRetryWorkflowInstanceId_IncludesTenantAndMemoryUnit()
    {
        NaturalLanguageEmbeddingRetryHostedService
            .GetRetryWorkflowInstanceId("tenant-a", "mu-001")
            .ShouldBe("retry-nl-tenant-a-mu-001");
    }

    [Theory]
    [InlineData(WorkflowRuntimeStatus.Completed, true)]
    [InlineData(WorkflowRuntimeStatus.Failed, true)]
    [InlineData(WorkflowRuntimeStatus.Terminated, true)]
    [InlineData(WorkflowRuntimeStatus.Running, false)]
    [InlineData(WorkflowRuntimeStatus.Pending, false)]
    public void IsTerminalStatus_MatchesExpectedTerminalStates(WorkflowRuntimeStatus status, bool expected)
    {
        NaturalLanguageEmbeddingRetryHostedService.IsTerminalStatus(status).ShouldBe(expected);
    }
}
