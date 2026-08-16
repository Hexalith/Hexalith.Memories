#!/usr/bin/env python3
"""Validate one approved Git publication envelope without mutating state.

The caller fetches remotes before invoking this tool. The validator reads the
root repository, its index or a committed snapshot, local remote-tracking refs,
and checked-out submodule repositories. It never fetches, stages, commits,
pushes, or rewrites refs.
"""

from __future__ import annotations

import argparse
import configparser
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path, PurePosixPath
from urllib.parse import unquote, urlsplit


OBJECT_ID_PATTERN = re.compile(r"[0-9a-f]{40}", re.ASCII)
SCP_REMOTE_PATTERN = re.compile(r"^(?:[^@/]+@)?(?P<host>[^:/]+):(?P<path>.+)$")


class ValidationError(Exception):
    """Raised for an expected publication validation failure."""


@dataclass(frozen=True)
class GitlinkExpectation:
    """An approved gitlink path and exact commit object ID."""

    path: str
    object_id: str


@dataclass(frozen=True)
class TreeEntry:
    """One index or tree entry."""

    mode: str
    object_type: str
    object_id: str
    path: str


@dataclass(frozen=True)
class SubmoduleRegistration:
    """One root .gitmodules registration."""

    path: str
    url: str


def parse_args(argv: list[str]) -> argparse.Namespace:
    """Parse command-line arguments."""
    parser = argparse.ArgumentParser(
        description=(
            "Validate a complete approved publication delta, exact gitlink OIDs, "
            "submodule identity/reachability, and root fast-forward safety using "
            "already-fetched refs."
        ),
    )
    parser.add_argument(
        "--repository",
        default=".",
        help="Root Git repository to inspect. Defaults to the current directory.",
    )
    parser.add_argument(
        "--snapshot",
        required=True,
        help="Publication snapshot: 'index' before commit, or a committed tree-ish after commit.",
    )
    parser.add_argument(
        "--root-remote-ref",
        default="origin/main",
        help="Already-fetched root remote-tracking ref. Defaults to origin/main.",
    )
    parser.add_argument(
        "--expected-root-remote-oid",
        required=True,
        help="Exact fetched root remote OID authorized for this publication.",
    )
    parser.add_argument(
        "--candidate-ref",
        default="HEAD",
        help="Root baseline before commit or fast-forward candidate after commit. Defaults to HEAD.",
    )
    parser.add_argument(
        "--submodule-remote-ref",
        default="origin/main",
        help="Already-fetched ref used for every submodule reachability check.",
    )
    parser.add_argument(
        "--expected-path",
        action="append",
        required=True,
        metavar="PATH",
        help="Approved changed non-gitlink path. Repeat for every expected path.",
    )
    parser.add_argument(
        "--expected-gitlink",
        action="append",
        required=True,
        metavar="PATH=OID",
        help="Approved changed gitlink and exact 40-hex commit OID. Repeat for every gitlink.",
    )
    return parser.parse_args(argv)


def normalize_object_id(value: str, description: str) -> str:
    """Return a canonical lower-case object ID or fail closed."""
    normalized = value.strip().lower()
    if not OBJECT_ID_PATTERN.fullmatch(normalized):
        raise ValidationError(f"{description} must be exactly 40 hexadecimal characters: {value}")
    return normalized


def normalize_git_path(value: str) -> str:
    """Return a safe normalized repository-relative POSIX path."""
    cleaned = value.strip().replace("\\", "/")
    path = PurePosixPath(cleaned)
    if (
        not cleaned
        or path.is_absolute()
        or any(part in {"", ".", ".."} for part in path.parts)
        or path.as_posix() != cleaned
    ):
        raise ValidationError(f"Path must be normalized and repository-relative: {value}")
    return path.as_posix()


def parse_gitlink(value: str) -> GitlinkExpectation:
    """Parse one PATH=OID expectation."""
    if "=" not in value:
        raise ValidationError(f"Expected gitlink must use PATH=OID syntax: {value}")
    raw_path, raw_object_id = value.rsplit("=", 1)
    return GitlinkExpectation(
        normalize_git_path(raw_path),
        normalize_object_id(raw_object_id, f"Gitlink OID for {raw_path.strip()}"),
    )


