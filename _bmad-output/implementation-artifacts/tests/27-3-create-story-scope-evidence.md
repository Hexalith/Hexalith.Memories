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
PREDECESSOR_KEY = "27-2-bounded-retention-ttl-and-purge-implementation"
A41_ID = "20.5-A41-ACCESS-TELEMETRY-RETENTION"
STORY = Path("_bmad-output/implementation-artifacts/27-3-retention-verification-operations-runbook-and-a41-close-out.md")
SPRINT = Path("_bmad-output/implementation-artifacts/sprint-status.yaml")
EVIDENCE = Path("_bmad-output/implementation-artifacts/tests/27-3-create-story-scope-evidence.md")
DEFERRED = Path("_bmad-output/implementation-artifacts/deferred-work.md")
MATRIX = Path("_bmad-output/implementation-artifacts/tests/27-3-retention-verification-evidence.md")
MATRIX_ANCHOR = MATRIX.as_posix()
PATHS = [STORY.as_posix(), SPRINT.as_posix(), EVIDENCE.as_posix()]
STATUS_RANK = {"ready-for-dev": 0, "in-progress": 1, "review": 2, "done": 3}


def require(condition, message):
    if not condition:
        raise SystemExit(message)


class UniqueKeyLoader(yaml.SafeLoader):
    """Reject duplicate mapping keys instead of silently selecting one status."""


def construct_unique_mapping(loader, node, deep=False):
    mapping = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node, deep=deep)
        if key in mapping:
            raise ValueError(f"duplicate YAML key: {key}")
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


def load_yaml(text):
    document = yaml.load(text, Loader=UniqueKeyLoader)
    require(isinstance(document, dict), "YAML root must be a mapping")
    return document


def unique_story_status(text, source):
    matches = re.findall(r"^Status: ([a-z-]+)$", text, re.MULTILINE)
    require(len(matches) == 1, f"{source} must contain exactly one Status line: {matches}")
    require(matches[0] in STATUS_RANK, f"unsupported {source} status: {matches[0]}")
    return matches[0]


def section(text, start, end):
    require(start in text, f"missing section start: {start}")
    body = text.split(start, 1)[1]
    if end is not None:
        require(end in body, f"missing section end: {end}")
        body = body.split(end, 1)[0]
    return body


def checkpoint_rows(text):
    body = section(text, "## Implementation Checkpoints", "## Dev Notes")
    lines = [line for line in body.splitlines() if re.match(r"^\| C[0-9]+ -", line)]
    parsed = []
    for line in lines:
        cells = [cell.strip() for cell in line.strip("|").split("|")]
        require(len(cells) == 6, f"checkpoint row must have six cells: {line}")
        match = re.match(r"^C([0-9]+) -", cells[0])
        require(match is not None, f"invalid checkpoint identifier: {cells[0]}")
        parsed.append((int(match.group(1)), cells))
    require([identifier for identifier, _ in parsed] == list(range(7)), f"checkpoints must be unique ordered C0-C6: {lines}")
    return parsed


def ledger_rows(text):
    body = section(text, "## Change Log", None)
    rows = []
    for line in body.splitlines():
        if not re.match(r"^\| [0-9]{4}-[0-9]{2}-[0-9]{2} \|", line):
            continue
        cells = [cell.strip() for cell in line.strip("|").split("|")]
        require(len(cells) == 5, f"ledger row must have five cells: {line}")
        rows.append(cells)
    require(rows, "change ledger has no phase rows")
    dates = [row[0] for row in rows]
    require(dates == sorted(dates), f"ledger dates are not chronological: {dates}")
    return rows


def successful_phase(row, phase):
    if row[1] != phase:
        return False
    combined = " ".join(row[2:]).lower()
    blockers = ("blocked", "halted", "unavailable", "no implementation", "not captured")
    return not any(blocker in combined for blocker in blockers) and re.search(r"matched [0-9]+/[0-9]+", row[4]) is not None


for path in (STORY, SPRINT, EVIDENCE):
    require(path.is_file(), f"missing governed artifact: {path}")

creation_sprint = git("show", f"{CREATION}:{SPRINT.as_posix()}")
require(hashlib.sha256(creation_sprint.encode()).hexdigest() == POST_CREATE_SHA, "creation sprint hash mismatch")
ready_line = f"  {KEY}: ready-for-dev\n"
backlog_line = f"  {KEY}: backlog\n"
require(creation_sprint.count(ready_line) == 1, "creation sprint must contain exactly one ready-for-dev row")
pre_create_sprint = creation_sprint.replace(ready_line, backlog_line, 1)
require(hashlib.sha256(pre_create_sprint.encode()).hexdigest() == PRE_CREATE_SHA, "pre-create sprint hash mismatch")

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
require(actual_name_status == expected_name_status, f"creation name-status mismatch: {actual_name_status}")

status_document = load_yaml(SPRINT.read_text(encoding="utf-8"))
status = status_document["development_status"]
current = status[KEY]
require(current in STATUS_RANK, f"unsupported sprint status: {current}")
story_text = STORY.read_text(encoding="utf-8")
story_status = unique_story_status(story_text, "working story")
require(story_status == current, f"story/sprint status mismatch: {story_status} != {current}")

