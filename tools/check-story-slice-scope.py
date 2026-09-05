#!/usr/bin/env python3
"""Require a well-formed slice-scope record on stories that reuse prior work.

Enforces the mechanically checkable subset of `_bmad/custom/story-scope-guard.md`
and the shape rule in `_bmad-output/planning-artifacts/epics.md` ("Engineering/
Operational Readiness Track" preamble). When a changed story file cites a prior
story, it must carry a `Historical Context Classification` record in which every
row is classified, and every `anti-template` row states its permitted use. When a
story declares checkpoint identifiers, each one needs its own table row with an
owner, an evidence command or artifact, a review state, and a completion state --
because a shared row cannot record partial completion.

WHAT THIS GATE DOES NOT DO. It does not judge whether a classification label is
*correct*, whether a reuse is genuinely narrow, or whether two outcomes are
genuinely independently deployable. Those judgements stay with the create-story
and code-review layers defined in the policy. A green run is evidence the record
exists and is well formed, never evidence the record is right.

Background: DW 27.3-CR16 recorded that the 2026-07-26 correct-course split
produced two anti-template stories (see epics.md "Split note (2026-07-27)" for
Stories 30.1 and 31.1) because the splitting route did not load the policy and
the gate bound at `ready-for-dev` rather than at authoring. The policy now binds
at authoring and registration, at any status including `backlog`.

Escape hatch: a `Story-Slice-Scope: not-applicable - <reason>` commit trailer.
Fail-closed; exit 1 on violation, 0 on pass or on a genuine no-op.
"""

from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path


STORY_DIR = "_bmad-output/implementation-artifacts"
STORY_KEY_PATTERN = re.compile(r"^(\d+)-(\d+)-[a-z][a-z0-9-]*$")
STORY_FILE_PATTERN = re.compile(
    rf"^{re.escape(STORY_DIR)}/(?:spec-)?(\d+-\d+-[a-z][a-z0-9-]*)\.md$"
)
# Registration surfaces. A change to either binds the stories it registers, which
# is what closes the "compliant while backlog" loophole.
EPICS_PATH = "_bmad-output/planning-artifacts/epics.md"
SPRINT_STATUS_PATH = f"{STORY_DIR}/sprint-status.yaml"

# `Story 27.3` / `Story 27.3's` / `Story 27.3.` -- a prose citation of a prior
# story. The trailing lookahead must reject a deeper version number (27.3.1)
# without rejecting an ordinary sentence-ending period, which is why it is
# `(?!\d|\.\d)` and not `(?![\d.])`.
STORY_CITATION_PATTERN = re.compile(r"(?<![\w.])Story\s+(\d+)\.(\d+)(?!\d|\.\d)")
# A story key used inside another story's text, e.g. `31-1-openbao-...`.
FOREIGN_KEY_PATTERN = re.compile(r"(?<![\w-])(\d+-\d+-[a-z][a-z0-9-]*)(?![\w-])")
# Checkpoint identifiers: C1, C1.11, C12. Bounded so ordinary prose like "C#" or
# a hex blob does not match. As with the story-citation pattern, the trailing
# lookahead must not treat a sentence-ending period as part of the identifier.
CHECKPOINT_ID_PATTERN = re.compile(r"(?<![\w.])(C\d{1,2}(?:\.\d{1,2})?)(?!\w|\.\d)")

CODE_FENCE_PATTERN = re.compile(r"^\s*(`{3,}|~{3,})(.*)$")
HTML_COMMENT_PATTERN = re.compile(r"<!--.*?-->", re.DOTALL)
ATX_HEADING_PATTERN = re.compile(r"^(#{1,6})\s+(.*?)\s*#*\s*$")

CLASSIFICATION_HEADING = "historical context classification"
SLICE_PROOF_HEADING = "slice proof"
CHECKPOINT_HEADING_HINT = "checkpoint"

CLASSIFICATIONS = ("current-narrow-pattern", "historical-reference-only", "anti-template")
ANTI_TEMPLATE = "anti-template"

# epics.md: "it binds any story whose acceptance criteria enumerate more than
# five independently verifiable gates".
CHECKPOINT_TABLE_THRESHOLD = 5

BYPASS_TRAILER_KEY = "story-slice-scope"
BYPASS_DISPOSITION = "not-applicable"

POLICY_HELP = (
    "See _bmad/custom/story-scope-guard.md (Creation gate, Executable subset) and\n"
    "the Engineering/Operational Readiness Track preamble in\n"
    "_bmad-output/planning-artifacts/epics.md.\n"
    "If this change does not author or register story scope, add a\n"
    "'Story-Slice-Scope: not-applicable - <reason>' commit trailer."
)


class ValidationError(Exception):
    """Raised for an unusable invocation, not for a policy violation."""


@dataclass(frozen=True)
class Row:
    cells: list[str]
    line: int


