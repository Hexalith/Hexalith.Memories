// <copyright file="RedisExceptionFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests;

using System.Reflection;

using StackExchange.Redis;

/// <summary>Creates Redis exceptions needed by tests without statically consuming experimental API types.</summary>
internal static class RedisExceptionFactory
{
    private static readonly ConstructorInfo ServerExceptionConstructor = typeof(RedisServerException)
        .GetConstructors()
        .Single(static candidate =>
        {
            ParameterInfo[] parameters = candidate.GetParameters();
            return parameters.Length == 3
                && parameters[1].ParameterType == typeof(CommandFlags)
                && parameters[2].ParameterType == typeof(string);
        });

    /// <summary>Creates a server exception with no command flags and the supplied message.</summary>
    /// <param name="message">The simulated Redis server error message.</param>
    /// <returns>The constructed Redis server exception.</returns>
    public static RedisServerException CreateServerException(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        ParameterInfo[] parameters = ServerExceptionConstructor.GetParameters();
        object unspecifiedErrorKind = Enum.ToObject(parameters[0].ParameterType, 0);
        return (RedisServerException)ServerExceptionConstructor.Invoke(
            [unspecifiedErrorKind, CommandFlags.None, message]);
    }
}
