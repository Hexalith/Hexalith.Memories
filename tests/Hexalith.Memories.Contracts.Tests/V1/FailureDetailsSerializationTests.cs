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

    [Fact]
    public void WithErrorMessageAndLastRetryAt_ShouldRoundTrip()
    {
        var lastRetryAt = new DateTimeOffset(2026, 4, 15, 12, 0, 0, TimeSpan.Zero);
        var original = new FailureDetails("embedding", "PROVIDER_500", 5, "Provider returned 500", lastRetryAt);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        FailureDetails? deserialized = JsonSerializer.Deserialize<FailureDetails>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
        deserialized!.ErrorMessage.ShouldBe("Provider returned 500");
        deserialized.LastRetryAt.ShouldBe(lastRetryAt);
    }

    [Fact]
    public void LegacyPayload_WithoutNewFields_ShouldDeserializeWithNulls()
    {
        string legacyJson = """{"stage":"embedding","errorCode":"TIMEOUT","retryCount":3}""";
        FailureDetails? deserialized = JsonSerializer.Deserialize<FailureDetails>(legacyJson, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized!.Stage.ShouldBe("embedding");
        deserialized.ErrorCode.ShouldBe("TIMEOUT");
        deserialized.RetryCount.ShouldBe(3);
        deserialized.ErrorMessage.ShouldBeNull();
        deserialized.LastRetryAt.ShouldBeNull();
    }
}
