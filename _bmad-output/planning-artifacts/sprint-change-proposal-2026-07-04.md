# Sprint Change Proposal: Architecture Audit Remediation

**Date:** 2026-07-04
**Project:** Hexalith.Memories
**Trigger:** Architecture, performance & readiness audit `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md` (six parallel read-only subsystem audits at commit `331cf10`)
**Status:** Approved by Jerome on 2026-07-04 and applied (Epics 20-26 added to `epics.md` and `sprint-status.yaml`)
**Mode:** Batch
**Change Scope:** Major — new remediation epics with implementation stories across security, data integrity, retrieval quality, scalability, factorization, and operational readiness. No PRD FR/NFR is removed; several are reinforced by making already-claimed behavior actually hold.

---

## 1. Issue Summary

A structured multi-agent audit of the RAG management server surfaced 51 evidence-grounded findings (IDs `A1`–`A51`, full detail in the audit evidence file). Five are Critical and roughly twenty are High. They cluster into a small number of systemic problems rather than scattered defects:

- **Security posture is absent at the server boundary.** No authentication or authorization exists on any of the 46 HTTP endpoints; the deferral is acknowledged in code at `Server/Program.cs:3122`. Tenant identity is a caller-supplied parameter and the audit user is read from a spoofable `x-user-id` header. This directly undercuts FR44 (enforce tenant context, reject cross-tenant), NFR8 (zero cross-tenant leakage), and FR67 (per-tenant audit).
- **The persistence model does not match its own conventions or its own claims.** Domain state is written as unguarded Redis + FalkorDB triple-writes with no outbox or event sourcing, contradicting the Hexalith state rules and `project-context.md` ("Tenant isolation is physical, not just filtered"; "Never hand-roll durable orchestration"). A whole consistency-repair subsystem exists to *detect* divergence the write path cannot prevent — and one key-namespace collision (`{t}:vec:nl:` nested inside `{t}:vec:`) makes that subsystem crash on tenants that use natural-language embeddings.
- **The most dangerous operational tool is the least safe and least tested.** Embedding vector migration drops indexes before generating replacements, its rollback always errors, its write-block marker has no TTL or lock, and the 26-file subsystem has one test file.
- **RAG retrieval quality is capped by correctness bugs**, not just tuning: no chunking (one embedding per whole document), semantic pagination silently ignores `Offset`, hybrid fusion mixes uncalibrated scores and drops case attribution, and graph traversals are unbounded and outlive their own client-side timeout.
- **Operational readiness is thin**: no container/K8s/Helm/compose artifacts exist anywhere, the export has no restore counterpart, no coverage gate exists, and 28 of 29 `[RunnableSkippedFact]` integration tests are empty stubs that pass by default — precisely the retry, rate-limit, and degradation scenarios operators depend on.

This proposal converts every finding into an implementable story under seven new themed remediation epics (per the approved scope: **all findings become stories**, organized into **new themed epics**). The audit also recorded genuine strengths (health-check depth, contract serialization sweep, Testcontainers/Aspire end-state fixtures, ingestion compensation skeleton, disciplined secrets handling) that these epics must preserve, not regress.

## 2. Impact Analysis

### Epic Impact

No completed epic is reopened. Seven new epics (20–26) are added in a new **Post-MVP — Audit Remediation** phase. They reinforce existing epics rather than replace them:

- **Epic 5 (Tenant Isolation)** claimed physical isolation and zero-leakage verification; the audit shows isolation is prefix-only and the API layer has no authorization. **Epic 20** closes the enforcement gap; **Epic 24** addresses physical isolation and verifier scalability. Epic 5 stays `done`; its acceptance evidence is superseded by explicit follow-on stories, not rewritten.
- **Epic 1/6 (Ingestion)** delivered the pipeline and its resilience skeleton; **Epic 21** and **Epic 23** close the consistency, migration-safety, and scalability gaps that the skeleton does not currently guarantee (FR13 partial-failure recovery, FR12 re-ingestion, NFR22 429 handling).
- **Epic 2 (Search/Fusion)** delivered three-axis search; **Epic 22** fixes correctness bugs in pagination (FR22), case attribution (FR34), and score calibration (NFR24/NFR25) that the current implementation regresses.
- **Epic 8 (Observability)** delivered health and telemetry; **Epic 24** closes NFR28 trace propagation across the workflow boundary and the missing dashboard/metric-naming artifacts.
- **Epic 13 (Embedding Migration)** delivered the Path A tool; **Epic 21** hardens it against the destructive-failure and stale-marker risks the audit found.
- **Epic 17 (Future Web UX)** — **Epic 25** includes one story bringing the already-built evidence cockpit into FrontComposer/Fluent V5 conformance; this does not pull forward the deferred web host.

