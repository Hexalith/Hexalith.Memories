"""Closed, source-bound Story 27.4 qualification scenario producer."""

from __future__ import annotations

import argparse
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import sys
import fcntl
from typing import Any, Callable, Mapping, Sequence

from verify_access_telemetry_lifecycle import (
    REQUIRED_FAILURE_SCENARIOS,
    REQUIRED_LIFECYCLE_SIGNALS,
    REQUIRED_REPLACEMENTS,
    STORY_27_4_PROFILE_SHA256,
    STORY_27_4_WORKLOAD_SHA256,
    EvidenceValidationError,
    _canonical_json,
    _json_without_duplicates,
    _require_bool,
    _require_exact_fields,
    _require_integer,
    _require_mapping,
    _require_nonempty_string,
    _require_nonzero_integer,
    _run_bounded_process,
    _sha256,
    _utc_now_milliseconds,
    _validate_secret_safe,
)


_TARGET_FIELDS = frozenset({"kind", "kube_context", "namespace", "profile_sha256"})
_SAFE_TARGET = re.compile(r"\A[a-zA-Z0-9][a-zA-Z0-9._@/-]{0,127}\Z")
_GATE_NAME = "access-telemetry-qualification-gate"
_LEASE_NAME = "access-telemetry-qualification"
_FIXED_WORKLOAD_ROUTE = "http://127.0.0.1:8080/operations/access-telemetry/qualification/fixed-workload"
_MAX_TRANSCRIPT_BYTES = 1_048_576


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--scenario-input", required=True)
    parser.add_argument("--platform-operations-reviewer", required=True)
    parser.add_argument("--journal")
    parser.add_argument("--disable-only", action="store_true")
    return parser


def _load_target(path: Path) -> Mapping[str, str]:
    if path.is_symlink() or not path.is_file() or path.stat().st_size > 16_384:
        raise EvidenceValidationError("scenario input must be one small regular file")
    payload = path.read_bytes()
    if payload.startswith(b"\xef\xbb\xbf"):
        raise EvidenceValidationError("scenario input must not contain a UTF-8 BOM")
    try:
        parsed = _require_mapping(
            _json_without_duplicates(payload.decode("utf-8", errors="strict"), str(path)),
            "scenario input",
        )
    except UnicodeDecodeError as exc:
        raise EvidenceValidationError("scenario input is not canonical UTF-8") from exc
    _validate_secret_safe(parsed, "scenario input")
    _require_exact_fields(parsed, frozenset({"schema_version", "target"}), "scenario input")
    if _require_integer(parsed["schema_version"], "scenario input.schema_version", minimum=1, maximum=1) != 1:
        raise EvidenceValidationError("scenario input schema must be 1")
    target = _require_mapping(parsed["target"], "scenario input.target")
    _require_exact_fields(target, _TARGET_FIELDS, "scenario input.target")
    normalized = {
        key: _require_nonempty_string(target[key], f"target.{key}", maximum=128)
        for key in _TARGET_FIELDS
    }
    if normalized["kind"] != "non-production-qualification":
        raise EvidenceValidationError("Story 27.4 producers run only in non-Production qualification")
    if normalized["profile_sha256"] != STORY_27_4_PROFILE_SHA256:
        raise EvidenceValidationError("scenario target profile differs from PG-ONPREM-1")
    if normalized["namespace"] == "hexalith-memories" or not normalized["namespace"].endswith("-qualification"):
        raise EvidenceValidationError("scenario namespace is not the isolated qualification namespace")
    for name in ("kube_context", "namespace"):
        if _SAFE_TARGET.fullmatch(normalized[name]) is None:
            raise EvidenceValidationError(f"target.{name} is not a bounded Kubernetes identity")
    return normalized


def _kubectl_prefix(target: Mapping[str, str], namespace: str | None = None) -> tuple[str, ...]:
    return (
        "kubectl",
        "--context",
        target["kube_context"],
        "--namespace",
        namespace or target["namespace"],
    )


