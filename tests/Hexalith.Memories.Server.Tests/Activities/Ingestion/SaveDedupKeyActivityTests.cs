// <copyright file="SaveDedupKeyActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using Dapr.Client;
using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Ingestion;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

public class SaveDedupKeyActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldSaveStateWithCorrectKeyAndValue()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SaveDedupKeyActivity activity = new(daprClient);

        await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new DedupKeyInput("dedup:tenant-1:case-1:abc123", "mu-001"));

        await daprClient.Received(1).SaveStateAsync("statestore", "dedup:tenant-1:case-1:abc123", "mu-001");
    }

    [Fact]
    public async Task RunAsync_ShouldReturnTrue()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SaveDedupKeyActivity activity = new(daprClient);

        bool result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new DedupKeyInput("dedup:tenant-1:case-1:abc123", "mu-001"));

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_StateStoreUnavailable_ShouldPropagateException()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.SaveStateAsync("statestore", Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("State store unavailable"));
        SaveDedupKeyActivity activity = new(daprClient);

        await Should.ThrowAsync<InvalidOperationException>(
            () => activity.RunAsync(
                Substitute.For<WorkflowActivityContext>(),
                new DedupKeyInput("dedup:tenant-1:case-1:abc123", "mu-001")));
    }
}
