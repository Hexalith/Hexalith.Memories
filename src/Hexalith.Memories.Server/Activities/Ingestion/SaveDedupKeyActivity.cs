// <copyright file="SaveDedupKeyActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Client;
using Dapr.Workflow;

/// <summary>DAPR Workflow activity that persists a dedup key to the state store after successful ingestion.</summary>
public sealed class SaveDedupKeyActivity : WorkflowActivity<DedupKeyInput, bool>
{
    private readonly DaprClient _daprClient;

    public SaveDedupKeyActivity(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(
        WorkflowActivityContext context,
        DedupKeyInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.DedupKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MemoryUnitId);

        await _daprClient.SaveStateAsync("statestore", input.DedupKey, input.MemoryUnitId).ConfigureAwait(false);
        return true;
    }
}
