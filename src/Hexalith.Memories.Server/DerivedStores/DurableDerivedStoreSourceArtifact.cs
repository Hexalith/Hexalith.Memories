// <copyright file="DurableDerivedStoreSourceArtifact.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.DerivedStores;

using Hexalith.Memories.Contracts.V1;

/// <summary>Durable exact source and resolved generation evidence retained for a MemoryUnit lifetime.</summary>
internal sealed record DurableDerivedStoreSourceArtifact(
    string TenantId,
    string MemoryUnitId,
    string CaseId,
    string SourceUri,
    SourceType SourceType,
    string ContentType,
    byte[] SourceBytes,
    string EmbeddingProvider,
    string EmbeddingModel,
    int EmbeddingDimensions,
    Dictionary<string, MetadataField> Metadata,
    string IngestedBy,
    string? CausationId,
    string? CorrelationId,
    string GenerationConfigurationJson,
    DateTimeOffset CapturedAtUtc);
