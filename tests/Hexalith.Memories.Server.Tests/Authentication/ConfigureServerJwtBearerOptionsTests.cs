// <copyright file="ConfigureServerJwtBearerOptionsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using Hexalith.Memories.Server.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

/// <summary>Configuration guards for Server JWT bearer validation.</summary>
[Trait("Category", "Unit")]
public sealed class ConfigureServerJwtBearerOptionsTests
{
    [Fact]
    public void Configure_DevelopmentSigningKeyMode_SetsStrictTokenValidationParameters()
    {
        MemoriesServerAuthenticationOptions auth = ValidOptions();
        JwtBearerOptions options = Configure(auth);

        options.MapInboundClaims.ShouldBeFalse();
        options.TokenValidationParameters.ValidateIssuer.ShouldBeTrue();
        options.TokenValidationParameters.ValidateAudience.ShouldBeTrue();
        options.TokenValidationParameters.ValidateIssuerSigningKey.ShouldBeTrue();
        options.TokenValidationParameters.ValidateLifetime.ShouldBeTrue();
        options.TokenValidationParameters.RequireExpirationTime.ShouldBeTrue();
        options.TokenValidationParameters.RequireSignedTokens.ShouldBeTrue();
        options.TokenValidationParameters.ClockSkew.ShouldBe(TimeSpan.FromMinutes(1));
        options.TokenValidationParameters.ValidIssuer.ShouldBe(auth.Issuer);
        options.TokenValidationParameters.ValidAudience.ShouldBe(auth.Audience);
        options.TokenValidationParameters.ValidAlgorithms.ShouldBe(auth.ValidAlgorithms);
        options.TokenValidationParameters.IssuerSigningKey.ShouldBeOfType<SymmetricSecurityKey>();
        options.Authority.ShouldBeNull();
    }

    [Fact]
    public void Configure_OidcMode_SetsAuthorityAndHttpsMetadata()
    {
        MemoriesServerAuthenticationOptions auth = ValidOptions() with
        {
            Authority = "https://issuer.example/realms/hexalith",
            SigningKey = null,
            RequireHttpsMetadata = false,
        };

        JwtBearerOptions options = Configure(auth);

        options.Authority.ShouldBe(auth.Authority);
        options.RequireHttpsMetadata.ShouldBeFalse();
        options.TokenValidationParameters.IssuerSigningKey.ShouldBeNull();
        options.TokenValidationParameters.ValidIssuer.ShouldBe(auth.Issuer);
        options.TokenValidationParameters.ValidAudience.ShouldBe(auth.Audience);
    }

    [Fact]
    public void Configure_NonBearerScheme_DoesNotMutateOptions()
    {
        var configure = new ConfigureServerJwtBearerOptions(
            Options.Create(ValidOptions()),
            NullLoggerFactory.Instance);
        var options = new JwtBearerOptions
        {
            Authority = "https://custom.example/realm",
            TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = "custom-issuer",
                ValidAudience = "custom-audience",
            },
        };

        configure.Configure("custom", options);

        options.Authority.ShouldBe("https://custom.example/realm");
        options.TokenValidationParameters.ValidIssuer.ShouldBe("custom-issuer");
        options.TokenValidationParameters.ValidAudience.ShouldBe("custom-audience");
        options.TokenValidationParameters.IssuerSigningKey.ShouldBeNull();
    }

    private static JwtBearerOptions Configure(MemoriesServerAuthenticationOptions auth)
    {
        var configure = new ConfigureServerJwtBearerOptions(
            Options.Create(auth),
            NullLoggerFactory.Instance);
        var options = new JwtBearerOptions();

        configure.Configure(JwtBearerDefaults.AuthenticationScheme, options);

        return options;
    }

    private static MemoriesServerAuthenticationOptions ValidOptions() => new()
    {
        Issuer = "hexalith-memories-test",
        Audience = "hexalith-memories-server",
        SigningKey = "hexalith-memories-test-signing-key-32b",
        RequireHttpsMetadata = false,
    };
}
