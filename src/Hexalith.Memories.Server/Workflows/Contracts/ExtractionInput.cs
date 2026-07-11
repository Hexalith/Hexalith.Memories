// <copyright file="ExtractionInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Input for content extraction via Kreuzberg.</summary>
/// <param name="SourceUri">The URI identifying the content source.</param>
/// <param name="ContentBytes">The raw file bytes to extract text from.</param>
/// <param name="ContentType">The MIME type of the content (e.g. application/pdf).</param>
/// <param name="SourceType">The origin type of the ingested content.</param>
/// <param name="TenantId">
/// Tenant identifier used by the per-tenant extraction concurrency gate (Story 6.2). Defaults to the empty
/// string so legacy workflow history that predates the field deserializes without throwing; the activity
/// treats empty/whitespace values as a contract-violation and fails fast.
/// </param>
/// <param name="MemoryUnitId">Optional memory-unit identifier used to scope claim-checked extracted text.</param>
/// <param name="PayloadReference">Optional claim-check reference for <paramref name="ContentBytes"/>.</param>
public sealed record ExtractionInput(
    string SourceUri,
    byte[] ContentBytes,
    string ContentType,
    SourceType SourceType,
    string TenantId = "",
    string MemoryUnitId = "",
    WorkflowPayloadReference? PayloadReference = null,
    WorkflowTraceContext? TraceContext = null) : IWorkflowTraceContextCarrier;
