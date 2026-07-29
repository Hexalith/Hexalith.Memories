#!/usr/bin/env python3
"""Validate a BMad story's review readiness before it advances to review or done.

Enforces the mechanically checkable subset of `_bmad/custom/story-phase-ledger.md`:

    C1  File List completeness, in both directions, against the story's diff.
    C2  Required phase-ledger rows present with non-placeholder cells.
    C3  A recognised story/spec status.
    C4  sprint-status.yaml agreement.
    C6  No evidence/checkpoint row left `pending` once the story reaches
        review or done.

There is deliberately no C5. A proposed `File List` / `File Scope` set-agreement
check was specified, implemented, and withdrawn after measurement: `File Scope`
is a forward-looking allow-list and `File List` is a backward-looking record, so
"allowed but unchanged" is the normal case (17 of 21 measured artifacts), and
"changed but unscoped" is legitimate whenever a `Scope-Override:` commit trailer
authorised it. Those trailers live in the commit message, which a story-file
check cannot see. `tools/check-story-file-scope.py` already enforces that
relation at commit time, with override support.

A green exit is a floor, never a ceiling: this tool does not verify count
arithmetic, discovery evidence, or whether a recorded command was truly run.
Those remain with the code-review ledger auditor.

Exit 0 on pass or a documented no-op, exit 1 on violation. All output to stdout.
"""

from __future__ import annotations

import argparse
import fnmatch
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path


STORY_KEY_PATTERN = re.compile(
    r"(?<![\w-])(\d+-\d+-[a-z](?:[a-z0-9-]*[a-z0-9])?)(?![\w-])",
    re.IGNORECASE,
)
SPEC_KEY_PATTERN = re.compile(
    r"(?<![\w-])(spec-[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)(?![\w-])",
    re.IGNORECASE | re.ASCII,
)
AND_JOINED_STORY_KEYS_PATTERN = re.compile(
    r"(?<![\w-])(\d+-\d+-[a-z](?:[a-z0-9-]*?[a-z0-9])?)-and-(\d+-\d+-[a-z](?:[a-z0-9-]*[a-z0-9])?)(?![\w-])",
    re.IGNORECASE,
)
BACKTICK_PATTERN = re.compile(r"`([^`]+)`")
CODE_FENCE_PATTERN = re.compile(r"^\s*(`{3,}|~{3,})(.*)$")
STATUS_LINE_PATTERN = re.compile(r"^\s*(?:\*\*)?status(?:\*\*)?\s*:\s*(.+?)\s*$", re.IGNORECASE)

# The canonical phase-ledger header from story-phase-ledger.md. All five cells
# must be present for the table to be the ledger rather than an unrelated table.
LEDGER_HEADER_CELLS = ("date", "phase", "change", "test count", "file list reconciliation")
# `correct-course` was admitted to the canonical set on 2026-07-28: an approved
# sprint change proposal landing between creation and development changes the
# story's File List, and before the amendment that had no row of its own.
CANONICAL_PHASES = (
    "create-story",
    "correct-course",
    "dev-story",
    "qa-gap-closure",
    "code-review",
)

# A cell that records nothing. `-` and the em dash read as "no value supplied",
# which is exactly the placeholder this gate exists to reject.
PLACEHOLDER_CELLS = {"", "-", "--", "—", "–", "tbd", "n/a", "na", "none", "todo"}

# Evidence/checkpoint tables are identified by their status column, not by
# their first column, because the first column varies across story generations
# (`Evidence item`, `Checkpoint`, `Gate`, `Item`).
EVIDENCE_STATUS_HEADERS = ("review status", "review state")
PENDING_CELLS = {"", "-", "--", "—", "–", "pending", "not started", "todo"}
# A row that names an owner, consequence and reopen trigger is a recorded
# decision rather than an unanswered question, so `blocked` is accepted.
BLOCKED_MARKERS = ("blocked", "waived", "deferred", "accepted", "not applicable", "n/a")

