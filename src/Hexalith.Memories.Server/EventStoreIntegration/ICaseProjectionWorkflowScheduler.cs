// <copyright file="ICaseProjectionWorkflowScheduler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

/// <summary>Schedules case and memory-unit projection workflows after EventStore command acceptance.</summary>
internal interface ICaseProjectionWorkflowScheduler
{
    /// <summary>Schedules a projection workflow.</summary>
    Task<string> ScheduleAsync(
        string workflowName,
        string instanceId,
        object input,
        CancellationToken cancellationToken);
}
