#!/usr/bin/env python3
"""Memories' five-image adapter for the exact approved Builds publication helpers."""

import argparse
import base64
import gzip
from datetime import datetime, timezone
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile


APPROVED_BUILDS_SHA = "aebabdadadffa108801992253c7910f37437b115"
# Keep previous release revisions available for recovery of their retained artifacts.
RECOVERY_BUILDS_SHAS = {
    "81c88b34d09d34260b92d7f0928dbbc1c7446baa",
    "ae77d5690a9411a2154fa4ed5231edc373942573",
    "38e038d1179b1c829ba47a45bfa0a4683ea5e2a5",
    "b80f3e85fc803f71bf7b2809965db0aceafdc3c5",
    APPROVED_BUILDS_SHA,
    "87d45477c16fef2b3db706d2531e1cf3ff98640f",
    "62876129eaa035e78bd0324419231f07d43a07b8",
}
PLATFORMS = ["linux/amd64", "linux/arm64"]
CANONICAL_IMAGES = [
    {"name": "server", "project": "src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj", "repository": "memories", "health": "liveness"},
    {"name": "mcp", "project": "src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj", "repository": "memories-mcp", "health": "liveness"},
    {"name": "access-telemetry", "project": "src/Hexalith.Memories.AccessTelemetry/Hexalith.Memories.AccessTelemetry.csproj", "repository": "memories-access-telemetry", "health": "disabled-readiness"},
    {"name": "access-telemetry-clock", "project": "src/Hexalith.Memories.AccessTelemetry.Clock/Hexalith.Memories.AccessTelemetry.Clock.csproj", "repository": "memories-access-telemetry-clock", "health": "signed-readiness"},
    {"name": "domain-service", "project": "src/Hexalith.Memories.DomainService/Hexalith.Memories.DomainService.csproj", "repository": "memories-domain-service", "health": "liveness"},
]
SHARED_FILES = (
    "publication_preflight.py",
    "oci_registry_validator.py",
    "oci_attestation.py",
    "smoke_container_platforms.py",
    "smoke-container-platforms.sh",
    "publish-containers.sh",
    "action.yml",
)


def shared_files_for(sha):
    """Current pin records the attestation helper; earlier recovery SHAs do not."""
    if sha == APPROVED_BUILDS_SHA:
        return SHARED_FILES
    return tuple(name for name in SHARED_FILES if name != "oci_attestation.py")


# Every OCI child must start exactly like the verified single-RID deployment archive.
RUNTIME_CONFIG_FIELDS = ("Env", "Entrypoint", "Cmd", "WorkingDir", "User", "ExposedPorts")
COMMAND_TIMEOUT_SECONDS = 1800
# verify-container-registry.ps1 writes authorization.json here by default; authorize reads it.
DEFAULT_EVIDENCE_DIRECTORY = Path("artifacts/release/container-evidence")


def strict_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON field.")
        result[key] = value
    return result


def read_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"), object_pairs_hook=strict_object)


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(",", ":")).encode()


def sha256(path):
    with Path(path).open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def write_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def load_local(name):
    specification = importlib.util.spec_from_file_location(name.replace("-", "_"), Path(__file__).with_name(name + ".py"))
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


def redact(value):
    text = str(value)
    secrets = [os.environ.get(key, "") for key in ("GITHUB_TOKEN", "GH_TOKEN", "NUGET_API_KEY", "HEXALITH_ZOT_USERNAME", "HEXALITH_ZOT_API_KEY")]
    if secrets[-2] and secrets[-1]:
        secrets.append(base64.b64encode(f"{secrets[-2]}:{secrets[-1]}".encode()).decode())
    for secret in sorted(filter(None, secrets), key=len, reverse=True):
        text = text.replace(secret, "[redacted]")
    return re.sub(r"(?i)(bearer|basic)\s+\S+", r"\1 [redacted]", text)


def write_log(log, output):
    if log:
        Path(log).parent.mkdir(parents=True, exist_ok=True)
        Path(log).write_text(output, encoding="utf-8")


def captured_text(value):
    # TimeoutExpired always carries bytes, whatever text= requested.
    if isinstance(value, bytes):
        return value.decode("utf-8", errors="replace")
    return value or ""


def run(command, log=None, timeout=COMMAND_TIMEOUT_SECONDS):
    name = Path(str(command[0])).name
    try:
        result = subprocess.run([str(item) for item in command], capture_output=True, text=True, check=False, timeout=timeout)
    except subprocess.TimeoutExpired as expired:
        # Keep the partial output as diagnostic evidence instead of losing it with the process.
        output = redact(captured_text(expired.stdout) + captured_text(expired.stderr) + f"\n[timeout] {name} did not finish within {timeout} s.\n")
        write_log(log, output)
        raise ValueError(f"{name} timed out after {timeout} s: {output[-2000:]}") from None
    output = redact(result.stdout + result.stderr)
    write_log(log, output)
    if result.returncode:
        raise ValueError(f"{name} failed ({result.returncode}): {output[-2000:]}")
    return result.stdout


def load_shared(directory, recovery_identity=None):
    directory = Path(directory).resolve()
    root = directory.parents[1]
    checkout = subprocess.run(["git", "-C", str(root), "rev-parse", "HEAD"], capture_output=True, text=True, check=True).stdout.strip()
    # Health uses this same loader in its own process; its identity must pass the same
    # offline identity and helper checks as the recovery caller before import.
    if recovery_identity is None and os.environ.get("MEMORIES_RECOVERY_IDENTITY"):
        recovery_identity = read_json(os.environ["MEMORIES_RECOVERY_IDENTITY"])
    expected_sha = APPROVED_BUILDS_SHA
    if recovery_identity is not None:
        verify_frozen_identity(recovery_identity)
        expected_sha = recovery_identity["buildsSha"]
    if checkout != expected_sha:
        raise ValueError("Publication helpers are not the approved Builds revision.")
    files = {}
    for name in (*shared_files_for(expected_sha), "../../Props/ContainerProvenance.targets"):
        path = (directory / name).resolve()
        relative = path.relative_to(root).as_posix()
        approved = subprocess.run(["git", "-C", str(root), "show", f"{expected_sha}:{relative}"], capture_output=True, check=True).stdout
        # A checkout may materialize CRLF, but executable helper bytes must match its approved blob.
        if path.read_bytes().replace(b"\r\n", b"\n") != approved.replace(b"\r\n", b"\n"):
            raise ValueError(f"Approved Builds file changed: {relative}")
        files[relative] = sha256(path)
    if recovery_identity is not None and files != recovery_identity["helpers"]:
        raise ValueError("Recovery helper hashes differ from the failed run's recorded helpers.")
    for name in ("oci_registry_validator", "smoke_container_platforms"):
        cached = sys.modules.get(name)
        if cached and Path(cached.__file__).resolve() != directory / (name + ".py"):
            raise ValueError("An unapproved Builds module is already loaded.")
    # No approved executable is imported until every approved byte has been checked.
    sys.path.insert(0, str(directory))
    try:
        specification = importlib.util.spec_from_file_location("approved_publication_preflight", directory / "publication_preflight.py")
        preflight = importlib.util.module_from_spec(specification)
        specification.loader.exec_module(preflight)
        import oci_registry_validator
        import smoke_container_platforms
    finally:
        sys.path.pop(0)
    return preflight, oci_registry_validator, smoke_container_platforms, files


def validate_manifest(root, path):
    document = read_json(path)
    if document != {"schemaVersion": "hexalith.memories.container-publication.v1", "buildsSha": APPROVED_BUILDS_SHA, "platforms": PLATFORMS, "images": CANONICAL_IMAGES}:
        raise ValueError("The exact five canonical project/repository/health mappings and approved Builds identity are required.")
    discovered = {}
    for project in (root / "src").rglob("*.csproj"):
        if "obj" in project.relative_to(root).parts or "bin" in project.relative_to(root).parts:
            continue
        tree = ET.parse(project)
        # MSBuild boolean conditions are case-insensitive, so `True` also enables a container.
        if any((node.text or "").strip().lower() == "true" for node in tree.findall(".//EnableContainer")):
            repositories = [node.text for node in tree.findall(".//ContainerRepository")]
            discovered[project.relative_to(root).as_posix()] = repositories
    if discovered != {image["project"]: [image["repository"]] for image in CANONICAL_IMAGES}:
        raise ValueError("Container project inventory is missing, added or remapped.")
    return document


