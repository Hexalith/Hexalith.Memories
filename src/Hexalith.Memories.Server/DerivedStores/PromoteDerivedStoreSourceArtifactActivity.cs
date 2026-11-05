// <copyright file="PromoteDerivedStoreSourceArtifactActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.DerivedStores;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Promotes exact ingestion source bytes and resolved generation evidence before transient cleanup.</summary>
internal sealed class PromoteDerivedStoreSourceArtifactActivity(
    IWorkflowPayloadStore payloadStore,
    RedisDerivedStoreService store) : WorkflowActivity<PromoteDerivedStoreSourceArtifactInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, PromoteDerivedStoreSourceArtifactInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        byte[] sourceBytes = input.SourceBytes;
        if (input.SourceReference is not null)
        {
            WorkflowPayloadKind expectedKind = input.SourceType == SourceType.Url
                ? WorkflowPayloadKind.FetchedUrlBytes : WorkflowPayloadKind.SourceBytes;
            if (input.SourceReference.ContentKind != expectedKind
                || input.SourceReference.TenantId != input.TenantId
                || !SourcePayloadScope.IsExpectedOwner(input.TenantId, input.CaseId, input.SourceUri, input.SourceType,
                    input.MemoryUnitId, input.SourceReference.MemoryUnitId))
            {
                throw new DerivedStoreStateException("SOURCE_ARTIFACT_SCOPE_INVALID", "The source reference does not belong to the exact source owner.");
            }
            sourceBytes = await payloadStore.ReadAsync(
                input.SourceReference,
                input.TenantId,
                input.SourceReference.MemoryUnitId,
                expectedKind,
                CancellationToken.None).ConfigureAwait(false);
        }

        await store.SaveSourceArtifactAsync(
            new DurableDerivedStoreSourceArtifact(
                input.TenantId,
                input.MemoryUnitId,
                input.CaseId,
                input.SourceUri,
                input.SourceType,
                input.ContentType,
                sourceBytes,
                input.EmbeddingProvider,
                input.EmbeddingModel,
                input.EmbeddingDimensions,
                input.Metadata,
                input.IngestedBy,
                input.CausationId,
                input.CorrelationId,
                input.GenerationConfigurationJson,
                input.CapturedAtUtc),
            CancellationToken.None).ConfigureAwait(false);
        return true;
    }
}
