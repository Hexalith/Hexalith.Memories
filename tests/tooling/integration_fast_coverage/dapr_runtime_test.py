"""Exercise Dapr startup with fake CLI tools and fixture-owned TCP listeners."""

from contextlib import contextmanager
import os
from pathlib import Path
import shutil
import signal
import socket
import subprocess
import tempfile
import time
import unittest


SCRIPT = Path(__file__).resolve().parents[3] / "tools/init-dapr-runtime.sh"
DAPR_FIXTURE = """#!/bin/sh
printf '%s\\n' "$*" >> "$DAPR_CALLS"
case "$*" in
    'init --slim --runtime-version 1.19.1')
        if [ "$STALL_COMMAND" = 'dapr init' ]; then
            exec fixture-stall
        fi
        if [ ! -e "$DAPR_FIRST_ATTEMPT" ]; then
            : > "$DAPR_FIRST_ATTEMPT"
            exit 1
        fi ;;
    '--version')
        if [ "$STALL_COMMAND" = 'dapr --version' ]; then
            exec fixture-stall
        fi ;;
    *) exit 97 ;;
esac
"""
DOCKER_FIXTURE = """#!/bin/sh
printf '%s\\n' "$*" >> "$DOCKER_CALLS"
if [ -n "$STALL_COMMAND" ]; then
    case "docker $*" in
        "$STALL_COMMAND"*) exec fixture-stall ;;
    esac
fi
if [ "$1 $2" = 'rm -f' ]; then
    if [ -n "$DOCKER_RM_ERROR" ]; then
        printf '%s\\n' "$DOCKER_RM_ERROR" >&2
        exit 1
    fi
    shift 2
    for name do
        if [ -f "$DOCKER_CONTAINERS/$name" ]; then
            rm -- "$DOCKER_CONTAINERS/$name" || exit 97
            printf '%s\\n' "$name"
        fi
    done
fi
"""
STALL_FIXTURE = """#!/bin/sh
trap '' TERM
"$REAL_SLEEP" 60 &
child=$!
if [ "$EXIT_LAUNCHER_ON_TERM" = 'yes' ]; then
    trap 'exit 0' TERM
fi
printf '%s %s\\n' "$$" "$child" >> "$STALLED_JOBS"
wait "$child"
"""
TIMEOUT_FIXTURE = """#!/bin/sh
[ "$1 $2" = '--foreground --kill-after=5s' ] || exit 97
deadline=$3
shift 3
case "$1 $2" in
    'dapr init'|'docker run') [ "$deadline" = '120s' ] || exit 97 ;;
    'dapr --version'|'docker rm'|'docker volume') [ "$deadline" = '30s' ] || exit 97 ;;
    'bash -c')
        [ "$deadline" = '30s' ] || exit 97
        case "$5" in
            50005) port=$PLACEMENT_FIXTURE_PORT ;;
            50006) port=$SCHEDULER_FIXTURE_PORT ;;
            *) exit 97 ;;
        esac
        set -- "$1" "$2" "$3" "$4" "$port" ;;
    *) exit 97 ;;
esac
exec "$REAL_TIMEOUT" --foreground --kill-after=0.2s "$FIXTURE_TIMEOUT" "$@"
"""
SLEEP_FIXTURE = """#!/bin/sh
[ "$1" = 5 ] || exec "$REAL_SLEEP" "$@"
"""
CURL_FIXTURE = """#!/bin/sh
for url do :; done
printf '%s\\n' "$url" >> "$READINESS_CALLS"
if [ "$STALL_COMMAND" = 'readiness' ]; then
    exec fixture-stall
fi
case "$url" in
    "$PLACEMENT_READY_URL"|"$SCHEDULER_READY_URL") printf '2 200 application/grpc' ;;
    *) exec "$REAL_CURL" "$@" ;;
esac
"""


