// <copyright file="DaprWorkflowDuplicateInstanceDetector.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using Grpc.Core;

/// <summary>Detects Dapr Workflow duplicate-instance scheduling conflicts.</summary>
internal static class DaprWorkflowDuplicateInstanceDetector
{
    public static bool IsDuplicateInstance(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is RpcException { StatusCode: StatusCode.AlreadyExists })
        {
            return true;
        }

        if (exception is AggregateException aggregate)
        {
            return aggregate.InnerExceptions.Any(IsDuplicateInstance);
        }

        if (exception.InnerException is not null && IsDuplicateInstance(exception.InnerException))
        {
            return true;
        }

        string message = exception.Message;
        if (!Contains(message, "workflow"))
        {
            return false;
        }

        if (Contains(message, "already exists")
            || Contains(message, "already started")
            || Contains(message, "already running")
            || Contains(message, "duplicate"))
        {
            return true;
        }

        bool instanceConflict = Contains(message, "instance")
            && (Contains(message, "409") || Contains(message, "conflict"));
        return instanceConflict;
    }

    private static bool Contains(string value, string fragment)
        => value.Contains(fragment, StringComparison.OrdinalIgnoreCase);
}
