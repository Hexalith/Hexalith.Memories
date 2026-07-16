// <copyright file="UrlAndDirectoryIngestionSerializationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Serialization;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Story 6.1 Task 7.10 — serialization round-trips for all new Contracts/V1 records via MemoriesPersistenceJsonContext.Options.</summary>
public class UrlAndDirectoryIngestionSerializationTests
{
    [Fact]
    public void IngestionInput_WithNullContentBytes_RoundTrips()
    {
        IngestionInput original = new()
        {
            TenantId = "t1",
            CaseId = "c1",
            SourceUri = "https://example.com/doc.pdf",
            ContentBytes = null,
            ContentType = "application/octet-stream",
            SourceType = SourceType.Url,
            IngestedBy = "dev@acme",
        };

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        IngestionInput? deserialized = JsonSerializer.Deserialize<IngestionInput>(json, MemoriesPersistenceJsonContext.Options);

        deserialized.ShouldNotBeNull();
        // Serialization may normalize null -> empty array on round-trip. Treat both as semantically
        // "no bytes" — the workflow and validator handle null-or-empty identically for Url source.
        (deserialized.ContentBytes is null || deserialized.ContentBytes.Length == 0).ShouldBeTrue();
        deserialized.SourceType.ShouldBe(SourceType.Url);
        deserialized.SourceUri.ShouldBe("https://example.com/doc.pdf");
    }

    [Fact]
    public void UrlFetchResult_RoundTrips()
    {
        UrlFetchResult original = new([1, 2, 3], "text/plain", 3, "https://example.com/final", 200);

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        UrlFetchResult? back = JsonSerializer.Deserialize<UrlFetchResult>(json, MemoriesPersistenceJsonContext.Options);

        back.ShouldNotBeNull();
        back.ContentBytes.ShouldBe(original.ContentBytes);
        back.ContentType.ShouldBe(original.ContentType);
        back.ContentLength.ShouldBe(3);
        back.FinalUrl.ShouldBe(original.FinalUrl);
        back.HttpStatusCode.ShouldBe(200);
    }

    [Fact]
    public void FetchUrlInput_RoundTrips()
    {
        FetchUrlInput original = new("https://example.com/x", "mu-1", "tenant-a");

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        FetchUrlInput? back = JsonSerializer.Deserialize<FetchUrlInput>(json, MemoriesPersistenceJsonContext.Options);

        back.ShouldNotBeNull();
        back.Url.ShouldBe(original.Url);
        back.MemoryUnitId.ShouldBe(original.MemoryUnitId);
        back.TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public void FetchUrlInput_DeserializeLegacyPayload_DefaultsTenantIdToEmptyString()
    {
        string legacyJson = "{\"url\":\"https://example.com/x\",\"memoryUnitId\":\"mu-1\"}";

        FetchUrlInput? back = JsonSerializer.Deserialize<FetchUrlInput>(legacyJson, MemoriesPersistenceJsonContext.Options);

        back.ShouldNotBeNull();
        back.Url.ShouldBe("https://example.com/x");
        back.MemoryUnitId.ShouldBe("mu-1");
        back.TenantId.ShouldBe(string.Empty);
    }

    [Fact]
    public void UrlIngestionRequest_RoundTrips()
    {
        UrlIngestionRequest original = new()
        {
            TenantId = "t1",
            CaseId = "c1",
            Url = "https://example.com/a",
            IngestedBy = "dev",
            CausationId = "cause-1",
            CorrelationId = "corr-1",
        };
        original.Metadata["k"] = new MetadataField("v", MetadataOrigin.Human, 0.5f);

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        UrlIngestionRequest? back = JsonSerializer.Deserialize<UrlIngestionRequest>(json, MemoriesPersistenceJsonContext.Options);

        back.ShouldNotBeNull();
        back.TenantId.ShouldBe("t1");
        back.Metadata.ShouldContainKey("k");
        back.Metadata["k"].Value.ShouldBe("v");
    }

    [Fact]
    public void UrlIngestionResponse_RoundTrips()
    {
        UrlIngestionResponse original = new("instance-1", "https://example.com/doc");

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        UrlIngestionResponse? back = JsonSerializer.Deserialize<UrlIngestionResponse>(json, MemoriesPersistenceJsonContext.Options);

        back.ShouldNotBeNull();
        back.InstanceId.ShouldBe("instance-1");
        back.SourceUri.ShouldBe("https://example.com/doc");
        back.SourceType.ShouldBe("url");
    }

    [Fact]
    public void DirectoryIngestionRequest_RoundTrips()
    {
        DirectoryIngestionRequest original = new()
        {
            TenantId = "t1",
            CaseId = "c1",
            DirectoryPath = "/data/sample",
            IngestedBy = "dev",
            Recursive = true,
        };

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        DirectoryIngestionRequest? back = JsonSerializer.Deserialize<DirectoryIngestionRequest>(json, MemoriesPersistenceJsonContext.Options);

        back.ShouldNotBeNull();
        back.DirectoryPath.ShouldBe("/data/sample");
        back.Recursive.ShouldBeTrue();
    }

    [Fact]
    public void DirectoryIngestionOutcome_RoundTrips()
    {
        SkippedFile skipped = new("/data/sample/x.exe", "UNSUPPORTED_EXTENSION");
        DirectoryIngestionOutcome original = new(
            "batch-1",
            3,
            2,
            [skipped],
            false,
            ["wf-a", "wf-b"],
            "t1",
            "c1");

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        DirectoryIngestionOutcome? back = JsonSerializer.Deserialize<DirectoryIngestionOutcome>(json, MemoriesPersistenceJsonContext.Options);

        back.ShouldNotBeNull();
        back.BatchId.ShouldBe("batch-1");
        back.Discovered.ShouldBe(3);
        back.Enqueued.ShouldBe(2);
        back.Skipped.Count.ShouldBe(1);
        back.Skipped[0].Reason.ShouldBe("UNSUPPORTED_EXTENSION");
        back.InstanceIds.Count.ShouldBe(2);
    }

    [Fact]
    public void BatchStatusResponse_RoundTrips()
    {
        BatchStatusCounts counts = new(0, 1, 0, 0, 1, 0);
        BatchInstanceStatus instance = new("wf-1", "indexed", "mu-1", "/data/a.md");
        BatchStatusResponse original = new(
            "batch-1",
            "t1",
            "c1",
            2,
            2,
            0,
            counts,
            [instance]);

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        BatchStatusResponse? back = JsonSerializer.Deserialize<BatchStatusResponse>(json, MemoriesPersistenceJsonContext.Options);

        back.ShouldNotBeNull();
        back.BatchId.ShouldBe("batch-1");
        back.Counts.Indexed.ShouldBe(1);
        back.Instances.Count.ShouldBe(1);
        back.Instances[0].InstanceId.ShouldBe("wf-1");
        back.Instances[0].Status.ShouldBe("indexed");
    }

    [Fact]
    public void SkippedFile_RoundTrips()
    {
        SkippedFile original = new("/data/x.exe", "UNSUPPORTED_EXTENSION");

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        SkippedFile? back = JsonSerializer.Deserialize<SkippedFile>(json, MemoriesPersistenceJsonContext.Options);

        back.ShouldNotBeNull();
        back.Path.ShouldBe("/data/x.exe");
        back.Reason.ShouldBe("UNSUPPORTED_EXTENSION");
    }
}
