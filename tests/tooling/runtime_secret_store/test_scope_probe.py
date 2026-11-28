"""Replay the in-pod BusyBox probe against synthetic loopback responses."""
from http.server import BaseHTTPRequestHandler, HTTPServer
import json
import os
from pathlib import Path
import secrets
import select
import socket
import subprocess
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
        self.state = {}
        token, credential, state = self.token, self.credential, self.state
        canceled = {"errorCode": "ERR_SECRET_GET", "message": "context canceled " + credential}

        class Handler(BaseHTTPRequestHandler):
            def do_GET(self):
                if self.headers.get("dapr-api-token") != token:
                    status, body = 401, {"message": credential}
                elif self.path == "/v1.0/metadata":
                    status, body = 200, dict(state["metadata"], components=state["components"])
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
                self.wfile.write(json.dumps(body).encode())

            def log_message(self, *args):
                pass

        def client_closed(connection):
            ready, _, _ = select.select([connection], [], [], 0.25)
            return bool(ready) and connection.recv(1, socket.MSG_PEEK) == b""

        self.server = HTTPServer(("127.0.0.1", 0), Handler)
        thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        thread.start()
        self.addCleanup(thread.join)
        self.addCleanup(self.server.server_close)
        self.addCleanup(self.server.shutdown)

    def replay(self, cases):
        """Run each case as a subTest. A case is (args, metadata, components, store, key, response, passed)."""
        for index, (args, metadata, components, store, key, response, passed) in enumerate(cases):
            with self.subTest(case=index, args=args):
                self.state.update(metadata=metadata, components=components, store=store, key=key, response=response)
                env = dict(os.environ, DAPR_API_TOKEN=self.token, DAPR_HTTP_PORT=str(self.server.server_port))
                result = subprocess.run(["busybox", "sh", str(PROBE), *args], env=env, capture_output=True, timeout=20)
                output = result.stdout + result.stderr
                # The credential contains a quote, so also check its JSON-escaped form.
                leaks = (self.token, self.credential, json.dumps(self.credential)[1:-1])
                self.assertFalse(any(value.encode() in output for value in leaks),
                                 "Credential appeared in probe diagnostics")
                self.assertEqual(result.returncode == 0, passed)

    def test_authentication_policy_scope_and_key_presence(self):
        credential = self.credential
        policy = (403, {"errorCode": "ERR_PERMISSION_DENIED", "message": credential})
        unavailable = (401, {"errorCode": "ERR_SECRET_STORE_NOT_FOUND", "message": credential})
        found = (200, {"password": credential})

        def meta(app):
            return {"id": app, "runtimeVersion": "1.19.2"}

        runtime = ("secretstore", "redis-secret")
        outside = ("secretstore", "access-telemetry-marker-key")
        telemetry = ("access-telemetry-secrets", "redis-secret")
        cases = [
            (("memories", "allowed"), meta("memories"), [RUNTIME], *runtime, found, True),
            (("memories", "outside"), meta("memories"), [RUNTIME], *outside, policy, True),
            (("memories-mcp", "unscoped"), meta("memories-mcp"), [], *runtime, unavailable, True),
            (("memories-mcp", "unscoped"), meta("memories-mcp"), [], *runtime, policy, True),
            (("memories-mcp", "unscoped"), meta("memories-mcp"), [], *runtime, (401, {"message": credential}), False),
            (("memories", "outside"), meta("memories"), [RUNTIME], *outside, (404, {}), False),
            (("memories", "allowed"), meta("memories"), [RUNTIME], *runtime,
             (200, {"other": '"password":"' + credential + '"'}), False),
            (("memories", "allowed"), meta("memories"), [RUNTIME], *runtime, (200, {"password": ""}), False),
            (("memories", "outside"), meta("memories"), [RUNTIME], *outside, found, False),
            (("memories", "allowed"), meta("memories"), [RUNTIME], *runtime, (200, {"child": {"password": credential}}), False),
            (("memories", "allowed"), meta("memories"), [], *runtime, found, False),
            (("memories-mcp", "unscoped"), meta("memories-mcp"), [RUNTIME], *runtime, policy, False),
            (("eventstore", "allowed"), meta("eventstore"), [RUNTIME], *runtime, found, True),
            # The lifecycle app reads its own telemetry store, and the runtime store is outside its scopes.
            (("memories-access-telemetry", "allowed"), meta("memories-access-telemetry"), [TELEMETRY], *telemetry, found, True),
            (("memories-access-telemetry", "outside"), meta("memories-access-telemetry"), [TELEMETRY],
             "access-telemetry-secrets", "access-telemetry-clock-key", policy, True),
            (("memories-access-telemetry", "unscoped"), meta("memories-access-telemetry"), [TELEMETRY], *runtime, unavailable, True),
            (("memories-access-telemetry", "allowed"), meta("memories-access-telemetry"), [RUNTIME], *telemetry, found, False),
            (("memories-access-telemetry", "unscoped"), meta("memories-access-telemetry"), [RUNTIME, TELEMETRY], *runtime, policy, False),
            (("memories-access-telemetry", "outside"), meta("memories-access-telemetry"), [TELEMETRY],
             "access-telemetry-secrets", "access-telemetry-clock-key", found, False),
            # An expected runtime version gates every check, and only the public version shape is printed.
            (("memories", "allowed", "1.19.2"), meta("memories"), [RUNTIME], *runtime, found, True),
            (("memories", "allowed", "1.19.2"), {"id": "memories", "runtimeVersion": "1.18.1"}, [RUNTIME], *runtime, found, False),
            (("memories", "allowed", "1.19.2"), {"id": "memories", "runtimeVersion": credential}, [RUNTIME], *runtime, found, False),
            (("Memories", "allowed"), meta("Memories"), [RUNTIME], *runtime, found, False),
            (("memories", "allowed", "1.19.2;x"), meta("memories"), [RUNTIME], *runtime, found, False),
            # The probe's placeholder words are never an expected version.
            (("memories", "allowed", "absent"), {"id": "memories"}, [RUNTIME], *runtime, found, False),
        ]
        self.replay(cases)

    def test_builtin_kubernetes_store_denial_and_runtime_identity(self):
        credential = self.credential
        policy = (403, {"errorCode": "ERR_PERMISSION_DENIED", "message": credential})

        def meta(app, version="1.19.2"):
            return {"id": app, "runtimeVersion": version}

        builtin = ("kubernetes", SYNTHETIC)
        cases = [
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN, RUNTIME, TELEMETRY], *builtin, policy, True),
            (("memories-mcp", "builtin", "1.19.2"), meta("memories-mcp"), [BUILTIN], *builtin, policy, True),
            (("memories-access-telemetry", "builtin", "1.19.2"), meta("memories-access-telemetry"), [BUILTIN, TELEMETRY],
             *builtin, policy, True),
            # The built-in check needs an expected runtime version.
            (("memories", "builtin"), meta("memories"), [BUILTIN, RUNTIME], *builtin, policy, False),
            (("memories", "builtin", "unrecognized"), meta("memories", credential), [BUILTIN, RUNTIME], *builtin, policy, False),
            # Only a lowercase app ID passes. BusyBox awk -v would decode the escape to "memories".
            (("Memories", "builtin", "1.19.2"), meta("Memories"), [BUILTIN, RUNTIME], *builtin, policy, False),
            (("memorie\\163", "builtin", "1.19.2"), meta("memories"), [BUILTIN, RUNTIME], *builtin, policy, False),
            # Identity and runtime must both match the pod under test.
            (("memories", "builtin", "1.19.2"), meta("memories-mcp"), [BUILTIN, RUNTIME], *builtin, policy, False),
            (("memories", "builtin", "1.19.2"), meta("memories", "1.18.1"), [BUILTIN, RUNTIME], *builtin, policy, False),
            (("memories", "builtin", "1.19.2"), {"id": "memories"}, [BUILTIN, RUNTIME], *builtin, policy, False),
            (("memories", "builtin", "1.19.2"), meta("memories", credential), [BUILTIN, RUNTIME], *builtin, policy, False),
            # The built-in store must be registered exactly once with its own type.
            (("memories", "builtin", "1.19.2"), meta("memories"), [RUNTIME], *builtin, policy, False),
            (("memories", "builtin", "1.19.2"), meta("memories"),
             [{"name": "kubernetes", "type": "secretstores.local.file"}], *builtin, policy, False),
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN, BUILTIN], *builtin, policy, False),
            # Only an explicit policy denial passes. A missed lookup means the policy is absent.
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], *builtin,
             (500, {"errorCode": "ERR_SECRET_GET", "message": credential}), False),
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], *builtin, (200, {"password": credential}), False),
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], *builtin, (200, {}), False),
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], *builtin,
             (403, {"errorCode": "ERR_SECRET_STORE_NOT_FOUND", "message": credential}), False),
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], *builtin, (403, {"message": credential}), False),
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], *builtin,
             (401, {"errorCode": "ERR_SECRET_STORE_NOT_FOUND", "message": credential}), False),
            # A request for any other key or store answers 404 and fails the check.
            (("memories", "builtin", "1.19.2"), meta("memories"), [BUILTIN], "kubernetes", "openbao-runtime-bootstrap", policy, False),
        ]
        self.replay(cases)
