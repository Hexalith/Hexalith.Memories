# Sprint Change Proposal — Implementation-Readiness Rerun Disposition

**Date:** 2026-08-03  
**Mode:** Batch  
**Status:** Approved — Major-change implementation-planning handoff active  
**Trigger:** `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-03-rerun.md`  
**Change classification:** Major coordinated direct adjustment  
**Current implementation boundary:** **NOT READY for unrestricted full-backlog implementation**

Administrator approved this proposal on 2026-08-04. It is a delta over the already approved
2026-08-03 planning corrections. It does not
replace their one-gate C1 map, invent a parallel Epic 27 plan, reopen completed delivery, or treat
proposal text as implementation evidence. Where this proposal conflicts with earlier text, it
supersedes only the Story 27.3 C2 ownership and rerun-disposition clauses stated below.

## 1. Issue Summary

The readiness rerun confirms a strong planning foundation: all four canonical planning documents
exist, every FR1-FR74 maps exactly once, and the PRD, architecture, UX, and core epics are
substantially aligned. It identifies 24 current findings: six PRD clarity risks, eight UX
alignment/scope cautions, and ten epic-quality/readiness findings.

The decisive blocker is Epic 27. Story 27.4 requires all C1.1-C1.25 gates to pass against one
immutable profile, but only Story 27.21/C1.15 is registered. The remaining 24 C1 definitions are
held without current registered owners. Unrestricted numeric backlog execution therefore remains
unsafe, although an individually selected story may proceed after its own prerequisites,
activation gate, owner, and evidence producer are present.

The rerun also exposes a second active Epic 27 quality problem: Story 27.3 still owns three
independently reviewable qualification lanes. C0, C3, and C4 are complete, while C2 remains blocked
on a fresh `production-deployment-verification` run. Keeping that open external qualification
inside Story 27.3 preserves the broad shape that the story-scope guard is intended to retire.

### 1.1 Source-backed verification ledger

Verified on 2026-08-03 against the current worktree. Proposed future requirements are intent, not
claims that the implementation already satisfies them.

| Exact claim | Class | Rerunnable command / artifact | Observation | Verdict |
| :---------- | :---- | :---------------------------- | :---------- | :------ |
| “All 74 FRs are mapped exactly once.” | Quantitative | `sed -n '302,397p' _bmad-output/planning-artifacts/implementation-readiness-report-2026-08-03-rerun.md` | The rerun records FR1-FR74 once each, zero missing, zero duplicate, and 100% coverage. | `confirmed` |
| “The rerun identified 24 findings.” | Quantitative | `rg -n 'identified \*\*24 findings' _bmad-output/planning-artifacts/implementation-readiness-report-2026-08-03-rerun.md` | The final note records 6 PRD, 8 UX, and 10 epic findings. | `confirmed` |
| “Only Story 27.21 is a registered C1 successor.” | Existence | `rg -n '^### Story 27\.(7|8|9|1[0-9]|2[0-9]|3[01]):' _bmad-output/planning-artifacts/epics.md` | The only match is Story 27.21. | `confirmed` |
| “The approved C1 map contains 25 one-to-one rows, Stories 27.7-27.31.” | Quantitative/location | `awk 'NR>=240 && NR<=270 && /^\| 27\./ {print}' _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md` | Twenty-five rows map C1.1-C1.25 without a gap or duplicate. | `confirmed` |
| “Story 27.3 C0 remains reopened.” | Behavioral | `rg -n 'Independent C0 re-acceptance|Task 0 and C0 are re-closed' _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md` | The implementation record shows independent reviewer-owned re-acceptance on 2026-08-01. The stale reopened statement in `epics.md` must be corrected. | `corrected` |
| “Story 27.3 C2 is blocked while C3 and C4 are complete.” | Behavioral | `sed -n '800,825p' _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md` | The current checkpoint table records C2 blocked/not complete and C3/C4 reviewed and complete. | `confirmed` |
| “Story identifier 27.35 is unused.” | Existence | `rg -n '27[.-]35([^0-9]|$)' _bmad-output/planning-artifacts/epics.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/*.md` | No match. | `confirmed` |
| “The repository baseline is .NET 10 / C# 14.” | Existence/location | `sed -n '1,12p' global.json && rg -n 'TargetFramework|LangVersion' Directory.Build.props` | SDK `10.0.302`, `net10.0`, and `LangVersion` 14 are explicit. | `confirmed` |
| “The release inventory contains nine package IDs.” | Quantitative | `rg -c '"packageId":' tools/release-packages.json` | Nine package IDs are present. | `confirmed` |
| “External Server ingress authentication is active.” | Behavioral/location | `rg -n -F -e 'AddAuthentication' -e 'RequireAuthenticatedUser' -e 'UseAuthentication' -e 'UseAuthorization' src/Hexalith.Memories.Server` | JWT authentication, a fallback authenticated-user policy, and middleware are registered. | `confirmed` |
| “The PRD currently ends at NFR31.” | Existence | `rg -n '\bNFR3[12]\b' _bmad-output/planning-artifacts/prd.md` | NFR31 is present and NFR32 is absent. | `confirmed` |
| “Story 28.1 lacks an Acceptance Criteria heading.” | Existence | `awk 'NR>=5011 && NR<5076 && /^\*\*Acceptance Criteria/ {print}' _bmad-output/planning-artifacts/epics.md` | No output. | `confirmed` |
| “Epic 17's execution override omits Story 17.1 and orders 17.6 before the component stories it audits.” | Behavioral/location | `sed -n '130,145p' _bmad-output/implementation-artifacts/sprint-status.yaml && sed -n '3615,3670p' _bmad-output/planning-artifacts/epics.md` | The override starts at 17.6, omits 17.1, and Story 17.6 explicitly audits the Story 17.1 RCL and Stories 17.2-17.5. | `confirmed` |

