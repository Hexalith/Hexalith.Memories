#!/usr/bin/env python3
"""Memories' five-image adapter for the exact approved Builds publication helpers."""

import argparse
import base64
from datetime import datetime, timezone
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tarfile
import tempfile
import xml.etree.ElementTree as ET


APPROVED_BUILDS_SHA = "81c88b34d09d34260b92d7f0928dbbc1c7446baa"
PLATFORMS = ["linux/amd64", "linux/arm64"]
CANONICAL_IMAGES = [
    {"name": "server", "project": "src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj", "repository": "memories", "health": "liveness"},
    {"name": "mcp", "project": "src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj", "repository": "memories-mcp", "health": "liveness"},
    {"name": "access-telemetry", "project": "src/Hexalith.Memories.AccessTelemetry/Hexalith.Memories.AccessTelemetry.csproj", "repository": "memories-access-telemetry", "health": "disabled-readiness"},
    {"name": "access-telemetry-clock", "project": "src/Hexalith.Memories.AccessTelemetry.Clock/Hexalith.Memories.AccessTelemetry.Clock.csproj", "repository": "memories-access-telemetry-clock", "health": "signed-readiness"},
    {"name": "domain-service", "project": "src/Hexalith.Memories.DomainService/Hexalith.Memories.DomainService.csproj", "repository": "memories-domain-service", "health": "liveness"},
]
SHARED_FILES = ("publication_preflight.py", "oci_registry_validator.py", "smoke_container_platforms.py", "smoke-container-platforms.sh", "publish-containers.sh", "action.yml")


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


def run(command, log=None):
    result = subprocess.run([str(item) for item in command], capture_output=True, text=True, check=False, timeout=1800)
    output = redact(result.stdout + result.stderr)
    if log:
        Path(log).parent.mkdir(parents=True, exist_ok=True)
        Path(log).write_text(output, encoding="utf-8")
    if result.returncode:
        raise ValueError(f"{Path(str(command[0])).name} failed ({result.returncode}): {output[-2000:]}")
    return result.stdout


def load_shared(directory):
    directory = Path(directory).resolve()
    root = directory.parents[1]
    checkout = subprocess.run(["git", "-C", str(root), "rev-parse", "HEAD"], capture_output=True, text=True, check=True).stdout.strip()
    if checkout != APPROVED_BUILDS_SHA:
        raise ValueError("Publication helpers are not the approved Builds revision.")
    files = {}
    for name in (*SHARED_FILES, "../../Props/ContainerProvenance.targets"):
        path = (directory / name).resolve()
        relative = path.relative_to(root).as_posix()
        approved = subprocess.run(["git", "-C", str(root), "show", f"{APPROVED_BUILDS_SHA}:{relative}"], capture_output=True, check=True).stdout
        # A checkout may materialize CRLF, but executable helper bytes must match its approved blob.
        if path.read_bytes().replace(b"\r\n", b"\n") != approved.replace(b"\r\n", b"\n"):
            raise ValueError(f"Approved Builds file changed: {relative}")
        files[relative] = sha256(path)
    for name in ("oci_registry_validator", "smoke_container_platforms"):
        cached = sys.modules.get(name)
        if cached and Path(cached.__file__).resolve() != directory / (name + ".py"):
            raise ValueError("An unapproved Builds module is already loaded.")
    # No approved executable is imported until every approved byte has been checked.
    preflight = load_local("check-release-destinations").load_pinned_helper(directory)
    sys.path.insert(0, str(directory))
    try:
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
        if any((node.text or "").strip() == "true" for node in tree.findall(".//EnableContainer")):
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
            with tarfile.open(legacy, "r:*") as archive:
                manifest = json.load(archive.extractfile("manifest.json"))
                if len(manifest) != 1:
                    raise ValueError("Deployment archive must contain exactly one Docker image.")
                config = json.load(archive.extractfile(manifest[0]["Config"]))
                validator._validate_labels(config, labels_for(validator, identity))
                if config.get("architecture") != "amd64" or config.get("os") != "linux":
                    raise ValueError("Stable deployment archive must remain linux/amd64 Docker format.")
            compatibility.append({"name": image["name"], "filename": legacy.name, "format": "docker-archive", "sha256": sha256(legacy), "ociArchive": oci.name, "ociSha256": sha256(oci), "indexDigest": graph["index_digest"], "sourceSha": identity["sourceSha"], "setSha256": identity["setSha256"]})
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
    write_json(path, {"schemaVersion": 1, "artifactKind": "container-images", "status": status, "version": identity["version"], "source": identity["registry"], "sourceSha": identity["sourceSha"], "setSha256": identity["setSha256"], "push": push, "images": outcomes, "pushed": [item["image"] for item in (transported if push else succeeded)], "failed": [{"package": item["image"], "exitCode": item["exitCode"], "error": item["error"]} for item in failed], "notAttempted": [], "recovery": "Stop and reconcile immutable evidence with Jérôme Piquot; current missing-only recovery belongs to Story 30.5."})


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
                    run([skopeo, "copy", "--all", "--preserve-digests", "--authfile", auth, f"oci-archive:{output / (image['name'] + '.oci.tar.gz')}", f"docker://{reference}"], member / "transport.log")
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


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--phase", choices=("build", "authorize", "publish"), required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--registry", default=os.environ.get("HEXALITH_ZOT_REGISTRY") or "registry.hexalith.com")
    parser.add_argument("--repository-prefix", default="memories")
    parser.add_argument("--output-directory", type=Path, default=Path("artifacts/containers/release"))
    parser.add_argument("--evidence-directory", type=Path, default=Path("artifacts/release/container-evidence"))
    parser.add_argument("--shared-directory", type=Path, default=Path(".hexalith/builds-execution/Github/publish-containers"))
    parser.add_argument("--skopeo-command", default="skopeo")
    arguments = parser.parse_args()
    root = Path.cwd().resolve()
    evidence = arguments.evidence_directory.resolve()
    output = arguments.output_directory.resolve()
    try:
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
