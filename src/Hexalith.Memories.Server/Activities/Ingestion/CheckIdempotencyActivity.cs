// <copyright file="CheckIdempotencyActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Client;
using Dapr.Workflow;

/// <summary>DAPR Workflow activity that checks whether a source has already been ingested (dedup).</summary>
public sealed class CheckIdempotencyActivity : WorkflowActivity<IdempotencyInput, IdempotencyResult>
{
    private readonly DaprClient _daprClient;

    public CheckIdempotencyActivity(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    /// <inheritdoc/>
    public override async Task<IdempotencyResult> RunAsync(
        WorkflowActivityContext context,
        IdempotencyInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.SourceUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.CaseId);

        string dedupKey = DedupKeyBuilder.BuildKey(input.TenantId, input.CaseId, input.SourceUri);
        string? existing = await _daprClient.GetStateAsync<string>("statestore", dedupKey).ConfigureAwait(false);

        return !string.IsNullOrEmpty(existing)
            ? new IdempotencyResult(true, existing)
            : new IdempotencyResult(false, null);
    }
}