## 2. Impact Analysis

### 2.1 Epic impact

| Epic or area | Impact |
| :----------- | :----- |
| Epic 17 | Correct the circular/partial execution override. Preserve completed story keys and history. |
| Epics 14, 15, 19, and 25 | Preserve completed history but mark each as a non-reusable enabling/governance archive. Reopened work must be attached to an actor-visible outcome or retained in the deferred ledger. |
| Epics 11, 28, and 30 | Reframe active/future descriptions around contributor, consumer, and release-operator outcomes. Do not reopen completed work merely to change prose. |
| Epic 27 | Keep the approved C1.1-C1.25 map. Register the remaining 24 successors only through their one-gate transactions. Correct C0. Transfer open C2/AC6 from oversized Story 27.3 to proposed Story 27.35. Preserve C3/C4 evidence in Story 27.3. |
| Epic 28 | Add the missing Acceptance Criteria heading without changing its seven BDD scenarios. |
| Epics 30 and 31 | Keep external activation and independent-countersignature blockers visible and fail closed. |

No new product epic is required. No completed story is reopened solely to rewrite history.

### 2.2 Story impact and selection boundary

- Stories 27.7-27.31 retain the already approved one-gate mapping. Story 27.21 remains the only
  registered successor until another gate's literal producer, focused fixture, story file,
  verification ledger, scope record, and registration transaction all pass.
- Story 27.3 retains C0/C3/C4 and its append-only historical record. C2/AC6 transfers to proposed
  Story 27.35. Story 27.3 is not automatically marked `done`; its own remaining review and ledger
  obligations must be reconciled after the transfer.
- Story 27.35 is proposed, not registered by this document. Registration requires its dedicated
  story file and all creation guards.
- Story 27.4 remains blocked. The previously approved 27.32-27.34 close-out split remains a
  separate authorized change and is not remapped here.
- Oversized completed stories remain historical anti-templates. Any reopened behavior gets a new
  one-outcome story with failure, compatibility, rollback, and tenant-negative scenarios as
  applicable.
- Stories 30.3/30.4 and Epic 31 closure remain excluded from ready queues until their recorded
  external evidence or independent authority exists.

### 2.3 Artifact conflicts

