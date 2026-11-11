#!/usr/bin/env python3
"""Retain a complete hashed release evidence packet; diagnostics never imply success."""

import argparse
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import re
import sys
import tarfile
import tempfile

specification = importlib.util.spec_from_file_location("memories_contract_archive", Path(__file__).with_name("container-publication-contract.py"))
contract = importlib.util.module_from_spec(specification)
specification.loader.exec_module(contract)

MEMBER_FILES = ("index.raw", "child-linux-amd64.manifest.raw", "child-linux-amd64.config.raw", "child-linux-arm64.manifest.raw", "child-linux-arm64.config.raw", "oci-validation.json", "health.json", "health-amd64.log", "health-arm64.log", "health-run.log", "transport.log", "destination-before-copy.json", "build-deployment.log", "build-oci.log", "runtime-config-parity.json", "smoke-preflight.log", "local/oci-validation.json", "local/index.raw", "local/child-linux-amd64.manifest.raw", "local/child-linux-amd64.config.raw", "local/child-linux-arm64.manifest.raw", "local/child-linux-arm64.config.raw")


def required_files(evidence, container_directory, package_directory, summary_directory, deployment_path):
    paths = [evidence / name for name in ("identity.json", "authorization.json", "archive-compatibility.json", "preflight-verify.log", "preflight-publish.log", "preflight-container.log", "preflight/publication-identity.json", "preflight/publication-preflight.verify.json", "preflight/publication-preflight.publish.json", "preflight/publication-preflight.container.json")]
    for image in contract.CANONICAL_IMAGES:
        paths += [evidence / image["name"] / name for name in MEMBER_FILES]
    paths += [Path("tools/release-containers.json"), Path("tools/release-packages.json"), container_directory / "build-summary.json", container_directory / "publish-summary.json", package_directory / "publish-summary.json", summary_directory / "publish-summary.json", deployment_path]
    return paths


ISO_TIMESTAMP = re.compile(r"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(?:\.(\d+))?(Z|[+-]\d{2}:\d{2})")


def timestamps_normalized(value):
    """Drop trailing fractional-second zeros, which PowerShell's JSON round trip trims; all else stays exact."""
    if isinstance(value, dict):
        return {key: timestamps_normalized(item) for key, item in value.items()}
    if isinstance(value, list):
        return [timestamps_normalized(item) for item in value]
    if isinstance(value, str) and (match := ISO_TIMESTAMP.fullmatch(value)):
        fraction = (match[2] or "").rstrip("0")
        return match[1] + ("." + fraction if fraction else "") + match[3]
    return value


def validate_recorded_graph(directory, graph, identity, validator, reference):
    document = contract.read_json(directory / "oci-validation.json")
    expected_fields = {key: value for key, value in graph.items() if key not in ("index_bytes", "children")}
    if any(document.get(key) != value for key, value in expected_fields.items()) or document.get("image") != reference or document.get("raw_index_file") != "index.raw":
        raise ValueError("Retained OCI graph identity differs from its prepared archive.")
    if (directory / "index.raw").read_bytes() != graph["index_bytes"] or len(document.get("children", [])) != 2:
        raise ValueError("Retained OCI index or children differ from the prepared archive.")
    for child, expected in zip(document["children"], graph["children"]):
        if any(child.get(key) != value for key, value in expected.items() if key not in ("manifest_bytes", "config_bytes")):
            raise ValueError("Retained OCI child metadata differs from the prepared archive.")
        architecture = expected["platform"].split("/")[1]
        for kind in ("manifest", "config"):
            name = f"child-linux-{architecture}.{kind}.raw"
            data = expected[kind + "_bytes"]
            if (child.get(kind + "_raw_file") != name or child.get(kind + "_raw_sha256") != hashlib.sha256(data).hexdigest()
                    or (directory / name).read_bytes() != data):
                raise ValueError("Retained local or published OCI raw object differs from the prepared archive.")
    return document


