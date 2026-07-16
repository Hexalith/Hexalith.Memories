// <copyright file="RedisChunkReadStream.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

using StackExchange.Redis;

/// <summary>Forward-only stream that fetches one staged Redis chunk at a time.</summary>
internal sealed class RedisChunkReadStream : Stream
{
    private readonly int _chunkCount;
    private readonly IDatabase _database;
    private readonly string _stagingKey;
    private byte[] _chunk = [];
    private int _chunkIndex;
    private int _chunkOffset;
    private long _position;

    internal RedisChunkReadStream(IDatabase database, string stagingKey, int chunkCount, long length)
    {
        _database = database;
        _stagingKey = stagingKey;
        _chunkCount = chunkCount;
        Length = length;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length { get; }

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
        => ReadAsync(buffer.AsMemory(offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.Length == 0 || _position >= Length)
        {
            return 0;
        }

        int written = 0;
        while (written < buffer.Length && _position < Length)
        {
            if (_chunkOffset >= _chunk.Length)
            {
                if (_chunkIndex >= _chunkCount)
                {
                    throw new EndOfStreamException($"Staged import '{_stagingKey}' ended before its declared length.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                RedisValue value = await _database
                    .StringGetAsync(RedisImportStagingStore.BuildChunkKey(_stagingKey, _chunkIndex++))
                    .ConfigureAwait(false);
                _chunk = value.IsNull
                    ? throw new EndOfStreamException($"A chunk of staged import '{_stagingKey}' is missing or expired.")
                    : (byte[])value!;
                _chunkOffset = 0;
            }

            int available = Math.Min(_chunk.Length - _chunkOffset, buffer.Length - written);
            _chunk.AsMemory(_chunkOffset, available).CopyTo(buffer[written..]);
            _chunkOffset += available;
            written += available;
            _position += available;
        }

        return written;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
