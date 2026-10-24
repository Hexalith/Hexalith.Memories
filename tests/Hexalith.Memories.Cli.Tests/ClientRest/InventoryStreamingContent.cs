namespace Hexalith.Memories.Cli.Tests.ClientRest;

using System.Net;

/// <summary>Rejects eager HTTP buffering and supplies optional lying response lengths.</summary>
internal sealed class InventoryStreamingContent(byte[] payload, long? reportedLength = null, Action<CancellationToken>? beforeRead = null, Func<Stream>? createStream = null) : HttpContent
{
    internal bool StreamOpened { get; private set; }
    protected override bool TryComputeLength(out long length) { length = reportedLength ?? 0; return reportedLength.HasValue; }
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => throw new InvalidOperationException("Inventory must use headers-only HTTP completion.");
    protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
    {
        StreamOpened = true;
        beforeRead?.Invoke(cancellationToken);
        return Task.FromResult<Stream>(createStream?.Invoke() ?? new MemoryStream(payload, writable: false));
    }
}
