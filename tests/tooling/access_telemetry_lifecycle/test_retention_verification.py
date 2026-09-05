from __future__ import annotations

from copy import deepcopy
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch


REPO_ROOT = Path(__file__).resolve().parents[3]
TOOLS_DIR = REPO_ROOT / "tools"
sys.path.insert(0, str(TOOLS_DIR))

from verify_access_telemetry_lifecycle import (  # noqa: E402
    A41_ALLOWED_MUTATION_PATHS,
    A41_PROTECTED_PATHS,
    A41_SEMANTIC_TRANSITIONS,
    REQUIRED_FAILURE_SCENARIOS,
    REQUIRED_LIFECYCLE_SIGNALS,
    REQUIRED_REPLACEMENTS,
    REQUIRED_TENANT_DENIAL_TESTS,
    STORY_27_4_PRODUCERS,
    STORY_27_4_PROFILE_SHA256,
    STORY_27_4_WORKLOAD_SHA256,
    _canonical_json,
    _read_bounded_json,
    _require_evidence_path,
    _run_bounded_process,
    _sha256,
    _validated_evidence_root,
    _validate_mutation_manifest,
    collect_a41_inventory,
    validate_story_27_4_checkpoint,
)


SHA = "a" * 64
COMMIT = "b" * 40


def now_ms() -> int:
    return int(time.time() * 1000)


def observation(command_id: str) -> dict[str, object]:
    return {"command_id": command_id, "output_sha256": SHA, "result_count": 1}


def transition() -> dict[str, object]:
    return {
        "non_production": True,
        "enable_observation": observation("qualification-enable"),
        "disable_observation": observation("qualification-disable"),
        "final_observation": observation("qualification-final-state"),
        "final_writes_state": "disabled",
    }


def bind_result_commands(payload: dict[str, object]) -> dict[str, object]:
    observations: list[dict[str, object]] = []

    def visit(value: object) -> None:
        if isinstance(value, dict):
            if set(value) == {"command_id", "output_sha256", "result_count"}:
                observations.append(value)
                return
            for nested in value.values():
                visit(nested)
        elif isinstance(value, list):
            for nested in value:
                visit(nested)

    visit(payload["results"])
    started = payload["started_utc"]
    finished = payload["finished_utc"]
    commands = payload["commands"]
    assert isinstance(commands, list)
    for item in observations:
        command_id = item["command_id"]
        arguments = {"operation": command_id}
        commands.append(
            {
                "command_id": command_id,
                "arguments": arguments,
                "arguments_sha256": _sha256(_canonical_json(arguments)),
                "started_utc_ms": started,
                "finished_utc_ms": finished,
                "exit_code": 0,
                "stdout_sha256": item["output_sha256"],
                "stderr_sha256": SHA,
                "result_count": item["result_count"],
            }
        )
    payload["result_count"] = sum(command["result_count"] for command in commands)
    return payload


def predecessor() -> dict[str, object]:
    finished = now_ms() - 1000
    gates = {
        f"C1.{index}": {
            "status": "passed",
            "artifact_path": f"c1/gate-{index}.json",
            "artifact_sha256": hashlib.sha256(f"gate-{index}\n".encode()).hexdigest(),
            "source_commit": COMMIT,
            "source_path": "tools/access_telemetry_c2_producer.py",
            "source_sha256": SHA,
            "started_utc_ms": finished - 1000,
            "finished_utc_ms": finished,
            "result_count": 1,
            "command": {
                "command_id": f"c1-gate-{index}",
                "arguments": {"gate_id": f"C1.{index}"},
                "arguments_sha256": _sha256(_canonical_json({"gate_id": f"C1.{index}"})),
                "started_utc_ms": finished - 1000,
                "finished_utc_ms": finished,
                "exit_code": 0,
                "stdout_sha256": SHA,
                "stderr_sha256": SHA,
                "result_count": 1,
            },
        }
        for index in range(1, 26)
    }
    return {
        "checkpoint": "C1",
        "status": "passed",
        "profile_sha256": STORY_27_4_PROFILE_SHA256,
        "production_lifecycle_writes": "disabled",
        "qualification_authorized": True,
        "evidence_is_approval": True,
        "gates": gates,
        "approvals": [
            {
                "role": "platform-operations",
                "reviewer": "c1-operator-reviewer",
                "state": "approved",
                "profile_sha256": STORY_27_4_PROFILE_SHA256,
            },
            {
                "role": "security",
                "reviewer": "c1-security-reviewer",
                "state": "approved",
                "profile_sha256": STORY_27_4_PROFILE_SHA256,
            },
        ],
    }


