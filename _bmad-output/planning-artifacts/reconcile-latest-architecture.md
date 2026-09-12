# Reconciliation Extract — Latest Architecture → PRD

**Source:** `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` (final, 2026-09-09), including its three reviewer files after their final PASS retests
**Compared with:** legacy `architecture.md`, current `prd.md`, `addendum.md`, and `.memlog.md`
**Extraction date:** 2026-09-12
**Write scope:** Analysis only. No PRD, addendum, or memlog changes were made.

## Verdict

The final architecture spine is not merely a shorter replacement for `architecture.md`. It adopts several product-observable guarantees that the current PRD states only partially or not at all, and its brownfield gap ledger disproves several current `Shipped`/`Verified` entries. The highest-impact upstream corrections are projection completion/replay, tenant erasure, internal caller authorization, tenant-wide graph scoping, and capability-aware degradation/readiness.

One architecture rule must be phase-resolved before it is imported: AD-14 mandates staged, non-destructive tenant migrations without a phase qualifier, while the PRD and legacy architecture defer zero-downtime/concurrent-index migration to Phase 2 and explicitly tolerate MVP degradation.

## 1. Genuine New or Changed Product Requirements

### A. Projection completion must cover the current revision and active configuration, not only an EventStore version

- **Latest architecture:** AD-3 identifies work by tenant, case, unit, authoritative source version, schema generation, and embedding-configuration epoch; stale acknowledgements cannot complete a newer revision; repair/replay use the same completion protocol (`ARCHITECTURE-SPINE.md:68-72`). The current-code ledger says missing axes can still produce `Indexed`, no canonical all-axis checkpoint exists, and no EventStore replay-to-all-projections path exists (`:294-296`).
- **Current PRD gap:** FR6 and the pipeline section require only “the same EventStore source version”; FR13 does not say that stale schema/model acknowledgements cannot complete a current revision. The delivery register calls both FR6 and FR13 **Shipped**. NFR16 describes EventStore rebuild as an available recovery path while the spine says it does not exist.
- **Proposed PRD changes:**
  - **FR6 / Knowledge Ingestion:** require all three axes to acknowledge the *current authoritative revision under the active schema and embedding configuration*; stale or incompatible acknowledgements cannot produce `indexed`.
  - **FR13 / Knowledge Ingestion:** require retry, repair, and replay to converge on that same current-revision outcome and state explicitly that query-time degradation cannot promote an incomplete revision.
  - **Async Ingestion Pipeline:** distinguish current product words (`pending`/`projecting`) from V1 wire values (`queued`/`indexing`) while describing the strengthened completion outcome.
  - **NFR16 / Reliability:** make the authoritative EventStore replay-to-all-projections path an unmet requirement, not an already available alternative.
- **Proposed addendum change:** extend **Ingestion runtime** with the exact six-field tuple, the single Dapr-state checkpoint owner, monotonic ETag/CAS, stale-ack rejection, and the distinction between EventStore replay, export restore, and the current Redis-input repair.
- **Status correction:** move **FR6** and **FR13** to `Partial`; retain **NFR16** in re-verification owed but say the rebuild mechanism itself is missing, not merely its recorded run.

### B. Durable duplicate suppression is a public ingestion guarantee missing from the PRD

- **Latest architecture:** AD-4 scopes a V1 `IdempotencyToken` by tenant, case, and command/operation; scopes CloudEvent identity by tenant, case, exact validated `source`, and `id`; and requires durable EventStore/workflow suppression plus idempotent projection upserts (`ARCHITECTURE-SPINE.md:74-78`).
- **Current PRD gap:** no FR or NFR promises idempotent command/event ingestion. This is observable behavior under retry and at-least-once delivery, not merely an implementation choice.
- **Proposed PRD change:** add **FR75 / Knowledge Ingestion**: retries carrying the same operation-scoped V1 idempotency token, or the same tenant/case/CloudEvent-source/id identity, create one durable mutation and one projection outcome; a duplicate is handled safely rather than creating another memory unit. Add FR75 to the MVP phase and delivery registers as `Partial` until the authoritative boundary exists.
- **Proposed addendum change:** under **Ingestion runtime**, record exact ordinal/no-post-validation-normalization semantics and the fact that Redis preflight reservation is fail-open admission optimization only.
- **Downstream consequence:** the spine frontmatter currently binds `FR1-FR74`; adding FR75 requires a later architecture-bind refresh. If retaining 74 IDs is mandatory, the weaker alternative is to fold this into FR13, but that mixes duplicate handling with projection failure recovery.