| Artifact | Current conflict | Required reconciliation |
| :------- | :--------------- | :---------------------- |
| `prd.md` | C# version, minimum-scope fallback, package wording, backend access boundary, auth phase, and broad FR phase semantics are ambiguous or stale. | Apply PRD-1 through PRD-6 below. |
| `architecture.md` | Early C# 13 and Phase 1.5 authentication text conflict with verified current facts; future web and physical-isolation states need fail-closed ownership. | Apply ARCH-1 through ARCH-4. |
| `ux-design-specification.md` | Accessibility/responsive and freshness semantics lack PRD anchors; automatic synthesis/graph wording and first-class-surface language can over-pull future scope. | Apply UX-1 through UX-4. |
| `epics.md` | C0 is stale; Story 27.3 remains broad; only one C1 successor is registered; Story 28.1 lacks its heading; several historical epics remain reusable-looking. | Apply EPIC-1 through EPIC-6. |
| `sprint-status.yaml` | Epic 17 order is circular/partial; Epic 27 lacks the proposed C2 successor; external blockers must remain visible. | Update only after approved story-registration transactions pass. |
| Readiness rerun | C0 is stale against the implementation record; it correctly reports the 24 unregistered C1 gates. | Preserve the rerun as historical evidence and add a dated correction note or superseding rerun after canonical edits. |

### 2.4 Technical and operational impact

The canonical planning edits change no runtime, cluster, package, dependency, tenant data, secret,
or Production lifecycle state. Follow-up story execution affects CI/deployment evidence and the
existing access-telemetry qualification path. Production lifecycle writes remain disabled and A41
remains open until the exact same-profile evidence and approvals pass.

Any future tenant/case routing, isolation, identity, or evidence-scope change must attach focused
cross-tenant denial or fail-closed evidence to its story and completion/review record.

## 3. Recommended Approach

Use a **coordinated Direct Adjustment**, classified as **Major** because it reconciles PRD,
architecture, UX, epics, and sprint tracking and requires Product Manager, Architect, Product
Owner, UX, Security, Platform Operations, Release, and Developer participation.

This is not a fundamental product replan. The thesis, MVP gate, 74-FR coverage, and completed
implementation remain intact. The planning-only reconciliation is medium effort and low-to-medium
technical risk. The Epic 27 running-profile producers and approvals are high evidence effort,
environment-dependent, and likely multi-sprint. No honest fixed completion date can be inferred
until the remaining producer-specific prerequisites and external review availability are known.

### 3.1 Alternatives evaluated

| Option | Verdict | Reason |
| :----- | :------ | :----- |
| Direct adjustment | Selected | Corrects active ownership and artifact drift while preserving product scope and completed evidence. |
| Roll back completed Epic 27 or related work | Rejected | It discards valid C0/C3/C4 evidence and still does not create C1 owners or a qualifying C2 run. |
| Reduce or redefine MVP | Rejected | The blocker is operational-readiness governance, not a failure of the product thesis or FR coverage. |
| Bulk-register 24 placeholder stories without producers | Rejected | It violates the approved registration transaction and would create a backlog that is formally owned but not executable. |
| Keep C2 inside Story 27.3 | Rejected | It leaves the only open qualification outcome inside an acknowledged broad active story. |

## 4. Detailed Change Proposals

### 4.1 PRD changes

#### PRD-1 — select the current language baseline

**Section:** Developer Tool / API Backend Specific Requirements — implementation matrix

**OLD:**

> Server runtime | .NET 10 / C# 13

**NEW:**

> Server runtime | .NET 10 / C# 14. The current build identity is SDK 10.0.302 with
> `rollForward=latestFeature`; repository configuration remains authoritative when the SDK moves.

#### PRD-2 — remove the contradictory minimum-scope escape

**Section:** Risk Mitigation Strategy

**OLD:**

> Absolute minimum if resources tighten further: Engine + Search + CLI ... Cases and tenant
> isolation deferred to fast-follow.

**NEW:**