def current_identity(root, manifest, version, registry, helper_hashes):
    if not re.fullmatch(r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?", version):
        raise ValueError("Version must be an image-taggable semantic version.")
    if "-" in version and any(part.isdigit() and len(part) > 1 and part.startswith("0") for part in version.split("-", 1)[1].split(".")):
        raise ValueError("Numeric prerelease identifiers cannot have leading zeroes.")
    if not re.fullmatch(r"[a-z0-9]+(?:[a-z0-9.-]*[a-z0-9])?(?::[0-9]+)?", registry):
        raise ValueError("Registry must be an unambiguous lowercase host and optional port.")
    source = run(["git", "-C", root, "rev-parse", "HEAD"]).strip()
    run(["git", "-C", root, "diff", "--quiet", "HEAD", "--", ".", ":!references"])
    untracked = run(["git", "-C", root, "ls-files", "--others", "--exclude-standard", "--", "src", "tools", "deploy", ".github", ".config", "*.props", "*.targets", "*.slnx", "*.rsp", "global.json", "NuGet.Config", "nuget.config"])
    if untracked.strip():
        raise ValueError("Untracked release build inputs are not part of the frozen source revision.")
    ignored_source = run(["git", "-C", root, "ls-files", "--others", "--", "src"])
    if any(Path(path).suffix == ".cs" and not {"bin", "obj"}.intersection(Path(path).parts) for path in ignored_source.splitlines()):
        raise ValueError("Untracked SDK-glob compiled source is not part of the frozen source revision.")
    workflow = "Hexalith/Hexalith.Memories/.github/workflows/release.yml@refs/heads/main"
    if (not re.fullmatch(r"[0-9a-f]{40}", source) or os.environ.get("GITHUB_SHA") != source
            or os.environ.get("GITHUB_WORKFLOW_SHA") != source or os.environ.get("GITHUB_REPOSITORY") != "Hexalith/Hexalith.Memories"
            or os.environ.get("GITHUB_WORKFLOW_REF") != workflow or os.environ.get("GITHUB_EVENT_NAME") != "workflow_dispatch"
            or os.environ.get("GITHUB_REF") != "refs/heads/main"):
        raise ValueError("Publication requires the exact dispatched main source and release workflow identity.")
    run_identity = {key: os.environ.get(key, "") for key in ("GITHUB_RUN_ID", "GITHUB_RUN_ATTEMPT", "GITHUB_RUN_NUMBER", "GITHUB_ACTOR", "GITHUB_TRIGGERING_ACTOR")}
    if any(not value for value in run_identity.values()) or any(not re.fullmatch(r"[1-9][0-9]*", run_identity[key]) for key in ("GITHUB_RUN_ID", "GITHUB_RUN_ATTEMPT", "GITHUB_RUN_NUMBER")):
        raise ValueError("Publication workflow run identity is incomplete.")
    return {"schemaVersion": "hexalith.memories.release-set.v1", "version": version, "sourceSha": source,
            "repository": "Hexalith/Hexalith.Memories", "workflow": workflow, "workflowSha": source,
            "run": run_identity, "registry": registry, "buildsSha": APPROVED_BUILDS_SHA,
            "helpers": helper_hashes, "manifestSha256": sha256(manifest), "platforms": PLATFORMS, "images": CANONICAL_IMAGES}


def freeze_identity(path, identity, create=False):
    if create:
        if path.exists():
            raise ValueError("Release set identity already exists; use a fresh evidence directory.")
        identity = dict(identity, created=datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"))
        identity["setSha256"] = hashlib.sha256(canonical(identity)).hexdigest()
        write_json(path, identity)
        return identity
    frozen = read_json(path)
    current = {key: value for key, value in frozen.items() if key not in ("created", "setSha256")}
    if current != identity or frozen.get("setSha256") != hashlib.sha256(canonical({key: value for key, value in frozen.items() if key != "setSha256"})).hexdigest():
        raise ValueError("Frozen project/source/version/workflow/release-set identity changed.")
    return frozen


def shared_phase(shared, evidence, identity, phase):
    arguments = [sys.executable, shared / "publication_preflight.py", "--phase", phase,
                 "--repository", identity["repository"], "--version", identity["version"],
                 "--source-sha", identity["sourceSha"], "--source-branch", "main", "--source-ci-workflow", "ci.yml",
                 "--builds-execution-sha", APPROVED_BUILDS_SHA, "--environment-name", "production",
                 "--package-manifest", "tools/release-packages.json", "--expected-package-count", "10",
                 "--contract-directory", shared, "--evidence-directory", evidence / "preflight"]
    for image in CANONICAL_IMAGES:
        arguments += ["--container-repository", f"{identity['registry']}/{image['repository']}"]
    run(arguments, evidence / f"preflight-{phase}.log")


def archive_graph(path, validator, labels):
    with tarfile.open(path, "r:*") as archive:
        names = [member.name.removeprefix("./") for member in archive.getmembers()]
        if len(names) != len(set(names)):
            raise ValueError("OCI archive has duplicate entries.")
        members = dict(zip(names, archive.getmembers()))
        def read(name):
            member = members.get(name)
            if member is None or not member.isfile() or member.size > 16 * 1024 * 1024:
                raise ValueError("OCI metadata object is absent or malformed.")
            return archive.extractfile(member).read()
        def response(data):
            return {"body_bytes": data, "docker_content_digest": "sha256:" + hashlib.sha256(data).hexdigest(), "content_type": json.loads(data).get("mediaType")}
        def resolve(digest):
            validator._validate_digest(digest)
            return response(read("blobs/sha256/" + digest.split(":", 1)[1]))
        if json.loads(read("oci-layout")).get("imageLayoutVersion") != "1.0.0":
            raise ValueError("OCI archive layout version is invalid.")
        index = read("index.json")
        parsed = json.loads(index)
        # OCI archive index.json may be a transport wrapper around the actual image index.
        descriptors = parsed.get("manifests", [])
        if len(descriptors) == 1 and descriptors[0].get("mediaType") == validator.OCI_INDEX_MEDIA_TYPE:
            resolved = resolve(descriptors[0]["digest"])
            index = resolved["body_bytes"]
            if resolved["docker_content_digest"] != descriptors[0]["digest"] or len(index) != descriptors[0].get("size"):
                raise ValueError("OCI archive transport wrapper changed its index digest or size.")
        initial = response(index)
        return validator._validate_graph(initial, lambda _: initial, resolve,
                                         lambda digest: ((item := resolve(digest)), item["body_bytes"]), labels)


def labels_for(validator, identity):
    return validator.expected_provenance_labels(identity["repository"], identity["sourceSha"], identity["version"], identity["created"])


def deployment_config(path):
    """Return the image config of a stable single-image Docker deployment archive."""
    with tarfile.open(path, "r:*") as archive:
        manifest = json.load(archive.extractfile("manifest.json"))
        if len(manifest) != 1:
            raise ValueError("Deployment archive must contain exactly one Docker image.")
        return json.load(archive.extractfile(manifest[0]["Config"]))


def runtime_config(config):
    image_config = config.get("config") if isinstance(config, dict) else None
    if not isinstance(image_config, dict):
        raise ValueError("Image config has no runtime configuration.")
    return {field: image_config.get(field) for field in RUNTIME_CONFIG_FIELDS}


def runtime_config_parity(deployment, graph):
    """Compare each OCI child's runtime config with the single-RID deployment archive's."""
    expected = runtime_config(deployment)
    children = {child["platform"]: runtime_config(json.loads(child["config_bytes"])) for child in graph["children"]}
    mismatches = [{"platform": platform, "field": field, "deployment": expected[field], "child": values[field]}
                  for platform, values in children.items() for field in RUNTIME_CONFIG_FIELDS if values[field] != expected[field]]
    record = {"fields": list(RUNTIME_CONFIG_FIELDS), "deploymentSha256": hashlib.sha256(canonical(expected)).hexdigest(),
              "children": {platform: hashlib.sha256(canonical(values)).hexdigest() for platform, values in children.items()}}
    diagnostics = {"result": "fail" if mismatches else "pass", "fields": list(RUNTIME_CONFIG_FIELDS), "deployment": expected,
                   "children": children, "mismatches": mismatches, "sha256": record}
    return record, diagnostics


def mismatch_error(mismatches):
    differences = ", ".join(f"{item['platform']} {item['field']}" for item in mismatches)
    return f"OCI child runtime config differs from the single-RID deployment archive ({differences}); see runtime-config-parity.json."


def build_archives(root, output, evidence, shared, identity, validator):
    shared_phase(shared, evidence, identity, "verify")
    compatibility = []
    failures = []
    outcomes = []
    for image in CANONICAL_IMAGES:
        member = evidence / image["name"]
        try:
            common = ["dotnet", "publish", image["project"], "--configuration", "Release", "-t:PublishContainer",
                      "-p:UseHexalithProjectReferences=false", "-p:ContinuousIntegrationBuild=true",
                      f"-p:Version={identity['version']}", f"-p:PackageVersion={identity['version']}",
                      f"-p:ContainerRepository={image['repository']}", f"-p:ContainerRegistry={identity['registry']}",
                      f"-p:ContainerImageTag={identity['version']}", f"-p:ContainerProvenanceSourceSha={identity['sourceSha']}",
                      f"-p:ContainerProvenanceRepositoryUrl=https://github.com/{identity['repository']}",
                      f"-p:ContainerProvenanceReleaseVersion={identity['version']}", f"-p:ContainerProvenanceCreated={identity['created']}",
                      f"-p:MemoriesApprovedProvenanceTargets={shared.parents[1] / 'Props/ContainerProvenance.targets'}"]
            legacy = output / (image["name"] + ".tar.gz")
            oci = output / (image["name"] + ".oci.tar.gz")
            if legacy.exists() or oci.exists():
                raise ValueError("Build archive already exists; refuse stale release output.")
            run(common + ["-p:RuntimeIdentifier=linux-musl-x64", "-p:ContainerImageFormat=Docker", f"-p:ContainerArchiveOutputPath={legacy}"], member / "build-deployment.log")
            run(common + ['-p:RuntimeIdentifiers="linux-musl-x64;linux-musl-arm64"', '-p:ContainerRuntimeIdentifiers="linux-musl-x64;linux-musl-arm64"',
                          "-p:ContainerImageFormat=OCI", f"-p:ContainerArchiveOutputPath={oci}"], member / "build-oci.log")
            graph = archive_graph(oci, validator, labels_for(validator, identity))
            validator.write_evidence(member / "local", f"{identity['registry']}/{image['repository']}:{identity['version']}", graph)
            # Stable deployment archives retain Docker format and the same provenance source.
            config = deployment_config(legacy)
            validator._validate_labels(config, labels_for(validator, identity))
            if config.get("architecture") != "amd64" or config.get("os") != "linux":
                raise ValueError("Stable deployment archive must remain linux/amd64 Docker format.")
            # Refuse here, before the authorize/publish phases can write anything, when a child
            # would start differently from the verified same-source deployment archive.
            parity, diagnostics = runtime_config_parity(config, graph)
            write_json(member / "runtime-config-parity.json", diagnostics)
            if diagnostics["mismatches"]:
                raise ValueError(mismatch_error(diagnostics["mismatches"]))
            compatibility.append({"name": image["name"], "filename": legacy.name, "format": "docker-archive", "sha256": sha256(legacy), "ociArchive": oci.name, "ociSha256": sha256(oci), "indexDigest": graph["index_digest"], "runtimeConfig": parity, "sourceSha": identity["sourceSha"], "setSha256": identity["setSha256"]})
            outcomes.append(outcome(image, identity, "succeeded", "built"))
        except Exception as error:
            failures.append(redact(error))
            outcomes.append(outcome(image, identity, "failed", "build-failed", redact(error)))
    write_json(evidence / "archive-compatibility.json", {"result": "pass" if not failures else "fail", "setSha256": identity["setSha256"], "archives": compatibility})
    write_summary(output / "build-summary.json", identity, outcomes, False)
    if failures:
        raise ValueError("One or more release archive builds failed; see member diagnostics.")


def outcome(image, identity, status, disposition, error=None, transported=False):
    return {"name": image["name"], "image": f"{identity['registry']}/{image['repository']}:{identity['version']}", "status": status, "disposition": disposition, "exitCode": 0 if status == "succeeded" else 1, "error": error, "transported": transported}


def write_summary(path, identity, outcomes, push):
    succeeded = [item for item in outcomes if item["status"] == "succeeded"]
    failed = [item for item in outcomes if item["status"] != "succeeded"]
    transported = [item for item in outcomes if item.get("transported")]
    status = "succeeded" if len(succeeded) == 5 else "partial-publish" if succeeded or transported else "publish-failed"
    write_json(path, {"schemaVersion": 1, "artifactKind": "container-images", "status": status, "version": identity["version"], "source": identity["registry"], "sourceSha": identity["sourceSha"], "setSha256": identity["setSha256"], "push": push, "images": outcomes, "pushed": [item["image"] for item in (transported if push else succeeded)], "failed": [{"package": item["image"], "exitCode": item["exitCode"], "error": item["error"]} for item in failed], "notAttempted": [], "recovery": f"Dispatch Recover Partial Release from main with version {identity['version']} and release-run-id {identity['run']['GITHUB_RUN_ID']}; retained bytes and production approval are required."})


def validate_archives(output, evidence, identity, validator):
    receipt = read_json(evidence / "archive-compatibility.json")
    if receipt.get("result") != "pass" or receipt.get("setSha256") != identity["setSha256"] or [item["name"] for item in receipt.get("archives", [])] != [image["name"] for image in CANONICAL_IMAGES]:
        raise ValueError("All five same-source deployment and OCI archive receipts are required.")
    graphs = {}
    for image, record in zip(CANONICAL_IMAGES, receipt["archives"]):
        if (record["filename"] != image["name"] + ".tar.gz" or record["ociArchive"] != image["name"] + ".oci.tar.gz"
                or record["sourceSha"] != identity["sourceSha"] or record["setSha256"] != identity["setSha256"]
                or sha256(output / record["filename"]) != record["sha256"] or sha256(output / record["ociArchive"]) != record["ociSha256"]):
            raise ValueError("Release archive changed after preparation.")
        graph = archive_graph(output / record["ociArchive"], validator, labels_for(validator, identity))
        if graph["index_digest"] != record["indexDigest"]:
            raise ValueError("OCI archive index identity changed.")
        parity, diagnostics = runtime_config_parity(deployment_config(output / record["filename"]), graph)
        if diagnostics["mismatches"] or record.get("runtimeConfig") != parity or set(parity["children"].values()) != {parity["deploymentSha256"]}:
            raise ValueError("Runtime config parity record is missing or differs from the prepared archives.")
        graphs[image["name"]] = graph
    return graphs


def validate_authorization(evidence, identity):
    authorization = read_json(evidence / "authorization.json")
    if (authorization.get("result") != "pass" or authorization.get("mode") != "preemptive-basic"
            or authorization.get("registry") != identity["registry"] or authorization.get("cleanup") != "pass"
            or authorization.get("repositories") != [image["repository"] for image in CANONICAL_IMAGES]
            or authorization.get("pushProven") is not False):
        raise ValueError("Complete redacted registry authorization and successful cleanup are required before release writes.")
    return authorization


def materialize_oci_archive(archive_path, destination):
    """Unpack validated OCI bytes without skopeo's rootless chown of tar owners."""
    destination.mkdir()
    with tarfile.open(archive_path, "r:*") as archive:
        for member in archive:
            name = member.name.removeprefix("./")
            relative = PurePosixPath(name)
            if not name or name == ".":
                continue
            if relative.is_absolute() or ".." in relative.parts or not (member.isfile() or member.isdir()):
                raise ValueError(f"OCI archive contains an unsafe member: {member.name}")
            target = destination.joinpath(*relative.parts)
            if member.isdir():
                target.mkdir(parents=True, exist_ok=True)
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                with archive.extractfile(member) as source, target.open("xb") as result:
                    shutil.copyfileobj(source, result)


def publish_archives(output, evidence, shared, identity, validator, skopeo):
    graphs = validate_archives(output, evidence, identity, validator)
    validate_authorization(evidence, identity)
    shared_phase(shared, evidence, identity, "container")
    preflight = load_shared(shared)[0]
    username, key = os.environ.get("HEXALITH_ZOT_USERNAME", ""), os.environ.get("HEXALITH_ZOT_API_KEY", "")
    if not username or not key:
        raise ValueError("Registry credentials are required; owner: Jérôme Piquot; consequence: publication blocked; reopen: authorized credentialed validation.")
    outcomes = []
    with tempfile.TemporaryDirectory(prefix="memories-registry-") as temporary:
        auth = Path(temporary) / "config.json"
        write_json(auth, {"auths": {identity["registry"]: {"auth": base64.b64encode(f"{username}:{key}".encode()).decode()}}})
        auth.chmod(0o600)
        # Docker's digest-bound read-only health pulls use the same scoped credentials.
        previous_docker_config = os.environ.get("DOCKER_CONFIG")
        os.environ["DOCKER_CONFIG"] = temporary
        try:
            for image in CANONICAL_IMAGES:
                member = evidence / image["name"]
                reference = f"{identity['registry']}/{image['repository']}:{identity['version']}"
                transported = False
                try:
                    # Recheck each destination immediately before its copy; an occupied or
                    # ambiguous tag can never be reconciled by the current publication path.
                    absence = preflight.validate_container_absence(identity["version"], [f"{identity['registry']}/{image['repository']}"], preflight.destination_probe(username, key))
                    write_json(member / "destination-before-copy.json", absence)
                    layout = Path(temporary) / image["name"]
                    materialize_oci_archive(output / (image["name"] + ".oci.tar.gz"), layout)
                    run([skopeo, "copy", "--all", "--preserve-digests", "--authfile", auth, f"oci:{layout}", f"docker://{reference}"], member / "transport.log")
                    transported = True
                    remote = validator.validate_registry(reference, username, key, labels_for(validator, identity))
                    if remote["index_digest"] != graphs[image["name"]]["index_digest"]:
                        raise ValueError("Registry transport changed the prepared OCI index digest.")
                    validator.write_evidence(member, reference, remote)
                    run([sys.executable, Path(__file__).with_name("verify-container-health.py"), "--name", image["name"], "--image", reference,
                         "--shared-directory", shared, "--evidence-directory", member, "--set-sha256", identity["setSha256"]], member / "health-run.log")
                    outcomes.append(outcome(image, identity, "succeeded", "pushed-and-verified", transported=True))
                except Exception as error:
                    outcomes.append(outcome(image, identity, "failed", "publication-or-verification-failed", redact(error), transported))
        finally:
            if previous_docker_config is None:
                os.environ.pop("DOCKER_CONFIG", None)
            else:
                os.environ["DOCKER_CONFIG"] = previous_docker_config
    write_summary(output / "publish-summary.json", identity, outcomes, True)
    if any(item["status"] != "succeeded" for item in outcomes):
        raise ValueError("Container publication or verification failed; owner: Jérôme Piquot; consequence: no registry-success credit; reopen: repair authorization/health and reconcile immutable evidence.")


def verify_frozen_identity(identity):
    """Verify a retained identity offline, without pretending this is the release run."""
    if (identity.get("schemaVersion") != "hexalith.memories.release-set.v1"
            or identity.get("buildsSha") not in RECOVERY_BUILDS_SHAS
            or identity.get("repository") != "Hexalith/Hexalith.Memories"
            or identity.get("images") != CANONICAL_IMAGES or identity.get("platforms") != PLATFORMS
            or not re.fullmatch(r"[0-9a-f]{40}", str(identity.get("sourceSha")))
            or identity.get("workflowSha") != identity["sourceSha"]
            or identity.get("workflow") != "Hexalith/Hexalith.Memories/.github/workflows/release.yml@refs/heads/main"
            or identity.get("setSha256") != hashlib.sha256(canonical({key: value for key, value in identity.items() if key != "setSha256"})).hexdigest()):
        raise ValueError("Frozen identity, setSha256 or owner-approved recovery Builds SHA is invalid.")
    if (not re.fullmatch(r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z.-]+)?", str(identity.get("version")))
            or not re.fullmatch(r"[a-z0-9]+(?:[a-z0-9.-]*[a-z0-9])?(?::[0-9]+)?", str(identity.get("registry")))
            or any(not re.fullmatch(r"[1-9][0-9]*", str(identity.get("run", {}).get(name))) for name in ("GITHUB_RUN_ID", "GITHUB_RUN_ATTEMPT"))):
        raise ValueError("Frozen version, registry or failed-run identity is invalid.")
    expected_helpers = {f"Github/publish-containers/{name}" for name in shared_files_for(identity.get("buildsSha"))} | {"Props/ContainerProvenance.targets"}
    if (set(identity.get("helpers", {})) != expected_helpers
            or any(not re.fullmatch(r"[0-9a-f]{64}", str(value)) for value in identity["helpers"].values())):
        raise ValueError("Frozen identity does not record the complete helper hashes.")
    return identity


def package_entries(path):
    with zipfile.ZipFile(path) as package:
        names = package.namelist()
        if len(set(names)) != len(names):
            raise ValueError("NuGet package contains duplicate zip entries.")
        return {name: package.read(name) for name in names if name != ".signature.p7s"}


def package_metadata(path):
    entries = package_entries(path)
    specs = [data for name, data in entries.items() if name.lower().endswith(".nuspec")]
    if len(specs) != 1:
        raise ValueError("Package must contain exactly one nuspec.")
    metadata = ET.fromstring(specs[0]).find("{*}metadata")
    return metadata.findtext("{*}id"), metadata.findtext("{*}version")


class NoPackageRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, message, headers, new_url):
        return None


