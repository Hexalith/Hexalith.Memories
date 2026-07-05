// <copyright file="IndexingHotPathGuardTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using System.IO;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>
/// Story 23.7 (A34) guards: the ingestion index hot path must not reintroduce the audited anti-patterns —
/// blocking <c>Thread.Sleep</c> readiness retries (AC4) or per-document <c>FT.CREATE</c> / "index already exists"
/// warning noise (AC1/AC5). Index creation is owned by <c>TenantProvisioningWorkflow</c>.
/// </summary>
public static partial class IndexingHotPathGuardTests
{
    private static readonly string[] IndexWriteHotPathFiles =
    [
        Path.Combine("src", "Hexalith.Memories.Server", "Activities", "Indexing", "IndexSyntacticActivity.cs"),
        Path.Combine("src", "Hexalith.Memories.Server", "Activities", "Indexing", "IndexSemanticActivity.cs"),
        Path.Combine("src", "Hexalith.Memories.Server", "Activities", "Indexing", "IndexSemanticChunksActivity.cs"),
        Path.Combine("src", "Hexalith.Memories.Server", "Activities", "Indexing", "IndexNaturalLanguageSemanticActivity.cs"),
        Path.Combine("src", "Hexalith.Memories.Server", "EventStoreIntegration", "RedisSearchIndexMaintenanceAdapter.cs"),
    ];

    [Fact]
    public static void IndexingActivities_DoNotUseBlockingThreadSleep()
    {
        string repoRoot = ResolveRepoRoot();
        string indexingDir = Path.Combine(repoRoot, "src", "Hexalith.Memories.Server", "Activities", "Indexing");

        List<string> violations = [];
        foreach (string file in Directory.EnumerateFiles(indexingDir, "*.cs", SearchOption.AllDirectories))
        {
            if (StripComments(File.ReadAllText(file)).Contains("Thread.Sleep(", StringComparison.Ordinal))
            {
                violations.Add(Path.GetRelativePath(repoRoot, file));
            }
        }

        violations.ShouldBeEmpty(
            "Indexing readiness must use asynchronous Task.Delay, never a blocking Thread.Sleep (Story 23.7 AC4).");
    }

    [Fact]
    public static void IndexWriteHotPaths_DoNotCreateIndexesOnDemand()
    {
        string repoRoot = ResolveRepoRoot();

        List<string> violations = [];
        foreach (string relativeFile in IndexWriteHotPathFiles)
        {
            string source = StripComments(File.ReadAllText(Path.Combine(repoRoot, relativeFile)));
            if (source.Contains(".Create(", StringComparison.Ordinal) && source.Contains(".FT()", StringComparison.Ordinal))
            {
                violations.Add($"{relativeFile}: issues an FT.CREATE on the ingestion path");
            }

            if (source.Contains("Index already exists", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"{relativeFile}: retains an 'index already exists' warning path");
            }
        }

        violations.ShouldBeEmpty(
            "Index write hot paths must verify readiness via ITenantIndexReadinessVerifier, not create indexes per document (Story 23.7 AC1/AC5).");
    }

    private static string StripComments(string source)
        => LineCommentRegex().Replace(source, string.Empty);

    private static string ResolveRepoRoot()
    {
        string candidate = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        return AppContext.BaseDirectory;
    }

    [GeneratedRegex("//[^\\n]*", RegexOptions.CultureInvariant)]
    private static partial Regex LineCommentRegex();
}