> Resource pressure may defer phase-qualified interfaces and diagnostics, but it may not defer
> tenant isolation, tenant/case validation, or the zero-leakage release gate without an approved
> MVP rebaseline. Engine, scoped search, minimum case bootstrap, tenant provisioning, and their
> fail-closed guards remain inseparable MVP foundations.

#### PRD-3 — make package and host counts unambiguous

**Section:** Project/package inventory

**OLD:**

> 9 published NuGet packages + 3 non-packable service/orchestration projects.

**NEW:**

> `tools/release-packages.json` is the sole release inventory and currently contains nine package
> IDs. A separate non-packable-host table names every service/orchestration project and explicitly
> states that those rows are not part of the nine-package count.

The canonical edit must enumerate the current non-packable rows rather than retain an unexplained
aggregate of three.

#### PRD-4 — separate DAPR state access from direct backend access

**Section:** Service communication

**OLD:**

> Internal (Server ↔ Redis/FalkorDB) | DAPR state / direct connection via DAPR sidecar

**NEW:**

> DAPR state access uses the sidecar state API. Direct Redis/FalkorDB access uses only approved
> infrastructure-boundary clients with Aspire-injected keyed connections; it does not traverse the
> DAPR state API or treat the sidecar as a generic connection proxy. Product projects do not
> construct infrastructure endpoints or clients.

#### PRD-5 — align the external-auth phase with current MVP reality

**Section:** NFR11 and interface security

**OLD:**

> NFR11 ... no unauthenticated access to REST API endpoints ... P1.5

**NEW:**

> NFR11 — External product REST access is authenticated for the active MVP HTTP surface. Health
> probes and required DAPR infrastructure routes are the only deliberate anonymous exceptions and
> are named and tested. Additional identity-provider hardening may remain operational-readiness
> work; unauthenticated product ingress is not a Phase 1.5 allowance.

#### PRD-6 — bind broad FRs to the phase register

**Section:** Functional Requirements preamble and interface capability matrix

**OLD:**

> The FR list is broad-horizon, while phase qualifiers appear mainly in prose and the interface
> matrix.

**NEW:**

> FR1-FR74 are the product-horizon inventory, not a claim that every FR is active MVP scope. Add a
> canonical phase register referenced by each phase-qualified FR. At minimum, preserve FR23,
> FR54, FR58-FR62 as Phase 1.5 and FR71 as Phase 2 unless a later approved change advances them.

Add two future-web requirements before Epic 17 is selected:

- **NFR32 — Web accessibility and responsive trust:** WCAG 2.2 AA, keyboard/focus behavior,
  non-color status semantics, reduced motion, forced colors, zoom/reflow, and responsive access to
  trust fundamentals, verified through the Epic 17 browser/assistive-technology evidence matrix.
- **NFR33 — Evidence freshness semantics:** authoritative `current`, `aging`, `stale`, and
  `unknown` thresholds, transitions, disclosure, and recovery actions, versioned in the Evidence
  Packet contract and activated per delivery surface.

### 4.2 Architecture changes

#### ARCH-1 — reconcile current facts

Replace the early `.NET 10 / C# 13` constraint with `.NET 10 / C# 14`. Move point-in-time package,
SDK, DAPR, Aspire, and submodule identities into a dated verified-facts table so an old validation
paragraph cannot override current repository configuration.

Move NFR11 from Phase 1.5 authentication intent to the implemented authenticated-ingress boundary,
while retaining explicit anonymous health/DAPR infrastructure exceptions.

#### ARCH-2 — bind physical-isolation assurance to runtime state

**OLD:**

> Per-tenant Redis ACL users plus tenant-scoped backend resolution are the target; full enforcement
> remains follow-up work.

**NEW:**

> Physical isolation has explicit states: `target`, `configured`, `verified`, `degraded`, and
> `unknown`. Only enforcement, migration/cutover, and attached deployment-shaped cross-tenant
> negative evidence for the same profile may produce `verified`. Story 24.3 remains decision and
> verifier history; the separately approved qualification, enforcement, migration, and evidence
> stories own the remaining state transitions.

#### ARCH-3 — make future-web contracts phase-scoped

