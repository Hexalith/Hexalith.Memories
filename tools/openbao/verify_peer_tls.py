#!/usr/bin/env python3
"""Read-only EVAL-01 evidence. Never retrieve Secrets, tokens, keys or audit logs."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import select
import socket
import ssl
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
BASE = ["kubectl", "--context=jpiquot@local", "--request-timeout=12s", "-n", "openbao"]
CLEAN = ["env", "-i", "PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin",
         "HOME=/nonexistent", "BAO_CLIENT_TIMEOUT=8s"]
SERVICE = "hexalith-keys.openbao.svc.cluster.local"
CA = "/openbao/userconfig/openbao-server-tls/ca.crt"
CONFIG_FILES = ["/openbao/config/extraconfig-from-values.hcl", "/tmp/storageconfig.hcl"]
PRESERVED = [
    "_bmad-output/implementation-artifacts/tests/31-1-assistant-security-evaluation-2026-11-16.md",
    "_bmad-output/implementation-artifacts/tests/31-1-assistant-security-evaluation-2026-11-16.json",
    "_bmad-output/implementation-artifacts/tests/31-1-ai-security-review-2026-11-16.md",
    "_bmad-output/implementation-artifacts/tests/31-1-ai-security-review-2026-11-16.json",
    "_bmad-output/implementation-artifacts/tests/31-1-security-evidence-refresh-2026-11-16.md",
    "_bmad-output/implementation-artifacts/tests/31-1-security-evidence-refresh-2026-11-16.json",
    "_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md",
    "_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md",
    "_bmad-output/implementation-artifacts/sprint-status.yaml",
    "_bmad-output/planning-artifacts/epics.md",
    "docs/governance/openbao-independent-security-review.md",
    "docs/operations/openbao.md",
    "deploy/openbao/values.yaml",
    "_bmad-output/implementation-artifacts/spec-31-1-openbao-security-guard-remediation.md",
]


def now():
    return datetime.now(timezone.utc).isoformat(timespec="microseconds")


def sha(data):
    return hashlib.sha256(data).hexdigest()


def execute(command, project):
    record = {"command": command, "started_utc": now()}
    try:
        result = subprocess.run(command, capture_output=True, text=True, timeout=25)
        record["exit_code"] = result.returncode
        record["observation"] = project(result)
    except (subprocess.TimeoutExpired, OSError):
        record.update(exit_code=-1, observation={"failure": "execution unavailable or timed out; output discarded"})
    except (ValueError, KeyError, TypeError):
        record["observation"] = {"failure": "unexpected response shape; output discarded"}
    record["finished_utc"] = now()
    return record


def project_json(result, project):
    if result.returncode:
        return {"failure": "metadata query failed; output discarded"}
    return project(json.loads(result.stdout))


def metadata():
    return {
        "pods": execute(BASE + ["get", "pods", "-l", "app.kubernetes.io/instance=hexalith-keys,component=server", "-o", "json"],
            lambda r: project_json(r, lambda d: sorted([{
                "name": p["metadata"]["name"], "uid": p["metadata"]["uid"],
                "chart_label": p["metadata"].get("labels", {}).get("helm.sh/chart"),
                "active_label": p["metadata"].get("labels", {}).get("openbao-active"),
                "phase": p["status"]["phase"],
                "containers": [{k: c.get(k) for k in ["name", "ready", "image", "imageID", "restartCount"]}
                               for c in p["status"].get("containerStatuses", [])],
            } for p in d["items"]], key=lambda p: p["name"]))),
        "configmap": execute(BASE + ["get", "configmap", "hexalith-keys-config", "-o", "json"],
            lambda r: project_json(r, lambda d: {
                "uid": d["metadata"]["uid"], "resource_version": d["metadata"]["resourceVersion"],
                "hcl_sha256": sha(d["data"]["extraconfig-from-values.hcl"].encode()),
                "peer_names": re.findall(r'leader_tls_servername\s*=\s*"([a-z0-9.-]+)"', d["data"]["extraconfig-from-values.hcl"]),
                "evidence_boundary": "declared HCL; hash does not prove process-loaded configuration",
            })),
        "certificates": execute(BASE + ["get", "certificates.cert-manager.io", "-o", "name"],
            lambda r: {"count": len(r.stdout.splitlines())} if r.returncode == 0 else {"failure": "certificate inventory failed; output discarded"}),
    }


def status(result):
    if result.returncode == 0:
        data = json.loads(result.stdout)
        fields = ["initialized", "sealed", "storage_type", "version", "ha_enabled", "ha_mode", "type", "recovery_seal_type"]
        return {"status": {k: data.get(k) for k in fields},
                "healthy": data.get("initialized") is True and data.get("sealed") is False
                and data.get("storage_type") == "raft" and data.get("ha_enabled") is True}
    mismatch = "x509:" in result.stderr and ("not valid for" in result.stderr or "certificate is valid for" in result.stderr)
    return {"failure": "x509 hostname mismatch" if mismatch else "status query failed; output discarded"}


def probe(index, kind):
    pod = f"hexalith-keys-{index}"
    host = SERVICE if kind == "service-route" else f"{pod}.hexalith-keys-internal.openbao.svc.cluster.local"
    command = BASE + ["exec", pod, "-c", "openbao", "--"] + CLEAN + [
        "bao", "status", "-format=json", f"-address=https://{host}:8200",
        f"-ca-cert={CA}", "-tls-skip-verify=false"]
    if kind == "service-name-control":
        command.append(f"-tls-server-name={SERVICE}")
    return execute(command, status)


def config_hashes(result):
    if result.returncode:
        return {"failure": "configuration fingerprint unavailable; output discarded"}
    matches = re.findall(r"^([0-9a-f]{64})  (/[^\n]+)$", result.stdout, re.M)
    values = {path: digest for digest, path in matches if path in CONFIG_FILES}
    return {"sha256": values} if set(values) == set(CONFIG_FILES) else {"failure": "missing configuration fingerprint; output discarded"}


def public_certificate(index):
    """Inventory only the network-presented public leaf; CA checks are separate."""
    command = BASE + ["port-forward", "--address=127.0.0.1", f"pod/hexalith-keys-{index}", "0:8200"]
    decoder = ["openssl", "x509", "-inform", "DER", "-noout", "-subject", "-issuer", "-dates", "-ext", "subjectAltName"]
    record = {"started_utc": now(), "command": command, "decoder_command": decoder,
              "access": "loopback API-server tunnel; no NetworkPolicy evidence",
              "trust": "unverified public-certificate inventory only; use separate CA-verifying bao probes"}
    process = None
    try:
        process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
        deadline = time.monotonic() + 12
        port = None
        while time.monotonic() < deadline and process.poll() is None:
            if select.select([process.stdout], [], [], 0.2)[0]:
                match = re.search(r"Forwarding from 127\.0\.0\.1:(\d+) -> 8200", process.stdout.readline())
                if match:
                    port = int(match.group(1))
                    break
        if port is None:
            raise ValueError("tunnel unavailable")
        with socket.create_connection(("127.0.0.1", port), timeout=8) as raw:
            with ssl._create_unverified_context().wrap_socket(raw, server_hostname=SERVICE) as stream:
                leaf = stream.getpeercert(binary_form=True)
                tls_version = stream.version()
        result = subprocess.run(decoder, input=leaf, capture_output=True, timeout=8)
        if result.returncode:
            raise ValueError("decoder failed")
        # This is only public certificate metadata, never PEM, private keys or Secret data.
        record.update(exit_code=0, observation={"leaf_der_sha256": sha(leaf), "tls_version": tls_version,
                                               "public_metadata": result.stdout.decode("utf-8")})
    except (OSError, ValueError, subprocess.TimeoutExpired):
        record.update(exit_code=-1, observation={"failure": "public certificate inventory failed; output discarded"})
    finally:
        if process is not None:
            if process.poll() is None:
                process.terminate()
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
            for pipe in [process.stdout]:
                if pipe:
                    pipe.close()
            record["tunnel_terminated_and_awaited"] = True
        record["finished_utc"] = now()
    return record


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, type=Path, help="new evidence JSON; existing paths are refused")
    parser.add_argument("--phase", choices=["preflight", "post-rollout", "rollback"], required=True)
    parser.add_argument("--peer", type=int, choices=range(3), action="append", help="limit per-source probes during staged reload")
    args = parser.parse_args()
    # Reserve the evidence file before contacting Kubernetes, so a typo cannot overwrite a prior capture.
    with args.output.open("x", encoding="utf-8") as output:
        record = {"started_utc": now(), "phase": args.phase, "author": "Codex AI assistant; no independent sign-off",
                  "target": {"context": "jpiquot@local", "namespace": "openbao", "release": "hexalith-keys"},
                  "invocation": ["python3", "tools/openbao/verify_peer_tls.py"] + sys.argv[1:],
                  "source_revision": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
                  "recorder_sha256": sha(Path(__file__).read_bytes()),
                  "preserved_input_sha256": {p: sha((ROOT / p).read_bytes()) for p in PRESERVED},
                  "checkpoint_states": {k: "pending / not complete" for k in ["C4b", "C5b", "C7"]},
                  "story_31_1": "in-progress", "rollout_approval": "not asserted by this recorder"}
        record["before"] = metadata()
        record["probes"] = {}
        record["mounted_config"] = {}
        record["public_certificates"] = {}
        for i in sorted(set(args.peer or range(3))):
            for kind in ["peer-dns", "service-name-control", "service-route"]:
                record["probes"][f"peer-{i}-{kind}"] = probe(i, kind)
            record["mounted_config"][f"peer-{i}"] = execute(
                BASE + ["exec", f"hexalith-keys-{i}", "-c", "openbao", "--"] + CLEAN + ["sha256sum"] + CONFIG_FILES, config_hashes)
            record["public_certificates"][f"peer-{i}"] = public_certificate(i)
        record["after"] = metadata()
        record["finished_utc"] = now()
        checks = [*record["probes"].values(), *record["mounted_config"].values(), *record["public_certificates"].values(),
                  *record["before"].values(), *record["after"].values()]
        record["checks_pass"] = all(c["exit_code"] == 0 and "failure" not in c["observation"] for c in checks)
        record["checks_pass"] &= all(c["observation"].get("healthy") is True for c in record["probes"].values())
        record["identity_stable"] = all(record["before"][k]["observation"] == record["after"][k]["observation"]
                                        for k in ["pods", "configmap"])
        declared_hash = record["before"]["configmap"]["observation"].get("hcl_sha256")
        record["config_hashes_match_declaration"] = bool(declared_hash) and all(
            set(c["observation"].get("sha256", {}).values()) == {declared_hash}
            for c in record["mounted_config"].values())
        record["checks_pass"] &= record["identity_stable"] and record["config_hashes_match_declaration"]
        record["limits"] = ["No Secret reads; CA consumed only by installed bao CLI. No implicit OpenBao credential.",
                            "Status and file hashes do not prove actual Raft join, loaded HCL, failover, ACLs or production qualification.",
                            "Review before/after identities and owner CA/certificate receipt before accepting a post-rollout result."]
        output.write(json.dumps(record, indent=2) + "\n")
    print(json.dumps({"output": str(args.output), "checks_pass": record["checks_pass"],
                      "probes": {k: v["exit_code"] for k, v in record["probes"].items()}}))
    return 0 if record["checks_pass"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
