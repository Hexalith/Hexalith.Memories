// <copyright file="AccessTelemetryEventSchemaTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>
/// Story 7.5 Task 8.5 — audit schema immutability guard. Asserts
/// <see cref="AccessTelemetryEvent"/> field names against a frozen V1 manifest. A silent rename
/// (<c>tenantId → tenant_id</c>) fails this test loudly — preventing SIEM parser breakage.
/// </summary>
public sealed class AccessTelemetryEventSchemaTests
{
    private static readonly string[] ExpectedV1FieldNames =
    [
        "schemaVersion",
        "eventId",
        "timestamp",
        "tenantId",
        "operationType",
        "caseId",
        "user",
        "queryParams",
        "resultCount",
        "durationMs",
        "outcome",
        "errorCode",
        "traceId",
        "spanId",
    ];

    [Fact]
    public void SchemaVersion_IsOne() => AccessTelemetryEvent.CurrentSchemaVersion.ShouldBe(1);

    [Fact]
    public void V1_FieldNames_AreFrozen()
    {
        string[] actualNames = [.. typeof(AccessTelemetryEvent)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name)
            .OrderBy(n => n, System.StringComparer.Ordinal)];

        string[] expected = [.. ExpectedV1FieldNames.OrderBy(n => n, System.StringComparer.Ordinal)];

        actualNames.ShouldBe(expected);
    }

    [Fact]
    public void AllExpectedFieldsHaveJsonPropertyNames()
    {
        foreach (PropertyInfo prop in typeof(AccessTelemetryEvent).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            JsonPropertyNameAttribute? attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            attr.ShouldNotBeNull($"Property {prop.Name} must have an explicit [JsonPropertyName] attribute (schema-stability guard).");
        }
    }
}
