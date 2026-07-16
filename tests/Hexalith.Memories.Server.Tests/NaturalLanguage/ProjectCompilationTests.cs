// <copyright file="ProjectCompilationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using System.IO;

using Shouldly;

/// <summary>Story 9.2 Task 1.9 / Risk #1 — suppression-scoping guards. The <c>DAPR_CONVERSATION</c>
/// experimental diagnostic from <c>Dapr.AI</c> 1.17.6 must be suppressed ONLY in
/// <c>Hexalith.Memories.Server.csproj</c>. Leaking it into <c>Directory.Build.props</c> would sprawl the
/// suppression to every project, defeating the fail-fast posture that catches accidental Conversation API
/// adoption outside the server.</summary>
/// <remarks>The stronger dynamic-compilation guard (build a throwaway project that imports Server source
/// and asserts <c>csc</c> emits zero DAPR_CONVERSATION diagnostics) is documented in Task 1.9 as
/// Improvement AD; this file content check is the cheaper and still reliable form — a leak would update
/// <c>Directory.Build.props</c> and flip the first assertion immediately.</remarks>
public sealed class ProjectCompilationTests
{
    [Fact]
    public void DirectoryBuildProps_DoesNotSuppressDaprConversationWarning()
    {
        string propsPath = LocateRepoFile("Directory.Build.props");

        string content = File.ReadAllText(propsPath);

        content.ShouldNotContain(
            "DAPR_CONVERSATION",
            Case.Sensitive,
            customMessage: "Directory.Build.props must NOT suppress DAPR_CONVERSATION globally — Risk #1 " +
                "mitigation scopes the suppression to Hexalith.Memories.Server.csproj only so unintentional " +
                "spread of Dapr.AI alpha-API adoption still fails the build.");
    }

    [Fact]
    public void ServerCsproj_SuppressesDaprConversationWarning()
    {
        string csprojPath = LocateRepoFile(Path.Combine(
            "src",
            "Hexalith.Memories.Server",
            "Hexalith.Memories.Server.csproj"));

        string content = File.ReadAllText(csprojPath);

        content.ShouldContain(
            "DAPR_CONVERSATION",
            Case.Sensitive,
            customMessage: "Hexalith.Memories.Server.csproj MUST carry <NoWarn>$(NoWarn);DAPR_CONVERSATION" +
                "</NoWarn> per Story 9.2 Task 1.3 so Dapr.AI's [Experimental] diagnostic does not break " +
                "the build under TreatWarningsAsErrors=true.");
    }

    [Fact]
    public void OtherServerProjects_DoNotSuppressDaprConversationWarning()
    {
        string srcRoot = LocateRepoFile("src");

        foreach (string csproj in Directory.EnumerateFiles(srcRoot, "*.csproj", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(csproj).Equals(
                "Hexalith.Memories.Server.csproj",
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string content = File.ReadAllText(csproj);
            content.ShouldNotContain(
                "DAPR_CONVERSATION",
                Case.Sensitive,
                customMessage: $"{Path.GetFileName(csproj)} must NOT suppress DAPR_CONVERSATION — the " +
                    "suppression is scoped to Hexalith.Memories.Server.csproj only (Risk #1).");
        }
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