def validate_summaries(identity, container_directory, package_directory, summary_directory):
    references = [f"{identity['registry']}/{image['repository']}:{identity['version']}" for image in contract.CANONICAL_IMAGES]
    containers = {}
    for filename, push, disposition in (("build-summary.json", False, "built"), ("publish-summary.json", True, "pushed-and-verified")):
        summary = contract.read_json(container_directory / filename)
        if (summary.get("schemaVersion") != 1 or summary.get("artifactKind") != "container-images" or summary.get("status") != "succeeded"
                or summary.get("version") != identity["version"] or summary.get("sourceSha") != identity["sourceSha"]
                or summary.get("setSha256") != identity["setSha256"] or summary.get("source") != identity["registry"]
                or summary.get("push") is not push or summary.get("failed") != [] or summary.get("notAttempted") != []
                or summary.get("pushed") != references or len(summary.get("images", [])) != 5):
            raise ValueError("Container summary is incomplete, contradictory or belongs to a different release set.")
        for image, reference, result in zip(contract.CANONICAL_IMAGES, references, summary["images"]):
            if (result.get("name") != image["name"] or result.get("image") != reference or result.get("status") != "succeeded"
                    or result.get("exitCode") != 0 or result.get("error") is not None or result.get("disposition") != disposition
                    or result.get("transported") is not push):
                raise ValueError("Container summary outcomes contradict complete verified publication.")
        containers[filename] = summary
    packages = contract.read_json(package_directory / "publish-summary.json")
    inventory = contract.read_json("tools/release-packages.json")["packages"]
    expected_packages = sorted(f"{package['id']}.{identity['version']}.nupkg" for package in inventory)
    if (len(expected_packages) != 10 or len(set(expected_packages)) != 10 or packages.get("schemaVersion") != 1
            or packages.get("status") != "succeeded" or packages.get("version") != identity["version"]
            or packages.get("source") != "https://api.nuget.org/v3/index.json" or packages.get("pushed") != expected_packages
            or not isinstance(packages.get("packageDirectory"), str) or Path(packages["packageDirectory"]).resolve() != package_directory.resolve()
            or packages.get("failed") != [] or packages.get("notAttempted") != []):
        raise ValueError("NuGet family summary does not prove the complete ten-package inventory.")
    aggregate = contract.read_json(summary_directory / "publish-summary.json")
    if (aggregate.get("schemaVersion") != 1 or aggregate.get("artifactKind") != "release-artifacts" or aggregate.get("status") != "succeeded"
            or aggregate.get("version") != identity["version"] or aggregate.get("sourceSha") != identity["sourceSha"]
            or aggregate.get("setSha256") != identity["setSha256"] or aggregate.get("failed") != [] or aggregate.get("notAttempted") != []
            or aggregate.get("source") != f"NuGet and {identity['registry']}" or not isinstance(aggregate.get("packageDirectory"), str)
            or Path(aggregate["packageDirectory"]).resolve() != summary_directory.resolve()
            or len(aggregate.get("families", [])) != 2):
        raise ValueError("Aggregate summary is incomplete, contradictory or belongs to a different release set.")
    pushed = []
    for family, name, child, path in zip(aggregate["families"], ("NuGet packages", "container images"), (packages, containers["publish-summary.json"]), (package_directory / "publish-summary.json", container_directory / "publish-summary.json")):
        if (family.get("name") != name or family.get("status") != "succeeded" or family.get("exitCode") != 0
                or timestamps_normalized(family.get("summary")) != timestamps_normalized(child) or not isinstance(family.get("summaryPath"), str)
                or Path(family["summaryPath"]).resolve() != path.resolve()):
            raise ValueError("Aggregate family summary contradicts its actual recorded publisher summary.")
        pushed += [f"{name}: {item}" for item in child["pushed"]]
    if aggregate.get("pushed") != pushed:
        raise ValueError("Aggregate published side effects contradict the family summaries.")


def render_deployment(identity, destination):
    arguments = ["pwsh", "-NoLogo", "-NoProfile", "-File", str(Path("tools/render-production-deployment.ps1").resolve()), "-Version", identity["version"], "-OutputPath", str(destination)]
    for image, parameter in zip(contract.CANONICAL_IMAGES[:4], ("ServerImage", "McpImage", "AccessTelemetryImage", "AccessTelemetryClockImage")):
        arguments += ["-" + parameter, f"{identity['registry']}/{image['repository']}:{identity['version']}"]
    contract.run(arguments)


