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

    /// <summary>Story 24.9 — the distinct foreign-marker (confirmed marker mismatch/possible contamination) and
    /// missing-marker (incomplete evidence) diagnostic wording introduced by <c>TenantIsolationVerifier</c> still
    /// round-trips through the unchanged V1 <see cref="TenantIsolationCheckResult.Details"/> and
    /// <see cref="TenantIsolationCheckResult.Remediation"/> plain-string shape, with no blanket prefix/hash
    /// deletion guidance.</summary>
    [Theory]
    [InlineData(
        "raw semantic base key 'tenant-a:vec:leaked-vec' under tenant 'tenant-a' has a foreign tenantId marker 'tenant-b': confirmed marker mismatch (possible contamination) — expected tenant 'tenant-a', observed tenant 'tenant-b'",
        "For the confirmed marker mismatch (possible contamination) on 'tenant-a:vec:leaked-vec', inspect and quarantine the named key(s), then run tenant-scoped marker repair or reindex for tenant 'tenant-a' only after provenance is verified — never delete the prefix.")]
    [InlineData(
        "raw semantic base key 'tenant-a:vec:missing-marker' under tenant 'tenant-a' is missing its tenantId marker: incomplete evidence, not confirmed cross-tenant leakage — expected tenant 'tenant-a'",
        "For the incomplete evidence (missing marker, not confirmed leakage) on 'tenant-a:vec:missing-marker', inspect and quarantine the named key(s) before any tenant-scoped marker repair or reindex for tenant 'tenant-a', applied only after provenance is verified — never delete the prefix.")]
    public void MarkerDiagnosticWording_ShouldRoundTripWithoutShapeBreak(string details, string remediation)
    {
        var original = new TenantIsolationCheckResult("SemanticIsolation", false, 7.0)
        {
            Details = details,
            Remediation = remediation,
        };

        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantIsolationCheckResult? deserialized = JsonSerializer.Deserialize<TenantIsolationCheckResult>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
        deserialized.ShouldNotBeNull();
        deserialized.Details.ShouldBe(details);
        deserialized.Remediation.ShouldNotBeNull();
        deserialized.Remediation.ShouldBe(remediation);
        deserialized.Remediation.ShouldNotContain("remove mismatched target-prefix hashes");
        json1.ShouldContain("\"checkName\":\"SemanticIsolation\"");
    }
}
