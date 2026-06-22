// <copyright file="McpCompositionRootTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using Hexalith.Memories.Mcp;

using Shouldly;

/// <summary>Disables xUnit cross-collection parallelism for env-var-mutating tests.</summary>
[CollectionDefinition("EnvironmentVariableSerialized", DisableParallelization = true)]
public sealed class EnvironmentVariableSerializedCollection
{
}

/// <summary>
/// Tests for MCP service composition decisions that affect DAPR service invocation.
/// </summary>
[Collection("EnvironmentVariableSerialized")]
public sealed class McpCompositionRootTests
{
    [Fact]
    public void ResolveMemoriesServerAppId_WhenUnset_ReturnsDefault()
    {
        using var scope = new EnvScope((McpCompositionRoot.MemoriesServerAppIdEnvVar, null));

        string appId = McpCompositionRoot.ResolveMemoriesServerAppId();

        appId.ShouldBe(McpCompositionRoot.MemoriesServerAppId);
    }

    [Fact]
    public void ResolveMemoriesServerAppId_WhenConfigured_TrimsValue()
    {
        using var scope = new EnvScope((McpCompositionRoot.MemoriesServerAppIdEnvVar, "  memories-it-123  "));

        string appId = McpCompositionRoot.ResolveMemoriesServerAppId();

        appId.ShouldBe("memories-it-123");
    }

    private sealed class EnvScope : IDisposable
    {
        private readonly Dictionary<string, string?> _previous = new(StringComparer.Ordinal);

        public EnvScope(params (string Key, string? Value)[] values)
        {
            foreach ((string key, string? value) in values)
            {
                _previous[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        public void Dispose()
        {
            foreach ((string key, string? prior) in _previous)
            {
                Environment.SetEnvironmentVariable(key, prior);
            }
        }
    }
}
