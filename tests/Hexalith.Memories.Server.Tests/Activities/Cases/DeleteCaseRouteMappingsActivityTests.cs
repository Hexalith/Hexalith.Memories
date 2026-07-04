// <copyright file="DeleteCaseRouteMappingsActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Cases;

using Dapr.Workflow;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Activities.Cases;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

public sealed class DeleteCaseRouteMappingsActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldDeletePersistedMappingsBeforeInvalidatingCache()
    {
        IAggregateCaseMappingStore store = Substitute.For<IAggregateCaseMappingStore>();
        ITenantEventRouteCacheInvalidator invalidator = Substitute.For<ITenantEventRouteCacheInvalidator>();
        store.DeleteCaseMappingsAsync("tenant-1", "case-1", Arg.Any<CancellationToken>()).Returns(2L);
        DeleteCaseRouteMappingsActivity activity = new(store, invalidator);

        bool result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new CaseProjectionCleanupInput("tenant-1", "case-1"));

        result.ShouldBeTrue();
        Received.InOrder(() =>
        {
            store.DeleteCaseMappingsAsync("tenant-1", "case-1", Arg.Any<CancellationToken>());
            invalidator.InvalidateCaseRoutes("tenant-1", "case-1");
        });
    }

    [Fact]
    public async Task RunAsync_WhenStoreFails_ShouldSurfaceExceptionWithoutInvalidatingCache()
    {
        IAggregateCaseMappingStore store = Substitute.For<IAggregateCaseMappingStore>();
        ITenantEventRouteCacheInvalidator invalidator = Substitute.For<ITenantEventRouteCacheInvalidator>();
        store.DeleteCaseMappingsAsync("tenant-1", "case-1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new RedisExceptionStub());
        DeleteCaseRouteMappingsActivity activity = new(store, invalidator);

        _ = await Should.ThrowAsync<RedisExceptionStub>(() => activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new CaseProjectionCleanupInput("tenant-1", "case-1")));

        invalidator.DidNotReceive().InvalidateCaseRoutes(Arg.Any<string>(), Arg.Any<string>());
    }

    private sealed class RedisExceptionStub : Exception;
}
