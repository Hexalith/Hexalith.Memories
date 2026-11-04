#!/usr/bin/env python3
"""Digest-bound Development workload health; never enables Production lifecycle writes."""

import argparse
import base64
from contextlib import contextmanager
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import sys
import tempfile
import time

sys.path.insert(0, str(Path(__file__).parent))
from importlib.util import module_from_spec, spec_from_file_location

specification = spec_from_file_location("memories_container_contract", Path(__file__).with_name("container-publication-contract.py"))
contract = module_from_spec(specification)
specification.loader.exec_module(contract)


def command(arguments):
    return subprocess.run(arguments, capture_output=True, text=True, check=False, timeout=150)


# Reuse the exact non-Production Dapr dependency already declared by this repository.
DAPR_TEST_AUTHORITY_IMAGE = "ghcr.io/dapr/daprd:1.18.1@sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8"
PROBE_IMAGE = "docker.io/library/busybox:1.37.0@sha256:a7dbc319b1438115a207e11930dc4bb3e643dcb3ec0bdd694bb36cd8fad289ec"


class CleanupFailure(ValueError):
    """A test dependency could not be removed."""


@contextmanager
def server_dependencies(network, logs):
    """Supply the two keyed stores required by Server startup on its private network."""
    images = {
        "redis": "redis/redis-stack-server:7.4.0-v8@sha256:798ab84d9f266936b034ab11c4d04a2b8e4b441884c5aa7d17ac951eefdf742a",
        "falkordb": "falkordb/falkordb:v4.12.0@sha256:7927eb194df0fadf70e2deb6c20c8178743ec92aacd739c790f8a35ce5dee613",
    }
    attempted = []
    try:
        for key, image in images.items():
            name = network + "-" + key
            if command(["docker", "pull", "--platform", "linux/amd64", image]).returncode:
                raise ValueError("Isolated Server dependency pull failed.")
            attempted.append(name)
            if command(["docker", "run", "--detach", "--platform", "linux/amd64", "--network", network, "--name", name, image]).returncode:
                raise ValueError("Isolated Server dependency did not start.")
            deadline = time.monotonic() + 30
            while True:
                ping = command(["docker", "exec", name, "redis-cli", "ping"])
                if ping.returncode == 0 and ping.stdout.strip() == "PONG":
                    break
                if time.monotonic() >= deadline:
                    raise ValueError("Isolated Server dependency did not become available.")
                time.sleep(1)
            logs.append(f"dependency={key};image={image};ready=pass")
        yield {f"ConnectionStrings__{key}": f"{network}-{key}:6379,abortConnect=false" for key in images}
    finally:
        failed = False
        for name in reversed(attempted):
            removed = command(["docker", "rm", "--force", name]).returncode == 0
            logs.append(f"dependency={name};cleanup={'pass' if removed else 'failure'}")
            failed |= not removed
        if failed:
            raise CleanupFailure("Isolated Server dependency cleanup failed.")


@contextmanager
def clock_authority(network):
    """Fresh local-file secret authority on this run's isolated Docker bridge.

    DaprClient.GetSecretAsync uses gRPC, so the test uses a real isolated Dapr
    sidecar with local-file secrets rather than substituting its HTTP endpoint.
    """
    with tempfile.TemporaryDirectory(prefix="memories-clock-authority-") as temporary:
        private = Path(temporary) / "signer.pem"
        private.touch(mode=0o600)
        generated = command(["openssl", "genpkey", "-algorithm", "EC", "-pkeyopt", "ec_paramgen_curve:P-256", "-out", str(private)])
        if generated.returncode:
            raise ValueError("Could not create the isolated clock test signing authority.")
        pkcs8 = subprocess.run(["openssl", "pkcs8", "-topk8", "-nocrypt", "-in", str(private), "-outform", "DER"], capture_output=True, check=True, timeout=30).stdout
        public = subprocess.run(["openssl", "pkey", "-in", str(private), "-pubout", "-outform", "DER"], capture_output=True, check=True, timeout=30).stdout
        token = secrets.token_hex(32)
        authority = "memories-clock-authority-" + secrets.token_hex(6)
        components = Path(temporary) / "components"
        components.mkdir()
        secret = Path(temporary) / "secrets.json"
        contract.write_json(secret, {"access-telemetry-clock-key": base64.b64encode(pkcs8).decode()})
        secret.chmod(0o600)
        (components / "secretstore.yaml").write_text("""apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: access-telemetry-secrets
spec:
  type: secretstores.local.file
  version: v1
  metadata:
    - name: secretsFile
      value: /authority/secrets.json
""", encoding="utf-8")
        pulled = command(["docker", "pull", "--platform", "linux/amd64", DAPR_TEST_AUTHORITY_IMAGE])
        if pulled.returncode:
            raise ValueError("Isolated clock test authority image could not be pulled.")
        try:
            started = command(["docker", "run", "--detach", "--platform", "linux/amd64", "--network", network, "--user", "0:0",
                               "--name", authority, "--env", "DAPR_API_TOKEN=" + token,
                               "--mount", f"type=bind,source={temporary},destination=/authority,readonly", "--entrypoint", "/daprd",
                               DAPR_TEST_AUTHORITY_IMAGE, "--app-id", "publication-clock-test",
                               "--mode", "standalone", "--resources-path", "/authority/components",
                               "--log-level", "error", "--enable-metrics=false"])
            if started.returncode:
                raise ValueError("Isolated clock test authority did not start.")
            yield {"DAPR_HTTP_ENDPOINT": f"http://{authority}:3500", "DAPR_GRPC_ENDPOINT": f"http://{authority}:50001", "DAPR_API_TOKEN": token,
                   "Clock__SecretStoreName": "access-telemetry-secrets", "Clock__AllowDevelopmentSources": "true",
                   "Clock__SignerKeyEpoch": "publication-test-only", "AccessTelemetryLifecycle__ClockSignerKeyEpoch": "publication-test-only",
                   "AccessTelemetryLifecycle__AttestationVerificationKey": base64.b64encode(public).decode()}
        finally:
            if command(["docker", "rm", "--force", authority]).returncode:
                raise CleanupFailure("Clock test authority cleanup failed.")


