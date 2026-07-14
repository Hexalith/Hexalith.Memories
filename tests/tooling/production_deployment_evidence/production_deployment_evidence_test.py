import json
import os
import subprocess
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "validate-production-deployment-evidence.ps1"


def write_complete_evidence(root: Path, *, status: str = "succeeded") -> None:
    root.mkdir(parents=True, exist_ok=True)
    result = {
        "schemaVersion": 1,
        "status": status,
        "stage": "required-server-mcp-unhealthy",
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
