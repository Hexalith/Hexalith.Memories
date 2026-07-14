// <copyright file="ImportEnvelopeValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

using Hexalith.Memories.Contracts.V1;

/// <summary>Validates every tenant- and case-bearing record before restore mutates either data plane.</summary>
internal static class ImportEnvelopeValidator
{
    /// <summary>Ensures the complete envelope belongs to the requested target scope.</summary>
    internal static void EnsureTargetScope(ImportEnvelope envelope, string tenantId, string? caseId)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        EnsureManifestTarget(envelope.Manifest, tenantId, caseId);

        foreach (ImportedCase importedCase in envelope.Cases)
        {
            EnsureCaseTarget(importedCase, tenantId, caseId);
        }

        foreach (ExportedMemoryUnit exported in envelope.MemoryUnits)
        {
            EnsureMemoryUnitTarget(exported, tenantId, caseId);
        }
    }

    /// <summary>Validates the manifest against the route-derived target scope.</summary>
    internal static void EnsureManifestTarget(ExportManifest manifest, string tenantId, string? caseId)
    {
        ExportScope expectedScope = caseId is null ? ExportScope.Tenant : ExportScope.Case;
        ErrorResponse? manifestError = ImportRequestValidator.Validate(
            manifest,
            expectedScope,
            tenantId,
            caseId);
        if (manifestError is not null)
        {
            throw new ImportEnvelopeException(manifestError.Code, manifestError.Message);
        }
    }

    /// <summary>Validates one streamed case record against the target.</summary>
    internal static void EnsureCaseTarget(ImportedCase importedCase, string tenantId, string? caseId)
    {
        Case value = importedCase.Case;
        if (string.IsNullOrWhiteSpace(value.Id)
            || !string.Equals(value.TenantId, tenantId, StringComparison.Ordinal)
            || (caseId is not null && !string.Equals(value.Id, caseId, StringComparison.Ordinal)))
        {
            throw new ImportEnvelopeException(
                "IMPORT_RECORD_SCOPE_MISMATCH",
                $"Case '{value.Id}' does not belong to restore target tenant '{tenantId}'" +
                (caseId is null ? "." : $" and case '{caseId}'."));
        }
    }

    /// <summary>Validates one streamed memory-unit record against the target.</summary>
    internal static void EnsureMemoryUnitTarget(ExportedMemoryUnit exported, string tenantId, string? caseId)
    {
        MemoryUnit unit = exported.Unit
            ?? throw new ImportEnvelopeException("MALFORMED_IMPORT", "A memoryUnits entry is missing its unit object.");
        if (string.IsNullOrWhiteSpace(unit.Id)
            || string.IsNullOrWhiteSpace(unit.CaseId)
            || !string.Equals(unit.TenantId, tenantId, StringComparison.Ordinal)
            || (caseId is not null && !string.Equals(unit.CaseId, caseId, StringComparison.Ordinal)))
        {
            throw new ImportEnvelopeException(
                "IMPORT_RECORD_SCOPE_MISMATCH",
                $"Memory unit '{unit.Id}' does not belong to restore target tenant '{tenantId}'" +
                (caseId is null ? "." : $" and case '{caseId}'."));
        }

        if (unit.EmbeddingDimensions is <= 0)
        {
            throw new ImportEnvelopeException(
                "IMPORT_EMBEDDING_DIMENSIONS_INVALID",
                $"Memory unit '{unit.Id}' has invalid embedding dimensions '{unit.EmbeddingDimensions}'.");
        }
    }
}
