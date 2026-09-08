# Downstream Drift Review — Hexalith.Memories

## Verdict

The 2026-09-05 PRD Update did apply the August Major SCP product amendments. Today's `prd.md` is a trustworthy **source-extract for product outcomes** — dual ship contracts, FR1–FR74 with a phase register, NFR1–NFR35, the three EventStore meanings, identity/provenance, DAPR Workflow ingestion, RRF, isolation *outcome*, and two onboarding clocks. It is not a wholesale ingest of architecture or epics, and it must not be used that way. The live drift risk has reversed: architecture Requirements Overview / Coverage / PRD Deviations, and the epics Requirements Inventory, still describe the pre-Update PRD. Extracting UX, architecture, or stories from those stale copies reconstitutes the 2026-09-05 failure mode even though the PRD itself no longer does.

## Method

Read in full: `prd.md` (frontmatter `updated: 2026-09-05`) and `addendum.md` (same date). Source-extracted — not ingested wholesale — against architecture Requirements Overview, Technical Constraints, Cross-Cutting Concerns (isolation, fusion, consistency), Interface Philosophy, Evidence Packet, Security Architecture, Deployment Topology, PRD Deviations, Gate-Blocking, Decision Registry (D1–D10, D29), Requirements Coverage; and against epics Overview, Requirements Inventory, Additional Requirements, FR Coverage Map, Implementation Readiness Boundary, and epic headers 0–10, 20, 21, 27, 28. Deep-read both 2026-08-03 Major SCPs (PRD-n lists), `sprint-change-proposal-2026-09-06-story-27-4-live-producer-contract.md`, and skimmed `implementation-readiness-report-2026-08-04.md` PRD Analysis plus the 2026-09-05 `review-downstream-drift.md` (the document this file replaces).

**SCPs dated after 2026-08-03 (memories `_bmad-output/planning-artifacts/` only):**

| File | Product-level PRD rewrite? | Action |
|---|---|---|
| `sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md` | Yes — PRD-1…PRD-7 | Deep-read |
| `sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md` | Yes — PRD-1…PRD-6 | Deep-read |
| `sprint-change-proposal-2026-08-03.md` | No — Epic 27 C1 ownership | Title/scope only |
| `sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md` | No — verifier backlog; FR40/NFR8 unchanged | Title/scope only |
| `sprint-change-proposal-2026-08-14-story-own-commit-file-list-scoping.md` | No — story-gate process | Skipped |
| `sprint-change-proposal-2026-08-31-story-28-1-eventstore-identity-toolchain-mismatch.md` | No — Epic 28 package-hash AC; “`PRD.md` … need no changes” | Skimmed |
| `sprint-change-proposal-2026-09-06-story-27-4-live-producer-contract.md` | No — “PRD: no change” | Deep-read (only post-Update SCP) |

Skipped as process/CI/story-split unless they rewrote a product requirement (story-gate hooks, commit file-list, most Epic 27 checkpoint splits). EventStore-repo SCPs under `references/` are out of scope.

**Already true in today's PRD (do not re-litigate as missing amendments):** brownfield classification; dual Phase 1 / 1.5 go/no-go; Journey 9 as thesis path and Journey 1 as Phase 1.5; RRF not magnitude-blend; EventStore domain-SoT vs product integration vs runtime pin; C# 14; NFR11 MVP; identity `sub` / `system:*`; workflow ingestion + projection state machine; package inventory via `tools/release-packages.json`; NFR32–NFR35 with the NFR33 collision recorded in Assumptions Index.

## August 2026 SCP PRD amendments

Reconciliation note: the two same-day Major SCPs used overlapping `PRD-n` numbers for different edits, and both used **NFR33** for different requirements. The Update split them (Assumptions Index): NFR33 = Evidence Packet freshness (rerun); NFR35 = future-web interaction performance (remediation-batch). SDK `10.0.302` was **not** copied into the PRD; addendum + `global.json` own the pin (intentional — later 2026-08-31 toolchain is `10.0.400`).