def run_git(repository: Path, args: list[str], *, check: bool = True) -> subprocess.CompletedProcess[str]:
    """Run Git in a repository and preserve output for diagnostics."""
    try:
        completed = subprocess.run(
            ["git", "-C", str(repository), *args],
            text=True,
            capture_output=True,
            check=False,
        )
    except FileNotFoundError as exc:
        raise ValidationError("Required tool not found: git") from exc
    if check and completed.returncode != 0:
        detail = completed.stderr.strip() or completed.stdout.strip()
        raise ValidationError(detail or f"git {' '.join(args)} failed with exit {completed.returncode}")
    return completed


def resolve_commit(repository: Path, revision: str, description: str) -> str:
    """Resolve a revision to exactly one commit OID."""
    completed = run_git(repository, ["rev-parse", "--verify", f"{revision}^{{commit}}"])
    lines = [line.strip().lower() for line in completed.stdout.splitlines() if line.strip()]
    if len(lines) != 1 or not OBJECT_ID_PATTERN.fullmatch(lines[0]):
        raise ValidationError(f"{description} did not resolve to exactly one commit: {revision}")
    return lines[0]


def resolve_tree(repository: Path, revision: str, description: str) -> str:
    """Resolve a revision to exactly one tree OID."""
    completed = run_git(repository, ["rev-parse", "--verify", f"{revision}^{{tree}}"])
    lines = [line.strip().lower() for line in completed.stdout.splitlines() if line.strip()]
    if len(lines) != 1 or not OBJECT_ID_PATTERN.fullmatch(lines[0]):
        raise ValidationError(f"{description} did not resolve to exactly one tree: {revision}")
    return lines[0]


def parse_index_entries(repository: Path, path: str | None = None) -> list[TreeEntry]:
    """Read stage-zero entries from the root index."""
    arguments = ["ls-files", "--stage", "-z"]
    if path is not None:
        arguments.extend(("--", path))
    completed = run_git(repository, arguments)
    entries: list[TreeEntry] = []
    for raw_entry in (entry for entry in completed.stdout.split("\0") if entry):
        if "\t" not in raw_entry:
            raise ValidationError(f"Unexpected index entry: {raw_entry}")
        metadata, actual_path = raw_entry.split("\t", 1)
        fields = metadata.split()
        if len(fields) != 3:
            raise ValidationError(f"Unexpected index metadata for {actual_path}: {metadata}")
        mode, object_id, stage = fields
        if stage != "0":
            raise ValidationError(f"Index contains a non-stage-zero entry: {actual_path} stage {stage}")
        object_type = "commit" if mode == "160000" else "blob"
        entries.append(TreeEntry(mode, object_type, object_id.lower(), actual_path))
    return entries


def parse_tree_entries(repository: Path, snapshot: str, path: str | None = None) -> list[TreeEntry]:
    """Read entries recursively from one committed tree."""
    arguments = ["ls-tree", "-r", "-z", snapshot]
    if path is not None:
        arguments.extend(("--", path))
    completed = run_git(repository, arguments)
    entries: list[TreeEntry] = []
    for raw_entry in (entry for entry in completed.stdout.split("\0") if entry):
        if "\t" not in raw_entry:
            raise ValidationError(f"Unexpected tree entry in {snapshot}: {raw_entry}")
        metadata, actual_path = raw_entry.split("\t", 1)
        fields = metadata.split()
        if len(fields) != 3:
            raise ValidationError(f"Unexpected tree metadata for {actual_path}: {metadata}")
        mode, object_type, object_id = fields
        entries.append(TreeEntry(mode, object_type, object_id.lower(), actual_path))
    return entries