@dataclass
class Table:
    header: list[str]
    rows: list[Row]
    line: int


@dataclass(frozen=True)
class Violation:
    path: str
    line: int
    message: str

    def render(self) -> str:
        return f"{self.path}:{self.line}: {self.message}"


def normalize_path(value: str) -> str:
    return value.strip().replace("\\", "/").lstrip("./")


def strip_invisible(text: str) -> str:
    """Remove HTML comments: invisible in rendered Markdown, so they must not
    satisfy a record requirement (that would fail open)."""
    return HTML_COMMENT_PATTERN.sub("", text)


def visible_lines(text: str) -> list[tuple[int, str]]:
    """Return (1-indexed line number, line) outside fenced code blocks.

    Fenced content is excluded so a policy example quoted inside a code block
    cannot satisfy -- or violate -- a requirement. Fence length is tracked so a
    longer fence may contain shorter fence markers.
    """
    out: list[tuple[int, str]] = []
    fence: str | None = None
    for number, raw in enumerate(strip_invisible(text).splitlines(), start=1):
        match = CODE_FENCE_PATTERN.match(raw)
        if match:
            marker = match.group(1)
            if fence is None:
                fence = marker
                continue
            if marker[0] == fence[0] and len(marker) >= len(fence):
                fence = None
                continue
        if fence is None:
            out.append((number, raw))
    return out


def section_lines(
    lines: list[tuple[int, str]], heading_text: str
) -> list[tuple[int, str]]:
    """Lines under the first heading whose text contains `heading_text`, up to the
    next heading at the same or shallower depth."""
    collected: list[tuple[int, str]] = []
    depth: int | None = None
    for number, line in lines:
        match = ATX_HEADING_PATTERN.match(line)
        if match:
            level = len(match.group(1))
            title = match.group(2).strip().lower()
            if depth is None:
                if heading_text in title:
                    depth = level
                continue
            if level <= depth:
                break
            collected.append((number, line))
            continue
        if depth is not None:
            collected.append((number, line))
    return collected


def has_section(lines: list[tuple[int, str]], heading_text: str) -> bool:
    for _, line in lines:
        match = ATX_HEADING_PATTERN.match(line)
        if match and heading_text in match.group(2).strip().lower():
            return True
    return False


def parse_tables(lines: list[tuple[int, str]]) -> list[Table]:
    """Markdown pipe tables as (header, body rows).

    Header state is tracked per table, so a document containing several tables
    does not mistake a later table's header for a body row.
    """
    tables: list[Table] = []
    current: Table | None = None
    for number, raw in lines:
        line = raw.strip()
        if not line.startswith("|"):
            current = None
            continue
        cells = [cell.strip() for cell in line.strip("|").split("|")]
        if all(re.fullmatch(r":?-{2,}:?", cell) for cell in cells if cell):
            continue
        if current is None:
            current = Table(header=cells, rows=[], line=number)
            tables.append(current)
            continue
        current.rows.append(Row(cells=cells, line=number))
    return tables


def table_rows(lines: list[tuple[int, str]]) -> list[Row]:
    return [row for table in parse_tables(lines) for row in table.rows]


def is_checkpoint_evidence_table(table: Table) -> bool:
    """A checkpoint evidence table is identified by its columns, not by its rows.

    Story 27.3 also carries `| Checkpoint | Exact Python discovery command |
    Required case inventory |` and Story 31.1 carries `| Decision | Resolution |`,
    both of which legitimately start rows with a gate identifier while carrying
    no completion state. Only a table that actually promises owner, review and
    completion columns is held to the one-row-per-gate contract.
    """
    header = " ".join(table.header).lower()
    return (
        len(table.header) >= 4
        and "owner" in header
        and "review" in header
        and ("completion" in header or "complete" in header)
    )


def cites_prior_story(text: str, own_key: str) -> bool:
    lines = visible_lines(text)
    body = "\n".join(line for _, line in lines)
    key_match = STORY_KEY_PATTERN.match(own_key)
    own_epic, own_number = (key_match.group(1), key_match.group(2)) if key_match else ("", "")

    for match in STORY_CITATION_PATTERN.finditer(body):
        if (match.group(1), match.group(2)) != (own_epic, own_number):
            return True
    for match in FOREIGN_KEY_PATTERN.finditer(body):
        if match.group(1) != own_key:
            return True
    return False


