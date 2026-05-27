// <copyright file="DirectoryBatchStatusMapperTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Shouldly;

public class DirectoryBatchStatusMapperTests
{
    [Fact]
    public void MapInstance_NullWorkflowState_ReturnsQueued()
    {
        BatchFileRef file = new("wf-1", @"D:\\docs\\a.txt");

        BatchInstanceStatus status = DirectoryBatchStatusMapper.MapInstance(file, null);

        status.InstanceId.ShouldBe("wf-1");
        status.Status.ShouldBe("queued");
        status.MemoryUnitId.ShouldBeNull();
        status.SourceUri.ShouldBe(@"D:\\docs\\a.txt");
    }

    [Fact]
    public void BuildCounts_ShouldCountQueuedFallbacks()
    {
        BatchStatusCounts counts = DirectoryBatchStatusMapper.BuildCounts(
        [
            new BatchInstanceStatus("wf-1", "queued", null, "a"),
            new BatchInstanceStatus("wf-2", "queued", null, "b"),
            new BatchInstanceStatus("wf-3", "indexed", "mu-3", "c"),
        ]);

        counts.Queued.ShouldBe(2);
        counts.Indexed.ShouldBe(1);
        counts.Extracting.ShouldBe(0);
        counts.Embedding.ShouldBe(0);
        counts.Indexing.ShouldBe(0);
        counts.Failed.ShouldBe(0);
    }
}