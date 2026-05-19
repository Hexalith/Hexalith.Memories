// <copyright file="DaprComponentTemplateTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using System.IO;

using Shouldly;

/// <summary>Story 15.6 regression guards for production DAPR component templates.</summary>
public sealed class DaprComponentTemplateTests
{
    [Fact]
    public void StateStoreTemplate_UsesEnvironmentInterpolatedRedisPassword()
    {
        string content = NormalizeLineEndings(File.ReadAllText(LocateRepoFile(Path.Combine(
            "deploy",
            "dapr",
            "components",
            "statestore.yaml"))));

        content.ShouldStartWith("# Production deployment template.");
        content.ShouldContain("${STATESTORE_REDIS_PASSWORD:-}", Case.Sensitive);
        content.ShouldNotContain("redisPassword\n      value: \"\"", Case.Sensitive);
    }

    [Fact]
    public void SecretStoreTemplate_UsesAbsoluteSecretsPathAndDocumentsMount()
    {
        string content = NormalizeLineEndings(File.ReadAllText(LocateRepoFile(Path.Combine(
            "deploy",
            "dapr",
            "components",
            "secretstore.yaml"))));

        content.ShouldContain("/etc/dapr/secrets/secrets.json", Case.Sensitive);
        content.ShouldContain("volume mount", Case.Insensitive);
        content.ShouldNotContain("./secrets.json", Case.Sensitive);
    }

    [Fact]
    public void ConversationTemplate_UsesDaprSeventeenDocumentedCacheKey()
    {
        string content = NormalizeLineEndings(File.ReadAllText(LocateRepoFile(Path.Combine(
            "deploy",
            "dapr",
            "components",
            "conversation-llm.yaml"))));

        content.ShouldContain("https://docs.dapr.io/reference/components-reference/supported-conversation/openai/", Case.Sensitive);
        content.ShouldContain("responseCacheTTL", Case.Sensitive);

        // Bare `cacheTTL` (the legacy alias) must not appear as a metadata key. Match the YAML key
        // form `- name: cacheTTL` so the URL fragment `cacheTTL` inside the cited docs link does not
        // false-positive the assertion. The `\n` form of the assertion is safe after
        // NormalizeLineEndings because all `\r\n` sequences have been collapsed.
        content.ShouldNotContain("- name: cacheTTL\n", Case.Sensitive);
    }

    // Story 15.6 code review patch: normalize CRLF (Windows checkouts with core.autocrlf=true) to LF
    // so `ShouldNotContain("…\n…")` assertions cannot pass vacuously when the on-disk file has `\r\n`
    // endings. The previous direct ReadAllText would let a regression slip through on Windows CI/local.
    private static string NormalizeLineEndings(string content) => content.Replace("\r\n", "\n");

    private static string LocateRepoFile(string relativePath)
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, relativePath);
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate '{relativePath}' by walking up from '{AppContext.BaseDirectory}'.");
    }
}