def common(checkpoint: str) -> dict[str, object]:
    finished = now_ms() - 1000
    command_id, producer_path = STORY_27_4_PRODUCERS[checkpoint]
    arguments = {"scenario_input_sha256": SHA, "target_kind": "non-production-qualification"}
    source_hashes = {
        producer_path: SHA,
        "tools/verify_access_telemetry_lifecycle.py": SHA,
    }
    if checkpoint in {
        "c2-production-replacement",
        "c3-retention-reclamation",
        "c4-failure-privacy-observability",
    }:
        source_hashes["tools/access_telemetry_producer_common.py"] = SHA
    return {
        "schema_version": 1,
        "checkpoint": checkpoint,
        "profile_sha256": STORY_27_4_PROFILE_SHA256,
        "workload_sha256": STORY_27_4_WORKLOAD_SHA256,
        "source_commit": COMMIT,
        "source_hashes": source_hashes,
        "producer": {
            "command_id": command_id,
            "path": producer_path,
            "source_sha256": SHA,
            "arguments": arguments,
            "arguments_sha256": _sha256(_canonical_json(arguments)),
        },
        "owner": "platform-operations",
        "started_utc": finished - 1000,
        "finished_utc": finished,
        "failure_count": 0,
        "skip_count": 0,
        "failures": [],
        "skipped": [],
        "commands": [
            {
                "command_id": command_id,
                "arguments": arguments,
                "arguments_sha256": _sha256(_canonical_json(arguments)),
                "started_utc_ms": finished - 1000,
                "finished_utc_ms": finished,
                "exit_code": 0,
                "stdout_sha256": SHA,
                "stderr_sha256": SHA,
                "result_count": 1,
            }
        ],
        "result_count": 1,
    }


def c2_payload() -> dict[str, object]:
    payload = common("c2-production-replacement")
    payload["results"] = {
        "writers": {
            "steady_state_minutes": 30,
            "cluster_accepted_records_per_second": 250,
            "component_operations_per_second": 500,
            "writer_results": [
                {
                    "writer": f"server-writer-{index}",
                    "attempted": 225001,
                    "acknowledged": 225000,
                    "persisted": 225000,
                    "conflicted": 1,
                    "transaction_acknowledgements": 225000,
                    "observation": observation(f"writer-{index}"),
                }
                for index in (1, 2)
            ],
            "acknowledged_loss": 0,
            "actor_serialized": True,
            "idempotent_retry": True,
            "conflict_rejected": True,
            "transaction_acknowledged": True,
            "reconstructed": True,
            "reconnected": True,
            "direct_backend_dependencies": [],
        },
        "replacements": {
            name: {
                "exercised": True,
                "recovered": True,
                "acknowledged_loss": 0,
                "continuity_observed": True,
                "observation": observation(f"replace-{name}"),
            }
            for name in REQUIRED_REPLACEMENTS
        },
        "adapter_fault": {
            "exercised": True,
            "profile_unchanged": True,
            "acknowledged_loss": 0,
            "recovered": True,
            "observation": observation("approved-adapter-fault"),
        },
        "console_continuity": True,
        "otlp_configured": True,
        "otlp_continuity": True,
        "continuity_observation": observation("continuity"),
        "qualification_transition": transition(),
    }
    return bind_result_commands(payload)


def c3_payload() -> dict[str, object]:
    payload = common("c3-retention-reclamation")
    base = now_ms() - (8 * 24 * 3_600_000)
    cohorts = []
    for hours in (1, 24, 168):
        accepted = base
        expires = accepted + (hours * 3_600_000)
        purged = expires + 60_000
        cohorts.append(
            {
                "retention_hours": hours,
                "cohort_id": f"retention-{hours}h",
                "database": "memories_access_telemetry",
                "schema": "access_telemetry",
                "table": "lifecycle_state",
                "accepted_utc_ms": accepted,
                "expires_utc_ms": expires,
                "purged_utc_ms": purged,
                "reclaimed_utc_ms": purged + 60_000,
                "pre_tuple_count": 100,
                "post_tuple_count": 0,
                "candidate_count": 100,
                "deleted_count": 90,
                "already_absent_count": 10,
                "index_removal_count": 100,
                "logical_absence": True,
                "newer_record_names": [f"newer-{hours}h-a", f"newer-{hours}h-b"],
                "newer_records_preserved": True,
                "interrupted_recovery": True,
                "restart_recovery": True,
                "allocator_bytes_before": 1000,
                "allocator_bytes_after": 700,
                "os_disk_shrink_claimed": False,
                "expiry_observation": observation(f"cohort-{hours}h-expiry"),
                "purge_observation": observation(f"cohort-{hours}h-purge"),
                "reclamation_observation": observation(f"cohort-{hours}h-reclamation"),
            }
        )
    payload["results"] = {
        "retention": {
            "maximum_clock_delta_ms": 250,
            "late_record_remaining_lifetime": True,
            "already_expired_rejected": True,
            "attestation_freshness_rejected": True,
            "attestation_replay_rejected": True,
            "attestation_identity_rejected": True,
            "logical_expiry_millisecond": True,
            "ttl_defense_in_depth": True,
        },
        "retention_observation": observation("retention-controls"),
        "cohorts": cohorts,
        "qualification_transition": transition(),
    }
    return bind_result_commands(payload)


def c4_payload() -> dict[str, object]:
    payload = common("c4-failure-privacy-observability")
    payload["results"] = {
        "failure_scenarios": {
            name: {
                "exercised": True,
                "lifecycle_fail_closed": True,
                "business_readiness_available": True,
                "business_requests": 2,
                "business_failures": 0,
                "audit_continuity": True,
                "lifecycle_attempts": 2,
                "lifecycle_persisted": 0,
                "lifecycle_rejected": 1,
                "lifecycle_dropped": 1,
                "observation": observation(f"failure-{name}"),
            }
            for name in REQUIRED_FAILURE_SCENARIOS
        },
        "observability": {
            "signals": list(REQUIRED_LIFECYCLE_SIGNALS),
            "labels": ["state", "reason", "outcome"],
            "alerts_passed": True,
            "bounded_labels": True,
            "health_precedence": True,
            "no_data_passed": True,
            "last_evidence_timestamp_gauge": True,
            "json_console_continuity": True,
            "otlp_configured": True,
            "otlp_continuity": True,
            "observation": observation("observability"),
        },
        "privacy": {
            "inspection_least_privilege": True,
            "no_tenant_read_route": True,
            "raw_values_absent": True,
            "secret_values_absent": True,
            "tenant_denial_before_dependencies": True,
            "dependency_calls_after_denial": 0,
            "tenant_denial_tests": list(REQUIRED_TENANT_DENIAL_TESTS),
            "observation": observation("privacy-denial"),
        },
        "qualification_transition": transition(),
    }
    return bind_result_commands(payload)


