namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Recognizes only the output owner or the exact tenant/case/source Event dedup owner of original bytes.</summary>
internal static class SourcePayloadScope
{
    internal static bool IsExpectedOwner(string tenantId, string caseId, string sourceUri, SourceType sourceType,
        string memoryUnitId, string referenceMemoryUnitId)
        => string.Equals(referenceMemoryUnitId, memoryUnitId, StringComparison.Ordinal)
            || (sourceType == SourceType.Event && string.Equals(referenceMemoryUnitId,
                DedupKeyBuilder.BuildKey(tenantId, caseId, sourceUri), StringComparison.Ordinal));
}
