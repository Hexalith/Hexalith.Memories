import base64
import importlib.util
import json
import os
import subprocess
import tempfile
import threading
import unittest
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "verify-container-registry.ps1"
_CONTRACT_SPEC = importlib.util.spec_from_file_location("registry_authorization_contract", REPO_ROOT / "tools" / "container-publication-contract.py")
CONTRACT = importlib.util.module_from_spec(_CONTRACT_SPEC)
_CONTRACT_SPEC.loader.exec_module(CONTRACT)
USERNAME = "SECRET_ZOT_USERNAME_SHOULD_NOT_LEAK"
API_KEY = "SECRET_ZOT_API_KEY_SHOULD_NOT_LEAK"
REPOSITORIES = [
    "memories",
    "memories-mcp",
    "memories-access-telemetry",
    "memories-access-telemetry-clock",
    "memories-domain-service",
]


class RegistryState:
    def __init__(self) -> None:
        self.gets: list[str] = []
        self.get_authorizations: list[str | None] = []
        self.posts: list[str] = []
        self.deletes: list[str] = []
        self.delete_authorizations: list[str | None] = []
        self.challenge_header: str | None = 'Basic realm="test-zot"'
        self.challenge_status = 200
        self.denied_repository: str | None = None
        self.cancel_status = 204
        self.location_origin: str | None = None


def make_handler(state: RegistryState) -> type[BaseHTTPRequestHandler]:
    class RegistryHandler(BaseHTTPRequestHandler):
        def do_GET(self) -> None:  # noqa: N802 - BaseHTTPRequestHandler contract
            state.gets.append(self.path)
            state.get_authorizations.append(self.headers.get("Authorization"))
            if self.path != "/v2/":
                self.send_response(404)
                self.end_headers()
                return

            self.send_response(state.challenge_status)
            self.send_header("Docker-Distribution-Api-Version", "registry/2.0")
            if state.challenge_header is not None:
                self.send_header("WWW-Authenticate", state.challenge_header)
            self.end_headers()

        def do_POST(self) -> None:  # noqa: N802 - BaseHTTPRequestHandler contract
            expected = "Basic " + base64.b64encode(f"{USERNAME}:{API_KEY}".encode()).decode()
            if self.headers.get("Authorization") != expected:
                self.send_response(401)
                self.end_headers()
                return

            repository = self.path.removeprefix("/v2/").removesuffix("/blobs/uploads/")
            state.posts.append(repository)
            if repository == state.denied_repository:
                self.send_response(403)
                self.end_headers()
                return

            self.send_response(202)
            location = f"/uploads/{repository}"
            if state.location_origin is not None:
                location = state.location_origin + location
            self.send_header("Location", location)
            self.end_headers()

        def do_DELETE(self) -> None:  # noqa: N802 - BaseHTTPRequestHandler contract
            state.deletes.append(self.path)
            state.delete_authorizations.append(self.headers.get("Authorization"))
            self.send_response(state.cancel_status)
            self.end_headers()

        def log_message(self, format: str, *args: object) -> None:
            return

    return RegistryHandler


class RegistryServer:
    def __init__(self, state: RegistryState) -> None:
        self.server = ThreadingHTTPServer(("127.0.0.1", 0), make_handler(state))
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)

    def __enter__(self) -> "RegistryServer":
        self.thread.start()
        return self

    def __exit__(self, *_: object) -> None:
        self.server.shutdown()
        self.server.server_close()
        self.thread.join()

    @property
    def origin(self) -> str:
        return f"http://127.0.0.1:{self.server.server_port}"


def run_probe(
    origin: str,
    *,
    include_credentials: bool = True,
    classification_only: bool = False,
    evidence_path: Path | None = None,
) -> subprocess.CompletedProcess[str]:
    env = os.environ.copy()
    if include_credentials:
        env["HEXALITH_ZOT_USERNAME"] = USERNAME
        env["HEXALITH_ZOT_API_KEY"] = API_KEY
    else:
        env.pop("HEXALITH_ZOT_USERNAME", None)
        env.pop("HEXALITH_ZOT_API_KEY", None)
    if classification_only:
        env["HEXALITH_RELEASE_CLASSIFICATION_ONLY"] = "true"
    else:
        env.pop("HEXALITH_RELEASE_CLASSIFICATION_ONLY", None)

    # Never let a fixture receipt land in the repository's real release evidence directory.
    with tempfile.TemporaryDirectory() as temporary:
        receipt = evidence_path if evidence_path is not None else Path(temporary) / "authorization.json"
        return subprocess.run(
            [
                "pwsh",
                "-NoLogo",
                "-NoProfile",
                "-File",
                str(SCRIPT),
                "-Registry",
                origin,
                "-EvidencePath",
                str(receipt),
            ],
            cwd=REPO_ROOT,
            env=env,
            capture_output=True,
            text=True,
            check=False,
        )