VALID_STATUSES = ("backlog", "ready-for-dev", "in-progress", "review", "done")
# C1 needs the story's cumulative diff, which is only meaningful while the
# story is being worked. See EP-5 in the sprint change proposal.
C1_STATUSES = ("in-progress", "review")
# C6 fires only once the story claims its evidence is complete.
C6_STATUSES = ("review", "done")

DEFAULT_BRANCHES = ("main", "master")
BYPASS_TRAILER_KEY = "story-review-readiness-bypass"

MATCHED_PATTERN = re.compile(r"matched\s+(\d+)\s*/\s*(\d+)", re.IGNORECASE)
BLOCKED_EVIDENCE_MARKERS = ("blocker", "blocked", "could not run", "not run", "reopen trigger")


@dataclass(frozen=True)
class StorySource:
    name: str
    key: str


@dataclass(frozen=True)
class Exclusion:
    path: str
    owner: str
    reason: str


class ValidationError(Exception):
    """Raised for expected validation failures."""


def read_text_lf(path: Path) -> str:
    """Read a file and normalise it to LF with no BOM.

    `sprint-status.yaml` is `attr/text eol=lf` but materialises CRLF in the
    worktree, and `.md` artifacts materialise CRLF unconditionally. An
    unstripped `\\r` produces false status mismatches, so every parse in this
    module goes through here.
    """
    text = path.read_text(encoding="utf-8")
    if text.startswith("﻿"):
        text = text[1:]
    return text.replace("\r\n", "\n").replace("\r", "\n")


def normalize_path(value: str) -> str:
    cleaned = value.strip().strip("\"'`").replace("\\", "/")
    cleaned = re.sub(r"/+", "/", cleaned)
    if cleaned.startswith("./"):
        cleaned = cleaned[2:]

    parts: list[str] = []
    for part in cleaned.split("/"):
        if part in ("", "."):
            continue
        if part == "..":
            if parts:
                parts.pop()
            continue
        parts.append(part)

    return "/".join(parts)


def normalize_cell(value: str) -> str:
    """Strip markdown emphasis, backticks and YAML quoting so cells compare by content.

    Spec frontmatter writes `status: 'done'`; without the quote strip the value
    reads as `'done'` and fails the C3 vocabulary check on punctuation alone.
    """
    cleaned = value.strip()
    cleaned = re.sub(r"[`*_]", "", cleaned)
    return cleaned.strip().strip("'\"").strip().lower()


def extract_story_keys(value: str) -> list[str]:
    text = value or ""
    keys: list[str] = []
    joined_spans: list[tuple[int, int]] = []

    for match in AND_JOINED_STORY_KEYS_PATTERN.finditer(text):
        keys.extend([match.group(1).lower(), match.group(2).lower()])
        joined_spans.append(match.span())

    for match in STORY_KEY_PATTERN.finditer(text):
        if any(start <= match.start() < end for start, end in joined_spans):
            continue
        keys.append(match.group(1).lower())

    keys.extend(match.group(1).lower() for match in SPEC_KEY_PATTERN.finditer(text))

    return keys


def validate_exact_spec_value(value: str, source: str) -> None:
    """Reject partial standalone-spec matches from CLI and trailer sources."""
    if re.search(r"spec-", value, re.IGNORECASE | re.ASCII) and not SPEC_KEY_PATTERN.fullmatch(value.strip()):
        raise ValidationError(
            f"{source} must be exactly one valid standalone spec key: {value}",
        )


