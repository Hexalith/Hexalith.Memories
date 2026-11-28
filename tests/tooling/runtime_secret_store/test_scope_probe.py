"""Replay the in-pod BusyBox probe against synthetic loopback responses."""
from http.server import BaseHTTPRequestHandler, HTTPServer
import json
import os
from pathlib import Path
import secrets
import select
import shutil
import socket
import subprocess
import tempfile
import threading
import unittest

PROBE = Path(__file__).resolve().parents[3] / "tools/probe-dapr-secret-scope.sh"
VAULT = "secretstores.hashicorp.vault"
RUNTIME = {"name": "secretstore", "type": VAULT}
TELEMETRY = {"name": "access-telemetry-secrets", "type": VAULT}
BUILTIN = {"name": "kubernetes", "type": "secretstores.kubernetes"}
SYNTHETIC = "dapr-scope-probe-synthetic-absent"


class ScopeProbeTests(unittest.TestCase):
    def setUp(self):
        self.token = secrets.token_hex(16)
        self.credential = 'synthetic-escaped-quote-"' + secrets.token_hex(16)
        self.version_canary = "1.19.2-private-" + secrets.token_hex(16)
        busybox = shutil.which("busybox")
        if busybox is None:
            raise FileNotFoundError("busybox is required for probe replay")
        probe_directory = tempfile.TemporaryDirectory()
        self.addCleanup(probe_directory.cleanup)
        self.probe_path = probe_directory.name
        (Path(self.probe_path) / "busybox").symlink_to(Path(busybox).resolve())
        self.state = {"requests": []}
        token, credential, state = self.token, self.credential, self.state
        canceled = {"errorCode": "ERR_SECRET_GET", "message": "context canceled " + credential}

        class Handler(BaseHTTPRequestHandler):
            def do_GET(self):
                state["requests"].append(self.path)
                if self.headers.get("dapr-api-token") != token:
                    status, body = 401, {"message": credential}
                elif self.path == "/v1.0/metadata":
                    raw = state["metadata"]
                    status, body = 200, (raw if isinstance(raw, bytes) else dict(raw, components=state["components"]))
                elif self.path == "/v1.0/secrets/" + state["store"] + "/" + state["key"]:
                    status, body = state["response"]
                    # daprd decides scope and name policy locally, then cancels the store read
                    # when the client half-closes before the response, as BusyBox nc does at stdin EOF.
                    if status not in (401, 403) and client_closed(self.connection):
                        status, body = 500, canceled
                else:
                    status, body = 404, {}
                self.send_response(status)
                self.end_headers()
                self.wfile.write(body if isinstance(body, bytes) else json.dumps(body).encode())

            def log_message(self, *args):
                pass

        def client_closed(connection):
            ready, _, _ = select.select([connection], [], [], 0.25)
            return bool(ready) and connection.recv(1, socket.MSG_PEEK) == b""

        self.server = HTTPServer(("127.0.0.1", 0), Handler)
        thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        thread.start()
        self.addCleanup(self.server.server_close)
        self.addCleanup(thread.join)
        self.addCleanup(self.server.shutdown)

    def replay(self, cases, *, metadata_only=False):
        """Replay probe cases with an optional expected runtime diagnostic."""
        probe = PROBE.read_bytes()
        for index, (args, metadata, components, store, key, response, expected_exit, *runtime_marker) in enumerate(cases):
            with self.subTest(case=index, args=args):
                self.state.update(requests=[], metadata=metadata, components=components, store=store, key=key, response=response)
                env = dict(os.environ, PATH=self.probe_path, DAPR_API_TOKEN=self.token,
                           DAPR_HTTP_PORT=str(self.server.server_port))
                result = subprocess.run([str(Path(self.probe_path) / "busybox"), "sh", "-s", "--", *args], input=probe, env=env, capture_output=True, timeout=20)
                output = result.stdout + result.stderr
                # The credential contains a quote, so also check its JSON-escaped form.
                leaks = (self.token, self.credential, json.dumps(self.credential)[1:-1], self.version_canary)
                self.assertFalse(any(value.encode() in output for value in leaks),
                                 "Credential appeared in probe diagnostics")
                self.assertEqual(result.returncode, expected_exit)
                if expected_exit == 2:
                    self.assertEqual(self.state["requests"], [], "Invalid arguments sent an HTTP request")
                elif metadata_only:
                    self.assertEqual(self.state["requests"], ["/v1.0/metadata"])
                else:
                    self.assertEqual(self.state["requests"][0], "/v1.0/metadata")
                    self.assertEqual(len(self.state["requests"]), 2, "The probe stopped before its secret request")
                if runtime_marker:
                    self.assertIn(f" runtime={runtime_marker[0]} ".encode(), output)

    def test_authentication_policy_scope_and_key_presence(self):
        credential = self.credential
        policy = (403, {"errorCode": "ERR_PERMISSION_DENIED", "message": credential})
        unavailable = (401, {"errorCode": "ERR_SECRET_STORE_NOT_FOUND", "message": credential})
        found = (200, {"password": credential})

        def meta(app, version="1.19.2"):
            return {"id": app, "runtimeVersion": version}

        runtime = ("secretstore", "redis-secret")
        outside = ("secretstore", "access-telemetry-marker-key")
        telemetry = ("access-telemetry-secrets", "redis-secret")
        cases = [
            (("memories", "allowed"), meta("memories", self.version_canary), [RUNTIME], *runtime, found, 0, "unchecked"),
            (("memories", "outside"), meta("memories"), [RUNTIME], *outside, policy, 0),
            (("memories-mcp", "unscoped"), meta("memories-mcp"), [], *runtime, unavailable, 0),
            (("memories-mcp", "unscoped"), meta("memories-mcp"), [], *runtime, policy, 0),
            (("memories-mcp", "unscoped"), meta("memories-mcp"), [], *runtime, (401, {"message": credential}), 1),
            (("memories", "outside"), meta("memories"), [RUNTIME], *outside, (404, {}), 1),
            (("memories", "allowed"), meta("memories"), [RUNTIME], *runtime,
             (200, {"other": '"password":"' + credential + '"'}), 1),
            (("memories", "allowed"), meta("memories"), [RUNTIME], *runtime, (200, {"password": ""}), 1),
            (("memories", "outside"), meta("memories"), [RUNTIME], *outside, found, 1),
            (("memories", "allowed"), meta("memories"), [RUNTIME], *runtime, (200, {"child": {"password": credential}}), 1),
            (("eventstore", "allowed"), meta("eventstore"), [RUNTIME], *runtime, found, 0),
            # The lifecycle app reads its own telemetry store, and the runtime store is outside its scopes.
            (("memories-access-telemetry", "allowed"), meta("memories-access-telemetry"), [TELEMETRY], *telemetry, found, 0),
            (("memories-access-telemetry", "outside"), meta("memories-access-telemetry"), [TELEMETRY],
             "access-telemetry-secrets", "access-telemetry-clock-key", policy, 0),
            (("memories-access-telemetry", "unscoped"), meta("memories-access-telemetry"), [TELEMETRY], *runtime, unavailable, 0),
            (("memories-access-telemetry", "outside"), meta("memories-access-telemetry"), [TELEMETRY],
             "access-telemetry-secrets", "access-telemetry-clock-key", found, 1),
            # The probe only prints the requested version after a match.
            (("memories", "allowed", "1.19.2"), meta("memories"), [RUNTIME], *runtime, found, 0, "1.19.2"),
            (("Memories", "allowed"), meta("Memories"), [RUNTIME], *runtime, found, 2),
            (("memories", "allowed", "1.19.2;x"), meta("memories"), [RUNTIME], *runtime, found, 2),
            (("memories", "allowed", "1.19.2-rc-test.1"), meta("memories", "1.19.2-rc-test.1"),
             [RUNTIME], *runtime, found, 0, "1.19.2-rc-test.1"),
            (("memories", "allowed", "0.0.0-test.0.01alpha"), meta("memories", "0.0.0-test.0.01alpha"),
             [RUNTIME], *runtime, found, 0, "0.0.0-test.0.01alpha"),
            # Check arity and the explicit version before contacting the sidecar.
            ((), meta("memories"), [RUNTIME], *runtime, found, 2),
            (("memories",), meta("memories"), [RUNTIME], *runtime, found, 2),
            (("memories", "allowed", "1.19.2", "extra"), meta("memories"), [RUNTIME], *runtime, found, 2),
            (("memories", "allowed", ""), meta("memories"), [RUNTIME], *runtime, found, 2),
            (("memories", "allowed", "1.19"), meta("memories"), [RUNTIME], *runtime, found, 2),
            (("memories", "allowed", "1.19.2+build"), meta("memories"), [RUNTIME], *runtime, found, 2),
            # The probe's placeholder words are never an expected version.
            (("memories", "allowed", "absent"), {"id": "memories"}, [RUNTIME], *runtime, found, 2),
        ]
        cases.extend(
            (("memories", "allowed", version), meta("memories", version), [RUNTIME], *runtime, found, 2)
            for version in ("01.19.2", "1.019.2", "1.19.02", "1.19.2-", "1.19.2-.rc", "1.19.2-rc..1",
                            "1.19.2-rc.", "1.19.2-rc.01")
        )
        self.replay(cases)
        self.replay([
            (("memories", "allowed"), meta("memories"), [], *runtime, found, 1),
            (("memories-mcp", "unscoped"), meta("memories-mcp"), [RUNTIME], *runtime, policy, 1),
            (("memories-access-telemetry", "allowed"), meta("memories-access-telemetry"), [RUNTIME], *telemetry, found, 1),
            (("memories-access-telemetry", "unscoped"), meta("memories-access-telemetry"), [RUNTIME, TELEMETRY], *runtime, policy, 1),
            (("memories", "allowed", "1.19.2"), meta("memories", self.version_canary), [RUNTIME], *runtime, found, 1, "mismatch"),
            (("memories", "allowed", "1.19.2"), meta("memories", credential), [RUNTIME], *runtime, found, 1, "unrecognized"),
            (("memories", "allowed", "1.19.2"), meta("memories", "1.19.2+build"), [RUNTIME], *runtime, found, 1, "unrecognized"),
            (("memories", "allowed", "1.19.2"), meta("memories", ["1.19.2"]), [RUNTIME], *runtime, found, 1, "unrecognized"),
            # A malformed sidecar version is unrecognized even when the expected version is valid.
            (("memories", "allowed", "1.19.2"), meta("memories", "01.19.2"), [RUNTIME], *runtime, found, 1, "unrecognized"),
            (("memories", "allowed", "1.19.2"), meta("memories", "1.19.2-rc..1"), [RUNTIME], *runtime, found, 1, "unrecognized"),
            # An unparseable metadata body never reports a runtime version.
            (("memories", "allowed"), b"not-json", [RUNTIME], *runtime, found, 1, "unrecognized"),
        ], metadata_only=True)

    def test_builtin_kubernetes_store_denial_and_runtime_identity(self):
        credential = self.credential
        policy = (403, {"errorCode": "ERR_PERMISSION_DENIED", "message": credential})

        def meta(app, version="1.19.2"):
            return {"id": app, "runtimeVersion": version}

        builtin = ("kubernetes", SYNTHETIC)
        cases = [
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN, RUNTIME, TELEMETRY], *builtin, policy, 0),
            (("memories-mcp", "builtin", "1.19.2"), meta("memories-mcp"), [BUILTIN], *builtin, policy, 0),
            (("memories-access-telemetry", "builtin", "1.19.2"), meta("memories-access-telemetry"), [BUILTIN, TELEMETRY],
             *builtin, policy, 0),
            (("memories", "builtin", "1.19.2-rc-test.1"), meta("memories", "1.19.2-rc-test.1"),
             [BUILTIN, RUNTIME], *builtin, policy, 0),
            (("memories", "builtin", "1.19.2+build"), meta("memories"), [BUILTIN, RUNTIME], *builtin, policy, 2),
            # The built-in check needs an expected runtime version.
            (("memories", "builtin"), meta("memories"), [BUILTIN, RUNTIME], *builtin, policy, 2),
            (("memories", "builtin", "unrecognized"), meta("memories", credential), [BUILTIN, RUNTIME], *builtin, policy, 2),
            # Only a lowercase app ID passes. BusyBox awk -v would decode the escape to "memories".
            (("Memories", "builtin", "1.19.2"), meta("Memories"), [BUILTIN, RUNTIME], *builtin, policy, 2),
            (("memorie\\163", "builtin", "1.19.2"), meta("memories"), [BUILTIN, RUNTIME], *builtin, policy, 2),
            # Only an explicit policy denial passes. A missed lookup means the policy is absent.
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], *builtin,
             (500, {"errorCode": "ERR_SECRET_GET", "message": credential}), 1),
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], *builtin, (200, {"password": credential}), 1),
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], *builtin, (200, {}), 1),
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], *builtin,
             (403, {"errorCode": "ERR_SECRET_STORE_NOT_FOUND", "message": credential}), 1),
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], *builtin, (403, {"message": credential}), 1),
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], *builtin,
             (401, {"errorCode": "ERR_SECRET_STORE_NOT_FOUND", "message": credential}), 1),
            # A request for any other key or store answers 404 and fails the check.
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], "kubernetes", "openbao-runtime-bootstrap", policy, 1),
        ]
        self.replay(cases)
        self.replay([
            # Identity and runtime must match, and the expected store must be registered exactly once.
            (("memories", "builtin", "1.19.2"), meta("memories-mcp"), [BUILTIN, RUNTIME], *builtin, policy, 1),
            (("memories", "builtin", "1.19.2"), meta("memories", "1.18.1"), [BUILTIN, RUNTIME], *builtin, policy, 1, "mismatch"),
            (("memories", "builtin", "1.19.2"), {"id": "memories"}, [BUILTIN, RUNTIME], *builtin, policy, 1, "absent"),
            (("memories", "builtin", "1.19.2"), meta("memories", credential), [BUILTIN, RUNTIME], *builtin, policy, 1, "unrecognized"),
            (("memories", "builtin", "1.19.2"), meta("memories"), [RUNTIME], *builtin, policy, 1),
            (("memories", "builtin", "1.19.2"), meta("memories"),
             [{"name": "kubernetes", "type": "secretstores.local.file"}], *builtin, policy, 1),
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN, BUILTIN], *builtin, policy, 1),
        ], metadata_only=True)
