namespace Hexalith.Memories.Contracts.Tests.V1;

/// <summary>
/// ATDD RED-phase seminal tests for Story 8.2 — Tasks 3.1 + 3.2.
/// Guards that every new V1 contract record / enum added by Story 8.2 is registered with
/// <c>MemoriesJsonContext</c> via <c>[JsonSerializable]</c>, so the source-gen resolver
/// covers serialization without reflection fallback. A missing registration would surface
/// at runtime as an AOT warning or a DAPR Workflow "unknown type" dispatch error —
/// catching it here at build time is cheaper.
/// </summary>
/// <remarks>
/// Skip-gated until the 11 new records/enums + updated <c>MemoriesJsonContext</c> land.
/// When activated, the test serializes a minimal instance of each new type via
/// <c>MemoriesJsonContext.Options</c> and round-trips back, asserting:
/// <list type="bullet">
///   <item>Serialization does not throw (registered in source-gen).</item>
///   <item>Deserialized instance is non-null.</item>
///   <item>Property names in JSON use <c>camelCase</c> (project convention).</item>
///   <item>Enum values serialize as camelCase strings (not integers) — pattern from
///     <see cref="EnumSerializationTests"/>.</item>
/// </list>
/// </remarks>
public class ConsistencyContractSerializationTests
{
    // Blueprint — uncomment when target types exist (Tasks 3.1 + 3.2):
    //
    // using System.Text.Json;
    // using Hexalith.Memories.Contracts.V1;
    // using Shouldly;
    //
    // [Theory]
    // [InlineData(typeof(ConsistencyVerificationRequest))]
    // [InlineData(typeof(ConsistencyVerificationResult))]
    // [InlineData(typeof(ConsistencyDiscrepancy))]
    // [InlineData(typeof(ConsistencyRepairRecommendation))]
    // [InlineData(typeof(ConsistencyInspectionResult))]
    // [InlineData(typeof(ConsistencySyntacticDetail))]
    // [InlineData(typeof(ConsistencySemanticDetail))]
    // [InlineData(typeof(ConsistencyGraphDetail))]
    // [InlineData(typeof(ConsistencyRepairRequest))]
    // [InlineData(typeof(ConsistencyRepairResult))]
    // [InlineData(typeof(RepairActionRecord))]
    // public void NewV1Contracts_AreRegisteredInMemoriesJsonContext(Type type)
    // {
    //     JsonTypeInfo? info = MemoriesJsonContext.Default.GetTypeInfo(type);
    //     info.ShouldNotBeNull(
    //         $"Story 8.2 Task 3.2 — {type.Name} must be added to MemoriesJsonContext " +
    //         $"[JsonSerializable] attribute list.");
    // }

    /// <summary>
    /// ATDD RED — Story 8.2 Task 3.2 (JSON source-gen registration).
    /// Expected: every new V1 record + enum listed in the story's "Project structure notes"
    /// (lines 502-512) is present in <c>MemoriesJsonContext.Default</c>. Missing registration
    /// would either (a) fire AOT warnings in Release builds, or (b) cause DAPR Workflow to
    /// fail dispatching workflow/activity I/O with "unknown type" at runtime.
    /// </summary>
    [Theory(Skip = "ATDD RED — awaiting new V1 contracts + MemoriesJsonContext registration (Story 8.2 Tasks 3.1 + 3.2)")]
    [InlineData("ConsistencyVerificationRequest")]
    [InlineData("ConsistencyVerificationResult")]
    [InlineData("ConsistencyDiscrepancy")]
    [InlineData("ConsistencyRepairRecommendation")]
    [InlineData("ConsistencyInspectionResult")]
    [InlineData("ConsistencySyntacticDetail")]
    [InlineData("ConsistencySemanticDetail")]
    [InlineData("ConsistencyGraphDetail")]
    [InlineData("ConsistencyRepairRequest")]
    [InlineData("ConsistencyRepairResult")]
    [InlineData("RepairActionRecord")]
    public void NewV1Contracts_AreRegisteredInMemoriesJsonContext(string expectedTypeName)
    {
        // Activate when target types exist:
        //
        // Type type = typeof(ConsistencyVerificationRequest).Assembly.GetType(
        //     $"Hexalith.Memories.Contracts.V1.{expectedTypeName}",
        //     throwOnError: true)!;
        // JsonTypeInfo? info = MemoriesJsonContext.Default.GetTypeInfo(type);
        // info.ShouldNotBeNull(
        //     $"{expectedTypeName} must be added to MemoriesJsonContext [JsonSerializable] list.");

        Assert.Fail(
            $"ATDD RED (8.2-UNIT-014) — Story 8.2 Task 3.2: register {expectedTypeName} "
            + "in MemoriesJsonContext [JsonSerializable] attribute list. "
            + "Missing source-gen registration causes runtime AOT warnings and DAPR Workflow dispatch errors.");
    }
}
