// <copyright file="NaturalLanguageDescriptionInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Story 9.2 Task 2.1 — input for <c>GenerateNaturalLanguageDescriptionActivity</c>. Feeds the
/// DAPR Conversation API with enough context to author a single-sentence natural-language summary of an
/// event payload, to be embedded into the NL semantic index for business-meaning search (FR60).</summary>
/// <param name="TenantId">The tenant identifier — drives the DAPR sidecar tenant-scope metadata and
/// downstream embedding-rate-limit accounting.</param>
/// <param name="MemoryUnitId">The memory unit identifier — used for logging correlation and retry-queue
/// keying.</param>
/// <param name="RawJsonPayload">The raw JSON event payload (UTF-8 decoded). The activity truncates to
/// <c>NaturalLanguageDescriptionOptions.MaxPayloadChars</c> before building the prompt.</param>
/// <param name="EventType">The CloudEvents <c>type</c> attribute (e.g., <c>"CounterIncrementedV1"</c>).
/// Populated by Story 9.1's <c>CloudEventToIngestionInputMapper</c> and tagged
/// <see cref="MetadataOrigin.System"/>.</param>
/// <param name="AggregateType">The optional aggregate type (from the <c>event.aggregateType</c> metadata
/// field). <see langword="null"/> when absent — renders as <c>"(unspecified)"</c> in the prompt.</param>
/// <param name="RawPayloadReference">Optional claim-check reference for the raw event payload.</param>
public sealed record NaturalLanguageDescriptionInput(
    string TenantId,
    string MemoryUnitId,
    string RawJsonPayload,
    string EventType,
    string? AggregateType,
    WorkflowPayloadReference? RawPayloadReference = null,
    WorkflowTraceContext? TraceContext = null) : IWorkflowTraceContextCarrier;
