# Sprint Change Proposal: One-Shot Artifact Tracking Convention

**Date:** 2026-07-16  
**Mode:** Batch  
**Status:** Approved — implemented and routed  
**Scope classification:** Minor  
**Recommended owner:** Developer

## 1. Issue Summary

The repository has bounded implementation traces such as
`19-5-ci-submodule-metadata-cleanup.md` that look story-shaped but are not
registered stories in `epics.md` or `sprint-status.yaml`.

The current quick-dev workflow already distinguishes these files operationally:

- story-key resolution applies only when an artifact is an epic story;
- sprint synchronization skips work that has no registered `story_key`; and
- the one-shot route emits a completion trace with `route: 'one-shot'` and
  `status: 'done'`.

That behavior is not stated as a project tracking policy. As a result, Epic 19
and Epic 20 retrospectives independently recorded open actions asking whether
one-shot and story-automator artifacts require sprint-status rows. The Epic 19
retrospective also had to explain manually that the completed 19.5 trace was
supporting one-shot work rather than a fifth registered Epic 19 story.

This is a sprint-governance ambiguity, not a missing product requirement. The
needed correction is to define when an artifact self-tracks, when it belongs in
`development_status`, and when a bounded task must be promoted to a normal spec
or registered story.

## 2. Impact Analysis

### Epic and story impact

- Epic 19 remains complete with four registered stories. The 19.5 artifact is
  separately identified as one-shot completion evidence and does not become a
  retroactive Story 19.5.
- Epic 20 remains complete. Its duplicate carry-forward action can close against
  the same policy evidence as the Epic 19 action.
- No epic or story is added, removed, reopened, renumbered, or resequenced.
- Future epic completion calculations remain based only on registered
  `development_status` rows.

### Artifact impact

| Artifact | Impact | Required action |
|---|---|---|
| PRD | None | No edit |
| Architecture | None | No edit |
| UX specification | None | No edit |
| `epics.md` | Governance clarification | Add the canonical non-story artifact policy beside the story-key policy |
| `sprint-status.yaml` | Tracking clarification and action closure | Add workflow notes and close the two duplicate actions |
| Canonical project context | Durable agent guidance | Add one concise workflow rule |
| `19-5-ci-submodule-metadata-cleanup.md` | Already conforming | No historical edit; retain `route: 'one-shot'` and `status: 'done'` |
| Quick-dev workflow | Already aligned | No workflow implementation change |

### Technical and delivery impact

- No source, test, package, deployment, infrastructure, API, persistence, or UI
  behavior changes.
- No runtime validation is required.
- **Effort:** Low, expected to fit within one focused documentation/tracking
  change.
- **Risk:** Low. The primary risk is wording that accidentally turns completion
  evidence into a hidden backlog mechanism; the promotion rules below prevent
  that.
- **Timeline impact:** None expected.
- **MVP impact:** None.

## 3. Recommended Approach

Use **Direct Adjustment** and codify the behavior the quick-dev workflow already
implements.

A one-shot artifact is a self-identifying completion trace for a bounded,
zero-blast-radius correction completed and reviewed in one workflow execution.
It is not a backlog item and receives no `development_status` row. If work needs
draft, in-progress, review, dependency, or multi-session lifecycle tracking, it
must use the normal plan/code/review spec route. If the work belongs to an epic
or affects epic completion, it must first become a registered story in both
`epics.md` and `development_status`.

Generated story-automator or orchestration files are supporting evidence. They
inherit the lifecycle of the canonical registered story, normal spec, or
one-shot trace that links to them and do not receive individual sprint rows.

The convention applies prospectively from its 2026-07-16 approval and
expressly ratifies the historical 19.5 trace that triggered the correction.
Older one-shot traces retain their historical metadata without becoming
precedent or overriding the lifecycle of a registered story they support.

Rollback and MVP review are unnecessary: there is no product-scope change and
no implementation to revert.

## 4. Detailed Change Proposals

### Change A — Add a non-story artifact policy to `epics.md`

**Section:** after `### Story Key Policy`

**OLD:**

No explicit policy distinguishes registered stories, normal non-epic specs,
one-shot completion traces, and generated workflow evidence.

**NEW:**

```markdown
### Non-Story Implementation Artifact Policy

`development_status` is the registry for epics, stories defined in this
document, and retrospectives. A file does not become a story merely because its
name begins with an `Epic-Story`-shaped numeric prefix.

A one-shot artifact is permitted only for a bounded, zero-blast-radius
correction completed and reviewed in one workflow execution. Its canonical
trace must declare `route: 'one-shot'` and `status: 'done'` in valid frontmatter.
It remains outside `development_status`, does not lift, hold, reopen, or close an
epic, and is listed separately from registered stories when a retrospective
uses it as supporting evidence.

If the work needs a draft, in-progress, review, dependency, or multi-session
lifecycle, route it through a normal plan/code/review spec whose frontmatter
self-tracks that lifecycle. If the work belongs to an epic, changes epic scope
or acceptance criteria, or affects epic completion, register it as a story in
this document and `development_status` before implementation continues.

Generated story-automator, orchestration, review, and test-output files are
supporting evidence. They inherit the lifecycle of the canonical registered
story, normal spec, or one-shot trace that references them and do not receive
individual sprint-status rows.
```