### C. Internal channel authentication must not become tenant authorization

- **Latest architecture:** AD-5 requires external bearer authority to survive REST/CLI/MCP hops; trusted internal calls additionally need deny-by-default Dapr workload authorization and channel protection, then an operator-owned app-ID allowlist mapping to a canonical `system:*` principal and explicit tenant grants. Channel authentication never substitutes for tenant authorization (`ARCHITECTURE-SPINE.md:80-84`).
- **Current PRD conflict:** NFR10 says only “All inter-service communication authenticated via DAPR API tokens,” and the Service Communication table repeats that simplification. FR65 allows `system:*` provenance but does not require an explicit tenant grant. The latest ledger also says external ingestion currently accepts caller-controlled `IngestedBy` (`:297`).
- **Proposed PRD changes:**
  - **NFR10 / Security:** replace token-only wording with three independent outcomes: external/delegated identity, deny-by-default workload authorization, and protected app channels; require unknown apps and ungranted tenant access to fail closed.
  - **FR44 / Tenant Management:** clarify that every external and internal path derives or verifies tenant/case authority server-side; request fields, app identity, and channel credentials cannot authorize by themselves.
  - **FR65 and AI Reliability / Memory unit provenance:** require internal system provenance to come from the authenticated app-ID mapping plus an explicit tenant grant; preserve normalized external subject binding.
  - **Technical Architecture Considerations / Service Communication:** replace “DAPR API token (internal)” with the same layered contract.
- **Proposed addendum change:** expand **Identity** with the single owner/source of the finite app allowlist and tenant grants; keep Dapr policy/token mechanics out of the FR wording.
- **Status correction:** move **FR65** from `Shipped` to `Partial`; move **NFR10** from `Verified` to `Verified against previous wording; re-verification owed`.

### D. Tenant-wide discovery is allowed, but its graph work must remain case-partitioned

- **Latest architecture:** AD-7 permits a tenant-wide result set spanning cases with mandatory case attribution, but graph seeds and traversal are partitioned by each seed’s authoritative case and paths never cross cases (`ARCHITECTURE-SPINE.md:92-96`). AD-9 fixes the deterministic pre-rank merge for those partitions (`:104-108`).
- **Current PRD gap:** FR34 allows search across cases and FR33 requires case-scoped edges, but FR17’s top-candidate auto-seeding does not define a tenant-wide case partition. The latest ledger says this merge behavior is absent (`:302`).
- **Proposed PRD changes:**
  - **FR17 / Knowledge Retrieval:** add that tenant-wide auto-seeding partitions graph work by authoritative case and cannot cross case boundaries.
  - **FR33 / Memory Organization:** retain the absolute prohibition on cross-case edges/paths through Phase 1.5.
  - **FR34 / Memory Organization:** say tenant-wide discovery may return independently ranked units from multiple cases with mandatory attribution, while every graph contribution remains case-local.
  - **G4 / Measurable Outcomes:** include tenant-wide hybrid fixtures whose seeds span cases and prove that no path/node/edge crosses case ownership.
- **Proposed addendum change:** put the exact ascending-case/max-score/descending-score/ordinal-ID merge algorithm in **Fusion — decision vs numbers**, not in the PRD.
- **Status correction:** FR17 is already `Partial`; add **FR34** to a partial/re-verification row for the newly explicit tenant-wide hybrid behavior.

### E. Degraded reads and readiness are capability-aware