def open_package(url):
    # Preserve raw status meaning: a redirect is ambiguous, never an absent or
    # identical package at the requested flat-container destination.
    return urllib.request.build_opener(NoPackageRedirect()).open(url, timeout=30)


def write_package_identity(root, directory, identity_path):
    identity = read_json(identity_path)
    inventory = read_json(root / "tools/release-packages.json")["packages"]
    ids = [item["id"] for item in inventory]
    if len(ids) != 10 or len(set(ids)) != 10:
        raise ValueError("Package identity requires the exact ten-package manifest.")
    records = []
    for path in sorted(directory.glob("*.nupkg")):
        package_id, version = package_metadata(path)
        if package_id not in ids or version != identity["version"] or path.name != f"{package_id}.{version}.nupkg":
            raise ValueError("Prepared package metadata/version differs from the release inventory.")
        records.append({"id": package_id, "file": path.name, "sha256": sha256(path), "size": path.stat().st_size})
    if sorted(item["id"] for item in records) != sorted(ids):
        raise ValueError("Prepared package IDs differ from the ten-package manifest.")
    document = {key: identity[key] for key in ("version", "sourceSha", "setSha256")}
    document["packages"] = records
    write_json(root / "artifacts/release/package-identity.json", document)
    return document


def validate_package_identity(identity, inventory_path, identity_path, directory):
    """Prove the receipt against actual pre-publication package bytes and metadata."""
    inventory = read_json(inventory_path)["packages"]
    ids = [item["id"] for item in inventory]
    approved = [item["id"] for item in read_json(Path(__file__).with_name("release-packages.json"))["packages"]]
    document = read_json(identity_path)
    records = document.get("packages", [])
    if (len(ids) != 10 or sorted(ids) != sorted(approved)
            or any(document.get(key) != identity[key] for key in ("version", "sourceSha", "setSha256"))
            or len(records) != 10 or sorted(item["id"] for item in records) != sorted(ids)
            or {path.name for path in directory.glob("*.nupkg")} != {item["file"] for item in records}):
        raise ValueError("Prepared package identity differs from the exact ten-package release set.")
    for item in records:
        if item["file"] != f"{item['id']}.{identity['version']}.nupkg":
            raise ValueError("Prepared package identity file is not canonical.")
        path = directory / item["file"]
        if (path.is_symlink() or path.stat().st_size != item["size"] or sha256(path) != item["sha256"]
                or package_metadata(path) != (item["id"], identity["version"])):
            raise ValueError(f"Prepared package bytes or metadata differ: {item['id']}")
    return document


