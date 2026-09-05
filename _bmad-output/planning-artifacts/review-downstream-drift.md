# Downstream Drift Review — Hexalith.Memories

## Verdict

The PRD is no longer a trustworthy source-extract for UX, architecture, or stories. FR1–FR74 and NFR1–NFR31 identifiers still match across the three planning spines, and several 2026-05/06 change-control patches did land (Google-only MVP embeddings, sidecar event intake, OpenBao NFR9, RRF in NFR24, FR71 Phase 2 tag). After that, change control moved the product — EventStore as domain source of truth, JWT ingress, DAPR Workflow ingestion, C# 14, physical-isolation ACL target, Dapr Agents, 31 epics of brownfield work — while two approved 2026-08-03 Major SCPs that explicitly required PRD amendments were never applied. Extracting from the PRD today reconstitutes a March 2026 greenfield thesis with a few later footnotes, not the contract implementers actually follow.

## Method

Read in full: `prd.md` (2026-03-22 frontmatter; last readiness snapshot dated the file 2026-07-19), architecture Requirements Overview through PRD Deviations, Gate-Blocking, Decision Registry, Requirements Coverage, and sampled later D23–D31 / OpenBao / EventStore sections; epics Overview, Requirements Inventory, FR Coverage Map, Selected Implementation Scope, Implementation Readiness Boundary, and epic/story headers through Epic 31 (no story-by-story inventory). Skimmed UX `Platform Strategy` / effortless-search claims only where they contradict a PRD journey or interface sentence. Listed all 97 `sprint-change-proposal-*.md` filenames and titles/summaries; deep-read only scope-moving proposals. Optionally read `implementation-readiness-report-2026-08-04.md` PRD Analysis (it still names the same PRD contradictions).

**SCPs deep-read:**

- `sprint-change-proposal-2026-03-28.md` — Kreuzberg replaces Tika
- `sprint-change-proposal-2026-04-26.md` — Post-MVP transition / roadmap exhaustion
- `sprint-change-proposal-2026-04-29.md` — Ollama as embedding default (later superseded for MVP wording)
- `sprint-change-proposal-2026-05-18.md` — MVP embedding provider scope (Google-only; **PRD was patched**)
- `sprint-change-proposal-2026-06-24.md` — Dapr sidecar event intake (**PRD Phase 1.5 row was patched**)
- `sprint-change-proposal-2026-07-04.md` — Architecture audit remediation (Epics 20–26; auth/consistency)
- `sprint-change-proposal-2026-07-16-epic-26-benchmark-closure.md` — fusion calibration; PRD 80% line frozen
- `sprint-change-proposal-2026-07-16-tenant-provisioning-workflow-ownership.md` — provisioning owner (PRD already matched)
- `sprint-change-proposal-2026-07-17-eventstore-runtime-adoption.md` — Epic 28 identity
- `sprint-change-proposal-2026-07-17-infrastructure-dependency-abstraction.md` — D30 direct Redis vs Dapr state
- `sprint-change-proposal-2026-08-01-eventstore-source-and-3-89-package-identities.md` — EventStore source vs 3.89.0 packages
- `sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md` — **Major; PRD-1…PRD-7 required**
- `sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md` — **Major; PRD-1…PRD-6 required**
- `sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md` — isolation verifier (no PRD edit proposed)
- `sprint-change-proposal-2026-08-31-story-28-1-eventstore-identity-toolchain-mismatch.md` — SDK 10.0.400 vs sealed 10.0.302 hashes

Skipped as process/CI/story-split unless they rewrote a PRD-level requirement (story-gate hooks, slice guards, commit file-list, most Epic 27 checkpoint splits, CI alignment, historical-slice guards).

**Already patched in the PRD (do not re-litigate):** Google-only MVP embeddings (2026-05-18), Memories Server sidecar as Hexalith-module CloudEvent subscriber (2026-06-24), OpenBao-backed DAPR Secrets (NFR9), weighted RRF in NFR24 / domain score table, FR71 Phase 2 unless pulled, Evidence Packet as cross-surface envelope, `TenantProvisioningWorkflow` sequencing.

