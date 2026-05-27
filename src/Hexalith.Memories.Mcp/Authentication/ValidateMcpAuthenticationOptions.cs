// <copyright file="ValidateMcpAuthenticationOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Authentication;

using System.Text;

using Microsoft.Extensions.Options;

/// <summary>Validates <see cref="MemoriesMcpAuthenticationOptions"/> at startup.</summary>
public sealed class ValidateMcpAuthenticationOptions : IValidateOptions<MemoriesMcpAuthenticationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MemoriesMcpAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Authority) && string.IsNullOrWhiteSpace(options.SigningKey))
        {
            return ValidateOptionsResult.Fail(
                "Authentication:JwtBearer requires either 'Authority' (production OIDC) or 'SigningKey' (development symmetric key) to be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            return ValidateOptionsResult.Fail("Authentication:JwtBearer:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail("Authentication:JwtBearer:Audience must be configured.");
        }

        if (!string.IsNullOrWhiteSpace(options.SigningKey) && GetEffectiveSigningKeyByteCount(options.SigningKey) < 32)
        {
            return ValidateOptionsResult.Fail(
                "Authentication:JwtBearer:SigningKey must provide at least 32 bytes (256 bits) of effective key material.");
        }

        if (string.IsNullOrWhiteSpace(options.TenantClaimName))
        {
            return ValidateOptionsResult.Fail("Authentication:JwtBearer:TenantClaimName must be configured.");
        }

        if (options.ValidAlgorithms.Count == 0 || options.ValidAlgorithms.Any(string.IsNullOrWhiteSpace))
        {
            return ValidateOptionsResult.Fail("Authentication:JwtBearer:ValidAlgorithms must contain at least one algorithm name.");
        }

        return ValidateOptionsResult.Success;
    }

    private static int GetEffectiveSigningKeyByteCount(string signingKey)
    {
        int utf8Bytes = Encoding.UTF8.GetByteCount(signingKey);
        return TryGetBase64ByteCount(signingKey, out int base64Bytes)
            ? Math.Min(utf8Bytes, base64Bytes)
            : utf8Bytes;
    }

    private static bool TryGetBase64ByteCount(string value, out int byteCount)
    {
        try
        {
            byteCount = Convert.FromBase64String(value).Length;
            return true;
        }
        catch (FormatException)
        {
            byteCount = 0;
            return false;
        }
    }
}
