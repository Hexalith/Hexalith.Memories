#!/usr/bin/env python3
"""Validate changed files against a BMad story File Scope section."""

from __future__ import annotations

import argparse
import fnmatch
import os
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path, PurePosixPath


STORY_KEY_PATTERN = re.compile(r"(?<!\d)(\d+-\d+-[a-z0-9][a-z0-9-]*)(?![a-z0-9-])", re.IGNORECASE)
BACKTICK_PATTERN = re.compile(r"`([^`]+)`")

ALLOWED_LABELS = {
    "allowed files for this story:",
    "expected files to add or edit:",
    "allowed to modify:",
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


def extract_story_key(value: str) -> str | None:
    match = STORY_KEY_PATTERN.search(value or "")
    return match.group(1).lower() if match else None


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
    completed = subprocess.run(
        ["git", *args],
        text=True,
        capture_output=True,
        check=False,
    )
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

    completed = subprocess.run(
        ["git", "interpret-trailers", "--parse"],
        input=message,
        text=True,
        capture_output=True,
        check=False,
    )
    if completed.returncode != 0:
        raise ValidationError(completed.stderr.strip() or "git interpret-trailers --parse failed")

    story_keys: list[str] = []
    overrides: list[str] = []
    for line in completed.stdout.splitlines():
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        normalized_key = key.strip().lower()
        normalized_value = value.strip()
        if normalized_key in {"story", "story-key"}:
            story_key = extract_story_key(normalized_value)
            if not story_key:
                raise ValidationError(f"Malformed Story trailer: {line}")
            story_keys.append(story_key)
        elif normalized_key == "scope-override":
            overrides.append(normalized_value)

    if len(story_keys) > 1:
        raise ValidationError("Multiple Story/Story-Key trailers found; keep exactly one story trailer.")
    return story_keys, overrides


def resolve_story_key(args: argparse.Namespace, trailer_keys: list[str]) -> StorySource:
    sources: list[StorySource] = []
    explicit = extract_story_key(args.story_key or "")
    if explicit:
        sources.append(StorySource("cli", explicit))
    if trailer_keys:
        sources.append(StorySource("trailer", trailer_keys[0]))
    branch = extract_story_key(args.branch_name or "")
    if branch:
        sources.append(StorySource("branch", branch))

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
    match = BACKTICK_PATTERN.search(line)
    if match:
        return match.group(1)

    value = line.strip()
    if value.startswith("- "):
        value = value[2:].strip()
    if not value or " " in value:
        return None
    return value


def parse_allowed_scope(story_path: Path) -> list[str]:
    lines = story_path.read_text(encoding="utf-8").splitlines()
    in_file_scope = False
    in_allowed = False
    allowed: list[str] = []

    for line in lines:
        stripped = line.strip()
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
        if label in ALLOWED_LABELS:
            in_allowed = True
            continue
        if in_allowed and (stripped.endswith(":") or (stripped.startswith("**") and stripped.endswith(":**"))):
            break

        if not in_allowed:
            continue
        if not stripped.startswith("- "):
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
    normalized_pattern = normalize_path(pattern)
    if fnmatch.fnmatchcase(path, normalized_pattern):
        return True
    if "/**/" in normalized_pattern:
        collapsed = normalized_pattern.replace("/**/", "/")
        return fnmatch.fnmatchcase(path, collapsed)
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
        raise ValidationError(f"Story artifact not found: {story_path}")

    allowed = parse_allowed_scope(story_path)
    overrides = parse_overrides(trailer_overrides)

    print(f"Selected story key: {source.key}")
    print(f"Story source: {source.name}")
    print(f"Story artifact: {story_path}")
    print("Allowed scope entries:")
    for entry in allowed:
        print(f"  - {entry}")

    allowed_files: list[str] = []
    out_of_scope: list[str] = []
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

    if out_of_scope:
        print("Out-of-scope files:")
        for path in out_of_scope:
            print(f"  - {path}")
        print("Accepted override format: Scope-Override: path/or/narrow-glob - short rationale")

    if out_of_scope or forbidden_overridden:
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
