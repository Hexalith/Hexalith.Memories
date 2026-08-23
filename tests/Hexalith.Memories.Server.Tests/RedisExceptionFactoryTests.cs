// <copyright file="RedisExceptionFactoryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests;

using Shouldly;

using StackExchange.Redis;

/// <summary>Tests the StackExchange.Redis compatibility exception factory.</summary>
public sealed class RedisExceptionFactoryTests
{
    /// <summary>Verifies that the factory preserves the message and uses no command flags.</summary>
    [Fact]
    public void CreateServerException_ShouldPreserveMessageAndUseNoCommandFlags()
    {
        const string Message = "ERR simulated Redis failure";

        RedisServerException exception = RedisExceptionFactory.CreateServerException(Message);

        exception.Message.ShouldBe(Message);
        exception.Flags.ShouldBe(CommandFlags.None);
    }
}
