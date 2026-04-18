// <copyright file="AuditEventSchemaVersioningTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System.Collections.Generic;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>
/// Story 7.5 Task 9.5 — asserts <see cref="AccessTelemetryEvent.SchemaVersion"/> is <c>1</c> for every
/// newly minted event AND that <see cref="MemoriesJsonContext.Options"/> serializes the record with the
/// pinned V1 JSON shape (camelCase keys, <c>schemaVersion</c> always present, optional fields omitted
/// or null per record contract). Complements the frozen-manifest field-name guard in
/// <see cref="AccessTelemetryEventSchemaTests"/> by exercising the serializer path consumers will use.
/// </summary>
public sealed class AuditEventSchemaVersioningTests
{
    [Fact]
    public void Default_SchemaVersion_IsOne()
    {
        AccessTelemetryEvent @event = NewEvent();

        @event.SchemaVersion.ShouldBe(1);
        @event.SchemaVersion.ShouldBe(AccessTelemetryEvent.CurrentSchemaVersion);
    }

    [Fact]
    public void Serialize_EmitsSchemaVersionField()
    {
        AccessTelemetryEvent @event = NewEvent();

        string json = JsonSerializer.Serialize(@event, MemoriesJsonContext.Options);

        using JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("eventId").GetInt32().ShouldBe(7501);
        doc.RootElement.GetProperty("tenantId").GetString().ShouldBe("acme");
        doc.RootElement.GetProperty("operationType").GetString().ShouldBe("search");
        doc.RootElement.GetProperty("outcome").GetString().ShouldBe("ok");
    }

    [Fact]
    public void Roundtrip_PreservesAllFields()
    {
        AccessTelemetryEvent original = NewEvent() with
        {
            CaseId = "case-1",
            ResultCount = 42,
            ErrorCode = null,
            TraceId = "00-1234567890abcdef1234567890abcdef-1234567890abcdef-01",
            SpanId = "1234567890abcdef",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        AccessTelemetryEvent? roundtrip = JsonSerializer.Deserialize<AccessTelemetryEvent>(json, MemoriesJsonContext.Options);

        roundtrip.ShouldNotBeNull();
        roundtrip.SchemaVersion.ShouldBe(original.SchemaVersion);
        roundtrip.EventId.ShouldBe(original.EventId);
        roundtrip.Timestamp.ShouldBe(original.Timestamp);
        roundtrip.TenantId.ShouldBe(original.TenantId);
        roundtrip.OperationType.ShouldBe(original.OperationType);
        roundtrip.CaseId.ShouldBe(original.CaseId);
        roundtrip.User.ShouldBe(original.User);
        roundtrip.ResultCount.ShouldBe(original.ResultCount);
        roundtrip.DurationMs.ShouldBe(original.DurationMs);
        roundtrip.Outcome.ShouldBe(original.Outcome);
        roundtrip.ErrorCode.ShouldBe(original.ErrorCode);
        roundtrip.TraceId.ShouldBe(original.TraceId);
        roundtrip.SpanId.ShouldBe(original.SpanId);
    }

    [Fact]
    public void Deserialize_MissingOptionalFields_StillParses()
    {
        // Additive-field policy: consumers should tolerate events without optional keys. The required
        // fields (schemaVersion, eventId, timestamp, tenantId, operationType, user, durationMs, outcome)
        // MUST be present; the rest can be omitted.
        const string minimalJson = """
{
  "schemaVersion": 1,
  "eventId": 7501,
  "timestamp": "2026-04-18T00:00:00Z",
  "tenantId": "acme",
  "operationType": "search",
  "user": "anonymous",
  "durationMs": 12,
  "outcome": "ok"
}
""";

        AccessTelemetryEvent? parsed = JsonSerializer.Deserialize<AccessTelemetryEvent>(minimalJson, MemoriesJsonContext.Options);

        parsed.ShouldNotBeNull();
        parsed.SchemaVersion.ShouldBe(1);
        parsed.CaseId.ShouldBeNull();
        parsed.ResultCount.ShouldBeNull();
        parsed.ErrorCode.ShouldBeNull();
        parsed.TraceId.ShouldBeNull();
        parsed.SpanId.ShouldBeNull();
    }

    private static AccessTelemetryEvent NewEvent() => new()
    {
        EventId = 7501,
        Timestamp = "2026-04-18T12:34:56Z",
        TenantId = "acme",
        OperationType = "search",
        User = "anonymous",
        QueryParams = new Dictionary<string, object?> { ["axis"] = "hybrid" },
        DurationMs = 17,
        Outcome = "ok",
    };
}
