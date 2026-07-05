// <copyright file="CleanupWorkflowPayloadsActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Server.Activities;

using Dapr.Workflow;

using Hexalith.Memories.Server.Ingestion;

/// <summary>Workflow activity that deletes transient claim-checked payloads.</summary>
public sealed class CleanupWorkflowPayloadsActivity(IWorkflowPayloadStore payloadStore)
    : WorkflowTraceLinkedActivity<CleanupWorkflowPayloadsInput, bool>
{
    /// <inheritdoc/>
    protected override async Task<bool> RunActivityAsync(WorkflowActivityContext context, CleanupWorkflowPayloadsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MemoryUnitId);

        foreach (var reference in input.References.Distinct())
        {
            if (!string.Equals(reference.TenantId, input.TenantId, StringComparison.Ordinal)
                || !string.Equals(reference.MemoryUnitId, input.MemoryUnitId, StringComparison.Ordinal))
            {
                continue;
            }

            await payloadStore.DeleteAsync(reference, CancellationToken.None).ConfigureAwait(false);
        }

        return true;
    }
}