def validate_branch_spec_segments(branch_name: str) -> None:
    """Require each branch segment that names a spec to be an exact spec key."""
    for segment in re.split(r"[/\\]", branch_name):
        if re.search(r"spec-", segment, re.IGNORECASE | re.ASCII) and not SPEC_KEY_PATTERN.fullmatch(segment):
            raise ValidationError(
                "Branch name segment must be exactly one valid standalone spec key: "
                + segment,
            )


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate a story's File List, phase ledger, status, and evidence rows before review.",
    )
    parser.add_argument("--story-key", help="Explicit story key. Highest precedence.")
    parser.add_argument("--branch-name", help="Branch name to scan for a story key.")
    parser.add_argument("--commit-message-file", help="Commit message file to parse trailers from.")
    parser.add_argument("--commit-sha", help="Commit SHA whose message should be parsed.")
    parser.add_argument("--changed-file", action="append", default=[], help="Changed file path. May be repeated.")
    parser.add_argument("--changed-files-file", help="File containing changed paths, one per line.")
    parser.add_argument("--staged", action="store_true", help="Add git diff --cached --name-only -- to the changed set.")
    parser.add_argument(
        "--derive-cumulative",
        action="store_true",
        help="Derive the story's cumulative diff from frontmatter baseline_commit..HEAD. "
        "Local hook callers pass this; CI passes the PR base..head set verbatim instead.",
    )
    parser.add_argument(
        "--artifacts-root",
        default="_bmad-output/implementation-artifacts",
        help="Directory containing story artifacts.",
    )
    parser.add_argument(
        "--sprint-status-file",
        default="_bmad-output/implementation-artifacts/sprint-status.yaml",
        help="Sprint status file used for the C4 agreement check.",
    )
    return parser.parse_args(argv)


def run_git(args: list[str], check: bool = True) -> str:
    try:
        completed = subprocess.run(
            ["git", *args],
            text=True,
            capture_output=True,
            check=False,
        )
    except FileNotFoundError as exc:
        raise ValidationError(
            "Required tool not found: 'git' is unavailable. Install Git or ensure 'git' is on PATH."
        ) from exc
    if completed.returncode != 0:
        if not check:
            return ""
        raise ValidationError(completed.stderr.strip() or f"git {' '.join(args)} failed")
    return completed.stdout


def read_commit_message(args: argparse.Namespace) -> str:
    if args.commit_message_file:
        return Path(args.commit_message_file).read_text(encoding="utf-8")
    if args.commit_sha:
        return run_git(["log", "-1", "--format=%B", args.commit_sha])
    return ""


def parse_trailers(message: str) -> tuple[list[str], str | None]:
    """Return story keys and the bypass reason, if any."""
    if not message.strip():
        return [], None

    try:
        completed = subprocess.run(
            ["git", "interpret-trailers", "--parse"],
            input=message,
            text=True,
            capture_output=True,
            check=False,
        )
    except FileNotFoundError as exc:
        raise ValidationError(
            "Required tool not found: 'git interpret-trailers' is unavailable. "
            "Install Git (with the interpret-trailers helper) or ensure 'git' is on PATH."
        ) from exc

    if completed.returncode != 0:
        detail = completed.stderr.strip() or "git interpret-trailers --parse failed"
        raise ValidationError(
            "Git with 'interpret-trailers' is required. "
            "Install Git (with the interpret-trailers helper) or ensure 'git' is on PATH. "
            f"Command output: {detail}"
        )

    story_keys: list[str] = []
    bypass: str | None = None
    for line in completed.stdout.splitlines():
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        normalized_key = key.strip().lower()
        normalized_value = value.strip()
        if normalized_key in {"story", "story-key"}:
            keys_in_value = extract_story_keys(normalized_value)
            if len(keys_in_value) > 1:
                raise ValidationError(f"Multiple story keys in single trailer: {line}")
            validate_exact_spec_value(normalized_value, "Story trailer value")
            if not keys_in_value:
                raise ValidationError(f"Malformed Story trailer: {line}")
            story_keys.append(keys_in_value[0])
        elif normalized_key == BYPASS_TRAILER_KEY:
            if not normalized_value:
                raise ValidationError(
                    "Story-Review-Readiness-Bypass trailer requires a non-empty reason."
                )
            bypass = normalized_value

    if len(set(story_keys)) > 1:
        raise ValidationError(
            "Conflicting Story/Story-Key trailers: " + ", ".join(sorted(set(story_keys))),
        )
    return story_keys, bypass


