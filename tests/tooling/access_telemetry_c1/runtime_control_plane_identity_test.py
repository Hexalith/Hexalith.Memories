import copy
import hashlib
import json
import os
import re
import stat
import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
RUNNER = REPO_ROOT / "tools" / "verify-access-telemetry-c1.ps1"
FIXTURE = Path(__file__).parent / "fixtures" / "c1_15_complete.json"
STORY = REPO_ROOT / "_bmad-output" / "implementation-artifacts" / "27-21-runtime-and-control-plane-identity.md"
EXECUTION_SPEC = REPO_ROOT / "_bmad-output" / "implementation-artifacts" / "spec-27-21-runtime-control-plane-identity-6.md"
HISTORICAL_HANDOFF = REPO_ROOT / "_bmad-output" / "implementation-artifacts" / "spec-27-21-runtime-control-plane-identity.md"
SPRINT_STATUS = REPO_ROOT / "_bmad-output" / "implementation-artifacts" / "sprint-status.yaml"
DEFERRED_WORK = REPO_ROOT / "_bmad-output" / "implementation-artifacts" / "deferred-work.md"
EPIC_CONTEXT = REPO_ROOT / "_bmad-output" / "implementation-artifacts" / "epic-27-context.md"
BASE_KUSTOMIZATION = REPO_ROOT / "deploy" / "kubernetes" / "base" / "kustomization.yaml"
LIFECYCLE_DEPLOYMENTS = REPO_ROOT / "deploy" / "kubernetes" / "base" / "access-telemetry-deployments.yaml"
PRODUCTION_KUSTOMIZATION = REPO_ROOT / "deploy" / "kubernetes" / "overlays" / "production" / "kustomization.yaml"
PRODUCTION_DISABLED_PATCH = (
    REPO_ROOT / "deploy" / "kubernetes" / "overlays" / "production" / "access-telemetry-disabled-patch.yaml"
)
TOKEN_CANARY = "C1_SECRET_CANARY_DO_NOT_EMIT_7429"
TARGET_SELECTOR = "app.kubernetes.io/name=memories-access-telemetry"
POD_IDENTITY_OUTPUT = "jsonpath-as-json={range .items[*]}{['metadata','status']}{end}"
OBSERVED_PACKET_PATH = (
    "artifacts/access-telemetry-c1/C1.15/"
    "c1.15-runtime-control-plane-identity-20261004T104156676Z-af39ddb5939044f38e438e6e584c3268.json"
)
OBSERVED_PACKET_SHA256 = "17d7f350c3193ce6663364b0b4e6ef52d8f319f4ca4c0a25e9886a006ff0ed87"
INDEPENDENT_REVIEW_PATH = (
    "artifacts/access-telemetry-c1/C1.15/"
    "c1.15-independent-framed-packet-review-20261004T104839838701Z-1b6be26b1d604a50bc53eefcc2203278.json"
)
INDEPENDENT_REVIEW_SHA256 = "8a70077297e68962f12da991d76c37a58a2faa86697ea0e2212f590941e62c73"
LIFECYCLE_DEPLOYMENT_NAMES = (
    "memories-access-telemetry",
    "memories-access-telemetry-clock",
)


def write_executable(path: Path, content: str) -> None:
    path.write_text(content, encoding="utf-8")
    path.chmod(0o755)


def write_fake_kubectl(directory: Path) -> None:
    fake = directory / "fake_kubectl.py"
    fake.write_text(
        textwrap.dedent(
            """
            import json
            import os
            import sys
            import time
            from pathlib import Path

            args = sys.argv[1:]
            scenario = json.loads(Path(os.environ["C1_SCENARIO"]).read_text(encoding="utf-8"))
            log_path = Path(os.environ["C1_KUBECTL_LOG"])
            prior_calls = []
            if log_path.exists():
                prior_calls = [json.loads(line) for line in log_path.read_text(encoding="utf-8").splitlines()]
            with log_path.open("a", encoding="utf-8") as log:
                log.write(json.dumps(args) + "\\n")

            if args == ["config", "current-context"]:
                if scenario.get("hangPurpose") == "current-context":
                    time.sleep(scenario.get("hangSeconds", 5))
                print(scenario["context"])
                raise SystemExit(0)

            if args[:4] != ["--context", "jpiquot@local", "-n", "hexalith-memories"]:
                print("unexpected target identity", file=sys.stderr)
                raise SystemExit(99)

            if "get" in args and "pods" in args:
                selector_index = args.index("-l") if "-l" in args else -1
                if selector_index < 0 or args[selector_index + 1] != "app.kubernetes.io/name=memories-access-telemetry":
                    print("unexpected selector", file=sys.stderr)
                    raise SystemExit(98)
                prior_pod_gets = [call for call in prior_calls if "get" in call and "pods" in call]
                raw_key = "podsAfterRaw" if prior_pod_gets else "podsRaw"
                if raw_key in scenario:
                    print(scenario[raw_key])
                else:
                    pods = scenario.get("podsAfter", scenario["pods"]) if prior_pod_gets else scenario["pods"]
                    output = args[args.index("-o") + 1] if "-o" in args else ""
                    if output == "json":
                        print(json.dumps(pods))
                    elif output == "jsonpath-as-json={range .items[*]}{['metadata','status']}{end}":
                        if not isinstance(pods, dict) or not isinstance(pods.get("items"), list):
                            print(json.dumps(pods))
                        else:
                            for pod in pods["items"]:
                                frame = [pod[field] for field in ("metadata", "status") if field in pod]
                                print(json.dumps(frame))
                    else:
                        print("unsupported pod output format", file=sys.stderr)
                        raise SystemExit(93)
                raise SystemExit(0)

            if "exec" not in args:
                print("unsupported kubectl invocation", file=sys.stderr)
                raise SystemExit(97)

            pod = args[args.index("exec") + 1]
            container = args[args.index("-c") + 1]
            shell_text = args[-1]
            if container == "daprd" and args[-2:] == ["/daprd", "--version"]:
                print(scenario["daprdVersions"][pod])
                raise SystemExit(0)
            if container != "lifecycle":
                print("forbidden non-lifecycle container", file=sys.stderr)
                raise SystemExit(96)
            if "/v1.0/metadata" in shell_text:
                expected_probe = 'if [ -z "${DAPR_API_TOKEN:-}" ]; then echo "required runtime credential unavailable" >&2; exit 72; fi; metadata="$(wget -qO- --timeout=5 --header="dapr-api-token: ${DAPR_API_TOKEN}" http://127.0.0.1:3500/v1.0/metadata)" || exit $?; case "$metadata" in *"$DAPR_API_TOKEN"*) echo "secret-shaped-output" >&2; exit 73;; esac; printf "%s" "$metadata"'
                if shell_text != expected_probe:
                    print("metadata authentication missing", file=sys.stderr)
                    raise SystemExit(94)
                if not scenario.get("metadataTokenAvailable", True):
                    print("required runtime credential unavailable", file=sys.stderr)
                    raise SystemExit(72)
                raw = scenario.get("metadataRaw", {}).get(pod)
                if raw is not None:
                    print(raw)
                else:
                    print(json.dumps(scenario["metadata"][pod]))
                raise SystemExit(0)
            if (
                shell_text.startswith('printf ') and
                shell_text.count("AccessTelemetryLifecycle__ComponentIsAlpha") == 1 and
                shell_text.count("AccessTelemetryLifecycle__AllowAlphaComponent") == 1
            ):
                values = scenario["alphaOptIn"][pod]
                print(values[0])
                print(values[1])
                raise SystemExit(0)

            print("unsupported exec probe", file=sys.stderr)
            raise SystemExit(95)
            """
        ).strip()
        + "\n",
        encoding="utf-8",
    )
    write_executable(
        directory / "kubectl",
        f'#!/usr/bin/env sh\nexec "{sys.executable}" "{fake}" "$@"\n',
    )


