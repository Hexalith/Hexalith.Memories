// <copyright file="MemoriesRoutes.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Story 25.3 (audit finding A21) — the single source of truth for the Memories REST route templates. The
/// Server registers every endpoint against these constants and the REST client builds every request path from
/// them, so a route rename happens in exactly one place and cannot silently drift between the two sides (which
/// also protects the Dapr service-invocation ACL surface published at <c>docs/operations/route-surface.md</c>).
///
/// <para>
/// The template constants carry the leading slash and the ASP.NET route placeholders (e.g.
/// <c>/api/tenants/{tenantId}</c>) exactly as the Server maps them. The client-facing <c>*Path</c> builders
/// return the <b>relative</b> form (no leading slash, resolved against <see cref="System.Net.Http.HttpClient.BaseAddress"/>)
/// with every segment value <see cref="System.Uri.EscapeDataString(string)"/>-escaped. Query strings are NOT
/// modelled here — the client owns query composition; this table owns path templates only.
/// </para>
///
/// <para>
/// This is not a wire DTO: it is intentionally absent from <see cref="MemoriesJsonContext"/> and adds no
/// ASP.NET Core / routing dependency to the Contracts package.
/// </para>
/// </summary>
public static class MemoriesRoutes
{
    /// <summary>The common <c>/api</c> path prefix shared by every REST route.</summary>
    public const string ApiPrefix = "/api";

    // ---- Ingestion ----

    /// <summary>File ingestion submission route (<c>POST /api/ingest</c>).</summary>
    public const string Ingest = "/api/ingest";

    /// <summary>Ingestion workflow status route (<c>GET /api/ingest/{instanceId}</c>).</summary>
    public const string IngestStatus = "/api/ingest/{instanceId}";

    /// <summary>URL ingestion submission route (<c>POST /api/ingest/url</c>).</summary>
    public const string IngestUrl = "/api/ingest/url";

    /// <summary>Directory ingestion submission route (<c>POST /api/ingest/directory</c>).</summary>
    public const string IngestDirectory = "/api/ingest/directory";

    /// <summary>Directory ingestion batch status route (<c>GET /api/ingest/batches/{batchId}</c>).</summary>
    public const string IngestBatchStatus = "/api/ingest/batches/{batchId}";

    // ---- Search ----

    /// <summary>Search route (<c>GET /api/search</c>).</summary>
    public const string Search = "/api/search";

    // ---- Graph ----

    /// <summary>Graph traversal route (<c>GET /api/tenants/{tenantId}/traverse</c>).</summary>
    public const string Traverse = "/api/tenants/{tenantId}/traverse";

    /// <summary>Edge confidence promotion route (<c>PATCH /api/tenants/{tenantId}/edges/confidence</c>).</summary>
    public const string EdgeConfidence = "/api/tenants/{tenantId}/edges/confidence";

    // ---- Tenant lifecycle ----

    /// <summary>Tenant collection route (<c>POST</c> create / <c>GET</c> list at <c>/api/tenants</c>).</summary>
    public const string Tenants = "/api/tenants";

    /// <summary>Single tenant route (<c>GET</c> / <c>PATCH</c> / <c>DELETE</c> at <c>/api/tenants/{tenantId}</c>).</summary>
    public const string Tenant = "/api/tenants/{tenantId}";

    /// <summary>Tenant embedding-config route (<c>GET</c> / <c>PUT</c> at <c>/api/tenants/{tenantId}/embedding-config</c>).</summary>
    public const string TenantEmbeddingConfig = "/api/tenants/{tenantId}/embedding-config";

    /// <summary>Tenant provisioning workflow status route (<c>GET /api/tenants/{tenantId}/provision-status/{instanceId}</c>).</summary>
    public const string TenantProvisionStatus = "/api/tenants/{tenantId}/provision-status/{instanceId}";

    /// <summary>Tenant configuration route (<c>GET /api/tenants/{tenantId}/configuration</c>).</summary>
    public const string TenantConfiguration = "/api/tenants/{tenantId}/configuration";

