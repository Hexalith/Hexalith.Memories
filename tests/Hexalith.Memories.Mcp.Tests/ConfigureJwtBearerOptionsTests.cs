// <copyright file="ConfigureJwtBearerOptionsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using Hexalith.Memories.Mcp.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

public sealed class ConfigureJwtBearerOptionsTests
{
    [Fact]
    public void Configure_SetsStrictTokenValidationParameters()
    {
        var options = new JwtBearerOptions();
        var configure = new ConfigureJwtBearerOptions(
            Options.Create(new MemoriesMcpAuthenticationOptions
            {
                Issuer = "issuer",
                Audience = "audience",
                SigningKey = new string('x', 32),
                ValidAlgorithms = ["HS256"],
            }),
            NullLoggerFactory.Instance);

        configure.Configure(JwtBearerDefaults.AuthenticationScheme, options);

        TokenValidationParameters parameters = options.TokenValidationParameters;
        parameters.ValidateIssuer.ShouldBeTrue();
        parameters.ValidateAudience.ShouldBeTrue();
        parameters.ValidateIssuerSigningKey.ShouldBeTrue();
        parameters.ValidateLifetime.ShouldBeTrue();
        parameters.RequireExpirationTime.ShouldBeTrue();
        parameters.RequireSignedTokens.ShouldBeTrue();
        parameters.ValidIssuer.ShouldBe("issuer");
        parameters.ValidAudience.ShouldBe("audience");
        parameters.ValidAlgorithms.ShouldBe(["HS256"]);
        parameters.ClockSkew.ShouldBe(TimeSpan.FromMinutes(1));
        parameters.IssuerSigningKey.ShouldBeOfType<SymmetricSecurityKey>();
    }

    [Fact]
    public void Configure_OidcMode_SetsAuthorityAndHttpsMetadata()
    {
        var options = new JwtBearerOptions();
        var configure = new ConfigureJwtBearerOptions(
            Options.Create(new MemoriesMcpAuthenticationOptions
            {
                Issuer = "issuer",
                Audience = "audience",
                Authority = "https://login.example.test",
                RequireHttpsMetadata = true,
            }),
            NullLoggerFactory.Instance);

        configure.Configure(JwtBearerDefaults.AuthenticationScheme, options);

        options.Authority.ShouldBe("https://login.example.test");
        options.RequireHttpsMetadata.ShouldBeTrue();
    }
}
