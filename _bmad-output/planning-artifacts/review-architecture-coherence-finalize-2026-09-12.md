# Adversarial Architecture-to-PRD Coherence Review — 2026-09-12

- **Reviewed:** `_bmad-output/planning-artifacts/prd.md`, `_bmad-output/planning-artifacts/addendum.md`
- **Architecture authority:** `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md`, especially `Current Alignment Gaps`
- **Status authority checked:** `_bmad-output/planning-artifacts/.memlog.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, and the readiness boundary in `epics.md`
- **Severity rule:** impact on the PRD's usefulness for release, architecture, UX, and story decisions—not implementation difficulty

## Verdict — REVISE

The September 12 update substantially improves requirement-level alignment: current-revision projection completion, case-partitioned tenant-wide graph search, safe query degradation, layered internal authorization, tenant erasure, durable idempotency, telemetry failure posture, and mixed-workload fairness are now stated in recognizable product terms. The PRD is nevertheless unsafe as an unqualified downstream decision contract because its ship gates, delivery register, and authority wording do not reconcile those requirements with the architecture gap ledger or the existing tracking authority.

Most importantly, G1–G5 can pass while multiple adopted MVP contracts remain partial, unverified, or without registered owners. Separately, the PRD lets implementation evidence override `sprint-status.yaml`, labels gated Phase 1.5 capabilities “Shipped” while architecture says they remain inactive, and creates an unratified phase exception to adopted AD-14. Those are decision-model failures, not editorial residue.

## Finding counts

| Severity | Count |
|---|---:|
| Critical | 1 |
| High | 6 |
| Medium | 4 |
| Low | 1 |
| **Total** | **12** |

## What coheres after the update

- **Projection completion:** PRD FR6/FR13 and the ingestion pipeline now preserve the distinction between EventStore commit, all-axis completion for the current revision/configuration, and query-time degradation. This is materially aligned with AD-2/AD-3.
- **Tenant erasure:** FR39, NFR16, the compliance boundary, and the addendum now join projection purge, EventStore content inaccessibility, telemetry handoff, non-reuse, and replay/restore non-resurrection. This is substantially aligned with AD-16.
- **Internal authorization:** FR44/FR65 and NFR10 distinguish bearer authority, workload policy, channel protection, the finite app allowlist, canonical `system:*` provenance, and explicit tenant grants. This is aligned in intent with AD-5.
- **Degradation/readiness:** FR66/NFR18 correctly make partial reads explicit and keep incomplete ingestion incomplete; FR72 correctly keeps liveness distinct from capability degradation. This is directionally aligned with AD-10 and the architecture health convention.
- **Fairness:** FR8, NFR13, NFR22, and NFR36 now name batch/repair/recovery/migration interference and durable provider retry timing. This captures AD-18's workload classes, although it still lacks an acceptance budget (H6).

These improvements make the remaining contradictions more consequential: they are no longer missing prose, but conflicts over what controls release and delivery decisions.

## Critical findings

### C1 — The Phase 1 go/no-go can pass while adopted architecture-critical MVP contracts remain incomplete and unowned

**Evidence.** The PRD calls itself the contract for “ship gates” (`prd.md` §0) and lists G1–G5 as the complete Phase 1 go/no-go (`prd.md` §Measurable Outcomes). The same PRD classifies FR6, FR13, FR34, FR39, FR65, and FR75 as partial; classifies FR8, FR66, and FR72 as needing re-verification; classifies NFR8–NFR10, NFR13, NFR18, and NFR22 as verified only against previous wording; and classifies NFR16 as a partial mechanism (`prd.md` delivery registers). The architecture gap ledger independently confirms missing EventStore-first ordinary ingestion, the all-axis checkpoint, authoritative replay, server-derived external provenance, tenant backend principals, and complete tenant erasure (`ARCHITECTURE-SPINE.md` §Current Alignment Gaps).

Yet G1–G5 gate retrieval quality, tenant leakage, CLI onboarding, case isolation, and deterministic explain only. They do not gate EventStore-before-scheduling, source-versioned projection completion/replay, full tenant erasure, layered internal app authorization, durable FR75 suppression, or measurable fairness. `prd.md` §Technical Success even calls NFR16 a “key hard gate,” but NFR16 is absent from the actual go/no-go table. The Release decision record lists owners only for stub verbs, the two-axis control, graph seeding, and the corpus; no registered story ownership is named for FR75 or several architecture gaps.

**Usefulness risk.** A release decision-maker can follow the PRD exactly, pass G1–G5, and declare the thesis increment releasable while violating adopted architecture and incomplete MVP FR/NFR contracts. Conversely, a story generator cannot tell whether those gaps block release, are accepted debt, or are merely future conformance work. The PRD therefore does not answer its highest-stakes question: what must be true to ship Phase 1?

**Fix.** Make the release decision conjunctive and explicit: (a) G1–G5 pass; and (b) every architecture-critical MVP conformance item has either passed named evidence or has a dated, owner-approved release exception. Register each unresolved item through approved change control in `epics.md`/`sprint-status.yaml`; do not manufacture ownership in the PRD. At minimum disposition EventStore-first ingestion, FR6/FR13/NFR16 completion and replay, FR39 erasure, FR44/FR65/NFR10 internal authority, FR75 durable suppression, and FR8/NFR13 fairness. If some are intentionally not Phase 1 ship blockers, say so as explicit scope decisions and align the canonical phase register.

## High findings

### H1 — The delivery register reverses the recorded tracking-authority decision

`prd.md` §Functional Requirements says: “On conflict, current implementation evidence wins for delivery status.” The canonical PRD memlog says the opposite: “register conflict rule = `sprint-status.yaml` wins” (`planning-artifacts/.memlog.md`, 2026-09-08 decision). `epics.md` further says `sprint-status.yaml` owns machine-readable readiness accounting. The architecture gap ledger calls its rows “implementation obligations against adopted decisions”; it does not claim status or tracking authority. `addendum.md` also says `epics.md` and `sprint-status.yaml` still require a change-control pass after the September reconciliation.

The September update therefore silently changed an authority rule while claiming only to reconcile product-level deltas. A gap ledger may refute a claim of current conformance, but it cannot re-status a registered epic/story or replace readiness accounting.

**Fix.** Restore the memlog decision. Split every register entry into two independent facts: **tracking state** (verbatim from `sprint-status.yaml`, which wins conflicts) and **current-contract conformance** (confirmed/partial/unverified/refuted against the architecture and evidence). A contradicted implementation claim downgrades conformance and triggers change control; it does not silently replace tracking authority.

### H2 — “Shipped” incorrectly activates Phase 1.5 product capabilities that architecture and the release record keep inactive

The FR delivery register marks FR54–FR64 “Shipped,” which includes MCP (FR54/FR58) and EventStore product integration (FR59–FR62). The same PRD says L1–L3 have not been evaluated, packages stay preview, and the surfaces are not announced on launch failure (`prd.md` Release decision record). AD-12 is stricter: MCP and EventStore CloudEvent product capabilities remain inactive until L1–L3 pass even if deployment assets exist (`ARCHITECTURE-SPINE.md` AD-12). `sprint-status.yaml` can legitimately report Epics 9–10 `done` while readiness accounting excludes them from active MVP; that is implementation completion, not product activation.

**Usefulness risk.** “Shipped” is externally and downstream-readable as available product capability. The register collapses implemented code, tracker completion, preview availability, gate qualification, and launch activation into one word, contradicting the architecture's explicit activation boundary.

**Fix.** Use separate states such as `implemented / tracking done`, `inactive preview pending L1–L3`, and `activated/shipped`. Apply them to FR23, FR54, FR58–FR62 and any other gate-bound capability. Preserve `sprint-status.yaml` status verbatim rather than translating `done` into product “Shipped.”

### H3 — The PRD/addendum create an unratified phase exception to adopted AD-14

AD-14 is an adopted, unqualified rule: provider/schema changes use disjoint create-backfill-verify-switch-retire resources, full-tenant reindex, and atomic activation (`ARCHITECTURE-SPINE.md` AD-14). The spine says adopted rules remain binding where code differs. The PRD instead keeps MVP FR43 as an acknowledged degraded rebuild, while Phase 2 promises zero-downtime embedding/model migration and Phase 3 holds backend migration. `addendum.md` §Migration phasing declares that AD-14 is only the Phase 2/3 target and “does not rebaseline MVP FR43.” The PRD memlog records the same phasing decision.

That phase limitation does not exist in the final architecture decision. It is a product/addendum reinterpretation of architecture, not a reconciliation. Because the architecture gap ledger also contains no AD-14 row, a downstream reader cannot tell whether MVP FR43 is an approved exception, a contradiction, or already conformant.

**Fix.** Ratify one boundary in the architecture authority: either amend AD-14 to state its Phase 2/3 activation and explicitly permit the bounded FR43 MVP degraded-rebuild exception, or bring FR43/MVP behavior under AD-14. Then add any current non-conformance to the gap ledger and track it. Do not let the addendum phase an adopted architecture rule by assertion.

### H4 — Implementation detail retained in the PRD has already drifted into a false Dapr/FalkorDB topology

The PRD says topology and mechanism live in architecture/addendum, and the addendum's declared role is mechanism/topology that “must not live in the product-outcome PRD.” Nonetheless, the PRD retains detailed package, topology, service-communication, actor, route, and provider material. That duplication is already contradictory:

- `prd.md` §Deployment Topology says `Memories Server → DAPR → Redis/FalkorDB`.
- `prd.md` §Service Communication Model correctly says Redis/FalkorDB search and graph use approved direct boundary clients, not Dapr state as a generic proxy.
- `prd.md` §Open-Source Licensing says FalkorDB is reached “via DAPR state management abstraction” and that the Dapr sidecar is the client.
- AD-8 requires direct Redis/FalkorDB data-plane operations behind named adapters; Dapr owns portable coordination state.

The licensing rationale is therefore built partly on a topology fact that the architecture rejects. Whether network separation has a particular licence consequence requires legal review; the PRD must not derive that conclusion from an incorrect client boundary.

**Fix.** Remove the duplicated topology and licensing-mechanism rationale from the PRD, or align every copy to AD-8 and make architecture the only technical authority. State only the product constraint and required legal/operator decision in the PRD. Have the dependency-licence conclusion reviewed independently of the false “Dapr sidecar is the client” premise.

### H5 — FR75's “one projection outcome” can suppress legitimate reprojection across schema/configuration epochs

FR75 says duplicate identities produce “one durable domain mutation and one projection outcome.” AD-4 instead says the identity suppresses duplicate EventStore/workflow acceptance while every projection is an idempotent upsert for AD-3's full tuple, which includes source version, schema generation, and embedding configuration epoch. AD-3/AD-14 require later replay, repair, backfill, and migration to produce projection work for a new tuple without producing a second domain mutation.

**Usefulness risk.** A literal FR75 test could assert one projection outcome for the lifetime of a command/event and incorrectly reject a necessary re-projection under a new schema or embedding epoch. The new FR is also absent from the spine's formal `binds` metadata and has no registered story owner in `epics.md`/`sprint-status.yaml`, so downstream extraction has neither precise semantics nor tracked ownership.

**Fix.** Say: one durable domain mutation per accepted scoped identity; duplicate delivery is idempotent within each projection tuple; a new schema generation/configuration epoch may create a new projection outcome for the same authoritative mutation. Refresh the spine binding to FR1–FR75 and register implementation ownership through change control.

### H6 — Fairness names the workloads but still has no numeric pass/fail budget

AD-18 says numeric capacity/fairness budgets live in the PRD and validated configuration. FR8 and NFR13 say work “cannot starve,” but NFR13's target only describes a three-tenant workload mix; it supplies no maximum latency regression, queue wait, admission delay, throughput share, or bounded queue size. NFR12's 5% rule says “performance” without identifying which NFR1–NFR5 measure must stay within the delta. NFR36 tests one file/URL freshness path but cannot prove query fairness, large-ingest fairness, recovery priority, or provider scheduling fairness.

**Usefulness risk.** Any implementation can claim “bounded” queues and “no starvation” after merely running the proposed load. The strengthened fairness requirement cannot be accepted, rejected, or used as a release exception.

**Fix.** Define the allowed p95/p99 latency and throughput degradation for interactive query/ingest under the NFR13 workload; maximum admission/queue wait or minimum service share per active tenant; queue bounds and overload response; and the priority relationship for repair/recovery/migration. Tie the pass rule explicitly to NFR1–NFR5 and NFR12.

## Medium findings

### M1 — Capability-aware readiness has a query-dependent condition but no observable capability contract

FR72 and the health table say a backend outage leaves the server ready while “safe selected axes” remain usable. Readiness is evaluated outside a particular query, so there are no selected axes at probe time. The PRD does not state where per-axis status appears, which states exist, how an operator distinguishes degraded from unavailable, or when loss of an axis changes global readiness. FR66/NFR18 define an Evidence Packet for query responses, not the health/readiness payload.

**Fix.** Define a machine-readable capability-status matrix (at least syntactic/semantic/graph plus EventStore command path), its state vocabulary and freshness, the global readiness reduction rule, and the relationship between health state and FR66 response degradation. Keep request selection out of the readiness predicate.

### M2 — The tenant-erasure exception for “cross-references” is ambiguous against tenant/case isolation

The compliance section says cross-references to deleted tenant data in other tenants' memory units are the application's responsibility. AD-5/AD-7 and NFR8 require tenant isolation, and cross-case graph edges are forbidden even within a tenant. If “cross-reference” means a product graph edge or resolved pointer, the exception contradicts architecture; if it means independently copied/ingested content owned by another tenant, it is a legitimate erasure boundary but is not stated that way.

**Fix.** Explicitly prohibit product-created cross-tenant edges/pointers. Limit the erasure exception to independently supplied copies or textual references that are authoritative content of another tenant, and state that the product cannot discover or delete those without violating that tenant's authority.

### M3 — FR71's register note still confuses portable export with operational restore

The CLI table and FR71 correctly say Story 8.3 shipped developer-facing portable case/tenant export. The delivery register instead says “Story 8.3 operational export/restore; application-facing export stays Phase 2.” The actual Story 8.3 is `done` and explicitly ships the REST/client/CLI portable export; it explicitly excludes re-import/restore. `sprint-status.yaml` marks it Phase 2/reserved-non-MVP for readiness accounting, which is compatible with “implemented early” but not with the register's capability description.

**Fix.** Record: tracking `done`; phase/readiness `Phase2, reserved-non-mvp`; product capability `portable application-facing export implemented early`; operational restore is separate Epic 26 work. Do not use “operational export/restore” as a substitute for FR71.

### M4 — Journey 8 is still assigned to two phases

The heading says `Journey 8 ... (Phase 1)`, while the explicit Non-Goal, Journey summary, coverage check, and Phase 2 scope place Priya's application-facing REST UI in Phase 2. This is exactly the sort of isolated heading a UX/story extractor will use.

**Fix.** Change the Journey 8 heading to Phase 2 and retain the distinction that an internal/CLI HTTP transport may exist in Phase 1 without activating an application-facing UI.

## Low finding

### L1 — The architecture spine's formal binding metadata omits FR75

The spine frontmatter still binds `FR1-FR74`, while the PRD now declares FR1–FR75 and its addendum explicitly acknowledges the stale binding. AD-4 substantively anticipates FR75, so this is not an architecture-design hole, but automated traceability or downstream extraction can still classify FR75 as unbound.

**Fix.** After FR75 wording is corrected per H5 and accepted, update the architecture spine binding through its own change-control path. The PRD/addendum should not imply that acknowledgement in a handoff is equivalent to an updated architecture authority.

## Required disposition before this PRD is used as a release/story authority

1. Restore `sprint-status.yaml` as tracking/readiness authority and separate tracker state, requirement conformance, and product activation.
2. Add an explicit Phase 1 release disposition for every architecture-critical MVP gap; register owners via approved tracking change rather than in the PRD.
3. Resolve AD-14 phasing in architecture, not through addendum interpretation.
4. Correct FR75's projection scope and bind/register it.
5. Remove or reconcile the contradictory Dapr/FalkorDB topology and re-check the dependent licensing statement.
6. Give fairness and capability readiness executable pass/fail contracts.

