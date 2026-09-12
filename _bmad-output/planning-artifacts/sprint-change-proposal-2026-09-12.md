# Sprint Change Proposal — 2026-09-12 Sprint-Readiness Recovery

**Date:** 2026-09-12  
**Mode:** Batch  
**Status:** Draft — approval and the human decisions in section 3 are required before downstream planning changes  
**Trigger:** The 2026-09-12 sprint-readiness review remained no-go: the final architecture spine still leaves AD-14 unphased and stops at FR74/NFR36/G5, while G1 and G6 prerequisites have neither complete evidence nor registered successor ownership.  
**Recommended path:** Hybrid planning correction — phase-qualify the architecture first, then replace stale epic derivation with narrow successor stories and finally rebuild sprint tracking.  
**Change size:** Major and cross-cutting. This proposal changes planning authority and future work breakdown; it does not authorize implementation.

## Non-negotiable boundaries

- Do not modify `_bmad-output/implementation-artifacts/sprint-status.yaml` in this correction.
- Do not create implementation story files, start implementation, update dependencies, stage, commit, push, or alter submodule pointers.
- Preserve completed Epics 0–31, their story artifacts, test evidence, retrospectives, aliases, and historical status records. A successor may cite them as evidence but may not reopen or rewrite them as though their original acceptance criteria were current.
- Preserve `architecture.md` and `ux-design-specification.md` as historical artifacts. Remove them only from active derivation/source lists; do not delete them.
- Keep the release posture **no-go** until G1–G6 pass. Tracker completion is not release qualification.
- Apply the Historical Slice Scope Guard and Epic AC Verification policy before any story is authored or registered at any status. Proposed keys in this document are unregistered reservations, not sprint state.

## 1. Authoritative baseline

The correction derives from this ordered current set:

1. `_bmad-output/planning-artifacts/prd.md`
2. `_bmad-output/planning-artifacts/addendum.md`
3. `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md`, after the architecture handoff applies section 5
4. `_bmad-output/planning-artifacts/ux-designs/ux-memories-2026-09-12/DESIGN.md`
5. `_bmad-output/planning-artifacts/ux-designs/ux-memories-2026-09-12/EXPERIENCE.md`
6. Current source, tests, configuration, and approved sprint-change proposals

Pinned intake hashes:

| Artifact | SHA-256 |
| :------- | :------ |
| `prd.md` | `12579f3a22228348948e805968ea3835732e7ebbcb837e3beef75bd58fc115f1` |
| `addendum.md` | `009f7684b40e6a873056eeb3c35bca8e22112170019c24e75f9124d934ac1e01` |
| Final architecture spine | `e37070e372884925087b869bf8c96741849046b5d474f344dee8f32db77c918e` |
| `DESIGN.md` | `ddaaa7bccf178dca16d1fccc7059a1f251f26ace57a6e89b280bf0c2424922fb` |
| `EXPERIENCE.md` | `76a38dc4348fe1204520d167604278b5406a358f3adb37d1f4a7a28dabbb63af` |
| Current `epics.md` | `46e07efb14b9e577cdbe7d87e7f5f813bc206c51a7dad42fdf10a4c8e9a40d1c` |

`architecture.md`, `ux-design-specification.md`, and `ux-design-directions.html` are `historical-reference-only`. They may explain prior decisions or completed evidence but may not supply current FR, NFR, gate, phase, UX, or story scope.

## 2. Issue and impact summary

The original tracked increment can no longer be treated as sprint-ready against the current product contract. Epics 0–8 are historically `done`, but the current PRD records an incomplete Phase 1 CLI, a noncompliant N=8 diagnostic benchmark, missing graph auto-seeding and BM25+semantic control, several architecture-contract gaps, missing MVP NFR evidence, and no successor story ownership. The final architecture spine adds implementation obligations but does not yet bind FR75, NFR37, or G6. Its unphased AD-14 also conflicts with the PRD's approved MVP/Phase 2/Phase 3 migration boundary.

The impact is planning-wide:

- **PRD:** Product scope remains achievable, but the status register needs one narrow correction: the broad `FR14–FR22` implemented range overlaps the explicit partial FR17 row. Replace it with `FR14–FR16, FR18–FR22`; retain FR17 only in the partial protocol row. No product requirement is reduced.
- **Architecture:** AD-14 must be phase-qualified, the binding header and trace maps must extend through FR75/NFR37/G6, and every current alignment gap must acquire an owner, evidence path, resolved verdict, or approved phase exception.
- **Epics:** The current file is not a valid derivation of the current PRD/UX/architecture set. It ends its functional inventory at FR74, its NFR inventory before NFR37, carries legacy UX requirements, still lists superseded inputs as active, and treats completed broad stories as if they owned current gaps.
- **UX:** `DESIGN.md` and `EXPERIENCE.md` are the current presentation/behavior inputs. Their legacy UX source entries must be moved from active `sources` to explicitly non-normative `historicalSources`; the documents' current PRD hash and implementation evidence remain intact.
- **Tracking:** `sprint-status.yaml` is read-only during this correction. It currently contains no Story 32–35 entries, and Epic 24 remains `in-progress` despite its listed stories and retrospective being `done`; sprint planning must validate that state later rather than silently interpreting it here.

## 3. Required human decisions

### D1 — Retain or reset the G1–G6 decision date

**Owner:** Jerome. **Decision required before any Story 32–35 registration.**

Choose exactly one:

| Choice | Required record | Consequence |
| :----- | :-------------- | :---------- |
| `RETAIN-2026-12-01` | Jerome ratifies 2026-12-01 for the expanded G1–G6 gate, accepts the delivery/evidence critical path in this proposal, keeps 2026-10-31 as the prerequisite checkpoint, and confirms capacity plus named reviewers. | All prerequisite stories must be selected and capable of completing before the checkpoint. Missing names, corpus, capacity, or a prerequisite at 2026-10-31 makes the date unreachable; the PRD's no-verdict/no-launch rule applies. |
| `RESET-ONCE` **(recommended)** | Jerome supplies one exact replacement G1–G6 date and rationale. The same approved change records the re-derived prerequisite checkpoint and the Phase 1.5 launch date. | Uses the PRD's single permitted reset. A second miss is a gate failure. No placeholder date may be registered. |

Recommendation: select `RESET-ONCE`. The current work breakdown contains 48 unconditional narrow successor/evidence stories plus six conditional kill-switch stories, the corpus is not frozen, and neither independent reviewer is named. Retaining the date without a capacity-backed commitment would turn the 2026-10-31 checkpoint into a predictable no-go rather than a useful control.

