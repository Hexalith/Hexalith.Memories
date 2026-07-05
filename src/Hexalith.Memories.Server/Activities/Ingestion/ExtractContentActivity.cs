// <copyright file="ExtractContentActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Server.Activities;

using System.Text;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

/// <summary>DAPR Workflow activity that extracts text content via Kreuzberg.</summary>
public sealed class ExtractContentActivity : WorkflowTraceLinkedActivity<ExtractionInput, ExtractionResult>
{
    private readonly IContentExtractionClient _client;
    private readonly PerTenantConcurrencyGate _gate;
    private readonly IWorkflowPayloadStore? _payloadStore;

    /// <summary>Initializes a new instance of the <see cref="ExtractContentActivity"/> class.</summary>
    /// <param name="client">The content extraction client.</param>
    /// <param name="gate">The per-tenant concurrency gate (Story 6.2).</param>
    public ExtractContentActivity(
        IContentExtractionClient client,
        PerTenantConcurrencyGate gate,
        IWorkflowPayloadStore? payloadStore = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(gate);
        _client = client;
        _gate = gate;
        _payloadStore = payloadStore;
    }

    /// <inheritdoc/>
    protected override async Task<ExtractionResult> RunActivityAsync(
        WorkflowActivityContext context,
        ExtractionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);

        await using IAsyncDisposable lease = await _gate
            .AcquireAsync(input.TenantId, CancellationToken.None)
            .ConfigureAwait(false);

        ExtractionInput effectiveInput = input;
        if (input.PayloadReference is not null)
        {
            byte[] sourceBytes = await RequirePayloadStore()
                .ReadAsync(
                    input.PayloadReference,
                    input.TenantId,
                    input.PayloadReference.MemoryUnitId,
                    input.PayloadReference.ContentKind,
                    CancellationToken.None)
                .ConfigureAwait(false);
            effectiveInput = input with { ContentBytes = sourceBytes };
        }

        // Let exceptions propagate — DAPR Workflow retry policy handles retries.
        ExtractionResult result = await _client.ExtractAsync(effectiveInput).ConfigureAwait(false);
        if (_payloadStore is null)
        {
            return result;
        }

        WorkflowPayloadReference reference = await _payloadStore
            .SaveAsync(
                input.TenantId,
                string.IsNullOrWhiteSpace(input.MemoryUnitId) ? input.SourceUri : input.MemoryUnitId,
                WorkflowPayloadKind.ExtractedText,
                Encoding.UTF8.GetBytes(result.ExtractedContent),
                cancellationToken: CancellationToken.None)
            .ConfigureAwait(false);

        return result with
        {
            ExtractedContent = string.Empty,
            ExtractedContentReference = reference,
        };
    }

    private IWorkflowPayloadStore RequirePayloadStore()
        => _payloadStore ?? throw new WorkflowPayloadException("PAYLOAD_STORE_UNAVAILABLE", "extraction-input");
}
