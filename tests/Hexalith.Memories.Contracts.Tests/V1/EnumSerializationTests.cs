namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class EnumSerializationTests
{
    [Theory]
    [InlineData(SourceType.File, "\"file\"")]
    [InlineData(SourceType.Discussion, "\"discussion\"")]
    public void SourceType_ShouldRoundTripAsCamelCaseString(SourceType value, string expectedJson)
        => ShouldRoundTripAsCamelCaseString(value, expectedJson);

    [Theory]
    [InlineData("0")]
    [InlineData("5")]
    public void SourceType_ShouldRejectIntegerTokens(string json)
        => ShouldRejectIntegerTokens<SourceType>(json);

    [Theory]
    [InlineData(MemoryUnitStatus.Queued, "\"queued\"")]
    [InlineData(MemoryUnitStatus.Failed, "\"failed\"")]
    public void MemoryUnitStatus_ShouldRoundTripAsCamelCaseString(MemoryUnitStatus value, string expectedJson)
        => ShouldRoundTripAsCamelCaseString(value, expectedJson);

    [Theory]
    [InlineData("0")]
    [InlineData("5")]
    public void MemoryUnitStatus_ShouldRejectIntegerTokens(string json)
        => ShouldRejectIntegerTokens<MemoryUnitStatus>(json);

    [Theory]
    [InlineData(CaseStatus.Active, "\"active\"")]
    [InlineData(CaseStatus.Closed, "\"closed\"")]
    public void CaseStatus_ShouldRoundTripAsCamelCaseString(CaseStatus value, string expectedJson)
        => ShouldRoundTripAsCamelCaseString(value, expectedJson);

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void CaseStatus_ShouldRejectIntegerTokens(string json)
        => ShouldRejectIntegerTokens<CaseStatus>(json);

    [Theory]
    [InlineData(EdgeType.CausedBy, "\"causedBy\"")]
    [InlineData(EdgeType.Annotates, "\"annotates\"")]
    public void EdgeType_ShouldRoundTripAsCamelCaseString(EdgeType value, string expectedJson)
        => ShouldRoundTripAsCamelCaseString(value, expectedJson);

    [Theory]
    [InlineData("0")]
    [InlineData("4")]
    public void EdgeType_ShouldRejectIntegerTokens(string json)
        => ShouldRejectIntegerTokens<EdgeType>(json);

    [Theory]
    [InlineData(MetadataOrigin.Human, "\"human\"")]
    [InlineData(MetadataOrigin.Ai, "\"ai\"")]
    public void MetadataOrigin_ShouldRoundTripAsCamelCaseString(MetadataOrigin value, string expectedJson)
        => ShouldRoundTripAsCamelCaseString(value, expectedJson);

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void MetadataOrigin_ShouldRejectIntegerTokens(string json)
        => ShouldRejectIntegerTokens<MetadataOrigin>(json);

    [Theory]
    [InlineData(CaseActivityEventType.CaseCreated, "\"caseCreated\"")]
    [InlineData(CaseActivityEventType.MemoryUnitIngested, "\"memoryUnitIngested\"")]
    [InlineData(CaseActivityEventType.IngestionFailed, "\"ingestionFailed\"")]
    [InlineData(CaseActivityEventType.SearchExecuted, "\"searchExecuted\"")]
    [InlineData(CaseActivityEventType.MemberAdded, "\"memberAdded\"")]
    [InlineData(CaseActivityEventType.MemberRemoved, "\"memberRemoved\"")]
    public void CaseActivityEventType_ShouldRoundTripAsCamelCaseString(CaseActivityEventType value, string expectedJson)
        => ShouldRoundTripAsCamelCaseString(value, expectedJson);

    [Theory]
    [InlineData("0")]
    [InlineData("5")]
    public void CaseActivityEventType_ShouldRejectIntegerTokens(string json)
        => ShouldRejectIntegerTokens<CaseActivityEventType>(json);

    [Theory]
    [InlineData(EdgeOrigin.Explicit, "\"explicit\"")]
    [InlineData(EdgeOrigin.Inferred, "\"inferred\"")]
    public void EdgeOrigin_ShouldRoundTripAsCamelCaseString(EdgeOrigin value, string expectedJson)
        => ShouldRoundTripAsCamelCaseString(value, expectedJson);

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void EdgeOrigin_ShouldRejectIntegerTokens(string json)
        => ShouldRejectIntegerTokens<EdgeOrigin>(json);

    private static void ShouldRoundTripAsCamelCaseString<TEnum>(TEnum value, string expectedJson)
        where TEnum : struct, Enum
    {
        string json = JsonSerializer.Serialize(value, MemoriesJsonContext.Options);
        json.ShouldBe(expectedJson);

        TEnum deserialized = JsonSerializer.Deserialize<TEnum>(json, MemoriesJsonContext.Options);
        deserialized.ShouldBe(value);
    }

    private static void ShouldRejectIntegerTokens<TEnum>(string json)
        where TEnum : struct, Enum
    {
        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<TEnum>(json, MemoriesJsonContext.Options));
    }
}
