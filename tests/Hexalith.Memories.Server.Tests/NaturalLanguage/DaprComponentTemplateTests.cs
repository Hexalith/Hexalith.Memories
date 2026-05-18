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
        string content = File.ReadAllText(LocateRepoFile(Path.Combine(
            "deploy",
            "dapr",
            "components",
            "statestore.yaml")));

        content.ShouldStartWith("# Production deployment template.");
        content.ShouldContain("${STATESTORE_REDIS_PASSWORD:-}", Case.Sensitive);
        content.ShouldNotContain("redisPassword\n      value: \"\"", Case.Sensitive);
    }

    [Fact]
    public void SecretStoreTemplate_UsesAbsoluteSecretsPathAndDocumentsMount()
    {
        string content = File.ReadAllText(LocateRepoFile(Path.Combine(
            "deploy",
            "dapr",
            "components",
            "secretstore.yaml")));

        content.ShouldContain("/etc/dapr/secrets/secrets.json", Case.Sensitive);
        content.ShouldContain("volume mount", Case.Insensitive);
        content.ShouldNotContain("./secrets.json", Case.Sensitive);
    }

    [Fact]
    public void ConversationTemplate_UsesDaprSeventeenDocumentedCacheKey()
    {
        string content = File.ReadAllText(LocateRepoFile(Path.Combine(
            "deploy",
            "dapr",
            "components",
            "conversation-llm.yaml")));

        content.ShouldContain("https://docs.dapr.io/reference/components-reference/supported-conversation/openai/", Case.Sensitive);
        content.ShouldContain("responseCacheTTL", Case.Sensitive);
        content.ShouldNotContain("cacheTTL\n", Case.Sensitive);
    }

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