@unittest.skipUnless(all(shutil.which(tool) for tool in ("bash", "timeout", "curl", "ps", "sleep")),
                     "requires Bash, timeout, curl, ps, and sleep")
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
            ("docker", DOCKER_FIXTURE),
            ("curl", CURL_FIXTURE),
            ("timeout", TIMEOUT_FIXTURE),
            ("sleep", SLEEP_FIXTURE),
            ("fixture-stall", STALL_FIXTURE),
        ):
            executable = bin_dir / name
            executable.write_text(contents, encoding="utf-8")
            executable.chmod(0o755)
        self.calls = self.root / "dapr-calls"
        self.first_attempt = self.root / "dapr-first-attempt"
        self.docker_calls = self.root / "docker-calls"
        self.docker_containers = self.root / "docker-containers"
        self.docker_containers.mkdir()
        self.readiness_calls = self.root / "readiness-calls"
        self.stalled_jobs = self.root / "stalled-jobs"
        self.addCleanup(self.kill_stalled_jobs)
        self.env = dict(
            os.environ,
            PATH=os.pathsep.join((str(bin_dir), os.environ["PATH"])),
            TMPDIR=str(self.root),
            DAPR_RUNTIME_VERSION="1.19.1",
            DAPR_INIT_ATTEMPTS="2",
            DAPR_CALLS=str(self.calls),
            DAPR_FIRST_ATTEMPT=str(self.first_attempt),
            DOCKER_CALLS=str(self.docker_calls),
            DOCKER_CONTAINERS=str(self.docker_containers),
            DOCKER_RM_ERROR="",
            READINESS_CALLS=str(self.readiness_calls),
            STALLED_JOBS=str(self.stalled_jobs),
            STALL_COMMAND="",
            EXIT_LAUNCHER_ON_TERM="no",
            FIXTURE_TIMEOUT="1s",
            REAL_CURL=shutil.which("curl"),
            REAL_TIMEOUT=shutil.which("timeout"),
            REAL_SLEEP=shutil.which("sleep"),
        )

    @contextmanager
    def start_initializer(self, unavailable=None, **overrides):
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
            env.update(overrides)
            with subprocess.Popen(
                ["bash", SCRIPT.name], cwd=self.root, env=env, text=True,
                stdout=subprocess.PIPE, stderr=subprocess.PIPE,
            ) as process:
                try:
                    yield process
                finally:
                    if process.poll() is None:
                        process.terminate()
                        try:
                            process.wait(timeout=2)
                        except subprocess.TimeoutExpired:
                            process.kill()

    def run_initializer(self, unavailable=None, **overrides):
        with self.start_initializer(unavailable, **overrides) as process:
            stdout, stderr = process.communicate(timeout=10)
            self.assertEqual([], list(self.root.glob("dapr-docker-cleanup.*")))
            return subprocess.CompletedProcess(process.args, process.returncode, stdout, stderr)

    def stalled_pids(self):
        if not self.stalled_jobs.exists():
            return []
        return [int(pid) for pid in self.stalled_jobs.read_text(encoding="utf-8").split()]

    def running_stalled_pids(self):
        pids = self.stalled_pids()
        if not pids:
            return []
        result = subprocess.run(
            ["ps", "-o", "pid=,stat=", "-p", ",".join(map(str, pids))],
            capture_output=True, text=True, timeout=2,
        )
        self.assertIn(result.returncode, (0, 1), result.stderr)
        self.assertEqual("", result.stderr)
        states = [line.split() for line in result.stdout.splitlines()]
        return [int(pid) for pid, state in states if not state.startswith("Z")]

    def kill_stalled_jobs(self):
        for pid in self.running_stalled_pids():
            try:
                os.kill(pid, signal.SIGKILL)
            except ProcessLookupError:
                pass

    def assert_stalled_jobs_stopped(self, count):
        self.assertEqual(count * 2, len(self.stalled_pids()))
        deadline = time.monotonic() + 2
        while alive := self.running_stalled_pids():
            if time.monotonic() >= deadline:
                self.fail(f"Stalled jobs are still running: {alive}")
            time.sleep(0.01)

    def reset_calls(self):
        self.kill_stalled_jobs()
        for path in (self.calls, self.first_attempt, self.docker_calls, self.readiness_calls, self.stalled_jobs):
            path.unlink(missing_ok=True)

    def test_retries_slim_init_without_uninstalling_and_accepts_both_grpc_responses(self):
        result = self.run_initializer()

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("attempt 1/2", result.stderr)
        self.assertEqual(
            ["init --slim --runtime-version 1.19.1"] * 2 + ["--version"],
            self.calls.read_text(encoding="utf-8").splitlines(),
        )

    def test_removes_only_the_owned_dapr_containers(self):
        other_container = "dapr_placement_backup"
        for name in ("dapr_placement", "dapr_scheduler", other_container):
            (self.docker_containers / name).touch()

        result = self.run_initializer()

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual([other_container], [entry.name for entry in self.docker_containers.iterdir()])

    def test_continues_when_legacy_docker_reports_only_missing_dapr_containers(self):
        result = self.run_initializer(
            DOCKER_RM_ERROR="Error: No such container: dapr_placement\n"
            "Error response from daemon: No such container: dapr_scheduler",
        )

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("--version", self.calls.read_text(encoding="utf-8"))

    def test_refuses_startup_on_docker_removal_errors(self):
        for error in (
            "Cannot connect to the Docker daemon at unix:///var/run/docker.sock.",
            "Error response from daemon: authorization denied by plugin",
            "Error response from daemon: No such container: dapr_placement\n"
            "Error response from daemon: authorization denied by plugin",
            "Error response from daemon: No such container: dapr_placement_backup",
        ):
            with self.subTest(error=error):
                self.reset_calls()
                result = self.run_initializer(DOCKER_RM_ERROR=error)

                self.assertEqual(1, result.returncode, result.stderr)
                self.assertIn(error, result.stderr)
                self.assertEqual(
                    ["rm -f dapr_placement dapr_scheduler"],
                    self.docker_calls.read_text(encoding="utf-8").splitlines(),
                )
                self.assertFalse(self.readiness_calls.exists())

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

    def test_stalled_slim_install_retries_and_stops_its_children(self):
        result = self.run_initializer(STALL_COMMAND="dapr init")

        self.assertEqual(1, result.returncode, result.stderr)
        self.assertEqual(2, result.stderr.count("Timed out after 120s running dapr init --slim"))
        self.assertIn("dapr init --slim failed after 2 attempts", result.stderr)
        self.assertEqual(
            ["init --slim --runtime-version 1.19.1"] * 2,
            self.calls.read_text(encoding="utf-8").splitlines(),
        )
        self.assertFalse(self.docker_calls.exists())
        self.assertFalse(self.readiness_calls.exists())
        self.assert_stalled_jobs_stopped(count=2)

    def test_stalled_docker_operations_stop_setup_and_their_children(self):
        operations = (
            ("rm -f dapr_placement dapr_scheduler", "Timed out after 30s removing"),
            ("run -d --name dapr_placement", "Timed out after 120s running docker run"),
            ("volume create dapr_scheduler", "Timed out after 30s running docker volume create"),
            ("run -d --name dapr_scheduler", "Timed out after 120s running docker run"),
        )
        for count, (operation, message) in enumerate(operations, start=1):
            with self.subTest(operation=operation):
                self.reset_calls()
                result = self.run_initializer(
                    STALL_COMMAND=f"docker {operation}", EXIT_LAUNCHER_ON_TERM="yes",
                )

                self.assertEqual(124, result.returncode, result.stderr)
                self.assertIn(message, result.stderr)
                calls = self.docker_calls.read_text(encoding="utf-8").splitlines()
                self.assertEqual(count, len(calls), calls)
                self.assertEqual(operations[0][0], calls[0])
                self.assertTrue(calls[-1].startswith(operation), calls)
                self.assertFalse(self.readiness_calls.exists())
                self.assert_stalled_jobs_stopped(count=1)

    def test_readiness_deadline_stops_both_probes_and_their_children(self):
        result = self.run_initializer(STALL_COMMAND="readiness")

        self.assertEqual(1, result.returncode, result.stderr)
        self.assertIn("Timed out after 30s waiting for Dapr placement on 127.0.0.1:50005", result.stderr)
        self.assertIn("Timed out after 30s waiting for Dapr scheduler on 127.0.0.1:50006", result.stderr)
        self.assertNotIn("--version", self.calls.read_text(encoding="utf-8"))
        self.assertEqual(4, len(self.stalled_pids()))
        self.assertEqual([], self.running_stalled_pids())

    def test_stalled_version_reports_its_deadline_and_stops_its_child(self):
        result = self.run_initializer(
            STALL_COMMAND="dapr --version", EXIT_LAUNCHER_ON_TERM="yes",
        )

        self.assertEqual(124, result.returncode, result.stderr)
        self.assertIn("Timed out after 30s running dapr --version", result.stderr)
        self.assertEqual(2, len(self.readiness_calls.read_text(encoding="utf-8").splitlines()))
        self.assert_stalled_jobs_stopped(count=1)

    def test_interruption_stops_active_setup_and_readiness_jobs(self):
        for interrupted, operation, count in (
            (signal.SIGHUP, "dapr init", 1),
            (signal.SIGINT, "docker run -d --name dapr_placement", 1),
            (signal.SIGTERM, "docker rm -f dapr_placement dapr_scheduler", 1),
            (signal.SIGTERM, "readiness", 2),
        ):
            with self.subTest(signal=interrupted, operation=operation):
                self.reset_calls()
                with self.start_initializer(STALL_COMMAND=operation, FIXTURE_TIMEOUT="30s") as process:
                    deadline = time.monotonic() + 5
                    while len(self.stalled_pids()) < count * 2:
                        if process.poll() is not None or time.monotonic() >= deadline:
                            self.fail(f"{operation} stalled jobs did not start")
                        time.sleep(0.01)
                    process.send_signal(interrupted)
                    _, stderr = process.communicate(timeout=3)

                self.assertEqual(128 + interrupted, process.returncode, stderr)
                self.assertNotIn("--version", self.calls.read_text(encoding="utf-8"))
                self.assert_stalled_jobs_stopped(count)
                self.assertEqual([], list(self.root.glob("dapr-docker-cleanup.*")))
