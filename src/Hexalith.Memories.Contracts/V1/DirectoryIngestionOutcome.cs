// <copyright file="DirectoryIngestionOutcome.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Synchronous summary returned by POST /api/v1/ingest/directory.</summary>
/// <param name="BatchId">Server-generated ULID; correlates to each scheduled workflow's CorrelationId.</param>
/// <param name="Discovered">Total files the enumerator saw (after recursive traversal).</param>
/// <param name="Enqueued">Number of IngestionWorkflow instances scheduled (one per accepted file).</param>
/// <param name="Skipped">Files the endpoint rejected at discovery (bounded by MaxSkippedReportSize).</param>
/// <param name="SkippedTruncated">True when additional skipped files were omitted from <paramref name="Skipped"/>.</param>
/// <param name="InstanceIds">Workflow instance identifiers (one per accepted file).</param>
/// <param name="TenantId">The tenant that scheduled the batch.</param>
/// <param name="CaseId">The target case.</param>
public sealed record DirectoryIngestionOutcome(
    string BatchId,
    int Discovered,
    int Enqueued,
    IReadOnlyList<SkippedFile> Skipped,
    bool SkippedTruncated,
    IReadOnlyList<string> InstanceIds,
    string TenantId,
    string CaseId);