def get_snapshot_entry(repository: Path, snapshot: str, path: str) -> TreeEntry:
    """Read exactly one expected path from the selected snapshot."""
    entries = (
        parse_index_entries(repository, path)
        if snapshot == "index"
        else parse_tree_entries(repository, snapshot, path)
    )
    if len(entries) != 1 or entries[0].path != path:
        raise ValidationError(f"Snapshot {snapshot} must contain exactly one entry for expected path: {path}")
    return entries[0]


def list_snapshot_gitlinks(repository: Path, snapshot: str) -> dict[str, str]:
    """Return every gitlink in an index or committed snapshot."""
    entries = parse_index_entries(repository) if snapshot == "index" else parse_tree_entries(repository, snapshot)
    return {entry.path: entry.object_id for entry in entries if entry.mode == "160000"}


def changed_paths(repository: Path, snapshot: str, baseline_oid: str, candidate_oid: str) -> set[str]:
    """Return the complete no-renames publication delta."""
    if snapshot == "index":
        arguments = ["diff", "--cached", "--name-only", "--no-renames", "-z", candidate_oid, "--"]
    else:
        arguments = ["diff", "--name-only", "--no-renames", "-z", baseline_oid, candidate_oid, "--"]
    completed = run_git(repository, arguments)
    return {path for path in completed.stdout.split("\0") if path}


def validate_exact_delta(actual_paths: set[str], expected_paths: set[str]) -> None:
    """Require exact set equality for the publication envelope."""
    missing = sorted(expected_paths - actual_paths)
    extra = sorted(actual_paths - expected_paths)
    if missing or extra:
        details: list[str] = []
        if missing:
            details.append("missing expected paths: " + ", ".join(missing))
        if extra:
            details.append("unexpected changed paths: " + ", ".join(extra))
        raise ValidationError("Publication delta differs from the approved envelope (" + "; ".join(details) + ")")


def validate_root_safety(
    repository: Path,
    snapshot: str,
    root_remote_ref: str,
    expected_root_remote_oid: str,
    candidate_ref: str,
) -> tuple[str, str]:
    """Bind the selected snapshot to an unchanged, fast-forward-safe root."""
    remote_oid = resolve_commit(repository, root_remote_ref, "Root remote ref")
    if remote_oid != expected_root_remote_oid:
        raise ValidationError(
            f"Root remote moved: {root_remote_ref} is {remote_oid}, expected {expected_root_remote_oid}",
        )
    candidate_oid = resolve_commit(repository, candidate_ref, "Root candidate ref")

    if snapshot == "index":
        if candidate_oid != remote_oid:
            raise ValidationError(
                f"Pre-commit candidate baseline must equal {root_remote_ref}: "
                f"candidate {candidate_oid}, remote {remote_oid}",
            )
        return remote_oid, candidate_oid

    if candidate_oid == remote_oid:
        raise ValidationError("Post-commit candidate must be strictly ahead of the expected root remote")
    ancestry = run_git(
        repository,
        ["merge-base", "--is-ancestor", remote_oid, candidate_oid],
        check=False,
    )
    if ancestry.returncode != 0:
        raise ValidationError(
            f"Root candidate {candidate_oid} is not a fast-forward of {root_remote_ref} {remote_oid}",
        )
    snapshot_tree = resolve_tree(repository, snapshot, "Publication snapshot")
    candidate_tree = resolve_tree(repository, candidate_oid, "Root candidate")
    if snapshot_tree != candidate_tree:
        raise ValidationError(
            f"Publication snapshot tree {snapshot_tree} does not match candidate tree {candidate_tree}",
        )
    return remote_oid, candidate_oid


def read_submodule_registrations(repository: Path, candidate_oid: str) -> dict[str, SubmoduleRegistration]:
    """Read root submodule path/URL registrations from the candidate tree."""
    completed = run_git(repository, ["show", f"{candidate_oid}:.gitmodules"])
    parser = configparser.RawConfigParser(interpolation=None)
    try:
        parser.read_string(completed.stdout)
    except configparser.Error as exc:
        raise ValidationError(f"Candidate .gitmodules is invalid: {exc}") from exc

    registrations: dict[str, SubmoduleRegistration] = {}
    for section in parser.sections():
        if not section.startswith('submodule "'):
            continue
        if not parser.has_option(section, "path") or not parser.has_option(section, "url"):
            raise ValidationError(f"Candidate .gitmodules section lacks path or url: {section}")
        path = normalize_git_path(parser.get(section, "path"))
        url = parser.get(section, "url").strip()
        if not url:
            raise ValidationError(f"Candidate .gitmodules URL is empty for {path}")
        if path in registrations:
            raise ValidationError(f"Candidate .gitmodules registers the path more than once: {path}")
        registrations[path] = SubmoduleRegistration(path, url)
    return registrations


