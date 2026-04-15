// <copyright file="IngestionContentTypeSupport.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>
/// Shared content-type helpers for URL and directory ingestion.
/// </summary>
internal static class IngestionContentTypeSupport
{
    private static readonly HashSet<string> SupportedMediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/markdown",
        "text/plain",
        "text/html",
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-powerpoint",
        "text/csv",
        "application/json",
        "application/rtf",
        "application/epub+zip",
    };

    internal static string InferFromPath(string path)
        => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".md" => "text/markdown",
            ".txt" => "text/plain",
            ".html" or ".htm" => "text/html",
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".rtf" => "application/rtf",
            ".epub" => "application/epub+zip",
            _ => "application/octet-stream",
        };

    internal static bool IsSupported(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        string mediaType = contentType.Split(';', 2, StringSplitOptions.TrimEntries)[0];
        return SupportedMediaTypes.Contains(mediaType);
    }
}