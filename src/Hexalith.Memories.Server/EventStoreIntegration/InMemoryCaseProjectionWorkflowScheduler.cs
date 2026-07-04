// <copyright file="InMemoryCaseProjectionWorkflowScheduler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

/// <summary>Test fallback scheduler used only when services are manually constructed without Dapr.</summary>
internal sealed class InMemoryCaseProjectionWorkflowScheduler : ICaseProjectionWorkflowScheduler
{
    /// <inheritdoc/>
    public Task<string> ScheduleAsync(
        string workflowName,
        string instanceId,
        object input,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowName);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(input);
        return Task.FromResult(instanceId);
    }
}