Map the FrontComposer/Fluent UI web component boundary to NFR32 and Evidence Packet freshness to
NFR33. State that synthesis and graph context are capability- and query-sensitive; ranked results
or summaries remain valid when synthesis or graph traversal is not selected or available.

#### ARCH-4 — preserve external activation gates

Keep Epic 30 publication/recovery stories behind their immutable Hexalith.Builds qualification
evidence and keep Epic 31 closure behind genuinely independent countersignatures. A present
workflow, package, or configuration key is not completion evidence.

### 4.3 UX changes

#### UX-1 — establish PRD traceability

Map accessibility, focus, responsive, forced-color, reduced-motion, and touch-target requirements
to NFR32. Map freshness states, transitions, disclosure, and recovery to NFR33. Retain the detailed
UX requirements as the normative acceptance source.

#### UX-2 — make automatic composition capability-sensitive

**OLD:**

> Memories should automatically perform source lookup, evidence strength scoring, explain
> breakdown, and relevant graph traversal.

**NEW:**

> Memories automatically performs the operations required by the selected capability and query.
> Source attribution and explain semantics remain mandatory where supported; synthesis and graph
> traversal run only when selected, available, and phase-authorized. The Evidence Packet states
> omitted axes or synthesis and gives a recovery/expansion action.

#### UX-3 — phase-gate permissions, export, and surfaces

Clarify that case membership is metadata until an approved authorization model promotes it to an
access-control source. Rename component roadmap labels `Phase 1/2/3` to `Web UX Wave 1/2/3`, each
with its product phase and activation story. Portable case/tenant export remains FR71 Phase 2;
diagnostic packet export does not imply FR71 completion.

#### UX-4 — preserve semantic equivalence without simultaneous delivery

**OLD:**

> CLI, MCP, and web UI are all first-class surfaces ... the same query ... exposes equivalent
> evidence fields.

**NEW:**

> Each registered surface is first-class when active. Evidence Packet semantics are equivalent per
> capability and phase, while CLI, MCP, and web may ship at different times and present different
> densities. No inactive surface becomes an MVP acceptance obligation merely through UX guidance.

### 4.4 Epic and story changes

#### EPIC-1 — correct Story 27.3 current authority

**Story:** 27.3  
**Section:** Scope, title/display name, acceptance criteria, and current checkpoint table

**OLD:**

> Story 27.3 owns C0 and independent C2/C3/C4; binding acceptance is AC6-AC8.

**NEW:**

> Story 27.3 retains its historical key and append-only evidence but its current executable scope
> is C0, C3, and C4 only. C0 is complete after independent re-acceptance; C3/AC7 and C4/AC8 are
> complete on their recorded evidence. C2/AC6 transfers intact to Story 27.35. Story 27.3 advances
> no C1 gate, enables no Production lifecycle write, and is marked `done` only after its remaining
> story-local review/ledger obligations reconcile against this narrowed authority.

The display title becomes **Production Adapter Manifest and Unit Qualification**; the existing
story key/file remains stable for history.

#### EPIC-2 — create one C2 successor

**Proposed Story 27.35: Disposable Production Deployment Qualification**

As a Platform Operations and release-review pair,  
I want the checked-in four-archive deployment-verification lane to pass on immutable current-source
evidence,  
So that C2 is independently reviewable without keeping Story 27.3 broad.

**Acceptance Criteria:**

1. **Given** one reviewed source SHA and the checked-in archive producer, **when** the
   `production-deployment-verification` CI job runs, **then** it builds all four release archives
   from that SHA, renders and applies the production manifests to a disposable `kind` context, and
   fails on any missing archive, render/apply failure, wrong context, or empty Component inventory.
2. **Given** the applied Component set, **when** vault-typed stores require disposable-cluster
   substitution, **then** the lane dynamically enumerates every matching Component, validates
   consumer secret prerequisites, records actual post-patch readback including failed/absent
   observations, and discloses the admission window. A successfully enumerated non-empty set with
   zero vault-typed Components is accepted as the Story 31.2 end state; zero total Components or a
   failed enumeration is not.
