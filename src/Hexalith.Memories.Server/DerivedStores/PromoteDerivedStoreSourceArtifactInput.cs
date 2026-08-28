// <copyright file="PromoteDerivedStoreSourceArtifactInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.DerivedStores;

using Hexalith.Memories.Contracts.V1;

/// <summary>Carries source/config evidence from ingestion into durable artifact promotion.</summary>
internal sealed record PromoteDerivedStoreSourceArtifactInput(
    string TenantId,
    string MemoryUnitId,
    string CaseId,
    string SourceUri,
    SourceType SourceType,
    string ContentType,
    byte[] SourceBytes,
    WorkflowPayloadReference? SourceReference,
    string EmbeddingProvider,
    string EmbeddingModel,
    int EmbeddingDimensions,
    Dictionary<string, MetadataField> Metadata,
    string IngestedBy,
    string? CausationId,
    string? CorrelationId,
    string GenerationConfigurationJson,
    DateTimeOffset CapturedAtUtc);