    /// <summary>Tenant deletion workflow status route (<c>GET /api/tenants/{tenantId}/deletion-status/{instanceId}</c>).</summary>
    public const string TenantDeletionStatus = "/api/tenants/{tenantId}/deletion-status/{instanceId}";

    /// <summary>Tenant isolation verification route (<c>POST /api/tenants/{tenantId}/verify</c>).</summary>
    public const string TenantVerify = "/api/tenants/{tenantId}/verify";

    /// <summary>Tenant telemetry summary route (<c>GET /api/tenants/{tenantId}/telemetry/summary</c>).</summary>
    public const string TenantTelemetrySummary = "/api/tenants/{tenantId}/telemetry/summary";

    /// <summary>Handler registry snapshot route (<c>GET /api/handlers</c>). Experimental (<c>HXL002</c>).</summary>
    public const string Handlers = "/api/handlers";

    /// <summary>Tenant handler-mismatch report route (<c>GET /api/tenants/{tenantId}/handlers/mismatches</c>). Experimental (<c>HXL002</c>).</summary>
    public const string TenantHandlerMismatches = "/api/tenants/{tenantId}/handlers/mismatches";

    // ---- Cases ----

    /// <summary>Case collection route (<c>POST</c> create / <c>GET</c> list at <c>/api/tenants/{tenantId}/cases</c>).</summary>
    public const string Cases = "/api/tenants/{tenantId}/cases";

    /// <summary>Single case route (<c>GET</c> / <c>DELETE</c> at <c>/api/tenants/{tenantId}/cases/{caseId}</c>).</summary>
    public const string Case = "/api/tenants/{tenantId}/cases/{caseId}";

    /// <summary>Case status route (<c>GET /api/tenants/{tenantId}/cases/{caseId}/status</c>).</summary>
    public const string CaseStatus = "/api/tenants/{tenantId}/cases/{caseId}/status";

    /// <summary>Case failed-units route (<c>GET /api/tenants/{tenantId}/cases/{caseId}/failed-units</c>).</summary>
    public const string CaseFailedUnits = "/api/tenants/{tenantId}/cases/{caseId}/failed-units";

    /// <summary>Single case memory-unit route (<c>GET</c> / <c>DELETE</c> at <c>/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}</c>).</summary>
    public const string CaseMemoryUnit = "/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}";

    /// <summary>Source-URI-keyed memory-unit lookup route (<c>GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri</c>).</summary>
    public const string CaseMemoryUnitBySourceUri = "/api/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri";

    /// <summary>Single memory-unit re-ingestion route (<c>POST /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/re-ingest</c>).</summary>
    public const string CaseMemoryUnitReIngest = "/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/re-ingest";

    /// <summary>Bulk failed-units re-ingestion route (<c>POST /api/tenants/{tenantId}/cases/{caseId}/failed-units/re-ingest</c>).</summary>
    public const string CaseFailedUnitsReIngest = "/api/tenants/{tenantId}/cases/{caseId}/failed-units/re-ingest";

    /// <summary>Case activity route (<c>GET /api/tenants/{tenantId}/cases/{caseId}/activity</c>).</summary>
    public const string CaseActivity = "/api/tenants/{tenantId}/cases/{caseId}/activity";

    /// <summary>Single case member route (<c>PUT</c> / <c>DELETE</c> at <c>/api/tenants/{tenantId}/cases/{caseId}/members/{memberId}</c>).</summary>
    public const string CaseMember = "/api/tenants/{tenantId}/cases/{caseId}/members/{memberId}";

    /// <summary>Case member collection route (<c>GET /api/tenants/{tenantId}/cases/{caseId}/members</c>).</summary>
    public const string CaseMembers = "/api/tenants/{tenantId}/cases/{caseId}/members";

    /// <summary>Memory-unit annotations route (<c>POST</c> create / <c>GET</c> list at <c>/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations</c>).</summary>
    public const string CaseMemoryUnitAnnotations = "/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations";

    // ---- Consistency ----

