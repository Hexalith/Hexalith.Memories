# Adversarial PRD Review — Hexalith.Memories

- **Reviewed:** `prd.md` and `addendum.md`
- **Lens:** decision-readiness, phase/scope integrity, testability, ownership, security, operations, data governance, and downstream ambiguity
- **Verdict:** **Not decision-ready for downstream architecture/epic sign-off or a Phase 1.5 launch decision.** Two critical contradictions make promised behavior or gates impossible to prove as written; eight high-severity gaps leave the core thesis, authorization, recovery, and launch qualification open to materially different implementations.
- **Finding counts:** Critical 2 · High 8 · Medium 8 · Low 0

## Critical

### ADV-C1 — “Delete all data” can be undone by the durability contract

- **Locations:** § Compliance Boundary (“Tenant deletion … removes all indexes, graph data, and memory units”); FR27; FR39; FR13; NFR16; NFR34; addendum § EventStore — three contracts.
- **Conflict:** FR27/FR39 and the compliance text present case/tenant deletion as erasure. FR13 and the addendum make the EventStore commit the durable source of truth, while NFR16 requires lost projections to be rebuilt from that commit. The PRD never states whether deletion purges, redacts, crypto-shreds, or permanently suppresses the corresponding EventStore history, nor how backups and access telemetry follow deletion.
- **Failure mode:** A tenant can be reported deleted while recoverable source events remain and later replay recreates its memory units. That breaks the product’s erasure claim and creates an undeclared data-retention obligation at the exact boundary the compliance section says applications may rely on.
- **PRD-level fix:** Define one deletion/erasure contract by data class: authoritative EventStore records, extracted content, projections, embeddings, graph edges, workflow history, telemetry, and backups. State the deletion mechanism, replay suppression rule, completion state/SLA, backup expiry, failure recovery, and verification evidence. Until that contract exists, replace “enabling applications to fulfill erasure requests” with a bounded deletion claim that names retained data.

### ADV-C2 — Launch gate L1 requires labels that, by definition, do not exist

- **Location:** § Measurable Outcomes › Phase 1.5 launch go/no-go, L1.
- **Conflict:** L1 requires “≥10 topics not used in G1 labelling,” then names “the frozen G1 labels” as the scorer. A topic excluded from G1 labelling has no G1 relevance labels. The threshold is also written as “≥8/10” despite allowing more than 10 topics.
- **Failure mode:** L1 cannot be evaluated without silently relabelling the held-out set, reusing non-held-out topics, or inventing a denominator. Any of those changes can turn the same agent results into pass or fail after the fact.
- **PRD-level fix:** Define a separately frozen, independently graded L1 set that is excluded from G1 scoring and all agent tuning but uses a named relevance scale and adjudication method. Fix the size to exactly 10 or use a percentage for N ≥ 10; identify the corpus hash, owner, scorer, reference-agent configuration, and pass/fail artifact.

## High

### ADV-H1 — The core G1 thesis gate still lacks a scoreable ground-truth contract

- **Locations:** § Measurable Outcomes › Thesis-gate protocol (“representative mix,” “graded labels”); § Release decision record › G1 prerequisites; Open Question 2.
- **Gap:** N ≥ 50 and κ ≥ 0.6 are numeric, but the PRD never defines the relevance-grade scale used by NDCG, how three reviewers’ grades become the canonical label, whether every pair must meet κ, how ties/abstentions are handled, or the sampling frame that makes the mix “representative.” The corpus and reviewer recruitment remain an open question, and the release record acknowledges that no owning story exists.
- **Failure mode:** Teams can produce incompatible yet facially compliant G1 suites, and a 2026-12-01 decision can be made on a corpus selected to favor or suppress graph benefit.
- **PRD-level fix:** Promote Open Question 2 to a gate prerequisite. Specify the query-source population and sampling rule, relevance-grade scale, per-reviewer workflow, agreement rule, aggregation/adjudication, freeze/hash artifacts, accountable owner, and owning epic/story. No scoring run should count until that protocol artifact is approved.

### ADV-H2 — Authorization is only tenant isolation; privileged product actions have no policy

- **Locations:** Glossary › Member; § AI Reliability › Memory unit provenance (“Tenant claims authorize”); FR28–FR29; FR38–FR45; FR51; FR67; FR71; FR74; NFR8; FR44; Journey 5.
- **Conflict/gap:** The PRD explicitly says case membership does not authorize, but defines no replacement role or permission model for tenant deletion, tenant configuration, export, consistency repair, access-telemetry viewing, member mutation, or edge-confidence promotion. NFR8 also permits a mismatched tenant request to be “rejected **or returns only A’s data**,” while FR44 and Journey 5 require a tenant mismatch to be rejected.
- **Failure mode:** One implementation can let every tenant principal perform destructive/admin operations; another can invent operator roles and produce an incompatible API. A test suite can accept silent retargeting to tenant A even though the user explicitly requested B, causing integrity failures and hiding authorization bugs.
- **PRD-level fix:** Add a capability-by-role authorization matrix for platform operator, tenant operator, contributor, reader, and authenticated service identities, including telemetry and destructive operations. Make every tenant-claim/request mismatch fail closed with one specified error; remove “or returns only A’s data” from NFR8. State that membership remains non-authorizing until a named phase.