Decision record to complete before approval:

- `D1 choice:` **OPEN**
- `G1–G6 decision date:` **OPEN**
- `Prerequisite checkpoint:` **OPEN** (`2026-10-31` only if retained; otherwise re-derived)
- `Phase 1.5 launch date:` **OPEN** (`2027-01-01` only if retained; otherwise re-derived)
- `Decision owner/signature:` **Jerome — OPEN**

### D2 — Corpus and independent reviewer assignment

**Accountable owner:** Jerome. **Test-governance owner:** Murat. **Decision required before Stories 33.6 or 33.7 can be registered.**

- Jerome must name the real Phase 1 corpus steward and approve the corpus acquisition/use boundary.
- Jerome must name **Independent Reviewer A** and **Independent Reviewer B**. “TBD”, an agent identity, the implementing developer, or the corpus author does not satisfy independence.
- Murat owns the labelling protocol, blinded assignment, pairwise Cohen's kappa calculation, freeze manifest, and dispute/re-freeze evidence.
- There is no acceptable G1 phase exception for missing reviewers: the PRD fallback explicitly allows a diagnostic run but says it does not satisfy the hard gate.

Decision record to complete before approval:

- `Corpus steward:` **OPEN**
- `Independent Reviewer A:` **OPEN**
- `Independent Reviewer B:` **OPEN**
- `Independence/conflict check accepted by:` **Jerome + Murat — OPEN**

## 4. Recommended path and rejected alternatives

### Selected: hybrid planning correction

1. Correct architecture authority and phase boundaries.
2. Re-derive the epic inventory and coverage from current artifacts.
3. Register only narrow successor stories whose creation gates pass.
4. Rebuild sprint readiness and sequencing from those registered stories.

| Option | Verdict | Effort | Risk | Reason |
| :----- | :------ | :----- | :--- | :----- |
| Directly edit old Epics 0–8 and reopen completed stories | Rejected | High | High | Destroys historical evidence and violates the successor-story and historical-slice policies. |
| Treat the N=8 suite and existing CLI stubs as sufficient | Rejected | Low | Critical | Contradicts FR17, FR25, FR53, NFR31, G1, and G6. |
| Rebaseline all AD-14 staged migration into MVP | Rejected | High | High | Expands MVP beyond the approved product phase boundary and hides the actual conflict. |
| Defer G6 or soften it | Rejected | Medium | Critical | Changes a ratified hard release gate and permits architecture gaps without evidence. |
| Architecture-first correction plus narrow successor epics | Selected | High | Medium | Preserves history, makes ownership explicit, and keeps the PRD's hard outcomes intact. |

## 5. Architecture correction contract

The architecture handoff must make the following changes atomically.

### 5.1 Phase-qualify AD-14

Replace the unphased AD-14 rule with this decision outcome, preserving its adapter and secret-resolution constraints:

> **AD-14 — Evolve schemas and providers under phase-qualified tenant migration contracts [ADOPTED].** In Phase 1/MVP, FR43 permits only a tenant-scoped, explicitly acknowledged degraded rebuild of rebuildable projections. The command must disclose impact and progress, fail closed on incomplete verification, preserve authoritative EventStore truth, use the FR75 configuration epoch, and never claim zero downtime. In Phase 2, embedding-provider/model/dimension and index-schema changes use versioned create-backfill-verify-switch-retire with disjoint active/staging resources and atomic activation. In Phase 3, backend replacement uses the same staged cutover at the adapter boundary. No MVP implementation or completed historical migration is retroactively claimed to meet the Phase 2/3 zero-downtime contract.

This is the proposed product/architecture-approved MVP phase exception required by G6. It narrows AD-14 by phase; it does not weaken Phase 2 or Phase 3. Approval must be recorded by Jerome and Architecture before the spine returns to `final`.

### 5.2 Extend binding and traceability

- Change frontmatter bindings to `FR1-FR75`, `NFR1-NFR37`, `G1-G6`, and `L1-L3`.
- Remove `architecture.md` from active `sources`; retain it under `historicalSources` with `status: superseded` and `permittedUse: historical decision/evidence provenance only`.
- Bind FR75 to AD-2, AD-3, AD-4, and phase-qualified AD-14 across command acceptance, CloudEvent identity, and projection epochs.
- Bind NFR37 to AD-12 and AD-19, and trace it to the active CLI, shared contracts, output formatters, progress/error paths, automated terminal modes, and manual keyboard walkthrough.
- Add **AD-20 — Treat release gates as evidence contracts [ADOPTED]**. It binds G1–G6, requires a current evidence or approved phase-exception row for every governed requirement/gap, requires an owner and tracker entry, keeps phase-inactive surfaces out of gate credit, and prevents historical `done` state from being interpreted as current qualification.
- Add capability-map rows for durable idempotency, active CLI accessibility, and release-gate closure.
- Add G1–G6 to the architecture trace table and identify the exact evidence-producing successor stories from sections 9–12.

### 5.3 Reconcile the current alignment-gap ledger

Use the following dispositions. A row may be removed only with a `confirmed resolved` evidence verdict; otherwise it remains with its owner/story or approved exception.

| Gap | Disposition | Owner / authority |
| :-- | :---------- | :---------------- |
| Ordinary ingest precedes EventStore acceptance | Stories 34.1–34.3 | Amelia + Winston |
| No current-revision all-axis checkpoint | Story 34.4 | Amelia + Winston |
| No authoritative replay-to-all-projections | Story 34.5 | Amelia + Murat |
| Caller-controlled `IngestedBy` | Story 34.8 | Amelia + security reviewer |
| No tenant backend principals / direct credentials | Story 34.9, coordinated with Story 31.2 without absorbing it | Winston + Amelia + security reviewer |
| Incomplete erasure, non-reuse, and restore quarantine | Stories 34.6–34.7 | Amelia + Winston + Murat |
| Provider/coordination boundary violations | Story 34.10; any retained exception must be a named finite AD exception | Winston + Amelia |
| Missing canonical fusion preprocessing | Story 33.1 | Amelia + Murat |
| Missing auto-seed, case merge, kill switch, and G1 harness | Stories 33.2–33.8 | Amelia + Murat + Jerome |
| No Kubernetes EventStore gateway workload | `EX-P15-01`: Phase 1.5/L2 exception; no G6 credit and no public launch. Retain DW-713/DW-728 and require a future selected successor before L2. | Jerome + Winston |
| Source/package mode evidence separation | Existing Story 30.2; `EX-OP-01` excludes it from Phase 1 G6 unless AD-20 explicitly classifies it as active-foundation critical | Memories Maintainer |
| Floating AppHost Redis/Falkor defaults | Story 34.14 because AppHost is on the G3 path | Memories Maintainer + Murat |
| OpenBao versions precede patches | Existing Story 31.1; no invented completion or waiver | Memories Maintainer + independent security reviewer |
| Web described as non-runnable RCL | Correct the gap as stale only after re-running the existence/browser evidence for `tests/Hexalith.Memories.Web.SpecimenHost` and `.Web.E2E`; product Web remains future | Sally + Murat |
| Kubernetes includes MCP before launch gates | `EX-P15-02`: assets may exist, but ingress, publication, announcement, and product activation stay disabled until L1–L3 pass | Jerome + Winston |

