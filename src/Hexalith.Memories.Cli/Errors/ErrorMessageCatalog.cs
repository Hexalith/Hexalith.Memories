// <copyright file="ErrorMessageCatalog.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Errors;

/// <summary>
/// Static lookup table mapping server <c>ErrorResponse.Code</c> literals (and synthetic CLI transport
/// codes) to the CLI-rendered <see cref="ErrorTranslation"/>. The catalog covers every
/// <c>ErrorResponse.Code</c> literal emitted by the server source at the time Story 7.3 ships,
/// plus the synthetic CLI transport/HTTP codes documented in ADR-7.3-001.
/// <para>
/// Contributors adding new server error codes MUST add a corresponding entry here. The drift-detection
/// test (<c>ErrorCatalogDriftTests</c>) fails CI if a code exists in server source but is missing from
/// this catalog. The test still exposes a temporary <c>KnownUnmappedCodes</c> escape hatch for
/// concurrent-PR recovery, but that allow-list should normally stay empty on <c>main</c>. Default
/// fall-through for unknown codes is <c>ExitCode = 1</c> (domain) because a structured
/// <c>ErrorResponse</c> from the server is, by construction, a server-reported-domain-shaped failure.
/// Explicitly set
/// <c>ExitCode = 2</c> for codes that signal infrastructure plumbing (DAPR/Redis/FalkorDB unreachable)
/// rather than user/data errors.
/// </para>
/// <para>
/// Per-code exit-code classifications are <em>policy decisions</em> tuned in Story 7.3, not a factual
/// mapping derived from server semantics. Edge cases (<c>RE_INGESTION_IN_PROGRESS</c>,
/// <c>MEMBER_LIMIT_EXCEEDED</c>, <c>BATCH_TOO_LARGE</c>) are judgment calls. Expect churn from
/// operator feedback post-Phase-1.5.
/// </para>
/// </summary>
public static class ErrorMessageCatalog
{
    /// <summary>Default suggestion used when the server returns a code the catalog does not handle.</summary>
    public const string UnknownCodeSuggestion = "Run with --verbose for diagnostic detail.";

    /// <summary>
    /// Gets the catalog of error code translations. Keys are server codes (e.g. <c>TENANT_NOT_FOUND</c>)
    /// or synthetic CLI transport codes (e.g. <c>CONNECTION_REFUSED</c>) or <c>HTTP_&lt;status&gt;</c>
    /// synthetic codes from <c>ErrorResponseDecoder</c>.
    /// </summary>
    public static IReadOnlyDictionary<string, ErrorTranslation> Translations { get; } = BuildTranslations();

    /// <summary>
    /// Resolves the translation for <paramref name="code"/>. Unknown codes return a default translation
    /// pointing at <c>--verbose</c> for diagnostic detail and exit code <c>1</c> (domain). Never throws.
    /// </summary>
    /// <param name="code">The server-reported or synthetic error code.</param>
    /// <returns>The catalog entry or a sensible default.</returns>
    public static ErrorTranslation Resolve(string? code)
    {
        if (!string.IsNullOrEmpty(code) && Translations.TryGetValue(code, out ErrorTranslation? translation))
        {
            return translation;
        }

        return new ErrorTranslation(CliMessage: null, CliSuggestion: UnknownCodeSuggestion, ExitCode: 1);
    }