class RegistryAuthorizationTests(unittest.TestCase):
    def test_authorization_receipt_records_actual_mode_redaction_and_no_push_credit(self) -> None:
        for denied, cancel_status, expected, cleanup in ((None, 204, "pass", "pass"), ("memories-mcp", 204, "fail", "pass"), (None, 500, "fail", "failure")):
            state = RegistryState()
            state.denied_repository = denied
            state.cancel_status = cancel_status
            with self.subTest(denied=denied, cleanup=cancel_status), tempfile.TemporaryDirectory() as temporary, RegistryServer(state) as registry:
                receipt = Path(temporary) / "authorization.json"
                result = run_probe(registry.origin, evidence_path=receipt)
                text = receipt.read_text(encoding="utf-8-sig")
                evidence = json.loads(text)
                self.assertEqual(expected, evidence["result"])
                self.assertEqual(cleanup, evidence["cleanup"])
                registry_host = registry.origin.removeprefix("http://")
                self.assertEqual(registry_host, evidence["registry"])
                if expected == "pass":
                    # The real PowerShell receipt must satisfy the gate that consumes it.
                    CONTRACT.validate_authorization(receipt.parent, {"registry": registry_host})
                self.assertEqual("preemptive-basic", evidence["mode"])
                self.assertTrue(evidence["challengeObserved"])
                self.assertFalse(evidence["pushProven"])
                self.assertEqual(REPOSITORIES, evidence["repositories"])
                self.assertEqual("Jérôme Piquot", evidence["owner"])
                self.assertTrue(evidence["consequence"])
                self.assertTrue(evidence["reopenTrigger"])
                self.assertNotIn(USERNAME, text + result.stdout + result.stderr)
                self.assertNotIn(API_KEY, text + result.stdout + result.stderr)

    def test_probe_verifies_and_cancels_all_repository_upload_sessions(self) -> None:
        state = RegistryState()
        with RegistryServer(state) as registry:
            result = run_probe(registry.origin)

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertEqual(["/v2/"], state.gets)
        self.assertEqual([None], state.get_authorizations)
        self.assertEqual(REPOSITORIES, state.posts)
        self.assertEqual([f"/uploads/{repository}" for repository in REPOSITORIES], state.deletes)
        self.assertEqual(len(REPOSITORIES), len([value for value in state.delete_authorizations if value]))
        self.assertNotIn(USERNAME, result.stdout + result.stderr)
        self.assertNotIn(API_KEY, result.stdout + result.stderr)

    def test_missing_basic_challenge_fails_before_opening_upload_sessions(self) -> None:
        state = RegistryState()
        state.challenge_header = None
        with RegistryServer(state) as registry:
            result = run_probe(registry.origin)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["/v2/"], state.gets)
        self.assertEqual([None], state.get_authorizations)
        self.assertEqual([], state.posts)
        self.assertIn("WWW-Authenticate", result.stdout + result.stderr)
        self.assertNotIn(USERNAME, result.stdout + result.stderr)
        self.assertNotIn(API_KEY, result.stdout + result.stderr)

    def test_standard_unauthorized_basic_challenge_is_accepted(self) -> None:
        state = RegistryState()
        state.challenge_status = 401
        with RegistryServer(state) as registry:
            result = run_probe(registry.origin)

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertEqual(["/v2/"], state.gets)
        self.assertEqual(REPOSITORIES, state.posts)

    def test_cross_origin_upload_locations_fail_without_forwarding_credentials(self) -> None:
        registry_state = RegistryState()
        cancellation_state = RegistryState()
        with RegistryServer(cancellation_state) as cancellation_server:
            registry_state.location_origin = cancellation_server.origin
            with RegistryServer(registry_state) as registry:
                result = run_probe(registry.origin)

        self.assertNotEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertEqual(REPOSITORIES[:1], registry_state.posts)
        self.assertEqual(
            [],
            cancellation_state.deletes,
        )
        self.assertEqual([], cancellation_state.delete_authorizations)
        self.assertNotIn(USERNAME, result.stdout + result.stderr)
        self.assertNotIn(API_KEY, result.stdout + result.stderr)

    def test_denied_second_repository_cancels_first_and_fails_before_side_effects(self) -> None:
        state = RegistryState()
        state.denied_repository = "memories-mcp"
        with RegistryServer(state) as registry:
            result = run_probe(registry.origin)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["memories", "memories-mcp"], state.posts)
        self.assertEqual(["/uploads/memories"], state.deletes)
        self.assertIn("memories-mcp", result.stdout + result.stderr)
        self.assertIn("HTTP 403", result.stdout + result.stderr)
        self.assertNotIn(USERNAME, result.stdout + result.stderr)
        self.assertNotIn(API_KEY, result.stdout + result.stderr)

    def test_cancellation_failure_blocks_release(self) -> None:
        state = RegistryState()
        state.cancel_status = 500
        with RegistryServer(state) as registry:
            result = run_probe(registry.origin)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(len(REPOSITORIES), len(state.deletes))
        self.assertIn("could not cancel every", result.stdout + result.stderr)

    def test_missing_credentials_fail_without_contacting_registry(self) -> None:
        state = RegistryState()
        with RegistryServer(state) as registry:
            result = run_probe(registry.origin, include_credentials=False)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual([], state.gets)
        self.assertEqual([], state.posts)
        self.assertIn("HEXALITH_ZOT_USERNAME", result.stdout + result.stderr)

    def test_release_classification_skips_probe_without_credentials_or_requests(self) -> None:
        state = RegistryState()
        with RegistryServer(state) as registry:
            result = run_probe(
                registry.origin,
                include_credentials=False,
                classification_only=True,
            )

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertEqual([], state.gets)
        self.assertEqual([], state.posts)
        self.assertIn("deferred from release classification", result.stdout)


if __name__ == "__main__":
    unittest.main()
