namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Minimal immutable unit issuance or exact-unit historical reconciliation authority.</summary>
/// <param name="Kind">The issuance or historical reconciliation protocol.</param>
/// <param name="Tenant">The exact tenant.</param>
/// <param name="Unit">The exact unit, independent of source aliases.</param>
/// <param name="Generation">The operator-reserved issuance generation.</param>
/// <param name="ProducerBinding">The original producer binding digest.</param>
/// <param name="EvidenceId">The exact issuance or historical inventory evidence identity.</param>
/// <param name="WriterExclusionEvidenceId">The operator's external writer-exclusion custody reference.</param>
internal sealed record UnitOwnershipRecord(string Kind, string Tenant, string Unit, string Generation,
    string ProducerBinding, string EvidenceId, string WriterExclusionEvidenceId);
