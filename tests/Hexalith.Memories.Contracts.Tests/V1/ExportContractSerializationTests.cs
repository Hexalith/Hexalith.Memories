// <copyright file="ExportContractSerializationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>
/// Story 8.3 — guard that every new V1 export contract record / enum is registered with
/// <see cref="MemoriesJsonContext"/> via a <c>[JsonSerializable]</c> attribute. Missing
/// registrations surface at runtime as AOT warnings.
/// </summary>
public class ExportContractSerializationTests
{
    [Theory]
    [InlineData(typeof(ExportManifest))]
    [InlineData(typeof(ExportScope))]
    [InlineData(typeof(ExportStatistics))]
    [InlineData(typeof(ExportedEdge))]
    [InlineData(typeof(ExportedTenantConfig))]
    [InlineData(typeof(ExportedMemoryUnit))]
    public void NewV1Contracts_AreRegisteredInMemoriesJsonContext(Type type)
    {
        System.Text.Json.Serialization.Metadata.JsonTypeInfo? info =
            MemoriesJsonContext.Options.GetTypeInfo(type);

        info.ShouldNotBeNull(
            $"{type.Name} must be registered in MemoriesJsonContext via [JsonSerializable].");
    }

    [Fact]
    public void ExportScope_SerializesAsCamelCaseString()
    {
        string json = JsonSerializer.Serialize(ExportScope.Tenant, MemoriesJsonContext.Options);
        json.ShouldBe("\"tenant\"");

        json = JsonSerializer.Serialize(ExportScope.Case, MemoriesJsonContext.Options);
        json.ShouldBe("\"case\"");
    }

    [Fact]
    public void ExportManifest_RoundTripsThroughMemoriesJsonContext()
    {
        ExportManifest original = new(
            SchemaVersion: 1,
            Scope: ExportScope.Tenant,
            TenantId: "acme",
            CaseId: null,
            ExportedAt: new DateTimeOffset(2026, 4, 20, 10, 0, 0, TimeSpan.Zero),
            SnapshotAt: new DateTimeOffset(2026, 4, 20, 10, 0, 1, TimeSpan.Zero));

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ExportManifest? roundTripped = JsonSerializer.Deserialize<ExportManifest>(json, MemoriesJsonContext.Options);

        roundTripped.ShouldNotBeNull();
        roundTripped.SchemaVersion.ShouldBe(1);
        roundTripped.Scope.ShouldBe(ExportScope.Tenant);
        roundTripped.TenantId.ShouldBe("acme");
        roundTripped.CaseId.ShouldBeNull();
    }

    [Fact]
    public void ExportedEdge_SerializesPromotionAuditFields()
    {
        ExportedEdge original = new(
            Id: "4273",
            SourceId: "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            TargetId: "01HM5Q9WXGK6T8Q4Z5Y6V7W8X0",
            EdgeType: "causedBy",
            Confidence: 0.95f,
            Origin: "inferred",
            CreatedAt: new DateTimeOffset(2026, 4, 20, 10, 0, 0, TimeSpan.Zero),
            VerifiedBy: "alice@acme.com",
            PreviousConfidence: 0.8f);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ExportedEdge? roundTripped = JsonSerializer.Deserialize<ExportedEdge>(json, MemoriesJsonContext.Options);

        roundTripped.ShouldNotBeNull();
        roundTripped.Id.ShouldBe("4273");
        roundTripped.EdgeType.ShouldBe("causedBy");
        roundTripped.VerifiedBy.ShouldBe("alice@acme.com");
        roundTripped.PreviousConfidence.ShouldBe(0.8f);
    }

    [Fact]
    public void ExportedMemoryUnit_WrapperShape_PreservesAnnotationTargets()
    {
        MemoryUnit unit = new()
        {
            Id = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            TenantId = "acme",
            CaseId = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X0",
            Content = "text",
            ContentHash = "sha256:abc",
            SourceUri = "file:///a.md",
            SourceType = SourceType.File,
            IngestedBy = "alice",
            IngestedAt = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow,
            Status = MemoryUnitStatus.Indexed,
        };

        ExportedMemoryUnit original = new(unit, new[] { "anno1", "anno2" });
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"annotationTargets\"");
        json.ShouldContain("\"unit\"");

        ExportedMemoryUnit? roundTripped = JsonSerializer.Deserialize<ExportedMemoryUnit>(json, MemoriesJsonContext.Options);
        roundTripped.ShouldNotBeNull();
        roundTripped.AnnotationTargets.Count.ShouldBe(2);
        roundTripped.Unit.Id.ShouldBe(unit.Id);
    }

    [Fact]
    public void ExportStatistics_SerializesCountsAsIntegers()
    {
        ExportStatistics stats = new(MemoryUnitCount: 42, EdgeCount: 87, CaseCount: 3);
        string json = JsonSerializer.Serialize(stats, MemoriesJsonContext.Options);

        json.ShouldContain("\"memoryUnitCount\":42");
        json.ShouldContain("\"edgeCount\":87");
        json.ShouldContain("\"caseCount\":3");
    }
}