def verify(evidence, shared, container_directory=Path("artifacts/containers/release"), package_directory=Path("artifacts/packages/release"), summary_directory=Path("artifacts/release"), deployment_path=Path("artifacts/deployment/hexalith-memories-production.yaml")):
    paths = required_files(evidence, container_directory, package_directory, summary_directory, deployment_path)
    missing = [str(path) for path in paths if not path.is_file()]
    if missing:
        raise ValueError("Release evidence is incomplete: " + ", ".join(missing))
    identity = contract.read_json(evidence / "identity.json")
    _, validator, _, hashes = contract.load_shared(shared)
    manifest = Path("tools/release-containers.json")
    contract.validate_manifest(Path.cwd(), manifest)
    current = contract.current_identity(Path.cwd(), manifest, identity["version"], identity["registry"], hashes)
    contract.freeze_identity(evidence / "identity.json", current)
    graphs = contract.validate_archives(container_directory, evidence, identity, validator)
    contract.validate_authorization(evidence, identity)
    preflight_identity = contract.read_json(evidence / "preflight/publication-identity.json")
    repositories = sorted(f"{identity['registry']}/{image['repository']}" for image in contract.CANONICAL_IMAGES)
    if (preflight_identity.get("source_sha") != identity["sourceSha"] or preflight_identity.get("version") != identity["version"]
            or preflight_identity.get("container_repositories") != repositories or preflight_identity.get("builds", {}).get("workflow_sha") != contract.APPROVED_BUILDS_SHA):
        raise ValueError("Shared preflight does not bind the complete release identity.")
    preflight_hash = contract.sha256(evidence / "preflight/publication-identity.json")
    for phase in ("verify", "publish", "container"):
        document = contract.read_json(evidence / f"preflight/publication-preflight.{phase}.json")
        if document.get("result") != "pass" or document.get("phase") != phase or document.get("identity_sha256") != preflight_hash or document.get("identity") != preflight_identity:
            raise ValueError("Shared publication phase evidence changed or failed.")
    compatibility = contract.read_json(evidence / "archive-compatibility.json")["archives"]
    for image, record in zip(contract.CANONICAL_IMAGES, compatibility):
        member = evidence / image["name"]
        # Every child must carry the deployment archive's runtime config; validate_archives has
        # already recomputed these hashes from the archive bytes.
        parity = record.get("runtimeConfig")
        diagnostics = contract.read_json(member / "runtime-config-parity.json")
        if (not isinstance(parity, dict) or parity.get("fields") != list(contract.RUNTIME_CONFIG_FIELDS)
                or not re.fullmatch(r"[0-9a-f]{64}", str(parity.get("deploymentSha256")))
                or parity.get("children") != {platform: parity["deploymentSha256"] for platform in contract.PLATFORMS}
                or diagnostics.get("result") != "pass" or diagnostics.get("mismatches") != [] or diagnostics.get("sha256") != parity
                or diagnostics.get("fields") != parity["fields"]
                or hashlib.sha256(contract.canonical(diagnostics.get("deployment"))).hexdigest() != parity["deploymentSha256"]
                or diagnostics.get("children") != {child["platform"]: contract.runtime_config(json.loads(child["config_bytes"]))
                                                   for child in graphs[image["name"]]["children"]}):
            raise ValueError("Runtime config parity evidence is missing, failed or changed.")
    for image in contract.CANONICAL_IMAGES:
        member = evidence / image["name"]
        reference = f"{identity['registry']}/{image['repository']}:{identity['version']}"
        repository = f"{identity['registry']}/{image['repository']}"
        absence = contract.read_json(member / "destination-before-copy.json")
        if absence != {"result": "pass", "version": identity["version"], "container_count": 1, "container_repositories": [repository], "container_repository": repository}:
            raise ValueError("Destination-before-copy receipt does not prove exact repository/version absence.")
        validation = validate_recorded_graph(member, graphs[image["name"]], identity, validator, reference)
        validate_recorded_graph(member / "local", graphs[image["name"]], identity, validator, reference)
        health = contract.read_json(member / "health.json")
        expected = [{"platform": child["platform"], "digest": child["digest"], "outcome": "pass", "cleanup": "pass"} for child in validation["children"]]
        if (health.get("result") != "pass" or health.get("setSha256") != identity["setSha256"] or health.get("platforms") != expected
                or health.get("ociValidationSha256") != contract.sha256(member / "oci-validation.json") or health.get("image") != reference
                or health.get("indexDigest") != validation["index_digest"] or health.get("contract") != image["health"]
                or health.get("endpoint") != ("/alive" if image["health"] == "liveness" else "/ready") or health.get("environment") != "Development" or health.get("lifecycleWritesEnabled") is not False):
            raise ValueError("Digest-bound workload health evidence is missing, failed or changed.")
    validate_summaries(identity, container_directory, package_directory, summary_directory)
    # Re-run the existing caller renderer with the frozen four hosted mappings. DomainService
    # remains a publication/archive member and receives no hosted deployment argument.
    with tempfile.TemporaryDirectory(prefix="memories-deployment-proof-") as temporary:
        rendered = Path(temporary) / "production.yaml"
        render_deployment(identity, rendered)
        if rendered.read_bytes() != deployment_path.read_bytes() or rendered.read_bytes() != (container_directory / "production-deployment.yaml").read_bytes():
            raise ValueError("Retained deployment differs from the caller renderer and frozen image mappings.")
    paths.append(container_directory / "production-deployment.yaml")
    return identity, paths


