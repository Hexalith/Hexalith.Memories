import json
import os
import subprocess
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "validate-production-deployment-evidence.ps1"
VERIFIER = REPO_ROOT / "tools" / "verify-production-deployment.ps1"


def write_complete_evidence(
    root: Path,
    *,
    status: str = "succeeded",
    stage: str = "required-server-mcp-restored",
) -> None:
    root.mkdir(parents=True, exist_ok=True)
    result = {
        "schemaVersion": 1,
        "status": status,
        "stage": stage,
        "capturedAt": "2026-07-14T00:00:00Z",
        "error": None if status == "succeeded" else "redacted rollout failure",
    }
    (root / "verification-result.json").write_text(json.dumps(result), encoding="utf-8")
    (root / "last-stage.txt").write_text(result["stage"], encoding="utf-8")
    for name in (
        "pods.txt",
        "events.txt",
        "describe-pods.txt",
        "describe-workloads.txt",
        "pods.json",
        "memories-current.log",
        "memories-previous.log",
    ):
        (root / name).write_text("redacted evidence", encoding="utf-8")
    (root / "health-initial-server-health.json").write_text(
        json.dumps(
            {
                "schemaVersion": 1,
                "stage": "initial-server-health",
                "statusCode": 200,
                "body": json.dumps({"schemaVersion": 1, "status": "Healthy"}),
            }
        ),
        encoding="utf-8",
    )
    (root / "health-required-redis-unhealthy.json").write_text(
        json.dumps(
            {
                "schemaVersion": 1,
                "stage": "required-redis-unhealthy",
                "statusCode": 503,
                "body": json.dumps({"schemaVersion": 1, "status": "Unhealthy"}),
            }
        ),
        encoding="utf-8",
    )


def run_validator(root: Path, env: dict[str, str] | None = None) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [
            "pwsh",
            "-NoLogo",
            "-NoProfile",
            "-File",
            str(SCRIPT),
            "-EvidenceDirectory",
            str(root),
        ],
        cwd=REPO_ROOT,
        env=env,
        text=True,
        capture_output=True,
        check=False,
    )