def resolve_story_key(args: argparse.Namespace, trailer_keys: list[str]) -> StorySource | None:
    """Resolve the governed story, or None when no key is available.

    Returning None is a documented no-op rather than an error: `correct-course`
    commits carry no Story: trailer by convention, and the gate must not fail
    them.
    """
    sources: list[StorySource] = []

    explicit_raw = (args.story_key or "").strip()
    if explicit_raw:
        explicit_keys = extract_story_keys(explicit_raw)
        if len(explicit_keys) > 1:
            raise ValidationError(
                "--story-key value contains multiple story keys: " + ", ".join(sorted(set(explicit_keys))),
            )
        validate_exact_spec_value(explicit_raw, "--story-key value")
        if not explicit_keys:
            raise ValidationError(f"--story-key value is not a valid story key: {explicit_raw}")
        unique_explicit = sorted(set(explicit_keys))
        sources.append(StorySource("cli", unique_explicit[0]))

    if trailer_keys:
        sources.append(StorySource("trailer", trailer_keys[0]))

    branch_name = args.branch_name or ""
    branch_keys = extract_story_keys(branch_name)
    if branch_keys:
        if len(branch_keys) > 1:
            raise ValidationError(
                "Branch name contains multiple story keys: " + ", ".join(sorted(set(branch_keys))),
            )
        validate_branch_spec_segments(branch_name)
        unique_branch = sorted(set(branch_keys))
        sources.append(StorySource("branch", unique_branch[0]))
    else:
        validate_branch_spec_segments(branch_name)

    if not sources:
        return None

    unique = {source.key for source in sources}
    if len(unique) > 1:
        details = ", ".join(f"{source.name}={source.key}" for source in sources)
        raise ValidationError(f"Conflicting story keys: {details}")

    for name in ("cli", "trailer", "branch"):
        for source in sources:
            if source.name == name:
                return source
    return sources[0]


def extract_backtick_path(line: str) -> str | None:
    match = BACKTICK_PATTERN.search(line)
    return match.group(1) if match else None


def iter_unfenced_lines(text: str):
    """Yield (index, raw_line) for lines outside fenced code blocks."""
    fence_marker: str | None = None
    for index, raw_line in enumerate(text.splitlines()):
        fence_match = CODE_FENCE_PATTERN.match(raw_line)
        if fence_match:
            marker = fence_match.group(1)
            trailing = fence_match.group(2)
            if fence_marker is None:
                fence_marker = marker
                continue
            if marker[0] == fence_marker[0] and len(marker) >= len(fence_marker) and not trailing.strip():
                fence_marker = None
                continue
        if fence_marker is not None:
            continue
        yield index, raw_line


def parse_frontmatter(text: str) -> dict[str, str]:
    lines = text.splitlines()
    if not lines or lines[0].strip() != "---":
        return {}
    values: dict[str, str] = {}
    for line in lines[1:]:
        if line.strip() == "---":
            break
        if ":" not in line or line.startswith((" ", "\t", "-")):
            continue
        key, value = line.split(":", 1)
        values[key.strip().lower()] = value.strip().strip("'\"")
    return values


def parse_status(text: str) -> str | None:
    """Return the artifact status: a `Status:` body line, else frontmatter `status:`."""
    for _, raw_line in iter_unfenced_lines(text):
        stripped = raw_line.strip()
        if not stripped or stripped.startswith("|"):
            continue
        match = STATUS_LINE_PATTERN.match(stripped)
        if match:
            value = normalize_cell(match.group(1))
            # `Status: done` in prose is the story's own declaration; a table
            # row or a longer sentence is not.
            if value and len(value.split()) <= 3:
                return value
    frontmatter = parse_frontmatter(text)
    if "status" in frontmatter:
        return normalize_cell(frontmatter["status"])
    return None


