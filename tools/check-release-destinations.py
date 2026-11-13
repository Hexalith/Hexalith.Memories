#!/usr/bin/env python3
"""Prove every versioned release destination absent with the pinned Builds helper."""

import argparse
import importlib.util
import os
import re
import subprocess
import sys
from pathlib import Path


APPROVED_BUILDS_SHA = "ae77d5690a9411a2154fa4ed5231edc373942573"
EXPECTED_PACKAGE_COUNT = 10
IMAGE_REPOSITORIES = (
    "memories",
    "memories-mcp",
    "memories-access-telemetry",
    "memories-access-telemetry-clock",
    "memories-domain-service",
)


def load_pinned_helper(shared_directory: Path):
    """Load only the independently approved Builds checkout."""
    result = subprocess.run(
        ["git", "-C", str(shared_directory.parents[1]), "rev-parse", "HEAD"],
        capture_output=True,
        text=True,
        check=True,
    )
    if result.stdout.strip() != APPROVED_BUILDS_SHA:
        raise ValueError("Destination proof is not the approved Builds revision.")
    helper = shared_directory / "publication_preflight.py"
    if not helper.is_file() or not (shared_directory / "oci_registry_validator.py").is_file():
        raise ValueError("Pinned Builds destination helper is unavailable.")
    sys.path.insert(0, str(shared_directory))
    try:
        specification = importlib.util.spec_from_file_location("pinned_publication_preflight", helper)
        if specification is None or specification.loader is None:
            raise ValueError("Pinned Builds destination helper could not load.")
        module = importlib.util.module_from_spec(specification)
        specification.loader.exec_module(module)
        return module
    finally:
        sys.path.pop(0)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True)
    parser.add_argument("--manifest", type=Path, default=Path("tools/release-packages.json"))
    parser.add_argument("--shared-directory", type=Path, default=Path(".hexalith/builds-execution/Github/publish-containers"))
    arguments = parser.parse_args()
    try:
        if not re.fullmatch(r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z.-]+)?", arguments.version):
            raise ValueError("Release version must be an image-taggable semantic version.")
        helper = load_pinned_helper(arguments.shared_directory)
        package_ids = helper._load_package_identity(arguments.manifest, EXPECTED_PACKAGE_COUNT)["ids"]
        registry = os.environ.get("HEXALITH_ZOT_REGISTRY") or "registry.hexalith.com"
        repositories = [f"{registry}/{name}" for name in IMAGE_REPOSITORIES]
        probe = helper.destination_probe(
            os.environ.get("HEXALITH_ZOT_USERNAME", ""),
            os.environ.get("HEXALITH_ZOT_API_KEY", ""),
        )
        helper.validate_destination_absence(
            package_ids, arguments.version, repositories, probe, EXPECTED_PACKAGE_COUNT
        )
    except Exception as error:
        print(f"[release-destinations] refused: {error}", file=sys.stderr)
        return 1
    print(f"[release-destinations] pass: {EXPECTED_PACKAGE_COUNT} NuGet packages and {len(IMAGE_REPOSITORIES)} images absent for {arguments.version}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
