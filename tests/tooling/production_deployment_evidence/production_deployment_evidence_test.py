import base64
import json
import os
import re
import shlex
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
                "schemaVersion": 2,
                "substitutionPerformed": True,
                "reason": "redacted verification-scoped substitution disclosure",
                "substitutedComponents": ["secretstore", "access-telemetry-secrets"],
                "observedComponents": [
                    {"name": "secretstore", "observedType": "secretstores.kubernetes"},
                    {"name": "access-telemetry-secrets", "observedType": "secretstores.kubernetes"},
                ],
                "originalType": "secretstores.hashicorp.vault",
                "substitutedType": "secretstores.kubernetes",
                "observedPostPatchTypes": ["secretstores.kubernetes"],
                "substitutionVerified": True,
                "verificationFailures": [],
                "residualVaultComponents": [],
            }
        ),
        encoding="utf-8",
    )


def flattened_output(result: subprocess.CompletedProcess[str]) -> str:
    """Combined stdout+stderr reduced to plain, unwrapped text.

    PowerShell renders a thrown message as an ANSI-coloured block, hard-wrapped across
    lines with `NNN | ` and `     | ` gutter markers. Stripping colour and collapsing
    whitespace is not enough: the gutter pipes survive inside the sentence, so
    "does not observe" arrives as "does | not observe" and a correct message fails a
    contiguous-substring assertion.
    """

    combined = (result.stdout or "") + (result.stderr or "")
    plain = re.sub(r"\x1b\[[0-9;]*[A-Za-z]", "", combined)
    unwrapped = [re.sub(r"^\s*(?:\d+\s*)?\|\s?", "", line) for line in plain.splitlines()]
    return " ".join(" ".join(unwrapped).split())


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
            # Deliberately NOT asserting `port != 18080`: the ephemeral range can legitimately
            # include it, so that assertion fails with no defect present. The property that
            # actually matters - distinctness while the previous port is still bound - is
            # proven by FreeLoopbackPortExecutionTests.

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
        # Assert ordering at the CALL SITE. The substitution now lives in a function defined
        # near the top of the file, so the file offset of its patch payload no longer tells
        # you anything about when it runs; the invocation does.
        apply_index = verifier.index("kubectl @('apply', '-f', $manifestPath)")
        substitution_call = verifier.index("Invoke-SecretStoreSubstitution -Namespace $namespace")
        scale_down = verifier.index("'--replicas=0')")
        self.assertLess(apply_index, substitution_call)
        self.assertLess(substitution_call, scale_down)
        self.assertIn('"type":"secretstores.kubernetes"', verifier)

        # The substituted set is enumerated from the cluster by spec.type, so a third
        # vault-typed component cannot be silently left unpatched.
        self.assertIn("$_.spec.type -eq 'secretstores.hashicorp.vault'", verifier)
        self.assertIn("'patch', 'component', $component", verifier)
        self.assertNotIn("'patch', 'component', 'secretstore'", verifier)
        # The disclosure records observed post-patch types read back from the cluster, so it
        # cannot be a literal the validator merely asserts back.
        self.assertIn("$observedComponents += [ordered]@{ name = $component", verifier)
        self.assertIn("observed post-patch type", verifier)
        # The disclosure must reach disk BEFORE any verification failure is raised, so a
        # partially rewritten cluster always ships a packet that says so.
        disclosure_write = verifier.index("'secret-store-substitution.json'")
        failure_throw = verifier.index("the cluster may be partially rewritten and secret-store-substitution.json records this failure")
        self.assertLess(disclosure_write, failure_throw)
        # A component that vanished between the patch loop and the readback must not pass.
        self.assertIn("was patched but is absent from the post-patch read", verifier)
        # A Component type rewrite may only target the disposable cluster.
        self.assertIn("Refusing to substitute secret stores", verifier)
        self.assertIn("secret-store-substitution.json", verifier)

    def test_missing_substitution_disclosure_fails_a_succeeded_run(self) -> None:
        # Absence must never be the off-switch for this gate. A substituted run whose
        # disclosure was never written, or was lost from the uploaded packet, previously
        # validated clean while the validator printed an unverified claim that the production
        # secret stores had run unmodified — the strongest claim in the packet, asserted by a
        # missing file. The verifier now always writes the record, so absence is a defect.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            (root / "secret-store-substitution.json").unlink()

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("secret-store-substitution.json is missing", flattened_output(result))

    def test_unmodified_run_is_accepted_only_via_a_positive_assertion(self) -> None:
        # Once Story 31.2 delivers the OpenBao path, a run applies the manifests unmodified.
        # That state is legitimate, but it must be asserted (substitutionPerformed=false), not
        # inferred from a missing file.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            disclosure = json.loads((root / "secret-store-substitution.json").read_text(encoding="utf-8"))
            disclosure["substitutionPerformed"] = False
            disclosure["substitutedComponents"] = []
            disclosure["observedComponents"] = []
            disclosure["observedPostPatchTypes"] = []
            disclosure["reason"] = "no vault-typed component was applied; production secret stores ran unmodified"
            (root / "secret-store-substitution.json").write_text(json.dumps(disclosure), encoding="utf-8")

            result = run_validator(root)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("substitutionPerformed=false", result.stdout)

    def test_unmodified_claim_contradicted_by_named_components_fails(self) -> None:
        # substitutionPerformed=false while still naming substituted components is a packet
        # that contradicts itself; neither half may be silently preferred.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            disclosure = json.loads((root / "secret-store-substitution.json").read_text(encoding="utf-8"))
            disclosure["substitutionPerformed"] = False
            (root / "secret-store-substitution.json").write_text(json.dumps(disclosure), encoding="utf-8")

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("while naming substituted components", flattened_output(result))

    def test_disclosure_naming_a_component_it_never_observed_fails(self) -> None:
        # The deleted per-name membership check left the field an auditor reads unverified: a
        # disclosure could name a component that was never touched, or omit one that was, and
        # the unique-collapsed type list could not cross-check either.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            disclosure = json.loads((root / "secret-store-substitution.json").read_text(encoding="utf-8"))
            disclosure["substitutedComponents"] = ["secretstore", "something-else"]
            (root / "secret-store-substitution.json").write_text(json.dumps(disclosure), encoding="utf-8")

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("does not observe exactly the components", flattened_output(result))

    def test_disclosure_with_a_component_left_at_the_vault_type_fails(self) -> None:
        # Per-component observation: one component still vault-typed must fail even though the
        # others are correct. A set-of-types check could not see this.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            disclosure = json.loads((root / "secret-store-substitution.json").read_text(encoding="utf-8"))
            disclosure["observedComponents"][1]["observedType"] = "secretstores.hashicorp.vault"
            (root / "secret-store-substitution.json").write_text(json.dumps(disclosure), encoding="utf-8")

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("observed post-patch type is not secretstores.kubernetes", flattened_output(result))

    def test_failed_run_with_a_false_disclosure_fails(self) -> None:
        # A failed packet may legitimately carry a disclosure, since the verifier writes it
        # before every health stage. It was previously never parsed at all, so a failed packet
        # could carry an arbitrarily false record.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root, status="failed", stage="initial-server-health")
            disclosure = json.loads((root / "secret-store-substitution.json").read_text(encoding="utf-8"))
            disclosure["substitutedType"] = "secretstores.hashicorp.vault"
            (root / "secret-store-substitution.json").write_text(json.dumps(disclosure), encoding="utf-8")

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("on a failed run does not match", flattened_output(result))

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

    def test_substitution_disclosure_without_observed_components_fails(self) -> None:
        # A disclosure that asserts only the verifier's own literals proves nothing; the
        # per-component observations read back from the cluster are what bind it. Deleting
        # observedPostPatchTypes no longer suffices as the test target: that field is a
        # unique-collapsed summary and cannot cross-check names or per-component types.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_complete_evidence(root)
            disclosure = json.loads((root / "secret-store-substitution.json").read_text(encoding="utf-8"))
            del disclosure["observedComponents"]
            (root / "secret-store-substitution.json").write_text(json.dumps(disclosure), encoding="utf-8")

            result = run_validator(root)

            self.assertNotEqual(0, result.returncode)
            self.assertIn("does not observe exactly the components", flattened_output(result))

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


