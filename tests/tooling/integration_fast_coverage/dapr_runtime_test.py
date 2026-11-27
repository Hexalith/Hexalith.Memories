"""Exercise Dapr startup with fake CLI tools and fixture-owned TCP listeners."""

import os
from pathlib import Path
import shutil
import socket
import subprocess
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[3] / "tools/init-dapr-runtime.sh"
DAPR_FIXTURE = """#!/bin/sh
printf '%s\\n' "$*" >> "$DAPR_CALLS"
case "$*" in
    'init --slim --runtime-version 1.19.1')
        if [ ! -e "$DAPR_FIRST_ATTEMPT" ]; then
            : > "$DAPR_FIRST_ATTEMPT"
            exit 1
        fi ;;
    '--version') ;;
    *) exit 97 ;;
esac
"""
TIMEOUT_FIXTURE = """#!/bin/sh
[ "$1" = '30s' ] || exit 97
case "$6" in
    50005) port=$PLACEMENT_FIXTURE_PORT ;;
    50006) port=$SCHEDULER_FIXTURE_PORT ;;
    *) exit 97 ;;
esac
exec "$REAL_TIMEOUT" 1s "$2" "$3" "$4" "$5" "$port"
"""
SLEEP_FIXTURE = """#!/bin/sh
[ "$1" = 5 ] || exec "$REAL_SLEEP" "$@"
"""
CURL_FIXTURE = """#!/bin/sh
for url do :; done
case "$url" in
    "$PLACEMENT_READY_URL"|"$SCHEDULER_READY_URL") printf '2 200 application/grpc' ;;
    *) exec "$REAL_CURL" "$@" ;;
esac
"""


@unittest.skipUnless(all(shutil.which(tool) for tool in ("bash", "timeout", "curl")),
                     "requires Bash, timeout, and curl")
class DaprRuntimeTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        shutil.copy2(SCRIPT, self.root / SCRIPT.name)
        bin_dir = self.root / "bin"
        bin_dir.mkdir()
        for name, contents in (
            ("dapr", DAPR_FIXTURE),
            ("docker", "#!/bin/sh\nexit 0\n"),
            ("curl", CURL_FIXTURE),
            ("timeout", TIMEOUT_FIXTURE),
            ("sleep", SLEEP_FIXTURE),
        ):
            executable = bin_dir / name
            executable.write_text(contents, encoding="utf-8")
            executable.chmod(0o755)
        self.calls = self.root / "dapr-calls"
        self.env = dict(
            os.environ,
            PATH=os.pathsep.join((str(bin_dir), os.environ["PATH"])),
            DAPR_RUNTIME_VERSION="1.19.1",
            DAPR_INIT_ATTEMPTS="2",
            DAPR_CALLS=str(self.calls),
            DAPR_FIRST_ATTEMPT=str(self.root / "dapr-first-attempt"),
            REAL_CURL=shutil.which("curl"),
            REAL_TIMEOUT=shutil.which("timeout"),
            REAL_SLEEP=shutil.which("sleep"),
        )

    def run_initializer(self, unavailable=None):
        with socket.socket(socket.AF_INET) as placement, socket.socket(socket.AF_INET) as scheduler:
            placement.bind(("127.0.0.1", 0))
            scheduler.bind(("127.0.0.1", 0))
            if unavailable != "placement":
                placement.listen()
            # The scheduler fixture accepts TCP handshakes even when gRPC is unavailable.
            scheduler.listen()
            placement_port = placement.getsockname()[1]
            scheduler_port = scheduler.getsockname()[1]
            health_path = "/grpc.health.v1.Health/Check"
            env = dict(
                self.env,
                PLACEMENT_FIXTURE_PORT=str(placement_port),
                SCHEDULER_FIXTURE_PORT=str(scheduler_port),
                PLACEMENT_READY_URL=f"http://127.0.0.1:{placement_port}{health_path}"
                if unavailable != "placement" else "",
                SCHEDULER_READY_URL=f"http://127.0.0.1:{scheduler_port}{health_path}"
                if unavailable != "scheduler" else "",
            )
            return subprocess.run(
                ["bash", SCRIPT.name], cwd=self.root, env=env,
                capture_output=True, text=True, timeout=10,
            )

    def test_retries_slim_init_without_uninstalling_and_accepts_both_grpc_responses(self):
        result = self.run_initializer()

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("attempt 1/2", result.stderr)
        self.assertEqual(
            ["init --slim --runtime-version 1.19.1"] * 2 + ["--version"],
            self.calls.read_text(encoding="utf-8").splitlines(),
        )

    def test_refuses_startup_when_the_scheduler_offers_only_tcp(self):
        result = self.run_initializer(unavailable="scheduler")

        self.assertEqual(1, result.returncode, result.stderr)
        self.assertIn("waiting for Dapr scheduler on 127.0.0.1:50006", result.stderr)
        self.assertNotIn("waiting for Dapr placement", result.stderr)
        self.assertNotIn("--version", self.calls.read_text(encoding="utf-8"))

    def test_refuses_startup_when_the_placement_listener_is_unavailable(self):
        result = self.run_initializer(unavailable="placement")

        self.assertEqual(1, result.returncode, result.stderr)
        self.assertIn("waiting for Dapr placement on 127.0.0.1:50005", result.stderr)
        self.assertNotIn("waiting for Dapr scheduler", result.stderr)
        self.assertNotIn("--version", self.calls.read_text(encoding="utf-8"))
