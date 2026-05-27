#!/usr/bin/env python3
"""Validate changed files against a BMad story File Scope section."""

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
# Track open-fence length so fences > 3 backticks/tildes can contain shorter
# nested fence lines without prematurely closing (12.4-RV12).
CODE_FENCE_PATTERN = re.compile(r"^\s*(`{3,}|~{3,})(.*)$")

ALLOWED_LABELS = {
    "allowed files for this story:",
    "expected files to add or edit:",
    "allowed to modify:",
}

# Recognized non-allowed-list labels that terminate the bullet collection.
# Other trailing-colon prose under `## File Scope` must NOT terminate the
# allow-list because rationales legitimately end with `:` (12.4-RV13).
TERMINATING_LABELS = {
    "read/verify only:",
    "forbidden by default:",
}

FORBIDDEN_DEFAULT_GLOBS = (
    "src/**/*.cs",
    "tests/**/*.cs",
    "tools/publish-nuget.ps1",
    "tools/pack-release.ps1",
    "tools/test-release.ps1",
    "package-lock.json",
    "Hexalith.AI.Tools/**",
    "Hexalith.Commons/**",
    "Hexalith.EventStore/**",
)


@dataclass(frozen=True)
class StorySource:
    name: str
    key: str


@dataclass(frozen=True)
class Override:
    pattern: str
    rationale: str
    raw: str


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


def normalize_label(value: str) -> str:
    return value.strip().strip("*").strip().lower()


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


def extract_story_key(value: str) -> str | None:
    keys = extract_story_keys(value)
    return keys[0] if keys else None


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate changed paths against the selected story's File Scope allow-list.",
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


def read_commit_message(args: argparse.Namespace) -> str:
    if args.commit_message_file:
        return Path(args.commit_message_file).read_text(encoding="utf-8")
    if args.commit_sha:
        return run_git(["log", "-1", "--format=%B", args.commit_sha])
    return ""


def parse_trailers(message: str) -> tuple[list[str], list[str]]:
    if not message.strip():
        return [], []

    try:
        completed = subprocess.run(
            ["git", "interpret-trailers", "--parse"],
            input=message,
            text=True,
            capture_output=True,
            check=False,
        )
    except FileNotFoundError as exc:
        # Convert raw FileNotFoundError to a clean ValidationError so contributors
        # see an actionable message instead of a Python stack trace (12.4-RV14).
        raise ValidationError(
            "Required tool not found: 'git interpret-trailers' is unavailable. "
            "Install Git (with the interpret-trailers helper) or ensure 'git' is on PATH."
        ) from exc

    if completed.returncode != 0:
        # Preserve git stderr context, but surface as a ValidationError so the
        # caller does not see a Python traceback for an expected failure mode.
        detail = completed.stderr.strip() or "git interpret-trailers --parse failed"
        raise ValidationError(
            "Git with 'interpret-trailers' is required. "
            "Install Git (with the interpret-trailers helper) or ensure 'git' is on PATH. "
            f"Command output: {detail}"
        )

    story_keys: list[str] = []
    overrides: list[str] = []
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
        elif normalized_key == "scope-override":
            overrides.append(normalized_value)

    if len(set(story_keys)) > 1:
        raise ValidationError(
            "Conflicting Story/Story-Key trailers: " + ", ".join(sorted(set(story_keys))),
        )
    return story_keys, overrides


