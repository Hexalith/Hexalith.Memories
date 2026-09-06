from __future__ import annotations

import base64
from copy import deepcopy
import hashlib
import json
import os
from pathlib import Path
import re
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
    EvidenceValidationError,
    REQUIRED_FAILURE_SCENARIOS,
    REQUIRED_LIFECYCLE_SIGNALS,
    REQUIRED_REPLACEMENTS,
    REQUIRED_TENANT_DENIAL_TESTS,
    STORY_27_4_PRODUCERS,
    STORY_27_4_PROFILE_SHA256,
    STORY_27_4_WORKLOAD_SHA256,
    _EvidenceAggregateBudget,
    _authenticate_snapshot,
    _canonical_json,
    _collect_a41_inventory_and_blobs,
    _read_bounded_json,
    _require_evidence_path,
    _run_bounded_process,
    _sha256,
    _validated_evidence_root,
    _validate_mutation_manifest,
    _validate_predecessor,
    _write_json_exclusive,
    collect_a41_inventory,
    create_recoverable_snapshot,
    validate_story_27_4_checkpoint,
)
from access_telemetry_producer_common import (  # noqa: E402
    _C3Journal,
    _TERMINATION_REQUESTED,
    _execute_qualification,
    _load_business_bearer,
    _qualification_record_ids,
    _run_operation,
    _run_writer_segments,
)
import access_telemetry_producer_common as producer_common  # noqa: E402


SHA = "a" * 64
COMMIT = "b" * 40


def now_ms() -> int:
    return int(time.time() * 1000)


def write_business_bearer(path: Path) -> None:
    def encoded(value: dict[str, object]) -> str:
        return base64.urlsafe_b64encode(
            json.dumps(value, separators=(",", ":")).encode("utf-8")
        ).rstrip(b"=").decode("ascii")

    token = ".".join((
        encoded({"alg": "none", "typ": "JWT"}),
        encoded({"tenant_id": "story-27-4-qualification", "exp": int(time.time()) + 1800}),
        "test-signature",
    ))
    path.write_text(token, encoding="ascii")
    path.chmod(0o600)


def observation(command_id: str) -> dict[str, object]:
    return {"command_id": command_id, "output_sha256": SHA, "result_count": 1}


