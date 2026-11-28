"""Exercise the deployed component with disposable bootstrap resolvers.

Case 1 supplies no bootstrap Secret. Case 2 supplies a synthetic bootstrap with an unreachable
OpenBao address and records whether the component load or the read failed.
"""
import http.client
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import tempfile
import time

UNREACHABLE = "https://127.0.0.1:1"


def request(port, path):
    connection = http.client.HTTPConnection("127.0.0.1", port, timeout=3)
    try:
        connection.request("GET", path)
        response = connection.getresponse()
        return response.status, response.read()
    except (OSError, http.client.HTTPException):
        return None, b""
    finally:
        connection.close()


def successful_read(port):
    status, _ = request(port, "/v1.0/secrets/secretstore/redis-secret")
    return status is not None and 200 <= status < 300


def run(component, bootstrap=None):
    """Start daprd with the tracked component. Returns (exit code, read status, stores, diagnostic, succeeded)."""
    with tempfile.TemporaryDirectory() as directory:
        fixture = Path(directory)
        resources = fixture / "resources"
        resources.mkdir()
        (fixture / "bootstrap.json").write_text(json.dumps(bootstrap or {}))
        (resources / "bootstrap.yaml").write_text("""
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: bootstrap
spec:
  type: secretstores.local.file
  version: v1
  metadata:
    - name: secretsFile
      value: bootstrap.json
    - name: multiValued
      value: "true"
""")
        source = component.read_text()
        expected = "https://hexalith-keys.openbao.svc.cluster.local:8200"
        if source.count(expected) != 1 or source.count("\nscopes:") != 1 or "\nauth:" in source:
            raise ValueError("Unexpected tracked component structure")
        source = source.replace(expected, UNREACHABLE)
        source = source.replace("\nscopes:", "\nauth:\n  secretStore: bootstrap\nscopes:")
        (resources / "secretstore.yaml").write_text(source)
        environment = {k: v for k, v in os.environ.items()
                       if not k.startswith(("DAPR_", "VAULT_", "BAO_", "KUBERNETES_", "OPENBAO_"))}
        ports = []
        sockets = []
        try:
            for _ in range(5):
                sock = socket.socket()
                sockets.append(sock)
                sock.bind(("127.0.0.1", 0))
                ports.append(sock.getsockname()[1])
        finally:
            for sock in sockets:
                sock.close()
        flags = ["--dapr-http-port", "--dapr-grpc-port", "--dapr-internal-grpc-port",
                 "--dapr-public-port", "--metrics-port"]
        binary = os.environ.get("DAPRD") or shutil.which("daprd") or str(Path.home() / ".dapr/bin/daprd")
        command = [binary, "--mode", "standalone", "--app-id", "memories",
                   "--resources-path", str(resources), "--dapr-listen-addresses", "127.0.0.1",
                   "--enable-metrics=false"]
        for flag, port in zip(flags, ports):
            command.extend([flag, str(port)])
        read_status, stores = None, []
        log_path = fixture / "daprd.log"
        # Polling readiness must not leave daprd blocked on an undrained pipe.
        with log_path.open("wb") as log, subprocess.Popen(
                command, cwd=fixture, env=environment, stdout=log, stderr=subprocess.STDOUT) as process:
            try:
                deadline = time.monotonic() + 15
                succeeded = False
                while process.poll() is None:
                    succeeded |= successful_read(ports[0])
                    status, _ = request(ports[0], "/v1.0/healthz/outbound")
                    if status is not None and status < 300:
                        # Started: record the registered secret stores, then the read outcome.
                        status, body = request(ports[0], "/v1.0/metadata")
                        if status == 200:
                            stores = [(item.get("name"), item.get("type")) for item in json.loads(body).get("components", [])
                                      if str(item.get("type", "")).startswith("secretstores.")]
                        read_status, _ = request(ports[0], "/v1.0/secrets/secretstore/redis-secret")
                        succeeded |= read_status is not None and 200 <= read_status < 300
                        process.terminate()
                        break
                    if time.monotonic() > deadline:
                        raise TimeoutError()
                    time.sleep(0.01)
                if read_status is None:
                    succeeded |= successful_read(ports[0])
                process.wait(timeout=10)
            finally:
                if process.poll() is None:
                    process.kill()
                    process.wait()
        return process.returncode, read_status, stores, log_path.read_bytes(), succeeded


def missing_bootstrap(component):
    code, read_status, _, diagnostic, succeeded = run(component)
    return (code != 0 and read_status is None and not succeeded
            and b"openbao-runtime-bootstrap not found" in diagnostic
            and b"failed to load components" in diagnostic
            and b"initialization error occurred for secretstore (secretstores.hashicorp.vault/v1)" in diagnostic
            and b"Component loaded: secretstore (" not in diagnostic)


def synthetic_ca():
    with tempfile.TemporaryDirectory() as directory:
        key, cert = Path(directory) / "key.pem", Path(directory) / "cert.pem"
        subprocess.run(["openssl", "req", "-x509", "-newkey", "ec", "-pkeyopt", "ec_paramgen_curve:prime256v1",
                        "-nodes", "-keyout", str(key), "-out", str(cert), "-days", "1", "-subj", "/CN=synthetic-ca"],
                       check=True, capture_output=True)
        return cert.read_text()


def unreachable_openbao(component):
    """Returns the failed stage ("load" or "read") when the contract holds, otherwise None."""
    bootstrap = {"openbao-runtime-bootstrap": {"token": "synthetic-token", "ca.pem": synthetic_ca()}}
    code, read_status, stores, diagnostic, succeeded = run(component, bootstrap)
    if succeeded or b"openbao-runtime-bootstrap not found" in diagnostic:
        return None
    if read_status is None:
        loaded = b"Component loaded: secretstore (" in diagnostic
        failed = b"initialization error occurred for secretstore (secretstores.hashicorp.vault/v1)" in diagnostic
        return "load" if code != 0 and failed and not loaded else None
    # No fallback: the only registered store named secretstore is the Vault component.
    vault_only = [store for store in stores if store[0] == "secretstore"] == [("secretstore", "secretstores.hashicorp.vault")]
    others = {store for store in stores if store[0] != "secretstore"} <= {("bootstrap", "secretstores.local.file")}
    return f"read (HTTP {read_status})" if read_status >= 400 and vault_only and others else None


if __name__ == "__main__":
    component = Path(sys.argv[1])
    try:
        passed = missing_bootstrap(component)
    except (OSError, ValueError, TimeoutError, subprocess.SubprocessError):
        passed = False
    print("Missing bootstrap: component failed, no successful read or loaded fallback."
          if passed else "Disposable component failure contract was not observed (check daprd on PATH).")
    try:
        stage = unreachable_openbao(component)
    except (OSError, ValueError, TimeoutError, subprocess.SubprocessError, json.JSONDecodeError):
        stage = None
    print(f"Unreachable OpenBao with synthetic bootstrap: {stage} failed, no successful read or fallback."
          if stage else "Unreachable OpenBao contract was not observed (check daprd and openssl on PATH).")
    sys.exit(0 if passed and stage else 1)