**Reverse drift (architecture fossil, not a PRD defect):** architecture `PRD Deviations` still quotes “All major [embedding] providers supported from MVP,” which the 2026-05-18 PRD patch removed. Syncing the PRD *to* that deviation table would reintroduce a lie.

## Findings

### critical Approved August 2026 PRD amendments never landed

- **PRD:** `prd.md` Language matrix still `.NET 10 / C# 13`; NFR11 still tagged **P1.5**; NFR inventory still ends at NFR31; Risk Mitigation still allows deferring cases and tenant isolation; indexing stage still says “Atomic write across all three backends”; Service Communication still says “Per-user identity: Not in MVP.”
- **Downstream:** `sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md` §5.1 (approved Major) required PRD-1 phase register, PRD-2 C# 14, PRD-3 identity/provenance, PRD-4 EventStore commit + projection state machine, PRD-5 NFR11 as current MVP invariant, PRD-6 NFR32/NFR33 web gates, PRD-7 NFR34 telemetry lifecycle. The same-day rerun SCP (`…-rerun.md` §4.1, Administrator-approved 2026-08-04) independently required C# 14, deletion of the minimum-scope escape, unambiguous package/host counts, DAPR-state vs direct-backend split, NFR11 MVP, and a canonical FR phase register. `implementation-readiness-report-2026-08-04.md` §PRD Completeness Assessment still lists those exact defects and records the PRD as last modified 2026-07-19.
- **Note:** This is not a disagreement among later artifacts about what the PRD *should* say. Change control already chose the PRD edits and they were not written. Until they land, every later extract (UX, architecture, stories) is guessing which of two approved, slightly different August patch sets is canonical (remediation-batch NFR33 is web performance; rerun NFR33 is evidence freshness).
- **Fix:** Apply a single reconciled PRD amendment covering both August patch sets (C# 14 with `global.json` as pin authority; kill the 13–18-story isolation escape; phase register; EventStore-commit state machine; NFR11 current; identity/provenance; NFR32–NFR34 with one NFR33 definition). Do not treat either SCP as already done.

### critical EventStore is domain truth in architecture, a Phase 1.5 integration in the PRD

- **PRD:** Scoping Phase 1.5: “EventStore / Hexalith Module Event Integration (DAPR pub/sub through the Memories Server sidecar…)”. Pipeline indexing: “Atomic write across all three backends.” NFR17 verification: “DAPR actor state persistence verified.” FR59–FR62 remain the only EventStore FRs and are untagged in the FR list (phase lives only in the Phase 1.5 table).
- **Downstream:** Architecture Requirements Overview driver #3 and Cross-Cutting Concern #9: “Story 21.1 ratifies the EventStore aggregate model as the consistency target for `Case`, `MemoryUnit`, and `Tenant`: domain state is sourced from Hexalith.EventStore events, while RediSearch… Redis Vector… FalkorDB… are rebuildable projections.” Gate-Blocking: “EventStore source of truth + projection compensation | Gate 1 | MVP.” `PRD Deviations` explicitly overrides the atomic-write sentence. Epic 21 Story 21.1 exists to ratify that model. Epic 28 then adopts a pinned EventStore *runtime identity* (source SHA / 3.89.0 packages) — a third EventStore meaning the PRD never names.
- **Note:** Three different “EventStore” contracts are now in play: (1) zero-code CloudEvent ingestion (PRD Phase 1.5 / Epic 9 / FR59–62), (2) aggregate source of truth for Memories domain writes (architecture D3 / Epic 21 / Gate 1), (3) consumer pin of Hexalith.EventStore bits (Epic 28). The PRD only knows (1), and still describes (2) as an atomic Redis triple-write owned by a pipeline actor.
- **Fix:** PRD must split the three contracts. Keep FR59–62 as Phase 1.5 product integration. Add a consistency/provenance requirement (or rewrite FR13/NFR17) that EventStore acknowledgement is the durable commit and search/graph writes are rebuildable projections. SDK/package identity stays architecture/Epic 28; PRD should defer version pins.

### high Greenfield living contract vs months of brownfield delivery

- **PRD:** Frontmatter `projectContext: 'Greenfield'`; Project Classification “Project Context: Greenfield”; Resource Requirements “Solo developer. Estimated 22-32 stories across 7 features.”
- **Downstream:** `epics.md` Overview + Epic List: foundation Epic 0 through operational Epic 31, with Phase 1.5, post-MVP operations, and audit-remediation tracks. `sprint-change-proposal-2026-04-26.md`: “Epic 11 is the last epic defined… The MVP roadmap is now exhausted at the planning level.” Architecture and epics continue to accrete D23–D31, OpenBao, PostgreSQL telemetry adapter profiles, and EventStore identity work through 2026-08.
- **Note:** Greenfield classification is not a harmless label. It still licenses a 22–32 story plan, a “restarted implementation sequence,” and a resource-tight escape that drops tenant isolation — all false as a *current* contract. The PRD date (2026-03-22) and unchanged classification tell extractors they are reading the product-as-imagined, not the product-as-governed.
- **Fix:** Reclassify as brownfield / change-controlled. Replace the story-count estimate with a pointer to `epics.md` + `sprint-status.yaml`. Keep the original MVP thesis as historical context, explicitly not as the active work-breakdown.

### high External auth and identity: PRD still Phase 1.5 / tenant-only; Epic 20 already shipped JWT

- **PRD:** NFR11 “External access authenticated at ingress layer — no unauthenticated access to REST API endpoints” | Phase **P1.5**. Service Communication: “Per-user identity | Not in MVP — tenant-level isolation sufficient.” Tenant context “Passed as parameter in payloads, validated by server.”
- **Downstream:** `sprint-change-proposal-2026-07-04.md` Epic 20: “No authentication or authorization exists on any of the 46 HTTP endpoints… directly undercuts FR44, NFR8, and FR67” — remediation, not a new product idea. Architecture Security Architecture: “Story 20.1 added the Server fallback `RequireAuthenticatedUser` policy for `/api/**`; only health probes and Dapr infrastructure routes are explicitly anonymous.” Story 20.2 maps principal claims to tenant sets. Both 2026-08-03 SCPs require NFR11 to be a current MVP invariant and define `sub` / `system:*` provenance. Architecture Requirements Coverage nevertheless still lists NFR11 under “Phase 1.5 fast-follow” — so architecture’s *coverage table* is as stale as the PRD, while its Security Architecture section is not.
- **Note:** Extracting NFR11 from the PRD (or from architecture’s coverage table) would schedule authentication as fast-follow work that change control already treated as a production-exposure blocker and that Epic 20 implemented.
- **Fix:** Move NFR11 to MVP/current; name the anonymous health/Dapr exceptions; replace “per-user identity not in MVP” with the 08-03 identity contract (tenant claims authorize; case membership is metadata; external provenance is server-derived `sub`). Architecture coverage table must move with the PRD so the two spines do not keep a stale-together P1.5 tag.

### high Ingestion is a DAPR Workflow in architecture, a pipeline actor in the PRD

- **PRD:** Async Ingestion Pipeline: “Ingestion uses a **per-tenant pipeline actor** managing a bounded queue. The pipeline actor owns throttling… ordering, and progress tracking.” Indexing is an actor responsibility. NFR17: “DAPR actor state persistence verified.” Complexity bullet still cites “DAPR actor model” as a primary driver.
- **Downstream:** Architecture: “Forces DAPR Workflow for pipeline orchestration” (driver #4); `IngestionWorkflow` with extract/embed/index activities and compensation; actors reserved for `EmbeddingRateLimiterActor` and `CorpusStatisticsActor`. Epics Additional Requirements copy D23 workflows vs D24 singleton actors. Story 6.4 / NFR17 reinforcement is workflow durability, not a document-queue actor.
- **Note:** The PRD’s actor-queue story is the pre-D23 design. Leaving it in place makes FR8/FR9/FR13/NFR17 extract as “build a pipeline actor,” which architecture forbids for orchestration.
- **Fix:** Rewrite the pipeline section: workflow owns stages, retry, and compensation; a per-tenant actor (if any) owns only rate-limit budget. Change NFR17 verification to workflow history / Durable Task persistence. Architecture owns the workflow/actor split; the PRD must stop specifying the discarded actor-queue.

### high Fusion spike text still describes magnitude blending; NFR24 and architecture settled on RRF

- **PRD:** Implementation Sequencing: “The fusion algorithm (BM25 normalization + cosine + graph proximity weighting) is research-grade R&D.” That sentence sits in the same document as NFR24: “Hybrid fusion uses deterministic weighted reciprocal-rank fusion with per-axis rank contributions in 0.0-1.0” and the domain score table that already documents RRF.
- **Downstream:** Architecture Fusion concern: “Story 22.4 selected a corpus-invariant, rank-based implementation: weighted reciprocal-rank fusion. Raw BM25, cosine, and graph-proximity magnitudes are not averaged in hybrid scoring.” Epic 26 SCP (`…-epic-26-benchmark-closure.md`) calibrated live weights / RRF `k=10` and **explicitly froze** the PRD 80% NDCG@10 hard line. Architecture Epic 26 calibration paragraph records `0.30/0.35/0.35`, `k=10`.
- **Note:** Identifier-level NFR24 matches. The living *design* paragraph in Scoping does not. An extractor using the MVP Strategy section would spike a different algorithm than the one Gate 1 already governs.
- **Fix:** Replace the BM25/cosine/proximity-weighting spike with RRF + explain-of-rank-contributions; keep 80%/NDCG@10/reproducibility as PRD gates. Numeric `k` and default weights: architecture owns these; PRD should defer.

### high MCP/CLI “every feature” vs capability alignment and Phase 1.5 MCP

- **PRD:** Executive Summary: “Every feature is accessible through both MCP (for LLM agents) and CLI (for developers). The MVP validates the three-axis thesis via CLI; MCP ships as a fast-follow within 4 weeks of thesis validation.” FR53: “Developer can interact with all retrieval and ingestion capabilities via CLI” (no phase tag). Journey 2 scope note: handlers/replay “must be explicitly included in MVP Feature #3 (EventStore Integration)” — but MVP Feature #3 is Three-Axis Search; EventStore is Phase 1.5 Feature #1.
- **Downstream:** Architecture Interface Philosophy: “Capability alignment, not feature parity.” MVP CLI essentials listed; MCP is search/ingest/traverse/case-info only; tenant/verify/status/handlers are CLI-only. Epics Epic 7 repeats that split; Epic 10 holds FR23/FR54/FR58 as Phase 1.5. 2026-08-03 remediation: FR53 is phased; several CLI verbs are still stubs; a help line backed by `NotImplementedCommand` is not coverage. UX Platform Strategy: CLI/MCP/web are “all first-class surfaces” on the full horizon, then immediately “MVP implementation is CLI-first… MCP/EventStore follow in Phase 1.5.”
- **Note:** MCP *timing* (Phase 1.5, 4-week fast-follow, pull-into-MVP if slipping) is still aligned across PRD/architecture/epics. The exec-summary parity claim and unphased FR53 are not. Journey 2’s “MVP Feature #3 = EventStore” error is still in the PRD (flagged again in the 2026-08-04 readiness report).
- **Fix:** Strike “every feature / both interfaces.” Point FR53/FR54 at the existing parity matrix and a phase register. Move Journey 2’s handlers/replay note to Phase 1.5 Epic 9/10. UX may keep full-horizon first-class language if it continues to disclaim MVP.

### medium C# 13 / package-count / backend-access wording still contradict repository and architecture facts

- **PRD:** “Server runtime | .NET 10 / C# 13.” “9 published NuGet packages + 3 non-packable service/orchestration projects” while the table names nine package rows and two explicitly non-packable hosts (`Server`, `AppHost`). “Internal (Server ↔ Redis/FalkorDB) | DAPR state / direct connection via DAPR sidecar.”
- **Downstream:** Architecture Constraints table still says `.NET 10 / C# 13`, but Current Verified Versions says “Runtime — C# 14.” Scale & Complexity: “7 published NuGet packages plus 3 non-packable…” vs PRD’s nine. D30 / `sprint-change-proposal-2026-07-17-infrastructure-dependency-abstraction.md`: Dapr state API and direct Redis/FalkorDB clients are different paths; search/graph use Aspire-injected clients in a boundary project, not “via DAPR sidecar” as a generic proxy. 2026-08-03 rerun SCP PRD-1/PRD-3/PRD-4 required exactly these PRD fixes. 2026-08-31 EventStore identity SCP then records Memories’ mandated SDK as **10.0.400**, so even the August “record 10.0.302” PRD patch would already be stale as a pin.
- **Note:** Inventory IDs (74/31) match; these are meaning mismatches in the implementation matrix. Architecture is internally split on C# 13 vs 14 and on 7 vs 9 packages — the PRD cannot be the resolver until it stops asserting C# 13 and the unexplained “3 non-packable.”
- **Fix:** PRD language baseline `.NET 10 / C# 14`; SDK pin deferred to `global.json`. Package inventory: `tools/release-packages.json` is the only count; list non-packable hosts in a separate table. Split Dapr state vs direct backend clients. Architecture should own SDK and package math; PRD should stop duplicating them incorrectly.

### medium Physical isolation: PRD still “separate indexes”; architecture moved the security boundary to ACL users

- **PRD:** FR38 “Operator can create a tenant with physically separate indexes.” Exec summary: “Physically separate indexes per tenant enforce enterprise-grade isolation.” NFR8 graph fixture (identical structures, colliding edge IDs) is still the leakage test.
- **Downstream:** Architecture Tenant Isolation concern and Story 24.3 decision: “Redis physical isolation target is per-tenant ACL users combined with tenant-scoped backend resolution… Key prefixes, Redis hash tags, and logical Redis databases are placement and routing tools only; they are not the primary security boundary.” Epic 24 / 2026-08-04 verifier SCP: FR40 and NFR8 remain sufficient *as product requirements*; enforcement, ACL lifecycle, cutover, and NFR8’s colliding-ID fixture are still follow-up. 2026-08-03 remediation: isolation described more strongly than owned enforcement.
- **Note:** The PRD requirement (zero leaks, tenant-scoped indexes, automated verify) is not obsolete. The *mechanism* the PRD implies (index names = isolation) is what architecture later demoted. Extracting FR38 as “indexes are the security boundary” under-builds Gate 2.
- **Fix:** Keep NFR8/FR38/FR40 outcomes. Add one sentence that physical isolation *target* is tenant-scoped backend principals (ACL users + resolver); indexes/databases remain lifecycle resources. Mechanism details: architecture owns this; PRD should defer.

### medium Dapr Agents / polyglot runtime never entered the PRD

- **PRD:** Language Future column: “.NET only (DAPR handles polyglot).” No Dapr Agents service, no Python sidecar, no `ai-agent` app-id. Journey 1 boot path: “`docker compose up` for Redis + FalkorDB.” FR7 requires AI-inferred metadata but does not name a runtime.
- **Downstream:** Architecture revisionNote (2026-03-25): “Dapr Agents Python sidecar (D27), Polyglot architecture (D28).” Deployment topology includes AI Agent Service (Python, `ai-agent`). D27: “Dapr Agents SDK is Python-only (GA 1.0.0). Run as a polyglot sidecar… MVP (optional enrichment), Phase 1.5 (full AI features).” Epics Additional Requirements copy D27/D28. Single-command boot: `dotnet run --project Hexalith.Memories.AppHost` “boots all containers (including Python AI Agent).”
- **Note:** This is a settled architecture decision the PRD never stated. It changes onboarding topology, license/ops surface, and how FR7 enrichment is produced. Silence reads as “C# only.”
- **Fix:** Architecture owns Dapr Agents. PRD should either defer (“AI enrichment runtime is an architecture decision; not a second product”) or add one NFR/constraint: optional Python `ai-agent` sidecar via DAPR invocation, not in-process C# agents.

### medium FR71 / export: PRD Phase 2 tag vs “already shipped” vs Epic 26 backup slice

- **PRD:** FR71 “Developer can export all memory units, metadata, and graph edges… **Phase:** Phase 2 unless a later sprint change explicitly pulls export into MVP.”
- **Downstream:** Epics Requirements Inventory copies FR71 **without** the phase clause. FR Coverage Map: “FR71: Epic 26 — Portable export reinforced through backup/restore… broader application-facing export remains Phase 2.” Architecture Requirements Coverage: “Deferred (Phase 2) | FR71 (export).” Story Key Policy: Story 8.3 `reserved-non-mvp` for FR71. 2026-08-03 remediation §1.1: “Story 8.3 is `done`; export services, REST endpoints, client methods, and CLI commands exist… Planning and sprint registration are stale.” That SCP told epics/sprint to register 8.3 as completed non-MVP and told the PRD to record “completed early but non-MVP.”
- **Note:** The PRD phase tag is still the right *MVP gate* (export is not a thesis gate). The living contract is wrong as a delivery record: it cannot tell an extractor that export already exists and must not be rescheduled as new Phase 2 work, nor that Epic 26 backup/restore is a different slice than application export.
- **Fix:** Keep FR71 out of MVP acceptance. Add “completed non-MVP (Story 8.3); Epic 26 covers operational backup/restore only.” Restore the phase clause in the epics inventory so PRD and epics match.

### medium Rate limiting: deferred to ingress in one PRD table, a product FR in the next

- **PRD:** Service Communication: “Rate limiting | Deferred to infrastructure (ingress, DAPR middleware).” FR8 per-tenant ingestion load; FR69 per-tenant embedding ceilings; pipeline actor “enforces per-tenant throttle ceilings.”
- **Downstream:** Architecture: `EmbeddingRateLimiterActor` is MVP-critical. Story 20.5 added ASP.NET inbound quotas partitioned by authenticated tenant, separate from embedding throttling. Epic 6 owns FR8.
- **Note:** Internal PRD contradiction. Downstream consistently made rate limiting a product concern. The communication table is the stale sentence.
- **Fix:** Delete “Deferred to infrastructure.” Distinguish embedding-provider throttle (FR69, actor) from inbound request quotas (Epic 20) from ingress. Architecture owns the split; PRD should name both as in-scope.

### medium Journey 1 / samples still sell EventStore zero-code as day-one onboarding

- **PRD:** Journey 1 is the EventStore auto-integration path (`dotnet add package Hexalith.Memories.Client`, DAPR subscription, test event). MVP Feature Set then says Journey 1 is “partial: CLI-only, no EventStore zero-code flow.” Samples table maps `samples/01-quickstart/` to Journey 1 and `samples/02-eventstore-integration/` to the zero-code promise.
- **Downstream:** Architecture Phase Compatibility: “MVP architecture (Phase 1: CLI-only, no MCP, no EventStore integration).” Epics: Epic 9 is Phase 1.5. Hard onboarding gate NFR31 is README quickstart, not EventStore.
- **Note:** The PRD almost corrects itself (“partial”), then lets Journey 1 and the samples table keep the zero-code story as the primary success path. Extracting journeys into UX without the scoping table would pull Epic 9 into MVP.
- **Fix:** Relabel Journey 1 as Phase 1.5 success path; make Journey 9 (CLI first case) the MVP success path in the summary table. Keep the zero-code narrative, with an explicit phase.

### low NFR6 / FR71 inventory wording drifted in epics, not in architecture’s count

- **PRD:** NFR6 includes “degradation documented when embedding provider is rate-limited.” FR71 includes the Phase 2 sentence.
- **Downstream:** Epics NFR6: “Event indexing freshness <5s from DAPR pub/sub publication to searchable [P1.5]” — degradation clause dropped. Epics FR71: phase sentence dropped (see FR71 finding). Architecture Requirements Overview does not restate individual FR/NFR text; it counts 74/31 and phase-filters in Requirements Coverage.
- **Note:** ID inventory is intact. Epics “Requirements Inventory” is no longer a lossless copy of the PRD, so it cannot be used as the PRD extract.
- **Fix:** Restore omitted clauses in epics inventory, or mark the inventory as a pointer to the PRD rather than a second full copy.

### low Epics Additional Requirements still freeze architecture D3 as “eventual consistency”

- **PRD:** Never stated D3. FR13 still “rollback or retry to achieve consistency across all axes.”
- **Downstream:** Epics Additional Requirements: “Eventual consistency + DAPR Workflow saga/compensation (D3).” Architecture Complete Decision Registry D3: “EventStore aggregate source of truth + rebuildable projections + DAPR Workflow projection compensation.”
- **Note:** This is epics lagging architecture, which makes a PRD→epics extract even less safe. It is not an independent PRD claim, but it shows the inventory section is not a current architecture extract either.
- **Fix:** Update epics D3 bullet to match architecture. PRD FR13 should use the EventStore-commit language from the August SCP (see critical finding).

### low UX makes explain mandatory on every search; PRD keeps `--explain` opt-in

- **PRD:** Interface matrix: “Search with explain | `memories search --explain`.” FR19 is “Developer can view per-axis score breakdown… (explain mode).”
- **Downstream:** UX Effortless Interactions: “After a search, Memories should automatically perform source lookup, evidence strength scoring, explain breakdown, and relevant graph traversal. The user should not need separate commands…” 2026-08-04 readiness report finding #1 records this exact conflict. Architecture defines the Evidence Packet envelope but “does not settle which fields are mandatory for every search versus populated only in explain mode.”
- **Note:** This is the one UX contradiction in scope. It is unresolved among all three spines, so the PRD is not uniquely stale — it is uniquely silent on the decision UX already made.
- **Fix:** PRD should pick: either every core search returns compact trust fields and `--explain` expands math, or UX-DR7 is opt-in. Do not leave extractors to reconcile FR19 with UX-DR7.

### low Access-telemetry PostgreSQL / OpenBao platform are operational, not a Redis-only product pivot

- **PRD:** “starts on Redis (RediSearch + Vector Search + FalkorDB), with architecture designed to support backend portability.” NFR15 Redis → Qdrant. NFR9 already requires OpenBao. FR67 is “logs search and access events per tenant,” not a store choice.
- **Downstream:** Epic 27 PG-ONPREM-1 / PostgreSQL 18.4 is the access-telemetry adapter, explicitly “not tamper-evident… audit retention.” Epics 29/31 OpenBao platform. Search backends remain Redis + FalkorDB.
- **Note:** No approved SCP replaced Redis search with another product backend. Claiming “PRD is Redis-only vs other backends” as a search-stack lie would be wrong. The miss is that the PRD never says telemetry may leave Redis, so an extractor could treat PG as scope creep.
- **Fix:** Architecture owns telemetry substrate. PRD FR67/NFR34 should defer store choice and repeat that access telemetry is not a compliance audit trail.

## Inventory check (IDs vs meaning)

| Spine | FR IDs | NFR IDs | Phase tags |
|---|---|---|---|
| PRD | FR1–FR74 present | NFR1–NFR31 present; NFR32+ absent | Sparse: FR71 has a phase sentence; NFR table is tagged; most FRs untagged |
| Architecture Requirements Overview | Count 74, not a verbatim list | Count 31, summarized | Requirements Coverage phase-filters FR/NFR; NFR11 still P1.5 there |
| Epics Requirements Inventory | FR1–FR74 verbatim except FR71 phase clause dropped | NFR1–NFR31; NFR6 shortened | Tags copied; D3/D4 bullets stale vs architecture body |

No missing or extra FR/NFR *numbers* between PRD and epics. The drift is meaning, phase, and decisions the PRD never absorbed.

## Brownfield harm

Yes, leaving “Greenfield” in the PRD harms it as a current contract. A new UX/architecture/story extract would still see a 7-feature, 22–32 story, actor-pipeline, C# 13, unauthenticated, atomic-Redis, EventStore-in-fast-follow product. The repository is a change-controlled brownfield system with 31 epics, shipped JWT, OpenBao, EventStore-backed domain writes, and an operational PostgreSQL telemetry path. The PRD can remain the thesis and outcome register only if it stops pretending the work has not started and if the August 2026 PRD patches are actually written.