- **Latest architecture:** AD-10 returns a safe partial result when at least one selected axis can respond, fails only when none can, and requires excluded/unavailable axes, freshness impact, and recovery guidance in the Evidence Packet (`ARCHITECTURE-SPINE.md:110-114`). The health convention says a failed query backend degrades a capability rather than making an otherwise safe server unready.
- **Current PRD gap/conflict:** FR66 mentions excluded axes only; NFR18 does not define the fail boundary; FR72 says health checks verify all backends and can be read as all-backends readiness. The Evidence Packet narrative is richer than FR66 but does not define unavailable-vs-empty semantics.
- **Proposed PRD changes:**
  - **FR66 / Trust & Transparency:** require the safe-partial/fail-only-none rule plus unavailable/excluded axes, freshness impact, and recovery guidance; distinguish an unavailable axis from an available axis with no hits.
  - **FR72 / Data Portability & System Health:** define liveness as process viability and readiness around authentication configuration, Dapr control boundary, and EventStore command availability; a search backend outage reports capability degradation while safe axes remain usable.
  - **NFR18 / Reliability:** extend the chaos test to assert the Evidence Packet and the exact no-safe-axis failure boundary.
  - **Evidence Packet paragraph:** state that all evidence-bearing surfaces preserve these null/empty/degraded meanings.
- **Status correction:** move **FR66**, **FR72**, and **NFR18** to `verified/shipped against previous wording; re-verification owed` rather than claiming the strengthened contract is already proven.

### F. Tenant deletion now means verified erasure across authoritative history and restore, not projection purge

- **Latest architecture:** AD-16 makes deletion complete only after projection purge, a durable telemetry erasure handoff, and verified EventStore tenant-key crypto-shredding. It forbids replay and tenant-ID reuse, quarantines unreadable restored payloads, and retains only content-free tombstones/evidence (`ARCHITECTURE-SPINE.md:146-150`). The ledger says current deletion does not do this (`:299`).
- **Current PRD conflict:** Compliance Boundary and FR39 say tenant deletion removes indexes, graph data, and memory units, which can falsely imply that purging projections fulfills erasure even while content-bearing EventStore history remains readable.
- **Proposed PRD changes:**
  - **Compliance Boundary / Tenant deletion:** replace projection-only language with the end-to-end erasure outcome and explicitly state that retained opaque access telemetry follows NFR34’s TTL rather than being deleted early or represented as legal-audit evidence.
  - **FR39 / Tenant Management:** define observable deletion completion as verified projection purge + EventStore content inaccessibility + telemetry handoff; reject replay and tenant-ID reuse after completion and quarantine unreadable payloads on restore.
  - **NFR16 / Reliability:** add a negative recovery guarantee: restart/replay/restore cannot resurrect content for an erased tenant.
  - **NFR34 / Future web, freshness, and telemetry:** own the retained-telemetry TTL/purge behavior and erasure mapping.
- **Proposed addendum change:** add the crypto-shredding, content-free tombstone, non-reuse, restore-quarantine, and accelerated-telemetry-purge mechanism under a new **Tenant erasure** subsection.
- **Status correction:** FR39 is not merely “server shipped, CLI stub”; mark it `Partial — CLI stub and erasure contract incomplete`.

### G. Access telemetry has a two-stage failure posture

- **Latest architecture:** AD-17 requires qualification/configuration to fail closed before Production activation, but after admission sanitized product writes remain non-blocking within the approved delivery bound and surface degradation when that bound is exceeded (`ARCHITECTURE-SPINE.md:152-156`).
- **Current PRD gap:** NFR34 includes owner, TTL, purge, erasure mapping, recovery, and accepted debt, but not this before-activation versus after-admission behavior.
- **Proposed PRD changes:**
  - **NFR34:** add the production admission gate, post-admission non-blocking product-write behavior, sanitization, and explicit degradation signal when the approved telemetry-delivery bound is exceeded.
  - **FR67:** clarify that telemetry failure never changes domain truth or rolls back an accepted product mutation.
- **Status correction:** retain `Implemented, verification run not recorded` for NFR34, but say the revised admission/runtime failure contract requires fresh evidence.

### H. Fairness covers interactive work versus batch, repair, recovery, and migration

