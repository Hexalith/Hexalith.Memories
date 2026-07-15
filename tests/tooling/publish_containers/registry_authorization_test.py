import base64
import os
import subprocess
import threading
import unittest
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "verify-container-registry.ps1"
USERNAME = "SECRET_ZOT_USERNAME_SHOULD_NOT_LEAK"
API_KEY = "SECRET_ZOT_API_KEY_SHOULD_NOT_LEAK"


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

    return subprocess.run(
        [
            "pwsh",
            "-NoLogo",
            "-NoProfile",
            "-File",
            str(SCRIPT),
            "-Registry",
            origin,
        ],
        cwd=REPO_ROOT,
        env=env,
        capture_output=True,
        text=True,
        check=False,
    )


class RegistryAuthorizationTests(unittest.TestCase):
    def test_probe_verifies_and_cancels_both_repository_upload_sessions(self) -> None:
        state = RegistryState()
        with RegistryServer(state) as registry:
            result = run_probe(registry.origin)

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertEqual(["/v2/"], state.gets)
        self.assertEqual([None], state.get_authorizations)
        self.assertEqual(["memories", "memories-mcp"], state.posts)
        self.assertEqual(["/uploads/memories", "/uploads/memories-mcp"], state.deletes)
        self.assertEqual(2, len([value for value in state.delete_authorizations if value]))
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
        self.assertEqual(["memories", "memories-mcp"], state.posts)

    def test_cross_origin_upload_locations_are_cancelled_without_forwarding_credentials(self) -> None:
        registry_state = RegistryState()
        cancellation_state = RegistryState()
        with RegistryServer(cancellation_state) as cancellation_server:
            registry_state.location_origin = cancellation_server.origin
            with RegistryServer(registry_state) as registry:
                result = run_probe(registry.origin)

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertEqual(["memories", "memories-mcp"], registry_state.posts)
        self.assertEqual(
            ["/uploads/memories", "/uploads/memories-mcp"],
            cancellation_state.deletes,
        )
        self.assertEqual([None, None], cancellation_state.delete_authorizations)
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
        self.assertEqual(2, len(state.deletes))
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