def check_classification(path: str, lines: list[tuple[int, str]]) -> list[Violation]:
    """R2 and R3: every row classified; every anti-template row states its use."""
    violations: list[Violation] = []
    section = section_lines(lines, CLASSIFICATION_HEADING)
    rows = table_rows(section)

    if not rows:
        # A prose-only classification record cannot be checked row-by-row.
        first = section[0][0] if section else 1
        violations.append(
            Violation(
                path,
                first,
                "Historical Context Classification has no table rows. Record one row "
                "per influencing artifact as | Source | Classification | Permitted use |.",
            )
        )
        return violations

    for row in rows:
        if len(row.cells) < 2:
            violations.append(
                Violation(
                    path,
                    row.line,
                    "Classification row needs at least a source and a classification cell.",
                )
            )
            continue
        classification = row.cells[1].lower()
        found = [name for name in CLASSIFICATIONS if name in classification]
        if not found:
            violations.append(
                Violation(
                    path,
                    row.line,
                    "Classification row carries none of `current-narrow-pattern`, "
                    "`historical-reference-only`, `anti-template`: "
                    f"{row.cells[1]!r}.",
                )
            )
            continue
        # A qualified classification such as Story 31.1's "current-narrow-pattern
        # (whole-story shape is `anti-template`)" is legitimate and must pass, so
        # this checks for presence, not exclusivity.
        if ANTI_TEMPLATE in classification:
            permitted_use = row.cells[2].strip() if len(row.cells) > 2 else ""
            if not permitted_use:
                violations.append(
                    Violation(
                        path,
                        row.line,
                        "`anti-template` row states no permitted use. A bare label is "
                        "a checkbox, not a classification.",
                    )
                )
    return violations


def declared_checkpoints(lines: list[tuple[int, str]]) -> set[str]:
    body = "\n".join(line for _, line in lines)
    return set(CHECKPOINT_ID_PATTERN.findall(body))


def check_checkpoints(path: str, lines: list[tuple[int, str]]) -> list[Violation]:
    """R4: a checkpoint-heavy story needs a table, and every row must be populated.

    DELIBERATELY NARROW. An earlier draft counted every checkpoint identifier
    appearing anywhere in the story and demanded a row for each. That produced
    false positives on real compliant stories: Story 27.3 names C1.1-C1.12 and
    C1.14 many times *because those gates were transferred to Story 27.5*, and
    Story 27.2 refers to `C1`'s ratified mapping in task prose. Nothing
    mechanical distinguishes "this story owns this gate" from "this story
    mentions this gate", so the gate does not try.

    What it checks instead:
      * a story mentioning more than five distinct gate identifiers must have a
        checkpoint table at all -- the pre-split Story 30.1 and 31.1 shape was
        "eight checkpoints with no owner, evidence command, review state or
        completion state" and "no checkpoint table at all"; and
      * every checkpoint row that does exist must be fully populated, because a
        shared or half-empty row cannot record partial completion.
    """
    evidence_tables = [
        table for table in parse_tables(lines) if is_checkpoint_evidence_table(table)
    ]
    rows = [
        row
        for table in evidence_tables
        for row in table.rows
        if row.cells and CHECKPOINT_ID_PATTERN.match(row.cells[0].strip().lstrip("*` "))
    ]

    violations: list[Violation] = []
    mentioned = declared_checkpoints(lines)
    if len(mentioned) > CHECKPOINT_TABLE_THRESHOLD and not evidence_tables:
        violations.append(
            Violation(
                path,
                1,
                f"Story names {len(mentioned)} checkpoint gates "
                f"({', '.join(sorted(mentioned)[:8])}...) but carries no checkpoint "
                "table. Split the checkpoints into separate stories, or give each "
                "gate its own row with an owner, an evidence command or artifact, a "
                "review state, and a completion state.",
            )
        )

    for row in rows:
        identifier = CHECKPOINT_ID_PATTERN.match(
            row.cells[0].strip().lstrip("*` ")
        ).group(1)
        populated = [cell for cell in row.cells[1:] if cell and cell not in {"-", "--"}]
        if len(populated) < 4:
            violations.append(
                Violation(
                    path,
                    row.line,
                    f"Checkpoint {identifier} row has {len(populated)} populated "
                    "cells; it needs an owner, an evidence command or artifact, a "
                    "review state, and a completion state.",
                )
            )
    return violations


def check_story(
    path: str, text: str, key: str, *, registered: bool
) -> list[Violation]:
    """Validate one story file.

    `registered` is true when the same change also touches `epics.md` or
    `sprint-status.yaml` -- i.e. the story is being authored or registered, which
    is the moment the policy binds (see the Creation gate in
    `_bmad/custom/story-scope-guard.md`).

    A story that predates the policy and is merely being edited is not asked to
    produce a record retroactively; whatever record it does carry is still
    validated. Demanding the full record on every historical edit would be scope
    the approved correction did not authorize, and would train contributors to
    reach for the bypass trailer.
    """
    lines = visible_lines(text)
    violations: list[Violation] = []

    partial_record = has_section(lines, CLASSIFICATION_HEADING) or has_section(
        lines, SLICE_PROOF_HEADING
    )
    if (registered or partial_record) and cites_prior_story(text, key):
        has_classification = has_section(lines, CLASSIFICATION_HEADING)
        has_slice_proof = has_section(lines, SLICE_PROOF_HEADING)
        if not has_classification:
            violations.append(
                Violation(
                    path,
                    1,
                    "Story cites a prior story but has no `Historical Context "
                    "Classification` section.",
                )
            )
        if not has_slice_proof:
            violations.append(
                Violation(
                    path,
                    1,
                    "Story cites a prior story but has no `Slice Proof` section.",
                )
            )
        if has_classification:
            violations.extend(check_classification(path, lines))

    violations.extend(check_checkpoints(path, lines))
    return violations


