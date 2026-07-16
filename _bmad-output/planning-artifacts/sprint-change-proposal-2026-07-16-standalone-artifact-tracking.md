---
project: memories
date: 2026-07-16
status: superseded
change_scope: minor
mode: incremental
trigger: "Define a tracking convention for one-shot or story-automator artifacts that sit outside registered story rows"
edit_proposals: approved
approved_by: Administrator
approved_at: 2026-07-16
superseded_by: sprint-change-proposal-2026-07-16-one-shot-artifact-tracking.md
superseded_at: 2026-07-16
---

# Sprint Change Proposal — Standalone Artifact Tracking

> **Superseded on 2026-07-16.** Quick-dev adversarial review identified that
> this proposal's standalone register conflicts with the approved one-shot
> self-tracking convention. The one-shot proposal is authoritative; the
> unimplemented register, metadata backfills, workflow customizations, and
> tests below are retained only as rejected-alternative decision history and
> must not be executed.

## 1. Issue Summary

Repository planning and sprint tracking define registered numeric stories, but
they do not define a lifecycle lane for bounded implementation artifacts that
have no registered story row. This makes an artifact's relationship to the
backlog, sprint state, and epic completion ambiguous.

The gap is demonstrated by current project evidence:

- `19-5-ci-submodule-metadata-cleanup.md` is useful completed evidence and
  declares `route: one-shot`, but there is no Story 19.5 in `epics.md` or
  `sprint-status.yaml`.
- The Epic 19 retrospective describes that artifact as outside the registered
  Epic 19 story set and records an action to define the convention.
- The Epic 20 retrospective carries the same action forward for one-shot or
  story-automator artifacts. Both copies remain open in `sprint-status.yaml`.
- `spec-clarify-epic-26-closure-status.md` is a second completed artifact with
  `route: one-shot`, confirming this is a repeatable workflow need rather than
  a single filename anomaly.
- The current Story Key Policy defines numeric story keys and execution order,
  but does not distinguish registered story companions from standalone work.

Without a convention, standalone artifacts can be mistaken for missing story
rows, silently omitted from centralized tracking, or incorrectly counted when
deciding whether an epic is complete.

## 2. Impact Analysis

### Epic impact

- Epics 19 and 20 remain complete and are not reopened.
- No epic scope, acceptance criteria, order, priority, or completion state
  changes.
- No new epic or story is required for this bounded governance correction.
- The two retrospective actions remain in place for audit history and become
  `done` when the convention is implemented.

### Story impact

- No registered story is added, removed, renumbered, or modified.
- Registered story artifacts continue to inherit lifecycle state from their
  existing `development_status` row.
- Future artifacts without a registered governing story use a separate
  standalone lifecycle record.
- Standalone work must be promoted to a newly registered numeric story before
  implementation when it introduces product scope, acceptance criteria,
  multi-phase story work, or an epic-completion dependency.

### Artifact conflicts

- **PRD:** No conflict or amendment. Product goals, requirements, MVP scope,
  and success metrics are unchanged.
- **Architecture:** No product architecture amendment. Runtime components,
  data models, APIs, integrations, and deployment topology are unchanged.
- **UX:** Not applicable; no user interface or journey changes.
- **Epics:** The Story Key Policy needs a standalone-artifact boundary.
- **Sprint status:** A separate lifecycle register and resolution of the two
  duplicate open actions are required.
- **Workflow governance:** Spec creation and code review need an update-safe
  classification and synchronization rule.
- **Existing artifacts:** The two current explicit one-shot artifacts need
  stable tracking metadata and register entries.

### Technical and operational impact

- No production code, package, database, API, infrastructure, deployment, or
  submodule change.
- No `development_status` schema or registered-story status semantics change.
- Repository-owned `_bmad/custom` files carry the workflow rule so it survives
  BMad skill refreshes.
- Existing customization-resolution fixtures should verify the merged policy
  for both installed skill surfaces.

