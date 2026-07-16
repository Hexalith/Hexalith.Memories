// <copyright file="CountingStreamTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Export;

using Hexalith.Memories.Cli.Export;

using Shouldly;

/// <summary>Story 8.3 — byte-count progress boundary behavior.</summary>
public sealed class CountingStreamTests
{
    [Fact]
    public void FiresCallback_WhenWritesCross64KiBBoundary()
    {
        MemoryStream inner = new();
        List<long> notifications = new();
        using CountingStream counter = new(inner, notifications.Add, leaveOpen: true);

        byte[] buffer = new byte[65 * 1024];
        counter.Write(buffer, 0, buffer.Length);

        notifications.Count.ShouldBe(1);
        notifications[0].ShouldBe(65L * 1024L);
    }

    [Fact]
    public async Task MultipleBoundariesCrossed_FireSequentially()
    {
        MemoryStream inner = new();
        List<long> notifications = new();
        await using CountingStream counter = new(inner, notifications.Add, leaveOpen: true);

        byte[] buffer = new byte[200 * 1024]; // crosses 64K, 128K, 192K
        await counter.WriteAsync(buffer, CancellationToken.None);

        notifications.Count.ShouldBe(3);
        notifications[0].ShouldBe(200L * 1024L);
        notifications[1].ShouldBe(200L * 1024L);
        notifications[2].ShouldBe(200L * 1024L);
    }

    [Fact]
    public void SmallWrites_BelowBoundary_DoNotFire()
    {
        MemoryStream inner = new();
        List<long> notifications = new();
        using CountingStream counter = new(inner, notifications.Add, leaveOpen: true);

        byte[] buffer = new byte[1024];
        counter.Write(buffer, 0, buffer.Length);
        counter.Write(buffer, 0, buffer.Length);

        notifications.ShouldBeEmpty();
        counter.BytesWritten.ShouldBe(2048L);
    }

    [Fact]
    public void LeaveOpen_DoesNotDisposeInnerStream()
    {
        MemoryStream inner = new();
        CountingStream counter = new(inner, _ => { }, leaveOpen: true);
        counter.Dispose();

        Should.NotThrow(() => inner.WriteByte(1));
    }
}
