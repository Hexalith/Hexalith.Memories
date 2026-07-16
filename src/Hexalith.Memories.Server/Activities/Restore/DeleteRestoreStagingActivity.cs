// <copyright file="DeleteRestoreStagingActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Restore;

using Dapr.Workflow;

using Hexalith.Memories.Server.Import;

using Microsoft.Extensions.Logging;

/// <summary>Story 26.2 — deletes the staged export payload once the restore workflow has drained it.</summary>
internal sealed class DeleteRestoreStagingActivity : WorkflowActivity<string, bool>
{
    private readonly ILogger<DeleteRestoreStagingActivity> _logger;
    private readonly IImportStagingStore _stagingStore;

    /// <summary>Initializes a new instance of the <see cref="DeleteRestoreStagingActivity"/> class.</summary>
    public DeleteRestoreStagingActivity(
        IImportStagingStore stagingStore,
        ILogger<DeleteRestoreStagingActivity> logger)
    {
        _stagingStore = stagingStore;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, string stagingKey)
    {
        if (string.IsNullOrWhiteSpace(stagingKey))
        {
            return false;
        }

        try
        {
            await _stagingStore.DeleteAsync(stagingKey, CancellationToken.None).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Best-effort cleanup failed for restore staging key {StagingKey}; TTL expiry remains the backstop.", stagingKey);
            return false;
        }
    }
}
