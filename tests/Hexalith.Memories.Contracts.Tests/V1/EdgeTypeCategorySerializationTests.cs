namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class EdgeTypeCategorySerializationTests
{
    [Theory]
    [InlineData(EdgeTypeCategory.Structural, "\"structural\"")]
    [InlineData(EdgeTypeCategory.Semantic, "\"semantic\"")]
    public void EdgeTypeCategory_ShouldRoundTripAsCamelCaseString(EdgeTypeCategory value, string expectedJson)
    {
        string json = JsonSerializer.Serialize(value, MemoriesJsonContext.Options);
        json.ShouldBe(expectedJson);

        EdgeTypeCategory deserialized = JsonSerializer.Deserialize<EdgeTypeCategory>(json, MemoriesJsonContext.Options);
        deserialized.ShouldBe(value);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void EdgeTypeCategory_ShouldRejectIntegerTokens(string json)
    {
        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<EdgeTypeCategory>(json, MemoriesJsonContext.Options));
    }
}