## 6. `epics.md` and UX derivation reconciliation

The epics handoff must apply these rules before adding any new epic or story:

1. Replace `inputDocuments` with the authoritative set in section 1. Move `architecture.md`, `ux-design-specification.md`, prior readiness reports, and prior proposals to typed historical/change-control metadata; none remains an active requirement source.
2. In `DESIGN.md` and `EXPERIENCE.md`, keep the current PRD hash and implementation evidence, add the addendum and corrected final spine as active sources, and move `ux-design-specification.md` plus `ux-design-directions.html` to `historicalSources`. Amend EXPERIENCE source precedence so legacy artifacts are non-normative lineage only.
3. Regenerate the requirements inventory through FR75 and NFR37 from the PRD. Do not copy the stale FR/NFR summaries currently at the top of `epics.md`.
4. Replace the legacy UX-DR coverage map as the current derivation map with direct DESIGN/EXPERIENCE traceability: phase/surface matrix, exact CLI grammar, stable reading order, state closure, accessibility, evidence semantics, recovery, and future-Web boundary. Preserve the old UX-DR map as historical lineage if needed.
5. Preserve every completed story and its original evidence. Add dated supersession notes where current wording differs. In particular:
   - Stories 2.8 and 26.8 remain the N=8 synthetic diagnostic history; they do not own G1.
   - Story 7.1 remains historical CLI foundation; it does not own missing Phase 1 verbs.
   - Epic 13's completed migration evidence remains historical; it does not override phase-qualified AD-14.
   - Epics 20–26 remain completed remediation history; current-contract gaps receive successor stories rather than retroactive AC edits.
6. Update the current implementation-readiness boundary so newly approved Epics 32–35 can be explicitly `MVP/included` when registered. Do not infer inclusion from numeric ordering.
7. Rebuild FR, NFR, gate, UX, and architecture-decision coverage maps from the corrected sources. Every current MVP ID must map to evidence, a narrow story, or an approved phase exception.
8. Correct stale textual status labels in `epics.md` from the tracker only after read-only validation. Do not change tracker history in this handoff.

## 7. Historical context classification and slice proof

This classification applies to every proposed story in sections 9–12 and must be copied into each eventual story's `Historical Context Classification` section with only its relevant rows.

| Prior influence | Classification | Permitted use |
| :-------------- | :------------- | :------------ |
| Completed Epics 0–31 and their story artifacts | `historical-reference-only` | Dependency, prior decision, and rerunnable evidence only; never current completion credit. |
| Stories 1.2, 1.5, and 1.6 | `anti-template` | Do not copy their broad domain/backend/orchestration slice shape, task density, or file scope. |
| Stories 2.8 and 26.8 | `anti-template` | Preserve diagnostic evidence only; do not reuse their N=8, synthetic, graph-start, or best-single-axis gate shape. |
| Story 7.1 and the broad Epic 7 CLI promise | `anti-template` | Do not create one “finish the CLI” umbrella or infer verbs from the old command-group placeholder. |
| Story 13.6 and pre-qualified migration wording | `historical-reference-only` | Migration evidence and lessons only; no current AD-14 authority. |
| Current implemented CLI commands and `Client.Rest` methods re-verified at story creation | `current-narrow-pattern` | Command wiring, global options, formatter, cancellation, and test patterns only; whole prior stories are not reused. |
| Current benchmark loader/scorer and deterministic fixtures re-verified at story creation | `current-narrow-pattern` | NDCG calculation and reproducibility mechanisms only; not corpus/control/gate scope. |
| Current server endpoints/contracts re-verified at story creation | `current-narrow-pattern` | A single story may consume one present endpoint/contract; existence and behavior must be command-verified in that story. |

**Slice proof:** Stories 32.1–32.15 each expose one Phase 1 CLI user outcome. Story 32.16 is one cross-command conformance gate and uses a checkpoint table rather than implementing missing verbs. Stories 33.1–33.8 each produce one retrieval/gate prerequisite; Stories 33.9–33.14 are single-action conditional contracts that remain unregistered unless Story 33.8 records a G1 failure. Stories 34.1–34.14 each close one architecture or current-wording outcome. Stories 35.1–35.9 each produce one independently reviewable evidence result. Story 35.10 is the only umbrella: it assembles the indivisible G1–G6 decision packet and therefore carries a checkpoint row for every gate. No split recreates a completed broad story.

## 8. Epic AC verification at proposal time

Verified 2026-09-12 against branch `main`. These commands must be re-run in each generated story; line numbers are observations, not permanent anchors.