def extract_ps_function(source: str, name: str) -> str:
    """Return a top-level PowerShell function from the verifier, verbatim.

    The verifier's top-level functions close with a column-0 brace and every brace
    inside them is indented, so splitting on "\\n}\\n" yields exactly one function.
    Extracting the real function is the whole point: a test that re-implements the
    logic it claims to prove passes while the production path regresses.
    """

    head = f"function {name} {{"
    if head not in source:
        raise AssertionError(f"{name} not found in {VERIFIER}")
    return head + source.split(head, 1)[1].split("\n}\n", 1)[0] + "\n}"


def run_pwsh_with_stub_path(script: str, stub_dir: Path) -> subprocess.CompletedProcess[str]:
    """Run pwsh with a stub binary directory ahead of the real PATH."""

    env = dict(os.environ)
    env["PATH"] = f"{stub_dir}{os.pathsep}{env.get('PATH', '')}"
    return subprocess.run(
        ["pwsh", "-NoLogo", "-NoProfile", "-Command", script],
        cwd=REPO_ROOT,
        text=True,
        capture_output=True,
        check=False,
        env=env,
    )


def write_kubectl_stub(root: Path, transcript: str) -> Path:
    """Create a kubectl stub that emits a fixed in-container probe transcript."""

    bin_dir = root / "stub-bin"
    bin_dir.mkdir(parents=True, exist_ok=True)
    transcript_path = root / "in-container.txt"
    transcript_path.write_text(transcript, encoding="utf-8")
    stub = bin_dir / "kubectl"
    stub.write_text(f'#!/bin/sh\ncat "{transcript_path}"\nexit 0\n', encoding="utf-8")
    stub.chmod(0o755)
    return bin_dir


class GetHealthResponseExecutionTests(unittest.TestCase):
    """Execute the real Get-HealthResponse.

    The sibling source-text pins and the hand-written Resolve-Response copy in
    test_fallback_trigger_and_isolated_parse_decide_the_recorded_response cannot
    observe the production function at all: inverting the fallback trigger,
    reverting the isolated parse, and widening the @(200, 503) allowlist each left
    the whole suite green. These cases drive the actual function so those three
    mutations fail.
    """

    def drive(self, *, in_container: str, fallback: str) -> dict[str, object]:
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        function = extract_ps_function(verifier, "Get-HealthResponse")

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            bin_dir = write_kubectl_stub(root, in_container)
            marker = root / "fallback-was-called"
            encoded_fallback = base64.b64encode(fallback.encode("utf-8")).decode("ascii")

            script = (
                ". ./tools/production-deployment-health.ps1\n"
                f"{function}\n"
                "$namespace = 'stub-namespace'\n"
                # Double the port-forward capture so the trigger itself is observable.
                "function Get-HealthResponseViaPortForward {\n"
                "    param([Parameter(Mandatory)][string]$Pod, [Parameter(Mandatory)][string]$Container)\n"
                f"    Set-Content -LiteralPath '{marker}' -Value 'called' -Encoding utf8\n"
                "    return [System.Text.Encoding]::UTF8.GetString("
                f"[System.Convert]::FromBase64String('{encoded_fallback}'))\n"
                "}\n"
                "$response = Get-HealthResponse 'pod-under-test' 'server'\n"
                "Write-Output \"STATUSCODE=$($response.StatusCode)\"\n"
                "Write-Output \"BODY=$([System.Convert]::ToBase64String("
                "[System.Text.Encoding]::UTF8.GetBytes([string]$response.Body)))\"\n"
            )

            result = run_pwsh_with_stub_path(script, bin_dir)
            self.assertEqual(0, result.returncode, result.stderr)

            values: dict[str, object] = {"fallback_called": marker.exists()}
            for line in result.stdout.splitlines():
                if line.startswith("STATUSCODE="):
                    raw = line.split("=", 1)[1].strip()
                    values["status_code"] = int(raw) if raw else None
                elif line.startswith("BODY="):
                    values["body"] = base64.b64decode(line.split("=", 1)[1].strip()).decode("utf-8")
            return values

    def test_healthy_in_container_response_does_not_fire_the_port_forward_fallback(self) -> None:
        """Inverting the trigger to fire only on a good body must fail here."""

        result = self.drive(
            in_container=(
                "HTTP/1.1 200 OK\r\n"
                "Content-Type: application/json\r\n"
                "\r\n"
                '{"schemaVersion":1,"status":"Healthy"}\n'
            ),
            fallback='HTTP/1.1 503 Service Unavailable\n\n{"schemaVersion":1,"status":"Unhealthy"}\n',
        )
        self.assertFalse(result["fallback_called"], "fallback fired despite a status-bearing body")
        self.assertEqual(200, result["status_code"])
        self.assertEqual("Healthy", json.loads(str(result["body"]))["status"])

    def test_malformed_in_container_transcript_parses_the_fallback_in_isolation(self) -> None:
        """Reverting to Get-HealthJsonBody over the concatenation must fail here.

        The in-container half carries an unbalanced brace and an odd quote, which is
        exactly the precondition for firing the fallback; scanning the concatenation
        swallows the clean fallback object and returns the raw transcript.
        """

        result = self.drive(
            in_container='nc-fallback begin (wget exit 1)\n{"truncated":"no close brace\nnc-fallback end (nc exit 1)\n',
            fallback='HTTP/1.1 503 Service Unavailable\n\n{"schemaVersion":1,"status":"Unhealthy"}\n',
        )
        self.assertTrue(result["fallback_called"])
        self.assertEqual("Unhealthy", json.loads(str(result["body"]))["status"])

    def test_fallback_status_code_outside_the_stage_contract_cannot_replace_the_pod_answer(self) -> None:
        """Widening the allowlist to any parsed code must fail here.

        A probe-side 401 (an unmounted or corrupt app token) must not overwrite the
        status code the pod itself reported.
        """

        result = self.drive(
            in_container="HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nnot json at all\n",
            fallback='HTTP/1.1 401 Unauthorized\n\n{"schemaVersion":1,"status":"Healthy"}\n',
        )
        self.assertTrue(result["fallback_called"])
        self.assertEqual(200, result["status_code"], "a 401 overwrote the pod's own status code")

    def test_fallback_status_code_inside_the_stage_contract_replaces_the_pod_answer(self) -> None:
        """The allowlist must still admit the contract codes it exists to pass."""

        result = self.drive(
            in_container="HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nnot json at all\n",
            fallback='HTTP/1.1 503 Service Unavailable\n\n{"schemaVersion":1,"status":"Unhealthy"}\n',
        )
        self.assertTrue(result["fallback_called"])
        self.assertEqual(503, result["status_code"])
        self.assertEqual("Unhealthy", json.loads(str(result["body"]))["status"])