def validate_preparation(root, evidence, container_directory, package_directory, identity_path):
    """Validate preparation receipts without executing helpers or contacting destinations."""
    identity = verify_frozen_identity(read_json(evidence / "identity.json"))
    manifest_path = root / "tools/release-containers.json"
    if (read_json(manifest_path) != {"schemaVersion": "hexalith.memories.container-publication.v1", "buildsSha": identity["buildsSha"], "platforms": PLATFORMS, "images": CANONICAL_IMAGES}
            or sha256(manifest_path) != identity["manifestSha256"]):
        raise ValueError("Preparation manifest differs from the frozen identity.")
    validate_package_identity(identity, root / "tools/release-packages.json", identity_path, package_directory)
    receipt = read_json(evidence / "archive-compatibility.json")
    if (receipt.get("result") != "pass" or receipt.get("setSha256") != identity["setSha256"]
            or [item["name"] for item in receipt.get("archives", [])] != [image["name"] for image in CANONICAL_IMAGES]):
        raise ValueError("Preparation archive-compatibility receipt failed or belongs to another release set.")
    for image, record in zip(CANONICAL_IMAGES, receipt["archives"]):
        if (record.get("sourceSha") != identity["sourceSha"] or record.get("setSha256") != identity["setSha256"]
                or record.get("filename") != image["name"] + ".tar.gz" or record.get("ociArchive") != image["name"] + ".oci.tar.gz"
                or sha256(container_directory / record["filename"]) != record.get("sha256")
                or sha256(container_directory / record["ociArchive"]) != record.get("ociSha256")):
            raise ValueError(f"Preparation archive bytes/bindings differ: {image['name']}")
    return identity


def git_bytes(source, *arguments):
    """Run git without text decoding, which would normalize the archived line endings."""
    try:
        return subprocess.run(["git", "-C", str(source), *arguments], capture_output=True, check=False, timeout=COMMAND_TIMEOUT_SECONDS)
    except subprocess.TimeoutExpired:
        raise ValueError(f"git {arguments[0]} timed out after {COMMAND_TIMEOUT_SECONDS} s.") from None


def require_own_checkout(source):
    """Refuse a source Git would resolve through an enclosing repository, or cannot read at all."""
    top = git_bytes(source, "rev-parse", "--show-toplevel")
    if top.returncode:
        raise ValueError(f"Git cannot read recovery source {source}: " + redact(captured_text(top.stderr))[-2000:])
    if Path(os.fsdecode(top.stdout.strip())).resolve() != Path(source).resolve():
        raise ValueError(f"Recovery source {source} is not its own Git checkout; Git would read an enclosing repository instead. Check out the tagged Memories repository itself.")


def verify_tagged_deployment(source, tag_sha, identity, deployment):
    """Run only the tagged renderer and its tagged kustomize inputs, in private scratch.

    `git archive` applies the archived tree's own `.gitattributes`, so the inputs carry the
    bytes the release runner's checkout rendered (CRLF `redis-stack.conf`), which kustomize
    hashes into ConfigMap names. Blob bytes (`git show`) are LF and can never match.
    """
    renderer = "tools/render-production-deployment.ps1"
    require_own_checkout(source)
    commit = git_bytes(source, "cat-file", "-e", f"{tag_sha}^{{commit}}")
    if commit.returncode:
        raise ValueError(f"Tag commit {tag_sha} is absent from {source}; recovery needs the full tagged history (actions/checkout fetch-depth: 0): " + redact(captured_text(commit.stderr))[-2000:])
    archive = git_bytes(source, "archive", "--format=tar", tag_sha, "--", renderer, "deploy/kubernetes")
    if archive.returncode:
        raise ValueError("Tagged production renderer input is missing: " + redact(captured_text(archive.stderr))[-2000:])
    with tempfile.TemporaryDirectory(prefix="memories-tagged-deployment-") as temporary, tarfile.open(fileobj=io.BytesIO(archive.stdout), mode="r:") as members:
        root = Path(temporary)
        names = set()
        # tarfile consumes git's pax global header (the commit ID) itself; it is never a member.
        for member in members:
            relative = PurePosixPath(member.name)
            if relative.is_absolute() or ".." in relative.parts or "\\" in member.name:
                raise ValueError("Tagged renderer input contains an unsafe path.")
            if member.isdir():
                continue
            if not member.isreg():
                raise ValueError(f"Tagged renderer input is not a regular file: {member.name}")
            target = root / member.name
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(members.extractfile(member).read())
            names.add(relative.as_posix())
        if not {renderer, "deploy/kubernetes/overlays/production/kustomization.yaml"} <= names:
            raise ValueError("Tagged production renderer input is missing.")
        rendered = root / "production.yaml"
        command = ["pwsh", "-NoLogo", "-NoProfile", "-File", root / renderer, "-Version", identity["version"], "-OutputPath", rendered]
        for image, parameter in zip(CANONICAL_IMAGES[:4], ("ServerImage", "McpImage", "AccessTelemetryImage", "AccessTelemetryClockImage")):
            command += ["-" + parameter, f"{identity['registry']}/{image['repository']}:{identity['version']}"]
        completed = subprocess.run([str(value) for value in command], capture_output=True, text=True, check=False, timeout=COMMAND_TIMEOUT_SECONDS)
        if completed.returncode or not rendered.is_file() or rendered.read_bytes() != deployment.read_bytes():
            raise ValueError("Retained deployment differs from the tagged renderer and frozen hosted mappings: " + redact(completed.stderr))


def verify_bundle(bundle, destination, source, version, release_run_id, repository):
    """Check the external receipt and every retained byte before opening any destination."""
    require_own_checkout(source)
    try:
        tag_sha = run(["git", "-C", source, "rev-list", "-n", "1", f"refs/tags/v{version}"]).strip()
    except ValueError as error:
        raise ValueError(f"Recovery tag v{version} cannot be resolved in {source}; recovery needs the full tagged history (actions/checkout fetch-depth: 0). {error}") from None
    receipt = read_json(bundle.with_suffix(bundle.suffix + ".sha256.json"))
    if receipt.get("path") != bundle.name or receipt.get("sha256") != sha256(bundle) or receipt.get("result") != "recoverable":
        raise ValueError("Recovery bundle receipt is missing, unrecoverable or differs from the retained bytes.")
    destination.mkdir(parents=True, exist_ok=True)
    with tarfile.open(bundle, "r:*") as archive:
        members = archive.getmembers()
        names = [member.name for member in members]
        if len(names) != len(set(names)) or "recovery-manifest.json" not in names:
            raise ValueError("Recovery bundle has duplicate paths or no recovery manifest.")
        for member in members:
            relative = PurePosixPath(member.name)
            if not member.isfile() or relative.is_absolute() or ".." in relative.parts or "\\" in member.name or relative.as_posix() != member.name:
                raise ValueError("Recovery bundle contains unsafe paths or links.")
        manifest = json.load(archive.extractfile("recovery-manifest.json"), object_pairs_hook=strict_object)
        entries = manifest.get("files", [])
        if (manifest.get("schemaVersion") != "hexalith.memories.release-recovery.v1" or manifest.get("result") != "recoverable"
                or len({item["path"] for item in entries}) != len(entries)
                or set(names) != {item["path"] for item in entries} | {"recovery-manifest.json"}):
            raise ValueError("Recovery bundle inventory is incomplete or unrecoverable.")
        for entry in entries:
            target = destination / entry["path"]
            target.parent.mkdir(parents=True, exist_ok=True)
            with archive.extractfile(entry["path"]) as incoming, target.open("wb") as outgoing:
                shutil.copyfileobj(incoming, outgoing)
            if target.stat().st_size != entry["size"] or sha256(target) != entry["sha256"]:
                raise ValueError(f"Recovery bundle file hash differs: {entry['path']}")
    identity = verify_frozen_identity(read_json(destination / "artifacts/release/container-evidence/identity.json"))
    bindings = {key: identity[key] for key in ("version", "sourceSha", "setSha256")}
    if any(manifest.get(key) != value for key, value in bindings.items()) or identity["version"] != version or identity["repository"] != repository:
        raise ValueError("Bundle version/source/set bindings differ from the requested recovery.")
    if str(identity["run"]["GITHUB_RUN_ID"]) != str(release_run_id):
        raise ValueError("Bundle identity belongs to another failed release run.")
    if tag_sha != identity["sourceSha"] or run(["git", "-C", source, "rev-parse", "HEAD"]).strip() != tag_sha:
        raise ValueError("Trusted tag commit, checked-out source and failed-run identity differ.")
    failed = json.loads(run(["gh", "api", f"repos/{repository}/actions/runs/{release_run_id}"]), object_pairs_hook=strict_object)
    if (failed.get("id") != int(release_run_id) or failed.get("head_sha") != tag_sha or failed.get("path") != ".github/workflows/release.yml"
            or failed.get("conclusion") != "failure" or failed.get("status") != "completed"
            or failed.get("run_attempt") != int(identity["run"]["GITHUB_RUN_ATTEMPT"])
            or failed.get("repository", {}).get("full_name") != repository):
        raise ValueError("Failed run must be completed release.yml with the exact frozen run attempt, tag head SHA and failed conclusion.")
    for name in ("release-containers.json", "release-packages.json"):
        tagged = run(["git", "-C", source, "show", f"{tag_sha}:tools/{name}"])
        if json.loads(tagged, object_pairs_hook=strict_object) != read_json(destination / "tools" / name):
            raise ValueError(f"Tagged manifest differs from retained {name}.")
    container_manifest = read_json(destination / "tools/release-containers.json")
    if (container_manifest != {"schemaVersion": "hexalith.memories.container-publication.v1", "buildsSha": identity["buildsSha"], "platforms": PLATFORMS, "images": CANONICAL_IMAGES}
            or sha256(destination / "tools/release-containers.json") != identity["manifestSha256"]):
        raise ValueError("Tagged five-image manifest differs from the frozen identity.")
    validate_preparation(destination, destination / "artifacts/release/container-evidence", destination / "artifacts/containers/release", destination / "artifacts/packages/release", destination / "artifacts/release/package-identity.json")
    packages = read_json(destination / "artifacts/release/package-identity.json")
    deployment = destination / "artifacts/deployment/hexalith-memories-production.yaml"
    verify_tagged_deployment(source, tag_sha, identity, deployment)
    return identity, packages, manifest