### Artifact Conflicts

- **PRD:** No FR/NFR removed. Several are reinforced (FR12, FR13, FR22, FR34, FR44, FR67, NFR8, NFR22, NFR24, NFR25, NFR28). No scope reset required.
- **Architecture (`architecture.md`):** Change required. The audit contradicts three standing architecture claims that must be reconciled: (a) tenant isolation described as physical when it is prefix-only on a shared Redis (decision D-tenant-isolation); (b) eventual consistency "via DAPR Workflow saga/compensation" (D3) when `CaseService` writes are direct and unguarded; (c) the `Contracts.V1` boundary (D14) leaking backend key/store names. Epics 20, 21, 24, 25 each carry an architecture-update acceptance criterion.
- **UX Design (`ux-design-specification.md`):** Minor. Epic 25's evidence-cockpit conformance story references existing UX-DR governance; no new UX scope.
- **Sprint Status (`sprint-status.yaml`):** Change required. Add Epics 20–26 with `backlog` status and their stories; no existing status changes.
- **Deferred-work register:** Several audit findings overlap existing deferred entries (D8 TenantAuthorizationMiddleware referenced at `AppHost/Program.cs:42`; Story-9.3-MemoriesServerAuthN at `Program.cs:3122-3123`; the Story-15.x migration-marker residuals swept by Story 19.4). Those entries are now given concrete story homes in Epics 20/21 and should be cross-linked, not duplicated.

### Technical Impact

Substantial code change across Server, EventStore, Redis, Contracts, Client.Rest, Cli, Mcp, Web, ServiceDefaults, deploy/, and .github/. Two items are architectural forks requiring a decision before implementation (Section 6, Open Decisions): the consistency model (A3) and the physical-isolation strategy (A36). The remaining items are behavioral fixes, decompositions, or additive features that follow existing repository patterns (Dapr workflows, endpoint-handler extraction, `IEmbeddingProvider`-style strategies, SDK container publishing).

## 3. Recommended Approach

**Recommended path: Direct Adjustment (additive remediation epics), sequenced by risk.**

Rationale:

- The PRD MVP thesis is validated and does not need reduction; the defects are in how delivered behavior is enforced, not in what was scoped. Rollback is not justified — the affected code is load-bearing and the skeletons (compensation, failed-units, health, telemetry) are worth keeping.
- A single umbrella epic was rejected because the findings have genuinely different owners, gates, and risk profiles; themed epics preserve sequencing semantics consistent with the Epic 18/19 SCP-driven pattern.
- The two architectural forks (A3 consistency model, A36 physical isolation) are isolated into decision-first stories so implementation does not begin on an unratified design.

Recommended sequencing (see Section 5 roadmap):

1. **Epic 20 (Security)** first and in full — it is the single largest risk and blocks safe exposure of every other capability.
2. **Epic 21 (Data Integrity & Migration Safety)** next — A4/A5 are actively harmful to live tenants and A4's fix (A44 key-schema SSOT) is a prerequisite for several others.
3. **Epic 22 (Retrieval Quality)** and **Epic 23 (Ingestion Scalability)** in parallel — mostly independent behavioral fixes.
4. **Epic 24 (Observability & Performance)** and **Epic 25 (Factorization)** — enabling and hygiene work; Epic 25's Program.cs decomposition (A7) is best done after Epic 20 lands its filters so extraction has a home.
5. **Epic 26 (Test/Deploy/Ops)** — runs alongside as a continuous track; its stub-closure and coverage-gate stories should land early to prevent regressions in 20–25.

Effort estimate: **High** (multi-quarter). Risk level: **Medium** — mitigated by decision-first stories, preserved strengths, and the existing integration-fixture harness.

Timeline impact: This is post-MVP hardening; it does not block MVP thesis validation but is a prerequisite for any production exposure or first external deployment.

## 4. Detailed Change Proposals

The seven epics below are appended verbatim to `epics.md` (new phase heading after Epic 19) and their rows added to `sprint-status.yaml`. Each story lists the audit finding(s) it closes. Stories are `backlog`; no story files are created by this proposal.

