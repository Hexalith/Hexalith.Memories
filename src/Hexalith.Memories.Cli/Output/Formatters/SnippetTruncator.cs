// <copyright file="SnippetTruncator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

/// <summary>Trims a content snippet to a fixed character budget for one-line rendering.</summary>
internal static class SnippetTruncator
{
    public const int DefaultMaxLength = 80;

    public static string Truncate(string? value, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string singleLine = value.ReplaceLineEndings(" ");
        if (singleLine.Length <= maxLength)
        {
            return singleLine;
        }

        int cut = maxLength;
        if (char.IsHighSurrogate(singleLine[cut - 1]))
        {
            cut--;
        }

        return singleLine[..cut];
    }
}
