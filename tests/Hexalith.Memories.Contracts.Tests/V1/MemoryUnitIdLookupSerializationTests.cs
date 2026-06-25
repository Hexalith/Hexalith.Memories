// <copyright file="MemoryUnitIdLookupSerializationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Story 18.5 — wire-shape guard for the additive <see cref="MemoryUnitIdLookupResponse"/> contract.</summary>
public class MemoryUnitIdLookupSerializationTests
{
    [Fact]
    public void Serialize_EmitsCamelCaseMemoryUnitId()
    {
        MemoryUnitIdLookupResponse response = new() { MemoryUnitId = "mu-123" };

        string json = JsonSerializer.Serialize(response, MemoriesJsonContext.Options);

        json.ShouldContain("\"memoryUnitId\":\"mu-123\"");
    }

    [Fact]
    public void RoundTrip_PreservesMemoryUnitId()
    {
        MemoryUnitIdLookupResponse original = new() { MemoryUnitId = "mu-abc-001" };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        MemoryUnitIdLookupResponse? deserialized = JsonSerializer.Deserialize<MemoryUnitIdLookupResponse>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.MemoryUnitId.ShouldBe("mu-abc-001");
    }

    [Fact]
    public void Deserialize_FromCamelCaseWire_BindsMemoryUnitId()
    {
        const string wire = "{\"memoryUnitId\":\"mu-from-wire\"}";

        MemoryUnitIdLookupResponse? deserialized = JsonSerializer.Deserialize<MemoryUnitIdLookupResponse>(wire, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.MemoryUnitId.ShouldBe("mu-from-wire");
    }
}