def classify_package(item, identity, bundle_root, nuget_base):
    result = {"kind": "package", "name": item["id"], "file": item["file"]}
    url = f"{nuget_base.rstrip('/')}/{item['id'].lower()}/{identity['version'].lower()}/{item['id'].lower()}.{identity['version'].lower()}.nupkg"
    try:
        with open_package(url) as response:
            if response.status != 200:
                raise ValueError(f"HTTP {response.status}")
            with tempfile.TemporaryFile() as served:
                shutil.copyfileobj(response, served)
                served.seek(0)
                same = package_entries(served) == package_entries(bundle_root / "artifacts/packages/release" / item["file"])
        result["classification"] = "present-identical" if same else "present-different"
    except urllib.error.HTTPError as error:
        result["classification"] = "absent" if error.code == 404 else "ambiguous"
        result["diagnostic"] = f"HTTP {error.code}"
        error.close()
    except Exception as error:
        result.update(classification="ambiguous", diagnostic=redact(error))
    return result


def await_package_validation(item, identity, bundle_root, arguments, evidence):
    """Wait read-only for nuget.org validation; never repeat an immutable push."""
    deadline = time.monotonic() + getattr(arguments, "nuget_validation_timeout", 600)
    observations = []
    while True:
        observed = classify_package(item, identity, bundle_root, arguments.nuget_base_uri)
        observations.append(observed)
        write_json(evidence / "packages" / (item["id"] + ".validation.json"), {"observations": observations})
        if observed["classification"] != "absent" or time.monotonic() >= deadline:
            return observed
        time.sleep(min(10, max(0, deadline - time.monotonic())))


def classify_image(image, identity, graphs, preflight, validator):
    reference = f"{identity['registry']}/{image['repository']}:{identity['version']}"
    result = {"kind": "image", "name": image["name"], "image": reference, "indexDigest": graphs[image["name"]]["index_digest"]}
    try:
        username, key = os.environ.get("HEXALITH_ZOT_USERNAME", ""), os.environ.get("HEXALITH_ZOT_API_KEY", "")
        status = preflight.destination_probe(username, key)("container", f"{identity['registry']}/{image['repository']}", identity["version"])
        if status == 404:
            result["classification"] = "absent"
        elif status == 200:
            graph = validator.validate_registry(reference, username, key, labels_for(validator, identity))
            result["classification"] = "present-identical" if graph["index_digest"] == result["indexDigest"] else "present-different"
        else:
            result.update(classification="ambiguous", diagnostic=f"HTTP {status}")
    except Exception as error:
        result.update(classification="ambiguous", diagnostic=redact(error))
    return result


def recovery_plan(bundle_root, evidence, identity, packages, graphs, preflight, validator, nuget_base, refuse=True):
    members = [classify_package(item, identity, bundle_root, nuget_base) for item in packages["packages"]]
    members += [classify_image(image, identity, graphs, preflight, validator) for image in CANONICAL_IMAGES]
    plan = {"artifactKind": "partial-release-recovery-plan", **{key: identity[key] for key in ("version", "sourceSha", "setSha256")}, "members": members}
    plan["result"] = "pass" if len(members) == 15 and all(item["classification"] in ("absent", "present-identical") for item in members) else "refused"
    plan["absentPackages"] = sum(item["kind"] == "package" and item["classification"] == "absent" for item in members)
    write_json(evidence / "recovery-plan.json", plan)
    if plan["result"] != "pass" and refuse:
        raise ValueError("Recovery plan refuses: " + ", ".join(f"{item['name']}={item['classification']}" for item in members if item["classification"] not in ("absent", "present-identical")))
    return plan


HEALTH_RECORD_FILES = ("oci-validation.json", "index.raw", "child-linux-amd64.manifest.raw", "child-linux-amd64.config.raw", "child-linux-arm64.manifest.raw", "child-linux-arm64.config.raw", "health.json", "health-amd64.log", "health-arm64.log", "health-run.log", "smoke-preflight.log")
IDENTITY_RECORD_FILES = ("artifacts/release/container-evidence/identity.json", "artifacts/release/package-identity.json", "tools/release-containers.json", "tools/release-packages.json")


def health_matches(health, image, graph, identity, validation_sha256):
    return (health.get("result") == "pass" and health.get("setSha256") == identity["setSha256"]
            and health.get("image") == f"{identity['registry']}/{image['repository']}:{identity['version']}"
            and health.get("indexDigest") == graph["index_digest"] and health.get("contract") == image["health"]
            and health.get("ociValidationSha256") == validation_sha256
            and health.get("platforms") == [{"platform": child["platform"], "digest": child["digest"], "outcome": "pass", "cleanup": "pass"} for child in graph["children"]]
            and health.get("environment") == "Development" and health.get("lifecycleWritesEnabled") is False
            and health.get("endpoint") == ("/alive" if image["health"] == "liveness" else "/ready"))


def passing_health(member, image, graph, identity):
    try:
        return (all((member / name).is_file() and not (member / name).is_symlink() for name in HEALTH_RECORD_FILES)
                and health_matches(read_json(member / "health.json"), image, graph, identity, sha256(member / "oci-validation.json")))
    except (OSError, ValueError):
        return False


def save_progress(evidence, progress):
    write_json(evidence / "recovery-progress.json", {"artifactKind": "partial-release-recovery-progress", "sideEffects": progress})


def recover_images(bundle_root, evidence, shared, identity, graphs, preflight, validator, skopeo, progress):
    outcomes = []
    with tempfile.TemporaryDirectory(prefix="memories-recovery-auth-") as temporary:
        auth = Path(temporary) / "config.json"
        username, key = os.environ.get("HEXALITH_ZOT_USERNAME", ""), os.environ.get("HEXALITH_ZOT_API_KEY", "")
        write_json(auth, {"auths": {identity["registry"]: {"auth": base64.b64encode(f"{username}:{key}".encode()).decode()}}})
        auth.chmod(0o600)
        previous = {name: os.environ.get(name) for name in ("DOCKER_CONFIG", "MEMORIES_RECOVERY_IDENTITY")}
        os.environ.update(DOCKER_CONFIG=temporary, MEMORIES_RECOVERY_IDENTITY=str(bundle_root / "artifacts/release/container-evidence/identity.json"))
        try:
            for image in CANONICAL_IMAGES:
                current = classify_image(image, identity, graphs, preflight, validator)
                write_json(evidence / image["name"] / "destination-before-copy.json", current)
                if current["classification"] not in ("absent", "present-identical"):
                    raise ValueError(f"Immediate image recheck refuses {image['name']}: {current['classification']}")
                if current["classification"] == "absent":
                    layout = Path(temporary) / image["name"]
                    materialize_oci_archive(bundle_root / "artifacts/containers/release" / (image["name"] + ".oci.tar.gz"), layout)
                    # Materialization can be lengthy. Reclassify with authentication at
                    # the actual copy boundary; another writer may have occupied the tag.
                    # Residual registry atomicity remains deferred; this is a read recheck.
                    current = classify_image(image, identity, graphs, preflight, validator)
                    write_json(evidence / image["name"] / "destination-before-copy.json", current)
                    if current["classification"] not in ("absent", "present-identical"):
                        raise ValueError(f"Immediate image recheck after materialization refuses {image['name']}: {current['classification']}")
                    if current["classification"] == "absent":
                        attempt = dict(current, **{key: identity[key] for key in ("version", "sourceSha", "setSha256")}, operation="image-copy", attempted=True, verified=False, uncertain=True)
                        progress.append(attempt)
                        save_progress(evidence, progress)
                        try:
                            run([skopeo, "copy", "--all", "--preserve-digests", "--authfile", auth, f"oci:{layout}", f"docker://{current['image']}"], evidence / image["name"] / "transport.log")
                        except Exception as error:
                            attempt["error"] = redact(error)
                            raise
                        finally:
                            observed = classify_image(image, identity, graphs, preflight, validator)
                            attempt.update(classification=observed["classification"], verified=observed["classification"] == "present-identical", uncertain=observed["classification"] not in ("absent", "present-identical"), observation=observed)
                            save_progress(evidence, progress)
                if classify_image(image, identity, graphs, preflight, validator)["classification"] != "present-identical":
                    raise ValueError(f"Post-copy image validation refuses {image['name']}.")
                member = evidence / image["name"]
                retained = bundle_root / "artifacts/release/container-evidence" / image["name"]
                graph = validator.validate_registry(current["image"], username, key, labels_for(validator, identity))
                validator.write_evidence(member, current["image"], graph)
                if passing_health(retained, image, graphs[image["name"]], identity):
                    for name in ("health.json", "health-amd64.log", "health-arm64.log", "health-run.log", "smoke-preflight.log"):
                        if (retained / name).is_file():
                            shutil.copyfile(retained / name, member / name)
                elif not passing_health(member, image, graphs[image["name"]], identity):
                    run([sys.executable, Path(__file__).with_name("verify-container-health.py"), "--name", image["name"], "--image", current["image"], "--shared-directory", shared, "--evidence-directory", member, "--set-sha256", identity["setSha256"]], member / "health-run.log")
                if not passing_health(member, image, graph, identity):
                    raise ValueError(f"Digest-bound health failed: {image['name']}")
                outcomes.append(dict(current, classification="present-identical"))
        finally:
            for name, value in previous.items():
                if value is None:
                    os.environ.pop(name, None)
                else:
                    os.environ[name] = value
    return outcomes


PACKET_NAME = "hexalith-memories-release-evidence.tar.gz"


def release_snapshot(identity, evidence, assets):
    """Inspect/download first: an occupied asset must never be overwritten or hidden."""
    reference = f"v{identity['version']}"
    command = ["gh", "release", "view", reference, "--repo", identity["repository"], "--json", "tagName,isDraft,isPrerelease,assets"]
    result = subprocess.run(command, capture_output=True, text=True, check=False, timeout=60)
    if result.returncode:
        if re.search(r"release not found|HTTP 404", result.stdout + result.stderr, re.IGNORECASE):
            return None, {}
        raise ValueError("GitHub Release probe is ambiguous: " + redact(result.stderr))
    release = json.loads(result.stdout, object_pairs_hook=strict_object)
    names = [item["name"] for item in release["assets"]]
    if release["tagName"] != reference or release["isPrerelease"] or len(names) != len(set(names)) or set(names) - set(assets):
        raise ValueError("Existing Release contains unexpected assets or conflicting release identity.")
    with tempfile.TemporaryDirectory(prefix="memories-release-download-") as temporary:
        directory = Path(temporary)
        if names:
            run(["gh", "release", "download", reference, "--repo", identity["repository"], "--dir", directory])
        if {path.name for path in directory.iterdir()} != set(names):
            raise ValueError("Release asset download inventory differs from the GitHub snapshot.")
        downloaded = {path.name: path.read_bytes() for path in directory.iterdir()}
        for name in names:
            expected = assets[name]
            if expected is not None and downloaded[name] != expected.read_bytes():
                raise ValueError(f"Existing Release asset did not match retained bytes: {name}")
    write_json(evidence / "release-before-write.json", release)
    return release, downloaded


