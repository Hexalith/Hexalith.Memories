# Story 27.3 Create-Story Scope Evidence

Generated: 2026-07-18

## Declared Baseline Snapshot

- Repository baseline: `272c33bc5d30d71ac46f20e703b9d5456e75a093`.
- Creation commit: `6d7fd8aaa0a2fc58de741e31f38544fc15a10c08`.
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

## Reproducible Scope and Workflow-State Verification

This verifier reconstructs the pre-create snapshot, checks both declared sprint hashes, proves the immutable creation commit's scoped 3/3 name-status set, rejects duplicate YAML keys, and binds the current story header to sprint status. Statuses at or beyond `review` additionally require the predecessor, task, checkpoint, and phase-ledger gates; a syntactically allowed status is never completion evidence by itself.

```bash
python3 - <<'PY'
import hashlib
import re
import subprocess
from pathlib import Path

import yaml

BASELINE = "272c33bc5d30d71ac46f20e703b9d5456e75a093"
CREATION = "6d7fd8aaa0a2fc58de741e31f38544fc15a10c08"
PRE_CREATE_SHA = "f2cc7a2520634d2ca280622faf1494477c0ec5500e24ae0c8b7baf36f1dc13c8"
POST_CREATE_SHA = "cb351de28a5ac78667cfdd57d67a45d6e5c4ab53a429cf50985819126be23116"
KEY = "27-3-retention-verification-operations-runbook-and-a41-close-out"
STORY = Path("_bmad-output/implementation-artifacts/27-3-retention-verification-operations-runbook-and-a41-close-out.md")
SPRINT = Path("_bmad-output/implementation-artifacts/sprint-status.yaml")
EVIDENCE = Path("_bmad-output/implementation-artifacts/tests/27-3-create-story-scope-evidence.md")
PATHS = [STORY.as_posix(), SPRINT.as_posix(), EVIDENCE.as_posix()]


class UniqueKeyLoader(yaml.SafeLoader):
    """Reject duplicate mapping keys instead of silently selecting one status."""


def construct_unique_mapping(loader, node, deep=False):
    mapping = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node, deep=deep)
        if key in mapping:
            raise AssertionError(f"duplicate YAML key: {key}")
        mapping[key] = loader.construct_object(value_node, deep=deep)
    return mapping


UniqueKeyLoader.add_constructor(
    yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG,
    construct_unique_mapping,
)


def git(*arguments):
    return subprocess.run(
        ["git", *arguments],
        check=True,
        capture_output=True,
        text=True,
    ).stdout


for path in (STORY, SPRINT, EVIDENCE):
    assert path.is_file(), f"missing governed artifact: {path}"

creation_sprint = git("show", f"{CREATION}:{SPRINT.as_posix()}")
assert hashlib.sha256(creation_sprint.encode()).hexdigest() == POST_CREATE_SHA
ready_line = f"  {KEY}: ready-for-dev\n"
backlog_line = f"  {KEY}: backlog\n"
assert creation_sprint.count(ready_line) == 1
pre_create_sprint = creation_sprint.replace(ready_line, backlog_line, 1)
assert hashlib.sha256(pre_create_sprint.encode()).hexdigest() == PRE_CREATE_SHA

expected_name_status = [
    f"A\t{STORY.as_posix()}",
    f"M\t{SPRINT.as_posix()}",
    f"A\t{EVIDENCE.as_posix()}",
]
actual_name_status = git(
    "diff",
    "--name-status",
    BASELINE,
    CREATION,
    "--",
    *PATHS,
).splitlines()
assert actual_name_status == expected_name_status, actual_name_status

status_document = yaml.load(SPRINT.read_text(), Loader=UniqueKeyLoader)
status = status_document["development_status"]
current = status[KEY]
story_text = STORY.read_text()
story_status = re.search(r"^Status: ([a-z-]+)$", story_text, re.MULTILINE)
assert story_status is not None
assert story_status.group(1) == current
assert current in {"ready-for-dev", "in-progress", "review", "done"}
assert status["epic-27"] in {"in-progress", "done"}

if current in {"review", "done"}:
    assert status["27-2-bounded-retention-ttl-and-purge-implementation"] == "done"
    assert not re.search(r"^- \[ \] Task ", story_text, re.MULTILINE)
    checkpoint_rows = [line for line in story_text.splitlines() if line.startswith("| C")]
    assert len(checkpoint_rows) == 7
    assert all("| reviewed | complete |" in line for line in checkpoint_rows)
    assert "| dev-story |" in story_text

if current == "done":
    assert "| code-review |" in story_text
    assert not re.search(r"^- \[ \] \[Review\]", story_text, re.MULTILINE)
PY
```

The immutable creation commit, reconstructed pre-create SHA, exact before/after value, scoped name-status assertion, same-file exclusions, and current parity/gate checks distinguish Story 27.3 ownership from concurrent work and fail closed on missing or contradictory artifacts.
