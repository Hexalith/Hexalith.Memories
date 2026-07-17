# Story 27.1 Create-Story Scope Evidence

Generated: 2026-07-16

## Declared Baseline Snapshot

- Repository baseline: `119c0a49`.
- Pre-create `_bmad-output/implementation-artifacts/sprint-status.yaml` SHA-256: `a0a85ef2a7dfd22493b677976e4eb9bd14f37b668ab03bdbe4237bd6c0bd4633`.
- Story file state: absent.
- Evidence file state: absent.
- Story-owned sprint values before create-story:

```yaml
epic-27: backlog
27-1-access-telemetry-retention-ownership-decision: backlog
```

The sprint-status file was already modified by the user relative to `119c0a49`; the SHA above freezes that whole-file pre-create snapshot without assigning those existing edits to Story 27.1.

## Story-Owned Diff

```diff
- epic-27: backlog
- 27-1-access-telemetry-retention-ownership-decision: backlog
+ epic-27: in-progress
+ 27-1-access-telemetry-retention-ownership-decision: ready-for-dev
```

Post-create sprint-status SHA-256 at reconciliation: `2aa6b1a96ecd87a9cbc0f42f8b7958ccbeb157900bbe519061ad663689d3f0c9`.

## Reconciled Name-Status Set

```text
?? _bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md
 M _bmad-output/implementation-artifacts/sprint-status.yaml
?? _bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md
```

Matched File List: 3/3.

## Same-File Exclusions

The following `sprint-status.yaml` content existed in the declared baseline snapshot and is user-owned, not Story 27.1 create-story work:

- the Epic 27 heading, Story 27.2, Story 27.3, and Epic 27 retrospective rows;
- the Epic 18 contract-document drift-guard action change;
- the Epic 21 migration-marker action change;
- the A41 action comment that keeps the action `open`;
- every other sprint-status line outside the two story-owned keys shown in the scoped diff.

Post-baseline committed exclusions are also user-owned and outside Story 27.1:

- the Epic 28 registration (`epic-28`, Story 28.1, and its retrospective) added by the approved 2026-07-17 course correction;
- the Epic 18 contract-document action-item transition and Epic 19 migration-marker action-item transition;
- later comments or state changes on any action item other than the Story 27.1 story/epic workflow keys.

## Reproducible Monotonic-Status Verification

The create-stage values above are frozen by the post-create SHA and scoped diff.
This current-state check intentionally accepts only valid forward workflow
transitions, so it remains reproducible after development and review.

```bash
git status --short -- \
  _bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md \
  _bmad-output/implementation-artifacts/sprint-status.yaml \
  _bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md

python3 - <<'PY'
from pathlib import Path
import yaml

status = yaml.safe_load(Path(
    "_bmad-output/implementation-artifacts/sprint-status.yaml"
).read_text())["development_status"]
assert status["epic-27"] in {
    "in-progress",
    "done",
}
assert status["27-1-access-telemetry-retention-ownership-decision"] in {
    "ready-for-dev",
    "in-progress",
    "blocked",
    "review",
    "done",
}
PY
```

The declared pre-create SHA, exact before-values, scoped diff, exclusions, and
monotonic-status command together distinguish Story 27.1 changes from concurrent
edits in the same YAML file.