| Ref | Epic/current claim | Class | Rerunnable command / evidence | Observed | Verdict |
| :-- | :----------------- | :---- | :---------------------------- | :------- | :------ |
| V01 | “No owning successor stories exist” for the strengthened prerequisites | Existence | `rg -n '^### Story (32|33|34|35)\\.' _bmad-output/planning-artifacts/epics.md; rg -n '^  (32|33|34|35)-' _bmad-output/implementation-artifacts/sprint-status.yaml` | No Story 32–35 definition or tracker row exists. | `confirmed` |
| V02 | `ingest`, `traverse`, and `case` are stubs; tenant has only `list`; status has only `telemetry` | Behavioral / existence | `sed -n '75,170p' src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs` | The three groups are created through `NotImplementedCommand`; the only wired tenant/status children are `list` and `telemetry`. | `confirmed` |
| V03 | Hybrid skips graph without a start node | Behavioral | `sed -n '160,188p' src/Hexalith.Memories.Server/Search/HybridSearchService.cs` | The graph branch logs “graphStartNodeId is null” and creates no graph task. | `confirmed` |
| V04 | Current benchmark has N=8, all topics require all three axes, and every topic carries a graph start | Quantitative | `jq -r '"topics=\\(length)", "axisSets=\\([.[].requiredAxes] | unique | tostring)", "graphStarts=\\([.[].graphStartNodeId] | map(select(. != null)) | length)"' tests/Hexalith.Memories.Benchmarks/Data/ground-truth.json` | `topics=8`; one axis set `syntactic,semantic,graph`; `graphStarts=8`. | `confirmed` |
| V05 | Current benchmark control is best single active axis | Behavioral | `sed -n '176,190p' tests/Hexalith.Memories.Benchmarks/BenchmarkSuiteTests.cs` | It computes `bestSingleNdcg` and tests `hybridNdcg > bestSingleNdcg`; no BM25+semantic control appears. | `confirmed` |
| V06 | No NFR31 timed run is recorded | Existence | `sed -n '1,20p' docs/dev/quickstart-walkthrough-log.md` | The only row is `_pending_` and says no walkthrough has been recorded. | `confirmed` |
| V07 | The final spine binds only through FR74/NFR36/G5 and lists legacy architecture as a source | Existence | `sed -n '1,25p' _bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` | Frontmatter contains `FR1-FR74`, `NFR1-NFR36`, `G1-G5`, and active `architecture.md`. | `confirmed` gap |
| V08 | AD-14 is unphased staged migration | Behavioral | `sed -n '134,140p' _bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` | One staged rule binds all provider/schema migration with no MVP/Phase 2/Phase 3 qualification. | `confirmed` conflict |
| V09 | `epics.md` derives from superseded architecture and UX inputs and omits current spines | Location | `sed -n '1,20p' _bmad-output/planning-artifacts/epics.md` | Active inputs include `architecture.md` and `ux-design-specification.md`; addendum, final spine, DESIGN, and EXPERIENCE are absent. | `confirmed` gap |
| V10 | Current DESIGN/EXPERIENCE still list legacy UX artifacts as active sources | Location | `sed -n '1,18p' _bmad-output/planning-artifacts/ux-designs/ux-memories-2026-09-12/{DESIGN,EXPERIENCE}.md` | Both list `ux-design-specification.md` and `ux-design-directions.html` under `sources`. | `confirmed` metadata gap |
| V11 | Architecture's “non-runnable RCL” Web gap is current | Existence | `test -d tests/Hexalith.Memories.Web.SpecimenHost && test -d tests/Hexalith.Memories.Web.E2E` | Both directories exist. | `corrected` — the gap ledger is stale; re-run browser evidence before marking resolved. |
| V12 | AppHost defaults are floating while deployment images are pinned | Location | `rg -n 'AddContainer\\("memories-vectors"|AddContainer\\("memories-graphs"|image:' src/Hexalith.Memories.AppHost/Program.cs deploy/kubernetes/base/{redis-statefulset,falkordb-statefulset}.yaml` | AppHost container calls have no tags; Kubernetes images carry tag+digest. | `confirmed` |
| V13 | The PRD implemented range and partial FR17 row overlap | Location | `sed -n '988,1000p' _bmad-output/planning-artifacts/prd.md` | `FR14–FR22` includes FR17 while the next specific row marks FR17 partial. | `corrected` — use the explicit partial row and correct the broad range in the next PRD update. |
| V14 | Proposed acceptance criteria below describe future end states | Intent | This proposal, sections 9–12 | They do not assert that the desired behavior currently exists. Current premises are V01–V13 and must be re-verified per story. | `confirmed` as non-verifiable intent |

No `unverifiable` current-state premise is used to justify story scope. D1 and D2 are human decisions, not technical claims; they remain explicitly open and block registration where stated.

## 9. Proposed Epic 32 — Phase 1 CLI completion

**Epic outcome:** A developer or operator can perform every Phase 1 CLI operation in the PRD's exact command tree, with the shared contract semantics required by NFR37.  
**Owners:** Amelia (delivery), Sally (interaction/accessibility), Murat (test evidence).  
**Registration rule:** No story below is registered until its current endpoint/client premise has a story-local Epic AC Verification verdict. V02 is the shared baseline; each story must add exact method/route/test evidence.  
**Sequencing:** Stories 32.11, 32.5, and 32.1 are the G3 critical-path minimum; this priority does not merge them. Story 32.16 follows all command stories.