class DisposableClusterGuardExecutionTests(unittest.TestCase):
    """Execute the guard that stops the verifier rewriting Component spec.type off-cluster.

    Inverting the comparison in place left the whole suite green while inverting the
    safety property exactly: refuse on the disposable cluster, proceed on any other.
    """

    def run_guard(self, *, context: str, stderr: str = "", exit_code: int = 0) -> subprocess.CompletedProcess[str]:
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        function = extract_ps_function(verifier, "Assert-DisposableClusterContext")

        with tempfile.TemporaryDirectory() as tmp:
            bin_dir = Path(tmp) / "stub-bin"
            bin_dir.mkdir()
            stub = bin_dir / "kubectl"
            stub.write_text(
                "#!/bin/sh\n"
                + (f"printf %s {shlex.quote(stderr)} 1>&2\n" if stderr else "")
                + f"printf %s {shlex.quote(context)}\n"
                + f"exit {exit_code}\n",
                encoding="utf-8",
            )
            stub.chmod(0o755)
            script = f"{function}\nAssert-DisposableClusterContext 'memories-prod-verify'\nWrite-Output 'GUARD=allowed'\n"
            return run_pwsh_with_stub_path(script, bin_dir)

    def test_guard_refuses_a_nonzero_exit_that_still_prints_a_context(self) -> None:
        # test_guard_refuses_when_the_context_cannot_be_read sets BOTH context="" and
        # exit_code=1, so deleting the $LASTEXITCODE clause survived it. A kubectl that
        # exits non-zero while still printing a plausible context name must be refused.
        result = self.run_guard(context="kind-memories-prod-verify", exit_code=1)
        self.assertEqual(1, result.returncode)
        self.assertIn("Refusing to substitute secret stores", flattened_output(result))

    def test_guard_allows_the_disposable_cluster(self) -> None:
        """Inverting the comparison must fail here."""

        result = self.run_guard(context="kind-memories-prod-verify")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("GUARD=allowed", result.stdout)

    def test_guard_refuses_any_other_context(self) -> None:
        result = self.run_guard(context="jpiquot@local")
        self.assertNotEqual(0, result.returncode)
        self.assertIn("Refusing to substitute secret stores", flattened_output(result))
        self.assertNotIn("GUARD=allowed", result.stdout)

    def test_guard_tolerates_a_kubectl_warning_on_stderr(self) -> None:
        """Invoke-Checked merges stderr; a warning line would refuse a valid cluster."""

        result = self.run_guard(
            context="kind-memories-prod-verify",
            stderr="W0730 12:00:00.000000   1 warnings.go:70] deprecated flag\n",
        )
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("GUARD=allowed", result.stdout)

    def test_guard_refuses_when_the_context_cannot_be_read(self) -> None:
        result = self.run_guard(context="", exit_code=1)
        self.assertNotEqual(0, result.returncode)
        self.assertIn("could not read the active kubectl context", flattened_output(result))


class FreeLoopbackPortExecutionTests(unittest.TestCase):
    """The helper replaced a fixed 18080; returning any other constant is the same defect."""

    def test_consecutive_calls_return_distinct_bindable_ports(self) -> None:
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        function = extract_ps_function(verifier, "Get-FreeLoopbackPort")

        # Hold each port open while asking for the next one. The original test released every
        # listener before the next call, so identical ports were a legal result and `return
        # 18081` passed - the same defect under a different constant.
        script = (
            f"{function}\n"
            "$held = @()\n"
            "$ports = @()\n"
            "try {\n"
            "    1..3 | ForEach-Object {\n"
            "        $port = Get-FreeLoopbackPort\n"
            "        $ports += $port\n"
            "        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $port)\n"
            "        $listener.Start()\n"
            "        $held += $listener\n"
            "    }\n"
            "}\n"
            "finally { $held | ForEach-Object { $_.Stop() } }\n"
            "Write-Output ($ports -join ',')\n"
        )
        result = run_pwsh(script)
        self.assertEqual(0, result.returncode, result.stderr)
        ports = [int(v) for v in result.stdout.strip().splitlines()[-1].split(",")]
        self.assertEqual(3, len(ports))
        # Each port was still bound when the next was requested, so a constant cannot pass.
        self.assertEqual(3, len(set(ports)), f"helper returned a repeated port: {ports}")
        for port in ports:
            self.assertGreater(port, 1024)
            self.assertLess(port, 65536)


class StaleEvidenceCleanupExecutionTests(unittest.TestCase):
    """A reused evidence directory must not let a stale disclosure survive into a new run."""

    def test_stale_secret_store_disclosure_is_removed_before_a_new_run(self) -> None:
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        block = verifier.split("$ownedEvidenceNames = @(", 1)[1].split("Remove-Item -Force", 1)[0]
        cleanup = "$ownedEvidenceNames = @(" + block + "Remove-Item -Force"

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "secret-store-substitution.json").write_text("stale disclosure", encoding="utf-8")
            (root / "verification-result.json").write_text("stale result", encoding="utf-8")
            (root / "operator-notes.txt").write_text("not owned by the verifier", encoding="utf-8")

            script = f"$evidencePath = '{root}'\n{cleanup}\n"
            result = run_pwsh(script)
            self.assertEqual(0, result.returncode, result.stderr)

            self.assertFalse(
                (root / "secret-store-substitution.json").exists(),
                "a stale disclosure survived into a new run; the validator would accept a "
                "substitution this run never performed",
            )
            self.assertFalse((root / "verification-result.json").exists())
            self.assertTrue((root / "operator-notes.txt").exists(), "cleanup removed a file it does not own")


class ProbeTimeoutBudgetTests(unittest.TestCase):
    """Tie the per-poll worst case to the startup budget it is sized against.

    Every latency bound was unpinned in both directions: sleep 2 -> 20, --max-time 6 -> 600,
    the 2s establish deadline -> 200s and the 4-minute outer deadline -> 40 all survived.
    The verifier's own comment reasons about "about 10s per poll ... against the 60-second
    startup budget", so assert that arithmetic rather than each constant in isolation.
    """

    def constants(self) -> dict[str, int]:
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        patterns = {
            "wget_timeout": r"wget -S -O- -T (\d+) ",
            "nc_grace_sleep": r"sleep (\d+); \} \| nc ",
            "nc_timeout": r"\| nc -w (\d+) 127\.0\.0\.1 8080",
            "curl_max_time": r"--max-time', '(\d+)'|--max-time (\d+)",
            "establish_seconds": r"\$establishDeadline = \[DateTime\]::UtcNow\.AddSeconds\((\d+)\)",
            "forward_kill_wait_ms": r"\$forward\.WaitForExit\((\d+)\)",
            "exec_request_timeout": r"kubectl --request-timeout=(\d+)s exec",
            "outer_deadline_minutes": r"\$deadline = \[DateTime\]::UtcNow\.AddMinutes\((\d+)\)",
        }
        found: dict[str, int] = {}
        for name, pattern in patterns.items():
            match = re.search(pattern, verifier)
            self.assertIsNotNone(match, f"could not locate {name} in the verifier")
            found[name] = int(next(g for g in match.groups() if g is not None))
        return found

    def test_per_poll_worst_case_stays_inside_the_startup_budget(self) -> None:
        c = self.constants()
        in_container_worst_case = c["wget_timeout"] + c["nc_grace_sleep"] + c["nc_timeout"]
        # The runner side owes three terms, not two: the port-forward kill wait added by the
        # marker fix, and the token-read exec, which was the one unbounded kubectl call in the
        # per-poll path. Omitting them let the class claim a bound it did not model.
        runner_worst_case = (
            c["establish_seconds"]
            + c["curl_max_time"]
            + c["forward_kill_wait_ms"] // 1000
            + c["exec_request_timeout"]
        )
        per_poll = in_container_worst_case + runner_worst_case

        # 60s is the -TimeoutSeconds contract of both startup stages. A single poll must not be
        # able to consume the whole budget, or one slow poll decides the stage.
        self.assertLess(
            per_poll, 60,
            f"one poll can consume {per_poll}s of the 60-second startup budget: {c}",
        )
        # The in-container branch is diagnostic; the runner-side capture is the deterministic
        # body producer. Neither may dominate the poll on its own.
        # Bound at the value the verifier's own comment derives (wget 6 + sleep 2 + nc 4 = 12),
        # not at a slack ceiling. The previous limit of 30 was satisfied EXACTLY by the
        # sleep 2 -> 20 mutation, so the bound admitted the regression it existed to catch.
        self.assertLessEqual(in_container_worst_case, 12, f"in-container branch too slow: {c}")
        self.assertLessEqual(runner_worst_case, 25, f"runner-side capture too slow: {c}")
        # The outer deadline must allow several polls, not one or two.
        self.assertGreaterEqual(c["outer_deadline_minutes"] * 60, 4 * per_poll, f"outer deadline too tight: {c}")
        # ...and must not be so long that a wedged stage runs for most of a CI job.
        self.assertLessEqual(c["outer_deadline_minutes"], 5, f"outer deadline too permissive: {c}")