### Epic 20: API Security & Tenant Authorization
*Closes A1, A2, A6, A20, A31, A41 (security portions). Reinforces FR44, FR67, NFR8, NFR11.*

- **20.1 Server authentication foundation** — JWT/OIDC bearer in ServiceDefaults, fallback `RequireAuthenticatedUser` policy, `AllowAnonymous` only on health + Dapr subscription routes. Closes A1.
- **20.2 Tenant authorization filter & principal-derived audit identity** — claims-based tenant-membership endpoint filter on `/api/tenants/{tenantId}/**`; audit user taken from the authenticated principal, not `x-user-id`. Closes A2. Negative cross-tenant denial tests required.
- **20.3 Tenant-scope workflow & batch status endpoints** — verify `tenantId` against stored state before returning; project a status DTO instead of raw `WorkflowState`. Closes A6.
- **20.4 MCP production signing-key hardening** — fail startup when an HS256 `SigningKey` is set under `IsProduction()`; require `RequireHttpsMetadata`. Closes A20.
- **20.5 Inbound per-tenant rate limiting, quotas & audit completeness** — `AddRateLimiter` partitioned by tenant; extend `AccessTelemetryLog` emission to tenant lifecycle, case-member, annotation, and deletion paths. Closes A41.
- **20.6 RediSearch query-injection hardening** — one shared escaper covering the full dialect-2 special set, applied on both syntactic and semantic axes. Closes A31.

### Epic 21: Data Integrity, Consistency & Migration Safety
*Closes A3, A4, A5, A16, A17, A22, A27, A28, A44, A47. Reinforces FR13, FR39, NFR16–NFR19.*

- **21.1 Consistency model decision (decision-first)** — ratify event-sourced-projections vs workflow-wrapped-multi-write for `Case`/`MemoryUnit`/`Tenant`; update `architecture.md` D3. Frames A3. No production code until ratified.
- **21.2 Transactional multi-backend mutation** — implement the ratified model so Redis/graph/stream writes are atomic or compensated (mirror `TenantDeletionWorkflow`). Closes A3.
- **21.3 Natural-language vector namespace separation** — move NL hashes to a disjoint prefix + data migration; rebuild raw semantic index with a non-overlapping prefix. Closes A4.
- **21.4 Key-schema single source of truth** — `Build{Syntactic,Semantic,NlSemantic}Key` helpers on `IndexSchemaDefinitions`; replace ≥12 hand-interpolated sites; add a CI grep guard against `:mu:`/`:vec:` literals. Closes A44 (root cause of A4).
- **21.5 Deletion completeness** — `HDEL` aggregate-case-map + cache invalidation on case delete; extend `DeleteTenantDataKeysActivity` to `eventstore:*`, `embedding-migration:*`, and a defensive `mu:*`/`vec:*` sweep. Closes A16, A17.
- **21.6 Event routing for unknown/unavailable tenants** — return 500 (retry) or dead-letter for `TenantNotFound`/`Unavailable` instead of ACK-and-drop. Closes A27.
- **21.7 Dedup TOCTOU & duplicate-instance handling** — `SaveDedupKeyActivity` uses `When.NotExists` and compensates the loser; catch duplicate workflow-instance on scheduling → `Duplicate()`. Closes A28.
- **21.8 Tenant registry CAS & rollback integrity** — ETag CAS on `UpdateTenantStatusAsync`; transactional entry+index save so a failed rollback cannot leave an invisible tenant. Closes A47.
- **21.9 Blue/green embedding migration** — staging prefix + staging index + atomic cutover + real previous-index retention/rollback; `SET NX` ownership + TTL/heartbeat marker + `--abort`. Closes A5.
- **21.10 Migration subsystem test coverage** — unit tests for store/marker/generator; a real-vector integration migration (768→1024 dims) asserting `FT.INFO` + marker end-state + rollback path. Closes A22.

### Epic 22: RAG Retrieval Quality & Correctness
*Closes A8, A9, A29, A30, A48, A49, A50. Reinforces FR22, FR34, NFR24, NFR25, NFR4.*

