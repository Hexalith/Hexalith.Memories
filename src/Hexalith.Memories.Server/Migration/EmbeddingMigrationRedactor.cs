// <copyright file="EmbeddingMigrationRedactor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

using System.Text.RegularExpressions;

/// <summary>Sanitizes migration output before it reaches logs, artifacts, or command output.</summary>
public static partial class EmbeddingMigrationRedactor
{
    private const int MaxMessageLength = 512;

    /// <summary>Redacts known secret and token shapes from an operator-visible value.</summary>
    /// <param name="value">The value to sanitize.</param>
    /// <returns>The sanitized value.</returns>
    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string sanitized = ApplyRedactions(value);
        if (sanitized.Length > MaxMessageLength)
        {
            sanitized = ApplyRedactions(sanitized[..MaxMessageLength]) + "...";
        }

        return sanitized;
    }

    private static string ApplyRedactions(string value)
    {
        // Bearer-prefixed tokens first so JWT bodies with `Bearer ` are caught here, leaving
        // RawJwtRegex to handle any unprefixed JWT shapes that appear in error payloads.
        string sanitized = BearerRegex().Replace(value, "Bearer [redacted]");
        sanitized = RawJwtRegex().Replace(sanitized, "[redacted-jwt]");
        sanitized = GoogleApiKeyRegex().Replace(sanitized, "AIza[redacted]");
        sanitized = AwsAccessKeyIdRegex().Replace(sanitized, "[redacted-aws-key]");
        sanitized = BasicAuthRegex().Replace(sanitized, "Basic [redacted]");
        sanitized = SecretFieldRegex().Replace(sanitized, "$1[redacted]$3");
        sanitized = JsonEscapedSecretRegex().Replace(sanitized, "$1[redacted]$3");
        return sanitized;
    }

    [GeneratedRegex("Bearer\\s+[A-Za-z0-9._~+/=-]+", RegexOptions.IgnoreCase)]
    private static partial Regex BearerRegex();

    // RFC 7519 JWTs are base64url-encoded triplets. Anchoring on the canonical "eyJ" header prefix
    // (base64url of `{"`) avoids false positives on generic dotted identifiers, and the lookbehind/
    // lookahead reject partial matches inside larger base64url payloads.
    [GeneratedRegex("(?<![A-Za-z0-9_-])eyJ[A-Za-z0-9_-]{4,}\\.[A-Za-z0-9_-]{4,}\\.[A-Za-z0-9_-]{4,}(?![A-Za-z0-9_-])", RegexOptions.None)]
    private static partial Regex RawJwtRegex();

    [GeneratedRegex("AIza[A-Za-z0-9_-]{8,}", RegexOptions.IgnoreCase)]
    private static partial Regex GoogleApiKeyRegex();

    // AWS long-term ("AKIA") and temporary ("ASIA") access key IDs are 20 characters total,
    // uppercase alphanumeric. Word-class boundaries prevent matching inside larger strings.
    [GeneratedRegex("(?<![A-Z0-9])A[KS]IA[A-Z0-9]{16}(?![A-Z0-9])", RegexOptions.None)]
    private static partial Regex AwsAccessKeyIdRegex();

    // HTTP Basic auth header values are `Basic <base64>` per RFC 7617. Require >=8 base64 chars to
    // avoid matching the bare word "Basic" followed by a short token.
    [GeneratedRegex("(?<![A-Za-z])Basic\\s+[A-Za-z0-9+/=]{8,}", RegexOptions.IgnoreCase)]
    private static partial Regex BasicAuthRegex();

    [GeneratedRegex("((?:client[_-]?secret|api[_-]?key|access[_-]?token|refresh[_-]?token|id[_-]?token)\\s*[:=]\\s*[\"']?)([^\\s\"',;}]+)([\"']?)", RegexOptions.IgnoreCase)]
    private static partial Regex SecretFieldRegex();

    [GeneratedRegex("(\\\\?\"(?:client[_-]?secret|api[_-]?key|access[_-]?token|refresh[_-]?token|id[_-]?token)\\\\?\"\\s*:\\s*\\\\?\")([^\"\\\\]+)(\\\\?\")", RegexOptions.IgnoreCase)]
    private static partial Regex JsonEscapedSecretRegex();
}