### Remediation-batch (`…-remediation-batch.md` §5.1)

| Item | Required edit | Status |
|---|---|---|
| **PRD-1** | Canonical FR phase register (MVP / 1.5 / Phase 2 FR71; FR53 per-phase; `NotImplementedCommand` is not coverage) | **Landed in PRD** — Functional Requirements preamble + FR53/FR71 tags + CLI matrix |
| **PRD-2** | `.NET 10 / C# 14`; record SDK `10.0.302` | **Landed in PRD** (language). **Landed in addendum** (SDK pin / `global.json`). Not missing as a product FR |
| **PRD-3** | Tenant claims authorize; case membership is metadata; provenance binds to `sub`; allowlisted `system:*` only through an authenticated service boundary | **Landed in PRD** — AI Reliability “Memory unit provenance” + Service Communication identity row |
| **PRD-4** | EventStore ack = durable commit; search/vector/graph = rebuildable projections; observable state machine; no distributed transaction | **Landed in PRD** — Async Ingestion Pipeline + FR6 + FR13 |
| **PRD-5** | NFR11 is current MVP invariant; anonymous only named health/DAPR routes | **Landed in PRD** — NFR11 phase **MVP** |
| **PRD-6** | NFR32 WCAG/web a11y; NFR33 web interaction performance | **Landed in PRD** — NFR32 as specified; web performance **as NFR35** (id collision) |
| **PRD-7** | NFR34 access-telemetry lifecycle; not a compliance audit trail; Epic 27 C1 governs Production | **Landed in PRD** — NFR34 |

### Rerun (`…-rerun.md` §4.1)

| Item | Required edit | Status |
|---|---|---|
| **PRD-1** | C# 14; SDK `10.0.302` + `rollForward=latestFeature` | **Landed in PRD** (C# 14). **Landed in addendum** (SDK / `global.json` authority) |
| **PRD-2** | Delete the “defer cases and tenant isolation” minimum-scope escape | **Landed in PRD** — Resource Risks + Non-Goals last bullet |
| **PRD-3** | `tools/release-packages.json` sole published inventory; enumerate non-packable hosts; no unexplained “+3” | **Landed in PRD** — Package Distribution tables (Server, AppHost) |
| **PRD-4** | DAPR state API vs Aspire-injected Redis/FalkorDB clients; no “sidecar as generic proxy” | **Landed in PRD** — Service Communication split rows |
| **PRD-5** | NFR11 MVP; unauthenticated product ingress is not a P1.5 allowance | **Landed in PRD** — same NFR11 text as remediation PRD-5 |
| **PRD-6** | Bind FR1–FR74 to the phase register; NFR32 web a11y; NFR33 freshness semantics | **Landed in PRD** — phase register + NFR32 + NFR33 freshness |

Nothing from either required PRD-n list is still missing as a product-outcome sentence. The 2026-08-04 readiness report’s PRD Analysis is **historical**; it describes the 2026-07-19 PRD, not today’s file.

## Post-2026-09-05 drift

One memories SCP exists after the Update:

- **`sprint-change-proposal-2026-09-06-story-27-4-live-producer-contract.md`** (Administrator-approved). Amends Story 27.4’s Trusted Evidence Contract and two sequencing sentences in `epics.md` / `architecture.md` so repository producers may proceed as `awaiting-operator` while C1 successor files are unproven. Explicitly: no Production enablement, A41 stays open, **“PRD: no change.”**

That is **not** a product-level requirement the PRD failed to absorb. NFR34 already defers Production qualification to Epic 27 C1 evidence and store choice to architecture. New 27.4 sentences (live producers, 15-minute runbook cap, `awaiting-operator`) are mechanism/sequencing. Promoting them into a new PRD FR would be the wrong extract.

No other `sprint-change-proposal-2026-09-*.md` exists under memories planning artifacts. Architecture/epic body text that advanced after 2026-09-05 on this slice is the 27.4 sequencing correction only.

## Remaining forks

These are meaning/phase forks that survive the Update. Mechanism detail the addendum assigned to architecture is **not** listed as a PRD defect.

