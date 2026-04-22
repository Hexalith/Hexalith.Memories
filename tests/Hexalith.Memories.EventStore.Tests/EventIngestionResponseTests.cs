// <copyright file="EventIngestionResponseTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using Hexalith.Memories.EventStore;

using Shouldly;

public sealed class EventIngestionResponseTests
{
    [Fact]
    public void Accepted_PopulatesInstanceId()
    {
        EventIngestionResponse response = EventIngestionResponse.Accepted("wf-1");

        response.Status.ShouldBe("accepted");
        response.InstanceId.ShouldBe("wf-1");
        response.WasDuplicate.ShouldBeFalse();
    }

    [Fact]
    public void Duplicate_OmitsInstanceId_SetsWasDuplicate()
    {
        EventIngestionResponse response = EventIngestionResponse.Duplicate();

        response.Status.ShouldBe("duplicate");
        response.InstanceId.ShouldBeNull();
        response.WasDuplicate.ShouldBeTrue();
    }

    [Fact]
    public void Drop_OmitsInstanceId()
    {
        EventIngestionResponse response = EventIngestionResponse.Drop("unknown-source", "reason");

        response.InstanceId.ShouldBeNull();
        response.WasDuplicate.ShouldBeFalse();
        response.Reason.ShouldBe("reason");
    }

    [Fact]
    public void Invalid_SetsInvalidCloudEventStatus()
    {
        EventIngestionResponse response = EventIngestionResponse.Invalid("why");

        response.Status.ShouldBe("invalid-cloudevent");
        response.InstanceId.ShouldBeNull();
        response.Reason.ShouldBe("why");
    }
}