def resolve_story_key(args: argparse.Namespace, trailer_keys: list[str]) -> StorySource:
    sources: list[StorySource] = []

    explicit_raw = (args.story_key or "").strip()
    if explicit_raw:
        explicit_keys = extract_story_keys(explicit_raw)
        if not explicit_keys:
            raise ValidationError(
                f"--story-key value is not a valid story key: {explicit_raw}",
            )
        unique_explicit = sorted(set(explicit_keys))
        if len(unique_explicit) > 1:
            # Mirror trailer multi-key rejection so CLI input cannot bypass the
            # same guard (12.4-RV7). Report every detected key.
            raise ValidationError(
                "--story-key value contains multiple story keys: "
                + ", ".join(unique_explicit),
            )
        sources.append(StorySource("cli", unique_explicit[0]))

    if trailer_keys:
        sources.append(StorySource("trailer", trailer_keys[0]))

    branch_keys = extract_story_keys(args.branch_name or "")
    if branch_keys:
        unique_branch = sorted(set(branch_keys))
        if len(unique_branch) > 1:
            # Mirror trailer/CLI multi-key rejection (12.4-RV8). Report every
            # detected key so contributors can fix the branch name without
            # guessing which key the validator picked.
            raise ValidationError(
                "Branch name contains multiple story keys: "
                + ", ".join(unique_branch),
            )
        sources.append(StorySource("branch", unique_branch[0]))

    if not sources:
        raise ValidationError(
            "No story key resolved. Pass --story-key, add a Story:/Story-Key: trailer, or use a branch name containing a full story key.",
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


def extract_backtick_path(line: str) -> str | None:
    # The first backticked token in a bullet is the path. Additional backticks
    # in the same bullet are rationale and are intentionally ignored — splitting
    # one bullet into multiple paths would silently widen scope.
    match = BACKTICK_PATTERN.search(line)
    return match.group(1) if match else None


def parse_allowed_scope(story_path: Path) -> list[str]:
    text = story_path.read_text(encoding="utf-8")
    if text.startswith("﻿"):
        text = text[1:]
    lines = text.splitlines()

    in_file_scope = False
    in_allowed = False
    fence_marker: str | None = None  # full opener, e.g. "```" or "````"
    allowed: list[str] = []

    for raw_line in lines:
        fence_match = CODE_FENCE_PATTERN.match(raw_line)
        if fence_match:
            marker = fence_match.group(1)
            trailing = fence_match.group(2)
            if fence_marker is None:
                fence_marker = marker
                continue
            # CommonMark: a closing fence must be the same marker character
            # (backtick vs tilde) and at least as long as the opener. A shorter
            # 3-backtick line nested inside a 4-backtick fence must NOT close
            # the outer fence (12.4-RV12).
            if marker[0] == fence_marker[0] and len(marker) >= len(fence_marker) and not trailing.strip():
                fence_marker = None
                continue
            # Otherwise treat as ordinary content inside the open fence.
        if fence_marker is not None:
            continue

        stripped = raw_line.strip()
        if stripped.startswith("## "):
            if stripped.lower() == "## file scope":
                in_file_scope = True
                in_allowed = False
                continue
            if in_file_scope:
                break

        if not in_file_scope:
            continue

        label = normalize_label(stripped)
        # Bold-wrapped section labels like `**Forbidden by default:**` should
        # behave like the plain form for both opening and terminating checks.
        bold_label: str | None = None
        if stripped.startswith("**") and stripped.endswith(":**"):
            bold_label = stripped[2:-3].strip().lower() + ":"

        if label in ALLOWED_LABELS or (bold_label is not None and bold_label in ALLOWED_LABELS):
            in_allowed = True
            continue

        if in_allowed and (
            label in TERMINATING_LABELS
            or (bold_label is not None and bold_label in TERMINATING_LABELS)
        ):
            in_allowed = False
            continue

        if not in_allowed:
            continue

        # Only accept top-level bullets (no leading indentation). Sub-bullets
        # under an allowed entry are explanatory, not authoritative.
        if not raw_line.startswith("- "):
            continue

        path = extract_backtick_path(stripped)
        if path:
            allowed.append(normalize_path(path))

    allowed = [entry for entry in allowed if entry]
    if not allowed:
        raise ValidationError(f"Story file has no parseable File Scope section or has an empty allowed scope: {story_path}")
    return allowed


def collect_changed_files(args: argparse.Namespace) -> list[str]:
    values: list[str] = []
    values.extend(args.changed_file)
    if args.changed_files_file:
        changed_file = Path(args.changed_files_file)
        if not changed_file.exists():
            raise ValidationError(f"Changed-files input does not exist: {changed_file}")
        values.extend(changed_file.read_text(encoding="utf-8").splitlines())
    if args.staged:
        values.extend(run_git(["diff", "--cached", "--name-only", "--"]).splitlines())

    return [normalized for value in values if (normalized := normalize_path(value))]


def matches_glob(path: str, pattern: str) -> bool:
    return _glob_match(path.split("/"), normalize_path(pattern).split("/"))


def _glob_match(parts: list[str], pat: list[str]) -> bool:
    if not pat:
        return not parts
    head, *rest = pat
    if head == "**":
        # `**` matches zero or more path segments.
        for i in range(len(parts) + 1):
            if _glob_match(parts[i:], rest):
                return True
        return False
    if not parts:
        return False
    if fnmatch.fnmatchcase(parts[0], head):
        return _glob_match(parts[1:], rest)
    return False


def is_forbidden_default(path: str) -> bool:
    return any(matches_glob(path, pattern) for pattern in FORBIDDEN_DEFAULT_GLOBS)


def split_override(value: str) -> tuple[str, str]:
    for separator in (" - ", " -- ", ": "):
        if separator in value:
            pattern, rationale = value.split(separator, 1)
            return pattern.strip(), rationale.strip()
    return value.strip(), ""


def parse_overrides(values: list[str]) -> list[Override]:
    overrides: list[Override] = []
    invalid: list[str] = []
    for value in values:
        pattern, rationale = split_override(value)
        normalized = normalize_path(pattern)
        is_vague = (
            not normalized
            or normalized in {"*", ".", "/"}
            or normalized.endswith("/**")
            or "/" not in normalized
            or not rationale
            or not any(ch in pattern for ch in ("/", "\\", "*", "?"))
        )
        if is_vague:
            invalid.append(value)
            continue
        overrides.append(Override(normalized, rationale, value))

    if invalid:
        raise ValidationError(
            "Invalid Scope-Override value(s): "
            + "; ".join(invalid)
            + "\nAccepted format: Scope-Override: path/or/narrow-glob - short rationale",
        )
    return overrides


def override_matches(path: str, override: Override) -> bool:
    if any(ch in override.pattern for ch in "*?[]"):
        return matches_glob(path, override.pattern)
    return path == override.pattern


def to_posix(path: Path) -> str:
    return str(path).replace("\\", "/")


def validate(args: argparse.Namespace) -> int:
    changed = collect_changed_files(args)
    if not changed:
        print("No changed files; story-scope check is a no-op.")
        return 0

    message = read_commit_message(args)
    trailer_keys, trailer_overrides = parse_trailers(message)
    source = resolve_story_key(args, trailer_keys)

    artifacts_root = Path(args.artifacts_root)
    story_path = artifacts_root / f"{source.key}.md"
    if not story_path.exists():
        raise ValidationError(f"Story artifact not found: {to_posix(story_path)}")

    allowed = parse_allowed_scope(story_path)
    overrides = parse_overrides(trailer_overrides)

    print(f"Selected story key: {source.key}")
    print(f"Story source: {source.name}")
    print(f"Story artifact: {to_posix(story_path)}")
    print("Allowed scope entries:")
    for entry in allowed:
        print(f"  - {entry}")

    allowed_files: list[str] = []
    out_of_scope: list[str] = []
    forbidden_no_override: list[str] = []
    overridden: list[str] = []
    forbidden_overridden: list[str] = []

    for path in changed:
        if any(matches_glob(path, pattern) for pattern in allowed):
            allowed_files.append(path)
            continue

        matching_override = next((override for override in overrides if override_matches(path, override)), None)
        if matching_override:
            if is_forbidden_default(path):
                forbidden_overridden.append(path)
            else:
                overridden.append(path)
            continue

        if is_forbidden_default(path):
            forbidden_no_override.append(path)
        else:
            out_of_scope.append(path)

    if allowed_files:
        print("In-scope changed files:")
        for path in allowed_files:
            print(f"  - {path}")

    if overridden:
        print("Audited Scope-Override entries:")
        for path in overridden:
            print(f"  - {path}")

    if forbidden_overridden:
        print("Forbidden-default files cannot be authorized by Scope-Override:")
        for path in forbidden_overridden:
            print(f"  - {path}")

    if forbidden_no_override:
        print("Forbidden-default files (no override; D5-class):")
        for path in forbidden_no_override:
            print(f"  - {path}")

    if out_of_scope:
        print("Out-of-scope files:")
        for path in out_of_scope:
            print(f"  - {path}")

    if out_of_scope or forbidden_overridden or forbidden_no_override:
        print("Accepted override format: Scope-Override: path/or/narrow-glob - short rationale")
        return 1

    print("Story file scope validation passed.")
    return 0


def main(argv: list[str]) -> int:
    try:
        return validate(parse_args(argv))
    except ValidationError as error:
        print(error)
        return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
