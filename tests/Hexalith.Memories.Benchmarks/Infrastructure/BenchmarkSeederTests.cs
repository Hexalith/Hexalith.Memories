// <copyright file="BenchmarkSeederTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks.Infrastructure;

using Hexalith.Memories.Benchmarks.Models;
using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Unit tests covering benchmark seeder preconditions and deterministic seed metadata.</summary>
public class BenchmarkSeederTests
{
    [Fact]
    public void GetSharedCaseId_EmptyCorpus_ShouldThrowInvalidOperationException()
    {
        BenchmarkCorpus corpus = new([], []);

        Should.Throw<InvalidOperationException>(() => BenchmarkSeeder.GetSharedCaseId(corpus))
            .Message.ShouldContain("at least one memory unit");
    }

    [Fact]
    public void GetSharedCaseId_MultipleCaseIds_ShouldThrowInvalidOperationException()
    {
        BenchmarkCorpus corpus = new(
            [
                CreateMemoryUnit("mu-1", "case-a"),
                CreateMemoryUnit("mu-2", "case-b"),
            ],
            []);

        Should.Throw<InvalidOperationException>(() => BenchmarkSeeder.GetSharedCaseId(corpus))
            .Message.ShouldContain("single CaseId");
    }

    [Fact]
    public void GetSharedCaseId_UniformCaseIds_ShouldReturnCaseId()
    {
        BenchmarkCorpus corpus = new(
            [
                CreateMemoryUnit("mu-1", "case-incident-march"),
                CreateMemoryUnit("mu-2", "case-incident-march"),
            ],
            []);

        string caseId = BenchmarkSeeder.GetSharedCaseId(corpus);

        caseId.ShouldBe("case-incident-march");
    }

    [Fact]
    public void BenchmarkSeedTimestamp_ShouldBeDeterministic()
    {
        BenchmarkSeeder.BenchmarkSeedTimestamp.ShouldBe(new DateTimeOffset(2026, 3, 15, 8, 0, 0, TimeSpan.Zero));
    }

    private static BenchmarkMemoryUnit CreateMemoryUnit(string id, string caseId)
        => new(
            id,
            "Synthetic content",
            $"file:///{id}.md",
            SourceType.File,
            "benchmark-tenant",
            caseId,
            [1.0f]);
}
