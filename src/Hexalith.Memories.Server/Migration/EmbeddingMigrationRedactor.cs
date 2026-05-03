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
        string sanitized = BearerRegex().Replace(value, "Bearer [redacted]");
        sanitized = GoogleApiKeyRegex().Replace(sanitized, "AIza[redacted]");
        sanitized = SecretFieldRegex().Replace(sanitized, "$1[redacted]$3");
        sanitized = JsonEscapedSecretRegex().Replace(sanitized, "$1[redacted]$3");
        return sanitized;
    }

    [GeneratedRegex("Bearer\\s+[A-Za-z0-9._~+/=-]+", RegexOptions.IgnoreCase)]
    private static partial Regex BearerRegex();

    [GeneratedRegex("AIza[A-Za-z0-9_-]{8,}", RegexOptions.IgnoreCase)]
    private static partial Regex GoogleApiKeyRegex();

    [GeneratedRegex("((?:client[_-]?secret|api[_-]?key|access[_-]?token|refresh[_-]?token|id[_-]?token)\\s*[:=]\\s*[\"']?)([^\\s\"',;}]+)([\"']?)", RegexOptions.IgnoreCase)]
    private static partial Regex SecretFieldRegex();

    [GeneratedRegex("(\\\\?\"(?:client[_-]?secret|api[_-]?key|access[_-]?token|refresh[_-]?token|id[_-]?token)\\\\?\"\\s*:\\s*\\\\?\")([^\"\\\\]+)(\\\\?\")", RegexOptions.IgnoreCase)]
    private static partial Regex JsonEscapedSecretRegex();
}
