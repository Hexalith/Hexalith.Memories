// <copyright file="IntegrationStubClosureGuardTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using System.IO;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>Story 26.3 source guards that prevent assertion-free integration targets from reporting success.</summary>
public static partial class IntegrationStubClosureGuardTests
{
    private static readonly string[] GenericSkipFragments =
    [
        "requires aspire fixture",
        "requires aspire apphost fixture",
        "story 6.4",
        "epic 7",
        "todo",
        "not implemented",
    ];

    [Fact]
    public static void IntegrationTests_ContainNoRunnableSkipAttributeOrNoOpPlaceholderBodies()
    {
        string repoRoot = ResolveRepoRoot();
        string integrationRoot = Path.Combine(repoRoot, "tests", "Hexalith.Memories.IntegrationTests");
        List<string> violations = [];

        foreach (string file in Directory.EnumerateFiles(integrationRoot, "*.cs", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(repoRoot, file);
            string source = File.ReadAllText(file);

            AddLineViolations(violations, relativePath, source, "RunnableSkippedFact", "uses the false-pass RunnableSkippedFact attribute/type");
            AddLineViolations(violations, relativePath, source, "_ = _fixture;", "contains the canonical fixture no-op body");
            AddLineViolations(violations, relativePath, source, "await Task.CompletedTask;", "contains the canonical completed-task no-op body");

        }

        violations.ShouldBeEmpty(
            "Integration tests must use normal facts with assertions or literal, specific xUnit skips; false-pass placeholders are forbidden.");
    }

    [Fact]
    public static void IntegrationTests_ExplicitSkipsReferenceStructuredDeferralAndEnablingCondition()
    {
        string repoRoot = ResolveRepoRoot();
        string integrationRoot = Path.Combine(repoRoot, "tests", "Hexalith.Memories.IntegrationTests");
        List<string> violations = [];

        foreach (string file in Directory.EnumerateFiles(integrationRoot, "*.cs", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(repoRoot, file);
            string source = File.ReadAllText(file);

            foreach (Match match in ExplicitSkipRegex().Matches(source))
            {
                string reason = match.Groups["reason"].Value.Trim();
                int line = GetLineNumber(source, match.Index);
                if (reason.Length < 40 || GenericSkipFragments.Any(
                    fragment => reason.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                {
                    violations.Add($"{relativePath}:{line} has a blank/generic/stale explicit skip reason");
                    continue;
                }

                if (!StructuredDeferralIdRegex().IsMatch(reason))
                {
                    violations.Add($"{relativePath}:{line} skip reason does not reference a structured deferred-work ID");
                }

                if (!reason.Contains("Owner:", StringComparison.Ordinal) ||
                    !reason.Contains("Unskip when:", StringComparison.Ordinal))
                {
                    violations.Add($"{relativePath}:{line} skip reason must name Owner and Unskip when");
                }
            }
        }

        violations.ShouldBeEmpty(
            "Literal skips must identify a structured deferred-work entry, current owner, and stable enabling condition.");
    }

    private static void AddLineViolations(
        ICollection<string> violations,
        string relativePath,
        string source,
        string fragment,
        string message)
    {
        int searchStart = 0;
        while (source.IndexOf(fragment, searchStart, StringComparison.Ordinal) is int index && index >= 0)
        {
            violations.Add($"{relativePath}:{GetLineNumber(source, index)} {message}");
            searchStart = index + fragment.Length;
        }
    }

    private static int GetLineNumber(string source, int index)
        => source.AsSpan(0, Math.Clamp(index, 0, source.Length)).Count('\n') + 1;

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

    [GeneratedRegex("""\[Fact\s*\(\s*Skip\s*=\s*"(?<reason>(?:\\.|[^"])*)"\s*\)\s*\]""", RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitSkipRegex();

    [GeneratedRegex(@"\d+\.\d+-[A-Z0-9][A-Z0-9-]+", RegexOptions.CultureInvariant)]
    private static partial Regex StructuredDeferralIdRegex();

}
