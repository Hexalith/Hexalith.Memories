// <copyright file="FailedNaturalLanguageEmbeddingRegistryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.NaturalLanguage;

using Shouldly;

public class FailedNaturalLanguageEmbeddingRegistryTests
{
    [Fact]
    public void SerializeRecord_RoundTripsAllFields()
    {
        FailedNaturalLanguageEmbeddingRecord record = new(
            TenantId: "tenant-a",
            MemoryUnitId: "mu-1",
            TruncatedRawJsonPayload: "{\"foo\":\"bar\"}",
            EventType: "TestEventV1",
            AggregateType: "Account",
            CaseId: "case-1",
            EmbeddingProvider: "openai",
            EmbeddingModel: "text-embedding-3-small",
            EmbeddingDimensions: 1536,
            QueuedAtTicks: 1_000_000,
            Attempts: 2);

        string json = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record);
        FailedNaturalLanguageEmbeddingRecord? roundTripped = FailedNaturalLanguageEmbeddingRegistry.TryDeserialize(json);

        roundTripped.ShouldNotBeNull();
        roundTripped!.TenantId.ShouldBe("tenant-a");
        roundTripped.MemoryUnitId.ShouldBe("mu-1");
        roundTripped.TruncatedRawJsonPayload.ShouldBe("{\"foo\":\"bar\"}");
        roundTripped.Attempts.ShouldBe(2);
        roundTripped.QueuedAtTicks.ShouldBe(1_000_000);
    }

    [Fact]
    public void TryDeserialize_Garbage_ReturnsNull()
    {
        FailedNaturalLanguageEmbeddingRegistry.TryDeserialize("not json at all").ShouldBeNull();
    }

    [Fact]
    public void LiveKeyPrefix_IsTenantScoped()
    {
        string key = FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a");
        key.ShouldBe("nl-embedding-retry:tenant-a");
    }

    [Fact]
    public void DeadKeyPrefix_IsTenantScoped()
    {
        string key = FailedNaturalLanguageEmbeddingRegistry.DeadKey("tenant-a");
        key.ShouldBe("nl-embedding-retry-dead:tenant-a");
    }

    [Fact]
    public void EnqueueDequeueRoundTrip_StoresIdsPlusBoundedPayload_NotFullPayload()
    {
        // Pre-mortem Failure δ regression: record size is bounded by the truncation at enqueue time.
        string fourKb = new string('A', 4096);
        FailedNaturalLanguageEmbeddingRecord record = new(
            TenantId: "tenant-a",
            MemoryUnitId: "mu-1",
            TruncatedRawJsonPayload: fourKb,
            EventType: "E",
            AggregateType: null,
            CaseId: "c",
            EmbeddingProvider: "p",
            EmbeddingModel: "m",
            EmbeddingDimensions: 3,
            QueuedAtTicks: 10,
            Attempts: 0);

        string serialized = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record);
        // 4KB payload + envelope ≤ ~4.5KB — the Task 8.1 fallback shape cap.
        serialized.Length.ShouldBeLessThan(5 * 1024);
    }
}
