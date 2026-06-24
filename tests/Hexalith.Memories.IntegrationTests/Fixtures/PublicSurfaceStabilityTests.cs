// <copyright file="PublicSurfaceStabilityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using Hexalith.Memories.Mcp.Authentication;
using Hexalith.Memories.Server.Graph;

using Shouldly;

/// <summary>
/// Story 18.1 / MEM-1 — runtime guard for the assembly-name and root-namespace half of the public-surface
/// stability contract (<c>docs/dev/public-surface-stability.md</c>). Downstream Aspire AppHosts depend on
/// <c>Hexalith.Memories.Server</c> and <c>Hexalith.Memories.Mcp</c> keeping their default assembly names and
/// root namespaces — neither csproj overrides <c>&lt;AssemblyName&gt;</c> / <c>&lt;RootNamespace&gt;</c>, so
/// both default to the project (csproj base) name. These tests reflect over a stable public type from each
/// assembly and fail if a project rename or an identity override drifts the assembly name or root namespace.
/// Like <see cref="AppHostProjectResolutionTests"/> this stays in the default (no-Docker) lane: plain
/// <c>[Fact]</c>s, no fixture, no <c>DistributedApplicationTestingBuilder</c>. The PackageId half of the
/// contract is not reflectable from a built assembly and remains review-enforced per the contract doc.
/// </summary>
public sealed class PublicSurfaceStabilityTests
{
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
}