class VerifierInvariantPinTests(unittest.TestCase):
    """Presence pins for verifier behaviour this lane cannot execute.

    These three ride on `kind`, a live cluster and a real port-forward, none of which
    exist here, so they are pinned rather than driven. Each records WHY it matters, so a
    future change that removes one has to argue with the reason rather than a bare string.
    """

    def test_disclosure_is_written_to_the_path_the_validator_reads(self) -> None:
        # Redirecting the write (e.g. to a .bak suffix) produces a succeeded packet with no
        # disclosure. The validator now rejects that, but only the verifier decides where the
        # file lands, and nothing here can run the verifier end to end.
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        # Two halves now: the call site names the path the validator reads, and the function
        # writes to the path it was given. Asserting only one half would let the other drift.
        self.assertIn(
            "-DisclosurePath (Join-Path $evidencePath 'secret-store-substitution.json')",
            verifier,
        )
        body = extract_ps_function(verifier, "Invoke-SecretStoreSubstitution")
        self.assertIn("Set-Content -LiteralPath $DisclosurePath -Encoding utf8", body)
        self.assertIn("substitutionPerformed = $substitutionPerformed", verifier)

    def test_curl_is_a_declared_prerequisite(self) -> None:
        # The runner-side capture shells out to curl. Without the preflight entry the failure
        # is a terminating CommandNotFoundException raised from inside the poll loop, aborting
        # mid-stage with an error unrelated to the deployment.
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        self.assertRegex(verifier, r"@\('docker', 'kind', 'kubectl', 'dapr', 'pwsh', 'curl'\)")

    def test_port_forward_kill_is_awaited_and_its_outcome_recorded(self) -> None:
        # Without the wait, a forward that outlives its Stop-Process keeps holding its port;
        # the next poll's Get-FreeLoopbackPort then avoids that port, so the leak is silent
        # and unbounded across a 4-minute stage. Awaiting is not enough on its own: the
        # original code discarded the result into $null and never read $forward.ExitCode, so a
        # forward that survived the wait left no trace at all.
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        self.assertIn("$exited = $forward.WaitForExit(2000)", verifier)
        self.assertIn("port-forward kill: process did not exit within 2s", verifier)
        self.assertNotIn("$null = $forward.WaitForExit(2000)", verifier)


class PodApplicationTokenExecutionTests(unittest.TestCase):
    """Execute the real Get-PodApplicationToken.

    Both existing pins target the call site, so replacing the helper body with the
    literal 'verification-app-api-token' left them green - restoring exactly the defect
    the helper was added to fix: a pod whose token is wrong or unmounted answers 401
    in-container while the runner-side probe presents the correct literal, and the
    stage passes on the substitute credential.
    """

    def read_token(self, *, stdout: str, stderr: str = "", exit_code: int = 0) -> str:
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        function = extract_ps_function(verifier, "Get-PodApplicationToken")

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            bin_dir = root / "stub-bin"
            bin_dir.mkdir()
            stub = bin_dir / "kubectl"
            stub.write_text(
                "#!/bin/sh\n"
                + (f"printf %s {shlex.quote(stderr)} 1>&2\n" if stderr else "")
                + f"printf %s {shlex.quote(stdout)}\n"
                + f"exit {exit_code}\n",
                encoding="utf-8",
            )
            stub.chmod(0o755)

            script = (
                f"{function}\n"
                "$namespace = 'stub-namespace'\n"
                "$token = Get-PodApplicationToken 'pod-under-test' 'server'\n"
                "Write-Output \"TOKEN=[$token]\"\n"
            )
            result = run_pwsh_with_stub_path(script, bin_dir)
            self.assertEqual(0, result.returncode, result.stderr)
            for line in result.stdout.splitlines():
                if line.startswith("TOKEN=["):
                    return line[len("TOKEN=["):].rsplit("]", 1)[0]
        raise AssertionError("token line not emitted")

    def test_token_is_read_from_the_pod_not_a_hardcoded_literal(self) -> None:
        """Substituting the seeded literal for the helper body must fail here."""

        self.assertEqual("token-actually-in-the-pod", self.read_token(stdout="token-actually-in-the-pod"))

    def test_kubectl_warning_on_stderr_does_not_corrupt_the_token(self) -> None:
        """kubectl exits 0 while writing warnings to stderr.

        Merging them with 2>&1 and -join '' produced a corrupt dapr-api-token whose 401
        the caller's @(200, 503) allowlist then discards, so a valid cluster looked like
        a pod that never answered.
        """

        token = self.read_token(
            stdout="real-token-value",
            stderr="W0730 12:00:00.000000   1234 warnings.go:70] deprecated flag\n",
        )
        self.assertEqual("real-token-value", token)

    def test_failed_token_read_yields_a_distinguishable_marker(self) -> None:
        # Returning '' for a failed exec made it indistinguishable from an empty token, and
        # the resulting 401 was silently dropped by the caller's @(200, 503) allowlist.
        self.assertEqual(
            "<unavailable: kubectl exec failed>",
            self.read_token(stdout="unusable", exit_code=1),
        )

    def test_empty_but_successful_token_read_yields_its_own_marker(self) -> None:
        # `printf %s ""` on an unset APP_API_TOKEN writes nothing and exits 0. This case
        # previously returned '' - the same value as the exec-failed path - so an unmounted
        # token was indistinguishable from a genuine 401 and appeared in no transcript.
        self.assertEqual(
            "<unavailable: APP_API_TOKEN is unset or empty in the container>",
            self.read_token(stdout="", exit_code=0),
        )


