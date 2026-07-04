// <copyright file="IndexSchemaLiteralGuardTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using System.IO;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>
/// Story 21.4 guard: production memory/index key literals must live in
/// IndexSchemaDefinitions so schema renames cannot silently orphan callers.
/// </summary>
public static partial class IndexSchemaLiteralGuardTests
{
    private static readonly string[] ForbiddenFragments =
    [
        ":mu:",
        ":vec:",
        ":vecnl:",
        ":memories:idx",
        ":memories:vec",
        ":memories:vec:nl",
    ];

    [Fact]
    public static void ProductionCode_UsesIndexSchemaDefinitionsForMemoryAndIndexKeyLiterals()
    {
        string repoRoot = ResolveRepoRoot();
        string schemaFile = Path.Combine(
            repoRoot,
            "src",
            "Hexalith.Memories.Server",
            "Infrastructure",
            "IndexSchemaDefinitions.cs");

        List<string> violations = [];
        foreach (string file in Directory.EnumerateFiles(Path.Combine(repoRoot, "src"), "*.cs", SearchOption.AllDirectories))
        {
            if (Path.GetFullPath(file).Equals(schemaFile, StringComparison.Ordinal))
            {
                continue;
            }

            string relativePath = Path.GetRelativePath(repoRoot, file);
            foreach (Match match in CSharpStringLiteralRegex().Matches(File.ReadAllText(file)))
            {
                string literal = match.Value;
                if (ForbiddenFragments.Any(fragment => literal.Contains(fragment, StringComparison.Ordinal)))
                {
                    violations.Add($"{relativePath}: raw key/index literal {literal}");
                }
            }
        }

        violations.ShouldBeEmpty(
            "Raw memory/index key fragments must be centralized in IndexSchemaDefinitions; use Build*Key, Get*IndexName, or named legacy migration helpers.");
    }

    [Theory]
    [InlineData("\"tenant-a:mu:mu-1\"")]
    [InlineData("$\"{tenantId}:vec:{memoryUnitId}\"")]
    [InlineData("@\"tenant-a:vecnl:mu-1\"")]
    [InlineData("$@\"{tenantId}:memories:vec\"")]
    [InlineData("@$\"{tenantId}:memories:idx\"")]
    [InlineData("\"\"\"tenant-a:memories:vec:nl\"\"\"")]
    public static void CSharpStringLiteralRegex_MatchesSupportedStringLiteralForms(string source)
    {
        Match match = CSharpStringLiteralRegex().Match(source);

        match.Success.ShouldBeTrue();
        match.Value.ShouldBe(source);
    }

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

    [GeneratedRegex("(?:\\$@|@\\$|\\$|@)?(?:\"\"\"[\\s\\S]*?\"\"\"|\"(?:\\\\.|[^\"])*\")", RegexOptions.CultureInvariant)]
    private static partial Regex CSharpStringLiteralRegex();
}
