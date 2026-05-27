// <copyright file="IndexingActivityRecordSerializationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;

using Shouldly;

public class IndexingActivityRecordSerializationTests
{
    // --- ConsistencyInput ---

    [Fact]
    public void ConsistencyInput_RoundTrip()
    {
        var original = new ConsistencyInput("mu-001", "tenant-1");
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ConsistencyInput? deserialized = JsonSerializer.Deserialize<ConsistencyInput>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);
        json2.ShouldBe(json1);
    }

    // --- ConsistencyResult ---

    [Fact]
    public void ConsistencyResult_RoundTrip_AllTrue()
    {
        var original = new ConsistencyResult(true, true, true);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ConsistencyResult? deserialized = JsonSerializer.Deserialize<ConsistencyResult>(json, MemoriesJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.SyntacticExists.ShouldBeTrue();
        deserialized.SemanticExists.ShouldBeTrue();
        deserialized.GraphExists.ShouldBeTrue();
    }

    [Fact]
    public void ConsistencyResult_RoundTrip_MixedValues()
    {
        var original = new ConsistencyResult(true, false, true);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ConsistencyResult? deserialized = JsonSerializer.Deserialize<ConsistencyResult>(json, MemoriesJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.SyntacticExists.ShouldBeTrue();
        deserialized.SemanticExists.ShouldBeFalse();
        deserialized.GraphExists.ShouldBeTrue();
    }

    // --- CleanupInput ---

    [Fact]
    public void CleanupInput_RoundTrip()
    {
        var original = new CleanupInput("mu-001", "tenant-1");
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CleanupInput? deserialized = JsonSerializer.Deserialize<CleanupInput>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);
        json2.ShouldBe(json1);
    }
}
