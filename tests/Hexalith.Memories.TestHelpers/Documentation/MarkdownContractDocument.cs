// <copyright file="MarkdownContractDocument.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.TestHelpers.Documentation;

using System.Text;

/// <summary>
/// Provides narrow, assertion-neutral access to exact ATX sections and Markdown table data rows.
/// </summary>
public sealed class MarkdownContractDocument
{
    private readonly List<(int LineIndex, int Level, string Title)> _headings = [];
    private readonly bool[] _isExcludedLine;
    private readonly string[] _lines;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownContractDocument"/> class.
    /// </summary>
    /// <param name="markdown">The Markdown content to parse.</param>
    public MarkdownContractDocument(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        this._lines = NormalizeLineEndings(markdown).Split('\n');
        this._isExcludedLine = new bool[this._lines.Length];
        this.IndexStructure();
    }

    /// <summary>
    /// Returns the content owned by one exact ATX heading, including subordinate sections.
    /// </summary>
    /// <param name="heading">The exact heading text without leading hash characters.</param>
    /// <returns>The normalized-LF section body, excluding the owning heading line.</returns>
    /// <exception cref="InvalidOperationException">The heading is missing or occurs more than once.</exception>
    public string GetSection(string heading)
    {
        (int start, int end) = this.GetSectionBounds(heading);
        return string.Join('\n', this._lines[start..end]);
    }

    /// <summary>
    /// Returns the trimmed data cells from the single Markdown table owned by one exact ATX section.
    /// </summary>
    /// <param name="heading">The exact heading text without leading hash characters.</param>
    /// <returns>The table data rows, excluding the header and delimiter rows.</returns>
    /// <exception cref="InvalidOperationException">The heading or table is missing, duplicated, or malformed.</exception>
    public IReadOnlyList<IReadOnlyList<string>> GetTableRows(string heading)
        => this.GetTable(heading).Rows;

    /// <summary>
    /// Returns the trimmed header cells from the single Markdown table owned by one exact ATX section.
    /// </summary>
    /// <param name="heading">The exact heading text without leading hash characters.</param>
    /// <returns>The table header cells.</returns>
    /// <exception cref="InvalidOperationException">The heading or table is missing, duplicated, or malformed.</exception>
    public IReadOnlyList<string> GetTableHeader(string heading)
        => this.GetTable(heading).Header;

