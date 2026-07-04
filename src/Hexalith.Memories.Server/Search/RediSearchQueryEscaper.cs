// <copyright file="RediSearchQueryEscaper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using System.Text;

/// <summary>Escapes user-controlled RediSearch dialect-2 query values by query context.</summary>
internal static class RediSearchQueryEscaper
{
    /// <summary>Escapes a value used as TEXT/free-text query content.</summary>
    /// <param name="input">The raw text value.</param>
    /// <returns>The escaped text value.</returns>
    internal static string EscapeText(string input) => Escape(input);

    /// <summary>Escapes a value used inside a TAG filter body.</summary>
    /// <param name="input">The raw tag value.</param>
    /// <returns>The escaped tag value.</returns>
    internal static string EscapeTag(string input) => Escape(input);

    /// <summary>Escapes an attribute key/value pair stored as a single <c>key=value</c> TAG value.</summary>
    /// <param name="key">The raw attribute key.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <returns>The escaped composite tag value.</returns>
    internal static string EscapeTagComposite(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        return $"{EscapeTag(key.Trim())}\\={EscapeTag(value.Trim())}";
    }

    private static string Escape(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        int firstEscapableIndex = -1;
        for (int i = 0; i < input.Length; i++)
        {
            if (IsEscapable(input[i]))
            {
                firstEscapableIndex = i;
                break;
            }
        }

        if (firstEscapableIndex < 0)
        {
            return input;
        }

        var builder = new StringBuilder(input.Length + 8);
        _ = builder.Append(input, 0, firstEscapableIndex);
        for (int i = firstEscapableIndex; i < input.Length; i++)
        {
            char character = input[i];
            if (IsEscapable(character))
            {
                _ = builder.Append('\\');
            }

            _ = builder.Append(character);
        }

        return builder.ToString();
    }

    private static bool IsEscapable(char character)
        => character is '\\'
            or '@'
            or ':'
            or '('
            or ')'
            or '['
            or ']'
            or '{'
            or '}'
            or '|'
            or '-'
            or '~'
            or '*'
            or '%'
            or '='
            or '>'
            or '$'
            or '"'
            or '\''
            or ','
            or '!'
            or '^'
            or '?'
            or '#'
            or ';'
            or '.'
            or '<'
            or '+'
            or '/'
            or '&'
            or '`';
}