def parse_trailers(message: str) -> dict[str, str]:
    trailers: dict[str, str] = {}
    for raw in message.splitlines():
        line = raw.strip()
        if not line or ":" not in line:
            continue
        key, _, value = line.partition(":")
        key = key.strip().lower()
        if re.fullmatch(r"[a-z][a-z0-9-]*", key):
            trailers[key] = value.strip()
    return trailers


def read_lines_file(path: str | None) -> list[str] | None:
    if not path:
        return None
    candidate = Path(path)
    if not candidate.is_file():
        raise ValidationError(f"file not found: {path}")
    return [
        normalize_path(line)
        for line in candidate.read_text(encoding="utf-8").splitlines()
        if line.strip()
    ]


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--changed-files-file")
    parser.add_argument("--commit-message-file")
    parser.add_argument("--branch-name")
    parser.add_argument("--story-key")
    parser.add_argument("--repo-root", default=".")
    parser.add_argument(
        "--require-record",
        action="store_true",
        help=(
            "Treat the story as newly authored or registered even when the change "
            "does not touch epics.md or sprint-status.yaml."
        ),
    )
    args = parser.parse_args(argv)

    try:
        changed = read_lines_file(args.changed_files_file)
        message = ""
        if args.commit_message_file:
            message_path = Path(args.commit_message_file)
            if not message_path.is_file():
                raise ValidationError(f"file not found: {args.commit_message_file}")
            message = message_path.read_text(encoding="utf-8")
    except ValidationError as error:
        print(f"story-slice-scope: {error}")
        return 1

    trailers = parse_trailers(message)
    bypass = trailers.get(BYPASS_TRAILER_KEY, "")
    if bypass.lower().startswith(BYPASS_DISPOSITION):
        reason = bypass[len(BYPASS_DISPOSITION) :].lstrip(" -").strip()
        if not reason:
            print(
                "story-slice-scope: 'Story-Slice-Scope: not-applicable' needs a "
                "reason, e.g. 'not-applicable - no story scope authored'."
            )
            return 1
        print(f"story-slice-scope: bypassed by trailer - {reason}")
        return 0

    root = Path(args.repo_root)

    if changed is None:
        if args.story_key:
            spec_path = root / STORY_DIR / f"spec-{args.story_key}.md"
            plain_path = root / STORY_DIR / f"{args.story_key}.md"
            if spec_path.is_file() and not plain_path.is_file():
                changed = [f"{STORY_DIR}/spec-{args.story_key}.md"]
            else:
                changed = [f"{STORY_DIR}/{args.story_key}.md"]
        else:
            # Be explicit rather than exiting 0 silently: a bare invocation that
            # passes is not evidence the gate ran against anything.
            print(
                "story-slice-scope: no --changed-files-file and no --story-key; "
                "nothing to check. This is a no-op, not a pass."
            )
            return 0

    story_paths: list[tuple[str, str]] = []
    registration_touched = False
    for path in changed:
        if path in {EPICS_PATH, SPRINT_STATUS_PATH}:
            registration_touched = True
            continue
        match = STORY_FILE_PATTERN.match(path)
        if match:
            story_paths.append((path, match.group(1)))

    if not story_paths:
        if registration_touched:
            print(
                "story-slice-scope: registration surface changed with no story file "
                "in the same change; nothing to check here."
            )
        else:
            print("story-slice-scope: no governed story file changed.")
        return 0

    violations: list[Violation] = []
    checked: list[str] = []
    for path, key in story_paths:
        candidate = root / path
        if not candidate.is_file():
            # Deleted or renamed in this change; nothing to validate.
            continue
        checked.append(path)
        violations.extend(
            check_story(
                path,
                candidate.read_text(encoding="utf-8"),
                key,
                registered=registration_touched or args.require_record,
            )
        )

    if not checked:
        print("story-slice-scope: no governed story file present on disk.")
        return 0

    if violations:
        print("story-slice-scope: FAILED")
        for violation in violations:
            print(f"  {violation.render()}")
        print()
        print(POLICY_HELP)
        return 1

    print(
        "story-slice-scope: OK - "
        f"{len(checked)} story file(s) checked: {', '.join(checked)}"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
