---
project: Hexalith.Memories
date: 2026-05-19
author: Jerome (with Claude / bmad-correct-course)
trigger: implementation-readiness-report-2026-05-19.md
scope_classification: Minor
review_mode: Incremental
input_documents:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/planning-artifacts/implementation-readiness-report-2026-05-19.md
artifacts_modified:
  - _bmad-output/planning-artifacts/epics.md
artifacts_unchanged:
  - prd.md
  - architecture.md
  - ux-design-specification.md
---

# Sprint Change Proposal — 2026-05-19

## 1. Issue Summary

The 2026-05-19 Implementation Readiness Assessment passed on vision, requirements (74/74 FRs traced), UX alignment, and architecture, but flagged **8 issues across 3 categories** that block a clean broad-implementation pass:

- **Structural / scope governance** — operational tracks mixed with product epics, active MVP scope not explicit enough, CI gate physically sequenced after Phase 1.5.
- **Story sizing / execution risk** — Stories 1.5, 1.6, 8.5, 13.2, 15.6 are large bundles; only 1.6 had a sizing/scope-guard note; 13.2 already had Implementation Checkpoints.
- **Documentation / tooling hygiene** — historical story aliases (Story 0.0 ↔ 1.1), reserved keys (Story 8.3), and external deferred-artifact references must be handled deliberately.

### How discovered

`bmad-check-implementation-readiness` review of `epics.md`, `prd.md`, `architecture.md`, and `ux-design-specification.md` against the BMAD epic/story authoring standards. Findings written to `implementation-readiness-report-2026-05-19.md`.

### Evidence

- Report Section "Issue Count" lists 8 issues across 3 categories.
- File scan confirmed Story 1.5 lacks the sizing/scope-guard note that Story 1.6 has.
- File scan confirmed Story 0.2 lacks the Ownership Boundary that Story 0.1 has.
- File scan confirmed Story 8.5 has no slice plan despite covering 4 distinct deliverables.
- File scan confirmed Story 15.6 has no Implementation Checkpoints structure despite covering 15 patch findings.
- Implementation Readiness Boundary section was present but did not declare a pre-implementation CI preflight gate.

## 2. Impact Analysis

### Epic Impact

| Epic | Impact | Action |
|---|---|---|
| Epic 0 | Scope-creep risk in Story 0.2 | Add Ownership Boundary to Story 0.2 |
| Epic 1 | Story 1.5 sizing | Add Sizing Note + Historical Scope Guard to Story 1.5 |
| Epic 8 | Story 8.5 sizing | Add Sizing Note + Historical Scope Guard to Story 8.5 |
| Epic 11 | Sequencing visibility | Add pre-implementation CI preflight reference to Implementation Readiness Boundary (Story 11.1 content unchanged) |
| Epic 15 | Story 15.6 sizing | Add Implementation Checkpoints A/B/C/D to Story 15.6 |
| Epics 2, 3, 4, 5, 6, 7, 9, 10, 12, 13, 14, 16, 17 | No impact | No edits |

No epic added, removed, or renumbered. No story added, removed, or renumbered.

### Story Impact

| Story | Change Type | Summary |
|---|---|---|
| Story 0.2 | Add Ownership Boundary | Explicitly defers case status, activity, members, single-case ownership, case-scoped edges, cross-case search, deletion, annotations to Epic 3 / Story 5.4 |
| Story 1.5 | Add Sizing Note + Scope Guard | Documents 3 slices (RediSearch / Redis Vector / FalkorDB) for any future re-implementation, with observable-proof requirement |
| Story 8.5 | Add Sizing Note + Scope Guard | Documents 4 slices (instrumentation registration, Tier-3 hard assertion, Tier-2 registration tests, doc update) |
| Story 15.6 | Add Implementation Checkpoints | A: AppHost boot; B: submodule guard; C: health-check + DAPR templates; D: Scope-Override + regression coverage |

### Artifact Conflicts

- **PRD** — no conflict. 74/74 FR coverage unchanged. No edits.
- **Architecture** — no conflict. Components, contracts, integrations unchanged. No edits.
- **UX Design** — no conflict. Evidence Packet grammar and trust/state UX unchanged. No edits.
- **sprint-status.yaml** — no epic/story add/remove/renumber. No edits.
- **project-context.md** — no rule change. No edits.

### Technical Impact

None. All edits are planning-document text changes. No code, no schema, no API, no deployment, no infrastructure change.

## 3. Recommended Approach

### Path forward: **Direct Adjustment (Option 1)**

