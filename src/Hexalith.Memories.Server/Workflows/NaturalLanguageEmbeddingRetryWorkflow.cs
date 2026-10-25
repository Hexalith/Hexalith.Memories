// <copyright file="NaturalLanguageEmbeddingRetryWorkflow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;

/// <summary>Story 9.2 Task 8.4 — minimal retry orchestration that re-runs the NL description +
/// embedding + index activities for memory units whose first attempt degraded (LLM unavailable).
/// Returns <see cref="NaturalLanguageEmbeddingRetryResult"/> instead of throwing so the hosted
/// service can distinguish retry-soon from dead-letter at a structural level.</summary>
public sealed class NaturalLanguageEmbeddingRetryWorkflow
    : Workflow<NaturalLanguageEmbeddingRetryInput, NaturalLanguageEmbeddingRetryResult>
{
    /// <inheritdoc/>
    public override async Task<NaturalLanguageEmbeddingRetryResult> RunAsync(
        WorkflowContext context,
        NaturalLanguageEmbeddingRetryInput input)
    {
        if (!string.IsNullOrEmpty(input.RawJsonPayload))
        {
            // Historic workflows are drained/purged during erasure. Never replay their raw input
            // into another durable activity; the worker can read the fenced registry on a fresh run.
            return new NaturalLanguageEmbeddingRetryResult(Indexed: false, Reason: "legacy-retry-input-requires-restart");
        }
        return await context.CallActivityAsync<NaturalLanguageEmbeddingRetryResult>(
            nameof(RetryNaturalLanguageEmbeddingActivity), input);
    }
}
