// <copyright file="ConfigureJwtBearerOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Authentication;

using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

/// <summary>Configures JWT bearer validation for the MCP ingress.</summary>
public sealed class ConfigureJwtBearerOptions(
    IOptions<MemoriesMcpAuthenticationOptions> authOptions,
    ILoggerFactory loggerFactory) : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ConfigureJwtBearerOptions>();

    /// <inheritdoc />
    public void Configure(string? name, JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        MemoriesMcpAuthenticationOptions authConfig = authOptions.Value;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidIssuer = authConfig.Issuer,
            ValidAudience = authConfig.Audience,
            ValidAlgorithms = authConfig.ValidAlgorithms,
        };

        if (!string.IsNullOrWhiteSpace(authConfig.Authority))
        {
            options.Authority = authConfig.Authority;
            options.RequireHttpsMetadata = authConfig.RequireHttpsMetadata;
        }
        else if (!string.IsNullOrWhiteSpace(authConfig.SigningKey))
        {
            options.TokenValidationParameters.IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authConfig.SigningKey));
        }

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                string sourceIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                string requestPath = context.Request.Path;
                string failureReason = context.Exception switch
                {
                    SecurityTokenExpiredException => "TokenExpired",
                    SecurityTokenInvalidSignatureException => "InvalidSignature",
                    SecurityTokenInvalidIssuerException => "InvalidIssuer",
                    SecurityTokenInvalidAudienceException => "InvalidAudience",
                    _ => context.Exception.GetType().Name,
                };

                _logger.LogWarning(
                    "MCP authentication failed: SecurityEvent={SecurityEvent}, SourceIp={SourceIp}, Path={RequestPath}, Reason={Reason}, FailureLayer={FailureLayer}",
                    "AuthenticationFailed",
                    sourceIp,
                    requestPath,
                    failureReason,
                    "JwtValidation");

                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                string sourceIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                string requestPath = context.Request.Path;
                string challengeReason = context.AuthenticateFailure switch
                {
                    SecurityTokenExpiredException => "TokenExpired",
                    not null => "InvalidToken",
                    _ => string.IsNullOrWhiteSpace(context.Error) ? "MissingToken" : context.Error,
                };

                _logger.LogWarning(
                    "MCP authentication challenge: SecurityEvent={SecurityEvent}, SourceIp={SourceIp}, Path={RequestPath}, Reason={Reason}, FailureLayer={FailureLayer}",
                    "AuthenticationFailed",
                    sourceIp,
                    requestPath,
                    challengeReason,
                    "JwtChallenge");

                return MemoriesMcpProblemDetailsChallengeWriter.WriteAsync(context);
            },
        };
    }

    /// <inheritdoc />
    public void Configure(JwtBearerOptions options) => Configure(JwtBearerDefaults.AuthenticationScheme, options);
}
