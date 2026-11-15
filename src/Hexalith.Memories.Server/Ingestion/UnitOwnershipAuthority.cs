namespace Hexalith.Memories.Server.Ingestion;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using StackExchange.Redis;

/// <summary>Exact unit authority issued by the configured service after an operator-controlled writer cutover.</summary>
internal static class UnitOwnershipAuthority
{
    /// <summary>Maximum serialized authority characters accepted by both readers and writers.</summary>
    internal const int MaximumRecordCharacters = 4096;

    /// <summary>Serializes bounded authority before any Redis mutation.</summary>
    internal static string Serialize(UnitOwnershipRecord record)
    {
        string serialized = JsonSerializer.Serialize(record);
        if (serialized.Length > MaximumRecordCharacters)
            throw new InvalidOperationException("unit-ownership-authority-capacity");
        return serialized;
    }

    /// <summary>Validates the paired generation and external writer-exclusion custody reference.</summary>
    internal static void ValidateConfiguration(IngestionSettings settings)
    {
        if (settings.OwnershipGeneration is null && settings.OwnershipWriterExclusionEvidenceId is null) { return; }
        if (settings.OwnershipGeneration is not { } generation || !Regex.IsMatch(generation, "\\A[0-9a-f]{32}\\z")
            || string.IsNullOrWhiteSpace(settings.OwnershipWriterExclusionEvidenceId))
        { throw new InvalidOperationException("unit-ownership-generation-and-writer-exclusion-required"); }
        IngestionIdentityGuard.Handle(settings.OwnershipWriterExclusionEvidenceId);
    }

    /// <summary>Qualifies a service-generated identifier without changing historical identifiers.</summary>
    internal static string Qualify(string identifier, string? generation)
        => generation is null ? identifier : "owned-" + generation + "-" + identifier;

    /// <summary>Determines whether the exact unit belongs to the newly reserved generation namespace.</summary>
    internal static bool IsIssuedNamespace(string unit, string? generation)
        => generation is not null && unit.StartsWith("owned-" + generation + "-", StringComparison.Ordinal)
            && unit.Length > 39;

    /// <summary>Retains content-free authority independently of source and workflow lifetimes.</summary>
    internal static string Key(string tenant, string unit) => "ingestion-unit-authority:" + Digest(JsonSerializer.Serialize(new[] { tenant, unit }));

    /// <summary>Retains an immutable generation reservation independently of tenant deletion.</summary>
    internal static string GenerationKey(string generation) => "ingestion-ownership-generation:" + generation;

    /// <summary>Binds issuance to the original producer without storing source content or its URI.</summary>
    internal static string Producer(string workflow, string source, string caseId)
        => Digest(JsonSerializer.Serialize(new[] { workflow, source, caseId }));

    /// <summary>Checks both the exact unit identity and the retained authority witness before claiming coverage.</summary>
    internal static bool IsComplete(RedisValue provenance, RedisValue authority, string tenant, string unit)
    {
        if (!authority.HasValue || provenance != authority || authority.ToString().Length > MaximumRecordCharacters) { return false; }
        try
        {
            UnitOwnershipRecord? record = JsonSerializer.Deserialize<UnitOwnershipRecord>(authority.ToString());
            return record is not null && record.Tenant == tenant && record.Unit == unit
                && record.Kind is "issued-v2" or "historical-v2"
                && Regex.IsMatch(record.Generation ?? string.Empty, "\\A[0-9a-f]{32}\\z")
                && Regex.IsMatch(record.ProducerBinding ?? string.Empty, "\\A[0-9a-f]{64}\\z")
                && !string.IsNullOrWhiteSpace(record.EvidenceId) && !string.IsNullOrWhiteSpace(record.WriterExclusionEvidenceId)
                && (record.Kind != "issued-v2" || IsIssuedNamespace(unit, record.Generation));
        }
        catch (JsonException) { return false; }
    }

    /// <summary>Requires the original exact producer binding for an issuance-based never-scheduled cleanup.</summary>
    internal static bool IsProducer(RedisValue authority, string workflow, string source, string caseId)
    {
        try
        {
            UnitOwnershipRecord? record = JsonSerializer.Deserialize<UnitOwnershipRecord>(authority.ToString());
            return record?.Kind == "issued-v2" && record.ProducerBinding == Producer(workflow, source, caseId);
        }
        catch (JsonException) { return false; }
    }

    private static string Digest(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
