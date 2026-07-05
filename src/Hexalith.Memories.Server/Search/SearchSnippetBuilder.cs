// <copyright file="SearchSnippetBuilder.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

/// <summary>Builds bounded, source-attributable search snippets.</summary>
internal static class SearchSnippetBuilder
{
    internal const int MaxSnippetLength = 200;

    internal static string FromStoredContent(string content)
    {
        if (content.Length <= MaxSnippetLength)
        {
            return content;
        }

        int lastSpace = content.LastIndexOf(' ', MaxSnippetLength);
        int cutoff = lastSpace > 0 ? lastSpace : MaxSnippetLength;
        return content[..cutoff] + "...";
    }

    internal static string FromHighlightedContent(string? highlightedContent, string storedContent)
    {
        string candidate = string.IsNullOrWhiteSpace(highlightedContent)
            ? storedContent
            : highlightedContent;

        return FromStoredContent(candidate);
    }
}
