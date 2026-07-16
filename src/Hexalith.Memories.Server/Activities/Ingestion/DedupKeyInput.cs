// <copyright file="DedupKeyInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Input for the dedup key persistence activity.</summary>
/// <param name="DedupKey">The dedup key to store (format: dedup:{tenantId}:{caseId}:{hash}).</param>
/// <param name="MemoryUnitId">The memory unit ID to associate with the dedup key.</param>
public sealed record DedupKeyInput(
    string DedupKey,
    string MemoryUnitId,
    WorkflowTraceContext? TraceContext = null) : IWorkflowTraceContextCarrier;