### EventStore meanings

| Spine | Domain SoT (Case / MemoryUnit / Tenant) | Product integration (FR59–FR62) | Runtime pin |
|---|---|---|---|
| **PRD + addendum** | Current MVP consistency contract; Glossary terms; FR13 | Phase 1.5; Non-Goal for thesis MVP; Epic 9 | Architecture / Epic 28; not a product FR |
| **Architecture body** | Driver #3, Concern #9, D3, Gate 1 — aligned | Phase Compatibility: “MVP … no EventStore integration” = product integration, not domain SoT | Additional Decisions / Epic 28 |
| **Architecture fossils** | PRD Deviations still quotes the deleted “Atomic write across all three backends” as if it were current PRD | — | — |
| **Epics** | Epic 21 / Story 21.1 ratifies domain SoT | Epic 9 title still “Zero-Code Memory” and “validating the ‘zero-code’ promise” — PRD forbids that slogan | Epic 28; 2026-08-31 SDK hash exception |
| **Epics Additional Requirements** | Still “Eventual consistency + DAPR Workflow saga/compensation (D3)” — **wrong D3** vs architecture’s EventStore-SoT D3 | — | — |

Extract from the PRD: three contracts stay split. Extract from Epic 9’s title or epics D3: they collapse again.

### Isolation boundary

| Spine | Outcome | Mechanism |
|---|---|---|
| **PRD** | NFR8, FR38, FR40 — zero leaks; tenant-scoped principals **and** indexes; mechanism architecture-owned | Does not name ACL users |
| **Addendum / architecture** | Same outcomes | Per-tenant Redis ACL users + tenant-scoped resolver; prefixes/hash tags/logical DBs are placement only (Story 24.3) |
| **Epics inventory + Epic 0/5 headers** | FR38 still “physically separate indexes”; Epic 5 “physically separate indexes across all three backends” | Does not restate ACL-as-boundary |

Outcome-aligned. An extract of **epics FR38** still sells index names as the security boundary.

### Identity

| Spine | Contract |
|---|---|
| **PRD** | Tenant claims authorize; case membership is metadata; provenance = authenticated `sub` / allowlisted `system:*`; NFR11 **MVP** |
| **Architecture Security / D8** | JWT `/api/**`, `TenantAuthorizationMiddleware`, Story 20.2 claims — aligned with PRD |
| **Architecture Coverage + Gate-Blocking** | NFR11 still “Phase 1.5 fast-follow”; `TenantAuthorizationMiddleware` still “Not gate-blocking \| Phase 1.5” — **stale vs D8 and vs PRD** |
| **Epics inventory** | NFR11 still `[P1.5]` |
| **Epic 20** | Implements NFR11 as post-MVP *audit remediation* (delivery track), while reinforcing NFR11 |

Product text in the PRD is current. Scheduling labels in architecture coverage and epics inventory are not.

### Ingestion owner

| Spine | Orchestrator | NFR17 proof | State names |
|---|---|---|---|
| **PRD** | `IngestionWorkflow`; rate-limiter actor = budget only | Durable Task / workflow history | `pending`, `projecting`, `indexed`, `partially failed/retrying`, `failed`/`dead-lettered`, `repaired` |
| **Architecture drivers / D23** | Workflow — aligned | Workflow — aligned | Field inventory still `queued`, `extracting`, `embedding`, `indexing`, `indexed`, `failed` |
| **Architecture fossils** | Scale & Complexity “per-tenant pipeline actors”; Silent Failure “Pipeline Actor”; Requirements Overview NFR17 “DAPR actors” | Overview still “DAPR actors” | — |
| **Epics** | Additional Requirements D23/D24 correct | Inventory NFR17 still “DAPR actor state” | FR10 still “queued, embedding, indexed, failed” |
| **UX-DR22** | — | — | Union: pending, queued, extracting, embedding, indexing, indexed, failed, retried, re-ingested |

Owner decision is in the PRD. Vocabulary is still three-way forked.

### Fusion

