# Story 27.3 Create-Story Scope Evidence

Generated: 2026-07-18

## Declared Baseline Snapshot

- Repository baseline: `272c33bc5d30d71ac46f20e703b9d5456e75a093`.
- Pre-create `_bmad-output/implementation-artifacts/sprint-status.yaml` SHA-256: `f2cc7a2520634d2ca280622faf1494477c0ec5500e24ae0c8b7baf36f1dc13c8`.
- Story file state: absent.
- Evidence file state: absent.
- Story-owned sprint values before create-story:

```yaml
epic-27: in-progress
27-3-retention-verification-operations-runbook-and-a41-close-out: backlog
```

The pre-existing dirty `27-2-bounded-retention-ttl-and-purge-implementation: in-progress` transition and modified `references/Hexalith.EventStore` submodule are user-owned and excluded from Story 27.3.

## Story-Owned Diff

```diff
  epic-27: in-progress
- 27-3-retention-verification-operations-runbook-and-a41-close-out: backlog
+ 27-3-retention-verification-operations-runbook-and-a41-close-out: ready-for-dev
```

Post-create sprint-status SHA-256 at reconciliation: `cb351de28a5ac78667cfdd57d67a45d6e5c4ab53a429cf50985819126be23116`.

## Reconciled Name-Status Set

```text
?? _bmad-output/implementation-artifacts/27-3-retention-verification-operations-runbook-and-a41-close-out.md
 M _bmad-output/implementation-artifacts/sprint-status.yaml
?? _bmad-output/implementation-artifacts/tests/27-3-create-story-scope-evidence.md
```

Matched File List: 3/3.

## Same-File Exclusions

All `sprint-status.yaml` content outside the single Story 27.3 status key is excluded from Story 27.3 create-story ownership, including:

- the pre-existing header/body `last_updated: 2026-07-18` changes and Story 27.2 `in-progress` transition;
- `epic-27: in-progress`, Story 27.1 `done`, and the Epic 27 retrospective row;
- every Epic 28 status/action and every A41/deferred-work action, including the still-open retention action;
- every other epic, story, retrospective, comment, and action-item row.

The dirty `references/Hexalith.EventStore` submodule is outside the sprint-status diff and Story 27.3 File List.

## Reproducible Monotonic-Status Verification

The create-stage values above are frozen by the post-create SHA and scoped diff. This current-state check intentionally accepts only valid forward workflow transitions so it remains reproducible after development and review.

```bash
git status --short -- \
  _bmad-output/implementation-artifacts/27-3-retention-verification-operations-runbook-and-a41-close-out.md \
  _bmad-output/implementation-artifacts/sprint-status.yaml \
  _bmad-output/implementation-artifacts/tests/27-3-create-story-scope-evidence.md

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
assert status["27-3-retention-verification-operations-runbook-and-a41-close-out"] in {
    "ready-for-dev",
    "in-progress",
    "review",
    "done",
}
PY
```

The declared pre-create SHA, exact before-value, scoped diff, exclusions, and monotonic-status command distinguish Story 27.3 changes from concurrent work in the same YAML file.
