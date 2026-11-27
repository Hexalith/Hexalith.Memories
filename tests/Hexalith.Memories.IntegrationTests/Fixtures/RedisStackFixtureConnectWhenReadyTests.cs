// <copyright file="RedisStackFixtureConnectWhenReadyTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;
using System.Net.Sockets;

using DotNet.Testcontainers.Containers;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>Owned Redis Stack startup must fail within its budget and report the failing phase.</summary>
[Trait("Category", "Integration")]
public sealed class RedisStackFixtureConnectWhenReadyTests
{
    [Fact]
    public async Task ConnectWhenReadyAsync_UnreachableLoopbackPort_ThrowsBeforeDeadline()
    {
        RedisConnectionException error = await Should.ThrowAsync<RedisConnectionException>(
            () => RedisStackFixture.ConnectWhenReadyAsync("127.0.0.1:59999", timeout: TimeSpan.FromSeconds(3)));

        error.FailureType.ShouldBe(ConnectionFailureType.UnableToConnect);
    }

    [Fact]
    public async Task StartOwnedContainerAsync_StalledDockerStart_CancelsAndReportsState()
    {
        IContainer container = Substitute.For<IContainer>();
        container.Id.Returns("stalled-redis");
        container.State.Returns(TestcontainersStates.Running);
        container.StartAsync(Arg.Any<CancellationToken>()).Returns(call =>
            Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>()));

        CancellationToken token = TestContext.Current.CancellationToken;
        TimeoutException error = await Should.ThrowAsync<TimeoutException>(() =>
            RedisStackFixture.StartOwnedContainerAsync(container, token, startupTimeout: TimeSpan.FromMilliseconds(100))
                .WaitAsync(TimeSpan.FromSeconds(5), token));

        error.Message.ShouldContain("Docker startup");
        error.Message.ShouldContain("container stalled-redis, last Docker state Running, endpoint unavailable");
        error.InnerException.ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task StartOwnedContainerAsync_UnavailableMappedPort_ReportsLoopbackEndpoint()
    {
        // Reserve an ephemeral port without listening so another process cannot claim it.
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        ushort port = (ushort)((IPEndPoint)socket.LocalEndPoint!).Port;
        IContainer container = Substitute.For<IContainer>();
        container.Id.Returns("unavailable-redis");
        container.State.Returns(TestcontainersStates.Running);
        container.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        container.GetMappedPublicPort(6379).Returns(port);

        CancellationToken token = TestContext.Current.CancellationToken;
        TimeoutException error = await Should.ThrowAsync<TimeoutException>(() =>
            RedisStackFixture.StartOwnedContainerAsync(container, token, startupTimeout: TimeSpan.FromSeconds(2))
                .WaitAsync(TimeSpan.FromSeconds(8), token));

        error.Message.ShouldContain("Redis handshake");
        error.Message.ShouldContain($"container unavailable-redis, last Docker state Running, endpoint 127.0.0.1:{port}");
    }
}
