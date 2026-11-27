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


@unittest.skipUnless(shutil.which("bash") and shutil.which("timeout"), "requires Bash and timeout")
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
            REAL_TIMEOUT=shutil.which("timeout"),
            REAL_SLEEP=shutil.which("sleep"),
        )

    def run_initializer(self, scheduler_ready):
        with socket.socket(socket.AF_INET) as placement, socket.socket(socket.AF_INET) as scheduler:
            placement.bind(("127.0.0.1", 0))
            scheduler.bind(("127.0.0.1", 0))
            placement.listen()
            if scheduler_ready:
                scheduler.listen()
            env = dict(
                self.env,
                PLACEMENT_FIXTURE_PORT=str(placement.getsockname()[1]),
                SCHEDULER_FIXTURE_PORT=str(scheduler.getsockname()[1]),
            )
            return subprocess.run(
                ["bash", SCRIPT.name], cwd=self.root, env=env,
                capture_output=True, text=True, timeout=10,
            )

    def test_retries_slim_init_without_uninstalling_and_accepts_both_listeners(self):
        result = self.run_initializer(scheduler_ready=True)

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("attempt 1/2", result.stderr)
        self.assertEqual(
            ["init --slim --runtime-version 1.19.1"] * 2 + ["--version"],
            self.calls.read_text(encoding="utf-8").splitlines(),
        )

    def test_refuses_startup_when_the_scheduler_listener_is_unavailable(self):
        result = self.run_initializer(scheduler_ready=False)

        self.assertEqual(1, result.returncode, result.stderr)
        self.assertIn("waiting for Dapr scheduler on 127.0.0.1:50006", result.stderr)
        self.assertNotIn("waiting for Dapr placement", result.stderr)
        self.assertNotIn("--version", self.calls.read_text(encoding="utf-8"))