## 3. Recommended Approach

### Selected path: Direct Adjustment

Define a strict boundary in the Story Key Policy, add a standalone lifecycle
register beside—but not inside—`development_status`, enforce classification in
the update-safe spec/review customizations, and backfill the two artifacts that
already declare `route: one-shot`.

This approach is preferred because the product plan remains valid, completed
epics do not need reopening, and a distinct lane makes bounded support work
visible without manufacturing story rows. Rollback would discard useful
history without fixing the convention, while an MVP or epic replan would be
disproportionate to a repository-governance gap.

### Effort, risk, and timeline

- **Effort:** Low to medium; one focused developer change including fixtures.
- **Product risk:** Low; no product behavior changes.
- **Process risk:** Low after resolver and tracker consistency checks pass.
- **Timeline impact:** No backlog resequencing or milestone impact.
- **Scope classification:** Minor; direct Developer-agent implementation.

## 4. Detailed Change Proposals

### 4.1 Define the planning boundary

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `Story Key Policy`

**OLD**

The section defines numeric `Epic.Story` keys, historical aliases, and
`story_execution_order`, but does not define what makes an implementation
artifact a registered story artifact.

**NEW**

Append the following policy:

> An artifact is a registered story artifact only when its numeric key exists
> in both `epics.md` and `sprint-status.yaml` under `development_status`.
> Automation-generated companion specs must declare their governing `story`
> and inherit that story row's lifecycle.
>
> Work without a registered governing story uses the standalone route. It
> must:
>
> - declare `route: one-shot`, a stable `standalone_id`, `source`, `owner`, and
>   `status`;
> - declare `producer: story-automator` when applicable;
> - use a non-story filename rather than an unregistered `Epic-Story` prefix;
> - be tracked in the standalone artifact register, not
>   `development_status`; and
> - remain excluded from story readiness, execution order, and epic-completion
>   calculations.
>
> Standalone work must be promoted to a registered numeric story before
> implementation if it introduces product scope, acceptance criteria,
> multi-phase story work, or an epic-completion dependency.
>
> `19-5-ci-submodule-metadata-cleanup.md` is a historical filename exception.
> It remains a standalone artifact and must not be treated as registered Story
> 19.5.

Rationale: this creates an explicit and durable boundary among registered
backlog work, registered-story companion artifacts, and bounded standalone
work.

### 4.2 Add the standalone lifecycle register

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

**OLD**

- Only registered epics and stories appear under `development_status`.
- No centralized lifecycle record exists for standalone artifacts.
- The same convention action remains open under Epics 19 and 20.

**NEW**

Add a sibling top-level register. Its keys are stable `standalone_id` values;
its status is authoritative and must remain synchronized with artifact
frontmatter.

```yaml
# Standalone artifacts are tracked independently and never contribute to
# story readiness, execution order, or epic completion.
# status: backlog | ready-for-dev | in-progress | review | done
standalone_artifacts:
  standalone-2026-07-04-ci-submodule-metadata-cleanup:
    artifact: "19-5-ci-submodule-metadata-cleanup.md"
    route: one-shot
    producer: manual
    governing_story: null
    source: "epic-19-retrospective-action-4"
    owner: "Amelia"
    status: done
  standalone-2026-07-16-clarify-epic-26-closure-status:
    artifact: "spec-clarify-epic-26-closure-status.md"
    route: one-shot
    producer: story-automator
    governing_story: null
    source: "Epic 26 closure-status follow-up"
    owner: "Amelia"
    status: done
```

Retain both historical action entries and update only their lifecycle and
resolution comments:

```yaml
- epic: 19
  action: "Define the tracking convention for one-shot artifacts like 19-5-ci-submodule-metadata-cleanup.md"
  owner: "Amelia"
  status: done  # 2026-07-16: Standalone-artifact policy and lifecycle register defined; the triggering artifact was backfilled.

- epic: 20
  action: "Define a tracking convention for one-shot or story-automator artifacts that sit outside registered story rows"
  owner: "Amelia"
  status: done  # 2026-07-16: Same convention as the Epic 19 action; both existing explicit one-shot artifacts were backfilled.
```

Rationale: standalone work becomes centrally discoverable without creating a
false story row or changing epic-completion arithmetic. Keeping both actions
preserves the retrospective audit trail.

### 4.3 Add an update-safe standalone policy

Artifact: `_bmad/custom/standalone-artifact-tracking.md` (new)

**OLD**

No canonical workflow policy resolves whether a generated artifact belongs to
a registered story or the standalone lane.

**NEW**

Create a shared policy with these invariants:

1. Resolve a proposed governing story against both `epics.md` and
   `development_status` before writing or reviewing an artifact.
2. A registered companion declares `story: Epic.Story`, inherits the story
   lifecycle, and does not receive a standalone entry.
3. An unregistered artifact declares the approved standalone metadata and has
   exactly one matching `standalone_artifacts` entry.
4. Artifact frontmatter and register status must agree before close-out.
5. New standalone filenames cannot use an unregistered numeric story prefix.
6. The historical `19-5` filename is the sole named exception introduced by
   this correction.
7. Work that crosses the standalone boundary halts for promotion to a
   registered story before implementation continues.
8. Standalone status never contributes to registered-story readiness,
   execution order, or epic completion.

Rationale: one shared contract prevents creation and review workflows from
developing different interpretations of the new lane.

### 4.4 Classify artifacts during spec creation

Artifact: `_bmad/custom/bmad-spec.toml` (new)

**OLD**

Resolved `bmad-spec` configuration loads project context but has no
standalone-artifact classification directive.

**NEW**

- Load `_bmad/custom/standalone-artifact-tracking.md` as a persistent fact.
- Append one `STANDALONE_ARTIFACT_TRACKING:` directive requiring the workflow
  to resolve a governing story before writing.
- For a registered story, write the `story` relationship and retain the story
  lifecycle.
- Without a registered story, use a non-story filename, write the required
  standalone frontmatter, create or update the matching standalone register
  entry, and never mutate `development_status`.
- Halt for story promotion when the requested work exceeds the standalone
  boundary.

Rationale: the automator classifies the artifact at creation rather than
leaving retrospective or review work to infer its status later.

### 4.5 Synchronize the correct lifecycle during review

Artifact: `_bmad/custom/bmad-code-review.toml` (update)

**OLD**

The resolved review workflow loads story-scope and story-phase policies. Its
base status synchronization skips `sprint-status.yaml` when no story key is
available, which leaves standalone artifacts without centralized lifecycle
synchronization.

**NEW**

- Add `_bmad/custom/standalone-artifact-tracking.md` to persistent facts.
- Append one `STANDALONE_ARTIFACT_TRACKING:` directive requiring review to
  classify the artifact before status synchronization.
- Registered artifacts retain the existing story-status path.
- Standalone artifacts synchronize frontmatter with their exact
  `standalone_artifacts` entry and never add or update `development_status`.
- Fail close-out when the register entry is missing, duplicated, disagrees
  with frontmatter, or uses a numeric story-like key without the historical
  exception.
- Preserve all existing historical-slice and story-phase-ledger directives and
  review layers.

Rationale: review closes the same lifecycle lane selected at creation and does
not mistake absence of a story key for absence of tracking.

### 4.6 Backfill existing explicit one-shot artifacts

Artifacts:

- `_bmad-output/implementation-artifacts/19-5-ci-submodule-metadata-cleanup.md`
- `_bmad-output/implementation-artifacts/spec-clarify-epic-26-closure-status.md`

**OLD**

Both files declare `route: one-shot` and `status: done`, but neither has a
stable tracking ID, producer, explicit null governing story, source, or owner.