class RetentionVerificationTests(unittest.TestCase):
    def test_complete_c2_c3_and_c4_packets_validate(self) -> None:
        for checkpoint, payload in (
            ("c2-production-replacement", c2_payload()),
            ("c3-retention-reclamation", c3_payload()),
            ("c4-failure-privacy-observability", c4_payload()),
        ):
            validate_story_27_4_checkpoint(checkpoint, payload, predecessor())

    def test_exact_types_freshness_and_scenario_inventories_fail_closed(self) -> None:
        stale = c2_payload()
        stale["finished_utc"] = now_ms() - (16 * 60 * 1000)
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c2-production-replacement", stale, predecessor())
        future = c2_payload()
        future["started_utc"] = now_ms() + 2_000
        future["finished_utc"] = future["started_utc"]
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c2-production-replacement", future, predecessor())
        boolean_count = c2_payload()
        boolean_count["result_count"] = True
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c2-production-replacement", boolean_count, predecessor())
        float_horizon = c3_payload()
        float_horizon["results"]["cohorts"][0]["retention_hours"] = 1.0
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c3-retention-reclamation", float_horizon, predecessor())
        missing = c4_payload()
        del missing["results"]["failure_scenarios"]["application-outage"]
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c4-failure-privacy-observability", missing, predecessor())
        labels = c4_payload()
        labels["results"]["observability"]["labels"].append("tenant_id")
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c4-failure-privacy-observability", labels, predecessor())
        otlp = c2_payload()
        otlp["results"]["otlp_continuity"] = False
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c2-production-replacement", otlp, predecessor())

    def test_c1_requires_unique_25_gate_artifacts_disabled_production_and_authorization(self) -> None:
        reused = predecessor()
        reused["gates"]["C1.2"]["artifact_path"] = reused["gates"]["C1.1"]["artifact_path"]
        reused["gates"]["C1.2"]["artifact_sha256"] = reused["gates"]["C1.1"]["artifact_sha256"]
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c2-production-replacement", c2_payload(), reused)
        enabled = predecessor()
        enabled["production_lifecycle_writes"] = "enabled"
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c2-production-replacement", c2_payload(), enabled)
        unauthorized = predecessor()
        unauthorized["qualification_authorized"] = False
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c2-production-replacement", c2_payload(), unauthorized)

    def test_secret_alias_duplicate_nonfinite_and_oversized_output_fail_closed(self) -> None:
        unsafe = c4_payload()
        unsafe["results"]["privacy"]["api_key"] = "opaque-value"
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c4-failure-privacy-observability", unsafe, predecessor())
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name, content in (
                ("duplicate.json", b'{"value":1,"value":2}'),
                ("nonfinite.json", b'{"value":NaN}'),
                ("invalid.json", b'{"value":"\xff"}'),
                ("oversized.json", b'{"value":"' + (b"x" * 1_048_577) + b'"}'),
            ):
                path = root / name
                path.write_bytes(content)
                with self.assertRaises(ValueError):
                    _read_bounded_json(path)

        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaises(ValueError):
                _run_bounded_process(
                    [sys.executable, "-c", "import sys; sys.stdout.write('x' * 1048577)"],
                    cwd=Path(directory),
                    timeout_seconds=10,
                )

    def test_evidence_paths_reject_traversal_and_symlink_aliases(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            repository = base / "repository"
            evidence = base / "evidence"
            actual = evidence / "actual"
            repository.mkdir()
            actual.mkdir(parents=True)
            (actual / "packet.json").write_text("{}", encoding="utf-8")
            alias = evidence / "alias"
            alias.symlink_to(actual, target_is_directory=True)
            approved = _validated_evidence_root(evidence, repository)
            with self.assertRaises(ValueError):
                _require_evidence_path(
                    evidence / "actual" / ".." / "actual" / "packet.json",
                    approved,
                    "packet",
                    must_exist=True,
                )
            with self.assertRaises(ValueError):
                _require_evidence_path(alias / "packet.json", approved, "packet", must_exist=True)
            root_alias = base / "evidence-alias"
            root_alias.symlink_to(evidence, target_is_directory=True)
            with self.assertRaises(ValueError):
                _validated_evidence_root(root_alias, repository)

    def test_mutation_manifest_is_exact_and_verifier_owned(self) -> None:
        exact = manifest({path: SHA for path in A41_ALLOWED_MUTATION_PATHS})
        self.assertEqual(exact["paths"], _validate_mutation_manifest(exact))
        empty = deepcopy(exact)
        empty["semantics"][A41_ALLOWED_MUTATION_PATHS[0]]["required"] = []
        with self.assertRaises(ValueError):
            _validate_mutation_manifest(empty)
        drift = deepcopy(exact)
        drift["semantics"][A41_ALLOWED_MUTATION_PATHS[0]]["required"][0] = "weaker"
        with self.assertRaises(ValueError):
            _validate_mutation_manifest(drift)

    def test_producer_restores_disabled_state_when_body_fails_after_enable(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            fake_bin = base / "bin"
            fake_bin.mkdir()
            install_fake_kubectl(fake_bin)
            operation_log = base / "operations.log"
            scenario_input = base / "scenario-input.json"
            scenario_input.write_text(json.dumps({"schema_version": 1, "target": {
                "kind": "non-production-qualification", "kube_context": "operator@local",
                "namespace": "memories-qualification", "profile_sha256": STORY_27_4_PROFILE_SHA256,
            }}), encoding="utf-8")
            environment = {
                **os.environ,
                "PATH": f"{fake_bin}{os.pathsep}{os.environ['PATH']}",
                "QUALIFICATION_FAIL_OPERATION": "writer-1",
                "QUALIFICATION_OPERATION_LOG": str(operation_log),
            }

            result = subprocess.run(
                [sys.executable, "-B", str(TOOLS_DIR / "access_telemetry_c2_producer.py"),
                 "--scenario-input", str(scenario_input)],
                check=False,
                capture_output=True,
                text=True,
                env=environment,
            )

            self.assertEqual(1, result.returncode)
            operations = operation_log.read_text(encoding="utf-8").splitlines()
            self.assertEqual(
                ["qualification-enable", "writer-1", "qualification-disable", "qualification-final-state"],
                operations,
            )

    def test_producer_restores_disabled_state_when_enable_response_is_malformed(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            fake_bin = base / "bin"
            fake_bin.mkdir()
            install_fake_kubectl(fake_bin)
            operation_log = base / "operations.log"
            scenario_input = base / "scenario-input.json"
            scenario_input.write_text(json.dumps({"schema_version": 1, "target": {
                "kind": "non-production-qualification", "kube_context": "operator@local",
                "namespace": "memories-qualification", "profile_sha256": STORY_27_4_PROFILE_SHA256,
            }}), encoding="utf-8")
            environment = {
                **os.environ,
                "PATH": f"{fake_bin}{os.pathsep}{os.environ['PATH']}",
                "QUALIFICATION_INVALID_OPERATION": "qualification-enable",
                "QUALIFICATION_OPERATION_LOG": str(operation_log),
            }

            result = subprocess.run(
                [sys.executable, "-B", str(TOOLS_DIR / "access_telemetry_c2_producer.py"),
                 "--scenario-input", str(scenario_input)],
                check=False,
                capture_output=True,
                text=True,
                env=environment,
            )

            self.assertEqual(1, result.returncode)
            self.assertEqual(
                ["qualification-enable", "qualification-disable", "qualification-final-state"],
                operation_log.read_text(encoding="utf-8").splitlines(),
            )

    def test_a41_inventory_hashes_authenticated_head_blobs(self) -> None:
        inventory = collect_a41_inventory(REPO_ROOT)
        by_path = {item["path"]: item for item in inventory["references"]}
        self.assertEqual(
            "historical-or-orchestrator-read-only",
            by_path["_bmad-output/implementation-artifacts/sprint-status.yaml"]["classification"],
        )
        self.assertEqual(
            "close-out-mutable",
            by_path["_bmad-output/implementation-artifacts/deferred-work.md"]["classification"],
        )

    def test_registered_producers_and_complete_close_out_chain(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            repository = base / "repository"
            evidence = base / "evidence"
            remote = base / "remote.git"
            fake_bin = base / "bin"
            repository.mkdir()
            evidence.mkdir()
            fake_bin.mkdir()
            self._run("git", "init", "-q", "-b", "main", str(repository))
            self._run("git", "init", "-q", "--bare", str(remote))
            self._run("git", "-C", str(repository), "config", "user.email", "test@example.invalid")
            self._run("git", "-C", str(repository), "config", "user.name", "Test")
            self._run("git", "-C", str(repository), "remote", "add", "origin", str(remote))
            install_tools(repository)
            install_a41_files(repository, "A41 open\n")
            install_fake_kubectl(fake_bin)
            self._run("git", "-C", str(repository), "add", ".")
            self._run("git", "-C", str(repository), "commit", "-q", "-m", "test: create source baseline")
            commit = self._run("git", "-C", str(repository), "rev-parse", "HEAD", capture=True)
            c1_path = write_c1(evidence, repository, commit)
            scenario_input = evidence / "scenario-input.json"
            scenario_input.write_text(json.dumps({"schema_version": 1, "target": {
                "kind": "non-production-qualification", "kube_context": "operator@local",
                "namespace": "memories-qualification", "profile_sha256": STORY_27_4_PROFILE_SHA256,
            }}), encoding="utf-8")
            artifacts: dict[str, Path] = {"C1": c1_path}
            with patch.dict(os.environ, {"PATH": f"{fake_bin}{os.pathsep}{os.environ['PATH']}"}):
                adapter_result = self._cli(
                    repository,
                    "--checkpoint", "adapter-profile",
                    "--kube-context", "operator@local",
                    "--namespace", "hexalith-memories",
                    "--deployment-id", "deployment-27-4-test",
                    "--profile-id", "postgresql-v2-dapr-1.18.1-postgresql-18.4-onprem-k8s1-openebs-local-retain-400g-v1",
                    "--workload-profile", "adr-27.1-two-writer-500eps",
                    "--steady-state-minutes", "30",
                    "--purge-backlog-records", "150000",
                    "--declared-single-component-fault", "postgresql-pod-replacement",
                    "--evidence-root", str(evidence),
                    "--evidence", str(evidence / "adapter-cli.md"))
                self.assertEqual(1, adapter_result.returncode)
                c2_source = repository / STORY_27_4_PRODUCERS["c2-production-replacement"][1]
                reviewed_source = c2_source.read_bytes()
                c2_source.write_bytes(reviewed_source + b"\n# unreviewed drift\n")
                drift_result = self._cli(
                    repository,
                    "--checkpoint", "c2-production-replacement",
                    "--scenario-input", str(scenario_input),
                    "--predecessor", str(c1_path),
                    "--owner", "operator",
                    "--evidence-root", str(evidence),
                    "--evidence", str(evidence / "C2-source-drift.json"))
                self.assertEqual(1, drift_result.returncode)
                c2_source.write_bytes(reviewed_source)
                for name, checkpoint in (("C2", "c2-production-replacement"),
                                         ("C3", "c3-retention-reclamation"),
                                         ("C4", "c4-failure-privacy-observability")):
                    output = evidence / f"{name}.json"
                    result = self._cli(
                        repository,
                        "--checkpoint", checkpoint,
                        "--scenario-input", str(scenario_input),
                        "--predecessor", str(c1_path),
                        "--owner", "operator",
                        "--evidence-root", str(evidence),
                        "--evidence", str(output))
                    self.assertEqual(0, result.returncode, result.stderr)
                    artifacts[name] = output
                adapter_path = evidence / "adapter-profile.json"
                c0_path = evidence / "C0.json"
                c0_result = self._cli(
                    repository,
                    "--checkpoint", "adapter-profile",
                    "--kube-context", "operator@local",
                    "--namespace", "hexalith-memories-qualification",
                    "--deployment-id", "deployment-27-4-test",
                    "--profile-id", "postgresql-v2-dapr-1.18.1-postgresql-18.4-onprem-k8s1-openebs-local-retain-400g-v1",
                    "--workload-profile", "adr-27.1-two-writer-500eps",
                    "--steady-state-minutes", "30",
                    "--purge-backlog-records", "150000",
                    "--declared-single-component-fault", "postgresql-pod-replacement",
                    "--evidence-root", str(evidence),
                    "--evidence", str(adapter_path),
                    "--c0-wrapper", str(c0_path))
                self.assertEqual(
                    0,
                    c0_result.returncode,
                    f"stdout: {c0_result.stdout}\nstderr: {c0_result.stderr}",
                )
                self.assertEqual("passed", json.loads(adapter_path.read_text(encoding="utf-8"))["status"])
                self.assertEqual("C0", json.loads(c0_path.read_text(encoding="utf-8"))["checkpoint"])
                artifacts["C0"] = c0_path
            offline_input = evidence / "offline-C2-input.json"
            offline_input.write_text(json.dumps(c2_payload()), encoding="utf-8")
            offline_result = self._cli(
                repository,
                "--checkpoint", "c2-production-replacement",
                "--input", str(offline_input),
                "--predecessor", str(c1_path),
                "--owner", "operator",
                "--evidence-root", str(evidence),
                "--evidence", str(evidence / "offline-C2-rejection.json"))
            self.assertEqual(1, offline_result.returncode)
            artifacts.update(write_terminal_artifacts(evidence, repository, commit, artifacts))
            bundle_path = evidence / "bundle.json"
            bundle = {"profile_sha256": STORY_27_4_PROFILE_SHA256,
                "checkpoints": {name: {"status": "passed", "profile_sha256": STORY_27_4_PROFILE_SHA256,
                    "artifact_path": str(path.relative_to(evidence)),
                    "artifact_sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
                    for name, path in artifacts.items()}}
            bundle_path.write_text(json.dumps(bundle), encoding="utf-8")
            desired = {path: "\n".join(A41_SEMANTIC_TRANSITIONS[path]["required"]) + "\n"
                       for path in A41_ALLOWED_MUTATION_PATHS}
            manifest_path = evidence / "manifest.json"
            manifest_path.write_text(json.dumps(manifest({path: hashlib.sha256(content.encode()).hexdigest()
                                                          for path, content in desired.items()})), encoding="utf-8")
            snapshot_path = evidence / "snapshot.json"
            preflight_path = evidence / "preflight.json"
            inventory_path = evidence / "inventory.json"
            inventory_result = self._cli(
                repository,
                "--checkpoint", "a41-inventory",
                "--evidence-root", str(evidence),
                "--evidence", str(inventory_path))
            self.assertEqual(0, inventory_result.returncode, inventory_result.stderr)
            tampered_bundle = deepcopy(bundle)
            tampered_bundle["checkpoints"]["C2"]["artifact_sha256"] = SHA
            tampered_bundle_path = evidence / "tampered-bundle.json"
            tampered_bundle_path.write_text(json.dumps(tampered_bundle), encoding="utf-8")
            tampered_result = self._cli(
                repository,
                "--checkpoint", "close-out-preflight",
                "--bundle", str(tampered_bundle_path),
                "--mutation-manifest", str(manifest_path),
                "--snapshot", str(evidence / "tampered-snapshot.json"),
                "--evidence-root", str(evidence),
                "--evidence", str(evidence / "tampered-preflight.json"),
                "--remote", "origin",
                "--branch", "main")
            self.assertEqual(1, tampered_result.returncode)
            dirty_path = repository / "dirty.txt"
            dirty_path.write_text("untracked\n", encoding="utf-8")
            dirty_result = self._cli(
                repository,
                "--checkpoint", "close-out-preflight",
                "--bundle", str(bundle_path),
                "--mutation-manifest", str(manifest_path),
                "--snapshot", str(evidence / "dirty-snapshot.json"),
                "--evidence-root", str(evidence),
                "--evidence", str(evidence / "dirty-preflight.json"),
                "--remote", "origin",
                "--branch", "main")
            self.assertEqual(1, dirty_result.returncode)
            dirty_path.unlink()
            preflight_result = self._cli(
                repository,
                "--checkpoint", "close-out-preflight",
                "--bundle", str(bundle_path),
                "--mutation-manifest", str(manifest_path),
                "--snapshot", str(snapshot_path),
                "--evidence-root", str(evidence),
                "--evidence", str(preflight_path),
                "--remote", "origin",
                "--branch", "main")
            self.assertEqual(0, preflight_result.returncode, preflight_result.stderr)
            for relative, content in desired.items():
                (repository / relative).write_text(content, encoding="utf-8")
            self._run("git", "-C", str(repository), "add", *A41_ALLOWED_MUTATION_PATHS)
            postflight_path = evidence / "postflight.json"
            postflight_result = self._cli(
                repository,
                "--checkpoint", "close-out-postflight",
                "--preflight", str(preflight_path),
                "--mutation-manifest", str(manifest_path),
                "--snapshot", str(snapshot_path),
                "--evidence-root", str(evidence),
                "--evidence", str(postflight_path))
            self.assertEqual(0, postflight_result.returncode, postflight_result.stderr)
            self._run("git", "-C", str(repository), "commit", "-q", "-m", "test: close residual")
            close_out_commit = self._run("git", "-C", str(repository), "rev-parse", "HEAD", capture=True)
            (repository / "descendant.txt").write_text("advanced remote tip\n", encoding="utf-8")
            self._run("git", "-C", str(repository), "add", "descendant.txt")
            self._run("git", "-C", str(repository), "commit", "-q", "-m", "test: advance remote")
            unpublished_path = evidence / "unpublished.json"
            unpublished_result = self._cli(
                repository,
                "--checkpoint", "publish-verification",
                "--preflight", str(preflight_path),
                "--postflight", str(postflight_path),
                "--mutation-manifest", str(manifest_path),
                "--snapshot", str(snapshot_path),
                "--commit", close_out_commit,
                "--remote", "origin",
                "--branch", "main",
                "--evidence-root", str(evidence),
                "--evidence", str(unpublished_path))
            self.assertEqual(1, unpublished_result.returncode)
            self._run("git", "-C", str(repository), "push", "-q", "origin", "main")
            publish_path = evidence / "publish.json"
            publish_result = self._cli(
                repository,
                "--checkpoint", "publish-verification",
                "--preflight", str(preflight_path),
                "--postflight", str(postflight_path),
                "--mutation-manifest", str(manifest_path),
                "--snapshot", str(snapshot_path),
                "--commit", close_out_commit,
                "--remote", "origin",
                "--branch", "main",
                "--evidence-root", str(evidence),
                "--evidence", str(publish_path))
            self.assertEqual(0, publish_result.returncode, publish_result.stderr)
            packet = json.loads(publish_path.read_text(encoding="utf-8"))
            self.assertEqual("published-close-out-verified", packet["a41_status"])

            wrong_branch_result = self._cli(
                repository,
                "--checkpoint", "publish-verification",
                "--preflight", str(preflight_path),
                "--postflight", str(postflight_path),
                "--mutation-manifest", str(manifest_path),
                "--snapshot", str(snapshot_path),
                "--commit", close_out_commit,
                "--remote", "origin",
                "--branch", "other",
                "--evidence-root", str(evidence),
                "--evidence", str(evidence / "wrong-branch.json"))
            self.assertEqual(1, wrong_branch_result.returncode)
            self._run("git", "-C", str(repository), "remote", "set-url", "origin", str(base / "remapped.git"))
            remapped_result = self._cli(
                repository,
                "--checkpoint", "publish-verification",
                "--preflight", str(preflight_path),
                "--postflight", str(postflight_path),
                "--mutation-manifest", str(manifest_path),
                "--snapshot", str(snapshot_path),
                "--commit", close_out_commit,
                "--remote", "origin",
                "--branch", "main",
                "--evidence-root", str(evidence),
                "--evidence", str(evidence / "remapped-remote.json"))
            self.assertEqual(1, remapped_result.returncode)

    @staticmethod
    def _run(*args: str, capture: bool = False) -> str:
        result = subprocess.run(args, check=True, capture_output=capture, text=True)
        return result.stdout.strip() if capture else ""

    @staticmethod
    def _cli(repository: Path, *args: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [sys.executable, "-B", str(repository / "tools/verify-access-telemetry-lifecycle.py"),
             "--repository-root", str(repository), *args],
            check=False,
            capture_output=True,
            text=True,
        )


def manifest(paths: dict[str, str]) -> dict[str, object]:
    return {"paths": paths, "semantics": {path: {
        "required": list(A41_SEMANTIC_TRANSITIONS[path]["required"]),
        "forbidden": list(A41_SEMANTIC_TRANSITIONS[path]["forbidden"]),
    } for path in A41_ALLOWED_MUTATION_PATHS}}


def install_tools(repository: Path) -> None:
    destination = repository / "tools"
    destination.mkdir()
    for name in ("verify-access-telemetry-lifecycle.py", "verify_access_telemetry_lifecycle.py",
                 "access_telemetry_producer_common.py", "access_telemetry_c2_producer.py",
                 "access_telemetry_c3_producer.py", "access_telemetry_c4_producer.py"):
        shutil.copy2(TOOLS_DIR / name, destination / name)


def install_a41_files(repository: Path, content: str) -> None:
    for relative in (*A41_ALLOWED_MUTATION_PATHS, *A41_PROTECTED_PATHS):
        path = repository / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")


def write_c1(evidence: Path, repository: Path, commit: str) -> Path:
    value = predecessor()
    source = repository / "tools/access_telemetry_c2_producer.py"
    source_sha = hashlib.sha256(source.read_bytes()).hexdigest()
    for index, gate in enumerate(value["gates"].values(), 1):
        artifact = evidence / f"c1/gate-{index}.json"
        artifact.parent.mkdir(exist_ok=True)
        artifact.write_text(f"gate-{index}\n", encoding="utf-8")
        gate.update({"artifact_sha256": hashlib.sha256(artifact.read_bytes()).hexdigest(),
                     "source_commit": commit, "source_path": "tools/access_telemetry_c2_producer.py",
                     "source_sha256": source_sha})
    path = evidence / "C1.json"
    path.write_text(json.dumps(value), encoding="utf-8")
    return path


def chain_common(checkpoint: str, repository: Path, commit: str) -> dict[str, object]:
    value = common(checkpoint)
    producer_path = STORY_27_4_PRODUCERS[checkpoint][1]
    source_sha = hashlib.sha256((repository / producer_path).read_bytes()).hexdigest()
    value["source_commit"] = commit
    value["source_hashes"] = {
        path: hashlib.sha256((repository / path).read_bytes()).hexdigest()
        for path in value["source_hashes"]
    }
    value["producer"]["source_sha256"] = source_sha
    value["status"] = "passed"
    return value


def write_terminal_artifacts(evidence: Path, repository: Path, commit: str,
                             artifacts: dict[str, Path]) -> dict[str, Path]:
    if "C0" not in artifacts:
        raise AssertionError("terminal fixture requires the subprocess-produced C0 artifact")
    accepted = {name: hashlib.sha256(artifacts[name].read_bytes()).hexdigest()
                for name in ("C0", "C1", "C2", "C3", "C4")}
    result: dict[str, Path] = {}
    for name, role, approver in (("C5", "platform-operations", "post-evidence-operator"),
                                 ("C6", "security", "post-evidence-security")):
        payload = chain_common(name, repository, commit)
        payload["results"] = {"role": role, "approver": approver,
                              "approved_utc_ms": now_ms() - 1000,
                              "accepted_checkpoint_hashes": accepted}
        path = evidence / f"{name}.json"
        path.write_text(json.dumps(payload), encoding="utf-8")
        result[name] = path
        artifacts[name] = path
    terminal = chain_common("terminal", repository, commit)
    terminal["results"] = {"checkpoint_hashes": {
        name: hashlib.sha256(artifacts[name].read_bytes()).hexdigest()
        for name in ("C0", "C1", "C2", "C3", "C4", "C5", "C6")},
        "failure_count": 0, "skip_count": 0, "result_count": 7}
    terminal_path = evidence / "terminal.json"
    terminal_path.write_text(json.dumps(terminal), encoding="utf-8")
    result["terminal"] = terminal_path
    return result


def install_fake_kubectl(directory: Path) -> None:
    script = directory / "kubectl"
    script.write_text("""#!/usr/bin/env python3
import json, os, sys, time
args = sys.argv[1:]
if '--' in args and args[-1] in {'--version', '--build-info'} and args[-2] in {'/daprd', 'daprd'}:
    print('1.18.1' if args[-1] == '--version' else 'Version: 1.18.1\\nGit Commit: qualification-test')
    raise SystemExit(0)
if 'get' in args and args[-1] == 'json':
    resource = args[args.index('get') + 1]
    postgres_image = 'docker.io/library/postgres:18.4-trixie@sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a'
    items = {
        'deployments': [
            {'metadata': {'name': 'memories-access-telemetry', 'generation': 1},
             'spec': {'replicas': 1, 'template': {'spec': {'containers': [{'name': 'lifecycle', 'image': 'registry/access-telemetry@sha256:aa'}]}}},
             'status': {'readyReplicas': 1, 'availableReplicas': 1}},
            {'metadata': {'name': 'memories', 'generation': 1},
             'spec': {'replicas': 2, 'template': {'spec': {'containers': [{'name': 'memories', 'image': 'registry/memories@sha256:bb'}]}}},
             'status': {'readyReplicas': 2, 'availableReplicas': 2}},
        ],
        'components.dapr.io': [
            {'metadata': {'name': 'access-telemetry-store', 'generation': 1},
             'spec': {'type': 'state.postgresql', 'version': 'v2',
                      'metadata': [{'name': 'maxConns', 'value': '40'}]}},
        ],
        'configurations.dapr.io': [
            {'metadata': {'name': 'memories-access-telemetry-config', 'generation': 1},
             'spec': {'accessControl': {'defaultAction': 'deny', 'policies': []}}},
        ],
        'statefulsets': [
            {'metadata': {'name': 'access-telemetry-postgresql', 'generation': 1},
             'spec': {'replicas': 1, 'template': {'spec': {'containers': [{'name': 'postgresql', 'image': postgres_image}]}}},
             'status': {'readyReplicas': 1}},
        ],
        'pods': [
            {'metadata': {'name': 'memories-1', 'generation': 1},
             'spec': {'nodeName': 'qualification-node'}, 'status': {'phase': 'Running', 'containerStatuses': [
                 {'name': 'memories', 'image': 'registry/memories:1', 'imageID': 'registry/memories@sha256:bb'},
                 {'name': 'daprd', 'image': 'ghcr.io/dapr/daprd:1.18.1', 'imageID': 'ghcr.io/dapr/daprd@sha256:cc'},
             ]}},
        ],
    }.get(resource, [])
    print(json.dumps({'items': items}, separators=(',', ':')))
    raise SystemExit(0)
op = sys.argv[-1].rsplit('/', 1)[-1]
operation_log = os.environ.get('QUALIFICATION_OPERATION_LOG')
if operation_log:
    with open(operation_log, 'a', encoding='utf-8') as stream:
        stream.write(op + '\\n')
if os.environ.get('QUALIFICATION_FAIL_OPERATION') == op:
    raise SystemExit(9)
if os.environ.get('QUALIFICATION_INVALID_OPERATION') == op:
    print('{')
    raise SystemExit(0)
now = int(time.time() * 1000)
if op in {'qualification-enable', 'qualification-disable', 'qualification-final-state'}:
    value = {'state': 'enabled' if op == 'qualification-enable' else 'disabled'}
elif op.startswith('writer-'):
    index = int(op[-1]); value = {'writer': f'server-writer-{index}', 'attempted': 225001,
        'acknowledged': 225000, 'persisted': 225000, 'conflicted': 1, 'transaction_acknowledgements': 225000}
elif op.startswith('replace-'):
    value = {'exercised': True, 'recovered': True, 'acknowledged_loss': 0, 'continuity_observed': True}
elif op == 'approved-adapter-fault':
    value = {'exercised': True, 'profile_unchanged': True, 'acknowledged_loss': 0, 'recovered': True}
elif op == 'continuity':
    value = {'component_operations_per_second': 500, 'acknowledged_loss': 0, 'actor_serialized': True,
        'idempotent_retry': True, 'conflict_rejected': True, 'transaction_acknowledged': True,
        'reconstructed': True, 'reconnected': True, 'direct_backend_dependencies': [],
        'console_continuity': True, 'otlp_configured': True, 'otlp_continuity': True}
elif op == 'retention-controls':
    value = {'maximum_clock_delta_ms': 250, 'late_record_remaining_lifetime': True,
        'already_expired_rejected': True, 'attestation_freshness_rejected': True,
        'attestation_replay_rejected': True, 'attestation_identity_rejected': True,
        'logical_expiry_millisecond': True, 'ttl_defense_in_depth': True}
elif op.startswith('cohort-'):
    hours = int(op.split('-')[1][:-1]); accepted = (now // 3600000) * 3600000 - (8 * 24 * 3600000)
    expires = accepted + hours * 3600000; purged = expires + 60000
    value = {'retention_hours': hours, 'cohort_id': f'retention-{hours}h',
        'database': 'memories_access_telemetry', 'schema': 'access_telemetry', 'table': 'lifecycle_state',
        'accepted_utc_ms': accepted, 'expires_utc_ms': expires, 'purged_utc_ms': purged,
        'reclaimed_utc_ms': purged + 60000, 'pre_tuple_count': 100, 'post_tuple_count': 0,
        'candidate_count': 100, 'deleted_count': 90, 'already_absent_count': 10,
        'index_removal_count': 100, 'logical_absence': True, 'newer_record_names': [f'newer-{hours}h-a'],
        'newer_records_preserved': True, 'interrupted_recovery': True, 'restart_recovery': True,
        'allocator_bytes_before': 1000, 'allocator_bytes_after': 700, 'os_disk_shrink_claimed': False}
elif op.startswith('failure-'):
    value = {'exercised': True, 'lifecycle_fail_closed': True, 'business_readiness_available': True,
        'business_requests': 2, 'business_failures': 0, 'audit_continuity': True, 'lifecycle_attempts': 2,
        'lifecycle_persisted': 0, 'lifecycle_rejected': 1, 'lifecycle_dropped': 1}
elif op == 'observability':
    value = {'signals': ['accepted','dropped','enqueued','expired','failed','persisted','purged','rejected','retried'],
        'labels': ['state','reason','outcome'], 'alerts_passed': True, 'bounded_labels': True,
        'health_precedence': True, 'no_data_passed': True, 'last_evidence_timestamp_gauge': True,
        'json_console_continuity': True, 'otlp_configured': True, 'otlp_continuity': True}
elif op == 'privacy-denial':
    value = {'inspection_least_privilege': True, 'no_tenant_read_route': True, 'raw_values_absent': True,
        'secret_values_absent': True, 'tenant_denial_before_dependencies': True, 'dependency_calls_after_denial': 0,
        'tenant_denial_tests': ['SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies',
        'TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState',
        'TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies',
        'VerifyAsync_DetectsMissingSemanticTenantId_ReturnsFailed', 'VerifyAsync_DetectsSemanticTenantIdMismatch_ReturnsFailed',
        'VerifyAsync_DetectsSyntacticTenantIdMismatch_ReturnsFailed']}
else: raise SystemExit(2)
value['result_count'] = 1
print(json.dumps(value, separators=(',', ':')))
""", encoding="utf-8")
    script.chmod(0o755)


if __name__ == "__main__":
    unittest.main()
