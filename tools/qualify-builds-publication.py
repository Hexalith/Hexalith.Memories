#!/usr/bin/env python3
"""Qualify the pinned Hexalith.Builds container-publication contract offline.

Story 30.2 owns this harness. It installs the real pinned Builds helpers the way
``Github/publish-containers/action.yml`` does, then drives the real publisher,
publication preflight, OCI registry validator and platform smoke over the declared
Memories release set. Only the network, registry, docker and dotnet seams are fake:
the harness never pushes, publishes, uploads or contacts a registry.

Six revision-bound checkpoint results and a pending owner-approval record are written
to ``--output-directory``. A mismatch, missing member, unsupported phase, ambiguous
destination or incomplete evidence fails the whole qualification closed: every result
then reports ``fail`` and the process exits non-zero. The harness never records owner
approval and refuses to overwrite an approval record that is no longer pending.

The same file provides the fake seams. The generated shims re-enter it with
``--seam <name>``; that mode is internal and not a supported interface.
"""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import io
import json
import os
import re
import shlex
import shutil
import subprocess  # nosec B404 -- only repository-owned helpers and generated shims are executed.
import sys
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET  # nosec B405 -- parses repository-owned project files only.
from dataclasses import dataclass, field
from email.message import Message
from pathlib import Path, PurePosixPath


SCHEMA_VERSION = "hexalith.memories.builds-publication-qualification.v1"
PROFILE_SCHEMA_VERSION = "hexalith.memories.builds-publication-profile.v1"
PARTIAL_RESULT_SCHEMA = "hexalith.memories.partial-publication-result.v1"
STORY_KEY = "30-2-pinned-builds-publication-and-recovery-qualification"
CHECKPOINTS = (
    "complete-set-dry-run",
    "set-drift",
    "phase-verification",
    "partial-publication",
    "recovery",
    "evidence-upload",
)
APPROVAL_FILE = "approval.md"
PENDING_STATUS_LINE = "Status: pending"
OWNER_DECISIONS_HEADING = "## Owner decisions"
APPROVAL_ROLES = ("Builds Maintainer", "Memories Release Engineer")
UPSTREAM_FIXTURE_COMMAND = "pwsh -NoProfile -File references/Hexalith.Builds/Tools/test-publish-containers.ps1"

HARNESS_LABEL = "tools/qualify-builds-publication.py"
PROFILE_LABEL = "tools/builds-publication-qualification.json"
BUILDS_REPOSITORY = "Hexalith/Hexalith.Builds"
BUILDS_PATH = PurePosixPath("references/Hexalith.Builds")
HELPER_DIRECTORY = PurePosixPath("Github/publish-containers")
# The five files action.yml installs into .hexalith/release and the publisher executes.
EXECUTED_HELPERS = (
    "publish-containers.sh",
    "oci_registry_validator.py",
    "publication_preflight.py",
    "smoke-container-platforms.sh",
    "smoke_container_platforms.py",
)
DOMAIN_RELEASE_WORKFLOW = ".github/workflows/domain-release.yml"
PREPARE_DOMAIN_RELEASE_ACTION = "Github/prepare-domain-release/action.yml"
# Pinned files this harness reads for its findings and exceptions, never executes.
REFERENCE_FILES = (
    "Github/publish-containers/action.yml",
    DOMAIN_RELEASE_WORKFLOW,
    PREPARE_DOMAIN_RELEASE_ACTION,
)
UPLOAD_STEP_NAME = "Upload complete release evidence"
MANIFEST_PATH = PurePosixPath("tools/release-packages.json")
RECOVERY_WORKFLOW_PATH = PurePosixPath(".github/workflows/recover-partial-release.yml")
BUILD_CUSTOMIZATION_FILES = ("Directory.Build.props", "Directory.Build.targets")

RELEASE_REPOSITORY = "Hexalith/Hexalith.Memories"
REGISTRY = "registry.hexalith.com"
# A synthetic stable version. The version floor requires a stable candidate, and the fake
# destinations never hold it, so no real release identity is implied.
QUALIFICATION_VERSION = "0.0.302"
ENVIRONMENT_NAME = "production"
SOURCE_BRANCH = "main"
SOURCE_CI_WORKFLOW = "ci.yml"
PLATFORMS = ("linux/amd64", "linux/arm64")
RUNTIME_IDENTIFIERS = '"linux-musl-x64;linux-musl-arm64"'
SYNTHETIC_CI_RUN_ID = 3020000000
SYNTHETIC_RUN_ENVIRONMENT = {
    "GITHUB_REF": f"refs/heads/{SOURCE_BRANCH}",
    "GITHUB_RUN_ID": "3020000001",
    "GITHUB_RUN_ATTEMPT": "1",
    "GITHUB_RUN_NUMBER": "302",
    "GITHUB_EVENT_NAME": "workflow_dispatch",
    "GITHUB_WORKFLOW_REF": f"{RELEASE_REPOSITORY}/.github/workflows/release.yml@refs/heads/{SOURCE_BRANCH}",
    "GITHUB_ACTOR": "memories-qualification",
    "GITHUB_TRIGGERING_ACTOR": "memories-qualification",
}
SYNTHETIC_REGISTRY_USERNAME = "memories-qualification"
SYNTHETIC_REGISTRY_API_KEY = "synthetic-qualification-key-not-a-secret"
WORLD_VARIABLE = "MEMORIES_QUALIFICATION_WORLD"
UNREACHABLE_PROXY = "http://127.0.0.1:9"
SUBPROCESS_TIMEOUT_SECONDS = 600

OCI_INDEX_MEDIA_TYPE = "application/vnd.oci.image.index.v1+json"
OCI_MANIFEST_MEDIA_TYPE = "application/vnd.oci.image.manifest.v1+json"
OCI_CONFIG_MEDIA_TYPE = "application/vnd.oci.image.config.v1+json"
PREFLIGHT_FILES = (
    "publication-identity.json",
    "publication-preflight.verify.json",
    "publication-preflight.publish.json",
    "publication-preflight.container.json",
)
MEMBER_FILES = (
    "index.raw",
    "child-linux-amd64.manifest.raw",
    "child-linux-amd64.config.raw",
    "child-linux-arm64.manifest.raw",
    "child-linux-arm64.config.raw",
    "oci-validation.json",
    "smoke-preflight.log",
    "smoke-linux-amd64.log",
    "smoke-linux-arm64.log",
    "smoke-results.json",
    "smoke-sha256.txt",
)
SMOKE_HASHED_FILES = ("smoke-preflight.log", "smoke-linux-amd64.log", "smoke-linux-arm64.log", "smoke-results.json")
PARTIAL_VARIANTS = (
    ("publish-failure", "publish-failure"),
    ("validation-failure", "invalid-image"),
    ("smoke-failure", "smoke-failure"),
)
PARTIAL_CAUSES = {
    "publish-failure": "[fake-dotnet] synthetic publish failure",
    "invalid-image": "[oci-registry-validator] platform-set-mismatch:",
    "smoke-failure": "[container-smoke] image-start-failure",
}
EXPECTED_DOTNET_PROPERTIES = (
    "RuntimeIdentifiers",
    "ContainerRuntimeIdentifiers",
    "ContainerImageFormat",
    "UseHexalithProjectReferences",
    "ContainerRegistry",
    "ContainerRepository",
    "ContainerImageTag",
    "Version",
    "ContainerProvenanceSourceSha",
    "ContainerProvenanceReleaseVersion",
    "ContainerProvenanceCreated",
)
REQUIRED_PROVENANCE_LABELS = (
    "org.opencontainers.image.source",
    "org.opencontainers.image.url",
    "org.opencontainers.image.documentation",
    "org.opencontainers.image.revision",
    "org.opencontainers.image.version",
    "org.opencontainers.image.created",
    "org.opencontainers.artifact.created",
)

SHA_PATTERN = re.compile(r"[0-9a-f]{40}", re.ASCII)
REPOSITORY_NAME_PATTERN = re.compile(r"[a-z0-9]+(?:[._-][a-z0-9]+)*", re.ASCII)
CREATED_PATTERN = re.compile(r"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z", re.ASCII)
PREFLIGHT_REFUSAL = re.compile(r"\[publication-preflight\] ([a-z0-9-]+):")
VALIDATOR_REFUSAL = re.compile(r"\[oci-registry-validator\] ([a-z0-9-]+):")

# Approved 2026-10-31: each Builds gap is a named caller-owned exception.
EXCEPTION_CATALOG = {
    "no-dry-run-mode": {
        "gap": "The pinned publisher has no dry-run mode; every invocation logs in and publishes.",
        "callerControl": "This fixture-driven offline harness drives the real pinned helpers with fake network, registry, docker and dotnet seams.",
        "handedTo": [],
    },
    "no-partial-result-document": {
        "gap": "The publisher stops at the first failed member and writes no set-level result.",
        "callerControl": "The caller derives published, ambiguous and not-attempted members from per-image evidence; an ambiguous member is never success.",
        "handedTo": ["30-5-partial-release-recovery"],
    },
    "no-missing-only-republish": {
        "gap": "The publisher cannot republish only the missing members; a rerun refuses the occupied destinations before any write.",
        "callerControl": "Caller-owned missing-member publication from immutable evidence.",
        "handedTo": ["30-5-partial-release-recovery"],
    },
    "no-caller-set-check": {
        "gap": "Builds validates mapping syntax and freezes the repository set, but never checks the caller's declared release set or project-to-repository mapping.",
        "callerControl": "tools/builds-publication-qualification.json declares the set, and the caller refuses drift before invoking Builds.",
        "handedTo": ["30-4-five-image-publication-contract"],
    },
    "warn-upload-without-hash-manifest": {
        "gap": "The reference upload step uses if-no-files-found: warn and uploads no hash manifest of the evidence set.",
        "callerControl": "The caller writes a SHA-256 manifest of the complete evidence set, and the caller upload must fail on missing files.",
        "handedTo": [
            "30-3-guarded-release-dispatch-and-shared-caller-adoption",
            "30-4-five-image-publication-contract",
        ],
    },
}


class QualificationError(Exception):
    """A deterministic, categorized qualification failure."""

    def __init__(self, code: str, message: str) -> None:
        """Initialize a failure with a stable code."""
        super().__init__(f"{code}: {message}")
        self.code = code


# --------------------------------------------------------------------------- utilities


def sha256_bytes(data: bytes) -> str:
    """Return the lowercase SHA-256 hex digest of *data*."""
    return hashlib.sha256(data).hexdigest()


class _DuplicateField(ValueError):
    """A JSON object repeats a field, so its last value would silently win."""


def _unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise _DuplicateField(f"duplicate JSON field {key!r}")
        result[key] = value
    return result


def load_strict_json(data: bytes, code: str, subject: str):
    """Parse UTF-8 JSON, rejecting duplicate fields."""
    try:
        return json.loads(data.decode("utf-8"), object_pairs_hook=_unique_object)
    except (UnicodeDecodeError, ValueError) as error:
        raise QualificationError(code, f"{subject} is not valid strict JSON: {error}") from error


def _read_json_file(path: Path):
    try:
        return json.loads(path.read_bytes().decode("utf-8"))
    except (OSError, UnicodeDecodeError, ValueError):
        return None


def _read_member_json(path: Path) -> tuple:
    """Strictly parse retained member evidence; return ``(document, problem)``."""
    try:
        data = path.read_bytes()
    except OSError:
        return None, "unreadable"
    try:
        document = json.loads(data.decode("utf-8"), object_pairs_hook=_unique_object)
    except _DuplicateField:
        return None, "duplicate-key"
    except (UnicodeDecodeError, ValueError):
        return None, "invalid-json"
    return (document, None) if isinstance(document, dict) else (None, "invalid-json")


def write_crlf(path: Path, text: str) -> bytes:
    """Write *text* atomically with CRLF line endings and return the written bytes.

    ``.gitattributes`` materializes JSON and Markdown as CRLF, so recorded SHA-256 values
    of the evidence stay valid after a fresh checkout.
    """
    data = text.replace("\r\n", "\n").replace("\n", "\r\n").encode("utf-8")
    temporary = path.with_name(path.name + ".tmp")
    temporary.write_bytes(data)
    os.replace(temporary, path)
    return data


