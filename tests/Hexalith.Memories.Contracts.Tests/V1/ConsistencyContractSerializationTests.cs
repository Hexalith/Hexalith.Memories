namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>
/// Story 8.2 — guard that every new V1 contract record / enum added by the consistency
/// workflow is registered with <see cref="MemoriesJsonContext"/> via a
/// <c>[JsonSerializable]</c> attribute. Missing registrations surface at runtime as
/// AOT warnings or DAPR Workflow "unknown type" dispatch errors; catching it here keeps
/// the registry + the source files in sync.
/// </summary>
public class ConsistencyContractSerializationTests
{
    [Theory]
    [InlineData(typeof(ConsistencyVerificationRequest))]
    [InlineData(typeof(ConsistencyVerificationResult))]
    [InlineData(typeof(ConsistencyDiscrepancy))]
    [InlineData(typeof(ConsistencyRepairRecommendation))]
    [InlineData(typeof(ConsistencyInspectionResult))]
    [InlineData(typeof(ConsistencySyntacticDetail))]
    [InlineData(typeof(ConsistencySemanticDetail))]
    [InlineData(typeof(ConsistencyGraphDetail))]
    [InlineData(typeof(ConsistencyRepairRequest))]
    [InlineData(typeof(ConsistencyRepairResult))]
    [InlineData(typeof(ConsistencyRepairStatus))]
    [InlineData(typeof(RepairActionRecord))]
    [InlineData(typeof(ConsistencyVerificationStatus))]
    [InlineData(typeof(ConsistencyWorkflowProgress))]
    [InlineData(typeof(ConsistencyWorkflowState))]
    public void NewV1Contracts_AreRegisteredInMemoriesJsonContext(Type type)
    {
        System.Text.Json.Serialization.Metadata.JsonTypeInfo? info =
            MemoriesJsonContext.Options.GetTypeInfo(type);

        info.ShouldNotBeNull(
            $"{type.Name} must be registered in MemoriesJsonContext via [JsonSerializable]. "
            + "Missing source-gen registration causes AOT warnings and DAPR Workflow dispatch errors.");
    }

    [Theory]
    [InlineData(typeof(IReadOnlyList<ConsistencyDiscrepancy>))]
    [InlineData(typeof(IReadOnlyList<RepairActionRecord>))]
    public void CollectionContracts_AreRegisteredInMemoriesJsonContext(Type type)
    {
        System.Text.Json.Serialization.Metadata.JsonTypeInfo? info =
            MemoriesJsonContext.Options.GetTypeInfo(type);

        info.ShouldNotBeNull($"{type.Name} must be registered in MemoriesJsonContext via [JsonSerializable].");
    }

    [Fact]
    public void ConsistencyRepairRecommendation_SerializesAsCamelCaseString()
    {
        string json = JsonSerializer.Serialize(
            ConsistencyRepairRecommendation.RemoveOrphanedSemanticAndGraph,
            MemoriesJsonContext.Options);

        json.ShouldBe("\"removeOrphanedSemanticAndGraph\"");
    }

    [Fact]
    public void ConsistencyInspectionResult_RoundTripsThroughMemoriesJsonContext()
    {
        DateTimeOffset checkedAt = DateTimeOffset.UtcNow;
        ConsistencyInspectionResult original = new(
            TenantId: "tenant-1",
            MemoryUnitId: "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            SyntacticPresent: true,
            SemanticPresent: false,
            GraphPresent: true,
            SyntacticDetail: new ConsistencySyntacticDetail(
                "hash123", checkedAt, "file:///a.md", "file", "case-1", "gemini", "gemini-embedding-001"),
            SemanticDetail: null,
            GraphDetail: new ConsistencyGraphDetail(1, 2, 1),
            Recommendation: ConsistencyRepairRecommendation.ReIndexSemantic,
            CheckedAt: checkedAt);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ConsistencyInspectionResult? roundTripped = JsonSerializer
            .Deserialize<ConsistencyInspectionResult>(json, MemoriesJsonContext.Options);

        roundTripped.ShouldNotBeNull();
        roundTripped.Recommendation.ShouldBe(ConsistencyRepairRecommendation.ReIndexSemantic);
        roundTripped.SyntacticDetail.ShouldNotBeNull();
        roundTripped.SemanticDetail.ShouldBeNull();
        roundTripped.GraphDetail.ShouldNotBeNull();
        roundTripped.SyntacticDetail.EmbeddingModel.ShouldBe("gemini-embedding-001");
    }
}
