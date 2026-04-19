// <copyright file="BackendCapabilityCatalogTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.HealthChecks;

using Hexalith.Memories.ServiceDefaults.Health;

using Shouldly;

/// <summary>
/// Story 8.1 Task 3.5 — pins the <see cref="BackendCapabilityCatalog"/> against
/// the set of health-check names wired in <see cref="Hexalith.Memories.Server.Program"/>
/// so that adding a backend check without a capability entry (or removing a mapping
/// without unregistering the check) fails the build's test pass loudly.
/// </summary>
public class BackendCapabilityCatalogTests
{
    private static readonly string[] RegisteredCheckNames =
    [
        "dapr-sidecar",
        "dapr-statestore",
        "redisearch",
        "redis-vector",
        "falkordb",
    ];

    [Fact]
    public void Map_ContainsEveryRegisteredCheckName()
    {
        foreach (string name in RegisteredCheckNames)
        {
            BackendCapabilityCatalog.Map.ShouldContainKey(name);
            BackendCapabilityCatalog.Map[name].ShouldNotBeEmpty(
                $"Check '{name}' must map to at least one affected capability.");
        }
    }

    [Fact]
    public void Map_ContainsNoUnexpectedKeys()
    {
        foreach (string key in BackendCapabilityCatalog.Map.Keys)
        {
            RegisteredCheckNames.ShouldContain(
                key,
                $"Catalog key '{key}' has no matching Program.cs registration.");
        }
    }

    [Theory]
    [InlineData("redisearch", "syntactic-search")]
    [InlineData("redis-vector", "semantic-search")]
    [InlineData("falkordb", "graph-traversal")]
    [InlineData("dapr-sidecar", "workflow-orchestration")]
    [InlineData("dapr-statestore", "workflow-state-persistence")]
    public void GetCapabilities_ReturnsExpectedCapability(string checkName, string expected)
    {
        BackendCapabilityCatalog.GetCapabilities(checkName).ShouldContain(expected);
    }

    [Fact]
    public void GetCapabilities_UnknownName_ReturnsEmpty()
    {
        BackendCapabilityCatalog.GetCapabilities("no-such-check").ShouldBeEmpty();
    }

    [Fact]
    public void GetCapabilities_NullName_Throws()
    {
        Should.Throw<ArgumentNullException>(() => BackendCapabilityCatalog.GetCapabilities(null!));
    }
}