def git(directory: Path, *arguments: str, binary: bool = False):
    """Run a read-only git command and return its output."""
    try:
        completed = subprocess.run(  # nosec B603 B607 -- fixed read-only git invocations.
            ["git", "-C", str(directory), *arguments],
            capture_output=True,
            check=False,
            timeout=120,
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise QualificationError("git-unavailable", f"git {' '.join(arguments)} could not run.") from error
    if completed.returncode != 0:
        raise QualificationError(
            "git-command-failed",
            f"git {' '.join(arguments)} exited {completed.returncode}: "
            f"{completed.stderr.decode('utf-8', 'replace').strip()[:300]}",
        )
    return completed.stdout if binary else completed.stdout.decode("utf-8")


def _bounded(text: str, limit: int = 600) -> str:
    text = " | ".join(line.strip() for line in (text or "").splitlines() if line.strip())
    return text if len(text) <= limit else text[: limit - 3] + "..."


def _bounded_tail(text: str, limit: int = 600) -> str:
    text = " | ".join(line.strip() for line in (text or "").splitlines() if line.strip())
    return text if len(text) <= limit else "..." + text[-(limit - 3):]


# ------------------------------------------------------------- profile and release set


@dataclass(frozen=True)
class Mapping:
    """One declared ``project.csproj|repository`` container mapping."""

    project: str
    repository: str

    @property
    def line(self) -> str:
        """Return the Builds ``HEXALITH_CONTAINER_PROJECTS`` line."""
        return f"{self.project}|{self.repository}"


@dataclass(frozen=True)
class Profile:
    """The independent caller declaration of the release set."""

    builds_sha: str
    expected_package_count: int
    mappings: tuple[Mapping, ...]


def parse_mapping(value) -> Mapping:
    """Parse one declared mapping line, failing closed on any ambiguity."""
    if not isinstance(value, str) or value != value.strip() or value.count("|") != 1:
        raise QualificationError("profile-invalid", f"Container mapping {value!r} must be exactly project|repository.")
    project, repository = value.split("|")
    path = PurePosixPath(project)
    if (
        not project
        or path.is_absolute()
        or ".." in path.parts
        or "\\" in project
        or path.suffix != ".csproj"
        or path.as_posix() != project
    ):
        raise QualificationError("profile-invalid", f"Container project {project!r} must be a relative .csproj path.")
    if REPOSITORY_NAME_PATTERN.fullmatch(repository) is None:
        raise QualificationError("profile-invalid", f"Container repository {repository!r} is invalid.")
    return Mapping(project, repository)


def load_profile(data: bytes) -> Profile:
    """Load the declared qualification profile with an exact schema."""
    document = load_strict_json(data, "profile-invalid", "Qualification profile")
    keys = ("schemaVersion", "buildsSha", "expectedPackageCount", "containerProjects")
    if not isinstance(document, dict) or set(document) != set(keys):
        raise QualificationError("profile-invalid", f"Qualification profile must contain exactly {', '.join(keys)}.")
    if document["schemaVersion"] != PROFILE_SCHEMA_VERSION:
        raise QualificationError("profile-invalid", "Qualification profile schemaVersion is unsupported.")
    builds_sha = document["buildsSha"]
    if not isinstance(builds_sha, str) or SHA_PATTERN.fullmatch(builds_sha) is None:
        raise QualificationError("profile-invalid", "buildsSha must be an exact lowercase 40-character commit SHA.")
    count = document["expectedPackageCount"]
    if not isinstance(count, int) or isinstance(count, bool) or count <= 0:
        raise QualificationError("profile-invalid", "expectedPackageCount must be a positive integer.")
    projects = document["containerProjects"]
    if not isinstance(projects, list) or not projects:
        raise QualificationError("profile-invalid", "containerProjects must be a nonempty list.")
    mappings = tuple(parse_mapping(value) for value in projects)
    if len({mapping.project for mapping in mappings}) != len(mappings):
        raise QualificationError("profile-invalid", "Container projects must be declared once.")
    if len({mapping.repository for mapping in mappings}) != len(mappings):
        raise QualificationError("profile-invalid", "Container repositories must be declared once.")
    return Profile(builds_sha, count, mappings)


def discover_container_projects(root: Path) -> tuple[Mapping, ...]:
    """Return every ``src`` project that enables SDK container publication."""
    mappings = []
    for path in sorted((root / "src").rglob("*.csproj")):
        try:
            tree = ET.parse(path)  # nosec B314 -- repository-owned project file.
        except (OSError, ET.ParseError) as error:
            raise QualificationError("container-project-unreadable", f"{path} is not readable XML.") from error
        enabled = {(element.text or "").strip().lower() for element in tree.iter("EnableContainer")}
        if "true" not in enabled:
            continue
        repositories = {
            (element.text or "").strip()
            for element in tree.iter("ContainerRepository")
            if (element.text or "").strip()
        }
        relative = path.relative_to(root).as_posix()
        if len(repositories) != 1:
            raise QualificationError(
                "container-repository-ambiguous",
                f"{relative} enables containers without exactly one ContainerRepository.",
            )
        mappings.append(Mapping(relative, repositories.pop()))
    return tuple(mappings)


def load_release_manifest(data: bytes) -> tuple[dict, tuple[str, ...]]:
    """Load the authoritative ``id`` manifest and return it with its IDs."""
    document = load_strict_json(data, "release-manifest-invalid", "Release package manifest")
    packages = document.get("packages") if isinstance(document, dict) else None
    if not isinstance(packages, list) or not packages:
        raise QualificationError("release-manifest-invalid", "Release package manifest has no packages list.")
    identifiers = []
    for entry in packages:
        if not isinstance(entry, dict) or "packageId" in entry:
            raise QualificationError(
                "release-manifest-invalid",
                "Each package must be an object declaring id and no packageId.",
            )
        package_id = entry.get("id")
        if not isinstance(package_id, str) or not package_id or package_id != package_id.strip():
            raise QualificationError("release-manifest-invalid", "Each package must declare a canonical id.")
        identifiers.append(package_id)
    if len({identifier.lower() for identifier in identifiers}) != len(identifiers):
        raise QualificationError("release-manifest-invalid", "Package IDs must be unique ignoring case.")
    return document, tuple(identifiers)


def manifest_bytes(document: dict) -> bytes:
    """Serialize the validated manifest deterministically for qualification."""
    return (json.dumps(document, indent=2) + "\n").encode("utf-8")


def mapping_drift(declared, candidate) -> list[str]:
    """Classify every difference between the declared and a candidate mapping set."""
    reasons = []
    candidate_projects = [mapping.project for mapping in candidate]
    candidate_repositories = [mapping.repository for mapping in candidate]
    if len(set(candidate_projects)) != len(candidate_projects):
        reasons.append("container-project-duplicate")
    if len(set(candidate_repositories)) != len(candidate_repositories):
        reasons.append("container-repository-duplicate")
    declared_by_project = {mapping.project: mapping.repository for mapping in declared}
    candidate_by_project = {mapping.project: mapping.repository for mapping in candidate}
    declared_repositories = set(declared_by_project.values())
    for project, repository in declared_by_project.items():
        if project not in candidate_by_project:
            reasons.append(f"container-member-dropped:{project}|{repository}")
            continue
        observed = candidate_by_project[project]
        if observed != repository:
            kind = "remapped" if observed in declared_repositories else "renamed"
            reasons.append(f"container-member-{kind}:{project}|{repository}->{observed}")
    for project, repository in candidate_by_project.items():
        if project not in declared_by_project:
            reasons.append(f"container-member-added:{project}|{repository}")
    return sorted(reasons)


def package_drift(expected_count: int, reference_ids, candidate_ids) -> list[str]:
    """Refuse a candidate package inventory that differs from the declared count or IDs."""
    reasons = []
    if len(candidate_ids) != expected_count:
        reasons.append(f"package-count-mismatch:{len(candidate_ids)}!={expected_count}")
    if len({identifier.lower() for identifier in candidate_ids}) != len(candidate_ids):
        reasons.append("package-ids-not-unique")
    if tuple(candidate_ids) != tuple(reference_ids):
        reasons.append("package-inventory-drift")
    return reasons


# ------------------------------------------------------------------ revision bindings


@dataclass
class BuildsBinding:
    """The verified pinned Builds revision and its executed bytes."""

    sha: str
    gitlink: str
    checkout: str
    helpers: dict
    helper_bytes: dict
    references: dict
    reference_text: dict


def _gitlink(root: Path, *arguments: str) -> str | None:
    output = git(root, *arguments, "--", BUILDS_PATH.as_posix()).strip()
    if not output:
        return None
    metadata = output.split("\t", 1)[0].split()
    if len(output.splitlines()) != 1 or not metadata or metadata[0] != "160000":
        raise QualificationError("builds-gitlink-invalid", "references/Hexalith.Builds is not a single gitlink.")
    return metadata[2] if metadata[1] == "commit" else metadata[1]


def verify_builds_pin(root: Path, declared_sha: str) -> BuildsBinding:
    """Prove the gitlink, checkout and every executed helper equal the declared commit."""
    committed = _gitlink(root, "ls-tree", "HEAD")
    staged = _gitlink(root, "ls-files", "--stage")
    if committed is None or staged is None:
        raise QualificationError("builds-gitlink-missing", "references/Hexalith.Builds has no committed and staged gitlink.")
    if committed != declared_sha or staged != declared_sha:
        raise QualificationError(
            "builds-gitlink-mismatch",
            f"Gitlink HEAD {committed} / index {staged} differs from the declared {declared_sha}.",
        )
    builds_directory = root / BUILDS_PATH
    if not (builds_directory / ".git").exists():
        raise QualificationError("builds-checkout-missing", "references/Hexalith.Builds is not checked out.")
    checkout = git(builds_directory, "rev-parse", "HEAD").strip()
    if checkout != declared_sha:
        raise QualificationError(
            "builds-checkout-mismatch",
            f"The Builds checkout is at {checkout}, not the declared {declared_sha}.",
        )

    def pinned_bytes(relative: str, code: str) -> bytes:
        working = builds_directory / relative
        if not working.is_file():
            raise QualificationError(code.replace("hash-mismatch", "missing"), f"{relative} is missing from the checkout.")
        try:
            pinned = git(builds_directory, "cat-file", "blob", f"{declared_sha}:{relative}", binary=True)
        except QualificationError as error:
            raise QualificationError(code.replace("hash-mismatch", "missing"), f"{relative} is absent at the pin.") from error
        data = working.read_bytes()
        if data != pinned:
            raise QualificationError(code, f"{relative} bytes differ from the pinned commit.")
        return data

    helper_bytes = {
        name: pinned_bytes((HELPER_DIRECTORY / name).as_posix(), "builds-helper-hash-mismatch")
        for name in EXECUTED_HELPERS
    }
    reference_bytes = {
        relative: pinned_bytes(relative, "builds-reference-hash-mismatch") for relative in REFERENCE_FILES
    }
    return BuildsBinding(
        sha=declared_sha,
        gitlink=committed,
        checkout=checkout,
        helpers={name: sha256_bytes(data) for name, data in helper_bytes.items()},
        helper_bytes=helper_bytes,
        references={name: sha256_bytes(data) for name, data in reference_bytes.items()},
        reference_text={name: data.decode("utf-8") for name, data in reference_bytes.items()},
    )


def source_binding(root: Path) -> dict:
    """Return the Memories source revision and whether the working tree is dirty."""
    return {
        "repository": RELEASE_REPOSITORY,
        "sha": git(root, "rev-parse", "HEAD").strip(),
        "dirty": bool(git(root, "status", "--porcelain").strip()),
    }


@dataclass
class Context:
    """Everything a qualification run binds to, prepared before any scenario executes."""

    root: Path
    harness_sha256: str
    profile_sha256: str | None
    profile: Profile | None = None
    builds: BuildsBinding | None = None
    source: dict | None = None
    package_ids: tuple = ()
    release_document: dict | None = None
    release_manifest: bytes = b""
    discovered: tuple = ()
    prerequisite_failures: list = field(default_factory=list)
    profile_label: str = PROFILE_LABEL

    @property
    def ready(self) -> bool:
        """Return whether every prerequisite binding is proven."""
        return not self.prerequisite_failures

    def bindings(self) -> dict:
        """Return the identity block bound into every evidence file."""
        builds = {"repository": BUILDS_REPOSITORY, "sha": self.profile.builds_sha if self.profile else None}
        if self.builds is not None:
            builds.update(
                {
                    "verified": True,
                    "superprojectGitlink": self.builds.gitlink,
                    "checkout": self.builds.checkout,
                    "executedHelpers": dict(self.builds.helpers),
                    "readReferences": dict(self.builds.references),
                }
            )
        else:
            builds["verified"] = False
        return {
            "builds": builds,
            "source": dict(self.source) if self.source else None,
            "harness": {"path": HARNESS_LABEL, "sha256": self.harness_sha256},
            "profile": {"path": self.profile_label, "sha256": self.profile_sha256},
        }

    def release(self) -> dict:
        """Return the synthetic release identity the scenarios exercise."""
        return {
            "repository": RELEASE_REPOSITORY,
            "registry": REGISTRY,
            "version": QUALIFICATION_VERSION,
            "environment": ENVIRONMENT_NAME,
            "authorityGate": "disabled (ordinary protected-environment release posture)",
            "packageCount": len(self.package_ids),
            "packageIds": list(self.package_ids),
            "containerProjects": [mapping.line for mapping in self.profile.mappings] if self.profile else [],
        }


def profile_label(root: Path, profile_path: Path) -> str:
    """Return the profile path actually used, repository-relative when it is under *root*."""
    resolved, base = profile_path.resolve(), root.resolve()
    return resolved.relative_to(base).as_posix() if resolved.is_relative_to(base) else str(resolved)


def prepare_context(root: Path, profile_path: Path, harness_path: Path) -> Context:
    """Bind the run to its pinned Builds revision, source, harness and profile."""
    harness_sha256 = sha256_bytes(harness_path.read_bytes())
    label = profile_label(root, profile_path)
    try:
        profile_data = profile_path.read_bytes()
    except OSError:
        context = Context(root, harness_sha256, None, profile_label=label)
        context.prerequisite_failures.append("profile-missing: the qualification profile is unreadable.")
        return context
    context = Context(root, harness_sha256, sha256_bytes(profile_data), profile_label=label)
    try:
        context.source = source_binding(root)
    except QualificationError as error:
        context.prerequisite_failures.append(str(error))
    try:
        context.profile = load_profile(profile_data)
    except QualificationError as error:
        context.prerequisite_failures.append(str(error))
        return context
    try:
        context.builds = verify_builds_pin(root, context.profile.builds_sha)
    except QualificationError as error:
        context.prerequisite_failures.append(str(error))
    try:
        release_manifest = (root / MANIFEST_PATH).read_bytes()
        document, package_ids = load_release_manifest(release_manifest)
        context.package_ids = package_ids
        context.release_document = document
        context.release_manifest = release_manifest
    except OSError:
        context.prerequisite_failures.append("release-manifest-missing: tools/release-packages.json is unreadable.")
    except QualificationError as error:
        context.prerequisite_failures.append(str(error))
    try:
        context.discovered = discover_container_projects(root)
    except QualificationError as error:
        context.prerequisite_failures.append(str(error))
    if context.release_document is not None:
        drift = mapping_drift(context.profile.mappings, context.discovered) + package_drift(
            context.profile.expected_package_count, context.package_ids, context.package_ids
        )
        context.prerequisite_failures.extend(f"repository-set-drift: {reason}" for reason in drift)
    for mapping in context.profile.mappings:
        if not (root / mapping.project).is_file():
            context.prerequisite_failures.append(f"container-project-missing: {mapping.project}")
    return context


# ------------------------------------------------------------------- fake world (seams)


class World:
    """File-backed state shared by the fake network, registry, docker and dotnet seams."""

    def __init__(self, directory: Path) -> None:
        """Bind the world to its directory."""
        self.directory = directory
        self.config_path = directory / "world.json"
        self.calls_path = directory / "calls.jsonl"
        self.registry_directory = directory / "registry"
        self.containers_directory = directory / "containers"
        self.bin_directory = directory / "bin"
        self.seam_directory = directory / "seams"

    @classmethod
    def from_environment(cls) -> "World":
        """Return the world named by the seam environment."""
        value = os.environ.get(WORLD_VARIABLE, "")
        if not value:
            raise QualificationError("world-missing", f"{WORLD_VARIABLE} is not set.")
        return cls(Path(value))

    def config(self) -> dict:
        """Return the current fake-world configuration."""
        return json.loads(self.config_path.read_text(encoding="utf-8"))

    def write_config(self, config: dict) -> None:
        """Persist the fake-world configuration."""
        self.config_path.write_text(json.dumps(config, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    def update(self, **changes) -> None:
        """Merge *changes* into the fake-world configuration."""
        config = self.config()
        config.update(changes)
        self.write_config(config)

    def log(self, entry: dict) -> None:
        """Append one invocation record."""
        with self.calls_path.open("a", encoding="utf-8") as stream:
            stream.write(json.dumps(entry, sort_keys=True) + "\n")

    def calls(self) -> list:
        """Return every invocation record in order."""
        if not self.calls_path.exists():
            return []
        return [json.loads(line) for line in self.calls_path.read_text(encoding="utf-8").splitlines() if line]

    def _repository_directory(self, repository: str) -> Path:
        if REPOSITORY_NAME_PATTERN.fullmatch(repository) is None:
            raise QualificationError("fake-registry-repository-invalid", repository)
        return self.registry_directory / repository

    def put_object(self, repository: str, media_type: str, data: bytes) -> str:
        """Store one registry object and return its digest."""
        digest = sha256_bytes(data)
        objects = self._repository_directory(repository) / "objects"
        objects.mkdir(parents=True, exist_ok=True)
        (objects / digest).write_bytes(data)
        (objects / f"{digest}.type").write_text(media_type, encoding="utf-8")
        return f"sha256:{digest}"

    def get_object(self, repository: str, digest: str):
        """Return ``(media_type, bytes)`` for a stored object, or ``None``."""
        if not re.fullmatch(r"sha256:[0-9a-f]{64}", digest or ""):
            return None
        try:
            objects = self._repository_directory(repository) / "objects"
        except QualificationError:
            return None
        path = objects / digest.removeprefix("sha256:")
        if not path.is_file():
            return None
        return (objects / f"{path.name}.type").read_text(encoding="utf-8"), path.read_bytes()

    def put_tag(self, repository: str, tag: str, digest: str) -> None:
        """Point a tag at an index digest."""
        tags = self._repository_directory(repository) / "tags"
        tags.mkdir(parents=True, exist_ok=True)
        (tags / tag).write_text(digest, encoding="utf-8")

    def tag_digest(self, repository: str, tag: str) -> str | None:
        """Return the digest a tag resolves to, or ``None``."""
        try:
            path = self._repository_directory(repository) / "tags" / tag
        except QualificationError:
            return None
        return path.read_text(encoding="utf-8") if path.is_file() else None

    def tags(self, repository: str) -> list:
        """Return the tags stored for *repository*."""
        try:
            tags = self._repository_directory(repository) / "tags"
        except QualificationError:
            return []
        return sorted(path.name for path in tags.iterdir()) if tags.is_dir() else []


def _registry_repository(identity: str):
    registry, separator, repository = identity.partition("/")
    return (repository if separator and registry == REGISTRY else None)


def _network_disabled(*_arguments, **_keywords):
    raise urllib.error.URLError("network access is disabled during offline qualification")


class _FakeResponse:
    def __init__(self, status: int, headers: dict, body: bytes) -> None:
        self.status = status
        self.headers = Message()
        for name, value in headers.items():
            self.headers[name] = value
        self._body = io.BytesIO(body)

    def read(self, *arguments):
        return self._body.read(*arguments)

    def __enter__(self):
        return self

    def __exit__(self, *_arguments):
        return False


def _fake_registry_open(world: World):
    """Return a replacement for ``URL_OPENER.open`` backed by the fake registry."""

    def not_found(url: str, code: str):
        body = json.dumps({"errors": [{"code": code}]}).encode("utf-8")
        headers = Message()
        headers["Content-Type"] = "application/json"
        return urllib.error.HTTPError(url, 404, "Not Found", headers, io.BytesIO(body))

    def open_request(request, timeout=None):  # noqa: ARG001 -- mirrors the opener signature.
        url = request.full_url
        parsed = urllib.parse.urlsplit(url)
        if parsed.scheme != "https" or parsed.netloc != REGISTRY:
            raise urllib.error.URLError("the fake registry serves only the qualification registry")
        if not (request.get_header("Authorization") or "").startswith("Basic "):
            raise urllib.error.HTTPError(url, 401, "Unauthorized", Message(), io.BytesIO(b"{}"))
        match = re.fullmatch(r"/v2/(?P<repository>.+)/(?P<kind>manifests|blobs)/(?P<reference>[^/]+)", parsed.path)
        if match is None or request.get_method() != "GET":
            raise not_found(url, "UNSUPPORTED")
        repository = urllib.parse.unquote(match.group("repository"))
        reference = urllib.parse.unquote(match.group("reference"))
        digest = reference
        if match.group("kind") == "manifests" and not reference.startswith("sha256:"):
            digest = world.tag_digest(repository, reference)
            if digest is None:
                raise not_found(url, "MANIFEST_UNKNOWN")
        stored = world.get_object(repository, digest)
        if stored is None:
            raise not_found(url, "MANIFEST_UNKNOWN" if match.group("kind") == "manifests" else "BLOB_UNKNOWN")
        media_type, body = stored
        return _FakeResponse(200, {"Content-Type": media_type, "Docker-Content-Digest": digest}, body)

    return open_request


def _fake_probe_factory(world: World, module, overrides=None):
    """Return a fake ``destination_probe`` consulting the fake registry and NuGet state."""
    config = world.config()
    statuses = {**config.get("probeOverrides", {}), **(overrides or {})}
    existing_packages = {identifier.lower() for identifier in config.get("nugetExisting", [])}

    def factory(username, api_key):
        def probe(kind, identity, version):
            world.log({"tool": "destination-probe", "kind": kind, "identity": identity, "version": version})
            key = f"{kind}:{identity}"
            if key in statuses:
                return int(statuses[key])
            if kind == "nuget":
                return 200 if identity.lower() in existing_packages else 404
            if not username or not api_key:
                raise module.PreflightError("destination-probe-failure", "Registry destination probe is not configured.")
            repository = _registry_repository(identity)
            if repository is None:
                return 502
            return 200 if world.tag_digest(repository, version) else 404

        return probe

    return factory


def _fake_source_proof(repository, source_sha, source_branch, source_ci_workflow, token):  # noqa: ARG001
    return {
        "branch": source_branch,
        "ref": f"refs/heads/{source_branch}",
        "live_sha": source_sha,
        "ci_workflow": source_ci_workflow,
        "ci_run": {
            "id": SYNTHETIC_CI_RUN_ID,
            "head_sha": source_sha,
            "head_branch": source_branch,
            "event": "push",
            "status": "completed",
            "conclusion": "success",
        },
    }


def _fake_observations(world: World):
    def read(
        repository,
        package_ids,
        container_repositories,
        token,  # noqa: ARG001
        registry_username,  # noqa: ARG001
        registry_api_key,  # noqa: ARG001
        semantic_release_version=None,
        source_sha=None,
    ):
        existing = {identifier.lower() for identifier in world.config().get("nugetExisting", [])}
        observations = [
            {"kind": "github-release", "identity": repository, "versions": []},
            {"kind": "git-tag", "identity": repository, "versions": []},
        ]
        if semantic_release_version is not None:
            # The publish phase runs after Semantic Release created its own tag.
            observations.append(
                {
                    "kind": "semantic-release-tag",
                    "identity": f"{repository}#refs/tags/v{semantic_release_version}@{source_sha}",
                    "versions": [],
                }
            )
        observations.extend(
            {
                "kind": "nuget",
                "identity": package_id,
                "versions": [QUALIFICATION_VERSION] if package_id.lower() in existing else [],
            }
            for package_id in package_ids
        )
        for identity in sorted(repository.lower() for repository in container_repositories):
            name = _registry_repository(identity)
            observations.append(
                {"kind": "oci-registry", "identity": identity, "versions": world.tags(name) if name else []}
            )
        return observations

    return read


def _load_pinned_module(world: World, name: str):
    """Load an installed pinned helper module after re-proving its bytes."""
    config = world.config()
    release = Path(config["releaseDirectory"])
    for helper in ("oci_registry_validator.py", f"{name}.py"):
        data = (release / helper).read_bytes()
        if sha256_bytes(data) != config["helperHashes"][helper]:
            raise QualificationError("installed-helper-changed", f"{helper} differs from the bound pinned bytes.")
    if str(release) not in sys.path:
        sys.path.insert(0, str(release))
    if name != "oci_registry_validator":
        _load_pinned_module(world, "oci_registry_validator")
    if name in sys.modules:
        return sys.modules[name]
    specification = importlib.util.spec_from_file_location(name, release / f"{name}.py")
    module = importlib.util.module_from_spec(specification)
    sys.modules[name] = module
    specification.loader.exec_module(module)
    return module


def _run_pinned_main(module, path: Path, arguments) -> int:
    previous = sys.argv
    sys.argv = [str(path), *arguments]
    try:
        return int(module.main())
    except SystemExit as error:
        return error.code if isinstance(error.code, int) else 1
    finally:
        sys.argv = previous


def _argument_value(arguments, name: str):
    return arguments[arguments.index(name) + 1] if name in arguments and arguments.index(name) + 1 < len(arguments) else None


def _seam_preflight(world: World, arguments) -> int:
    module = _load_pinned_module(world, "publication_preflight")
    sys.modules["oci_registry_validator"].URL_OPENER.open = _network_disabled
    module.URL_OPENER.open = _network_disabled
    urllib.request.urlopen = _network_disabled
    module.prove_current_green_source = _fake_source_proof
    module.read_external_version_observations = _fake_observations(world)
    module.destination_probe = _fake_probe_factory(world, module)
    phase = _argument_value(arguments, "--phase")
    world.log({"tool": "preflight", "event": "start", "phase": phase})
    code = _run_pinned_main(module, Path(world.config()["releaseDirectory"]) / "publication_preflight.py", arguments)
    world.log({"tool": "preflight", "event": "exit", "phase": phase, "returncode": code})
    return code


def _seam_validator(world: World, arguments) -> int:
    module = _load_pinned_module(world, "oci_registry_validator")
    module.URL_OPENER.open = _fake_registry_open(world)
    urllib.request.urlopen = _network_disabled
    entry = {
        "tool": "validator",
        "image": _argument_value(arguments, "--image"),
        "evidence": _argument_value(arguments, "--evidence-directory"),
        "mode": "observe-created" if "--observe-created" in arguments else "created",
    }
    world.log({**entry, "event": "start"})
    code = _run_pinned_main(module, Path(world.config()["releaseDirectory"]) / "oci_registry_validator.py", arguments)
    world.log({**entry, "event": "exit", "returncode": code})
    return code


def _seam_absence(world: World, arguments) -> int:
    """Run the pinned container-absence check for recovery classification."""
    module = _load_pinned_module(world, "publication_preflight")
    module.URL_OPENER.open = _network_disabled
    repositories = [arguments[index + 1] for index, value in enumerate(arguments[:-1]) if value == "--container-repository"]
    overrides = {}
    for index, value in enumerate(arguments[:-1]):
        if value == "--probe-status":
            identity, _, status = arguments[index + 1].rpartition("=")
            overrides[f"container:{identity}"] = int(status)
    probe = _fake_probe_factory(world, module, overrides)(SYNTHETIC_REGISTRY_USERNAME, SYNTHETIC_REGISTRY_API_KEY)
    try:
        evidence = module.validate_container_absence(QUALIFICATION_VERSION, repositories, probe)
    except module.PreflightError as error:
        print(json.dumps({"result": "refused", "code": error.code}))
        return 1
    print(json.dumps({"result": evidence["result"], "containerRepositories": evidence["container_repositories"]}))
    return 0


def _parse_dotnet_publish(arguments) -> dict:
    if len(arguments) < 2 or arguments[0] != "publish":
        raise QualificationError("fake-dotnet-unexpected", "Only dotnet publish is supported.")
    project = arguments[1]
    if not Path(project).is_file():
        raise QualificationError("fake-dotnet-unexpected", f"Project {project} does not exist.")
    configuration = target = None
    properties = {}
    remaining = list(arguments[2:])
    while remaining:
        token = remaining.pop(0)
        if token == "--configuration" and configuration is None and remaining:
            configuration = remaining.pop(0)
        elif token == "/t:PublishContainer" and target is None:
            target = token
        elif token.startswith("-p:") and "=" in token:
            name, value = token[3:].split("=", 1)
            if name in properties:
                raise QualificationError("fake-dotnet-unexpected", f"Property {name} is repeated.")
            properties[name] = value
        else:
            raise QualificationError("fake-dotnet-unexpected", f"Unexpected argument {token!r}.")
    version = properties.get("Version")
    if (
        configuration != "Release"
        or target is None
        or set(properties) != set(EXPECTED_DOTNET_PROPERTIES)
        or properties["RuntimeIdentifiers"] != RUNTIME_IDENTIFIERS
        or properties["ContainerRuntimeIdentifiers"] != RUNTIME_IDENTIFIERS
        or properties["ContainerImageFormat"] != "OCI"
        or properties["UseHexalithProjectReferences"] != "false"
        or properties["ContainerRegistry"] != REGISTRY
        or properties["ContainerImageTag"] != version
        or properties["ContainerProvenanceReleaseVersion"] != version
        or SHA_PATTERN.fullmatch(properties["ContainerProvenanceSourceSha"]) is None
        or CREATED_PATTERN.fullmatch(properties["ContainerProvenanceCreated"]) is None
        or REPOSITORY_NAME_PATTERN.fullmatch(properties["ContainerRepository"]) is None
    ):
        raise QualificationError("fake-dotnet-unexpected", "The multi-RID OCI publication contract was not honoured.")
    return {
        "project": project,
        "repository": properties["ContainerRepository"],
        "version": version,
        "source_sha": properties["ContainerProvenanceSourceSha"],
        "created": properties["ContainerProvenanceCreated"],
    }


def _compact(value) -> bytes:
    return json.dumps(value, sort_keys=True, separators=(",", ":")).encode("utf-8")


def push_synthetic_image(world: World, request: dict, architectures) -> str:
    """Store a synthetic OCI index modelling the SDK multi-RID container output."""
    repository_url = f"https://github.com/{os.environ.get('GITHUB_REPOSITORY', '')}"
    repository = request["repository"]
    descriptors = []
    for architecture in architectures:
        labels = {
            "org.opencontainers.image.source": repository_url,
            "org.opencontainers.image.url": f"{repository_url}/releases/tag/v{request['version']}",
            "org.opencontainers.image.documentation": f"{repository_url}/blob/{request['source_sha']}/README.md",
            "org.opencontainers.image.revision": request["source_sha"],
            "org.opencontainers.image.version": request["version"],
            "org.opencontainers.image.created": request["created"],
            "org.opencontainers.artifact.created": request["created"],
            "org.opencontainers.image.title": repository,
            "com.hexalith.qualification.synthetic": "true",
            "com.hexalith.qualification.project": request["project"],
        }
        config = _compact(
            {
                "architecture": architecture,
                "os": "linux",
                "config": {"Labels": labels, "User": "1654", "ExposedPorts": {"8080/tcp": {}}},
                "rootfs": {"type": "layers", "diff_ids": []},
            }
        )
        config_digest = world.put_object(repository, OCI_CONFIG_MEDIA_TYPE, config)
        manifest = _compact(
            {
                "schemaVersion": 2,
                "mediaType": OCI_MANIFEST_MEDIA_TYPE,
                "config": {"mediaType": OCI_CONFIG_MEDIA_TYPE, "digest": config_digest, "size": len(config)},
                "layers": [],
            }
        )
        descriptors.append(
            {
                "mediaType": OCI_MANIFEST_MEDIA_TYPE,
                "digest": world.put_object(repository, OCI_MANIFEST_MEDIA_TYPE, manifest),
                "size": len(manifest),
                "platform": {"os": "linux", "architecture": architecture},
            }
        )
    index = _compact({"schemaVersion": 2, "mediaType": OCI_INDEX_MEDIA_TYPE, "manifests": descriptors})
    digest = world.put_object(repository, OCI_INDEX_MEDIA_TYPE, index)
    world.put_tag(repository, request["version"], digest)
    return digest


def _seam_dotnet(world: World, arguments) -> int:
    world.log({"tool": "dotnet", "args": list(arguments)})
    try:
        request = _parse_dotnet_publish(arguments)
    except QualificationError as error:
        print(f"[fake-dotnet] {error}", file=sys.stderr)
        return 97
    mode = world.config().get("publishFailures", {}).get(request["repository"])
    if mode == "publish-failure":
        print(f"[fake-dotnet] synthetic publish failure for {request['repository']}", file=sys.stderr)
        return 1
    architectures = ("amd64",) if mode == "invalid-image" else ("amd64", "arm64")
    digest = push_synthetic_image(world, request, architectures)
    print(f"[fake-dotnet] stored synthetic {REGISTRY}/{request['repository']}:{request['version']} {digest}")
    return 0


def _resolve_image(world: World, image: str):
    repository_reference, separator, digest = image.partition("@")
    repository = _registry_repository(repository_reference)
    if not separator or repository is None:
        return None
    stored = world.get_object(repository, digest)
    if stored is None or stored[0] != OCI_MANIFEST_MEDIA_TYPE:
        return None
    manifest = json.loads(stored[1])
    config = world.get_object(repository, manifest["config"]["digest"])
    if config is None:
        return None
    return repository, json.loads(config[1])


def _container_record(world: World, name: str):
    path = world.containers_directory / f"{name}.json"
    return json.loads(path.read_text(encoding="utf-8")) if path.is_file() else None


def _smoke_fails(world: World, repository: str) -> bool:
    return world.config().get("publishFailures", {}).get(repository) == "smoke-failure"


def _parse_docker_run(arguments):
    options = {"--env": []}
    flags = set()
    remaining = list(arguments)
    while remaining and remaining[0].startswith("--"):
        token = remaining.pop(0)
        if token in {"--rm", "--detach"}:
            flags.add(token)
        elif token in {"--platform", "--publish", "--name", "--entrypoint", "--env"} and remaining:
            value = remaining.pop(0)
            if token == "--env":
                options["--env"].append(value)
            else:
                options[token] = value
        else:
            return None
    if not remaining:
        return None
    return flags, options, remaining[0], remaining[1:]


def _seam_docker(world: World, arguments) -> int:  # noqa: C901 -- one strict branch per supported command.
    world.log({"tool": "docker", "args": list(arguments)})
    command = arguments[0] if arguments else ""
    if command == "login" and len(arguments) == 5 and arguments[2] == "--username" and arguments[4] == "--password-stdin":
        password = sys.stdin.read().strip()
        return 0 if arguments[1] == REGISTRY and arguments[3] and password else 1
    if list(arguments) == ["buildx", "inspect", "--bootstrap"]:
        print("Name: memories-qualification\nDriver: docker-container\nPlatforms: linux/amd64, linux/arm64")
        return 0
    if command == "pull" and len(arguments) == 4 and arguments[1] == "--platform":
        resolved = _resolve_image(world, arguments[3])
        if resolved is None or f"linux/{resolved[1]['architecture']}" != arguments[2]:
            print("Error response from daemon: manifest unknown", file=sys.stderr)
            return 1
        return 0
    if command == "run":
        parsed = _parse_docker_run(arguments[1:])
        if parsed is None:
            return _reject("docker", arguments)
        flags, options, image, command_arguments = parsed
        resolved = _resolve_image(world, image)
        if resolved is None or f"linux/{resolved[1]['architecture']}" != options.get("--platform"):
            print("Unable to find image", file=sys.stderr)
            return 125
        repository, config = resolved
        if flags == {"--rm"} and options.get("--entrypoint") == "/bin/uname" and command_arguments == ["-m"]:
            print("aarch64" if config["architecture"] == "arm64" else "x86_64")
            return 0
        if flags == {"--detach"} and options.get("--publish") == "127.0.0.1::8080" and options.get("--name") and not command_arguments:
            world.containers_directory.mkdir(parents=True, exist_ok=True)
            counter = world.directory / "next-port"
            port = int(counter.read_text(encoding="utf-8")) if counter.is_file() else 49152
            counter.write_text(str(port + 1), encoding="utf-8")
            record = {"name": options["--name"], "image": image, "repository": repository, "port": port}
            (world.containers_directory / f"{options['--name']}.json").write_text(json.dumps(record), encoding="utf-8")
            print(sha256_bytes(options["--name"].encode("utf-8")))
            return 0
        return _reject("docker", arguments)
    if command in {"port", "inspect", "logs", "rm"} and len(arguments) >= 2:
        record = _container_record(world, arguments[1] if command == "port" else arguments[-1])
        if record is None:
            print("Error: No such container", file=sys.stderr)
            return 1
        if command == "port" and arguments[1:] == [record["name"], "8080/tcp"]:
            print(f"127.0.0.1:{record['port']}")
            return 0
        if command == "inspect" and arguments[1:3] == ["--format", "{{.State.Status}}|{{.State.ExitCode}}"]:
            print("exited|139" if _smoke_fails(world, record["repository"]) else "running|0")
            return 0
        if command == "logs" and arguments[1:3] == ["--tail", "50"]:
            print("synthetic qualification container log")
            return 0
        if command == "rm" and arguments[1] == "--force":
            (world.containers_directory / f"{record['name']}.json").unlink()
            return 0
    return _reject("docker", arguments)


def _seam_curl(world: World, arguments) -> int:
    world.log({"tool": "curl", "args": list(arguments)})
    match = re.fullmatch(r"http://127\.0\.0\.1:(\d+)/alive", arguments[-1] if arguments else "")
    if match is None or "--write-out" not in arguments:
        return _reject("curl", arguments)
    port = int(match.group(1))
    records = (
        [json.loads(path.read_text(encoding="utf-8")) for path in world.containers_directory.glob("*.json")]
        if world.containers_directory.is_dir()
        else []
    )
    record = next((item for item in records if item["port"] == port), None)
    if record is None:
        print("000", end="")
        return 7
    print("503" if _smoke_fails(world, record["repository"]) else "200", end="")
    return 0


def _reject(tool: str, arguments) -> int:
    print(f"[fake-{tool}] unexpected invocation: {' '.join(arguments)}", file=sys.stderr)
    return 97


SEAMS = {
    "preflight": _seam_preflight,
    "validator": _seam_validator,
    "absence": _seam_absence,
    "dotnet": _seam_dotnet,
    "docker": _seam_docker,
    "curl": _seam_curl,
}


def run_seam(name: str, arguments) -> int:
    """Dispatch one internal fake-seam invocation."""
    handler = SEAMS.get(name)
    if handler is None:
        print(f"[qualification-seam] unknown seam {name!r}", file=sys.stderr)
        return 97
    try:
        return handler(World.from_environment(), list(arguments))
    except QualificationError as error:
        print(f"[qualification-seam] {error}", file=sys.stderr)
        return 96


# ------------------------------------------------------------------------- sandboxes


@dataclass
class Outcome:
    """One subprocess result."""

    returncode: int
    stdout: str
    stderr: str

    def refusal(self) -> str | None:
        """Return the first Builds refusal code, if any."""
        for pattern in (PREFLIGHT_REFUSAL, VALIDATOR_REFUSAL):
            match = pattern.search(self.stderr)
            if match:
                return match.group(1)
        return None

    def refusal_line(self) -> str | None:
        """Return the stderr line carrying the Builds refusal, if any."""
        for pattern in (PREFLIGHT_REFUSAL, VALIDATOR_REFUSAL):
            line = next((line.strip() for line in self.stderr.splitlines() if pattern.search(line)), None)
            if line:
                return _bounded(line)
        return None

    def summary(self) -> dict:
        """Return a support-safe summary keeping both ends of stderr and the refusal line."""
        return {
            "exitCode": self.returncode,
            "refusal": self.refusal(),
            "refusalLine": self.refusal_line(),
            "stderr": _bounded(self.stderr),
            "stderrTail": _bounded_tail(self.stderr),
        }


class Sandbox:
    """One isolated workspace with installed pinned helpers and its own fake world."""

    def __init__(self, context: Context, scratch: Path, name: str, failures: dict | None = None) -> None:
        """Install the pinned helpers and fakes for one scenario."""
        self.context = context
        self.root = scratch / name
        self.root.mkdir(parents=True)
        self.workspace = self.root / "workspace"
        self.release_directory = self.workspace / ".hexalith" / "release"
        self.evidence_root = self.workspace / ".hexalith" / "release-evidence" / QUALIFICATION_VERSION
        self.world = World(self.root / "fakes")
        for directory in (self.release_directory, self.world.bin_directory, self.world.seam_directory,
                          self.world.registry_directory, self.root / "home", self.root / "tmp"):
            directory.mkdir(parents=True, exist_ok=True)
        # Mirror action.yml: copy exactly the five approved helper files and mark them executable.
        for name_, data in context.builds.helper_bytes.items():
            target = self.release_directory / name_
            target.write_bytes(data)
            target.chmod(0o755)
        for mapping in context.profile.mappings:
            target = self.workspace / mapping.project
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(context.root / mapping.project, target)
        manifest = self.workspace / MANIFEST_PATH
        manifest.parent.mkdir(parents=True, exist_ok=True)
        manifest.write_bytes(context.release_manifest)
        self.world.write_config(
            {
                "releaseDirectory": str(self.release_directory),
                "helperHashes": dict(context.builds.helpers),
                "publishFailures": dict(failures or {}),
                "probeOverrides": {},
                "nugetExisting": [],
            }
        )
        harness = Path(__file__).resolve()
        for directory, name_, seam in (
            (self.world.bin_directory, "docker", "docker"),
            (self.world.bin_directory, "curl", "curl"),
            (self.world.bin_directory, "dotnet", "dotnet"),
            (self.world.seam_directory, "publication-preflight", "preflight"),
            (self.world.seam_directory, "oci-validator", "validator"),
            (self.world.seam_directory, "destination-absence", "absence"),
        ):
            command = " ".join(shlex.quote(part) for part in (sys.executable, "-I", "-B", str(harness), "--seam", seam))
            shim = directory / name_
            shim.write_text(f'#!/usr/bin/env bash\nexec {command} "$@"\n', encoding="utf-8")
            shim.chmod(0o755)
        path = self.environment()["PATH"]
        for tool in ("docker", "curl", "dotnet"):
            if shutil.which(tool, path=path) != str(self.world.bin_directory / tool):
                raise QualificationError("fake-seam-shadowed", f"The fake {tool} is not first on PATH.")

    def environment(self, mappings=None) -> dict:
        """Return a minimal synthetic environment that cannot reach real services."""
        mappings = self.context.profile.mappings if mappings is None else mappings
        source_sha = self.context.source["sha"]
        environment = {
            "PATH": f"{self.world.bin_directory}{os.pathsep}{os.environ.get('PATH', '/usr/bin:/bin')}",
            "HOME": str(self.root / "home"),
            "TMPDIR": str(self.root / "tmp"),
            "LANG": "C.UTF-8",
            "LC_ALL": "C.UTF-8",
            "PYTHONDONTWRITEBYTECODE": "1",
            "DOCKER_CONFIG": str(self.root / "home" / ".docker"),
            "DOCKER_HOST": "unix:///nonexistent/memories-qualification.sock",
            WORLD_VARIABLE: str(self.world.directory),
            "GITHUB_REPOSITORY": RELEASE_REPOSITORY,
            "GITHUB_SHA": source_sha,
            "GITHUB_WORKFLOW_SHA": source_sha,
            **SYNTHETIC_RUN_ENVIRONMENT,
            "HEXALITH_ZOT_REGISTRY": REGISTRY,
            "HEXALITH_ZOT_USERNAME": SYNTHETIC_REGISTRY_USERNAME,
            "HEXALITH_ZOT_API_KEY": SYNTHETIC_REGISTRY_API_KEY,
            "HEXALITH_CONTAINER_PROJECTS": "\n".join(mapping.line for mapping in mappings),
            "HEXALITH_OCI_VALIDATOR": str(self.world.seam_directory / "oci-validator"),
            "HEXALITH_PUBLICATION_PREFLIGHT": str(self.world.seam_directory / "publication-preflight"),
            "HEXALITH_BUILDS_EXECUTION_SHA": self.context.profile.builds_sha,
            "HEXALITH_RELEASE_ENVIRONMENT": ENVIRONMENT_NAME,
            "HEXALITH_RELEASE_SOURCE_BRANCH": SOURCE_BRANCH,
            "HEXALITH_RELEASE_SOURCE_CI_WORKFLOW": SOURCE_CI_WORKFLOW,
            "HEXALITH_RELEASE_PACKAGE_MANIFEST": MANIFEST_PATH.as_posix(),
            "HEXALITH_RELEASE_EXPECTED_PACKAGE_COUNT": str(self.context.profile.expected_package_count),
            "HEXALITH_RELEASE_REQUIRE_AUTHORITY": "false",
            "HEXALITH_CONTAINER_SMOKE_TIMEOUT_SECONDS": "5",
            "HEXALITH_CONTAINER_SMOKE_INTERVAL_SECONDS": "0.1",
        }
        for variable in ("HTTP_PROXY", "HTTPS_PROXY", "http_proxy", "https_proxy", "ALL_PROXY", "all_proxy"):
            environment[variable] = UNREACHABLE_PROXY
        return environment

    def run(self, command, mappings=None) -> Outcome:
        """Run one command inside the workspace."""
        try:
            completed = subprocess.run(  # nosec B603 -- installed pinned helpers and generated shims only.
                command,
                cwd=self.workspace,
                env=self.environment(mappings),
                capture_output=True,
                text=True,
                stdin=subprocess.DEVNULL,
                check=False,
                timeout=SUBPROCESS_TIMEOUT_SECONDS,
            )
        except subprocess.TimeoutExpired:
            return Outcome(-1, "", f"timeout after {SUBPROCESS_TIMEOUT_SECONDS}s: {' '.join(map(str, command))}")
        return Outcome(completed.returncode, completed.stdout, completed.stderr)

    def preflight(self, phase, repositories=None, manifest=None, evidence=None) -> Outcome:
        """Run the real pinned preflight with the publisher's exact argument shape."""
        repositories = [mapping.repository for mapping in self.context.profile.mappings] if repositories is None else repositories
        arguments = [
            str(self.world.seam_directory / "publication-preflight"),
            "--repository", RELEASE_REPOSITORY,
            "--version", QUALIFICATION_VERSION,
            "--source-sha", self.context.source["sha"],
            "--source-branch", SOURCE_BRANCH,
            "--source-ci-workflow", SOURCE_CI_WORKFLOW,
            "--builds-execution-sha", self.context.profile.builds_sha,
            "--environment-name", ENVIRONMENT_NAME,
            "--package-manifest", manifest or MANIFEST_PATH.as_posix(),
            "--expected-package-count", str(self.context.profile.expected_package_count),
            "--contract-directory", ".hexalith/release",
            "--evidence-directory", evidence or f".hexalith/release-evidence/{QUALIFICATION_VERSION}/preflight",
            "--phase", phase,
        ]
        for repository in repositories:
            arguments.extend(("--container-repository", f"{REGISTRY}/{repository}"))
        return self.run(arguments)

    def publisher(self, mappings=None) -> Outcome:
        """Run the real installed pinned publisher exactly as semantic-release would."""
        return self.run(["./.hexalith/release/publish-containers.sh", QUALIFICATION_VERSION], mappings)

    def observe(self, repository: str) -> Outcome:
        """Re-read one published member with the pinned recovery-reader mode."""
        return self.run(
            [
                str(self.world.seam_directory / "oci-validator"),
                "--image", f"{REGISTRY}/{repository}:{QUALIFICATION_VERSION}",
                "--repository", RELEASE_REPOSITORY,
                "--source-sha", self.context.source["sha"],
                "--version", QUALIFICATION_VERSION,
                "--observe-created",
                "--evidence-directory", f".hexalith/recovery-evidence/{repository}",
            ]
        )

    def absence(self, repositories, statuses=None) -> tuple:
        """Run the pinned container-absence proof and return ``(outcome, verdict)``."""
        arguments = [str(self.world.seam_directory / "destination-absence")]
        for repository in repositories:
            arguments.extend(("--container-repository", f"{REGISTRY}/{repository}"))
        for repository, status in (statuses or {}).items():
            arguments.extend(("--probe-status", f"{REGISTRY}/{repository}={status}"))
        outcome = self.run(arguments)
        try:
            verdict = json.loads(outcome.stdout.strip().splitlines()[-1])
        except (IndexError, ValueError):
            verdict = {"result": "unreadable"}
        return outcome, verdict

    def mark(self) -> int:
        """Return a position in the invocation log."""
        return len(self.world.calls())

    def calls_since(self, marker: int) -> list:
        """Return invocations recorded after *marker*."""
        return self.world.calls()[marker:]


# --------------------------------------------------------------------- evidence analysis


@dataclass
class MemberInspection:
    """The caller's re-verification of one member's retained Builds evidence."""

    problems: list
    index_digest: str | None = None
    created: str | None = None
    children: dict = field(default_factory=dict)


def inspect_member(directory: Path, image: str, source_sha: str, version: str) -> MemberInspection:
    """Re-verify one member's validation and smoke evidence from retained bytes."""
    if not directory.is_dir():
        return MemberInspection(["evidence-directory-missing"])
    problems = [f"missing:{name}" for name in MEMBER_FILES if not (directory / name).is_file()]
    inspection = MemberInspection(problems)
    validation, problem = _read_member_json(directory / "oci-validation.json")
    if validation is None:
        problems.append(f"validation-{problem}")
    else:
        _inspect_validation(directory, validation, image, source_sha, version, inspection)
    smoke, problem = _read_member_json(directory / "smoke-results.json")
    if smoke is None:
        problems.append(f"smoke-{problem}")
    else:
        if smoke.get("result") != "pass":
            problems.append(f"smoke-not-pass:{smoke.get('result')}")
        outcomes = {entry.get("platform"): entry for entry in smoke.get("platforms", []) if isinstance(entry, dict)}
        if set(outcomes) != set(PLATFORMS) or len(smoke.get("platforms", [])) != len(PLATFORMS):
            problems.append("smoke-platform-set-mismatch")
        for platform, entry in outcomes.items():
            if entry.get("outcome") != "pass" or entry.get("cleanup") != "pass":
                problems.append(f"smoke-platform-not-pass:{platform}")
            if inspection.children and inspection.children.get(platform) != entry.get("digest"):
                problems.append(f"smoke-digest-mismatch:{platform}")
    problems.extend(_smoke_hash_problems(directory))
    return inspection


def _inspect_validation(directory, validation, image, source_sha, version, inspection) -> None:
    problems = inspection.problems
    if validation.get("result") != "pass":
        problems.append("validation-not-pass")
    if validation.get("image") != image:
        problems.append("validation-image-mismatch")
    if validation.get("platforms") != list(PLATFORMS):
        problems.append("validation-platform-mismatch")
    index = directory / "index.raw"
    if index.is_file() and sha256_bytes(index.read_bytes()) != validation.get("raw_index_sha256"):
        problems.append("index-raw-hash-mismatch")
    inspection.index_digest = validation.get("index_digest")
    created = set()
    children = validation.get("children") if isinstance(validation.get("children"), list) else []
    if len(children) != len(PLATFORMS):
        problems.append("validation-child-count-mismatch")
    for child in children:
        platform = child.get("platform")
        inspection.children[platform] = child.get("digest")
        for kind in ("manifest", "config"):
            raw = directory / str(child.get(f"{kind}_raw_file"))
            if not raw.is_file() or sha256_bytes(raw.read_bytes()) != child.get(f"{kind}_raw_sha256"):
                problems.append(f"{kind}-raw-hash-mismatch:{platform}")
        labels = child.get("provenance_labels") or {}
        if labels.get("org.opencontainers.image.revision") != source_sha:
            problems.append(f"revision-label-mismatch:{platform}")
        if labels.get("org.opencontainers.image.version") != version:
            problems.append(f"version-label-mismatch:{platform}")
        if labels.get("org.opencontainers.image.created") != labels.get("org.opencontainers.artifact.created"):
            problems.append(f"created-label-mismatch:{platform}")
        created.add(labels.get("org.opencontainers.image.created"))
    if len(created) != 1:
        problems.append("created-instant-not-unique")
    inspection.created = next(iter(created)) if len(created) == 1 else None


def _smoke_hash_problems(directory: Path) -> list:
    hashes = directory / "smoke-sha256.txt"
    if not hashes.is_file():
        return []
    listed = {}
    for line in hashes.read_text(encoding="utf-8").splitlines():
        digest, _, name = line.partition("  ")
        listed[name] = digest
    problems = []
    if set(listed) != set(SMOKE_HASHED_FILES):
        problems.append("smoke-hash-list-incomplete")
    for name, digest in listed.items():
        path = directory / name
        if not path.is_file() or sha256_bytes(path.read_bytes()) != digest:
            problems.append(f"smoke-hash-mismatch:{name}")
    return problems


def classify_members(evidence_root: Path, mappings, source_sha: str) -> list:
    """Derive each member's state from per-image evidence alone."""
    members = []
    for mapping in mappings:
        directory = evidence_root / mapping.repository
        image = f"{REGISTRY}/{mapping.repository}:{QUALIFICATION_VERSION}"
        inspection = inspect_member(directory, image, source_sha, QUALIFICATION_VERSION)
        if inspection.problems == ["evidence-directory-missing"]:
            state = "not-attempted"
        elif not inspection.problems:
            state = "published"
        else:
            state = "ambiguous"
        members.append(
            {
                "repository": mapping.repository,
                "project": mapping.project,
                "state": state,
                "indexDigest": inspection.index_digest if state == "published" else None,
                "created": inspection.created if state == "published" else None,
                "children": inspection.children if state == "published" else {},
                "problems": [] if state != "ambiguous" else inspection.problems,
            }
        )
    return members


def derive_partial_result(members) -> dict:
    """Return the caller-owned set result Builds does not write; ambiguity is never success."""
    by_state = {state: [member["repository"] for member in members if member["state"] == state]
                for state in ("published", "ambiguous", "not-attempted")}
    complete = bool(members) and len(by_state["published"]) == len(members)
    return {
        "schema": PARTIAL_RESULT_SCHEMA,
        "result": "pass" if complete else "incomplete",
        "success": complete,
        "published": by_state["published"],
        "ambiguous": by_state["ambiguous"],
        "notAttempted": by_state["not-attempted"],
        "rule": "Only fully re-verified members count as published; an ambiguous member is never success.",
    }


def build_hash_manifest(root: Path) -> list:
    """Return sorted ``(relative path, sha256)`` entries for every file below *root*."""
    if not root.is_dir():
        return []
    return sorted(
        (path.relative_to(root).as_posix(), sha256_bytes(path.read_bytes()))
        for path in root.rglob("*")
        if path.is_file()
    )


def render_hash_manifest(entries) -> str:
    """Render entries in ``sha256sum`` format."""
    return "".join(f"{digest}  {path}\n" for path, digest in entries)


def verify_hash_manifest(root: Path, entries) -> list:
    """Return every reason the caller upload must refuse this evidence set."""
    problems = [] if entries else ["manifest-empty"]
    listed = {path for path, _ in entries}
    for path, digest in entries:
        file = root / path
        if not file.is_file():
            problems.append(f"missing:{path}")
        elif sha256_bytes(file.read_bytes()) != digest:
            problems.append(f"hash-mismatch:{path}")
    problems.extend(f"unlisted:{path}" for path, _ in build_hash_manifest(root) if path not in listed)
    return problems


def workflow_steps(text: str, name: str) -> list:
    """Return the scalar keys of every workflow step named *name*, in file order."""
    lines = text.splitlines()
    steps = []
    for index, line in enumerate(lines):
        if line.strip() != f"- name: {name}":
            continue
        indent = len(line) - len(line.lstrip())
        values = {"line": index + 1}
        for following in lines[index + 1:]:
            if following.strip() and len(following) - len(following.lstrip()) <= indent:
                break
            match = re.fullmatch(r"\s*([A-Za-z0-9_-]+):\s*(.*?)\s*", following)
            if match and match.group(2) and not match.group(2).startswith("#"):
                values[match.group(1)] = match.group(2).split(" #", 1)[0].strip()
        steps.append(values)
    return steps


# ------------------------------------------------------------------------ checkpoints


@dataclass
class CheckpointResult:
    """The checks, observations, findings and exceptions of one checkpoint."""

    checkpoint: str
    checks: list = field(default_factory=list)
    observations: dict = field(default_factory=dict)
    findings: list = field(default_factory=list)
    exceptions: list = field(default_factory=list)

    def check(self, name: str, passed: bool, detail=None) -> bool:
        """Record one check and return whether it passed."""
        entry = {"name": name, "result": "pass" if passed else "fail"}
        if detail is not None:
            entry["detail"] = detail
        self.checks.append(entry)
        return bool(passed)

    @property
    def passed(self) -> bool:
        """Return whether at least one check ran and every check passed."""
        return bool(self.checks) and all(entry["result"] == "pass" for entry in self.checks)

    def exception(self, code: str, premise: bool, evidence) -> None:
        """Record an approved caller-owned exception after proving its premise."""
        self.check(f"exception-premise:{code}", premise, evidence)
        self.exceptions.append({"code": code, **EXCEPTION_CATALOG[code], "premiseObserved": bool(premise), "evidence": evidence})


def _expect_refusal(result: CheckpointResult, name: str, outcome: Outcome, code: str) -> None:
    result.check(name, outcome.returncode != 0 and outcome.refusal() == code, {**outcome.summary(), "expected": code})


def _calls(sandbox, marker=0, tool=None, event=None):
    return [call for call in sandbox.calls_since(marker)
            if (tool is None or call.get("tool") == tool) and (event is None or call.get("event") == event)]


def run_complete_set_dry_run(context: Context, scratch: Path, state: dict, result: CheckpointResult) -> None:
    """Drive all three phases and the real publisher over the complete declared set."""
    sandbox = Sandbox(context, scratch, "complete-set-dry-run")
    state["dry-run"] = sandbox
    mappings = context.profile.mappings
    for phase in ("verify", "publish"):
        outcome = sandbox.preflight(phase)
        result.check(f"preflight-{phase}-passes", outcome.returncode == 0, outcome.summary())
    marker = sandbox.mark()
    publisher = sandbox.publisher()
    result.check("publisher-completes-the-declared-set", publisher.returncode == 0, publisher.summary())
    calls = sandbox.calls_since(marker)

    def first(predicate):
        return next((index for index, call in enumerate(calls) if predicate(call)), None)

    container_exit = first(lambda call: call.get("tool") == "preflight" and call.get("phase") == "container"
                           and call.get("event") == "exit" and call.get("returncode") == 0)
    logins = [index for index, call in enumerate(calls) if call.get("tool") == "docker" and call["args"][:1] == ["login"]]
    dotnet = [call for call in calls if call.get("tool") == "dotnet"]
    first_dotnet = first(lambda call: call.get("tool") == "dotnet")
    result.check(
        "container-preflight-precedes-login-and-every-write",
        container_exit is not None and len(logins) == 1 and first_dotnet is not None and container_exit < logins[0] < first_dotnet,
        {"containerPreflightExit": container_exit, "logins": logins, "firstDotnet": first_dotnet},
    )
    published = [_sdk_publication(sandbox, call) for call in dotnet]
    result.check(
        "one-sdk-publication-per-member-in-declared-order",
        published == [(mapping.project, mapping.repository) for mapping in mappings],
        published,
    )
    validators = _calls(sandbox, marker, "validator", "exit")
    smokes = [call for call in calls if call.get("tool") == "docker" and call["args"] == ["buildx", "inspect", "--bootstrap"]]
    result.check(
        "one-validate-and-smoke-run-per-member",
        len(validators) == len(mappings) and all(call["returncode"] == 0 and call["mode"] == "created" for call in validators)
        and len(smokes) == len(mappings),
        {"validatorRuns": len(validators), "smokeRuns": len(smokes)},
    )
    members = classify_members(sandbox.evidence_root, mappings, context.source["sha"])
    result.check("every-member-published-and-re-verified", all(member["state"] == "published" for member in members),
                 [{key: member[key] for key in ("repository", "state", "problems")} for member in members])
    created = {member["created"] for member in members}
    result.check("one-created-instant-for-the-set", len(created) == 1 and None not in created, sorted(map(str, created)))
    result.check("derived-set-result-pass", derive_partial_result(members)["result"] == "pass", derive_partial_result(members))
    result.observations = {
        "members": [{key: member[key] for key in ("repository", "project", "state", "indexDigest", "created", "children")}
                    for member in members],
        "registryLogins": len(logins),
        "sdkPublications": len(dotnet),
    }
    publisher_text = context.builds.helper_bytes["publish-containers.sh"].decode("utf-8")
    result.exception(
        "no-dry-run-mode",
        re.search(r"dry[-_ ]?run", publisher_text, re.IGNORECASE) is None and len(logins) == 1 and len(dotnet) == len(mappings),
        "publish-containers.sh declares no dry-run switch; the run logged in once and invoked one SDK publication per member.",
    )
    result.findings.extend(_static_findings(context, ("provenance-labels-not-emitted",
                                                       "prepare-domain-release-cannot-publish-containers")))
    for finding in result.findings:
        if finding["code"] == "prepare-domain-release-cannot-publish-containers":
            result.check("finding-premise:prepare-domain-release-cannot-publish-containers", finding["premiseObserved"],
                         finding["evidence"])


def _property(arguments, name: str):
    prefix = f"-p:{name}="
    return next((value[len(prefix):] for value in arguments if value.startswith(prefix)), None)


def _sdk_publication(sandbox: Sandbox, call: dict) -> tuple:
    """Return the workspace-relative project and repository of one fake SDK publication."""
    project = Path(call["args"][1]).resolve()
    workspace = sandbox.workspace.resolve()
    relative = project.relative_to(workspace).as_posix() if project.is_relative_to(workspace) else str(project)
    return relative, _property(call["args"], "ContainerRepository")


def _static_findings(context: Context, codes) -> list:
    findings = []
    for code in codes:
        finding = STATIC_FINDINGS[code](context)
        if finding is not None:
            findings.append({"code": code, **finding})
    return findings


def _finding_provenance_labels(context: Context):
    text = "\n".join(
        (context.root / name).read_text(encoding="utf-8")
        for name in BUILD_CUSTOMIZATION_FILES
        if (context.root / name).is_file()
    )
    missing = [label for label in REQUIRED_PROVENANCE_LABELS if label not in text]
    if "ContainerProvenance" in text or not missing:
        return None
    return {
        "owner": "30-4-five-image-publication-contract",
        "summary": "Memories imports no Builds ContainerProvenance targets and its own container labels omit labels the "
                   "pinned validator requires, so real SDK images would fail validation. The synthetic images model "
                   "the required labels; real images and health belong to 30.4.",
        "premiseObserved": True,
        "evidence": {"files": list(BUILD_CUSTOMIZATION_FILES), "missingLabels": missing},
    }


def _finding_prepare_domain_release(context: Context):
    text = context.builds.reference_text[PREPARE_DOMAIN_RELEASE_ACTION]
    inputs = text.split("\noutputs:", 1)[0]
    premise = "container" not in inputs.lower() and "publish-containers" not in text
    return {
        "owner": "30-3-guarded-release-dispatch-and-shared-caller-adoption; 30-4-five-image-publication-contract",
        "summary": "Pinned Github/prepare-domain-release declares no container input and never installs or invokes "
                   "publish-containers, so container publication stays a caller-owned semantic-release hook.",
        "premiseObserved": premise,
        "evidence": {"path": f"{BUILDS_PATH}/{PREPARE_DOMAIN_RELEASE_ACTION}",
                     "sha256": context.builds.references[PREPARE_DOMAIN_RELEASE_ACTION]},
    }


def _finding_recovery_workflow(context: Context):
    path = context.root / RECOVERY_WORKFLOW_PATH
    if not path.is_file():
        return None
    lines = path.read_text(encoding="utf-8").splitlines()
    images = [index + 1 for index, line in enumerate(lines) if re.search(r"-notin @\(2, 4\)", line)]
    packages = [index + 1 for index, line in enumerate(lines) if re.search(r"packages\)\.Count -ne 9\b", line)]
    if not images and not packages:
        return None
    return {
        "owner": "30-5-partial-release-recovery",
        "summary": "recover-partial-release.yml still accepts only 2 or 4 images and 9 packages, not the declared "
                   "five-image/ten-package set. This story does not change it.",
        "premiseObserved": True,
        "evidence": {"path": RECOVERY_WORKFLOW_PATH.as_posix(), "imageCountLines": images, "packageCountLines": packages},
    }


def _finding_evidence_retention(context: Context):
    steps = workflow_steps(context.builds.reference_text[DOMAIN_RELEASE_WORKFLOW], UPLOAD_STEP_NAME)
    retained = [{"line": step["line"], "retentionDays": step["retention-days"]} for step in steps if "retention-days" in step]
    if not retained:
        return None
    return {
        "owner": "30-4-five-image-publication-contract; 30-5-partial-release-recovery",
        "summary": "The reference release-evidence upload expires after "
                   f"{', '.join(sorted({item['retentionDays'] for item in retained}))} days, which bounds recovery from "
                   "immutable evidence. The caller must retain or archive the evidence for the whole recovery window.",
        "premiseObserved": True,
        "evidence": {"path": f"{BUILDS_PATH}/{DOMAIN_RELEASE_WORKFLOW}",
                     "sha256": context.builds.references[DOMAIN_RELEASE_WORKFLOW], "steps": retained},
    }


STATIC_FINDINGS = {
    "provenance-labels-not-emitted": _finding_provenance_labels,
    "prepare-domain-release-cannot-publish-containers": _finding_prepare_domain_release,
    "recovery-workflow-superseded-set": _finding_recovery_workflow,
    "evidence-retention-limited": _finding_evidence_retention,
}


SYNTHETIC_ADDED_MEMBER = Mapping(
    "src/Hexalith.Memories.QualificationSyntheticAdded/Hexalith.Memories.QualificationSyntheticAdded.csproj",
    "memories-qualification-synthetic-added",
)


@dataclass(frozen=True)
class DriftVariant:
    """One release-set drift and the verdicts it must produce."""

    name: str
    kind: str
    mappings: tuple
    packages: tuple
    caller_reason: str
    fresh: str
    between: str


def drift_variants(context: Context) -> tuple:
    """Return the dropped, added, renamed, remapped and count-drift variants."""
    declared = context.profile.mappings
    packages = tuple(context.release_document["packages"])
    swapped = list(declared)
    swapped[0] = Mapping(declared[0].project, declared[1].repository)
    swapped[1] = Mapping(declared[1].project, declared[0].repository)
    renamed = list(declared)
    renamed[1] = Mapping(declared[1].project, f"{declared[1].repository}-renamed")
    extra_package = {"id": "Hexalith.Memories.Qualification.Extra",
                     "project": "src/Hexalith.Memories.Qualification.Extra/Hexalith.Memories.Qualification.Extra.csproj"}
    # A synthetic member that no real project declares, so the added variant can never read as a duplicate.
    added = SYNTHETIC_ADDED_MEMBER
    if added.project in {m.project for m in declared} or added.repository in {m.repository for m in declared}:
        raise QualificationError("drift-variant-collides", "The synthetic added member is already declared.")
    return (
        DriftVariant("container-dropped", "container", declared[:-1], packages, "container-member-dropped", "accepted",
                     "publication-identity-changed"),
        DriftVariant("container-added", "container",
                     (*declared, added),
                     packages, "container-member-added", "accepted", "publication-identity-changed"),
        DriftVariant("container-renamed", "container", tuple(renamed), packages, "container-member-renamed", "accepted",
                     "publication-identity-changed"),
        DriftVariant("container-remapped", "container", tuple(swapped), packages, "container-member-remapped", "accepted",
                     "not-detected"),
        DriftVariant("package-count-low", "package", declared, packages[:-1], "package-count-mismatch",
                     "package-inventory-mismatch", "package-inventory-mismatch"),
        DriftVariant("package-count-high", "package", declared, (*packages, extra_package), "package-count-mismatch",
                     "package-inventory-mismatch", "package-inventory-mismatch"),
        DriftVariant("package-renamed", "package", declared,
                     (*packages[:-1], {**packages[-1], "id": f"{packages[-1]['id']}Renamed"}), "package-inventory-drift",
                     "accepted", "publication-identity-changed"),
    )


def _verdict(outcome: Outcome) -> str:
    return "accepted" if outcome.returncode == 0 else (outcome.refusal() or f"exit-{outcome.returncode}")


def _drift_between(sandbox: Sandbox, variant: DriftVariant, manifest: str | None) -> dict:
    repositories = [mapping.repository for mapping in variant.mappings]
    if variant.name != "container-remapped":
        publish = sandbox.preflight("publish", repositories=repositories, manifest=manifest)
        return {"verdict": "not-detected" if publish.returncode == 0 else _verdict(publish), "publish": publish.summary()}
    publish = sandbox.preflight("publish", repositories=repositories)
    first = variant.mappings[0]
    # Stop after the first SDK write: reaching it proves Builds accepted the remapped set.
    sandbox.world.update(publishFailures={first.repository: "publish-failure"})
    marker = sandbox.mark()
    publisher = sandbox.publisher(variant.mappings)
    container_passed = any(call.get("phase") == "container" and call.get("returncode") == 0
                           for call in _calls(sandbox, marker, "preflight", "exit"))
    dotnet = [_sdk_publication(sandbox, call) for call in _calls(sandbox, marker, "dotnet")]
    reached = bool(dotnet) and dotnet[0] == (first.project, first.repository)
    return {
        "verdict": "not-detected" if publish.returncode == 0 and container_passed and reached else _verdict(publisher),
        "publish": publish.summary(),
        "publisher": publisher.summary(),
        "firstSdkPublication": {"project": dotnet[0][0], "repository": dotnet[0][1]} if dotnet else None,
    }


def run_set_drift(context: Context, scratch: Path, state: dict, result: CheckpointResult) -> None:
    """Refuse every drift caller-side and record how Builds treats it between phases."""
    declared = context.profile.mappings
    repository_reasons = mapping_drift(declared, context.discovered) + package_drift(
        context.profile.expected_package_count, context.package_ids, context.package_ids
    )
    result.check("repository-set-equals-declared-profile", not repository_reasons,
                 {"discovered": [mapping.line for mapping in context.discovered], "reasons": repository_reasons})
    variants = []
    for variant in drift_variants(context):
        caller = mapping_drift(declared, variant.mappings) + package_drift(
            context.profile.expected_package_count, context.package_ids, [package["id"] for package in variant.packages]
        )
        result.check(f"{variant.name}:refused-caller-side", any(reason.startswith(variant.caller_reason) for reason in caller),
                     caller)
        sandbox = Sandbox(context, scratch, f"set-drift-{variant.name}")
        baseline = sandbox.preflight("verify")
        result.check(f"{variant.name}:declared-verify-passes", baseline.returncode == 0, baseline.summary())
        manifest = None
        if variant.kind == "package":
            manifest = "tools/release-packages.drift.json"
            (sandbox.workspace / manifest).write_bytes(manifest_bytes({**context.release_document, "packages": list(variant.packages)}))
        fresh = sandbox.preflight("verify", repositories=[mapping.repository for mapping in variant.mappings],
                                  manifest=manifest, evidence=".hexalith/drift-fresh/preflight")
        result.check(f"{variant.name}:fresh-builds-verdict", _verdict(fresh) == variant.fresh,
                     {**fresh.summary(), "expected": variant.fresh})
        between = _drift_between(sandbox, variant, manifest)
        result.check(f"{variant.name}:between-phase-builds-verdict", between["verdict"] == variant.between,
                     {**between, "expected": variant.between})
        variants.append({"variant": variant.name, "callerRefusal": caller, "freshBuildsVerdict": _verdict(fresh),
                         "betweenPhaseBuildsVerdict": between["verdict"],
                         "candidate": [mapping.line for mapping in variant.mappings] if variant.kind == "container"
                         else {"packageCount": len(variant.packages)}})
    result.observations = {"variants": variants}
    container_variants = [item for item in variants if item["variant"].startswith("container-")]
    remapped = next(item for item in variants if item["variant"] == "container-remapped")
    result.exception(
        "no-caller-set-check",
        all(item["freshBuildsVerdict"] == "accepted" for item in container_variants)
        and remapped["betweenPhaseBuildsVerdict"] == "not-detected",
        "Builds accepted every drifted container set at a fresh verify and let a set-preserving remap reach SDK publication; "
        "only the declared caller profile refused them.",
    )
    if remapped["betweenPhaseBuildsVerdict"] == "not-detected":
        result.findings.append({
            "code": "builds-identity-omits-project-mapping",
            "owner": "30-4-five-image-publication-contract",
            "summary": "The pinned frozen identity binds the sorted container repository set but not the project-to-repository "
                       "mapping: a remap preserving the set passed all three phases and reached SDK publication. The caller "
                       "must refuse mapping drift before invoking the publisher.",
            "premiseObserved": True,
            "evidence": {"variant": "container-remapped"},
        })


def run_phase_verification(context: Context, scratch: Path, state: dict, result: CheckpointResult) -> None:
    """Prove one frozen identity across phases, per-image verification without collisions and refusals."""
    dry = state.get("dry-run")
    if not result.check("dry-run-evidence-available", dry is not None and dry.evidence_root.is_dir()):
        return
    preflight = dry.evidence_root / "preflight"
    present = sorted(path.name for path in preflight.iterdir()) if preflight.is_dir() else []
    result.check("one-record-per-phase-for-the-whole-set", present == sorted(PREFLIGHT_FILES), present)
    identity_bytes = (preflight / "publication-identity.json").read_bytes()
    identity = json.loads(identity_bytes)
    identity_sha256 = sha256_bytes(identity_bytes)
    for phase in ("verify", "publish", "container"):
        record = _read_json_file(preflight / f"publication-preflight.{phase}.json") or {}
        result.check(f"{phase}-record-binds-the-frozen-identity",
                     record.get("result") == "pass" and record.get("phase") == phase
                     and record.get("identity_sha256") == identity_sha256 and record.get("identity") == identity,
                     {"identitySha256": record.get("identity_sha256")})
    expected_repositories = sorted(f"{REGISTRY}/{mapping.repository}" for mapping in context.profile.mappings)
    bindings = {
        "builds-workflow-and-action-sha": identity.get("builds", {}).get("workflow_sha") == context.profile.builds_sha
        and identity.get("builds", {}).get("action_sha") == context.profile.builds_sha,
        "builds-executed-helper-hashes": identity.get("builds", {}).get("files") == context.builds.helpers,
        "memories-source": identity.get("source_sha") == context.source["sha"]
        and identity.get("source", {}).get("live_sha") == context.source["sha"],
        "container-repository-set": identity.get("container_repositories") == expected_repositories,
        "package-inventory": identity.get("packages", {}).get("ids") == list(context.package_ids)
        and identity.get("packages", {}).get("manifest_sha256") == sha256_bytes(context.release_manifest),
        "release-identity": identity.get("repository") == RELEASE_REPOSITORY and identity.get("version") == QUALIFICATION_VERSION
        and identity.get("environment") == ENVIRONMENT_NAME and identity.get("platforms") == list(PLATFORMS),
    }
    for name, passed in bindings.items():
        result.check(f"frozen-identity-binds:{name}", passed)
    members = sorted(path.name for path in dry.evidence_root.iterdir() if path.name != "preflight")
    result.check("one-distinct-evidence-directory-per-member",
                 members == sorted(mapping.repository for mapping in context.profile.mappings), members)
    validator_directories = [call["evidence"] for call in _calls(dry, 0, "validator", "start")]
    result.check("per-image-verification-never-collides",
                 len(validator_directories) == len(set(validator_directories)) == len(context.profile.mappings)
                 and len(_calls(dry, 0, "preflight", "start")) == 3,
                 {"validatorDirectories": len(set(validator_directories)),
                  "preflightRuns": len(_calls(dry, 0, "preflight", "start"))})

    sequence = Sandbox(context, scratch, "phase-sequence")
    for phase in ("verify", "publish", "container"):
        outcome = sequence.preflight(phase)
        result.check(f"sequence-{phase}-passes", outcome.returncode == 0, outcome.summary())
    for phase in ("verify", "publish", "container"):
        _expect_refusal(result, f"replayed-{phase}-refused", sequence.preflight(phase), "preflight-phase-collision")
    unsupported = sequence.preflight("recover")
    result.check("unsupported-phase-refused", unsupported.returncode == 2 and "invalid choice" in unsupported.stderr,
                 unsupported.summary())
    repeated = (sequence.evidence_root / "preflight" / "publication-identity.json").read_bytes()
    result.check("repeated-verification-reproduces-the-identity", repeated == identity_bytes,
                 {"dryRun": identity_sha256, "repeated": sha256_bytes(repeated)})

    order = Sandbox(context, scratch, "phase-order")
    _expect_refusal(result, "container-before-verify-refused", order.preflight("container"), "frozen-identity-missing")
    result.check("order-verify-passes", order.preflight("verify").returncode == 0)
    _expect_refusal(result, "container-without-publish-refused", order.preflight("container"), "preflight-sequence-invalid")

    tamper = Sandbox(context, scratch, "phase-tamper")
    result.check("tamper-verify-passes", tamper.preflight("verify").returncode == 0)
    frozen = tamper.evidence_root / "preflight" / "publication-identity.json"
    frozen.write_bytes(frozen.read_bytes().replace(b'"production"', b'"staging"'))
    _expect_refusal(result, "changed-frozen-identity-refused", tamper.preflight("publish"), "publication-identity-changed")

    collision = Sandbox(context, scratch, "phase-frozen-collision")
    result.check("collision-verify-passes", collision.preflight("verify").returncode == 0)
    (collision.evidence_root / "preflight" / "publication-preflight.verify.json").unlink()
    _expect_refusal(result, "second-frozen-identity-refused", collision.preflight("verify"), "frozen-identity-collision")
    result.observations = {"frozenIdentitySha256": identity_sha256, "memberEvidenceDirectories": members,
                           "nugetDestinations": _nuget_destination_refusals(context, scratch, result)}


NUGET_DESTINATION_CASES = (
    # (case, world change for the first manifest package, expected pinned refusal)
    ("package-already-published", "nugetExisting", "version-not-newer"),
    ("package-probe-503", "probeOverrides", "destination-probe-failure"),
)


def _nuget_destination_refusals(context: Context, scratch: Path, result: CheckpointResult) -> dict:
    """Prove an occupied or ambiguous NuGet destination stops the phases before any write."""
    package = context.package_ids[0]
    observed = {}
    for case, change, expected in NUGET_DESTINATION_CASES:
        sandbox = Sandbox(context, scratch, f"nuget-{case}")
        result.check(f"nuget-{case}:clean-verify-passes", sandbox.preflight("verify").returncode == 0)
        sandbox.world.update(**({"nugetExisting": [package]} if change == "nugetExisting"
                                else {"probeOverrides": {f"nuget:{package}": 503}}))
        publish = sandbox.preflight("publish")
        fresh = sandbox.preflight("verify", evidence=".hexalith/nuget-fresh/preflight")
        publisher = sandbox.publisher()
        writes = [call for call in sandbox.world.calls()
                  if call.get("tool") == "dotnet" or (call.get("tool") == "docker" and call["args"][:1] == ["login"])]
        _expect_refusal(result, f"nuget-{case}:publish-phase-refused", publish, expected)
        _expect_refusal(result, f"nuget-{case}:fresh-verify-refused", fresh, expected)
        result.check(f"nuget-{case}:refused-before-any-write",
                     not (sandbox.evidence_root / "preflight" / "publication-preflight.publish.json").exists()
                     and not (sandbox.workspace / ".hexalith" / "nuget-fresh").exists()
                     and publisher.returncode != 0 and publisher.refusal() == "preflight-sequence-invalid" and not writes,
                     {"publisher": publisher.summary(), "writes": len(writes)})
        observed[case] = {"package": package, "publish": publish.refusal(), "freshVerify": fresh.refusal(),
                          "publisher": publisher.refusal()}
    return observed


def run_partial_publication(context: Context, scratch: Path, state: dict, result: CheckpointResult) -> None:
    """Fail the third member three ways and derive the set result from per-image evidence."""
    mappings = context.profile.mappings
    if not result.check("declared-set-has-a-third-member", len(mappings) >= 3):
        return
    failing = mappings[2].repository
    expected = {"published": [m.repository for m in mappings[:2]], "ambiguous": [failing],
                "notAttempted": [m.repository for m in mappings[3:]]}
    state["partial"] = {}
    variants = []
    summaries = []
    for name, mode in PARTIAL_VARIANTS:
        sandbox = Sandbox(context, scratch, f"partial-{name}", failures={failing: mode})
        state["partial"][name] = sandbox
        for phase in ("verify", "publish"):
            result.check(f"{name}:preflight-{phase}-passes", sandbox.preflight(phase).returncode == 0)
        marker = sandbox.mark()
        publisher = sandbox.publisher()
        result.check(f"{name}:publisher-fails", publisher.returncode != 0, publisher.summary())
        result.check(f"{name}:failed-for-the-injected-cause", PARTIAL_CAUSES[mode] in publisher.stderr, publisher.summary())
        members = classify_members(sandbox.evidence_root, mappings, context.source["sha"])
        derived = derive_partial_result(members)
        observed = {key: derived[key] for key in ("published", "ambiguous", "notAttempted")}
        result.check(f"{name}:members-classified", observed == expected, observed)
        result.check(f"{name}:ambiguous-is-never-success", derived["result"] != "pass" and derived["success"] is False,
                     derived["result"])
        publications = [_property(call["args"], "ContainerRepository") for call in _calls(sandbox, marker, "dotnet")]
        result.check(f"{name}:stopped-at-the-failed-member", publications == [m.repository for m in mappings[:3]],
                     publications)
        entries = sorted(path.name for path in sandbox.evidence_root.iterdir())
        summaries.append(entries == sorted(["preflight", *(m.repository for m in mappings[:3])]))
        variants.append({"variant": name, "failureMode": mode, "publisher": publisher.summary(), "derivedResult": derived,
                         "ambiguousProblems": next(member["problems"] for member in members if member["state"] == "ambiguous")})
    result.observations = {"failedMember": failing, "variants": variants}
    result.exception("no-partial-result-document", all(summaries),
                     "Each partial run left only the preflight records and per-member directories; no set-level result.")


def recovery_decision(sandbox: Sandbox, mappings, source_sha: str, statuses=None) -> dict:
    """Decide every member from retained evidence, the pinned absence probe and an observe-created re-read.

    A member is ``published`` only when its evidence re-verifies, its destination is occupied and the
    re-read matches the initial receipt; ``missing`` only when its evidence is incomplete and its
    destination is provably absent. Everything else, including any ambiguous destination, is ``blocked``.
    """
    decision = {"published": [], "missing": [], "blocked": [], "members": []}
    for member in classify_members(sandbox.evidence_root, mappings, source_sha):
        repository = member["repository"]
        outcome, verdict = sandbox.absence([repository], statuses)
        if outcome.returncode == 0 and verdict.get("result") == "pass":
            destination = "absent"
        elif verdict.get("code") == "version-collision":
            destination = "occupied"
        else:
            destination = verdict.get("code") or "unreadable"
        observed = None
        matches_receipt = False
        if destination == "occupied":
            reread = sandbox.observe(repository)
            evidence, _ = _read_member_json(sandbox.workspace / ".hexalith" / "recovery-evidence" / repository / "oci-validation.json")
            evidence = evidence if reread.returncode == 0 and evidence else {}
            children = {child.get("platform"): child.get("digest") for child in evidence.get("children", [])}
            created = {(child.get("provenance_labels") or {}).get("org.opencontainers.image.created")
                       for child in evidence.get("children", [])}
            matches_receipt = (member["state"] == "published" and evidence.get("index_digest") == member["indexDigest"]
                               and children == member["children"] and created == {member["created"]})
            observed = {"exitCode": reread.returncode, "refusal": reread.refusal(),
                        "indexDigest": evidence.get("index_digest"), "matchesInitialReceipt": matches_receipt}
        if member["state"] == "published" and destination == "occupied" and matches_receipt:
            disposition = "published"
        elif member["state"] != "published" and destination == "absent":
            disposition = "missing"
        else:
            disposition = "blocked"
        decision[disposition].append(repository)
        decision["members"].append({"repository": repository, "evidenceState": member["state"], "destination": destination,
                                    "observeCreated": observed, "disposition": disposition})
    return decision


def _member(decision: dict, repository: str) -> dict:
    return next(member for member in decision["members"] if member["repository"] == repository)


def run_recovery(context: Context, scratch: Path, state: dict, result: CheckpointResult) -> None:
    """Prove partial evidence supports exact missing-member recovery and that a rerun refuses collisions."""
    partial = state.get("partial", {})
    sandbox = partial.get("publish-failure")
    if not result.check("partial-evidence-available",
                        sandbox is not None and {"validation-failure", "smoke-failure"} <= set(partial)):
        return
    mappings = context.profile.mappings
    source_sha = context.source["sha"]
    failed = mappings[2].repository
    primary = recovery_decision(sandbox, mappings, source_sha)
    expected_published = [mapping.repository for mapping in mappings[:2]]
    for repository in expected_published:
        member = _member(primary, repository)
        result.check(f"observe-created-re-reads:{repository}",
                     member["disposition"] == "published" and member["observeCreated"]["matchesInitialReceipt"], member)
        result.check(f"published-member-occupies-destination:{repository}", member["destination"] == "occupied", member)
    for mapping in mappings[2:]:
        member = _member(primary, mapping.repository)
        result.check(f"missing-member-destination-absent:{mapping.repository}",
                     member["disposition"] == "missing" and member["destination"] == "absent", member)
    outcome, verdict = sandbox.absence(primary["missing"])
    result.check("missing-set-is-exact",
                 primary["published"] == expected_published and not primary["blocked"]
                 and primary["missing"] == [mapping.repository for mapping in mappings[2:]]
                 and outcome.returncode == 0
                 and verdict.get("containerRepositories") == sorted(f"{REGISTRY}/{name}" for name in primary["missing"]),
                 {key: primary[key] for key in ("published", "missing", "blocked")})
    negative = {}
    for status in (401, 503):
        decision = recovery_decision(sandbox, mappings, source_sha, {failed: status})
        member = _member(decision, failed)
        result.check(f"ambiguous-destination-{status}-blocks-recovery",
                     decision["blocked"] == [failed] and member["destination"] == "destination-probe-failure", member)
        negative[f"probe-{status}"] = {"blocked": decision["blocked"], "member": member}
    for name, check_name, observe_refusal in (
        ("validation-failure", "present-but-unverifiable-member-blocks-recovery", "platform-set-mismatch"),
        ("smoke-failure", "present-valid-but-unhealthy-member-blocks-recovery", None),
    ):
        decision = recovery_decision(partial[name], mappings, source_sha)
        member = _member(decision, failed)
        observed = member["observeCreated"] or {}
        result.check(check_name,
                     member["evidenceState"] == "ambiguous" and member["destination"] == "occupied"
                     and decision["blocked"] == [failed] and failed not in decision["published"] + decision["missing"]
                     and (observed.get("refusal") == observe_refusal if observe_refusal else observed.get("exitCode") == 0),
                     member)
        negative[name] = {"blocked": decision["blocked"], "member": member}
    marker = sandbox.mark()
    rerun = sandbox.publisher()
    writes = [call for call in sandbox.calls_since(marker)
              if call.get("tool") == "dotnet" or (call.get("tool") == "docker" and call["args"][:1] == ["login"])]
    result.check("rerun-refuses-collisions-before-any-write",
                 rerun.returncode != 0 and rerun.refusal() == "version-collision" and not writes,
                 {**rerun.summary(), "writes": len(writes)})
    fresh = sandbox.preflight("verify", evidence=".hexalith/release-evidence-rerun/preflight")
    _expect_refusal(result, "fresh-verify-refuses-the-occupied-version", fresh, "version-not-newer")
    result.observations = {
        "recoveryDecision": {key: primary[key] for key in ("published", "missing", "blocked")}
        | {"missingMemberPublisher": "30-5-partial-release-recovery"},
        "members": primary["members"],
        "negativeCases": negative,
    }
    result.exception("no-missing-only-republish", rerun.refusal() == "version-collision" and not writes,
                     "The pinned publisher rerun refused the occupied set before login or any SDK publication.")
    result.findings.extend(_static_findings(context, ("recovery-workflow-superseded-set",)))


def run_evidence_upload(context: Context, scratch: Path, state: dict, result: CheckpointResult) -> None:
    """Prove the caller hash manifest covers the complete evidence set and refuses gaps."""
    steps = workflow_steps(context.builds.reference_text[DOMAIN_RELEASE_WORKFLOW], UPLOAD_STEP_NAME)
    if not result.check("builds-upload-steps-located", bool(steps), [step["line"] for step in steps]):
        return
    for step in steps:
        result.check(f"builds-upload-covers-hidden-evidence-root:line-{step['line']}",
                     step.get("path") == ".hexalith/release-evidence/**" and step.get("include-hidden-files") == "true", step)
    dry = state.get("dry-run")
    if not result.check("dry-run-evidence-available", dry is not None and dry.evidence_root.is_dir()):
        return
    upload_root = dry.evidence_root.parent
    required = {f"{QUALIFICATION_VERSION}/preflight/{name}" for name in PREFLIGHT_FILES} | {
        f"{QUALIFICATION_VERSION}/{mapping.repository}/{name}" for mapping in context.profile.mappings for name in MEMBER_FILES
    }
    # Exact paths only: the installed helpers, the copied projects, the release manifest and the expected evidence.
    allowed = {f".hexalith/release/{name}" for name in EXECUTED_HELPERS} | {
        mapping.project for mapping in context.profile.mappings
    } | {MANIFEST_PATH.as_posix()} | {f"{upload_root.relative_to(dry.workspace).as_posix()}/{path}" for path in required}
    stray = sorted(path.relative_to(dry.workspace).as_posix() for path in dry.workspace.rglob("*")
                   if path.is_file() and path.relative_to(dry.workspace).as_posix() not in allowed)
    result.check("no-stray-builds-output-outside-the-expected-evidence", not stray, stray)
    entries = build_hash_manifest(upload_root)
    listed = {path for path, _ in entries}
    result.check("complete-set-evidence-present", required <= listed, sorted(required - listed))
    result.check("caller-manifest-verifies", verify_hash_manifest(upload_root, entries) == [])
    negative = scratch / "upload-negative"
    for case, expected in (("missing-file", "missing:"), ("tampered-file", "hash-mismatch:"), ("unlisted-file", "unlisted:")):
        copy = negative / case
        shutil.copytree(upload_root, copy)
        target = copy / f"{QUALIFICATION_VERSION}/{context.profile.mappings[0].repository}/smoke-results.json"
        if case == "missing-file":
            target.unlink()
        elif case == "tampered-file":
            target.write_bytes(target.read_bytes() + b" ")
        else:
            (copy / f"{QUALIFICATION_VERSION}/unlisted.txt").write_text("x", encoding="utf-8")
        problems = verify_hash_manifest(copy, entries)
        result.check(f"caller-upload-refuses-{case}", any(problem.startswith(expected) for problem in problems), problems)
    (negative / "empty").mkdir(parents=True)
    result.check("caller-upload-refuses-empty-evidence", verify_hash_manifest(negative / "empty", []) == ["manifest-empty"])
    builds_covered = set()
    for path, _ in entries:
        if path.endswith("/smoke-sha256.txt"):
            prefix = path.rsplit("/", 1)[0]
            builds_covered.update(f"{prefix}/{name}" for name in SMOKE_HASHED_FILES)
    partial = state.get("partial", {}).get("publish-failure")
    if result.check("partial-evidence-available", partial is not None):
        copy = scratch / "upload-partial"
        shutil.copytree(partial.evidence_root.parent, copy)
        partial_entries = build_hash_manifest(copy)
        original = classify_members(partial.evidence_root, context.profile.mappings, context.source["sha"])
        uploaded = classify_members(copy / QUALIFICATION_VERSION, context.profile.mappings, context.source["sha"])
        result.check("uploaded-partial-evidence-supports-recovery",
                     verify_hash_manifest(copy, partial_entries) == []
                     and [member["state"] for member in uploaded] == [member["state"] for member in original])
    rendered = render_hash_manifest(entries)
    result.observations = {
        "buildsUploadSteps": steps,
        "callerHashManifestSha256": sha256_bytes(rendered.encode("utf-8")),
        "callerHashManifest": [{"path": path, "sha256": digest} for path, digest in entries],
        "buildsHashCoverage": {"files": len(builds_covered & listed), "of": len(listed)},
    }
    locations = ", ".join(f"{DOMAIN_RELEASE_WORKFLOW}:{step['line']}" for step in steps)
    result.exception(
        "warn-upload-without-hash-manifest",
        all(step.get("if-no-files-found") == "warn" for step in steps) and builds_covered < listed,
        f"Every upload step ({locations}) uses if-no-files-found: "
        f"{', '.join(sorted({str(step.get('if-no-files-found')) for step in steps}))}; "
        f"Builds hash lists cover {len(builds_covered & listed)} of {len(listed)} evidence files.",
    )
    result.findings.extend(_static_findings(context, ("evidence-retention-limited",)))


GROUPS = (
    ("complete-set-dry-run", run_complete_set_dry_run),
    ("set-drift", run_set_drift),
    ("phase-verification", run_phase_verification),
    ("partial-publication", run_partial_publication),
    ("recovery", run_recovery),
    ("evidence-upload", run_evidence_upload),
)


def run_groups(context: Context, scratch: Path) -> dict:
    """Run every checkpoint group; an unexpected error fails that group closed."""
    state = {}
    results = {}
    for checkpoint, function in GROUPS:
        result = CheckpointResult(checkpoint)
        try:
            function(context, scratch, state, result)
        except Exception as error:  # noqa: BLE001 -- any unexpected error must fail the checkpoint closed.
            result.check("checkpoint-completed", False, f"{type(error).__name__}: {error}")
        results[checkpoint] = result
    return results


# --------------------------------------------------------------------------- documents


def build_documents(context: Context, results: dict, forced_failures=()) -> tuple:
    """Build the six evidence documents; no checkpoint passes unless all of them pass."""
    failing = [checkpoint for checkpoint in CHECKPOINTS if not results[checkpoint].passed]
    overall = context.ready and not failing and not forced_failures
    documents = {}
    for checkpoint in CHECKPOINTS:
        result = results[checkpoint]
        reasons = [f"check-failed:{entry['name']}" for entry in result.checks if entry["result"] != "pass"]
        reasons.extend(f"prerequisite-failed:{failure}" for failure in context.prerequisite_failures)
        reasons.extend(f"qualification-incomplete:{other}" for other in failing if other != checkpoint)
        reasons.extend(forced_failures)
        # The JSON round-trip normalizes tuples so the post-write self-check compares like with like.
        documents[checkpoint] = json.loads(json.dumps({
            "schemaVersion": SCHEMA_VERSION,
            "story": STORY_KEY,
            "checkpoint": checkpoint,
            "result": "pass" if overall else "fail",
            "checksResult": "pass" if result.passed else "fail",
            "overallResult": "pass" if overall else "fail",
            "synthetic": True,
            "bindings": context.bindings(),
            "release": context.release(),
            "checks": result.checks,
            "observations": result.observations,
            "findings": result.findings,
            "exceptions": result.exceptions,
            "reasons": reasons,
        }))
    return overall, documents


def prerequisite_results(context: Context) -> dict:
    """Return failed results for a run that could not start."""
    results = {}
    for checkpoint in CHECKPOINTS:
        result = CheckpointResult(checkpoint)
        result.check("qualification-prerequisites", False, list(context.prerequisite_failures))
        results[checkpoint] = result
    return results


def _approval_is_pending(text: str) -> bool:
    """Return whether the record is untouched: one pending status and both owner rows blank and pending."""
    lines = [line.strip() for line in text.splitlines()]
    if [line for line in lines if line.startswith("Status:")] != [PENDING_STATUS_LINE] or OWNER_DECISIONS_HEADING not in lines:
        return False
    section = []
    for line in lines[lines.index(OWNER_DECISIONS_HEADING) + 1:]:
        if line.startswith("## "):
            break
        section.append(line)
    rows = [[cell.strip() for cell in line.strip("|").split("|")] for line in section if line.startswith("|")]
    for role in APPROVAL_ROLES:
        matches = [row for row in rows if row and row[0] == role]
        # Role | Owner | Decision | Date | Notes: the owner and date must be empty and the decision exactly pending.
        if len(matches) != 1 or len(matches[0]) != 5 or matches[0][1:4] != ["", "pending", ""]:
            return False
    return True


def approval_guard(output_directory: Path) -> str | None:
    """Refuse to overwrite an approval record unless it is still exactly pending."""
    path = output_directory / APPROVAL_FILE
    if not path.exists():
        return None
    if not _approval_is_pending(path.read_text(encoding="utf-8")):
        return f"{path} is no longer pending; move it aside before regenerating the qualification."
    return None


def verify_written(output_directory: Path, documents: dict) -> list:
    """Re-read every evidence file and prove it is complete and binds identical identities."""
    problems = []
    bindings = set()
    for checkpoint, document in documents.items():
        observed = _read_json_file(output_directory / f"{checkpoint}.json")
        if observed != document:
            problems.append(f"evidence-unreadable-or-changed:{checkpoint}")
            continue
        bindings.add(json.dumps(observed["bindings"], sort_keys=True))
    if len(bindings) != 1:
        problems.append("evidence-bindings-differ")
    return problems


def render_approval(context: Context, overall: bool, documents: dict, hashes: dict) -> str:
    """Render the pending owner-approval record; the harness never records a decision."""
    bindings = context.bindings()
    source = bindings["source"] or {}
    lines = [
        "# Story 30.2 Exact-Revision Owner Approval",
        "",
        PENDING_STATUS_LINE,
        "",
        f"Qualification result: {'pass' if overall else 'fail'}",
        "",
        f"Generated by `{HARNESS_LABEL}`. The harness never records approval and refuses to overwrite this file once "
        "it is no longer pending. Approval grants no publication, predecessor, or later-story credit, and upstream "
        "fixture success alone never qualifies Memories.",
        "",
    ]
    if source.get("dirty"):
        lines.extend([
            "**Not approvable:** this evidence came from a dirty working tree. Regenerate it from a clean commit "
            "before any decision.",
            "",
        ])
    lines.extend([
        "## Bound identities",
        "",
        "| Identity | Value |",
        "| --- | --- |",
        f"| Hexalith.Builds revision | `{bindings['builds']['sha']}` (verified: {'yes' if bindings['builds']['verified'] else 'no'}) |",
        f"| Memories source revision | `{source.get('sha')}` (dirty working tree: {'yes' if source.get('dirty') else 'no'}) |",
        f"| Harness SHA-256 (`{HARNESS_LABEL}`) | `{bindings['harness']['sha256']}` |",
        f"| Profile SHA-256 (`{bindings['profile']['path']}`) | `{bindings['profile']['sha256']}` |",
        "",
        "## Upstream candidate fixtures",
        "",
        "The Builds Maintainer's decision also approves the upstream candidate fixtures, run at exactly the bound "
        f"Builds revision `{bindings['builds']['sha']}`:",
        "",
        "```",
        UPSTREAM_FIXTURE_COMMAND,
        "```",
        "",
        "## Executed Builds helpers",
        "",
        "| Helper | SHA-256 |",
        "| --- | --- |",
    ])
    lines.extend(f"| `{HELPER_DIRECTORY}/{name}` | `{digest}` |"
                 for name, digest in bindings["builds"].get("executedHelpers", {}).items())
    lines.extend(["", "## Evidence", "", "| Checkpoint | File | Result | SHA-256 |", "| --- | --- | --- | --- |"])
    lines.extend(f"| {checkpoint} | `{checkpoint}.json` | {documents[checkpoint]['result']} | `{hashes[checkpoint]}` |"
                 for checkpoint in CHECKPOINTS)
    lines.extend(["", "## Caller-owned exceptions", "", "| Exception | Gap | Caller control | Hand-off |", "| --- | --- | --- | --- |"])
    for document in documents.values():
        lines.extend(f"| `{item['code']}` | {item['gap']} | {item['callerControl']} | {', '.join(item['handedTo']) or 'none'} |"
                     for item in document["exceptions"])
    lines.extend(["", "## Findings", "", "| Finding | Owner | Summary |", "| --- | --- | --- |"])
    for document in documents.values():
        lines.extend(f"| `{item['code']}` | {item['owner']} | {item['summary']} |" for item in document["findings"])
    lines.extend([
        "",
        OWNER_DECISIONS_HEADING,
        "",
        "Record a decision only after reviewing the evidence at exactly these identities. Any harness rerun regenerates "
        "the evidence and requires a new decision.",
        "",
        "| Role | Owner | Decision | Date | Notes |",
        "| --- | --- | --- | --- | --- |",
        "| Builds Maintainer | | pending | | |",
        "| Memories Release Engineer | | pending | | |",
        "",
    ])
    return "\n".join(lines)


def write_outputs(output_directory: Path, context: Context, results: dict) -> tuple:
    """Write the six evidence files and the pending approval record; return ``(overall, hashes)``."""
    output_directory.mkdir(parents=True, exist_ok=True)
    forced = []
    while True:
        overall, documents = build_documents(context, results, forced)
        hashes = {
            checkpoint: sha256_bytes(write_crlf(output_directory / f"{checkpoint}.json", json.dumps(document, indent=2) + "\n"))
            for checkpoint, document in documents.items()
        }
        problems = verify_written(output_directory, documents)
        if not problems or forced:
            break
        forced = [f"evidence-self-check-failed:{problem}" for problem in problems]
    overall = overall and not problems
    write_crlf(output_directory / APPROVAL_FILE, render_approval(context, overall, documents, hashes))
    return overall, hashes, documents


# -------------------------------------------------------------------------------- main


def parse_arguments(arguments) -> argparse.Namespace:
    """Parse the public command line."""
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description="Qualify the pinned Builds container-publication contract offline.")
    parser.add_argument("--output-directory", required=True, help="Directory receiving the six results and approval.md.")
    parser.add_argument("--repository-root", default=str(root), help=argparse.SUPPRESS)
    parser.add_argument("--profile", default=None, help=argparse.SUPPRESS)
    return parser.parse_args(arguments)


def main(argv=None) -> int:
    """Run the qualification and return its process exit code."""
    arguments = list(sys.argv[1:] if argv is None else argv)
    if arguments[:1] == ["--seam"]:
        return run_seam(arguments[1] if len(arguments) > 1 else "", arguments[2:])
    options = parse_arguments(arguments)
    root = Path(options.repository_root).resolve()
    output_directory = Path(options.output_directory).resolve()
    refusal = approval_guard(output_directory)
    if refusal:
        print(f"[qualify-builds-publication] approval-record-not-pending: {refusal}", file=sys.stderr)
        return 1
    profile = Path(options.profile).resolve() if options.profile else root / PROFILE_LABEL
    context = prepare_context(root, profile, Path(__file__).resolve())
    if context.ready:
        with tempfile.TemporaryDirectory(prefix="memories-30-2-qualification-") as scratch:
            results = run_groups(context, Path(scratch))
    else:
        results = prerequisite_results(context)
    overall, hashes, documents = write_outputs(output_directory, context, results)
    for checkpoint in CHECKPOINTS:
        print(f"[qualify-builds-publication] {checkpoint}: {documents[checkpoint]['result']} "
              f"(checks {documents[checkpoint]['checksResult']}) sha256 {hashes[checkpoint]}")
    for failure in context.prerequisite_failures:
        print(f"[qualify-builds-publication] prerequisite failed: {failure}", file=sys.stderr)
    print(f"[qualify-builds-publication] overall: {'pass' if overall else 'fail'}; approval pending in "
          f"{output_directory / APPROVAL_FILE}")
    return 0 if overall else 1


if __name__ == "__main__":
    sys.exit(main())
