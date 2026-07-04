---
change_trigger: "_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-04.md"
mode: batch
status: approved-and-applied
project: Hexalith.Memories
date: 2026-07-04
scope_classification: moderate
---

# Sprint Change Proposal: Implementation Readiness Cleanup

**Date:** 2026-07-04
**Project:** Hexalith.Memories
**Trigger:** Implementation Readiness Assessment `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-04.md`
**Status:** Approved by Jerome on 2026-07-04 and applied
**Mode:** Batch
**Change Scope:** Moderate - backlog and planning-artifact reorganization. No PRD requirement rewrite, product scope reduction, or architecture reset is proposed.

---

## 1. Issue Summary

The 2026-07-04 implementation readiness assessment found that the core planning artifacts are present and materially aligned:

- PRD, architecture, epics/stories, UX specification, and sprint change proposals exist.
- PRD functional requirement coverage is complete: 74/74 FRs are traceable in the epics document.
- UX alignment is sound: the Evidence Packet model, scope-first trust model, and FrontComposer/Fluent UI Blazor V5 boundary are documented.
- Active MVP Epic 0-8 is broadly coherent and can remain the product-readiness boundary.

The blocker is implementation discipline, not missing requirements. Broad implementation should wait until sequencing, readiness-accounting, story-sizing, and UX-gating rules are enforceable by tooling and sprint-status data rather than only explained in prose.

Concrete evidence from the readiness report:

1. Story execution order conflicts with story numbering or sprint-status order:
   - Story 17.6 is the FrontComposer/Fluent UI V5 conformance gate for Stories 17.2-17.5.
   - Story 18.5 consumes the stability contract in Story 18.6.
   - Story 23.1 consumes the provider batch API in Story 23.9.
2. Operational and technical epics are mixed into the same epic list as product capability epics. The document labels them, but readiness accounting is not yet machine-readable enough to prevent accidental MVP-count inflation.
3. Future web UX implementation must treat Story 17.6 as a hard preflight before any further Epic 17 expansion.
4. Historical broad stories and checkpoint-heavy operational stories need guardrails before they are reopened or selected for new implementation.
5. Epics 20-26 cite concrete code anchors from the 2026-07-04 architecture audit, but those anchors can move before the stories are implemented.
6. Epics frontmatter currently names only the latest sprint change proposals even though older approved proposals are materially incorporated.

## 2. Impact Analysis

### Epic Impact

**Epic 0-8:** No scope change. These remain the only active MVP readiness epics unless a later sprint change explicitly selects additional work. Story 0.4 remains the minimum build/test CI preflight before any Epic 1 data-writing work.

**Epic 17:** No product scope expansion. Story 17.6 must become a machine-readable preflight for any future Epic 17 story work. Existing done history can remain intact, but tooling must not select future web stories using numeric order alone.

**Epic 18:** No completed-history rewrite. Preserve the existing story keys because Epic 18 is done and externally traceable. Add sequencing metadata that declares 18.6 before 18.5 for execution and validation semantics.

**Epic 23:** This epic is still backlog. Preferred cleanup is to make provider strategy execution precede content chunking through machine-readable sequencing immediately. Renumbering is optional, but not required if tooling consumes the execution-order metadata.

**Epics 11-16, 19, 21, 24-26:** Keep as Engineering/Operational Readiness or remediation tracks. They may be implemented when sprint-selected, but they must not count as product capability completion for MVP readiness.

### Story Impact

Affected stories:

- `17.6` must gate any future `17.2`, `17.3`, `17.4`, or `17.5` work.
- `18.6` must be declared as the prerequisite for `18.5` without renumbering done history.
- `23.9` must be declared as the prerequisite for `23.1`.
- Historical `1.2`, `1.5`, and `1.6` must not be reopened as broad implementation units.
- Checkpoint-heavy stories such as `21.9` and `26.5` need separately evidenced implementation slices before they can be accepted as done.
- Epics `20` through `26` need a shared audit-anchor preflight before any story file is created or implemented.

