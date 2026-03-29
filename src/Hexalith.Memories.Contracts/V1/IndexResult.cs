namespace Hexalith.Memories.Contracts.V1;

/// <summary>Result from an indexing activity. Success is implicit (returned without throwing).</summary>
public sealed record IndexResult(string Backend, string MemoryUnitId, string TenantId);