3. **Given** the substituted disposable deployment, **when** required health and controlled fault
   stages run, **then** every required stage produces bounded transcripts and the expected aggregate
   status; no skipped or malformed stage is treated as success. The result explicitly does not
   claim to exercise the Production OpenBao secret-resolution path when substitution occurred.
4. **Given** the uploaded `production-deployment-evidence` artifact, **when** an independent reviewer
   retrieves it through `gh api`, **then** the current validator accepts the exact packet schema,
   all four archives and all required evidence files are present, every required job step succeeded,
   and the reviewer records the run ID, commit SHA, artifact ID/hash, verdict, and completion date.

**Historical Context Classification:**

| Prior influence | Classification | Permitted use |
| :-------------- | :------------- | :------------ |
| Story 27.3 as a whole | `anti-template` | Scope-transfer provenance only; do not copy its task count, mixed C0/C2/C3/C4 shape, file list, or ledger density. |
| Story 27.3 AC6/C2 text | `historical-reference-only` | Preserve exact current qualification and disclosure semantics until the new story verifies and adopts them. |
| Current CI job and `publish`/`verify`/`validate` tools | `current-narrow-pattern` | Reverify and reuse only the single end-to-end C2 producer/evidence path. |

**Slice Proof:** one outcome (C2 disposable-deployment qualification); accountable owners
(CI owner and Platform Operations); independent release/security reviewer; exact producer
(`production-deployment-verification` job); exact artifact (`production-deployment-evidence`);
current review state `blocked`; current completion state `not complete`.

**Creation-time Epic AC Verification:**

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| “The `production-deployment-verification` job exists and owns archive publication, rollout verification, evidence validation, and upload.” | Existence/location | `sed -n '438,505p' .github/workflows/ci.yml` | The job and all four named stages are present. | `confirmed` |
| “C2 is blocked pending a fresh qualifying current-source run.” | Behavioral/location | `sed -n '805,812p' _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md` | C2 is explicitly blocked/not complete with owner, consequence, and reopen trigger. | `confirmed` |
| “Story 27.35 is unused.” | Existence | `rg -n '27[.-]35([^0-9]|$)' _bmad-output/planning-artifacts/epics.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/*.md` | No match. | `confirmed` |

Before registration, create the dedicated story file, run the focused tooling fixture, run
`python3 tools/check-story-slice-scope.py --require-record --story-key
27-35-disposable-production-deployment-qualification`, reconcile source-artifact corrections, and
add the sprint row as `backlog`. Registration does not complete C2.

#### EPIC-3 — retain the approved C1 map and transaction gate

Do not rename or bulk-register Stories 27.7-27.31. For each of the remaining 24 gates, preserve
the approved one-gate map and create/register the story only when its literal producer mode,
focused fixture, complete Epic AC Verification table, Historical Context Classification, Slice
Proof, exact evidence command, owner, reviewer, and incomplete checkpoint are all present. Story
27.21 remains the current narrow pattern for the registration transaction, not a template for a
different gate's evidence semantics.

#### EPIC-4 — preserve history without reusing anti-templates

Add a non-reusable archive note to Epics 14, 15, 19, and 25 and to the oversized stories named by
the rerun. The note must not change a completed status or pretend deferred work was delivered.
Future reopened behavior receives a fresh vertical story attached to an actor-visible outcome.

#### EPIC-5 — repair active tracking defects

- Add `**Acceptance Criteria:**` immediately before Story 28.1's existing seven BDD scenarios.
- Remove Epic 17's circular override or replace it with the actual historical dependency order
  `17.1 -> 17.2 -> 17.3 -> 17.4 -> 17.5 -> 17.6 -> 17.7`. Preserve completed statuses.
- Reframe Epic 30 around the release maintainer's complete, identity-bound, recoverable four-image
  outcome while keeping its external qualification gates.
- Keep Epic 31's independent C4b/C5b/C7 countersignature blocker explicit; do not invent an owner.

#### EPIC-6 — keep anchors advisory

