// <copyright file="SyntacticHashProjectionTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Import;

using System;
using System.Collections.Generic;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Cases;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 26.2 (AC2) — proves the shared syntactic-hash projection used by both ingest and restore round-trips
/// through the canonical <c>CaseService.ParseMemoryUnitFromHash</c> read path to an equal <see cref="MemoryUnit"/>.
/// </summary>
public class SyntacticHashProjectionTests
{
    [Fact]
    public void BuildEntries_RoundTripsThroughParseMemoryUnitFromHash()
    {
        DateTimeOffset ingestedAt = new(2026, 7, 1, 8, 30, 15, TimeSpan.Zero);
        DateTimeOffset lastUpdated = new(2026, 7, 2, 9, 45, 0, TimeSpan.Zero);
        Dictionary<string, MetadataField> metadata = new(StringComparer.Ordinal)
        {
            ["topic"] = new MetadataField("legal", MetadataOrigin.Human, 0.9f),
        };

        List<HashEntry> entries = SyntacticHashProjection.BuildEntries(
            "mu-1",
            "acme",
            "hello world",
            "file:///a.txt",
            SourceType.File,
            metadata,
            "content-hash-1",
            "case-1",
            "google:text-embedding-004",
            "text-embedding-004",
            "tester",
            ingestedAt,
            lastUpdated);

        MemoryUnit? parsed = CaseService.ParseMemoryUnitFromHash([.. entries], "acme", "mu-1");

        parsed.ShouldNotBeNull();
        parsed.Id.ShouldBe("mu-1");
        parsed.TenantId.ShouldBe("acme");
        parsed.CaseId.ShouldBe("case-1");
        parsed.Content.ShouldBe("hello world");
        parsed.ContentHash.ShouldBe("content-hash-1");
        parsed.SourceUri.ShouldBe("file:///a.txt");
        parsed.SourceType.ShouldBe(SourceType.File);
        parsed.IngestedBy.ShouldBe("tester");
        parsed.IngestedAt.ShouldBe(ingestedAt);
        parsed.LastUpdated.ShouldBe(lastUpdated);
        parsed.EmbeddingProvider.ShouldBe("google:text-embedding-004");
        parsed.EmbeddingModel.ShouldBe("text-embedding-004");
        parsed.Metadata.ShouldContainKey("topic");
        parsed.Metadata["topic"].Value.ShouldBe("legal");
        parsed.Metadata["topic"].Origin.ShouldBe(MetadataOrigin.Human);
    }

    [Fact]
    public void BuildEntries_WritesTheDocumentedFieldSet()
    {
        List<HashEntry> entries = SyntacticHashProjection.BuildEntries(
            "mu-1",
            "acme",
            "hello",
            "file:///a.txt",
            SourceType.File,
            new Dictionary<string, MetadataField>(StringComparer.Ordinal),
            "h1",
            "case-1",
            "google:text-embedding-004",
            "text-embedding-004",
            "tester",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch);

        HashSet<string> fieldNames = [.. entries.ConvertAll(static e => e.Name.ToString())];

        // The exact field contract shared with IndexSyntacticActivity (must satisfy ParseMemoryUnitFromHash).
        foreach (string expected in new[]
        {
            "id", "tenantId", "content", "sourceUri", "sourceUriText", "sourceType", "sourceTypeText",
            "metadataText", "attributeTags", "metadataJson", "contentHash", "caseId", "embeddingProvider",
            "embeddingModel", "ingestedBy", "ingestedAt", "lastUpdated",
        })
        {
            fieldNames.ShouldContain(expected);
        }
    }
}