def transition(command_id: str | None = None) -> dict[str, object]:
    prefix = f"{command_id}:" if command_id else ""
    return {
        "non_production": True,
        "identity_observation": observation(prefix + "qualification-target-identity"),
        "initial_writes_state": "disabled",
        "enable_observation": observation(prefix + "qualification-enable"),
        "disable_observation": observation(prefix + "qualification-disable"),
        "final_observation": observation(prefix + "qualification-final-state"),
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
        source_hashes["deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml"] = SHA
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
    interval_end = now_ms() - 2_000
    interval_start = interval_end - 1_800_000
    post_segment = {
        "segment_id": "writer-1-segment-0002",
        "writer": "writer-1",
        "writer_pod": "memories-1",
        "started_utc_ms": interval_start + 2_000,
        "finished_utc_ms": interval_start + 3_000,
        "record_inventory_sha256": SHA,
        "durable_count": 125,
    }
    payload["results"] = {
        "writers": {
            "steady_state_minutes": 30,
            "cluster_accepted_records_per_second": 250,
            "component_operations_per_second": 500,
            "component_counter": {
                "counter_name": "memories_access_telemetry_lifecycle_state_operations_total",
                "window_milliseconds": 1_800_000,
                "operation_delta": 900_000,
                "observation": observation("component-throughput"),
            },
            "overlap_milliseconds": 1_800_000,
            "writer_results": [
                {
                    "writer": f"server-writer-{index}",
                    "started_utc_ms": interval_start,
                    "finished_utc_ms": interval_end,
                    "segment_count": 1_800,
                    "replayed_segment_count": 0,
                    "dispatch_lag_max_milliseconds": 0,
                    "segment_inventory_sha256": SHA,
                    "record_inventory_sha256": SHA,
                    "attempted": 225000,
                    "enqueued": 225000,
                    "acknowledged": 225000,
                    "persisted": 225000,
                    "conflicted": 0,
                    "transaction_acknowledgements": 225000,
                    "observation": observation(f"writer-{index}"),
                }
                for index in (1, 2)
            ],
            "acknowledged_loss": 0,
            "actor_serialized": True,
            "idempotent_retry": True,
            "conflict_rejected": True,
            "idempotence_conflict_observation": observation("idempotence-conflict-proof"),
            "transaction_acknowledged": True,
            "reconstructed": True,
            "reconnected": True,
            "direct_backend_dependencies": [],
        },
        "replacements": {
            name: {
                "exercised": True,
                "recovered": True,
                "before_runtime_identity_sha256": SHA,
                "after_runtime_identity_sha256": "b" * 64,
                "mutation_finished_utc_ms": interval_start + 1_000,
                "post_replacement_segment": dict(post_segment),
                "acknowledged_loss": 0,
                "continuity_observed": True,
                "observation": observation(f"replace-{name}"),
            }
            for name in REQUIRED_REPLACEMENTS
        },
        "adapter_fault": {
            "exercised": True,
            "profile_unchanged": True,
            "before_runtime_identity_sha256": SHA,
            "after_runtime_identity_sha256": "b" * 64,
            "mutation_finished_utc_ms": interval_start + 1_000,
            "post_replacement_segment": dict(post_segment),
            "acknowledged_loss": 0,
            "recovered": True,
            "observation": observation("approved-adapter-fault"),
        },
        "console_continuity": True,
        "otlp_configured": True,
        "otlp_continuity": True,
        "continuity_observation": observation("continuity"),
        "qualification_transition": transition(),
        "renew_observations": [observation("qualification-renew:1")],
    }
    return bind_result_commands(payload)


def c3_payload() -> dict[str, object]:
    payload = common("c3-retention-reclamation")
    base = now_ms() - (8 * 24 * 3_600_000)
    cohorts = []
    for hours in (1, 24, 168):
        emitted = base
        accepted = emitted + 25
        expires = emitted + (hours * 3_600_000)
        purged = expires + 60_000
        cohorts.append(
            {
                "retention_hours": hours,
                "cohort_id": f"retention-{hours}h",
                "database": "memories_access_telemetry",
                "schema": "access_telemetry",
                "table": "lifecycle_state",
                "record_ids": [f"{hours * 1000 + index:026d}" for index in range(1, 126)],
                "accepted_utc_ms": accepted,
                "emitted_utc_ms": emitted,
                "expires_utc_ms": expires,
                "purged_utc_ms": purged,
                "reclaimed_utc_ms": purged + 60_000,
                "pre_tuple_count": 125,
                "post_tuple_count": 0,
                "candidate_count": 125,
                "deleted_count": 115,
                "already_absent_count": 10,
                "index_removal_count": 125,
                "strong_absent_read_count": 125,
                "logical_absence": True,
                "newer_record_names": [f"{index:026d}" for index in range(1, 126)],
                "newer_records_preserved": True,
                "interrupted_recovery": True,
                "restart_recovery": True,
                "allocator_free_bytes_before": 100,
                "allocator_free_bytes_after": 700,
                "os_disk_shrink_claimed": False,
                "seed_observation": observation(f"cohort-{hours}h-seed"),
                "wait_observation": observation(f"cohort-{hours}h-wait"),
                "expiry_observation": observation(f"cohort-{hours}h-expiry"),
                "purge_observation": observation(f"cohort-{hours}h-purge"),
                "reclamation_observation": observation(f"cohort-{hours}h-reclamation"),
                "seed_transition": transition(f"cohort-{hours}h-seed"),
                "purge_transition": transition(f"cohort-{hours}h-purge"),
                "reclamation_transition": transition(f"cohort-{hours}h-reclamation"),
            }
        )
    payload["results"] = {
        "runtime_identity_sha256": SHA,
        "runtime_identity_observation": observation("c3-runtime-identity"),
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
        "retention_transition": transition("retention-controls"),
        "empty_preflight_observation": observation("c3-empty-preflight"),
        "final_newer_control": {
            "record_count": 125,
            "newer_record_names": [f"{index:026d}" for index in range(1, 126)],
            "observation": observation("newer-control-seed"),
            "transition": transition("newer-control-seed"),
        },
        "physical_report": {
            "reported": True,
            "artifact_sha256": SHA,
            "reporter_image_digest": SHA,
            "observation": observation("cohort-168h-report"),
            "transition": transition("cohort-168h-report"),
        },
        "cohorts": cohorts,
    }
    return bind_result_commands(payload)


def c4_payload() -> dict[str, object]:
    payload = common("c4-failure-privacy-observability")
    expected = {
        **{name: "persisted" for name in (
            "actor-failover", "application-outage", "approved-adapter-fault",
            "capacity-pressure", "clock-outage", "dapr-outage", "reconnect",
            "reminder-delay", "shutdown", "state-outage",
        )},
        **{name: "dropped" for name in (
            "queue-byte-exhaustion", "queue-record-exhaustion", "retry-exhaustion",
        )},
        **{name: "rejected" for name in (
            "bad-configuration", "bad-key", "degraded-rollback", "etag-failure",
            "profile-drift", "stale-attestation", "ttl-failure", "transaction-failure",
        )},
    }
    payload["results"] = {
        "failure_scenarios": {
            name: {
                "exercised": True,
                "expected_disposition": expected[name],
                "business_operation_succeeded": True,
                "business_requests": 2,
                "business_failures": 0,
                "audit_continuity": True,
                "lifecycle_attempts": 2,
                "lifecycle_persisted": 2 if expected[name] == "persisted" else 0,
                "lifecycle_rejected": 2 if expected[name] == "rejected" else 0,
                "lifecycle_dropped": 2 if expected[name] == "dropped" else 0,
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
    def test_c3_reporter_resume_reuses_exact_completed_job_receipt(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            fake_bin = Path(directory) / "bin"
            fake_bin.mkdir()
            install_fake_kubectl(fake_bin)
            reclaimed = 1_700_000_000_000
            target = {
                "kube_context": "operator@local", "namespace": "memories-qualification",
                "_platform_operations_reviewer": "reviewer",
                "_lease_holder": "story-27-4/reviewer/test",
                "_reporter_image": "registry/reporter@sha256:" + ("d" * 64),
                "_c3_reporter_artifact_sha256": SHA,
                "_c3_reclamation_observation": {
                    "reclaimed_utc_ms": reclaimed, "allocator_free_bytes": 700,
                },
            }
            with patch.dict(os.environ, qualification_test_env(
                fake_bin,
                QUALIFICATION_COMPLETED_REPORTER="1",
                QUALIFICATION_ARTIFACT=SHA,
                QUALIFICATION_RECLAIMED=str(reclaimed),
            )):
                result, _ = _run_operation(
                    target, "c3-retention-reclamation", "cohort-168h-report"
                )

            self.assertTrue(result["reported"])
            self.assertEqual(SHA, result["artifact_sha256"])
            self.assertTrue(target["_reuse_completed_reporter"])

    def test_c2_rejects_accepted_count_below_emitted_inventory(self) -> None:
        payload = c2_payload()
        writer = payload["results"]["writers"]["writer_results"][0]
        writer["acknowledged"] -= 125
        writer["persisted"] -= 125
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint(
                "c2-production-replacement", payload, predecessor()
            )

    def test_qualification_renewal_rejects_lost_lease_ownership(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            fake_bin = Path(directory) / "bin"
            fake_bin.mkdir()
            install_fake_kubectl(fake_bin)
            target = {
                "kube_context": "operator@local",
                "namespace": "memories-qualification",
                "_platform_operations_reviewer": "reviewer",
                "_lease_holder": "story-27-4/reviewer/test",
            }
            with patch.dict(os.environ, qualification_test_env(
                fake_bin,
                QUALIFICATION_RENEW_FOREIGN="1",
            )):
                with self.assertRaises(EvidenceValidationError):
                    _run_operation(
                        target, "c4-failure-privacy-observability", "qualification-renew"
                    )

    def test_business_bearer_is_owner_only_short_lived_and_single_tenant(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            bearer = root / "bearer.jwt"
            write_business_bearer(bearer)
            with patch.dict(os.environ, {
                "HEXALITH_STORY_27_4_BUSINESS_BEARER_FILE": str(bearer)
            }):
                self.assertEqual(bearer.read_bytes() + b"\n", _load_business_bearer())
                bearer.chmod(0o644)
                with self.assertRaises(EvidenceValidationError):
                    _load_business_bearer()
                bearer.chmod(0o600)
            alias = root / "bearer-alias.jwt"
            alias.symlink_to(bearer)
            with patch.dict(os.environ, {
                "HEXALITH_STORY_27_4_BUSINESS_BEARER_FILE": str(alias)
            }):
                with self.assertRaises(EvidenceValidationError):
                    _load_business_bearer()

    def test_writer_scheduler_uses_absolute_cadence_and_reconciles_lost_response(self) -> None:
        clock = [0.0]
        sleeps: list[float] = []
        attempts: dict[str, int] = {}

        def monotonic() -> float:
            return clock[0]

        def sleep(seconds: float) -> None:
            sleeps.append(seconds)
            clock[0] += seconds

        def runner(command: tuple[str, ...], **_: object) -> tuple[int, bytes, bytes]:
            text = " ".join(command)
            if " get pods " in f" {text} ":
                pods = {"items": [
                    {"metadata": {"name": f"memories-{index}"}, "status": {
                        "phase": "Running", "conditions": [{"type": "Ready", "status": "True"}]
                    }} for index in (1, 2)
                ]}
                return 0, json.dumps(pods).encode(), b""
            segment_id = re.search(r"X-Hexalith-Qualification-Segment: ([a-z0-9-]+)", text).group(1)
            run_id = re.search(r"X-Hexalith-Qualification-Run: ([a-z0-9-]+)", text).group(1)
            attempts[segment_id] = attempts.get(segment_id, 0) + 1
            if segment_id.endswith("0002") and attempts[segment_id] == 1:
                return 9, b"", b"pod replaced after durable commit"
            ordinal = int(segment_id.rsplit("-", 1)[-1])
            conflicted = 125 if segment_id.endswith("0002") else 0
            payload = {
                "writer": "memories-1", "runId": run_id, "segmentId": segment_id,
                "recordIds": _qualification_record_ids(run_id, segment_id),
                "startedUtcMs": 1_700_000_000_000 + ((ordinal - 1) * 1000),
                "finishedUtcMs": 1_700_000_001_000 + ((ordinal - 1) * 1000),
                "attempted": 125, "enqueued": 125, "acknowledged": 125 - conflicted,
                "persisted": 125 - conflicted, "conflicted": conflicted,
                "transactionAcknowledgements": 125 - conflicted,
                "dropped": 0, "rejected": 0, "resultCount": 125,
            }
            return 0, json.dumps(payload).encode(), b""

        target = {
            "kube_context": "operator@local", "namespace": "memories-qualification",
            "_platform_operations_reviewer": "reviewer", "_lease_holder": "story-27-4/reviewer/test",
        }
        result, _ = _run_writer_segments(
            target, "c2-production-replacement", "writer-1", _segment_count=3,
            _monotonic=monotonic, _sleep=sleep, _process_runner=runner,
        )

        self.assertEqual([1.0, 1.0], sleeps)
        self.assertEqual(3, result["segment_count"])
        self.assertEqual(1, result["replayed_segment_count"])
        self.assertEqual(375, result["acknowledged"] + result["conflicted"])
        self.assertEqual(0, result["dispatch_lag_max_milliseconds"])

    def test_c2_session_renews_owned_lease_during_writer_window(self) -> None:
        original_writer = producer_common._run_writer_segments

        def delayed_writer(
            target: object,
            checkpoint: str,
            command_id: str,
            **kwargs: object,
        ) -> tuple[object, dict[str, object]]:
            time.sleep(0.4)
            kwargs.setdefault("_segment_count", 1)
            return original_writer(target, checkpoint, command_id, **kwargs)

        with tempfile.TemporaryDirectory() as directory:
            fake_bin = Path(directory) / "bin"
            fake_bin.mkdir()
            install_fake_kubectl(fake_bin)
            target = {
                "kube_context": "operator@local",
                "namespace": "memories-qualification",
                "_platform_operations_reviewer": "reviewer",
                "_lease_holder": f"story-27-4/reviewer/{os.getpid()}-{now_ms()}",
            }
            with patch.dict(os.environ, qualification_test_env(fake_bin)):
                with patch.object(producer_common, "_run_writer_segments", delayed_writer):
                    try:
                        _results, commands = _execute_qualification(
                            target,
                            "c2-production-replacement",
                            ["writer-1", "writer-2"],
                        )
                    finally:
                        _TERMINATION_REQUESTED.clear()

        self.assertTrue(
            any(str(command["command_id"]).startswith("qualification-renew") for command in commands),
            [command["command_id"] for command in commands],
        )

    def test_qualification_overlay_renders_zero_default_gate_reporter_and_least_privilege_rbac(self) -> None:
        import yaml

        completed = subprocess.run(
            ("kubectl", "kustomize", str(REPO_ROOT / "deploy/kubernetes/overlays/qualification")),
            check=False,
            capture_output=True,
            text=True,
            timeout=30,
        )
        self.assertEqual(0, completed.returncode, completed.stderr)
        resources = [item for item in yaml.safe_load_all(completed.stdout) if isinstance(item, dict)]

        def one(kind: str, name: str, namespace: str = "hexalith-memories-qualification") -> dict[str, object]:
            matches = [
                item for item in resources
                if item.get("kind") == kind
                and item.get("metadata", {}).get("name") == name
                and item.get("metadata", {}).get("namespace") == namespace
            ]
            self.assertEqual(1, len(matches), f"{kind}/{namespace}/{name}")
            return matches[0]

        for name in ("memories-access-telemetry", "memories-access-telemetry-clock"):
            self.assertEqual(0, one("Deployment", name)["spec"]["replicas"])
        server = one("Deployment", "memories")
        server_spec = server["spec"]["template"]["spec"]
        gate_volume = next(item for item in server_spec["volumes"] if item["name"] == "access-telemetry-qualification-gate")
        self.assertEqual("access-telemetry-qualification-gate", gate_volume["configMap"]["name"])
        server_container = next(item for item in server_spec["containers"] if item["name"] == "memories")
        gate_mount = next(item for item in server_container["volumeMounts"] if item["name"] == "access-telemetry-qualification-gate")
        self.assertTrue(gate_mount["readOnly"])

        reporter = one("Job", "access-telemetry-physical-evidence-reporter")
        reporter_spec = reporter["spec"]["template"]["spec"]
        self.assertFalse(reporter_spec["automountServiceAccountToken"])
        self.assertTrue(reporter["spec"]["suspend"])
        container = reporter_spec["containers"][0]
        self.assertEqual(["/bin/sh", "-ec"], container["command"])
        self.assertEqual(1, len(container["args"]))
        self.assertIn("physical-reclamation-evidence", container["args"][0])

        qualification_role = one("Role", "access-telemetry-qualification-operator")
        lease_rule = next(
            rule for rule in qualification_role["rules"] if rule["resources"] == ["leases"]
        )
        self.assertEqual(["access-telemetry-qualification"], lease_rule["resourceNames"])
        deployment_write = next(
            rule for rule in qualification_role["rules"]
            if rule["resources"] == ["deployments"] and "patch" in rule["verbs"]
        )
        self.assertEqual(
            {"memories", "memories-access-telemetry", "memories-access-telemetry-clock"},
            set(deployment_write["resourceNames"]),
        )
        dapr_role = one(
            "Role", "access-telemetry-qualification-dapr-control-plane", "dapr-system"
        )
        dapr_statefulsets = next(
            rule for rule in dapr_role["rules"] if rule["resources"] == ["statefulsets"]
        )
        self.assertEqual(
            {"dapr-placement-server", "dapr-scheduler-server"},
            set(dapr_statefulsets["resourceNames"]),
        )
        self.assertFalse(any(
            item.get("kind") in {"ClusterRole", "ClusterRoleBinding"}
            and "qualification" in item.get("metadata", {}).get("name", "")
            for item in resources
        ))

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

    def test_c1_freshness_is_authorization_only_and_retained_evidence_stays_valid(self) -> None:
        retained = predecessor()
        finished = now_ms() - (8 * 24 * 60 * 60 * 1000)
        for gate in retained["gates"].values():
            gate["started_utc_ms"] = finished - 1000
            gate["finished_utc_ms"] = finished
            gate["command"]["started_utc_ms"] = finished - 1000
            gate["command"]["finished_utc_ms"] = finished
        validate_story_27_4_checkpoint(
            "c2-production-replacement", c2_payload(), retained
        )
        retained_checkpoint = c2_payload()
        retained_checkpoint["started_utc"] = finished - 1000
        retained_checkpoint["finished_utc"] = finished
        for command in retained_checkpoint["commands"]:
            command["started_utc_ms"] = finished - 1000
            command["finished_utc_ms"] = finished
        validate_story_27_4_checkpoint(
            "c2-production-replacement",
            retained_checkpoint,
            retained,
            require_current_freshness=False,
        )
        with self.assertRaises(ValueError):
            _validate_predecessor(retained, require_authorization_freshness=True)

    def test_c3_horizons_use_emission_time_and_bind_the_final_newer_control(self) -> None:
        wrong_horizon = c3_payload()
        wrong_horizon["results"]["cohorts"][0]["emitted_utc_ms"] += 1
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint(
                "c3-retention-reclamation", wrong_horizon, predecessor()
            )

        acceptance_jitter = c3_payload()
        acceptance_jitter["results"]["cohorts"][0]["accepted_utc_ms"] += 200
        validate_story_27_4_checkpoint(
            "c3-retention-reclamation", acceptance_jitter, predecessor()
        )

        mismatched_control = c3_payload()
        mismatched_control["results"]["cohorts"][2]["newer_record_names"] = [
            "newer-control-999"
        ]
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint(
                "c3-retention-reclamation", mismatched_control, predecessor()
            )

    def test_c1_requires_unique_25_gate_artifacts_disabled_production_and_authorization(self) -> None:
        reused = predecessor()
        reused["gates"]["C1.2"]["artifact_path"] = reused["gates"]["C1.1"]["artifact_path"]
        reused["gates"]["C1.2"]["artifact_sha256"] = reused["gates"]["C1.1"]["artifact_sha256"]
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c2-production-replacement", c2_payload(), reused)
        reused_hash = predecessor()
        reused_hash["gates"]["C1.2"]["artifact_sha256"] = reused_hash["gates"]["C1.1"]["artifact_sha256"]
        with self.assertRaises(ValueError):
            validate_story_27_4_checkpoint("c2-production-replacement", c2_payload(), reused_hash)
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
            with self.assertRaises(ValueError):
                _write_json_exclusive(
                    Path(directory) / "oversized-output.json",
                    {"value": "x" * 1_048_577},
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
            environment = qualification_test_env(
                fake_bin,
                QUALIFICATION_FAIL_OPERATION="writer-1",
                QUALIFICATION_OPERATION_LOG=str(operation_log),
            )

            result = subprocess.run(
                [sys.executable, "-B", str(TOOLS_DIR / "access_telemetry_c2_producer.py"),
                 "--scenario-input", str(scenario_input), "--platform-operations-reviewer",
                 "c1-operator-reviewer"],
                check=False,
                capture_output=True,
                text=True,
                env=environment,
            )

            self.assertEqual(1, result.returncode)
            operations = operation_log.read_text(encoding="utf-8").splitlines()
            self.assertEqual(
                ["qualification-target-identity", "qualification-enable"],
                operations[:2],
                result.stderr,
            )
            self.assertIn("writer-1", operations[2:-2])
            self.assertIn("writer-2", operations[2:-2])
            self.assertEqual(
                ["qualification-disable", "qualification-final-state"],
                operations[-2:],
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
            environment = qualification_test_env(
                fake_bin,
                QUALIFICATION_INVALID_OPERATION="qualification-enable",
                QUALIFICATION_OPERATION_LOG=str(operation_log),
            )

            result = subprocess.run(
                [sys.executable, "-B", str(TOOLS_DIR / "access_telemetry_c2_producer.py"),
                 "--scenario-input", str(scenario_input), "--platform-operations-reviewer",
                 "c1-operator-reviewer"],
                check=False,
                capture_output=True,
                text=True,
                env=environment,
            )

            self.assertEqual(1, result.returncode)
            self.assertEqual(
                [
                    "qualification-target-identity",
                    "qualification-enable",
                    "qualification-disable",
                    "qualification-final-state",
                ],
                operation_log.read_text(encoding="utf-8").splitlines(),
            )

    def test_producer_refuses_unverifiable_target_identity_without_mutating_target(self) -> None:
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
            environment = qualification_test_env(
                fake_bin,
                QUALIFICATION_INVALID_OPERATION="qualification-target-identity",
                QUALIFICATION_OPERATION_LOG=str(operation_log),
            )

            result = subprocess.run(
                [sys.executable, "-B", str(TOOLS_DIR / "access_telemetry_c2_producer.py"),
                 "--scenario-input", str(scenario_input), "--platform-operations-reviewer",
                 "c1-operator-reviewer"],
                check=False,
                capture_output=True,
                text=True,
                env=environment,
            )

            self.assertEqual(1, result.returncode)
            self.assertEqual(
                ["qualification-target-identity"],
                operation_log.read_text(encoding="utf-8").splitlines(),
            )

    def test_producer_recovers_only_an_expired_story_lease(self) -> None:
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
            result = subprocess.run(
                [sys.executable, "-B", str(TOOLS_DIR / "access_telemetry_c2_producer.py"),
                 "--scenario-input", str(scenario_input), "--platform-operations-reviewer",
                 "c1-operator-reviewer"],
                check=False,
                capture_output=True,
                text=True,
                env=qualification_test_env(
                    fake_bin,
                    QUALIFICATION_STALE_LEASE="1",
                    QUALIFICATION_OPERATION_LOG=str(operation_log),
                ),
            )
            self.assertEqual(1, result.returncode)
            self.assertEqual(
                ["qualification-target-identity", "qualification-disable", "qualification-final-state"],
                operation_log.read_text(encoding="utf-8").splitlines(),
            )

    def test_producer_rejects_active_foreign_lease_without_mutating_target(self) -> None:
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
            result = subprocess.run(
                [sys.executable, "-B", str(TOOLS_DIR / "access_telemetry_c2_producer.py"),
                 "--scenario-input", str(scenario_input), "--platform-operations-reviewer",
                 "c1-operator-reviewer"],
                check=False,
                capture_output=True,
                text=True,
                env=qualification_test_env(
                    fake_bin,
                    QUALIFICATION_ACTIVE_FOREIGN_LEASE="1",
                    QUALIFICATION_OPERATION_LOG=str(operation_log),
                ),
            )
            self.assertEqual(1, result.returncode)
            self.assertEqual(
                ["qualification-target-identity"],
                operation_log.read_text(encoding="utf-8").splitlines(),
            )

    def test_c3_journal_is_exclusive_append_only_and_tamper_evident(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "c3.jsonl"
            def journal_result(command_id: str) -> dict[str, object]:
                timestamp = now_ms()
                arguments = {"operation": command_id}
                return {
                    "result_count": 1,
                    "_command": {
                        "command_id": command_id,
                        "arguments": arguments,
                        "arguments_sha256": _sha256(_canonical_json(arguments)),
                        "started_utc_ms": timestamp,
                        "finished_utc_ms": timestamp,
                        "exit_code": 0,
                        "stdout_sha256": SHA,
                        "stderr_sha256": SHA,
                        "result_count": 1,
                    },
                }

            with _C3Journal(path) as journal:
                journal.append("cohort-1h-expiry", journal_result("cohort-1h-expiry"))
                with self.assertRaises(ValueError):
                    _C3Journal(path)
                journal.append("cohort-1h-purge", journal_result("cohort-1h-purge"))

            with _C3Journal(path) as journal:
                self.assertEqual(
                    ["cohort-1h-expiry", "cohort-1h-purge"],
                    list(journal.resume_results),
                )

            lines = path.read_text(encoding="utf-8").splitlines()
            first = json.loads(lines[0])
            first["recorded_utc_ms"] += 1
            lines[0] = json.dumps(first, separators=(",", ":"), sort_keys=True)
            path.write_text("\n".join(lines) + "\n", encoding="utf-8")

            with self.assertRaises(ValueError):
                _C3Journal(path)

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

    def test_snapshot_reuses_authenticated_blobs_and_chain_budget_is_aggregate(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            repository = base / "repository"
            evidence = base / "evidence"
            repository.mkdir()
            evidence.mkdir()
            self._run("git", "init", "-q", "-b", "main", str(repository))
            self._run("git", "-C", str(repository), "config", "user.email", "test@example.invalid")
            self._run("git", "-C", str(repository), "config", "user.name", "Test")
            install_a41_files(repository, "A41 open\n")
            (repository / A41_ALLOWED_MUTATION_PATHS[0]).write_text(
                "A41 open\n" + ("recoverable-content\n" * 512),
                encoding="utf-8",
            )
            self._run("git", "-C", str(repository), "add", ".")
            self._run("git", "-C", str(repository), "commit", "-q", "-m", "test: create source baseline")

            inventory, source_blobs = _collect_a41_inventory_and_blobs(repository)
            with patch(
                "verify_access_telemetry_lifecycle._read_git_blob",
                side_effect=AssertionError("snapshot attempted a second Git blob read"),
            ):
                snapshot = create_recoverable_snapshot(
                    repository,
                    evidence / "snapshot.json",
                    inventory,
                    source_blobs,
                )
            self.assertEqual(set(A41_ALLOWED_MUTATION_PATHS), set(snapshot["paths"]))
            snapshot_path = evidence / "snapshot.json"
            authenticated = _authenticate_snapshot(
                snapshot_path,
                {
                    "source_head": inventory["source_head"],
                    "snapshot_sha256": hashlib.sha256(snapshot_path.read_bytes()).hexdigest(),
                },
                evidence,
            )
            self.assertEqual(snapshot["source_head"], authenticated["source_head"])

            budget = _EvidenceAggregateBudget(evidence)
            for index in range(4):
                path = evidence / f"large-{index}.bin"
                path.write_bytes(b"x" * 1_048_000)
                budget.account(path, f"large-{index}")
            overflow = evidence / "overflow.bin"
            overflow.write_bytes(b"x" * 3_000)
            with self.assertRaises(ValueError):
                budget.account(overflow, "overflow")

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
            bearer = evidence / "business-bearer.jwt"
            write_business_bearer(bearer)
            artifacts: dict[str, Path] = {"C1": c1_path}
            with patch.dict(os.environ, qualification_test_env(
                fake_bin,
                HEXALITH_STORY_27_4_BUSINESS_BEARER_FILE=str(bearer),
            )):
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
                    self.assertEqual(
                        0,
                        result.returncode,
                        result.stderr + (output.read_text(encoding="utf-8") if output.exists() else ""),
                    )
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
    reporter = repository / "deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml"
    reporter.parent.mkdir(parents=True)
    shutil.copy2(
        REPO_ROOT / "deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml",
        reporter,
    )


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
                              "approved_utc_ms": now_ms(),
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


def qualification_test_env(fake_bin: Path, **extra: str) -> dict[str, str]:
    env = {
        **os.environ,
        "PATH": f"{fake_bin}{os.pathsep}{os.environ.get('PATH', '')}",
        "HEXALITH_STORY_27_4_HOST_CADENCE_QUANTUM": "0",
        "HEXALITH_STORY_27_4_INLINE_KUBECTL": str(fake_bin / "kubectl"),
    }
    env.update(extra)
    return env


def install_fake_kubectl(directory: Path) -> None:
    script = directory / "kubectl"
    script.write_text("""#!/usr/bin/env python3
import hashlib, json, os, sys, time
args = sys.argv[1:]
step = int(os.environ.get('HEXALITH_STORY_27_4_STEP', '0'))
namespace = args[args.index('--namespace') + 1] if '--namespace' in args else ''
def selected_items(items):
    if '-l' not in args or not isinstance(items, list):
        return items
    key, _, expected = args[args.index('-l') + 1].partition('=')
    return [item for item in items if isinstance(item, dict)
        and item.get('metadata', {}).get('labels', {}).get(key) == expected]
def control_record_names():
    run_id = 'run-' + hashlib.sha256(os.environ['HEXALITH_STORY_27_4_LEASE_HOLDER'].encode()).hexdigest()[:24]
    return qualification_ids(run_id, 'newer-control-seed-segment-0001')
def qualification_ids(run_id, segment_id):
    correlation = hashlib.sha256(f'{run_id}/{segment_id}'.encode()).hexdigest()[:32]
    alphabet = '0123456789ABCDEFGHJKMNPQRSTVWXYZ'
    result = []
    for ordinal in range(125):
        digest = bytearray(hashlib.sha256(f'qualification-{correlation}-{ordinal:03d}'.encode()).digest())
        digest[0] &= 0x7f
        value = int.from_bytes(digest[:16], 'big')
        encoded = ['0'] * 26
        for index in range(25, -1, -1):
            value, remainder = divmod(value, 32)
            encoded[index] = alphabet[remainder]
        result.append(''.join(encoded))
    return result
if '--' in args and args[-1] in {'--version', '--build-info'} and args[-2] in {'/daprd', 'daprd'}:
    print('1.18.1' if args[-1] == '--version' else 'Version: 1.18.1\\nGit Commit: qualification-test')
    raise SystemExit(0)
op = os.environ.get('HEXALITH_STORY_27_4_COMMAND_ID') or sys.argv[-1].rsplit('/', 1)[-1]
proof_scenarios = {'failure-etag-failure', 'failure-ttl-failure', 'failure-transaction-failure',
    'failure-queue-byte-exhaustion', 'failure-queue-record-exhaustion'}
if op.endswith('-purge'):
    changed_at = 3
elif op == 'approved-adapter-fault':
    changed_at = 8
elif op.startswith('replace-'):
    changed_at = 7
elif op in proof_scenarios:
    changed_at = 12
elif op.startswith('failure-'):
    changed_at = 10
else:
    changed_at = 4
changed = step >= changed_at
pod_phase = 'same' if op.endswith('-dapr-sidecar') else ('new' if changed else 'old')
operation_log = os.environ.get('QUALIFICATION_OPERATION_LOG')
if operation_log:
    with open(operation_log, 'a', encoding='utf-8') as stream:
        existing = []
        try:
            with open(operation_log, encoding='utf-8') as current:
                existing = current.read().splitlines()
        except OSError: pass
        if not existing or existing[-1] != op:
            stream.write(op + '\\n')
if os.environ.get('QUALIFICATION_FAIL_OPERATION') == op:
    raise SystemExit(9)
if os.environ.get('QUALIFICATION_INVALID_OPERATION') == op and (op != 'qualification-enable' or step >= 21):
    print('{')
    raise SystemExit(0)
if 'auth' in args and 'can-i' in args:
    print('yes')
    raise SystemExit(0)
if 'get' in args and args[-1] == 'json':
    resource = args[args.index('get') + 1]
    if resource == 'namespace':
        print(json.dumps({'metadata': {'name': 'memories-qualification'}}, separators=(',', ':')))
        raise SystemExit(0)
    if resource == 'configmap':
        name = args[args.index('get') + 2]
        if name == 'access-telemetry-qualification-gate':
            enabled = op in {'qualification-enable', 'qualification-renew'} or op.startswith(('replace-', 'failure-')) or op == 'approved-adapter-fault'
            gate = {'schemaVersion': 1, 'state': 'enabled' if enabled else 'disabled',
                'profileSha256': 'dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14',
                'expiresUtcMs': int(time.time() * 1000) + 900000 if enabled else 0}
            print(json.dumps({'data': {'gate.json': json.dumps(gate, separators=(',', ':'))}}, separators=(',', ':')))
            raise SystemExit(0)
        if op == 'cohort-168h-report' and os.environ.get('QUALIFICATION_COMPLETED_REPORTER') == '1':
            evidence = {'evidenceId': 'story-27-4-c3',
                'componentProfileHash': 'dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14',
                'artifactSha256': os.environ['QUALIFICATION_ARTIFACT'],
                'reporterImageDigest': 'd' * 64,
                'observedAtUnixMilliseconds': int(os.environ['QUALIFICATION_RECLAIMED'])}
            print(json.dumps({'metadata': {'name': name}, 'data': {
                'evidence.json': json.dumps(evidence, separators=(',', ':'))}}, separators=(',', ':')))
            raise SystemExit(0)
        print(json.dumps({'metadata': {'name': name}, 'data': {'retentionSeconds': '604800'}}, separators=(',', ':')))
        raise SystemExit(0)
    if resource == 'lease':
        enabled = op in {'qualification-enable', 'qualification-renew'} or op.startswith(('replace-', 'failure-')) or op == 'approved-adapter-fault'
        stale = os.environ.get('QUALIFICATION_STALE_LEASE') == '1' and op in {'qualification-target-identity', 'qualification-disable'}
        active_foreign = os.environ.get('QUALIFICATION_ACTIVE_FOREIGN_LEASE') == '1' and op == 'qualification-target-identity'
        renew_foreign = os.environ.get('QUALIFICATION_RENEW_FOREIGN') == '1' and op == 'qualification-renew'
        print(json.dumps({'metadata': {'resourceVersion': '10'}, 'spec': {
            'holderIdentity': ('foreign-controller' if active_foreign or renew_foreign else 'story-27-4/prior-reviewer/1-1' if stale else
                os.environ.get('HEXALITH_STORY_27_4_LEASE_HOLDER', '') if enabled else ''),
            'leaseDurationSeconds': 900 if active_foreign else (30 if stale else (900 if enabled else 0)),
            'acquireTime': '2026-09-05T12:00:00Z' if active_foreign else ('2020-01-01T00:00:00Z' if stale else '2026-09-05T00:00:00Z')}}, separators=(',', ':')))
        raise SystemExit(0)
    if resource == 'job':
        completed_reporter = op == 'cohort-168h-report' and os.environ.get('QUALIFICATION_COMPLETED_REPORTER') == '1'
        print(json.dumps({'apiVersion': 'batch/v1', 'kind': 'Job',
            'metadata': {'name': 'access-telemetry-physical-evidence-reporter'},
            'spec': {'suspend': not completed_reporter, 'template': {'spec': {'automountServiceAccountToken': False,
                'serviceAccountName': 'access-telemetry-adapter', 'containers': [
                {'name': 'reporter', 'image': 'registry/reporter@sha256:' + ('d' * 64),
                 'command': ['/bin/sh', '-ec'],
                 'args': ['wget -qO- --header="dapr-api-token: ${DAPR_API_TOKEN}" --header="Content-Type: application/json" --post-file=/evidence/evidence.json http://127.0.0.1:3500/v1.0/invoke/memories-access-telemetry/method/v1/access-telemetry/physical-reclamation-evidence'],
                 'env': [{'name': 'DAPR_API_TOKEN', 'valueFrom': {'secretKeyRef': {'name': 'dapr-api-token', 'key': 'token'}}}],
                 'volumeMounts': [{'name': 'evidence', 'mountPath': '/evidence', 'readOnly': True}]}
            ], 'volumes': [{'name': 'evidence', 'configMap': {'name': 'access-telemetry-physical-evidence-report'}}]}}},
            'status': ({'succeeded': 1, 'completionTime': '2026-09-06T12:00:00Z'} if completed_reporter else {})}, separators=(',', ':')))
        raise SystemExit(0)
    if resource == 'deployment':
        name = args[args.index('get') + 2]
        replicas = (1 if name.endswith('-clock') else 2) if op == 'qualification-enable' else 0
        print(json.dumps({'metadata': {'name': name}, 'spec': {'replicas': replicas}}, separators=(',', ':')))
        raise SystemExit(0)
    if resource in {'component', 'components.dapr.io'} and 'access-telemetry-store' in args:
        print(json.dumps({'apiVersion': 'dapr.io/v1alpha1', 'kind': 'Component',
            'metadata': {'name': 'access-telemetry-store'},
            'spec': {'type': 'state.postgresql', 'version': 'v2'}}, separators=(',', ':')))
        raise SystemExit(0)
    if resource in {'configuration.dapr.io', 'configurations.dapr.io'} and 'memories-config' in args:
        print(json.dumps({'apiVersion': 'dapr.io/v1alpha1', 'kind': 'Configuration',
            'metadata': {'name': 'memories-config'},
            'spec': {'accessControl': {'defaultAction': 'deny', 'policies': []}}}, separators=(',', ':')))
        raise SystemExit(0)
    postgres_image = 'docker.io/library/postgres:18.4-trixie@sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a'
    digest = {'memories': 'b' * 64, 'lifecycle': 'a' * 64, 'clock': 'c' * 64,
        'daprd': 'd' * 64, 'operator': 'e' * 64, 'placement': 'f' * 64,
        'scheduler': '1' * 64, 'sentry': '2' * 64, 'injector': '3' * 64}
    if resource == 'serviceaccounts':
        names = (['dapr-operator', 'dapr-placement', 'dapr-scheduler', 'dapr-sentry', 'dapr-injector']
            if namespace == 'dapr-system' else ['memories', 'memories-access-telemetry',
            'memories-access-telemetry-clock', 'access-telemetry-postgresql', 'access-telemetry-adapter'])
        print(json.dumps({'items': [{'metadata': {'name': name}} for name in names]}, separators=(',', ':')))
        raise SystemExit(0)
    if namespace == 'dapr-system':
        system_items = {
            'deployments': [
                {'metadata': {'name': 'dapr-operator'}, 'spec': {'template': {'spec': {
                    'serviceAccountName': 'dapr-operator', 'containers': [{'image': 'registry/operator@sha256:' + digest['operator']}]}}}},
                {'metadata': {'name': 'dapr-sentry'}, 'spec': {'template': {'spec': {
                    'serviceAccountName': 'dapr-sentry', 'containers': [{'image': 'registry/sentry@sha256:' + digest['sentry']}]}}}},
                {'metadata': {'name': 'dapr-sidecar-injector'}, 'spec': {'template': {'spec': {
                    'serviceAccountName': 'dapr-injector', 'containers': [{'image': 'registry/injector@sha256:' + digest['injector']}]}}}},
            ],
            'statefulsets': [
                {'metadata': {'name': 'dapr-placement-server'}, 'spec': {'template': {'spec': {
                    'serviceAccountName': 'dapr-placement', 'containers': [{'image': 'registry/placement@sha256:' + digest['placement']}]}}}},
                {'metadata': {'name': 'dapr-scheduler-server'}, 'spec': {'template': {'spec': {
                    'serviceAccountName': 'dapr-scheduler', 'containers': [{'image': 'registry/scheduler@sha256:' + digest['scheduler']}]}}}},
            ],
            'pods': [
                {'metadata': {'name': 'dapr-operator-1', 'uid': 'dapr-operator-' + pod_phase, 'labels': {'app': 'dapr-operator'}},
                 'spec': {'serviceAccountName': 'dapr-operator'}, 'status': {'phase': 'Running', 'conditions': [{'type':'Ready','status':'True'}], 'containerStatuses': [
                     {'imageID': 'registry/operator@sha256:' + digest['operator']}]}},
                *[{'metadata': {'name': f'dapr-placement-server-{index}', 'uid': f'dapr-placement-{index}-' + pod_phase, 'labels': {'app': 'dapr-placement-server'}},
                   'spec': {'serviceAccountName': 'dapr-placement'}, 'status': {'phase': 'Running', 'conditions': [{'type':'Ready','status':'True'}], 'containerStatuses': [
                       {'imageID': 'registry/placement@sha256:' + digest['placement']}]}} for index in range(3)],
                *[{'metadata': {'name': f'dapr-scheduler-server-{index}', 'uid': f'dapr-scheduler-{index}-' + pod_phase, 'labels': {'app': 'dapr-scheduler-server'}},
                   'spec': {'serviceAccountName': 'dapr-scheduler'}, 'status': {'phase': 'Running', 'conditions': [{'type':'Ready','status':'True'}], 'containerStatuses': [
                       {'imageID': 'registry/scheduler@sha256:' + digest['scheduler']}]}} for index in range(3)],
                {'metadata': {'name': 'dapr-sentry-1', 'uid': 'dapr-sentry-' + pod_phase, 'labels': {'app': 'dapr-sentry'}},
                 'spec': {'serviceAccountName': 'dapr-sentry'}, 'status': {'phase': 'Running', 'conditions': [{'type':'Ready','status':'True'}], 'containerStatuses': [
                     {'imageID': 'registry/sentry@sha256:' + digest['sentry']}]}},
                {'metadata': {'name': 'dapr-sidecar-injector-1', 'uid': 'dapr-injector-' + pod_phase, 'labels': {'app': 'dapr-sidecar-injector'}},
                 'spec': {'serviceAccountName': 'dapr-injector'}, 'status': {'phase': 'Running', 'conditions': [{'type':'Ready','status':'True'}], 'containerStatuses': [
                     {'imageID': 'registry/injector@sha256:' + digest['injector']}]}},
            ],
        }.get(resource, [])
        print(json.dumps({'items': selected_items(system_items)}, separators=(',', ':')))
        raise SystemExit(0)
    items = {
        'deployments': [
            {'metadata': {'name': 'memories-access-telemetry', 'generation': 1},
             'spec': {'replicas': 1, 'template': {'metadata': {'annotations': {'dapr.io/app-id': 'memories-access-telemetry'}}, 'spec': {
                 'serviceAccountName': 'memories-access-telemetry', 'containers': [{'name': 'lifecycle', 'image': 'registry/access-telemetry@sha256:' + digest['lifecycle'],
                 'env': [{'name': 'AccessTelemetryLifecycle__PhysicalReclamationReporterImageDigest', 'value': 'd' * 64}]}]}}},
             'status': {'readyReplicas': 1, 'availableReplicas': 1}},
            {'metadata': {'name': 'memories', 'generation': 1},
             'spec': {'replicas': 2, 'template': {'metadata': {'annotations': {'dapr.io/app-id': 'memories'}}, 'spec': {
                 'serviceAccountName': 'memories', 'containers': [{'name': 'memories', 'image': 'registry/memories@sha256:' + digest['memories']}]}}},
             'status': {'readyReplicas': 2, 'availableReplicas': 2}},
            {'metadata': {'name': 'memories-access-telemetry-clock', 'generation': 1},
             'spec': {'replicas': 1, 'template': {'metadata': {'annotations': {'dapr.io/app-id': 'memories-access-telemetry-clock'}}, 'spec': {
                 'serviceAccountName': 'memories-access-telemetry-clock', 'containers': [{'name': 'clock', 'image': 'registry/access-telemetry-clock@sha256:' + digest['clock']}]}}},
             'status': {'readyReplicas': 1, 'availableReplicas': 1}},
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
             'spec': {'replicas': 1, 'template': {'spec': {'serviceAccountName': 'access-telemetry-postgresql',
                 'containers': [{'name': 'postgresql', 'image': postgres_image}]}}},
             'status': {'readyReplicas': 1}},
        ],
        'pods': [
            {'metadata': {'name': 'memories-1', 'uid': 'pod-1-' + pod_phase, 'generation': 1,
                          'labels': {'app.kubernetes.io/name': 'memories'}, 'annotations': {'dapr.io/app-id': 'memories'}},
             'spec': {'nodeName': 'qualification-node', 'serviceAccountName': 'memories'}, 'status': {'phase': 'Running', 'conditions': [{'type':'Ready','status':'True'}], 'containerStatuses': [
                 {'name': 'memories', 'restartCount': 0, 'image': 'registry/memories:1', 'imageID': 'registry/memories@sha256:' + digest['memories']},
                 {'name': 'daprd', 'restartCount': 1 if changed else 0, 'image': 'ghcr.io/dapr/daprd:1.18.1', 'imageID': 'ghcr.io/dapr/daprd@sha256:' + digest['daprd']},
             ]}},
            {'metadata': {'name': 'memories-2', 'uid': 'pod-2-' + pod_phase, 'generation': 1,
                          'labels': {'app.kubernetes.io/name': 'memories'}, 'annotations': {'dapr.io/app-id': 'memories'}},
             'spec': {'nodeName': 'qualification-node', 'serviceAccountName': 'memories'}, 'status': {'phase': 'Running', 'conditions': [{'type':'Ready','status':'True'}], 'containerStatuses': [
                 {'name': 'memories', 'restartCount': 0, 'image': 'registry/memories:1', 'imageID': 'registry/memories@sha256:' + digest['memories']},
                 {'name': 'daprd', 'restartCount': 1 if changed else 0, 'image': 'ghcr.io/dapr/daprd:1.18.1', 'imageID': 'ghcr.io/dapr/daprd@sha256:' + digest['daprd']},
             ]}},
            {'metadata': {'name': 'memories-3', 'uid': 'pod-3-' + pod_phase, 'generation': 1,
                          'labels': {'app.kubernetes.io/name': 'memories'}, 'annotations': {'dapr.io/app-id': 'memories'}},
             'spec': {'nodeName': 'qualification-node', 'serviceAccountName': 'memories'}, 'status': {'phase': 'Running', 'conditions': [{'type':'Ready','status':'True'}], 'containerStatuses': [
                 {'name': 'memories', 'restartCount': 0, 'image': 'registry/memories:1', 'imageID': 'registry/memories@sha256:' + digest['memories']},
                 {'name': 'daprd', 'restartCount': 1 if changed else 0, 'image': 'ghcr.io/dapr/daprd:1.18.1', 'imageID': 'ghcr.io/dapr/daprd@sha256:' + digest['daprd']},
             ]}},
            *[{'metadata': {'name': f'lifecycle-{index}', 'uid': f'lifecycle-{index}-' + pod_phase,
                            'labels': {'app.kubernetes.io/name': 'memories-access-telemetry'}},
               'spec': {'serviceAccountName': 'memories-access-telemetry'},
               'status': {'phase': 'Running', 'conditions': [{'type':'Ready','status':'True'}], 'containerStatuses': [
                   {'name': 'lifecycle', 'restartCount': 0, 'imageID': 'registry/access-telemetry@sha256:' + digest['lifecycle']},
                   {'name': 'daprd', 'restartCount': 1 if changed else 0, 'imageID': 'ghcr.io/dapr/daprd@sha256:' + digest['daprd']},
               ]}} for index in (1, 2)],
            {'metadata': {'name': 'clock-1', 'uid': 'clock-1-' + pod_phase,
                          'labels': {'app.kubernetes.io/name': 'memories-access-telemetry-clock'}},
             'spec': {'serviceAccountName': 'memories-access-telemetry-clock'},
             'status': {'phase': 'Running', 'conditions': [{'type':'Ready','status':'True'}], 'containerStatuses': [
                 {'name': 'clock', 'restartCount': 0, 'imageID': 'registry/access-telemetry-clock@sha256:' + digest['clock']},
                 {'name': 'daprd', 'restartCount': 1 if changed else 0, 'imageID': 'ghcr.io/dapr/daprd@sha256:' + digest['daprd']},
             ]}},
            {'metadata': {'name': 'postgres-0', 'uid': 'postgres-' + pod_phase, 'labels': {'app.kubernetes.io/name': 'access-telemetry-postgresql'}},
             'spec': {'serviceAccountName': 'access-telemetry-postgresql'}, 'status': {'phase': 'Running', 'conditions': [{'type':'Ready','status':'True'}],
                 'containerStatuses': [{'name': 'postgresql', 'restartCount': 0, 'imageID': postgres_image}]}},
        ],
    }.get(resource, [])
    print(json.dumps({'items': selected_items(items)}, separators=(',', ':')))
    raise SystemExit(0)
now = int(time.time() * 1000)
command_text = ' '.join(args)
if 'redis-cli' in command_text or '/v1.0/shutdown' in command_text:
    raise SystemExit(0)
if any(verb in args for verb in {'patch', 'scale', 'rollout', 'wait', 'delete', 'set'}):
    raise SystemExit(0)
if '/operations/access-telemetry/qualification/fixed-workload' in command_text:
    import re
    run_id = re.search(r'X-Hexalith-Qualification-Run: ([a-z0-9-]+)', command_text).group(1)
    segment_id = re.search(r'X-Hexalith-Qualification-Segment: ([a-z0-9-]+)', command_text).group(1)
    if op.startswith('writer-'):
        index = int(op[-1])
        segment = int(segment_id.rsplit('-', 1)[-1])
        segment_start = int(os.environ['HEXALITH_STORY_27_4_LEASE_HOLDER'].rsplit('-', 1)[-1]) + ((segment - 1) * 1000)
        value = {'runId': run_id, 'segmentId': segment_id, 'writer': f'memories-{index}',
            'startedUtcMs': segment_start, 'finishedUtcMs': segment_start + 1000, 'attempted': 125,
            'enqueued': 125, 'acknowledged': 125, 'persisted': 125, 'conflicted': 0,
            'transactionAcknowledgements': 125, 'dropped': 0, 'rejected': 0,
            'recordIds': qualification_ids(run_id, segment_id), 'resultCount': 125}
    elif op.endswith('-seed'):
        value = {'runId': run_id, 'segmentId': segment_id, 'writer': 'memories-1',
            'attempted': 125, 'enqueued': 125, 'acknowledged': 125, 'persisted': 125,
            'conflicted': 0, 'transactionAcknowledgements': 125, 'dropped': 0, 'rejected': 0,
            'recordIds': qualification_ids(run_id, segment_id), 'resultCount': 125}
    else:
        persisted_scenarios = {'failure-actor-failover', 'failure-application-outage',
            'failure-approved-adapter-fault', 'failure-capacity-pressure', 'failure-clock-outage',
            'failure-dapr-outage', 'failure-reconnect', 'failure-reminder-delay',
            'failure-shutdown', 'failure-state-outage'}
        dropped_scenarios = {'failure-queue-byte-exhaustion',
            'failure-queue-record-exhaustion', 'failure-retry-exhaustion'}
        disposition = ('persisted' if op in persisted_scenarios else
            'dropped' if op in dropped_scenarios else 'rejected')
        value = {'runId': run_id, 'segmentId': segment_id, 'writer': 'memories-1',
            'attempted': 2, 'enqueued': 2, 'acknowledged': 2 if disposition == 'persisted' else 0,
            'persisted': 2 if disposition == 'persisted' else 0, 'conflicted': 0,
            'transactionAcknowledgements': 2 if disposition == 'persisted' else 0,
            'dropped': 2 if disposition == 'dropped' else 0,
            'rejected': 2 if disposition == 'rejected' else 0,
            'resultCount': 2}
    print(json.dumps(value, separators=(',', ':')))
    raise SystemExit(0)
if 'cat /var/run/hexalith/access-telemetry-qualification/gate.json' in command_text:
    print(json.dumps({'schemaVersion': 1, 'state': 'disabled',
        'profileSha256': 'dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14',
        'expiresUtcMs': 0}, separators=(',', ':')))
    raise SystemExit(0)
if '/api/v1/handlers' in command_text:
    print(json.dumps({'business_status': 200}, separators=(',', ':')))
    raise SystemExit(0)
if '/api/v1/tenants/story-27-4-qualification' in command_text:
    print(json.dumps({'allowed_status': 200, 'denied_status': 403,
        'denied_dependency_calls': 0}, separators=(',', ':')))
    raise SystemExit(0)
if 'consistency=strong' in command_text and 'records%2F' in command_text:
    print(json.dumps({'stage': 'strong-absence', 'strong_absent_read_count': 125}, separators=(',', ':')))
    raise SystemExit(0)
if '/v1/access-telemetry/inspect' in command_text:
    print(json.dumps({'health': 'Healthy', 'reason': 'None', 'retainedRecordCount': 100,
        'configurationEpoch': 'qualification', 'physicalReclamationEvidencePending': False}, separators=(',', ':')))
    raise SystemExit(0)
if 'memories_access_telemetry_lifecycle_state_operations_total' in command_text:
    print(json.dumps({'status': 'success', 'data': {'result': [
        {'value': [int(time.time()), '900000']}
    ]}}, separators=(',', ':')))
    raise SystemExit(0)
if 'prometheus-operated.monitoring.svc.cluster.local' in command_text:
    states = ['accepted','dropped','enqueued','expired','failed','persisted','purged','rejected','retried']
    for state in states:
        print(f'memories_access_telemetry_lifecycle_records_total{{state="{state}",reason="none"}} 1')
    print('memories_access_telemetry_lifecycle_reminders_total{outcome="succeeded"} 1')
    print('memories_access_telemetry_lifecycle_health{state="healthy",reason="none"} 1')
    print('memories_access_telemetry_lifecycle_health{state="unhealthy",reason="dependency_unavailable"} 1')
    print('memories_access_telemetry_lifecycle_health{state="no_data",reason="none"} 1')
    print('memories_access_telemetry_lifecycle_profile{state="matched"} 1')
    print('memories_access_telemetry_lifecycle_physical_evidence_total{state="present"} 1')
    print('memories_access_telemetry_lifecycle_physical_evidence_last_timestamp_seconds 1700000000')
    raise SystemExit(0)
if 'http://127.0.0.1:9090/metrics' in command_text:
    print('# TYPE dapr_http_server_request_count counter')
    print('dapr_http_server_request_count{app_id="memories"} 1')
    raise SystemExit(0)
if 'http://127.0.0.1:8080/ready' in command_text:
    print(json.dumps({'schemaVersion': 1, 'status': 'Healthy', 'entries': {}}, separators=(',', ':')))
    raise SystemExit(0)
if 'logs' in args:
    if op == 'cohort-168h-report':
        # The producer verifies this receipt against the exact dynamically
        # submitted aggregate; recompute the same deterministic fake payload.
        aggregate = {'stage': 'reclamation', 'reclaimed_utc_ms': int(os.environ.get('QUALIFICATION_RECLAIMED', ((now // 3600000) * 3600000 - (8 * 24 * 3600000)) + 168 * 3600000 + 120000)),
            'allocator_free_bytes': 700}
        print(json.dumps({'status': 'accepted', 'evidenceId': 'story-27-4-c3',
            'componentProfileHash': 'dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14',
            'artifactSha256': os.environ['HEXALITH_STORY_27_4_PHYSICAL_ARTIFACT_SHA256'],
            'reporterImageDigest': 'd' * 64,
            'observedAtUnixMilliseconds': aggregate['reclaimed_utc_ms']}, separators=(',', ':')))
        raise SystemExit(0)
    run_id = 'run-' + __import__('hashlib').sha256(
        os.environ['HEXALITH_STORY_27_4_LEASE_HOLDER'].encode()).hexdigest()[:24]
    segment_id = f'{op}-segment-0001'
    correlation = __import__('hashlib').sha256(f'{run_id}/{segment_id}'.encode()).hexdigest()[:32]
    print(json.dumps({'eventId': 7506, 'auditEvent': {'queryParams': {
        'workflowInstanceIdPrefix': f'qualification-{correlation}-000'}}}, separators=(',', ':')))
    raise SystemExit(0)
if 'psql' in command_text and 'VACUUM (ANALYZE,' in command_text:
    print('VACUUM')
    raise SystemExit(0)
if 'psql' in command_text and op == 'c3-empty-preflight':
    print(json.dumps({'stage': 'preflight', 'record_count': 0, 'index_candidate_count': 0}, separators=(',', ':')))
    raise SystemExit(0)
if 'psql' in command_text and op == 'newer-control-seed':
    print(json.dumps({'stage': 'control', 'record_count': 125,
        'newer_record_names': control_record_names()}, separators=(',', ':')))
    raise SystemExit(0)
if 'psql' in command_text and op.startswith('cohort-'):
    hours = int(op.split('-')[1][:-1]); stage = op.rsplit('-', 1)[-1]
    emitted = (now // 3600000) * 3600000 - (8 * 24 * 3600000)
    accepted = emitted + 25; expires = emitted + hours * 3600000; purged = expires + 60000
    if "'stage','index'" in command_text:
        value = {'stage': 'index', 'index_name': 'idx_lifecycle_expiredate', 'post_index_candidate_count': 0}
    elif stage == 'seed':
        run_id = 'run-' + hashlib.sha256(os.environ['HEXALITH_STORY_27_4_LEASE_HOLDER'].encode()).hexdigest()[:24]
        value = {'stage': stage, 'retention_hours': hours, 'cohort_id': f'retention-{hours}h',
            'database': 'memories_access_telemetry', 'schema': 'access_telemetry', 'table': 'lifecycle_state',
            'accepted_utc_ms': accepted, 'emitted_utc_ms': emitted, 'expires_utc_ms': expires,
            'pre_tuple_count': 125,
            'record_ids': qualification_ids(run_id, f'cohort-{hours}h-seed-segment-0001')}
    elif stage == 'wait':
        value = {'stage': stage, 'retention_hours': hours, 'cohort_id': f'retention-{hours}h',
            'ready': True, 'candidate_count': 125}
    elif stage == 'expiry':
        value = {'stage': stage, 'retention_hours': hours, 'cohort_id': f'retention-{hours}h',
            'database': 'memories_access_telemetry', 'schema': 'access_telemetry', 'table': 'lifecycle_state',
            'accepted_utc_ms': accepted, 'emitted_utc_ms': emitted, 'expires_utc_ms': expires,
            'pre_tuple_count': 125,             'candidate_count': 125,
            'newer_record_names': control_record_names(),
            'newer_records_preserved': True}
    elif stage == 'purge':
        value = {'stage': stage, 'purged_utc_ms': purged, 'post_tuple_count': 0,
            'logical_absence': True, 'newer_records_preserved': True}
    else:
        value = {'stage': stage, 'reclaimed_utc_ms': purged + 60000,
            'allocator_free_bytes': 100 if step == 0 else 700}
    print(json.dumps(value, separators=(',', ':')))
    raise SystemExit(0)
if op == 'qualification-target-identity':
    value = {'kind': 'non-production-qualification', 'namespace': 'memories-qualification',
        'profile_sha256': 'dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14',
        'writes_state': 'disabled'}
elif op in {'qualification-enable', 'qualification-disable', 'qualification-final-state'}:
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
elif op == 'newer-control-seed':
    value = {'record_count': 125,
        'newer_record_names': control_record_names()}
elif op.startswith('cohort-'):
    hours = int(op.split('-')[1][:-1]); emitted = (now // 3600000) * 3600000 - (8 * 24 * 3600000)
    accepted = emitted + 25; expires = emitted + hours * 3600000; purged = expires + 60000
    value = {'retention_hours': hours, 'cohort_id': f'retention-{hours}h',
        'database': 'memories_access_telemetry', 'schema': 'access_telemetry', 'table': 'lifecycle_state',
        'record_ids': [f'{hours * 1000 + index:026d}' for index in range(1, 126)],
        'accepted_utc_ms': accepted, 'emitted_utc_ms': emitted, 'expires_utc_ms': expires, 'purged_utc_ms': purged,
        'reclaimed_utc_ms': purged + 60000, 'pre_tuple_count': 125, 'post_tuple_count': 0,
        'candidate_count': 125, 'deleted_count': 115, 'already_absent_count': 10,
        'index_removal_count': 125, 'logical_absence': True,
        'newer_record_names': control_record_names(),
        'newer_records_preserved': True, 'interrupted_recovery': True, 'restart_recovery': True,
        'allocator_free_bytes_before': 100, 'allocator_free_bytes_after': 700, 'os_disk_shrink_claimed': False}
elif op.startswith('failure-'):
    value = {'exercised': True, 'expected_disposition': 'rejected', 'business_operation_succeeded': True,
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
    dotnet = directory / "dotnet"
    dotnet.write_text("""#!/usr/bin/env python3
tests = [
    'PersistAsync_WritesRecordAndExpiryIndexAtomicallyWithCeilingTtl',
    'PersistAsync_FutureOrExpiredSource_FailsClosed',
    'AttestAsync_MajorityIntervalWiderThan250Milliseconds_FailsClosed',
    'Verify_ContextProfileOrNonceMismatch_FailsClosed',
    'Verify_ReplayStaleDeltaOrTamperedSignature_FailsClosed',
    'LifecycleCounter_EmitsOnlyBoundedStateAndReasonLabels',
    'LifecycleGauges_UseLiveClockAndAggregateHealthWithoutInventingPhysicalEvidence',
    'HealthPrecedence_IsUnhealthyThenDegradedThenNoDataOrHealthy',
    'RuntimeGate_ClosesImmediatelyWhenPublishedEvidenceExpires',
    'PersistAsync_SameIdHashAndExpiry_IsIdempotent',
    'PersistAsync_SameIdWithDifferentEnvelopeOrExpiry_ReturnsConflict',
    'WriteRecordAndIndexAsync_ConcurrentCatalogWriteBetweenReadAndCommit_ThrowsAndCommitsNoPartialState',
    'WriteRecordAndIndexAsync_TtlReapedRecordWithLingeringBucketEntry_ReturnsConflictWithoutResurrection',
    'DeleteAndVerifyAsync_SecondOperationFails_CommitsNoPartialDelete',
    'Queue_DropsNewestAtExactRecordAndByteBounds',
    'SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies',
    'TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState',
    'TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies',
    'VerifyAsync_DetectsMissingSemanticTenantId_ReturnsFailed',
    'VerifyAsync_DetectsSemanticTenantIdMismatch_ReturnsFailed',
    'VerifyAsync_DetectsSyntacticTenantIdMismatch_ReturnsFailed',
]
for name in tests:
    print('Passed ' + name)
print('=== TEST EXECUTION SUMMARY ===')
print('  Tests: ' + str(len(tests)) + ', Errors: 0, Failed: 0, Skipped: 0, Not Run: 0')
""", encoding="utf-8")
    dotnet.chmod(0o755)


if __name__ == "__main__":
    unittest.main()