Modify `epics.md` only — 5 surgical edits that match the report's own recommended next steps.

### Path rationale

- The 8 issues are entirely about planning-document tightness, not requirements or vision.
- All edits are additive (new notes, scope guards, preflight reference); no AC removal, no contract change.
- No epic/story renumbering means no downstream tooling churn (sprint-status, deferred-work, traceability).
- Rejected: **Rollback** — story files for 1.5/1.6/etc are historical completed scope; rolling back creates rework without addressing the planning-control gap. **MVP Review** — MVP scope is already correctly defined (Epic 0-8); the failure is doc hygiene, not scope.

### Effort / risk / timeline

- **Effort:** Low — 5 in-place edits to one document.
- **Risk:** Low — no AC removal; all edits are additive notes that strengthen rather than relax existing constraints.
- **Timeline impact:** None for in-flight Epic 12 and underway Epics 13-16. The new CI preflight gate applies prospectively to any future Epic 1.x re-implementation; existing completed stories are unaffected because they are historical-completed scope.

## 4. Detailed Change Proposals

All five proposals were approved interactively in Incremental review mode and applied to `epics.md` on 2026-05-19.

### Edit 1 — Implementation Readiness Boundary: Pre-Implementation CI Preflight Gate

**File:** `_bmad-output/planning-artifacts/epics.md` — "Implementation Readiness Boundary" section.

**Before:** Section declared MVP=Epic 0-8, Phase 1.5=Epic 9-10, Epics 11-15 require explicit sprint selection, future web UI is non-MVP. No CI preflight reference.

**After:** Same content, restructured under explicit **Active MVP scope**, **Phase 1.5 fast-follow**, **Engineering/Operational Readiness Track**, **Future web UI** subsections. New subsection **Pre-Implementation CI Preflight Gate (2026-05-19)** requires the minimum-CI subset of Story 11.1 (PR build, `dotnet build` with `TreatWarningsAsErrors=true`, Tier-1 unit-test execution) before any Story 1.2+ data-writing product work begins. The rest of Story 11.1 (semantic release hardening, branch protection, release run integrity) remains in the Engineering/Operational Readiness Track.

**Justification:** Addresses report findings 2 ("Active scope explicit") and 3 ("CI physically sequenced late") in one place without moving Story 11.1.

### Edit 2 — Story 1.5: Sizing Note + Historical Scope Guard

**File:** `_bmad-output/planning-artifacts/epics.md` — Story 1.5 header block.

**Before:** Title, user-story statement, ACs, "Validation Evidence Required" note, and "Observable Proof Gate" note. No sizing/decomposition note.

**After:** Adds **Sizing note** documenting three slices — (a) syntactic indexing on RediSearch, (b) semantic indexing on Redis Vector, (c) graph indexing on FalkorDB (including edge writes, `IGraphQueryBuilder` enforcement, and tenant lifecycle separation). Adds **Historical Scope Guard** matching the Story 1.6 pattern: any reopen requires separate numeric stories per slice with observable proof.

**Justification:** Addresses report finding 4 (Story 1.5 oversized). Mirrors the existing Story 1.6 pattern for consistency.

### Edit 3 — Story 0.2: Ownership Boundary

**File:** `_bmad-output/planning-artifacts/epics.md` — Story 0.2, between final AC and Traceability line.

**Before:** Story 0.2 had only a one-line `Traceability` note pointing to Story 3.1. No scope-creep guard.

**After:** Adds **Ownership Boundary** mirroring Story 0.1: Story 0.2 delivers only minimal case creation, listing, and the case-node-in-graph requirement. Explicitly defers case status, activity history, member management, single-case ownership enforcement, case-scoped graph edges, cross-case search, deletion, and annotation work to Epic 3 (Stories 3.1-3.6) and Story 5.4.

**Justification:** Addresses report finding 7 (protect Epic 0 from scope creep). Parallels Story 0.1.

### Edit 4 — Story 8.5: Sizing Note + Historical Scope Guard

**File:** `_bmad-output/planning-artifacts/epics.md` — Story 8.5 header block.

**Before:** Title, user-story statement, "Outcome summary", and ACs covering multiple distinct deliverables. No sizing/decomposition note.

**After:** Adds **Sizing note** documenting four slices — (a) instrumentation registration with keyed `IConnectionMultiplexer` resolution, (b) Tier-3 end-to-end trace hard assertion replacing the soft-skip helper, (c) Tier-2 registration tests, (d) `docs/dev/telemetry.md` update. Adds **Historical Scope Guard** matching the Stories 1.5/1.6 pattern.