def split_table_row(line: str) -> list[str] | None:
    """Split a markdown table row on unescaped pipes only.

    Live ledger cells embed shell pipelines as `... 2>&1 \\| grep ...`. A naive
    split on every `|` shreds those rows into phantom rows whose columns are
    shifted, which silently misreads the phase and reconciliation cells.
    """
    stripped = line.strip()
    if not stripped.startswith("|"):
        return None

    cells: list[str] = []
    current: list[str] = []
    escaped = False
    for char in stripped:
        if escaped:
            current.append(char)
            escaped = False
            continue
        if char == "\\":
            current.append(char)
            escaped = True
            continue
        if char == "|":
            cells.append("".join(current))
            current = []
            continue
        current.append(char)
    cells.append("".join(current))

    # A row starts and ends with a delimiter, so the first and last fragments
    # are empty padding rather than cells.
    if cells and not cells[0].strip():
        cells = cells[1:]
    if cells and not cells[-1].strip():
        cells = cells[:-1]
    return [cell.strip() for cell in cells]


def is_separator_row(cells: list[str]) -> bool:
    return bool(cells) and all(re.fullmatch(r":?-{1,}:?", cell.strip()) for cell in cells if cell.strip())


def parse_tables(text: str) -> list[tuple[list[str], list[list[str]]]]:
    """Return (header cells, data rows) for every markdown table outside fences."""
    tables: list[tuple[list[str], list[list[str]]]] = []
    pending_header: list[str] | None = None
    current: tuple[list[str], list[list[str]]] | None = None

    for _, raw_line in iter_unfenced_lines(text):
        cells = split_table_row(raw_line)
        if cells is None:
            if current:
                tables.append(current)
                current = None
            pending_header = None
            continue
        if current is None:
            if pending_header is not None and is_separator_row(cells):
                current = (pending_header, [])
                pending_header = None
                continue
            pending_header = cells
            continue
        if is_separator_row(cells):
            continue
        current[1].append(cells)

    if current:
        tables.append(current)
    return tables


def find_ledger(text: str) -> list[list[str]] | None:
    for header, rows in parse_tables(text):
        normalized = [normalize_cell(cell) for cell in header]
        if all(any(wanted == cell for cell in normalized) for wanted in LEDGER_HEADER_CELLS):
            return rows
    return None


def find_evidence_tables(text: str) -> list[tuple[int, list[list[str]]]]:
    """Return (status column index, rows) for each evidence/checkpoint table."""
    found: list[tuple[int, list[list[str]]]] = []
    for header, rows in parse_tables(text):
        normalized = [normalize_cell(cell) for cell in header]
        for index, cell in enumerate(normalized):
            if cell in EVIDENCE_STATUS_HEADERS:
                found.append((index, rows))
                break
    return found


def parse_section_paths(text: str, heading: str) -> set[str] | None:
    """Collect backticked or bare paths from top-level bullets under a heading."""
    target = heading.strip().lower()
    in_section = False
    paths: set[str] = set()
    found = False

    for _, raw_line in iter_unfenced_lines(text):
        stripped = raw_line.strip()
        if stripped.startswith("#"):
            heading_text = stripped.lstrip("#").strip().lower()
            if heading_text == target:
                in_section = True
                found = True
                continue
            if in_section:
                break
        if not in_section:
            continue
        if not raw_line.startswith("- "):
            continue
        candidate = extract_backtick_path(stripped)
        if candidate is None:
            # `26-5` lists bare paths with no backticks.
            body = stripped[2:].strip()
            token = body.split(" ")[0].split("—")[0].strip()
            candidate = token if "/" in token or token.endswith(".md") else None
        if candidate:
            normalized = normalize_path(candidate)
            if normalized:
                paths.add(normalized)

    return paths if found else None


