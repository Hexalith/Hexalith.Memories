// <copyright file="IEventIngestionWorkflowScheduler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using Hexalith.Memories.Contracts.V1;

/// <summary>Adapter used by the EventStore package to schedule a workflow instance for an event-sourced
/// ingestion. Implemented in the Server project as a thin wrapper over <c>DaprWorkflowClient</c> so the
/// package does not depend on Server workflow type names (ADR 9.1-D).</summary>
public interface IEventIngestionWorkflowScheduler
{
    /// <summary>Schedules a new ingestion workflow instance with the caller-supplied <paramref name="instanceId"/>.</summary>
    /// <param name="instanceId">The deterministic workflow instance id — typically derived from the CloudEvents
    /// <c>id</c> and tenant/case so redeliveries collide deterministically.</param>
    /// <param name="input">The mapped <see cref="IngestionInput"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The workflow instance id DAPR confirmed (usually identical to <paramref name="instanceId"/>).</returns>
    Task<string> ScheduleAsync(string instanceId, IngestionInput input, CancellationToken cancellationToken);
}
