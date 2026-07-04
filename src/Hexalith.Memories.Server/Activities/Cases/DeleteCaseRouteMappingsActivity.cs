// <copyright file="DeleteCaseRouteMappingsActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using Dapr.Workflow;

using Hexalith.Memories.EventStore;

/// <summary>Deletes aggregate event routes that still point at a deleted case.</summary>
internal sealed class DeleteCaseRouteMappingsActivity(
    IAggregateCaseMappingStore mappingStore,
    ITenantEventRouteCacheInvalidator cacheInvalidator) : WorkflowActivity<CaseProjectionCleanupInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, CaseProjectionCleanupInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        _ = await mappingStore
            .DeleteCaseMappingsAsync(input.TenantId, input.CaseId, CancellationToken.None)
            .ConfigureAwait(false);
        cacheInvalidator.InvalidateCaseRoutes(input.TenantId, input.CaseId);
        return true;
    }
}