def packet_files(data, manifest_name):
    """Read an immutable packet with a safe, exhaustive, hash-bound inventory."""
    with tarfile.open(fileobj=io.BytesIO(data), mode="r:gz") as packet:
        members = packet.getmembers()
        names = [member.name for member in members]
        if len(names) != len(set(names)) or manifest_name not in names:
            raise ValueError("Evidence packet has a duplicate or missing manifest.")
        for member in members:
            name = PurePosixPath(member.name)
            if not member.isfile() or name.is_absolute() or ".." in name.parts or "\\" in member.name or name.as_posix() != member.name:
                raise ValueError("Evidence packet has an unsafe record path.")
        document = json.load(packet.extractfile(manifest_name), object_pairs_hook=strict_object)
        files = {name: packet.extractfile(name).read() for name in names if name != manifest_name}
    inventory = document.get("files", [])
    if len(inventory) != len(files) or {item["path"] for item in inventory} != set(files):
        raise ValueError("Evidence packet inventory differs from its raw records.")
    for item in inventory:
        data = files[item["path"]]
        if len(data) != item["size"] or hashlib.sha256(data).hexdigest() != item["sha256"]:
            raise ValueError("Evidence packet record hash differs: " + item["path"])
    return document, files


def ordinary_evidence_files(evidence, container_directory, package_directory, summary_directory, deployment_path):
    """Keep the ordinary producer and its recovery consumer on one required inventory."""
    member_files = ("index.raw", "child-linux-amd64.manifest.raw", "child-linux-amd64.config.raw", "child-linux-arm64.manifest.raw", "child-linux-arm64.config.raw", "oci-validation.json", "health.json", "health-amd64.log", "health-arm64.log", "health-run.log", "transport.log", "destination-before-copy.json", "build-deployment.log", "build-oci.log", "runtime-config-parity.json", "smoke-preflight.log", "local/oci-validation.json", "local/index.raw", "local/child-linux-amd64.manifest.raw", "local/child-linux-amd64.config.raw", "local/child-linux-arm64.manifest.raw", "local/child-linux-arm64.config.raw")
    paths = [evidence / name for name in ("identity.json", "authorization.json", "archive-compatibility.json", "preflight-verify.log", "preflight-publish.log", "preflight-container.log", "preflight/publication-identity.json", "preflight/publication-preflight.verify.json", "preflight/publication-preflight.publish.json", "preflight/publication-preflight.container.json")]
    for image in CANONICAL_IMAGES:
        paths += [evidence / image["name"] / name for name in member_files]
    paths += [Path("tools/release-containers.json"), Path("tools/release-packages.json"), summary_directory / "package-identity.json", container_directory / "build-summary.json", container_directory / "publish-summary.json", package_directory / "publish-summary.json", summary_directory / "publish-summary.json", deployment_path]
    return paths


def validate_ordinary_packet(data, receipt_data, identity, bundle_root):
    receipt = json.loads(receipt_data, object_pairs_hook=strict_object)
    if receipt != {"path": PACKET_NAME, "sha256": hashlib.sha256(data).hexdigest(), "result": "pass"}:
        raise ValueError("Retained ordinary packet receipt differs from its immutable bytes.")
    document, files = packet_files(data, "evidence-manifest.json")
    if (document.get("schemaVersion") != "hexalith.memories.release-evidence.v1" or document.get("result") != "pass"
            or document.get("identityReadable") is not True
            or any(document.get(key) != identity[key] for key in ("version", "sourceSha", "setSha256"))
            or json.loads(files.get("artifacts/release/container-evidence/identity.json", b"{}"), object_pairs_hook=strict_object) != identity):
        raise ValueError("Retained ordinary evidence packet differs from the release identity.")
    required = {path.as_posix() for path in ordinary_evidence_files(DEFAULT_EVIDENCE_DIRECTORY, Path("artifacts/containers/release"), Path("artifacts/packages/release"), Path("artifacts/release"), Path("artifacts/deployment/hexalith-memories-production.yaml"))}
    required.add("artifacts/containers/release/production-deployment.yaml")
    if not required <= set(files):
        raise ValueError("Retained ordinary evidence packet is incomplete.")
    for name, data in files.items():
        retained = bundle_root / name
        if not retained.is_file() or retained.read_bytes() != data:
            raise ValueError("Ordinary evidence record differs from the retained bundle: " + name)
    return document


def recovery_packet(path, document, evidence, bundle_root):
    """Stable archive headers permit an exact receipt and never require asset replacement."""
    sources = {name: (bundle_root / name).read_bytes() for name in IDENTITY_RECORD_FILES}
    for image in CANONICAL_IMAGES:
        member = evidence / image["name"]
        for name in HEALTH_RECORD_FILES:
            source = member / name
            if not source.is_file() or source.is_symlink():
                raise ValueError(f"Recovery packet lacks raw OCI/health evidence: {image['name']}/{name}")
            sources[f"records/{image['name']}/{name}"] = source.read_bytes()
        for source in member.glob("*.log"):
            sources[f"records/{image['name']}/{source.name}"] = source.read_bytes()
    document = dict(document, files=[{"path": name, "sha256": hashlib.sha256(data).hexdigest(), "size": len(data)} for name, data in sorted(sources.items())])
    data = canonical(document) + b"\n"
    with path.open("wb") as target, gzip.GzipFile(fileobj=target, mode="wb", mtime=0, filename="") as compressed, tarfile.open(fileobj=compressed, mode="w") as packet:
        header = tarfile.TarInfo("recovery-evidence.json")
        header.size = len(data)
        packet.addfile(header, io.BytesIO(data))
        for name, data in sorted(sources.items()):
            header = tarfile.TarInfo(name)
            header.size = len(data)
            packet.addfile(header, io.BytesIO(data))
    write_json(path.with_suffix(path.suffix + ".sha256.json"), {"path": path.name, "sha256": sha256(path), "result": "pass"})


def validate_recovery_packet(data, identity, manifest, graphs, packages, restore_directory=None):
    document, files = packet_files(data, "recovery-evidence.json")
    plan = document.get("plan", {})
    canonical_members = {("package", item["id"]): {"file": item["file"]} for item in packages["packages"]}
    canonical_members.update({("image", image["name"]): {"image": f"{identity['registry']}/{image['repository']}:{identity['version']}",
                             "indexDigest": graphs[image["name"]]["index_digest"]} for image in CANONICAL_IMAGES})
    members = plan.get("members", [])
    if (document.get("artifactKind") != "partial-release-recovery" or document.get("result") != "pass"
            or any(document.get(key) != identity[key] for key in ("version", "sourceSha", "setSha256"))
            or document.get("bundleManifest") != manifest or plan.get("result") != "pass"
            or any(plan.get(key) != identity[key] for key in ("version", "sourceSha", "setSha256"))
            or len(members) != 15 or {(item.get("kind"), item.get("name")) for item in members} != set(canonical_members)
            or any(item.get("classification") not in ("absent", "present-identical")
                   or any(item.get(key) != value for key, value in canonical_members.get((item.get("kind"), item.get("name")), {}).items()) for item in members)):
        raise ValueError("Existing recovery packet differs from the retained release set/bundle or lacks a passing complete plan.")
    retained_hashes = {item["path"]: item["sha256"] for item in manifest["files"]}
    for name in IDENTITY_RECORD_FILES:
        raw = files.get(name)
        if raw is None or hashlib.sha256(raw).hexdigest() != retained_hashes.get(name):
            raise ValueError("Existing recovery packet frozen/package identity record differs: " + name)
    raw_identity = verify_frozen_identity(json.loads(files[IDENTITY_RECORD_FILES[0]], object_pairs_hook=strict_object))
    if raw_identity != identity or json.loads(files[IDENTITY_RECORD_FILES[1]], object_pairs_hook=strict_object) != packages:
        raise ValueError("Existing recovery packet raw identity does not recompute the retained release set.")
    expected = {("package", item["id"]): item["sha256"] for item in packages["packages"]}
    expected.update({("image", image["name"]): graphs[image["name"]]["index_digest"] for image in CANONICAL_IMAGES})
    results = document.get("results", [])
    if (len(results) != 15 or {(item.get("kind"), item.get("name")): item.get("digest") for item in results} != expected
            or any(item.get("classification") != "present-identical" for item in results)):
        raise ValueError("Existing recovery packet does not prove all fifteen expected members.")
    if len(document.get("health", [])) != 5:
        raise ValueError("Existing recovery packet does not prove five digest-bound health records.")
    for image, health in zip(CANONICAL_IMAGES, document["health"]):
        graph = graphs[image["name"]]
        prefix = f"records/{image['name']}/"
        required = {prefix + name for name in HEALTH_RECORD_FILES}
        if not required <= set(files):
            raise ValueError("Existing recovery packet omits referenced raw OCI/health records.")
        validation = json.loads(files[prefix + "oci-validation.json"], object_pairs_hook=strict_object)
        if (validation.get("image") != f"{identity['registry']}/{image['repository']}:{identity['version']}"
                or validation.get("raw_index_file") != "index.raw"
                or any(validation.get(key) != value for key, value in graph.items() if key not in ("children", "index_bytes"))):
            raise ValueError("Existing recovery packet OCI validator metadata differs from the recomputed graph.")
        if (files[prefix + "index.raw"] != graph["index_bytes"] or validation.get("index_digest") != graph["index_digest"]
                or len(validation.get("children", [])) != 2 or json.loads(files[prefix + "health.json"], object_pairs_hook=strict_object) != health
                or health.get("ociValidationSha256") != hashlib.sha256(files[prefix + "oci-validation.json"]).hexdigest()):
            raise ValueError("Existing recovery packet raw validator/health binding differs.")
        for child, recorded in zip(graph["children"], validation["children"]):
            if any(recorded.get(key) != value for key, value in child.items() if key not in ("manifest_bytes", "config_bytes")):
                raise ValueError("Existing recovery packet OCI child metadata differs from the recomputed graph.")
            for kind in ("manifest", "config"):
                name = f"child-linux-{child['platform'].split('/')[1]}.{kind}.raw"
                raw = files.get(prefix + name)
                if (raw != child[kind + "_bytes"] or recorded.get(kind + "_raw_file") != name
                        or recorded.get(kind + "_raw_sha256") != hashlib.sha256(raw).hexdigest()):
                    raise ValueError("Existing recovery packet raw OCI graph differs.")
        if not health_matches(health, image, graph, identity, hashlib.sha256(files[prefix + "oci-validation.json"]).hexdigest()):
            raise ValueError("Existing recovery packet health proof differs from the prepared OCI graph.")
    # Only restore after every identity, raw object and health contract has passed.
    if restore_directory is not None:
        for name, raw in files.items():
            if name.startswith("records/"):
                target = restore_directory / name.removeprefix("records/")
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(raw)
    return document


