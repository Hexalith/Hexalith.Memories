// <copyright file="ConsistencyVerificationResultFormatterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Tests for consistency verification output formatters.</summary>
public sealed class ConsistencyVerificationResultFormatterTests
{
    [Fact]
    public void HumanFormatter_NoteOnlyUnits_UsesUntruncatedTotal()
    {
        ConsistencyVerificationResult result = new(
            "tenant-1",
            TotalUnits: 12_000,
            ConsistentCount: 12_000,
            InconsistentCount: 0,
            Discrepancies: [],
            TotalDiscrepancyCount: 0,
            TruncatedAt: DateTimeOffset.UtcNow,
            EnumerationTruncated: false,
            StartedAt: DateTimeOffset.UtcNow.AddSeconds(-1),
            CompletedAt: DateTimeOffset.UtcNow,
            Duration: TimeSpan.FromSeconds(1))
        {
            NoteCount = 10_000,
            TotalNoteCount = 12_000,
            Notes = Enumerable.Range(0, 10_000)
                .Select(i => new ConsistencyDiscrepancy(
                    $"memory-{i}",
                    SyntacticPresent: true,
                    SemanticPresent: true,
                    GraphPresent: true,
                    ConsistencyRepairRecommendation.NoOp)
                {
                    ConsistencyNoteKind = ConsistencyNoteKind.NaturalLanguageEmbeddingMissing,
                })
                .ToArray(),
        };
        using StringWriter writer = new();

        new ConsistencyVerificationResultHumanFormatter().Write(result, writer);

        string output = writer.ToString();
        output.ShouldContain("note-only units:     12000");
        output.ShouldContain("note records:        10000");
    }
}