    private static bool HasIndentedCodePrefix(string line)
    {
        if (line.StartsWith('\t'))
        {
            return true;
        }

        return CountLeadingSpaces(line) >= 4;
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

    private (IReadOnlyList<string> Header, IReadOnlyList<IReadOnlyList<string>> Rows) GetTable(string heading)
    {
        (int start, int end) = this.GetSectionBounds(heading);
        var tables = new List<(IReadOnlyList<string> Header, IReadOnlyList<IReadOnlyList<string>> Rows)>();

        for (int lineIndex = start; lineIndex + 1 < end; lineIndex++)
        {
            if (this._isExcludedLine[lineIndex] || this._isExcludedLine[lineIndex + 1] ||
                !TrySplitTableRow(this._lines[lineIndex], out string[]? header) ||
                !TrySplitTableRow(this._lines[lineIndex + 1], out string[]? delimiter) ||
                header.Length != delimiter.Length ||
                !delimiter.All(IsDelimiterCell))
            {
                continue;
            }

            var rows = new List<IReadOnlyList<string>>();
            int dataLineIndex = lineIndex + 2;
            while (dataLineIndex < end && !this._isExcludedLine[dataLineIndex] &&
                TrySplitTableRow(this._lines[dataLineIndex], out string[]? cells))
            {
                if (cells.Length != header.Length)
                {
                    throw new InvalidOperationException(
                        $"Table under heading '{heading}' has {cells.Length} cells on line {dataLineIndex + 1}; expected {header.Length}.");
                }

                rows.Add(Array.AsReadOnly(cells));
                dataLineIndex++;
            }

            tables.Add((Array.AsReadOnly(header), rows.AsReadOnly()));
            lineIndex = dataLineIndex - 1;
        }

        if (tables.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one Markdown table under heading '{heading}', but found {tables.Count}.");
        }

        return tables[0];
    }

    private static bool IsDelimiterCell(string cell)
    {
        string candidate = cell;
        if (candidate.StartsWith(':'))
        {
            candidate = candidate[1..];
        }

        if (candidate.EndsWith(':'))
        {
            candidate = candidate[..^1];
        }

        return candidate.Length >= 3 && candidate.All(static character => character == '-');
    }

    private static string NormalizeHeadingTitle(string title)
    {
        string candidate = title.Trim();
        int lastNonHash = candidate.Length - 1;
        while (lastNonHash >= 0 && candidate[lastNonHash] == '#')
        {
            lastNonHash--;
        }

        if (lastNonHash >= 0 && lastNonHash < candidate.Length - 1 && char.IsWhiteSpace(candidate[lastNonHash]))
        {
            candidate = candidate[..lastNonHash].TrimEnd();
        }

        return candidate;
    }

    private static bool TryReadAtxHeading(string line, out int level, out string title)
    {
        int offset = CountLeadingSpaces(line);
        if (offset > 3)
        {
            level = 0;
            title = string.Empty;
            return false;
        }

        string trimmed = line[offset..];
        level = 0;
        while (level < trimmed.Length && level < 6 && trimmed[level] == '#')
        {
            level++;
        }

        if (level == 0 || (level < trimmed.Length && !char.IsWhiteSpace(trimmed[level])))
        {
            title = string.Empty;
            return false;
        }

        title = NormalizeHeadingTitle(trimmed[level..]);
        return title.Length > 0;
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

        length = 0;
        while (length < trimmed.Length && trimmed[length] == character)
        {
            length++;
        }

        if (length < 3)
        {
            hasOnlyTrailingWhitespace = false;
            return false;
        }

        hasOnlyTrailingWhitespace = string.IsNullOrWhiteSpace(trimmed[length..]);
        return true;
    }

    private static bool TrySplitTableRow(string line, out string[] cells)
    {
        int offset = CountLeadingSpaces(line);
        if (offset > 3 || line.StartsWith('\t'))
        {
            cells = [];
            return false;
        }

        string trimmed = line[offset..].TrimEnd();
        if (trimmed.Length < 2 || trimmed[0] != '|' || trimmed[^1] != '|')
        {
            cells = [];
            return false;
        }

        var result = new List<string>();
        int cellStart = 1;
        int codeDelimiterLength = 0;
        for (int index = 1; index < trimmed.Length - 1; index++)
        {
            if (trimmed[index] == '`')
            {
                int runLength = 1;
                while (index + runLength < trimmed.Length && trimmed[index + runLength] == '`')
                {
                    runLength++;
                }

                if (codeDelimiterLength == 0)
                {
                    codeDelimiterLength = runLength;
                }
                else if (codeDelimiterLength == runLength)
                {
                    codeDelimiterLength = 0;
                }

                index += runLength - 1;
                continue;
            }

            if (trimmed[index] == '|' && codeDelimiterLength == 0 && trimmed[index - 1] != '\\')
            {
                result.Add(trimmed[cellStart..index].Trim());
                cellStart = index + 1;
            }
        }

        result.Add(trimmed[cellStart..^1].Trim());
        cells = result.ToArray();
        return true;
    }

    private void IndexStructure()
    {
        char fenceCharacter = '\0';
        int fenceLength = 0;
        bool inHtmlComment = false;
        for (int lineIndex = 0; lineIndex < this._lines.Length; lineIndex++)
        {
            string line = this._lines[lineIndex];
            if (fenceCharacter != '\0')
            {
                this._isExcludedLine[lineIndex] = true;
                if (TryReadFence(line, out char closingCharacter, out int closingLength, out bool closingHasOnlyTrailingWhitespace) &&
                    closingCharacter == fenceCharacter && closingLength >= fenceLength && closingHasOnlyTrailingWhitespace)
                {
                    fenceCharacter = '\0';
                    fenceLength = 0;
                }

                continue;
            }

            if (inHtmlComment)
            {
                this._isExcludedLine[lineIndex] = true;
                if (line.Contains("-->", StringComparison.Ordinal))
                {
                    inHtmlComment = false;
                }

                continue;
            }

            int commentStart = line.IndexOf("<!--", StringComparison.Ordinal);
            if (commentStart >= 0)
            {
                this._isExcludedLine[lineIndex] = true;
                inHtmlComment = line.IndexOf("-->", commentStart + 4, StringComparison.Ordinal) < 0;
                continue;
            }

            if (TryReadFence(line, out char candidateCharacter, out int candidateLength, out bool hasOnlyTrailingWhitespace))
            {
                this._isExcludedLine[lineIndex] = true;
                fenceCharacter = candidateCharacter;
                fenceLength = candidateLength;

                continue;
            }

            if (HasIndentedCodePrefix(line))
            {
                this._isExcludedLine[lineIndex] = true;
                continue;
            }

            if (TryReadAtxHeading(line, out int level, out string? title))
            {
                this._headings.Add((lineIndex, level, title));
            }
        }
    }

    private (int Start, int End) GetSectionBounds(string heading)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(heading);

        List<(int LineIndex, int Level, string Title)> matches = this._headings
            .Where(candidate => string.Equals(candidate.Title, heading, StringComparison.Ordinal))
            .ToList();
        if (matches.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one ATX heading named '{heading}', but found {matches.Count}.");
        }

        (int lineIndex, int level, _) = matches[0];
        int end = this._lines.Length;
        foreach ((int candidateLineIndex, int candidateLevel, _) in this._headings)
        {
            if (candidateLineIndex > lineIndex && candidateLevel <= level)
            {
                end = candidateLineIndex;
                break;
            }
        }

        return (lineIndex + 1, end);
    }

    private static string NormalizeLineEndings(string markdown)
    {
        var normalized = new StringBuilder(markdown.Length);
        int index = 0;
        while (index < markdown.Length)
        {
            char character = markdown[index];
            if (character != '\r')
            {
                normalized.Append(character);
                index++;
                continue;
            }

            // Collapse one or more consecutive carriage returns, optionally followed by a
            // single line feed, into exactly one line break. A lone "\r\n" pair is the
            // ordinary CRLF case; a repeated run such as "\r\r\n" is the corrupted shape a
            // naive LF-to-CRLF materialization step produces when applied to text that is
            // already CRLF (see the repository's CRLF-normalization guidance). Both must
            // collapse to a single "\n" so section extraction is identical regardless of
            // which line-ending pathology produced the source string.
            normalized.Append('\n');
            while (index < markdown.Length && markdown[index] == '\r')
            {
                index++;
            }

            if (index < markdown.Length && markdown[index] == '\n')
            {
                index++;
            }
        }

        return normalized.ToString();
    }
}
