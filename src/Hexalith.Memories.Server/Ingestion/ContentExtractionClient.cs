// <copyright file="ContentExtractionClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

using Hexalith.Memories.Contracts.V1;

/// <summary>In-process content extraction client using Kreuzberg (Rust core via P/Invoke).</summary>
public sealed class ContentExtractionClient : IContentExtractionClient
{
    /// <inheritdoc/>
    public async Task<ExtractionResult> ExtractAsync(ExtractionInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.SourceUri);
        ArgumentNullException.ThrowIfNull(input.ContentBytes);

        string contentType = string.IsNullOrWhiteSpace(input.ContentType)
            ? "application/octet-stream"
            : input.ContentType;

        string extractedContent;
        if (IsMarkdownContent(contentType, input.SourceUri))
        {
            extractedContent = DecodeMarkdownBytes(input.ContentBytes, contentType);
        }
        else
        {
            Kreuzberg.ExtractionConfig config = new();
            Kreuzberg.ExtractionResult kreuzbergResult = await Kreuzberg.KreuzbergClient
                .ExtractBytesAsync(input.ContentBytes, contentType, config, cancellationToken)
                .ConfigureAwait(false);

            extractedContent = kreuzbergResult.Content;
        }

        if (string.IsNullOrWhiteSpace(extractedContent))
        {
            throw new InvalidOperationException(
                $"Kreuzberg returned empty content for '{input.SourceUri}' " +
                $"(content type: {contentType}). " +
                "The file may be corrupt or in an unsupported format.");
        }

        string contentHash = ComputeSha256(extractedContent);

        return new ExtractionResult(
            extractedContent,
            contentHash,
            DateTimeOffset.UtcNow);
    }

    private static string ComputeSha256(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    // Story 15.6 code review patch: parse the MIME type properly (so `text/markdown; charset=utf-8`
    // routes through the raw-bytes path); inspect the URI's local path so a query string or fragment
    // does not hide a `.md`/`.markdown` extension; guard against null/empty source URIs.
    internal static bool IsMarkdownContent(string contentType, string? sourceUri)
    {
        if (TryParseMediaType(contentType) is { MediaType: string mediaType }
            && (string.Equals(mediaType, "text/markdown", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mediaType, "text/x-markdown", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(sourceUri))
        {
            return false;
        }

        string pathSegment = TryExtractUriPath(sourceUri) ?? sourceUri;
        return pathSegment.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || pathSegment.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);
    }

    // Story 15.6 code review patch: a UTF-8 BOM that leaks into extractedContent silently diverges
    // every downstream hash and embedding from the BOMless equivalent. Non-UTF-8 markdown (UTF-16,
    // Windows-1252) was previously decoded as U+FFFD-littered mojibake. Respect the declared charset
    // when one is provided, fall back to a UTF-8 BOM-skipping decode otherwise.
    internal static string DecodeMarkdownBytes(byte[] contentBytes, string contentType)
    {
        Encoding encoding = ResolveDeclaredEncoding(contentType) ?? Encoding.UTF8;

        ReadOnlySpan<byte> bytes = contentBytes;
        ReadOnlySpan<byte> preamble = encoding.Preamble;
        if (preamble.Length > 0
            && bytes.Length >= preamble.Length
            && bytes[..preamble.Length].SequenceEqual(preamble))
        {
            bytes = bytes[preamble.Length..];
        }

        return encoding.GetString(bytes);
    }

    private static Encoding? ResolveDeclaredEncoding(string contentType)
    {
        if (TryParseMediaType(contentType) is not { CharSet: { Length: > 0 } charset })
        {
            return null;
        }

        try
        {
            return Encoding.GetEncoding(charset.Trim().Trim('"'));
        }
        catch (ArgumentException)
        {
            // Unknown / unsupported charset name — fall back to UTF-8 rather than throwing on input.
            return null;
        }
    }

    private static MediaTypeHeaderValue? TryParseMediaType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        return MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue? parsed) ? parsed : null;
    }

    private static string? TryExtractUriPath(string sourceUri)
    {
        if (Uri.TryCreate(sourceUri, UriKind.Absolute, out Uri? absolute))
        {
            return absolute.LocalPath;
        }

        // Strip a query/fragment if present so a relative URI like "notes.md?v=1" still matches.
        int queryStart = sourceUri.IndexOfAny(['?', '#']);
        return queryStart >= 0 ? sourceUri[..queryStart] : sourceUri;
    }
}