def canonical_remote_identity(url: str, repository: Path) -> str:
    """Normalize equivalent local, HTTPS, SSH, and scp-style Git remote URLs."""
    value = url.strip()
    if not value:
        raise ValidationError("Git remote URL is empty")

    parsed = urlsplit(value)
    if parsed.scheme == "file":
        return "local:" + str(Path(unquote(parsed.path)).resolve())
    if parsed.scheme and parsed.hostname:
        path = unquote(parsed.path).strip("/")
        if path.endswith(".git"):
            path = path[:-4]
        return f"network:{parsed.hostname.lower()}:{parsed.port or ''}/{path}"

    scp_match = SCP_REMOTE_PATTERN.fullmatch(value)
    if scp_match and not re.match(r"^[A-Za-z]:[\\/]", value):
        path = scp_match.group("path").strip("/")
        if path.endswith(".git"):
            path = path[:-4]
        return f"network:{scp_match.group('host').lower()}:/{path}"

    local_path = Path(value)
    if not local_path.is_absolute():
        local_path = repository / local_path
    return "local:" + str(local_path.resolve())


def validate_submodule_identity(
    repository: Path,
    registration: SubmoduleRegistration,
    expectation: GitlinkExpectation,
    submodule_remote_ref: str,
) -> None:
    """Require registered remote identity and reachable approved commit."""
    submodule_repository = repository / expectation.path
    inside = run_git(submodule_repository, ["rev-parse", "--is-inside-work-tree"], check=False)
    if inside.returncode != 0 or inside.stdout.strip() != "true":
        raise ValidationError(f"Approved submodule checkout is unavailable: {expectation.path}")

    actual_remote = run_git(submodule_repository, ["remote", "get-url", "origin"]).stdout.strip()
    registered_identity = canonical_remote_identity(registration.url, repository)
    actual_identity = canonical_remote_identity(actual_remote, submodule_repository)
    if registered_identity != actual_identity:
        raise ValidationError(
            f"Submodule origin does not match root .gitmodules for {expectation.path}: "
            f"registered {registration.url}, checkout {actual_remote}",
        )

    try:
        resolved_object_id = resolve_commit(
            submodule_repository,
            expectation.object_id,
            f"Approved submodule commit for {expectation.path}",
        )
    except ValidationError as exc:
        raise ValidationError(
            f"Approved submodule commit is missing for {expectation.path}: {expectation.object_id} ({exc})",
        ) from exc
    if resolved_object_id != expectation.object_id:
        raise ValidationError(
            f"Approved submodule commit resolved unexpectedly for {expectation.path}: {resolved_object_id}",
        )
    remote_oid = resolve_commit(
        submodule_repository,
        submodule_remote_ref,
        f"Submodule remote ref for {expectation.path}",
    )
    ancestry = run_git(
        submodule_repository,
        ["merge-base", "--is-ancestor", expectation.object_id, remote_oid],
        check=False,
    )
    if ancestry.returncode != 0:
        raise ValidationError(
            f"Approved submodule commit is not reachable from {submodule_remote_ref} for "
            f"{expectation.path}: {expectation.object_id}",
        )