class StartupBudgetExecutionTests(unittest.TestCase):
    """Execute the real Wait-AggregateStatus startup-budget arithmetic.

    Nothing executed this function before, so multiplying the overhead credit by 100
    left the suite green. Two defects rode along in the shipped code: the credit was
    subtracted from ($healthyAt - $runningAt), a difference of two cluster-recorded
    timestamps that contains no verifier capture time at all; and it was never reset
    when the container instance changed, so a crash-looping container pre-paid its
    replacement's budget.
    """

    def drive(self, polls: list[dict[str, object]], timeout_seconds: int = 60) -> dict[str, object]:
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        function = extract_ps_function(verifier, "Wait-AggregateStatus")

        # Each poll scripts one Get-RunningContainerObservation + Get-HealthResponse pair.
        # Offsets are seconds BEFORE the frozen reference instant, so a container that
        # became Ready 90s after starting is started_ago=90, ready_ago=0.
        entries = ",".join(
            "@{{ StartedAgo = {started}; ReadyAgo = {ready}; Status = '{status}'; Code = {code}; Fallback = {fallback} }}".format(
                started=poll["started_ago"],
                ready="$null" if poll.get("ready_ago") is None else poll["ready_ago"],
                status=poll["status"],
                code=poll["code"],
                fallback=poll["fallback"],
            )
            for poll in polls
        )

        script = (
            f"{function}\n"
            "$now = [DateTime]::UtcNow\n"
            f"$polls = @({entries})\n"
            "$script:index = 0\n"
            "function Set-VerificationStage { param([string]$Stage) }\n"
            "function Save-HealthResponseEvidence { param($Stage, $StatusCode, $Body, $Transcript) }\n"
            "function Start-Sleep { param($Seconds) }\n"
            "function Get-RunningContainerObservation {\n"
            "    param($AppName, $Container, $RequiredPodAnnotationName, $RequiredPodAnnotationValue)\n"
            "    $p = $polls[[math]::Min($script:index, $polls.Count - 1)]\n"
            "    [pscustomobject]@{\n"
            "        PodName = 'pod-' + $p.StartedAgo\n"
            "        ContainerStartedAt = $now.AddSeconds(-1 * $p.StartedAgo)\n"
            "        ReadyAt = $(if ($null -eq $p.ReadyAgo) { $null } else { $now.AddSeconds(-1 * $p.ReadyAgo) })\n"
            "    }\n"
            "}\n"
            "function Get-HealthResponse {\n"
            "    param($Pod, $Container)\n"
            "    $p = $polls[[math]::Min($script:index, $polls.Count - 1)]\n"
            "    $script:index++\n"
            "    [pscustomobject]@{\n"
            "        StatusCode = $p.Code\n"
            "        Body = '{\"schemaVersion\":1,\"status\":\"' + $p.Status + '\"}'\n"
            "        Raw = 'raw'\n"
            "        FallbackSeconds = [double]$p.Fallback\n"
            "    }\n"
            "}\n"
            "try {\n"
            f"    $null = Wait-AggregateStatus -AppName 'memories' -Container 'server' -ExpectedStatus 'Healthy' -Stage 'test-stage' -TimeoutSeconds {timeout_seconds} -MeasureFromContainerRunning\n"
            "    Write-Output 'OUTCOME=passed'\n"
            "}\n"
            "catch {\n"
            "    Write-Output 'OUTCOME=threw'\n"
            "    Write-Output \"MESSAGE=$($_.Exception.Message)\"\n"
            "}\n"
        )

        result = run_pwsh(script)
        self.assertEqual(0, result.returncode, result.stderr)
        values: dict[str, object] = {"message": ""}
        for line in result.stdout.splitlines():
            if line.startswith("OUTCOME="):
                values["outcome"] = line.split("=", 1)[1].strip()
            elif line.startswith("MESSAGE="):
                values["message"] = line.split("=", 1)[1].strip()
        return values

    def test_kubernetes_recorded_ready_interval_is_not_credited_with_capture_time(self) -> None:
        """The false-pass this arithmetic produced.

        Kubernetes recorded Ready 90s after the container started - outside the
        60-second contract. Crediting 30s of verifier-side capture against a pair of
        cluster timestamps that never contained it computes 60 and passes.
        """

        result = self.drive(
            [{"started_ago": 90, "ready_ago": 0, "status": "Healthy", "code": 200, "fallback": 30}]
        )
        self.assertEqual("threw", result["outcome"], "a 90s cold start passed the 60s contract")
        self.assertIn("startup limit", str(result["message"]))

    def test_ready_inside_the_budget_still_passes_with_capture_overhead(self) -> None:
        """Control: the fix must not start failing genuinely healthy startups."""

        result = self.drive(
            [{"started_ago": 30, "ready_ago": 0, "status": "Healthy", "code": 200, "fallback": 30}]
        )
        self.assertEqual("passed", result["outcome"])

    def test_reported_capture_credit_matches_the_overhead_actually_accrued(self) -> None:
        """The wall-clock branch is the only place the credit is applied; pin its magnitude.

        The other cases in this class either take the cluster-timestamp path (credit 0) or
        reset the credit at a restart, so inflating the accumulator survived them. Here a
        single instance accrues a known 5s and the throw must report exactly that: an
        inflated accumulator reports the clamp (60s) instead.
        """

        result = self.drive(
            [{"started_ago": 130, "ready_ago": None, "status": "Unhealthy", "code": 503, "fallback": 5}]
        )
        self.assertEqual("threw", result["outcome"])
        self.assertIn("excluding 5s", str(result["message"]))

    def test_capture_credit_is_capped_at_the_budget(self) -> None:
        """An uncapped credit grows faster than the ~2s cadence, so the throw never fires."""

        result = self.drive(
            [{"started_ago": 130, "ready_ago": None, "status": "Unhealthy", "code": 503, "fallback": 5000}]
        )
        self.assertEqual("threw", result["outcome"])
        self.assertIn("excluding 60s", str(result["message"]))
        self.assertIn("capped at the 60-second budget", str(result["message"]))

    def test_capture_credit_does_not_survive_a_container_restart(self) -> None:
        """Overhead charged while probing a failed predecessor must not pre-pay its replacement.

        Poll 1 accrues 50s against instance A. Poll 2 observes a different instance;
        its own budget must start from zero, so the reported exclusion is 0s, not 50s.
        """

        result = self.drive(
            [
                {"started_ago": 30, "ready_ago": None, "status": "Unhealthy", "code": 503, "fallback": 50},
                {"started_ago": 130, "ready_ago": None, "status": "Unhealthy", "code": 503, "fallback": 0},
            ]
        )
        self.assertEqual("threw", result["outcome"])
        self.assertIn("excluding 0s", str(result["message"]))
        self.assertNotIn("excluding 50s", str(result["message"]))


class CallSiteInvariantTests(unittest.TestCase):
    """Pin that extracted guards are INVOKED, not merely defined.

    Extracting Assert-DisposableClusterContext moved the string "Refusing to substitute
    secret stores" out of the executed call site and into the function body, so the pin
    that had caught deletion of the guard matched whether or not the guard ever ran.
    Commenting out the invocation left all 67 tests green while the verifier would
    `kubectl patch component ... spec.type` on whatever context happened to be active.
    Extraction without a call-site pin is a coverage regression, so every extraction in
    this file gets one.
    """

    def setUp(self) -> None:
        self.verifier = VERIFIER.read_text(encoding="utf-8-sig")

    def test_disposable_cluster_guard_is_invoked_inside_the_substitution_function(self) -> None:
        body = extract_ps_function(self.verifier, "Invoke-SecretStoreSubstitution")
        self.assertIn("Assert-DisposableClusterContext $ClusterName", body)

    def test_substitution_function_is_invoked_outside_its_own_definition(self) -> None:
        call = "Invoke-SecretStoreSubstitution -Namespace $namespace"
        body = extract_ps_function(self.verifier, "Invoke-SecretStoreSubstitution")
        self.assertNotIn(call, body)
        self.assertIn(call, self.verifier)

    def test_guard_precedes_every_component_patch(self) -> None:
        body = extract_ps_function(self.verifier, "Invoke-SecretStoreSubstitution")
        self.assertLess(
            body.index("Assert-DisposableClusterContext"),
            body.index("'patch', 'component'"),
        )

    def test_fallback_seconds_is_carried_from_the_measured_capture(self) -> None:
        # Source pin, stated as such: the only producer of the startup credit is this
        # assignment, and replacing it with 0.0 silently disabled the whole mechanism
        # while both execution classes stayed green (one stubs Get-HealthResponse, the
        # other reads only StatusCode and Body).
        body = extract_ps_function(self.verifier, "Get-HealthResponse")
        self.assertIn("FallbackSeconds = $fallbackSeconds", body)