- **Latest architecture:** AD-18 requires separate tenant/global quota ownership, tenant-partitioned admission/concurrency, bounded durable queues, provider `Retry-After` via durable workflow timers, and lower priority for repair/migration than interactive work (`ARCHITECTURE-SPINE.md:158-162`).
- **Current PRD gap:** FR8 and NFR13 cover per-tenant ingestion isolation; NFR22 covers rate-limit backoff. They do not prevent one tenant’s batch/recovery/migration work from starving interactive work or define the global-vs-tenant fairness outcome.
- **Proposed PRD changes:**
  - **FR8:** broaden “ingestion load per tenant independently” to include bounded admission and protection of interactive operations from batch/recovery work.
  - **NFR13:** require noisy-neighbor evidence across interactive ingestion/query and batch/repair/migration workloads, not only concurrent ingestion across three tenants.
  - **NFR22:** require honoring provider retry windows without an unbounded in-memory retry loop.
  - **NFR12/NFR36:** reference the same fairness conditions in scale/freshness evidence.
- **Proposed addendum change:** keep queue type, workflow timer, quota owners, and priority implementation in **Ingestion runtime** or a new **Capacity and backpressure** subsection.
- **Status correction:** FR8, NFR13, and NFR22 need re-verification under the stronger contract; the spine does not prove that the new evidence already exists.

### I. Preserve exact V1 wire status values for the current contract window

- **Latest architecture:** V1 JSON remains lowercase `queued`, `extracting`, `embedding`, `indexing`, `indexed`, `failed`; `pending`/`projecting` are product/display semantics until a versioned break.
- **Current PRD gap:** the Glossary maps PascalCase enum member names but does not state the exact lowercase JSON wire values; Open Question 8 leaves the current decision looking less settled than the final architecture does.
- **Proposed PRD changes:**
  - **Glossary / Ingestion state:** name the exact lowercase V1 JSON values and keep the product-label mapping.
  - **Open Question 8:** close the current V1 choice (“preserve existing wire values until a versioned break”); a future breaking-version rename can be a revisit trigger, not an active phase blocker.

## 2. Phase Conflict That Must Be Resolved Before Applying

### AD-14 versus the PRD’s Phase 2 migration commitment

- **Latest architecture:** AD-14 requires create-backfill-verify-switch-retire migrations with disjoint active/staging resources and atomic activation, with no phase qualifier (`ARCHITECTURE-SPINE.md:134-138`).
- **Current PRD:** Phase 2 owns embedding versioning/model migration with zero downtime; FR43 in MVP only requires explicit acknowledgement that vectors will be rebuilt. Journey 6 places zero-downtime backend migration in Phase 3.
- **Legacy architecture:** D10 explicitly says MVP accepts degradation and concurrent index versions are Phase 2 (`architecture.md:296-320`, `:364`).
- **Memlog:** no later product decision explicitly pulls non-disruptive migration into MVP.
- **Required disposition:** do not silently import unphased AD-14 into the MVP register. The least disruptive reconciliation is to preserve current product phasing and state in `addendum.md` that AD-14 is the adopted target activated for the relevant Phase 2/3 migration capability, while MVP FR43 remains explicit-acknowledgement/degraded rebuild. If AD-14 is intended to bind immediately, this is an MVP scope rebaseline affecting FR43, FR68-FR70, delivery status, release gates, and epics.

## 3. Implementation-Only Material — Keep Out of FR/NFR Prose

