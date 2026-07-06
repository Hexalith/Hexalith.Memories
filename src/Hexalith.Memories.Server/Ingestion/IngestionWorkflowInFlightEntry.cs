// <copyright file="IngestionWorkflowInFlightEntry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Represents an app-tracked ingestion workflow instance.</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="InstanceId">The workflow instance identifier.</param>
/// <param name="TrackedAt">The UTC time when the instance was tracked.</param>
internal sealed record IngestionWorkflowInFlightEntry(
    string TenantId,
    string InstanceId,
    DateTimeOffset TrackedAt);
