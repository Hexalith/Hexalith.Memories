// <copyright file="CloudEventToIngestionInputMapperTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;

using Shouldly;

public sealed class CloudEventToIngestionInputMapperTests
{
    private static readonly TenantEventRoute Route = new("tenant-1", "case-1", "Claims");

    private static CloudEventEnvelope Envelope(
        string id = "evt-1",
        string source = "/enterprise/hr",
        string type = "MyApp.Claims.ClaimSubmittedV2",
        string? subject = "aggregate-42",
        string? time = "2026-04-22T10:00:00Z",
        string? contentType = "application/json",
        string data = "{\"claimId\":\"abc\"}")
        => new(id, source, type, subject, time, contentType, JsonDocument.Parse(data).RootElement);

    [Fact]
    public void Map_HappyPath_PopulatesIngestionInputAndMetadata()
    {
        CloudEventEnvelope envelope = Envelope();

        IngestionInput input = CloudEventToIngestionInputMapper.Map(envelope, Route);

        input.TenantId.ShouldBe("tenant-1");
        input.CaseId.ShouldBe("case-1");
        input.SourceUri.ShouldBe("evt-1");
        input.SourceType.ShouldBe(SourceType.Event);
        input.IngestedBy.ShouldBe(CloudEventToIngestionInputMapper.IngestedByEvents);
        input.ContentType.ShouldBe("application/json");
        Encoding.UTF8.GetString(input.ContentBytes!).ShouldBe("{\"claimId\":\"abc\"}");

        input.Metadata[CloudEventToIngestionInputMapper.MetadataCloudEventId].Value.ShouldBe("evt-1");
        input.Metadata[CloudEventToIngestionInputMapper.MetadataCloudEventSource].Value.ShouldBe("/enterprise/hr");
        input.Metadata[CloudEventToIngestionInputMapper.MetadataCloudEventType].Value.ShouldBe("MyApp.Claims.ClaimSubmittedV2");
        input.Metadata[CloudEventToIngestionInputMapper.MetadataCloudEventSubject].Value.ShouldBe("aggregate-42");
        input.Metadata[CloudEventToIngestionInputMapper.MetadataCloudEventTime].Value.ShouldBe("2026-04-22T10:00:00Z");
        input.Metadata[CloudEventToIngestionInputMapper.MetadataEventAggregateType].Value.ShouldBe("Claims");
    }

    [Fact]
    public void Map_SubjectMissing_MetadataShowsExplicitUnset_NoCrash()
    {
        CloudEventEnvelope envelope = Envelope(subject: null);

        IngestionInput input = CloudEventToIngestionInputMapper.Map(envelope, Route);

        input.Metadata[CloudEventToIngestionInputMapper.MetadataCloudEventSubject].Value
            .ShouldBe(CloudEventToIngestionInputMapper.SubjectUnset);
    }

    [Fact]
    public void Map_DataUndefined_Throws()
    {
        CloudEventEnvelope envelope = new(
            "evt-1",
            "/x",
            "a.b.c",
            Subject: null,
            Time: null,
            DataContentType: null,
            Data: default);

        Should.Throw<InvalidOperationException>(() => CloudEventToIngestionInputMapper.Map(envelope, Route))
            .Message.ShouldBe("cloudevent.data missing");
    }

    [Fact]
    public void Map_DataContentTypeAbsent_DefaultsToApplicationJson()
    {
        CloudEventEnvelope envelope = Envelope(contentType: null);

        IngestionInput input = CloudEventToIngestionInputMapper.Map(envelope, Route);

        input.ContentType.ShouldBe("application/json");
    }

    [Fact]
    public void Map_TimeAbsent_MetadataEmptyString()
    {
        CloudEventEnvelope envelope = Envelope(time: null);

        IngestionInput input = CloudEventToIngestionInputMapper.Map(envelope, Route);

        input.Metadata[CloudEventToIngestionInputMapper.MetadataCloudEventTime].Value.ShouldBe(string.Empty);
    }

    [Fact]
    public void Map_SourceUriEqualsCloudEventId_ForIdempotency()
    {
        CloudEventEnvelope envelope = Envelope(id: "cloudevent-xyz");

        IngestionInput input = CloudEventToIngestionInputMapper.Map(envelope, Route);

        input.SourceUri.ShouldBe("cloudevent-xyz");
    }

    [Fact]
    public void Map_IngestedByIsSystemIdentity_NotUserId()
    {
        CloudEventEnvelope envelope = Envelope();

        IngestionInput input = CloudEventToIngestionInputMapper.Map(envelope, Route);

        input.IngestedBy.ShouldBe("events");
    }
}
