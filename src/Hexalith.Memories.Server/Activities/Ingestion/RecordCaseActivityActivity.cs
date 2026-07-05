// <copyright file="RecordCaseActivityActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Server.Activities;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Cases;

/// <summary>DAPR Workflow activity that records a case activity event via Redis Streams.</summary>
internal sealed class RecordCaseActivityActivity : WorkflowTraceLinkedActivity<CaseActivityInput, bool>
{
    private readonly CaseActivityService _activityService;

    public RecordCaseActivityActivity(CaseActivityService activityService)
    {
        _activityService = activityService;
    }

    /// <inheritdoc/>
    protected override async Task<bool> RunActivityAsync(WorkflowActivityContext context, CaseActivityInput input)
    {
        return await _activityService.RecordEventAsync(
            input.TenantId,
            input.CaseId,
            input.EventType,
            input.Actor,
            input.Description,
            input.MemoryUnitId).ConfigureAwait(false);
    }
}