- **22.1 Semantic-axis pagination** — honor `Offset` (fetch `offset+max`, skip after enrichment) or reject non-zero offsets explicitly. Closes A8.
- **22.2 Bounded, cancellable graph traversal** — pass server-side `timeout`; add `LIMIT`; restrict `BuildTraverseFromNode` to semantic edge types. Closes A9.
- **22.3 Graph-scoped & hybrid pagination correctness** — `INKEYS`/TAG pre-filter for Mode-2; honest `TotalCount`; explicit deep-pagination cap. Closes A29.
- **22.4 Fusion case attribution, score calibration & pinned scorer** — carry `CaseId` through fusion; pin `SCORER BM25`; adopt RRF (or per-axis min-max) for scale-free fusion. Closes A30.
- **22.5 Case-scoped traversal path integrity** — apply the all-path-nodes case predicate in `BuildTraverseFromNode`. Closes A48.
- **22.6 Post-filter recall** — over-fetch (or index a filterable TAG pre-filter) so metadata/source-type filters cannot return 0 while matches exist beyond top-K. Closes A49.
- **22.7 Retrieval feature completion** — wire the stranded `axis=nl`; expose fusion-weight tuning; add RediSearch highlighting and an `IResultFuser` reranker seam. Closes A50.

### Epic 23: Ingestion Pipeline Scalability & Resilience
*Closes A11, A12, A13, A14, A15, A33, A34, A35, A51. Reinforces FR6, FR12, NFR5, NFR22.*

- **23.1 Content chunking & batch embedding** — token-aware splitter → N vectors per unit under `{t}:vec:{id}:{seq}`; provider batch API. Closes A12 (prerequisite: 23.9).
- **23.2 Claim-check workflow payloads** — persist content/vectors in the producing activity; pass `{id, hash}` between activities; slim per-activity input records. Closes A11.
- **23.3 Retry-After-aware 429 orchestration** — durable `CreateTimer(retryAfter)` before re-calling embedding; fix rate-limit window-open math. Closes A13.
- **23.4 Non-URL re-ingestion** — persist a content pointer in the failed record, or reject non-URL re-ingest with a clear error instead of scheduling a doomed workflow. Closes A14.
- **23.5 Rate-limiter admission simplification** — single `TryConsume(ceiling)` actor method or Redis Lua token bucket; cache tenant config. Closes A15.
- **23.6 Directory-batch scalability** — checkpoint batch state instead of per-file O(n²) rewrites; bounded-parallel scheduling; apply `SupportedExtensions` allowlist. Closes A33.
- **23.7 Index-provisioning ownership** — memoized per-tenant index verification; remove per-ingest `FT.CREATE`/`Thread.Sleep`/Warning. Closes A34.
- **23.8 Workflow config determinism** — pass retry-policy/NL options via workflow input captured at scheduling; remove mutable static reads from orchestrator code. Closes A35.
- **23.9 EmbeddingClient provider strategy** — extract `IEmbeddingProvider` (BuildRequest/ParseResponse/Authenticate) + shared transport/auth-retry decorator + `GenerateBatchAsync`. Closes A51.

### Epic 24: Observability & Performance Hardening
*Closes A19, A26, A36, A46. Reinforces NFR28, NFR12, NFR8.*

- **24.1 Trace propagation across the workflow boundary** — serialize `traceparent` into workflow input; linked spans via an activity base class; register `Microsoft.DurableTask` source. Closes A19.
- **24.2 Read-path caching & tenant-list bounding** — short-TTL cache for tenant status/embedding config/corpus stats invalidated on writes; page + bound `GET /api/tenants` fan-out. Closes A26.
- **24.3 Physical tenant isolation & verifier scaling (decision-first)** — evaluate per-tenant Redis ACL users vs hash-tag/DB separation; replace O(n²) pairwise deep-pagination with cursor/aggregate checks; delete the runtime self-test. Closes A36.
- **24.4 Metric naming & committed dashboards** — reconcile dot vs snake_case instruments; commit at least one Grafana/Aspire dashboard alongside `MetricTagKeyPolicy`. Closes A19 (metrics portion).
- **24.5 Hot-path write-amplification cleanup** — stop actor state writes on read paths; `XADD MAXLEN` + counter for activity streams; app-owned in-flight set for the replay gate; id-keyed NL retry queue. Closes A46.

### Epic 25: Architecture Factorization & Code Health
*Closes A7, A21, A32, A37, A38, A39, A40, A43, A45. No FR/NFR change; enables maintainability and NFR15.*

