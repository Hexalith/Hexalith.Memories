// <copyright file="MemoriesMcpDaprInvocationHandlerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using Hexalith.Memories.Mcp;

using Shouldly;

public sealed class MemoriesMcpDaprInvocationHandlerTests
{
    [Fact]
    public void ApplyDaprApiToken_AddsHeader_WhenTokenModeEnabledAndTokenSet()
    {
        using var scope = new EnvScope(
            (MemoriesMcpDaprInvocationHandler.TokenModeEnvVar, "enabled"),
            (MemoriesMcpDaprInvocationHandler.TokenEnvVar, "secret-token-value"));

        using var client = new HttpClient();
        MemoriesMcpDaprInvocationHandler.ApplyDaprApiToken(client);

        client.DefaultRequestHeaders.TryGetValues(
            MemoriesMcpDaprInvocationHandler.DaprApiTokenHeader,
            out IEnumerable<string>? values).ShouldBeTrue();
        values!.ShouldContain("secret-token-value");
    }

    [Fact]
    public void ApplyDaprApiToken_OmitsHeader_WhenTokenModeUnset()
    {
        using var scope = new EnvScope(
            (MemoriesMcpDaprInvocationHandler.TokenModeEnvVar, null),
            (MemoriesMcpDaprInvocationHandler.TokenEnvVar, "secret-token-value"));

        using var client = new HttpClient();
        MemoriesMcpDaprInvocationHandler.ApplyDaprApiToken(client);

        client.DefaultRequestHeaders.Contains(MemoriesMcpDaprInvocationHandler.DaprApiTokenHeader)
            .ShouldBeFalse();
    }

    [Fact]
    public void ApplyDaprApiToken_OmitsHeader_WhenTokenModeEnabledButTokenMissing()
    {
        using var scope = new EnvScope(
            (MemoriesMcpDaprInvocationHandler.TokenModeEnvVar, "enabled"),
            (MemoriesMcpDaprInvocationHandler.TokenEnvVar, null));

        using var client = new HttpClient();
        MemoriesMcpDaprInvocationHandler.ApplyDaprApiToken(client);

        client.DefaultRequestHeaders.Contains(MemoriesMcpDaprInvocationHandler.DaprApiTokenHeader)
            .ShouldBeFalse();
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
