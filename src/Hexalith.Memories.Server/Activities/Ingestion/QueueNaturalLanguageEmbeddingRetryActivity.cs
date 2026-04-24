// <copyright file="QueueNaturalLanguageEmbeddingRetryActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using System.Text;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.NaturalLanguage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Story 9.2 Task 5.4 — workflow-replay-safe facade around the NL retry queue. The ingestion
/// workflow calls this activity (instead of the registry service directly) so the I/O stays on the
/// activity boundary per architecture D25 (Workflow-Activity-Service separation). The record carries
/// bounded payload-by-value (Spike 0.1 fallback — cap at
/// <c>NaturalLanguageDescriptionOptions.QueuedPayloadMaxBytes</c>).</summary>
public sealed partial class QueueNaturalLanguageEmbeddingRetryActivity
    : WorkflowActivity<QueueNaturalLanguageEmbeddingRetryInput, bool>
{
    private readonly IFailedNaturalLanguageEmbeddingRegistry _registry;
    private readonly IOptions<NaturalLanguageDescriptionOptions> _options;
    private readonly ILogger<QueueNaturalLanguageEmbeddingRetryActivity> _logger;

    public QueueNaturalLanguageEmbeddingRetryActivity(
        IFailedNaturalLanguageEmbeddingRegistry registry,
        IOptions<NaturalLanguageDescriptionOptions> options,
        ILogger<QueueNaturalLanguageEmbeddingRetryActivity> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _registry = registry;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(
        WorkflowActivityContext context,
        QueueNaturalLanguageEmbeddingRetryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        string truncatedPayload = Truncate(input.RawJsonPayload, _options.Value.QueuedPayloadMaxBytes);

        FailedNaturalLanguageEmbeddingRecord record = new(
            input.TenantId,
            input.MemoryUnitId,
            truncatedPayload,
            input.EventType,
            input.AggregateType,
            input.CaseId,
            input.EmbeddingProvider,
            input.EmbeddingModel,
            input.EmbeddingDimensions,
            QueuedAtTicks: DateTime.UtcNow.Ticks,
            Attempts: 0);

        await _registry.EnqueueAsync(record, CancellationToken.None).ConfigureAwait(false);
        NaturalLanguageIntegrationLog.NaturalLanguageEmbeddingQueuedForRetry(
            _logger,
            input.TenantId,
            input.MemoryUnitId,
            record.QueuedAtTicks);
        return true;
    }

    internal static string Truncate(string? raw, int maxBytes)
    {
        if (string.IsNullOrEmpty(raw) || maxBytes <= 0)
        {
            return string.Empty;
        }

        byte[] utf8 = Encoding.UTF8.GetBytes(raw);
        if (utf8.Length <= maxBytes)
        {
            return raw;
        }

        StringBuilder builder = new(raw.Length);
        int byteCount = 0;
        foreach (Rune rune in raw.EnumerateRunes())
        {
            if (byteCount + rune.Utf8SequenceLength > maxBytes)
            {
                break;
            }

            _ = builder.Append(rune.ToString());
            byteCount += rune.Utf8SequenceLength;
        }

        return builder.ToString();
    }
}
