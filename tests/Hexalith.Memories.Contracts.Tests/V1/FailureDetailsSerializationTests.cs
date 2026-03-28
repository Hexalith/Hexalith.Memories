namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class FailureDetailsSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new FailureDetails("embedding", "TIMEOUT", 3);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        FailureDetails? deserialized = JsonSerializer.Deserialize<FailureDetails>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }

    [Fact]
    public void ZeroRetryCount_ShouldRoundTrip()
    {
        var original = new FailureDetails("extraction", "PARSE_ERROR", 0);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        FailureDetails? deserialized = JsonSerializer.Deserialize<FailureDetails>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }
}
