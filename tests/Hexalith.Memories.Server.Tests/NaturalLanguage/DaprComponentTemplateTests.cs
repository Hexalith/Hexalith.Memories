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
    public void StateStoreTemplate_UsesKubernetesSecretAndActorStore()
    {
        string content = NormalizeLineEndings(File.ReadAllText(LocateRepoFile(Path.Combine(
            "deploy",
            "dapr",
            "components",
            "statestore.yaml"))));

        content.ShouldContain("name: redisPassword", Case.Sensitive);
        content.ShouldContain("secretKeyRef:", Case.Sensitive);
        content.ShouldContain("name: redis-secret", Case.Sensitive);
        content.ShouldContain("key: password", Case.Sensitive);
        content.ShouldContain("name: actorStateStore", Case.Sensitive);
        content.ShouldContain("value: \"true\"", Case.Sensitive);
        content.ShouldContain("secretStore: secretstore", Case.Sensitive);
        content.ShouldNotContain("${STATESTORE_REDIS_PASSWORD:-}", Case.Sensitive);
    }

    [Fact]
    public void SecretStoreTemplate_UsesKubernetesStoreScopedToRuntimeApps()
    {
        string content = NormalizeLineEndings(File.ReadAllText(LocateRepoFile(Path.Combine(
            "deploy",
            "dapr",
            "components",
            "secretstore.yaml"))));

        content.ShouldContain("type: secretstores.kubernetes", Case.Sensitive);
        content.ShouldContain("scopes:", Case.Sensitive);
        content.ShouldContain("- eventstore", Case.Sensitive);
        content.ShouldContain("- memories", Case.Sensitive);
        content.ShouldNotContain("secretstores.local.file", Case.Sensitive);
        content.ShouldNotContain("secretsFile", Case.Sensitive);
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
