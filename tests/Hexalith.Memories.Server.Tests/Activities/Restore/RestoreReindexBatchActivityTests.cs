// <copyright file="RestoreReindexBatchActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Restore;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Restore;
using Hexalith.Memories.Server.Import;

using NSubstitute;

using Shouldly;

/// <summary>Tests bounded staged identifier paging and lease renewal.</summary>
public sealed class RestoreReindexBatchActivityTests
{
    [Fact]
    public async Task RunAsync_MissingPage_FailsClosedAfterRenewal()
    {
        IImportStagingStore store = Substitute.For<IImportStagingStore>();
        store.OwnsRestoreLeaseAsync("staging", CancellationToken.None).Returns(true);
        store.ReadReindexIdsAsync("staging", 100, 2, CancellationToken.None).Returns(["mu-1"]);
        RestoreReindexBatchActivity activity = new(store, Substitute.For<IRestoreReindexUnitProcessor>());

        await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RestoreReindexBatchInput("acme", "staging", 100, 2)));

        await store.Received(1).RenewAsync("staging", CancellationToken.None);
    }
}