def archive(evidence, output, shared, diagnostics=False, **directories):
    identity_readable = True
    if diagnostics:
        try:
            identity = contract.read_json(evidence / "identity.json")
            if not isinstance(identity, dict):
                raise ValueError("Identity must be an object.")
        except (OSError, ValueError, UnicodeError):
            identity, identity_readable = {}, False
        defaults = dict(container_directory=Path("artifacts/containers/release"), package_directory=Path("artifacts/packages/release"), summary_directory=Path("artifacts/release"), deployment_path=Path("artifacts/deployment/hexalith-memories-production.yaml"))
        defaults.update(directories)
        paths = [path for path in required_files(evidence, **defaults) if path.is_file()]
        result = "diagnostic-only"
    else:
        identity, paths = verify(evidence, shared, **directories)
        result = "pass"
    paths += [path for path in evidence.rglob("*") if path.is_file() or path.is_symlink()]
    for path in paths:
        if path.is_symlink():
            raise ValueError("Evidence cannot be a symbolic link.")
    paths = sorted({path.resolve() for path in paths if path.resolve() not in (output.resolve(), output.with_suffix(output.suffix + ".sha256.json").resolve())}, key=str)
    root = Path.cwd().resolve()
    entries, sources = [], {}
    for path in paths:
        try:
            relative = path.relative_to(root).as_posix()
        except ValueError:
            relative = "external/" + hashlib.sha256(str(path.parent).encode()).hexdigest()[:16] + "/" + path.name
        entries.append({"path": relative, "sha256": contract.sha256(path), "size": path.stat().st_size})
        sources[relative] = path
    manifest = {"schemaVersion": "hexalith.memories.release-evidence.v1", "result": result, "identityReadable": identity_readable, "version": identity.get("version"), "sourceSha": identity.get("sourceSha"), "setSha256": identity.get("setSha256"), "files": entries}
    output.parent.mkdir(parents=True, exist_ok=True)
    with tarfile.open(output, "w:gz") as packet:
        data = json.dumps(manifest, indent=2, sort_keys=True).encode() + b"\n"
        header = tarfile.TarInfo("evidence-manifest.json")
        header.size = len(data)
        packet.addfile(header, io.BytesIO(data))
        for entry in entries:
            packet.add(sources[entry["path"]], arcname=entry["path"], recursive=False)
    contract.write_json(output.with_suffix(output.suffix + ".sha256.json"), {"path": output.name, "sha256": contract.sha256(output), "result": result})
    return manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence-directory", type=Path, default=contract.DEFAULT_EVIDENCE_DIRECTORY)
    parser.add_argument("--output", type=Path, default=Path("artifacts/release/hexalith-memories-release-evidence.tar.gz"))
    parser.add_argument("--shared-directory", type=Path, default=Path(".hexalith/builds-execution/Github/publish-containers"))
    parser.add_argument("--diagnostics", action="store_true")
    parser.add_argument("--container-directory", type=Path, default=Path("artifacts/containers/release"))
    parser.add_argument("--package-directory", type=Path, default=Path("artifacts/packages/release"))
    parser.add_argument("--summary-directory", type=Path, default=Path("artifacts/release"))
    parser.add_argument("--deployment-path", type=Path, default=Path("artifacts/deployment/hexalith-memories-production.yaml"))
    arguments = parser.parse_args()
    try:
        archive(arguments.evidence_directory, arguments.output, arguments.shared_directory, arguments.diagnostics, container_directory=arguments.container_directory, package_directory=arguments.package_directory, summary_directory=arguments.summary_directory, deployment_path=arguments.deployment_path)
    except Exception as error:
        print(f"[release-evidence] refused: {contract.redact(error)}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