| Story | Independently demonstrable outcome and acceptance boundary | Owner | Dependencies |
| :---- | :--------------------------------------------------------- | :---- | :----------- |
| **32.1 — CLI local-file ingestion** | `memories ingest <file> --tenant --case` accepts a local file, reports accepted/progress/final state without duplicate “created” output, and preserves human/table/JSON/error semantics. URL and directory inputs are rejected with the appropriate next command grammar in this slice. | Amelia + Murat | Verified current file-ingest client/route |
| **32.2 — CLI URL ingestion** | The same command accepts one URL, exposes provider/queue delay and terminal failure safely, and does not absorb local-file or directory behavior. | Amelia + Murat | Verified current URL-ingest client/route |
| **32.3 — CLI directory batch ingestion** | The same command accepts one directory, reports bounded per-batch/per-unit progress and failure counts, and remains cancellable without implying rollback of accepted work. | Amelia + Murat | Verified directory batch client/route |
| **32.4 — CLI graph traversal** | `memories traverse --tenant --from [--depth] [--edge-type]` returns ordered, typed, gap-aware paths with case scope and safe unavailable/empty/error output. It does not add a search `--from` flag. | Amelia + Sally + Murat | FR47 server/client verification |
| **32.5 — CLI case creation** | `memories case create` creates one case inside authorized tenant scope, prints the id/unit-count/next-step result, and distinguishes validation, denial, duplicate, and unavailable service. | Amelia + Murat | Verified create-case client/route |
| **32.6 — CLI case listing** | `memories case list` returns bounded tenant-scoped cases with empty-state and pagination semantics; it performs no mutation. | Amelia + Murat | Verified list-case client/route |
| **32.7 — CLI case deletion** | `memories case delete` requires explicit scoped confirmation, reports asynchronous/completed outcome honestly, and never promises recovery that the server contract lacks. | Amelia + Sally + Murat | Verified delete-case client/route |
| **32.8 — CLI add case member** | `case add-member` adds attribution metadata in authorized tenant/case scope and returns the updated member state without granting authorization. | Amelia + security reviewer + Murat | Verified add-member route; NFR8 boundary |
| **32.9 — CLI remove case member** | `case remove-member` removes attribution metadata in authorized tenant/case scope and reports missing/conflicting membership without changing authorization. | Amelia + security reviewer + Murat | Verified remove-member route; NFR8 boundary |
| **32.10 — CLI case activity** | `case activity` returns chronological, source-safe activity with bounded empty/error output and no membership-as-authorization inference. | Amelia + Sally + Murat | Verified activity client/route |
| **32.11 — CLI tenant creation** | `tenant create` invokes the sole tenant provisioning workflow, reports provisioned resource/readiness status without secrets, and fails safely on invalid/duplicate scope. | Amelia + Winston + Murat | Story 34.9 contract or approved compatible ordering |
| **32.12 — CLI tenant isolation verification** | `tenant verify` prints the current NFR8 checks and evidence boundary, distinguishes pass/fail/incomplete, and never turns missing backend evidence into pass. | Amelia + security reviewer + Murat | Stories 34.9 and 35.6 |
| **32.13 — CLI tenant erasure** | `tenant delete` requires explicit tenant confirmation and reaches only the verified erasure workflow; “deleted” is emitted only after FR39 completion evidence, otherwise progress/failure is shown. | Amelia + Winston + Murat | Stories 34.6–34.7 |
| **32.14 — CLI case ingestion-status counts** | `memories status --case` reports exactly the six product stages while preserving V1 wire mappings, last update, delay, and incomplete-current-revision meaning. | Amelia + Sally + Murat | Story 34.4 |
| **32.15 — CLI failed-unit inspection** | `memories status --failed` lists stage, sanitized error, attempts, last update, and implemented recovery; absent recovery is stated rather than invented. | Amelia + Sally + Murat | Verified failed-unit client/route |
| **32.16 — Phase 1 CLI semantic/accessibility conformance** | Across the completed Phase 1 command tree, automated and keyboard-only evidence proves NFR37 reading order, wrapping, linear table alternative, redirected/no-color determinism, progress, cancellation/timeout, duplicate suppression, sanitization, and human/table/JSON/stderr/exit-code parity. This story fixes only cross-command conformance defects; it may not implement a missing verb. | Sally + Murat + Amelia | Stories 32.1–32.15 and FR75 stories |

Story 32.16 requires this registration-time checkpoint table in its story file:

| Checkpoint | Owner | Evidence command or artifact | Review state | Completion state |
| :--------- | :---- | :--------------------------- | :----------- | :--------------- |
| Reading order and text labels | Sally | Focused CLI formatter/contract lane | unreviewed | not-started |
| Narrow/wrapped and linear table alternative | Sally + Murat | Narrow-terminal snapshot lane | unreviewed | not-started |
| Redirected and no-color determinism | Amelia + Murat | Redirect/no-color CLI lane | unreviewed | not-started |
| Progress, delay, cancellation, and timeout | Amelia + Murat | Lifecycle CLI integration lane | unreviewed | not-started |
| Duplicate and secret/restricted-output suppression | Security reviewer + Murat | Negative/sanitization lane | unreviewed | not-started |
| Cross-format/stderr/exit-code semantic parity | Sally + Murat | Phase 1 command-tree parity matrix | unreviewed | not-started |
| Keyboard-only manual walkthrough | Independent human tester | Dated NFR37 walkthrough artifact | unreviewed | not-started |

## 10. Proposed Epic 33 — Three-axis thesis prerequisites and G1

**Epic outcome:** The real Phase 1 thesis protocol can be executed exactly as a developer experiences hybrid search and can produce an immutable G1 pass/fail record.  
**Owners:** Amelia (retrieval/harness), Murat (test architecture), Jerome (corpus/review/governance), Winston (architecture consistency).  
**Historical guard:** Stories 2.8 and 26.8 are anti-templates. V03–V05 are the baseline corrections.

| Story | Independently demonstrable outcome and acceptance boundary | Owner | Dependencies |
| :---- | :--------------------------------------------------------- | :---- | :----------- |
| **33.1 — Canonical per-axis fusion preprocessing** | Blank ids, non-finite scores, and duplicate candidates are handled by the AD-9 canonical rule before competition-rank allocation; cross-surface golden vectors preserve order, scores, and axis-health meanings. | Amelia + Murat | Corrected spine AD-9 trace |
| **33.2 — Case-local hybrid graph auto-seeding** | When public hybrid search has no explicit graph start, each case-local contribution seeds from the union of top five syntactic and top five semantic candidates and traverses to depth at most two; populated graph omission is not silent. | Amelia + Winston + Murat | Story 33.1 |
| **33.3 — Tenant-wide case-partitioned graph fusion** | Tenant-wide search ranks across cases with mandatory attribution while every graph seed, node, edge, and path stays in its authoritative case partition. | Amelia + security reviewer + Murat | Story 33.2; FR34 current route |
| **33.4 — Default-hybrid graph kill switch** | A named, tested release action can remove graph from default hybrid while retaining explicit graph search/traversal, and produces the PRD-required repositioning/configuration evidence if G1 fails. It does not execute the product re-scope before a fail decision. | Winston + Amelia + Jerome | Corrected architecture AD-20 |
| **33.5 — BM25+semantic two-axis benchmark control** | The harness computes a deterministic BM25+semantic RRF control, per-topic delta, mean delta, and worst regression; single-axis values remain diagnostics. | Amelia + Murat | Stories 33.1–33.3 |
| **33.6 — Real Phase 1 corpus and topic freeze** | A lawful real-content Phase 1 corpus and at least 50 representative topics are frozen with hashes, pre-registered thesis-stress share, edge type/source census, cached embedding hashes, and no post-score mutation path. | Corpus steward + Jerome + Murat | D2 named corpus steward |
| **33.7 — Independent graded-label freeze** | Jerome plus two named independent humans grade after topics exist; every pair's Cohen's kappa is at least 0.6 before labels freeze; disagreement/re-freeze history is immutable. | Jerome + Reviewer A + Reviewer B + Murat | D2 completed; Story 33.6 |
| **33.8 — G1 execution and immutable decision packet** | The unchanged frozen inputs run twice; the packet records per-topic and aggregate metrics and an immutable pass/fail verdict. A failure opens the ordered conditional contracts below; this story does not bundle their implementation. | Murat + Jerome + independent gate reviewer | Stories 33.1–33.7 |

Stories 33.6 and 33.7 must carry checkpoint tables with one row for each named population, freeze, reviewer, agreement, and immutability obligation. Placeholder people or a shared “reviewers” row fails registration.

