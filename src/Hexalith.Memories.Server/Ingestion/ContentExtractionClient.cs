// <copyright file="ContentExtractionClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

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
            extractedContent = Encoding.UTF8.GetString(input.ContentBytes);
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

    private static bool IsMarkdownContent(string contentType, string sourceUri)
        => contentType.Equals("text/markdown", StringComparison.OrdinalIgnoreCase) ||
           contentType.Equals("text/x-markdown", StringComparison.OrdinalIgnoreCase) ||
           sourceUri.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
           sourceUri.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);
}
