# Story 27.2 Create-Story Scope Evidence

Generated: 2026-07-17

## Declared Baseline Snapshot

- Repository baseline: `4856b0ab5d927ad07d82e5bed9b61597a380269e`.
- Pre-create `_bmad-output/implementation-artifacts/sprint-status.yaml` SHA-256: `39b50b4c6a49553494bc3b2e7aeb58f76c0c84e421362984b84701d47821fad6`.
- Story file state: absent.
- Evidence file state: absent.
- Story-owned sprint values before create-story:

```yaml
epic-27: in-progress
27-2-bounded-retention-ttl-and-purge-implementation: backlog
```

The unrelated untracked `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17-infrastructure-dependency-abstraction.md` existed before Story 27.2 creation and remains user-owned.

## Story-Owned Diff

```diff
  epic-27: in-progress
- 27-2-bounded-retention-ttl-and-purge-implementation: backlog
+ 27-2-bounded-retention-ttl-and-purge-implementation: ready-for-dev
```

Post-create sprint-status SHA-256 at reconciliation: `ce5b010c7e388496c02829a46e02c67c8c128c3f2eb86803db8f93f12cb45f6c`.

## Reconciled Name-Status Set

```text
?? _bmad-output/implementation-artifacts/27-2-bounded-retention-ttl-and-purge-implementation.md
 M _bmad-output/implementation-artifacts/sprint-status.yaml
?? _bmad-output/implementation-artifacts/tests/27-2-create-story-scope-evidence.md
```

Matched File List: 3/3.

## Same-File Exclusions

All `sprint-status.yaml` content outside the single Story 27.2 status key is excluded from Story 27.2 create-story ownership, including:

- `epic-27: in-progress`, Story 27.1 `done`, Story 27.3 `backlog`, and the Epic 27 retrospective row;
- every Epic 28 status/action and every A41/deferred-work action, including the still-open retention action;
- every other epic, story, retrospective, comment, and action-item row.

The unrelated infrastructure-dependency-abstraction planning proposal is outside both the sprint-status diff and the Story 27.2 File List.

## Reproducible Monotonic-Status Verification

The create-stage values above are frozen by the post-create SHA and scoped diff. This current-state check intentionally accepts only valid forward workflow transitions so it remains reproducible after development and review.

```bash
git status --short -- \
  _bmad-output/implementation-artifacts/27-2-bounded-retention-ttl-and-purge-implementation.md \
  _bmad-output/implementation-artifacts/sprint-status.yaml \
  _bmad-output/implementation-artifacts/tests/27-2-create-story-scope-evidence.md

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
assert status["27-2-bounded-retention-ttl-and-purge-implementation"] in {
    "ready-for-dev",
    "in-progress",
    "blocked",
    "review",
    "done",
}
PY
```

The declared pre-create SHA, exact before-value, scoped diff, exclusions, and monotonic-status command distinguish Story 27.2 changes from concurrent work in the same YAML file.