    /// <summary>Consistency verification scheduling route (<c>POST /api/tenants/{tenantId}/consistency/verify</c>).</summary>
    public const string ConsistencyVerify = "/api/tenants/{tenantId}/consistency/verify";

    /// <summary>Consistency verification status route (<c>GET /api/tenants/{tenantId}/consistency/verify/{instanceId}</c>).</summary>
    public const string ConsistencyVerifyStatus = "/api/tenants/{tenantId}/consistency/verify/{instanceId}";

    /// <summary>Per-unit consistency inspection route (<c>GET /api/tenants/{tenantId}/consistency/inspect/{memoryUnitId}</c>).</summary>
    public const string ConsistencyInspect = "/api/tenants/{tenantId}/consistency/inspect/{memoryUnitId}";

    /// <summary>Consistency repair scheduling route (<c>POST /api/tenants/{tenantId}/consistency/repair</c>).</summary>
    public const string ConsistencyRepair = "/api/tenants/{tenantId}/consistency/repair";

    /// <summary>Consistency repair status route (<c>GET /api/tenants/{tenantId}/consistency/repair/{instanceId}</c>).</summary>
    public const string ConsistencyRepairStatus = "/api/tenants/{tenantId}/consistency/repair/{instanceId}";

    // ---- Export ----

    /// <summary>Case export route (<c>GET /api/tenants/{tenantId}/cases/{caseId}/export</c>).</summary>
    public const string CaseExport = "/api/tenants/{tenantId}/cases/{caseId}/export";

    /// <summary>Tenant export route (<c>GET /api/tenants/{tenantId}/export</c>).</summary>
    public const string TenantExport = "/api/tenants/{tenantId}/export";

    // ---- Client-facing relative path builders (segment values escaped) ----

    /// <summary>Builds the relative client request path for <c>GET /api/search</c> (query appended by the caller).</summary>
    /// <returns>The relative request path.</returns>
    public static string SearchPath() => Relative(Search);

    /// <summary>Builds the relative client request path for <c>POST /api/ingest</c>.</summary>
    /// <returns>The relative request path.</returns>
    public static string IngestPath() => Relative(Ingest);

    /// <summary>Builds the relative client request path for the tenant collection (<c>/api/tenants</c>).</summary>
    /// <returns>The relative request path.</returns>
    public static string TenantsPath() => Relative(Tenants);

    /// <summary>Builds the relative client request path for a single tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The relative request path with the tenant segment escaped.</returns>
    public static string TenantPath(string tenantId) => Fill(Tenant, ("tenantId", tenantId));

    /// <summary>Builds the relative client request path for the tenant telemetry summary.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The relative request path with the tenant segment escaped.</returns>
    public static string TenantTelemetrySummaryPath(string tenantId) => Fill(TenantTelemetrySummary, ("tenantId", tenantId));

    /// <summary>Builds the relative client request path for the handler registry snapshot (<c>/api/handlers</c>).</summary>
    /// <returns>The relative request path.</returns>
    public static string HandlersPath() => Relative(Handlers);

    /// <summary>Builds the relative client request path for a tenant handler-mismatch report.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The relative request path with the tenant segment escaped.</returns>
    public static string TenantHandlerMismatchesPath(string tenantId) => Fill(TenantHandlerMismatches, ("tenantId", tenantId));

    /// <summary>Builds the relative client request path for a graph traversal (query appended by the caller).</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The relative request path with the tenant segment escaped.</returns>
    public static string TraversePath(string tenantId) => Fill(Traverse, ("tenantId", tenantId));

    /// <summary>Builds the relative client request path for the case collection under a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The relative request path with the tenant segment escaped.</returns>
    public static string CasesPath(string tenantId) => Fill(Cases, ("tenantId", tenantId));

    /// <summary>Builds the relative client request path for a single case.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <returns>The relative request path with the tenant and case segments escaped.</returns>
    public static string CasePath(string tenantId, string caseId) => Fill(Case, ("tenantId", tenantId), ("caseId", caseId));

