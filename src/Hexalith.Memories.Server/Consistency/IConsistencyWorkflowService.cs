// <copyright file="IConsistencyWorkflowService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Consistency;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Workflows;

/// <summary>
/// Small seam around <see cref="Dapr.Workflow.DaprWorkflowClient"/> for consistency endpoints.
/// </summary>
public interface IConsistencyWorkflowService
{
    /// <summary>Schedules a consistency-verification workflow instance.</summary>
    Task ScheduleVerificationAsync(string instanceId, ConsistencyVerificationInput input, CancellationToken cancellationToken);

    /// <summary>Gets the current status of a consistency-verification workflow instance.</summary>
    Task<ConsistencyVerificationStatus?> GetVerificationStatusAsync(string instanceId, CancellationToken cancellationToken);

    /// <summary>Schedules a consistency-repair workflow instance.</summary>
    Task ScheduleRepairAsync(string instanceId, ConsistencyRepairInput input, CancellationToken cancellationToken);

    /// <summary>Gets the current status of a consistency-repair workflow instance.</summary>
    Task<ConsistencyRepairStatus?> GetRepairStatusAsync(string instanceId, CancellationToken cancellationToken);
}