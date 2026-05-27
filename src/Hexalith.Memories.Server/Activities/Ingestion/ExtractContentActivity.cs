// <copyright file="ExtractContentActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

/// <summary>DAPR Workflow activity that extracts text content via Kreuzberg.</summary>
public sealed class ExtractContentActivity : WorkflowActivity<ExtractionInput, ExtractionResult>
{
    private readonly IContentExtractionClient _client;
    private readonly PerTenantConcurrencyGate _gate;

    /// <summary>Initializes a new instance of the <see cref="ExtractContentActivity"/> class.</summary>
    /// <param name="client">The content extraction client.</param>
    /// <param name="gate">The per-tenant concurrency gate (Story 6.2).</param>
    public ExtractContentActivity(IContentExtractionClient client, PerTenantConcurrencyGate gate)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(gate);
        _client = client;
        _gate = gate;
    }

    /// <inheritdoc/>
    public override async Task<ExtractionResult> RunAsync(
        WorkflowActivityContext context,
        ExtractionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);

        await using IAsyncDisposable lease = await _gate
            .AcquireAsync(input.TenantId, CancellationToken.None)
            .ConfigureAwait(false);

        // Let exceptions propagate — DAPR Workflow retry policy handles retries.
        return await _client.ExtractAsync(input).ConfigureAwait(false);
    }
}