### ADV-H3 — NFR16 promises rebuild without requiring durable rebuild inputs

- **Locations:** FR6; FR13; FR70; § Async Ingestion Pipeline; NFR16 (“vectors must be re-embedded because the commit does not store them”).
- **Gap:** The PRD requires all EventStore-committed units to become searchable after projection loss, but does not require the commit to retain an immutable extracted-content snapshot, source bytes/hash, extraction version, chunking/version metadata, or a usable embedding-model contract. URL/file sources can change or disappear, and the original embedding model/provider can be retired.
- **Failure mode:** “Rebuild from EventStore” can produce different content/rankings, fail permanently, or be impossible while still appearing to satisfy zero source-event loss. NFR16’s throughput bound does not make the result equivalent or even rebuildable.
- **PRD-level fix:** Define the minimum durable recovery payload and its versioning: content snapshot or immutable content address, extraction/chunking versions, metadata, source version, provider/model identity, and fallback when a model is unavailable. Define equivalence criteria for rebuilt projections and evidence that every committed unit is recoverable without re-fetching mutable external sources.

### ADV-H4 — Access telemetry is launchable before its privacy and retention contract exists

- **Locations:** Glossary › Access telemetry; Journey 5 (“who, when, and what was attempted”); § Compliance Boundary; FR67; NFR34; NFR delivery status.
- **Gap:** FR67 is MVP/shipped, but the PRD does not define whether raw search terms, content, identifiers, errors, or rejected payloads are recorded; who may read/export them; what must be redacted; or how tenant deletion interacts with them. NFR34 asks for a “configured TTL” and an owner but gives no required default/range and remains in progress.
- **Failure mode:** Queries and failures can become a second, less-protected store of sensitive tenant data. Operators can claim erasure while telemetry persists, or erase security evidence too early, depending on architecture choices not constrained by the PRD.
- **PRD-level fix:** Add a telemetry data dictionary and minimization rule, sensitive-field redaction, access roles, tenant scoping, encryption expectations, default/max TTL, tenant-erasure behavior, purge evidence, and an explicitly named accountable owner. Make production/launch qualification contingent on NFR34 evidence or declare telemetry disabled by default until it exists.

### ADV-H5 — The public API has two incompatible versioning stories and no stability contract

- **Locations:** § Service Communication Model; § Versioning Strategy (“no versioned endpoints”); CLI Specification (“`/api/v1`,” `Contracts.V1.MemoriesRoutes`); § Evidence Packet (`Contracts.V1`); published `Client.Rest`, `Contracts`, `Mcp`, and `EventStore` packages.
- **Conflict/gap:** The PRD says the service has “no versioned endpoints,” while the shipped REST routes and contract namespace are explicitly V1. “Backward-compatible additions only” does not define which JSON fields/enums/CLI JSON/MCP schemas are stable, preview, or removable, nor a support/deprecation window.
- **Failure mode:** Architecture and epics can independently treat `/api/v1`, `Contracts.V1`, CLI JSON, and MCP as either permanent contracts or disposable preview surfaces. Consumers can be broken while every team claims compliance with a different sentence.
- **PRD-level fix:** Establish one compatibility policy across REST, DAPR messages, NuGet contracts, MCP tools, and CLI JSON: current maturity, version identifier, allowed additive changes, enum/required-field rules, breaking-change vehicle, deprecation/support window, and how preview Phase 1.5 packages differ from stable Phase 1 surfaces.

### ADV-H6 — The onboarding stopwatches do not define reproducible starting states

- **Locations:** L2 launch stopwatch; NFR31; § Developer Experience & Documentation; Release decision record.
- **Gap:** NFR31’s “clean machine” has only Docker and .NET installed, but the procedure starts from an AppHost/README without saying whether repository checkout, CLI installation, restore, and sample data are pre-positioned. L2 starts in an “existing EventStore-based service” yet uses a fresh profile and a sample that does not exist. The provider API key is off-clock, while secret-store/AppHost seeding and external-service setup are not clearly classified.
- **Failure mode:** One run starts with a cloned/restored repo and warm package caches; another includes checkout, restore, image pulls, and service preparation. Both can claim the same <30-minute target. The launch gate is schedule-sensitive but not reproducible.
- **PRD-level fix:** Publish a fixture manifest and exact T0 snapshot for G3 and L2: source checkout/sample commit, cache state, CLI installation state, secret-store state, allowed pre-provisioned credentials, network/cache rules, commands, stop event, supported OS matrix, and evidence template. Make absent samples an owning story, not only a status note.

