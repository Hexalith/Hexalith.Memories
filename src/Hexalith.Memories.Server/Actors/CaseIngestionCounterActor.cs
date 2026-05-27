// <copyright file="CaseIngestionCounterActor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using Dapr.Actors.Runtime;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;

/// <summary>DAPR Actor maintaining per-case in-flight ingestion counts (Story 6.3 FR10). Thin host
/// delegating to <see cref="CaseIngestionCounterLogic"/>.</summary>
internal sealed class CaseIngestionCounterActor : Actor, ICaseIngestionCounterActor
{
    private const string StateName = "counterState";

    private readonly CaseIngestionCounterLogic _logic;
    private readonly ILogger<CaseIngestionCounterActor> _logger;

    /// <summary>Initializes a new instance of the <see cref="CaseIngestionCounterActor"/> class.</summary>
    /// <param name="host">The actor host provided by the DAPR runtime.</param>
    /// <param name="logic">The transition logic.</param>
    public CaseIngestionCounterActor(
        ActorHost host,
        CaseIngestionCounterLogic logic,
        ILogger<CaseIngestionCounterActor> logger)
        : base(host)
    {
        ArgumentNullException.ThrowIfNull(logic);
        ArgumentNullException.ThrowIfNull(logger);
        _logic = logic;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task TransitionAsync(string previousStage, string nextStage, string transitionId)
    {
        CaseIngestionCounterState current = await GetOrCreateStateAsync().ConfigureAwait(false);
        CaseIngestionCounterState next = _logic.Transition(current, previousStage, nextStage, transitionId);
        (string tenantId, string caseId) = GetTenantAndCaseIds();
        if (ReferenceEquals(next, current))
        {
            RetryFailureLog.LogCounterActorTransitionIdempotent(_logger, tenantId, caseId, transitionId);
            return;
        }

        await StateManager.SetStateAsync(StateName, next).ConfigureAwait(false);
        RetryFailureLog.LogCounterActorTransitionApplied(_logger, tenantId, caseId, previousStage, nextStage, transitionId);
    }

    /// <inheritdoc/>
    public async Task<CaseIngestionCounts> GetCountsAsync()
        => _logic.ToCounts(await GetOrCreateStateAsync().ConfigureAwait(false));

    /// <inheritdoc/>
    public async Task ResetAsync()
        => await StateManager
            .SetStateAsync(StateName, new CaseIngestionCounterState(0, 0, 0, 0, null))
            .ConfigureAwait(false);

    private async Task<CaseIngestionCounterState> GetOrCreateStateAsync()
    {
        ConditionalValue<CaseIngestionCounterState> value = await StateManager
            .TryGetStateAsync<CaseIngestionCounterState>(StateName)
            .ConfigureAwait(false);
        return value.HasValue ? value.Value : new CaseIngestionCounterState(0, 0, 0, 0, null);
    }

    private (string TenantId, string CaseId) GetTenantAndCaseIds()
    {
        string actorId = Id.GetId();
        int separatorIndex = actorId.IndexOf(':');
        return separatorIndex < 0
            ? (actorId, string.Empty)
            : (actorId[..separatorIndex], actorId[(separatorIndex + 1)..]);
    }
}