Retain point-in-time line anchors only as historical citations. Every authoring/execution route
must rederive the file, symbol, and current location before using an anchor. The existing Epic AC
Verification policy remains the gate; no new duplicate policy is required.

### 4.5 Sprint-status changes after approval and creation gates

1. Correct Epic 17's execution order without changing completed story statuses.
2. Add Story 27.35 as `backlog` only in the same transaction that creates its compliant story file
   and passes all creation guards.
3. Place 27.35 after 27.3 and before the running-target C1 execution sequence.
4. Keep Story 27.4 blocked and keep the previously approved 27.32-27.34 close-out split separate.
5. Register each C1 successor independently; never pre-register the 24 held definitions in bulk.
6. Keep 30.3/30.4 and Epic 31 closure activation blockers visible.
7. Set `last_updated` only when the canonical mutation actually occurs.

## 5. Implementation Handoff

### 5.1 Ownership

| Recipient | Responsibility |
| :-------- | :------------- |
| Product Manager | Approve the PRD phase, minimum-scope, auth, and NFR32/NFR33 changes; confirm no MVP reduction. |
| Solution Architect | Approve language/auth/backend-boundary corrections, physical-isolation states, future-web contract mapping, and Story 27.3 C2 transfer. |
| Product Owner | Apply epic/archive/ordering edits and register stories only after their creation transactions pass. |
| UX Designer | Apply NFR traceability, capability-sensitive composition, phase-gated surfaces, and freshness semantics. |
| CI owner / Platform Operations | Own Story 27.35 and the remaining operational C1 producers; keep Production writes fail closed. |
| Security / independent reviewer | Review isolation-sensitive evidence and the hash-bound approval gates; never self-approve producer output. |
| Developer | Implement only a selected bounded story whose prerequisites and producer exist; attach focused tenant-negative evidence when applicable. |
| Readiness assessor | Rerun readiness after canonical reconciliation; report remaining external gates independently. |

### 5.2 Ordered implementation plan

1. Obtain explicit Administrator approval of this complete delta proposal.
2. Reconcile PRD, architecture, and UX decisions through Product Manager/Architect/UX review.
3. Apply safe planning corrections: C# baseline, C0 status, Story 28.1 heading, Epic 17 order,
   phase/assurance wording, and archive labels.
4. Author Story 27.35, verify every inherited claim, run its focused creation guards, then register
   it as `backlog` and transfer C2/AC6 atomically from Story 27.3.
5. Continue the previously approved one-gate C1 registration transactions independently.
6. Rerun implementation readiness. Keep the result restricted until all remaining ownership,
   activation, and external-authority blockers are truthfully represented.

### 5.3 Success criteria

The correction is complete when:

1. The PRD carries one coherent C# 14, MVP foundation, package inventory, backend-boundary,
   authenticated-ingress, and phase-register account.
2. NFR32/NFR33 bind future web accessibility/responsive and freshness semantics without pulling
   web, export, synthesis, or mandatory graph composition into MVP.
3. Architecture and UX expose physical-isolation and freshness assurance without unsupported
   `verified` claims.
4. `epics.md` and the Story 27.3 record agree that C0/C3/C4 are complete and C2 belongs only to
   Story 27.35.
5. Story 27.35 has one outcome, one checkpoint, accountable owners, an independent reviewer,
   current verification rows, an exact CI/artifact contract, and a passing slice-scope gate before
   registration.
6. The C1.1-C1.25 map remains one-to-one; every registered successor has a real producer and
   focused fixture; missing gates remain explicitly held rather than fake-ready.
7. Story 27.4, Production lifecycle writes, and A41 remain blocked until same-profile evidence and
   approvals actually pass.
8. Story 28.1 has its heading, Epic 17 has a coherent complete order, and historical broad stories
   are visibly non-reusable.
9. External Epic 30 and Epic 31 gates remain excluded from ready queues until their evidence or
   independent authority exists.
10. Focused planning/tooling validation and `git diff --check` pass for the approved mutation.

## 6. Change Navigation Checklist Result