**NEW**

Add to the triggering artifact:

```yaml
route: one-shot
standalone_id: standalone-2026-07-04-ci-submodule-metadata-cleanup
producer: manual
governing_story: null
source: epic-19-retrospective-action-4
owner: Amelia
```

Add to the Epic 26 closure-status artifact:

```yaml
route: one-shot
standalone_id: standalone-2026-07-16-clarify-epic-26-closure-status
producer: story-automator
governing_story: null
source: "Epic 26 closure-status follow-up"
owner: Amelia
```

Preserve their existing intent, evidence, resolution, and `done` status.

Rationale: the convention starts with all currently explicit one-shot
artifacts reconciled rather than knowingly leaving an exception.

### 4.7 Verify customization and tracker invariants

Artifact: `tests/tooling/bmad_customization/bmad_customization_test.py`

**OLD**

Existing fixtures verify other team-owned customization policies but do not
cover standalone artifact tracking.

**NEW**

Extend the fixture to verify:

- resolved `bmad-spec` and `bmad-code-review` workflows load the shared policy
  and contain exactly one standalone directive on both installed surfaces;
- existing facts, directives, and review layers remain present;
- both explicit one-shot frontmatter blocks match exactly one register entry;
- no standalone key appears under `development_status`;
- both duplicate retrospective actions are retained and marked `done`; and
- the historical `19-5` exception is explicit while new numeric standalone
  naming remains prohibited by policy.

Also parse `sprint-status.yaml` with the project's existing YAML validation
path and run the focused customization fixture.

Rationale: executable checks protect the convention from skill refreshes and
future tracker drift.

## 5. Implementation Handoff

### Scope and recipient

This is a **Minor** change. Hand off directly to the Developer agent; Product
Owner, Product Manager, Architect, and UX coordination are not required because
no backlog, product, architectural, or interface scope changes.

### Developer responsibilities

1. Apply Proposals 4.1 through 4.7 without changing registered story rows.
2. Preserve all unrelated sprint-status content and comments.
3. Keep generated `.agents/skills/**` and `.claude/skills/**` files unchanged;
   use only repository-owned `_bmad/custom` overrides.
4. Run the focused customization fixtures and YAML validation.
5. Reconcile both one-shot artifacts, both register entries, and both action
   resolutions before close-out.

### Success criteria

- Registered story companions and standalone artifacts have mutually
  exclusive, deterministic classifications.
- Both current `route: one-shot` artifacts have matching standalone entries.
- `development_status` contains no invented Story 19.5 or other standalone
  row.
- Standalone lifecycle state cannot affect story readiness, execution order,
  or epic completion.
- Spec creation selects the correct lane before writing.
- Code review synchronizes the selected lane and fails closed on drift.
- The two duplicate retrospective actions are preserved and resolved.
- Focused resolver/tracker fixtures and YAML validation pass.

## 6. Checklist Status

| Section | Result |
|---|---|
| 1. Trigger and context | Done. No triggering registered story; retrospective evidence and two concrete artifacts establish the gap. |
| 2. Epic impact | Done. No epic modification, addition, removal, invalidation, or resequencing. |
| 3. Artifact conflicts | Done. PRD, architecture, and UX are unaffected; planning, sprint tracking, workflow customization, and two artifacts require adjustment. |
| 4. Path evaluation | Done. Direct Adjustment is viable at low risk; rollback and MVP review are not viable or proportionate. |
| 5. Proposal components | Done. Issue, impact, path, action plan, handoff, and success criteria are documented. |
| 6. Final review and handoff | Done. Administrator approved implementation on 2026-07-16. Sprint epic/story synchronization is N/A because no registered rows change. |

## 7. Approval State

The three incremental edit proposals and the complete Sprint Change Proposal
were approved by Administrator on 2026-07-16. The proposal is finalized and
ready for direct Developer-agent implementation.
