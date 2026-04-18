// <copyright file="MemoriesActivitySourceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System.Diagnostics;

using Hexalith.Memories.Telemetry;

using Shouldly;

/// <summary>Story 7.5 Task 8.1 — asserts ActivitySource constants are pinned.</summary>
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class MemoriesActivitySourceTests
{
    [Fact]
    public void SourceName_IsPinned() => MemoriesActivitySource.SourceName.ShouldBe("Hexalith.Memories");

    [Fact]
    public void ActivityNames_ArePinned()
    {
        MemoriesActivitySource.SearchRequest.ShouldBe("memories.search");
        MemoriesActivitySource.IngestRequest.ShouldBe("memories.ingest");
        MemoriesActivitySource.TraverseRequest.ShouldBe("memories.traverse");
        MemoriesActivitySource.CaseAccess.ShouldBe("memories.case-access");
        MemoriesActivitySource.CliInvoke.ShouldBe("memories.cli.invoke");
    }

    [Fact]
    public void TagKeys_ArePinned()
    {
        MemoriesActivitySource.TagTenantId.ShouldBe("memories.tenant_id");
        MemoriesActivitySource.TagCaseId.ShouldBe("memories.case_id");
        MemoriesActivitySource.TagOperation.ShouldBe("memories.operation");
        MemoriesActivitySource.TagAxis.ShouldBe("memories.axis");
        MemoriesActivitySource.TagSourceType.ShouldBe("memories.source_type");
        MemoriesActivitySource.TagOutcome.ShouldBe("memories.outcome");
        MemoriesActivitySource.TagErrorCode.ShouldBe("memories.error_code");
    }

    [Fact]
    public void Instance_ReturnsSingletonActivitySource()
    {
        MemoriesActivitySource.Instance.ShouldBeOfType<ActivitySource>();
        MemoriesActivitySource.Instance.Name.ShouldBe(MemoriesActivitySource.SourceName);
    }

    [Fact]
    public void StartActivity_WithListener_EmitsActivityOnSource()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == MemoriesActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using Activity? activity = MemoriesActivitySource.Instance.StartActivity(MemoriesActivitySource.SearchRequest);
        activity.ShouldNotBeNull();
        activity.Source.Name.ShouldBe(MemoriesActivitySource.SourceName);
    }
}