| Checklist area | Status | Result |
| :------------- | :----- | :----- |
| 1.1 Triggering story | [x] | Epic 27's Story 27.4 predecessor graph and active Story 27.3 qualification shape triggered the change. |
| 1.2 Core problem | [x] | Planning/dependency incompleteness and artifact drift; no thesis failure. |
| 1.3 Evidence | [x] | Rerun, canonical planning text, sprint state, Story 27.3 checkpoint ledger, and current configuration/source checks recorded. |
| 2.1 Current epic | [x] | Epic 27 remains viable after C2 transfer and gated C1 registration. |
| 2.2 Epic-level changes | [x] | Modify Epic 27; no new epic required; preserve completed history. |
| 2.3 Remaining epics | [x] | Epics 17, 28, 30, and 31 tracking/activation impacts identified. |
| 2.4 Obsolete/new epics | [N/A] | No epic is removed and no product epic is added. |
| 2.5 Order/priority | [x] | C2 becomes 27.35 before C1 execution; external gates stay out of ready queues. |
| 3.1 PRD | [x] | Six exact clarity corrections and two future-web NFRs proposed; MVP unchanged. |
| 3.2 Architecture | [x] | Language, auth, backend boundary, isolation state, future-web mapping, and external gates addressed. |
| 3.3 UX | [x] | Traceability, freshness, capability sensitivity, permissions/export, and phase-scoped equivalence addressed. |
| 3.4 Other artifacts | [x] | Epics, sprint status, story records, readiness history, CI evidence, and deferred A41 effects identified. |
| 4.1 Direct adjustment | [x] viable | Medium planning effort; high evidence effort; controlled risk. |
| 4.2 Rollback | [x] not viable | Discards valid evidence and does not resolve ownership. |
| 4.3 MVP review | [x] not viable | No MVP scope or goal change is required. |
| 4.4 Recommended path | [x] | Major coordinated Direct Adjustment. |
| 5.1-5.5 Proposal components | [x] | Issue, impact, rationale, old/new edits, MVP statement, owners, sequence, and success criteria are present. |
| 6.1 Checklist review | [x] | All applicable items are addressed; pending approval/status actions are explicit. |
| 6.2 Proposal accuracy | [x] | Verifiable claims carry commands and verdicts; future intent is not presented as current fact. |
| 6.3 Explicit approval | [x] | Administrator replied `approve` on 2026-08-04. |
| 6.4 Sprint-status update | [!] | Approved but intentionally deferred until each affected story file and creation gate pass; approval alone cannot register Story 27.35 or any held C1 successor. |
| 6.5 Final handoff | [x] | Major change routed to Product Manager and Solution Architect, with the cross-functional responsibilities in Section 5. |

## 7. Approval State and Workflow Log

**Decision:** Approved  
**Approved by:** Administrator  
**Approval date:** 2026-08-04  
**Final scope:** Major coordinated Direct Adjustment  
**Routed to:** Product Manager and Solution Architect, with Product Owner, UX, Security, CI,
Platform Operations, Release, Developer, and readiness-assessor responsibilities defined in
Section 5  
**Implementation status:** Pending

Approval authorizes the bounded planning reconciliation and story-authoring work in this proposal.
It does not itself mutate a canonical artifact, register Story 27.35 or a C1 successor, assign an
independent reviewer, change a sprint state, pass an evidence gate, enable Production lifecycle
writes, close A41, change a dependency, commit, or push. Every creation, verification, approval,
and selection gate remains binding.

- 2026-08-03: Administrator supplied the readiness-rerun result.
- 2026-08-03: Administrator selected Batch mode.
- 2026-08-03: Required planning artifacts, source-backed corrections, historical-slice guard, and
  Epic AC verification policy were assessed.
- 2026-08-03: Batched rerun-disposition proposal drafted for Continue/Edit review.
- 2026-08-04: Administrator selected Continue after reviewing the complete batch proposal.
- 2026-08-04: Administrator explicitly approved the Sprint Change Proposal.
- 2026-08-04: Major change routed to Product Manager and Solution Architect for canonical artifact
  reconciliation and cross-functional implementation-planning handoff.
