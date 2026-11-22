"""Exercise the deployed component with an empty disposable bootstrap resolver."""
import http.client
import os
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import tempfile
import time


def successful_read(port):
    connection = http.client.HTTPConnection("127.0.0.1", port, timeout=0.25)
    try:
        connection.request("GET", "/v1.0/secrets/secretstore/redis-secret")
        return 200 <= connection.getresponse().status < 300
    except (OSError, http.client.HTTPException):
        return False
    finally:
        connection.close()


def run(component):
    with tempfile.TemporaryDirectory() as directory:
        fixture = Path(directory)
        resources = fixture / "resources"
        resources.mkdir()
        (fixture / "empty.json").write_text("{}")
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
      value: empty.json
""")
        source = component.read_text()
        expected = "https://hexalith-keys.openbao.svc.cluster.local:8200"
        if source.count(expected) != 1 or source.count("\nscopes:") != 1:
            raise ValueError("Unexpected tracked component structure")
        source = source.replace(expected, "https://127.0.0.1:1")
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
        with subprocess.Popen(command, cwd=fixture, env=environment, stdout=subprocess.PIPE,
                              stderr=subprocess.STDOUT) as process:
            try:
                deadline = time.monotonic() + 15
                succeeded = False
                while process.poll() is None:
                    succeeded |= successful_read(ports[0])
                    if time.monotonic() > deadline:
                        raise TimeoutError()
                    time.sleep(0.01)
                succeeded |= successful_read(ports[0])
                diagnostic = process.communicate(timeout=2)[0]
            finally:
                if process.poll() is None:
                    process.kill()
                    process.communicate()
        return (process.returncode != 0 and not succeeded
                and b"openbao-runtime-bootstrap not found" in diagnostic
                and b"failed to load components" in diagnostic
                and b"initialization error occurred for secretstore (secretstores.hashicorp.vault/v1)" in diagnostic
                and b"Component loaded: secretstore (" not in diagnostic)


if __name__ == "__main__":
    try:
        passed = run(Path(sys.argv[1]))
    except (OSError, ValueError, TimeoutError, subprocess.SubprocessError):
        passed = False
    print("Missing bootstrap: component failed, no successful read or loaded fallback."
          if passed else "Disposable component failure contract was not observed (check daprd on PATH).")
    sys.exit(0 if passed else 1)
