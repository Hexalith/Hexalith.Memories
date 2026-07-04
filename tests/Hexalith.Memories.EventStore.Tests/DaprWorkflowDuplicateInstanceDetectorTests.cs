// <copyright file="DaprWorkflowDuplicateInstanceDetectorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using Grpc.Core;

using Hexalith.Memories.EventStore;

using Shouldly;

public sealed class DaprWorkflowDuplicateInstanceDetectorTests
{
    [Fact]
    public void IsDuplicateInstance_AlreadyExistsRpcException_ReturnsTrue()
    {
        RpcException exception = new(new Status(StatusCode.AlreadyExists, "workflow instance already exists"));

        DaprWorkflowDuplicateInstanceDetector.IsDuplicateInstance(exception).ShouldBeTrue();
    }

    [Fact]
    public void IsDuplicateInstance_NestedWorkflowConflictMessage_ReturnsTrue()
    {
        InvalidOperationException exception = new(
            "Dapr workflow scheduling failed",
            new InvalidOperationException("workflow instance conflict: 409"));

        DaprWorkflowDuplicateInstanceDetector.IsDuplicateInstance(exception).ShouldBeTrue();
    }

    [Fact]
    public void IsDuplicateInstance_UnavailableRpcException_ReturnsFalse()
    {
        RpcException exception = new(new Status(StatusCode.Unavailable, "sidecar unavailable"));

        DaprWorkflowDuplicateInstanceDetector.IsDuplicateInstance(exception).ShouldBeFalse();
    }

    [Fact]
    public void IsDuplicateInstance_GenericWorkflowConflict_ReturnsFalse()
    {
        InvalidOperationException exception = new("workflow conflict while loading metadata");

        DaprWorkflowDuplicateInstanceDetector.IsDuplicateInstance(exception).ShouldBeFalse();
    }
}
