// <copyright file="CheckIdempotencyActivityTests.cs" company="ITANEO">
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

public class CheckIdempotencyActivityTests
{
    [Fact]
    public async Task RunAsync_NewSource_ShouldReturnIsDuplicateFalse()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<string>("statestore", Arg.Any<string>())
            .Returns(Task.FromResult<string>(null!));
        CheckIdempotencyActivity activity = new(daprClient);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IdempotencyResult result = await activity.RunAsync(
            context,
            new IdempotencyInput("file:///doc.pdf", "tenant-1", "case-1"));

        result.IsDuplicate.ShouldBeFalse();
        result.ExistingMemoryUnitId.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_ExistingSource_ShouldReturnIsDuplicateTrue()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<string>("statestore", Arg.Any<string>())
            .Returns("mu-existing-id");
        CheckIdempotencyActivity activity = new(daprClient);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IdempotencyResult result = await activity.RunAsync(
            context,
            new IdempotencyInput("file:///doc.pdf", "tenant-1", "case-1"));

        result.IsDuplicate.ShouldBeTrue();
        result.ExistingMemoryUnitId.ShouldBe("mu-existing-id");
    }

    [Fact]
    public async Task RunAsync_DedupKeyFormat_ShouldUseTenantCaseSourceUriHash()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<string>("statestore", Arg.Any<string>())
            .Returns(Task.FromResult<string>(null!));
        CheckIdempotencyActivity activity = new(daprClient);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        string sourceUri = "file:///doc.pdf";
        string expectedKey = DedupKeyBuilder.BuildKey("tenant-1", "case-1", sourceUri);

        await activity.RunAsync(
            context,
            new IdempotencyInput(sourceUri, "tenant-1", "case-1"));

        await daprClient.Received(1).GetStateAsync<string>("statestore", expectedKey);
    }

    [Fact]
    public async Task RunAsync_StateStoreUnavailable_ShouldPropagateException()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<string>("statestore", Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("State store unavailable"));
        CheckIdempotencyActivity activity = new(daprClient);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await Should.ThrowAsync<InvalidOperationException>(
            () => activity.RunAsync(
                context,
                new IdempotencyInput("file:///doc.pdf", "tenant-1", "case-1")));
    }
}
