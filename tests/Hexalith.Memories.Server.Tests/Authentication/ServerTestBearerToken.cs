// <copyright file="ServerTestBearerToken.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Microsoft.IdentityModel.Tokens;

/// <summary>Creates development JWT bearer tokens for in-process Server endpoint tests.</summary>
internal static class ServerTestBearerToken
{
    /// <summary>Gets the configured test issuer.</summary>
    public const string Issuer = "hexalith-memories-test";

    /// <summary>Gets the configured test audience.</summary>
    public const string Audience = "hexalith-memories-server";

    /// <summary>Gets the configured symmetric test signing key.</summary>
    public const string SigningKey = "hexalith-memories-test-signing-key-32b";

    /// <summary>Creates an encoded JWT matching <c>appsettings.Development.json</c>.</summary>
    /// <param name="subject">The token subject.</param>
    /// <param name="tenants">The tenant ids carried by the configured tenant claim.</param>
    /// <param name="additionalClaims">Additional claims to include in the token.</param>
    /// <returns>A signed bearer token.</returns>
    public static string Create(
        string subject = "operator-1",
        IEnumerable<string>? tenants = null,
        IEnumerable<Claim>? additionalClaims = null)
    {
        string[] tenantValues = [.. tenants ?? ["acme"]];
        Claim[] tenantClaims = tenantValues.Length == 1
            ? [new Claim("tenant_id", tenantValues[0])]
            : [new Claim("tenants", JsonSerializer.Serialize(tenantValues))];
        Claim[] claims =
        [
            new Claim("sub", subject),
            .. tenantClaims,
            .. additionalClaims ?? [],
        ];
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = credentials,
        };

        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }
}
