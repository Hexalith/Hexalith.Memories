// <copyright file="EmbeddingResponseSanitizer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Redacts secret-like values and submitted input text from provider error bodies before they cross the
/// embedding boundary. Shared by every provider through <see cref="EmbeddingProviderTransport"/> (Story 23.9, AC4).</summary>
internal static class EmbeddingResponseSanitizer
{
    /// <summary>Minimum length below which a sensitive value is not redacted, to avoid masking benign short
    /// substrings (e.g., common words) that happen to overlap with a sensitive value. Full input text is redacted
    /// without this floor.</summary>
    internal const int RedactionMinLength = 8;

    /// <summary>Redacts the supplied sensitive values from <paramref name="text"/>.</summary>
    /// <param name="text">The upstream payload that may contain leaked secrets.</param>
    /// <param name="sensitiveValues">The values that must not appear in the redacted output.</param>
    /// <returns>The redacted string.</returns>
    internal static string Redact(string text, IReadOnlyCollection<string?> sensitiveValues)
        => Redact(text, sensitiveValues, fullInputTexts: null);

    /// <summary>Redacts the supplied sensitive values plus a single full input text when present.</summary>
    /// <param name="text">The upstream payload that may contain leaked secrets.</param>
    /// <param name="sensitiveValues">The secret-like values that must not appear in the redacted output.</param>
    /// <param name="fullInputText">The full embedding input text, redacted without the secret-length floor.</param>
    /// <returns>The redacted string.</returns>
    internal static string Redact(string text, IReadOnlyCollection<string?> sensitiveValues, string? fullInputText)
        => Redact(text, sensitiveValues, fullInputText is null ? null : [fullInputText]);

    /// <summary>Redacts the supplied sensitive values plus every submitted input text when present. Batch callers must
    /// redact every submitted text, not only the first one (Story 23.9, AC4).</summary>
    /// <param name="text">The upstream payload that may contain leaked secrets.</param>
    /// <param name="sensitiveValues">The secret-like values that must not appear in the redacted output.</param>
    /// <param name="fullInputTexts">The full embedding input texts, redacted without the secret-length floor.</param>
    /// <returns>The redacted string.</returns>
    internal static string Redact(
        string text,
        IReadOnlyCollection<string?> sensitiveValues,
        IReadOnlyList<string>? fullInputTexts)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Filter to non-blank, sufficiently-long, distinct values; longest first so a longer secret is replaced before
        // any shorter secret it contains as a substring.
        IEnumerable<string> orderedSecrets = (sensitiveValues ?? [])
            .Where(static v => !string.IsNullOrWhiteSpace(v) && v!.Length >= RedactionMinLength)
            .Select(static v => v!)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(static v => v.Length);

        string sanitized = text;
        foreach (string value in orderedSecrets)
        {
            sanitized = sanitized.Replace(value, "[redacted]", StringComparison.Ordinal);
        }

        if (fullInputTexts is not null)
        {
            // Input text carries no length floor; order longest-first so overlapping inputs are fully masked.
            IEnumerable<string> orderedInputs = fullInputTexts
                .Where(static v => !string.IsNullOrEmpty(v))
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(static v => v.Length);
            foreach (string input in orderedInputs)
            {
                sanitized = sanitized.Replace(input, "[redacted]", StringComparison.Ordinal);
            }
        }

        return sanitized;
    }
}
