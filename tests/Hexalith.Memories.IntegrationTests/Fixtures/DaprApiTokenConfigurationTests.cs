// <copyright file="DaprApiTokenConfigurationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using Hexalith.Memories.AppHost;

using Shouldly;

/// <summary>Executable tests for the AppHost Dapr API token mode.</summary>
[Trait("Category", "Integration")]
public sealed class DaprApiTokenConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("disabled")]
    [InlineData("DISABLED")]
    public void Resolve_DisabledOrUnsetMode_ReturnsNoTokens(string? mode)
    {
        (string? daprToken, string? appToken) = DaprApiTokenConfiguration.Resolve(
            Environment(mode, daprToken: "dapr-token", appToken: "app-token"));

        daprToken.ShouldBeNull();
        appToken.ShouldBeNull();
    }

    [Theory]
    [InlineData("enabled")]
    [InlineData("Enabled")]
    public void Resolve_EnabledModeWithBothTokens_ReturnsTokens(string mode)
    {
        (string? daprToken, string? appToken) = DaprApiTokenConfiguration.Resolve(
            Environment(mode, daprToken: "dapr-token", appToken: "app-token"));

        daprToken.ShouldBe("dapr-token");
        appToken.ShouldBe("app-token");
    }

    [Theory]
    [InlineData(null, "app-token")]
    [InlineData("dapr-token", null)]
    [InlineData(" ", "app-token")]
    public void Resolve_EnabledModeWithoutBothTokens_Throws(string? daprToken, string? appToken)
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => DaprApiTokenConfiguration.Resolve(Environment("enabled", daprToken, appToken)));

        exception.Message.ShouldContain("requires both DAPR_API_TOKEN and APP_API_TOKEN");
    }

    [Theory]
    [InlineData("enable")]
    [InlineData("true")]
    [InlineData("on")]
    [InlineData(" enabled")]
    [InlineData("disable")]
    public void Resolve_UnrecognizedMode_Throws(string mode)
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => DaprApiTokenConfiguration.Resolve(Environment(mode, daprToken: "dapr-token", appToken: "app-token")));

        exception.Message.ShouldContain("DAPR_API_TOKEN_MODE has an unrecognized value");
    }

    [Theory]
    [InlineData(" enabled", true)]
    [InlineData("disabled ", true)]
    [InlineData("enable", false)]
    public void Resolve_UnrecognizedMode_NamesPaddingWithoutEchoingTheValue(string mode, bool padded)
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => DaprApiTokenConfiguration.Resolve(Environment(mode, daprToken: "dapr-token", appToken: "app-token")));

        exception.Message.Contains("leading or trailing whitespace", StringComparison.Ordinal).ShouldBe(padded);
    }

    [Fact]
    public void Resolve_UnrecognizedMode_DoesNotEchoTheConfiguredValue()
    {
        const string misplacedSecret = "s.misplaced-token-value";

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => DaprApiTokenConfiguration.Resolve(Environment(misplacedSecret, daprToken: "dapr-token", appToken: "app-token")));

        exception.Message.ShouldNotContain(misplacedSecret);
    }

    private static Func<string, string?> Environment(string? mode, string? daprToken, string? appToken)
        => name => name switch
        {
            "DAPR_API_TOKEN_MODE" => mode,
            "DAPR_API_TOKEN" => daprToken,
            "APP_API_TOKEN" => appToken,
            _ => null,
        };
}
