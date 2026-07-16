// <copyright file="EmbeddingInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Input for the embedding generation activity.</summary>
/// <remarks>
/// <para>Story 9.2 Task 3.1 / Risk #17 — <see cref="ContentKind"/> is a POSITIONAL parameter with a
/// default value. DO NOT switch to property-init record syntax — the positional JSON wire shape is
/// load-bearing for durable workflow replay across the ingestion plane (<c>IngestionWorkflow</c>,
/// <c>ReIngestionCoordinator</c> from Story 6.3, future backfill workflows). A paused workflow's
/// history carries <c>{"TenantId":"t","ContentText":"c"}</c> (9.1 shape) — the default value ensures
/// those histories deserialize correctly with <see cref="EmbeddingContentKind.Payload"/> applied.</para>
/// </remarks>
/// <param name="TenantId">The tenant identifier for rate limiting and secret scoping.</param>
/// <param name="ContentText">The extracted text content to generate embeddings for.</param>
/// <param name="ContentKind">Classifies whether the text is a raw extracted payload (default) or an
/// LLM-authored natural-language description. Drives telemetry partitioning so operators can observe
/// the 2:1 call volume split under dual-embedding (Risk #6).</param>
public sealed record EmbeddingInput(
    string TenantId,
    string ContentText,
    EmbeddingContentKind ContentKind = EmbeddingContentKind.Payload,
    WorkflowPayloadReference? ContentReference = null,
    WorkflowTraceContext? TraceContext = null) : IWorkflowTraceContextCarrier;