def parse_exclusions(text: str) -> list[Exclusion]:
    """Parse the machine-readable `### File List Exclusions` block.

    Free prose elsewhere records intent for a human reader; it does not exempt
    a path from this gate.
    """
    exclusions: list[Exclusion] = []
    in_section = False

    for _, raw_line in iter_unfenced_lines(text):
        stripped = raw_line.strip()
        if stripped.startswith("#"):
            heading_text = stripped.lstrip("#").strip().lower()
            if heading_text == "file list exclusions":
                in_section = True
                continue
            if in_section:
                break
        if not in_section or not raw_line.startswith("- "):
            continue

        path = extract_backtick_path(stripped)
        if not path:
            raise ValidationError(f"File List Exclusions entry has no backticked path: {stripped}")
        owner_match = re.search(r"owner\s*:\s*([^;]+)", stripped, re.IGNORECASE)
        if not owner_match:
            raise ValidationError(f"File List Exclusions entry names no owner: {stripped}")
        remainder = stripped[owner_match.end():].strip(" ;—-")
        if not remainder:
            raise ValidationError(f"File List Exclusions entry gives no reason: {stripped}")
        exclusions.append(
            Exclusion(normalize_path(path), owner_match.group(1).strip(), remainder)
        )
    return exclusions


def parse_sprint_status(path: Path) -> dict[str, str]:
    """Map artifact key -> status from the development_status block."""
    if not path.exists():
        raise ValidationError(f"Sprint status file not found: {to_posix(path)}")
    statuses: dict[str, str] = {}
    for line in read_text_lf(path).splitlines():
        if not line.startswith("  ") or line.startswith("    "):
            continue
        stripped = line.strip()
        if not stripped or stripped.startswith("#") or ":" not in stripped:
            continue
        key, value = stripped.split(":", 1)
        key = key.strip()
        if not re.fullmatch(r"[a-z0-9][a-z0-9-]*", key):
            continue
        value = value.split("#", 1)[0].strip()
        if value in VALID_STATUSES:
            statuses[key] = value
    return statuses


def matches_glob(path: str, pattern: str) -> bool:
    return _glob_match(path.split("/"), normalize_path(pattern).split("/"))


def _glob_match(parts: list[str], pat: list[str]) -> bool:
    if not pat:
        return not parts
    head, *rest = pat
    if head == "**":
        for i in range(len(parts) + 1):
            if _glob_match(parts[i:], rest):
                return True
        return False
    if not parts:
        return False
    if fnmatch.fnmatchcase(parts[0], head):
        return _glob_match(parts[1:], rest)
    return False


def to_posix(path: Path) -> str:
    return str(path).replace("\\", "/")


def collect_supplied_changed(args: argparse.Namespace) -> list[str]:
    values: list[str] = list(args.changed_file)
    if args.changed_files_file:
        changed_file = Path(args.changed_files_file)
        if not changed_file.exists():
            raise ValidationError(f"Changed-files input does not exist: {changed_file}")
        values.extend(changed_file.read_text(encoding="utf-8").splitlines())
    if args.staged:
        values.extend(run_git(["diff", "--cached", "--name-only", "--"]).splitlines())
    return [normalized for value in values if (normalized := normalize_path(value))]


def current_branch() -> str:
    return run_git(["rev-parse", "--abbrev-ref", "HEAD"], check=False).strip()


def derive_cumulative_changed(baseline: str, supplied: list[str]) -> tuple[list[str], str | None]:
    """Union of baseline..HEAD, the staged set, and any supplied paths.

    Returns (paths, skip_reason). `baseline..HEAD` is only the story's diff on a
    story branch; on the default branch it returns everything everyone has
    committed since, so C1 is skipped there with an explicit note rather than
    fabricating violations.
    """
    branch = current_branch()
    if branch in DEFAULT_BRANCHES:
        return supplied, (
            f"C1 SKIPPED: HEAD is on default branch '{branch}', where baseline..HEAD "
            "returns unrelated work. C1 is enforced in CI against the PR diff."
        )

    resolved = run_git(["rev-parse", "--verify", f"{baseline}^{{commit}}"], check=False).strip()
    if not resolved:
        raise ValidationError(
            f"frontmatter baseline_commit could not be resolved: {baseline}. "
            "C1 cannot run without a resolvable baseline; unverifiable must not render as passed."
        )

    values = list(supplied)
    values.extend(run_git(["diff", "--name-only", f"{resolved}..HEAD"]).splitlines())
    values.extend(run_git(["diff", "--cached", "--name-only", "--"]).splitlines())
    return [normalized for value in values if (normalized := normalize_path(value))], None