class SecretStoreSubstitutionExecutionTests(unittest.TestCase):
    """Execute the real Invoke-SecretStoreSubstitution against a stubbed kubectl.

    While this block was inline script, seven independent mutations of it survived the
    whole suite: the disclosure write, the substitutionPerformed flag, both post-patch
    checks, the observedPostPatchTypes field, the reason narrative, and the validator's
    boolean type check. Its only coverage was source-text assertIn over the file, which
    every one of those mutations preserves.
    """

    def run_substitution(
        self,
        *,
        pre: object,
        post: object,
        context: str = "kind-verify",
        cluster: str = "verify",
    ) -> dict[str, object]:
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        functions = "\n".join(
            extract_ps_function(verifier, name)
            for name in ("Invoke-Checked", "Assert-DisposableClusterContext", "Invoke-SecretStoreSubstitution")
        )

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "pre.json").write_text(json.dumps(pre), encoding="utf-8")
            (root / "post.json").write_text(json.dumps(post), encoding="utf-8")
            counter = root / "get-count"
            disclosure = root / "secret-store-substitution.json"

            bin_dir = root / "stub-bin"
            bin_dir.mkdir()
            stub = bin_dir / "kubectl"
            stub.write_text(
                "#!/bin/sh\n"
                f"COUNTER={shlex.quote(str(counter))}\n"
                'case "$1" in\n'
                f"  config) printf %s {shlex.quote(context)}; exit 0;;\n"
                "  get)\n"
                '    n=$(cat "$COUNTER" 2>/dev/null || echo 0); n=$((n+1)); echo "$n" > "$COUNTER"\n'
                '    if [ "$n" = "1" ]; then\n'
                f"      cat {shlex.quote(str(root / 'pre.json'))}\n"
                "    else\n"
                f"      cat {shlex.quote(str(root / 'post.json'))}\n"
                "    fi\n"
                "    exit 0;;\n"
                "  patch) exit 0;;\n"
                "esac\n"
                "exit 0\n",
                encoding="utf-8",
            )
            stub.chmod(0o755)

            script = (
                f"{functions}\n"
                "try {\n"
                f"    Invoke-SecretStoreSubstitution -Namespace 'ns' -ClusterName '{cluster}' "
                f"-DisclosurePath {shlex.quote(str(disclosure))}\n"
                "    Write-Output 'OUTCOME=passed'\n"
                "}\n"
                "catch {\n"
                "    Write-Output 'OUTCOME=threw'\n"
                "    Write-Output \"MESSAGE=$($_.Exception.Message)\"\n"
                "}\n"
            )
            result = run_pwsh_with_stub_path(script, bin_dir)
            text = flattened_output(result)
            outcome = "threw" if "OUTCOME=threw" in text else ("passed" if "OUTCOME=passed" in text else "unknown")
            written = json.loads(disclosure.read_text(encoding="utf-8")) if disclosure.exists() else None
            return {"outcome": outcome, "message": text, "disclosure": written}

    @staticmethod
    def component(name: str, type_name: str) -> dict[str, object]:
        return {"metadata": {"name": name}, "spec": {"type": type_name}}

    def test_two_vault_components_are_substituted_and_disclosed(self) -> None:
        pre = {"items": [
            self.component("secretstore", "secretstores.hashicorp.vault"),
            self.component("access-telemetry-secrets", "secretstores.hashicorp.vault"),
            self.component("statestore", "state.postgresql"),
        ]}
        post = {"items": [
            self.component("secretstore", "secretstores.kubernetes"),
            self.component("access-telemetry-secrets", "secretstores.kubernetes"),
            self.component("statestore", "state.postgresql"),
        ]}
        result = self.run_substitution(pre=pre, post=post)
        self.assertEqual("passed", result["outcome"], result["message"])
        disclosure = result["disclosure"]
        self.assertIsNotNone(disclosure)
        self.assertTrue(disclosure["substitutionPerformed"])
        self.assertTrue(disclosure["substitutionVerified"])
        self.assertEqual(
            ["access-telemetry-secrets", "secretstore"],
            sorted(disclosure["substitutedComponents"]),
        )
        self.assertEqual(["secretstores.kubernetes"], disclosure["observedPostPatchTypes"])
        self.assertEqual([], disclosure["residualVaultComponents"])

    def test_no_vault_component_records_a_positive_unmodified_assertion(self) -> None:
        # The Story 31.2 end state: components exist, none of them vault-typed. This must
        # pass and must be recorded as an explicit substitutionPerformed=false, because it
        # is the state DW 27.3-CR29/CR30 have to be able to discharge.
        components = {"items": [self.component("statestore", "state.postgresql")]}
        result = self.run_substitution(pre=components, post=components)
        self.assertEqual("passed", result["outcome"], result["message"])
        disclosure = result["disclosure"]
        self.assertFalse(disclosure["substitutionPerformed"])
        self.assertTrue(disclosure["substitutionVerified"])
        self.assertEqual([], list(disclosure["substitutedComponents"]))
        self.assertEqual([], list(disclosure["observedComponents"]))

    def test_a_payload_without_items_is_not_read_as_nothing_to_substitute(self) -> None:
        # kubectl can exit 0 with a Status object. `@($payload.items)` then yields a
        # one-element array holding $null and the vault-typed set came out empty, so a
        # failed enumeration was recorded as the by-design unmodified end state.
        result = self.run_substitution(pre={"kind": "Status"}, post={"items": []})
        self.assertEqual("threw", result["outcome"], result["message"])
        self.assertIn("no 'items' property", str(result["message"]))

    def test_zero_components_is_treated_as_a_render_or_apply_regression(self) -> None:
        result = self.run_substitution(pre={"items": []}, post={"items": []})
        self.assertEqual("threw", result["outcome"], result["message"])
        self.assertIn("zero Dapr Components", str(result["message"]))

    def test_a_component_absent_from_the_readback_still_writes_its_disclosure(self) -> None:
        # Every throw between the patch loop and the disclosure write previously left a
        # rewritten cluster with no disclosure at all, and the validator skips its
        # disclosure branch entirely when the file is absent.
        pre = {"items": [self.component("secretstore", "secretstores.hashicorp.vault")]}
        post = {"items": [self.component("statestore", "state.postgresql")]}
        result = self.run_substitution(pre=pre, post=post)
        self.assertEqual("threw", result["outcome"], result["message"])
        self.assertIsNotNone(result["disclosure"])
        self.assertFalse(result["disclosure"]["substitutionVerified"])
        self.assertTrue(any(
            "absent from the post-patch read" in f
            for f in result["disclosure"]["verificationFailures"]
        ))

    def test_a_component_left_at_the_vault_type_still_writes_its_disclosure(self) -> None:
        pre = {"items": [self.component("secretstore", "secretstores.hashicorp.vault")]}
        post = {"items": [self.component("secretstore", "secretstores.hashicorp.vault")]}
        result = self.run_substitution(pre=pre, post=post)
        self.assertEqual("threw", result["outcome"], result["message"])
        disclosure = result["disclosure"]
        self.assertFalse(disclosure["substitutionVerified"])
        self.assertEqual(["secretstore"], list(disclosure["residualVaultComponents"]))

    def test_a_vault_component_missed_before_patching_is_caught_on_readback(self) -> None:
        # Informer lag right after the apply, or a controller re-creating a component
        # between the patch loop and the readback: the readback iterated only the names
        # enumerated BEFORE patching, so a surviving vault-typed store reached the health
        # stages while the disclosure claimed it named exactly what it observed.
        pre = {"items": [self.component("secretstore", "secretstores.hashicorp.vault")]}
        post = {"items": [
            self.component("secretstore", "secretstores.kubernetes"),
            self.component("late-arriving-store", "secretstores.hashicorp.vault"),
        ]}
        result = self.run_substitution(pre=pre, post=post)
        self.assertEqual("threw", result["outcome"], result["message"])
        self.assertEqual(
            ["late-arriving-store"],
            list(result["disclosure"]["residualVaultComponents"]),
        )

    def test_a_foreign_context_is_refused_before_any_patch(self) -> None:
        pre = {"items": [self.component("secretstore", "secretstores.hashicorp.vault")]}
        result = self.run_substitution(pre=pre, post=pre, context="jpiquot@local")
        self.assertEqual("threw", result["outcome"], result["message"])
        self.assertIn("Refusing to substitute secret stores", str(result["message"]))
        self.assertIsNone(result["disclosure"])

    def test_a_case_variant_context_is_refused(self) -> None:
        # kubectl context names are case-sensitive, so KIND-verify is a genuinely
        # different context. `-ne` admitted it; this is the sole barrier before the
        # script rewrites Dapr Component spec.types.
        pre = {"items": [self.component("secretstore", "secretstores.hashicorp.vault")]}
        result = self.run_substitution(pre=pre, post=pre, context="KIND-verify")
        self.assertEqual("threw", result["outcome"], result["message"])
        self.assertIn("Refusing to substitute secret stores", str(result["message"]))


