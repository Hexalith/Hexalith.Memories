# Current PRD UX Contract Extraction

This working extraction was regenerated from the complete 1,243-line change-controlled PRD at SHA-256 `12579f3a22228348948e805968ea3835732e7ebbcb837e3beef75bd58fc115f1`. It records UX consequences without replacing the source. [PRD metadata and lineage](../../../prd.md#L1)

## Authority, status, and release posture

- The PRD is the product-outcome authority for the thesis, phases, G1–G6, L1–L3, FR1–FR75, and NFR1–NFR37. Architecture owns mechanisms and binding gaps; `Contracts.V1` and current implementation own serialized names and delivery facts; active work ownership remains in epics and sprint status. [Source precedence](../../../prd.md#L24)
- Phase and delivery are independent. A later-phase capability can ship early without changing phase, and an MVP requirement can remain incomplete. Current implementation evidence wins only for delivery facts; it does not weaken target behavior or release gates. [Phase register](../../../prd.md#L976)
- The current release posture is **no-go**. Retaining 2026-12-01 for the expanded Phase 1 decision is an explicit assumption, not evidence. Missing evidence has no successor-story ownership; AD-14 phasing is unresolved; architecture has not yet bound FR75 and NFR37. [Release posture](../../../prd.md#L62)
- Phase 1 is the CLI/REST thesis surface. Phase 1.5 is the separately gated MCP and EventStore launch path. J4/J8 application experiences are Phase 2. Memory Explorer and migration operations are Phase 3. `Hexalith.Memories.Web` is a non-product conformance specimen. [Scope and increments](../../../prd.md#L219)
- Epics 0–8 being closed does not establish PRD acceptance: Phase 1 commands are missing, FR17/FR25 are incomplete, G1 has not run, and G3 has no clean-machine stopwatch evidence. L1–L3 remain unevaluated. [Release decision record](../../../prd.md#L208)

## Decision gates

All six Phase 1 gates are hard requirements; none may be averaged away or treated as a soft launch criterion. [G1–G6 register](../../../prd.md#L180)

| Gate | Binding decision and UX implication |
|---|---|
| G1 | On a frozen real-content corpus with N≥50 topics, hybrid must beat the BM25+semantic two-axis RRF control on ≥80% of topics by pre-registered ΔNDCG@10≥0.02; mean Δ over all N must be ≥0, no topic may regress by >0.10, and pairwise reviewer Cohen’s κ must be ≥0.6. Run public search exactly as shipped: no search start-node input. Graph auto-seeding is required. |
| G2 | Zero cross-tenant leaks under principal-driven isolation tests. Denial must fail closed and disclose no restricted evidence. |
| G3 | README/AppHost → tenant create → case create → ingest → first CLI search in under 30 minutes on the defined clean machine. `quickstart` supplements but does not replace the manual path. |
| G4 | Case-scoped queries never escape their case; tenant-wide hybrid may span cases only with mandatory case attribution, while every graph seed/node/edge/path stays case-local. |
| G5 | Fusion explanation is deterministic under NFR24–NFR26. UX preserves rank-contribution semantics and deterministic ordering. |
| G6 | Every MVP item lacking current-wording verification, plus every architecture-critical active-foundation gap, needs current evidence or an approved phase-specific exception, owner, and tracking entry. |

G1 labels freeze before scoring; topic/corpus vectors are cached and hashed; thesis-stress topics are pre-registered at ≤20% of N; a run with <30% explicit + metadata-carried non-`contains` edges is labelled similarity-graph fusion; post-score relabelling discards the run. The existing synthetic N=8/best-single-axis benchmark is diagnostic only. If G1 fails, the ordered kill switch removes graph from default hybrid, retains it only as explicit graph/traversal, repositions the product before further decisions, cancels L1–L3 evaluation, and requires sprint re-scope. [Full G1 protocol and kill switch](../../../prd.md#L160)

Phase 1.5 is separately gated: L1 requires a held-out ≥10-topic MCP set, 100% token-budget compliance, and ≥8/10 relevant citations; L2 requires the clean-machine EventStore integration stopwatch under 30 minutes, but its required sample does not exist; L3 requires ≥95% causal-chain completeness on the named fixture extended to at least 20 labelled chains. A failed or unevaluated launch gate is a no-go, not a slip and not permission to fold MCP into the thesis MVP. [L1–L3 and launch rule](../../../prd.md#L193)

## Current delivery register

| Status on 2026-09-12 | Requirements |
|---|---|
| Implemented and product-active under current wording | FR4–FR5, FR7, FR9, FR12, FR14–FR22, FR24, FR31–FR33, FR35, FR37, FR41–FR43, FR45–FR46, FR48–FR52, FR55–FR57, FR63–FR64, FR67–FR70, FR73–FR74 |
| Preview rather than launch-approved | FR23, FR54, FR58–FR62; implementation exists, L1–L3 do not |
| Partial protocol prerequisite | FR17 auto-seeding/case merge; FR25 BM25+semantic control |
| Partial architecture contract | FR6/FR13 projection completion/replay, FR34 tenant-wide case-partitioned graph merge, FR39 verified erasure, FR65 server-derived provenance, FR75 durable duplicate suppression |
| Shipped against previous wording; re-verification required | FR8 fairness, FR66 capability-aware degradation, FR72 readiness |
| Shipped; hardening in progress | FR44 tenant-scoped principals/authority boundary |
| Server exists; Phase 1 CLI slice is incomplete | FR1–FR3, FR10–FR11, FR26–FR30, FR36, FR38, FR40, FR47, FR53; FR38/FR40 also depend on isolation hardening |
| Delivered early without phase change | FR71 application export remains Phase 2 |

The authoritative status table is in the PRD. A `NotImplementedCommand` is not delivered coverage. [FR delivery status](../../../prd.md#L986)

NFR evidence is similarly qualified: NFR11, NFR17, NFR19–NFR20, NFR26–NFR28, and NFR30 are verified against current wording; NFR8–NFR10, NFR13, NFR18, NFR21–NFR22, and NFR24–NFR25 need re-verification; NFR16 is partial; NFR1–NFR5, NFR7, NFR12, NFR14, NFR23, NFR29, NFR31, and NFR33–NFR34 are implemented without a recorded current-wording run; NFR6, NFR15, NFR32, NFR35, NFR36, and NFR37 are not started. [NFR delivery status](../../../prd.md#L1109)

## Journeys J1–J10

The exact identifiers are J1–J10; the PRD does not define a separate UJ namespace. Narrative timings/counts are illustrative; gates, FRs, and NFRs are binding. [Journey introduction](../../../prd.md#L303)

| ID | Exact PRD title | Protagonist, phase, and surface | Climax | Failure/recovery contract |
|---|---|---|---|---|
| J1 | **Alex — "Zero to First Search" (Phase 1.5 launch path)** | EventStore developer; Phase 1.5 package/subscription setup plus CLI. | Event-derived search returns a CausationId chain and `--explain` shows three RRF contributions. | Keep the L2 launch stopwatch distinct from Phase 1 onboarding; expose subscription, secrets, boot, publication, and handler evolution honestly. [J1](../../../prd.md#L307) |
| J2 | **Alex — "Something's Wrong" (Debug Path)** | Developer diagnosing a missing claim; Phase 1.5 CLI/log diagnostics. | Registration and replay restore the claim and causal chain. | Zero result → stale ingest → no axis matches → handler/serialization mismatch → register/replay → verified recovery. The narrative `handlers --list` is stale relative to current grammar. [J2](../../../prd.md#L327) |
| J3 | **Alex — "Wiring Up the AI Assistant" (MCP Integration, Phase 1.5)** | Developer configuring four MCP tools. | A 2,000-token request returns concise evidence with deterministic omission disclosure and supports a sourced causal walk. | Omission is counted and expandable; truncation never silently drops context. [J3](../../../prd.md#L347) |
| J4 | **Marcus — "Brief the New Person" (Phase 2)** | Team lead onboarding a colleague through a downstream assistant. | A sourced causal chain answers why a decision was made. | Source inspection reveals a date discrepancy and the user corrects it by annotation. Membership is attribution metadata, never authority. [J4](../../../prd.md#L363) |
| J5 | **Kenji — "New Tenant, No Drama" (MVP)** | Operator provisioning and verifying an isolated tenant through CLI/monitoring. | Tenant creation and verification establish an isolated space. | A tenant-A principal is denied tenant B without B-data leakage; feedback names authenticated scope and records sanitized access telemetry. [J5](../../../prd.md#L383) |
| J6 | **Kenji — "Time to Scale" (Phase 3)** | Operator migrating a two-million-unit tenant. | Dry-run precedes zero-downtime cutover; reads stay on Redis until ready. | Assessment, volume, plan, duration, rollback, progress, cutover, and confirmation are explicit. Deferred; not a current promise. [J6](../../../prd.md#L399) |
| J7 | **LLM Agent — Technical Integration Path** | Non-human Phase 1.5 MCP consumer. | Agent performs budgeted hybrid search and composes an attributed causal answer. | Deterministic omitted-detail handles, empty alternatives, case disambiguation, freshness caveat, retry-after, named unavailable axes, and no-hallucination fallback. Protocol-first; no screen is inherently required. [J7](../../../prd.md#L417) |
| J8 | **Priya — "I Need to Understand This Case" (Phase 2 downstream application)** | Claims adjuster using another team’s downstream application. | Priya handles the call with a chronological sourced explanation and verifies evidence live. | “Show sources” enables verification; 0.95 is relevance, never factual certainty. The current title is explicitly Phase 2. [J8](../../../prd.md#L447) |
| J9 | **Alex — "The First Case" (Empty State)** | Developer on the Phase 1 README/CLI thesis path. | Case creation and directory ingestion lead to a first real-data hybrid search. | Healthy empty, case-created, lifecycle progress, indexed, and next-step states each name the action. `case`/`ingest` remain target-state placeholders and block G3. [J9](../../../prd.md#L467) |
| J10 | **The Contributor — "From Bug Report to First PR"** | External .NET contributor using repository/build/test/CI/review infrastructure. | CI passes and the first PR merges. | Build/test/review failures need actionable guidance. This is infrastructure, not an interactive Memories product surface. [J10](../../../prd.md#L487) |

Phase closure is J5/J9 in Phase 1; J1/J2/J3/J7 in Phase 1.5; J4/J8 in Phase 2; J6 in Phase 3; and J10 in the contributor ecosystem. [Journey summary](../../../prd.md#L505)

## Exact CLI surface and current grammar

The PRD target grammar is not the same as current registration. The current implementation evidence establishes the following safe presentation; missing target verbs remain target-state requirements. [PRD CLI surface](../../../prd.md#L880)

| Current registration | Status / behavior |
|---|---|
| Global `--endpoint <uri>`, `--token <value>`, `--verbose`, `--format human|json|table`, `--telemetry` | Shipped. `--tenant` requests scope; claims authorize it. There is no ambient tenant switch. Prefer `HEXALITH_MEMORIES_API_TOKEN`; literal token examples are forbidden. |
| `tenant list`; `config show` | Shipped. Target `tenant create/delete/verify` are absent, not individual stubs. |
| `search query --tenant <id> [--case <id>] [--query <text>] [--axis syntactic|semantic|nl|graph|hybrid] [--max-results <1..1000>] [--explain]` | Shipped. Hybrid and 10 default. Query is optional only for graph. There is no public search `--from` or `--token-budget`. |
| `search inspect --tenant <id> --case <id> --id <memoryUnitId>`; `search lookup --tenant <id> --case <id> --source-uri <uri>` | Shipped. |
| `quickstart [--tenant <id>] [--skip-boot-check] [--skip-prereq-check] [--dry-run] [--tenant-timeout-seconds <positive-int>]` | Shipped; does not replace G3/NFR31 manual evidence. |
| `status telemetry --tenant <id>` | Shipped. Target `status --case/--failed` are absent. |
| `consistency verify --tenant <id> [--batch-size <10..5000>] [--wait]`; `consistency inspect --tenant <id> --id <memoryUnitId>` | Shipped. |
| `consistency repair --tenant <id> [--batch-size <10..5000>] [--include-unrepairable] [--wait] [--yes]` | Shipped. Preserve dry-run/apply intent, tenant scope, provenance, and current-revision limits. |
| `export case --tenant <id> --case <id> [--output <path>] [--force] [--allow-absolute-path]`; `export tenant --tenant <id> ...` | Shipped early for Phase 2 FR71. Success is raw portable JSON, not a formatter envelope; non-human `--format` is ignored with a stderr warning. |
| `handlers list`; `handlers mismatches --tenant <id> [--severity info|warning] [--only-warning] [--exclude-stale]` | Shipped Phase 1.5 diagnostics. JSON mismatches intentionally remain unfiltered. |
| Top-level `ingest`, `traverse`, `case`, `explore` | The only current `NotImplementedCommand` placeholders; each writes the Story 7.2 message to stderr and exits 2. Target subcommands are not registered. |

Current exit codes are 0 success, 1 domain error, 2 plumbing/not implemented, 4 not found, and 130 cancelled. Formatter-routed JSON success is `{ schemaVersion, command, data }`; JSON error is `{ schemaVersion, command, error }`, with errors on stdout. Export success and cancellation (`Cancelled.` on stderr, no JSON body) are documented exceptions. NFR37 cross-form semantic parity is target contract, not current proof. [CLI output and configuration](../../../prd.md#L905)

Configuration precedence is flags → `HEXALITH_MEMORIES_*` environment variables → user/project configuration → DAPR Secrets/OpenBao for runtime secrets → DAPR non-secret configuration. Sensitive values do not use ordinary fallback. [Configuration hierarchy](../../../prd.md#L913)

## MCP and versioned contracts

- MCP is the Phase 1.5 subset with exactly `search_memory`, `ingest_content`, `traverse_relations`, and `get_case_info`. It has no operator parity and is preview until L1–L3 pass. [MCP surface](../../../prd.md#L850)
- Current safe field/type names belong to the implementation extraction; the UX spine may project them but cannot invent serialized members. `Contracts.V1` owns Evidence Packet, status, error, freshness, degradation, omission, and recovery values. [Evidence Packet contract](../../../prd.md#L568)
- NuGet contracts use semantic versioning; backward-compatible additions remain in the version, breaking wire changes require new message types plus deprecation. REST is `/api/v1`; DAPR app id is unversioned. Errors preserve Hexalith code/component/recovery semantics across adapters instead of collapsing to a generic transport error. [Versioning and error propagation](../../../prd.md#L754)
- CLI is the operational superset. MCP intentionally omits tenant administration, isolation verification, full ingestion status/failure diagnostics, consistency repair, export, handler operations, quickstart, exploration, and directory batch ingest. [Surface boundaries](../../../prd.md#L850)

## Search, RRF, graph, and confidence

- Search axes are syntactic/BM25, semantic/embedding, and graph/traversal proximity. `nl` is an optional Phase 1.5 event-description semantic score, not a fourth marketed axis. [Vocabulary](../../../prd.md#L83)
- Hybrid means deterministic weighted reciprocal-rank fusion of available rankings. Raw BM25, cosine, and graph-proximity magnitudes are not blended directly. Hybrid explanation values are weighted rank contributions; single-axis values retain axis-specific meaning. Numeric weights and RRF `k` remain architecture-owned. [Fusion semantics](../../../prd.md#L158)
- **Settled invocation rule:** public search has no start-node input. When hybrid has no explicit graph start, FR17 must auto-seed each case-local graph contribution from the union of top-five syntactic and top-five semantic candidates and traverse depth ≤2. Explicit starts exist only on CLI `traverse` and MCP `traverse_relations`; do not invent search `--from`. The shipped service’s skip-without-start behavior is a gap, not an allowance. [Graph-seeding decision](../../../prd.md#L167)
- Tenant-wide ranking may span cases only with mandatory case attribution. Seeds, nodes, edges, and paths remain within each authoritative case. Case-scoped traversal never crosses cases. [G4](../../../prd.md#L187)
- If G1 fails, default hybrid becomes BM25+semantic and graph remains explicit/traverse-only. UX must not promise graph in default hybrid before the gate passes. [Fallback decision](../../../prd.md#L170)
- Relevance confidence is relevance, not factual accuracy, completeness, freshness, probability, or truth. Metadata confidence distinguishes `human-declared` from `ai-inferred`; edge confidence is relationship strength and is never auto-promoted. Freshness remains a separate `current|aging|stale|unknown` concept. [Confidence vocabulary](../../../prd.md#L556)
- Edge types/defaults are `caused_by` 1.0, `correlated_with` 0.8, explicit/inferred `references` 1.0/0.5, `contains` 1.0, and `annotates` 1.0. Causation never collapses into correlation. Traversal is directed and ordered with timestamps, confidence, and literal gaps such as `[MISSING: id]`. [Graph semantics](../../../prd.md#L586)
- Phase 1 ships graph mechanics, not a complete causal corpus. File/URL ingestion creates available relationship types only where metadata supports them; EventStore population belongs to Phase 1.5. [Graph scope](../../../prd.md#L243)

## Evidence, absence, degradation, and recovery

Every evidence-bearing surface preserves the same meaning for tenant/case scope, result state, source/origin, relevance and per-axis contributions, freshness impact, omission/detail-group/expansion handling, axis availability, degradation, and recovery. [Evidence Packet meaning](../../../prd.md#L568)

Selected-and-available-with-hits, selected-and-available-with-no-hits, selected-but-unavailable, and deliberately excluded are distinct. A selected search may safely return partial output while at least one selected axis responds; when none responds it fails. “No results” must distinguish real absence from empty/wrong scope, incomplete or delayed ingestion, filters, stale evidence, authorization refusal, and backend degradation. Unauthorized responses carry no restricted evidence. [Reliability and empty-state contract](../../../prd.md#L556)

Omission is deterministic, counted, and recoverable where the contract offers expansion. Conflict between sources is a separate evidence condition; it does not create an unversioned packet state or change relevance confidence. Compact trust fields may stay visible while detailed ranks/weights/matched terms remain opt-in explain content; the default web explain behavior is still an open UX decision. [Evidence and confidence](../../../prd.md#L568)

## Ingestion, projection, retry, fairness, and timing

Human states are `pending → extracting → embedding → projecting → indexed`, with terminal `failed`. V1 JSON remains lowercase `queued`, `extracting`, `embedding`, `indexing`, `indexed`, `failed`; the settled display mapping is `pending`≙`queued` and `projecting`≙`indexing`. Retry, dead-letter, replay, repair, attempt count, last error, and `repaired_at` are details, not new lifecycle states. [Lifecycle vocabulary](../../../prd.md#L83)

`indexed` means search, vector, and graph all acknowledge the current authoritative EventStore revision under the active schema generation and embedding configuration. Two-of-three, stale, or incompatible acknowledgements never complete a newer revision. Retry, repair, and replay converge on the same rule, and query degradation cannot promote incomplete ingestion. [Projection completion](../../../prd.md#L817)

FR75 defines one durable mutation for retried V1 commands sharing tenant-, case-, and operation-scoped idempotency token, and for retried CloudEvents sharing tenant, case, exact validated source, and event id. One authoritative source-version/schema-generation/embedding-configuration tuple yields one idempotent projection outcome; a legitimate new epoch remains allowed. No current public CLI/MCP idempotency option exists, so UX describes the outcome without inventing a control. [FR75](../../../prd.md#L1017)

Bounded global and per-tenant admission, queue, and concurrency controls must preserve interactive search/ingest responsiveness while batch ingest, repair, recovery, or migration runs. Noisy-neighbor evidence spans at least three tenants. Queuing/throttling delays remain visible in progress and freshness. [Fairness requirements](../../../prd.md#L1137)

Under normal admitted load, ≤10 KB file/URL ingestion reaches `indexed` within 60 seconds and ≤1 MB within five minutes; NFR36 evidence is not started. Throughput is >100 small or >10 large units/minute per tenant. Phase 1.5 EventStore freshness is <5 seconds. Search p95 at 10 concurrent requests and 10K units is <200 ms syntactic, <500 ms semantic, <1 second hybrid, and graph <2 seconds at depth≤5. [Performance requirements](../../../prd.md#L1125) [Ingestion timing](../../../prd.md#L1205)

## Tenant, authorization, provenance, erasure, and readiness

- Tenant is the isolation boundary; case is a container; membership is attribution metadata. Case id, membership, request fields, display metadata, app id, and channel credentials do not authorize. [Domain vocabulary](../../../prd.md#L83)
- Delegated bearer identity survives external/internal hops. Internal calls additionally require protected DAPR channels, deny-by-default workload authorization, finite operator allowlist mapping to canonical `system:*` principals, and explicit tenant grants. Unknown apps and ungranted tenants fail closed. [Authorization boundary](../../../prd.md#L738)
- `ingested_by` is server-derived: normalized `sub` externally; allowlist/grant-derived `system:*` internally. Caller provenance cannot override it. [Provenance requirement](../../../prd.md#L1083)
- Tenant deletion is verified multi-system erasure: projections are purged, EventStore content becomes irreversibly inaccessible with verification, access-telemetry handoff is durable, replay/restart/restore cannot resurrect data, unsafe restored payloads are quarantined, and tenant ids cannot be reused. Retained opaque telemetry follows approved TTL and is not legal-audit evidence. [Compliance and erasure](../../../prd.md#L530)
- Production telemetry qualification fails closed. After admission, access-telemetry delivery is bounded and non-blocking for accepted domain writes; exceeding the bound exposes degradation but never rewrites domain truth. [Telemetry posture](../../../prd.md#L1079)
- Liveness is process viability. Readiness requires valid authentication configuration, the DAPR control boundary, and EventStore command availability. A query-backend outage degrades the selected capability rather than automatically making the whole process unready. [Readiness](../../../prd.md#L1095)
- Consistency repair never crosses tenants, silently deletes units without telemetry provenance, or invents edges unsupported by the current authoritative revision. [Repair constraints](../../../prd.md#L1095)

## FR1–FR75 requirements-to-surface coverage

| FR group | UX consequence |
|---|---|
| FR1–FR13 plus FR75 — ingestion | File/URL/directory target flow, case scope, content types, durable status, current-revision three-projection completion, actionable failure, re-ingestion, single-operation retry identity. Current CLI verbs and durable suppression remain incomplete. [FR1–FR13/FR75](../../../prd.md#L1004) |
| FR14–FR25 — retrieval | Syntactic, semantic, graph, and deterministic hybrid; explicit axis selection; explain, inspect, lookup, filters, pagination, token budgets, omission handles; auto-seeding and BM25+semantic control remain gaps. [FR14–FR25](../../../prd.md#L1021) |
| FR26–FR37 — case/organization | Case lifecycle, activity, annotations, membership attribution, one-case ownership, case-local graph isolation, tenant-wide attributed merge, and discoverability. Membership never grants authority. [FR26–FR37](../../../prd.md#L1033) |
| FR38–FR45 — tenancy/security | Provision/configure/delete/list/verify, tenant-scoped backends and principals, explicit mismatch denial, internal authority boundary, and verified erasure. [FR38–FR45](../../../prd.md#L1044) |
| FR46–FR52 — graph | Typed edges, directional traversal, depth/filtering, confidence, promotion after verification, literal gaps, and case-local boundaries. [FR46–FR52](../../../prd.md#L1054) |
| FR53–FR58 — CLI/MCP | Discoverable CLI with machine-readable output/errors and exactly four typed MCP tools; CLI is broader. [FR53–FR58](../../../prd.md#L1065) |
| FR59–FR62 — EventStore | CloudEvent subscription, dual embeddings, causal metadata, safe unknown-event handling, and handler diagnostics; Phase 1.5 preview only. [FR59–FR62](../../../prd.md#L1073) |
| FR63–FR67 — trust | Source/origin, server-derived provenance, relevance caveat, capability-aware degradation, omission, freshness, and safe partial/no-safe-axis handling. [FR63–FR67](../../../prd.md#L1081) |
| FR68–FR70 — providers | Local/Azure embedding choice, configuration, rate-limit handling, and visible retry timing. [FR68–FR70](../../../prd.md#L1089) |
| FR71–FR74 — portability/health | Phase 2 export delivered early, liveness/readiness distinction, telemetry/freshness, and bounded repair. [FR71–FR74](../../../prd.md#L1095) |

## NFR1–NFR37 experience consequences

| NFR group | Required experience |
|---|---|
| NFR1–NFR7 | Search/traversal latency, ingestion throughput, <5-second EventStore freshness, and ≤60-second cold start determine loading, delay, timeout, and degradation feedback. [NFR1–NFR7](../../../prd.md#L1125) |
| NFR8–NFR11 | Principal-driven tenant isolation, active ingress authentication, retry-after behavior, and versioning/error propagation must fail closed without leaking data or secrets. [NFR8–NFR11](../../../prd.md#L1137) |
| NFR12–NFR15 | Tenant scaling, cross-tenant fairness, migration, and configurable provider/backend boundaries require queue visibility and explicit unsupported/deferred states. [NFR12–NFR15](../../../prd.md#L1146) |
| NFR16–NFR19 | Restart-safe lifecycle, verified erasure/recovery, capability-aware degradation, and reproducible setup need progress, proof, quarantine, and actionable recovery. [NFR16–NFR19](../../../prd.md#L1155) |
| NFR20–NFR23 | MCP interoperability, contract compatibility, event handling, and current-revision projection convergence preserve shared semantics across adapters. [NFR20–NFR23](../../../prd.md#L1164) |
| NFR24–NFR26 | Deterministic fusion, tie-breaking, score semantics, and cross-surface null/empty meanings constrain explain presentation. [NFR24–NFR26](../../../prd.md#L1173) |
| NFR27–NFR29 | Sanitized observability, attribution, access telemetry, queue/failure visibility, and retention must never be marketed as a tamper-evident audit trail. [NFR27–NFR29](../../../prd.md#L1181) |
| NFR30–NFR31 | Every implemented command has example help; clean-machine onboarding is manually timed to first search in under 30 minutes. [NFR30–NFR31](../../../prd.md#L1189) |
| NFR32–NFR35 | Future activated web must meet WCAG 2.2 AA, measurable trust-content performance, freshness disclosure, responsive/reflow requirements, and a dated browser/AT matrix; these are activation gates, not current UI claims. [NFR32–NFR35](../../../prd.md#L1196) |
| NFR36 | File/URL time-to-index and visible provider/fairness delay are active product targets with no current evidence. [NFR36](../../../prd.md#L1205) |
| NFR37 | Active CLI outputs must provide text-labelled state/score/omission/recovery, readable narrow-terminal and redirected/no-color behavior, bounded output with a linear alternative, durable progress, explicit timeout/cancellation, secret sanitization, semantic parity across human/table/JSON/stderr/exit codes, automation, and a keyboard-only Phase 1 walkthrough. It is not started and blocks unqualified accessibility/conformance claims. [NFR37](../../../prd.md#L1211) |

## Open questions and settled decisions

1. **Open/assumption:** Jerome confirmed 2026-12-01 for the former G1–G5 decision and 2027-01-01 for Phase 1.5; the expanded G1–G6 gate retains 2026-12-01 only until Jerome ratifies or resets it at the 2026-10-31 checkpoint.
2. **Open:** who recruits the two independent reviewers and which real non-synthetic Phase 1 corpus is frozen for G1; Jerome, 2026-10-31.
3. **Open UX decision:** compact trust fields stay visible; determine whether detailed explain defaults open when web activates.
4. **Closed:** licence is MIT; remaining handoff is publishing the no-relicense sentence in README.
5. **Open/deferred:** FR32’s one-case ownership remains absolute through Phase 1.5; revisit cross-case association in Phase 2.
6. **Closed/deferred:** additional ingest sources remain deferred until the thesis proves demand.
7. **Closed:** a Python AI-agent sidecar is architecture-only, not a Phase 1.5 product surface.
8. **Closed:** exact V1 wire values are lowercase `queued|extracting|embedding|indexing|indexed|failed`; human `pending`/`projecting` map to `queued`/`indexing`.
9. **Open G6 blocker:** Architecture + Jerome must qualify AD-14 or approve an MVP rebaseline before 2026-10-31 because the PRD places embedding/schema migration in Phase 2 and backend migration in Phase 3.
10. **Open G6 blocker:** Architecture must bind and trace FR75 and NFR37 before the 2026-10-31 checkpoint and G6.

These dispositions are authoritative in the PRD open-question register. [Open Questions](../../../prd.md#L1217)

## Surface implications and infrastructure-only journeys

| Surface | UX posture |
|---|---|
| Phase 1 CLI search/evidence | Active but contract gaps remain. Preserve scope, case attribution, source, relevance caveat, axis contribution/state, freshness, omission, degradation, and recovery. Do not imply graph auto-seeding is delivered. |
| Phase 1 onboarding/ingestion/case/tenant/traversal | Target behavior is binding but key registrations are placeholders or absent. Show current versus target explicitly; do not use J9 narrative as delivery proof. |
| REST `/api/v1` | Shipped programmatic transport for capabilities without CLI verbs; not a Memories application UI. |
| Phase 1.5 MCP/EventStore | Implemented preview, not launch-approved. Preserve four-tool scope and shared Evidence Packet meanings without claiming L1–L3. |
| J4/J8 downstream application | Phase 2 composition. UX may specify evidence behavior but does not own or invent the downstream application chrome. |
| J6 migration and Memory Explorer | Phase 3, requiring later discovery. No current routes, controls, or product capability may be inferred. |
| `Hexalith.Memories.Web` | Conformance specimen only. Component existence does not establish product routing, authorization, live data, focus integration, notification behavior, or activation. |
| J7 agent integration | Protocol/schema/error/recovery journey; may not warrant any interactive screen. |
| J10 contributor flow | Repository/build/test/CI infrastructure; does not warrant an interactive Memories product surface. |
| AppHost, DAPR, OpenBao, projections, backends | Infrastructure. Surface only user-relevant prerequisites, authority, readiness, health, progress, or recovery. |

## Migration hazards

- Do not revive the superseded 2026-09-08 claim that search accepts `--from`. The current PRD explicitly settles no public search start-node input and requires automatic seeding. [Settled graph-seeding rule](../../../prd.md#L167)
- Do not collapse relevance, factual certainty, metadata confidence, edge confidence, freshness, degradation, omission, projection completion, and authorization into one score, status, or color.
- Do not market server capability as CLI completion, a placeholder as a command tree, MCP preview as launched, early export as a phase change, or Web specimen code as a product UI.
- Do not call membership authorization, DAPR channel identity a tenant grant, telemetry a legal audit log, projection deletion verified erasure, two-of-three projection `indexed`, or duplicate retry a second operation.
- Do not claim CLI accessibility conformance until NFR37 has dated current-contract evidence.
