namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class CreateCaseInputSerializationTests
{
    [Fact]
    public void RoundTrip_WithDescription_ShouldProduceIdenticalJson()
    {
        var original = new CreateCaseInput("tenant-1", "Claims Pilot", "First investigation case");
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CreateCaseInput? deserialized = JsonSerializer.Deserialize<CreateCaseInput>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void RoundTrip_WithNullDescription_ShouldRoundTrip()
    {
        var original = new CreateCaseInput("tenant-1", "Claims Pilot", null);
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CreateCaseInput? deserialized = JsonSerializer.Deserialize<CreateCaseInput>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new CreateCaseInput("tenant-1", "Test", "desc");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"tenantId\":");
        json.ShouldContain("\"name\":");
        json.ShouldContain("\"description\":");
    }
}