class SubstitutionDisclosureRoundTripTests(unittest.TestCase):
    """Feed the verifier's REAL disclosure to the real validator.

    Every disclosure case in this suite was hand-written Python JSON built by
    write_complete_evidence, so the two producers were coupled only by human
    transcription plus two assertIn source pins. A field-name or type drift on either
    side was undetectable, which is the structural reason the substitution-block
    mutations all survived at once.
    """

    def test_a_real_succeeded_disclosure_validates(self) -> None:
        producer = SecretStoreSubstitutionExecutionTests()
        pre = {"items": [
            producer.component("secretstore", "secretstores.hashicorp.vault"),
            producer.component("access-telemetry-secrets", "secretstores.hashicorp.vault"),
        ]}
        post = {"items": [
            producer.component("secretstore", "secretstores.kubernetes"),
            producer.component("access-telemetry-secrets", "secretstores.kubernetes"),
        ]}
        produced = producer.run_substitution(pre=pre, post=post)
        self.assertEqual("passed", produced["outcome"], produced["message"])

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_complete_evidence(root)
            (root / "secret-store-substitution.json").write_text(
                json.dumps(produced["disclosure"]), encoding="utf-8"
            )
            result = run_validator(root)
            self.assertEqual(0, result.returncode, flattened_output(result))

    def test_a_real_unmodified_disclosure_validates(self) -> None:
        producer = SecretStoreSubstitutionExecutionTests()
        components = {"items": [producer.component("statestore", "state.postgresql")]}
        produced = producer.run_substitution(pre=components, post=components)
        self.assertEqual("passed", produced["outcome"], produced["message"])

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_complete_evidence(root)
            (root / "secret-store-substitution.json").write_text(
                json.dumps(produced["disclosure"]), encoding="utf-8"
            )
            result = run_validator(root)
            self.assertEqual(0, result.returncode, flattened_output(result))
            self.assertIn("substitutionPerformed=false", flattened_output(result))

    def test_a_real_failed_disclosure_is_rejected_on_a_succeeded_packet(self) -> None:
        # The verifier writes a disclosure even when verification failed. That packet is
        # legitimate on a failed run and must never validate as a succeeded one.
        producer = SecretStoreSubstitutionExecutionTests()
        pre = {"items": [producer.component("secretstore", "secretstores.hashicorp.vault")]}
        post = {"items": [producer.component("secretstore", "secretstores.hashicorp.vault")]}
        produced = producer.run_substitution(pre=pre, post=post)
        self.assertEqual("threw", produced["outcome"], produced["message"])

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_complete_evidence(root)
            (root / "secret-store-substitution.json").write_text(
                json.dumps(produced["disclosure"]), encoding="utf-8"
            )
            result = run_validator(root)
            self.assertEqual(1, result.returncode, flattened_output(result))