| Spine | Algorithm | Numbers |
|---|---|---|
| **PRD** | Weighted RRF (NFR24); magnitude-blend rejected | Explicitly architecture-owned |
| **Architecture** | Story 22.4 RRF; Epic 26 `k=10`, weights `0.30/0.35/0.35` | Architecture-owned — correct |
| **Epics** | NFR24 matches PRD | Does not copy `k`/weights into inventory |

No product-level fusion fork remains. Do not copy calibration numbers into the PRD.

### Onboarding clocks

| Spine | Phase 1 clock | Phase 1.5 clock | Boot command |
|---|---|---|---|
| **PRD** | NFR31: README/AppHost → first CLI search on file/URL, &lt;30 min | Separate launch gate: `dotnet add package` + DAPR subscription → first event search | `dotnet run --project Hexalith.Memories.AppHost` |
| **Addendum** | Same | Same | AppHost; not a second product |
| **Journey 1 (Phase 1.5)** | — | Narrative still `docker compose up` for Redis + FalkorDB | Compose, not AppHost |
| **Architecture Gate-Blocking** | Gate 3 “Docker Compose single-command boot” | — | Compose |
| **Architecture topology** | — | — | AppHost boots all containers including Python `ai-agent` |
| **Epics NFR31** | Single “working quickstart … &lt;30 minutes” — **no second clock** | Missing | Epic 7 README path |
| **Epic 7 header** | Thesis CLI + README | `status` / `explore` / `handlers` / `quickstart` as Phase 1.5 polish | — |

Two clocks are in the PRD. Epics NFR31 and Journey 1 / Gate 3 boot path have not been re-extracted.

### Other phase/inventory forks

- **FR53 / `status`:** PRD thesis CLI includes `status` (FR10 is MVP). Architecture Interface Philosophy and Epic 7 still park `status` in Phase 1.5 polish.
- **Architecture Coverage** lists FR1–FR53 as “Active MVP” (entire FR53) and **31 NFRs**, omitting NFR32–NFR35; NFR11 still P1.5.
- **Epics inventory** still uses pre-Update FR6/FR13/FR38/FR53/FR71 wording (FR71 phase clause exists in FR Coverage Map but not in the inventory bullet).
- **`--explain` vs UX-DR7:** PRD Open Question 3 names the fork; it does not pick. Architecture still “does not settle which fields are mandatory.” Unresolved on purpose — not silent anymore.

## Findings

### high Architecture overview still extracts the pre-2026-09-05 PRD

