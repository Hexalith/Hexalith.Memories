"""Offline integrity check for this inactive proposal; never contacts a target."""

import argparse
import base64
import hashlib
import io
import json
import re
import sys
import tarfile
from pathlib import Path


class VerificationError(ValueError):
    """A bounded, explicit candidate-package rejection, including under python -O."""


def require(condition, message):
    if not condition:
        raise VerificationError(message)


def unique_object(pairs):
    result = {}
    for name, value in pairs:
        require(name not in result, "duplicate-json-property")
        result[name] = value
    return result


def reject_constant(_value):
    raise VerificationError("non-finite-json-number")


def strict_json(data):
    try:
        value = json.loads(data, object_pairs_hook=unique_object, parse_constant=reject_constant)
    except (UnicodeError, json.JSONDecodeError) as error:
        raise VerificationError("malformed-json") from error
    require(isinstance(value, dict), "json-object-required")
    return value


def read_json(path):
    return strict_json(path.read_bytes())


def sha(data):
    return hashlib.sha256(data).hexdigest()


def canonical(value):
    return json.dumps(value, ensure_ascii=True, sort_keys=True, separators=(",", ":"))


def require_secret_safe(value):
    if isinstance(value, dict):
        for name, item in value.items():
            credential = re.fullmatch(
                r"authorization|dapr[-_]?api[-_]?token|password|passwd|token|secret|client[-_]?secret|access[-_]?token|connection[-_]?string",
                name, re.I,
            )
            nonempty = item is not None and (not isinstance(item, (str, list, dict)) or len(item) > 0)
            require(not credential or not nonempty, "historical-packet-credential-content")
            require_secret_safe(name)
            require_secret_safe(item)
    elif isinstance(value, list):
        for item in value:
            require_secret_safe(item)
    elif isinstance(value, str):
        require(not re.search(r"C1[_-]?SECRET[_-]?CANARY|SECRET[_-]?CANARY|\b(?:hvs|hvb|hvr)\.[A-Za-z0-9_-]{8,}", value, re.I),
                "historical-packet-secret-shaped-content")