    /// <summary>Builds the relative client request path for a single memory unit within a case.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="memoryUnitId">The memory unit identifier.</param>
    /// <returns>The relative request path with all segments escaped.</returns>
    public static string CaseMemoryUnitPath(string tenantId, string caseId, string memoryUnitId)
        => Fill(CaseMemoryUnit, ("tenantId", tenantId), ("caseId", caseId), ("memoryUnitId", memoryUnitId));

    /// <summary>Builds the relative client request path for the source-URI-keyed lookup (query appended by the caller).</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <returns>The relative request path with the tenant and case segments escaped.</returns>
    public static string CaseMemoryUnitBySourceUriPath(string tenantId, string caseId)
        => Fill(CaseMemoryUnitBySourceUri, ("tenantId", tenantId), ("caseId", caseId));

    /// <summary>Builds the relative client request path for consistency verification scheduling.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The relative request path with the tenant segment escaped.</returns>
    public static string ConsistencyVerifyPath(string tenantId) => Fill(ConsistencyVerify, ("tenantId", tenantId));

    /// <summary>Builds the relative client request path for a consistency verification workflow status.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="instanceId">The workflow instance identifier.</param>
    /// <returns>The relative request path with the tenant and instance segments escaped.</returns>
    public static string ConsistencyVerifyStatusPath(string tenantId, string instanceId)
        => Fill(ConsistencyVerifyStatus, ("tenantId", tenantId), ("instanceId", instanceId));

    /// <summary>Builds the relative client request path for a per-unit consistency inspection.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="memoryUnitId">The memory unit identifier.</param>
    /// <returns>The relative request path with the tenant and memory-unit segments escaped.</returns>
    public static string ConsistencyInspectPath(string tenantId, string memoryUnitId)
        => Fill(ConsistencyInspect, ("tenantId", tenantId), ("memoryUnitId", memoryUnitId));

    /// <summary>Builds the relative client request path for consistency repair scheduling.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The relative request path with the tenant segment escaped.</returns>
    public static string ConsistencyRepairPath(string tenantId) => Fill(ConsistencyRepair, ("tenantId", tenantId));

    /// <summary>Builds the relative client request path for a consistency repair workflow status.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="instanceId">The workflow instance identifier.</param>
    /// <returns>The relative request path with the tenant and instance segments escaped.</returns>
    public static string ConsistencyRepairStatusPath(string tenantId, string instanceId)
        => Fill(ConsistencyRepairStatus, ("tenantId", tenantId), ("instanceId", instanceId));

    /// <summary>Builds the relative client request path for a case export.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <returns>The relative request path with the tenant and case segments escaped.</returns>
    public static string CaseExportPath(string tenantId, string caseId) => Fill(CaseExport, ("tenantId", tenantId), ("caseId", caseId));

    /// <summary>Builds the relative client request path for a tenant export.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The relative request path with the tenant segment escaped.</returns>
    public static string TenantExportPath(string tenantId) => Fill(TenantExport, ("tenantId", tenantId));

    /// <summary>Returns the relative (leading-slash-trimmed) form of a template that has no placeholders.</summary>
    /// <param name="template">The absolute route template.</param>
    /// <returns>The template without its leading slash.</returns>
    private static string Relative(string template) => template.TrimStart('/');

    /// <summary>
    /// Substitutes each <c>{token}</c> placeholder in <paramref name="template"/> with its
    /// <see cref="System.Uri.EscapeDataString(string)"/>-escaped value and returns the relative
    /// (leading-slash-trimmed) result.
    /// </summary>
    /// <param name="template">The absolute route template carrying <c>{token}</c> placeholders.</param>
    /// <param name="tokens">The placeholder name/value pairs to substitute (values are URL-escaped).</param>
    /// <returns>The relative request path with every placeholder replaced by its escaped value.</returns>
    private static string Fill(string template, params (string Token, string Value)[] tokens)
    {
        string result = template;
        foreach ((string token, string value) in tokens)
        {
            result = result.Replace("{" + token + "}", Uri.EscapeDataString(value), StringComparison.Ordinal);
        }

        return result.TrimStart('/');
    }
}
