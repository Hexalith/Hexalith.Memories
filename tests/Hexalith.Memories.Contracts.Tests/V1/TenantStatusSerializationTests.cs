namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TenantStatusSerializationTests
{
    [Theory]
    [InlineData(TenantStatus.Provisioning, "\"provisioning\"")]
    [InlineData(TenantStatus.Active, "\"active\"")]
    [InlineData(TenantStatus.Deleting, "\"deleting\"")]
    [InlineData(TenantStatus.Failed, "\"failed\"")]
    [InlineData(TenantStatus.CompensationFailed, "\"compensationFailed\"")]
    public void TenantStatus_ShouldRoundTripAsCamelCaseString(TenantStatus value, string expectedJson)
    {
        string json = JsonSerializer.Serialize(value, MemoriesJsonContext.Options);
        json.ShouldBe(expectedJson);

        TenantStatus deserialized = JsonSerializer.Deserialize<TenantStatus>(json, MemoriesJsonContext.Options);
        deserialized.ShouldBe(value);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("5")]
    public void TenantStatus_ShouldRejectIntegerTokens(string json)
    {
        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<TenantStatus>(json, MemoriesJsonContext.Options));
    }
}