| Latest architecture material | Correct destination | Exact affected addendum section |
| --- | --- | --- |
| Exact six-field projection tuple, Dapr-state coordinator, ETag/CAS, checkpoint algorithm | Addendum + architecture | **Ingestion runtime** |
| Exact RRF preprocessing, `Double.Equals`, competition rank `1,1,3`, `k=10`, weights, tenant-wide merge ordering | Addendum + architecture | **Fusion — decision vs numbers** |
| Server/EventStore adapter namespaces, provider-SDK allowlist, compatibility-only Redis facade, finite direct-Redis exception registry | Addendum + architecture | **Package inventory** and **Ingestion runtime** |
| Exact OpenBao/Redis/FalkorDB/Dapr/Aspire pins, patch versions, digests, Redis 8 migration plan | Addendum + architecture | **Topology and secrets** and **Language / SDK** |
| Missing Kubernetes EventStore gateway or external dependency declaration | Addendum + deployment architecture | **Topology and secrets** |
| Separate source-mode and package-mode restore/build/contract/integration lanes | Addendum + CI/release architecture | **Package inventory** |
| Floating AppHost/Aspire Redis and FalkorDB defaults | Addendum + architecture | **Topology and secrets** |
| Provider-client relocation, Dapr-state migration of registries/leases/fences | Architecture only; summarize debt in addendum | **Package inventory** |
| Graph runtime kill-switch mechanism | Architecture; PRD retains only the already-defined product kill-switch outcome | **Fusion — decision vs numbers** |
| Runnable Web conformance host versus non-activated product Web | Addendum/architecture; current PRD package table already has the correct product distinction | **Package inventory** |
| MCP present in deployment assets but ingress/publication/announcement disabled until L1-L3 | Addendum/architecture; PRD launch gate already correct | **Topology and secrets** |
| Python/Dapr Agents sidecar remains deferred until a selected feature requires it | Addendum/architecture | **Topology and secrets**; close PRD Open Question 7 unless product promotion is still desired |

## 4. Conflicts With Prior Decisions and Current Source Text

| Conflict | Prior/current source | Latest resolution | Required upstream action |
| --- | --- | --- | --- |
| Duplicate identity | Legacy architecture uses `event ID + aggregate ID` (`architecture.md:74`, `:188`, `:1250`) | Tenant + case + exact CloudEvent `source` + `id`; V1 tokens additionally scoped by operation | Add FR75 (or explicitly extend FR13), update addendum, and mark legacy architecture superseded for this rule |
| Internal authorization | Legacy architecture and current PRD treat Dapr API token as the internal authorization boundary | Workload policy + channel protection + app allowlist + explicit tenant grant; none substitutes for tenant authorization | Rewrite NFR10 and Service Communication rows |
| Tenant deletion | Legacy `TenantDeletionWorkflow` deletes only RediSearch/Vector/FalkorDB; current FR39 mirrors projection purge | Verified EventStore crypto-shredding plus projection purge, telemetry handoff, non-reuse, replay denial, restore quarantine | Rewrite Compliance Boundary/FR39/NFR16; add addendum mechanism |
| Data-plane secrets | Legacy D31/current PRD allow “direct pod inputs” outside Dapr in broad terms; addendum says Kubernetes Secrets are only OpenBao bootstrap | Current direct Redis/Falkor credentials are explicitly an alignment gap, not a compliant second secret path | Tighten NFR9 outcome, correct addendum wording, and mark NFR9 re-verification owed |
| Schema/provider migration phase | Legacy D10 and PRD defer non-disruptive migration; final AD-14 is unphased | Not actually reconciled by a product decision | Resolve phase before editing; recommended default is preserve PRD phase and qualify AD-14 as target |
| Cross-case “insight” wording | `addendum.md` says cross-case insight discovery is blocked by FR32, while PRD FR34 already permits tenant-wide discovery | AD-7 permits tenant-wide discovery but forbids cross-case edges/paths | Rewrite that addendum row to say tenant-wide discovery exists; only cross-case references/paths are deferred |
| Release-date status in addendum | **Release decision** says dates are assistant assumptions pending Jerome | `.memlog.md` records Jerome’s 2026-09-08 confirmation: release 2026-12-01, launch 2027-01-01 | Correct addendum; only the 2026-10-31 sprint-selection date remains derived |
| PRD delivery/evidence claims | FR6/FR13/FR39/FR65 and NFR9/NFR10/NFR16 are presented more strongly than repository reality | Final spine gap ledger labels the missing mechanisms explicitly | Refresh the FR/NFR status registers during this Update; do not preserve “Shipped/Verified” for superseded wording |

No active `.memlog.md` decision conflicts with the latest EventStore-as-truth, workflow-not-queue-actor, weighted-RRF, phase-split MCP/EventStore, MIT licence, or release-date decisions. The main logged-history issue is omission: the new erasure, durable idempotency, internal tenant-grant, and capability-aware health decisions have not yet been appended.

