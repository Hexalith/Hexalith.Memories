// <copyright file="DeleteRestoreStagingActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Restore;

using Dapr.Workflow;

using Hexalith.Memories.Server.Import;

/// <summary>Story 26.2 — deletes the staged export payload once the restore workflow has drained it.</summary>
/// <param name="stagingStore">The import staging store.</param>
internal sealed class DeleteRestoreStagingActivity(IImportStagingStore stagingStore)
    : WorkflowActivity<string, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, string stagingKey)
    {
        if (string.IsNullOrWhiteSpace(stagingKey))
        {
            return false;
        }

        await stagingStore.DeleteAsync(stagingKey, CancellationToken.None).ConfigureAwait(false);
        return true;
    }
}