- **25.1 Program.cs decomposition** — route groups + per-resource endpoint classes (`{Ingestion,TenantLifecycle,Cases,Search,Graph,Consistency,Export}Endpoints`); target composition root ≤ ~150 lines. Closes A7.
- **25.2 Error/telemetry centralization** — `ErrorResults` factory, tenant-id/tenant-active endpoint filters, `EndpointTelemetryFilter`, one `IExceptionHandler`. Closes A32.
- **25.3 Shared route table & client consolidation** — `MemoriesRoutes` in Contracts consumed by server + client; single generic `MemoriesClient.SendAsync<T>`; fix `TraverseAsync` param order while `Experimental`. Closes A21.
- **25.4 Contract/persistence separation & route versioning** — axis-named contracts (drop Redis/store names); `/api/v1/` prefix; split persistence DTOs out of the public package. Closes A37.
- **25.5 CLI consolidation** — move CLI onto `Client.Rest`; collapse 14 clone JSON formatters into a generic `JsonEnvelopeFormatter<T>`. Closes A38.
- **25.6 MCP tool executor** — `McpToolExecutor.RunAsync(...)` owning validate/authorize/catch; single authorized-tenant source. Closes A39.
- **25.7 Evidence cockpit UX conformance** — FluentAccordion + `FluentLabel`; localize via `EvidenceResourceKeys`; consume a shared `EvidencePacketMapper.Unavailable(...)`. Closes A40.
- **25.8 Dead-code & topology cleanup** — remove the unregistered `RedisPreflightDedupStore` twin, dead `SupportedExtensions`/`:previous`/verifier self-test; resolve `ServiceDefaults→Contracts`, the orphaned `Web`, unused `Aspire` package, and placeholder `Redis` project boundaries. Closes A43, A45.

### Epic 26: Test, Deployment & Operational Readiness
*Closes A23, A24, A25, A42. Reinforces NFR7, NFR14, NFR16.*

- **26.1 Production deployment artifacts** — SDK container publishing per Hexalith convention; K8s overlay/Helm with resource limits + real Dapr component values (no echo LLM, no empty passwords). Closes A24.
- **26.2 Backup & restore** — import/restore endpoint consuming the export format; backup fidelity integration test; backup/restore + DR runbooks. Closes A25 (feature portion).
- **26.3 Integration stub closure** — implement (or explicitly skip) the 28 empty `[RunnableSkippedFact]` tests, prioritizing retry, rate-limit, and degradation scenarios asserting state-store end-state. Closes A23.
- **26.4 Coverage gating & benchmark lane** — enable coverage collection + threshold in CI; give the NDCG benchmarks a nightly lane. Closes A42.
- **26.5 Operational runbook set** — capacity planning, incident response, index-rebuild, tenant onboarding/offboarding, upgrade/migration, monitoring/alerting thresholds under `docs/operations/`. Closes A25 (docs portion).

## 5. Implementation Handoff

**Change scope classification: Major** — fundamental hardening across the system with two architectural decision points.

Routing:

- **Product Manager / Solution Architect (Winston):** ratify the two decision-first stories (21.1 consistency model, 24.3 physical isolation) and the `architecture.md` reconciliations before dependent implementation begins.
- **Product Owner + Developer (Amelia):** sequence Epics 20→21→(22‖23)→(24‖25) with Epic 26 as a continuous track; create story files per epic as capacity allows, honoring the operational-readiness acceptance-evidence rules already in `epics.md`.
- **Developer (Amelia):** implement stories following `project-context.md` (physical tenant isolation, no hand-rolled orchestration, replay-safe workflows, parameterized graph queries, structured errors, centralized formatters, no `.csproj` versions, CRLF/one-type-per-file).
- **Test Architect (Murat):** own the Epic 26 coverage gate, stub closure, and the migration/backup integration tests; enforce negative cross-tenant coverage on Epic 20/21 stories.

**Success criteria:** all 51 findings have a tracked story; Critical findings (A1–A5) are closed with tests before any production exposure; no MVP behavior regresses (existing integration suite stays green); the two architecture decisions are ratified and reflected in `architecture.md`.

---

## Approval

- [x] Approved by Jerome — 2026-07-04
- [x] `epics.md` and `sprint-status.yaml` updated (applied by this proposal)
- [ ] Architecture decisions (21.1 consistency model, 24.3 physical isolation) assigned to PM/Architect — **pending, gates Epic 21/24 implementation**

*Full per-finding evidence with file:line references: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md`.*