## 5. Status Register Corrections Required by the Architecture Evidence

| ID(s) | Current PRD status | Recommended status after requirements reconciliation | Evidence |
| --- | --- | --- | --- |
| FR6, FR13 | Shipped | Partial — authoritative revision/checkpoint/replay gaps | Spine gaps 294-296 |
| FR39 | Partial only because CLI stub | Partial — CLI stub **and** end-to-end erasure incomplete | Spine gap 299 |
| FR65 | Shipped | Partial — caller-controlled external provenance remains | Spine gap 297 |
| FR17 | Partial | Keep Partial; add tenant-wide case merge and kill-switch gaps | Spine gap 302 |
| FR34 | Shipped | Re-verification owed / Partial for tenant-wide hybrid graph semantics | AD-7/AD-9 and spine gap 302 |
| FR66, FR72 | Shipped | Shipped against previous wording; re-verification owed | AD-10 and health convention |
| NFR9 | Verified | Verified against previous wording; current data-plane secret path is non-compliant | Spine gaps 298, 306 |
| NFR10 | Verified | Verified against previous token-only wording; re-verification owed | AD-5 |
| NFR16 | Previous wording verified; re-verification owed | Partial mechanism + re-verification owed | Spine gap 296 |
| NFR18 | Verified | Verified against previous wording; re-verification owed | AD-10 |
| NFR24-NFR25 | Verified | Re-verification owed against canonical preprocessing/cross-surface golden vectors | Spine gaps 301-302 |
| FR8, NFR13, NFR22 | Shipped/implemented/verified | Re-verification owed against AD-18’s broader fairness contract | AD-18 |
| NFR34 | Implemented, no run | Keep category; explicitly require evidence for the revised admission/runtime failure posture | AD-17 |

Do not infer that every architecture alignment row is a product delivery-status change. Provider-SDK placement, image pins, missing CI lanes, and topology ownership are architecture/release debts; they belong in the addendum and downstream tracking unless the PM explicitly turns them into product gates.

## 6. Already Aligned — No New PRD Requirement Needed

- The product is a reusable technical platform rather than a domain module; the PRD’s package/surface classification is compatible with AD-1.
- EventStore domain truth, Phase 1.5 CloudEvent product integration, and runtime/source evidence remain three separate meanings; current PRD and memlog already carry this split.
- Phase 1 graph edge sources versus Phase 1.5 automatic EventStore population are already aligned.
- Weighted RRF, top-five auto-seeding, graph depth at most two, and the product kill-switch outcome are already in the PRD; only deterministic merge detail and implementation status need reconciliation.
- Evidence Packet equivalence across CLI JSON, MCP, REST, and future Web is already stated; FR66/FR72 need the precise degradation/health boundary.
- MCP/EventStore product activation remains gated by L1-L3 even if artifacts exist; the PRD already says this.
- The Web project remains a runnable conformance specimen rather than an activated product UI; the current package table and NFR32/NFR35 phase language are already correct.
- Cross-case references remain deferred; tenant-wide discovery is not the same capability.
- Redis 8, FalkorDB upgrades, multi-region/HA, managed-service licensing posture, and Python sidecar activation remain deferred architecture topics with revisit conditions, not current FRs.

## Recommended Application Order

1. Resolve the AD-14 migration-phase conflict.
2. Update product guarantees (FR6/FR13, FR39, FR44/FR65, FR17/FR34, FR66/FR72, NFR9/NFR10/NFR13/NFR16/NFR18/NFR22/NFR34); add FR75 if durable idempotency remains a distinct capability.
3. Refresh the FR/NFR delivery-status registers from the final architecture gap ledger.
4. Update addendum mechanism sections and correct its cross-case, release-date, and secret-path drift.
5. Append each adopted change/override to `.memlog.md` through `memlog.py`; the architecture phase decision and any choice to add FR75 are decisions, not silent editorial changes.
6. Flag `architecture.md` as legacy/superseded in downstream guidance and later refresh the final spine’s `binds` range if the PRD gains FR75.
