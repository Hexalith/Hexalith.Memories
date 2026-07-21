#!/usr/bin/env python3
"""Require attached cross-tenant negative evidence on scope-sensitive changes.

Enforces the repository rule "Tenant isolation requires attached negative
evidence" (_bmad-output/project-context.md, ### Testing Rules). When a changed
file matches a tenant-isolation surface glob (tools/tenant-isolation-surfaces.txt)
the resolving story/spec must carry a `## Cross-Tenant Negative Evidence`
section documenting the denial/fail-closed proof, an accepted blocker, or a
reviewed not-triggered disposition. A `Tenant-Isolation-Evidence: not-applicable
- <reason>` commit trailer is the escape hatch. Fail-closed; exit 1 on violation.
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
AND_JOINED_STORY_KEYS_PATTERN = re.compile(
    r"(?<![\w-])(\d+-\d+-[a-z](?:[a-z0-9-]*?[a-z0-9])?)-and-(\d+-\d+-[a-z](?:[a-z0-9-]*[a-z0-9])?)(?![\w-])",
    re.IGNORECASE,
)
BACKTICK_PATTERN = re.compile(r"`([^`]+)`")
# Length-aware fence tracking so a fence > 3 markers can contain shorter nested
# fence lines without prematurely closing (mirrors check-story-file-scope.py).
CODE_FENCE_PATTERN = re.compile(r"^\s*(`{3,}|~{3,})(.*)$")
# `**Label:**` at the start of a (possibly bulleted) line inside the evidence
# section. The label must not contain `*` or `:` so rationale colons are safe.
FIELD_LABEL_PATTERN = re.compile(r"^\*\*([^*:]+):\*\*\s*(.*)$")
# HTML comments are invisible in rendered Markdown, so evidence labels hidden in
# one must NOT count (fail-open otherwise). Matched across lines and stripped.
HTML_COMMENT_PATTERN = re.compile(r"<!--.*?-->", re.DOTALL)

EVIDENCE_HEADING = "cross-tenant negative evidence"
BYPASS_TRAILER_KEY = "tenant-isolation-evidence"
BYPASS_DISPOSITION = "not-applicable"

BYPASS_HINT = (
    "If this change does not touch a live tenant-isolation surface, add a "
    "'Tenant-Isolation-Evidence: not-applicable - <reason>' commit trailer, or a "
    "'**Not triggered:** <reason>' line under a '## Cross-Tenant Negative Evidence' "
    "section in the story/spec."
)
EVIDENCE_HELP = (
    "Add a '## Cross-Tenant Negative Evidence' section to the story/spec with one of:\n"
    "  proof   -> **Surfaces:** <named surfaces> / **Tests:** `Name` / **Command:** `cmd` / **Result:** <outcome>\n"
    "  blocker -> **Accepted blocker:** <what> / **Owner:** <who> / **Consequence:** <risk> / **Reopen trigger:** <when>\n"
    "  waive   -> **Not triggered:** <why the matched files change no live tenant-isolation surface>\n"
    "See CONTRIBUTING.md and _bmad-output/project-context.md (### Testing Rules)."
)


@dataclass(frozen=True)
class StorySource:
    name: str
    key: str


@dataclass(frozen=True)
class Bypass:
    present: bool
    reason: str


class ValidationError(Exception):
    """Raised for expected validation failures."""


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

    return keys


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Require attached cross-tenant negative evidence for scope-sensitive changes.",
    )
    parser.add_argument("--story-key", help="Explicit story key. Highest precedence.")
    parser.add_argument("--branch-name", help="Branch name to scan for a story key.")
    parser.add_argument("--commit-message-file", help="Commit message file to parse trailers from.")
    parser.add_argument("--commit-sha", help="Commit SHA whose message should be parsed.")
    parser.add_argument("--changed-file", action="append", default=[], help="Changed file path. May be repeated.")
    parser.add_argument("--changed-files-file", help="File containing changed paths, one per line.")
    parser.add_argument("--staged", action="store_true", help="Use git diff --cached --name-only -- as changed files.")
    parser.add_argument(
        "--artifacts-root",
        default="_bmad-output/implementation-artifacts",
        help="Directory containing story artifacts.",
    )
    parser.add_argument(
        "--surfaces-file",
        default="tools/tenant-isolation-surfaces.txt",
        help="File of tenant-isolation surface globs, one per line.",
    )
    return parser.parse_args(argv)


def run_git(args: list[str]) -> str:
    try:
        completed = subprocess.run(
            ["git", *args],
            text=True,
            capture_output=True,
            check=False,
        )
    except FileNotFoundError as exc:
        raise ValidationError(
            "Required tool not found: 'git' is unavailable. "
            "Install Git or ensure 'git' is on PATH."
        ) from exc
    if completed.returncode != 0:
        raise ValidationError(completed.stderr.strip() or f"git {' '.join(args)} failed")
    return completed.stdout


def read_text(path: Path, description: str) -> str:
    # Convert unreadable / non-UTF-8 inputs into clean ValidationErrors so the
    # gate honors its "no raw traceback" contract instead of crashing.
    try:
        return path.read_text(encoding="utf-8")
    except OSError as exc:
        raise ValidationError(f"Cannot read {description}: {to_posix(path)} ({exc})") from exc
    except UnicodeDecodeError as exc:
        raise ValidationError(f"{description} is not valid UTF-8: {to_posix(path)} ({exc})") from exc


def read_commit_message(args: argparse.Namespace) -> str:
    if args.commit_message_file:
        return read_text(Path(args.commit_message_file), "commit message file")
    if args.commit_sha:
        return run_git(["log", "-1", "--format=%B", args.commit_sha])
    return ""


def parse_bypass(value: str) -> str:
    stripped = value.strip()
    remainder = stripped[len(BYPASS_DISPOSITION):]
    # The disposition token must be exactly "not-applicable" followed by a
    # separator, whitespace, or end — never "not-applicableXYZ" (fail-open).
    if not stripped.lower().startswith(BYPASS_DISPOSITION) or (remainder and remainder[0] not in " \t—–:-"):
        raise ValidationError(
            "Invalid Tenant-Isolation-Evidence trailer. Expected "
            "'Tenant-Isolation-Evidence: not-applicable - <reason>'. Got: " + value
        )
    # Strip a single leading separator (em dash, en dash, hyphen, or colon).
    remainder = re.sub(r"^\s*[—–:\-]+\s*", "", remainder)
    return remainder.strip()


def parse_trailers(message: str) -> tuple[list[str], Bypass | None]:
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
    bypass: Bypass | None = None
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
            if not keys_in_value:
                raise ValidationError(f"Malformed Story trailer: {line}")
            story_keys.append(keys_in_value[0])
        elif normalized_key == BYPASS_TRAILER_KEY:
            bypass = Bypass(present=True, reason=parse_bypass(normalized_value))

    if len(set(story_keys)) > 1:
        raise ValidationError(
            "Conflicting Story/Story-Key trailers: " + ", ".join(sorted(set(story_keys))),
        )
    return story_keys, bypass


def resolve_story_source(args: argparse.Namespace, trailer_keys: list[str]) -> StorySource:
    sources: list[StorySource] = []

    explicit_raw = (args.story_key or "").strip()
    if explicit_raw:
        explicit_keys = extract_story_keys(explicit_raw)
        if not explicit_keys:
            raise ValidationError(f"--story-key value is not a valid story key: {explicit_raw}")
        unique_explicit = sorted(set(explicit_keys))
        if len(unique_explicit) > 1:
            raise ValidationError(
                "--story-key value contains multiple story keys: " + ", ".join(unique_explicit),
            )
        sources.append(StorySource("cli", unique_explicit[0]))

    if trailer_keys:
        sources.append(StorySource("trailer", trailer_keys[0]))

    branch_keys = extract_story_keys(args.branch_name or "")
    if branch_keys:
        unique_branch = sorted(set(branch_keys))
        if len(unique_branch) > 1:
            raise ValidationError(
                "Branch name contains multiple story keys: " + ", ".join(unique_branch),
            )
        sources.append(StorySource("branch", unique_branch[0]))

    if not sources:
        raise ValidationError(
            "Tenant-isolation surface changed but no story key resolved. Pass --story-key, "
            "add a Story:/Story-Key: trailer, or use a branch name containing a full story "
            "key so the attached evidence can be located.\n" + BYPASS_HINT
        )

    unique = {source.key for source in sources}
    if len(unique) > 1:
        details = ", ".join(f"{source.name}={source.key}" for source in sources)
        raise ValidationError(f"Conflicting story keys: {details}")

    for name in ("cli", "trailer", "branch"):
        for source in sources:
            if source.name == name:
                return source
    return sources[0]


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


def load_surface_globs(surfaces_file: Path) -> list[str]:
    if not surfaces_file.exists():
        raise ValidationError(f"Surfaces file does not exist: {to_posix(surfaces_file)}")
    globs: list[str] = []
    for raw in read_text(surfaces_file, "surfaces file").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        globs.append(line)
    if not globs:
        raise ValidationError(f"Surfaces file has no glob entries: {to_posix(surfaces_file)}")
    return globs


def collect_changed_files(args: argparse.Namespace) -> list[str]:
    values: list[str] = []
    values.extend(args.changed_file)
    if args.changed_files_file:
        changed_file = Path(args.changed_files_file)
        if not changed_file.exists():
            raise ValidationError(f"Changed-files input does not exist: {changed_file}")
        values.extend(read_text(changed_file, "changed-files input").splitlines())
    if args.staged:
        values.extend(run_git(["diff", "--cached", "--name-only", "--"]).splitlines())

    return [normalized for value in values if (normalized := normalize_path(value))]


def extract_section(story_path: Path, heading: str) -> list[str] | None:
    text = read_text(story_path, "story/spec")
    if text.startswith("﻿"):
        text = text[1:]

    in_section = False
    fence_marker: str | None = None
    body: list[str] = []

    for raw_line in text.splitlines():
        fence_match = CODE_FENCE_PATTERN.match(raw_line)
        if fence_match:
            marker = fence_match.group(1)
            trailing = fence_match.group(2)
            if fence_marker is None:
                fence_marker = marker
                if in_section:
                    body.append(raw_line)
                continue
            if marker[0] == fence_marker[0] and len(marker) >= len(fence_marker) and not trailing.strip():
                fence_marker = None
                if in_section:
                    body.append(raw_line)
                continue

        if fence_marker is not None:
            if in_section:
                body.append(raw_line)
            continue

        stripped = raw_line.strip()
        if stripped.startswith("## "):
            if stripped[3:].strip().lower() == heading:
                in_section = True
                continue
            if in_section:
                break
            continue

        if in_section:
            body.append(raw_line)

    return body if in_section else None


def parse_evidence_fields(body: list[str]) -> dict[str, str]:
    # Evidence labels only count when they are actually visible in the rendered
    # spec: not inside an HTML comment and not inside a fenced code block. Both
    # would otherwise let placeholder/example text satisfy the gate (fail-open).
    text = HTML_COMMENT_PATTERN.sub("", "\n".join(body))
    if "<!--" in text:  # an unclosed comment hides everything after it
        text = text[: text.index("<!--")]

    fields: dict[str, str] = {}
    current: str | None = None
    fence_marker: str | None = None
    for raw_line in text.split("\n"):
        fence_match = CODE_FENCE_PATTERN.match(raw_line)
        if fence_match:
            marker = fence_match.group(1)
            trailing = fence_match.group(2)
            if fence_marker is None:
                fence_marker = marker
            elif marker[0] == fence_marker[0] and len(marker) >= len(fence_marker) and not trailing.strip():
                fence_marker = None
            current = None
            continue
        if fence_marker is not None:
            continue

        stripped = raw_line.strip()
        candidate = stripped[2:].strip() if stripped.startswith("- ") else stripped
        label_match = FIELD_LABEL_PATTERN.match(candidate)
        if label_match:
            current = label_match.group(1).strip().lower()
            fields[current] = label_match.group(2).strip()
            continue
        if not stripped:
            current = None
            continue
        if current is not None:
            fields[current] = (fields[current] + " " + stripped).strip()
    return fields


def validate_evidence(story_path: Path) -> list[str]:
    body = extract_section(story_path, EVIDENCE_HEADING)
    if body is None:
        return [f"missing required '## Cross-Tenant Negative Evidence' section in {to_posix(story_path)}"]

    fields = parse_evidence_fields(body)

    if "not triggered" in fields:
        if fields["not triggered"]:
            return []
        return ["'**Not triggered:**' requires a non-empty reason"]

    if "accepted blocker" in fields:
        problems: list[str] = []
        for label, display in (
            ("accepted blocker", "Accepted blocker"),
            ("owner", "Owner"),
            ("consequence", "Consequence"),
            ("reopen trigger", "Reopen trigger"),
        ):
            if not fields.get(label, "").strip():
                problems.append(f"'**{display}:**' is empty or missing")
        return problems

    problems = []
    if not fields.get("surfaces", "").strip():
        problems.append("'**Surfaces:**' is empty or missing (name the affected tenant-isolation surfaces)")
    if not BACKTICK_PATTERN.search(fields.get("tests", "")):
        problems.append("'**Tests:**' must name at least one backticked test")
    if not BACKTICK_PATTERN.search(fields.get("command", "")):
        problems.append("'**Command:**' must include a backticked command")
    if not fields.get("result", "").strip():
        problems.append("'**Result:**' is empty or missing")
    return problems


def to_posix(path: Path) -> str:
    return str(path).replace("\\", "/")


def validate(args: argparse.Namespace) -> int:
    changed = collect_changed_files(args)
    if not changed:
        print("No changed files; tenant-isolation evidence check is a no-op.")
        return 0

    surfaces = load_surface_globs(Path(args.surfaces_file))
    triggered = [path for path in changed if any(matches_glob(path, glob) for glob in surfaces)]
    if not triggered:
        print("No tenant-isolation surface changed; evidence check is a no-op.")
        return 0

    print("Tenant-isolation surface(s) changed:")
    for path in triggered:
        print(f"  - {path}")

    message = read_commit_message(args)
    trailer_keys, bypass = parse_trailers(message)

    if bypass is not None:
        if not bypass.reason:
            raise ValidationError(
                "Tenant-Isolation-Evidence bypass trailer requires a non-empty reason. "
                "Use 'Tenant-Isolation-Evidence: not-applicable - <reason>'."
            )
        print(f"Bypass accepted (Tenant-Isolation-Evidence: not-applicable): {bypass.reason}")
        return 0

    source = resolve_story_source(args, trailer_keys)
    story_path = Path(args.artifacts_root) / f"{source.key}.md"
    if not story_path.exists():
        raise ValidationError(
            f"Tenant-isolation surface changed but story/spec artifact not found: {to_posix(story_path)}\n"
            + BYPASS_HINT
        )

    print(f"Selected story/spec: {to_posix(story_path)} (source: {source.name})")

    problems = validate_evidence(story_path)
    if problems:
        print("Cross-tenant negative evidence is missing or incomplete:")
        for problem in problems:
            print(f"  - {problem}")
        print(EVIDENCE_HELP)
        return 1

    print("Cross-tenant negative evidence validation passed.")
    return 0


def main(argv: list[str]) -> int:
    try:
        return validate(parse_args(argv))
    except ValidationError as error:
        print(error)
        return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
