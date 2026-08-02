---
title: 'Publish Epic 23 review gates and EventStore evidence corrections'
type: 'docs'
created: '2026-08-02'
status: 'ready-for-dev'
baseline_commit: '8d0c1a5873d65e2ead93f50e84cd7ce20c99a92e'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02-apphost-eventstore-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md'
---

<frozen-after-approval reason="human-owned publication intent — do not modify unless the Administrator renegotiates">

## Intent

**Problem:** Two Administrator-approved 2026-08-02 course corrections are already staged as one
parent-repository snapshot, but the commit gate requires one exact standalone owner for the aggregate
changed-file set.

**Approach:** Authorize exactly one parent commit containing this ownership envelope plus the staged
planning, retrospective, documentation, test-comment, and root gitlink updates. This spec owns only
the publication envelope; the two course-correction proposals retain their separate intent and
evidence claims.

## Boundaries & Constraints

**Always:** Keep the staged path set equal to this File Scope; preserve the approved proposal and
checklist content; require both FrontComposer `d78f57a6` and Tenants `085e5021` to be reachable from
their fetched `origin/main`; validate the message before and after commit; push only after the
branch is published for PR creation.

**Ask First:** Any staged path addition, removal, rename, content change beyond this envelope, OID
change, remote divergence, merge, rebase, force push, or broader ownership claim.

**Never:** Edit submodule content, initialize or update nested submodules, bypass hooks, use a scope
override, rewrite history, or represent this publication as EventStore-to-Memories full-stack proof.

## Ownership Partition

- The Epic 23 ingestion-invariant checklist workflow retains ownership of the corrective review
  matrices and six-row gates.
- The AppHost/EventStore validation correct-course workflow retains ownership of the formal
  `23.7-APPHOST-EVENTSTORE-FULLSTACK` acceptance and evidence-claim boundary.
- Dependency-sync sessions retain ownership of the two root submodule pointer updates.
- This spec owns only the exact aggregate commit and PR publication envelope.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/epic-24-context.md`
- `_bmad-output/implementation-artifacts/epic-24-retro-2026-07-06.md`
- `_bmad-output/implementation-artifacts/epic-25-context.md`
- `_bmad-output/implementation-artifacts/epic-25-retro-2026-07-12.md`
- `_bmad-output/implementation-artifacts/spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md`
- `_bmad-output/implementation-artifacts/spec-publish-epic-23-review-and-eventstore-evidence-2026-08-02.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02-apphost-eventstore-validation.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md`
- `docs/dev/eventstore-integration.md`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Tenants`
- `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs`

## Acceptance Criteria

- The staged path set equals the 16 paths in File Scope with no additions or omissions.
- Scope, tenant-isolation, review-readiness, whitespace, and commitlint gates pass without bypasses.
- Each gitlink commit is reachable from its fetched submodule `origin/main`.
- The pull-request title is an explicit Conventional Commit subject and validates with commitlint.

</frozen-after-approval>

## Approval

The Administrator explicitly requested commit and pull-request creation for this exact staged
snapshot on 2026-08-02 after the repository hook rejected the mixed set for lacking a standalone
File Scope owner.

## Verification

- `git diff --cached --name-only` must equal the File Scope above.
- `git diff --cached --check` must report no whitespace or conflict-marker errors.
- `python3 tools/check-story-file-scope.py --story-key spec-publish-epic-23-review-and-eventstore-evidence-2026-08-02 --staged` must pass all 16 paths.
- `python3 tools/check-tenant-isolation-evidence.py --story-key spec-publish-epic-23-review-and-eventstore-evidence-2026-08-02 --staged` must pass.
- `python3 tools/check-story-review-readiness.py --story-key spec-publish-epic-23-review-and-eventstore-evidence-2026-08-02 --staged --derive-cumulative` must pass.
- Commitlint must pass before and after the commit, and across the complete push range.