### Artifact Conflicts

**PRD:** No requirement change. FR71 remains covered with a scope caveat: operational backup/restore is covered by Epic 26, while broader application-facing portable export remains Phase 2 unless sprint-selected.

**Architecture:** No architecture decision change. The architecture already phase-filters active MVP, Phase 1.5, and ongoing operational hardening. The proposed change is to make readiness accounting enforce that phase split.

**UX Design:** No UX spec rewrite. The UX document already requires FrontComposer + Fluent UI Blazor V5 and states future web work is not MVP unless sprint-selected. The proposed change makes Story 17.6 an enforceable gate in epics and sprint status.

**Epics:** Changes required. Add machine-readable readiness metadata, sequencing metadata, provenance context, and an Epic 20-26 audit-anchor preflight.

**Sprint status:** Changes required. Add machine-readable readiness accounting and story execution order; reorder or annotate affected story rows so tooling does not choose numeric order where dependencies disagree.

**Project knowledge/docs:** Optional. If change-control provenance needs long-term automation, create or reference a canonical change-control index rather than relying on frontmatter lists that drift.

### Technical Impact

No production code change is required for the planning cleanup itself. If the guardrails are automated, the likely technical touchpoints are:

- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/epics.md`
- Optional `_bmad/scripts/validate_readiness_accounting.py` or equivalent BMAD validation script

No C# code, UI code, submodule update, or data persistence change is proposed by this sprint change.

## 3. Recommended Approach

**Recommended path: Direct Adjustment with machine-readable planning metadata.**

Do not renumber completed story history in Epic 17 or Epic 18. Renumbering done stories would damage traceability and create unnecessary churn. Instead, preserve story keys and add explicit execution-order metadata consumed by story tooling.

For Epic 23, either approach is acceptable because the epic is still backlog:

- Preferred low-churn option: keep keys and declare execution order `23.9 -> 23.1 -> 23.2...`.
- Optional stricter option: renumber provider strategy to `23.1` and shift chunking/resilience stories. This is cleaner numerically but creates broader key churn.

This proposal recommends the low-churn option because it handles all three sequencing defects with one consistent mechanism.

Effort estimate: **Low to Medium** for planning artifacts; **Medium** if validation script support is added immediately.

Risk level: **Low**. The risk is mostly tooling drift. The proposal intentionally avoids product scope changes and completed-history renumbering.

Timeline impact: Broad implementation should pause until this metadata is in place. Once approved, applying the planning edits should be a short cleanup pass.

## 4. Detailed Change Proposals

### 4.1 Epics Frontmatter: Change-Control Provenance

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: YAML frontmatter

OLD:

```yaml
inputDocuments:
  - '_bmad-output/planning-artifacts/prd.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/ux-design-specification.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01.md'
```

NEW:

```yaml
inputDocuments:
  - '_bmad-output/planning-artifacts/prd.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/ux-design-specification.md'
  - '_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-04.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01.md'
changeControlContext:
  approvedProposalGlob: '_bmad-output/planning-artifacts/sprint-change-proposal-*.md'
  note: 'The latest frontmatter inputs are not the full change-control history. Approved sprint-change proposals are discovered through the glob unless a canonical index replaces it.'
```

Rationale: The readiness report found that older sprint-change proposals are materially incorporated. This makes provenance discoverable without listing every historical proposal in frontmatter.

### 4.2 Sprint Status: Readiness Accounting Metadata

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Section: add top-level metadata before `development_status`

OLD:

```yaml
project: Hexalith.Memories
project_key: NOKEY
tracking_system: file-system
story_location: _bmad-output/implementation-artifacts

development_status:
```

NEW:

```yaml
project: Hexalith.Memories
project_key: NOKEY
tracking_system: file-system
story_location: _bmad-output/implementation-artifacts