The following contracts are defined but remain unregistered unless Story 33.8 records G1 failure. If triggered, register them in order; do not merge them:

| Conditional story | Single kill-switch outcome | Owner |
| :---------------- | :------------------------- | :---- |
| **33.9 — Stop graph-fusion R&D as the default** | Freeze further default graph-fusion investment and record the portfolio decision. | Jerome |
| **33.10 — Change default hybrid to BM25+semantic** | Activate the tested Story 33.4 switch so default hybrid excludes graph while explicit graph and `traverse` remain. | Amelia + Winston + Murat |
| **33.11 — Remove FalkorDB from general search scoring** | Remove graph-store participation from general scoring without removing explicit traversal. | Amelia + Murat |
| **33.12 — Reposition public product claims** | Update README and executive summary to “BM25+semantic search with an explicit graph” before any later decision. | Jerome + documentation owner |
| **33.13 — Cancel Phase 1.5 gate evaluation** | Mark L1–L3 unevaluated/cancelled and remove the launch decision date without launching or silently slipping. | Jerome + Winston |
| **33.14 — Re-scope and re-estimate after G1 failure** | Produce the required new sprint-change proposal and replacement product plan. | Jerome + Winston + Murat |

## 11. Proposed Epic 34 — Architecture-contract convergence

**Epic outcome:** Every currently identified active-foundation architecture gap has an implementation/evidence owner or an approved phase exception, without reopening completed remediation history.  
**Owners:** Winston (decision integrity), Amelia (delivery), Murat (verification), security reviewer where named.  
**Registration rule:** V07–V13 are shared baseline rows. Each story adds symbol/route/test evidence for its own premise before registration.

| Story | Independently demonstrable outcome and acceptance boundary | Owner | Dependencies |
| :---- | :--------------------------------------------------------- | :---- | :----------- |
| **34.1 — Authoritative V1 command acceptance and idempotency** | File/URL V1 mutations reach durable EventStore acceptance before projection scheduling; the same tenant/case/operation token yields one domain mutation across retry. | Amelia + Winston + Murat | Corrected AD-2/AD-4/FR75 trace |
| **34.2 — Durable CloudEvent identity** | The same tenant, case, exact validated source, and event id yields one durable domain mutation across redelivery; spoofed or changed source identity cannot share credit. | Amelia + security reviewer + Murat | Story 34.1 contract primitives |
| **34.3 — Epoch-aware projection idempotency** | One source-version/schema-generation/embedding-configuration tuple produces one projection outcome; a new legitimate epoch is not suppressed and stale acknowledgements cannot complete it. | Amelia + Winston + Murat | Stories 34.1–34.2 |
| **34.4 — Current-revision all-axis completion checkpoint** | `indexed` requires monotonic acknowledgements from syntactic, semantic, and graph projections for the current authoritative revision and active configuration epoch; two-of-three and stale acknowledgements remain incomplete. | Amelia + Winston + Murat | Story 34.3 |
| **34.5 — Authoritative replay-to-all-projections** | Projection loss rebuilds from EventStore truth through the same FR6/FR13 completion contract, exposes progress, and is distinct from Redis-input repair and export import. | Amelia + Murat | Story 34.4 |
| **34.6 — Irreversible tenant erasure completion** | Tenant deletion completes only after projection purge, EventStore-held content becomes irreversibly inaccessible, and the access-telemetry handoff is durable; failure remains incomplete. | Amelia + Winston + security reviewer | EventStore erasure mechanism decision |
| **34.7 — Erased-tenant non-resurrection and non-reuse** | Replay, restart, export restore, and backup restore cannot resurrect erased content; unsafe payloads are quarantined and the deleted tenant id cannot be reused. | Amelia + Murat + security reviewer | Stories 34.5–34.6 |
| **34.8 — Server-derived mandatory provenance** | External `ingested_by` comes only from normalized authenticated subject; internal provenance comes only from the authenticated app allowlist plus explicit tenant grant; caller values cannot override it. | Amelia + security reviewer + Murat | Current auth boundary verification |
| **34.9 — Tenant backend principals and secret boundary** | Provisioning creates/resolves tenant-scoped Redis/Falkor authority through the adopted Dapr/OpenBao boundary, and principal-driven negative evidence proves cross-tenant denial. It coordinates with Story 31.2 but does not absorb its deployed secret-store migration. | Winston + Amelia + security reviewer + Murat | Story 31.2 compatible boundary |
| **34.10 — Finite adapter and coordination boundary** | Provider/data-plane calls sit behind named adapters and permanent coordination uses Dapr state unless a finite, named AD exception is approved and architecture-tested. | Winston + Amelia + Murat | Corrected architecture exception registry |
| **34.11 — Current per-tenant fairness contract** | A reproducible mixed workload proves FR8's current admission/fairness behavior without weakening the NFR13 budgets; failure remains evidence, not an exception. | Amelia + Murat | Current load harness verification |
| **34.12 — Capability-aware degradation contract** | Selected-axis failures preserve unavailable/excluded/no-hit distinctions, safe partial result only when at least one selected axis responds, no-safe-axis failure, freshness/recovery, and incomplete-revision safety. | Amelia + Sally + Murat | Story 34.4 |
| **34.13 — Current readiness contract** | Readiness proves authentication configuration, Dapr control boundary, and EventStore command availability while individual search backend failure is reported as capability degradation. | Amelia + Winston + Murat | Story 34.12 |
| **34.14 — Qualified AppHost backend defaults** | AppHost/Aspire Redis and Falkor defaults use qualified pinned tags/digests with explicit consumer override and a current compatibility/recovery evidence record. | Memories Maintainer + Murat | V12; no dependency update without separate authorization |

## 12. Proposed Epic 35 — MVP quality and release-gate evidence

**Epic outcome:** Every current MVP NFR and each G1–G6 gate has a dated, rerunnable, independently reviewed evidence or exception record.  
**Owners:** Murat (evidence architecture), Jerome (release decision), Amelia (defect remediation only through separately scoped stories), Winston (architecture exceptions).

