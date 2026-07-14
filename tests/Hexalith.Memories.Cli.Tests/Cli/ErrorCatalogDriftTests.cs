// <copyright file="ErrorCatalogDriftTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Reflection;
using System.Text.RegularExpressions;

using Hexalith.Memories.Cli.Errors;

using Shouldly;

/// <summary>
/// Story 7.3 Task 7.2 — CI guard that fires when the server introduces a new
/// <c>ErrorResponse.Code</c> literal without a corresponding entry in <see cref="ErrorMessageCatalog"/>
/// or in <see cref="KnownUnmappedCodes"/>. The allow-list is a temporary concurrent-PR escape hatch,
/// not a steady-state design: it should normally stay empty on <c>main</c>, and any current server
/// code should be promoted into <see cref="ErrorMessageCatalog"/> immediately. The sanity guard
/// prevents the false-green failure mode where the grep silently returns zero.
/// </summary>
public sealed class ErrorCatalogDriftTests
{
    /// <summary>
    /// Temporary escape hatch for codes that land on <c>main</c> from another PR while the catalog fix
    /// is still in flight. This set should remain empty in the normal shipped state.
    /// <para>
    /// Concurrent-PR recovery (Revision 0.4): if the drift test fails on your PR for a code you did
    /// not add, add the code here with a TODO naming the owning story to unblock your PR; the owning
    /// story moves the entry into the catalog proper when they land.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> KnownUnmappedCodes = new(StringComparer.Ordinal);

    [Fact]
    public void Catalog_CoversEveryCurrentServerErrorCode()
    {
        string repoRoot = LocateRepoRoot();
        string serverDir = Path.Combine(repoRoot, "src", "Hexalith.Memories.Server");

        if (!Directory.Exists(serverDir))
        {
            // Defensive: the test must not false-green on a missing directory.
            throw new Xunit.Sdk.XunitException(
                $"Drift test could not locate server source directory: {serverDir}. Verify the RepoRoot AssemblyMetadata resolves to the repo root.");
        }

        IEnumerable<string> serverFiles = Directory
            .EnumerateFiles(serverDir, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        Regex[] codePatterns =
        [
            new Regex("ErrorResponse\\(\\s*\"([A-Z][A-Z0-9_]*)\"", RegexOptions.Compiled),
            // Catch filters that deliberately propagate an ImportEnvelopeException code into ErrorResponse(ex.Code).
            // Without this pattern, RESTORE_TARGET_NOT_CLEAN bypassed the literal-only drift guard.
            new Regex("string\\.Equals\\(ex\\.Code,\\s*\"([A-Z][A-Z0-9_]*)\"", RegexOptions.Compiled),
        ];
        var foundCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (string file in serverFiles)
        {
            string content = File.ReadAllText(file);
            foreach (Regex codePattern in codePatterns)
            {
                foreach (Match match in codePattern.Matches(content))
                {
                    foundCodes.Add(match.Groups[1].Value);
                }
            }
        }

        // Sanity guard: the grep must find at least 30 codes — if it returns fewer, the search scope
        // is almost certainly mis-resolved and we are in the false-green failure mode.
        foundCodes.Count.ShouldBeGreaterThanOrEqualTo(
            30,
            "Drift test located fewer than 30 ErrorResponse literals — verify RepoRoot AssemblyMetadata resolves to the repository root, not a build output directory.");

        // Codes emitted by the server that are neither in the catalog nor in the temporary allow-list are drift.
        string[] missing = foundCodes
            .Where(code => !ErrorMessageCatalog.Translations.ContainsKey(code))
            .Where(code => !KnownUnmappedCodes.Contains(code))
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();

        missing.Length.ShouldBe(
            0,
            $"Server emits these codes that neither the CLI catalog nor the temporary KnownUnmappedCodes allow-list handles: {string.Join(", ", missing)}. Add current server codes to ErrorMessageCatalog immediately; use KnownUnmappedCodes only as a short-lived concurrent-PR escape hatch.");
    }

    [Fact]
    public void KnownUnmappedCodes_DoNotOverlapCatalog()
    {
        string[] overlap = KnownUnmappedCodes
            .Where(c => ErrorMessageCatalog.Translations.ContainsKey(c))
            .ToArray();

        overlap.Length.ShouldBe(
            0,
            $"These codes appear in BOTH the catalog and the allow-list (remove from allow-list): {string.Join(", ", overlap)}.");
    }

    private static string LocateRepoRoot()
    {
        string? repoRoot = typeof(ErrorCatalogDriftTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(a => a.Key == "RepoRoot")
            ?.Value;

        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            throw new Xunit.Sdk.XunitException(
                "RepoRoot AssemblyMetadata attribute missing — ensure the test csproj includes <AssemblyMetadata Include=\"RepoRoot\" Value=\"$(MSBuildThisFileDirectory)..\\..\\\" />.");
        }

        return Path.GetFullPath(repoRoot);
    }
}