readiness_accounting:
  active_mvp_epics:
    - epic-0
    - epic-1
    - epic-2
    - epic-3
    - epic-4
    - epic-5
    - epic-6
    - epic-7
    - epic-8
  excluded_unless_sprint_selected:
    - epic-9
    - epic-10
    - epic-11
    - epic-12
    - epic-13
    - epic-14
    - epic-15
    - epic-16
    - epic-17
    - epic-18
    - epic-19
    - epic-20
    - epic-21
    - epic-22
    - epic-23
    - epic-24
    - epic-25
    - epic-26
  default_story_inherits_epic_metadata: true
  epic_metadata:
    epic-0: { track: product, phase: MVP, mvpReadiness: included }
    epic-1: { track: product, phase: MVP, mvpReadiness: included }
    epic-2: { track: product, phase: MVP, mvpReadiness: included }
    epic-3: { track: product, phase: MVP, mvpReadiness: included }
    epic-4: { track: product, phase: MVP, mvpReadiness: included }
    epic-5: { track: product, phase: MVP, mvpReadiness: included }
    epic-6: { track: product, phase: MVP, mvpReadiness: included }
    epic-7: { track: product, phase: MVP, mvpReadiness: included }
    epic-8: { track: product, phase: MVP, mvpReadiness: included }
    epic-9: { track: product, phase: P1.5, mvpReadiness: excluded }
    epic-10: { track: product, phase: P1.5, mvpReadiness: excluded }
    epic-11: { track: operational, phase: operational-readiness, mvpReadiness: excluded }
    epic-12: { track: operational, phase: post-MVP, mvpReadiness: excluded }
    epic-13: { track: operational, phase: post-MVP, mvpReadiness: excluded }
    epic-14: { track: operational, phase: operational-readiness, mvpReadiness: excluded }
    epic-15: { track: operational, phase: operational-readiness, mvpReadiness: excluded }
    epic-16: { track: operational, phase: operational-readiness, mvpReadiness: excluded }
    epic-17: { track: future-ui, phase: future-web-ui, mvpReadiness: excluded }
    epic-18: { track: operational, phase: downstream-consumer-hardening, mvpReadiness: excluded }
    epic-19: { track: operational, phase: deferred-register-governance, mvpReadiness: excluded }
    epic-20: { track: remediation, phase: audit-remediation, mvpReadiness: excluded }
    epic-21: { track: remediation, phase: audit-remediation, mvpReadiness: excluded }
    epic-22: { track: remediation, phase: audit-remediation, mvpReadiness: excluded }
    epic-23: { track: remediation, phase: audit-remediation, mvpReadiness: excluded }
    epic-24: { track: remediation, phase: audit-remediation, mvpReadiness: excluded }
    epic-25: { track: remediation, phase: audit-remediation, mvpReadiness: excluded }
    epic-26: { track: remediation, phase: audit-remediation, mvpReadiness: excluded }
  story_overrides:
    8-3-data-export: { track: product, phase: Phase2, mvpReadiness: reserved-non-mvp }
    18-4-stable-ingest-contract-with-explicit-idempotency-token-and-atomic-dedup: { releaseSensitive: true }

development_status:
```

Rationale: This prevents operational, remediation, Phase 1.5, and future UI work from being counted as active MVP readiness by accident.

### 4.3 Sprint Status: Story Execution Order

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Section: add top-level metadata before `development_status`

NEW:

```yaml
story_execution_order:
  epic-17:
    reason: 'Story 17.6 is the conformance gate for future Epic 17 web work.'
    order:
      - 17-6-frontcomposer-fluent-ui-blazor-v5-conformance-hardening
      - 17-2-recovery-and-feedback-state-grammar
      - 17-3-contract-aware-web-interaction-patterns
      - 17-4-role-specific-web-inspection-lenses
      - 17-5-responsive-and-accessible-web-validation
  epic-18:
    reason: 'Story 18.5 consumes the stability contract produced by Story 18.6; completed keys are preserved for traceability.'
    order:
      - 18-6-memory-unit-id-stability-contract
      - 18-5-source-uri-keyed-memory-unit-lookup-endpoint
  epic-23:
    reason: 'Story 23.1 content chunking consumes the provider batch API from Story 23.9.'
    order:
      - 23-9-embeddingclient-provider-strategy
      - 23-1-content-chunking-and-batch-embedding
      - 23-2-claim-check-workflow-payloads
      - 23-3-retry-after-aware-429-orchestration
      - 23-4-non-url-re-ingestion
      - 23-5-rate-limiter-admission-simplification
      - 23-6-directory-batch-scalability
      - 23-7-index-provisioning-ownership
      - 23-8-workflow-config-determinism