**Rationale:** This makes the story boundary explicit, prevents numeric filenames
from changing epic accounting, and defines a promotion path when work is no
longer truly one-shot.

### Change B — Add matching workflow notes to `sprint-status.yaml`

**Section:** status definitions and workflow notes before the YAML data

**OLD:**

```yaml
# Story Status:
#   - backlog: Story only exists in epic file
#   - ready-for-dev: Story file created in stories folder
#   - in-progress: Developer actively working on implementation
#   - review: Ready for code review (via Dev code-review workflow)
#   - done: Story completed
```

There is no definition for non-story artifacts.

**NEW:** Keep the story definition and append:

```yaml
# Non-Story Artifact Tracking:
#   - development_status is reserved for registered epics, stories, and retrospectives
#   - a one-shot completion trace self-identifies with route: one-shot and status: done; it receives no development_status row and does not affect epic completion
#   - multi-stage non-epic work self-tracks through normal spec frontmatter; epic-owned work must be registered in epics.md and development_status
#   - generated story-automator, orchestration, review, and test-output files are evidence owned by a canonical story/spec/one-shot artifact, not independent status rows
```

**Rationale:** Readers and status tooling can interpret the absence of a story row
as governed behavior rather than tracking drift.

### Change C — Add one durable project-context rule

**Section:** Development Workflow Rules

**OLD:** No concise agent rule defines the one-shot tracking boundary.

**NEW:**

```markdown
- **Keep one-shot traces out of story accounting** — a bounded one-shot artifact
  self-identifies with `route: one-shot` and `status: done` and receives no
  `development_status` row. Use a normal spec for multi-stage non-epic work;
  register epic-owned work in both `epics.md` and `development_status` before
  implementation. Generated automation artifacts are supporting evidence only.
```

**Rationale:** The canonical project context is loaded by implementation agents,
so the convention remains visible outside planning and retrospective workflows.

### Change D — Close both duplicate retrospective actions

**Artifact:** `sprint-status.yaml`

**OLD:**

```yaml
- epic: 19
  action: "Define the tracking convention for one-shot artifacts like 19-5-ci-submodule-metadata-cleanup.md"
  owner: "Amelia"
  status: open

- epic: 20
  action: "Define a tracking convention for one-shot or story-automator artifacts that sit outside registered story rows"
  owner: "Amelia"
  status: open
```

**NEW:** Preserve both historical rows and set each to `done` with a concise,
dated comment pointing to the same policy:

```yaml
status: done  # 2026-07-16: epics.md, sprint-status workflow notes, and project context now define one-shot traces as self-tracked completion evidence outside development_status; multi-stage or epic-owned work must use the normal spec/story lifecycle, and generated automation artifacts inherit their canonical owner's status.
```

**Rationale:** Both actions describe the same resolved ambiguity. Preserving and
closing both rows retains retrospective provenance without keeping duplicate
work open.

### Change E — Preserve the historical 19.5 artifact as-is

**Artifact:** `19-5-ci-submodule-metadata-cleanup.md`

**OLD and retained state:**

```yaml
status: 'done'
route: 'one-shot'
```

**NEW:** No content change. Under the new policy, this is explicitly a
self-tracked one-shot completion trace, not Story 19.5 and not a missing
`development_status` row.

**Rationale:** Historical evidence already satisfies the convention; rewriting it
would add churn without improving traceability.

## 5. Verification and Acceptance Criteria

The change is complete only when:

1. `epics.md` explicitly defines the registered-story, normal-spec, one-shot,
   and generated-evidence boundaries.
2. `sprint-status.yaml` states that one-shot traces remain outside
   `development_status` and do not affect epic completion.
3. The canonical project context carries the same rule without contradicting
   the planning or status wording.
4. `19-5-ci-submodule-metadata-cleanup.md` remains unchanged and still declares
   `route: 'one-shot'` and `status: 'done'`.
5. Both duplicate retrospective actions are marked `done` with dated closure
   evidence; no unrelated action item is changed.
6. No new `19-5-*` development-status row is introduced.
7. Existing sprint-status YAML remains parseable and all comments/structure are
   preserved.
8. Documentation whitespace and link checks pass; no runtime build is required
   because the approved scope changes only Markdown and YAML comments/statuses.
9. Pre-policy one-shot traces, including the Epic 26 companion traces, remain
   historical evidence rather than precedent and do not override registered
   story lifecycle state.

## 6. Alternatives Considered

### Add every one-shot file to `development_status`

