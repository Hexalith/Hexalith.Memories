// <copyright file="MemoryUnitFormatterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Text.Json;

using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Contracts.V1;

using Shouldly;

public sealed class MemoryUnitFormatterTests
{
    [Fact]
    public void Human_MixedOrigins_PrintsLowercaseOriginPrefixes()
    {
        MemoryUnit unit = BuildUnit(includeMetadata: true);
        using var writer = new StringWriter() { NewLine = "\n" };

        new MemoryUnitHumanFormatter().Write(unit, writer);
        string output = writer.ToString();

        // Origin prefix starts at '[' and is followed by ', confidence=...' (Task 7.4 format).
        output.ShouldContain("[human, confidence=", Shouldly.Case.Sensitive);
        output.ShouldContain("[ai, confidence=", Shouldly.Case.Sensitive);
        output.ShouldNotContain("[Human", Shouldly.Case.Sensitive);
        output.ShouldNotContain("[Ai,", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void Human_EmptyMetadata_PrintsMetadataNone()
    {
        MemoryUnit unit = BuildUnit(includeMetadata: false);
        using var writer = new StringWriter() { NewLine = "\n" };

        new MemoryUnitHumanFormatter().Write(unit, writer);
        writer.ToString().ShouldContain("metadata: (none)");
    }

    [Fact]
    public void Human_IngestedAt_UsesIsoRoundTripFormat()
    {
        MemoryUnit unit = BuildUnit(includeMetadata: true) with
        {
            IngestedAt = new DateTimeOffset(2026, 4, 16, 15, 30, 0, TimeSpan.Zero),
        };
        using var writer = new StringWriter() { NewLine = "\n" };

        new MemoryUnitHumanFormatter().Write(unit, writer);

        writer.ToString().ShouldContain("ingestedAt=2026-04-16T15:30:00.0000000+00:00");
    }

    [Fact]
    public void Json_EmitsEnvelopeWithCamelCaseOriginValues()
    {
        MemoryUnit unit = BuildUnit(includeMetadata: true);
        using var writer = new StringWriter() { NewLine = "\n" };

        new MemoryUnitJsonFormatter().Write(unit, writer);
        using JsonDocument doc = JsonDocument.Parse(writer.ToString());

        doc.RootElement.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("command").GetString().ShouldBe("search inspect");
        JsonElement metadata = doc.RootElement.GetProperty("data").GetProperty("metadata");
        metadata.GetProperty("author").GetProperty("origin").GetString().ShouldBe("human");
        metadata.GetProperty("topic").GetProperty("origin").GetString().ShouldBe("ai");
    }

    private static MemoryUnit BuildUnit(bool includeMetadata)
        => new()
        {
            Id = "mu-1",
            TenantId = "acme",
            CaseId = "case-1",
            Content = "content body",
            ContentHash = "hash",
            SourceUri = "mem://case/mu-1",
            SourceType = SourceType.File,
            IngestedBy = "user@acme",
            IngestedAt = new DateTimeOffset(2026, 4, 16, 15, 30, 0, TimeSpan.Zero),
            LastUpdated = new DateTimeOffset(2026, 4, 16, 16, 0, 0, TimeSpan.Zero),
            Status = MemoryUnitStatus.Indexed,
            Metadata = includeMetadata
                ? new Dictionary<string, MetadataField>
                {
                    ["author"] = new("Jerome", MetadataOrigin.Human, 1.0f),
                    ["topic"] = new("compliance", MetadataOrigin.Ai, 0.87f),
                }
                : [],
        };
}