def validate_gitlink_changes(
    repository: Path,
    snapshot: str,
    baseline_oid: str,
    expectations: list[GitlinkExpectation],
) -> None:
    """Reject omitted, extra, or unrelated root gitlink changes."""
    baseline_gitlinks = list_snapshot_gitlinks(repository, baseline_oid)
    snapshot_gitlinks = list_snapshot_gitlinks(repository, snapshot)
    changed_gitlinks = {
        path
        for path in baseline_gitlinks.keys() | snapshot_gitlinks.keys()
        if baseline_gitlinks.get(path) != snapshot_gitlinks.get(path)
    }
    expected_gitlinks = {expectation.path for expectation in expectations}
    missing = sorted(expected_gitlinks - changed_gitlinks)
    extra = sorted(changed_gitlinks - expected_gitlinks)
    if missing or extra:
        details: list[str] = []
        if missing:
            details.append("expected gitlinks did not change: " + ", ".join(missing))
        if extra:
            details.append("unexpected root gitlinks changed: " + ", ".join(extra))
        raise ValidationError("Root gitlink delta differs from approval (" + "; ".join(details) + ")")


def validate(args: argparse.Namespace) -> None:
    """Validate the complete publication preflight contract."""
    repository = Path(args.repository).resolve()
    top_level = run_git(repository, ["rev-parse", "--show-toplevel"]).stdout.strip()
    if Path(top_level).resolve() != repository:
        raise ValidationError(f"--repository must name the root Git worktree: {repository}")

    expected_root_remote_oid = normalize_object_id(
        args.expected_root_remote_oid,
        "Expected root remote OID",
    )
    expected_paths = [normalize_git_path(value) for value in args.expected_path]
    expectations = [parse_gitlink(value) for value in args.expected_gitlink]
    all_expected_paths = expected_paths + [expectation.path for expectation in expectations]
    if len(all_expected_paths) != len(set(all_expected_paths)):
        raise ValidationError("Each approved publication path must be specified exactly once")

    remote_oid, candidate_oid = validate_root_safety(
        repository,
        args.snapshot,
        args.root_remote_ref,
        expected_root_remote_oid,
        args.candidate_ref,
    )
    validate_gitlink_changes(repository, args.snapshot, remote_oid, expectations)
    actual_paths = changed_paths(repository, args.snapshot, remote_oid, candidate_oid)
    validate_exact_delta(actual_paths, set(all_expected_paths))

    for path in expected_paths:
        entry = get_snapshot_entry(repository, args.snapshot, path)
        if entry.mode == "160000" or entry.object_type == "commit":
            raise ValidationError(f"Expected non-gitlink path is a gitlink: {path}")

    for expectation in expectations:
        entry = get_snapshot_entry(repository, args.snapshot, expectation.path)
        if entry.mode != "160000" or entry.object_type != "commit":
            raise ValidationError(f"Approved path is not a 160000 gitlink: {expectation.path}")
        if entry.object_id != expectation.object_id:
            raise ValidationError(
                f"Gitlink OID drift for {expectation.path}: "
                f"found {entry.object_id}, expected {expectation.object_id}",
            )

    registrations = read_submodule_registrations(repository, candidate_oid)
    for expectation in expectations:
        registration = registrations.get(expectation.path)
        if registration is None:
            raise ValidationError(
                f"Approved submodule path is not registered by root .gitmodules: {expectation.path}",
            )
        validate_submodule_identity(
            repository,
            registration,
            expectation,
            args.submodule_remote_ref,
        )

    mode = "pre-commit index" if args.snapshot == "index" else "post-commit tree"
    print(f"Publication mode: {mode}")
    print(f"Root remote unchanged: {args.root_remote_ref}={remote_oid}")
    print(f"Root candidate bound to snapshot: {args.candidate_ref}={candidate_oid}")
    print("Approved publication paths: " + ", ".join(sorted(all_expected_paths)))
    for expectation in expectations:
        print(
            f"Approved gitlink registered and reachable: {expectation.path}={expectation.object_id} "
            f"via {args.submodule_remote_ref}",
        )
    print("Approved publication preflight passed.")


def main(argv: list[str] | None = None) -> int:
    """Run the validator and return a process exit code."""
    try:
        validate(parse_args(sys.argv[1:] if argv is None else argv))
    except ValidationError as exc:
        print(f"Approved publication preflight failed: {exc}")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
