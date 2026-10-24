namespace Hexalith.Memories.Contracts.V1;

/// <summary>Explicit acknowledgement required before a client treats an admission as revision-aware refresh.</summary>
public static class SourceRefreshProtocol
{
    /// <summary>The authenticated producer acknowledgement header, absent on ordinary/legacy ingestion.</summary>
    public const string AcknowledgementHeader = "X-Hexalith-Source-Refresh";
    /// <summary>The supported revision-publication protocol.</summary>
    public const string Version = "source-refresh-v1";

    /// <summary>Computes the immutable content and revision binding retained in authoritative producer admission.</summary>
    /// <param name="sourceUri">The canonical source URI.</param>
    /// <param name="contentType">The exact submitted MIME content type.</param>
    /// <param name="sourceRevisionNumber">The authoritative monotonic revision.</param>
    /// <param name="metadata">The submitted metadata.</param>
    /// <param name="content">The submitted source bytes.</param>
    /// <returns>The lowercase SHA-256 admission binding.</returns>
    public static string CreateIdentityDigest(string sourceUri, string contentType, long sourceRevisionNumber,
        IReadOnlyDictionary<string, MetadataField> metadata, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(content);
        return Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
        {
            SourceUri = sourceUri, ContentType = contentType, SourceRevisionNumber = sourceRevisionNumber,
            Metadata = metadata.OrderBy(pair => pair.Key, StringComparer.Ordinal),
            ContentHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(content)),
        })));
    }
}