### ADV-H7 — Degraded reads and incomplete ingestion have no single eligibility rule

- **Locations:** FR6; FR13; FR66; § Async Ingestion Pipeline; NFR18; Evidence Packet.
- **Conflict/gap:** FR6/FR13 prohibit a unit from becoming searchable until all three projections acknowledge the same source version. FR66/NFR18 require results when one backend is unavailable. The PRD does not distinguish a previously complete unit whose backend is temporarily unavailable from a newly partial/stale unit, nor define whether results across surviving axes must share a source version.
- **Failure mode:** One implementation hides every result during a backend outage to preserve FR13; another serves newly incomplete or version-skewed units under FR66. Either behavior can be defended, but they produce materially different trust and consistency guarantees.
- **PRD-level fix:** Define query eligibility under degradation: only units that previously reached `indexed`, required source-version agreement across available axes, stale-version rules, per-result degradation/freshness fields, and recovery behavior. State whether a temporarily unavailable graph may remove the unit or only the graph contribution.

### ADV-H8 — The launch gate omits known unmet Phase 1.5 security/reliability requirements

- **Locations:** § Phase 1.5 launch go/no-go (L1–L3); NFR delivery status; NFR6; NFR11; NFR16; NFR21; NFR34; Release decision record.
- **Gap:** L1–L3 cover token budget/answer relevance, setup time, and causal completeness only. The status register says event freshness is not started, NFR21 negative-envelope proof is owed, NFR16’s current recovery proof is owed, and NFR34 is in progress. Yet none is a launch criterion, and “no launch” is described solely in terms of L1–L3.
- **Failure mode:** The product can formally launch while events miss the freshness promise, malformed publishers behave unpredictably, recovery is unproven, or telemetry retention is uncontrolled. “All NFRs apply” is not a decision rule when the release table names only three gates.
- **PRD-level fix:** Add a production-qualification row to the launch record listing the NFRs that must be verified or explicitly accepted as dated debt. Name the acceptance authority and evidence links. If Phase 1.5 is preview-only, say so and define which requirements are deferred before a general-availability launch.

## Medium

### ADV-M1 — Journey 8 assigns the same web experience to Phase 1 and Phase 2

- **Locations:** Journey 8 heading (“Phase 1”); Non-Goals (“Application-facing REST search UI … Phase 2”); Journey Requirements Summary (“J8, Phase 2”); § Language & Platform Matrix.
- **Failure mode:** UX and epic authors can legitimately schedule Priya’s narrative UI, source-link workflow, and causal explanation in either phase, producing scope creep or missing Phase 2 acceptance.
- **PRD-level fix:** Retag the Journey 8 heading and every capability beat as Phase 2. Explicitly separate the already-available REST transport/client from the application-facing search experience and narrative composition that remain out of Phase 1.

### ADV-M2 — The “one ingestion vocabulary” is still two public vocabularies

- **Locations:** Glossary › Ingestion state; FR10; § Async Ingestion Pipeline; Open Question 8.
- **Conflict:** The PRD mandates `pending`/`projecting`, while shipped `Contracts.V1.MemoryUnitStatus` exposes `Queued`/`Indexing`; the mapping is called temporary but is also allowed to remain permanently open.
- **Failure mode:** CLI JSON, REST, docs, tests, and future UX can choose different wire values while each points to a PRD-approved term. This is especially costly once the public contract is consumed.
- **PRD-level fix:** Choose canonical wire values before the next stable contract release. If compatibility prevents rename, make the shipped enum the canonical wire vocabulary and reserve the other terms for presentation with an explicit mapping/version policy; close Open Question 8 with an owner/date.

### ADV-M3 — FR71 is marked shipped for a different capability than FR71 states

- **Locations:** FR delivery status; FR71; CLI surface › `export`; canonical phase register.
- **Conflict:** FR71 promises developer-facing portable case/tenant export and is marked “Shipped early,” while its notes say Story 8.3 delivered “operational export/restore” and that “application-facing export stays Phase 2.”
- **Failure mode:** Phase 2 planning may omit the application export because FR71 is shipped, while acceptance may count an operational backup format as the portable developer contract.
- **PRD-level fix:** Split operational backup/restore and portable application export into separate requirements and statuses, or mark FR71 partial until its public format, completeness, versioning, and CLI behavior meet the stated capability.

### ADV-M4 — Freshness states are named but have no product semantics