@contextmanager
def dapr_authority(network):
    """Provide the isolated Dapr sidecar required by workload liveness checks."""
    authority = "memories-dapr-authority-" + secrets.token_hex(6)
    token = secrets.token_hex(32)
    if command(["docker", "pull", "--platform", "linux/amd64", DAPR_TEST_AUTHORITY_IMAGE]).returncode:
        raise ValueError("Isolated Dapr test authority image could not be pulled.")
    started = command(["docker", "run", "--detach", "--platform", "linux/amd64", "--network", network,
                       "--name", authority, "--env", "DAPR_API_TOKEN=" + token, "--entrypoint", "/daprd",
                       DAPR_TEST_AUTHORITY_IMAGE, "--app-id", "publication-workload-test", "--mode", "standalone",
                       "--log-level", "error", "--enable-metrics=false"])
    if started.returncode:
        raise ValueError("Isolated Dapr test authority did not start.")
    try:
        yield {"DAPR_HTTP_ENDPOINT": f"http://{authority}:3500", "DAPR_GRPC_ENDPOINT": f"http://{authority}:50001",
               "DAPR_API_TOKEN": token}
    finally:
        if command(["docker", "rm", "--force", authority]).returncode:
            raise CleanupFailure("Dapr test authority cleanup failed.")


def health_response_valid(name, body):
    try:
        document = json.loads(body)
        key = "access-telemetry-lifecycle" if name == "access-telemetry" else "clock-attestation"
        entry = document["entries"][key]
        if document["status"] != "Healthy" or entry["status"] != "Healthy":
            return False
        return name != "access-telemetry" or entry["description"] == "Access telemetry lifecycle writes are disabled."
    except (KeyError, TypeError, ValueError):
        return False


def probe(container, endpoint):
    # An internal Docker network has no published host port. Share only the
    # workload's network namespace with a digest-pinned, disposable HTTP probe.
    return command(["docker", "run", "--rm", "--pull=never", "--network", "container:" + container,
                    PROBE_IMAGE, "wget", "-q", "-T", "5", "-t", "1", "-O", "-", "http://127.0.0.1:8080" + endpoint])


def poll_readiness(name, container, smoke, timeout):
    deadline = time.monotonic() + timeout
    while True:
        result = probe(container, "/ready")
        if result.returncode == 0 and health_response_valid(name, result.stdout):
            return "pass"
        state = smoke._container_state(container)
        if state and not state.startswith("running|"):
            return "image-start-failure"
        if time.monotonic() >= deadline:
            return "readiness-timeout"
        time.sleep(2)


def poll_liveness(container, smoke, timeout, logs):
    deadline = time.monotonic() + timeout
    while True:
        if probe(container, "/alive").returncode == 0:
            return "pass"
        state = smoke._container_state(container)
        if state:
            logs.append(f"container_state={state}")
            if not state.startswith("running|"):
                return "image-start-failure"
        if time.monotonic() >= deadline:
            return "liveness-timeout"
        time.sleep(2)