class RuntimeControlPlaneIdentityTests(unittest.TestCase):
    maxDiff = None

    def setUp(self) -> None:
        self.base_scenario = json.loads(FIXTURE.read_text(encoding="utf-8"))

    def add_second_pod(self, scenario: dict) -> tuple[str, str]:
        first_pod = scenario["pods"]["items"][0]
        first_name = first_pod["metadata"]["name"]
        second_name = "memories-access-telemetry-7cc55d9fd8-second"
        second_pod = copy.deepcopy(first_pod)
        second_pod["metadata"]["name"] = second_name
        second_pod["metadata"]["uid"] = "b5720433-c832-4b36-a698-862dbde85641"
        scenario["pods"]["items"].append(second_pod)
        scenario["daprdVersions"][second_name] = scenario["daprdVersions"][first_name]
        scenario["metadata"][second_name] = copy.deepcopy(scenario["metadata"][first_name])
        scenario["alphaOptIn"][second_name] = copy.deepcopy(scenario["alphaOptIn"][first_name])
        return first_name, second_name

    def run_gate(
        self,
        scenario: dict,
        *,
        gate: str = "C1.15",
        profile_id: str = "PG-ONPREM-1",
        evidence_directory: bool = True,
        repeat: int = 1,
        command_timeout_seconds: int = 30,
    ) -> tuple[subprocess.CompletedProcess[str], list[dict], list[list[str]], Path]:
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        root = Path(temp.name)
        fake_bin = root / "bin"
        fake_bin.mkdir()
        write_fake_kubectl(fake_bin)
        scenario_path = root / "scenario.json"
        scenario_path.write_text(json.dumps(scenario), encoding="utf-8")
        log_path = root / "kubectl.jsonl"
        evidence = root / "evidence"

        env = os.environ.copy()
        env["PATH"] = str(fake_bin) + os.pathsep + env.get("PATH", "")
        env["C1_SCENARIO"] = str(scenario_path)
        env["C1_KUBECTL_LOG"] = str(log_path)
        env["DAPR_API_TOKEN"] = TOKEN_CANARY
        command = [
            "pwsh",
            str(RUNNER),
            "-Gate",
            gate,
            "-ProfileId",
            profile_id,
            "-EvidenceDirectory",
            str(evidence),
            "-CommandTimeoutSeconds",
            str(command_timeout_seconds),
        ]
        result = None
        for _ in range(repeat):
            result = subprocess.run(
                command,
                cwd=REPO_ROOT,
                env=env,
                text=True,
                capture_output=True,
                check=False,
                timeout=max(command_timeout_seconds + 5, 10),
            )
            if result.returncode != 0:
                break
        assert result is not None
        packets = []
        if evidence_directory and evidence.exists():
            packets = [json.loads(path.read_text(encoding="utf-8")) for path in sorted(evidence.glob("*.json"))]
        calls = []
        if log_path.exists():
            calls = [json.loads(line) for line in log_path.read_text(encoding="utf-8").splitlines()]
        return result, packets, calls, evidence

    def test_complete_fixture_emits_all_c1_15_observations_without_passing_gate(self) -> None:
        result, packets, calls, evidence = self.run_gate(self.base_scenario, repeat=2)

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(2, len(packets))
        packet = packets[-1]
        self.assertEqual("C1.15", packet["gate"])
        self.assertEqual("PG-ONPREM-1", packet["profileId"])
        self.assertEqual("jpiquot@local", packet["context"])
        self.assertEqual("hexalith-memories", packet["namespace"])
        self.assertEqual("observed", packet["producerStatus"])
        self.assertEqual("not-evaluated", packet["gateStatus"])
        self.assertFalse(packet["productionGatePassed"])
        self.assertEqual("not-evaluated", packet["productionLifecycleWrites"])
        observation = packet["observations"]
        self.assertEqual(["1.18.1"], observation["runtimeVersions"])
        self.assertEqual(
            ["sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8"],
            observation["sidecarImageDigests"],
        )
        self.assertEqual(["memories-access-telemetry"], observation["appIds"])
        self.assertEqual(3, len(observation["schedulerConnectedAddresses"]))
        self.assertEqual(["AccessTelemetryLifecycleActor"], observation["actorTypes"])
        self.assertEqual(["Actor.Reentrancy"], observation["enabledFeatures"])
        self.assertEqual(
            {"componentIsAlpha": False, "allowAlphaComponent": False},
            observation["alphaOptIn"],
        )
        serialized = json.dumps(packet)
        self.assertNotIn(TOKEN_CANARY, serialized)
        self.assertNotIn(TOKEN_CANARY, result.stdout + result.stderr)
        self.assertTrue(packet["sources"])
        self.assertTrue(packet["commands"])
        for entry in packet["sources"] + packet["commands"]:
            self.assertRegex(entry["sha256"], r"^[0-9a-f]{64}$")
        self.assertTrue(all(TARGET_SELECTOR in call for call in calls if "get" in call))
        self.assertTrue(
            all(
                call[:4] == ["--context", "jpiquot@local", "-n", "hexalith-memories"]
                for call in calls
                if call != ["config", "current-context"]
            )
        )
        self.assertNotIn(TOKEN_CANARY, json.dumps(calls))

        sources = {entry["source"]: entry["sha256"] for entry in packet["sources"]}
        self.assertEqual(hashlib.sha256(RUNNER.read_bytes()).hexdigest(), sources[str(RUNNER.relative_to(REPO_ROOT))])
        packet_calls = calls[-len(packet["commands"]) :]
        self.assertEqual(len(packet_calls), len(packet["commands"]))
        for ledger_entry, call in zip(packet["commands"], packet_calls, strict=True):
            command_identity = "kubectl " + "\x1f".join(call)
            self.assertEqual(
                hashlib.sha256(command_identity.encode("utf-8")).hexdigest(),
                ledger_entry["sha256"],
            )

        pod_name = self.base_scenario["pods"]["items"][0]["metadata"]["name"]
        metadata = self.base_scenario["metadata"][pod_name]
        allowlisted = {
            "id": metadata["id"],
            "runtimeVersion": metadata["runtimeVersion"],
            "schedulerConnectedAddresses": metadata["scheduler"]["connectedAddresses"],
            "actorTypes": [actor["type"] for actor in metadata["actors"]],
            "enabledFeatures": metadata["enabledFeatures"],
        }
        allowlisted_json = json.dumps(allowlisted, separators=(",", ":"))
        self.assertEqual(
            hashlib.sha256(allowlisted_json.encode("utf-8")).hexdigest(),
            sources[f"kubectl:metadata:{pod_name}:allowlisted"],
        )

        packet_paths = sorted(evidence.glob("*.json"))
        self.assertEqual(2, len({path.name for path in packet_paths}))
        self.assertTrue(all(path.stat().st_size > 0 for path in packet_paths))
        self.assertTrue(
            all(
                path.stat().st_mode & (stat.S_IWUSR | stat.S_IWGRP | stat.S_IWOTH) == 0
                for path in packet_paths
            )
        )

    def test_no_running_lifecycle_pod_blocks_without_server_fallback(self) -> None:
        scenario = copy.deepcopy(self.base_scenario)
        scenario["pods"]["items"][0]["status"]["phase"] = "Pending"

        result, packets, calls, _ = self.run_gate(scenario)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(1, len(packets))
        self.assertEqual("blocked", packets[0]["producerStatus"])
        self.assertEqual("not-evaluated", packets[0]["gateStatus"])
        self.assertIn("no-running-lifecycle-pod", packets[0]["blockers"])
        self.assertFalse(any("exec" in call for call in calls))
        pod_gets = [call for call in calls if "get" in call and "pods" in call]
        self.assertEqual(1, len(pod_gets))
        selector_index = pod_gets[0].index("-l")
        self.assertEqual(TARGET_SELECTOR, pod_gets[0][selector_index + 1])

    def test_pod_probe_templates_and_environments_are_excluded_before_scanning_and_hashing(self) -> None:
        baseline_result, baseline_packets, _, _ = self.run_gate(self.base_scenario)
        self.assertEqual(0, baseline_result.returncode, baseline_result.stderr)
        scenario = copy.deepcopy(self.base_scenario)
        scenario["pods"]["items"][0]["spec"] = {
            "containers": [{
                "name": "lifecycle",
                "env": [{"name": "DAPR_API_TOKEN", "value": TOKEN_CANARY}],
                "readinessProbe": {"exec": {"command": [
                    "/bin/sh", "-ec",
                    'wget -qO- --header="dapr-api-token: ${APP_API_TOKEN}" http://127.0.0.1:8080/ready >/dev/null',
                ]}},
            }],
        }
        scenario["podsAfter"] = copy.deepcopy(scenario["pods"])
        scenario["podsAfter"]["items"][0]["spec"]["diagnostic"] = "authorization: Bearer-discarded-value"

        result, packets, calls, _ = self.run_gate(scenario)

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("observed", packets[0]["producerStatus"])
        self.assertEqual(baseline_packets[0]["observations"], packets[0]["observations"])
        self.assertEqual(baseline_packets[0]["sources"], packets[0]["sources"])
        self.assertEqual(baseline_packets[0]["commands"], packets[0]["commands"])
        pod_gets = [call for call in calls if "get" in call and "pods" in call]
        self.assertEqual(2, len(pod_gets))
        self.assertTrue(all(call[call.index("-o") + 1] == POD_IDENTITY_OUTPUT for call in pod_gets))
        serialized = json.dumps(packets) + result.stdout + result.stderr + json.dumps(calls)
        for discarded in (TOKEN_CANARY, "Bearer-discarded-value", "APP_API_TOKEN", "readinessProbe"):
            self.assertNotIn(discarded, serialized)
        full_pod_hashes = {
            hashlib.sha256((json.dumps(scenario[key]) + "\n").encode("utf-8")).hexdigest()
            for key in ("pods", "podsAfter")
        }
        self.assertTrue(full_pod_hashes.isdisjoint(entry["sha256"] for entry in packets[0]["sources"]))

    def test_secrets_in_selected_pod_metadata_and_status_still_block_without_provenance(self) -> None:
        for phase in ("initial", "recheck"):
            for field in ("metadata", "status"):
                for encoding in ("literal", "escaped"):
                    with self.subTest(phase=phase, field=field, encoding=encoding):
                        scenario = copy.deepcopy(self.base_scenario)
                        key = "pods"
                        if phase == "recheck":
                            key = "podsAfter"
                            scenario[key] = copy.deepcopy(scenario["pods"])
                        scenario[key]["items"][0][field]["diagnostic"] = TOKEN_CANARY
                        projection = [
                            scenario[key]["items"][0]["metadata"],
                            scenario[key]["items"][0]["status"],
                        ]
                        raw_projection = json.dumps(projection)
                        if encoding == "escaped":
                            encoded_secret = "".join(f"\\u{ord(character):04x}" for character in TOKEN_CANARY)
                            raw_projection = raw_projection.replace(TOKEN_CANARY, encoded_secret)
                            scenario["podsAfterRaw" if phase == "recheck" else "podsRaw"] = raw_projection

                        result, packets, calls, _ = self.run_gate(scenario)

                        self.assertNotEqual(0, result.returncode)
                        self.assertEqual(["secret-shaped-output"], packets[0]["blockers"])
                        self.assertEqual("blocked", packets[0]["producerStatus"])
                        self.assertEqual([], packets[0]["observations"]["pods"])
                        self.assertEqual("not-evaluated", packets[0]["gateStatus"])
                        self.assertFalse(packets[0]["productionGatePassed"])
                        self.assertNotIn(TOKEN_CANARY, json.dumps(packets) + result.stdout + result.stderr + json.dumps(calls))
                        if phase == "initial":
                            self.assertFalse(any("exec" in call for call in calls))
                        for rejected in (raw_projection, raw_projection + "\n"):
                            rejected_hash = hashlib.sha256(rejected.encode("utf-8")).hexdigest()
                            self.assertNotIn(rejected_hash, [entry["sha256"] for entry in packets[0]["sources"]])

    def test_malformed_pod_identity_projection_pairs_and_types_fail_closed(self) -> None:
        pod = self.base_scenario["pods"]["items"][0]
        metadata, status = pod["metadata"], pod["status"]
        cases = {
            "object-root": {"items": [pod]},
            "null-root": None,
            "scalar-root": "ordinary",
            "odd-pair": [metadata],
            "extra-entry": [metadata, status, metadata],
            "null-metadata": [None, status],
            "scalar-metadata": [7, status],
            "array-metadata": [[], status],
            "null-status": [metadata, None],
            "boolean-status": [metadata, True],
            "string-status": [metadata, "Running"],
            "array-status": [metadata, []],
            "empty-selected-frame": [],
            "identityless-metadata": [{"annotations": {}}, status],
            "metadata-in-status-slot": [metadata, metadata],
        }
        raw_cases = {name: json.dumps(projection) for name, projection in cases.items()}
        valid_frame = json.dumps([metadata, status])
        raw_cases.update({
            "malformed-first-frame": "{not-json",
            "trailing-non-json": valid_frame + "\nordinary",
            "malformed-second-frame": valid_frame + "\n[",
            "empty-second-frame": valid_frame + "\n[]",
            "one-field-second-frame": valid_frame + "\n" + json.dumps([metadata]),
            "flattened-two-pod-union": json.dumps([metadata, metadata, status, status]),
        })
        for phase in ("initial", "recheck"):
            for name, raw_projection in raw_cases.items():
                with self.subTest(phase=phase, name=name):
                    scenario = copy.deepcopy(self.base_scenario)
                    scenario["podsAfterRaw" if phase == "recheck" else "podsRaw"] = raw_projection
                    result, packets, calls, _ = self.run_gate(scenario)
                    self.assertNotEqual(0, result.returncode)
                    self.assertEqual(["malformed-pod-list-json"], packets[0]["blockers"])
                    self.assertEqual([], packets[0]["observations"]["pods"])
                    self.assertEqual("not-evaluated", packets[0]["gateStatus"])
                    self.assertFalse(packets[0]["productionGatePassed"])
                    rejected_source = (
                        "kubectl:lifecycle-pods-recheck:identity" if phase == "recheck"
                        else "kubectl:lifecycle-pods:identity"
                    )
                    self.assertFalse(any(entry["source"] == rejected_source for entry in packets[0]["sources"]))
                    if phase == "initial":
                        self.assertFalse(any("exec" in call for call in calls))
            with self.subTest(phase=phase, name="empty-sequence"):
                scenario = copy.deepcopy(self.base_scenario)
                scenario["podsAfterRaw" if phase == "recheck" else "podsRaw"] = ""
                result, packets, calls, _ = self.run_gate(scenario)
                self.assertNotEqual(0, result.returncode)
                self.assertEqual(
                    ["running-pod-changed" if phase == "recheck" else "no-running-lifecycle-pod"],
                    packets[0]["blockers"],
                )
                self.assertEqual([], packets[0]["observations"]["pods"])
                if phase == "initial":
                    self.assertFalse(any("exec" in call for call in calls))

    def test_partial_or_wrong_identity_observation_blocks_fail_closed(self) -> None:
        cases = {}
        missing_scheduler = copy.deepcopy(self.base_scenario)
        pod = next(iter(missing_scheduler["metadata"]))
        missing_scheduler["metadata"][pod]["scheduler"]["connectedAddresses"] = []
        cases["missing-scheduler"] = missing_scheduler
        wrong_app = copy.deepcopy(self.base_scenario)
        wrong_app["metadata"][pod]["id"] = "memories"
        cases["wrong-app-id"] = wrong_app
        missing_alpha = copy.deepcopy(self.base_scenario)
        missing_alpha["alphaOptIn"][pod][0] = "__MISSING__"
        cases["missing-alpha"] = missing_alpha
        null_features = copy.deepcopy(self.base_scenario)
        null_features["metadata"][pod]["enabledFeatures"] = None
        cases["null-features"] = null_features
        invalid_feature_member = copy.deepcopy(self.base_scenario)
        invalid_feature_member["metadata"][pod]["enabledFeatures"].append(None)
        cases["invalid-feature-member"] = invalid_feature_member
        invalid_version = copy.deepcopy(self.base_scenario)
        invalid_version["daprdVersions"][pod] = "usage text"
        invalid_version["metadata"][pod]["runtimeVersion"] = "usage text"
        cases["invalid-version"] = invalid_version
        invalid_scheduler_port = copy.deepcopy(self.base_scenario)
        invalid_scheduler_port["metadata"][pod]["scheduler"]["connectedAddresses"] = ["scheduler.local:99999"]
        cases["invalid-scheduler-port"] = invalid_scheduler_port
        ambiguous_scheduler = copy.deepcopy(self.base_scenario)
        ambiguous_scheduler["metadata"][pod]["scheduler"]["connected_addresses"] = ["other.local:50006"]
        cases["ambiguous-scheduler-alias"] = ambiguous_scheduler

        for name, scenario in cases.items():
            with self.subTest(name=name):
                result, packets, calls, _ = self.run_gate(scenario)
                self.assertNotEqual(0, result.returncode)
                self.assertEqual(1, len(packets))
                self.assertEqual("blocked", packets[0]["producerStatus"])
                self.assertEqual("not-evaluated", packets[0]["gateStatus"])
                self.assertFalse(packets[0]["productionGatePassed"])
                pod_gets = [call for call in calls if "get" in call and "pods" in call]
                self.assertTrue(pod_gets)
                self.assertEqual(TARGET_SELECTOR, pod_gets[0][pod_gets[0].index("-l") + 1])

    def test_two_pods_are_recorded_and_all_identity_drift_blocks(self) -> None:
        complete = copy.deepcopy(self.base_scenario)
        first_name, second_name = self.add_second_pod(complete)

        result, packets, _, _ = self.run_gate(complete)

        self.assertEqual(0, result.returncode, result.stderr)
        emitted_identities = [
            (pod["pod"], pod["podUid"])
            for pod in packets[0]["observations"]["pods"]
        ]
        self.assertEqual(
            [
                (first_name, "7e36eb30-17d5-48de-9c67-f9c6b95430ce"),
                (second_name, "b5720433-c832-4b36-a698-862dbde85641"),
            ],
            emitted_identities,
        )

        cases = {}
        runtime = copy.deepcopy(self.base_scenario)
        _, pod = self.add_second_pod(runtime)
        runtime["daprdVersions"][pod] = "1.18.2"
        runtime["metadata"][pod]["runtimeVersion"] = "1.18.2"
        cases["runtime"] = runtime
        digest = copy.deepcopy(self.base_scenario)
        _, pod = self.add_second_pod(digest)
        statuses = digest["pods"]["items"][1]["status"]["containerStatuses"]
        next(status for status in statuses if status["name"] == "daprd")["imageID"] = (
            "docker-pullable://ghcr.io/dapr/daprd@sha256:" + "c" * 64
        )
        cases["digest"] = digest
        scheduler = copy.deepcopy(self.base_scenario)
        _, pod = self.add_second_pod(scheduler)
        scheduler["metadata"][pod]["scheduler"]["connectedAddresses"].append(
            "dapr-scheduler-server-3.dapr-system.svc.cluster.local:50006"
        )
        cases["scheduler"] = scheduler
        actor = copy.deepcopy(self.base_scenario)
        _, pod = self.add_second_pod(actor)
        actor["metadata"][pod]["actors"].append({"type": "UnexpectedActor", "count": 1})
        cases["actor"] = actor
        feature = copy.deepcopy(self.base_scenario)
        _, pod = self.add_second_pod(feature)
        feature["metadata"][pod]["enabledFeatures"].append("SchedulerReminders")
        cases["feature"] = feature
        case_sensitive_feature = copy.deepcopy(self.base_scenario)
        first, pod = self.add_second_pod(case_sensitive_feature)
        case_sensitive_feature["metadata"][first]["enabledFeatures"] = ["SchedulerReminders"]
        case_sensitive_feature["metadata"][pod]["enabledFeatures"] = ["schedulerReminders"]
        cases["case-sensitive-feature"] = case_sensitive_feature
        case_sensitive_actor = copy.deepcopy(self.base_scenario)
        first, pod = self.add_second_pod(case_sensitive_actor)
        case_sensitive_actor["metadata"][first]["actors"].append({"type": "ExtraActor", "count": 1})
        case_sensitive_actor["metadata"][pod]["actors"].append({"type": "extraActor", "count": 1})
        cases["case-sensitive-actor"] = case_sensitive_actor
        alpha = copy.deepcopy(self.base_scenario)
        _, pod = self.add_second_pod(alpha)
        alpha["alphaOptIn"][pod] = ["true", "true"]
        cases["alpha"] = alpha

        for name, scenario in cases.items():
            with self.subTest(name=name):
                result, packets, _, _ = self.run_gate(scenario)
                self.assertNotEqual(0, result.returncode)
                self.assertEqual(["running-target-identity-drift"], packets[0]["blockers"])
                self.assertEqual("not-evaluated", packets[0]["gateStatus"])

    def test_framed_multi_pod_projection_preserves_identity_and_phase_provenance(self) -> None:
        scenario = copy.deepcopy(self.base_scenario)
        first_name, second_name = self.add_second_pod(scenario)
        first, second = scenario["pods"]["items"]
        first["metadata"]["annotations"] = {"capture-ordinal": "first"}
        second["metadata"]["annotations"] = {"capture-ordinal": "second"}
        first["status"]["message"] = "first stable observation"
        second["status"]["message"] = "second stable observation"
        common_digest = "sha256:" + "b" * 64
        expected_images = {
            first_name: "docker-pullable://ghcr.io/dapr/daprd@" + common_digest,
            second_name: "ghcr.io/dapr/daprd@" + common_digest,
        }
        for pod in (first, second):
            sidecar = next(status for status in pod["status"]["containerStatuses"] if status["name"] == "daprd")
            sidecar["imageID"] = expected_images[pod["metadata"]["name"]]
        scenario["podsAfter"] = copy.deepcopy(scenario["pods"])
        scenario["podsAfter"]["items"].reverse()
        second_after, first_after = scenario["podsAfter"]["items"]
        first_after["status"]["message"] = "first stable recheck"
        second_after["status"]["message"] = "second stable recheck"

        result, packets, calls, _ = self.run_gate(scenario)

        self.assertEqual(0, result.returncode, result.stderr)
        packet = packets[0]
        self.assertEqual("observed", packet["producerStatus"])
        self.assertEqual(
            [(first_name, first["metadata"]["uid"]), (second_name, second["metadata"]["uid"])],
            [(pod["pod"], pod["podUid"]) for pod in packet["observations"]["pods"]],
        )
        for observed in packet["observations"]["pods"]:
            metadata = scenario["metadata"][observed["pod"]]
            self.assertEqual(metadata["id"], observed["appId"])
            self.assertEqual(metadata["runtimeVersion"], observed["runtimeVersion"])
            self.assertEqual(metadata["enabledFeatures"], observed["enabledFeatures"])
            self.assertEqual(expected_images[observed["pod"]], observed["sidecarImageId"])
            self.assertEqual(common_digest, observed["sidecarImageDigest"])
        pod_gets = [call for call in calls if "get" in call and "pods" in call]
        self.assertEqual(2, len(pod_gets))
        self.assertTrue(all(call[call.index("-o") + 1] == POD_IDENTITY_OUTPUT for call in pod_gets))
        self.assertEqual(6, len([call for call in calls if "exec" in call]))

        # Derive exact source bytes independently of the fake and collector parser.
        # Real kubectl range emits one pair array per pod, separated by newlines.
        discovery_json = json.dumps([first["metadata"], first["status"]]) + "\n" + json.dumps([
            second["metadata"], second["status"],
        ])
        recheck_json = json.dumps([second_after["metadata"], second_after["status"]]) + "\n" + json.dumps([
            first_after["metadata"], first_after["status"],
        ])
        discovery_hash = hashlib.sha256(discovery_json.encode("utf-8")).hexdigest()
        recheck_hash = hashlib.sha256(recheck_json.encode("utf-8")).hexdigest()
        self.assertNotEqual(discovery_hash, recheck_hash)
        for source, expected_hash in (
            ("kubectl:lifecycle-pods:identity", discovery_hash),
            ("kubectl:lifecycle-pods-recheck:identity", recheck_hash),
        ):
            self.assertEqual(
                [{"source": source, "sha256": expected_hash}],
                [entry for entry in packet["sources"] if entry["source"] == source],
            )
        self.assertEqual("not-evaluated", packet["gateStatus"])
        self.assertFalse(packet["productionGatePassed"])
        self.assertEqual("not-evaluated", packet["productionLifecycleWrites"])

    def test_framed_running_and_pending_pods_preserve_running_identity_after_reordering(self) -> None:
        scenario = copy.deepcopy(self.base_scenario)
        running_name, pending_name = self.add_second_pod(scenario)
        running, pending = scenario["pods"]["items"]
        pending["status"]["phase"] = "Pending"
        scenario["podsAfter"] = copy.deepcopy(scenario["pods"])
        scenario["podsAfter"]["items"].reverse()

        result, packets, calls, _ = self.run_gate(scenario)

        self.assertEqual(0, result.returncode, result.stderr)
        packet = packets[0]
        self.assertEqual("observed", packet["producerStatus"])
        self.assertEqual(
            [(running_name, running["metadata"]["uid"])],
            [(pod["pod"], pod["podUid"]) for pod in packet["observations"]["pods"]],
        )
        exec_pods = [call[call.index("exec") + 1] for call in calls if "exec" in call]
        self.assertEqual([running_name] * 3, exec_pods)
        self.assertNotIn(pending_name, exec_pods)
        self.assertEqual("not-evaluated", packet["gateStatus"])
        self.assertFalse(packet["productionGatePassed"])
        self.assertEqual("not-evaluated", packet["productionLifecycleWrites"])

    def test_framed_projection_preserves_omitted_fields_as_malformed_selected_pods(self) -> None:
        for phase in ("initial", "recheck"):
            for missing_fields in (("metadata",), ("status",), ("metadata", "status")):
                with self.subTest(phase=phase, missing_fields=missing_fields):
                    scenario = copy.deepcopy(self.base_scenario)
                    self.add_second_pod(scenario)
                    key = "pods"
                    if phase == "recheck":
                        key = "podsAfter"
                        scenario[key] = copy.deepcopy(scenario["pods"])
                        scenario[key]["items"].reverse()
                    # A valid sibling must never hide this selected Pod's malformed frame.
                    for field in missing_fields:
                        del scenario[key]["items"][1][field]

                    result, packets, calls, _ = self.run_gate(scenario)

                    self.assertNotEqual(0, result.returncode)
                    self.assertEqual(["malformed-pod-list-json"], packets[0]["blockers"])
                    self.assertEqual([], packets[0]["observations"]["pods"])
                    rejected_source = (
                        "kubectl:lifecycle-pods-recheck:identity" if phase == "recheck"
                        else "kubectl:lifecycle-pods:identity"
                    )
                    self.assertFalse(any(entry["source"] == rejected_source for entry in packets[0]["sources"]))
                    self.assertEqual("not-evaluated", packets[0]["gateStatus"])
                    self.assertFalse(packets[0]["productionGatePassed"])
                    if phase == "initial":
                        self.assertFalse(any("exec" in call for call in calls))

    def test_duplicate_pod_uid_blocks_before_exec_but_case_differing_uid_is_distinct(self) -> None:
        duplicate_uid = copy.deepcopy(self.base_scenario)
        first_name, second_name = self.add_second_pod(duplicate_uid)
        duplicate_uid["pods"]["items"][1]["metadata"]["uid"] = (
            duplicate_uid["pods"]["items"][0]["metadata"]["uid"]
        )

        result, packets, calls, _ = self.run_gate(duplicate_uid)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["duplicate-running-pod-uid"], packets[0]["blockers"])
        self.assertFalse(any("exec" in call for call in calls))
        self.assertNotIn(TOKEN_CANARY, json.dumps(packets[0]) + result.stdout + result.stderr)

        case_differing_uid = copy.deepcopy(self.base_scenario)
        self.add_second_pod(case_differing_uid)
        case_differing_uid["pods"]["items"][1]["metadata"]["uid"] = (
            case_differing_uid["pods"]["items"][0]["metadata"]["uid"].upper()
        )

        result, packets, _, _ = self.run_gate(case_differing_uid)

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(
            [
                (first_name, "7e36eb30-17d5-48de-9c67-f9c6b95430ce"),
                (second_name, "7E36EB30-17D5-48DE-9C67-F9C6B95430CE"),
            ],
            [(pod["pod"], pod["podUid"]) for pod in packets[0]["observations"]["pods"]],
        )

    def test_invalid_alpha_pair_blocks_explicitly(self) -> None:
        scenario = copy.deepcopy(self.base_scenario)
        pod = next(iter(scenario["alphaOptIn"]))
        scenario["alphaOptIn"][pod] = ["true", "false"]

        result, packets, _, _ = self.run_gate(scenario)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["alpha-component-not-explicitly-allowed"], packets[0]["blockers"])

    def test_empty_enabled_features_is_observed_but_missing_property_blocks(self) -> None:
        empty = copy.deepcopy(self.base_scenario)
        pod = next(iter(empty["metadata"]))
        empty["metadata"][pod]["enabledFeatures"] = []

        result, packets, _, _ = self.run_gate(empty)

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual([], packets[0]["observations"]["enabledFeatures"])

        missing = copy.deepcopy(self.base_scenario)
        del missing["metadata"][pod]["enabledFeatures"]
        result, packets, _, _ = self.run_gate(missing)
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["metadata-enabled-features-missing"], packets[0]["blockers"])

    def test_wrong_context_blocks_before_pod_query(self) -> None:
        scenario = copy.deepcopy(self.base_scenario)
        scenario["context"] = "different-cluster"

        result, packets, calls, _ = self.run_gate(scenario)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["profile-context-mismatch"], packets[0]["blockers"])
        self.assertEqual([["config", "current-context"]], calls)

    def test_malformed_or_duplicate_pod_identity_blocks(self) -> None:
        malformed = copy.deepcopy(self.base_scenario)
        malformed["pods"]["items"] = malformed["pods"]["items"][0]

        result, packets, calls, _ = self.run_gate(malformed)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["malformed-pod-list-json"], packets[0]["blockers"])
        self.assertFalse(any("exec" in call for call in calls))

        duplicate = copy.deepcopy(self.base_scenario)
        first_name, _ = self.add_second_pod(duplicate)
        duplicate["pods"]["items"][1]["metadata"]["name"] = first_name
        result, packets, calls, _ = self.run_gate(duplicate)
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["duplicate-running-pod"], packets[0]["blockers"])
        self.assertFalse(any("exec" in call for call in calls))

    def test_initial_pod_identity_requires_nonblank_string_names_and_uids_before_exec(self) -> None:
        cases: dict[str, tuple[str, object, str]] = {
            "blank-name": ("name", " ", "running-pod-name-missing"),
            "blank-uid": ("uid", " ", "running-pod-uid-missing"),
            "numeric-name": ("name", 7, "running-pod-name-missing"),
            "numeric-uid": ("uid", 7, "running-pod-uid-missing"),
        }

        for name, (field, value, blocker) in cases.items():
            with self.subTest(name=name):
                scenario = copy.deepcopy(self.base_scenario)
                self.add_second_pod(scenario)
                scenario["pods"]["items"][1]["metadata"][field] = value

                result, packets, calls, _ = self.run_gate(scenario)

                self.assertNotEqual(0, result.returncode)
                self.assertEqual([blocker], packets[0]["blockers"])
                self.assertFalse(any("exec" in call for call in calls))
                self.assertNotIn(TOKEN_CANARY, json.dumps(packets[0]) + result.stdout + result.stderr)

    def test_every_post_capture_collection_shape_and_identity_drift_blocks(self) -> None:
        def stable_recheck() -> dict:
            scenario = copy.deepcopy(self.base_scenario)
            scenario["podsAfter"] = copy.deepcopy(scenario["pods"])
            return scenario

        cases: dict[str, tuple[dict, str]] = {}

        malformed_json = copy.deepcopy(self.base_scenario)
        malformed_json["podsAfterRaw"] = "{not-json"
        cases["malformed-json"] = (malformed_json, "malformed-pod-list-json")

        malformed_items = stable_recheck()
        malformed_items["podsAfter"]["items"] = malformed_items["podsAfter"]["items"][0]
        cases["non-array-items"] = (malformed_items, "malformed-pod-list-json")

        count = stable_recheck()
        count["podsAfter"]["items"] = []
        cases["count"] = (count, "running-pod-changed")

        running_count = stable_recheck()
        running_count["podsAfter"]["items"][0]["status"]["phase"] = "Pending"
        cases["running-count"] = (running_count, "running-pod-changed")

        replacement = stable_recheck()
        replacement["podsAfter"]["items"][0]["metadata"]["name"] = "memories-access-telemetry-replacement"
        cases["replacement"] = (replacement, "running-pod-changed")

        blank_name = stable_recheck()
        blank_name["podsAfter"]["items"][0]["metadata"]["name"] = " "
        cases["blank-name"] = (blank_name, "running-pod-changed")

        duplicate_name = copy.deepcopy(self.base_scenario)
        self.add_second_pod(duplicate_name)
        duplicate_name["podsAfter"] = copy.deepcopy(duplicate_name["pods"])
        duplicate_name["podsAfter"]["items"][1]["metadata"]["name"] = (
            duplicate_name["podsAfter"]["items"][0]["metadata"]["name"]
        )
        cases["duplicate-name"] = (duplicate_name, "running-pod-changed")

        label = stable_recheck()
        label["podsAfter"]["items"][0]["metadata"]["labels"]["app.kubernetes.io/name"] = "memories"
        cases["label"] = (label, "running-pod-changed")

        deletion = stable_recheck()
        deletion["podsAfter"]["items"][0]["metadata"]["deletionTimestamp"] = "2026-09-01T12:00:00Z"
        cases["deletion"] = (deletion, "running-pod-changed")

        ready_missing = stable_recheck()
        ready_missing["podsAfter"]["items"][0]["status"]["conditions"] = []
        cases["ready-missing"] = (ready_missing, "running-pod-changed")

        ready_duplicate = stable_recheck()
        ready_duplicate["podsAfter"]["items"][0]["status"]["conditions"].append(
            {"type": "Ready", "status": "True"}
        )
        cases["ready-duplicate"] = (ready_duplicate, "running-pod-changed")

        ready_false = stable_recheck()
        ready_false["podsAfter"]["items"][0]["status"]["conditions"][0]["status"] = "False"
        cases["ready-false"] = (ready_false, "running-pod-changed")

        ready_type_case_drift = stable_recheck()
        ready_type_case_drift["podsAfter"]["items"][0]["status"]["conditions"][0]["type"] = "ready"
        cases["ready-type-case-drift"] = (ready_type_case_drift, "running-pod-changed")

        lifecycle_missing = stable_recheck()
        lifecycle_missing["podsAfter"]["items"][0]["status"]["containerStatuses"] = [
            status
            for status in lifecycle_missing["podsAfter"]["items"][0]["status"]["containerStatuses"]
            if status["name"] != "lifecycle"
        ]
        cases["lifecycle-status-missing"] = (lifecycle_missing, "running-pod-changed")

        lifecycle_duplicate = stable_recheck()
        lifecycle_statuses = lifecycle_duplicate["podsAfter"]["items"][0]["status"]["containerStatuses"]
        lifecycle_statuses.append(copy.deepcopy(next(status for status in lifecycle_statuses if status["name"] == "lifecycle")))
        cases["lifecycle-status-duplicate"] = (lifecycle_duplicate, "running-pod-changed")

        daprd_missing = stable_recheck()
        daprd_missing["podsAfter"]["items"][0]["status"]["containerStatuses"] = [
            status
            for status in daprd_missing["podsAfter"]["items"][0]["status"]["containerStatuses"]
            if status["name"] != "daprd"
        ]
        cases["daprd-status-missing"] = (daprd_missing, "running-pod-changed")

        daprd_duplicate = stable_recheck()
        daprd_statuses = daprd_duplicate["podsAfter"]["items"][0]["status"]["containerStatuses"]
        daprd_statuses.append(copy.deepcopy(next(status for status in daprd_statuses if status["name"] == "daprd")))
        cases["daprd-status-duplicate"] = (daprd_duplicate, "running-pod-changed")

        for original_name, drifted_name in (("lifecycle", "Lifecycle"), ("daprd", "Daprd")):
            container_name_case_drift = stable_recheck()
            statuses = container_name_case_drift["podsAfter"]["items"][0]["status"]["containerStatuses"]
            next(status for status in statuses if status["name"] == original_name)["name"] = drifted_name
            cases[f"{original_name}-name-case-drift"] = (container_name_case_drift, "running-pod-changed")

        for container_name in ("lifecycle", "daprd"):
            non_boolean = stable_recheck()
            statuses = non_boolean["podsAfter"]["items"][0]["status"]["containerStatuses"]
            next(status for status in statuses if status["name"] == container_name)["ready"] = "true"
            cases[f"{container_name}-ready-non-boolean"] = (non_boolean, "running-pod-changed")

            not_ready = stable_recheck()
            statuses = not_ready["podsAfter"]["items"][0]["status"]["containerStatuses"]
            next(status for status in statuses if status["name"] == container_name)["ready"] = False
            cases[f"{container_name}-not-ready"] = (not_ready, "running-pod-changed")

        uid_missing = stable_recheck()
        uid_missing["podsAfter"]["items"][0]["metadata"]["uid"] = ""
        cases["uid-missing"] = (uid_missing, "running-pod-changed")

        uid_changed = stable_recheck()
        uid_changed["podsAfter"]["items"][0]["metadata"]["uid"] = "cc30cc1b-a706-4681-b944-3e923d96fa20"
        cases["uid-changed"] = (uid_changed, "running-pod-changed")

        image_missing = stable_recheck()
        image_statuses = image_missing["podsAfter"]["items"][0]["status"]["containerStatuses"]
        next(status for status in image_statuses if status["name"] == "daprd")["imageID"] = ""
        cases["image-missing"] = (image_missing, "running-pod-changed")

        image_changed = stable_recheck()
        image_statuses = image_changed["podsAfter"]["items"][0]["status"]["containerStatuses"]
        next(status for status in image_statuses if status["name"] == "daprd")["imageID"] = (
            "docker-pullable://ghcr.io/dapr/daprd@sha256:" + "c" * 64
        )
        cases["image-changed"] = (image_changed, "running-pod-changed")

        for name, (scenario, blocker) in cases.items():
            with self.subTest(name=name):
                result, packets, calls, _ = self.run_gate(scenario)
                self.assertNotEqual(0, result.returncode)
                self.assertEqual([blocker], packets[0]["blockers"])
                self.assertEqual("not-evaluated", packets[0]["gateStatus"])
                self.assertFalse(packets[0]["productionGatePassed"])
                self.assertNotIn(TOKEN_CANARY, json.dumps(packets[0]) + result.stdout + result.stderr)
                pod_gets = [call for call in calls if "get" in call and "pods" in call]
                self.assertEqual(2, len(pod_gets))

    def test_missing_runtime_metadata_token_blocks(self) -> None:
        scenario = copy.deepcopy(self.base_scenario)
        scenario["metadataTokenAvailable"] = False

        result, packets, _, _ = self.run_gate(scenario)

        self.assertNotEqual(0, result.returncode)
        self.assertRegex(packets[0]["blockers"][0], r"^kubectl-metadata:.*-exit-72$")

    def test_running_unready_or_terminating_pod_blocks_before_exec(self) -> None:
        cases = {}
        container_false = copy.deepcopy(self.base_scenario)
        container_false["pods"]["items"][0]["status"]["containerStatuses"][0]["ready"] = False
        cases["container-false"] = (container_false, "running-pod-containers-not-ready")
        container_string = copy.deepcopy(self.base_scenario)
        container_string["pods"]["items"][0]["status"]["containerStatuses"][0]["ready"] = "false"
        cases["container-string"] = (container_string, "running-pod-containers-not-ready")
        pod_unready = copy.deepcopy(self.base_scenario)
        pod_unready["pods"]["items"][0]["status"]["conditions"][0]["status"] = "False"
        cases["pod-unready"] = (pod_unready, "running-pod-not-stable")
        terminating = copy.deepcopy(self.base_scenario)
        terminating["pods"]["items"][0]["metadata"]["deletionTimestamp"] = "2026-08-03T12:00:00Z"
        cases["terminating"] = (terminating, "running-pod-not-stable")

        for name, (scenario, blocker) in cases.items():
            with self.subTest(name=name):
                result, packets, calls, _ = self.run_gate(scenario)
                self.assertNotEqual(0, result.returncode)
                self.assertEqual([blocker], packets[0]["blockers"])
                self.assertFalse(any("exec" in call for call in calls))

    def test_kubectl_timeout_writes_blocker_packet(self) -> None:
        scenario = copy.deepcopy(self.base_scenario)
        scenario["hangPurpose"] = "current-context"
        scenario["hangSeconds"] = 5

        result, packets, _, _ = self.run_gate(scenario, command_timeout_seconds=1)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["kubectl-current-context-timeout"], packets[0]["blockers"])
        self.assertEqual("not-evaluated", packets[0]["gateStatus"])

    def test_malformed_metadata_writes_blocker_packet_and_exits_nonzero(self) -> None:
        scenario = copy.deepcopy(self.base_scenario)
        pod = next(iter(scenario["metadata"]))
        scenario["metadataRaw"] = {pod: "{not-json"}

        result, packets, _, _ = self.run_gate(scenario)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(1, len(packets))
        self.assertEqual(["malformed-metadata-json"], packets[0]["blockers"])
        self.assertEqual("not-evaluated", packets[0]["gateStatus"])

    def test_oversized_probe_output_is_bounded_and_blocks(self) -> None:
        scenario = copy.deepcopy(self.base_scenario)
        pod = next(iter(scenario["metadata"]))
        scenario["metadataRaw"] = {pod: "x" * (1024 * 1024 + 1)}

        result, packets, _, _ = self.run_gate(scenario)

        self.assertNotEqual(0, result.returncode)
        self.assertRegex(packets[0]["blockers"][0], r"^kubectl-metadata:.*-output-too-large$")
        self.assertNotIn("x" * 1024, json.dumps(packets[0]))

    def assert_metadata_json_rejected(self, raw_metadata: str, *secrets: str) -> None:
        scenario = copy.deepcopy(self.base_scenario)
        pod = next(iter(scenario["metadata"]))
        scenario["metadataRaw"] = {pod: raw_metadata}
        result, packets, calls, evidence = self.run_gate(scenario)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(1, len(packets))
        packet = packets[0]
        self.assertEqual(["malformed-metadata-json"], packet["blockers"])
        self.assertEqual("blocked", packet["producerStatus"])
        self.assertEqual("not-evaluated", packet["gateStatus"])
        self.assertFalse(packet["productionGatePassed"])
        self.assertEqual("not-evaluated", packet["productionLifecycleWrites"])
        self.assertEqual([], packet["observations"]["pods"])
        self.assertFalse(any("AccessTelemetryLifecycle__ComponentIsAlpha" in call[-1] for call in calls))
        self.assertFalse(any("get" in call and "pods" in call for call in calls[2:]))
        self.assertFalse(any(entry["source"].startswith("kubectl:metadata:") for entry in packet["sources"]))
        captured = json.dumps(packet) + result.stdout + result.stderr + json.dumps(calls)
        for secret in secrets:
            self.assertNotIn(secret, captured)
        if raw_metadata.strip():
            self.assertNotIn(raw_metadata, result.stdout + result.stderr + json.dumps(calls))
            self.assertNotIn(
                hashlib.sha256(raw_metadata.encode("utf-8")).hexdigest(),
                [entry["sha256"] for entry in packet["sources"]],
            )
        self.assertEqual(0, next(evidence.glob("*.json")).stat().st_mode & (stat.S_IWUSR | stat.S_IWGRP | stat.S_IWOTH))

    def test_duplicate_metadata_properties_block_before_conversion_can_overwrite_secrets(self) -> None:
        pod = next(iter(self.base_scenario["metadata"]))
        base = json.dumps(self.base_scenario["metadata"][pod])
        encoded_canary = '"' + "".join(f"\\u{ord(character):04x}" for character in TOKEN_CANARY) + '"'
        cases = {
            "identity": '"id": "memories-access-telemetry"',
            "diagnostic": '"diagnostic": 1, "diagnostic": 2',
            "escaped-name": '"\\u0064iagnostic": 1, "diagnostic": 2',
            "empty-name": '"diagnostic": {"": 1, "": 2}',
            "nested-object": '"diagnostic": {"details": {"message": 1, "message": 2}}',
            "nested-array": '"diagnostic": [null, [{"message": 1, "message": 2}]]',
            "encoded-secret": '"diagnostic": ' + encoded_canary + ', "diagnostic": "harmless"',
            "nested-encoded-secret": '"diagnostic": [{"message": ' + encoded_canary + ', "message": "harmless"}]',
            "encoded-credential": '"diagnostic": {"\\u0061uthorization": "fixture-credential", "authorization": ""}',
            "undecodable-high-surrogate": '"\\ud800": ' + encoded_canary + ', "\\ufffd": "harmless"',
            "undecodable-low-surrogate": '"\\ufffd": ' + encoded_canary + ', "\\udc00": "harmless"',
        }
        for name, fragment in cases.items():
            with self.subTest(name=name):
                raw_metadata = base[:-1] + ", " + fragment + "}"
                json.loads(raw_metadata)
                self.assertNotIn(TOKEN_CANARY, raw_metadata)
                self.assert_metadata_json_rejected(raw_metadata, TOKEN_CANARY, "fixture-credential")

    def test_metadata_root_arrays_and_empty_responses_block_before_conversion(self) -> None:
        pod = next(iter(self.base_scenario["metadata"]))
        metadata = self.base_scenario["metadata"][pod]
        cases = {
            "singleton": json.dumps([metadata]),
            "nested": json.dumps([[metadata]]),
            "multiple": json.dumps([metadata, metadata]),
            "empty-array": "[]",
            "null-item": "[null]",
            "empty-response": "",
            "whitespace-response": " \t\n ",
        }
        for name, raw_metadata in cases.items():
            with self.subTest(name=name):
                self.assert_metadata_json_rejected(raw_metadata)

    def test_repeated_names_in_separate_objects_and_escaped_names_preserve_observations(self) -> None:
        baseline_result, baseline_packets, _, _ = self.run_gate(self.base_scenario)
        self.assertEqual(0, baseline_result.returncode, baseline_result.stderr)
        scenario = copy.deepcopy(self.base_scenario)
        pod = next(iter(scenario["metadata"]))
        scenario["metadata"][pod]["diagnostic"] = {
            "first": {"message": "harmless", "value": None},
            "second": {"message": "ordinary", "value": False},
            "array": [{"message": 1}, {"message": 2}],
        }
        raw_metadata = json.dumps(scenario["metadata"][pod]).replace('"id":', '"\\u0069d":')
        self.assertEqual(scenario["metadata"][pod], json.loads(raw_metadata))
        scenario["metadataRaw"] = {pod: raw_metadata}

        result, packets, _, _ = self.run_gate(scenario)

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(1, len(packets))
        self.assertEqual("observed", packets[0]["producerStatus"])
        self.assertEqual([], packets[0]["blockers"])
        self.assertEqual(baseline_packets[0]["observations"], packets[0]["observations"])
        self.assertEqual(baseline_packets[0]["sources"], packets[0]["sources"])
        self.assertEqual(baseline_packets[0]["commands"], packets[0]["commands"])

    def test_secret_shaped_metadata_blocks_without_copying_secret_to_packet(self) -> None:
        scenario = copy.deepcopy(self.base_scenario)
        pod = next(iter(scenario["metadata"]))
        scenario["metadata"][pod]["diagnostic"] = TOKEN_CANARY

        result, packets, _, _ = self.run_gate(scenario)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(1, len(packets))
        serialized = json.dumps(packets[0])
        self.assertNotIn(TOKEN_CANARY, serialized)
        self.assertNotIn(TOKEN_CANARY, result.stdout + result.stderr)
        self.assertEqual(["secret-shaped-output"], packets[0]["blockers"])

        encoded = copy.deepcopy(self.base_scenario)
        encoded_canary = TOKEN_CANARY.replace("_", r"\u005f")
        raw_metadata = json.dumps(encoded["metadata"][pod]).replace(
            '"Actor.Reentrancy"',
            f'"{encoded_canary}"',
        )
        encoded["metadataRaw"] = {pod: raw_metadata}
        result, packets, _, _ = self.run_gate(encoded)
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["secret-shaped-output"], packets[0]["blockers"])
        self.assertNotIn(TOKEN_CANARY, json.dumps(packets[0]) + result.stdout + result.stderr)

        secret_property = copy.deepcopy(self.base_scenario)
        secret_property["metadata"][pod]["authorization"] = "Bearer-sensitive-value"
        result, packets, _, _ = self.run_gate(secret_property)
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["secret-shaped-output"], packets[0]["blockers"])
        self.assertNotIn("Bearer-sensitive-value", json.dumps(packets[0]) + result.stdout + result.stderr)

    def assert_metadata_secret_rejected(self, scenario: dict, *secrets: str) -> None:
        result, packets, calls, evidence = self.run_gate(scenario)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(1, len(packets))
        packet = packets[0]
        self.assertEqual(["secret-shaped-output"], packet["blockers"])
        self.assertEqual("blocked", packet["producerStatus"])
        self.assertEqual("not-evaluated", packet["gateStatus"])
        self.assertFalse(packet["productionGatePassed"])
        self.assertEqual("not-evaluated", packet["productionLifecycleWrites"])
        self.assertEqual([], packet["observations"]["pods"])
        self.assertFalse(any("alpha-opt-in:" in entry["purpose"] for entry in packet["commands"]))
        self.assertFalse(any("AccessTelemetryLifecycle__ComponentIsAlpha" in call[-1] for call in calls))
        self.assertFalse(any("get" in call and "pods" in call for call in calls[2:]))
        self.assertFalse(any(entry["source"].startswith("kubectl:metadata:") for entry in packet["sources"]))
        captured = json.dumps(packet) + result.stdout + result.stderr + json.dumps(calls)
        for secret in secrets:
            self.assertNotIn(secret, captured)
        for raw_metadata in scenario.get("metadataRaw", {}).values():
            self.assertNotIn(raw_metadata, captured)
            self.assertNotIn(
                hashlib.sha256(raw_metadata.encode("utf-8")).hexdigest(),
                [entry["sha256"] for entry in packet["sources"]],
            )
        packet_path = next(evidence.glob("*.json"))
        self.assertEqual(0, packet_path.stat().st_mode & (stat.S_IWUSR | stat.S_IWGRP | stat.S_IWOTH))

    def test_encoded_secrets_in_discarded_names_and_values_block_before_projection(self) -> None:
        cases = {
            "string-canary": (TOKEN_CANARY, TOKEN_CANARY),
            "nested-object-canary": ({"details": {"message": TOKEN_CANARY}}, TOKEN_CANARY),
            "nested-array-canary": ([None, 0, False, [], {}, [{"message": TOKEN_CANARY}]], TOKEN_CANARY),
            "property-name-canary": ({TOKEN_CANARY: "harmless"}, TOKEN_CANARY),
            "hvs-token": (["harmless", {"message": "hvs.discarded_token_7429"}], "hvs.discarded_token_7429"),
            "hvb-token": ({"message": "hvb.discarded_token_7429"}, "hvb.discarded_token_7429"),
            "hvr-token": ([["hvr.discarded_token_7429"]], "hvr.discarded_token_7429"),
            "decoded-whitespace": (
                {"message": "authorization\t:\tBearer-discarded-value"},
                "authorization\t:\tBearer-discarded-value",
            ),
            "decoded-assignment": (
                {"message": "dapr_api_token = discarded_credential"},
                "dapr_api_token = discarded_credential",
            ),
            "decoded-quoted-property": (
                '"authorization": "Bearer-discarded-value"',
                '"authorization": "Bearer-discarded-value"',
            ),
        }
        for name, (diagnostic, secret) in cases.items():
            with self.subTest(name=name):
                scenario = copy.deepcopy(self.base_scenario)
                pod = next(iter(scenario["metadata"]))
                scenario["metadata"][pod]["diagnostic"] = diagnostic
                encoded_secret = '"' + "".join(f"\\u{ord(character):04x}" for character in secret) + '"'
                raw_metadata = json.dumps(scenario["metadata"][pod]).replace(json.dumps(secret), encoded_secret)
                self.assertEqual(scenario["metadata"][pod], json.loads(raw_metadata))
                self.assertNotIn(secret, raw_metadata)
                scenario["metadataRaw"] = {pod: raw_metadata}

                self.assert_metadata_secret_rejected(scenario, secret, "Bearer-discarded-value", "discarded_credential")

    def test_encoded_credential_property_names_block_in_discarded_nested_objects(self) -> None:
        credential = "Bearer-sensitive-value"
        for name in ("authorization", "dapr-api-token", "dapr_api_token", "DaPrApiToken"):
            with self.subTest(name=name):
                scenario = copy.deepcopy(self.base_scenario)
                pod = next(iter(scenario["metadata"]))
                scenario["metadata"][pod]["diagnostic"] = [None, {name: credential}]
                encoded_name = '"' + "".join(f"\\u{ord(character):04x}" for character in name) + '"'
                raw_metadata = json.dumps(scenario["metadata"][pod]).replace(json.dumps(name), encoded_name)
                self.assertEqual(scenario["metadata"][pod], json.loads(raw_metadata))
                self.assertNotIn(json.dumps(name), raw_metadata)
                scenario["metadataRaw"] = {pod: raw_metadata}

                self.assert_metadata_secret_rejected(scenario, credential)

    def test_invalid_metadata_types_and_depth_block_without_later_probes(self) -> None:
        excessive_depth: object = "harmless"
        for _ in range(32):
            excessive_depth = {"nested": excessive_depth}
        cases = {
            "null": None,
            "string": "ordinary",
            "number": 7,
            "boolean": True,
            "empty-array": [],
            "mixed-array": [None, "harmless", 0, False, {}, []],
            "missing-id": {"diagnostic": "harmless"},
            "excessive-depth": excessive_depth,
        }
        for name, metadata in cases.items():
            with self.subTest(name=name):
                scenario = copy.deepcopy(self.base_scenario)
                pod = next(iter(scenario["metadata"]))
                scenario["metadataRaw"] = {pod: json.dumps(metadata)}

                result, packets, calls, _ = self.run_gate(scenario)

                self.assertNotEqual(0, result.returncode)
                self.assertEqual(1, len(packets))
                self.assertEqual("blocked", packets[0]["producerStatus"])
                self.assertEqual("not-evaluated", packets[0]["gateStatus"])
                self.assertFalse(packets[0]["productionGatePassed"])
                self.assertEqual([], packets[0]["observations"]["pods"])
                self.assertFalse(any("AccessTelemetryLifecycle__ComponentIsAlpha" in call[-1] for call in calls))
                self.assertFalse(
                    any(entry["source"].startswith("kubectl:metadata:") for entry in packets[0]["sources"])
                )
                if name == "excessive-depth":
                    self.assertEqual(["malformed-metadata-json"], packets[0]["blockers"])

        encoded_canary = TOKEN_CANARY.replace("_", r"\u005f")
        raw_metadata = '{"diagnostic":"' + encoded_canary + '"}'
        self.assertEqual({"diagnostic": TOKEN_CANARY}, json.loads(raw_metadata))
        scenario = copy.deepcopy(self.base_scenario)
        pod = next(iter(scenario["metadata"]))
        scenario["metadataRaw"] = {pod: raw_metadata}
        self.assert_metadata_secret_rejected(scenario, TOKEN_CANARY)

    def test_benign_discarded_metadata_preserves_observations_and_allowlisted_hashes(self) -> None:
        baseline_result, baseline_packets, _, _ = self.run_gate(self.base_scenario)
        self.assertEqual(0, baseline_result.returncode, baseline_result.stderr)
        scenario = copy.deepcopy(self.base_scenario)
        pod = next(iter(scenario["metadata"]))
        diagnostic = {
            "message": "benign-discarded-diagnostic-7429",
            "nested": [None, 0, 1.25, True, False, "", [], {}, ["ordinary", {"detail": "harmless"}]],
            "credentials": {
                "authorization": "",
                "dapr-api-token": None,
                "dapr_api_token": 0,
                "DaprApiToken": False,
            },
        }
        nested: object = {"message": "harmless"}
        for _ in range(10):
            nested = {"nested": [nested]}
        diagnostic["deep"] = nested
        scenario["metadata"][pod]["diagnostic"] = diagnostic
        scenario["metadata"][pod]["actors"][0]["diagnostic"] = [None, {"message": "harmless"}]
        raw_metadata = json.dumps(scenario["metadata"][pod])
        scenario["metadataRaw"] = {pod: raw_metadata}

        result, packets, calls, evidence = self.run_gate(scenario, repeat=2)

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(2, len(packets))
        for packet in packets:
            self.assertEqual("observed", packet["producerStatus"])
            self.assertEqual([], packet["blockers"])
            self.assertEqual("not-evaluated", packet["gateStatus"])
            self.assertFalse(packet["productionGatePassed"])
            self.assertEqual(baseline_packets[0]["observations"], packet["observations"])
            self.assertEqual(baseline_packets[0]["sources"], packet["sources"])
            self.assertEqual(baseline_packets[0]["commands"], packet["commands"])
            self.assertNotIn("benign-discarded-diagnostic-7429", json.dumps(packet) + result.stdout + result.stderr)
            self.assertNotIn(
                hashlib.sha256(raw_metadata.encode("utf-8")).hexdigest(),
                [entry["sha256"] for entry in packet["sources"]],
            )
            metadata_sources = [
                entry["source"]
                for entry in packet["sources"]
                if entry["source"].startswith("kubectl:metadata:")
            ]
            self.assertEqual([f"kubectl:metadata:{pod}:allowlisted"], metadata_sources)
        self.assertEqual(2, sum("AccessTelemetryLifecycle__ComponentIsAlpha" in call[-1] for call in calls))
        packet_paths = list(evidence.glob("*.json"))
        self.assertEqual(2, len({path.name for path in packet_paths}))
        self.assertTrue(
            all(path.stat().st_mode & (stat.S_IWUSR | stat.S_IWGRP | stat.S_IWOTH) == 0 for path in packet_paths)
        )

    def test_accepted_c1_15_identity_evidence_preserves_production_and_other_gate_restrictions(self) -> None:
        base_kustomization = BASE_KUSTOMIZATION.read_text(encoding="utf-8")
        production_kustomization = PRODUCTION_KUSTOMIZATION.read_text(encoding="utf-8")
        base_deployments = LIFECYCLE_DEPLOYMENTS.read_text(encoding="utf-8")
        production_patch = PRODUCTION_DISABLED_PATCH.read_text(encoding="utf-8")

        self.assertIn("- access-telemetry-deployments.yaml", base_kustomization)
        self.assertIn("- ../../base", production_kustomization)
        self.assertIn("- path: access-telemetry-disabled-patch.yaml", production_kustomization)

        zero_scaled = True
        for deployment_name in LIFECYCLE_DEPLOYMENT_NAMES:
            base_documents = [
                document
                for document in base_deployments.split("\n---\n")
                if re.search(rf"(?m)^  name: {re.escape(deployment_name)}$", document)
            ]
            self.assertEqual(1, len(base_documents), deployment_name)
            self.assertRegex(base_documents[0], r"(?m)^kind: Deployment$")

            patch_documents = [
                document
                for document in production_patch.split("\n---\n")
                if re.search(rf"(?m)^  name: {re.escape(deployment_name)}$", document)
            ]
            self.assertEqual(1, len(patch_documents), deployment_name)
            self.assertRegex(patch_documents[0], r"(?m)^kind: Deployment$")
            zero_scaled = zero_scaled and re.search(r"(?m)^  replicas: 0$", patch_documents[0]) is not None

        production_inputs = sorted((REPO_ROOT / "deploy" / "kubernetes" / "base").rglob("*.yaml"))
        production_inputs.extend(
            sorted((REPO_ROOT / "deploy" / "kubernetes" / "overlays" / "production").rglob("*.yaml"))
        )
        production_text = "\n".join(path.read_text(encoding="utf-8") for path in production_inputs)
        explicit_alpha_pair_present = all(
            option_name in production_text
            for option_name in (
                "AccessTelemetryLifecycle__ComponentIsAlpha",
                "AccessTelemetryLifecycle__AllowAlphaComponent",
            )
        )
        self.assertTrue(zero_scaled or not explicit_alpha_pair_present)
        self.assertIn("ACCESS_TELEMETRY_ENABLED=false", production_text)

        story_text = STORY.read_text(encoding="utf-8")
        sprint_status = SPRINT_STATUS.read_text(encoding="utf-8")
        story_state = re.search(r"(?m)^Status: (review|done)$", story_text)
        self.assertIsNotNone(story_state)
        self.assertRegex(
            sprint_status,
            rf"(?m)^  27-21-runtime-and-control-plane-identity: {story_state.group(1)}$",
        )
        spec_text = EXECUTION_SPEC.read_text(encoding="utf-8")
        expected_spec_state = "in-review" if story_state.group(1) == "review" else "done"
        self.assertRegex(spec_text, rf"(?m)^status: '{expected_spec_state}'$")
        self.assertRegex(
            sprint_status,
            r"(?m)^  27-4-retention-verification-operations-runbook-and-a41-close-out: (?:backlog|in-progress)$",
        )

        slice_proof_match = re.search(
            r"(?ms)^## Slice Proof\s*$.*?(?=^## |\Z)",
            story_text,
        )
        self.assertIsNotNone(slice_proof_match)
        slice_rows = [
            line
            for line in slice_proof_match.group(0).splitlines()
            if line.startswith("| C1.15 |")
        ]
        self.assertEqual(1, len(slice_rows))
        slice_cells = [cell.strip() for cell in slice_rows[0].strip("|").split("|")]
        self.assertEqual(["accepted", "complete"], slice_cells[-2:])
        self.assertEqual("`/root/review_c1_packet`", slice_cells[-3])
        self.assertEqual(
            [f"../../{OBSERVED_PACKET_PATH}", f"../../{INDEPENDENT_REVIEW_PATH}"],
            re.findall(r"\]\(([^)]+)\)", slice_cells[3]),
        )
        self.assertIn(OBSERVED_PACKET_SHA256, slice_cells[3])
        self.assertIn(INDEPENDENT_REVIEW_SHA256, slice_cells[3])
        self.assertIn(
            "pwsh ./tools/verify-access-telemetry-c1.ps1 -Gate C1.15 -ProfileId PG-ONPREM-1 "
            "-EvidenceDirectory ./artifacts/access-telemetry-c1/C1.15",
            story_text,
        )

        expected_disposition = {
            "Independent reviewer": "/root/review_c1_packet",
            "Packet disposition": "accepted",
            "Accepted scope": "DW-718 capture/review closure and C1.15 runtime/control-plane identity capture only",
            "Observed packet": f"[Observed packet](../../{OBSERVED_PACKET_PATH})",
            "Observed packet SHA-256": OBSERVED_PACKET_SHA256,
            "Independent review artifact": f"[Independent packet review](../../{INDEPENDENT_REVIEW_PATH})",
            "Independent review SHA-256": INDEPENDENT_REVIEW_SHA256,
            "C1.15 review state": "accepted",
            "C1.15 completion state": "complete",
            "Production acceptance": "not-evaluated",
            "productionGatePassed": "false",
            "Production write enablement authorized": "false",
            "Other C1 gates discharged": "none",
            "C1.25 security approval": "false",
            "Story 27.4 advanced": "false",
            "A41 closed": "false",
            "Human sign-off": "false",
        }
        # Bind acceptance to its tracked exact section and immutable references.
        # Local ignored evidence files need not exist in a fresh CI checkout.
        for label, text in (("story", story_text), ("execution-spec", spec_text)):
            with self.subTest(record=label):
                disposition_section = re.search(
                    r"(?ms)^## 2026-10-04 Independent C1\.15 Packet Disposition\s*$.*?(?=^## |\Z)",
                    text,
                )
                self.assertIsNotNone(disposition_section)
                actual_disposition = {}
                for line in disposition_section.group(0).splitlines():
                    if not line.startswith("| "):
                        continue
                    cells = [cell.strip().strip("`") for cell in line.strip("|").split("|")]
                    self.assertEqual(2, len(cells))
                    if cells[0] == "Field" or cells[0].startswith(":--"):
                        continue
                    self.assertNotIn(cells[0], actual_disposition)
                    actual_disposition[cells[0]] = cells[1]
                self.assertEqual(expected_disposition, actual_disposition)

        handoff_text = HISTORICAL_HANDOFF.read_text(encoding="utf-8")
        self.assertRegex(handoff_text, r"(?m)^status: 'awaiting-operator'$")
        historical_halt = re.search(r"(?ms)^## 2026-09-05 DW-718 Halt Note\s*$.*?\Z", handoff_text)
        self.assertIsNotNone(historical_halt)
        self.assertIn("DW-718 capture was halted", historical_halt.group(0))
        self.assertIn("no Ready", historical_halt.group(0))
        self.assertIn("awaiting-operator", historical_halt.group(0))

        result, packets, _, _ = self.run_gate(self.base_scenario)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("observed", packets[0]["producerStatus"])
        self.assertEqual("not-evaluated", packets[0]["gateStatus"])
        self.assertFalse(packets[0]["productionGatePassed"])
        self.assertEqual("not-evaluated", packets[0]["productionLifecycleWrites"])

        change_log_match = re.search(
            r"(?ms)^## Change Log\s*$.*?(?=^## |\Z)",
            story_text,
        )
        self.assertIsNotNone(change_log_match)
        change_log_rows = change_log_match.group(0).splitlines()
        creation_rows = [line for line in change_log_rows if line.startswith("| 2026-08-03 | create-story |")]
        review_rows = [line for line in change_log_rows if line.startswith("| 2026-08-03 | code-review |")]
        self.assertEqual(1, len(creation_rows))
        self.assertEqual(1, len(review_rows))
        self.assertIn("Creation baseline records 6 discovered test methods", creation_rows[0])
        self.assertIn("comparable discovery `6 -> 12` test methods", review_rows[0])

        a41_action_match = re.search(
            r'(?ms)^  - epic: 20\s*$\n    action: "Keep 20\.5-A41-ACCESS-TELEMETRY-RETENTION .*?'
            r'(?=^  - epic:|\Z)',
            sprint_status,
        )
        self.assertIsNotNone(a41_action_match)
        self.assertRegex(a41_action_match.group(0), r"(?m)^    status: open(?:\s|$)")

        epic_context = EPIC_CONTEXT.read_text(encoding="utf-8")
        self.assertIn(
            "Twenty-three other gates remain held and unregistered; planning drafts confer no ownership or gate credit.",
            epic_context,
        )
        self.assertIn(INDEPENDENT_REVIEW_PATH, epic_context)
        self.assertIn(INDEPENDENT_REVIEW_SHA256, epic_context)
        self.assertIn(OBSERVED_PACKET_SHA256, epic_context)

        deferred_work = DEFERRED_WORK.read_text(encoding="utf-8")
        deferred_sections = {}
        for deferred_id in ("17", "718"):
            section_match = re.search(
                rf"(?ms)^### DW-{deferred_id}:.*?(?=^### DW-|\Z)",
                deferred_work,
            )
            self.assertIsNotNone(section_match)
            deferred_sections[deferred_id] = section_match.group(0)

        self.assertRegex(deferred_sections["17"], r"(?m)^status: open$")
        residual = deferred_sections["718"]
        self.assertIn("27.21-C1.15-REAL-PACKET-REVIEW", residual)
        self.assertRegex(residual, r"(?m)^status: done 2026-10-04$")
        resolution = re.search(r"(?m)^resolution: 2026-10-04 .*?$", residual)
        self.assertIsNotNone(resolution)
        for bound_evidence in (
            OBSERVED_PACKET_PATH, OBSERVED_PACKET_SHA256,
            INDEPENDENT_REVIEW_PATH, INDEPENDENT_REVIEW_SHA256, "/root/review_c1_packet",
        ):
            self.assertIn(bound_evidence, resolution.group(0))

    def test_legacy_lowercase_gate_and_profile_preserve_c1_15_capture(self) -> None:
        result, packets, calls, evidence = self.run_gate(
            self.base_scenario, gate="c1.15", profile_id="pg-onprem-1",
        )

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(1, len(packets))
        self.assertEqual("observed", packets[0]["producerStatus"])
        self.assertEqual("c1.15", packets[0]["gate"])
        self.assertEqual("pg-onprem-1", packets[0]["profileId"])
        self.assertEqual("not-evaluated", packets[0]["gateStatus"])
        self.assertFalse(packets[0]["productionGatePassed"])
        self.assertTrue(calls)
        self.assertTrue(next(evidence.glob("c1.15-runtime-control-plane-identity-*.json")))

    def test_unsupported_gate_fails_parameter_validation_before_producer_runs(self) -> None:
        result, packets, calls, evidence = self.run_gate(
            self.base_scenario,
            gate="C1.14",
            evidence_directory=False,
        )

        self.assertNotEqual(0, result.returncode)
        self.assertEqual([], packets)
        self.assertEqual([], calls)
        self.assertFalse(evidence.exists())
        self.assertRegex(result.stderr, re.compile(r"ValidateSet|validation set", re.IGNORECASE))

    def test_unsupported_profile_fails_parameter_validation_before_producer_runs(self) -> None:
        result, packets, calls, evidence = self.run_gate(
            self.base_scenario,
            profile_id="PG-CLOUD-1",
            evidence_directory=False,
        )

        self.assertNotEqual(0, result.returncode)
        self.assertEqual([], packets)
        self.assertEqual([], calls)
        self.assertFalse(evidence.exists())
        self.assertRegex(result.stderr, re.compile(r"ValidateSet|validation set", re.IGNORECASE))


if __name__ == "__main__":
    unittest.main()
