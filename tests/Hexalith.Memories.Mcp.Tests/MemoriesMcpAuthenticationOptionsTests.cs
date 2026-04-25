// <copyright file="MemoriesMcpAuthenticationOptionsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using Hexalith.Memories.Mcp.Authentication;

using Microsoft.Extensions.Options;

using Shouldly;

public sealed class MemoriesMcpAuthenticationOptionsTests
{
    [Fact]
    public void Validate_Fails_WhenNeitherAuthorityNorSigningKeyConfigured()
    {
        var validator = new ValidateMcpAuthenticationOptions();

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
        var validator = new ValidateMcpAuthenticationOptions();

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
        var validator = new ValidateMcpAuthenticationOptions();
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
        var validator = new ValidateMcpAuthenticationOptions();

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
    public void Validate_Succeeds_WithSymmetricDevelopmentConfiguration()
    {
        var validator = new ValidateMcpAuthenticationOptions();

        ValidateOptionsResult result = validator.Validate(null, new MemoriesMcpAuthenticationOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            SigningKey = "not-base64-signing-key-value-32-bytes",
        });

        result.Succeeded.ShouldBeTrue();
    }
}
