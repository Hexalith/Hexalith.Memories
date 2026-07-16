// <copyright file="DeleteRestoreStagingActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Restore;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Restore;
using Hexalith.Memories.Server.Import;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>Proves cleanup cannot turn a successful restore into a failed workflow.</summary>
public sealed class DeleteRestoreStagingActivityTests
{
    [Fact]
    public async Task RunAsync_DeleteFails_ReturnsFalseWithoutThrowing()
    {
        IImportStagingStore store = Substitute.For<IImportStagingStore>();
        store.DeleteAsync("staging-key", CancellationToken.None).ThrowsAsync(new InvalidOperationException("offline"));
        DeleteRestoreStagingActivity activity = new(
            store,
            Substitute.For<ILogger<DeleteRestoreStagingActivity>>());

        bool result = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), "staging-key");

        result.ShouldBeFalse();
    }
}