```

Rationale: This is the least disruptive fix for the no-forward-dependency issue. Tooling can consult this field before choosing the next story.

### 4.4 Epics: Implementation Readiness Boundary Update

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `## Implementation Readiness Boundary`

OLD:

```markdown
**Active MVP scope:** Epic 0 through Epic 8 are the only epics in active MVP implementation readiness. Any work outside this set must be explicitly sprint-selected and must not be pulled into MVP completion accounting by accident.
```

NEW:

```markdown
**Active MVP scope:** Epic 0 through Epic 8 are the only epics in active MVP implementation readiness. Any work outside this set must be explicitly sprint-selected and must not be pulled into MVP completion accounting by accident.

**Machine-readable accounting:** `_bmad-output/implementation-artifacts/sprint-status.yaml` owns readiness metadata under `readiness_accounting`. Readiness reports and story tooling must use that metadata rather than inferring MVP readiness from story status, numeric ordering, or FR coverage alone.
```

Rationale: Keeps the prose boundary aligned with sprint-status metadata.

### 4.5 Epics: Story Execution Order Policy

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `### Story Key Policy`

OLD:

```markdown
New story keys must use numeric `Epic.Story` format. Alphabetic suffixes are allowed only as historical aliases during migration and must not be introduced for new work unless story tooling explicitly supports them and a sprint change approves the exception.
```

NEW:

```markdown
New story keys must use numeric `Epic.Story` format. Alphabetic suffixes are allowed only as historical aliases during migration and must not be introduced for new work unless story tooling explicitly supports them and a sprint change approves the exception.

When completed history or external traceability prevents renumbering, execution order must be declared in `_bmad-output/implementation-artifacts/sprint-status.yaml` under `story_execution_order`. Story tooling must honor `story_execution_order` before numeric key order. Do not create a story file for a story whose declared prerequisite in that execution-order list is incomplete unless a sprint change explicitly approves the exception.
```

Rationale: This solves Epic 17, Epic 18, and Epic 23 ordering without destroying completed story history.

### 4.6 Epic 17: Hard Preflight Note

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `## Epic 17: Future Web UX Composition & Accessibility`

OLD:

```markdown
**Readiness note:** Story 17.6 is the current conformance gate for the host-less web RCL. It does not close product-route browser, axe, forced-colors, reduced-motion, zoom/reflow, touch, or manual screen-reader validation gaps; those remain future web-host validation work and must be sprint-selected separately.
```

NEW:

```markdown
**Readiness note:** Story 17.6 is the current conformance gate for the host-less web RCL. It does not close product-route browser, axe, forced-colors, reduced-motion, zoom/reflow, touch, or manual screen-reader validation gaps; those remain future web-host validation work and must be sprint-selected separately.

**Execution gate:** Any future Epic 17 implementation or reopened Story 17.2-17.5 work must verify Story 17.6 completion evidence first and must reuse its conformance tests. If `story_execution_order` is present in sprint status, tooling must treat 17.6 as the preflight regardless of numeric suffix.
```

Rationale: Matches the UX instructions and the approved 2026-06-24 FrontComposer/Fluent UI V5 course correction.

### 4.7 Epic 23: Provider Strategy Preflight

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `## Epic 23: Ingestion Pipeline Scalability & Resilience`

OLD:

```markdown
### Story 23.9: EmbeddingClient Provider Strategy
...
### Story 23.1: Content Chunking & Batch Embedding
```

