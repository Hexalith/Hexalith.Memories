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
TOKEN_CANARY = "C1_SECRET_CANARY_DO_NOT_EMIT_7429"
TARGET_SELECTOR = "app.kubernetes.io/name=memories-access-telemetry"


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
            with Path(os.environ["C1_KUBECTL_LOG"]).open("a", encoding="utf-8") as log:
                log.write(json.dumps(args) + "\\n")

            if args == ["config", "current-context"]:
                if scenario.get("hangPurpose") == "current-context":
                    time.sleep(scenario.get("hangSeconds", 5))
                print(scenario["context"])
                raise SystemExit(0)

            if "get" in args and "pods" in args:
                selector_index = args.index("-l") if "-l" in args else -1
                if selector_index < 0 or args[selector_index + 1] != "app.kubernetes.io/name=memories-access-telemetry":
                    print("unexpected selector", file=sys.stderr)
                    raise SystemExit(98)
                print(json.dumps(scenario["pods"]))
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
                if "DAPR_API_TOKEN" not in shell_text or "dapr-api-token:" not in shell_text:
                    print("metadata authentication missing", file=sys.stderr)
                    raise SystemExit(94)
                raw = scenario.get("metadataRaw", {}).get(pod)
                if raw is not None:
                    print(raw)
                else:
                    print(json.dumps(scenario["metadata"][pod]))
                raise SystemExit(0)
            if "AccessTelemetryLifecycle__ComponentIsAlpha" in shell_text:
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
            "PG-ONPREM-1",
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
        self.assertEqual("observed", packet["producerStatus"])
        self.assertEqual("not-evaluated", packet["gateStatus"])
        self.assertFalse(packet["productionGatePassed"])
        self.assertEqual("disabled", packet["productionLifecycleWrites"])
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
        _, second_name = self.add_second_pod(complete)

        result, packets, _, _ = self.run_gate(complete)

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(2, len(packets[0]["observations"]["pods"]))

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


if __name__ == "__main__":
    unittest.main()
