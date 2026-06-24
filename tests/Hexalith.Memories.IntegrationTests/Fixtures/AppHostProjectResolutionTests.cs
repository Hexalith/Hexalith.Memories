// <copyright file="AppHostProjectResolutionTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using Aspire.Hosting;

using Shouldly;

/// <summary>
/// Story 18.1 / MEM-1 — compile-time guard that the consumer-facing AppHost project symbols
/// <c>Projects.Hexalith_Memories_Server</c> and <c>Projects.Hexalith_Memories_Mcp</c> keep resolving for
/// downstream Aspire AppHosts. The mere act of naming these generated <see cref="IProjectMetadata"/> types
/// makes this assembly fail to compile if either symbol stops resolving (e.g. a project rename or a dropped
/// AppHost <c>&lt;ProjectReference&gt;</c>) — that compile failure IS the guard. This test intentionally
/// avoids Docker: it is a plain <c>[Fact]</c> in the default (no-container) test lane and does NOT use
/// <c>DistributedApplicationTestingBuilder</c> or any Testcontainers fixture.
/// </summary>
public sealed class AppHostProjectResolutionTests
{
    [Fact]
    public void AppHost_ServerAndMcpProjectSymbols_ResolveAtCompileTime()
    {
        // Referencing these generated types is itself the compile-time guard: the IntegrationTests
        // assembly fails to build if either symbol stops resolving. The runtime assertions below add a
        // second layer — the generated metadata still points at the expected csproj on disk.
        IProjectMetadata server = new Projects.Hexalith_Memories_Server();
        IProjectMetadata mcp = new Projects.Hexalith_Memories_Mcp();

        server.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        server.ProjectPath.ShouldEndWith("Hexalith.Memories.Server.csproj");

        mcp.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        mcp.ProjectPath.ShouldEndWith("Hexalith.Memories.Mcp.csproj");

        // The generated symbol *shape* is itself contract: Aspire derives the type name by replacing each
        // '.' in the project name with '_' and emits it into the `Projects` namespace, so a project rename
        // silently changes the symbol downstream AppHosts must name (see public-surface-stability.md). Guard
        // the exact derived shape so a rename can't quietly produce a different — but still resolving — symbol.
        server.GetType().Namespace.ShouldBe("Projects");
        server.GetType().Name.ShouldBe("Hexalith_Memories_Server");

        mcp.GetType().Namespace.ShouldBe("Projects");
        mcp.GetType().Name.ShouldBe("Hexalith_Memories_Mcp");
    }
}