NEW:

```markdown
**Execution note:** Story 23.9 must execute before Story 23.1 because content chunking depends on the provider batch API. The numeric keys are preserved to avoid unnecessary backlog churn; sprint-status `story_execution_order.epic-23` is authoritative for story selection.

### Story 23.9: EmbeddingClient Provider Strategy
...
### Story 23.1: Content Chunking & Batch Embedding
```

Rationale: Makes the dependency explicit at the epic section level and ties it to machine-readable sprint-status metadata.

### 4.8 Historical Broad Story Guardrail

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: after existing historical scope guards for Stories 1.2, 1.5, and 1.6

NEW:

```markdown
**Readiness tooling guard:** Do not create a new implementation story file that reuses this broad historical story key for new work. New implementation must use newly numbered split stories with externally observable completion evidence.
```

Rationale: The prose already warns against reopening these stories. This makes the intended tooling behavior explicit.

### 4.9 Operational Checkpoint Acceptance Guardrail

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `### Engineering/Operational Readiness Track`

OLD:

```markdown
Operational checkpoint stories may remain umbrella tracking stories, but each checkpoint must be implemented, reviewed, and evidenced as a separately verifiable slice. A checkpoint cannot be marked complete only because the umbrella story is complete.
```

NEW:

```markdown
Operational checkpoint stories may remain umbrella tracking stories, but each checkpoint must be implemented, reviewed, and evidenced as a separately verifiable slice. A checkpoint cannot be marked complete only because the umbrella story is complete.

When checkpoint-heavy stories are selected for implementation, the story file must either split checkpoints into separately tracked child story files or include a checklist evidence table with owner, validation command or artifact, review status, and completion date for each checkpoint. This guard applies especially to future or backlog stories such as 21.9 and 26.5.
```

Rationale: Prevents a large umbrella story from reaching done without independently reviewable evidence.

### 4.10 Epics 20-26: Audit-Anchor Preflight

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `## Phase: Post-MVP — Audit Remediation (2026-07-04)`

NEW:

```markdown
**Audit-anchor preflight:** Before any Epic 20-26 story is selected, created, or implemented, re-verify the current code anchors and implementation-state assumptions cited by that story against the repository. Story files must record the re-verification date, moved or renamed anchors, and how the implementation adapts. If an anchor is stale enough to change scope or acceptance evidence, update the story from current code evidence before development begins.
```

Rationale: The architecture-audit remediation stories cite line numbers, files, and current implementation states. This prevents stale anchors from sending implementation work to the wrong code path.

### 4.11 Optional Validation Script

Artifact: `_bmad/scripts/validate_readiness_accounting.py` or equivalent

NEW behavior:

```text
Validate that:
1. Every `development_status` epic has `readiness_accounting.epic_metadata`.
2. Active MVP readiness includes only Epic 0-8 unless an explicit sprint-selected override exists.
3. Product MVP stories do not contain "accepted or carried forward" completion language.
4. `story_execution_order` prerequisites are complete before later stories are selected for `ready-for-dev`.
5. Reserved non-MVP items such as Story 8.3 are not reported as missing active MVP stories.
6. The Epics 20-26 audit-anchor preflight is present.
```

Rationale: Metadata is useful only if readiness checks consume it. This script can be added now or tracked as a follow-up in Epic 26.

## 5. Change Navigation Checklist Results