class SubstitutionDisclosureShapeTests(unittest.TestCase):
    """Drive the validator's disclosure contract field by field.

    The succeeded branch left schemaVersion and the substitutionPerformed type check
    unexercised (every fixture wrote 2 and a real bool), and the failed branch validated
    2 of 8 fields, so a failed packet could assert a substitution that never happened.
    """

    def validate_with(self, disclosure: dict[str, object], *, status: str = "succeeded") -> subprocess.CompletedProcess[str]:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_complete_evidence(root, status=status)
            (root / "secret-store-substitution.json").write_text(
                json.dumps(disclosure), encoding="utf-8"
            )
            return run_validator(root)

    @staticmethod
    def baseline() -> dict[str, object]:
        return {
            "schemaVersion": 2,
            "substitutionPerformed": True,
            "reason": "redacted verification-scoped substitution disclosure",
            "substitutedComponents": ["secretstore", "access-telemetry-secrets"],
            "observedComponents": [
                {"name": "secretstore", "observedType": "secretstores.kubernetes"},
                {"name": "access-telemetry-secrets", "observedType": "secretstores.kubernetes"},
            ],
            "originalType": "secretstores.hashicorp.vault",
            "substitutedType": "secretstores.kubernetes",
            "observedPostPatchTypes": ["secretstores.kubernetes"],
            "substitutionVerified": True,
            "verificationFailures": [],
            "residualVaultComponents": [],
        }

    def test_the_baseline_disclosure_validates(self) -> None:
        result = self.validate_with(self.baseline())
        self.assertEqual(0, result.returncode, flattened_output(result))

    def test_a_schema_version_1_packet_is_rejected(self) -> None:
        # The shape the pre-2026-07-30 verifier wrote, including the DW 27.3-CR17 discharge
        # artifact. No fixture ever varied this field, so disabling the check survived.
        disclosure = self.baseline()
        disclosure["schemaVersion"] = 1
        result = self.validate_with(disclosure)
        self.assertEqual(1, result.returncode)
        self.assertIn("schemaVersion", flattened_output(result))

    def test_a_string_substitution_performed_is_rejected(self) -> None:
        # "false" is TRUTHY in PowerShell, so a string here reads as a performed
        # substitution and silently takes the opposite branch.
        disclosure = self.baseline()
        disclosure["substitutionPerformed"] = "false"
        result = self.validate_with(disclosure)
        self.assertEqual(1, result.returncode)
        self.assertIn("boolean", flattened_output(result))

    def test_a_duplicated_component_name_is_rejected(self) -> None:
        # `-notcontains` on both sides is a set comparison, so claiming three
        # substitutions while observing two produced empty missing/unexpected lists.
        disclosure = self.baseline()
        disclosure["substitutedComponents"] = ["secretstore", "secretstore", "access-telemetry-secrets"]
        result = self.validate_with(disclosure)
        self.assertEqual(1, result.returncode)
        self.assertIn("more than once", flattened_output(result))

    def test_observed_post_patch_types_must_agree_with_the_per_component_record(self) -> None:
        # The verifier writes this field into every packet and nothing validated it, so a
        # packet could record every component as kubernetes while this field said vault.
        disclosure = self.baseline()
        disclosure["observedPostPatchTypes"] = ["secretstores.hashicorp.vault"]
        result = self.validate_with(disclosure)
        self.assertEqual(1, result.returncode)
        self.assertIn("observedPostPatchTypes", flattened_output(result))

    def test_a_succeeded_run_must_assert_substitution_verified(self) -> None:
        disclosure = self.baseline()
        disclosure["substitutionVerified"] = False
        result = self.validate_with(disclosure)
        self.assertEqual(1, result.returncode)
        self.assertIn("substitutionVerified=true", flattened_output(result))

    def test_a_succeeded_run_may_not_carry_residual_vault_components(self) -> None:
        disclosure = self.baseline()
        disclosure["residualVaultComponents"] = ["late-arriving-store"]
        result = self.validate_with(disclosure)
        self.assertEqual(1, result.returncode)
        self.assertIn("survived the substitution", flattened_output(result))

    def test_a_failed_run_disclosure_is_held_to_the_same_shape(self) -> None:
        # Previously the failed branch checked originalType and substitutedType only, so
        # every assertion below passed unchallenged on a failed packet.
        for field, value, expected in (
            ("schemaVersion", 99, "schemaVersion"),
            ("substitutionPerformed", "false", "boolean"),
            ("reason", "   ", "non-empty reason"),
            ("observedPostPatchTypes", ["secretstores.hashicorp.vault"], "observedPostPatchTypes"),
        ):
            with self.subTest(field=field):
                disclosure = self.baseline()
                disclosure["substitutionVerified"] = False
                disclosure[field] = value
                result = self.validate_with(disclosure, status="failed")
                self.assertEqual(1, result.returncode, flattened_output(result))
                self.assertIn(expected, flattened_output(result))

    def test_a_failed_run_may_legitimately_record_an_unverified_substitution(self) -> None:
        disclosure = self.baseline()
        disclosure["substitutionVerified"] = False
        disclosure["verificationFailures"] = ["component 'secretstore' observed post-patch type 'secretstores.hashicorp.vault'"]
        disclosure["residualVaultComponents"] = ["secretstore"]
        result = self.validate_with(disclosure, status="failed")
        self.assertEqual(0, result.returncode, flattened_output(result))


class StartupBudgetContractTests(unittest.TestCase):
    """The 60-second startup contract must be the enforced one.

    The credit was bounded at $TimeoutSeconds rather than the RESULT being bounded, so
    the effective ceiling reached 2 x $TimeoutSeconds. Neither branch below was driven by
    the existing class: all five of its cases use a non-null readyAt or the failure path.
    """

    def drive(self, polls: list[dict[str, object]], timeout_seconds: int = 60) -> dict[str, object]:
        return StartupBudgetExecutionTests.drive(self, polls, timeout_seconds)

    def test_a_119_second_start_fails_the_60_second_contract(self) -> None:
        # Administrator decision 2026-07-31. Previously passed: $healthyAt fell back to the
        # runner clock, the credit was capped at the budget itself, and 119 - 60 <= 60.
        result = self.drive(
            [{"started_ago": 119, "ready_ago": None, "status": "Healthy", "code": 200, "fallback": 5000}]
        )
        self.assertEqual("threw", result["outcome"], result["message"])
        self.assertIn("beyond the 60-second startup limit", str(result["message"]))

    def test_accrued_capture_is_reported_but_not_credited(self) -> None:
        result = self.drive(
            [{"started_ago": 119, "ready_ago": None, "status": "Healthy", "code": 200, "fallback": 40}]
        )
        self.assertEqual("threw", result["outcome"], result["message"])
        self.assertIn("not credited", str(result["message"]))

    def test_a_fast_start_without_a_recorded_ready_transition_still_passes(self) -> None:
        result = self.drive(
            [{"started_ago": 10, "ready_ago": None, "status": "Healthy", "code": 200, "fallback": 3}]
        )
        self.assertEqual("passed", result["outcome"], result["message"])

    def test_a_ready_condition_older_than_the_container_is_not_used(self) -> None:
        # A container restarting inside the pod while the pod-level Ready condition still
        # carried its predecessor's lastTransitionTime gave $readyAt < $runningAt, a
        # NEGATIVE interval that could never breach the budget.
        result = self.drive(
            [{"started_ago": 200, "ready_ago": 250, "status": "Healthy", "code": 200, "fallback": 0}]
        )
        self.assertEqual("threw", result["outcome"], result["message"])
        self.assertIn("beyond the 60-second startup limit", str(result["message"]))

    def test_a_ready_condition_inside_the_current_instance_still_passes(self) -> None:
        result = self.drive(
            [{"started_ago": 200, "ready_ago": 160, "status": "Healthy", "code": 200, "fallback": 0}]
        )
        self.assertEqual("passed", result["outcome"], result["message"])


class CapturedProcessTextExecutionTests(unittest.TestCase):
    """Execute Get-CapturedProcessText's three paths.

    The `<unreadable: ...>` marker had no execution test and no source pin, so replacing
    it with '' reinstated the exact defect it was added to fix: a port-forward bind or
    upgrade error silently dropped and indistinguishable from "no stderr".
    """

    def read(self, *, exists: bool = True, content: str = "", unreadable: bool = False) -> str:
        verifier = VERIFIER.read_text(encoding="utf-8-sig")
        function = extract_ps_function(verifier, "Get-CapturedProcessText")
        with tempfile.TemporaryDirectory() as tmp:
            target = Path(tmp) / "capture.err"
            if exists:
                target.write_text(content, encoding="utf-8")
            path_literal = shlex.quote(str(target))
            if unreadable:
                # Force the read to fail while the file exists, the way a still-running
                # kubectl port-forward holding the handle does on the CI runner.
                script = (
                    f"{function}\n"
                    "function Get-Content { param([string]$LiteralPath, [switch]$Raw, $ErrorAction, [string]$ErrorVariable)\n"
                    "  Set-Variable -Name $ErrorVariable -Scope 1 -Value @([pscustomobject]@{ Exception = [pscustomobject]@{ Message = 'file in use' } })\n"
                    "  return $null }\n"
                    f"Write-Output ('RESULT=' + (Get-CapturedProcessText {path_literal}))\n"
                )
            else:
                script = f"{function}\nWrite-Output ('RESULT=' + (Get-CapturedProcessText {path_literal}))\n"
            result = run_pwsh(script)
            self.assertEqual(0, result.returncode, result.stderr)
            for line in result.stdout.splitlines():
                if line.startswith("RESULT="):
                    return line.split("=", 1)[1].strip()
            return ""

    def test_missing_file_yields_empty(self) -> None:
        self.assertEqual("", self.read(exists=False))

    def test_present_file_is_trimmed(self) -> None:
        self.assertEqual("bind failed", self.read(content="  bind failed \n"))

    def test_unreadable_file_yields_a_marker_rather_than_silence(self) -> None:
        self.assertIn("<unreadable:", self.read(unreadable=True))


if __name__ == "__main__":
    unittest.main()