class ProductionDeploymentEvidenceTests(unittest.TestCase):
    def test_fault_rollouts_preserve_capacity_and_restore_deployment_state(self) -> None:
        verifier = VERIFIER.read_text(encoding="utf-8-sig")

        self.assertIn("Save-MemoriesDeploymentState", verifier)
        self.assertIn("Set-CapacityPreservingMemoriesRollout", verifier)
        self.assertIn("Restore-MemoriesDeploymentState", verifier)
        self.assertIn('"maxSurge":0', verifier)
        self.assertIn('"maxUnavailable":1', verifier)
        self.assertIn("path = '/spec/replicas'", verifier)
        self.assertIn("path = '/spec/strategy'", verifier)
        self.assertIn("required-server-restored", verifier)
        self.assertIn("required-server-mcp-restored", verifier)

    def test_health_probe_uses_authenticated_image_native_client_and_preserves_fault_body(self) -> None:
        verifier = VERIFIER.read_text(encoding="utf-8-sig")

        self.assertIn('wgetOutput="$(wget -S -O- -T 6 --header="dapr-api-token: ${APP_API_TOKEN}"', verifier)
        self.assertIn("wgetExit=$?", verifier)
        self.assertIn("grep -Eq 'HTTP/[0-9.]+ 503([[:space:]]|$)'", verifier)
        self.assertIn("dapr-api-token: %s", verifier)
        self.assertIn("Connection: close\\r\\ndapr-api-token: %s\\r\\n\\r\\n", verifier)
        self.assertIn("nc -w 6 127.0.0.1 8080", verifier)
        self.assertIn("$probeCommand = $probeCommand.Replace(\"`r\", '')", verifier)
        self.assertIn("$text = Protect-EvidenceText ($output -join [Environment]::NewLine)", verifier)
        self.assertIn("function Get-HealthJsonBody", verifier)
        self.assertIn("ConvertFrom-Json -ErrorAction Stop", verifier)
        self.assertIn("Save-HealthResponseEvidence", verifier)
        self.assertIn("expectedHttpStatus = if ($ExpectedStatus -eq 'Unhealthy') { 503 } else { 200 }", verifier)

    def test_health_probe_shell_contract_handles_authenticated_200_and_503(self) -> None:
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        probe = verifier.split("$probeCommand = @'\n", 1)[1].split("\n'@", 1)[0]
        probe = probe.replace("\n", "\r\n").replace("\r", "")

        for status, health_status in ((200, "Healthy"), (503, "Unhealthy")):
            with self.subTest(status=status), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                bin_dir = root / "bin"
                bin_dir.mkdir()
                wget_args = root / "wget-args.txt"
                nc_request = root / "nc-request.txt"
                (bin_dir / "wget").write_text(
                    """#!/bin/sh
printf '%s\n' "$@" > "$WGET_ARGS_LOG"
if [ "$PROBE_STATUS" = "200" ]; then
    printf '  HTTP/1.1 200 OK\n' >&2
    printf '{"schemaVersion":1,"status":"Healthy"}\n'
    exit 0
fi
printf '  HTTP/1.1 503 Service Unavailable\n' >&2
exit 8
""",
                    encoding="utf-8",
                )
                (bin_dir / "nc").write_text(
                    """#!/bin/sh
cat > "$NC_REQUEST_LOG"
printf 'HTTP/1.1 503 Service Unavailable\r\nContent-Type: application/json\r\n\r\n{"schemaVersion":1,"status":"Unhealthy"}\n'
""",
                    encoding="utf-8",
                )
                (bin_dir / "sleep").write_text("#!/bin/sh\nexit 0\n", encoding="utf-8")
                for executable in bin_dir.iterdir():
                    os.chmod(executable, 0o755)

                env = os.environ.copy()
                env.update(
                    PATH=str(bin_dir) + os.pathsep + env["PATH"],
                    APP_API_TOKEN="verification-app-api-token",
                    PROBE_STATUS=str(status),
                    WGET_ARGS_LOG=str(wget_args),
                    NC_REQUEST_LOG=str(nc_request),
                )
                result = subprocess.run(
                    ["/bin/sh", "-ec", probe],
                    cwd=REPO_ROOT,
                    env=env,
                    text=True,
                    capture_output=True,
                    check=False,
                )

                self.assertEqual(0, result.returncode, result.stdout + result.stderr)
                self.assertIn(f"HTTP/1.1 {status}", result.stdout)
                self.assertIn(f'"status":"{health_status}"', result.stdout)
                self.assertIn("dapr-api-token: verification-app-api-token", wget_args.read_text(encoding="utf-8"))
                if status == 503:
                    self.assertIn(
                        "dapr-api-token: verification-app-api-token",
                        nc_request.read_text(encoding="utf-8"),
                    )
                else:
                    self.assertFalse(nc_request.exists())

    def test_missing_health_response_evidence_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            for path in root.glob("health-*.json"):
                path.unlink()

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("health response", result.stdout + result.stderr)

    def test_complete_success_and_failure_evidence_pass(self) -> None:
        for status in ("succeeded", "failed"):
            with self.subTest(status=status), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                write_complete_evidence(root, status=status)

                result = run_validator(root)

                self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_missing_required_cluster_evidence_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            (root / "events.txt").unlink()

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("events.txt", result.stdout + result.stderr)

    def test_success_evidence_before_final_restoration_stage_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root, stage="required-server-restored")

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            combined = result.stdout + result.stderr
            self.assertIn("Succeeded production deployment evidence", combined)
            self.assertIn("required-server-mcp-restored", combined)

    def test_known_verification_secret_canary_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            (root / "memories-current.log").write_text(
                "leaked verification-invalid-dapr-api-token",
                encoding="utf-8",
            )

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("unredacted secret canary", result.stdout + result.stderr)

    def test_environment_secret_canary_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            canary = "zot-secret-canary-for-evidence-test"
            (root / "events.txt").write_text(canary, encoding="utf-8")
            env = os.environ.copy()
            env["HEXALITH_ZOT_API_KEY"] = canary

            result = run_validator(root, env)

            self.assertNotEqual(0, result.returncode)
            combined = result.stdout + result.stderr
            self.assertIn("unredacted", combined)
            self.assertIn("secret canary", combined)


if __name__ == "__main__":
    unittest.main()
