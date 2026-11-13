#!/usr/bin/env python3
"""Reject release dispatches without exact current-main push CI before approval."""

import argparse
import importlib.util
import os
import re
import subprocess
import sys
from pathlib import Path


APPROVED_BUILDS_SHA = "ae77d5690a9411a2154fa4ed5231edc373942573"
SHA_PATTERN = re.compile(r"^[0-9a-f]{40}$", re.ASCII)


def checked_git_sha(path: Path) -> str:
    """Read an exact checkout identity, failing closed on any git error."""
    result = subprocess.run(
        ["git", "-C", str(path), "rev-parse", "HEAD"],
        capture_output=True,
        text=True,
        check=True,
    )
    sha = result.stdout.strip()
    if SHA_PATTERN.fullmatch(sha) is None:
        raise ValueError("Checkout has no exact lowercase commit SHA.")
    return sha


def prove_source(repository: str, sha: str, shared_directory: Path, token: str):
    """Use the pinned Builds source and CI proof without copying its rules."""
    if repository != "Hexalith/Hexalith.Memories":
        raise ValueError("Release repository is not Hexalith/Hexalith.Memories.")
    if os.environ.get("GITHUB_EVENT_NAME") != "workflow_dispatch":
        raise ValueError("Release requires an intentional workflow_dispatch event.")
    if os.environ.get("GITHUB_REF") != "refs/heads/main":
        raise ValueError("Release dispatch must select refs/heads/main.")
    if SHA_PATTERN.fullmatch(sha) is None or os.environ.get("GITHUB_SHA") != sha:
        raise ValueError("Dispatch SHA is invalid or differs from the selected source.")
    if checked_git_sha(Path.cwd()) != sha:
        raise ValueError("Caller checkout differs from the dispatch source.")
    if checked_git_sha(shared_directory.parents[1]) != APPROVED_BUILDS_SHA:
        raise ValueError("Shared source proof is not the approved Builds revision.")
    helper = shared_directory / "publication_preflight.py"
    if not helper.is_file() or not (shared_directory / "oci_registry_validator.py").is_file():
        raise ValueError("Pinned Builds source-proof helper is unavailable.")
    sys.path.insert(0, str(shared_directory))
    try:
        specification = importlib.util.spec_from_file_location("pinned_publication_preflight", helper)
        if specification is None or specification.loader is None:
            raise ValueError("Pinned Builds source-proof helper could not load.")
        module = importlib.util.module_from_spec(specification)
        specification.loader.exec_module(module)
        proof = module.prove_current_green_source(repository, sha, "main", "ci.yml", token)
    finally:
        sys.path.pop(0)
    if proof.get("live_sha") != sha or proof.get("ci_run", {}).get("head_sha") != sha:
        raise ValueError("Pinned Builds returned an incomplete exact-source proof.")
    return proof


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--shared-directory", type=Path, default=Path(".hexalith/builds-execution/Github/publish-containers"))
    args = parser.parse_args()
    try:
        repository = os.environ.get("GITHUB_REPOSITORY", "")
        sha = os.environ.get("GITHUB_SHA", "")
        proof = prove_source(repository, sha, args.shared_directory, os.environ.get("GITHUB_TOKEN", ""))
    except Exception as error:
        print(f"[release-source] refused: {error}", file=sys.stderr)
        return 1
    print(f"[release-source] pass: current main {proof['live_sha']} push CI {proof['ci_run']['id']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
