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
                if prior_pod_gets and "podsAfterRaw" in scenario:
                    print(scenario["podsAfterRaw"])
                else:
                    pods = scenario.get("podsAfter", scenario["pods"]) if prior_pod_gets else scenario["pods"]
                    print(json.dumps(pods))
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

    def test_unavailable_production_target_and_operator_residual_remain_explicitly_open(self) -> None:
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
        self.assertRegex(story_text, r"(?m)^Status: in-progress$")
        self.assertRegex(
            sprint_status,
            r"(?m)^  27-21-runtime-and-control-plane-identity: in-progress$",
        )
        self.assertRegex(
            sprint_status,
            r"(?m)^  27-4-retention-verification-operations-runbook-and-a41-close-out: backlog$",
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
        self.assertEqual(["pending", "not complete"], slice_cells[-2:])
        self.assertIn(
            "pwsh ./tools/verify-access-telemetry-c1.ps1 -Gate C1.15 -ProfileId PG-ONPREM-1 "
            "-EvidenceDirectory ./artifacts/access-telemetry-c1/C1.15",
            story_text,
        )

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
            "The remaining twenty-four C1 gates stay held without a registered owner.",
            epic_context,
        )

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
        self.assertRegex(residual, r"(?m)^status: open$")

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
