// <copyright file="ImportRequestValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

using Hexalith.Memories.Contracts.V1;

/// <summary>Validates an import manifest against the target import route (Story 26.2, AC1 + decision D2).</summary>
internal static class ImportRequestValidator
{
    /// <summary>The only export schema version the restore path understands.</summary>
    internal const int SupportedSchemaVersion = 1;

    /// <summary>
    /// Validates schema version, scope/route agreement, and same-tenant-id (and, for case scope, same-case-id)
    /// targeting. Story 26.2 scopes restore to same-tenant-id disaster recovery (decision D2); cross-tenant /
    /// cross-case id remapping is out of scope and rejected here rather than silently written.
    /// </summary>
    /// <param name="manifest">The parsed export manifest.</param>
    /// <param name="expectedScope">The scope implied by the route (case vs tenant).</param>
    /// <param name="routeTenantId">The tenant id from the route (the restore target).</param>
    /// <param name="routeCaseId">The case id from the route, for a case-scoped import; otherwise <see langword="null"/>.</param>
    /// <returns>An <see cref="ErrorResponse"/> to return as <c>400 Bad Request</c>, or <see langword="null"/> when valid.</returns>
    internal static ErrorResponse? Validate(
        ExportManifest manifest,
        ExportScope expectedScope,
        string routeTenantId,
        string? routeCaseId)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (manifest.SchemaVersion != SupportedSchemaVersion)
        {
            return new ErrorResponse(
                "IMPORT_SCHEMA_VERSION_UNSUPPORTED",
                $"Import schema version {manifest.SchemaVersion} is not supported; this server understands schema version {SupportedSchemaVersion}.",
                "Re-export the data from a compatible server version, then retry the import.");
        }

        if (manifest.Scope != expectedScope)
        {
            return new ErrorResponse(
                "IMPORT_SCOPE_MISMATCH",
                $"Import manifest scope '{manifest.Scope}' does not match the '{expectedScope}' import route.",
                expectedScope == ExportScope.Case
                    ? "Post a case-scoped export to the case import route, or post a tenant export to the tenant import route."
                    : "Post a tenant-scoped export to the tenant import route, or post a case export to the case import route.");
        }

        if (!string.Equals(manifest.TenantId, routeTenantId, StringComparison.Ordinal))
        {
            return new ErrorResponse(
                "IMPORT_TENANT_MISMATCH",
                $"Import manifest tenant '{manifest.TenantId}' does not match the target tenant '{routeTenantId}'.",
                "Story 26.2 supports same-tenant-id restore only. Restore into the tenant the export was taken from.");
        }

        if (expectedScope == ExportScope.Case
            && !string.Equals(manifest.CaseId, routeCaseId, StringComparison.Ordinal))
        {
            return new ErrorResponse(
                "IMPORT_CASE_MISMATCH",
                $"Import manifest case '{manifest.CaseId}' does not match the target case '{routeCaseId}'.",
                "Restore a case export into the same case id it was exported from.");
        }

        return null;
    }
}
