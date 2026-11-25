// <copyright file="RedisStackFixtureConnectWhenReadyTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

using StackExchange.Redis;

/// <summary>Handshake retry for Redis Stack fixtures must fail closed when nothing answers.</summary>
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
}