def check_ledger(rows: list[list[str]], status: str) -> list[str]:
    """C2 - required rows present, cells non-placeholder, newest row reconciled."""
    failures: list[str] = []
    phases: list[str] = []
    for cells in rows:
        if len(cells) < 5:
            failures.append(f"C2: ledger row has {len(cells)} cells, expected 5: {' | '.join(cells)}")
            continue
        phase = normalize_cell(cells[1])
        if phase in CANONICAL_PHASES:
            phases.append(phase)

    if not phases:
        failures.append(
            "C2: no ledger row carries a canonical phase "
            f"({', '.join(CANONICAL_PHASES)})."
        )
        return failures

    if status in ("review", "done") and "dev-story" not in phases:
        failures.append(f"C2: status is '{status}' but no dev-story ledger row exists.")
    if status == "done" and "code-review" not in phases:
        failures.append("C2: status is 'done' but no code-review ledger row exists.")

    for cells in rows:
        if len(cells) < 5:
            continue
        phase = normalize_cell(cells[1])
        if phase not in CANONICAL_PHASES:
            continue
        for label, index in (("Test count", 3), ("File List reconciliation", 4)):
            if normalize_cell(cells[index]) in PLACEHOLDER_CELLS:
                failures.append(
                    f"C2: {phase} row leaves '{label}' as a placeholder: "
                    f"'{cells[index].strip()}'."
                )

    newest = None
    for cells in rows:
        if len(cells) >= 5 and normalize_cell(cells[1]) in CANONICAL_PHASES:
            newest = cells
    if newest is not None:
        # Strip emphasis first: live cells write `Matched **27/27**`, and a
        # pattern anchored straight after "matched" breaks on the bold markers.
        reconciliation = re.sub(r"[`*_]", "", newest[4])
        if not MATCHED_PATTERN.search(reconciliation) and not any(
            marker in reconciliation.lower() for marker in BLOCKED_EVIDENCE_MARKERS
        ):
            failures.append(
                "C2: newest ledger row records neither 'matched N/N' nor a blocked-evidence "
                "record in its File List reconciliation cell."
            )
    return failures


def check_evidence_rows(text: str, status: str) -> tuple[list[str], int]:
    """C6 - no evidence/checkpoint row left pending once status is review or done."""
    failures: list[str] = []
    tables = find_evidence_tables(text)
    if not tables:
        return failures, 0

    if status not in C6_STATUSES:
        return failures, len(tables)

    for status_index, rows in tables:
        for cells in rows:
            if status_index >= len(cells):
                continue
            value = normalize_cell(cells[status_index])
            if value not in PENDING_CELLS:
                continue
            if any(marker in value for marker in BLOCKED_MARKERS):
                continue
            label = cells[0].strip() if cells else "?"
            failures.append(
                f"C6: evidence row '{label}' is '{cells[status_index].strip() or '(empty)'}' "
                f"while the story status is '{status}'."
            )
    return failures, len(tables)


