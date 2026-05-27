// <copyright file="EventIngestionWorkflowSchedulerAdapter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Server-side adapter implementing the EventStore package's
/// <see cref="IEventIngestionWorkflowScheduler"/> over the existing
/// <see cref="IIngestionWorkflowScheduler"/> used by the HTTP ingestion endpoints.
/// Keeps the EventStore package free of Server workflow type references (ADR 9.1-D).</summary>
internal sealed class EventIngestionWorkflowSchedulerAdapter : IEventIngestionWorkflowScheduler
{
    private readonly IIngestionWorkflowScheduler _inner;

    public EventIngestionWorkflowSchedulerAdapter(IIngestionWorkflowScheduler inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public Task<string> ScheduleAsync(string instanceId, IngestionInput input, CancellationToken cancellationToken)
        => _inner.ScheduleAsync(instanceId, input);
}