def run_platform(name, repository, platform, digest, directory, smoke, timeout):
    container = f"memories-publication-{name}-{platform.split('/')[1]}-{os.getpid()}"
    network = container + "-network"
    image = repository + "@" + digest
    result = "fail"
    cleanup = "pass"
    logs = [f"platform={platform}", f"image={image}", "environment=Development"]
    network_attempted = False
    container_attempted = False
    try:
        result = smoke._pull_immutable(platform, image)
        if result != "pass":
            return {"platform": platform, "digest": digest, "outcome": result, "cleanup": cleanup}
        created = command(["docker", "network", "create", "--internal", network])
        if created.returncode:
            raise ValueError("Isolated test network could not be created.")
        network_attempted = True

        def execute(extra):
            nonlocal container_attempted
            settings = {"ASPNETCORE_ENVIRONMENT": "Development", "DOTNET_ENVIRONMENT": "Development",
                        "ASPNETCORE_URLS": "http://+:8080", "AccessTelemetryLifecycle__Enabled": "false",
                        "Authentication__JwtBearer__Issuer": smoke.SMOKE_JWT_ISSUER,
                        "Authentication__JwtBearer__Audience": smoke.SMOKE_JWT_AUDIENCE,
                        "Authentication__JwtBearer__SigningKey": smoke.SMOKE_JWT_SIGNING_KEY,
                        "Authentication__JwtBearer__AllowInsecureSymmetricKey": "true", **extra}
            arguments = ["docker", "run", "--detach", "--platform", platform, "--network", network,
                         "--name", container]
            for key, value in settings.items():
                arguments += ["--env", key + "=" + value]
            container_attempted = True
            started = command(arguments + [image])
            if started.returncode:
                return smoke._classify_start_failure(started)
            if name in ("access-telemetry", "access-telemetry-clock"):
                return poll_readiness(name, container, smoke, timeout)
            return poll_liveness(container, smoke, timeout, logs)

        if name == "access-telemetry-clock":
            with clock_authority(network) as settings:
                result = execute(settings)
        elif name == "server":
            with server_dependencies(network, logs) as stores, dapr_authority(network) as sidecar:
                result = execute({**stores, **sidecar})
        elif name == "mcp":
            with dapr_authority(network) as settings:
                result = execute(settings)
        else:
            result = execute({})
    except CleanupFailure as error:
        result = "cleanup-failure"
        cleanup = "failure"
        logs.append(contract.redact(f"error={type(error).__name__}: {error}"))
    except Exception as error:
        result = "workload-health-failure"
        logs.append(contract.redact(f"error={type(error).__name__}: {error}"))
    finally:
        if container_attempted:
            # Reuse approved bounded runtime diagnostics and exact removal checks.
            smoke._append_runtime_evidence(container, result, logs)
            if smoke._remove_container(container) != "pass":
                cleanup = "failure"
        if network_attempted and command(["docker", "network", "rm", network]).returncode:
            cleanup = "failure"
        if cleanup != "pass":
            result = "cleanup-failure"
        logs += [f"cleanup={cleanup}", f"outcome={result}"]
        (directory / f"health-{platform.split('/')[1]}.log").write_text(contract.redact("\n".join(logs)) + "\n", encoding="utf-8")
    return {"platform": platform, "digest": digest, "outcome": result, "cleanup": cleanup}


def verify(name, image, directory, shared, set_sha256, timeout=180):
    _, _, smoke, _ = contract.load_shared(shared)
    children = smoke._load_children(directory)
    validation = contract.read_json(directory / "oci-validation.json")
    if validation.get("image") != image:
        raise ValueError("Health image differs from the validated OCI image.")
    repository = smoke._repository_without_tag(image)
    outcome, _ = smoke._preflight(repository, children[smoke.ARM64_PLATFORM], directory)
    if outcome == "pass" and command(["docker", "pull", "--platform", "linux/amd64", PROBE_IMAGE]).returncode:
        raise ValueError("Digest-pinned private-network HTTP probe could not be pulled.")
    platforms = []
    if outcome == "pass":
        platforms = [run_platform(name, repository, platform, children[platform], directory, smoke, timeout) for platform in contract.PLATFORMS]
        outcome = "pass" if all(item["outcome"] == "pass" and item["cleanup"] == "pass" for item in platforms) else "workload-health-failure"
    summary = {"result": outcome, "name": name, "image": image, "setSha256": set_sha256,
               "ociValidationSha256": contract.sha256(directory / "oci-validation.json"), "indexDigest": validation["index_digest"],
               "environment": "Development", "endpoint": "/ready" if name in ("access-telemetry", "access-telemetry-clock") else "/alive",
               "contract": next(item["health"] for item in contract.CANONICAL_IMAGES if item["name"] == name),
               "lifecycleWritesEnabled": False, "authorities": "fresh isolated test signing authority and Development UTC sources" if name == "access-telemetry-clock" else "Development only",
               "platforms": platforms}
    contract.write_json(directory / "health.json", summary)
    return summary


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--name", choices=[image["name"] for image in contract.CANONICAL_IMAGES], required=True)
    parser.add_argument("--image", required=True)
    parser.add_argument("--evidence-directory", type=Path, required=True)
    parser.add_argument("--shared-directory", type=Path, required=True)
    parser.add_argument("--set-sha256", required=True)
    arguments = parser.parse_args()
    try:
        if not re.fullmatch(r"[0-9a-f]{64}", arguments.set_sha256):
            raise ValueError("Health requires a frozen release-set digest.")
        summary = verify(arguments.name, arguments.image, arguments.evidence_directory, arguments.shared_directory, arguments.set_sha256)
        return 0 if summary["result"] == "pass" else 1
    except Exception as error:
        contract.write_json(arguments.evidence_directory / "health.json", {"result": "fail", "error": contract.redact(error), "setSha256": arguments.set_sha256})
        print("[container-health] workload health proof failed; see retained health evidence.", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
