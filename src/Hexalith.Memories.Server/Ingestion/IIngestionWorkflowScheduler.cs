// <copyright file="IIngestionWorkflowScheduler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Internal seam for scheduling ingestion workflows with a caller-specified workflow instance id.</summary>
internal interface IIngestionWorkflowScheduler
{
    Task<string> ScheduleAsync(string instanceId, IngestionInput input, CancellationToken cancellationToken = default);
}
