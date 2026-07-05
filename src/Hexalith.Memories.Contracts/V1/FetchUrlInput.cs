// <copyright file="FetchUrlInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input to the URL fetch workflow activity.</summary>
/// <param name="Url">Absolute http(s) URL to fetch.</param>
/// <param name="MemoryUnitId">Correlating memory unit identifier (used for structured logging).</param>
/// <param name="TenantId">
/// Tenant identifier used by the per-tenant extraction concurrency gate (Story 6.2). Defaults to the empty
/// string so legacy workflow history that predates the field deserializes without throwing; the activity
/// treats empty/whitespace values as a contract-violation and fails fast.
/// </param>
public sealed record FetchUrlInput(
    string Url,
    string MemoryUnitId,
    string TenantId = "",
    WorkflowTraceContext? TraceContext = null) : IWorkflowTraceContextCarrier;
