// <copyright file="MemoriesMcpAuthenticationOptionsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using Hexalith.Memories.Mcp.Authentication;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public sealed class MemoriesMcpAuthenticationOptionsTests
{
    [Fact]
    public void Validate_Fails_WhenNeitherAuthorityNorSigningKeyConfigured()
    {
        ValidateMcpAuthenticationOptions validator = BuildValidator("Development");

        ValidateOptionsResult result = validator.Validate(null, new MemoriesMcpAuthenticationOptions
        {
            Issuer = "issuer",
            Audience = "audience",
        });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Authority");
        result.FailureMessage.ShouldContain("SigningKey");
    }

    [Fact]
    public void Validate_Fails_WhenSigningKeyHasLessThanThirtyTwoBytes()
    {
        ValidateMcpAuthenticationOptions validator = BuildValidator("Development");

        ValidateOptionsResult result = validator.Validate(null, new MemoriesMcpAuthenticationOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            SigningKey = "short-key",
        });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("at least 32 bytes");
    }

    [Fact]
    public void Validate_Fails_WhenBase64SigningKeyDecodesBelowThirtyTwoBytes()
    {
        ValidateMcpAuthenticationOptions validator = BuildValidator("Development");
        string weakBase64Key = Convert.ToBase64String(new byte[24]);

        ValidateOptionsResult result = validator.Validate(null, new MemoriesMcpAuthenticationOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            SigningKey = weakBase64Key,
        });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("at least 32 bytes");
    }

    [Fact]
    public void Validate_Fails_WhenValidAlgorithmsEmpty()
    {
        ValidateMcpAuthenticationOptions validator = BuildValidator("Development");

        ValidateOptionsResult result = validator.Validate(null, new MemoriesMcpAuthenticationOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            SigningKey = "not-base64-signing-key-value-32-bytes",
            ValidAlgorithms = [],
        });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("ValidAlgorithms");
    }

    [Fact]
    public void DevelopmentWithSigningKey_Succeeds()
    {
        ValidateMcpAuthenticationOptions validator = BuildValidator("Development");

        ValidateOptionsResult result = validator.Validate(null, new MemoriesMcpAuthenticationOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            SigningKey = "not-base64-signing-key-value-32-bytes",
        });

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void ProductionWithSigningKey_Fails()
    {
        ValidateMcpAuthenticationOptions validator = BuildValidator("Production");
        const string SecretSigningKey = "production-static-signing-key-32-bytes";

        ValidateOptionsResult result = validator.Validate(null, new MemoriesMcpAuthenticationOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            Authority = "https://login.example.test",
            SigningKey = SecretSigningKey,
        });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Production");
        result.FailureMessage.ShouldContain("SigningKey");
        result.FailureMessage.ShouldNotContain(SecretSigningKey);
    }

    [Fact]
    public void ProductionWithAuthorityAndRequireHttpsMetadataFalse_Fails()
    {
        ValidateMcpAuthenticationOptions validator = BuildValidator("Production");

        ValidateOptionsResult result = validator.Validate(null, new MemoriesMcpAuthenticationOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            Authority = "https://login.example.test",
            RequireHttpsMetadata = false,
        });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Production");
        result.FailureMessage.ShouldContain("RequireHttpsMetadata");
    }

    [Fact]
    public void ProductionWithAuthorityAndRequireHttpsMetadataTrue_Succeeds()
    {
        ValidateMcpAuthenticationOptions validator = BuildValidator("Production");

        ValidateOptionsResult result = validator.Validate(null, new MemoriesMcpAuthenticationOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            Authority = "https://login.example.test",
            RequireHttpsMetadata = true,
        });

        result.Succeeded.ShouldBeTrue();
    }

    private static ValidateMcpAuthenticationOptions BuildValidator(string environmentName)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        return new ValidateMcpAuthenticationOptions(environment);
    }
}
