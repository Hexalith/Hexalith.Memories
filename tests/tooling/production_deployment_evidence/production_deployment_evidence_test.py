import base64
import json
import os
import subprocess
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "validate-production-deployment-evidence.ps1"
VERIFIER = REPO_ROOT / "tools" / "verify-production-deployment.ps1"
HEALTH_MODULE = REPO_ROOT / "tools" / "production-deployment-health.ps1"
RENDERER = REPO_ROOT / "tools" / "render-production-deployment.ps1"


def run_pwsh(script: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["pwsh", "-NoLogo", "-NoProfile", "-Command", script],
        cwd=REPO_ROOT,
        text=True,
        capture_output=True,
        check=False,
    )


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
                "attempt": 1,
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
                "attempt": 1,
                "statusCode": 503,
                "body": json.dumps({"schemaVersion": 1, "status": "Unhealthy"}),
            }
        ),
        encoding="utf-8",
    )
    (root / "secret-store-substitution.json").write_text(
        json.dumps(
            {
                "schemaVersion": 1,
                "reason": "redacted verification-scoped substitution disclosure",
                "substitutedComponents": ["secretstore", "access-telemetry-secrets"],
                "originalType": "secretstores.hashicorp.vault",
                "substitutedType": "secretstores.kubernetes",
                "observedPostPatchTypes": ["secretstores.kubernetes"],
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
        self.assertIn("dapr-api-token: %s", verifier)
        self.assertIn("Connection: close\\r\\ndapr-api-token: %s\\r\\n\\r\\n", verifier)
        self.assertIn("nc -w 4 127.0.0.1 8080", verifier)
        self.assertIn("$probeCommand = $probeCommand.Replace(\"`r\", '')", verifier)
        self.assertIn("Save-HealthResponseEvidence", verifier)
        self.assertIn("expectedHttpStatus = if ($ExpectedStatus -eq 'Unhealthy') { 503 } else { 200 }", verifier)

        # Redaction must not sit on the health-decision path: Protect-EvidenceText replaces
        # values this script does not control ($env:HEXALITH_ZOT_USERNAME/_API_KEY), so a
        # short or common value would corrupt the status text the gate parses. It belongs at
        # the evidence write, where Save-HealthResponseEvidence already applies it.
        #
        # These two assertions belong to THIS test. A later change inserted a new method
        # mid-body and silently carried them into it, so this pin stopped asserting the
        # contract its own name claims while aggregate coverage stayed unchanged.
        self.assertNotIn("$text = Protect-EvidenceText ($output", verifier)
        self.assertIn("$text = $output -join [Environment]::NewLine", verifier)

    def test_health_probe_records_fallback_markers_and_captures_body_via_port_forward(self) -> None:
        # CI run 30402973401: the in-container netcat fallback exited 0 with zero bytes on all
        # 34 redis-fault polls while the same binaries capture delayed 503 bodies in every
        # local-docker and live-cluster reproduction. Two guarantees follow. The nc branch must
        # be instrumented — begin/end markers with the wget and nc exit codes — so a silent
        # transcript can be told apart from a skipped branch. And when no status-bearing JSON
        # was captured in-container, the runner must capture the body deterministically through
        # a pod port-forward with curl, whose HTTP-error bodies are never discarded.
        verifier = VERIFIER.read_text(encoding="utf-8-sig")

        self.assertIn("printf 'nc-fallback begin (wget exit %s)\\n' \"$wgetExit\"", verifier)
        self.assertIn("printf '\\nnc-fallback end (nc exit %s)\\n' \"$?\"", verifier)
        self.assertIn("function Get-HealthResponseViaPortForward", verifier)
        self.assertIn("'port-forward', '-n', $namespace, \"pod/$Pod\", \"${localPort}:8080\"", verifier)
        # No --retry: curl treats HTTP 503 as transient, so retrying would discard the exact
        # response the fault stages exist to capture. The establishment race is handled by an
        # explicit wait-for-listener instead, which --retry-connrefused used to mask.
        self.assertNotIn("--retry-connrefused", verifier)
        self.assertNotIn("--retry 3", verifier)
        self.assertIn("$probe.ConnectAsync([System.Net.IPAddress]::Loopback, $localPort)", verifier)
        # An ephemeral loopback port, not a fixed 18080 a leaked forward could still hold.
        self.assertIn("$localPort = Get-FreeLoopbackPort", verifier)
        self.assertNotIn("$localPort = 18080", verifier)
        # The runner-side probe presents the pod's own token, never a hardcoded literal.
        self.assertIn("$token = Get-PodApplicationToken $Pod $Container", verifier)
        self.assertNotIn("-H 'dapr-api-token: verification-app-api-token'", verifier)
        # The fallback fires only when no status-bearing JSON object was captured, and its
        # transcript is persisted so both capture paths stay auditable.
        self.assertIn("$fallbackText = Get-HealthResponseViaPortForward $Pod $Container", verifier)
        self.assertIn("'port-forward fallback:'", verifier)
        self.assertIn("-Transcript $response.Raw", verifier)
        self.assertIn("transcript = Protect-EvidenceText $Transcript", verifier)
        # Ordering: the port-forward fallback decision happens inside Get-HealthResponse,
        # after the in-container probe ran, never instead of it.
        exec_index = verifier.index("kubectl exec -n $namespace $Pod -c $Container")
        fallback_index = verifier.index("$fallbackText = Get-HealthResponseViaPortForward $Pod $Container")
        self.assertLess(exec_index, fallback_index)

    def test_fallback_trigger_and_isolated_parse_decide_the_recorded_response(self) -> None:
        """Execute the fallback decision instead of pinning its source text.

        Three behaviours are proven against the real Get-HealthJsonBody /
        Get-HealthStatusCode: the trigger fires only when the in-container capture has no
        status-bearing JSON; the fallback transcript is parsed in ISOLATION so a malformed
        in-container half cannot swallow it; and only a 200/503 from the fallback may replace
        the in-container status code.
        """

        decision = """
$ErrorActionPreference = 'Stop'
. ./tools/production-deployment-health.ps1

function Resolve-Response {
    param([string]$Text, [string]$FallbackText)

    $statusCode = Get-HealthStatusCode $Text
    $body = Get-HealthJsonBody $Text
    $aggregate = $null
    try { $aggregate = $body | ConvertFrom-Json } catch { }
    $fired = $false
    if ($null -eq $aggregate -or $null -eq $aggregate.status) {
        $fired = $true
        $combined = $Text + [Environment]::NewLine + 'port-forward fallback:' + [Environment]::NewLine + $FallbackText
        $fallbackBody = Get-HealthJsonBody $FallbackText
        if ($fallbackBody -ne $FallbackText) { $body = $fallbackBody } else { $body = Get-HealthJsonBody $combined }
        $fallbackStatusCode = Get-HealthStatusCode $FallbackText
        if ($fallbackStatusCode -in @(200, 503)) { $statusCode = $fallbackStatusCode }
    }
    return [pscustomobject]@{ Fired = $fired; StatusCode = $statusCode; Body = $body }
}

$healthy = "HTTP/1.1 200 OK`r`n`r`n{""schemaVersion"":1,""status"":""Healthy""}"
$fallback503 = "port-forward begin`nHTTP/1.1 503`r`n`r`n{""schemaVersion"":1,""status"":""Unhealthy""}`nport-forward end (curl exit 0)"
$oddQuote = 'error: unable to upgrade connection: container "memories not found'
$fallback401 = "HTTP/1.1 401 Unauthorized`r`n`r`n"

$results = [ordered]@{
    goodStaysInContainer = (Resolve-Response $healthy $fallback503)
    poisonedIsolatedParse = (Resolve-Response $oddQuote $fallback503)
    authErrorDoesNotOverwrite = (Resolve-Response $oddQuote $fallback401)
}
$results | ConvertTo-Json -Depth 6 -Compress
"""
        result = run_pwsh(decision)
        self.assertEqual(0, result.returncode, result.stderr)
        parsed = json.loads(result.stdout.strip().splitlines()[-1])

        # A status-bearing in-container body must NOT trigger the runner-side capture.
        # Inverting the trigger condition makes this assertion fail.
        self.assertFalse(parsed["goodStaysInContainer"]["Fired"])
        self.assertEqual(200, parsed["goodStaysInContainer"]["StatusCode"])

        # An odd unescaped quote in the in-container half poisons Get-HealthJsonBody's
        # in-string state across a concatenation, so the clean fallback object survives only
        # because it is parsed in isolation.
        poisoned = parsed["poisonedIsolatedParse"]
        self.assertTrue(poisoned["Fired"])
        self.assertEqual(503, poisoned["StatusCode"])
        self.assertEqual({"schemaVersion": 1, "status": "Unhealthy"}, json.loads(poisoned["Body"]))

        # A probe-side 401 must not overwrite the in-container observation.
        self.assertTrue(parsed["authErrorDoesNotOverwrite"]["Fired"])
        self.assertIsNone(parsed["authErrorDoesNotOverwrite"]["StatusCode"])

    def test_free_loopback_port_helper_returns_a_bindable_ephemeral_port(self) -> None:
        """The fixed 18080 let a leaked forward answer for a different pod."""

        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        helper = verifier.split("function Get-FreeLoopbackPort {", 1)[1]
        helper = "function Get-FreeLoopbackPort {" + helper.split("\n}\n", 1)[0] + "\n}"

        result = run_pwsh(
            helper
            + "\n$ports = 1..3 | ForEach-Object { Get-FreeLoopbackPort }\n"
            + "$ports -join ','"
        )
        self.assertEqual(0, result.returncode, result.stderr)
        ports = [int(value) for value in result.stdout.strip().splitlines()[-1].split(",")]
        self.assertEqual(3, len(ports))
        for port in ports:
            self.assertGreater(port, 1024)
            self.assertLess(port, 65536)
            self.assertNotEqual(18080, port)

    def test_redaction_at_write_does_not_corrupt_the_health_decision(self) -> None:
        """Exercise the actual scenario the source-text pins above only describe.

        A short/common HEXALITH_ZOT_USERNAME value that collides with real response
        content must not corrupt the parsed status: parsing runs on the raw transcript,
        and Protect-EvidenceText only runs separately at the evidence write.
        """

        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        protect_fn = verifier.split("function Protect-EvidenceText {", 1)[1]
        protect_fn = "function Protect-EvidenceText {" + protect_fn.split("\n}\n", 1)[0] + "\n}"

        # "Healthy" is exactly the status value the health decision must observe; if
        # redaction ran before parsing (the pre-fix shape), this value would corrupt it.
        colliding_value = "Healthy"
        raw = '{"schemaVersion":1,"status":"Healthy"}'
        encoded = base64.b64encode(raw.encode("utf-8")).decode("ascii")

        script = (
            f"{protect_fn}\n"
            ". ./tools/production-deployment-health.ps1\n"
            "$t = [System.Text.Encoding]::UTF8.GetString("
            f"[System.Convert]::FromBase64String('{encoded}'))\n"
            "$body = Get-HealthJsonBody $t\n"
            "$status = ($body | ConvertFrom-Json).status\n"
            "$protected = Protect-EvidenceText $t\n"
            "Write-Output \"status=$status\"\n"
            "Write-Output \"protected=$protected\"\n"
        )
        env = os.environ.copy()
        env["HEXALITH_ZOT_USERNAME"] = colliding_value
        result = subprocess.run(
            ["pwsh", "-NoLogo", "-NoProfile", "-Command", script],
            cwd=REPO_ROOT,
            env=env,
            text=True,
            capture_output=True,
            check=False,
        )

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        # Parsing the raw transcript is unaffected by the colliding secret value.
        self.assertIn("status=Healthy", result.stdout)
        # Redaction still applies when run separately, at the evidence write.
        self.assertIn("protected=" + '{"schemaVersion":1,"status":"***"}', result.stdout)

        health = HEALTH_MODULE.read_text(encoding="utf-8-sig")
        self.assertIn("function Get-HealthJsonBody", health)
        self.assertIn("function Get-HealthStatusCode", health)
        self.assertIn("ConvertFrom-Json -ErrorAction Stop", health)

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

    def test_health_probe_falls_back_to_netcat_on_any_wget_failure(self) -> None:
        """A slow or timed-out wget must still reach the fallback.

        The fallback used to run only when the wget output already carried an
        `HTTP/... 503` line, so the slow-but-healthy case it was written for died on the
        stage deadline instead.
        """

        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        probe = verifier.split("$probeCommand = @'\n", 1)[1].split("\n'@", 1)[0]
        probe = probe.replace("\n", "\r\n").replace("\r", "")

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            bin_dir = root / "bin"
            bin_dir.mkdir()
            nc_request = root / "nc-request.txt"
            # wget times out: non-zero exit, and no status line at all to key on.
            (bin_dir / "wget").write_text(
                "#!/bin/sh\nprintf 'wget: download timed out\\n' >&2\nexit 1\n",
                encoding="utf-8",
            )
            (bin_dir / "nc").write_text(
                """#!/bin/sh
cat > "$NC_REQUEST_LOG"
printf 'HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n{"schemaVersion":1,"status":"Healthy"}\n'
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
            self.assertTrue(nc_request.exists(), "the netcat fallback did not run")
            self.assertIn('"status":"Healthy"', result.stdout)

    def test_health_json_body_extracts_the_last_status_bearing_object(self) -> None:
        """Drive Get-HealthJsonBody with the noisy transcripts it exists for.

        It was pinned only by a source-text assertion, so dropping the escape/in-string
        handling, or keeping the first rather than the last status-bearing object, passed
        every suite and the live kind job.
        """

        cases = (
            # A diagnostic brace, an escaped quote inside a string, then the real body.
            (
                'wget: note {not json}\n{"note":"a \\"quoted\\" {brace}"}\n'
                '{"schemaVersion":1,"status":"Unhealthy"}',
                '{"schemaVersion":1,"status":"Unhealthy"}',
            ),
            # Two status-bearing objects: the LAST one is the response being decided on.
            (
                '{"schemaVersion":1,"status":"Healthy"}\n{"schemaVersion":1,"status":"Unhealthy"}',
                '{"schemaVersion":1,"status":"Unhealthy"}',
            ),
            # A brace inside a string must not open or close an object.
            (
                '{"detail":"unbalanced { in a string","status":"Healthy"}',
                '{"detail":"unbalanced { in a string","status":"Healthy"}',
            ),
        )

        for text, expected in cases:
            with self.subTest(text=text):
                encoded = base64.b64encode(text.encode("utf-8")).decode("ascii")
                result = run_pwsh(
                    ". ./tools/production-deployment-health.ps1; "
                    "$t = [System.Text.Encoding]::UTF8.GetString("
                    f"[System.Convert]::FromBase64String('{encoded}')); "
                    "Write-Output (Get-HealthJsonBody $t)"
                )
                self.assertEqual(0, result.returncode, result.stderr)
                self.assertEqual(expected, result.stdout.strip())

    def test_health_status_code_uses_the_last_status_line(self) -> None:
        text = "  HTTP/1.1 200 OK\nwget: retrying\n  HTTP/1.1 503 Service Unavailable"
        encoded = base64.b64encode(text.encode("utf-8")).decode("ascii")

        result = run_pwsh(
            ". ./tools/production-deployment-health.ps1; "
            "$t = [System.Text.Encoding]::UTF8.GetString("
            f"[System.Convert]::FromBase64String('{encoded}')); "
            "Write-Output (Get-HealthStatusCode $t)"
        )

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("503", result.stdout.strip())

        empty = run_pwsh(
            ". ./tools/production-deployment-health.ps1; "
            "$v = Get-HealthStatusCode 'no status line here'; "
            "Write-Output ($null -eq $v)"
        )
        self.assertEqual("True", empty.stdout.strip())

    def test_malformed_health_body_fails_a_succeeded_run(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            (root / "health-initial-server-health.json").write_text(
                json.dumps(
                    {
                        "schemaVersion": 1,
                        "stage": "initial-server-health",
                        "attempt": 1,
                        "statusCode": 200,
                        "body": "not json at all",
                    }
                ),
                encoding="utf-8",
            )

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("unparsable body", result.stdout + result.stderr)

    def test_health_status_code_outside_200_or_503_fails_a_succeeded_run(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            (root / "health-initial-server-health.json").write_text(
                json.dumps(
                    {
                        "schemaVersion": 1,
                        "stage": "initial-server-health",
                        "attempt": 1,
                        "statusCode": 500,
                        "body": json.dumps({"schemaVersion": 1, "status": "Healthy"}),
                    }
                ),
                encoding="utf-8",
            )

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("valid HTTP status", result.stdout + result.stderr)

    def test_wrong_health_schema_version_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            (root / "health-initial-server-health.json").write_text(
                json.dumps(
                    {
                        "schemaVersion": 2,
                        "stage": "initial-server-health",
                        "attempt": 1,
                        "statusCode": 200,
                        "body": json.dumps({"schemaVersion": 1, "status": "Healthy"}),
                    }
                ),
                encoding="utf-8",
            )

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("schemaVersion", result.stdout + result.stderr)

    def test_succeeded_run_without_an_observed_503_fails(self) -> None:
        """The rule that makes a successful rollout prove it observed a real fault state."""

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            (root / "health-required-redis-unhealthy.json").unlink()

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            combined = result.stdout + result.stderr
            self.assertIn("HTTP 200 and HTTP 503", combined)

    def test_failed_run_health_evidence_without_a_parsable_body_is_valid(self) -> None:
        """A failed probe legitimately records a null status code and a raw transcript."""

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root, status="failed", stage="required-redis-unhealthy")
            for path in root.glob("health-*.json"):
                path.unlink()
            (root / "health-required-redis-unhealthy-001.json").write_text(
                json.dumps(
                    {
                        "schemaVersion": 1,
                        "stage": "required-redis-unhealthy",
                        "attempt": 1,
                        "statusCode": None,
                        "body": "wget: download timed out",
                    }
                ),
                encoding="utf-8",
            )

            result = run_validator(root)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_succeeded_run_tolerates_a_malformed_earlier_attempt_for_the_same_stage(self) -> None:
        """Only the terminal attempt per stage proves the stage's outcome.

        Every poll of a stage is now retained as its own attempt file instead of
        overwriting the previous one, and Wait-AggregateStatus polls through the gap
        between the container becoming Running and the app actually answering health
        checks, so an early attempt legitimately observes a raw transcript with no
        parsable body before the terminal attempt observes the real status. Requiring
        every retained attempt to already look healthy would fail a succeeded run on
        exactly the in-flight observation this diff started retaining.
        """

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            (root / "health-initial-server-health.json").unlink()
            (root / "health-initial-server-health-001.json").write_text(
                json.dumps(
                    {
                        "schemaVersion": 1,
                        "stage": "initial-server-health",
                        "attempt": 1,
                        "statusCode": None,
                        "body": "wget: download timed out",
                    }
                ),
                encoding="utf-8",
            )
            (root / "health-initial-server-health-002.json").write_text(
                json.dumps(
                    {
                        "schemaVersion": 1,
                        "stage": "initial-server-health",
                        "attempt": 2,
                        "statusCode": 200,
                        "body": json.dumps({"schemaVersion": 1, "status": "Healthy"}),
                    }
                ),
                encoding="utf-8",
            )

            result = run_validator(root)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_succeeded_run_fails_when_the_terminal_attempt_itself_is_malformed(self) -> None:
        """The terminal attempt per stage is still held to the succeeded-run contract."""

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            (root / "health-initial-server-health.json").unlink()
            (root / "health-initial-server-health-001.json").write_text(
                json.dumps(
                    {
                        "schemaVersion": 1,
                        "stage": "initial-server-health",
                        "attempt": 1,
                        "statusCode": 200,
                        "body": json.dumps({"schemaVersion": 1, "status": "Healthy"}),
                    }
                ),
                encoding="utf-8",
            )
            (root / "health-initial-server-health-002.json").write_text(
                json.dumps(
                    {
                        "schemaVersion": 1,
                        "stage": "initial-server-health",
                        "attempt": 2,
                        "statusCode": None,
                        "body": "wget: download timed out",
                    }
                ),
                encoding="utf-8",
            )

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("unparsable body", result.stdout + result.stderr)

    def test_renderer_rejects_transposed_and_duplicated_image_arguments(self) -> None:
        """Placeholder counts and the version suffix say nothing about which image goes where."""

        version = "1.2.3"
        correct = {
            "ServerImage": f"registry.hexalith.com/memories:{version}",
            "McpImage": f"registry.hexalith.com/memories-mcp:{version}",
            "AccessTelemetryImage": f"registry.hexalith.com/memories-access-telemetry:{version}",
            "AccessTelemetryClockImage": f"registry.hexalith.com/memories-access-telemetry-clock:{version}",
        }

        def render(images: dict[str, str], output: Path) -> subprocess.CompletedProcess[str]:
            arguments = ["pwsh", "-NoLogo", "-NoProfile", "-File", str(RENDERER), "-Version", version]
            for name, value in images.items():
                arguments += [f"-{name}", value]
            arguments += ["-OutputPath", str(output)]
            return subprocess.run(
                arguments, cwd=REPO_ROOT, text=True, capture_output=True, check=False
            )

        with tempfile.TemporaryDirectory() as temp:
            output = Path(temp) / "rendered.yaml"

            # Server and MCP transposed: every previous guard still passes.
            transposed = dict(correct)
            transposed["ServerImage"], transposed["McpImage"] = (
                correct["McpImage"],
                correct["ServerImage"],
            )
            result = render(transposed, output)
            self.assertNotEqual(0, result.returncode)
            self.assertIn("must reference repository", result.stdout + result.stderr)

            # The same image supplied twice.
            duplicated = dict(correct)
            duplicated["McpImage"] = correct["ServerImage"]
            result = render(duplicated, output)
            self.assertNotEqual(0, result.returncode)
            self.assertIn("must reference repository", result.stdout + result.stderr)

    def test_renderer_requires_every_image_argument(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            output = Path(temp) / "rendered.yaml"
            result = subprocess.run(
                [
                    "pwsh", "-NoLogo", "-NoProfile", "-NonInteractive", "-File", str(RENDERER),
                    "-Version", "1.2.3",
                    "-ServerImage", "registry.hexalith.com/memories:1.2.3",
                    "-OutputPath", str(output),
                ],
                cwd=REPO_ROOT,
                text=True,
                capture_output=True,
                check=False,
            )

            self.assertNotEqual(0, result.returncode)
            self.assertFalse(output.exists())

    def test_verifier_substitutes_openbao_secret_stores_for_the_disposable_cluster(self) -> None:
        # DW 27.3-CR17 (Administrator decision 2026-07-28): the disposable cluster has no
        # OpenBao, and the production `hashicorp.vault` secret stores route component
        # secretKeyRef resolution through it, so daprd exits fatally at component load.
        # The verifier substitutes them with verification-scoped `secretstores.kubernetes`
        # aliases via a merge patch (names and scopes preserved by the cluster object) after
        # the verbatim apply.
        #
        # NOTE, corrected 2026-07-29: the substitution does NOT run "while the applications
        # are scaled to zero". `kubectl apply -f` creates the Deployments at their manifest
        # replica count and the scale-to-zero happens after this block, so a sub-second
        # admission window exists. The assertions below pin the ordering that is actually
        # true and the comment records the window rather than asserting a false invariant.
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        apply_index = verifier.index("kubectl @('apply', '-f', $manifestPath)")
        patch_payload = verifier.index('"type":"secretstores.kubernetes"')
        scale_down = verifier.index("'--replicas=0')")
        self.assertLess(apply_index, patch_payload)
        self.assertLess(patch_payload, scale_down)

        # The substituted set is enumerated from the cluster by spec.type, so a third
        # vault-typed component cannot be silently left unpatched.
        self.assertIn("$_.spec.type -eq 'secretstores.hashicorp.vault'", verifier)
        self.assertIn("'patch', 'component', $component", verifier)
        self.assertNotIn("'patch', 'component', 'secretstore'", verifier)
        # The disclosure records observed post-patch types read back from the cluster, so it
        # cannot be a literal the validator merely asserts back.
        self.assertIn("observedPostPatchTypes = $observedTypes", verifier)
        self.assertIn("Secret-store substitution did not take effect", verifier)
        # A Component type rewrite may only target the disposable cluster.
        self.assertIn("Refusing to substitute secret stores", verifier)
        self.assertIn("secret-store-substitution.json", verifier)

    def test_undisclosed_substitution_is_accepted_and_reported_for_an_unmodified_run(self) -> None:
        # Once Story 31.2 delivers the OpenBao path, a run that applies the manifests
        # unmodified has nothing to disclose. Requiring the record unconditionally pinned the
        # lane to a non-production secret-store topology forever.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            (root / "secret-store-substitution.json").unlink()

            result = run_validator(root)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("applied the production secret stores unmodified", result.stdout)

    def test_failed_run_without_substitution_disclosure_still_validates(self) -> None:
        # The disclosure is written only after cluster create, image loads, contract asserts,
        # render and apply all succeed. Requiring it unconditionally made every honest earlier
        # failure unvalidatable — and because CI runs this step with `if: always()`, it
        # replaced the genuine terminal error with a message about a missing disclosure. This
        # is the same regression this validator already recorded fixing for health bodies.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root, status="failed", stage="apply-production-manifests")
            (root / "secret-store-substitution.json").unlink()

            result = run_validator(root)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertNotIn("substitution", (result.stdout + result.stderr).lower())

    def test_substitution_disclosure_without_observed_types_fails(self) -> None:
        # A disclosure that asserts only the verifier's own literals proves nothing; the
        # observed post-patch types are what bind it to the cluster.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            disclosure = json.loads((root / "secret-store-substitution.json").read_text(encoding="utf-8"))
            del disclosure["observedPostPatchTypes"]
            (root / "secret-store-substitution.json").write_text(json.dumps(disclosure), encoding="utf-8")

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("observed post-patch types", (result.stdout + result.stderr).lower())

    def test_substitution_disclosure_with_empty_reason_fails(self) -> None:
        # The reason narrative is the field an auditor reads. An empty one discloses nothing
        # while satisfying every structural check.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            disclosure = json.loads((root / "secret-store-substitution.json").read_text(encoding="utf-8"))
            disclosure["reason"] = "   "
            (root / "secret-store-substitution.json").write_text(json.dumps(disclosure), encoding="utf-8")

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("non-empty reason", (result.stdout + result.stderr).lower())

    def test_substitution_disclosure_with_wrong_cased_type_fails(self) -> None:
        # The membership and type comparisons are case-SENSITIVE; PowerShell's default
        # -eq/-contains are not, so a wrong-cased type used to satisfy the gate.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            disclosure = json.loads((root / "secret-store-substitution.json").read_text(encoding="utf-8"))
            disclosure["substitutedType"] = "SecretStores.Kubernetes"
            (root / "secret-store-substitution.json").write_text(json.dumps(disclosure), encoding="utf-8")

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("does not match the declared", (result.stdout + result.stderr).lower())

    def test_unparsable_substitution_disclosure_fails_with_a_domain_message(self) -> None:
        # A truncated file used to surface a raw ConvertFrom-Json parser error containing no
        # domain vocabulary at all, unlike every other JSON read in this validator.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            (root / "secret-store-substitution.json").write_text('{"schemaVersion":1,', encoding="utf-8")

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("not parsable json", (result.stdout + result.stderr).lower())

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