- **Locations:** Journey 7; Evidence Packet; NFR33.
- **Gap:** `current`, `aging`, `stale`, and `unknown` drive the agent’s caveat behavior, but the PRD supplies no thresholds, clock source, source-specific policy, or recovery actions. NFR33 delegates “authoritative thresholds” to a future contract without setting outcomes.
- **Failure mode:** CLI, MCP, and web implementations can classify the same unit differently, defeating the “cross-surface trust envelope.”
- **PRD-level fix:** Define freshness basis and thresholds per source class, how ingestion/update timestamps are selected, clock-skew tolerance, transitions, and required disclosure/recovery behavior. Architecture may encode the values but should not invent their product meaning.

### ADV-M5 — CLI bearer-token handling is unsafe and unspecified

- **Locations:** CLI Specification › global `--token`; Configuration Layering; NFR9; NFR11.
- **Gap:** Authentication is mandatory, but the only explicit CLI credential surface is a command-line token, which can leak via shell history and process inspection. There is no token acquisition, refresh, secure storage, redaction, or noninteractive CI policy.
- **Failure mode:** The official operational path encourages handling credentials less safely than the strict application-secret posture elsewhere in the PRD.
- **PRD-level fix:** Define supported secure credential sources and precedence (for example OS credential store/device login and stdin for automation), forbid token echo/logging/config-file persistence, specify expiry/refresh and CI injection behavior, and retain `--token` only if its risk is explicitly accepted and warned.

### ADV-M6 — The PRD makes categorical licensing conclusions without a decision authority

- **Locations:** § Open-Source Licensing; especially FalkorDB (“this architectural boundary means application code is not subject to AGPL copyleft”) and Redis Stack (“Cannot offer … as a competing managed service”); addendum § Topology.
- **Gap:** These are legal conclusions, not measurable product outcomes, and no legal reviewer, version-specific assessment, jurisdiction, or accepted-risk owner is named. The topology statement used to justify the conclusion can also drift.
- **Failure mode:** README/deployment guidance can repeat a legal conclusion as settled fact and expose users to a licensing interpretation the project is not authorized to guarantee.
- **PRD-level fix:** Recast these as identified licensing risks and required legal-review decisions. Name the exact deployed artifacts/versions, accountable owner, review date, accepted deployment modes, and wording approved for public docs. Keep architecture facts in the addendum, not as proof of legal outcome.

### ADV-M7 — The PRD/addendum boundary has no usable change-control rule

- **Locations:** § 0 Document Purpose; § Technical Architecture Considerations; § Deployment Topology; § Async Ingestion Pipeline; NFR9; Measurable Outcomes › Graph seeding; addendum § Why this file exists.
- **Conflict:** The PRD says topology and mechanisms belong in architecture/addendum, but then mandates exact products and mechanics: Redis/RediSearch/FalkorDB, OpenBao, DAPR Workflow ownership, service topology, package inventory, and top-5/top-5 depth-2 graph seeding. The addendum says architecture is authoritative when facts drift but also says it cannot override the PRD.
- **Failure mode:** A backend/topology or algorithm change can be treated either as an architecture-only change or as a PRD breach. That ambiguity is already a downstream-drift generator in a change-controlled product.
- **PRD-level fix:** Add a short decision-classification table: immutable product constraints, observable acceptance behavior, replaceable reference implementation, and architecture-owned current choices. For each mechanism retained in the PRD, state whether changing it requires product change control; move the rest to the addendum/architecture.

### ADV-M8 — Availability and disaster-recovery expectations stop at component examples

- **Locations:** NFR7; NFR16–NFR19; § Health Check & Observability; Release decision record.
- **Gap:** The PRD has latency, cold-start, Redis-restart, and single-backend chaos targets, but no service availability objective, recovery target for EventStore/DAPR Workflow/FalkorDB loss, maximum degraded duration, backup/restore cadence, or operator escalation boundary.
- **Failure mode:** A launch can satisfy every named gate while the service has undefined downtime and recovery behavior outside a controlled Redis restart. Architecture cannot size HA/backup work from the PRD.
- **PRD-level fix:** Either define a launch-level availability/RPO/RTO envelope for authoritative state and projections, including dependencies and degraded-mode duration, or explicitly state that production HA/SLA is out of scope and label Phase 1.5 as non-production preview.

## Gate recommendation

**Reject current sign-off.** Resolve ADV-C1 and ADV-C2 before any launch or compliance claim; promote ADV-H1, ADV-H2, ADV-H5, ADV-H6, and ADV-H8 to phase-blocking open items with owners and dated evidence. Architecture/epic work can continue on already-settled implementation details, but this PRD should not be used as the acceptance contract for deletion, public API compatibility, or Phase 1.5 launch until those items are closed.
