// <copyright file="CloudEventEnvelopeParserTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System.Text.Json;

using Hexalith.Memories.EventStore;

using Shouldly;

public sealed class CloudEventEnvelopeParserTests
{
    [Fact]
    public void Parse_CompleteEnvelope_PopulatesAllFields()
    {
        JsonElement root = JsonDocument.Parse("""
            {
              "id": "evt-1",
              "source": "/enterprise/hr",
              "type": "MyApp.Claims.ClaimSubmittedV2",
              "subject": "aggregate-42",
              "time": "2026-04-22T10:00:00Z",
              "datacontenttype": "application/json",
              "data": { "claimId": "abc" }
            }
            """).RootElement;

        CloudEventEnvelope envelope = CloudEventEnvelopeParser.Parse(root);

        envelope.Id.ShouldBe("evt-1");
        envelope.Source.ShouldBe("/enterprise/hr");
        envelope.Type.ShouldBe("MyApp.Claims.ClaimSubmittedV2");
        envelope.Subject.ShouldBe("aggregate-42");
        envelope.Time.ShouldBe("2026-04-22T10:00:00Z");
        envelope.DataContentType.ShouldBe("application/json");
        envelope.Data.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Fact]
    public void Parse_SubjectAbsent_ReturnsNullSubject()
    {
        JsonElement root = JsonDocument.Parse("""
            {
              "id": "evt-1",
              "source": "/x",
              "type": "a.b.c",
              "data": {}
            }
            """).RootElement;

        CloudEventEnvelope envelope = CloudEventEnvelopeParser.Parse(root);

        envelope.Subject.ShouldBeNull();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("source")]
    [InlineData("type")]
    public void Parse_RequiredFieldMissing_ThrowsTypedException(string missingField)
    {
        Dictionary<string, object> props = new()
        {
            ["id"] = "evt-1",
            ["source"] = "/x",
            ["type"] = "a.b.c",
            ["data"] = new Dictionary<string, object>(),
        };
        props.Remove(missingField);

        JsonElement root = JsonSerializer.SerializeToElement(props);

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => CloudEventEnvelopeParser.Parse(root));
        ex.Message.ShouldBe($"cloudevent.{missingField} missing");
    }

    [Fact]
    public void Parse_DataMissing_ThrowsTypedException()
    {
        JsonElement root = JsonDocument.Parse("""
            {
              "id": "evt-1",
              "source": "/x",
              "type": "a.b.c"
            }
            """).RootElement;

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => CloudEventEnvelopeParser.Parse(root));
        ex.Message.ShouldBe("cloudevent.data missing");
    }

    [Fact]
    public void Parse_DataNull_ThrowsTypedException()
    {
        JsonElement root = JsonDocument.Parse("""
            {
              "id": "evt-1",
              "source": "/x",
              "type": "a.b.c",
              "data": null
            }
            """).RootElement;

        Should.Throw<InvalidOperationException>(() => CloudEventEnvelopeParser.Parse(root))
            .Message.ShouldBe("cloudevent.data missing");
    }

    [Fact]
    public void Parse_NotAnObject_ThrowsEnvelopeMissing()
    {
        JsonElement root = JsonDocument.Parse("\"just-a-string\"").RootElement;

        Should.Throw<InvalidOperationException>(() => CloudEventEnvelopeParser.Parse(root))
            .Message.ShouldBe("cloudevent.envelope missing");
    }

    [Fact]
    public void Parse_WhitespaceRequiredField_ThrowsTypedException()
    {
        JsonElement root = JsonDocument.Parse("""
            {
              "id": "   ",
              "source": "/x",
              "type": "a.b.c",
              "data": {}
            }
            """).RootElement;

        Should.Throw<InvalidOperationException>(() => CloudEventEnvelopeParser.Parse(root))
            .Message.ShouldBe("cloudevent.id missing");
    }
}
