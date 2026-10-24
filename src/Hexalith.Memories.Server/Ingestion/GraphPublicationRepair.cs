namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Content-free durable repair authority committed atomically with the Redis publication pointer.</summary>
/// <param name="TenantId">The exact tenant graph.</param>
/// <param name="CaseId">The captured source case.</param>
/// <param name="SourceUri">The canonical source URI.</param>
/// <param name="MemoryUnitId">The committed Redis canonical output.</param>
/// <param name="Revision">The authoritative monotonic source revision.</param>
internal sealed record GraphPublicationRepair(string TenantId, string CaseId, string SourceUri, string MemoryUnitId, long Revision);