def validate(args: argparse.Namespace) -> int:
    message = read_commit_message(args)
    trailer_keys, bypass = parse_trailers(message)

    source = resolve_story_key(args, trailer_keys)

    if bypass:
        print(f"Story review readiness bypassed by trailer: {bypass}")
        return 0

    if source is None:
        print("No story key resolved; story review-readiness check is a no-op.")
        return 0

    artifacts_root = Path(args.artifacts_root)
    story_path = artifacts_root / f"{source.key}.md"
    if not story_path.exists():
        raise ValidationError(f"Story artifact not found: {to_posix(story_path)}")

    text = read_text_lf(story_path)
    frontmatter = parse_frontmatter(text)

    print(f"Selected story key: {source.key}")
    print(f"Story source: {source.name}")
    print(f"Story artifact: {to_posix(story_path)}")

    ledger = find_ledger(text)
    file_list = parse_section_paths(text, "File List")
    evidence_tables = find_evidence_tables(text)

    if ledger is None and file_list is None and not evidence_tables:
        print("Artifact carries no ledger, File List, or evidence table; check is a no-op.")
        return 0

    failures: list[str] = []
    notes: list[str] = []

    # C3 - a recognised status.
    status = parse_status(text)
    if status is None:
        failures.append("C3: artifact declares no Status: line and no frontmatter status: key.")
    elif status not in VALID_STATUSES:
        failures.append(
            f"C3: status '{status}' is not one of {', '.join(VALID_STATUSES)}."
        )
    else:
        print(f"Declared status: {status}")

    # C4 - sprint-status agreement. spec-* artifacts are not expected to carry a row.
    if status in VALID_STATUSES:
        sprint_statuses = parse_sprint_status(Path(args.sprint_status_file))
        recorded = sprint_statuses.get(source.key)
        if recorded is None:
            if source.key.startswith("spec-"):
                notes.append("C4: spec artifact carries no development_status row, as expected.")
            else:
                failures.append(
                    f"C4: no development_status row for '{source.key}' in "
                    f"{to_posix(Path(args.sprint_status_file))}."
                )
        elif recorded != status:
            failures.append(
                f"C4: story declares status '{status}' but sprint-status records '{recorded}'."
            )

    # C2 - phase ledger.
    if ledger is None:
        notes.append("C2: artifact carries no phase ledger; ledger checks skipped.")
    elif status in VALID_STATUSES:
        failures.extend(check_ledger(ledger, status))

    # C1 - File List completeness against the story's diff.
    if file_list is None:
        notes.append("C1: artifact carries no File List; completeness check skipped.")
    elif status not in C1_STATUSES:
        notes.append(
            f"C1: status '{status}' is outside {{{', '.join(C1_STATUSES)}}}; "
            "cumulative-diff comparison skipped."
        )
    else:
        supplied = collect_supplied_changed(args)
        skip_reason: str | None = None
        if args.derive_cumulative:
            baseline = frontmatter.get("baseline_commit")
            if not baseline:
                failures.append(
                    "C1: --derive-cumulative requested but frontmatter carries no baseline_commit."
                )
                changed = supplied
            else:
                changed, skip_reason = derive_cumulative_changed(baseline, supplied)
        else:
            changed = supplied

        if skip_reason:
            notes.append(skip_reason)
        elif not changed:
            failures.append(
                "C1: the changed set is empty for a governed story. An empty set must fail "
                "closed rather than pass vacuously."
            )
        else:
            exclusion_paths = {exclusion.path for exclusion in parse_exclusions(text)}
            unlisted = sorted(
                path
                for path in set(changed)
                if path not in file_list
                and path not in exclusion_paths
                and not any(matches_glob(path, entry) for entry in file_list)
            )
            for path in unlisted:
                failures.append(f"C1: '{path}' changed but is not in the File List.")
            if not unlisted:
                print(f"C1: all {len(set(changed))} changed paths are declared.")

    # C6 - evidence-row status.
    evidence_failures, table_count = check_evidence_rows(text, status or "")
    if not table_count:
        notes.append("C6: artifact declares no evidence or checkpoint table; check skipped.")
    else:
        failures.extend(evidence_failures)
        if not evidence_failures:
            print(f"C6: {table_count} evidence table(s) carry no unresolved pending row.")

    for note in notes:
        print(note)

    if failures:
        print("")
        print("Story review-readiness violations:")
        for failure in failures:
            print(f"  - {failure}")
        print("")
        print(
            "A green gate covers File List completeness, ledger row presence, evidence-row "
            "status, status validity, and sprint-status agreement. It is not evidence of "
            "count arithmetic or executed tests."
        )
        print(
            "Bypass format: Story-Review-Readiness-Bypass: <non-empty reason>"
        )
        return 1

    print("Story review readiness validation passed.")
    return 0


def main(argv: list[str]) -> int:
    try:
        return validate(parse_args(argv))
    except ValidationError as error:
        print(error)
        return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
