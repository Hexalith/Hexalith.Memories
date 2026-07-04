// <copyright file="RediSearchErrorClassifier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using StackExchange.Redis;

/// <summary>Classifies bounded RediSearch server error messages without exposing raw query text.</summary>
internal static class RediSearchErrorClassifier
{
    internal static bool IsMissingIndexError(RedisServerException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Message.Contains("no such index", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("Unknown Index name", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsQuerySyntaxError(RedisServerException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        string message = exception.Message;
        return message.Contains("syntax error", StringComparison.OrdinalIgnoreCase)
            || message.Contains("could not parse query", StringComparison.OrdinalIgnoreCase)
            || message.Contains("invalid query", StringComparison.OrdinalIgnoreCase)
            || message.Contains("parse error", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unexpected token", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsVectorDimensionMismatchError(RedisServerException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        string message = exception.Message;
        return message.Contains("blob size", StringComparison.OrdinalIgnoreCase)
            || message.Contains("vector size", StringComparison.OrdinalIgnoreCase)
            || (message.Contains("vector", StringComparison.OrdinalIgnoreCase)
                && message.Contains("dimension", StringComparison.OrdinalIgnoreCase));
    }
}