def verify(directory, root):
    proposal = read_json(directory / "candidate-profile.json")
    require(set(proposal) == {
        "schema_version", "state", "profile_alias", "repository_revision", "historical_profile_sha256",
        "activation_authorized", "security_requalification", "production_gate_passed", "manifest",
    }, "candidate-envelope-fields")
    require(type(proposal["schema_version"]) is int and proposal["schema_version"] == 1, "candidate-envelope-version")
    require(proposal["profile_alias"] == "PG-ONPREM-2", "candidate-profile-alias")
    require(proposal["state"] == "pending-exact-byte-approval", "candidate-disposition")
    require(proposal["activation_authorized"] is False and proposal["production_gate_passed"] is False,
            "candidate-activation-or-production-credit")
    require(proposal["security_requalification"] == "not-evaluated", "candidate-security-credit")
    manifest = proposal["manifest"]
    require(set(manifest) == {"canonical_profile", "canonical_profile_json", "profile_sha256", "mutation_manifest_sha256"},
            "candidate-manifest-fields")
    profile = manifest["canonical_profile"]
    identity = profile["identity"]
    security = identity["securityPlatform"]
    serialized = canonical(profile)
    require(serialized == manifest["canonical_profile_json"], "canonical-profile-bytes")
    strict_json(manifest["canonical_profile_json"])
    require(sha(serialized.encode()) == manifest["profile_sha256"] ==
            "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe", "canonical-profile-hash")
    mutation = canonical({"allowed_mutations": [], "profile_sha256": manifest["profile_sha256"]})
    require(sha(mutation.encode()) == manifest["mutation_manifest_sha256"], "mutation-manifest-hash")

    archive = read_json(directory / "registry-source-evidence.json")
    require(set(archive) == {"status", "source_revision", "encoding_note", "artifacts"}, "archive-envelope-fields")
    require(archive["status"] == "public-content-identity-evidence; no qualification or approval", "archive-disposition")
    require(proposal["repository_revision"] == archive["source_revision"], "archive-source-revision")
    expected_artifacts = {
        "postgres-current.json", "postgres-tag.json", "postgres-linux-amd64.json", "postgres-config.json",
        "openbao-tag.json", "openbao-linux-amd64.json", "openbao-config.json", "openbao-chart-tag.json",
        "openbao-chart-config.json", "openbao-0.29.6.tgz", "dapr-1.18.1-index.json",
        "dapr-1.18.1-linux-amd64-manifest.json", "dapr-1.18.1-linux-amd64-config.json",
        "openbao-render.candidate.yaml", "openbao-render-baseline.yaml", "openbao-values.baseline.yaml",
        "openbao-0.28.5.tgz", "candidate-lint.stdout",
    }
    require(set(archive["artifacts"]) == expected_artifacts, "archive-artifact-inventory")
    data = {}
    for name, record in archive["artifacts"].items():
        require(set(record) == {"sha256", "size_bytes", "original_bytes_base64"}, "archive-record-fields")
        require(type(record["size_bytes"]) is int and record["size_bytes"] >= 0, "archive-record-size-type")
        raw = base64.b64decode(record["original_bytes_base64"], validate=True)
        require(len(raw) == record["size_bytes"] and sha(raw) == record["sha256"], "archive-record-integrity: " + name)
        if name.endswith(".json"):
            strict_json(raw)
        data[name] = raw
    print("PASS: 18 exact archived artifact hashes and sizes; strict duplicate-rejecting JSON")

    records = read_json(directory / "artifact-records.json")
    require(records["source_repository_head"] == archive["source_revision"] and records["platform"] == "linux/amd64",
            "artifact-receipt-attribution")
    receipts = {}
    for receipt in records["sources"]:
        require(receipt["name"] not in receipts, "duplicate-artifact-receipt")
        receipts[receipt["name"]] = receipt
    require(set(receipts) == {"postgresql-18.6-trixie", "openbao-2.6.4", "openbao-chart-0.29.6"}, "artifact-receipt-inventory")
    dapr = read_json(directory / "dapr-authentication-summary.json")
    require(dapr["source_repository_head"] == archive["source_revision"], "dapr-summary-attribution")
    chains = (
        ("postgres", "postgres-tag.json", "postgres-linux-amd64.json", "postgres-config.json",
         identity["postgresqlImage"], identity["postgresqlLinuxAmd64Manifest"], receipts["postgresql-18.6-trixie"]),
        ("openbao", "openbao-tag.json", "openbao-linux-amd64.json", "openbao-config.json",
         security["openbaoImage"], security["openbaoLinuxAmd64Manifest"], receipts["openbao-2.6.4"]),
        ("dapr", "dapr-1.18.1-index.json", "dapr-1.18.1-linux-amd64-manifest.json", "dapr-1.18.1-linux-amd64-config.json",
         identity["runtimeImage"], identity["runtimeLinuxAmd64Manifest"], {
             "image_index_sha256": dapr["official_index_sha256"],
             "linux_amd64_manifest_sha256": dapr["linux_amd64_manifest_sha256"],
             "linux_amd64_config_sha256": dapr["linux_amd64_config_sha256"],
         }),
    )
    for label, index, child, config, pin, child_pin, receipt in chains:
        require(pin.rsplit("@", 1)[-1] == "sha256:" + sha(data[index]), label + ":canonical-index-pin")
        require(child_pin == "sha256:" + sha(data[child]), label + ":canonical-child-pin")
        require(receipt["image_index_sha256"] == sha(data[index]) and
                receipt["linux_amd64_manifest_sha256"] == sha(data[child]) and
                receipt["linux_amd64_config_sha256"] == sha(data[config]), label + ":receipt-chain")
        descriptors = [d for d in strict_json(data[index])["manifests"]
                       if d.get("platform") == {"architecture": "amd64", "os": "linux"}]
        require(len(descriptors) == 1, label + ":platform-descriptor-count")
        require(descriptors[0]["digest"] == "sha256:" + sha(data[child]) and
                descriptors[0]["size"] == len(data[child]), label + ":index-child-chain")
        descriptor = strict_json(data[child])["config"]
        require(descriptor["digest"] == "sha256:" + sha(data[config]) and
                descriptor["size"] == len(data[config]), label + ":child-config-chain")
        settings = strict_json(data[config])
        require(settings["architecture"] == "amd64" and settings["os"] == "linux", label + ":config-platform")
    old = strict_json(data["postgres-current.json"])
    old_children = [d for d in old["manifests"] if d.get("platform") == {"architecture": "amd64", "os": "linux"}]
    require(sha(data["postgres-current.json"]) == "3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a",
            "historical-postgres-index")
    require(len(old_children) == 1 and old_children[0]["digest"] ==
            "sha256:d93de42662696f278fb34354b06fdaa90ad7ca3106d6f72fbd01d16da006d2cf", "historical-postgres-child")
    chart_receipt = receipts["openbao-chart-0.29.6"]
    for name, canonical_pin, receipt_hash in (
        ("openbao-chart-tag.json", security["chartOciManifest"], chart_receipt["oci_manifest_sha256"]),
        ("openbao-0.29.6.tgz", security["chartContent"], chart_receipt["chart_content_sha256"]),
    ):
        require(canonical_pin == "sha256:" + sha(data[name]) and receipt_hash == sha(data[name]), "chart:canonical-receipt-pin")
    chart = strict_json(data["openbao-chart-tag.json"])
    content = [d for d in chart["layers"] if d["mediaType"] == "application/vnd.cncf.helm.chart.content.v1.tar+gzip"]
    require(len(content) == 1 and content[0]["digest"] == "sha256:" + sha(data["openbao-0.29.6.tgz"]) and
            content[0]["size"] == len(data["openbao-0.29.6.tgz"]), "chart:content-chain")
    require(chart["config"]["digest"] == "sha256:" + sha(data["openbao-chart-config.json"]) and
            chart["config"]["size"] == len(data["openbao-chart-config.json"]) and
            chart_receipt["config_sha256"] == sha(data["openbao-chart-config.json"]), "chart:config-chain")
    chart_config = strict_json(data["openbao-chart-config.json"])
    require(chart_config["version"] == security["chartVersion"] == "0.29.6" and
            chart_config["appVersion"] == chart_receipt["app_version_in_chart"] == "v2.6.3", "chart:version-identity")
    with tarfile.open(fileobj=io.BytesIO(data["openbao-0.29.6.tgz"]), mode="r:gz") as tar:
        members = [member for member in tar.getmembers() if member.name == "openbao/Chart.yaml"]
        require(len(members) == 1 and members[0].isfile(), "chart:metadata-member")
        metadata = tar.extractfile(members[0]).read().decode()
        require(re.search(r"^version: 0\.29\.6$", metadata, re.M) and
                re.search(r"^appVersion: v2\.6\.3$", metadata, re.M), "chart:metadata-version")
    dapr_receipts = read_json(directory / "dapr-registry-receipts.json")
    require(dapr_receipts["source_repository_head"] == archive["source_revision"] and
            len(dapr_receipts["receipts"]) == 4, "dapr-registry-receipt-attribution")
    dapr_data = {"dapr-accepted-c1.15-manifest.json": data["dapr-1.18.1-index.json"]}
    dapr_data.update({name: data[name] for name in data if name.startswith("dapr-")})
    seen_receipts = set()
    for receipt in dapr_receipts["receipts"]:
        name = Path(receipt["path"]).name
        require(name in dapr_data and name not in seen_receipts, "dapr-registry-receipt-inventory")
        seen_receipts.add(name)
        raw = dapr_data[name]
        require(receipt["sha256"] == sha(raw) and receipt["bytes"] == len(raw), "dapr-registry-receipt-integrity")
        require(receipt["docker_content_digest"] in (None, "sha256:" + sha(raw)), "dapr-registry-content-digest")
    print("PASS: canonical and receipt-bound image/platform/config and chart chains")

    for filename, expected in identity["deploymentInputs"].items():
        require(sha((directory / filename).read_bytes()) == expected, "deployment-input: " + filename)
    require(len(identity["deploymentInputs"]) == 3, "deployment-input-count")
    require(sha((directory / "openbao-values.candidate.yaml").read_bytes()) == security["valuesSha256"] and
            records["candidate_values_sha256"] == security["valuesSha256"], "candidate-values")
    require(sha(data["openbao-render.candidate.yaml"]) == security["renderSha256"] == records["candidate_render_sha256"],
            "candidate-render")
    for filename, expected in security["secretAndConfigurationInputs"].items():
        require(sha((root / filename).read_bytes()) == expected, "current-input: " + filename)
    require(len(security["secretAndConfigurationInputs"]) == 12, "current-input-count")
    require(len(re.findall(r"^  replicas: 0$", (directory / "lifecycle-deployments.candidate.yaml").read_text(), re.M)) == 2,
            "candidate-workloads-disabled")
    require(identity["capacityBoundary"]["nodeHa"] is False, "candidate-no-ha-boundary")
    print("PASS: canonical/mutation hashes, 3 inactive workloads, values/render, 12 current inputs")

    require(dapr["retained_historical_packet_path"] == "historical-c1-15-packet-bytes.json" and
            dapr["retained_historical_packet_format"] == "exact-bytes-base64/v1", "historical-packet-retention-identity")
    retained_packet = read_json(directory / dapr["retained_historical_packet_path"])
    require(set(retained_packet) == {"schema_version", "encoding", "sha256", "size_bytes", "original_bytes_base64"} and
            type(retained_packet["schema_version"]) is int and retained_packet["schema_version"] == 1 and
            retained_packet["encoding"] == "exact-bytes-base64/v1", "historical-packet-envelope")
    raw_packet = base64.b64decode(retained_packet["original_bytes_base64"], validate=True)
    require(type(retained_packet["size_bytes"]) is int and len(raw_packet) == retained_packet["size_bytes"], "historical-packet-size")
    require(sha(raw_packet) == retained_packet["sha256"] == dapr["historical_packet_sha256"] ==
            "17d7f350c3193ce6663364b0b4e6ef52d8f319f4ca4c0a25e9886a006ff0ed87", "historical-packet-hash")
    packet = strict_json(raw_packet)
    require_secret_safe(packet)
    require(packet["gate"] == "C1.15" and packet["profileId"] == "PG-ONPREM-1" and
            packet["producerStatus"] == "observed" and packet["gateStatus"] == "not-evaluated" and
            packet["productionGatePassed"] is False, "historical-packet-disposition")
    observations = packet["observations"]
    runtime = dapr["historical_runtime_version"]
    digest = dapr["historical_sidecar_image_digest"]
    require(observations["runtimeVersions"] == [runtime] == ["1.18.1"] and
            observations["sidecarImageDigests"] == [digest] == ["sha256:" + dapr["official_index_sha256"]],
            "historical-packet-runtime-image")
    require(isinstance(observations["pods"], list) and len(observations["pods"]) > 0, "historical-packet-pods")
    for pod in observations["pods"]:
        require(pod["runtimeVersion"] == runtime and pod["sidecarImageDigest"] == digest and
                pod["sidecarImageId"].endswith(digest), "historical-packet-pod-runtime-image")
    print("PASS: retained exact secret-safe historical C1.15 packet hash/runtime/image observations")

    sys.path.insert(0, str(root / "tools"))
    import verify_access_telemetry_lifecycle as lifecycle
    retained = read_json(directory / "historical-pg-onprem-1.json")
    require(retained == lifecycle.canonical_pg_onprem_profile().manifest(), "historical-profile-source")
    require(proposal["historical_profile_sha256"] == retained["profile_sha256"], "historical-profile-linkage")
    require(profile["capabilities"] == retained["canonical_profile"]["capabilities"] and
            profile["workload"] == retained["canonical_profile"]["workload"] and
            profile["workload"]["total_events_per_second"] == 500, "historical-capabilities-workload")
    require("ACCESS_TELEMETRY_ENABLED=false" in (root / "deploy/kubernetes/overlays/production/kustomization.yaml").read_text(),
            "production-enabled")
    require(len(re.findall(r"^  replicas: 0$", (root / "deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml").read_text(), re.M)) == 2,
            "production-workloads-enabled")
    print("PASS: unchanged historical profile/requirements and disabled Production source")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, required=True)
    args = parser.parse_args()
    try:
        verify(Path(__file__).resolve().parent, args.repo_root.resolve())
    except (ValueError, KeyError, TypeError, AttributeError, OSError, tarfile.TarError) as error:
        message = str(error) if isinstance(error, VerificationError) else "malformed-candidate-package"
        print("FAIL: " + message, file=sys.stderr)
        raise SystemExit(1)
