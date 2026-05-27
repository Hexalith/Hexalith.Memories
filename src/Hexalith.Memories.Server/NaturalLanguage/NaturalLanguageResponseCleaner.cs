// <copyright file="NaturalLanguageResponseCleaner.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

using System.Text.RegularExpressions;

/// <summary>Story 9.2 Task 2.4 / Risk #7 — normalizes LLM responses by stripping Markdown code fences,
/// common preambles, and collapsing whitespace. Some providers (Anthropic especially) wrap responses in
/// <c>```</c> fences or prefix "Here is the summary:" regardless of system prompt — a noisy embedding
/// drifts from the intended semantics. The cleaner is intentionally conservative: a short allow-list of
/// preambles, plain fence-stripping, no aggressive rewriting.</summary>
internal static partial class NaturalLanguageResponseCleaner
{
    private const int MinimumAcceptableLength = 10;

    private static readonly string[] PreamblesToStrip =
    [
        "Summary:",
        "Here is the summary:",
        "Here's the summary:",
        "Here is a summary:",
        "Here's a summary:",
        "Summary of the event:",
        "Description:",
    ];

    /// <summary>Attempts to clean the raw LLM response. Returns <see langword="true"/> when the cleaned
    /// string is non-empty and at least <see cref="MinimumAcceptableLength"/> characters; otherwise
    /// returns <see langword="false"/> and the caller should treat this as an unavailable description.</summary>
    /// <param name="rawResponse">The raw response text from <c>ResultMessage.Content</c>.</param>
    /// <param name="cleaned">The cleaned text when the method returns <see langword="true"/>;
    /// <see cref="string.Empty"/> otherwise.</param>
    /// <returns><see langword="true"/> when the cleaned response is usable.</returns>
    public static bool TryClean(string? rawResponse, out string cleaned)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            cleaned = string.Empty;
            return false;
        }

        string candidate = rawResponse.Trim();

        // Loop strip fence→preamble→fence until a pass produces no change. Providers that emit
        // "Summary: ```text\n...\n```" escape a single-pass cleaner: the fence isn't at start when
        // the fence-strip runs, and the preamble-strip doesn't re-scan for fences. The loop
        // converges in ≤3 passes for any realistic wrapping shape and terminates on idempotence.
        const int MaxPasses = 4;
        for (int pass = 0; pass < MaxPasses; pass++)
        {
            string before = candidate;
            candidate = StripCommonPreambles(candidate);
            candidate = StripMarkdownCodeFences(candidate);
            if (string.Equals(candidate, before, StringComparison.Ordinal))
            {
                break;
            }
        }

        candidate = CollapseWhitespace(candidate).Trim();

        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length < MinimumAcceptableLength)
        {
            cleaned = string.Empty;
            return false;
        }

        cleaned = candidate;
        return true;
    }

    private static string StripMarkdownCodeFences(string text)
    {
        // Remove opening fence with optional language tag, e.g. ```json or ``` on its own line.
        text = OpeningCodeFenceRegex().Replace(text, string.Empty);

        // Remove closing fence.
        if (text.EndsWith("```", StringComparison.Ordinal))
        {
            text = text[..^3];
        }

        return text.Trim();
    }

    private static string StripCommonPreambles(string text)
    {
        foreach (string preamble in PreamblesToStrip)
        {
            if (text.StartsWith(preamble, StringComparison.OrdinalIgnoreCase))
            {
                text = text[preamble.Length..].TrimStart();
            }
        }

        return text;
    }

    private static string CollapseWhitespace(string text)
        => WhitespaceRegex().Replace(text, " ");

    [GeneratedRegex(@"^```[A-Za-z]*\r?\n?", RegexOptions.CultureInvariant)]
    private static partial Regex OpeningCodeFenceRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
