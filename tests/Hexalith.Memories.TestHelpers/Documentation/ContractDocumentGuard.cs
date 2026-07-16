// <copyright file="ContractDocumentGuard.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.TestHelpers.Documentation;

/// <summary>
/// Finds raw tool-call markup that does not belong in a published contract document.
/// </summary>
public static class ContractDocumentGuard
{
    private static readonly string[] MarkerNames = ["content", "invoke", "parameter", "tool_call"];

    /// <summary>
    /// Finds raw opening, closing, attributed, or incomplete tool-call tags outside Markdown code spans and fences.
    /// </summary>
    /// <param name="markdown">The Markdown content to inspect.</param>
    /// <returns>Human-readable diagnostics containing the marker text and its one-based line and column.</returns>
    public static IReadOnlyList<string> FindLeakedToolCallMarkup(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        string[] lines = NormalizeLineEndings(markdown).Split('\n');
        bool[] fencedLines = FindFencedLines(lines);
        var diagnostics = new List<string>();
        int codeSpanDelimiterLength = 0;

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (fencedLines[lineIndex])
            {
                codeSpanDelimiterLength = 0;
                continue;
            }

            FindLineMarkers(lines, fencedLines, lineIndex, diagnostics, ref codeSpanDelimiterLength);
        }

        return diagnostics.AsReadOnly();
    }

    private static void FindLineMarkers(
        string[] lines,
        bool[] fencedLines,
        int lineIndex,
        List<string> diagnostics,
        ref int codeSpanDelimiterLength)
    {
        string line = lines[lineIndex];
        bool canContainCodeSpanDelimiter = CountLeadingSpaces(line) <= 3 && !line.StartsWith('\t');
        for (int index = 0; index < line.Length; index++)
        {
            if (canContainCodeSpanDelimiter && line[index] == '`' && !IsEscaped(line, index))
            {
                int delimiterLength = CountRun(line, index, '`');
                if (codeSpanDelimiterLength == delimiterLength)
                {
                    codeSpanDelimiterLength = 0;
                }
                else if (codeSpanDelimiterLength == 0 && HasMatchingCodeSpanDelimiter(
                    lines,
                    fencedLines,
                    lineIndex,
                    index + delimiterLength,
                    delimiterLength))
                {
                    codeSpanDelimiterLength = delimiterLength;
                }

                index += delimiterLength - 1;
                continue;
            }

            if (codeSpanDelimiterLength != 0 || line[index] != '<')
            {
                continue;
            }

            int cursor = index + 1;
            SkipWhitespace(line, ref cursor);
            if (cursor < line.Length && line[cursor] == '/')
            {
                cursor++;
                SkipWhitespace(line, ref cursor);
            }

            int nameStart = cursor;
            while (cursor < line.Length && IsTagNameCharacter(line[cursor]))
            {
                cursor++;
            }

            if (nameStart == cursor || !IsMarkerName(line[nameStart..cursor]))
            {
                continue;
            }

            int markerEnd = line.IndexOf('>', cursor);
            markerEnd = markerEnd < 0 ? line.Length : markerEnd + 1;
            string fragment = line[index..markerEnd];
            diagnostics.Add($"line {lineIndex + 1}, column {index + 1}: {fragment}");
            index = markerEnd - 1;
        }
    }

    private static bool IsMarkerName(string candidate)
        => MarkerNames.Contains(candidate, StringComparer.OrdinalIgnoreCase);

    private static bool IsTagNameCharacter(char character)
        => char.IsLetterOrDigit(character) || character is '_' or '-' or '.' or ':';

    private static bool HasMatchingCodeSpanDelimiter(
        string[] lines,
        bool[] fencedLines,
        int startLineIndex,
        int startIndex,
        int delimiterLength)
    {
        for (int lineIndex = startLineIndex; lineIndex < lines.Length && !fencedLines[lineIndex]; lineIndex++)
        {
            string line = lines[lineIndex];
            if (CountLeadingSpaces(line) > 3 || line.StartsWith('\t'))
            {
                continue;
            }

            for (int index = lineIndex == startLineIndex ? startIndex : 0; index < line.Length; index++)
            {
                if (line[index] != '`' || IsEscaped(line, index))
                {
                    continue;
                }

                int runLength = CountRun(line, index, '`');
                if (runLength == delimiterLength)
                {
                    return true;
                }

                index += runLength - 1;
            }
        }

        return false;
    }

    private static bool IsEscaped(string value, int index)
    {
        int backslashCount = 0;
        for (int cursor = index - 1; cursor >= 0 && value[cursor] == '\\'; cursor--)
        {
            backslashCount++;
        }

        return backslashCount % 2 != 0;
    }

    private static int CountRun(string value, int startIndex, char character)
    {
        int index = startIndex;
        while (index < value.Length && value[index] == character)
        {
            index++;
        }

        return index - startIndex;
    }

    private static void SkipWhitespace(string value, ref int index)
    {
        while (index < value.Length && char.IsWhiteSpace(value[index]))
        {
            index++;
        }
    }

    private static bool TryReadFence(string line, out char character, out int length, out bool hasOnlyTrailingWhitespace)
    {
        int offset = CountLeadingSpaces(line);
        if (offset > 3)
        {
            character = '\0';
            length = 0;
            hasOnlyTrailingWhitespace = false;
            return false;
        }

        string trimmed = line[offset..];
        character = trimmed.Length > 0 ? trimmed[0] : '\0';
        if (character is not ('`' or '~'))
        {
            length = 0;
            hasOnlyTrailingWhitespace = false;
            return false;
        }

        length = CountRun(trimmed, 0, character);
        if (length < 3)
        {
            hasOnlyTrailingWhitespace = false;
            return false;
        }

        hasOnlyTrailingWhitespace = string.IsNullOrWhiteSpace(trimmed[length..]);
        return true;
    }

    private static int CountLeadingSpaces(string line)
    {
        int count = 0;
        while (count < line.Length && line[count] == ' ')
        {
            count++;
        }

        return count;
    }

    private static bool[] FindFencedLines(string[] lines)
    {
        var fencedLines = new bool[lines.Length];
        char fenceCharacter = '\0';
        int fenceLength = 0;
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (TryReadFence(lines[lineIndex], out char candidateCharacter, out int candidateLength, out bool hasOnlyTrailingWhitespace))
            {
                fencedLines[lineIndex] = true;
                if (fenceCharacter == '\0')
                {
                    fenceCharacter = candidateCharacter;
                    fenceLength = candidateLength;
                }
                else if (candidateCharacter == fenceCharacter && candidateLength >= fenceLength && hasOnlyTrailingWhitespace)
                {
                    fenceCharacter = '\0';
                    fenceLength = 0;
                }

                continue;
            }

            fencedLines[lineIndex] = fenceCharacter != '\0';
        }

        return fencedLines;
    }

    private static string NormalizeLineEndings(string markdown)
        => markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
