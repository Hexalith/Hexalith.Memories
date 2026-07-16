// <copyright file="CountingStream.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Export;

/// <summary>
/// Story 8.3: thin <see cref="Stream"/> decorator that counts bytes written and fires a callback
/// whenever the total crosses a 64 KiB boundary. Used by the CLI export commands to emit
/// progress updates to stderr without parsing the JSON payload (byte-count progress is
/// test-deterministic and sidesteps the depth-counting pitfalls of an incremental JSON reader).
/// </summary>
internal sealed class CountingStream : Stream
{
    private const long NotificationBoundary = 64L * 1024L;

    private readonly Stream _inner;
    private readonly Action<long> _onBoundaryCrossed;
    private readonly bool _leaveOpen;
    private long _bytesWritten;
    private long _nextBoundary = NotificationBoundary;

    public CountingStream(Stream inner, Action<long> onBoundaryCrossed, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(onBoundaryCrossed);
        _inner = inner;
        _onBoundaryCrossed = onBoundaryCrossed;
        _leaveOpen = leaveOpen;
    }

    public long BytesWritten => _bytesWritten;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => _inner.CanWrite;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _bytesWritten;
        set => throw new NotSupportedException();
    }

    public override void Flush() => _inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        _inner.Write(buffer, offset, count);
        Advance(count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        Advance(count);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        Advance(buffer.Length);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _inner.Write(buffer);
        Advance(buffer.Length);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_leaveOpen)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_leaveOpen)
        {
            await _inner.DisposeAsync().ConfigureAwait(false);
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }

    private void Advance(int count)
    {
        if (count <= 0)
        {
            return;
        }

        _bytesWritten += count;
        while (_bytesWritten >= _nextBoundary)
        {
            _onBoundaryCrossed(_bytesWritten);
            _nextBoundary += NotificationBoundary;
        }
    }
}
