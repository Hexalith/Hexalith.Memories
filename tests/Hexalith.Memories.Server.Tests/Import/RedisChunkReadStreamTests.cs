// <copyright file="RedisChunkReadStreamTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Import;

using System.Text;

using Hexalith.Memories.Server.Import;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>Docker-free coverage for the forward-only staged import stream.</summary>
public class RedisChunkReadStreamTests
{
    [Fact]
    public async Task ReadAsync_MultipleChunks_ReadsDeclaredLength()
    {
        IDatabase database = Substitute.For<IDatabase>();
        database.StringGetAsync("stage:chunk:0", Arg.Any<CommandFlags>())
            .Returns((RedisValue)Encoding.UTF8.GetBytes("ab"));
        database.StringGetAsync("stage:chunk:1", Arg.Any<CommandFlags>())
            .Returns((RedisValue)Encoding.UTF8.GetBytes("cde"));
        RedisChunkReadStream stream = new(database, "stage", chunkCount: 2, length: 5);

        stream.CanRead.ShouldBeTrue();
        stream.CanSeek.ShouldBeFalse();
        stream.CanWrite.ShouldBeFalse();
        stream.Length.ShouldBe(5);
        byte[] first = new byte[3];
        byte[] second = new byte[4];

        (await stream.ReadAsync(first)).ShouldBe(3);
        stream.Read(second, 0, second.Length).ShouldBe(2);

        Encoding.UTF8.GetString(first).ShouldBe("abc");
        Encoding.UTF8.GetString(second, 0, 2).ShouldBe("de");
        stream.Position.ShouldBe(5);
        (await stream.ReadAsync(Memory<byte>.Empty)).ShouldBe(0);
        stream.Flush();
        Should.Throw<NotSupportedException>(() => stream.Position = 0);
        Should.Throw<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Should.Throw<NotSupportedException>(() => stream.SetLength(0));
        Should.Throw<NotSupportedException>(() => stream.Write([], 0, 0));
    }

    [Fact]
    public async Task ReadAsync_MissingChunk_ThrowsEndOfStreamException()
    {
        IDatabase database = Substitute.For<IDatabase>();
        database.StringGetAsync("stage:chunk:0", Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        RedisChunkReadStream stream = new(database, "stage", chunkCount: 1, length: 1);

        EndOfStreamException exception = await Should.ThrowAsync<EndOfStreamException>(
            async () => await stream.ReadExactlyAsync(new byte[1]));

        exception.Message.ShouldContain("missing or expired");
    }

    [Fact]
    public async Task ReadAsync_DeclaredLengthExceedsChunks_ThrowsEndOfStreamException()
    {
        IDatabase database = Substitute.For<IDatabase>();
        database.StringGetAsync("stage:chunk:0", Arg.Any<CommandFlags>())
            .Returns((RedisValue)Encoding.UTF8.GetBytes("a"));
        RedisChunkReadStream stream = new(database, "stage", chunkCount: 1, length: 2);

        EndOfStreamException exception = await Should.ThrowAsync<EndOfStreamException>(
            async () => await stream.ReadExactlyAsync(new byte[2]));

        exception.Message.ShouldContain("ended before its declared length");
    }

    [Fact]
    public async Task ReadAsync_CancelledBeforeFetch_ThrowsOperationCanceledException()
    {
        IDatabase database = Substitute.For<IDatabase>();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        RedisChunkReadStream stream = new(database, "stage", chunkCount: 1, length: 1);

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await stream.ReadExactlyAsync(new byte[1], cancellation.Token));

        await database.DidNotReceive()
            .StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }
}
