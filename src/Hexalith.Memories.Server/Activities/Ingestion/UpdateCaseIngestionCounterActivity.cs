// <copyright file="UpdateCaseIngestionCounterActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Server.Activities;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;

/// <summary>DAPR Workflow activity that forwards a stage transition to the per-case counter actor (Story 6.3 FR10).
/// Best-effort: a failure logs event 6310 and returns <c>false</c> but does NOT break the workflow.</summary>
internal sealed class UpdateCaseIngestionCounterActivity : WorkflowTraceLinkedActivity<CounterTransitionInput, bool>
{
    private readonly IActorProxyFactory _actorFactory;
    private readonly ILogger<UpdateCaseIngestionCounterActivity> _logger;

    public UpdateCaseIngestionCounterActivity(
        IActorProxyFactory actorFactory,
        ILogger<UpdateCaseIngestionCounterActivity> logger)
    {
        ArgumentNullException.ThrowIfNull(actorFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _actorFactory = actorFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task<bool> RunActivityAsync(WorkflowActivityContext context, CounterTransitionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        try
        {
            ICaseIngestionCounterActor proxy = _actorFactory.CreateActorProxy<ICaseIngestionCounterActor>(
                new ActorId($"{input.TenantId}:{input.CaseId}"),
                nameof(CaseIngestionCounterActor));
            await proxy.TransitionAsync(input.PreviousStage, input.NextStage, input.TransitionId).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            RetryFailureLog.LogCounterTransitionFailed(
                _logger, input.TenantId, input.CaseId, input.PreviousStage, input.NextStage, ex.Message);
            return false;
        }
    }
}