    private static IReadOnlyDictionary<string, ErrorTranslation> BuildTranslations()
    {
        var map = new Dictionary<string, ErrorTranslation>(StringComparer.Ordinal)
        {
            // === Domain errors (exit 1) reachable from wired commands ===

            // tenant list, search query, search inspect — tenant not found
            ["TENANT_NOT_FOUND"] = new(
                CliMessage: null,
                CliSuggestion: "Run 'memories tenant list' to see available tenants.",
                ExitCode: 1),

            // Every tenant-validation path — invalid id format.
            ["INVALID_TENANT_ID"] = new(
                CliMessage: null,
                CliSuggestion: "Tenant ids must be alphanumeric with hyphens only. Run 'memories tenant list' to see examples.",
                ExitCode: 1),

            // search inspect, case-scoped search — case not found.
            // Fallback wording: 'memories case list' is a NotImplementedCommand stub today.
            // TODO(7.x): replace with "memories case list --tenant <id>" once the case group is wired.
            ["CASE_NOT_FOUND"] = new(
                CliMessage: null,
                CliSuggestion: "List tenants with 'memories tenant list' and verify the case via the server's REST API at GET /api/tenants/{tenantId}/cases.",
                ExitCode: 1),

            // search inspect — cross-case lookup mismatch.
            ["CASE_MISMATCH"] = new(
                CliMessage: null,
                CliSuggestion: "Verify the case id using the memory unit's failed-units list.",
                ExitCode: 1),

            // search inspect — memory unit not found or not yet indexed.
            ["MEMORY_UNIT_NOT_FOUND"] = new(
                CliMessage: null,
                CliSuggestion: "Verify the memory unit id. Use 'memories search query --tenant <id> --query <terms>' to list known units.",
                ExitCode: 1),
            ["MEMORY_UNIT_NOT_INDEXED"] = new(
                CliMessage: null,
                CliSuggestion: "Indexing is still in progress. Retry in a few seconds.",
                ExitCode: 1),

            // search query — generic input validation + axis validation.
            ["INVALID_INPUT"] = new(
                CliMessage: null,
                CliSuggestion: "Review the command arguments. Run 'memories search query --help' for valid inputs.",
                ExitCode: 1),
            ["INVALID_REQUEST"] = new(
                CliMessage: null,
                CliSuggestion: "Review the command arguments. Run 'memories --help' for valid inputs.",
                ExitCode: 1),
            ["INVALID_AXIS"] = new(
                CliMessage: null,
                CliSuggestion: "Use one of: syntactic, semantic, graph, hybrid.",
                ExitCode: 1),

            // config show — server config rejection.
            ["INVALID_CONFIG"] = new(
                CliMessage: null,
                CliSuggestion: "Fix the configuration values and retry.",
                ExitCode: 1),

            // Tenant transient states — surface as domain (user/timing) rather than plumbing.
            // Fallback wording: 'memories tenant status' is not yet wired.
            // TODO(5.x/7.x): replace with "memories tenant status --id <id>" once wired.
            ["TENANT_DELETING"] = new(
                CliMessage: null,
                CliSuggestion: "Tenant is being deleted. Re-list tenants in a few seconds with 'memories tenant list' to confirm removal.",
                ExitCode: 1),
            ["TENANT_PROVISIONING"] = new(
                CliMessage: null,
                CliSuggestion: "Tenant is still provisioning. Re-list tenants in a few seconds with 'memories tenant list'.",
                ExitCode: 1),
            ["TENANT_FAILED"] = new(
                CliMessage: null,
                CliSuggestion: "Tenant provisioning failed. Re-list tenants with 'memories tenant list' and contact an operator to retry provisioning.",
                ExitCode: 1),
            ["TENANT_UNAVAILABLE"] = new(
                CliMessage: null,
                CliSuggestion: "Tenant is temporarily unavailable. Re-list tenants with 'memories tenant list' in a few seconds.",
                ExitCode: 1),

            // member endpoints (not yet wired in CLI but reachable server-side for completeness).
            ["MEMBER_NOT_FOUND"] = new(
                CliMessage: null,
                CliSuggestion: "Verify the member id. The case-member CLI surface is not yet wired; use the server's REST API in the meantime.",
                ExitCode: 1),
            ["MEMBER_LIMIT_EXCEEDED"] = new(
                CliMessage: null,
                CliSuggestion: "Remove existing members before adding new ones.",
                ExitCode: 1),

            // Case/admin validation and lifecycle surface.
            ["INVALID_CASE_ID"] = new(
                CliMessage: null,
                CliSuggestion: "Use a valid case id and retry. The case CLI surface is not fully wired yet; use the server's REST API to inspect known cases.",
                ExitCode: 1),
            ["INVALID_CASE_NAME"] = new(
                CliMessage: null,
                CliSuggestion: "Provide a non-empty case name and retry.",
                ExitCode: 1),
            ["INVALID_CASE_DESCRIPTION"] = new(
                CliMessage: null,
                CliSuggestion: "Shorten or sanitize the case description and retry.",
                ExitCode: 1),
            ["INVALID_MEMORY_UNIT_ID"] = new(
                CliMessage: null,
                CliSuggestion: "Provide a valid memory unit id and retry.",
                ExitCode: 1),
            ["CASE_DELETING"] = new(
                CliMessage: null,
                CliSuggestion: "Case deletion is in progress. Retry after it completes, or inspect case state via the server's REST API.",
                ExitCode: 1),

            // Annotation/correction surface.
            ["INVALID_ANNOTATION_CONTENT"] = new(
                CliMessage: null,
                CliSuggestion: "Provide non-empty annotation content and retry.",
                ExitCode: 1),
            ["INVALID_ANNOTATION_TYPE"] = new(
                CliMessage: null,
                CliSuggestion: "Use a supported annotation type and retry.",
                ExitCode: 1),
            ["INVALID_CONFIDENCE"] = new(
                CliMessage: null,
                CliSuggestion: "Confidence must be between 0 and 1.",
                ExitCode: 1),
            ["INVALID_INGESTED_BY"] = new(
                CliMessage: null,
                CliSuggestion: "Provide a valid ingested-by identifier and retry.",
                ExitCode: 1),
            ["MISSING_NEW_CONFIDENCE"] = new(
                CliMessage: null,
                CliSuggestion: "Provide the new confidence value and retry.",
                ExitCode: 1),
            ["MISSING_VERIFIED_BY"] = new(
                CliMessage: null,
                CliSuggestion: "Provide the verifier identity and retry.",
                ExitCode: 1),
            ["NESTED_ANNOTATION_NOT_ALLOWED"] = new(
                CliMessage: null,
                CliSuggestion: "Apply annotations to the memory unit directly rather than nesting them.",
                ExitCode: 1),

            // Member/edge/traversal admin surface.
            ["INVALID_MEMBER_ID"] = new(
                CliMessage: null,
                CliSuggestion: "Provide a valid member id and retry.",
                ExitCode: 1),
            ["INVALID_MEMBER_TYPE"] = new(
                CliMessage: null,
                CliSuggestion: "Use a supported member type and retry.",
                ExitCode: 1),
            ["INVALID_MEMBER_INPUT"] = new(
                CliMessage: null,
                CliSuggestion: "Review the member payload and retry.",
                ExitCode: 1),
            ["INVALID_REQUEST_BODY"] = new(
                CliMessage: null,
                CliSuggestion: "Fix the request payload and retry.",
                ExitCode: 1),
            ["EDGE_NOT_FOUND"] = new(
                CliMessage: null,
                CliSuggestion: "Verify the edge identifiers and retry. The traversal CLI surface is not fully wired yet; use the server's REST API if needed.",
                ExitCode: 1),
            ["INVALID_EDGE_TYPE"] = new(
                CliMessage: null,
                CliSuggestion: "Use a supported edge type and retry.",
                ExitCode: 1),
            ["MISSING_EDGE_TYPE"] = new(
                CliMessage: null,
                CliSuggestion: "Specify an edge type and retry.",
                ExitCode: 1),
            ["MISSING_SOURCE_NODE"] = new(
                CliMessage: null,
                CliSuggestion: "Specify the source node id and retry.",
                ExitCode: 1),
            ["MISSING_START_NODE"] = new(
                CliMessage: null,
                CliSuggestion: "Specify the start node id and retry.",
                ExitCode: 1),
            ["MISSING_TARGET_NODE"] = new(
                CliMessage: null,
                CliSuggestion: "Specify the target node id and retry.",
                ExitCode: 1),

            // Batch / ingestion admin surface.
            ["BATCH_NOT_FOUND"] = new(
                CliMessage: null,
                CliSuggestion: "Verify the batch id and retry. Use the server's REST API to inspect ingestion status until the CLI status surface is wired.",
                ExitCode: 1),
            ["BATCH_TOO_LARGE"] = new(
                CliMessage: null,
                CliSuggestion: "Split the ingest request into smaller batches and retry.",
                ExitCode: 1),
            ["RE_INGESTION_IN_PROGRESS"] = new(
                CliMessage: null,
                CliSuggestion: "Re-ingestion is already running. Retry after the current operation finishes.",
                ExitCode: 1),
            ["DIRECTORY_INGESTION_DISABLED"] = new(
                CliMessage: null,
                CliSuggestion: "Enable directory ingestion in server configuration or use a supported source type.",
                ExitCode: 1),
            ["INVALID_DIRECTORY_PATH"] = new(
                CliMessage: null,
                CliSuggestion: "Provide an existing directory path accessible to the server.",
                ExitCode: 1),
            ["INVALID_SOURCE_TYPE"] = new(
                CliMessage: null,
                CliSuggestion: "Use a supported source type and retry.",
                ExitCode: 1),
            ["INVALID_URL"] = new(
                CliMessage: null,
                CliSuggestion: "Provide a valid absolute URL and retry.",
                ExitCode: 1),
            ["DELETION_STATUS_NOT_FOUND"] = new(
                CliMessage: null,
                CliSuggestion: "Verify the deletion-tracking id and retry.",
                ExitCode: 1),
            ["PROVISIONING_STATUS_NOT_FOUND"] = new(
                CliMessage: null,
                CliSuggestion: "Verify the provisioning-tracking id and retry.",
                ExitCode: 1),

            // Embedding / vector configuration.
            ["EMBEDDING_UNAVAILABLE"] = new(
                CliMessage: null,
                CliSuggestion: "Embedding provider unavailable. Retry shortly and check embedding-service configuration.",
                ExitCode: 2),
            ["INVALID_DIMENSIONS"] = new(
                CliMessage: null,
                CliSuggestion: "Provide a positive dimensions value matching the embedding provider configuration.",
                ExitCode: 1),
            ["DIMENSION_MISMATCH"] = new(
                CliMessage: null,
                CliSuggestion: "Ensure the configured embedding dimensions match the provider and vector index dimensions.",
                ExitCode: 1),

            // Tenant admin surface beyond list.
            ["TENANT_UPDATE_CONFLICT"] = new(
                CliMessage: null,
                CliSuggestion: "Reload tenant state with 'memories tenant list' and retry the update.",
                ExitCode: 1),

            // === Server-side infrastructure plumbing (exit 2) ===
            // Used in BOTH fatal (HTTP 503) and per-axis-degraded contexts. Wording must work in both.
            ["DAPR_UNAVAILABLE"] = new(
                CliMessage: null,
                CliSuggestion: "DAPR sidecar is unavailable. Check DAPR sidecar connectivity and retry.",
                ExitCode: 2),

            ["BATCH_TRACKING_UNAVAILABLE"] = new(
                CliMessage: null,
                CliSuggestion: "Batch tracking infrastructure is unavailable. Retry shortly and check service health if the failure persists.",
                ExitCode: 2),

            ["BATCH_SCHEDULING_FAILED"] = new(
                CliMessage: null,
                CliSuggestion: "Batch scheduling failed before work started. Retry shortly and inspect infrastructure health if the failure persists.",
                ExitCode: 2),

            // Used in BOTH fatal (HTTP 503) and per-axis-degraded contexts. Wording must work in both.
            ["BACKEND_UNAVAILABLE"] = new(
                CliMessage: null,
                CliSuggestion: "Backend recovers automatically; retry shortly. Check Redis Stack connectivity.",
                ExitCode: 2),

            // Used in BOTH fatal (HTTP 503) and per-axis-degraded contexts. Wording must work in both.
            ["GRAPH_UNAVAILABLE"] = new(
                CliMessage: null,
                CliSuggestion: "Retry the request; graph auto-recovers when FalkorDB reconnects. Check infrastructure status.",
                ExitCode: 2),

            ["ALL_BACKENDS_UNAVAILABLE"] = new(
                CliMessage: null,
                CliSuggestion: "All backends unavailable. Check infrastructure status (Redis Stack, FalkorDB). The service auto-recovers when backends reconnect; retry the request.",
                ExitCode: 2),

            ["GRAPH_TIMEOUT"] = new(
                CliMessage: null,
                CliSuggestion: "Retry with a smaller --max-depth or a tighter seed-node scope.",
                ExitCode: 2),

            // === Synthetic CLI transport codes (exit 2) — ADR-7.3-001 ===
            ["CONNECTION_REFUSED"] = new(
                CliMessage: null,
                CliSuggestion: "Verify the service is running. Try: dotnet run --project Hexalith.Memories.AppHost",
                ExitCode: 2),
            ["REQUEST_TIMEOUT"] = new(
                CliMessage: null,
                CliSuggestion: "The server did not respond within 30s. Check that the service is healthy ('dotnet run --project Hexalith.Memories.AppHost') or use a longer client timeout.",
                ExitCode: 2),
            ["TLS_ERROR"] = new(
                CliMessage: null,
                CliSuggestion: "Verify the certificate chain for the configured endpoint, or use a plain-HTTP endpoint for local development.",
                ExitCode: 2),
            ["INVALID_ENDPOINT"] = new(
                CliMessage: null,
                CliSuggestion: "Set a valid absolute URI via --endpoint, HEXALITH_MEMORIES_ENDPOINT, or the config file.",
                ExitCode: 2),
            ["UNEXPECTED_ERROR"] = new(
                CliMessage: null,
                CliSuggestion: "Run with --verbose for diagnostic detail; file an issue if the failure persists.",
                ExitCode: 2),

            // === HTTP_<status> synthetic codes from ErrorResponseDecoder (exit 2) ===
            ["HTTP_400"] = new(
                CliMessage: null,
                CliSuggestion: "The server rejected the request shape. Review command arguments or run with --verbose for the raw response.",
                ExitCode: 2),
            ["HTTP_401"] = new(
                CliMessage: null,
                CliSuggestion: "Authentication failed. Check --token, HEXALITH_MEMORIES_TOKEN, or the config file.",
                ExitCode: 2),
            ["HTTP_403"] = new(
                CliMessage: null,
                CliSuggestion: "Access denied. Verify the token has permission for this tenant/case.",
                ExitCode: 2),
            ["HTTP_404"] = new(
                CliMessage: null,
                CliSuggestion: "Endpoint not found. Verify the --endpoint is pointing at a Memories Server deployment.",
                ExitCode: 2),
            ["HTTP_409"] = new(
                CliMessage: null,
                CliSuggestion: "The request conflicts with the current server state. Retry after resolving the conflict.",
                ExitCode: 2),
            ["HTTP_500"] = new(
                CliMessage: null,
                CliSuggestion: "Server error. Retry shortly; if the failure persists, check server logs.",
                ExitCode: 2),
            ["HTTP_502"] = new(
                CliMessage: null,
                CliSuggestion: "Bad gateway between the CLI and the server. Retry; check ingress/reverse-proxy configuration if persistent.",
                ExitCode: 2),
            ["HTTP_503"] = new(
                CliMessage: null,
                CliSuggestion: "Server is temporarily unavailable. Retry shortly; the service auto-recovers when backends reconnect.",
                ExitCode: 2),
            ["HTTP_504"] = new(
                CliMessage: null,
                CliSuggestion: "Gateway timeout. Retry with a narrower query, or check upstream health.",
                ExitCode: 2),
        };

        return map;
    }
}
