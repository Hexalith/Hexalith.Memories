# Evidence Packet contract

Story 2.7 adds `EvidencePacket` as the shared trust envelope for search, MCP, CLI JSON,
diagnostics, and future web consumers. It is additive: lower-level contracts such as
`SearchResult`, `HybridSearchResult`, `ScoredResult`, `FusedScoredResult`, and
`SearchExplanation` remain the retrieval contracts and keep their existing JSON fields.

Confidence language is intentionally narrow. Packet scores and `evidenceStrength` describe
query-result relevance and retrieval evidence only. They do not claim factual accuracy, source
truthfulness, or corpus completeness.

## Required packet sections

| Section | Purpose |
|---|---|
| `scope` | Tenant/case boundary, isolation status, and permission context. |
| `result` | Query, total count, returned count, and indexed-memory signal when known. |
| `sources` | Ranked source snippets and source metadata included in the current response. |
| `evidence` | Evidence strength, caveat, axes used, unavailable axes, and per-axis score summary. |
| `graph` | Graph path, edge type, and gap summary when graph evidence is available. Search packets currently emit an explicit unavailable graph summary when no graph traversal evidence exists. |
| `state` | One of `complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized`, or `pendingExpansion`. |
| `omittedDetails` | Omitted fields, detail groups, omission reason, counts, token estimate, and scoped expansion handles. |
| `recovery` | Safe next actions such as retrying, checking authorization, increasing `tokenBudget`, or broadening the authorized query scope. |

## Search mapping table

| Packet field | Source member | Transformation | Absence behavior | Redaction rule | Recovery guidance |
|---|---|---|---|---|---|
| `scope.tenantId` | CLI/MCP request tenant | Passed explicitly by the surface, never inferred from result rows. | Required. | Do not replace with row metadata. | Unauthorized packets use `checkAuthorization` only. |
| `scope.caseId` | CLI/MCP request case | Passed explicitly, null for tenant-wide requests. | Omitted from JSON when null. | Do not reveal alternate cases. | Empty packets may suggest broadening authorized scope. |
| `result.query` | `SearchResult.Query` / `HybridSearchResult.Query` | Echoed as received from lower-level result. | Empty string for diagnostics without a query. | Do not place query text inside expansion handles. | Re-run search with changed query only through authorized surfaces. |
| `result.totalCount` | `TotalCount` | Copied. | `0` for diagnostics. | No cross-tenant totals. | Empty packets suggest broadening query/scope. |
| `sources[]` | `ScoredResult` / `FusedScoredResult` | Preserve rank order; map id, URI, type, snippet, score, case metadata, annotation count. | Empty list means no source evidence is exposed in this packet. | Do not add backend keys, local diagnostic paths, tokens, prompts, or hidden scope identifiers. | Omitted ranked sources use `increaseTokenBudget` guidance. |
| `evidence.caveat` | `SearchExplanation.Caveat` | Copied when present; otherwise uses the standard relevance caveat. | Standard caveat. | Never describe scores as factual truth. | None for complete packets. |
| `evidence.axesUsed` | `AxesUsed` or row axis metadata | Normalized to lowercase, ordered deterministically. | Empty list when unavailable. | Do not invent axes. | Degraded packets expose unavailable axes separately. |
| `evidence.axisEvidence[]` | `SearchExplanation.AxisDetails`, row scores | Merge explain metadata and best per-axis scores. | Missing score/explain fields are omitted rather than invented. | No backend identifiers or formulas from exceptions. | None. |
| `evidence.degraded` / `unavailableAxes` | `Degraded`, `UnavailableAxes`, `AllEnabledAxesUnavailable` | Copied and normalized. | `false` and empty list when no degradation is reported. | Axis names only; no backend hostnames. | `retry` and `inspectBackendHealth`. |
| `state` | Scope, degradation, omitted count, totals, evidence strength | Precedence: unauthorized, degraded, pending expansion, empty, weak, complete. | `complete` only when evidence is present without higher-precedence signals. | Unauthorized dominates all expansion guidance. | See state-specific recovery. |
| `omittedDetails.reason` | `OmittedReason`, degradation flags | Maps `tokenBudget`, backend degradation, and combined signals into packet reasons. | `none` when no omission is reported. | Redaction/policy/authorization are distinct from true absence. | `increaseTokenBudget`, `retry`, or `checkAuthorization`. |
| `omittedDetails.expansionHandles[]` | Request scope plus query hash | Deterministic opaque handle scoped by tenant/case and detail group. | Empty for unauthorized, degraded-only, and complete packets. | No raw query, backend key, file path, token, prompt, connection string, or storage id. | Re-run authorized commands with larger budget or result count. |
| `recovery[]` | Packet state and omission reason | Machine-readable action kind plus safe guidance. | Empty for complete packets. | Unauthorized guidance must not reveal whether another tenant or case exists. | State-specific. |

## Diagnostic mapping

`EvidencePacketMapper.FromError` maps `ErrorResponse` into a diagnostic packet without copying raw
exception text into packet fields. Unauthorized and forbidden codes produce `state:
unauthorized`, `scope.isolationStatus: unauthorized`, no sources, no expansion handles, and a
single `checkAuthorization` recovery action. Other diagnostics produce `state: degraded` and
retry-oriented recovery.

Recovery guidance is sanitized before serialization. Guidance that looks like a bearer token,
connection string, backend identifier, local path, stack trace, prompt, or embedding payload is
replaced with generic safe guidance.

## Surface behavior

CLI JSON and MCP search results keep the existing lower-level payload shape and add an optional
`evidencePacket` member to `SearchResult` and `HybridSearchResult`. Human and table CLI formats
continue to render their existing output.

MCP success results serialize the same object to both `content[0].text` and `structuredContent`.
MCP error results keep `isError=true`, `code`, `service`, `tool`, `message`, and `suggestion`, and
may include `evidencePacket` for packet-style state and recovery semantics.

