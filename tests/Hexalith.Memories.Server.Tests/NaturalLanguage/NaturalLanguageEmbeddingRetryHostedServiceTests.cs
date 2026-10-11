// <copyright file="NaturalLanguageEmbeddingRetryHostedServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using Dapr.Workflow;

using Hexalith.Memories.Server.NaturalLanguage;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public class NaturalLanguageEmbeddingRetryHostedServiceTests
{
    [Fact]
    public async Task TickAsync_DisabledDuringBacklogRead_StopsBeforeDequeuing()
    {
        var registry = Substitute.For<IFailedNaturalLanguageEmbeddingRegistry>();
        var options = Substitute.For<IOptionsMonitor<NaturalLanguageDescriptionOptions>>();
        bool enabled = true;
        options.CurrentValue.Returns(_ => new NaturalLanguageDescriptionOptions { RetryWorkerEnabled = enabled });
        registry.ListTenantsWithBacklogAsync(Arg.Any<CancellationToken>()).Returns(_ => OneTenant());
        registry.GetBacklogCountAsync("tenant-a", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            enabled = false;
            return Task.FromResult(2L);
        });
        var worker = new NaturalLanguageEmbeddingRetryHostedService(
            registry,
            Substitute.For<IDaprWorkflowClient>(),
            options,
            NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance);

        await worker.TickAsync(new NaturalLanguageDescriptionOptions(), TestContext.Current.CancellationToken);

        _ = registry.Received(1).GetBacklogCountAsync("tenant-a", Arg.Any<CancellationToken>());
        _ = registry.DidNotReceive().DequeueBatchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    private static async IAsyncEnumerable<string> OneTenant()
    {
        yield return "tenant-a";
        await Task.CompletedTask;
    }

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
    [InlineData(WorkflowRuntimeStatus.Canceled, true)]
    [InlineData(WorkflowRuntimeStatus.Terminated, true)]
    [InlineData(WorkflowRuntimeStatus.Running, false)]
    [InlineData(WorkflowRuntimeStatus.Pending, false)]
    public void IsTerminalStatus_MatchesExpectedTerminalStates(WorkflowRuntimeStatus status, bool expected)
    {
        NaturalLanguageEmbeddingRetryHostedService.IsTerminalStatus(status).ShouldBe(expected);
    }
}
