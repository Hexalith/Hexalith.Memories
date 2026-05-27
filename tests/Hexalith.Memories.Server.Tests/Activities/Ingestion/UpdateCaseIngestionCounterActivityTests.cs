// <copyright file="UpdateCaseIngestionCounterActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Actors;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

public class UpdateCaseIngestionCounterActivityTests
{
    [Fact]
    public async Task RunAsync_InvokesActorWithExpectedActorIdAndArguments()
    {
        IActorProxyFactory factory = Substitute.For<IActorProxyFactory>();
        ICaseIngestionCounterActor proxy = Substitute.For<ICaseIngestionCounterActor>();
        factory.CreateActorProxy<ICaseIngestionCounterActor>(
            Arg.Is<ActorId>(a => a.GetId() == "tenantA:caseB"),
            nameof(CaseIngestionCounterActor)).Returns(proxy);

        UpdateCaseIngestionCounterActivity activity = new(factory, NullLogger<UpdateCaseIngestionCounterActivity>.Instance);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        bool result = await activity.RunAsync(
            context,
            new CounterTransitionInput("tenantA", "caseB", "queued", "extracting", "tx-1"));

        result.ShouldBeTrue();
        await proxy.Received().TransitionAsync("queued", "extracting", "tx-1");
    }

    [Fact]
    public async Task RunAsync_ActorThrows_ReturnsFalseAndDoesNotPropagate()
    {
        IActorProxyFactory factory = Substitute.For<IActorProxyFactory>();
        ICaseIngestionCounterActor proxy = Substitute.For<ICaseIngestionCounterActor>();
        proxy.TransitionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("actor unreachable")));
        factory.CreateActorProxy<ICaseIngestionCounterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(proxy);

        UpdateCaseIngestionCounterActivity activity = new(factory, NullLogger<UpdateCaseIngestionCounterActivity>.Instance);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        bool result = await activity.RunAsync(
            context,
            new CounterTransitionInput("t", "c", "queued", "none", "tx-2"));

        result.ShouldBeFalse();
    }
}
