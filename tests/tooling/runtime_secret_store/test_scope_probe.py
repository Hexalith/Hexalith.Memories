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


class ScopeProbeTests(unittest.TestCase):
    def test_authentication_policy_scope_and_key_presence(self):
        token = secrets.token_hex(16)
        credential = 'synthetic-escaped-quote-"' + secrets.token_hex(16)
        registered = [{"name": "secretstore", "type": "secretstores.hashicorp.vault"}]
        policy = {"errorCode": "ERR_PERMISSION_DENIED", "message": credential}
        unavailable = {"errorCode": "ERR_SECRET_STORE_NOT_FOUND", "message": credential}
        canceled = {"errorCode": "ERR_SECRET_GET", "message": "context canceled " + credential}

        class Handler(BaseHTTPRequestHandler):
            def do_GET(self):
                if self.headers.get("dapr-api-token") != token:
                    status, body = 401, {"message": credential}
                elif self.path == "/v1.0/metadata":
                    status, body = 200, {"id": app, "components": components}
                elif self.path == "/v1.0/secrets/secretstore/" + key:
                    status, body = response
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

        cases = [
            ("memories", "allowed", registered, (200, {"password": credential}), True),
            ("memories", "outside", registered, (403, policy), True),
            ("memories-mcp", "unscoped", [], (401, unavailable), True),
            ("memories-mcp", "unscoped", [], (403, policy), True),
            ("memories-mcp", "unscoped", [], (401, {"message": credential}), False),
            ("memories", "outside", registered, (404, {}), False),
            ("memories", "allowed", registered, (200, {"other": '"password":"' + credential + '"'}), False),
            ("memories", "allowed", registered, (200, {"password": ""}), False),
            ("memories", "outside", registered, (200, {"password": credential}), False),
            ("memories", "allowed", registered, (200, {"child": {"password": credential}}), False),
            ("memories", "allowed", [], (200, {"password": credential}), False),
            ("memories-mcp", "unscoped", registered, (403, policy), False),
            ("eventstore", "allowed", registered, (200, {"password": credential}), True),
        ]
        with HTTPServer(("127.0.0.1", 0), Handler) as server:
            thread = threading.Thread(target=server.serve_forever, daemon=True)
            thread.start()
            try:
                for index, (app, check, components, response, passed) in enumerate(cases):
                    with self.subTest(case=index):
                        key = "access-telemetry-marker-key" if check == "outside" else "redis-secret"
                        env = dict(os.environ, DAPR_API_TOKEN=token, DAPR_HTTP_PORT=str(server.server_port))
                        probe = Path(__file__).resolve().parents[3] / "tools/probe-dapr-secret-scope.sh"
                        result = subprocess.run(["busybox", "sh", str(probe), app, check], env=env,
                                                capture_output=True, timeout=20)
                        output = result.stdout + result.stderr
                        # The credential contains a quote, so also check its JSON-escaped form.
                        leaks = (token, credential, json.dumps(credential)[1:-1])
                        self.assertFalse(any(value.encode() in output for value in leaks),
                                         "Credential appeared in probe diagnostics")
                        self.assertEqual(result.returncode == 0, passed)
            finally:
                server.shutdown()
                thread.join()
