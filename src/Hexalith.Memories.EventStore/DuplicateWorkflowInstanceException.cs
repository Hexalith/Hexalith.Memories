// <copyright file="DuplicateWorkflowInstanceException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Raised when deterministic Dapr Workflow scheduling collides with an existing instance id.</summary>
internal sealed class DuplicateWorkflowInstanceException : Exception
{
    public DuplicateWorkflowInstanceException(string instanceId, Exception innerException)
        : base($"Workflow instance '{instanceId}' already exists.", innerException)
    {
        InstanceId = instanceId;
    }

    public string InstanceId { get; }
}
