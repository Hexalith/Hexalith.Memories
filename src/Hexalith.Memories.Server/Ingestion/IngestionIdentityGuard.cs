namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Server.Activities.Indexing;

/// <summary>Shared producer and inventory identity bounds, checked before durable side effects.</summary>
internal static class IngestionIdentityGuard
{
    /// <summary>Validates the tenant resource and public inventory scope.</summary>
    internal static void Scope(string tenant, string source)
    {
        TenantIdGuard.Validate(tenant);
        Handle(tenant, 128);
        Handle(source, 4096);
    }

    /// <summary>Validates an exact, bounded identifier without control characters.</summary>
    internal static void Handle(string value, int maximum = 512)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximum || value.Any(char.IsControl))
        {
            throw new ArgumentException("Ingestion identity exceeds its supported bound or contains control characters.");
        }
    }

    /// <summary>Validates every producer identity before preparation or admission.</summary>
    internal static void Input(IngestionInput input, string? workflow = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        Scope(input.TenantId, input.SourceUri);
        Handle(input.CaseId);
        if (workflow is not null) { Handle(workflow); }
        if (input.ExpectedMemoryUnitId is not null) { Handle(input.ExpectedMemoryUnitId); }
        if (!string.IsNullOrWhiteSpace(input.IdempotencyToken)) { Handle(input.IdempotencyToken, 200); }
        else if (input.IdempotencyToken?.Any(char.IsControl) == true) { throw new ArgumentException("Token contains control characters."); }
        if (input.ReopenSourceVersion is not null) { Handle(input.ReopenSourceVersion, 200); }
        if (input.PayloadReference is { } reference)
        {
            PayloadReference(reference);
        }
    }

    /// <summary>Validates the documented bounded claim-check shape before retention or durable reads.</summary>
    internal static void PayloadReference(WorkflowPayloadReference reference)
    {
        Handle(reference.MemoryUnitId);
        TenantIdGuard.Validate(reference.TenantId);
        Handle(reference.TenantId, 128);
        Handle(reference.Id, 2048);
        if (reference.Sha256Hash is not { Length: 64 } hash || hash.Any(character => !char.IsAsciiHexDigit(character)))
        {
            throw new ArgumentException("PayloadReference requires a 64-character hexadecimal SHA-256 hash.");
        }
    }

    /// <summary>Validates all identities exposed through bounded inventory readers.</summary>
    internal static void Entry(SourceInventoryEntry entry)
    {
        Handle(entry.CaseId);
        if (entry.WorkflowInstanceId is not null) { Handle(entry.WorkflowInstanceId); }
        if (entry.MemoryUnitId is not null) { Handle(entry.MemoryUnitId); }
        if (entry.CandidateMemoryUnitId is not null) { Handle(entry.CandidateMemoryUnitId); }
        if (entry.SourcePayloadReference is { } reference)
        {
            PayloadReference(reference);
        }
    }
}