**Justification:** Addresses report finding 6 (Story 8.5 oversized).

### Edit 5 — Story 15.6: Implementation Checkpoints

**File:** `_bmad-output/planning-artifacts/epics.md` — Story 15.6 header block.

**Before:** Title, user-story statement, and ACs covering 15 patch findings across AppHost orchestration, submodule guard, health checks, DAPR component templates, Scope-Override for Story 1.1, and regression coverage. No checkpoint structure.

**After:** Adds **Implementation Checkpoints** mirroring Story 13.2 — Checkpoint A (AppHost boot orchestration), Checkpoint B (Submodule guard expansion), Checkpoint C (Health-check and DAPR-template hardening), Checkpoint D (Story 1.1 Scope-Override and regression coverage). Story remains one tracked unit for Epic 15 sequencing; review must close each checkpoint independently. Per the Engineering/Operational Readiness Track preamble, individual checkpoints that cannot be applied may be deferred or accepted with rationale.

**Justification:** Addresses report finding 6 (Story 15.6 oversized). Mirrors the Story 13.2 Implementation Checkpoints pattern.

## 5. PRD MVP Impact and Action Plan

### MVP impact

**None.** MVP scope remains Epic 0 through Epic 8. FR coverage remains 100% traceable (74/74, FR71 Phase-2 deferred unchanged). MVP go/no-go hard gates (three-axis validation 80%, zero cross-tenant leak, sub-30-minute onboarding) remain unchanged. Soft gates (causal chain completeness ≥95%, MCP end-to-end, case-model correctness) remain unchanged.

### Action plan

| # | Action | Owner | Status |
|---|---|---|---|
| 1 | Apply 5 edits to `epics.md` | Claude / bmad-correct-course | ✅ Done 2026-05-19 |
| 2 | Write Sprint Change Proposal | Claude / bmad-correct-course | ✅ Done 2026-05-19 |
| 3 | Optional re-run of `bmad-check-implementation-readiness` against revised Epic 0-2 path | Jerome / next planning session | Pending |
| 4 | When Epic 1.x re-implementation or new Epic 1-equivalent work is queued, ensure CI preflight is in place first | Implementation Developer | Pending (prospective) |
| 5 | Preserve Evidence Packet grammar in any future stories shaping CLI/MCP/web output contracts | Implementation Developer | Ongoing (carries from prior guidance) |

### Dependencies / sequencing

- The CI preflight gate is prospective — it applies to any future Epic 1.x re-implementation or restarted product-capability sequence. In-flight Epic 12 work and underway Epics 13-16 are unaffected because they are operational-readiness, not Epic 1.x product capability.
- No sprint-status.yaml change is required; no epic or story was added, removed, or renumbered.

## 6. Implementation Handoff

### Scope classification: **Minor**

All edits are document text changes inside `epics.md`. No code, schema, contract, infrastructure, or CI workflow change is part of this proposal. The proposal can be executed by a single Developer agent (already done).

### Handoff recipients

| Role | Responsibility | Deliverable |
|---|---|---|
| **Developer agent** (this run) | Apply 5 edits to `epics.md`; write Sprint Change Proposal | ✅ Both done 2026-05-19 |
| **Jerome (Maintainer)** | Review and merge the change; optionally re-run readiness check | Confirmation merge / optional re-validation |
| **Future Implementation agents** | Honor the new CI preflight gate before Epic 1.x re-implementation; honor sizing notes if any of Story 1.5 / 8.5 / 15.6 is reopened | Compliance during execution |

### Success criteria

- ✅ Implementation Readiness Boundary section now declares active MVP scope explicitly and references CI preflight.
- ✅ Stories 0.2, 1.5, 8.5, 15.6 carry scope-discipline notes matching the Story 0.1 / 1.6 / 13.2 patterns already in the document.
- ✅ No PRD/architecture/UX change required; no FR coverage change.
- 🔜 Optional: re-run `bmad-check-implementation-readiness` confirms the 8 issues drop to zero, or marks any that remain as accepted-with-rationale.

## 7. Reference

- Triggering report: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-19.md`
- Affected document: `_bmad-output/planning-artifacts/epics.md`
- Prior precedent: `sprint-change-proposal-2026-05-18-epic-1-observable-proof-guard.md` (same incremental-edit pattern applied to Epic 1)
- Memory: `feedback_review_autonomy.md`, `feedback_scope_override_pattern.md`, `project_release_readiness.md`

**Proposal approved:** Pending Jerome's final yes/no.
**Author / Executor:** Claude Code (`bmad-correct-course` workflow).