- **Location:** `architecture.md` — Requirements Overview (NFR count 31; NFR17 “DAPR actors”; “per-tenant pipeline actors”); Technical Constraints “.NET 10 / C# 13”; Requirements Coverage (NFR11 Phase 1.5; 31/31 NFRs; FR53 wholly MVP); PRD Deviations rows that still quote “Atomic write across all three backends” and “All major [embedding] providers supported from MVP”; Gate-Blocking `TenantAuthorizationMiddleware` Phase 1.5 vs D8.
- **Trigger:** Those sections were not re-extracted after the PRD Update. Body sections (Concern #9, Security Architecture, Current Verified Versions C# 14, D3/D8/D29) already match the new PRD.
- **Consequence:** An architecture or story extract that starts at Requirements Overview / Coverage reconstitutes C# 13, 31 NFRs, P1.5 auth, actor-pipeline, and deleted PRD sentences — the exact 2026-09-05 failure mode.
- **Fix:** Re-extract those overview tables from today's PRD + addendum. Rewrite PRD Deviations to cite *historical* PRD text, or delete rows whose PRD source no longer exists. Move NFR11 and NFR32–NFR35 in Coverage. Do not edit the PRD to match the fossils.

### high Epics Requirements Inventory is a stale second PRD

- **Location:** `epics.md` — Requirements Inventory FR6, FR10, FR13, FR38, FR53, FR71; NFR6 (degradation clause dropped); NFR11 `[P1.5]`; NFR17 “DAPR actor state”; NFR31 single clock; NFR32–NFR35 absent; Additional Requirements D3 “Eventual consistency”.
- **Trigger:** Inventory was a lossless copy of the March/July PRD and was not updated when `prd.md` absorbed August change control.
- **Consequence:** Sprint/story authors who treat the inventory as the FR/NFR source-extract will schedule auth as P1.5, rebuild an actor pipeline, treat indexes as the isolation boundary, and miss NFR32–NFR35 and the dual onboarding clocks.
- **Fix:** Replace inventory bullets with pointers to the PRD, or refresh them to current PRD wording (including phase tags and NFR32–NFR35). Correct D3 to architecture’s EventStore-SoT text. Keep FR Coverage Map’s FR71 Phase 2 / Epic 26 backup split.

### medium CLI `status` and FR53 phase still disagree across spines

- **Location:** PRD MVP Feature Set + CLI Specification (`status` is a thesis essential; FR10 MVP). Architecture Interface Philosophy and Epic 7 header (`status` is Phase 1.5 polish; MVP list omits it). Architecture Coverage: all of FR53 = Active MVP.
- **Trigger:** August PRD-1/PRD-6 made FR53 phase-split and counted real commands (including status telemetry). Architecture/Epic 7 were not re-extracted.
- **Consequence:** A story extract from Epic 7 will defer the FR10 surface; an extract from architecture Coverage will pull remaining FR53 slices into thesis MVP.
- **Fix:** Pick one: either PRD drops `status` from the thesis essential list (FR10 still requires *a* status surface), or architecture/Epic 7 move `status` into MVP essentials and keep `explore`/`handlers`/`quickstart` in Phase 1.5. Coverage must say FR53 is split, not wholly MVP.

### medium Onboarding boot path and second clock are not shared

- **Location:** PRD NFR31 + Getting Started + addendum (AppHost; two clocks). Journey 1 Rising Action still `docker compose up`. Architecture Gate-Blocking Gate 3 “Docker Compose single-command boot” vs topology “`dotnet run --project Hexalith.Memories.AppHost`”. Epics NFR31: one &lt;30 min quickstart, no Phase 1.5 event clock.
- **Trigger:** The Update split clocks and named AppHost in product text; Journey 1 narrative and Gate 3 row were left as March compose-first copy. Epic 7 still treats `quickstart` as Phase 1.5 polish while NFR31 is the README/AppHost path.
- **Consequence:** UX/onboarding extracts can ship two incompatible “30-minute” gates or teach Compose as the thesis boot when AppHost (and the optional Python sidecar) is the living path.
- **Fix:** Relabel Journey 1 boot to AppHost (Compose may remain an operator alternative). Change Gate 3 to AppHost. Add the Phase 1.5 event clock to epics NFR31. Keep the two clocks only in the PRD if downstream copies the split.

### medium Ingestion state vocabulary is still three-way

- **Location:** PRD observable state machine (`pending` / `projecting` / `indexed` / …). Architecture Memory Unit Field Inventory `Status` enum (`queued` / `extracting` / `embedding` / `indexing` / …). Epics FR10 (`queued, embedding, indexed, failed`). UX-DR22 union of both plus `retried` / `re-ingested`.
- **Trigger:** PRD-4 replaced “atomic write” with a projection state machine; field inventory and UX-DR22 were not remapped.
- **Consequence:** Status CLI, Evidence Packet `state`, and operator UX can emit different enums for the same unit; FR10/FR31 extracts will not match NFR17/FR13 completion language.
- **Fix:** Architecture owns the stored enum; PRD owns observable operator states. Publish one mapping table (or collapse to one vocabulary) and refresh epics FR10/FR31 and UX-DR22 from that table. Do not invent a fourth list in the PRD.

### low Epic 9 still markets “zero-code” against the PRD glossary

- **Location:** Epic 9 header and story intro: “Zero-Code Memory”, “validating the ‘zero-code’ promise.” PRD Executive Summary / Innovation / FR59: conventions + subscription; schema evolution needs handler registration; not “zero configuration.”
- **Trigger:** Epic title predates the Update’s anti-slogan.
- **Consequence:** A Phase 1.5 story extract from the epic header reintroduces the claim the PRD killed.
- **Fix:** Rename/reword Epic 9 to “EventStore product integration” (or “conventions + subscription”). Keep FR59–FR62 scope.

### low `--explain` vs UX-DR7 remains an open product pick

- **Location:** PRD FR19 + interface matrix (`--explain` opt-in) vs Open Question 3 vs UX-DR7 (every search starts the full trust loop). Architecture Evidence Packet “does not settle which fields are mandatory.”
- **Trigger:** The Update named the fork; it did not decide it.
- **Consequence:** Epic 17 / CLI JSON extracts will guess whether compact trust fields are mandatory. This is no longer silent, but it is still unresolved.
- **Fix:** Answer Open Question 3 in the PRD (compact trust on every search + `--explain` expands math, **or** UX-DR7 is opt-in). Architecture then marks mandatory Evidence Packet fields. Do not leave Epic 17 to reconcile FR19 with UX-DR7.

### low NFR11 is a current product invariant scheduled as post-MVP remediation

- **Location:** PRD NFR11 **MVP**. Epic 20 lifecycle “Operational Readiness / Security Hardening” (post-MVP audit track) while reinforcing NFR11. Architecture Coverage still P1.5.
- **Trigger:** Delivery taxonomy (Epic 20 as 2026-07-04 remediation) was never reconciled with the August “NFR11 is current” product rule.
- **Consequence:** Readiness accounting can treat authentication as non-MVP while the PRD forbids unauthenticated product ingress. Not a missing PRD sentence — a spine classification fork.
- **Fix:** Keep NFR11 as current in the PRD. Tag Epic 20 as “implements a current MVP invariant; track is remediation, not phase deferral.” Move architecture Coverage NFR11 to Active MVP.

### low Post-Update 27.4 live-producer contract must stay out of the PRD

- **Location:** `sprint-change-proposal-2026-09-06-story-27-4-live-producer-contract.md`; `architecture.md` Security Architecture ownership paragraph; `epics.md` Epic 27 sequencing gate (2026-09-06 correction). PRD NFR34.
- **Trigger:** Approved Moderate SCP after the Update; PRD correctly unchanged.
- **Consequence:** A zealous “sync the PRD” pass could promote `awaiting-operator` / live-producer / 15-minute runbook rules into product NFRs the SCP forbade.
- **Fix:** Leave NFR34 as the product outcome. Architecture/epics own producer sequencing. Next PRD extract should cite the SCP only as deferred mechanism.

## Inventory check (IDs vs meaning)

| Spine | FR IDs | NFR IDs | Phase tags |
|---|---|---|---|
| PRD (2026-09-05) | FR1–FR74; phase register present | NFR1–NFR35; NFR33 = freshness, NFR35 = web perf | FRs tagged where phase-split; NFR table tagged; NFR11 MVP |
| Architecture Requirements Overview / Coverage | Count 74; Coverage still treats FR53 as wholly MVP | Count **31**; NFR32–NFR35 absent; NFR11 still P1.5 | Coverage phase-filters pre-Update |
| Epics Requirements Inventory | FR1–FR74; several bullets pre-Update wording | NFR1–NFR31; NFR6 shortened; NFR32–NFR35 absent; NFR11 P1.5 | Tags copied from old PRD; D3 bullet stale |

No missing or extra FR *numbers* between PRD and epics. New NFR numbers exist only in the PRD. Remaining drift is stale downstream copies, phase/boot/`status` forks, and one named open question — not unapplied August PRD amendments.

## Brownfield harm

The PRD no longer licenses a 22–32 story greenfield restart or an isolation escape. Classification, Resource Requirements, and Non-Goals now point at `epics.md` / `sprint-status.yaml`. Leaving **architecture Coverage** and **epics inventory** on the old contract *does* still harm extracts: those sections are what most story workflows copy first. Refresh them from the PRD; do not roll the PRD back to match them.