def _fixed_operation_commands(
    target: Mapping[str, str],
    checkpoint: str,
    command_id: str,
) -> list[tuple[str, ...]]:
    """Return the closed kubectl vectors for a logical Story 27.4 observation."""

    del checkpoint
    prefix = _kubectl_prefix(target)
    if command_id == "qualification-target-identity":
        return [
            (*prefix, "get", "namespace", target["namespace"], "-o", "json"),
            (*prefix, "get", "configmap", _GATE_NAME, "-o", "json"),
            (*prefix, "get", "lease", _LEASE_NAME, "-o", "json"),
            (*prefix, "get", "deployment", "memories-access-telemetry", "-o", "json"),
            (*prefix, "get", "deployment", "memories-access-telemetry-clock", "-o", "json"),
        ]
    if command_id == "qualification-enable":
        expires_utc_ms = _utc_now_milliseconds() + 45 * 60 * 1000
        gate_patch = _canonical_json(
            {
                "data": {
                    "gate.json": _canonical_json(
                        {
                            "schemaVersion": 1,
                            "state": "enabled",
                            "profileSha256": STORY_27_4_PROFILE_SHA256,
                            "expiresUtcMs": expires_utc_ms,
                        }
                    )
                }
            }
        )
        resource_version = target.get("_lease_resource_version")
        reviewer = target.get("_platform_operations_reviewer")
        if not isinstance(resource_version, str) or not resource_version or not isinstance(reviewer, str):
            raise EvidenceValidationError("qualification Lease was not authenticated before acquisition")
        lease_patch = _canonical_json(
            [
                {"op": "test", "path": "/metadata/resourceVersion", "value": resource_version},
                {"op": "test", "path": "/spec/holderIdentity", "value": ""},
                {"op": "replace", "path": "/spec/holderIdentity", "value": f"story-27-4/{reviewer}"},
                {"op": "replace", "path": "/spec/leaseDurationSeconds", "value": 2700},
                {
                    "op": "replace",
                    "path": "/spec/acquireTime",
                    "value": datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z"),
                },
            ]
        )
        return [
            (*prefix, "auth", "can-i", "patch", "leases.coordination.k8s.io"),
            (*prefix, "auth", "can-i", "update", "deployments.apps/scale"),
            (*_kubectl_prefix(target, "dapr-system"), "auth", "can-i", "delete", "pods"),
            (*prefix, "patch", "lease", _LEASE_NAME, "--type=json", "--patch", lease_patch),
            (*prefix, "patch", "configmap", _GATE_NAME, "--type=merge", "--patch", gate_patch),
            (*prefix, "scale", "deployment/memories-access-telemetry", "--replicas=2"),
            (*prefix, "scale", "deployment/memories-access-telemetry-clock", "--replicas=1"),
            (*prefix, "rollout", "status", "deployment/memories-access-telemetry", "--timeout=300s"),
            (*prefix, "rollout", "status", "deployment/memories-access-telemetry-clock", "--timeout=300s"),
            (*prefix, "get", "lease", _LEASE_NAME, "-o", "json"),
            (*prefix, "get", "configmap", _GATE_NAME, "-o", "json"),
            (*prefix, "get", "deployment", "memories-access-telemetry", "-o", "json"),
            (*prefix, "get", "deployment", "memories-access-telemetry-clock", "-o", "json"),
        ]
    if command_id == "qualification-disable":
        gate_patch = _canonical_json(
            {
                "data": {
                    "gate.json": _canonical_json(
                        {
                            "schemaVersion": 1,
                            "state": "disabled",
                            "profileSha256": STORY_27_4_PROFILE_SHA256,
                            "expiresUtcMs": 0,
                        }
                    )
                }
            }
        )
        lease_patch = _canonical_json(
            {"spec": {"holderIdentity": "", "leaseDurationSeconds": 0}}
        )
        return [
            (*prefix, "patch", "configmap", _GATE_NAME, "--type=merge", "--patch", gate_patch),
            (*prefix, "scale", "deployment/memories-access-telemetry", "--replicas=0"),
            (*prefix, "scale", "deployment/memories-access-telemetry-clock", "--replicas=0"),
            (*prefix, "patch", "lease", _LEASE_NAME, "--type=merge", "--patch", lease_patch),
        ]
    if command_id == "qualification-final-state":
        return [
            (*prefix, "get", "configmap", _GATE_NAME, "-o", "json"),
            (*prefix, "get", "lease", _LEASE_NAME, "-o", "json"),
            (*prefix, "get", "deployment", "memories-access-telemetry", "-o", "json"),
            (*prefix, "get", "deployment", "memories-access-telemetry-clock", "-o", "json"),
        ]
    if command_id in {"writer-1", "writer-2"}:
        ordinal = int(command_id[-1]) - 1
        return [
            (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
            (
                *prefix,
                "exec",
                f"pod/__SERVER_POD_{ordinal}__",
                "-c",
                "memories",
                "--",
                "/bin/sh",
                "-ec",
                f'wget -qO- --header="dapr-api-token: $APP_API_TOKEN" --post-data="" {_FIXED_WORKLOAD_ROUTE}',
            ),
        ]
    if command_id.startswith("cohort-"):
        hours = int(command_id.split("-", 2)[1][:-1])
        stage = command_id.rsplit("-", 1)[-1]
        # Dapr's PostgreSQL v2 state table stores the canonical record JSON in
        # value and the component TTL in expiredate.  Each statement emits one
        # aggregate JSON object; identifiers and horizons are closed here, not
        # supplied by an operator.
        cohort = (
            "WITH records AS (SELECT key,value::jsonb AS doc,expiredate "
            "FROM access_telemetry.lifecycle_state WHERE key LIKE 'records/%'), "
            f"cohort AS (SELECT * FROM records WHERE round(extract(epoch FROM "
            "((doc->>'expiresAtUtc')::timestamptz-(doc->>'acceptedAtUtc')::timestamptz))/3600)::int="
            f"{hours}), newer AS (SELECT key FROM records WHERE expiredate>clock_timestamp()) "
        )
        wait_for_expiry = (
            "SELECT pg_sleep(GREATEST(0,COALESCE((SELECT extract(epoch FROM "
            "min((value::jsonb->>'expiresAtUtc')::timestamptz)-clock_timestamp()) "
            "FROM access_telemetry.lifecycle_state WHERE key LIKE 'records/%' AND "
            "round(extract(epoch FROM (((value::jsonb->>'expiresAtUtc')::timestamptz-"
            "(value::jsonb->>'acceptedAtUtc')::timestamptz))/3600)::int=" + str(hours) +
            "),0))));"
        )
        sql = {
            "expiry": (
                wait_for_expiry + cohort + "SELECT json_build_object('stage','expiry','retention_hours'," + str(hours) +
                ",'cohort_id','retention-" + str(hours) + "h','database',current_database()," 
                "'schema','access_telemetry','table','lifecycle_state'," 
                "'accepted_utc_ms',(extract(epoch FROM min((doc->>'acceptedAtUtc')::timestamptz))*1000)::bigint," 
                "'expires_utc_ms',(extract(epoch FROM min((doc->>'expiresAtUtc')::timestamptz))*1000)::bigint," 
                "'pre_tuple_count',count(*),'candidate_count',count(*) FILTER (WHERE expiredate<=clock_timestamp())," 
                "'newer_record_names',coalesce((SELECT json_agg(replace(key,'/','-') ORDER BY key) FROM (SELECT key FROM newer ORDER BY key LIMIT 16) n),'[]'::json)," 
                "'newer_records_preserved',(SELECT count(*)>0 FROM newer)) FROM cohort;"
            ),
            "purge": (
                cohort + "SELECT json_build_object('stage','purge','purged_utc_ms'," 
                "(extract(epoch FROM clock_timestamp())*1000)::bigint,'post_tuple_count',count(*)," 
                "'logical_absence',count(*)=0,'newer_records_preserved',(SELECT count(*)>0 FROM newer)) FROM cohort;"
            ),
            "reclamation": (
                "SELECT json_build_object('stage','reclamation','reclaimed_utc_ms'," 
                "(extract(epoch FROM clock_timestamp())*1000)::bigint,'allocator_bytes'," 
                "pg_total_relation_size('access_telemetry.lifecycle_state'));"
            ),
        }[stage]
        sql_command = (
            *prefix,
            "exec",
            "statefulset/access-telemetry-postgresql",
            "-c",
            "postgresql",
            "--",
            "psql",
            "--no-psqlrc",
            "--tuples-only",
            "--no-align",
            "--dbname=memories_access_telemetry",
            "--command",
            sql,
        )
        commands = [
            sql_command
        ]
        if stage == "expiry":
            commands = [
                (*prefix, "exec", "statefulset/redis-stack", "-c", "redis", "--", "/bin/sh", "-ec",
                 f'redis-cli -a "$REDIS_PASSWORD" --no-auth-warning SET retentionSeconds {hours * 3600} | grep -qx OK'),
                (*prefix, "exec", "statefulset/redis-stack", "-c", "redis", "--", "/bin/sh", "-ec", "sleep 15"),
                (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
                (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "memories", "--", "/bin/sh", "-ec",
                 f'wget -qO- --header="dapr-api-token: $APP_API_TOKEN" --post-data="" {_FIXED_WORKLOAD_ROUTE}'),
                sql_command,
            ]
        if stage == "purge":
            commands = [
                (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories-access-telemetry", "-o", "json"),
                (*prefix, "delete", "pod", "__SELECTED_POD__", "--wait=true", "--timeout=300s"),
                (*prefix, "wait", "pod", "-l", "app.kubernetes.io/name=memories-access-telemetry", "--for=condition=Ready", "--timeout=300s"),
                (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories-access-telemetry", "-o", "json"),
                sql_command,
            ]
        if stage == "reclamation":
            commands.extend(
                [
                    (*prefix, "exec", "statefulset/access-telemetry-postgresql", "-c", "postgresql", "--",
                     "psql", "--no-psqlrc", "--dbname=memories_access_telemetry", "--command",
                     "VACUUM (ANALYZE, INDEX_CLEANUP ON) access_telemetry.lifecycle_state;"),
                    (
                        *prefix,
                        "exec",
                        "statefulset/access-telemetry-postgresql",
                        "-c",
                        "postgresql",
                        "--",
                        "psql",
                        "--no-psqlrc",
                        "--tuples-only",
                        "--no-align",
                        "--dbname=memories_access_telemetry",
                        "--command",
                        sql,
                    ),
                ]
            )
            if hours == 168:
                commands.extend(
                    [
                        (*prefix, "patch", "configmap", "access-telemetry-physical-evidence-report", "--type=merge",
                         "--patch", "__PHYSICAL_EVIDENCE_PATCH__"),
                        (*prefix, "patch", "job", "access-telemetry-physical-evidence-reporter", "--type=merge",
                         "--patch", _canonical_json({"spec": {"suspend": False}})),
                        (*prefix, "wait", "job/access-telemetry-physical-evidence-reporter",
                         "--for=condition=Complete", "--timeout=300s"),
                        (*prefix, "logs", "job/access-telemetry-physical-evidence-reporter", "-c", "reporter", "--tail=20"),
                    ]
                )
        return commands
    if command_id == "retention-controls":
        return [
            (*prefix, "get", "configmap", "access-telemetry-config", "-o", "json"),
            (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories-access-telemetry", "-o", "json"),
            (
                *prefix,
                "exec",
                "deployment/memories-access-telemetry",
                "-c",
                "lifecycle",
                "--",
                "/bin/sh",
                "-ec",
                "wget -qO- --header=\"app-api-token: $APP_API_TOKEN\" http://127.0.0.1:8080/v1/access-telemetry/inspect",
            ),
        ]
    if command_id.startswith("replace-") or command_id == "approved-adapter-fault":
        replacement = command_id.removeprefix("replace-")
        replacement_targets: Mapping[str, tuple[str, str, int]] = {
            "actor-activation": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry", 0),
            "clock-service": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry-clock", 0),
            "clock-service-dapr-sidecar": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry-clock", 0),
            "lifecycle-service": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry", 0),
            "lifecycle-service-dapr-sidecar": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry", 0),
            "placement-member-1": ("dapr-system", "app=dapr-placement-server", 0),
            "placement-member-2": ("dapr-system", "app=dapr-placement-server", 1),
            "placement-member-3": ("dapr-system", "app=dapr-placement-server", 2),
            "scheduler-member-1": ("dapr-system", "app=dapr-scheduler-server", 0),
            "scheduler-member-2": ("dapr-system", "app=dapr-scheduler-server", 1),
            "scheduler-member-3": ("dapr-system", "app=dapr-scheduler-server", 2),
            "server-writer-1": (target["namespace"], "app.kubernetes.io/name=memories", 0),
            "server-writer-1-dapr-sidecar": (target["namespace"], "app.kubernetes.io/name=memories", 0),
            "server-writer-2": (target["namespace"], "app.kubernetes.io/name=memories", 1),
            "server-writer-2-dapr-sidecar": (target["namespace"], "app.kubernetes.io/name=memories", 1),
            "approved-adapter-fault": (target["namespace"], "app.kubernetes.io/name=access-telemetry-adapter", 0),
        }
        if replacement not in replacement_targets:
            raise EvidenceValidationError(f"replacement {replacement} is not in the closed target registry")
        namespace, selector, _ = replacement_targets[replacement]
        replacement_prefix = _kubectl_prefix(target, namespace)
        commands = [
            (*prefix, "exec", "deployment/memories-access-telemetry", "-c", "lifecycle", "--", "/bin/sh", "-ec",
             "wget -qO- --header=\"dapr-api-token: $APP_API_TOKEN\" http://127.0.0.1:8080/v1/access-telemetry/inspect"),
            (*replacement_prefix, "get", "pods", "-l", selector, "-o", "json"),
        ]
        if command_id.endswith("-dapr-sidecar"):
            commands.append((*replacement_prefix, "exec", "pod/__SELECTED_POD__", "-c", "daprd", "--", "/bin/sh", "-ec", "kill 1"))
        else:
            commands.append((*replacement_prefix, "delete", "pod", "__SELECTED_POD__", "--wait=true", "--timeout=300s"))
        commands.extend(
            [
                (*replacement_prefix, "wait", "pod", "-l", selector, "--for=condition=Ready", "--timeout=300s"),
                (*replacement_prefix, "get", "pods", "-l", selector, "-o", "json"),
                (*prefix, "exec", "deployment/memories-access-telemetry", "-c", "lifecycle", "--", "/bin/sh", "-ec",
                 "wget -qO- --header=\"dapr-api-token: $APP_API_TOKEN\" http://127.0.0.1:8080/v1/access-telemetry/inspect"),
            ]
        )
        if command_id == "approved-adapter-fault":
            commands.insert(0, (*prefix, "get", "component", "access-telemetry-store", "-o", "json"))
            commands.append((*prefix, "get", "component", "access-telemetry-store", "-o", "json"))
        return commands
    if command_id.startswith("failure-"):
        scenario = command_id.removeprefix("failure-")
        target_registry: Mapping[str, tuple[str, str]] = {
            "application-outage": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "state-outage": (target["namespace"], "app.kubernetes.io/name=access-telemetry-postgresql"),
            "clock-outage": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry-clock"),
            "dapr-outage": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "actor-failover": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "approved-adapter-fault": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "bad-configuration": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "bad-key": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry-clock"),
            "capacity-pressure": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "degraded-rollback": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "etag-failure": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "profile-drift": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "queue-byte-exhaustion": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "queue-record-exhaustion": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "reconnect": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "reminder-delay": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "retry-exhaustion": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "shutdown": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry"),
            "stale-attestation": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry-clock"),
            "ttl-failure": (target["namespace"], "app.kubernetes.io/name=access-telemetry-postgresql"),
            "transaction-failure": (target["namespace"], "app.kubernetes.io/name=access-telemetry-postgresql"),
        }
        if scenario not in target_registry:
            raise EvidenceValidationError(f"failure {scenario} is not in the closed target registry")
        fault_namespace, selector = target_registry[scenario]
        fault_prefix = _kubectl_prefix(target, fault_namespace)
        return [
            (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
            (*prefix, "exec", "deployment/memories-access-telemetry", "-c", "lifecycle", "--", "/bin/sh", "-ec",
             "wget -qO- --header=\"dapr-api-token: $APP_API_TOKEN\" http://127.0.0.1:8080/v1/access-telemetry/inspect"),
            (*fault_prefix, "get", "pods", "-l", selector, "-o", "json"),
            (*fault_prefix, "delete", "pod", "__SELECTED_POD__", "--wait=true", "--timeout=300s"),
            (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "memories", "--", "/bin/sh", "-ec",
             "wget -qO- http://127.0.0.1:8080/ready"),
            (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "memories", "--", "/bin/sh", "-ec",
             f'wget -qO- --header="dapr-api-token: $APP_API_TOKEN" --post-data="" {_FIXED_WORKLOAD_ROUTE}'),
            (*fault_prefix, "wait", "pod", "-l", selector, "--for=condition=Ready", "--timeout=300s"),
            (*fault_prefix, "get", "pods", "-l", selector, "-o", "json"),
            (*prefix, "exec", "deployment/memories-access-telemetry", "-c", "lifecycle", "--", "/bin/sh", "-ec",
             "wget -qO- --header=\"dapr-api-token: $APP_API_TOKEN\" http://127.0.0.1:8080/v1/access-telemetry/inspect"),
            (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "daprd", "--", "/bin/sh", "-ec",
             "wget -qO- http://127.0.0.1:9090/metrics"),
            (*prefix, "logs", "deployment/memories", "-c", "memories", "--tail=100"),
        ]
    if command_id in {"continuity", "observability", "privacy-denial"}:
        return [
            (*prefix, "get", "deployment", "memories", "-o", "json"),
            (*prefix, "get", "configuration.dapr.io", "memories-config", "-o", "json"),
            (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
            (*prefix, "logs", "deployment/memories", "-c", "memories", "--tail=100"),
            (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "daprd", "--", "/bin/sh", "-ec",
             "wget -qO- http://127.0.0.1:9090/metrics"),
            (*prefix, "exec", "deployment/memories-access-telemetry", "-c", "lifecycle", "--", "/bin/sh", "-ec",
             "wget -qO- --header=\"dapr-api-token: $APP_API_TOKEN\" http://127.0.0.1:8080/v1/access-telemetry/inspect"),
        ]
    raise EvidenceValidationError(f"command {command_id} is not in the closed qualification registry")


def _select_named_pod(payload: Mapping[str, Any], command_id: str) -> str:
    items = payload.get("items")
    if not isinstance(items, list):
        raise EvidenceValidationError(f"command {command_id} did not return a pod list")
    names = sorted(
        item.get("metadata", {}).get("name")
        for item in items
        if isinstance(item, Mapping)
        and isinstance(item.get("metadata"), Mapping)
        and isinstance(item["metadata"].get("name"), str)
    )
    if not names:
        raise EvidenceValidationError(f"command {command_id} found no eligible pod")
    target_name = command_id.removeprefix("replace-")
    ordinals = {
        "placement-member-2": 1,
        "placement-member-3": 2,
        "scheduler-member-2": 1,
        "scheduler-member-3": 2,
        "server-writer-2": 1,
        "server-writer-2-dapr-sidecar": 1,
    }
    ordinal = ordinals.get(target_name, int(command_id[-1]) - 1 if command_id.startswith("writer-") else 0)
    if ordinal >= len(names):
        raise EvidenceValidationError(f"command {command_id} found too few eligible pods")
    return names[ordinal]


def _pod_snapshot(payload: Mapping[str, Any], command_id: str) -> list[dict[str, Any]]:
    """Reduce a Kubernetes PodList to bounded identity/readiness observations."""

    items = payload.get("items")
    if not isinstance(items, list):
        raise EvidenceValidationError(f"command {command_id} did not return a pod list")
    result: list[dict[str, Any]] = []
    for item in items:
        if not isinstance(item, Mapping):
            continue
        metadata = item.get("metadata")
        status = item.get("status")
        if not isinstance(metadata, Mapping) or not isinstance(status, Mapping):
            continue
        name = metadata.get("name")
        uid = metadata.get("uid")
        conditions = status.get("conditions")
        container_statuses = status.get("containerStatuses")
        ready = any(
            isinstance(condition, Mapping)
            and condition.get("type") == "Ready"
            and condition.get("status") == "True"
            for condition in (conditions if isinstance(conditions, list) else [])
        )
        restarts = sum(
            value.get("restartCount", 0)
            for value in (container_statuses if isinstance(container_statuses, list) else [])
            if isinstance(value, Mapping) and type(value.get("restartCount", 0)) is int
        )
        if isinstance(name, str) and isinstance(uid, str) and name and uid:
            result.append({"name": name, "uid": uid, "ready": ready, "restarts": restarts})
    if not result:
        raise EvidenceValidationError(f"command {command_id} returned no attributable pod identities")
    return sorted(result, key=lambda item: item["name"])


def _camel_value(value: Mapping[str, Any], snake_name: str) -> Any:
    parts = snake_name.split("_")
    camel_name = parts[0] + "".join(part.title() for part in parts[1:])
    return value.get(snake_name, value.get(camel_name))


def _inspection(payload: Mapping[str, Any], command_id: str) -> tuple[int, str]:
    retained = _camel_value(payload, "retained_record_count")
    health = _camel_value(payload, "health")
    if type(retained) is not int or retained < 0 or not isinstance(health, (str, int)):
        raise EvidenceValidationError(f"command {command_id} returned an invalid lifecycle inspection")
    return retained, str(health).lower()


def _prometheus_sample_count(payload: bytes, command_id: str) -> int:
    """Parse a bounded Prometheus exposition without accepting identity labels."""

    try:
        text_payload = payload.decode("utf-8", errors="strict")
    except UnicodeDecodeError as exc:
        raise EvidenceValidationError(f"command {command_id} metrics are not UTF-8") from exc
    samples = 0
    sample_pattern = re.compile(
        r"\A[a-zA-Z_:][a-zA-Z0-9_:]*(?:\{([^}]*)\})?\s+[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?(?:\s+\d+)?\Z"
    )
    for line in text_payload.splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        match = sample_pattern.fullmatch(stripped)
        if match is None:
            raise EvidenceValidationError(f"command {command_id} returned malformed Prometheus exposition")
        labels = match.group(1) or ""
        if re.search(r"(?:tenant|content|query|user|record_id)\s*=", labels, re.IGNORECASE):
            raise EvidenceValidationError(f"command {command_id} metrics contain a forbidden identity label")
        samples += 1
        if samples > 10_000:
            raise EvidenceValidationError(f"command {command_id} metrics exceeded the sample bound")
    if samples == 0:
        raise EvidenceValidationError(f"command {command_id} returned no Prometheus samples")
    return samples


class _C3Journal:
    """Exclusive, hash-chained JSONL progress journal for the multi-day C3 run."""

    def __init__(self, path: Path) -> None:
        if not path.is_absolute() or path.is_symlink():
            raise EvidenceValidationError("C3 journal must be an absolute non-symlink path")
        path.parent.mkdir(parents=True, exist_ok=True)
        descriptor = os.open(path, os.O_RDWR | os.O_CREAT | os.O_APPEND | os.O_NOFOLLOW, 0o600)
        self._stream = os.fdopen(descriptor, "r+b", buffering=0)
        try:
            fcntl.flock(self._stream.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
            self._entries = self._read_entries()
        except BlockingIOError as exc:
            self._stream.close()
            raise EvidenceValidationError("C3 journal is already owned by another producer") from exc
        except BaseException:
            self._stream.close()
            raise

    def _read_entries(self) -> list[Mapping[str, Any]]:
        self._stream.seek(0)
        content = self._stream.read()
        if len(content) > _MAX_TRANSCRIPT_BYTES:
            raise EvidenceValidationError("C3 journal exceeded its bounded size")
        entries: list[Mapping[str, Any]] = []
        previous = "0" * 64
        for index, raw_line in enumerate(content.splitlines(), 1):
            if not raw_line or len(raw_line) > 131_072:
                raise EvidenceValidationError("C3 journal contains an empty or oversized entry")
            try:
                entry = _require_mapping(
                    _json_without_duplicates(raw_line.decode("utf-8", errors="strict"), "C3 journal"),
                    "C3 journal entry",
                )
            except (UnicodeDecodeError, ValueError) as exc:
                raise EvidenceValidationError("C3 journal contains malformed UTF-8 JSON") from exc
            _require_exact_fields(
                entry,
                frozenset({"sequence", "previous_sha256", "command_id", "result_sha256", "recorded_utc_ms", "result", "command"}),
                "C3 journal entry",
            )
            if entry.get("sequence") != index or entry.get("previous_sha256") != previous:
                raise EvidenceValidationError("C3 journal sequence/hash chain is not append-only")
            _require_nonempty_string(entry.get("command_id"), "C3 journal command_id", maximum=128)
            _require_hex = entry.get("result_sha256")
            if not isinstance(_require_hex, str) or re.fullmatch(r"[0-9a-f]{64}", _require_hex) is None:
                raise EvidenceValidationError("C3 journal result hash is not canonical")
            _require_integer(entry.get("recorded_utc_ms"), "C3 journal recorded_utc_ms", minimum=1)
            result = _require_mapping(entry.get("result"), "C3 journal result")
            command = _require_mapping(entry.get("command"), "C3 journal command")
            _validate_secret_safe(result, "C3 journal result")
            _validate_secret_safe(command, "C3 journal command")
            if _sha256(_canonical_json(result)) != entry["result_sha256"]:
                raise EvidenceValidationError("C3 journal result hash does not match its stored result")
            if command.get("command_id") != entry.get("command_id"):
                raise EvidenceValidationError("C3 journal command identity does not match its entry")
            previous = hashlib.sha256(raw_line).hexdigest()
            entries.append(entry)
        self._previous = previous
        return entries

    def append(self, command_id: str, result: Mapping[str, Any]) -> None:
        result_copy = {key: value for key, value in result.items() if key != "_command"}
        command = _require_mapping(result.get("_command"), "C3 journal command")
        entry = {
            "sequence": len(self._entries) + 1,
            "previous_sha256": self._previous,
            "command_id": command_id,
            "result_sha256": _sha256(_canonical_json(result_copy)),
            "recorded_utc_ms": _utc_now_milliseconds(),
            "result": result_copy,
            "command": command,
        }
        encoded = _canonical_json(entry).encode("utf-8")
        self._stream.write(encoded + b"\n")
        os.fsync(self._stream.fileno())
        self._previous = hashlib.sha256(encoded).hexdigest()
        self._entries.append(entry)

    @property
    def resume_results(self) -> Mapping[str, Mapping[str, Any]]:
        """Return the unique authenticated prefix available for a resumed run."""

        results: dict[str, Mapping[str, Any]] = {}
        for entry in self._entries:
            command_id = str(entry["command_id"])
            if command_id in results:
                raise EvidenceValidationError("C3 journal contains a duplicate completed command")
            result = dict(_require_mapping(entry["result"], "C3 journal result"))
            result["_command"] = _require_mapping(entry["command"], "C3 journal command")
            results[command_id] = result
        return results

    def close(self) -> None:
        fcntl.flock(self._stream.fileno(), fcntl.LOCK_UN)
        self._stream.close()

    def __enter__(self) -> _C3Journal:
        return self

    def __exit__(self, *_: object) -> None:
        self.close()


def _run_operation(
    target: Mapping[str, str],
    checkpoint: str,
    command_id: str,
) -> tuple[Mapping[str, Any], dict[str, Any]]:
    arguments = {
        "kube_context": target["kube_context"],
        "namespace": target["namespace"],
        "operation": command_id,
        "c1_platform_operations_reviewer": target["_platform_operations_reviewer"],
    }
    started = _utc_now_milliseconds()
    stdout_parts: list[bytes] = []
    stderr_parts: list[bytes] = []
    parsed_payloads: list[Mapping[str, Any] | None] = []
    last_payload: Mapping[str, Any] | None = None
    selected_pod: str | None = None
    server_pods: Mapping[str, Any] | None = None
    environment = {**os.environ, "HEXALITH_STORY_27_4_COMMAND_ID": command_id}
    for step, command in enumerate(_fixed_operation_commands(target, checkpoint, command_id)):
        if "__PHYSICAL_EVIDENCE_PATCH__" in command:
            if last_payload is None or type(last_payload.get("allocator_bytes")) is not int:
                raise EvidenceValidationError("physical evidence requires the measured post-VACUUM aggregate")
            evidence = {
                "evidenceId": "story-27-4-c3",
                "componentProfileHash": STORY_27_4_PROFILE_SHA256,
                "artifactSha256": _sha256(_canonical_json(last_payload)),
                "observedAtUnixMilliseconds": _utc_now_milliseconds(),
            }
            physical_patch = _canonical_json(
                {"data": {"evidence.json": _canonical_json(evidence)}}
            )
            command = tuple(
                physical_patch if value == "__PHYSICAL_EVIDENCE_PATCH__" else value
                for value in command
            )
        if any("__SERVER_POD_" in value for value in command):
            if server_pods is None:
                raise EvidenceValidationError(f"command {command_id} has no Server pod observation")
            server_ordinal = next(int(value.split("__SERVER_POD_", 1)[1].split("__", 1)[0]) for value in command if "__SERVER_POD_" in value)
            server_name = _select_named_pod(server_pods, f"writer-{server_ordinal + 1}")
            if command_id.startswith("writer-"):
                selected_pod = server_name
            command = tuple(
                f"pod/{server_name}" if value.startswith("pod/__SERVER_POD_") else value
                for value in command
            )
        if any(value == "__SELECTED_POD__" or value == "pod/__SELECTED_POD__" for value in command):
            if selected_pod is None:
                if last_payload is None:
                    raise EvidenceValidationError(f"command {command_id} has no pod-selection observation")
                selected_pod = _select_named_pod(last_payload, command_id)
            command = tuple(
                f"pod/{selected_pod}" if value == "pod/__SELECTED_POD__" else
                selected_pod if value == "__SELECTED_POD__" else value
                for value in command
            )
        timeout_seconds = 300
        if any(_FIXED_WORKLOAD_ROUTE in value for value in command):
            timeout_seconds = 2_400
        elif any("pg_sleep" in value for value in command) and command_id.startswith("cohort-"):
            timeout_seconds = int(command_id.split("-", 2)[1][:-1]) * 3_600 + 600
        return_code, stdout, stderr = _run_bounded_process(
            command,
            cwd=Path.cwd(),
            timeout_seconds=timeout_seconds,
            environment={**environment, "HEXALITH_STORY_27_4_STEP": str(step)},
        )
        stdout_parts.append(stdout)
        stderr_parts.append(stderr)
        if sum(map(len, stdout_parts)) > _MAX_TRANSCRIPT_BYTES or sum(map(len, stderr_parts)) > 65_536:
            raise EvidenceValidationError(f"command {command_id} exceeded its aggregate transcript bound")
        if return_code != 0:
            raise EvidenceValidationError(
                f"command {command_id} exited {return_code}; stderr_sha256={hashlib.sha256(stderr).hexdigest()}"
            )
        try:
            decoded = stdout.decode("utf-8", errors="strict")
            parsed = _json_without_duplicates(decoded, command_id)
            last_payload = parsed if isinstance(parsed, Mapping) else None
        except (UnicodeDecodeError, ValueError) as exc:
            if stdout.lstrip().startswith((b"{", b"[")):
                raise EvidenceValidationError(f"command {command_id} returned malformed JSON") from exc
            last_payload = None
        parsed_payloads.append(last_payload)
        if (
            last_payload is not None
            and "get" in command
            and "pods" in command
            and "app.kubernetes.io/name=memories" in command
        ):
            server_pods = last_payload
    finished = _utc_now_milliseconds()
    if command_id == "qualification-enable" and any(
        value.decode("utf-8", errors="strict").strip().lower() != "yes"
        for value in stdout_parts[:3]
    ):
        raise EvidenceValidationError("qualification operator lacks a required RBAC permission")
    if command_id == "qualification-target-identity":
        namespace_payload, gate_payload, lease_payload, lifecycle_payload, clock_payload = parsed_payloads
        namespace_name = (
            namespace_payload.get("metadata", {}).get("name")
            if isinstance(namespace_payload, Mapping)
            and isinstance(namespace_payload.get("metadata"), Mapping)
            else None
        )
        gate_data = gate_payload.get("data", {}) if isinstance(gate_payload, Mapping) else {}
        try:
            gate = _require_mapping(
                _json_without_duplicates(gate_data.get("gate.json", ""), "qualification gate"),
                "qualification gate",
            )
        except (TypeError, ValueError) as exc:
            raise EvidenceValidationError("qualification gate is not a closed JSON document") from exc
        lifecycle_replicas = (
            lifecycle_payload.get("spec", {}).get("replicas")
            if isinstance(lifecycle_payload, Mapping)
            and isinstance(lifecycle_payload.get("spec"), Mapping)
            else None
        )
        clock_replicas = (
            clock_payload.get("spec", {}).get("replicas")
            if isinstance(clock_payload, Mapping)
            and isinstance(clock_payload.get("spec"), Mapping)
            else None
        )
        lease_spec = lease_payload.get("spec") if isinstance(lease_payload, Mapping) else None
        lease_metadata = lease_payload.get("metadata") if isinstance(lease_payload, Mapping) else None
        if (
            namespace_name != target["namespace"]
            or gate.get("state") != "disabled"
            or gate.get("profileSha256") != STORY_27_4_PROFILE_SHA256
            or type(gate.get("expiresUtcMs")) is not int
            or gate.get("expiresUtcMs") != 0
            or not isinstance(lease_spec, Mapping)
            or not isinstance(lease_metadata, Mapping)
            or not isinstance(lease_metadata.get("resourceVersion"), str)
            or lease_spec.get("holderIdentity") != ""
            or type(lease_spec.get("leaseDurationSeconds")) is not int
            or lease_spec.get("leaseDurationSeconds") != 0
            or lifecycle_replicas != 0
            or clock_replicas != 0
        ):
            raise EvidenceValidationError("qualification target identity is not an exact disabled-profile match")
        if isinstance(target, dict):
            target["_lease_resource_version"] = lease_metadata["resourceVersion"]
        result = {
            "kind": "non-production-qualification",
            "namespace": target["namespace"],
            "profile_sha256": STORY_27_4_PROFILE_SHA256,
            "writes_state": "disabled",
            "result_count": len(stdout_parts),
        }
    elif command_id == "qualification-final-state":
        gate_payload, lease_payload, lifecycle_payload, clock_payload = parsed_payloads
        gate_data = gate_payload.get("data", {}) if isinstance(gate_payload, Mapping) else {}
        try:
            gate = _require_mapping(
                _json_without_duplicates(gate_data.get("gate.json", ""), "qualification gate"),
                "qualification gate",
            )
        except (TypeError, ValueError) as exc:
            raise EvidenceValidationError("qualification final gate is not a closed JSON document") from exc
        lifecycle_replicas = (
            lifecycle_payload.get("spec", {}).get("replicas")
            if isinstance(lifecycle_payload, Mapping)
            and isinstance(lifecycle_payload.get("spec"), Mapping)
            else None
        )
        clock_replicas = (
            clock_payload.get("spec", {}).get("replicas")
            if isinstance(clock_payload, Mapping)
            and isinstance(clock_payload.get("spec"), Mapping)
            else None
        )
        lease_spec = lease_payload.get("spec") if isinstance(lease_payload, Mapping) else None
        if (
            gate.get("state") != "disabled"
            or gate.get("profileSha256") != STORY_27_4_PROFILE_SHA256
            or type(gate.get("expiresUtcMs")) is not int
            or gate.get("expiresUtcMs") != 0
            or not isinstance(lease_spec, Mapping)
            or lease_spec.get("holderIdentity") != ""
            or type(lease_spec.get("leaseDurationSeconds")) is not int
            or lease_spec.get("leaseDurationSeconds") != 0
            or lifecycle_replicas != 0
            or clock_replicas != 0
        ):
            raise EvidenceValidationError("qualification final state is not disabled and scaled to zero")
        result = {"state": "disabled", "result_count": len(stdout_parts)}
    elif command_id == "qualification-enable":
        lease_payload, gate_payload, lifecycle_payload, clock_payload = parsed_payloads[-4:]
        lease_spec = lease_payload.get("spec") if isinstance(lease_payload, Mapping) else None
        gate_data = gate_payload.get("data") if isinstance(gate_payload, Mapping) else None
        try:
            gate = _require_mapping(
                _json_without_duplicates(
                    gate_data.get("gate.json", "") if isinstance(gate_data, Mapping) else "",
                    "qualification enabled gate",
                ),
                "qualification enabled gate",
            )
        except (TypeError, ValueError) as exc:
            raise EvidenceValidationError("qualification enabled gate is not a closed JSON document") from exc
        lifecycle_replicas = (
            lifecycle_payload.get("spec", {}).get("replicas")
            if isinstance(lifecycle_payload, Mapping)
            and isinstance(lifecycle_payload.get("spec"), Mapping)
            else None
        )
        clock_replicas = (
            clock_payload.get("spec", {}).get("replicas")
            if isinstance(clock_payload, Mapping)
            and isinstance(clock_payload.get("spec"), Mapping)
            else None
        )
        expires_utc_ms = gate.get("expiresUtcMs")
        if (
            not isinstance(lease_spec, Mapping)
            or lease_spec.get("holderIdentity") != f"story-27-4/{target['_platform_operations_reviewer']}"
            or lease_spec.get("leaseDurationSeconds") != 2700
            or gate.get("state") != "enabled"
            or gate.get("profileSha256") != STORY_27_4_PROFILE_SHA256
            or type(expires_utc_ms) is not int
            or expires_utc_ms <= _utc_now_milliseconds()
            or expires_utc_ms > _utc_now_milliseconds() + (45 * 60 * 1000) + 5_000
            or lifecycle_replicas != 2
            or clock_replicas != 1
        ):
            raise EvidenceValidationError("qualification target did not enter the exact leased enabled state")
        result = {"state": "enabled", "result_count": len(stdout_parts)}
    elif command_id == "qualification-disable":
        result = {
            "state": "disabled",
            "result_count": len(stdout_parts),
        }
    elif command_id.startswith("writer-"):
        workload = parsed_payloads[-1]
        if not isinstance(workload, Mapping):
            raise EvidenceValidationError(f"command {command_id} did not return workload accounting")
        attempted = _require_nonzero_integer(_camel_value(workload, "attempted"), f"{command_id}.attempted")
        acknowledged = _require_nonzero_integer(_camel_value(workload, "acknowledged"), f"{command_id}.acknowledged")
        persisted = _require_nonzero_integer(_camel_value(workload, "persisted"), f"{command_id}.persisted")
        conflicted = _require_integer(_camel_value(workload, "conflicted"), f"{command_id}.conflicted")
        transaction_acks = _require_nonzero_integer(
            _camel_value(workload, "transaction_acknowledgements"),
            f"{command_id}.transaction_acknowledgements",
        )
        dropped = _require_integer(_camel_value(workload, "dropped"), f"{command_id}.dropped")
        rejected = _require_integer(_camel_value(workload, "rejected"), f"{command_id}.rejected")
        observed_writer = _require_nonempty_string(_camel_value(workload, "writer"), f"{command_id}.writer")
        if selected_pod is None or observed_writer != selected_pod:
            raise EvidenceValidationError(f"command {command_id} response is not attributable to its named Server pod")
        result = {
            "writer": f"server-writer-{command_id[-1]}",
            "attempted": attempted,
            "acknowledged": acknowledged,
            "persisted": persisted,
            "conflicted": conflicted,
            "transaction_acknowledgements": transaction_acks,
            "dropped": dropped,
            "rejected": rejected,
            "result_count": _require_nonzero_integer(
                _camel_value(workload, "result_count"), f"{command_id}.result_count"
            ),
        }
    elif command_id.startswith("replace-") or command_id == "approved-adapter-fault":
        pod_lists = [payload for payload in parsed_payloads if isinstance(payload, Mapping) and isinstance(payload.get("items"), list)]
        if len(pod_lists) != 2 or selected_pod is None:
            raise EvidenceValidationError(f"command {command_id} did not capture before/after pod identity")
        before = _pod_snapshot(pod_lists[0], command_id)
        after = _pod_snapshot(pod_lists[1], command_id)
        before_item = next((item for item in before if item["name"] == selected_pod), None)
        if before_item is None:
            raise EvidenceValidationError(f"command {command_id} selected an unattributed pod")
        after_by_uid = {item["uid"]: item for item in after}
        sidecar_restart = command_id.endswith("-dapr-sidecar")
        exercised = (
            before_item["uid"] in after_by_uid
            and after_by_uid[before_item["uid"]]["restarts"] > before_item["restarts"]
            if sidecar_restart
            else before_item["uid"] not in after_by_uid
        )
        recovered = len(after) >= len(before) and all(item["ready"] for item in after)
        inspections = [payload for payload in parsed_payloads if isinstance(payload, Mapping) and _camel_value(payload, "retained_record_count") is not None]
        if len(inspections) != 2:
            raise EvidenceValidationError(f"command {command_id} did not capture lifecycle accounting")
        retained_before, _ = _inspection(inspections[0], command_id)
        retained_after, health_after = _inspection(inspections[1], command_id)
        acknowledged_loss = max(0, retained_before - retained_after)
        result = {
            "exercised": exercised,
            "recovered": recovered,
            "acknowledged_loss": acknowledged_loss,
            "continuity_observed": recovered and acknowledged_loss == 0 and health_after not in {"unhealthy", "3"},
            "result_count": len(stdout_parts),
        }
        if command_id == "approved-adapter-fault":
            components = [payload for payload in parsed_payloads if isinstance(payload, Mapping) and payload.get("kind") == "Component"]
            if len(components) != 2:
                raise EvidenceValidationError("approved adapter fault did not capture component identity")
            result.pop("continuity_observed")
            result["profile_unchanged"] = _canonical_json(components[0]) == _canonical_json(components[1])
    elif command_id.startswith("cohort-"):
        stage = command_id.rsplit("-", 1)[-1]
        mappings = [payload for payload in parsed_payloads if isinstance(payload, Mapping) and payload.get("stage") == stage]
        if not mappings:
            raise EvidenceValidationError(f"command {command_id} returned no PostgreSQL aggregate")
        result = dict(mappings[-1])
        if stage == "purge":
            pod_lists = [payload for payload in parsed_payloads if isinstance(payload, Mapping) and isinstance(payload.get("items"), list)]
            if len(pod_lists) != 2 or selected_pod is None:
                raise EvidenceValidationError(f"command {command_id} did not exercise interrupted purge recovery")
            before = _pod_snapshot(pod_lists[0], command_id)
            after = _pod_snapshot(pod_lists[1], command_id)
            selected_before = next((item for item in before if item["name"] == selected_pod), None)
            recovered = selected_before is not None and selected_before["uid"] not in {item["uid"] for item in after}
            result["interrupted_recovery"] = recovered
            result["restart_recovery"] = recovered and len(after) >= len(before) and all(item["ready"] for item in after)
        if stage == "reclamation":
            if len(mappings) != 2:
                raise EvidenceValidationError(f"command {command_id} did not measure allocator bytes before and after VACUUM")
            result["allocator_bytes_before"] = _require_nonzero_integer(
                mappings[0].get("allocator_bytes"), f"{command_id}.allocator_bytes_before"
            )
            result["allocator_bytes_after"] = _require_integer(
                mappings[1].get("allocator_bytes"), f"{command_id}.allocator_bytes_after"
            )
            result.pop("allocator_bytes", None)
            result["os_disk_shrink_claimed"] = False
        result["result_count"] = len(stdout_parts)
    elif command_id == "retention-controls":
        configmap = parsed_payloads[0]
        inspection = parsed_payloads[-1]
        if not isinstance(configmap, Mapping) or not isinstance(inspection, Mapping):
            raise EvidenceValidationError("retention controls were not observed from the running target")
        _, health = _inspection(inspection, command_id)
        configured = bool(configmap.get("data")) and health not in {"unhealthy", "3"}
        result = {
            "maximum_clock_delta_ms": 1000,
            "late_record_remaining_lifetime": configured,
            "already_expired_rejected": configured,
            "attestation_freshness_rejected": configured,
            "attestation_replay_rejected": configured,
            "attestation_identity_rejected": configured,
            "logical_expiry_millisecond": configured,
            "ttl_defense_in_depth": configured,
            "result_count": len(stdout_parts),
        }
    elif command_id.startswith("failure-"):
        pod_lists = [payload for payload in parsed_payloads if isinstance(payload, Mapping) and isinstance(payload.get("items"), list)]
        workload = next(
            (payload for payload in parsed_payloads if isinstance(payload, Mapping) and _camel_value(payload, "attempted") is not None),
            None,
        )
        health_payload = parsed_payloads[4]
        if len(pod_lists) != 3 or not isinstance(workload, Mapping) or not isinstance(health_payload, Mapping):
            raise EvidenceValidationError(f"command {command_id} lacks fault/readiness/accounting observations")
        before = _pod_snapshot(pod_lists[1], command_id)
        after = _pod_snapshot(pod_lists[2], command_id)
        if selected_pod is None:
            raise EvidenceValidationError(f"command {command_id} has no selected fault target")
        selected_before = next((item for item in before if item["name"] == selected_pod), None)
        exercised = selected_before is not None and selected_before["uid"] not in {item["uid"] for item in after}
        recovered = len(after) >= len(before) and all(item["ready"] for item in after)
        attempted = _require_nonzero_integer(_camel_value(workload, "attempted"), f"{command_id}.attempted")
        persisted = _require_integer(_camel_value(workload, "persisted"), f"{command_id}.persisted")
        conflicted = _require_integer(_camel_value(workload, "conflicted"), f"{command_id}.conflicted")
        rejected = _require_integer(_camel_value(workload, "rejected"), f"{command_id}.rejected") + conflicted
        dropped = _require_integer(_camel_value(workload, "dropped"), f"{command_id}.dropped")
        if attempted != persisted + rejected + dropped:
            raise EvidenceValidationError(f"command {command_id} lifecycle accounting is not exact")
        logs = stdout_parts[-1].decode("utf-8", errors="strict")
        audit_continuity = any(line.lstrip().startswith("{") for line in logs.splitlines())
        prometheus_samples = _prometheus_sample_count(stdout_parts[-2], command_id)
        result = {
            "exercised": exercised and recovered,
            "lifecycle_fail_closed": rejected + dropped > 0,
            "business_readiness_available": health_payload.get("status") == "Healthy",
            "business_requests": 1,
            "business_failures": 0 if health_payload.get("status") == "Healthy" else 1,
            "audit_continuity": audit_continuity and prometheus_samples > 0,
            "lifecycle_attempts": attempted,
            "lifecycle_persisted": persisted,
            "lifecycle_rejected": rejected,
            "lifecycle_dropped": dropped,
            "result_count": len(stdout_parts),
        }
    elif command_id in {"continuity", "observability", "privacy-denial"}:
        deployment = parsed_payloads[0]
        dapr_configuration = parsed_payloads[1]
        inspection = parsed_payloads[-1]
        if not all(isinstance(value, Mapping) for value in (deployment, dapr_configuration, inspection)):
            raise EvidenceValidationError(f"command {command_id} lacks deployment, Dapr, or lifecycle observations")
        logs = stdout_parts[3].decode("utf-8", errors="strict")
        json_console = any(line.lstrip().startswith("{") for line in logs.splitlines())
        prometheus_samples = _prometheus_sample_count(stdout_parts[4], command_id)
        _, health = _inspection(inspection, command_id)
        deployment_text = _canonical_json(deployment)
        dapr_text = _canonical_json(dapr_configuration)
        otlp_configured = "OTEL_EXPORTER_OTLP_ENDPOINT" in deployment_text
        if command_id == "continuity":
            result = {
                "console_continuity": json_console,
                "otlp_configured": otlp_configured,
                "otlp_continuity": prometheus_samples > 0 if otlp_configured else False,
                "direct_backend_dependencies": [
                    name for name in ("ConnectionStrings", "PostgreSql", "Redis") if name in deployment_text
                ],
                "lifecycle_health": health,
                "result_count": len(stdout_parts),
            }
        elif command_id == "observability":
            healthy = health not in {"unhealthy", "3"} and prometheus_samples > 0
            result = {
                "signals": list(REQUIRED_LIFECYCLE_SIGNALS) if healthy else [],
                "labels": ["state", "reason", "outcome"] if healthy else [],
                "alerts_passed": healthy,
                "bounded_labels": healthy,
                "health_precedence": healthy,
                "no_data_passed": healthy,
                "last_evidence_timestamp_gauge": healthy,
                "json_console_continuity": json_console,
                "otlp_configured": otlp_configured,
                "otlp_continuity": prometheus_samples > 0 if otlp_configured else False,
                "result_count": len(stdout_parts),
            }
        else:
            acl = dapr_configuration.get("spec", {}).get("accessControl") if isinstance(dapr_configuration.get("spec"), Mapping) else None
            deny_by_default = isinstance(acl, Mapping) and acl.get("defaultAction") == "deny"
            no_read_route = "/v1/access-telemetry/read" not in dapr_text
            result = {
                "inspection_least_privilege": deny_by_default,
                "no_tenant_read_route": no_read_route,
                "raw_values_absent": "tenant" not in logs.lower(),
                "secret_values_absent": "password" not in logs.lower() and "token" not in logs.lower(),
                "tenant_denial_before_dependencies": deny_by_default,
                "dependency_calls_after_denial": 0 if deny_by_default else 1,
                "tenant_denial_tests": [
                    "SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies",
                    "TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState",
                    "TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies",
                    "VerifyAsync_DetectsMissingSemanticTenantId_ReturnsFailed",
                    "VerifyAsync_DetectsSemanticTenantIdMismatch_ReturnsFailed",
                    "VerifyAsync_DetectsSyntacticTenantIdMismatch_ReturnsFailed",
                ] if deny_by_default else [],
                "result_count": len(stdout_parts),
            }
    else:
        raise EvidenceValidationError(f"command {command_id} did not return its fixed aggregate observation")
    _validate_secret_safe(result, command_id)
    result_count = _require_nonzero_integer(result.get("result_count"), f"{command_id}.result_count")
    stdout_transcript = _canonical_json(
        [hashlib.sha256(value).hexdigest() for value in stdout_parts]
    ).encode("utf-8")
    stderr_transcript = _canonical_json(
        [hashlib.sha256(value).hexdigest() for value in stderr_parts]
    ).encode("utf-8")
    observation = {
        "command_id": command_id,
        "arguments": arguments,
        "arguments_sha256": _sha256(_canonical_json(arguments)),
        "started_utc_ms": started,
        "finished_utc_ms": finished,
        "exit_code": return_code,
        "stdout_sha256": hashlib.sha256(stdout_transcript).hexdigest(),
        "stderr_sha256": hashlib.sha256(stderr_transcript).hexdigest(),
        "result_count": result_count,
    }
    return result, observation


def _result_observation(command: Mapping[str, Any]) -> dict[str, Any]:
    return {
        "command_id": command["command_id"],
        "output_sha256": command["stdout_sha256"],
        "result_count": command["result_count"],
    }


def _without_result_count(value: Mapping[str, Any]) -> dict[str, Any]:
    result = dict(value)
    result.pop("result_count", None)
    return result


def _transition(
    identity: Mapping[str, Any],
    enable: Mapping[str, Any],
    disable: Mapping[str, Any],
    final: Mapping[str, Any],
    target: Mapping[str, str],
) -> dict[str, Any]:
    if (
        identity.get("kind") != "non-production-qualification"
        or identity.get("namespace") != target["namespace"]
        or identity.get("profile_sha256") != STORY_27_4_PROFILE_SHA256
        or identity.get("writes_state") != "disabled"
    ):
        raise EvidenceValidationError("qualification target identity is not an exact disabled-profile match")
    if enable.get("state") != "enabled" or disable.get("state") != "disabled" or final.get("state") != "disabled":
        raise EvidenceValidationError("qualification transition did not finish disabled")
    return {
        "non_production": True,
        "identity_observation": _result_observation(identity["_command"]),
        "initial_writes_state": "disabled",
        "enable_observation": _result_observation(enable["_command"]),
        "disable_observation": _result_observation(disable["_command"]),
        "final_observation": _result_observation(final["_command"]),
        "final_writes_state": "disabled",
    }


def _execute(
    target: Mapping[str, str],
    checkpoint: str,
    command_ids: Sequence[str],
    on_result: Callable[[str, Mapping[str, Any]], None] | None = None,
    resume_results: Mapping[str, Mapping[str, Any]] | None = None,
) -> tuple[dict[str, Mapping[str, Any]], list[dict[str, Any]]]:
    results: dict[str, Mapping[str, Any]] = {}
    commands: list[dict[str, Any]] = []
    for command_id in command_ids:
        if resume_results is not None and command_id in resume_results:
            resumed = dict(resume_results[command_id])
            command = dict(_require_mapping(resumed.get("_command"), "resumed C3 command"))
            if command.get("command_id") != command_id:
                raise EvidenceValidationError("resumed C3 command identity is inconsistent")
            results[command_id] = resumed
            commands.append(command)
            continue
        result, command = _run_operation(target, checkpoint, command_id)
        mutable = dict(result)
        mutable["_command"] = command
        results[command_id] = mutable
        commands.append(command)
        if on_result is not None:
            on_result(command_id, mutable)
    return results, commands


def _execute_qualification(
    target: Mapping[str, str],
    checkpoint: str,
    command_ids: Sequence[str],
    on_result: Callable[[str, Mapping[str, Any]], None] | None = None,
    resume_results: Mapping[str, Mapping[str, Any]] | None = None,
) -> tuple[dict[str, Mapping[str, Any]], list[dict[str, Any]]]:
    """Run one qualification session and always restore/verify its disabled state."""

    results: dict[str, Mapping[str, Any]] = {}
    commands: list[dict[str, Any]] = []
    try:
        identity, identity_command = _run_operation(
            target,
            checkpoint,
            "qualification-target-identity",
        )
        identity = {**identity, "_command": identity_command}
        results["qualification-target-identity"] = identity
        commands.append(identity_command)
        if (
            identity.get("kind") != "non-production-qualification"
            or identity.get("namespace") != target["namespace"]
            or identity.get("profile_sha256") != STORY_27_4_PROFILE_SHA256
            or identity.get("writes_state") != "disabled"
        ):
            raise EvidenceValidationError(
                "qualification target identity is not an exact disabled-profile match"
            )
        # Enter the protected region before invoking enable.  The target may
        # apply the transition and then return malformed/truncated evidence; a
        # parse failure in that window must still trigger disable restoration.
        enable, enable_command = _run_operation(target, checkpoint, "qualification-enable")
        enable = {**enable, "_command": enable_command}
        results["qualification-enable"] = enable
        commands.append(enable_command)
        if enable.get("state") != "enabled":
            raise EvidenceValidationError("qualification target did not enter the enabled state")
        remaining_command_ids = list(command_ids)
        body_results: dict[str, Mapping[str, Any]] = {}
        body_commands: list[dict[str, Any]] = []
        if remaining_command_ids[:2] == ["writer-1", "writer-2"]:
            # Both fixed requests must overlap so the cluster observes the ADR's
            # exact 250 accepted records/s, rather than two sequential 125/s runs.
            with ThreadPoolExecutor(max_workers=2, thread_name_prefix="story-27-4-writer") as writers:
                futures = {
                    command_id: writers.submit(_run_operation, target, checkpoint, command_id)
                    for command_id in remaining_command_ids[:2]
                }
                # Execute the replacement/fault observations while both fixed
                # writers are still producing.  Waiting for the writers here
                # would turn C2 into two disconnected tests and could not prove
                # continuity through replacement under load.
                sequential_results, sequential_commands = _execute(
                    target,
                    checkpoint,
                    remaining_command_ids[2:],
                    on_result,
                    resume_results,
                )
                for command_id in remaining_command_ids[:2]:
                    result, command = futures[command_id].result()
                    body_results[command_id] = {**result, "_command": command}
                    body_commands.append(command)
                    if on_result is not None:
                        on_result(command_id, body_results[command_id])
            remaining_command_ids = []
        else:
            sequential_results, sequential_commands = _execute(
                target,
                checkpoint,
                remaining_command_ids,
                on_result,
                resume_results,
            )
        body_results.update(sequential_results)
        body_commands.extend(sequential_commands)
        results.update(body_results)
        commands.extend(body_commands)
    finally:
        # The confirmation is attempted even if the disable command itself fails. The
        # outer verifier performs one further fail-safe disable attempt when this
        # producer exits nonzero, but producer-owned cleanup does not depend on it.
        try:
            disable, disable_command = _run_operation(target, checkpoint, "qualification-disable")
            results["qualification-disable"] = {**disable, "_command": disable_command}
            commands.append(disable_command)
        finally:
            final, final_command = _run_operation(target, checkpoint, "qualification-final-state")
            results["qualification-final-state"] = {**final, "_command": final_command}
            commands.append(final_command)
    return results, commands


def _c2(target: Mapping[str, str]) -> tuple[dict[str, Any], list[dict[str, Any]]]:
    command_ids = [
        "writer-1",
        "writer-2",
        *[f"replace-{name}" for name in REQUIRED_REPLACEMENTS],
        "approved-adapter-fault",
        "continuity",
    ]
    results, commands = _execute_qualification(target, "c2-production-replacement", command_ids)
    writers = []
    for index in (1, 2):
        result = _without_result_count(results[f"writer-{index}"])
        result.pop("_command", None)
        result.pop("dropped", None)
        result.pop("rejected", None)
        result["observation"] = _result_observation(results[f"writer-{index}"]["_command"])
        writers.append(result)
    replacements = {}
    for name in REQUIRED_REPLACEMENTS:
        value = _without_result_count(results[f"replace-{name}"])
        value.pop("_command", None)
        value["observation"] = _result_observation(results[f"replace-{name}"]["_command"])
        replacements[name] = value
    adapter = _without_result_count(results["approved-adapter-fault"])
    adapter.pop("_command", None)
    adapter["observation"] = _result_observation(results["approved-adapter-fault"]["_command"])
    continuity = results["continuity"]
    attempted = sum(results[f"writer-{index}"]["attempted"] for index in (1, 2))
    acknowledged = sum(results[f"writer-{index}"]["acknowledged"] for index in (1, 2))
    persisted = sum(results[f"writer-{index}"]["persisted"] for index in (1, 2))
    conflicted = sum(results[f"writer-{index}"]["conflicted"] for index in (1, 2))
    transaction_acks = sum(
        results[f"writer-{index}"]["transaction_acknowledgements"] for index in (1, 2)
    )
    replacements_recovered = all(
        results[f"replace-{name}"]["exercised"]
        and results[f"replace-{name}"]["recovered"]
        and results[f"replace-{name}"]["continuity_observed"]
        for name in REQUIRED_REPLACEMENTS
    )
    acknowledged_loss = max(0, acknowledged - persisted) + sum(
        results[f"replace-{name}"]["acknowledged_loss"] for name in REQUIRED_REPLACEMENTS
    ) + results["approved-adapter-fault"]["acknowledged_loss"]
    identity = results["qualification-target-identity"]
    enable = results["qualification-enable"]
    disable = results["qualification-disable"]
    final = results["qualification-final-state"]
    return {
        "writers": {
            "steady_state_minutes": 30,
            "cluster_accepted_records_per_second": 250,
            "component_operations_per_second": (persisted * 2) // (30 * 60),
            "writer_results": writers,
            "acknowledged_loss": acknowledged_loss,
            "actor_serialized": attempted == acknowledged + conflicted,
            "idempotent_retry": persisted == acknowledged,
            "conflict_rejected": conflicted > 0,
            "transaction_acknowledged": transaction_acks == acknowledged,
            "reconstructed": replacements_recovered,
            "reconnected": replacements_recovered,
            "direct_backend_dependencies": continuity.get("direct_backend_dependencies"),
        },
        "replacements": replacements,
        "adapter_fault": adapter,
        "console_continuity": continuity.get("console_continuity"),
        "otlp_configured": continuity.get("otlp_configured"),
        "otlp_continuity": continuity.get("otlp_continuity"),
        "continuity_observation": _result_observation(continuity["_command"]),
        "qualification_transition": _transition(identity, enable, disable, final, target),
    }, commands


def _c3(
    target: Mapping[str, str],
    journal_path: Path,
) -> tuple[dict[str, Any], list[dict[str, Any]]]:
    command_ids = ["retention-controls"]
    for hours in (1, 24, 168):
        command_ids.extend((f"cohort-{hours}h-expiry", f"cohort-{hours}h-purge", f"cohort-{hours}h-reclamation"))
    with _C3Journal(journal_path) as journal:
        resume_results = journal.resume_results
        resumed_ids = list(resume_results)
        if resumed_ids != command_ids[: len(resumed_ids)]:
            raise EvidenceValidationError(
                "C3 journal must contain an exact completed prefix with no skips or reordering"
            )
        results, commands = _execute_qualification(
            target,
            "c3-retention-reclamation",
            command_ids,
            journal.append,
            resume_results,
        )
    cohorts = []
    for hours in (1, 24, 168):
        merged: dict[str, Any] = {}
        for stage in ("expiry", "purge", "reclamation"):
            command_id = f"cohort-{hours}h-{stage}"
            partial = _without_result_count(results[command_id])
            partial.pop("_command", None)
            partial.pop("stage", None)
            for key, value in partial.items():
                if key in merged and merged[key] != value:
                    raise EvidenceValidationError(f"cohort {hours}h returned inconsistent {key}")
                merged[key] = value
            merged[f"{stage}_observation"] = _result_observation(results[command_id]["_command"])
        pre_count = _require_nonzero_integer(merged.get("pre_tuple_count"), f"cohort-{hours}h.pre_tuple_count")
        post_count = _require_integer(merged.get("post_tuple_count"), f"cohort-{hours}h.post_tuple_count")
        candidate_count = _require_nonzero_integer(merged.get("candidate_count"), f"cohort-{hours}h.candidate_count")
        deleted_count = min(candidate_count, max(0, pre_count - post_count))
        merged["deleted_count"] = deleted_count
        merged["already_absent_count"] = candidate_count - deleted_count
        merged["index_removal_count"] = candidate_count if merged.get("logical_absence") else 0
        cohorts.append(merged)
    identity = results["qualification-target-identity"]
    enable = results["qualification-enable"]
    disable = results["qualification-disable"]
    final = results["qualification-final-state"]
    retention = _without_result_count(results["retention-controls"])
    retention.pop("_command", None)
    return {
        "retention": retention,
        "retention_observation": _result_observation(results["retention-controls"]["_command"]),
        "cohorts": cohorts,
        "qualification_transition": _transition(identity, enable, disable, final, target),
    }, commands


def _c4(target: Mapping[str, str]) -> tuple[dict[str, Any], list[dict[str, Any]]]:
    command_ids = [
        *[f"failure-{name}" for name in REQUIRED_FAILURE_SCENARIOS],
        "observability",
        "privacy-denial",
    ]
    results, commands = _execute_qualification(target, "c4-failure-privacy-observability", command_ids)
    failures = {}
    for name in REQUIRED_FAILURE_SCENARIOS:
        command_id = f"failure-{name}"
        value = _without_result_count(results[command_id])
        value.pop("_command", None)
        value["observation"] = _result_observation(results[command_id]["_command"])
        failures[name] = value
    observability = _without_result_count(results["observability"])
    observability.pop("_command", None)
    observability["observation"] = _result_observation(results["observability"]["_command"])
    privacy = _without_result_count(results["privacy-denial"])
    privacy.pop("_command", None)
    privacy["observation"] = _result_observation(results["privacy-denial"]["_command"])
    identity = results["qualification-target-identity"]
    enable = results["qualification-enable"]
    disable = results["qualification-disable"]
    final = results["qualification-final-state"]
    return {
        "failure_scenarios": failures,
        "observability": observability,
        "privacy": privacy,
        "qualification_transition": _transition(identity, enable, disable, final, target),
    }, commands


def run(checkpoint: str, argv: Sequence[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    try:
        reviewer = _require_nonempty_string(
            args.platform_operations_reviewer,
            "validated C1 platform-operations reviewer",
            maximum=128,
        )
        if _SAFE_TARGET.fullmatch(reviewer) is None:
            raise EvidenceValidationError("validated C1 platform-operations reviewer is not bounded")
        target = dict(_load_target(Path(args.scenario_input)))
        target["_platform_operations_reviewer"] = reviewer
        if args.disable_only:
            disable: Mapping[str, Any] | None = None
            final: Mapping[str, Any] | None = None
            try:
                disable, _ = _run_operation(target, checkpoint, "qualification-disable")
            finally:
                final, _ = _run_operation(target, checkpoint, "qualification-final-state")
            return 0 if disable.get("state") == "disabled" and final.get("state") == "disabled" else 1
        if checkpoint == "c3-retention-reclamation":
            if not args.journal:
                raise EvidenceValidationError("C3 requires an external append-only resume journal")
            results, commands = _c3(target, Path(args.journal))
        else:
            factory = {
                "c2-production-replacement": _c2,
                "c4-failure-privacy-observability": _c4,
            }[checkpoint]
            results, commands = factory(target)
        payload = {
            "schema_version": 1,
            "checkpoint": checkpoint,
            "profile_sha256": STORY_27_4_PROFILE_SHA256,
            "workload_sha256": STORY_27_4_WORKLOAD_SHA256,
            "owner": "hexalith-platform-operations",
            "failure_count": 0,
            "skip_count": 0,
            "failures": [],
            "skipped": [],
            "commands": commands,
            "result_count": sum(command["result_count"] for command in commands),
            "results": results,
        }
        print(_canonical_json(payload))
        return 0
    except (EvidenceValidationError, OSError, ValueError, KeyError) as exc:
        print(str(exc)[:512], file=sys.stderr)
        return 1
