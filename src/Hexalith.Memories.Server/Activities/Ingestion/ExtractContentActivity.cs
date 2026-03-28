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

    /// <summary>Initializes a new instance of the <see cref="ExtractContentActivity"/> class.</summary>
    /// <param name="client">The content extraction client.</param>
    public ExtractContentActivity(IContentExtractionClient client)
    {
        _client = client;
    }

    /// <inheritdoc/>
    public override async Task<ExtractionResult> RunAsync(
        WorkflowActivityContext context,
        ExtractionInput input)
    {
        // Let exceptions propagate — DAPR Workflow retry policy handles retries.
        return await _client.ExtractAsync(input).ConfigureAwait(false);
    }
}