| Story | Independently demonstrable outcome and acceptance boundary | Owner | Dependencies |
| :---- | :--------------------------------------------------------- | :---- | :----------- |
| **35.1 — NFR1 syntactic latency evidence** | Record p95 syntactic latency below 200 ms at 10 concurrent queries and 10K units for one tenant, with environment and raw result artifact. | Murat + performance tester | Stable syntactic path |
| **35.2 — NFR2 semantic latency evidence** | Record p95 semantic latency below 500 ms under the same declared scale/tenancy conditions. | Murat + performance tester | Stable semantic path |
| **35.3 — NFR3 hybrid latency evidence** | Record p95 hybrid latency below 1 second under the same declared scale/tenancy conditions and the final auto-seeded hybrid behavior. | Murat + performance tester | Stories 33.2–33.3 |
| **35.4 — NFR4 graph latency evidence** | Record p95 graph traversal below 2 seconds at depth at most five, 10 concurrent queries, and 10K units, with bounded server work. | Murat + performance tester | Story 32.4/current graph path |
| **35.5 — NFR36 file/URL freshness evidence** | Over 100 normally admitted units, record p95 time to current-revision `indexed` within 60 seconds for at most 10 KB and 5 minutes for at most 1 MB, plus noisy-neighbor batch/repair fairness. | Murat + Amelia | Stories 32.1–32.2, 34.4, 34.11 |
| **35.6 — G2 principal-driven isolation evidence** | Re-run NFR8 with tenant-A principals against tenant-B ids, indexes, malformed ids, ingestion, search, and graph collisions; zero restricted evidence is returned. | Independent security reviewer + Murat | Stories 32.12, 34.9; Epic 24 state reconciled |
| **35.7 — G3 clean-machine onboarding measurement** | On the PRD clean-machine definition, time README/AppHost → tenant create → case create → ingest → search query below 30 minutes and append the dated machine/result log. `quickstart` is supplementary only. | Independent onboarding tester + Jerome | Stories 32.11, 32.5, 32.1 |
| **35.8 — G4 case ownership and graph-partition evidence** | Prove strict single-case ownership, case query containment, and tenant-wide attributed fusion with every graph seed/node/edge/path remaining case-local. | Independent security reviewer + Murat | Story 33.3 |
| **35.9 — G5 cross-surface deterministic fusion evidence** | Golden vectors and 100 repeated queries prove NFR24/NFR25 scores, ordering, tie-breaking, axis meanings, and equivalent REST/CLI JSON meanings. MCP may be tested as preview but gives no Phase 1.5 activation credit. | Murat + independent reviewer | Stories 33.1–33.5, 32.16 |
| **35.10 — G1–G6 evidence assembly and release decision** | Assemble immutable G1–G6 packets; for G6, produce one row per current MVP FR/NFR and each active-foundation architecture gap with current evidence or a product+architecture-approved phase exception, owner, tracker key, reviewer, and verdict. Record release/no-release exactly as the PRD requires. No implementation is performed in this story. | Jerome + Winston + Murat + independent release reviewer | Stories 33.8, 35.6–35.9, all G6 owner rows complete |

Story 35.10 is an approved umbrella only if its story file contains this checkpoint table and a linked per-requirement G6 matrix. Shared evidence may be referenced, but no row may inherit another row's completion state.

| Gate checkpoint | Owner | Evidence command or artifact | Review state | Completion state |
| :-------------- | :---- | :--------------------------- | :----------- | :--------------- |
| G1 thesis protocol | Jerome + Murat | Story 33.8 immutable G1 packet | unreviewed | not-started |
| G2 tenant isolation | Independent security reviewer | Story 35.6 evidence packet | unreviewed | not-started |
| G3 clean-machine onboarding | Independent onboarding tester | Story 35.7 walkthrough record | unreviewed | not-started |
| G4 case/graph containment | Independent security reviewer + Murat | Story 35.8 evidence packet | unreviewed | not-started |
| G5 deterministic explain/fusion | Independent reviewer + Murat | Story 35.9 evidence packet | unreviewed | not-started |
| G6 contract/architecture closure | Jerome + Winston + Murat | Per-requirement `g6-contract-closure-matrix.md` | unreviewed | not-started |

The linked G6 matrix must enumerate, at minimum:

- Every MVP FR in the canonical phase register: FR1–FR22, FR24–FR52, FR53 Phase 1 slices, FR55–FR57, FR63–FR70, and FR72–FR75.
- Every MVP NFR: NFR1–NFR4, NFR8, NFR11, NFR16–NFR17, NFR24–NFR26, NFR30–NFR31, and NFR36–NFR37.
- Every architecture alignment row classified by AD-20 as active-foundation critical.
- For each row: requirement/gap id, exact current wording/source hash, delivery state, evidence or exception id, accountable owner, registered story/tracker key, evidence command/artifact, independent reviewer, review state, completion state, and pass/fail/blocked verdict.

## 13. Complete ownership and phase-exception ledger

This ledger prevents any currently identified G6 prerequisite from returning to an unowned state.

