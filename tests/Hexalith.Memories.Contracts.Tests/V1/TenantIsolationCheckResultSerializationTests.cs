namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TenantIsolationCheckResultSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalJson()
    {
        var original = new TenantIsolationCheckResult("SyntacticIsolation", true, 12.34)
        {
            Details = "No cross-tenant data found",
        };
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantIsolationCheckResult? deserialized = JsonSerializer.Deserialize<TenantIsolationCheckResult>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new TenantIsolationCheckResult("TestCheck", true, 1.0);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"checkName\":");
        json.ShouldContain("\"passed\":");
        json.ShouldContain("\"durationMs\":");
        json.ShouldNotContain("\"CheckName\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void NullableFields_ShouldBeOmittedWhenNull()
    {
        var original = new TenantIsolationCheckResult("TestCheck", true, 1.0);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldNotContain("details");
        json.ShouldNotContain("remediation");
    }

    [Fact]
    public void NullableFields_ShouldBePresentWhenPopulated()
    {
        var original = new TenantIsolationCheckResult("TestCheck", false, 5.0)
        {
            Details = "Leakage detected",
            Remediation = "Run tenant delete to clean up",
        };
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"details\":\"Leakage detected\"");
        json.ShouldContain("\"remediation\":\"Run tenant delete to clean up\"");
    }

    [Fact]
    public void FailedCheck_ShouldRoundTrip()
    {
        var original = new TenantIsolationCheckResult("GraphIsolation", false, 42.5)
        {
            Details = "Cross-database query returned 3 nodes",
            Remediation = "Run `memories tenant delete --id ghost-tenant` to clean up orphaned database",
        };
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantIsolationCheckResult? deserialized = JsonSerializer.Deserialize<TenantIsolationCheckResult>(json1, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.CheckName.ShouldBe("GraphIsolation");
        deserialized.Passed.ShouldBeFalse();
        deserialized.DurationMs.ShouldBe(42.5);
        deserialized.Details.ShouldBe("Cross-database query returned 3 nodes");
        deserialized.Remediation.ShouldNotBeNull();
    }
}
