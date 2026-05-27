// <copyright file="EnvironmentVariableConfigurationSourceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using Hexalith.Memories.Cli.Configuration;

using Shouldly;

public class EnvironmentVariableConfigurationSourceTests
{
    [Fact]
    public void TryResolve_WithValidEndpointAndToken_ReturnsBoth()
    {
        var source = new EnvironmentVariableConfigurationSource(Fake(
            endpoint: "https://env.example.com/",
            token: "secret"));

        bool resolved = source.TryResolve(out Uri? endpoint, out string? apiToken);

        resolved.ShouldBeTrue();
        endpoint.ShouldBe(new Uri("https://env.example.com/"));
        apiToken.ShouldBe("secret");
    }

    [Fact]
    public void TryResolve_WithEmptyStringEndpoint_IsTreatedAsUnset()
    {
        // Empty string trap: HEXALITH_MEMORIES_ENDPOINT="" must fall through, not fail on Uri.TryCreate.
        var source = new EnvironmentVariableConfigurationSource(Fake(endpoint: string.Empty, token: string.Empty));

        bool resolved = source.TryResolve(out Uri? endpoint, out string? apiToken);

        resolved.ShouldBeFalse();
        endpoint.ShouldBeNull();
        apiToken.ShouldBeNull();
    }

    [Fact]
    public void TryResolve_WithMalformedEndpoint_ThrowsInvalidConfigurationException()
    {
        var source = new EnvironmentVariableConfigurationSource(Fake(endpoint: "not a uri", token: "env-token"));

        InvalidConfigurationException exception = Should.Throw<InvalidConfigurationException>(
            () => source.TryResolve(out _, out _));

        exception.FilePath.ShouldBe(EnvironmentVariableConfigurationSource.EndpointVariableName);
        exception.Message.ShouldContain(EnvironmentVariableConfigurationSource.EndpointVariableName);
    }

    [Fact]
    public void TryResolve_WithTokenOnly_ContributesTokenOnly()
    {
        var source = new EnvironmentVariableConfigurationSource(Fake(endpoint: null, token: "t"));

        bool resolved = source.TryResolve(out Uri? endpoint, out string? apiToken);

        resolved.ShouldBeTrue();
        endpoint.ShouldBeNull();
        apiToken.ShouldBe("t");
    }

    private static Func<string, string?> Fake(string? endpoint, string? token)
        => name => name switch
        {
            EnvironmentVariableConfigurationSource.EndpointVariableName => endpoint,
            EnvironmentVariableConfigurationSource.ApiTokenVariableName => token,
            _ => null,
        };
}