| Item | Status | Notes |
|---|---|---|
| 1.1 Triggering story | [N/A] | Trigger is an implementation-readiness assessment, not a single implementation story. |
| 1.2 Core problem | [x] | Planning artifacts are complete, but sequencing, readiness accounting, story sizing, and UX gating are not enforceable enough. |
| 1.3 Evidence gathered | [x] | Readiness report, PRD, architecture, UX spec, epics, sprint status, prior sprint change proposals, and Hexalith UX rules reviewed. |
| 2.1 Current epic impact | [x] | Active MVP Epic 0-8 remains viable; post-MVP and future UI tracks need enforceable boundaries. |
| 2.2 Epic-level changes | [x] | Add readiness metadata, sequencing metadata, and guardrails. |
| 2.3 Remaining epics impact | [x] | Epic 17, 18, 23 directly impacted; operational/remediation epics impacted by accounting metadata and audit-anchor preflight. |
| 2.4 New or obsolete epics | [N/A] | No new epic required. Optional validation can be absorbed into Epic 26 or direct tooling cleanup. |
| 2.5 Priority/order | [x] | Apply metadata cleanup before broad implementation resumes. |
| 3.1 PRD conflicts | [x] | No PRD change; FR71 scope caveat preserved. |
| 3.2 Architecture conflicts | [x] | No architecture decision change; architecture phase split reinforced through readiness metadata. |
| 3.3 UI/UX conflicts | [x] | No UX rewrite; Story 17.6 gate made enforceable. |
| 3.4 Other artifacts | [x] | `epics.md` and `sprint-status.yaml` require edits; optional validation script recommended. |
| 4.1 Direct adjustment | [x] | Viable. Low/medium effort, low risk. |
| 4.2 Rollback | [N/A] | Renumbering completed story history or reverting artifacts is not justified. |
| 4.3 PRD MVP review | [N/A] | MVP scope remains achievable; no requirement reset needed. |
| 4.4 Recommended path | [x] | Direct adjustment via metadata and guardrails. |
| 5.1 Issue summary | [x] | Included. |
| 5.2 Impact summary | [x] | Included. |
| 5.3 Path forward | [x] | Included. |
| 5.4 MVP impact/action plan | [x] | MVP boundary unchanged; action plan targets planning artifacts. |
| 5.5 Handoff plan | [x] | Included below. |
| 6.1 Checklist completion | [x] | All applicable items covered. |
| 6.2 Proposal accuracy | [x] | Draft cross-checked against local artifacts. |
| 6.3 User approval | [x] | Approved by Jerome on 2026-07-04. |
| 6.4 Sprint status update | [x] | Readiness metadata and execution-order metadata applied to sprint status. |
| 6.5 Handoff plan | [x] | Included below. |

## 6. Implementation Handoff

**Scope classification:** Moderate.

This is not a Developer-only implementation story and not a PM/Architect replan. It is a Product Owner / Developer backlog-governance cleanup:

- **Product Owner:** Approve the readiness-accounting model and decide whether Epic 23 should preserve keys with `story_execution_order` or be renumbered before any story files exist.
- **Developer:** Apply the approved edits to `epics.md` and `sprint-status.yaml`. If approved, add or update the readiness validation script.
- **Test Architect / QA:** Ensure the next readiness assessment consumes `readiness_accounting` and `story_execution_order`; verify that product MVP status excludes operational/remediation/future-ui tracks unless explicitly sprint-selected.

Success criteria:

1. The readiness report no longer flags 17.6, 18.5/18.6, or 23.1/23.9 as ambiguous for tooling.
2. Active MVP accounting is mechanically locked to Epic 0-8 unless an explicit sprint-selected override exists.
3. Operational/remediation/future-ui work can remain in the same epics file without inflating product readiness.
4. Future web work cannot bypass Story 17.6 conformance evidence.
5. Reopened broad historical stories and selected checkpoint-heavy operational stories require split/evidenced implementation slices.
6. Epics 20-26 cannot start from stale audit anchors without current-code re-verification.

## 7. Approval

- [x] Approved by Jerome - 2026-07-04
- [x] `epics.md` metadata and guardrail edits applied
- [x] `sprint-status.yaml` readiness metadata and execution-order edits applied
- [x] Optional readiness validation script added: `_bmad/scripts/validate_readiness_accounting.py`
- [x] Epic 20-26 audit-anchor preflight applied and covered by the readiness validation script

Routed to Product Owner / Developer agents for future planning-artifact governance. The implementation-readiness metadata update pass is complete.
