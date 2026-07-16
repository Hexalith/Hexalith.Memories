namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class ErrorResponseSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new ErrorResponse(
            "TENANT_NOT_FOUND",
            "Tenant 'acme' does not exist",
            "Run 'memories tenant list' to see available tenants");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ErrorResponse? deserialized = JsonSerializer.Deserialize<ErrorResponse>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new ErrorResponse("TEST_CODE", "Test message", "Run 'memories help'");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"code\":");
        json.ShouldContain("\"message\":");
        json.ShouldContain("\"suggestion\":");

        json.ShouldNotContain("\"Code\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"Message\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"Suggestion\":", Shouldly.Case.Sensitive);
    }
}