def current_completion(arguments):
    evidence = arguments.evidence_directory.resolve()
    evidence.mkdir(parents=True, exist_ok=True)
    result = {"artifactKind": "partial-release-recovery", "status": "failed", "version": arguments.version}
    progress = read_json(evidence / "recovery-progress.json").get("sideEffects", []) if (evidence / "recovery-progress.json").is_file() else []
    result["sideEffects"] = progress
    try:
        if not arguments.release_run_id or not arguments.recovery_bundle:
            raise ValueError("Current recovery requires a retained recovery bundle and release run ID; earlier failures have no recoverable bytes.")
        # A private extraction is verified afresh on every invocation; prior local files
        # cannot stand in for an expired or changed failed-run artifact.
        with tempfile.TemporaryDirectory(prefix="memories-recovery-bundle-") as temporary:
            bundle_root = Path(temporary)
            identity, packages, manifest = verify_bundle(arguments.recovery_bundle.resolve(), bundle_root, arguments.source_root.resolve(), arguments.version, arguments.release_run_id, arguments.repository)
            progress[:] = [attempt for attempt in progress if all(attempt.get(key) == identity[key] for key in ("version", "sourceSha", "setSha256"))]
            if arguments.registry != identity["registry"]:
                raise ValueError("Requested registry differs from the frozen release registry.")
            if getattr(arguments, "release_run_attempt", None) is not None and str(arguments.release_run_attempt) != str(identity["run"]["GITHUB_RUN_ATTEMPT"]):
                raise ValueError("Dispatched failed-run attempt differs from the frozen release identity.")
            if arguments.phase == "recovery-metadata":
                if os.environ.get("GITHUB_OUTPUT"):
                    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
                        output.write(f"builds-sha={identity['buildsSha']}\n")
                result["status"] = "validated"
                return
            preflight, validator, _, _ = load_shared(arguments.shared_directory, recovery_identity=identity)
            graphs = validate_archives(bundle_root / "artifacts/containers/release", bundle_root / "artifacts/release/container-evidence", identity, validator)
            plan = recovery_plan(bundle_root, evidence / "plans" / arguments.phase, identity, packages, graphs, preflight, validator, arguments.nuget_base_uri, refuse=False)
            # Keep the pre-first-write plan across the workflow's image/package phases;
            # each fresh read-only recheck has its own retained record.
            initial_plan_path = evidence / "recovery-plan.json"
            initial_plan = read_json(initial_plan_path) if initial_plan_path.exists() else None
            if (initial_plan is None or initial_plan.get("result") != "pass"
                    or any(initial_plan.get(key) != identity[key] for key in ("version", "sourceSha", "setSha256"))):
                initial_plan = plan
                write_json(initial_plan_path, initial_plan)
            if plan["result"] != "pass":
                raise ValueError("Recovery plan refuses: " + ", ".join(f"{item['name']}={item['classification']}" for item in plan["members"] if item["classification"] not in ("absent", "present-identical")))
            result.update({key: identity[key] for key in ("version", "sourceSha", "setSha256")})
            result.update(packages=plan["members"][:10], images=plan["members"][10:])
            if arguments.phase == "recovery-plan":
                if os.environ.get("GITHUB_OUTPUT"):
                    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
                        output.write(f"absent-packages={plan['absentPackages']}\nbuilds-sha={identity['buildsSha']}\n")
                result["status"] = "validated"
                return
            assets = {item["file"]: bundle_root / "artifacts/packages/release" / item["file"] for item in packages["packages"]}
            assets["hexalith-memories-production.yaml"] = bundle_root / "artifacts/deployment/hexalith-memories-production.yaml"
            assets.update({PACKET_NAME: None, PACKET_NAME + ".sha256.json": None})
            release, downloaded = release_snapshot(identity, evidence, assets)
            old_packet = None
            ordinary_path = bundle_root / "artifacts/release" / PACKET_NAME
            ordinary_receipt = ordinary_path.with_suffix(ordinary_path.suffix + ".sha256.json")
            occupied = PACKET_NAME in downloaded or PACKET_NAME + ".sha256.json" in downloaded
            ordinary_matches = (occupied and ordinary_path.is_file() and ordinary_receipt.is_file()
                                and all(data == (ordinary_path if name == PACKET_NAME else ordinary_receipt).read_bytes()
                                        for name, data in downloaded.items() if name in (PACKET_NAME, PACKET_NAME + ".sha256.json")))
            if ordinary_matches:
                old_packet = validate_ordinary_packet(ordinary_path.read_bytes(), ordinary_receipt.read_bytes(), identity, bundle_root)
                packet_path = evidence / PACKET_NAME
                shutil.copyfile(ordinary_path, packet_path)
                shutil.copyfile(ordinary_receipt, packet_path.with_suffix(packet_path.suffix + ".sha256.json"))
            elif PACKET_NAME in downloaded:
                old_packet = validate_recovery_packet(downloaded[PACKET_NAME], identity, manifest, graphs, packages, restore_directory=evidence)
                packet_path = evidence / PACKET_NAME
                packet_path.write_bytes(downloaded[PACKET_NAME])
                write_json(packet_path.with_suffix(packet_path.suffix + ".sha256.json"), {"path": PACKET_NAME, "sha256": sha256(packet_path), "result": "pass"})
                if PACKET_NAME + ".sha256.json" in downloaded and downloaded[PACKET_NAME + ".sha256.json"] != packet_path.with_suffix(packet_path.suffix + ".sha256.json").read_bytes():
                    raise ValueError("Existing recovery packet receipt differs from its immutable bytes.")
            elif PACKET_NAME + ".sha256.json" in downloaded:
                raise ValueError("Existing recovery receipt without its packet is ambiguous; retained packet required before writes.")
            if arguments.phase == "recover" and plan["absentPackages"] and not os.environ.get("NUGET_API_KEY"):
                raise ValueError("NuGet Trusted Publishing login required for recover-partial-release.yml / production; policy owner: Jérôme Piquot. No stored key is permitted.")
            for attempt in progress:
                if attempt.get("operation") != "package-push" or not attempt.get("attempted") or attempt.get("verified"):
                    continue
                observed = next(member for member in plan["members"] if member["kind"] == "package" and member["name"] == attempt["name"])
                if observed["classification"] != "present-identical":
                    raise ValueError(f"Prior NuGet package-push attempt remains unresolved for {attempt['name']}; refusing another write until positive identical readback.")
                attempt.update(classification="present-identical", verified=True, uncertain=False, observation=observed)
                save_progress(evidence, progress)
            # The full plan and every occupied asset have been checked before this point.
            # Images are recovered first; package keys are minted by the immediately
            # preceding workflow login only when the saved plan has absent packages.
            result["images"] = recover_images(bundle_root, evidence, arguments.shared_directory.resolve(), identity, graphs, preflight, validator, arguments.skopeo_command, progress)
            if arguments.phase == "recover-images":
                result["status"] = "images-verified"
                return
            for item in packages["packages"]:
                current = classify_package(item, identity, bundle_root, arguments.nuget_base_uri)
                write_json(evidence / "packages" / (item["id"] + ".before-push.json"), current)
                if current["classification"] not in ("absent", "present-identical"):
                    raise ValueError(f"Immediate NuGet recheck refuses {item['id']}: {current['classification']}")
                if current["classification"] == "absent":
                    attempt = dict(current, **{key: identity[key] for key in ("version", "sourceSha", "setSha256")}, operation="package-push", attempted=True, verified=False, uncertain=True)
                    progress.append(attempt)
                    save_progress(evidence, progress)
                    pushed = False
                    try:
                        run(["dotnet", "nuget", "push", bundle_root / "artifacts/packages/release" / item["file"], "--source", "https://api.nuget.org/v3/index.json", "--api-key", os.environ["NUGET_API_KEY"]], evidence / "packages" / (item["id"] + ".push.log"))
                        pushed = True
                    except Exception as error:
                        attempt["error"] = redact(error)
                        raise
                    finally:
                        observed = (await_package_validation(item, identity, bundle_root, arguments, evidence) if pushed
                                    else classify_package(item, identity, bundle_root, arguments.nuget_base_uri))
                        attempt.update(classification=observed["classification"], verified=observed["classification"] == "present-identical", uncertain=observed["classification"] != "present-identical", observation=observed)
                        save_progress(evidence, progress)
                    if not attempt["verified"]:
                        raise ValueError(f"NuGet validation deadline/readback left {item['id']} unverified; publication remains uncertain and subsequent writes are refused.")
            final = recovery_plan(bundle_root, evidence / "final", identity, packages, graphs, preflight, validator, arguments.nuget_base_uri)
            if any(item["classification"] != "present-identical" for item in final["members"]):
                raise ValueError("Final fifteen-member verification is incomplete (NuGet validation may still be pending); no Release completion credit.")
            result.update(packages=final["members"][:10], images=final["members"][10:])
            results = [dict(item, digest=next(package["sha256"] for package in packages["packages"] if package["id"] == item["name"]) if item["kind"] == "package" else graphs[item["name"]]["index_digest"]) for item in final["members"]]
            document = {"artifactKind": "partial-release-recovery", "result": "pass", **{key: identity[key] for key in ("version", "sourceSha", "setSha256")}, "bundleManifest": manifest, "plan": initial_plan, "results": results,
                        "health": [read_json(evidence / image["name"] / "health.json") for image in CANONICAL_IMAGES]}
            write_json(evidence / "recovery-results.json", dict(document, sideEffects=progress))
            packet_path = evidence / PACKET_NAME
            if old_packet is None:
                recovery_packet(packet_path, document, evidence, bundle_root)
            assets[PACKET_NAME] = packet_path
            assets[PACKET_NAME + ".sha256.json"] = packet_path.with_suffix(packet_path.suffix + ".sha256.json")
            # Sequential uploads must make the packet available before its receipt.
            assets = {name: assets[name] for name in (PACKET_NAME, PACKET_NAME + ".sha256.json", *(name for name in assets if name not in (PACKET_NAME, PACKET_NAME + ".sha256.json")))}
            # Recheck immediately before every GitHub write. No --clobber, edit of
            # asset bytes, tag changes, or mutation of an unexpected Release.
            tag = f"v{identity['version']}"
            def release_write(command, operation, expected, published=False):
                attempt = {"kind": "release", "name": tag, "operation": operation, "attempted": True, "verified": False, "uncertain": True,
                           **{key: identity[key] for key in ("version", "sourceSha", "setSha256")}}
                progress.append(attempt)
                save_progress(evidence, progress)
                try:
                    run(command)
                except Exception as error:
                    attempt["error"] = redact(error)
                    raise
                finally:
                    try:
                        observed, present = release_snapshot(identity, evidence, assets)
                        verified = observed is not None and set(expected) <= set(present) and (not published or not observed["isDraft"])
                        attempt.update(verified=verified, uncertain=not verified, verifiedAssets=sorted(present))
                        result["releaseAssets"] = [{"file": name, "sha256": hashlib.sha256(data).hexdigest()} for name, data in sorted(present.items())]
                    except Exception as error:
                        attempt["reprobeError"] = redact(error)
                    save_progress(evidence, progress)
            if release is None:
                release, downloaded = release_snapshot(identity, evidence, assets)
                if release is None:
                    release_write(["gh", "release", "create", tag, "--repo", identity["repository"], "--verify-tag", "--title", tag, "--generate-notes", "--draft"], "release-create", [])
                    release, downloaded = release_snapshot(identity, evidence, assets)
                    if release is None:
                        raise ValueError("Created recovery draft is not observable; refusing asset uploads until its state can be verified.")
            for name, path in assets.items():
                if name in downloaded:
                    continue
                release, downloaded = release_snapshot(identity, evidence, assets)
                if name not in downloaded:
                    release_write(["gh", "release", "upload", tag, "--repo", identity["repository"], path], "release-asset-upload", [name])
                    downloaded[name] = path.read_bytes()
            if release["isDraft"]:
                release, downloaded = release_snapshot(identity, evidence, assets)
                if set(downloaded) != set(assets):
                    raise ValueError("Draft must have all thirteen verified assets before publication.")
                release_write(["gh", "release", "edit", tag, "--repo", identity["repository"], "--draft=false", "--latest=false"], "release-publish", assets, published=True)
            release, downloaded = release_snapshot(identity, evidence, assets)
            if release is None or release["isDraft"] or set(downloaded) != set(assets):
                raise ValueError("GitHub Release does not contain the complete thirteen verified assets.")
            title = f"PARTIAL PUBLISH {identity['version']} - manual reconciliation required"
            issues = json.loads(run(["gh", "issue", "list", "--repo", identity["repository"], "--state", "open", "--search", f'in:title "{title}"', "--limit", "1000", "--json", "number,title"]))
            if len(issues) >= 1000:
                raise ValueError("Partial-publish incident lookup reached its result limit; incident state is ambiguous.")
            matching = [item for item in issues if item["title"] == title]
            if len(matching) > 1:
                raise ValueError("Multiple open partial-publish incidents match this version.")
            for issue in matching:
                run(["gh", "issue", "comment", issue["number"], "--repo", identity["repository"], "--body", f"Recovery verified all fifteen immutable members and thirteen assets for {tag}; setSha256={identity['setSha256']}; evidence: partial-release-completion-v{identity['version']}."])
                run(["gh", "issue", "close", issue["number"], "--repo", identity["repository"], "--reason", "completed"])
            result.update(status="succeeded", packages=final["members"][:10], images=final["members"][10:], releaseAssets=[{"file": name, "sha256": sha256(path)} for name, path in assets.items()])
    except Exception as error:
        result["error"] = redact(error)
        # Preserve the original plan. A failed push/copy may have committed an
        # immutable side effect, so report the latest read-only observation too.
        for group in ("packages", "images"):
            for item in result.get(group, []):
                observations = [attempt for attempt in progress if attempt.get("kind") == item.get("kind") and attempt.get("name") == item.get("name")]
                if observations:
                    item.update(observations[-1].get("observation", {}))
        raise
    finally:
        write_json(evidence / "completion-summary.json", result)


