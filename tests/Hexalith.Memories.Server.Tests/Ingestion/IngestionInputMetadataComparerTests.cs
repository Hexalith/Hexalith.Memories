// <copyright file="IngestionInputMetadataComparerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Story 9.2 — decision D6 (committed-branch review 2026-04-24). Pin
/// <see cref="StringComparer.Ordinal"/> on the metadata dictionaries so CloudEvent keys
/// (<c>cloudevent.type</c>, <c>event.aggregateType</c>, etc.) that the workflow reads back
/// match exactly what the <c>CloudEventToIngestionInputMapper</c> wrote — independent of which
/// code path constructed the dictionary (auto-backed default, init-assigned, or deserialized
/// replay payload).</summary>
public class IngestionInputMetadataComparerTests
{
    [Fact]
    public void IngestionInput_DefaultMetadata_UsesOrdinalComparer()
    {
        IngestionInput input = new()
        {
            TenantId = "t",
            CaseId = "c",
            SourceUri = "src",
            ContentType = "application/json",
            SourceType = SourceType.Event,
            IngestedBy = "test",
        };

        input.Metadata.Comparer.ShouldBe(StringComparer.Ordinal);
    }

    [Fact]
    public void IngestionInput_NullMetadataInit_ProducesOrdinalDictionary()
    {
        IngestionInput input = new()
        {
            TenantId = "t",
            CaseId = "c",
            SourceUri = "src",
            ContentType = "application/json",
            SourceType = SourceType.Event,
            IngestedBy = "test",
            Metadata = null!,
        };

        input.Metadata.Comparer.ShouldBe(StringComparer.Ordinal);
        input.Metadata.ShouldBeEmpty();
    }

    [Fact]
    public void IngestionInput_CaseInsensitiveSourceMetadata_IsNormalizedToOrdinal()
    {
        // If the caller passes a case-insensitive dictionary, IngestionInput MUST NOT silently
        // accept a loose comparer — that would allow two distinct CloudEvent keys ("Cloudevent.Type"
        // vs "cloudevent.type") to collide inside the workflow. Pinning forces a rebuild with
        // StringComparer.Ordinal so downstream consumers have deterministic semantics.
        Dictionary<string, MetadataField> caseInsensitive = new(StringComparer.OrdinalIgnoreCase)
        {
            ["cloudevent.type"] = new("payment.accepted.v1", MetadataOrigin.Ai, 1.0f),
        };

        IngestionInput input = new()
        {
            TenantId = "t",
            CaseId = "c",
            SourceUri = "src",
            ContentType = "application/json",
            SourceType = SourceType.Event,
            IngestedBy = "test",
            Metadata = caseInsensitive,
        };

        input.Metadata.Comparer.ShouldBe(StringComparer.Ordinal);
        input.Metadata.ShouldContainKey("cloudevent.type");
        input.Metadata.ContainsKey("CLOUDEVENT.TYPE").ShouldBeFalse(
            "Ordinal lookups must be case-sensitive. Case-insensitive lookup would regress to the"
            + " loose comparer D6 explicitly rejects.");
    }

    [Fact]
    public void IndexInput_DefaultMetadata_UsesOrdinalComparer()
    {
        IndexInput input = new()
        {
            MemoryUnitId = "mu",
            TenantId = "t",
            CaseId = "c",
            Content = "content",
            ContentHash = "hash",
            SourceUri = "src",
            SourceType = SourceType.Event,
            IngestedBy = "test",
            IngestedAt = DateTimeOffset.UtcNow,
            EmbeddingVector = [0f],
            EmbeddingProvider = "p",
            EmbeddingModel = "m",
            EmbeddingDimensions = 1,
        };

        input.Metadata.Comparer.ShouldBe(StringComparer.Ordinal);
    }

    [Fact]
    public void IndexInput_CaseInsensitiveSourceMetadata_IsNormalizedToOrdinal()
    {
        Dictionary<string, MetadataField> caseInsensitive = new(StringComparer.OrdinalIgnoreCase)
        {
            ["cloudevent.type"] = new("payment.accepted.v1", MetadataOrigin.Ai, 1.0f),
        };

        IndexInput input = new()
        {
            MemoryUnitId = "mu",
            TenantId = "t",
            CaseId = "c",
            Content = "content",
            ContentHash = "hash",
            SourceUri = "src",
            SourceType = SourceType.Event,
            IngestedBy = "test",
            IngestedAt = DateTimeOffset.UtcNow,
            EmbeddingVector = [0f],
            EmbeddingProvider = "p",
            EmbeddingModel = "m",
            EmbeddingDimensions = 1,
            Metadata = caseInsensitive,
        };

        input.Metadata.Comparer.ShouldBe(StringComparer.Ordinal);
        input.Metadata.ShouldContainKey("cloudevent.type");
        input.Metadata.ContainsKey("CLOUDEVENT.TYPE").ShouldBeFalse();
    }
}