Rejected. This would make a completion trace indistinguishable from a planned
story, distort epic story counts, and require story-lifecycle semantics for work
that is allowed only when it finishes in one bounded execution.

### Add a separate `one_shot_artifacts` YAML registry

Rejected for the current need. It would create another dual-write surface even
though the trace frontmatter already carries the authoritative route and terminal
status. If reporting later requires a machine-readable inventory, that need can
justify a registry plus validation tooling in a separate change.

### Leave the convention implicit in quick-dev

Rejected. Two retrospectives independently treated the missing policy as an open
action, proving that workflow behavior alone is not sufficient project guidance.

## 7. Implementation Handoff

**Classification:** Minor — direct Developer implementation.

**Developer responsibilities:**

- Add the same policy, without semantic drift, to `epics.md`, sprint-status
  workflow notes, and the canonical project context.
- Preserve the historical 19.5 trace.
- Close only the two matching retrospective action rows.
- Preserve all unrelated worktree content and sprint-status history.
- Validate YAML parsing and the focused textual acceptance criteria.

**Reviewer responsibilities:**

- Confirm the wording does not create a hidden backlog outside
  `development_status`.
- Confirm promotion is mandatory for multi-stage or epic-owned work.
- Confirm generated automation evidence is not independently counted.
- Confirm registered epic/story/retrospective accounting is unchanged.

**Success definition:** Future one-shot work is unambiguously self-tracked as
terminal completion evidence, registered story accounting remains authoritative,
and work that outgrows one-shot constraints is promoted before continuing.

## 8. Correct-Course Checklist Status

| Checklist item | Status | Finding |
|---|---|---|
| 1.1 Triggering story identified | Complete | No triggering story; duplicate Epic 19 and Epic 20 retrospective actions were triggered by the 19.5 one-shot trace |
| 1.2 Core problem defined | Complete | Existing workflow behavior lacks a durable tracking convention |
| 1.3 Initial evidence assessed | Complete | 19.5 frontmatter, quick-dev routing/sync behavior, sprint status, and both retrospectives agree on the gap |
| 2.1 Current epic impact | Complete | Epics 19 and 20 remain complete |
| 2.2 Epic-level changes | N/A | No epic scope or acceptance-criteria change |
| 2.3 Remaining epics reviewed | Complete | Policy applies prospectively without sequencing changes |
| 2.4 Epics invalidated/new epic required | N/A | None |
| 2.5 Priority/order changes | N/A | None |
| 3.1 PRD conflict/impact | Complete | None |
| 3.2 Architecture conflict/impact | Complete | None |
| 3.3 UX conflict/impact | N/A | None |
| 3.4 Other artifact impact | Complete | Epics governance, sprint workflow notes/actions, and project context require edits |
| 4.1 Direct adjustment viability | Complete | Viable; low effort and low risk |
| 4.2 Rollback viability | N/A | No implementation rollback target |
| 4.3 MVP review viability | N/A | Product scope and readiness are unaffected |
| 4.4 Recommended path selected | Complete | Direct Adjustment |
| 5.1 Issue summary | Complete | Section 1 |
| 5.2 Epic/artifact impact | Complete | Section 2 |
| 5.3 Recommended path | Complete | Section 3 |
| 5.4 Detailed proposals and MVP impact | Complete | Sections 4 and 5 |
| 5.5 Agent handoff | Complete | Section 7 |
| 6.1 Checklist reviewed | Complete | All applicable analysis items addressed |
| 6.2 Proposal consistency | Complete | Policy matches current quick-dev behavior and retrospective evidence |
| 6.3 Explicit approval | Complete | Approved by Administrator on 2026-07-16 |
| 6.4 Sprint-status implementation | Complete | Both duplicate action rows closed without adding a 19.5 development-status row |
| 6.5 Handoff confirmation | Complete | Minor change routed to and implemented by the Developer agent |

## 9. Approval Record

- **Decision:** Approved
- **Approver:** Administrator
- **Approval date:** 2026-07-16
- **Final scope:** Minor
- **Routed to:** Developer agent for direct implementation
- **Implementation status:** Complete

## 10. Workflow Execution Log

| Date | Event | Result |
|---|---|---|
| 2026-07-16 | Trigger confirmed from the Epic 19 and Epic 20 retrospective actions | Complete |
| 2026-07-16 | PRD, epics, architecture, UX, sprint status, one-shot trace, and quick-dev behavior assessed | Complete |
| 2026-07-16 | Batch proposal reviewed by Administrator | Continued |
| 2026-07-16 | Sprint Change Proposal explicitly approved by Administrator | Approved |
| 2026-07-16 | Minor Direct Adjustment routed to Developer agent | Complete |
| 2026-07-16 | Policy added to epics, sprint-status guidance, and canonical project context | Complete |
| 2026-07-16 | Duplicate Epic 19 and Epic 20 actions closed | Complete |
| 2026-07-16 | YAML parse, policy assertions, action closure, row-exclusion, and line-ending checks | Passed |