def workflow_gate(arguments):
    """Execute read-only workflow gates with retained, named refusal diagnostics."""
    evidence = arguments.evidence_directory.resolve()
    evidence.mkdir(parents=True, exist_ok=True)
    if arguments.phase == "production-gate":
        protection = json.loads(run(["gh", "api", f"repos/{arguments.repository}/environments/production"]), object_pairs_hook=strict_object)
        write_json(evidence / "production-protection.json", protection)
        if (not any(rule.get("type") == "required_reviewers" and rule.get("reviewers") for rule in protection.get("protection_rules", []))
                or protection.get("deployment_branch_policy") is None):
            raise ValueError("Production approval rule or deployment branch policy missing. Owner Jérôme Piquot must restore production protection before recovery.")
    elif arguments.phase == "failed-run-gate":
        failed = json.loads(run(["gh", "api", f"repos/{arguments.repository}/actions/runs/{arguments.release_run_id}"]), object_pairs_hook=strict_object)
        write_json(evidence / "failed-run.json", failed)
        checks = {"run ID": failed.get("id") == int(arguments.release_run_id),
                  "repository": failed.get("repository", {}).get("full_name") == arguments.repository,
                  "workflow": failed.get("path") == ".github/workflows/release.yml",
                  "completed failure": failed.get("status") == "completed" and failed.get("conclusion") == "failure",
                  "tag head SHA": failed.get("head_sha") == arguments.tag_commit,
                  "run attempt": type(failed.get("run_attempt")) is int and failed["run_attempt"] > 0}
        refused = [name for name, valid in checks.items() if not valid]
        if refused:
            raise ValueError("Failed release run proof differs: " + ", ".join(refused))
        # Read the failed source's renderer tool pin as data, never execute its workflow.
        workflow = run(["git", "-C", arguments.source_root, "show", f"{arguments.tag_commit}:.github/workflows/release.yml"])
        pins = re.findall(r"^  KUBECTL_VERSION: ['\"]?(v[0-9]+\.[0-9]+\.[0-9]+)['\"]?\s*$", workflow, re.MULTILINE)
        current = subprocess.run(["git", "-C", str(arguments.source_root), "cat-file", "-e", f"{arguments.tag_commit}:tools/release-containers.json"], capture_output=True, check=False).returncode == 0
        if current and len(pins) != 1:
            raise ValueError("Failed release source has no unique pinned kubectl version; renderer byte proof cannot proceed.")
        if os.environ.get("GITHUB_OUTPUT"):
            with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
                output.write(f"attempt={failed['run_attempt']}\n")
                if current:
                    output.write(f"kubectl-version={pins[0]}\n")
    else:
        document = json.loads(run(["gh", "api", f"repos/{arguments.repository}/actions/runs/{arguments.release_run_id}/artifacts?per_page=100"]), object_pairs_hook=strict_object)
        write_json(evidence / "artifacts.json", document)
        if document.get("total_count", len(document.get("artifacts", []))) > len(document.get("artifacts", [])):
            raise ValueError("Recovery artifact listing is truncated; artifact selection is ambiguous.")
        artifacts = [item for item in document.get("artifacts", []) if item.get("name") == "release-recovery-bundle" and not item.get("expired")]
        if len(artifacts) != 1:
            raise ValueError("Recovery bundle missing/expired or ambiguous; only failures retaining pre-publication bytes can recover. Owner: Jérôme Piquot.")
        artifact = artifacts[0]
        target = evidence / "bundle.zip"
        with target.open("wb") as stream:
            subprocess.run(["gh", "api", f"repos/{arguments.repository}/actions/artifacts/{artifact['id']}/zip"], stdout=stream, check=True, timeout=COMMAND_TIMEOUT_SECONDS)
        if artifact.get("digest") != "sha256:" + sha256(target):
            raise ValueError("Failed-run artifact digest mismatch or missing digest.")
        with zipfile.ZipFile(target) as archive:
            names = archive.namelist()
            expected = {"release-recovery-bundle.tar.gz", "release-recovery-bundle.tar.gz.sha256.json"}
            if len(names) != 2 or set(names) != expected:
                raise ValueError("Unexpected recovery artifact inventory.")
            archive.extractall(evidence / "bundle")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--phase", choices=("build", "authorize", "publish", "package-identity", "production-gate", "failed-run-gate", "download-bundle", "recovery-metadata", "recovery-plan", "recover-images", "recover"), required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--registry", default=os.environ.get("HEXALITH_ZOT_REGISTRY") or "registry.hexalith.com")
    parser.add_argument("--repository-prefix", default="memories")
    parser.add_argument("--output-directory", type=Path, default=Path("artifacts/containers/release"))
    parser.add_argument("--evidence-directory", type=Path, default=DEFAULT_EVIDENCE_DIRECTORY)
    parser.add_argument("--shared-directory", type=Path, default=Path(".hexalith/builds-execution/Github/publish-containers"))
    parser.add_argument("--skopeo-command", default="skopeo")
    parser.add_argument("--recovery-bundle", type=Path)
    parser.add_argument("--release-run-id")
    parser.add_argument("--release-run-attempt")
    parser.add_argument("--tag-commit")
    parser.add_argument("--nuget-validation-timeout", type=float, default=600)
    parser.add_argument("--source-root", type=Path, default=Path.cwd())
    parser.add_argument("--repository", default="Hexalith/Hexalith.Memories")
    parser.add_argument("--nuget-base-uri", default="https://api.nuget.org/v3-flatcontainer")
    parser.add_argument("--package-directory", type=Path, default=Path("artifacts/packages/release"))
    arguments = parser.parse_args()
    root = Path.cwd().resolve()
    evidence = arguments.evidence_directory.resolve()
    output = arguments.output_directory.resolve()
    try:
        if arguments.phase in ("production-gate", "failed-run-gate", "download-bundle"):
            workflow_gate(arguments)
            return 0
        if arguments.phase in ("recovery-metadata", "recovery-plan", "recover-images", "recover"):
            current_completion(arguments)
            return 0
        if arguments.phase == "package-identity":
            if read_json(evidence / "identity.json")["version"] != arguments.version:
                raise ValueError("Requested package version differs from the frozen release identity.")
            write_package_identity(root, arguments.package_directory.resolve(), evidence / "identity.json")
            return 0
        os.environ.pop("MEMORIES_RECOVERY_IDENTITY", None)
        if arguments.repository_prefix != "memories":
            raise ValueError("Canonical release repository prefix must be memories.")
        evidence.relative_to(root)
        manifest = root / "tools/release-containers.json"
        validate_manifest(root, manifest)
        _, validator, _, hashes = load_shared(arguments.shared_directory)
        identity = current_identity(root, manifest, arguments.version, arguments.registry, hashes)
        identity = freeze_identity(evidence / "identity.json", identity, create=arguments.phase == "build")
        shared = arguments.shared_directory.resolve()
        output.mkdir(parents=True, exist_ok=True)
        if arguments.phase == "build":
            build_archives(root, output, evidence, shared, identity, validator)
        elif arguments.phase == "authorize":
            validate_archives(output, evidence, identity, validator)
            validate_authorization(evidence, identity)
            shared_phase(shared, evidence, identity, "publish")
        else:
            publish_archives(output, evidence, shared, identity, validator, arguments.skopeo_command)
    except Exception as error:
        # Retain a support-safe refusal even when failure precedes the frozen identity.
        write_json(evidence / f"failure-{arguments.phase}.json", {"result": "fail", "phase": arguments.phase, "error": redact(error), "owner": "Jérôme Piquot", "consequence": "No publication-success credit.", "reopenTrigger": "Repair the failing proof and rerun authorized validation."})
        print(f"[container-publication] refused: {redact(error)}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