head_sprint = load_yaml(git("show", f"HEAD:{SPRINT.as_posix()}"))
head_story = git("show", f"HEAD:{STORY.as_posix()}")
prior_status = head_sprint["development_status"][KEY]
require(unique_story_status(head_story, "HEAD story") == prior_status, "HEAD story/sprint status mismatch")
require(STATUS_RANK[current] >= STATUS_RANK[prior_status], f"non-monotonic story transition: {prior_status} -> {current}")

epic_status = status["epic-27"]
require(epic_status in {"in-progress", "done"}, f"unsupported Epic 27 status: {epic_status}")
if current != "done":
    require(epic_status == "in-progress", f"Epic 27 cannot be done while Story 27.3 is {current}")

checkpoints = checkpoint_rows(story_text)
if status[PREDECESSOR_KEY] == "done":
    c0 = checkpoints[0][1]
    require(c0[3].startswith("reviewed "), f"C0 review is stale: {c0[3]}")
    require(c0[4].startswith("complete"), f"C0 is not complete: {c0[4]}")
    require(re.fullmatch(r"[0-9]{4}-[0-9]{2}-[0-9]{2}", c0[5]) is not None, f"C0 completion date is invalid: {c0[5]}")

if current in {"review", "done"}:
    require(status[PREDECESSOR_KEY] == "done", "Story 27.2 must be done before review")
    task_body = section(story_text, "## Tasks / Subtasks", "### Review Findings")
    require(re.search(r"^\s*- \[ \] ", task_body, re.MULTILINE) is None, "review requires every task and subtask checked")
    for identifier, cells in checkpoints:
        require(cells[3].startswith("reviewed "), f"C{identifier} is not reviewed: {cells[3]}")
        require(cells[4].startswith("complete"), f"C{identifier} is not complete: {cells[4]}")
        require(re.fullmatch(r"[0-9]{4}-[0-9]{2}-[0-9]{2}", cells[5]) is not None, f"C{identifier} completion date is invalid: {cells[5]}")
    ledger = ledger_rows(story_text)
    successful_dev = [index for index, row in enumerate(ledger) if successful_phase(row, "dev-story")]
    require(successful_dev, "review requires a successful reconciled dev-story row")

if current == "done":
    require(re.search(r"^- \[ \] \[Review\]", story_text, re.MULTILINE) is None, "done requires every review finding resolved")
    successful_review = [index for index, row in enumerate(ledger) if successful_phase(row, "code-review")]
    require(successful_review and successful_review[-1] > successful_dev[-1], "done requires a successful final code-review row after development")
    require(ledger[-1][1] == "code-review" and successful_phase(ledger[-1], "code-review"), "final ledger row must be successful code-review")

    require(DEFERRED.is_file(), f"missing deferred register: {DEFERRED}")
    deferred_text = DEFERRED.read_text(encoding="utf-8")
    residual_start = deferred_text.find(f"- ID: {A41_ID}")
    require(residual_start >= 0, "missing A41 residual")
    residual_end = deferred_text.find("\n## ", residual_start)
    residual = deferred_text[residual_start:] if residual_end < 0 else deferred_text[residual_start:residual_end]
    require(re.search(r"^\s*- Status: resolved$", residual, re.MULTILINE) is not None, "A41 residual is not resolved")
    require(MATRIX_ANCHOR in residual, "A41 residual does not cite the canonical matrix")

    actions = [item for item in status_document.get("action_items", []) if A41_ID in str(item.get("action", ""))]
    require(len(actions) == 1, f"expected one A41 sprint action: {actions}")
    require(actions[0].get("status") == "done", "A41 sprint action is not done")
    require(actions[0].get("evidence") == MATRIX_ANCHOR, "A41 sprint action evidence anchor differs")

    require(MATRIX.is_file(), f"missing canonical matrix: {MATRIX}")
    matrix_text = MATRIX.read_text(encoding="utf-8")
    require(re.search(r"^postflight_status: passed$", matrix_text, re.MULTILINE) is not None, "postflight did not pass")
    require(re.search(r"^postflight_index_tree: [0-9a-f]{40}$", matrix_text, re.MULTILINE) is not None, "missing postflight index tree")
    require(re.search(r"^approved_mutation_manifest_sha256: [0-9a-f]{64}$", matrix_text, re.MULTILINE) is not None, "missing mutation manifest hash")
    require(re.search(r"^publish_verify_status: passed$", matrix_text, re.MULTILINE) is not None, "publish verification did not pass")
    require(re.search(r"^published_commit: [0-9a-f]{40}$", matrix_text, re.MULTILINE) is not None, "missing published commit")
PY
```

The immutable creation commit, reconstructed pre-create SHA, exact before/after value, scoped name-status assertion, same-file exclusions, and current parity/gate checks distinguish Story 27.3 ownership from concurrent work and fail closed on missing or contradictory artifacts.
