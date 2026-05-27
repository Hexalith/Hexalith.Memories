# Sprint Change Proposal — Post-MVP Transition (Roadmap Exhaustion)

**Date:** 2026-04-26
**Triggered by:** Epic 11 retrospective (`_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md`)
**Sprint impact:** Project-wide — all 11 epics + planning artifacts + post-MVP direction
**Mode:** Batch (per Jerome's "resolve decisions yourself + summarize" feedback)
**Scope classification:** **Major** — strategic decision point + multi-artifact housekeeping

---

## Section 1: Issue Summary

**Problem statement:** Epic 11 (CI/CD & Automated Quality Pipeline) closed on 2026-04-26 with both stories `done`. Epic 11 is the **last epic defined in `_bmad-output/planning-artifacts/epics.md`**. The MVP roadmap is now exhausted at the planning level, but the retrospective surfaced that:

1. **The release path has never been exercised.** CI shipped, `release.yml` shipped, 7 NuGet packages packed and validated, but no real release has run. Two external maintainer actions (A1 branch protection, A2 `NUGET_API_KEY` secret) still gate first publish.
2. **Project state is inconsistent.** 10 of 11 epics still show `epic-N: in-progress` in `sprint-status.yaml` despite every story under them being `done` for days or weeks. Sprint-status workflow notes explicitly require manual `in-progress → done` transitions; that housekeeping was never done.
3. **Five retro action items + 4 deferred-work tracking items + 3 team agreements need operationalizing.** The retrospective surfaced them but did not commit them to artifacts the team can execute against.
4. **Six systemic patterns need governance.** D5 file-scope leak, baseline failures hiding under script-only execution, 4 tolerance-idiom silent failures, sibling-file convention drift, status-checkbox/reality gap (W10), untracked `package-lock.json` (P1).
5. **No Phase 2 / Epic 12 / "what comes next" exists.** The PRD ends at MVP. The deferred-work backlog has ~40+ entries with "Phase 2 candidate" or "re-open trigger: production observation" pointers, but no consolidated next-phase plan.

**Issue category:** **Strategic pivot** (MVP roadmap completion → next-phase decision required) **+ housekeeping debt** (sprint-status drift) **+ governance gaps** (no enforcement for retro patterns).

**Evidence:**
- `_bmad-output/planning-artifacts/epics.md` line 1841 = last epic header (Epic 11); no Epic 12 exists
- `_bmad-output/implementation-artifacts/sprint-status.yaml` lines 70/82/94/104/113/125/144/155/162/170 = 10 `epic-N: in-progress` entries with all sub-stories `done`
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md` = action items A1-A5, deferred work S11-FA/B/C/D, team agreements, post-MVP transition discussion
- `_bmad-output/implementation-artifacts/deferred-work.md` lines 1-100 = ~40 active deferred items, several explicitly tagged "Phase 2 candidate"

---

## Section 2: Impact Analysis

### Epic Impact

| Epic | Current status | Required action | Rationale |
|---|---|---|---|
| 1, 2, 3, 4, 5, 6 | `in-progress` (all stories done) | Flip → `done` | All stories under each epic are `done`. Manual closeout required by workflow notes. |
| 7 | `done` | None | Already correctly closed. |
| 8, 9, 10, 11 | `in-progress` (all stories done) | Flip → `done` | All stories under each epic are `done`. Manual closeout required by workflow notes. |
| **12 (proposed)** | **Does not exist** | **Decision required** — see Section 3 | Operations Epic to ship first release + close retro action items, OR Phase 2 features, OR no Epic 12 at all (declare MVP-done + operations pivot only) |

### Story Impact

No existing stories are modified or invalidated. **New stories may be created** depending on Section 3 directional decision (see proposed Epic 12 scaffolds).

### Artifact Conflicts

| Artifact | Sections Affected | Severity | Action |
|---|---|---|---|
| **`sprint-status.yaml`** | 10 `epic-N: in-progress` entries; `last_updated` comment chain | **Mechanical** | Flip 10 statuses + add comment line documenting bulk closeout |
| **`epics.md`** | After line 365 (Epic 11 description) and after line 1841 (Epic 11 detail block) | **Low** | Add `Phase: Post-MVP` decision-point marker; *optionally* scaffold Epic 12 pending Section 3 direction |
| **`prd.md`** | None | None | The PRD scope was MVP. A Phase 2 PRD addendum becomes required only if Section 3 selects the Phase 2 path. |
| **`architecture.md`** | None | None | No architecture changes from the retro. ADRs may need updates if Phase 2 introduces per-tenant LLM config, projection registry, etc. — gated on Section 3 direction. |
| **`product-brief-…md`** | None | None | Brief was MVP-scoped; Phase 2 brief is a new artifact if needed. |
| **`deferred-work.md`** | None directly; ~40 entries become candidate Phase 2 inputs | None | No edit — file remains source-of-truth. Phase 2 PRD (if pursued) consolidates relevant entries. |

### Technical Impact

- **No code changes from this proposal directly.** All housekeeping is metadata-only.
- **Two pending external maintainer actions** continue to gate first release: A1 (branch protection in GitHub UI), A2 (`NUGET_API_KEY` secret in GitHub UI).
- **Optional code changes** if Section 3 selects Operations Epic 12: implementing A3/A4/A5/S11-FA/S11-FD as concrete stories.

### Documentation Impact

| Artifact | Action |
|---|---|
| `MEMORY.md` (auto-memory) | Already updated by retro: `project_release_readiness.md`, `feedback_tolerance_idioms.md` |
| `CONTRIBUTING.md` | A3 retro action — add "Forbidden Default Tolerances" checklist (gated on Section 3) |
| `docs/dev/branch-protection.md` | Already covers A1; update on A1 completion to record applied date |
| `docs/dev/release-runbook.md` (NEW) | Optional D2 retro action — first-release runbook (deferred until first release is attempted) |

---

## Section 3: Recommended Path Forward

### Options Evaluated

| # | Option | Effort | Risk | Decision Latency | Best for |
|---|---|---|---|---|---|
| **A** | **Operations Epic 12 only** — ship first release + operationalize all retro action items as stories. No Phase 2 yet. | Medium | Low | Decide today | Validating the release path before committing to feature work |
| **B** | **Phase 2 Feature Roadmap directly** — mine deferred-work backlog, draft Phase 2 PRD addendum, define Epic 12+ as feature epics | High | Medium | Decide today, deliver in 2-4 weeks | If feature roadmap is already mentally clear and release path is treated as low-risk |
| **C** | **Hybrid: Operations Epic 12 first, Phase 2 decision deferred** | Medium → High | Low → Medium | Decide Operations now, Phase 2 after first release | **Recommended.** Ship operations, learn from real release, then decide Phase 2 with data |
| **D** | **MVP-done declaration + operations pivot only** — no Epic 12, no Phase 2, only ad-hoc maintenance | Low | Low | Decide today | If Hexalith.Memories is feature-complete and the team's attention is moving elsewhere |

### Recommended: Option C — Hybrid

**Rationale:**

1. **The release path has never been exercised end-to-end.** Validating it should not wait on Phase 2 scoping. Any Phase 2 PRD written today would be based on assumptions the first release may invalidate.
2. **Several deferred-work items are explicitly gated on first-release observation** ("re-open trigger: first observation in production data"). Without first release, those re-open triggers can never fire — Phase 2 scoping for them is premature.
3. **Retro action items A1-A5 + S11-FA-D + 3 team agreements are operational by nature.** They map cleanly to Epic 12 stories without requiring a new PRD chapter.
4. **Risk asymmetry favors operations-first.** A bad first release is recoverable (`--skip-duplicate` self-heals — though S11-FD wants to add alerting). A wrong Phase 2 PRD wastes weeks of discovery.
5. **Decision latency for Phase 2 stays small.** Epic 12 (Operations) is estimated ~1-2 weeks of focused work. Phase 2 decision returns shortly with real signal from the release path.

**Trade-offs of recommendation:**
- **Cost:** Defers any Phase 2 feature exploration by ~1-2 weeks
- **Benefit:** Phase 2 (if pursued) can be scoped against real release-path data, real first-contributor feedback, and a known-green production state
- **Alternative cost if rejected:** Either ship a release-untested project that may surprise on first publish (Options B or D), or block on Phase 2 scoping while operational debt grows (Option B alone)

---

## Section 4: Detailed Change Proposals

### Change 4.1 — `sprint-status.yaml` mechanical closeout (apply now, no decision needed)

**Action:** Flip 10 epic statuses from `in-progress → done`. Each flip annotated with a brief inline comment pointing at the bulk closeout date.

**Rationale:** Workflow notes in `sprint-status.yaml` (lines 32-39) require manual `in-progress → done` epic transition once all sub-stories are `done`. Every flipped epic has 100% of its sub-stories already `done`. This is housekeeping debt, not a discretionary change.

**Affected entries:**

| Line | Epic | Stories under it | Last story `done` date |
|---|---|---|---|
| 70 | epic-1 | 7/7 done | (Phase 1) |
| 82 | epic-2 | 7/7 done | (Phase 1) |
| 94 | epic-3 | 6/6 done | (Phase 1) |
| 104 | epic-4 | 3/3 done | (Phase 1) |
| 113 | epic-5 | 6/6 done | (Phase 1) |
| 125 | epic-6 | 4/4 done | (Phase 1) |
| 144 | epic-8 | 5/5 done | 2026-04-23 (Story 8.5) |
| 155 | epic-9 | 3/3 done | 2026-04-25 (Story 9.3 close-out) |
| 162 | epic-10 | 2/2 done | 2026-04-26 (Story 10.2) |
| 170 | epic-11 | 2/2 done | 2026-04-26 (Stories 11.1+11.2) |

**Diff sample (epic-1):**
```yaml
# OLD
epic-1: in-progress
# NEW
epic-1: done # 2026-04-26 bulk MVP-epic closeout (course correction): all 7 stories complete since Phase 1 — manual epic-status flip per sprint-status.yaml workflow notes lines 32-39
```

### Change 4.2 — `epics.md` Post-MVP marker (apply now, no decision needed)

**Action:** Insert a `## Post-MVP — Decision Point` section after the Epic 11 detail block, pointing readers at the retrospective and this proposal.

**Rationale:** Anyone reading `epics.md` today sees Epic 11 as the last entry with no signal about what comes next. The marker prevents the next planner (human or agent) from assuming the document is incomplete and attempting to guess Epic 12.

**Diff sample (after current end-of-file):**
```markdown
---

## Post-MVP — Decision Point

Epic 11 is the final planned epic. The MVP roadmap has been delivered.

The post-MVP direction is deliberately undecided pending the outcome of:

1. **First real release** (gated on Epic 11 retro actions A1 = branch protection, A2 = `NUGET_API_KEY`)
2. **Sprint Change Proposal 2026-04-26** (`sprint-change-proposal-2026-04-26.md`) — recommends Hybrid path: Operations Epic 12 first, then Phase 2 decision
3. **Epic 11 retrospective** (`_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md`) — captures 5 action items, 4 deferred follow-ups, 3 team agreements

Do not add new epics here without an explicit `feat:`-or-equivalent decision recorded in a sprint change proposal.
```

### Change 4.3 — Operations Epic 12 scaffold (apply only on Jerome's go-ahead)

**Status:** Conditional on Section 3 Option C selection.

**Proposed shape:**

```markdown
## Epic 12: First Release & Operations Foundation

Cut the first real release of Hexalith.Memories to nuget.org, apply branch protection on `main`,
operationalize the Epic 11 retrospective action items, and prove the release path end-to-end before
any further feature investment.

**Driven by:** Epic 11 retrospective + Sprint Change Proposal 2026-04-26 (Option C Hybrid)

### Story 12.1: First Release Path Validation
- A1 — Apply branch protection on `main` in GitHub UI per `docs/dev/branch-protection.md`
- A2 — Configure `NUGET_API_KEY` repository secret
- Cut a deliberate `feat:` or `fix:` commit on `main` to trigger `release.yml`
- Observe end-to-end: pack → validate → publish → tag → GitHub Release
- Validate published packages on nuget.org match the intended inventory
- Capture the runbook (D2 retro action)

### Story 12.2: Forbidden Default Tolerances Checklist (A3)
- Add the "tolerance idioms" review checklist to `CONTRIBUTING.md` Code Review section
- Reference Epic 11 retro Pattern 3 (process-substitution, if-no-files-found:ignore, --skip-duplicate, per-row zero-count)

### Story 12.3: Story-File-Scope Enforcement (A4)
- Implement a pre-commit / CI check that scans the diff against the story's `File Scope` section
- Fail loudly when `src/**/*.cs` is touched and the story scope forbids it
- Allow explicit `Scope-Override:` opt-out

### Story 12.4: Baseline Failures Sweep (A5)
- Replay `test-unit-contract` against recent stories (8.x, 9.x, 10.x history)
- Document any new tracked baselines as `S11-FX` style entries with re-open triggers
- Resolve or formally accept each one

### Story 12.5: Partial-Publish Alerting (S11-FD)
- Add Slack/issue-creation step to `tools/publish-nuget.ps1` for the partial-publish scenario
- Wires after first release (Story 12.1) succeeds

### Optional follow-up stories
- **Story 12.6 — S11-FA EmbeddingInputContentKindTests resolution** (investigate, fix, or formally accept)
- **Story 12.7 — S11-FB compile-time symbol verification** (only if a surface drift slips past the verifier first)
- **Story 12.8 — S11-FC release.yml stale-tag preflight** (only if a stale-tag collision actually bites first)
```

### Change 4.4 — Phase 2 PRD addendum (deferred; only if Section 3 decision shifts to Option B post-Epic-12)

**Status:** Not part of this proposal. Deferred until after first release lands and Epic 12 outcomes are known.

**Inputs available when needed:**
- `_bmad-output/implementation-artifacts/deferred-work.md` (~40 entries; ~10 explicitly tagged "Phase 2 candidate")
- Real release-path observations (after Epic 12)
- First-contributor friction reports (after public release)

---

## Section 5: PRD MVP Impact and High-Level Action Plan

**Is MVP affected?** No. MVP is delivered in full. All 11 planned epics are complete; all stories under them are `done`; CI/release infrastructure is built and validated.

**MVP closeout state:**
- ✅ Three-axis search (Gate 1) validated
- ✅ Tenant isolation (Gate 2) validated
- ✅ Developer experience (Gate 3) validated
- ✅ Operations / observability (Phase 1.5 part 1) delivered
- ✅ EventStore integration & MCP surface (Phase 1.5 part 2) delivered
- ✅ CI/CD & release automation (cross-cutting infrastructure) delivered
- ⚠️ First real release **not yet executed** (gated on external maintainer actions A1/A2)

**High-level action plan (recommended Hybrid path):**

1. **Today (mechanical, no decision needed)** — apply Change 4.1 (sprint-status closeout) + Change 4.2 (post-MVP marker)
2. **On Jerome's go-ahead** — apply Change 4.3 (scaffold Epic 12 in `epics.md`)
3. **Maintainer actions (Jerome only, outside repo)** — A1 branch protection + A2 `NUGET_API_KEY`
4. **Story 12.1 work session** — cut first real release, observe end-to-end
5. **After first release lands successfully** — execute Stories 12.2-12.5 (operationalize retro action items)
6. **Decision point: Phase 2 vs MVP-done** — make the call with real release-path data + first-contributor signal in hand

**Sequencing:**
- Steps 1-2 are mechanical and unblocked
- Steps 3-4 are externally gated by Jerome
- Steps 5-6 cascade from successful step 4

---

## Section 6: Implementation Handoff

### Scope classification: **Major** (strategic decision + cross-artifact updates)

**Major because:** The decision on what comes after Epic 11 affects PRD scope, future architecture decisions, deferred-work backlog disposition, and team direction. Even though the immediate housekeeping (Change 4.1, 4.2) is mechanical, the overall change shape requires Product Manager / Solution Architect involvement for the post-MVP direction.

### Routing

| Recipient | Responsibility | Deliverables |
|---|---|---|
| **Jerome (Project Lead / Architect)** | Approve Section 3 directional choice (Option C recommended); execute external maintainer actions A1+A2 | Approval decision; A1 (branch protection in GitHub UI); A2 (`NUGET_API_KEY` secret in GitHub UI) |
| **Claude (current session)** | Apply Change 4.1 (sprint-status closeout) + Change 4.2 (post-MVP marker) immediately as mechanical housekeeping | `sprint-status.yaml` updated; `epics.md` post-MVP marker appended |
| **Claude (next session, on go-ahead)** | If Option C selected: apply Change 4.3 (Epic 12 scaffold); generate Story 12.1 file via `bmad-create-story`; await first release | Epic 12 added to `epics.md`; Story 12.1 created in `_bmad-output/implementation-artifacts/` |
| **Developer agent (Amelia)** | Once first release succeeds, execute Stories 12.2-12.5 implementation | A3 checklist in `CONTRIBUTING.md`; A4 file-scope guard; A5 baseline sweep results; S11-FD alerting |
| **Product Manager (deferred)** | Only if Phase 2 path selected later: draft Phase 2 PRD addendum | `prd-phase2.md` (new artifact) |
| **Architect (deferred)** | Only if Phase 2 path selected and adds new architectural decisions: ADR updates | New ADRs in `architecture.md` |

### Success criteria

- [ ] Section 4.1 housekeeping applied (10 `in-progress → done` flips) — verifiable via `grep "epic-.*: done" sprint-status.yaml | wc -l` reaching 11
- [ ] Section 4.2 marker applied — verifiable via `grep "Post-MVP — Decision Point" epics.md`
- [ ] Jerome confirms or redirects Section 3 recommendation
- [ ] If Option C: Epic 12 scaffold appended to `epics.md`
- [ ] If Option C: A1+A2 external maintainer actions applied; first release executed
- [ ] If Option C: Stories 12.2-12.5 implemented and `done`

---

## Section 7: Change Log

- **2026-04-26** — Sprint Change Proposal drafted from Epic 11 retrospective findings. Mechanical Section 4.1 + 4.2 changes applied immediately by Claude. Section 4.3 (Epic 12 scaffold) staged but not applied; awaits Jerome's directional confirmation between Options A/B/C/D in Section 3.
- **2026-04-26 (later)** — Jerome confirmed **Option C — Hybrid**. Section 4.3 applied: Epic 12 (First Release & Operations Foundation) scaffolded into `_bmad-output/planning-artifacts/epics.md` with 6 stories (12.1 First Release Path Validation, 12.2 Forbidden Default Tolerances Checklist, 12.3 Story-File-Scope Enforcement, 12.4 Baseline Failures Sweep, 12.5 Partial-Publish Alerting, 12.6 EmbeddingInputContentKind Baseline Resolution). Stories 12.7 + 12.8 explicitly marked as conditional follow-ups not added to active scope per Epic 11 retro Action A4 (story-file-scope discipline). `sprint-status.yaml` now lists `epic-12: backlog` with all 6 stories `backlog`, `epic-12-retrospective: optional`. The "Post-MVP — Decision Point" placeholder section in `epics.md` was replaced with the full Epic 12 detail block + a reduced "Decision Point: Beyond Epic 12" section that preserves the speculative-epic guardrails for whatever comes after Epic 12. **Next:** Jerome executes external maintainer actions A1 (branch protection in GitHub UI) + A2 (NUGET_API_KEY secret), then `bmad-create-story` produces the Story 12.1 file when ready to begin implementation.
