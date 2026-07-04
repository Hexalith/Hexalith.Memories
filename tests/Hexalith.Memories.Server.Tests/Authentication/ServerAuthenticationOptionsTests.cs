// <copyright file="ServerAuthenticationOptionsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using Hexalith.Memories.Server.Authentication;

using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>Validation guards for Memories Server JWT bearer configuration.</summary>
[Trait("Category", "Unit")]
public sealed class ServerAuthenticationOptionsTests
{
    [Fact]
    public void Validate_MissingAuthorityAndSigningKey_Fails()
    {
        var validator = new ValidateServerAuthenticationOptions();

        ValidateOptionsResult result = validator.Validate(null, ValidOptions() with { Authority = null, SigningKey = null });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Authority");
        result.FailureMessage.ShouldContain("SigningKey");
    }

    [Fact]
    public void Validate_WeakSigningKey_Fails()
    {
        var validator = new ValidateServerAuthenticationOptions();

        ValidateOptionsResult result = validator.Validate(null, ValidOptions() with { SigningKey = "too-short" });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("at least 32 bytes");
    }

    [Theory]
    [InlineData("", "hexalith-memories-server", "tenant_id", "Issuer")]
    [InlineData("hexalith-memories-test", "", "tenant_id", "Audience")]
    [InlineData("hexalith-memories-test", "hexalith-memories-server", "", "TenantClaimName")]
    public void Validate_BlankRequiredString_Fails(string issuer, string audience, string tenantClaimName, string expectedName)
    {
        var validator = new ValidateServerAuthenticationOptions();

        ValidateOptionsResult result = validator.Validate(
            null,
            ValidOptions() with
            {
                Issuer = issuer,
                Audience = audience,
                TenantClaimName = tenantClaimName,
            });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(expectedName);
    }

    [Fact]
    public void Validate_EmptyAlgorithms_Fails()
    {
        var validator = new ValidateServerAuthenticationOptions();

        ValidateOptionsResult result = validator.Validate(null, ValidOptions() with { ValidAlgorithms = [] });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("ValidAlgorithms");
    }

    [Fact]
    public void Validate_OidcMode_Succeeds()
    {
        var validator = new ValidateServerAuthenticationOptions();

        ValidateOptionsResult result = validator.Validate(
            null,
            ValidOptions() with
            {
                Authority = "https://issuer.example/realms/hexalith",
                SigningKey = null,
            });

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_DevelopmentSigningKeyMode_Succeeds()
    {
        var validator = new ValidateServerAuthenticationOptions();

        ValidateOptionsResult result = validator.Validate(null, ValidOptions());

        result.Succeeded.ShouldBeTrue();
    }

    private static MemoriesServerAuthenticationOptions ValidOptions() => new()
    {
        Issuer = "hexalith-memories-test",
        Audience = "hexalith-memories-server",
        SigningKey = "hexalith-memories-test-signing-key-32b",
        RequireHttpsMetadata = false,
    };
}
