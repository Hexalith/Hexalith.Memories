// <copyright file="IngestionWorkflowStatusSerializationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public sealed class IngestionWorkflowStatusSerializationTests
{
    [Fact]
    public void RoundTrip_AllFieldsPopulated_ShouldUseSafeCamelCaseContract()
    {
        IngestionWorkflowStatus original = new(
            InstanceId: "wf-1",
            TenantId: "tenant-a",
            CaseId: "case-1",
            RuntimeStatus: "Completed",
            CreatedAt: DateTimeOffset.Parse("2026-07-04T10:00:00+00:00"),
            LastUpdatedAt: DateTimeOffset.Parse("2026-07-04T10:05:00+00:00"),
            MemoryUnitId: "mu-1",
            MemoryUnitStatus: MemoryUnitStatus.Indexed,
            FailureSummary: null);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IngestionWorkflowStatus? deserialized = JsonSerializer.Deserialize<IngestionWorkflowStatus>(
            json,
            MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.ShouldBe(original);
        json.ShouldContain("\"instanceId\":\"wf-1\"");
        json.ShouldContain("\"memoryUnitStatus\":\"indexed\"");
        json.ShouldNotContain("workflowState", Shouldly.Case.Insensitive);
        json.ShouldNotContain("serializedInput", Shouldly.Case.Insensitive);
        json.ShouldNotContain("serializedOutput", Shouldly.Case.Insensitive);
        json.ShouldNotContain("contentBytes", Shouldly.Case.Insensitive);
        json.ShouldNotContain("metadata", Shouldly.Case.Insensitive);
    }

    [Fact]
    public void IngestionWorkflowStatus_IsRegisteredInMemoriesJsonContext()
    {
        System.Text.Json.Serialization.Metadata.JsonTypeInfo? info =
            MemoriesJsonContext.Options.GetTypeInfo(typeof(IngestionWorkflowStatus));

        info.ShouldNotBeNull("IngestionWorkflowStatus must be registered in MemoriesJsonContext.");
    }
}