| Current prerequisite set | Owner/story or approved exception |
| :----------------------- | :-------------------------------- |
| Missing Phase 1 CLI operations: FR1–FR3, FR10–FR11, FR26–FR30, FR36, FR38–FR40, FR47, FR53 | Stories 32.1–32.15; Amelia accountable, with named specialist reviewers. FR39/tenant delete also depends on Stories 34.6–34.7. |
| Active CLI accessibility NFR37 | Story 32.16; Sally + Murat + Amelia. |
| FR17 auto-seeding | Story 33.2; Amelia + Winston + Murat. |
| FR17/FR34 tenant-wide case-partition merge | Story 33.3; Amelia + security reviewer + Murat. |
| Graph kill switch | Story 33.4; Winston + Amelia + Jerome. |
| FR25 two-axis control | Story 33.5; Amelia + Murat. |
| N ≥ 50 real corpus/topics | Story 33.6; named Corpus Steward + Jerome + Murat. Registration blocked by D2. |
| Jerome + two independent reviewers and kappa/freeze | Story 33.7; Jerome + two named human reviewers + Murat. No phase exception. |
| G1 execution | Story 33.8; Murat + Jerome + independent gate reviewer. |
| FR75 durable command/event/projection idempotency | Stories 34.1–34.3; Amelia + Winston + Murat/security. |
| FR6/FR13/NFR16 current revision and authoritative replay | Stories 34.4–34.5 and 34.7; Amelia + Winston + Murat. |
| FR39 verified erasure and non-resurrection | Stories 34.6–34.7 plus CLI Story 32.13; Amelia + Winston + security + Murat. |
| FR65 provenance | Story 34.8; Amelia + security reviewer + Murat. |
| FR38/FR40/FR44/NFR8 current tenant-principal boundary | Stories 34.9 and 35.6 plus CLI Stories 32.11–32.12; Winston + Amelia + security + Murat. |
| Architecture AD-8 adapter/coordination gap | Story 34.10; Winston + Amelia + Murat, or a finite named AD exception approved in the architecture handoff. |
| FR8 fairness re-verification | Story 34.11; Amelia + Murat. |
| FR66 degradation re-verification | Story 34.12; Amelia + Sally + Murat. |
| FR72 readiness re-verification | Story 34.13; Amelia + Winston + Murat. |
| AppHost floating backend defaults | Story 34.14; Memories Maintainer + Murat. |
| NFR1–NFR4 and NFR36 evidence | Stories 35.1–35.5; Murat + named performance tester. |
| NFR31/G3 | Story 35.7; independent onboarding tester + Jerome. |
| NFR24/NFR25/G5 | Story 35.9; Murat + independent reviewer. |
| Implemented current-wording MVP FRs and already-verified MVP NFRs | Story 35.10's per-requirement matrix owns evidence-link verification; it creates no implementation credit. Any missing current evidence creates a new narrow successor before G6 can pass. |
| AD-14 phase conflict | Section 5.1; Jerome + Winston approval. MVP degraded-rebuild exception is phase-specific and gives no Phase 2/3 credit. |
| FR75/NFR37/G6 architecture binding | Section 5.2; Winston/Architecture. |
| 2026-12-01 retain/reset decision | D1; Jerome. No inferred decision. |
| Phase 1.5 EventStore workload and MCP activation | `EX-P15-01` / `EX-P15-02`; Jerome + Winston. They give no G6 credit and keep L1–L3 closed. |
| Future product Web | Existing PRD phase exception; Sally + Jerome at future activation. Current specimen existence is re-verified and the stale architecture gap is corrected. |
| Operational source/package lanes | Existing Story 30.2 with `EX-OP-01`; Memories Maintainer. |
| OpenBao patch qualification | Existing Story 31.1; Memories Maintainer + independent security reviewer. |

## 14. Change-navigation checklist

### 1. Understand the trigger and context

- **1.1** [x] Trigger: failed 2026-09-12 sprint-readiness/finalize retest; the current release posture is no-go.
- **1.2** [x] Category: requirements/architecture misunderstanding plus planning derivation drift, exposed by a strengthened current PRD and final architecture review.
- **1.3** [x] Evidence: V01–V13, PRD release record, addendum current handoff, architecture/rubric retests.

### 2. Epic impact assessment

- **2.1** [x] Epics 0–31 remain historically valid; they cannot complete the newly strengthened contract without successors.
- **2.2** [x] Add Epics 32–35 only after architecture correction and story creation gates.
- **2.3** [x] Existing Epics 27–31 remain in their current tracks; only explicit dependencies/exceptions above affect them.
- **2.4** [x] No completed epic is obsolete or removed. New successor epics are necessary.
- **2.5** [x] Architecture precedes epic derivation; G3-critical CLI stories lead execution; evidence stories follow their capabilities.

### 3. Artifact conflict and impact analysis

- **3.1** [x] PRD core goal remains unchanged. One overlapping FR status range is corrected; D1 remains human-owned.
- **3.2** [x] Architecture changes are fully specified in section 5.
- **3.3** [x] Current UX spines remain authoritative; legacy UX inputs become historical only. NFR37 and surface semantics drive Epic 32.
- **3.4** [x] Secondary impacts are coverage/trace maps, evidence matrices, README walkthrough evidence, benchmark fixtures, and later tracking. No implementation occurs here.

### 4. Path-forward evaluation

- **4.1** [x] Direct adjustment alone: not viable because new epics/stories and evidence ownership are required. Effort High; risk High if historical stories are reopened.
- **4.2** [x] Rollback: not viable. It would discard completed evidence and does not restore current contract alignment.
- **4.3** [x] MVP review: viable only as a date/capacity decision, not requirement reduction. D1 owns retain/reset.
- **4.4** [x] Selected hybrid correction: architecture, epics/stories, then sprint planning.

### 5. Proposal components

- **5.1** [x] Issue summary and evidence supplied.
- **5.2** [x] Epic/artifact impacts and exact adjustments supplied.
- **5.3** [x] Recommended path and alternatives supplied.
- **5.4** [x] MVP impact, dependencies, owners, phase exceptions, and story plan supplied.
- **5.5** [x] Exact downstream handoff supplied in section 16.

### 6. Final review and handoff

- **6.1** [x] Applicable checklist items completed in Batch mode.
- **6.2** [x] Proposal reviewed for internal consistency against the pinned intake.
- **6.3** [ ] Approval pending, including D1, D2, AD-14 phase qualification, and proposed exceptions.
- **6.4** [N/A] `sprint-status.yaml` modification is explicitly prohibited in this run.
- **6.5** [x] Ordered handoff is defined; no implementation handoff is authorized.

## 15. Approval conditions

This proposal is approvable only when all of the following are recorded:

1. Jerome completes D1 with exact dates.
2. Jerome and Murat complete D2 with a corpus steward and two named independent human reviewers.
3. Jerome and Winston approve the phase-qualified AD-14 wording and EX-P15-01, EX-P15-02, and EX-OP-01 dispositions.
4. The approver confirms that Stories 32.1–35.10 are unregistered reservations until each story-local Historical Context Classification, Slice Proof, and Epic AC Verification record passes.
5. The approver confirms that completed evidence is immutable history and that the current no-go posture remains in force.

After approval, do not skip or reorder the following handoff.

## 16. Exact ordered handoff

1. `$bmad-architecture` — apply section 5 to the final architecture spine: phase-qualify AD-14; bind and trace FR75, NFR37, and G6; add AD-20; remove superseded architecture as an active source; reconcile every alignment-gap disposition; record Jerome's D1 and architecture approvals. Return a final, hashable spine and no implementation changes.
2. `$bmad-create-epics-and-stories` — use only the authoritative baseline plus the corrected spine; apply section 6; preserve completed history; create Epics 32–35 and the independently implementable story contracts in sections 9–12; move superseded UX inputs to historical metadata; run the Historical Slice Scope Guard and story-local Epic AC Verification before registering any story at any status. Do not touch `sprint-status.yaml`.
3. `$bmad-sprint-planning` — after the architecture and epics handoffs are approved, validate the complete G6 ownership/evidence matrix, D1 dates, D2 people, story dependencies, and Epic 24 tracking drift; only then generate or repair sprint tracking and select work. Do not start implementation.
