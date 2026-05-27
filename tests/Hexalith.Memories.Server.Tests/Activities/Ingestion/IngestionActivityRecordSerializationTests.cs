// <copyright file="IngestionActivityRecordSerializationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;

using Shouldly;

public class IngestionActivityRecordSerializationTests
{
    // --- ValidateResult ---

    [Fact]
    public void ValidateResult_RoundTrip_Success()
    {
        var original = new ValidateResult(true, null);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ValidateResult? deserialized = JsonSerializer.Deserialize<ValidateResult>(json, MemoriesJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.IsValid.ShouldBeTrue();
        deserialized.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void ValidateResult_RoundTrip_Failure()
    {
        var original = new ValidateResult(false, "TenantId is required");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ValidateResult? deserialized = JsonSerializer.Deserialize<ValidateResult>(json, MemoriesJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.IsValid.ShouldBeFalse();
        deserialized.ErrorMessage.ShouldBe("TenantId is required");
    }

    // --- IdempotencyInput ---

    [Fact]
    public void IdempotencyInput_RoundTrip()
    {
        var original = new IdempotencyInput("file:///doc.pdf", "tenant-1", "case-1");
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IdempotencyInput? deserialized = JsonSerializer.Deserialize<IdempotencyInput>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);
        json2.ShouldBe(json1);
    }

    // --- IdempotencyResult ---

    [Fact]
    public void IdempotencyResult_RoundTrip_NotDuplicate()
    {
        var original = new IdempotencyResult(false, null);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IdempotencyResult? deserialized = JsonSerializer.Deserialize<IdempotencyResult>(json, MemoriesJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.IsDuplicate.ShouldBeFalse();
        deserialized.ExistingMemoryUnitId.ShouldBeNull();
    }

    [Fact]
    public void IdempotencyResult_RoundTrip_Duplicate()
    {
        var original = new IdempotencyResult(true, "mu-existing");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IdempotencyResult? deserialized = JsonSerializer.Deserialize<IdempotencyResult>(json, MemoriesJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.IsDuplicate.ShouldBeTrue();
        deserialized.ExistingMemoryUnitId.ShouldBe("mu-existing");
    }

    // --- DedupKeyInput ---

    [Fact]
    public void DedupKeyInput_RoundTrip()
    {
        var original = new DedupKeyInput("dedup:tenant-1:case-1:abc123", "mu-001");
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        DedupKeyInput? deserialized = JsonSerializer.Deserialize<DedupKeyInput>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);
        json2.ShouldBe(json1);
    }
}
