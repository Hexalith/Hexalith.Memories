// <copyright file="PublicSurfaceStabilityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.IO;

using Aspire.Hosting;

using Hexalith.Memories.Mcp.Authentication;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

/// <summary>
/// Story 18.1 / MEM-1 — runtime guard for the assembly-name and root-namespace half of the public-surface
/// stability contract (<c>docs/dev/public-surface-stability.md</c>). Downstream Aspire AppHosts depend on
/// <c>Hexalith.Memories.Server</c> and <c>Hexalith.Memories.Mcp</c> keeping their default assembly names and
/// root namespaces — neither csproj overrides <c>&lt;AssemblyName&gt;</c> / <c>&lt;RootNamespace&gt;</c>, so
/// both default to the project (csproj base) name. These tests reflect over a stable public type from each
/// assembly and fail if a project rename or an identity override drifts the assembly name or root namespace.
/// Like <see cref="AppHostProjectResolutionTests"/> this stays in the default (no-Docker) lane: plain
/// <c>[Fact]</c>s, no fixture, no <c>DistributedApplicationTestingBuilder</c>. Project-source checks tie the
/// Server packability and MCP PackageId cells that cannot be reflected from built assemblies.
/// </summary>
public sealed class PublicSurfaceStabilityTests
{
    private const string DocRelativePath = "docs/dev/public-surface-stability.md";

    [Fact]
    public void PublicSurfaceContract_HasExactlyTwoCompleteProjectRowsTiedToAspireSymbols()
    {
        var document = new MarkdownContractDocument(ReadDoc());
        document.GetTableHeader("Contract — consumer-facing host projects").ShouldBe(
            ["Project", "Project name", "Assembly name", "Root namespace", "PackageId", "Aspire metadata symbol"]);
        IReadOnlyList<IReadOnlyList<string>> rows = document.GetTableRows("Contract — consumer-facing host projects");

        rows.Count.ShouldBe(2);
        rows[0].ShouldBe(
        [
            "Server",
            "`Hexalith.Memories.Server`",
            "`Hexalith.Memories.Server`",
            "`Hexalith.Memories.Server`",
            "— (not packed, `IsPackable=false`)",
            "`Projects.Hexalith_Memories_Server`",
        ]);
        rows[1].ShouldBe(
        [
            "Mcp",
            "`Hexalith.Memories.Mcp`",
            "`Hexalith.Memories.Mcp`",
            "`Hexalith.Memories.Mcp`",
            "`Hexalith.Memories.Mcp`",
            "`Projects.Hexalith_Memories_Mcp`",
        ]);

        IProjectMetadata server = new Projects.Hexalith_Memories_Server();
        IProjectMetadata mcp = new Projects.Hexalith_Memories_Mcp();
        $"`{server.GetType().Namespace}.{server.GetType().Name}`".ShouldBe(rows[0][5]);
        $"`{mcp.GetType().Namespace}.{mcp.GetType().Name}`".ShouldBe(rows[1][5]);
    }

    [Fact]
    public void PublicSurfaceContract_ProjectRowsAreTiedToPackabilityAndPackageIdSources()
    {
        string serverProject = ReadRepoFile("src", "Hexalith.Memories.Server", "Hexalith.Memories.Server.csproj");
        string mcpProject = ReadRepoFile("src", "Hexalith.Memories.Mcp", "Hexalith.Memories.Mcp.csproj");

        serverProject.ShouldContain("<IsPackable>false</IsPackable>", Case.Sensitive);
        serverProject.ShouldNotContain("<AssemblyName>", Case.Sensitive);
        serverProject.ShouldNotContain("<RootNamespace>", Case.Sensitive);
        mcpProject.ShouldContain("<IsPackable>true</IsPackable>", Case.Sensitive);
        mcpProject.ShouldContain("<PackageId>Hexalith.Memories.Mcp</PackageId>", Case.Sensitive);
        mcpProject.ShouldNotContain("<AssemblyName>", Case.Sensitive);
        mcpProject.ShouldNotContain("<RootNamespace>", Case.Sensitive);
    }

    [Fact]
    public void PublicSurfaceContract_ContainsNoLeakedToolCallMarkup()
    {
        IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(ReadDoc());

        diagnostics.ShouldBeEmpty($"{DocRelativePath} contains leaked tool-call markup: {string.Join("; ", diagnostics)}");
    }

    [Fact]
    public void ServerAssembly_KeepsStableNameAndRootNamespace()
    {
        // IGraphQueryBuilder is an arbitrary stable public type used only as a handle to the
        // Hexalith.Memories.Server assembly; the assertions are about the assembly identity, not this type.
        Type anchor = typeof(IGraphQueryBuilder);

        anchor.Assembly.GetName().Name.ShouldBe("Hexalith.Memories.Server");
        anchor.Namespace.ShouldNotBeNull();
        anchor.Namespace!.ShouldStartWith("Hexalith.Memories.Server.");
    }

    [Fact]
    public void McpAssembly_KeepsStableNameAndRootNamespace()
    {
        // MemoriesMcpAuthenticationOptions is an arbitrary stable public type used only as a handle to the
        // Hexalith.Memories.Mcp assembly (its root-namespace types are internal and not visible from here).
        Type anchor = typeof(MemoriesMcpAuthenticationOptions);

        anchor.Assembly.GetName().Name.ShouldBe("Hexalith.Memories.Mcp");
        anchor.Namespace.ShouldNotBeNull();
        anchor.Namespace!.ShouldStartWith("Hexalith.Memories.Mcp.");
    }

    private static string ReadDoc() => ReadRepoFile("docs", "dev", "public-surface-stability.md");

    private static string ReadRepoFile(params string[] segments)
    {
        string path = Path.Combine([ResolveRepoRoot(), .. segments]);
        File.Exists(path).ShouldBeTrue($"Repository file not found at {path}");
        return File.ReadAllText(path);
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
}
