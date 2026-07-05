// <copyright file="CleanupWorkflowPayloadsInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Input for deleting transient claim-checked workflow payloads.</summary>
public sealed record CleanupWorkflowPayloadsInput(
    string TenantId,
    string MemoryUnitId,
    IReadOnlyList<WorkflowPayloadReference> References);
