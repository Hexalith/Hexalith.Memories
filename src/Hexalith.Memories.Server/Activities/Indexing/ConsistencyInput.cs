// <copyright file="ConsistencyInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Hexalith.Memories.Contracts.V1;

/// <summary>Input for the consistency verification activity.</summary>
/// <param name="MemoryUnitId">The memory unit to verify across all backends.</param>
/// <param name="TenantId">The tenant identifier for namespacing.</param>
public sealed record ConsistencyInput(
    string MemoryUnitId,
    string TenantId,
    WorkflowTraceContext? TraceContext = null) : IWorkflowTraceContextCarrier;
