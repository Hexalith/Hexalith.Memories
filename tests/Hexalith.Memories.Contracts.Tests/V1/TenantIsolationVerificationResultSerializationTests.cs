namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TenantIsolationVerificationResultSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalJson()
    {
        var original = new TenantIsolationVerificationResult(
            "tenant-a",
            DateTimeOffset.Parse("2026-04-14T10:00:00Z"),
            true,
            "7 of 7 checks passed",
            [
                new TenantIsolationCheckResult("SyntacticIsolation", true, 10.0),
                new TenantIsolationCheckResult("SemanticIsolation", true, 8.5),
            ]);
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantIsolationVerificationResult? deserialized = JsonSerializer.Deserialize<TenantIsolationVerificationResult>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new TenantIsolationVerificationResult(
            "tenant-a",
            DateTimeOffset.UtcNow,
            true,
            "All passed",
            []);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"tenantId\":");
        json.ShouldContain("\"verifiedAt\":");
        json.ShouldContain("\"allPassed\":");
        json.ShouldContain("\"summary\":");
        json.ShouldContain("\"checks\":");
        json.ShouldNotContain("\"TenantId\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void AllPassed_ShouldBeFalseWhenAnyCheckFails()
    {
        var original = new TenantIsolationVerificationResult(
            "tenant-a",
            DateTimeOffset.UtcNow,
            false,
            "3 of 7 checks failed: SyntacticIsolation, GraphIsolation, OrphanedDatabases",
            [
                new TenantIsolationCheckResult("SyntacticIsolation", false, 10.0)
                {
                    Details = "Leakage detected",
                    Remediation = "Investigate cross-tenant data",
                },
                new TenantIsolationCheckResult("SemanticIsolation", true, 8.5),
            ]);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantIsolationVerificationResult? deserialized = JsonSerializer.Deserialize<TenantIsolationVerificationResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.AllPassed.ShouldBeFalse();
        deserialized.Checks.Count.ShouldBe(2);
        deserialized.Checks[0].Passed.ShouldBeFalse();
        deserialized.Checks[0].Details.ShouldBe("Leakage detected");
        deserialized.Checks[1].Passed.ShouldBeTrue();
    }

    [Fact]
    public void EmptyChecks_ShouldRoundTrip()
    {
        var original = new TenantIsolationVerificationResult(
            "tenant-a",
            DateTimeOffset.UtcNow,
            true,
            "0 of 0 checks passed",
            []);
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantIsolationVerificationResult? deserialized = JsonSerializer.Deserialize<TenantIsolationVerificationResult>(json1, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Checks.ShouldBeEmpty();
    }

    [Fact]
    public void DateTimeOffset_ShouldPreserveOffset()
    {
        var original = new TenantIsolationVerificationResult(
            "tenant-a",
            DateTimeOffset.Parse("2026-04-14T12:00:00+02:00"),
            true,
            "All passed",
            []);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("+02:00");
    }
}
