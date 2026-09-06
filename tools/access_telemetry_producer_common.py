"""Closed, source-bound Story 27.4 qualification scenario producer."""

from __future__ import annotations

import argparse
import base64
from concurrent.futures import FIRST_COMPLETED, Future, ThreadPoolExecutor, wait
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import signal
import stat as stat_module
import sys
import fcntl
import threading
import time
from typing import Any, Callable, Mapping, Sequence

from verify_access_telemetry_lifecycle import (
    REQUIRED_FAILURE_SCENARIOS,
    REQUIRED_LIFECYCLE_SIGNALS,
    REQUIRED_REPLACEMENTS,
    REQUIRED_TENANT_DENIAL_TESTS,
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
    _require_sequence,
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
_RETENTION_PROOF_TESTS = (
    "PersistAsync_WritesRecordAndExpiryIndexAtomicallyWithCeilingTtl",
    "PersistAsync_FutureOrExpiredSource_FailsClosed",
    "AttestAsync_MajorityIntervalWiderThan250Milliseconds_FailsClosed",
    "Verify_ContextProfileOrNonceMismatch_FailsClosed",
    "Verify_ReplayStaleDeltaOrTamperedSignature_FailsClosed",
)
_PRIVACY_PROOF_TESTS = REQUIRED_TENANT_DENIAL_TESTS
_OBSERVABILITY_PROOF_TESTS = (
    "LifecycleCounter_EmitsOnlyBoundedStateAndReasonLabels",
    "LifecycleGauges_UseLiveClockAndAggregateHealthWithoutInventingPhysicalEvidence",
    "HealthPrecedence_IsUnhealthyThenDegradedThenNoDataOrHealthy",
    "RuntimeGate_ClosesImmediatelyWhenPublishedEvidenceExpires",
)
_C2_IDEMPOTENCE_CONFLICT_TESTS = (
    "PersistAsync_SameIdHashAndExpiry_IsIdempotent",
    "PersistAsync_SameIdWithDifferentEnvelopeOrExpiry_ReturnsConflict",
)
_C4_MECHANISM_PROOF_TESTS: Mapping[str, tuple[str, str, tuple[str, ...]]] = {
    "etag-failure": (
        "tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj",
        "tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Debug/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll",
        ("WriteRecordAndIndexAsync_ConcurrentCatalogWriteBetweenReadAndCommit_ThrowsAndCommitsNoPartialState",),
    ),
    "ttl-failure": (
        "tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj",
        "tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Debug/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll",
        ("WriteRecordAndIndexAsync_TtlReapedRecordWithLingeringBucketEntry_ReturnsConflictWithoutResurrection",),
    ),
    "transaction-failure": (
        "tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj",
        "tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Debug/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll",
        ("DeleteAndVerifyAsync_SecondOperationFails_CommitsNoPartialDelete",),
    ),
    "queue-byte-exhaustion": (
        "tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj",
        "tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll",
        ("Queue_DropsNewestAtExactRecordAndByteBounds",),
    ),
    "queue-record-exhaustion": (
        "tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj",
        "tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll",
        ("Queue_DropsNewestAtExactRecordAndByteBounds",),
    ),
}
_MAX_TRANSCRIPT_BYTES = 1_048_576
_QUALIFICATION_SESSION_SECONDS = 15 * 60
_BUSINESS_BEARER_ENVIRONMENT = "HEXALITH_STORY_27_4_BUSINESS_BEARER_FILE"
_BUSINESS_TENANT = "story-27-4-qualification"
_DENIED_TENANT = "story-27-4-denied"
_MAX_BEARER_BYTES = 8_192
_C4_EXPECTED_DISPOSITIONS: Mapping[str, str] = {
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
_TERMINATION_REQUESTED = threading.Event()
_LEASE_MUTATION_LOCK = threading.Lock()
_WRITER_RENEW_INTERVAL_SECONDS = 8 * 60
_WRITER_RENEW_TEST_INTERVAL_SECONDS = 0.25
_REPORTER_COMMAND = ["/bin/sh", "-ec"]
_REPORTER_ARGUMENTS = [
    'wget -qO- --header="dapr-api-token: ${DAPR_API_TOKEN}" '
    '--header="Content-Type: application/json" '
    '--post-file=/evidence/evidence.json '
    'http://127.0.0.1:3500/v1.0/invoke/memories-access-telemetry/'
    'method/v1/access-telemetry/physical-reclamation-evidence'
]
_REPORTER_ENV = [
    {
        "name": "DAPR_API_TOKEN",
        "valueFrom": {"secretKeyRef": {"name": "dapr-api-token", "key": "token"}},
    }
]
_REPORTER_VOLUME_MOUNTS = [{"name": "evidence", "mountPath": "/evidence", "readOnly": True}]
_REPORTER_VOLUMES = [
    {"name": "evidence", "configMap": {"name": "access-telemetry-physical-evidence-report"}}
]


def _load_business_bearer() -> bytes:
    """Load one owner-only short-lived JWT without retaining its path or claims."""

    configured = os.environ.get(_BUSINESS_BEARER_ENVIRONMENT)
    if not configured:
        raise EvidenceValidationError("C4 requires the external qualification bearer file")
    path = Path(configured)
    if not path.is_absolute():
        raise EvidenceValidationError("qualification bearer file must be an absolute non-symlink path")
    descriptor = -1
    try:
        descriptor = os.open(
            path,
            os.O_RDONLY | getattr(os, "O_CLOEXEC", 0) | getattr(os, "O_NOFOLLOW", 0),
        )
        metadata = os.fstat(descriptor)
        payload = os.read(descriptor, _MAX_BEARER_BYTES + 1)
    except OSError as exc:
        raise EvidenceValidationError("qualification bearer file is unavailable") from exc
    finally:
        if descriptor >= 0:
            os.close(descriptor)
    if (
        not stat_module.S_ISREG(metadata.st_mode)
        or metadata.st_uid != os.getuid()
        or metadata.st_mode & 0o077
    ):
        raise EvidenceValidationError("qualification bearer file must be owner-only")
    if not 32 <= len(payload) <= _MAX_BEARER_BYTES or payload != payload.strip():
        raise EvidenceValidationError("qualification bearer is malformed or outside its bound")
    try:
        token = payload.decode("ascii", errors="strict")
        parts = token.split(".")
        if len(parts) != 3 or any(not part for part in parts):
            raise ValueError("JWT segment count")
        encoded_claims = parts[1] + "=" * (-len(parts[1]) % 4)
        claims = _require_mapping(
            _json_without_duplicates(
                base64.urlsafe_b64decode(encoded_claims.encode("ascii")).decode(
                    "utf-8", errors="strict"
                ),
                "qualification bearer claims",
            ),
            "qualification bearer claims",
        )
    except (UnicodeDecodeError, ValueError) as exc:
        raise EvidenceValidationError("qualification bearer is not a canonical JWT") from exc
    expires = claims.get("exp")
    now_seconds = int(time.time())
    if type(expires) is not int or expires <= now_seconds + 60 or expires > now_seconds + 3_600:
        raise EvidenceValidationError("qualification bearer is stale or not short-lived")
    tenant_claim = claims.get("tenant_id", claims.get("tenants"))
    if isinstance(tenant_claim, str):
        tenants = tenant_claim.split()
    elif isinstance(tenant_claim, list) and all(isinstance(value, str) for value in tenant_claim):
        tenants = tenant_claim
    else:
        tenants = []
    if tenants != [_BUSINESS_TENANT]:
        raise EvidenceValidationError("qualification bearer is missing or exceeds the fixed tenant authority")
    return payload + b"\n"


def _business_probe_command(
    target: Mapping[str, str],
    command_id: str,
    *,
    privacy: bool = False,
) -> tuple[str, ...]:
    """Build the fixed in-pod request that receives its bearer only on stdin."""

    prefix = _kubectl_prefix(target)
    correlation = f"story-27-4-{command_id}"
    if privacy:
        request_script = (
            "IFS= read -r bearer; "
            "dependencies() { wget -qO- http://127.0.0.1:3500/metrics "
            "| awk '/^dapr_http_client_completed_count/{sum+=$NF} END{print sum+0}'; } ; "
            "status() { wget -S -qO /dev/null --header=\"Authorization: Bearer $bearer\" "
            f"--header=\"X-Hexalith-Qualification-Run: {correlation}\" \"$1\" 2>&1 "
            "| awk '/HTTP\\/{code=$2} END{print code+0}'; } ; "
            f"allowed=$(status http://127.0.0.1:8080/api/v1/tenants/{_BUSINESS_TENANT}); "
            "before_denial=$(dependencies); "
            f"denied=$(status http://127.0.0.1:8080/api/v1/tenants/{_DENIED_TENANT}); "
            "after_denial=$(dependencies); delta=$((after_denial-before_denial)); "
            "printf '{\"allowed_status\":%s,\"denied_status\":%s,\"denied_dependency_calls\":%s}' "
            "\"$allowed\" \"$denied\" \"$delta\""
        )
    else:
        request_script = (
            "IFS= read -r bearer; "
            "code=$(wget -S -qO /dev/null --header=\"Authorization: Bearer $bearer\" "
            f"--header=\"X-Hexalith-Qualification-Run: {correlation}\" "
            "http://127.0.0.1:8080/api/v1/handlers 2>&1 "
            "| awk '/HTTP\\/{value=$2} END{print value+0}'); "
            "printf '{\"business_status\":%s}' \"$code\""
        )
    return (
        "__BUSINESS_BEARER_STDIN__",
        *prefix,
        "exec",
        "-i",
        "pod/__SERVER_POD_0__",
        "-c",
        "memories",
        "--",
        "/bin/sh",
        "-ec",
        request_script,
    )


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


def _fixed_workload_shell(target: Mapping[str, str], command_id: str) -> str:
    """Build the bodyless invocation with bounded, retry-stable correlation."""

    lease_holder = _require_nonempty_string(
        target.get("_lease_holder"), "qualification lease holder", maximum=256
    )
    run_id = f"run-{_sha256(lease_holder)[:24]}"
    segment_id = f"{command_id}-segment-0001"
    if len(segment_id) > 64 or re.fullmatch(r"[a-z0-9][a-z0-9-]{0,63}", segment_id) is None:
        raise EvidenceValidationError("qualification workload segment identity is invalid")
    return (
        'wget -qO- --header="dapr-api-token: $APP_API_TOKEN" '
        f'--header="X-Hexalith-Qualification-Run: {run_id}" '
        f'--header="X-Hexalith-Qualification-Segment: {segment_id}" '
        f'--header="X-Hexalith-Qualification-Emitted-Utc-Ms: {_utc_now_milliseconds()}" '
        f'--post-data="" {_FIXED_WORKLOAD_ROUTE}'
    )


def _qualification_record_ids(run_id: str, segment_id: str, count: int = 125) -> list[str]:
    """Derive the exact retry-stable Crockford identities emitted by one segment."""

    correlation = hashlib.sha256(f"{run_id}/{segment_id}".encode("utf-8")).hexdigest()[:32]
    alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"
    result: list[str] = []
    for ordinal in range(count):
        digest = bytearray(
            hashlib.sha256(
                f"qualification-{correlation}-{ordinal:03d}".encode("utf-8")
            ).digest()
        )
        digest[0] &= 0x7f
        value = int.from_bytes(digest[:16], byteorder="big", signed=False)
        encoded = ["0"] * 26
        for index in range(25, -1, -1):
            value, remainder = divmod(value, 32)
            encoded[index] = alphabet[remainder]
        result.append("".join(encoded))
    return result


def _fixed_test_commands(
    project: str,
    assembly: str,
    methods: Sequence[str],
) -> list[tuple[str, ...]]:
    invocation: list[str] = ["dotnet", assembly]
    for method in methods:
        invocation.extend(("-method", f"*{method}"))
    invocation.extend(("-parallelMode", "none", "-noLogo", "-reporter", "verbose"))
    return [
        ("dotnet", "build", project, "--no-restore"),
        tuple(invocation),
    ]


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
            (*prefix, "get", "deployments", "-o", "json"),
            (*prefix, "get", "components.dapr.io", "-o", "json"),
            (*prefix, "get", "configurations.dapr.io", "-o", "json"),
            (*prefix, "get", "statefulsets", "-o", "json"),
            (*prefix, "get", "job", "access-telemetry-physical-evidence-reporter", "-o", "json"),
            (*prefix, "get", "pods", "-o", "json"),
            (*prefix, "get", "serviceaccounts", "-o", "json"),
            (*_kubectl_prefix(target, "dapr-system"), "get", "deployments", "-o", "json"),
            (*_kubectl_prefix(target, "dapr-system"), "get", "statefulsets", "-o", "json"),
            (*_kubectl_prefix(target, "dapr-system"), "get", "pods", "-o", "json"),
            (*_kubectl_prefix(target, "dapr-system"), "get", "serviceaccounts", "-o", "json"),
        ]
    if command_id == "qualification-enable":
        expires_utc_ms = _utc_now_milliseconds() + _QUALIFICATION_SESSION_SECONDS * 1000
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
                {"op": "replace", "path": "/spec/holderIdentity", "value": target["_lease_holder"]},
                {"op": "replace", "path": "/spec/leaseDurationSeconds", "value": _QUALIFICATION_SESSION_SECONDS},
                {
                    "op": "add",
                    "path": "/spec/acquireTime",
                    "value": datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z"),
                },
            ]
        )
        return [
            (*prefix, "auth", "can-i", "patch", f"lease/{_LEASE_NAME}"),
            (*prefix, "auth", "can-i", "patch", f"configmap/{_GATE_NAME}"),
            (*prefix, "auth", "can-i", "update", "deployment/memories-access-telemetry", "--subresource=scale"),
            (*prefix, "auth", "can-i", "update", "deployment/memories-access-telemetry-clock", "--subresource=scale"),
            (*prefix, "auth", "can-i", "patch", "deployment/memories"),
            (*prefix, "auth", "can-i", "patch", "deployment/memories-access-telemetry"),
            (*prefix, "auth", "can-i", "patch", "deployment/memories-access-telemetry-clock"),
            (*prefix, "auth", "can-i", "update", "statefulset/access-telemetry-postgresql", "--subresource=scale"),
            (*prefix, "auth", "can-i", "delete", "pods"),
            (*prefix, "auth", "can-i", "create", "pods", "--subresource=exec"),
            (*prefix, "auth", "can-i", "get", "pods", "--subresource=log"),
            (*prefix, "auth", "can-i", "patch", "configmap/access-telemetry-physical-evidence-report"),
            (*prefix, "auth", "can-i", "patch", "job/access-telemetry-physical-evidence-reporter"),
            (*_kubectl_prefix(target, "dapr-system"), "auth", "can-i", "delete", "pods"),
            (*_kubectl_prefix(target, "dapr-system"), "auth", "can-i", "update",
             "statefulset/dapr-scheduler-server", "--subresource=scale"),
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
    if command_id == "qualification-renew":
        return [
            (*prefix, "get", "lease", _LEASE_NAME, "-o", "json"),
            (*prefix, "patch", "lease", _LEASE_NAME, "--type=json", "--patch", "__LEASE_RENEW_PATCH__"),
            (*prefix, "patch", "configmap", _GATE_NAME, "--type=merge", "--patch", "__GATE_RENEW_PATCH__"),
            (*prefix, "get", "lease", _LEASE_NAME, "-o", "json"),
            (*prefix, "get", "configmap", _GATE_NAME, "-o", "json"),
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
        return [
            (*prefix, "get", "lease", _LEASE_NAME, "-o", "json"),
            (*prefix, "patch", "configmap", _GATE_NAME, "--type=merge", "--patch", gate_patch),
            (*prefix, "scale", "deployment/memories-access-telemetry", "--replicas=0"),
            (*prefix, "scale", "deployment/memories-access-telemetry-clock", "--replicas=0"),
            (*prefix, "patch", "lease", _LEASE_NAME, "--type=json", "--patch", "__LEASE_RELEASE_PATCH__"),
        ]
    if command_id == "qualification-final-state":
        return [
            (*prefix, "get", "configmap", _GATE_NAME, "-o", "json"),
            (*prefix, "get", "lease", _LEASE_NAME, "-o", "json"),
            (*prefix, "get", "deployment", "memories-access-telemetry", "-o", "json"),
            (*prefix, "get", "deployment", "memories-access-telemetry-clock", "-o", "json"),
            (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
            (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "memories", "--", "/bin/sh", "-ec",
             "cat /var/run/hexalith/access-telemetry-qualification/gate.json"),
            (*prefix, "exec", "pod/__SERVER_POD_1__", "-c", "memories", "--", "/bin/sh", "-ec",
             "cat /var/run/hexalith/access-telemetry-qualification/gate.json"),
        ]
    if command_id == "component-throughput":
        query = (
            "http://prometheus-operated.monitoring.svc.cluster.local:9090/api/v1/query?"
            "query=sum%28increase%28memories_access_telemetry_lifecycle_state_operations_total%5B30m%5D%29%29"
        )
        return [
            (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
            (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "memories", "--", "/bin/sh", "-ec",
             f"wget -qO- '{query}'"),
        ]
    if command_id == "idempotence-conflict-proof":
        return _fixed_test_commands(
            "tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj",
            "tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Debug/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll",
            _C2_IDEMPOTENCE_CONFLICT_TESTS,
        )
    if command_id == "c3-empty-preflight":
        return [
            (*prefix, "exec", "statefulset/access-telemetry-postgresql", "-c", "postgresql", "--",
             "psql", "--no-psqlrc", "--tuples-only", "--no-align",
             "--dbname=memories_access_telemetry", "--command",
             "SELECT json_build_object('stage','preflight','record_count',count(*),"
             "'index_candidate_count',count(*) FILTER (WHERE expiredate IS NOT NULL)) "
             "FROM access_telemetry.lifecycle_state WHERE key LIKE 'memories-access-telemetry||records/%';"),
        ]
    if command_id == "newer-control-seed":
        control_sql = (
            "WITH records AS (SELECT key,convert_from(value,'UTF8')::jsonb AS doc,expiredate "
            "FROM access_telemetry.lifecycle_state WHERE key LIKE 'memories-access-telemetry||records/%'), "
            "control AS (SELECT key,doc->>'recordId' AS record_id FROM records "
            "WHERE expiredate>clock_timestamp() AND "
            "(doc->>'expiresAtUtc')::timestamptz-(doc->>'emittedAtUtc')::timestamptz="
            "make_interval(hours=>24)) "
            "SELECT json_build_object('stage','control','record_count',count(*),"
            "'newer_record_names',coalesce((SELECT json_agg(record_id ORDER BY record_id) "
            "FROM control),'[]'::json)) FROM control;"
        )
        return [
            (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
            (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "memories", "--", "/bin/sh", "-ec",
             _fixed_workload_shell(target, command_id)),
            (*prefix, "exec", "statefulset/access-telemetry-postgresql", "-c", "postgresql", "--",
             "psql", "--no-psqlrc", "--tuples-only", "--no-align",
             "--dbname=memories_access_telemetry", "--command", control_sql),
        ]
    if command_id == "cohort-168h-report":
        return [
            (*prefix, "get", "job", "access-telemetry-physical-evidence-reporter", "-o", "json"),
            (*prefix, "get", "configmap", "access-telemetry-physical-evidence-report", "-o", "json"),
            (*prefix, "patch", "configmap", "access-telemetry-physical-evidence-report", "--type=merge",
             "--patch", "__PHYSICAL_EVIDENCE_PATCH__"),
            (*prefix, "patch", "job", "access-telemetry-physical-evidence-reporter", "--type=merge",
             "--patch", _canonical_json({"spec": {"suspend": False}})),
            (*prefix, "wait", "job/access-telemetry-physical-evidence-reporter",
             "--for=condition=Complete", "--timeout=300s"),
            (*prefix, "logs", "job/access-telemetry-physical-evidence-reporter", "-c", "reporter", "--tail=20"),
        ]
    if command_id.startswith("cohort-"):
        hours = int(command_id.split("-", 2)[1][:-1])
        stage = command_id.rsplit("-", 1)[-1]
        stored_cohorts = target.get("_c3_cohort_record_ids")
        record_ids = (
            stored_cohorts.get(hours)
            if isinstance(stored_cohorts, Mapping)
            else None
        )
        exact_records = ""
        if stage != "seed":
            if not isinstance(record_ids, list) or len(record_ids) != 125 or any(
                not isinstance(record_id, str) or
                re.fullmatch(r"[0-9A-HJKMNP-TV-Z]{26}", record_id) is None
                for record_id in record_ids
            ):
                raise EvidenceValidationError(f"cohort {hours}h exact record identity is unavailable")
            exact_records = ",".join(f"'{record_id}'" for record_id in record_ids)
            cohort_predicate = f"doc->>'recordId' IN ({exact_records})"
        else:
            cohort_predicate = (
                "(doc->>'expiresAtUtc')::timestamptz-(doc->>'emittedAtUtc')::timestamptz="
                f"make_interval(hours=>{hours})"
            )
        # Dapr's PostgreSQL v2 state table stores the canonical record JSON in
        # value and the component TTL in expiredate.  Each statement emits one
        # aggregate JSON object; identifiers and horizons are closed here, not
        # supplied by an operator.
        cohort = (
            "WITH records AS (SELECT key,convert_from(value,'UTF8')::jsonb AS doc,expiredate "
            "FROM access_telemetry.lifecycle_state WHERE key LIKE 'memories-access-telemetry||records/%'), "
            "cohort AS (SELECT * FROM records WHERE " + cohort_predicate + "), "
            "newer AS (SELECT key,doc->>'recordId' AS record_id "
            "FROM records WHERE expiredate>clock_timestamp()) "
        )
        sql = {
            "seed": (
                cohort + "SELECT json_build_object('stage','seed','retention_hours'," + str(hours) +
                ",'cohort_id','retention-" + str(hours) + "h','database',current_database(),"
                "'schema','access_telemetry','table','lifecycle_state',"
                "'accepted_utc_ms',(extract(epoch FROM min((doc->>'acceptedAtUtc')::timestamptz))*1000)::bigint,"
                "'emitted_utc_ms',(extract(epoch FROM min((doc->>'emittedAtUtc')::timestamptz))*1000)::bigint,"
                "'expires_utc_ms',(extract(epoch FROM min((doc->>'expiresAtUtc')::timestamptz))*1000)::bigint,"
                "'pre_tuple_count',count(*),'record_ids',coalesce(json_agg(doc->>'recordId' "
                "ORDER BY doc->>'recordId'),'[]'::json)) FROM cohort;"
            ),
            "wait": (
                cohort + "SELECT json_build_object('stage','wait','retention_hours'," + str(hours) +
                ",'cohort_id','retention-" + str(hours) + "h','ready',"
                "count(*)=0 OR coalesce(min(expiredate)<=clock_timestamp(),false),'cohort_missing',count(*)=0,"
                "'candidate_count',"
                "count(*) FILTER (WHERE expiredate<=clock_timestamp())) FROM cohort;"
            ),
            "expiry": (
                cohort + "SELECT json_build_object('stage','expiry','retention_hours'," + str(hours) +
                ",'cohort_id','retention-" + str(hours) + "h','database',current_database()," 
                "'schema','access_telemetry','table','lifecycle_state'," 
                "'accepted_utc_ms',(extract(epoch FROM min((doc->>'acceptedAtUtc')::timestamptz))*1000)::bigint," 
                "'emitted_utc_ms',(extract(epoch FROM min((doc->>'emittedAtUtc')::timestamptz))*1000)::bigint,"
                "'expires_utc_ms',(extract(epoch FROM min((doc->>'expiresAtUtc')::timestamptz))*1000)::bigint," 
                "'pre_tuple_count',count(*),'candidate_count',count(*) FILTER (WHERE expiredate<=clock_timestamp())," 
                "'newer_record_names',coalesce((SELECT json_agg(record_id ORDER BY record_id) FROM newer),'[]'::json),"
                "'newer_records_preserved',(SELECT count(*)>0 FROM newer)) FROM cohort;"
            ),
            "purge": (
                cohort + "SELECT json_build_object('stage','purge','purged_utc_ms'," 
                "(extract(epoch FROM clock_timestamp())*1000)::bigint,'post_tuple_count',count(*)," 
                "'logical_absence',count(*)=0,'newer_records_preserved',(SELECT count(*)>0 FROM newer)) FROM cohort;"
            ),
            "reclamation": (
                "SELECT json_build_object('stage','reclamation','reclaimed_utc_ms'," 
                "(extract(epoch FROM clock_timestamp())*1000)::bigint,'allocator_free_bytes',"
                "(SELECT free_space FROM pgstattuple('access_telemetry.lifecycle_state')));"
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
        if stage == "wait":
            # Keep the qualification gate closed while the host process waits.
            # Polling here lets the runner capture the first due observation
            # instead of asking an operator to resume inside Dapr's TTL cleanup
            # interval. Only the final aggregate is written to the transcript.
            wait_deadline_seconds = hours * 3_600 + 900
            wait_script = (
                f"deadline=$(( $(date +%s) + {wait_deadline_seconds} )); "
                "while :; do "
                "result=$(psql --no-psqlrc --tuples-only --no-align "
                f"--dbname=memories_access_telemetry --command \"{sql}\"); "
                "if printf '%s' \"$result\" | grep -Eq '\"ready\"[[:space:]]*:[[:space:]]*true'; then "
                "printf '%s\\n' \"$result\"; exit 0; fi; "
                "if [ \"$(date +%s)\" -ge \"$deadline\" ]; then "
                "printf '%s\\n' \"$result\"; exit 1; fi; sleep 5; done"
            )
            sql_command = (
                *prefix,
                "exec",
                "statefulset/access-telemetry-postgresql",
                "-c",
                "postgresql",
                "--",
                "/bin/sh",
                "-ec",
                wait_script,
            )
        index_sql_command = (
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
            "WITH buckets AS (SELECT convert_from(value,'UTF8')::jsonb AS doc FROM "
            "access_telemetry.lifecycle_state WHERE key LIKE "
            "'memories-access-telemetry||expiry-bucket/%'), entries AS (SELECT "
            "jsonb_array_elements(doc->'entries') AS entry FROM buckets) SELECT "
            "json_build_object('stage','index','index_name','dapr-expiry-bucket-json',"
            "'post_index_candidate_count',(SELECT count(*) FROM entries WHERE "
            "entry->>'recordId' IN (" + exact_records + ")));",
        )
        commands = [
            sql_command
        ]
        if stage == "seed":
            commands = [
                ("__FAULT_ACTION__", *prefix, "exec", "statefulset/redis-stack", "-c", "redis", "--", "/bin/sh", "-ec",
                 f'redis-cli -a "$REDIS_PASSWORD" --no-auth-warning SET retentionSeconds {hours * 3600} | grep -qx OK'),
                ("__FAULT_ACTION__", *prefix, "rollout", "restart", "deployment/memories"),
                ("__FAULT_ACTION__", *prefix, "rollout", "status", "deployment/memories", "--timeout=300s"),
                (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
                (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "memories", "--", "/bin/sh", "-ec",
                 _fixed_workload_shell(target, command_id)),
                sql_command,
                ("__FAULT_RESTORE__", *prefix, "exec", "statefulset/redis-stack", "-c", "redis", "--", "/bin/sh", "-ec",
                 'redis-cli -a "$REDIS_PASSWORD" --no-auth-warning SET retentionSeconds 86400 | grep -qx OK'),
                ("__FAULT_RESTORE__", *prefix, "rollout", "restart", "deployment/memories"),
                ("__FAULT_RESTORE__", *prefix, "rollout", "status", "deployment/memories", "--timeout=300s"),
                ("__FAULT_RESTORE__", *prefix, "exec", "statefulset/redis-stack", "-c", "redis", "--", "/bin/sh", "-ec",
                 'test "$(redis-cli -a "$REDIS_PASSWORD" --no-auth-warning GET retentionSeconds)" = 86400'),
            ]
        if stage == "purge":
            cohort_remaining_sql = (
                "SELECT count(*) FROM access_telemetry.lifecycle_state WHERE key LIKE "
                "'memories-access-telemetry||records/%' AND "
                "convert_from(value,'UTF8')::jsonb->>'recordId' IN (" + exact_records + ");"
            )
            commands = [
                (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories-access-telemetry", "-o", "json"),
                (*prefix, "delete", "pod", "__SELECTED_POD__", "--wait=true", "--timeout=300s"),
                (*prefix, "wait", "pod", "-l", "app.kubernetes.io/name=memories-access-telemetry", "--for=condition=Ready", "--timeout=300s"),
                (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories-access-telemetry", "-o", "json"),
                (*prefix, "exec", "statefulset/access-telemetry-postgresql", "-c", "postgresql", "--",
                 "/bin/sh", "-ec", "for attempt in $(seq 1 180); do "
                 "remaining=$(psql --no-psqlrc --tuples-only --no-align "
                 "--dbname=memories_access_telemetry --command \"" + cohort_remaining_sql + "\"); "
                 "[ \"$remaining\" = 0 ] && exit 0; sleep 5; done; exit 1"),
                sql_command,
                index_sql_command,
                (
                    *prefix,
                    "exec",
                    "deployment/memories-access-telemetry",
                    "-c",
                    "lifecycle",
                    "--",
                    "/bin/sh",
                    "-ec",
                    "count=0; for id in " + " ".join(record_ids) + "; do "
                    "value=$(wget -qO- --header=\"dapr-api-token: $APP_API_TOKEN\" "
                    "\"http://127.0.0.1:3500/v1.0/state/access-telemetry-store/records%2F$id?consistency=strong\"); "
                    "[ -z \"$value\" ] || exit 1; count=$((count+1)); done; "
                    "printf '{\"stage\":\"strong-absence\",\"strong_absent_read_count\":%s}' \"$count\"",
                ),
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
        return commands
    if command_id == "retention-controls":
        return [
            *_fixed_test_commands(
                "tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj",
                "tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Debug/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll",
                _RETENTION_PROOF_TESTS,
            ),
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
        replacement_targets: Mapping[str, tuple[str, str, str]] = {
            "actor-activation": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry", "lifecycle"),
            "clock-service": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry-clock", "clock"),
            "clock-service-dapr-sidecar": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry-clock", "clock"),
            "lifecycle-service": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry", "lifecycle"),
            "lifecycle-service-dapr-sidecar": (target["namespace"], "app.kubernetes.io/name=memories-access-telemetry", "lifecycle"),
            "placement-member-1": ("dapr-system", "app=dapr-placement-server", "dapr-placement-server"),
            "placement-member-2": ("dapr-system", "app=dapr-placement-server", "dapr-placement-server"),
            "placement-member-3": ("dapr-system", "app=dapr-placement-server", "dapr-placement-server"),
            "scheduler-member-1": ("dapr-system", "app=dapr-scheduler-server", "dapr-scheduler-server"),
            "scheduler-member-2": ("dapr-system", "app=dapr-scheduler-server", "dapr-scheduler-server"),
            "scheduler-member-3": ("dapr-system", "app=dapr-scheduler-server", "dapr-scheduler-server"),
            "server-writer-1": (target["namespace"], "app.kubernetes.io/name=memories", "memories"),
            "server-writer-1-dapr-sidecar": (target["namespace"], "app.kubernetes.io/name=memories", "memories"),
            "server-writer-2": (target["namespace"], "app.kubernetes.io/name=memories", "memories"),
            "server-writer-2-dapr-sidecar": (target["namespace"], "app.kubernetes.io/name=memories", "memories"),
            # PG-ONPREM-1's approved adapter is the state.postgresql/v2
            # component backed by this StatefulSet.  The short-lived physical
            # evidence reporter is not the adapter and is normally suspended,
            # so targeting its label would make this scenario non-executable.
            "approved-adapter-fault": (target["namespace"], "app.kubernetes.io/name=access-telemetry-postgresql", "postgresql"),
        }
        if replacement not in replacement_targets:
            raise EvidenceValidationError(f"replacement {replacement} is not in the closed target registry")
        namespace, selector, main_container = replacement_targets[replacement]
        replacement_prefix = _kubectl_prefix(target, namespace)
        commands = [
            *_fixed_operation_commands(target, "qualification", "qualification-renew"),
            (*prefix, "exec", "deployment/memories-access-telemetry", "-c", "lifecycle", "--", "/bin/sh", "-ec",
             "wget -qO- --header=\"dapr-api-token: $APP_API_TOKEN\" http://127.0.0.1:8080/v1/access-telemetry/inspect"),
            (*replacement_prefix, "get", "pods", "-l", selector, "-o", "json"),
        ]
        if command_id.endswith("-dapr-sidecar"):
            commands.append(
                (
                    *replacement_prefix,
                    "exec",
                    "pod/__SELECTED_POD__",
                    "-c",
                    main_container,
                    "--",
                    "/bin/sh",
                    "-ec",
                    'wget -qO- --header="dapr-api-token: ${DAPR_API_TOKEN}" --post-data="" http://127.0.0.1:3500/v1.0/shutdown',
                )
            )
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
        lifecycle = "deployment/memories-access-telemetry"
        clock = "deployment/memories-access-telemetry-clock"
        server = "deployment/memories"
        state = "statefulset/access-telemetry-postgresql"
        targets = {
            "lifecycle": "app.kubernetes.io/name=memories-access-telemetry",
            "clock": "app.kubernetes.io/name=memories-access-telemetry-clock",
            "server": "app.kubernetes.io/name=memories",
            "state": "app.kubernetes.io/name=access-telemetry-postgresql",
        }

        def scale_plan(resource: str, replicas: int, selector_name: str) -> tuple[str, str, list[tuple[str, ...]], list[tuple[str, ...]]]:
            return (
                target["namespace"],
                targets[selector_name],
                [
                    ("kubectl", *prefix[1:], "scale", resource, "--replicas=0"),
                    ("kubectl", *prefix[1:], "wait", "pod", "-l", targets[selector_name],
                     "--for=delete", "--timeout=300s"),
                ],
                [
                    ("kubectl", *prefix[1:], "scale", resource, f"--replicas={replicas}"),
                    ("kubectl", *prefix[1:], "rollout", "status", resource, "--timeout=300s"),
                ],
            )

        def environment_plan(resource: str, selector_name: str, setting: str) -> tuple[str, str, list[tuple[str, ...]], list[tuple[str, ...]]]:
            return (
                target["namespace"],
                targets[selector_name],
                [
                    ("kubectl", *prefix[1:], "set", "env", resource, setting),
                    ("kubectl", *prefix[1:], "delete", "pod", "-l", targets[selector_name],
                     "--wait=true", "--timeout=300s"),
                ],
                [
                    ("kubectl", *prefix[1:], "rollout", "undo", resource),
                    ("kubectl", *prefix[1:], "rollout", "status", resource, "--timeout=300s"),
                ],
            )

        delete_lifecycle = (
            target["namespace"], targets["lifecycle"],
            [(*prefix, "delete", "pod", "__SELECTED_POD__", "--wait=true", "--timeout=300s")],
            [(*prefix, "wait", "pod", "-l", targets["lifecycle"], "--for=condition=Ready", "--timeout=300s")],
        )
        delete_state = (
            target["namespace"], targets["state"],
            [(*prefix, "delete", "pod", "__SELECTED_POD__", "--wait=true", "--timeout=300s")],
            [(*prefix, "wait", "pod", "-l", targets["state"], "--for=condition=Ready", "--timeout=300s")],
        )
        plans: Mapping[str, tuple[str, str, list[tuple[str, ...]], list[tuple[str, ...]]]] = {
            "application-outage": scale_plan(lifecycle, 2, "lifecycle"),
            "state-outage": scale_plan(state, 1, "state"),
            "clock-outage": scale_plan(clock, 1, "clock"),
            "dapr-outage": (
                target["namespace"], targets["lifecycle"],
                [(*prefix, "exec", "pod/__SELECTED_POD__", "-c", "lifecycle", "--", "/bin/sh", "-ec",
                  'wget -qO- --header="dapr-api-token: ${DAPR_API_TOKEN}" --post-data="" http://127.0.0.1:3500/v1.0/shutdown'),
                 (*prefix, "wait", "--for=condition=Ready=false", "pod/__SELECTED_POD__", "--timeout=300s")],
                [(*prefix, "wait", "pod", "-l", targets["lifecycle"], "--for=condition=Ready", "--timeout=300s")],
            ),
            "actor-failover": delete_lifecycle,
            "approved-adapter-fault": delete_state,
            "bad-configuration": environment_plan(lifecycle, "lifecycle", "AccessTelemetryLifecycle__Retention=00:00:01"),
            "bad-key": environment_plan(lifecycle, "lifecycle", "AccessTelemetryLifecycle__AttestationVerificationKey=invalid"),
            "capacity-pressure": (
                target["namespace"], targets["state"],
                [(*prefix, "set", "resources", state, "--requests=memory=1Mi", "--limits=memory=1Mi"),
                 (*prefix, "wait", "--for=delete", "pod/__SELECTED_POD__", "--timeout=300s")],
                [(*prefix, "rollout", "undo", state), (*prefix, "rollout", "status", state, "--timeout=300s")],
            ),
            "degraded-rollback": environment_plan(lifecycle, "lifecycle", "AccessTelemetryLifecycle__Enabled=false"),
            "etag-failure": environment_plan(lifecycle, "lifecycle", "AccessTelemetryLifecycle__StateStoreName=qualification-etag-failure"),
            "profile-drift": environment_plan(lifecycle, "lifecycle", f"AccessTelemetryLifecycle__ComponentProfileHash={'0' * 64}"),
            "queue-byte-exhaustion": (
                target["namespace"], targets["server"],
                [(*prefix, "set", "env", server, "AccessTelemetryLifecycle__QueueByteLimit=1024"),
                 (*prefix, "rollout", "status", server, "--timeout=300s")],
                [(*prefix, "rollout", "undo", server),
                 (*prefix, "rollout", "status", server, "--timeout=300s")],
            ),
            "queue-record-exhaustion": (
                target["namespace"], targets["server"],
                [(*prefix, "set", "env", server, "AccessTelemetryLifecycle__QueueRecordLimit=1"),
                 (*prefix, "rollout", "status", server, "--timeout=300s")],
                [(*prefix, "rollout", "undo", server),
                 (*prefix, "rollout", "status", server, "--timeout=300s")],
            ),
            "reconnect": (
                target["namespace"], targets["lifecycle"],
                [(*prefix, "rollout", "restart", lifecycle),
                 (*prefix, "rollout", "status", lifecycle, "--timeout=300s")],
                [(*prefix, "rollout", "status", lifecycle, "--timeout=300s")],
            ),
            "reminder-delay": (
                "dapr-system", "app=dapr-scheduler-server",
                [(*_kubectl_prefix(target, "dapr-system"), "scale", "statefulset/dapr-scheduler-server", "--replicas=0"),
                 (*_kubectl_prefix(target, "dapr-system"), "wait", "pod", "-l", "app=dapr-scheduler-server", "--for=delete", "--timeout=300s")],
                [(*_kubectl_prefix(target, "dapr-system"), "scale", "statefulset/dapr-scheduler-server", "--replicas=3"),
                 (*_kubectl_prefix(target, "dapr-system"), "rollout", "status", "statefulset/dapr-scheduler-server", "--timeout=300s")],
            ),
            "retry-exhaustion": (
                target["namespace"], targets["lifecycle"],
                [(*prefix, "set", "env", server, "AccessTelemetryLifecycle__RetryMaximumDelay=00:00:00.100"),
                 (*prefix, "rollout", "status", server, "--timeout=300s"),
                 (*prefix, "scale", lifecycle, "--replicas=0"),
                 (*prefix, "wait", "pod", "-l", targets["lifecycle"], "--for=delete", "--timeout=300s")],
                [(*prefix, "scale", lifecycle, "--replicas=2"),
                 (*prefix, "rollout", "status", lifecycle, "--timeout=300s"),
                 (*prefix, "rollout", "undo", server),
                 (*prefix, "rollout", "status", server, "--timeout=300s")],
            ),
            "shutdown": (
                target["namespace"], targets["server"],
                [(*prefix, "delete", "pod", "__SELECTED_POD__", "--wait=true", "--timeout=300s")],
                [(*prefix, "wait", "pod", "-l", targets["server"], "--for=condition=Ready", "--timeout=300s")],
            ),
            "stale-attestation": (
                *scale_plan(clock, 1, "clock")[:2],
                [*scale_plan(clock, 1, "clock")[2], (*prefix, "exec", "deployment/memories", "-c", "memories", "--", "/bin/sh", "-ec", "sleep 35")],
                scale_plan(clock, 1, "clock")[3],
            ),
            "ttl-failure": environment_plan(lifecycle, "lifecycle", "AccessTelemetryLifecycle__SchemaVersion=2"),
            "transaction-failure": environment_plan(lifecycle, "lifecycle", "AccessTelemetryLifecycle__ConfigurationStoreName=qualification-transaction-failure"),
        }
        if scenario not in plans:
            raise EvidenceValidationError(f"failure {scenario} is not in the closed target registry")
        fault_namespace, selector, actions, restorations = plans[scenario]
        fault_prefix = _kubectl_prefix(target, fault_namespace)
        proof = _C4_MECHANISM_PROOF_TESTS.get(scenario)
        proof_commands = _fixed_test_commands(*proof) if proof is not None else []
        return [
            *_fixed_operation_commands(target, "qualification", "qualification-renew"),
            # Every failure lane starts with a fresh Server process so a prior
            # terminal delivery disposition or queue cannot discharge the next
            # scenario.
            (*prefix, "rollout", "restart", "deployment/memories"),
            (*prefix, "rollout", "status", "deployment/memories", "--timeout=300s"),
            *proof_commands,
            (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
            (*prefix, "exec", "deployment/memories-access-telemetry", "-c", "lifecycle", "--", "/bin/sh", "-ec",
             "wget -qO- --header=\"dapr-api-token: $APP_API_TOKEN\" http://127.0.0.1:8080/v1/access-telemetry/inspect"),
            (*fault_prefix, "get", "pods", "-l", selector, "-o", "json"),
            *[("__FAULT_ACTION__", *action) for action in actions],
            (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
            _business_probe_command(target, command_id),
            (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "memories", "--", "/bin/sh", "-ec",
             _fixed_workload_shell(target, command_id)),
            *[("__FAULT_RESTORE__", *restoration) for restoration in restorations],
            (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
            (*fault_prefix, "get", "pods", "-l", selector, "-o", "json"),
            (*prefix, "exec", "deployment/memories-access-telemetry", "-c", "lifecycle", "--", "/bin/sh", "-ec",
             "wget -qO- --header=\"dapr-api-token: $APP_API_TOKEN\" http://127.0.0.1:8080/v1/access-telemetry/inspect"),
            (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "memories", "--", "/bin/sh", "-ec",
             "wget -qO- http://127.0.0.1:9090/metrics"),
            (*prefix, "logs", "deployment/memories", "-c", "memories", "--tail=100"),
        ]
    if command_id in {"continuity", "observability", "privacy-denial"}:
        lifecycle_metrics_url = (
            "http://prometheus-operated.monitoring.svc.cluster.local:9090/federate?"
            "match%5B%5D=%7B__name__%3D~%22memories_access_telemetry_lifecycle_.%2A%22%7D"
        )
        commands = [
            (*prefix, "get", "deployment", "memories", "-o", "json"),
            (*prefix, "get", "configuration.dapr.io", "memories-config", "-o", "json"),
            (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
        ]
        if command_id in {"continuity", "observability"}:
            commands.extend(
                [
                    (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "memories", "--", "/bin/sh", "-ec",
                     _fixed_workload_shell(target, command_id)),
                    (*prefix, "logs", "deployment/memories", "-c", "memories", "--tail=100"),
                    (*_kubectl_prefix(target, "monitoring"), "logs", "-l",
                     "app.kubernetes.io/name=opentelemetry-collector", "--tail=500"),
                ]
            )
        else:
            commands.extend(
                (
                    _business_probe_command(target, command_id, privacy=True),
                    (*prefix, "logs", "deployment/memories", "-c", "memories", "--tail=100"),
                )
            )
        commands.extend(
            [
                (*prefix, "exec", "pod/__SERVER_POD_0__", "-c", "memories", "--", "/bin/sh", "-ec",
                 f"wget -qO- '{lifecycle_metrics_url}'"),
                (*prefix, "exec", "deployment/memories-access-telemetry", "-c", "lifecycle", "--", "/bin/sh", "-ec",
                 "wget -qO- --header=\"dapr-api-token: $APP_API_TOKEN\" http://127.0.0.1:8080/v1/access-telemetry/inspect"),
            ]
        )
        if command_id == "observability":
            commands.extend(
                _fixed_test_commands(
                    "tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj",
                    "tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Debug/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll",
                    _OBSERVABILITY_PROOF_TESTS,
                )
            )
        if command_id == "privacy-denial":
            commands.extend(
                _fixed_test_commands(
                    "tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj",
                    "tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll",
                    _PRIVACY_PROOF_TESTS,
                )
            )
        return commands
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
        and isinstance(item.get("status"), Mapping)
        and item["status"].get("phase") == "Running"
        and any(
            isinstance(condition, Mapping)
            and condition.get("type") == "Ready"
            and condition.get("status") == "True"
            for condition in (
                item["status"].get("conditions")
                if isinstance(item["status"].get("conditions"), list)
                else []
            )
        )
        and item["metadata"].get("deletionTimestamp") is None
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


def _has_correlated_audit_record(
    payload: bytes,
    workload: Mapping[str, Any],
    command_id: str,
) -> bool:
    """Require event 7506 and the segment marker in one JSON record."""

    run_id = _require_nonempty_string(_camel_value(workload, "run_id"), f"{command_id}.run_id")
    segment_id = _require_nonempty_string(
        _camel_value(workload, "segment_id"), f"{command_id}.segment_id"
    )
    correlation = _sha256(f"{run_id}/{segment_id}")[:32]
    marker = f"qualification-{correlation}"

    def values(value: Any) -> list[Any]:
        if isinstance(value, Mapping):
            return [item for child in value.values() for item in values(child)]
        if isinstance(value, list):
            return [item for child in value for item in values(child)]
        return [value]

    try:
        text_payload = payload.decode("utf-8", errors="strict")
    except UnicodeDecodeError as exc:
        raise EvidenceValidationError(f"command {command_id} audit output is not UTF-8") from exc
    for line in text_payload.splitlines():
        if not line.strip().startswith("{"):
            continue
        try:
            record = _json_without_duplicates(line, f"{command_id} correlated audit record")
        except ValueError as exc:
            raise EvidenceValidationError(f"command {command_id} returned malformed JSON audit output") from exc
        record_values = values(record)
        if 7506 in record_values and any(
            isinstance(value, str) and value.startswith(marker) for value in record_values
        ):
            return True
    return False


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


def _lifecycle_prometheus_observation(payload: bytes, command_id: str) -> dict[str, Any]:
    """Validate canonical lifecycle samples from the fixed Prometheus federation query."""

    try:
        text_payload = payload.decode("utf-8", errors="strict")
    except UnicodeDecodeError as exc:
        raise EvidenceValidationError(f"command {command_id} lifecycle metrics are not UTF-8") from exc
    sample_pattern = re.compile(
        r'\A([a-zA-Z_:][a-zA-Z0-9_:]*)(?:\{([^}]*)\})?\s+([-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?)'
        r'(?:\s+\d+)?\Z'
    )
    label_pattern = re.compile(r'([a-zA-Z_][a-zA-Z0-9_]*)="([^"\\]*(?:\\.[^"\\]*)*)"')
    samples: list[tuple[str, dict[str, str], float]] = []
    for line in text_payload.splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        match = sample_pattern.fullmatch(stripped)
        if match is None:
            raise EvidenceValidationError(f"command {command_id} returned malformed lifecycle metrics")
        raw_labels = match.group(2) or ""
        labels: dict[str, str] = {}
        position = 0
        while position < len(raw_labels):
            label_match = label_pattern.match(raw_labels, position)
            if label_match is None or label_match.group(1) in labels:
                raise EvidenceValidationError(f"command {command_id} returned malformed lifecycle labels")
            labels[label_match.group(1)] = label_match.group(2)
            position = label_match.end()
            if position < len(raw_labels):
                if raw_labels[position] != ",":
                    raise EvidenceValidationError(f"command {command_id} returned malformed lifecycle labels")
                position += 1
        forbidden = {key for key in labels if key.lower() in {"tenant", "tenant_id", "content", "query", "user", "record_id"}}
        if forbidden:
            raise EvidenceValidationError(f"command {command_id} lifecycle metrics contain identity labels")
        samples.append((match.group(1), labels, float(match.group(3))))
    if len(samples) > 10_000:
        raise EvidenceValidationError(f"command {command_id} lifecycle metrics exceeded the sample bound")

    infrastructure_labels = {"instance", "job", "namespace", "pod", "service_name", "service_namespace"}

    def application_labels(labels: Mapping[str, str]) -> dict[str, str]:
        return {key: value for key, value in labels.items() if key not in infrastructure_labels}

    records_name = "memories_access_telemetry_lifecycle_records_total"
    states = sorted(
        {
            application_labels(labels)["state"]
            for name, labels, value in samples
            if name == records_name
            and value > 0
            and set(application_labels(labels)) == {"state", "reason"}
        }
    )
    required_states = sorted(REQUIRED_LIFECYCLE_SIGNALS)
    if states != required_states:
        raise EvidenceValidationError(f"command {command_id} lacks canonical lifecycle state samples")
    health_states = {
        application_labels(labels).get("state")
        for name, labels, value in samples
        if name == "memories_access_telemetry_lifecycle_health"
        and value == 1
        and set(application_labels(labels)) == {"state", "reason"}
    }
    profile_states = {
        application_labels(labels).get("state")
        for name, labels, value in samples
        if name == "memories_access_telemetry_lifecycle_profile"
        and value == 1
        and set(application_labels(labels)) == {"state"}
    }
    evidence_states = {
        application_labels(labels).get("state")
        for name, labels, value in samples
        if name == "memories_access_telemetry_lifecycle_physical_evidence_total"
        and value > 0
        and set(application_labels(labels)) == {"state"}
    }
    timestamp_present = any(
        name == "memories_access_telemetry_lifecycle_physical_evidence_last_timestamp_seconds"
        and not application_labels(labels)
        and value > 0
        for name, labels, value in samples
    )
    observed_label_set = {
        key
        for _, labels, _ in samples
        for key in application_labels(labels)
        if key in {"state", "reason", "outcome"}
    }
    observed_labels = [key for key in ("state", "reason", "outcome") if key in observed_label_set]
    return {
        "signals": states,
        "labels": observed_labels,
        "current_health_present": bool(health_states),
        "profile_matched": "matched" in profile_states,
        "physical_evidence_present": "present" in evidence_states,
        "last_evidence_timestamp_gauge": timestamp_present,
        "sample_count": len(samples),
    }


def _executed_test_inventory(
    payload: bytes,
    expected: Sequence[str],
    command_id: str,
) -> list[str]:
    """Require a fixed reviewed test command's concrete zero-skip pass inventory."""

    try:
        output = payload.decode("utf-8", errors="strict")
    except UnicodeDecodeError as exc:
        raise EvidenceValidationError(f"command {command_id} test output is not UTF-8") from exc
    if "=== TEST EXECUTION SUMMARY ===" not in output:
        raise EvidenceValidationError(f"command {command_id} fixed proof tests returned no MTP summary")
    for field in ("Errors", "Failed", "Skipped", "Not Run"):
        if re.search(rf"\b{field}:\s+0\b", output, re.IGNORECASE) is None:
            raise EvidenceValidationError(f"command {command_id} fixed proof tests did not pass without skips")
    observed = [name for name in expected if re.search(rf"\b{re.escape(name)}\b", output) is not None]
    if observed != list(expected):
        raise EvidenceValidationError(f"command {command_id} fixed proof test inventory is incomplete")
    return observed


class _C3Journal:
    """Exclusive, hash-chained JSONL progress journal for the multi-day C3 run."""

    def __init__(self, path: Path, context: Mapping[str, Any] | None = None) -> None:
        if not path.is_absolute() or path.is_symlink():
            raise EvidenceValidationError("C3 journal must be an absolute non-symlink path")
        path.parent.mkdir(parents=True, exist_ok=True)
        self._context_sha256 = _sha256(_canonical_json(context)) if context is not None else None
        if context is not None:
            _validate_secret_safe(context, "C3 journal context")
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
                frozenset({"sequence", "previous_sha256", "context_sha256", "command_id", "result_sha256", "recorded_utc_ms", "result", "command"}),
                "C3 journal entry",
            )
            if entry.get("sequence") != index or entry.get("previous_sha256") != previous:
                raise EvidenceValidationError("C3 journal sequence/hash chain is not append-only")
            context_sha256 = entry.get("context_sha256")
            if not isinstance(context_sha256, str) or re.fullmatch(r"[0-9a-f]{64}", context_sha256) is None:
                raise EvidenceValidationError("C3 journal context hash is not canonical")
            if self._context_sha256 is None:
                self._context_sha256 = context_sha256
            elif context_sha256 != self._context_sha256:
                raise EvidenceValidationError("C3 journal context differs from this qualification session")
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
        if self._context_sha256 is None:
            self._context_sha256 = "0" * 64
        result_copy = {key: value for key, value in result.items() if key != "_command"}
        command = _require_mapping(result.get("_command"), "C3 journal command")
        entry = {
            "sequence": len(self._entries) + 1,
            "previous_sha256": self._previous,
            "context_sha256": self._context_sha256,
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

    @property
    def authenticated_prefix_sha256(self) -> str:
        """Bind the context and exact completed JSONL prefix for reporter release."""

        return _sha256(
            _canonical_json(
                {
                    "context_sha256": self._context_sha256,
                    "last_entry_sha256": self._previous,
                    "entry_count": len(self._entries),
                }
            )
        )

    def close(self) -> None:
        fcntl.flock(self._stream.fileno(), fcntl.LOCK_UN)
        self._stream.close()

    def __enter__(self) -> _C3Journal:
        return self

    def __exit__(self, *_: object) -> None:
        self.close()


def _host_cadence_sleep(seconds: float) -> None:
    """Wait for the next one-second host cadence, or skip when tests compress time."""

    if seconds <= 0:
        return
    if os.environ.get("HEXALITH_STORY_27_4_HOST_CADENCE_QUANTUM") == "0":
        return
    time.sleep(seconds)


def _writer_renew_interval_seconds() -> float:
    if os.environ.get("HEXALITH_STORY_27_4_HOST_CADENCE_QUANTUM") == "0":
        return _WRITER_RENEW_TEST_INTERVAL_SECONDS
    return _WRITER_RENEW_INTERVAL_SECONDS


def _command_mutates_qualification_lease(command_id: str) -> bool:
    return (
        command_id in {"qualification-enable", "qualification-renew", "qualification-disable"}
        or command_id.startswith("replace-")
        or command_id == "approved-adapter-fault"
        or command_id.startswith("failure-")
    )


def _run_writer_segments(
    target: Mapping[str, str],
    checkpoint: str,
    command_id: str,
    *,
    _segment_count: int = 1_800,
    _monotonic: Callable[[], float] = time.monotonic,
    _sleep: Callable[[float], None] = _host_cadence_sleep,
    _process_runner: Callable[..., tuple[int, bytes, bytes]] = _run_bounded_process,
) -> tuple[Mapping[str, Any], dict[str, Any]]:
    """Dispatch one-second segments on an absolute host cadence with bounded concurrency."""

    arguments = {
        "kube_context": target["kube_context"],
        "namespace": target["namespace"],
        "operation": command_id,
        "c1_platform_operations_reviewer": target["_platform_operations_reviewer"],
    }
    prefix = _kubectl_prefix(target)
    ordinal = int(command_id[-1]) - 1
    run_id = f"run-{hashlib.sha256(target['_lease_holder'].encode('utf-8')).hexdigest()[:24]}"
    started = _utc_now_milliseconds()
    output_hashes: list[str] = []
    error_hashes: list[str] = []
    totals = {
        "attempted": 0,
        "enqueued": 0,
        "acknowledged": 0,
        "persisted": 0,
        "conflicted": 0,
        "transaction_acknowledgements": 0,
        "dropped": 0,
        "rejected": 0,
        "result_count": 0,
    }
    failed_segments = 0
    segment_intervals: list[tuple[int, int]] = []
    segment_ids: list[str] = []
    record_inventory_hashes: list[str] = []
    segment_proofs: list[dict[str, Any]] = []
    environment = {
        **os.environ,
        "HEXALITH_STORY_27_4_COMMAND_ID": command_id,
        "HEXALITH_STORY_27_4_LEASE_HOLDER": target["_lease_holder"],
    }
    emitted_times: dict[int, int] = {}
    dispatched_monotonic: dict[int, float] = {}
    responses: dict[int, tuple[str, int, bytes, bytes]] = {}
    retries: dict[int, int] = {}

    def invoke(index: int) -> tuple[str, int, bytes, bytes]:
        segment_id = f"{command_id}-segment-{index + 1:04d}"
        code, pods_stdout, pods_stderr = _process_runner(
            (*prefix, "get", "pods", "-l", "app.kubernetes.io/name=memories", "-o", "json"),
            cwd=Path.cwd(), timeout_seconds=300, environment=environment,
        )
        if code != 0:
            return "", code, pods_stdout, pods_stderr
        pods = _require_mapping(
            _json_without_duplicates(pods_stdout.decode("utf-8", errors="strict"), command_id),
            "Server pods",
        )
        selected_pod = _select_named_pod(pods, command_id)
        code, stdout, stderr = _process_runner(
            (
                *prefix, "exec", f"pod/{selected_pod}", "-c", "memories", "--",
                "/bin/sh", "-ec",
                f'wget -qO- --header="dapr-api-token: $APP_API_TOKEN" '
                f'--header="X-Hexalith-Qualification-Run: {run_id}" '
                f'--header="X-Hexalith-Qualification-Segment: {segment_id}" '
                f'--header="X-Hexalith-Qualification-Emitted-Utc-Ms: {emitted_times[index]}" '
                f'--post-data="" {_FIXED_WORKLOAD_ROUTE}',
            ),
            cwd=Path.cwd(), timeout_seconds=120, environment=environment,
        )
        return selected_pod, code, stdout, stderr

    in_flight: dict[Future[tuple[str, int, bytes, bytes]], int] = {}
    schedule_origin = _monotonic()
    dispatch_lag_max_ms = 0
    with ThreadPoolExecutor(max_workers=4, thread_name_prefix=f"story-27-4-{command_id}") as pool:
        def collect(completed: set[Future[tuple[str, int, bytes, bytes]]]) -> None:
            nonlocal failed_segments
            for future in completed:
                index = in_flight.pop(future)
                try:
                    selected_pod, code, stdout, stderr = future.result()
                except (EvidenceValidationError, OSError, ValueError) as exc:
                    selected_pod, code, stdout, stderr = "", -1, b"", str(exc).encode("utf-8")
                output_hashes.append(hashlib.sha256(stdout).hexdigest())
                error_hashes.append(hashlib.sha256(stderr).hexdigest())
                if code == 0:
                    responses[index] = (selected_pod, code, stdout, stderr)
                    continue
                failed_segments += 1
                retries[index] = retries.get(index, 0) + 1
                if failed_segments > 300 or retries[index] > 8:
                    raise EvidenceValidationError(
                        f"command {command_id} exceeded its killed-segment retry bound"
                    )
                in_flight[pool.submit(invoke, index)] = index

        for index in range(_segment_count):
            if _TERMINATION_REQUESTED.is_set():
                raise EvidenceValidationError("qualification writer stopped for bounded cleanup")
            due = schedule_origin + index
            remaining = due - _monotonic()
            if remaining > 0:
                _sleep(remaining)
            now = _monotonic()
            dispatch_lag_max_ms = max(dispatch_lag_max_ms, int(max(0.0, now - due) * 1000))
            if dispatch_lag_max_ms > 250:
                raise EvidenceValidationError(f"command {command_id} exceeded its host dispatch cadence")
            completed_now = {future for future in in_flight if future.done()}
            if completed_now:
                collect(completed_now)
            while len(in_flight) >= 4:
                completed, _ = wait(in_flight, return_when=FIRST_COMPLETED)
                collect(completed)
            emitted_times[index] = _utc_now_milliseconds()
            dispatched_monotonic[index] = now
            in_flight[pool.submit(invoke, index)] = index
        while in_flight:
            completed, _ = wait(in_flight, return_when=FIRST_COMPLETED)
            collect(completed)

    for index in range(_segment_count):
        selected_pod, _, stdout, _ = responses[index]
        segment_id = f"{command_id}-segment-{index + 1:04d}"
        payload = _require_mapping(
            _json_without_duplicates(stdout.decode("utf-8", errors="strict"), command_id),
            f"{command_id} segment",
        )
        if _require_nonempty_string(_camel_value(payload, "writer"), f"{command_id}.writer") != selected_pod:
            raise EvidenceValidationError(f"command {command_id} segment is not attributable to its selected pod")
        if _require_nonempty_string(_camel_value(payload, "run_id"), f"{command_id}.run_id") != run_id:
            raise EvidenceValidationError(f"command {command_id} segment is bound to another run")
        if _require_nonempty_string(_camel_value(payload, "segment_id"), f"{command_id}.segment_id") != segment_id:
            raise EvidenceValidationError(f"command {command_id} returned a non-canonical segment identity")
        record_ids = _require_sequence(
            _camel_value(payload, "record_ids"), f"{command_id}.record_ids"
        )
        expected_record_ids = _qualification_record_ids(run_id, segment_id)
        if record_ids != expected_record_ids or len(set(record_ids)) != 125:
            raise EvidenceValidationError(
                f"command {command_id} did not return its exact deterministic record inventory"
            )
        segment_started = _require_integer(
            _camel_value(payload, "started_utc_ms"), f"{command_id}.started_utc_ms", minimum=1
        )
        segment_finished = _require_integer(
            _camel_value(payload, "finished_utc_ms"), f"{command_id}.finished_utc_ms", minimum=1
        )
        if not 950 <= segment_finished - segment_started <= 1_250:
            raise EvidenceValidationError(f"command {command_id} segment did not measure one second")
        attempted = _require_integer(_camel_value(payload, "attempted"), f"{command_id}.attempted")
        enqueued = _require_integer(_camel_value(payload, "enqueued"), f"{command_id}.enqueued")
        acknowledged = _require_integer(_camel_value(payload, "acknowledged"), f"{command_id}.acknowledged")
        persisted = _require_integer(_camel_value(payload, "persisted"), f"{command_id}.persisted")
        conflicted = _require_integer(_camel_value(payload, "conflicted"), f"{command_id}.conflicted")
        transaction_acks = _require_integer(
            _camel_value(payload, "transaction_acknowledgements"),
            f"{command_id}.transaction_acknowledgements",
        )
        dropped = _require_integer(_camel_value(payload, "dropped"), f"{command_id}.dropped")
        rejected = _require_integer(_camel_value(payload, "rejected"), f"{command_id}.rejected")
        if not (
            attempted == enqueued == 125
            and acknowledged + conflicted == 125
            and persisted == acknowledged
            and transaction_acks == acknowledged
            and dropped == rejected == 0
            and (acknowledged == 125 or conflicted == 125)
        ):
            raise EvidenceValidationError(f"command {command_id} returned a non-canonical one-second segment")
        values = {
            "attempted": attempted, "enqueued": enqueued, "acknowledged": acknowledged,
            "persisted": persisted, "conflicted": conflicted,
            "transaction_acknowledgements": transaction_acks,
            "dropped": dropped, "rejected": rejected,
        }
        for field, value in values.items():
            totals[field] += value
        totals["result_count"] += _require_nonzero_integer(
            _camel_value(payload, "result_count"), f"{command_id}.result_count"
        )
        segment_intervals.append((segment_started, segment_finished))
        segment_ids.append(segment_id)
        record_inventory_hashes.append(_sha256(_canonical_json(record_ids)))
        segment_proofs.append(
            {
                "segment_id": segment_id,
                "writer": command_id,
                "writer_pod": selected_pod,
                "started_utc_ms": segment_started,
                "finished_utc_ms": segment_finished,
                "record_inventory_sha256": record_inventory_hashes[-1],
                "durable_count": acknowledged + conflicted,
                "replayed": conflicted == 125,
            }
        )
    finished = _utc_now_milliseconds()
    result = {
        "writer": f"server-writer-{command_id[-1]}",
        "started_utc_ms": segment_intervals[0][0],
        "finished_utc_ms": segment_intervals[-1][1],
        "segment_count": len(segment_ids),
        "replayed_segment_count": sum(1 for proof in segment_proofs if proof["replayed"]),
        "dispatch_lag_max_milliseconds": dispatch_lag_max_ms,
        "segment_inventory_sha256": _sha256(_canonical_json(segment_ids)),
        "record_inventory_sha256": _sha256(_canonical_json(record_inventory_hashes)),
        **totals,
        "_segment_proofs": segment_proofs,
    }
    observation = {
        "command_id": command_id,
        "arguments": arguments,
        "arguments_sha256": _sha256(_canonical_json(arguments)),
        "started_utc_ms": started,
        "finished_utc_ms": finished,
        "exit_code": 0,
        "stdout_sha256": _sha256(_canonical_json(output_hashes)),
        "stderr_sha256": _sha256(_canonical_json(error_hashes)),
        "result_count": totals["result_count"],
    }
    return result, observation


def _run_operation(
    target: Mapping[str, str],
    checkpoint: str,
    command_id: str,
) -> tuple[Mapping[str, Any], dict[str, Any]]:
    if command_id in {"writer-1", "writer-2"}:
        return _run_writer_segments(target, checkpoint, command_id)
    if _command_mutates_qualification_lease(command_id):
        with _LEASE_MUTATION_LOCK:
            return _run_locked_operation(target, checkpoint, command_id)
    return _run_locked_operation(target, checkpoint, command_id)


def _run_locked_operation(
    target: Mapping[str, str],
    checkpoint: str,
    command_id: str,
) -> tuple[Mapping[str, Any], dict[str, Any]]:
    if command_id == "qualification-disable" and isinstance(target, dict):
        target.pop("_disable_lease_authorized", None)
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
    physical_evidence: Mapping[str, Any] | None = None
    reclamation_observation: Mapping[str, Any] | None = (
        target.get("_c3_reclamation_observation")
        if command_id == "cohort-168h-report"
        and isinstance(target.get("_c3_reclamation_observation"), Mapping)
        else None
    )
    lease_observation: Mapping[str, Any] | None = None
    environment = {
        **os.environ,
        "HEXALITH_STORY_27_4_COMMAND_ID": command_id,
        "HEXALITH_STORY_27_4_LEASE_HOLDER": target["_lease_holder"],
    }
    operation_commands = _fixed_operation_commands(target, checkpoint, command_id)
    pending_restorations = [command[1:] for command in operation_commands if command and command[0] == "__FAULT_RESTORE__"]
    fault_active = False

    def restore_fault() -> None:
        nonlocal fault_active
        if not fault_active:
            return
        failures: list[str] = []
        for restoration in pending_restorations:
            try:
                return_code, _, stderr = _run_bounded_process(
                    restoration,
                    cwd=Path.cwd(),
                    timeout_seconds=300,
                    environment=environment,
                )
                if return_code != 0:
                    failures.append(
                        f"exit={return_code},stderr_sha256={hashlib.sha256(stderr).hexdigest()}"
                    )
            except (EvidenceValidationError, OSError, ValueError) as exc:
                failures.append(type(exc).__name__)
        fault_active = False
        if failures:
            raise EvidenceValidationError(
                "fault restoration failed after all fixed attempts: " + ",".join(failures)
            )

    for step, command in enumerate(operation_commands):
        if (
            command_id == "qualification-disable"
            and step > 0
            and target.get("_disable_lease_authorized") is not True
        ):
            raise EvidenceValidationError("qualification cleanup lost Lease authority before mutation")
        marker = command[0] if command else ""
        stdin_bytes: bytes | None = None
        if marker == "__FAULT_ACTION__":
            fault_active = True
            if selected_pod is None and isinstance(last_payload, Mapping):
                try:
                    selected_pod = _select_named_pod(last_payload, command_id)
                except EvidenceValidationError:
                    restore_fault()
                    raise
            command = command[1:]
        elif marker == "__FAULT_RESTORE__":
            command = command[1:]
        elif marker == "__BUSINESS_BEARER_STDIN__":
            bearer = target.get("_business_bearer")
            if not isinstance(bearer, bytes):
                raise EvidenceValidationError("qualification bearer was not authenticated before target access")
            stdin_bytes = bearer
            command = command[1:]
        if "__LEASE_RENEW_PATCH__" in command or "__GATE_RENEW_PATCH__" in command:
            metadata = lease_observation.get("metadata") if isinstance(lease_observation, Mapping) else None
            lease_spec = lease_observation.get("spec") if isinstance(lease_observation, Mapping) else None
            resource_version = metadata.get("resourceVersion") if isinstance(metadata, Mapping) else None
            holder = lease_spec.get("holderIdentity") if isinstance(lease_spec, Mapping) else None
            if not isinstance(resource_version, str) or holder != target["_lease_holder"]:
                raise EvidenceValidationError("qualification Lease renewal lost atomic ownership")
            renew_utc = datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z")
            renew_patch = _canonical_json(
                [
                    {"op": "test", "path": "/metadata/resourceVersion", "value": resource_version},
                    {"op": "test", "path": "/spec/holderIdentity", "value": holder},
                    {"op": "replace", "path": "/spec/leaseDurationSeconds", "value": _QUALIFICATION_SESSION_SECONDS},
                    {"op": "add", "path": "/spec/renewTime", "value": renew_utc},
                ]
            )
            gate_patch = _canonical_json(
                {"data": {"gate.json": _canonical_json({
                    "schemaVersion": 1,
                    "state": "enabled",
                    "profileSha256": STORY_27_4_PROFILE_SHA256,
                    "expiresUtcMs": _utc_now_milliseconds() + _QUALIFICATION_SESSION_SECONDS * 1000,
                })}}
            )
            command = tuple(
                renew_patch if value == "__LEASE_RENEW_PATCH__" else
                gate_patch if value == "__GATE_RENEW_PATCH__" else value
                for value in command
            )
        if "__LEASE_RELEASE_PATCH__" in command:
            metadata = lease_observation.get("metadata") if isinstance(lease_observation, Mapping) else None
            lease_spec = lease_observation.get("spec") if isinstance(lease_observation, Mapping) else None
            resource_version = metadata.get("resourceVersion") if isinstance(metadata, Mapping) else None
            holder = lease_spec.get("holderIdentity") if isinstance(lease_spec, Mapping) else None
            expected_holder = target["_lease_holder"]
            holder_is_expired_story_run = False
            if isinstance(holder, str) and holder.startswith("story-27-4/") and holder != expected_holder:
                lease_time = lease_spec.get("renewTime") or lease_spec.get("acquireTime")
                duration = lease_spec.get("leaseDurationSeconds")
                try:
                    lease_time_ms = int(
                        datetime.fromisoformat(str(lease_time).replace("Z", "+00:00")).timestamp() * 1000
                    )
                except (TypeError, ValueError):
                    lease_time_ms = 0
                holder_is_expired_story_run = (
                    type(duration) is int
                    and duration > 0
                    and lease_time_ms > 0
                    and lease_time_ms + duration * 1000 < _utc_now_milliseconds()
                )
            if (
                not isinstance(resource_version, str)
                or not isinstance(holder, str)
                or (holder not in {"", expected_holder} and not holder_is_expired_story_run)
            ):
                raise EvidenceValidationError("qualification Lease release refused another runner's ownership")
            release_patch = _canonical_json(
                [
                    {"op": "test", "path": "/metadata/resourceVersion", "value": resource_version},
                    {"op": "test", "path": "/spec/holderIdentity", "value": holder},
                    {"op": "replace", "path": "/spec/holderIdentity", "value": ""},
                    {"op": "replace", "path": "/spec/leaseDurationSeconds", "value": 0},
                ]
            )
            command = tuple(release_patch if value == "__LEASE_RELEASE_PATCH__" else value for value in command)
        if "__PHYSICAL_EVIDENCE_PATCH__" in command:
            if (
                reclamation_observation is None
                or type(reclamation_observation.get("allocator_free_bytes")) is not int
            ):
                raise EvidenceValidationError("physical evidence requires the measured post-VACUUM aggregate")
            artifact_sha256 = _require_nonempty_string(
                target.get("_c3_reporter_artifact_sha256"),
                "authenticated C3 reporter prefix",
                maximum=64,
            )
            if re.fullmatch(r"[0-9a-f]{64}", artifact_sha256) is None:
                raise EvidenceValidationError("authenticated C3 reporter prefix is not canonical")
            evidence = {
                "evidenceId": "story-27-4-c3",
                "componentProfileHash": STORY_27_4_PROFILE_SHA256,
                "artifactSha256": artifact_sha256,
                "reporterImageDigest": _require_nonempty_string(
                    target.get("_reporter_image"), "C1-reviewed reporter image"
                ).rsplit("@sha256:", 1)[-1],
                "observedAtUnixMilliseconds": _require_integer(
                    reclamation_observation.get("reclaimed_utc_ms"),
                    "physical evidence reclaimed_utc_ms",
                    minimum=1,
                ),
            }
            reporter_job = next(
                (
                    payload
                    for payload in parsed_payloads
                    if isinstance(payload, Mapping) and payload.get("kind") == "Job"
                ),
                None,
            )
            reporter_status = reporter_job.get("status") if isinstance(reporter_job, Mapping) else None
            completed = (
                isinstance(reporter_status, Mapping)
                and reporter_status.get("succeeded") == 1
                and reporter_status.get("failed", 0) == 0
                and reporter_status.get("active", 0) == 0
                and isinstance(reporter_status.get("completionTime"), str)
            )
            fresh = (
                isinstance(reporter_job, Mapping)
                and isinstance(reporter_job.get("spec"), Mapping)
                and reporter_job["spec"].get("suspend") is True
                and isinstance(reporter_status, Mapping)
                and reporter_status.get("succeeded", 0) == 0
                and reporter_status.get("failed", 0) == 0
                and reporter_status.get("active", 0) == 0
                and reporter_status.get("completionTime") is None
            )
            if not (fresh or completed):
                raise EvidenceValidationError(
                    "physical evidence reporter is neither fresh nor an exact completed resume"
                )
            if completed:
                report_config = next(
                    (
                        payload
                        for payload in reversed(parsed_payloads)
                        if isinstance(payload, Mapping)
                        and isinstance(payload.get("data"), Mapping)
                        and "evidence.json" in payload["data"]
                    ),
                    None,
                )
                try:
                    existing = _require_mapping(
                        _json_without_duplicates(
                            report_config["data"]["evidence.json"],
                            "completed physical evidence report",
                        ),
                        "completed physical evidence report",
                    )
                except (KeyError, TypeError, ValueError) as exc:
                    raise EvidenceValidationError(
                        "completed physical evidence reporter has no authenticated input"
                    ) from exc
                _require_exact_fields(existing, frozenset(evidence), "completed physical evidence report")
                if (
                    existing.get("evidenceId") != evidence["evidenceId"]
                    or existing.get("componentProfileHash") != evidence["componentProfileHash"]
                    or existing.get("artifactSha256") != evidence["artifactSha256"]
                    or existing.get("reporterImageDigest") != evidence["reporterImageDigest"]
                    or type(existing.get("observedAtUnixMilliseconds")) is not int
                    or existing["observedAtUnixMilliseconds"] <= 0
                ):
                    raise EvidenceValidationError(
                        "completed physical evidence reporter is bound to another C3 prefix"
                    )
                physical_evidence = existing
                if isinstance(target, dict):
                    target["_reuse_completed_reporter"] = True
                environment["HEXALITH_STORY_27_4_PHYSICAL_ARTIFACT_SHA256"] = artifact_sha256
                continue
            physical_evidence = evidence
            environment["HEXALITH_STORY_27_4_PHYSICAL_ARTIFACT_SHA256"] = evidence["artifactSha256"]
            physical_patch = _canonical_json(
                {"data": {"evidence.json": _canonical_json(evidence)}}
            )
            command = tuple(
                physical_patch if value == "__PHYSICAL_EVIDENCE_PATCH__" else value
                for value in command
            )
        if target.get("_reuse_completed_reporter") is True and (
            ("patch" in command and "job" in command)
            or ("wait" in command and any("job/access-telemetry-physical-evidence-reporter" == value for value in command))
        ):
            continue
        if any("__SERVER_POD_" in value for value in command):
            if server_pods is None:
                restore_fault()
                raise EvidenceValidationError(f"command {command_id} has no Server pod observation")
            server_ordinal = next(int(value.split("__SERVER_POD_", 1)[1].split("__", 1)[0]) for value in command if "__SERVER_POD_" in value)
            try:
                server_name = _select_named_pod(server_pods, f"writer-{server_ordinal + 1}")
            except EvidenceValidationError:
                restore_fault()
                raise
            if command_id.startswith("writer-"):
                selected_pod = server_name
            command = tuple(
                f"pod/{server_name}" if value.startswith("pod/__SERVER_POD_") else value
                for value in command
            )
        if any(value == "__SELECTED_POD__" or value == "pod/__SELECTED_POD__" for value in command):
            if selected_pod is None:
                if last_payload is None:
                    restore_fault()
                    raise EvidenceValidationError(f"command {command_id} has no pod-selection observation")
                try:
                    selected_pod = _select_named_pod(last_payload, command_id)
                except EvidenceValidationError:
                    restore_fault()
                    raise
            command = tuple(
                f"pod/{selected_pod}" if value == "pod/__SELECTED_POD__" else
                selected_pod if value == "__SELECTED_POD__" else value
                for value in command
            )
        timeout_seconds = 300
        if any(_FIXED_WORKLOAD_ROUTE in value for value in command):
            timeout_seconds = 2_400
        if command_id.startswith("cohort-") and command_id.endswith("-wait"):
            retention_hours = int(command_id.split("-", 2)[1][:-1])
            timeout_seconds = retention_hours * 3_600 + 1_200
        try:
            process_arguments = {
                "cwd": Path.cwd(),
                "timeout_seconds": timeout_seconds,
                "environment": {**environment, "HEXALITH_STORY_27_4_STEP": str(step)},
                "stdin_bytes": stdin_bytes,
            }
            return_code, stdout, stderr = _run_bounded_process(command, **process_arguments)
        except (EvidenceValidationError, OSError, ValueError):
            restore_fault()
            raise
        stdout_parts.append(stdout)
        stderr_parts.append(stderr)
        if sum(map(len, stdout_parts)) > _MAX_TRANSCRIPT_BYTES or sum(map(len, stderr_parts)) > 65_536:
            restore_fault()
            raise EvidenceValidationError(f"command {command_id} exceeded its aggregate transcript bound")
        if return_code != 0:
            restore_fault()
            raise EvidenceValidationError(
                f"command {command_id} exited {return_code}; stderr_sha256={hashlib.sha256(stderr).hexdigest()}"
            )
        if (
            command_id == "qualification-enable"
            and "patch" in command
            and "lease" in command
            and _LEASE_NAME in command
            and isinstance(target, dict)
        ):
            # Ownership begins at the successful atomic Lease patch, not after
            # later gate/scaling output happens to parse. Cleanup must run if
            # any subsequent enable step fails.
            target["_lease_acquired"] = True
        try:
            decoded = stdout.decode("utf-8", errors="strict")
            parsed = _json_without_duplicates(decoded, command_id)
            last_payload = parsed if isinstance(parsed, Mapping) else None
        except (UnicodeDecodeError, ValueError) as exc:
            if stdout.lstrip().startswith((b"{", b"[")):
                restore_fault()
                raise EvidenceValidationError(f"command {command_id} returned malformed JSON") from exc
            last_payload = None
        parsed_payloads.append(last_payload)
        if last_payload is not None and last_payload.get("stage") == "reclamation":
            reclamation_observation = last_payload
        if (
            last_payload is not None
            and "get" in command
            and "lease" in command
            and _LEASE_NAME in command
        ):
            lease_observation = last_payload
            if command_id == "qualification-disable" and isinstance(target, dict):
                lease_spec = last_payload.get("spec")
                holder = lease_spec.get("holderIdentity") if isinstance(lease_spec, Mapping) else None
                duration = lease_spec.get("leaseDurationSeconds") if isinstance(lease_spec, Mapping) else None
                lease_time = (
                    lease_spec.get("renewTime") or lease_spec.get("acquireTime")
                    if isinstance(lease_spec, Mapping)
                    else None
                )
                try:
                    lease_time_ms = int(
                        datetime.fromisoformat(str(lease_time).replace("Z", "+00:00")).timestamp() * 1000
                    )
                except (TypeError, ValueError):
                    lease_time_ms = 0
                expired_story_holder = (
                    isinstance(holder, str)
                    and holder.startswith("story-27-4/")
                    and type(duration) is int
                    and duration > 0
                    and lease_time_ms > 0
                    and lease_time_ms + duration * 1000 < _utc_now_milliseconds()
                )
                target["_disable_lease_authorized"] = (
                    holder in {"", target["_lease_holder"]} or expired_story_holder
                )
        if (
            last_payload is not None
            and "get" in command
            and "pods" in command
            and "app.kubernetes.io/name=memories" in command
        ):
            server_pods = last_payload
        if marker == "__FAULT_RESTORE__" and pending_restorations:
            pending_restorations.pop(0)
            if not pending_restorations:
                fault_active = False
    finished = _utc_now_milliseconds()
    permission_count = sum(
        1 for fixed in operation_commands if "auth" in fixed and "can-i" in fixed
    )
    if command_id == "qualification-enable" and any(
        value.decode("utf-8", errors="strict").strip().lower() != "yes"
        for value in stdout_parts[:permission_count]
    ):
        raise EvidenceValidationError("qualification operator lacks a required RBAC permission")
    if command_id == "qualification-target-identity":
        namespace_payload, gate_payload, lease_payload, lifecycle_payload, clock_payload = parsed_payloads[:5]
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
        holder = lease_spec.get("holderIdentity") if isinstance(lease_spec, Mapping) else None
        lease_time = (
            lease_spec.get("renewTime") or lease_spec.get("acquireTime")
            if isinstance(lease_spec, Mapping)
            else None
        )
        duration = lease_spec.get("leaseDurationSeconds") if isinstance(lease_spec, Mapping) else None
        try:
            lease_time_ms = int(
                datetime.fromisoformat(str(lease_time).replace("Z", "+00:00")).timestamp() * 1000
            )
        except (TypeError, ValueError):
            lease_time_ms = 0
        expired_story_lease = (
            isinstance(holder, str)
            and holder.startswith("story-27-4/")
            and type(duration) is int
            and duration > 0
            and lease_time_ms > 0
            and lease_time_ms + duration * 1000 < _utc_now_milliseconds()
        )
        if expired_story_lease and isinstance(target, dict):
            target["_cleanup_expired_lease"] = True
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
        runtime_inventory: list[dict[str, Any]] = []
        for kind, payload in (
            ("Deployment", parsed_payloads[5]),
            ("Component", parsed_payloads[6]),
            ("Configuration", parsed_payloads[7]),
            ("StatefulSet", parsed_payloads[8]),
        ):
            items = payload.get("items") if isinstance(payload, Mapping) else None
            if not isinstance(items, list):
                raise EvidenceValidationError("qualification runtime identity returned no closed item inventory")
            for item in items:
                metadata = item.get("metadata") if isinstance(item, Mapping) else None
                name = metadata.get("name") if isinstance(metadata, Mapping) else None
                if not isinstance(name, str) or not name:
                    raise EvidenceValidationError("qualification runtime identity omitted an exact resource name")
                identity_item: dict[str, Any] = {"kind": kind, "name": name}
                if kind in {"Deployment", "StatefulSet"}:
                    spec = item.get("spec") if isinstance(item, Mapping) else None
                    template = spec.get("template") if isinstance(spec, Mapping) else None
                    pod_spec = template.get("spec") if isinstance(template, Mapping) else None
                    template_metadata = template.get("metadata") if isinstance(template, Mapping) else None
                    annotations = (
                        template_metadata.get("annotations")
                        if isinstance(template_metadata, Mapping)
                        else None
                    )
                    containers = pod_spec.get("containers") if isinstance(pod_spec, Mapping) else None
                    images = [
                        container.get("image")
                        for container in containers or []
                        if isinstance(container, Mapping)
                    ]
                    if not images or any(
                        not isinstance(image, str)
                        or re.fullmatch(r".+@sha256:[0-9a-f]{64}", image) is None
                        for image in images
                    ):
                        raise EvidenceValidationError(
                            f"qualification {kind}/{name} is not bound to digest-pinned container images"
                        )
                    identity_item["images"] = images
                    service_account = (
                        pod_spec.get("serviceAccountName") if isinstance(pod_spec, Mapping) else None
                    )
                    if not isinstance(service_account, str) or not service_account:
                        raise EvidenceValidationError(
                            f"qualification {kind}/{name} has no explicit service-account identity"
                        )
                    identity_item["service_account"] = service_account
                    if kind == "Deployment" and name in {
                        "memories",
                        "memories-access-telemetry",
                        "memories-access-telemetry-clock",
                    }:
                        expected_app_id = name
                        if not isinstance(annotations, Mapping) or annotations.get("dapr.io/app-id") != expected_app_id:
                            raise EvidenceValidationError(
                                f"qualification Deployment/{name} has the wrong Dapr workload identity"
                            )
                        identity_item["dapr_app_id"] = expected_app_id
                else:
                    spec = item.get("spec") if isinstance(item, Mapping) else None
                    identity_item["type"] = spec.get("type") if isinstance(spec, Mapping) else None
                    identity_item["version"] = spec.get("version") if isinstance(spec, Mapping) else None
                runtime_inventory.append(identity_item)
        required_runtime_objects = {
            ("Deployment", "memories"),
            ("Deployment", "memories-access-telemetry"),
            ("Deployment", "memories-access-telemetry-clock"),
            ("Component", "access-telemetry-store"),
            ("Configuration", "memories-access-telemetry-config"),
            ("StatefulSet", "access-telemetry-postgresql"),
        }
        observed_runtime_objects = {
            (str(item["kind"]), str(item["name"])) for item in runtime_inventory
        }
        if not required_runtime_objects.issubset(observed_runtime_objects):
            raise EvidenceValidationError(
                "qualification runtime identity omitted a required exact kind/name pair"
            )
        reporter_job = parsed_payloads[9]
        reporter_spec = reporter_job.get("spec") if isinstance(reporter_job, Mapping) else None
        reporter_template = reporter_spec.get("template") if isinstance(reporter_spec, Mapping) else None
        reporter_pod_spec = reporter_template.get("spec") if isinstance(reporter_template, Mapping) else None
        reporter_containers = (
            reporter_pod_spec.get("containers") if isinstance(reporter_pod_spec, Mapping) else None
        )
        reporter_container = (
            reporter_containers[0]
            if isinstance(reporter_containers, list)
            and len(reporter_containers) == 1
            and isinstance(reporter_containers[0], Mapping)
            else None
        )
        reporter_image = reporter_container.get("image") if isinstance(reporter_container, Mapping) else None
        if (
            not isinstance(reporter_image, str)
            or re.fullmatch(r".+@sha256:[0-9a-f]{64}", reporter_image) is None
            or reporter_pod_spec.get("automountServiceAccountToken") is not False
            or reporter_pod_spec.get("serviceAccountName") != "access-telemetry-adapter"
            or reporter_container.get("command") != _REPORTER_COMMAND
            or reporter_container.get("args") != _REPORTER_ARGUMENTS
            or reporter_container.get("env") != _REPORTER_ENV
            or reporter_container.get("volumeMounts") != _REPORTER_VOLUME_MOUNTS
            or reporter_pod_spec.get("volumes") != _REPORTER_VOLUMES
        ):
            raise EvidenceValidationError("qualification reporter runtime identity is not exact")
        reporter_digest = reporter_image.rsplit("@sha256:", 1)[1]
        lifecycle_deployments = parsed_payloads[5].get("items")
        lifecycle_deployment = next(
            (
                item
                for item in lifecycle_deployments
                if isinstance(item, Mapping)
                and isinstance(item.get("metadata"), Mapping)
                and item["metadata"].get("name") == "memories-access-telemetry"
            ),
            None,
        )
        lifecycle_containers = (
            lifecycle_deployment.get("spec", {}).get("template", {}).get("spec", {}).get("containers")
            if isinstance(lifecycle_deployment, Mapping)
            else None
        )
        lifecycle_container = next(
            (
                container
                for container in lifecycle_containers or []
                if isinstance(container, Mapping) and container.get("name") == "lifecycle"
            ),
            None,
        )
        reporter_options = [
            item.get("value")
            for item in (
                lifecycle_container.get("env", [])
                if isinstance(lifecycle_container, Mapping)
                else []
            )
            if isinstance(item, Mapping)
            and item.get("name")
            == "AccessTelemetryLifecycle__PhysicalReclamationReporterImageDigest"
        ]
        if reporter_options != [reporter_digest]:
            raise EvidenceValidationError(
                "qualification lifecycle reporter digest differs from the reviewed Job image"
            )
        runtime_inventory.append({
            "kind": "Job",
            "name": "access-telemetry-physical-evidence-reporter",
            "image": reporter_image,
            "command": _REPORTER_COMMAND,
            "args": _REPORTER_ARGUMENTS,
            "env": _REPORTER_ENV,
            "volume_mounts": _REPORTER_VOLUME_MOUNTS,
            "volumes": _REPORTER_VOLUMES,
            "service_account": "access-telemetry-adapter",
            "reporter_digest_option": reporter_digest,
        })
        if isinstance(target, dict):
            target["_reporter_image"] = reporter_image
        qualification_service_accounts = parsed_payloads[11]
        dapr_deployments = parsed_payloads[12]
        dapr_statefulsets = parsed_payloads[13]
        dapr_pods = parsed_payloads[14]
        dapr_service_accounts = parsed_payloads[15]
        for payload, required in (
            (qualification_service_accounts, {
                "memories",
                "memories-access-telemetry",
                "memories-access-telemetry-clock",
                "access-telemetry-postgresql",
                "access-telemetry-adapter",
            }),
            (dapr_service_accounts, {
                "dapr-operator",
                "dapr-placement",
                "dapr-scheduler",
                "dapr-sentry",
                "dapr-injector",
            }),
        ):
            items = payload.get("items") if isinstance(payload, Mapping) else None
            names = {
                item.get("metadata", {}).get("name")
                for item in items or []
                if isinstance(item, Mapping) and isinstance(item.get("metadata"), Mapping)
            }
            if not required.issubset(names):
                raise EvidenceValidationError("qualification runtime identity omitted a service account")
        for kind, payload in (("Deployment", dapr_deployments), ("StatefulSet", dapr_statefulsets)):
            items = payload.get("items") if isinstance(payload, Mapping) else None
            if not isinstance(items, list):
                raise EvidenceValidationError("Dapr control-plane identity returned no workload inventory")
            for item in items:
                metadata = item.get("metadata") if isinstance(item, Mapping) else None
                spec = item.get("spec") if isinstance(item, Mapping) else None
                template = spec.get("template") if isinstance(spec, Mapping) else None
                pod_spec = template.get("spec") if isinstance(template, Mapping) else None
                name = metadata.get("name") if isinstance(metadata, Mapping) else None
                containers = pod_spec.get("containers") if isinstance(pod_spec, Mapping) else None
                images = [container.get("image") for container in containers or [] if isinstance(container, Mapping)]
                service_account = pod_spec.get("serviceAccountName") if isinstance(pod_spec, Mapping) else None
                if (
                    not isinstance(name, str)
                    or not name
                    or not isinstance(service_account, str)
                    or not service_account
                    or not images
                    or any(not isinstance(image, str) or re.fullmatch(r".+@sha256:[0-9a-f]{64}", image) is None for image in images)
                ):
                    raise EvidenceValidationError("Dapr control-plane workload identity is incomplete")
                runtime_inventory.append({
                    "kind": f"dapr-system/{kind}",
                    "name": name,
                    "images": images,
                    "service_account": service_account,
                })
        required_dapr_objects = {
            ("dapr-system/Deployment", "dapr-operator"),
            ("dapr-system/Deployment", "dapr-sentry"),
            ("dapr-system/Deployment", "dapr-sidecar-injector"),
            ("dapr-system/StatefulSet", "dapr-placement-server"),
            ("dapr-system/StatefulSet", "dapr-scheduler-server"),
        }
        if not required_dapr_objects.issubset({(str(item["kind"]), str(item["name"])) for item in runtime_inventory}):
            raise EvidenceValidationError("qualification runtime identity omitted a Dapr control-plane workload")
        for namespace, pod_payload in ((target["namespace"], parsed_payloads[10]), ("dapr-system", dapr_pods)):
            pod_items = pod_payload.get("items") if isinstance(pod_payload, Mapping) else None
            if not isinstance(pod_items, list):
                raise EvidenceValidationError("qualification runtime identity returned no pod inventory")
            for item in pod_items:
                metadata = item.get("metadata") if isinstance(item, Mapping) else None
                spec = item.get("spec") if isinstance(item, Mapping) else None
                status = item.get("status") if isinstance(item, Mapping) else None
                labels = metadata.get("labels") if isinstance(metadata, Mapping) else None
                annotations = metadata.get("annotations") if isinstance(metadata, Mapping) else None
                workload = (
                    labels.get("app.kubernetes.io/name") or labels.get("app")
                    if isinstance(labels, Mapping)
                    else None
                )
                required_workloads = (
                    {"memories", "access-telemetry-postgresql"}
                    if namespace == target["namespace"]
                    else {
                        "dapr-operator",
                        "dapr-placement-server",
                        "dapr-scheduler-server",
                        "dapr-sentry",
                        "dapr-sidecar-injector",
                    }
                )
                if workload not in required_workloads:
                    continue
                container_statuses = status.get("containerStatuses") if isinstance(status, Mapping) else None
                images = sorted(
                    container.get("imageID")
                    for container in container_statuses or []
                    if isinstance(container, Mapping) and isinstance(container.get("imageID"), str)
                )
                service_account = spec.get("serviceAccountName") if isinstance(spec, Mapping) else None
                if (
                    not isinstance(workload, str)
                    or not workload
                    or not isinstance(service_account, str)
                    or not service_account
                    or not images
                    or any(re.search(r"sha256:[0-9a-f]{64}\Z", image) is None for image in images)
                ):
                    raise EvidenceValidationError("qualification running pod identity is incomplete")
                runtime_inventory.append({
                    "kind": "PodRuntime",
                    "namespace": namespace,
                    "workload": workload,
                    "service_account": service_account,
                    "dapr_app_id": annotations.get("dapr.io/app-id") if isinstance(annotations, Mapping) else None,
                    "image_ids": images,
                })
        observed_running_workloads = {
            (str(item.get("namespace")), str(item.get("workload")))
            for item in runtime_inventory
            if item.get("kind") == "PodRuntime"
        }
        required_running_workloads = {
            (target["namespace"], "memories"),
            (target["namespace"], "access-telemetry-postgresql"),
            ("dapr-system", "dapr-operator"),
            ("dapr-system", "dapr-placement-server"),
            ("dapr-system", "dapr-scheduler-server"),
            ("dapr-system", "dapr-sentry"),
            ("dapr-system", "dapr-sidecar-injector"),
        }
        if not required_running_workloads.issubset(observed_running_workloads):
            raise EvidenceValidationError("qualification runtime identity omitted a running image identity")
        runtime_inventory.sort(
            key=lambda item: (
                str(item["kind"]),
                str(item.get("namespace", "")),
                str(item.get("name", item.get("workload", ""))),
                _canonical_json(item.get("image_ids", [])),
            )
        )
        runtime_identity_sha256 = _sha256(_canonical_json(runtime_inventory))
        if isinstance(target, dict):
            target["_runtime_identity_sha256"] = runtime_identity_sha256
        result = {
            "kind": "non-production-qualification",
            "namespace": target["namespace"],
            "profile_sha256": STORY_27_4_PROFILE_SHA256,
            "writes_state": "disabled",
            "runtime_identity_sha256": runtime_identity_sha256,
            "result_count": len(stdout_parts),
        }
    elif command_id == "qualification-final-state":
        gate_payload, lease_payload, lifecycle_payload, clock_payload = parsed_payloads[:4]
        projected_gates = parsed_payloads[-2:]
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
            or len(projected_gates) != 2
            or any(projected != gate for projected in projected_gates)
        ):
            raise EvidenceValidationError("qualification final state is not disabled and scaled to zero")
        result = {"state": "disabled", "result_count": len(stdout_parts)}
    elif command_id in {"qualification-enable", "qualification-renew"}:
        lease_payload, gate_payload, lifecycle_payload, clock_payload = parsed_payloads[-4:]
        if command_id == "qualification-renew":
            lease_payload, gate_payload = parsed_payloads[-2:]
            lifecycle_payload = {"spec": {"replicas": 2}}
            clock_payload = {"spec": {"replicas": 1}}
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
            or lease_spec.get("holderIdentity") != target["_lease_holder"]
            or lease_spec.get("leaseDurationSeconds") != _QUALIFICATION_SESSION_SECONDS
            or gate.get("state") != "enabled"
            or gate.get("profileSha256") != STORY_27_4_PROFILE_SHA256
            or type(expires_utc_ms) is not int
            or expires_utc_ms <= _utc_now_milliseconds()
            or expires_utc_ms > _utc_now_milliseconds() + (_QUALIFICATION_SESSION_SECONDS * 1000) + 5_000
            or lifecycle_replicas != 2
            or clock_replicas != 1
        ):
            raise EvidenceValidationError("qualification target did not enter the exact leased enabled state")
        lease_metadata = lease_payload.get("metadata") if isinstance(lease_payload, Mapping) else None
        if not isinstance(lease_metadata, Mapping) or not isinstance(lease_metadata.get("resourceVersion"), str):
            raise EvidenceValidationError("qualification enabled Lease has no resource version")
        if isinstance(target, dict):
            target["_lease_resource_version"] = lease_metadata["resourceVersion"]
        result = {"state": "enabled", "result_count": len(stdout_parts)}
    elif command_id == "qualification-disable":
        result = {
            "state": "disabled",
            "result_count": len(stdout_parts),
        }
    elif command_id == "component-throughput":
        prometheus = parsed_payloads[-1] if parsed_payloads else None
        data = prometheus.get("data") if isinstance(prometheus, Mapping) else None
        series = data.get("result") if isinstance(data, Mapping) else None
        value_pair = (
            series[0].get("value")
            if isinstance(series, list) and len(series) == 1 and isinstance(series[0], Mapping)
            else None
        )
        if (
            not isinstance(prometheus, Mapping)
            or prometheus.get("status") != "success"
            or not isinstance(value_pair, list)
            or len(value_pair) != 2
            or not isinstance(value_pair[1], str)
            or re.fullmatch(r"[0-9]+(?:\.0+)?", value_pair[1]) is None
        ):
            raise EvidenceValidationError("C2 target state-operation counter query is not exact")
        result = {
            "counter_name": "memories_access_telemetry_lifecycle_state_operations_total",
            "window_milliseconds": 1_800_000,
            "operation_delta": int(float(value_pair[1])),
            "result_count": len(stdout_parts),
        }
    elif command_id == "idempotence-conflict-proof":
        executed = set(
            _executed_test_inventory(
                stdout_parts[1], _C2_IDEMPOTENCE_CONFLICT_TESTS, command_id
            )
        )
        result = {
            "idempotent_retry": _C2_IDEMPOTENCE_CONFLICT_TESTS[0] in executed,
            "conflict_rejected": _C2_IDEMPOTENCE_CONFLICT_TESTS[1] in executed,
            "result_count": len(stdout_parts),
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
            "before_runtime_identity_sha256": _sha256(_canonical_json(before)),
            "after_runtime_identity_sha256": _sha256(_canonical_json(after)),
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
    elif command_id == "c3-empty-preflight":
        preflight = next(
            (payload for payload in parsed_payloads if isinstance(payload, Mapping) and payload.get("stage") == "preflight"),
            None,
        )
        if (
            not isinstance(preflight, Mapping)
            or preflight.get("record_count") != 0
            or preflight.get("index_candidate_count") != 0
        ):
            raise EvidenceValidationError(
                "C3 requires an empty lifecycle record table/index before the first journal seed"
            )
        result = {"empty": True, "result_count": len(stdout_parts)}
    elif command_id == "newer-control-seed":
        workload = next(
            (
                payload
                for payload in parsed_payloads
                if isinstance(payload, Mapping) and _camel_value(payload, "attempted") is not None
            ),
            None,
        )
        control = next(
            (
                payload
                for payload in parsed_payloads
                if isinstance(payload, Mapping) and payload.get("stage") == "control"
            ),
            None,
        )
        expected_seed = {
            "attempted": 125,
            "acknowledged": 125,
            "persisted": 125,
            "conflicted": 0,
            "transaction_acknowledgements": 125,
            "dropped": 0,
            "rejected": 0,
        }
        if not isinstance(workload, Mapping) or not isinstance(control, Mapping):
            raise EvidenceValidationError("newer-record control returned incomplete accounting")
        if any(
            _require_integer(_camel_value(workload, field), f"{command_id}.{field}") != expected
            for field, expected in expected_seed.items()
        ):
            raise EvidenceValidationError("newer-record control accounting is not exact")
        response_record_ids = _require_sequence(
            _camel_value(workload, "record_ids"), f"{command_id}.record_ids"
        )
        expected_record_ids = _qualification_record_ids(
            _require_nonempty_string(_camel_value(workload, "run_id"), f"{command_id}.run_id"),
            _require_nonempty_string(_camel_value(workload, "segment_id"), f"{command_id}.segment_id"),
        )
        names = control.get("newer_record_names")
        if control.get("record_count") != 125 or not isinstance(names, list) or len(names) != 125:
            raise EvidenceValidationError("newer-record control did not isolate its 125 persisted rows")
        if (
            response_record_ids != expected_record_ids
            or names != expected_record_ids
            or len(set(names)) != 125
            or any(re.fullmatch(r"[0-9A-HJKMNP-TV-Z]{26}", str(name)) is None for name in names)
        ):
            raise EvidenceValidationError("newer-record control identities are not exact bounded ULIDs")
        result = {
            "record_count": 125,
            "newer_record_names": names,
            "result_count": len(stdout_parts),
        }
    elif command_id == "cohort-168h-report":
        reporter_job = next(
            (
                payload for payload in parsed_payloads
                if isinstance(payload, Mapping) and payload.get("kind") == "Job"
            ),
            None,
        )
        reporter_spec = reporter_job.get("spec") if isinstance(reporter_job, Mapping) else None
        template = reporter_spec.get("template") if isinstance(reporter_spec, Mapping) else None
        pod_spec = template.get("spec") if isinstance(template, Mapping) else None
        containers = pod_spec.get("containers") if isinstance(pod_spec, Mapping) else None
        reporter = (
            containers[0]
            if isinstance(containers, list) and len(containers) == 1
            and isinstance(containers[0], Mapping)
            else None
        )
        if (
            not isinstance(pod_spec, Mapping)
            or pod_spec.get("automountServiceAccountToken") is not False
            or pod_spec.get("serviceAccountName") != "access-telemetry-adapter"
            or not isinstance(reporter, Mapping)
            or reporter.get("image") != target.get("_reporter_image")
            or reporter.get("command") != _REPORTER_COMMAND
            or reporter.get("args") != _REPORTER_ARGUMENTS
            or reporter.get("env") != _REPORTER_ENV
            or reporter.get("volumeMounts") != _REPORTER_VOLUME_MOUNTS
            or pod_spec.get("volumes") != _REPORTER_VOLUMES
        ):
            raise EvidenceValidationError("physical evidence reporter changed after C1 review")
        receipt = parsed_payloads[-1] if parsed_payloads else None
        if physical_evidence is None or not isinstance(receipt, Mapping):
            raise EvidenceValidationError("physical evidence reporter returned no authenticated receipt")
        if receipt != {"status": "accepted", **physical_evidence}:
            raise EvidenceValidationError("physical evidence reporter receipt does not match submitted evidence")
        result = {
            "reported": True,
            "artifact_sha256": physical_evidence["artifactSha256"],
            "reporter_image_digest": physical_evidence["reporterImageDigest"],
            "result_count": len(stdout_parts),
        }
    elif command_id.startswith("cohort-"):
        stage = command_id.rsplit("-", 1)[-1]
        mappings = [payload for payload in parsed_payloads if isinstance(payload, Mapping) and payload.get("stage") == stage]
        if not mappings:
            raise EvidenceValidationError(f"command {command_id} returned no PostgreSQL aggregate")
        result = dict(mappings[-1])
        if stage == "seed":
            workload = next(
                (
                    payload
                    for payload in parsed_payloads
                    if isinstance(payload, Mapping) and _camel_value(payload, "attempted") is not None
                ),
                None,
            )
            if not isinstance(workload, Mapping):
                raise EvidenceValidationError(f"command {command_id} returned no seed accounting")
            expected_seed = {
                "attempted": 125,
                "acknowledged": 125,
                "persisted": 125,
                "conflicted": 0,
                "transaction_acknowledgements": 125,
                "dropped": 0,
                "rejected": 0,
            }
            if any(
                _require_integer(_camel_value(workload, field), f"{command_id}.{field}") != expected
                for field, expected in expected_seed.items()
            ):
                raise EvidenceValidationError(f"command {command_id} seed accounting is not exact")
            response_record_ids = _require_sequence(
                _camel_value(workload, "record_ids"), f"{command_id}.record_ids"
            )
            expected_record_ids = _qualification_record_ids(
                _require_nonempty_string(_camel_value(workload, "run_id"), f"{command_id}.run_id"),
                _require_nonempty_string(_camel_value(workload, "segment_id"), f"{command_id}.segment_id"),
            )
            if (
                response_record_ids != expected_record_ids
                or result.get("record_ids") != expected_record_ids
                or result.get("pre_tuple_count") != 125
            ):
                raise EvidenceValidationError(f"command {command_id} did not isolate its 125-row cohort")
        if stage == "wait":
            if result.get("ready") is not True:
                raise EvidenceValidationError(f"command {command_id} did not reach its bounded expiry")
            if _require_integer(result.get("candidate_count"), f"{command_id}.candidate_count") <= 0:
                raise EvidenceValidationError(
                    f"command {command_id} cohort disappeared before the due observation"
                )
        if stage == "expiry":
            pre_count = _require_nonzero_integer(
                result.get("pre_tuple_count"), f"{command_id}.pre_tuple_count"
            )
            candidates = _require_nonzero_integer(
                result.get("candidate_count"), f"{command_id}.candidate_count"
            )
            if candidates != pre_count:
                raise EvidenceValidationError(f"command {command_id} cohort expiry is incomplete")
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
            index_mappings = [
                payload
                for payload in parsed_payloads
                if isinstance(payload, Mapping) and payload.get("stage") == "index"
            ]
            if len(index_mappings) != 1:
                raise EvidenceValidationError(f"command {command_id} did not independently measure the expiry index")
            index_name = _require_nonempty_string(
                index_mappings[0].get("index_name"), f"{command_id}.index_name", maximum=128
            )
            post_index_count = _require_integer(
                index_mappings[0].get("post_index_candidate_count"),
                f"{command_id}.post_index_candidate_count",
            )
            result["index_name"] = index_name
            result["post_index_candidate_count"] = post_index_count
            strong_absence = [
                payload
                for payload in parsed_payloads
                if isinstance(payload, Mapping) and payload.get("stage") == "strong-absence"
            ]
            if len(strong_absence) != 1 or _require_integer(
                strong_absence[0].get("strong_absent_read_count"),
                f"{command_id}.strong_absent_read_count",
            ) != 125:
                raise EvidenceValidationError(
                    f"command {command_id} did not strongly read every exact Dapr key as absent"
                )
            result["strong_absent_read_count"] = 125
        if stage == "reclamation":
            if len(mappings) != 2:
                raise EvidenceValidationError(f"command {command_id} did not measure allocator bytes before and after VACUUM")
            result["allocator_free_bytes_before"] = _require_integer(
                mappings[0].get("allocator_free_bytes"), f"{command_id}.allocator_free_bytes_before"
            )
            result["allocator_free_bytes_after"] = _require_nonzero_integer(
                mappings[1].get("allocator_free_bytes"), f"{command_id}.allocator_free_bytes_after"
            )
            result.pop("allocator_free_bytes", None)
            if result["allocator_free_bytes_after"] <= result["allocator_free_bytes_before"]:
                raise EvidenceValidationError(
                    f"command {command_id} did not increase PostgreSQL reusable free space"
                )
            result["os_disk_shrink_claimed"] = False
        result["result_count"] = len(stdout_parts)
    elif command_id == "retention-controls":
        configmap = next(
            (payload for payload in parsed_payloads if isinstance(payload, Mapping) and isinstance(payload.get("data"), Mapping)),
            None,
        )
        inspection = next(
            (payload for payload in reversed(parsed_payloads) if isinstance(payload, Mapping) and _camel_value(payload, "retained_record_count") is not None),
            None,
        )
        if not isinstance(configmap, Mapping) or not isinstance(inspection, Mapping):
            raise EvidenceValidationError("retention controls were not observed from the running target")
        _, health = _inspection(inspection, command_id)
        if not configmap.get("data") or health in {"unhealthy", "3"}:
            raise EvidenceValidationError("retention controls are not active on the running target")
        executed = set(_executed_test_inventory(stdout_parts[1], _RETENTION_PROOF_TESTS, command_id))
        result = {
            "maximum_clock_delta_ms": 250,
            "late_record_remaining_lifetime": "PersistAsync_WritesRecordAndExpiryIndexAtomicallyWithCeilingTtl" in executed,
            "already_expired_rejected": "PersistAsync_FutureOrExpiredSource_FailsClosed" in executed,
            "attestation_freshness_rejected": "Verify_ReplayStaleDeltaOrTamperedSignature_FailsClosed" in executed,
            "attestation_replay_rejected": "Verify_ReplayStaleDeltaOrTamperedSignature_FailsClosed" in executed,
            "attestation_identity_rejected": "Verify_ContextProfileOrNonceMismatch_FailsClosed" in executed,
            "logical_expiry_millisecond": "PersistAsync_WritesRecordAndExpiryIndexAtomicallyWithCeilingTtl" in executed,
            "ttl_defense_in_depth": "PersistAsync_WritesRecordAndExpiryIndexAtomicallyWithCeilingTtl" in executed,
            "result_count": len(stdout_parts),
        }
    elif command_id.startswith("failure-"):
        scenario = command_id.removeprefix("failure-")
        proof = _C4_MECHANISM_PROOF_TESTS.get(scenario)
        proof_executed = True
        if proof is not None:
            proof_output = next(
                (part for part in stdout_parts if b"=== TEST EXECUTION SUMMARY ===" in part),
                None,
            )
            if proof_output is None:
                raise EvidenceValidationError(f"command {command_id} returned no mechanism proof")
            proof_executed = bool(_executed_test_inventory(proof_output, proof[2], command_id))
        pod_lists = [payload for payload in parsed_payloads if isinstance(payload, Mapping) and isinstance(payload.get("items"), list)]
        workload = next(
            (payload for payload in parsed_payloads if isinstance(payload, Mapping) and _camel_value(payload, "attempted") is not None),
            None,
        )
        business_payload = next(
            (
                payload
                for payload in parsed_payloads
                if isinstance(payload, Mapping) and type(payload.get("business_status")) is int
            ),
            None,
        )
        if len(pod_lists) < 3 or not isinstance(workload, Mapping) or not isinstance(business_payload, Mapping):
            raise EvidenceValidationError(f"command {command_id} lacks fault/business/accounting observations")
        before = _pod_snapshot(pod_lists[1], command_id)
        after = _pod_snapshot(pod_lists[-1], command_id)
        if selected_pod is None:
            raise EvidenceValidationError(f"command {command_id} has no selected fault target")
        selected_before = next((item for item in before if item["name"] == selected_pod), None)
        after_by_uid = {item["uid"]: item for item in after}
        exercised = selected_before is not None and (
            selected_before["uid"] not in after_by_uid
            or after_by_uid[selected_before["uid"]]["restarts"] > selected_before["restarts"]
        )
        recovered = len(after) >= len(before) and all(item["ready"] for item in after)
        attempted = _require_nonzero_integer(_camel_value(workload, "attempted"), f"{command_id}.attempted")
        persisted = _require_integer(_camel_value(workload, "persisted"), f"{command_id}.persisted")
        conflicted = _require_integer(_camel_value(workload, "conflicted"), f"{command_id}.conflicted")
        rejected = _require_integer(_camel_value(workload, "rejected"), f"{command_id}.rejected") + conflicted
        dropped = _require_integer(_camel_value(workload, "dropped"), f"{command_id}.dropped")
        if attempted != persisted + rejected + dropped:
            raise EvidenceValidationError(f"command {command_id} lifecycle accounting is not exact")
        expected_disposition = _C4_EXPECTED_DISPOSITIONS[scenario]
        observed_disposition = next(
            (
                disposition
                for disposition, count in (
                    ("persisted", persisted), ("rejected", rejected), ("dropped", dropped)
                )
                if count == attempted
            ),
            "mixed",
        )
        audit_continuity = _has_correlated_audit_record(stdout_parts[-1], workload, command_id)
        prometheus_samples = _prometheus_sample_count(stdout_parts[-2], command_id)
        business_status = _require_integer(
            business_payload.get("business_status"), f"{command_id}.business_status"
        )
        result = {
            "exercised": (
                exercised and recovered and proof_executed
                and observed_disposition == expected_disposition
            ),
            "expected_disposition": expected_disposition,
            "business_operation_succeeded": business_status == 200,
            "business_requests": 1,
            "business_failures": 0 if business_status == 200 else 1,
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
        inspection = next(
            (payload for payload in reversed(parsed_payloads) if isinstance(payload, Mapping) and _camel_value(payload, "retained_record_count") is not None),
            None,
        )
        if not all(isinstance(value, Mapping) for value in (deployment, dapr_configuration, inspection)):
            raise EvidenceValidationError(f"command {command_id} lacks deployment, Dapr, or lifecycle observations")
        correlated_workload = parsed_payloads[3] if command_id in {"continuity", "observability"} else None
        console_index = 4 if correlated_workload is not None else 4
        metrics_index = 6 if correlated_workload is not None else 5
        logs = stdout_parts[console_index].decode("utf-8", errors="strict")
        json_console = (
            _has_correlated_audit_record(stdout_parts[console_index], correlated_workload, command_id)
            if isinstance(correlated_workload, Mapping)
            else any(line.lstrip().startswith("{") for line in logs.splitlines())
        )
        otlp_record = (
            _has_correlated_audit_record(stdout_parts[5], correlated_workload, command_id)
            if isinstance(correlated_workload, Mapping)
            else False
        )
        metrics = (
            _lifecycle_prometheus_observation(stdout_parts[metrics_index], command_id)
            if command_id == "observability"
            else None
        )
        prometheus_samples = (
            metrics["sample_count"]
            if metrics is not None
            else _prometheus_sample_count(stdout_parts[metrics_index], command_id)
        )
        _, health = _inspection(inspection, command_id)
        deployment_text = _canonical_json(deployment)
        dapr_text = _canonical_json(dapr_configuration)
        otlp_configured = "OTEL_EXPORTER_OTLP_ENDPOINT" in deployment_text
        if command_id == "continuity":
            result = {
                "console_continuity": json_console,
                "otlp_configured": otlp_configured,
                "otlp_continuity": otlp_record if otlp_configured else False,
                "direct_backend_dependencies": [
                    name for name in ("ConnectionStrings", "PostgreSql", "Redis") if name in deployment_text
                ],
                "lifecycle_health": health,
                "result_count": len(stdout_parts),
            }
        elif command_id == "observability":
            if metrics is None:
                raise EvidenceValidationError("observability lacks canonical lifecycle metrics")
            executed = set(
                _executed_test_inventory(stdout_parts[-1], _OBSERVABILITY_PROOF_TESTS, command_id)
            )
            health_transition_proved = (
                "HealthPrecedence_IsUnhealthyThenDegradedThenNoDataOrHealthy" in executed
                and "RuntimeGate_ClosesImmediatelyWhenPublishedEvidenceExpires" in executed
            )
            observed = (
                prometheus_samples > 0
                and metrics["profile_matched"]
                and metrics["physical_evidence_present"]
                and metrics["current_health_present"]
            )
            result = {
                "signals": metrics["signals"],
                "labels": metrics["labels"],
                "alerts_passed": observed and health_transition_proved,
                "bounded_labels": metrics["labels"] == ["state", "reason", "outcome"],
                "health_precedence": health_transition_proved,
                "no_data_passed": "HealthPrecedence_IsUnhealthyThenDegradedThenNoDataOrHealthy" in executed,
                "last_evidence_timestamp_gauge": metrics["last_evidence_timestamp_gauge"],
                "json_console_continuity": json_console,
                "otlp_configured": otlp_configured,
                "otlp_continuity": otlp_record if otlp_configured else False,
                "result_count": len(stdout_parts),
            }
        else:
            acl = dapr_configuration.get("spec", {}).get("accessControl") if isinstance(dapr_configuration.get("spec"), Mapping) else None
            deny_by_default = isinstance(acl, Mapping) and acl.get("defaultAction") == "deny"
            no_read_route = "/v1/access-telemetry/read" not in dapr_text
            executed = _executed_test_inventory(stdout_parts[-1], _PRIVACY_PROOF_TESTS, command_id)
            privacy_request = parsed_payloads[3]
            if not isinstance(privacy_request, Mapping):
                raise EvidenceValidationError("privacy denial returned no target HTTP observation")
            allowed_status = _require_integer(
                privacy_request.get("allowed_status"), "privacy.allowed_status"
            )
            denied_status = _require_integer(
                privacy_request.get("denied_status"), "privacy.denied_status"
            )
            dependency_calls = _require_integer(
                privacy_request.get("denied_dependency_calls"),
                "privacy.denied_dependency_calls",
                minimum=0,
            )
            target_denial = allowed_status == 200 and denied_status == 403
            result = {
                "inspection_least_privilege": deny_by_default,
                "no_tenant_read_route": no_read_route,
                "raw_values_absent": not any(
                    marker in logs.lower()
                    for marker in ('"tenantid":', '"query":', '"userid":', '"recordid":')
                ),
                "secret_values_absent": not any(
                    marker in logs.lower()
                    for marker in ('"password":', '"token":', '"authorization":', '"credential":')
                ),
                "tenant_denial_before_dependencies": (
                    deny_by_default
                    and target_denial
                    and dependency_calls == 0
                    and executed == list(_PRIVACY_PROOF_TESTS)
                ),
                "dependency_calls_after_denial": dependency_calls,
                "tenant_denial_tests": executed,
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
    acquired_ownership = False
    if isinstance(target, dict):
        target.pop("_lease_acquired", None)
        target.pop("_cleanup_expired_lease", None)
    if checkpoint == "c4-failure-privacy-observability":
        # Credential validation is deliberately outside the protected mutation
        # region so an unusable canary can never open the qualification gate.
        if not isinstance(target.get("_business_bearer"), bytes):
            if not isinstance(target, dict):
                raise EvidenceValidationError("qualification bearer context is immutable")
            target["_business_bearer"] = _load_business_bearer()
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
        acquired_ownership = True
        remaining_command_ids = list(command_ids)
        body_results: dict[str, Mapping[str, Any]] = {}
        body_commands: list[dict[str, Any]] = []
        if remaining_command_ids[:2] == ["writer-1", "writer-2"]:
            # Both fixed requests must overlap so the cluster observes the ADR's
            # exact 250 accepted records/s, rather than two sequential 125/s runs.
            stop_renew = threading.Event()
            renew_error: list[BaseException] = []

            def renew_session() -> None:
                interval = _writer_renew_interval_seconds()
                try:
                    while not stop_renew.wait(timeout=interval):
                        _, command = _run_operation(
                            target, checkpoint, "qualification-renew"
                        )
                        if isinstance(target, dict):
                            renewals = target.setdefault("_session_renew_commands", [])
                            named = dict(command)
                            named["command_id"] = f"qualification-renew:{len(renewals) + 1}"
                            renewals.append(named)
                except BaseException as exc:
                    renew_error.append(exc)
                    _TERMINATION_REQUESTED.set()

            renew_thread = threading.Thread(
                target=renew_session,
                name="story-27-4-renew",
                daemon=True,
            )
            renew_thread.start()
            with ThreadPoolExecutor(max_workers=2, thread_name_prefix="story-27-4-writer") as writers:
                try:
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
                    throughput_result, throughput_command = _run_operation(
                        target, checkpoint, "component-throughput"
                    )
                    body_results["component-throughput"] = {
                        **throughput_result,
                        "_command": throughput_command,
                    }
                    body_commands.append(throughput_command)
                except BaseException:
                    _TERMINATION_REQUESTED.set()
                    raise
                finally:
                    stop_renew.set()
                    renew_thread.join(timeout=30)
            if isinstance(target, dict):
                body_commands.extend(list(target.get("_session_renew_commands", [])))
            if renew_error:
                raise renew_error[0]
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
        if acquired_ownership or target.get("_lease_acquired") is True or target.get("_cleanup_expired_lease") is True:
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
        "idempotence-conflict-proof",
    ]
    results, commands = _execute_qualification(target, "c2-production-replacement", command_ids)
    writers = []
    segment_proofs = sorted(
        [
            dict(proof)
            for index in (1, 2)
            for proof in _require_sequence(
                results[f"writer-{index}"].get("_segment_proofs"),
                f"writer-{index} segment proofs",
            )
            if isinstance(proof, Mapping)
        ],
        key=lambda proof: (proof["started_utc_ms"], proof["writer"]),
    )

    def post_replacement_proof(command: Mapping[str, Any]) -> dict[str, Any]:
        finished_utc_ms = _require_integer(
            command.get("finished_utc_ms"), "replacement command finished_utc_ms", minimum=1
        )
        proof = next(
            (candidate for candidate in segment_proofs if candidate["started_utc_ms"] >= finished_utc_ms),
            None,
        )
        if proof is None or proof.get("durable_count") != 125:
            raise EvidenceValidationError(
                "C2 replacement has no exact succeeding durable writer segment"
            )
        return {
            key: proof[key]
            for key in (
                "segment_id", "writer", "writer_pod", "started_utc_ms", "finished_utc_ms",
                "record_inventory_sha256", "durable_count",
            )
        }
    for index in (1, 2):
        result = _without_result_count(results[f"writer-{index}"])
        result.pop("_command", None)
        result.pop("_segment_proofs", None)
        result.pop("dropped", None)
        result.pop("rejected", None)
        result["observation"] = _result_observation(results[f"writer-{index}"]["_command"])
        writers.append(result)
    replacements = {}
    for name in REQUIRED_REPLACEMENTS:
        value = _without_result_count(results[f"replace-{name}"])
        value.pop("_command", None)
        value["mutation_finished_utc_ms"] = results[f"replace-{name}"]["_command"][
            "finished_utc_ms"
        ]
        value["post_replacement_segment"] = post_replacement_proof(
            results[f"replace-{name}"]["_command"]
        )
        value["observation"] = _result_observation(results[f"replace-{name}"]["_command"])
        replacements[name] = value
    adapter = _without_result_count(results["approved-adapter-fault"])
    adapter.pop("_command", None)
    adapter["mutation_finished_utc_ms"] = results["approved-adapter-fault"]["_command"][
        "finished_utc_ms"
    ]
    adapter["post_replacement_segment"] = post_replacement_proof(
        results["approved-adapter-fault"]["_command"]
    )
    adapter["observation"] = _result_observation(results["approved-adapter-fault"]["_command"])
    continuity = results["continuity"]
    conflict_proof = results["idempotence-conflict-proof"]
    attempted = sum(results[f"writer-{index}"]["attempted"] for index in (1, 2))
    acknowledged = sum(results[f"writer-{index}"]["acknowledged"] for index in (1, 2))
    persisted = sum(results[f"writer-{index}"]["persisted"] for index in (1, 2))
    conflicted = sum(results[f"writer-{index}"]["conflicted"] for index in (1, 2))
    transaction_acks = sum(
        results[f"writer-{index}"]["transaction_acknowledgements"] for index in (1, 2)
    )
    overlap_milliseconds = min(
        results["writer-1"]["finished_utc_ms"], results["writer-2"]["finished_utc_ms"]
    ) - max(results["writer-1"]["started_utc_ms"], results["writer-2"]["started_utc_ms"])
    if overlap_milliseconds <= 0:
        raise EvidenceValidationError("C2 writers returned no common measured interval")
    component_counter = results["component-throughput"]
    operation_delta = _require_nonzero_integer(
        component_counter.get("operation_delta"), "component-throughput.operation_delta"
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
    renew_observations = [
        _result_observation(command)
        for command in commands
        if str(command.get("command_id", "")).startswith("qualification-renew:")
    ]
    if not renew_observations:
        raise EvidenceValidationError("C2 did not renew its owned Lease and gate")
    return {
        "writers": {
            "steady_state_minutes": 30,
            "cluster_accepted_records_per_second": 250,
            "component_operations_per_second": (operation_delta * 1000) // overlap_milliseconds,
            "component_counter": {
                "counter_name": component_counter["counter_name"],
                "window_milliseconds": component_counter["window_milliseconds"],
                "operation_delta": operation_delta,
                "observation": _result_observation(component_counter["_command"]),
            },
            "overlap_milliseconds": overlap_milliseconds,
            "writer_results": writers,
            "acknowledged_loss": acknowledged_loss,
            "actor_serialized": attempted == acknowledged + conflicted,
            "idempotent_retry": conflict_proof.get("idempotent_retry"),
            "conflict_rejected": conflict_proof.get("conflict_rejected"),
            "idempotence_conflict_observation": _result_observation(conflict_proof["_command"]),
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
        "renew_observations": renew_observations,
    }, commands


def _c3(
    target: Mapping[str, str],
    journal_path: Path,
) -> tuple[dict[str, Any], list[dict[str, Any]]]:
    initial_identity, initial_identity_command = _run_operation(
        target, "c3-retention-reclamation", "qualification-target-identity"
    )
    runtime_identity_sha256 = _require_nonempty_string(
        initial_identity.get("runtime_identity_sha256"),
        "C3 initial runtime identity",
        maximum=64,
    )
    initial_identity_command = {
        **initial_identity_command,
        "command_id": "c3-runtime-identity",
    }
    command_ids = ["retention-controls", "c3-empty-preflight"]
    for hours in (168, 24, 1):
        command_ids.append(f"cohort-{hours}h-seed")
    for hours in (1, 24, 168):
        command_ids.append(f"cohort-{hours}h-wait")
        if hours == 168:
            command_ids.append("newer-control-seed")
        command_ids.extend(
            (f"cohort-{hours}h-expiry", f"cohort-{hours}h-purge", f"cohort-{hours}h-reclamation")
        )
        if hours == 168:
            command_ids.append("cohort-168h-report")
    journal_context = {
        "target_kind": target["kind"],
        "kube_context": target["kube_context"],
        "namespace": target["namespace"],
        "profile_sha256": STORY_27_4_PROFILE_SHA256,
        "workload_sha256": STORY_27_4_WORKLOAD_SHA256,
        "c1_platform_operations_reviewer": target["_platform_operations_reviewer"],
        "source_commit": os.environ.get("HEXALITH_STORY_27_4_SOURCE_COMMIT", "unbound"),
        "producer_source_sha256": os.environ.get("HEXALITH_STORY_27_4_PRODUCER_SHA256", "unbound"),
        "first_runtime_identity_sha256": runtime_identity_sha256,
    }
    with _C3Journal(journal_path, journal_context) as journal:
        resume_results = journal.resume_results
        resumed_ids = list(resume_results)
        if resumed_ids != command_ids[: len(resumed_ids)]:
            raise EvidenceValidationError(
                "C3 journal must contain an exact completed prefix with no skips or reordering"
            )
        results: dict[str, Mapping[str, Any]] = {}
        commands: list[dict[str, Any]] = [initial_identity_command]

        def run_plain(command_id: str) -> None:
            if command_id in resume_results:
                resumed = dict(resume_results[command_id])
                results[command_id] = resumed
                commands.append(dict(_require_mapping(resumed["_command"], f"{command_id} command")))
                return
            result, command = _run_operation(target, "c3-retention-reclamation", command_id)
            stored = {**result, "_command": command}
            journal.append(command_id, stored)
            results[command_id] = stored
            commands.append(command)

        def run_qualified(command_id: str) -> None:
            if command_id in resume_results:
                resumed = dict(resume_results[command_id])
                session_commands = _require_sequence(
                    resumed.get("_session_commands"), f"{command_id} session commands"
                )
                results[command_id] = resumed
                commands.extend(dict(_require_mapping(item, f"{command_id} session command")) for item in session_commands)
                return
            session_results, session_commands = _execute_qualification(
                target, "c3-retention-reclamation", [command_id]
            )
            renamed_commands: list[dict[str, Any]] = []
            renamed_by_original: dict[str, dict[str, Any]] = {}
            for command in session_commands:
                renamed = dict(command)
                original_id = str(renamed["command_id"])
                if original_id != command_id:
                    renamed["command_id"] = f"{command_id}:{original_id}"
                renamed_commands.append(renamed)
                renamed_by_original[original_id] = renamed
            for transition_id in (
                "qualification-target-identity",
                "qualification-enable",
                "qualification-disable",
                "qualification-final-state",
            ):
                transition_result = dict(session_results[transition_id])
                transition_result["_command"] = renamed_by_original[transition_id]
                session_results[transition_id] = transition_result
            body = dict(session_results[command_id])
            body["_command"] = renamed_by_original[command_id]
            body["_session_transition"] = _transition(
                session_results["qualification-target-identity"],
                session_results["qualification-enable"],
                session_results["qualification-disable"],
                session_results["qualification-final-state"],
                target,
            )
            body["_session_commands"] = renamed_commands
            journal.append(command_id, body)
            results[command_id] = body
            commands.extend(renamed_commands)

        def bind_cohort_identity(hours: int) -> None:
            command_id = f"cohort-{hours}h-seed"
            record_ids = _require_sequence(results[command_id].get("record_ids"), f"{command_id}.record_ids")
            if len(record_ids) != 125 or len(set(record_ids)) != 125 or any(
                not isinstance(record_id, str) or
                re.fullmatch(r"[0-9A-HJKMNP-TV-Z]{26}", record_id) is None
                for record_id in record_ids
            ):
                raise EvidenceValidationError(f"{command_id} did not bind 125 exact record ULIDs")
            if not isinstance(target, dict):
                raise EvidenceValidationError("C3 target context is not mutable")
            cohorts = target.setdefault("_c3_cohort_record_ids", {})
            if not isinstance(cohorts, dict):
                raise EvidenceValidationError("C3 cohort identity context is invalid")
            cohorts[hours] = list(record_ids)

        run_qualified("retention-controls")
        run_plain("c3-empty-preflight")
        for hours in (168, 24, 1):
            run_qualified(f"cohort-{hours}h-seed")
            bind_cohort_identity(hours)
        for hours in (1, 24, 168):
            run_plain(f"cohort-{hours}h-wait")
            if hours == 168:
                run_qualified("newer-control-seed")
            run_plain(f"cohort-{hours}h-expiry")
            run_qualified(f"cohort-{hours}h-purge")
            run_qualified(f"cohort-{hours}h-reclamation")
            if hours == 168:
                if not isinstance(target, dict):
                    raise EvidenceValidationError("C3 target context is not mutable")
                reclamation_result = results["cohort-168h-reclamation"]
                target["_c3_reclamation_observation"] = {
                    "reclaimed_utc_ms": reclamation_result["reclaimed_utc_ms"],
                    "allocator_free_bytes": reclamation_result["allocator_free_bytes_after"],
                }
                target["_c3_reporter_artifact_sha256"] = journal.authenticated_prefix_sha256
                run_qualified("cohort-168h-report")
    cohorts = []
    for hours in (1, 24, 168):
        merged: dict[str, Any] = {}
        for stage in ("seed", "expiry", "purge", "reclamation"):
            command_id = f"cohort-{hours}h-{stage}"
            partial = _without_result_count(results[command_id])
            for private in ("_command", "_session_commands", "_session_transition", "stage"):
                partial.pop(private, None)
            for key, value in partial.items():
                if key in merged and merged[key] != value:
                    raise EvidenceValidationError(f"cohort {hours}h returned inconsistent {key}")
                merged[key] = value
            merged[f"{stage}_observation"] = _result_observation(results[command_id]["_command"])
            if stage in {"seed", "purge", "reclamation"}:
                merged[f"{stage}_transition"] = results[command_id]["_session_transition"]
        merged["wait_observation"] = _result_observation(
            results[f"cohort-{hours}h-wait"]["_command"]
        )
        pre_count = _require_nonzero_integer(merged.get("pre_tuple_count"), f"cohort-{hours}h.pre_tuple_count")
        post_count = _require_integer(merged.get("post_tuple_count"), f"cohort-{hours}h.post_tuple_count")
        candidate_count = _require_nonzero_integer(merged.get("candidate_count"), f"cohort-{hours}h.candidate_count")
        deleted_count = min(candidate_count, max(0, pre_count - post_count))
        merged["deleted_count"] = deleted_count
        merged["already_absent_count"] = candidate_count - deleted_count
        post_index_count = _require_integer(
            merged.pop("post_index_candidate_count", None),
            f"cohort-{hours}h.post_index_candidate_count",
        )
        _require_nonempty_string(merged.pop("index_name", None), f"cohort-{hours}h.index_name")
        if post_index_count > candidate_count:
            raise EvidenceValidationError(f"cohort {hours}h expiry index count increased after purge")
        merged["index_removal_count"] = candidate_count - post_index_count
        cohorts.append(merged)
    retention = _without_result_count(results["retention-controls"])
    for private in ("_command", "_session_commands", "_session_transition"):
        retention.pop(private, None)
    return {
        "runtime_identity_sha256": runtime_identity_sha256,
        "runtime_identity_observation": _result_observation(initial_identity_command),
        "retention": retention,
        "retention_observation": _result_observation(results["retention-controls"]["_command"]),
        "retention_transition": results["retention-controls"]["_session_transition"],
        "empty_preflight_observation": _result_observation(results["c3-empty-preflight"]["_command"]),
        "final_newer_control": {
            "record_count": results["newer-control-seed"]["record_count"],
            "newer_record_names": results["newer-control-seed"]["newer_record_names"],
            "observation": _result_observation(results["newer-control-seed"]["_command"]),
            "transition": results["newer-control-seed"]["_session_transition"],
        },
        "physical_report": {
            "reported": results["cohort-168h-report"]["reported"],
            "artifact_sha256": results["cohort-168h-report"]["artifact_sha256"],
            "reporter_image_digest": results["cohort-168h-report"]["reporter_image_digest"],
            "observation": _result_observation(results["cohort-168h-report"]["_command"]),
            "transition": results["cohort-168h-report"]["_session_transition"],
        },
        "cohorts": cohorts,
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
    previous_sigterm: Any = None
    if threading.current_thread() is threading.main_thread():
        previous_sigterm = signal.getsignal(signal.SIGTERM)

        def request_cleanup(_signum: int, _frame: Any) -> None:
            _TERMINATION_REQUESTED.set()
            raise EvidenceValidationError("qualification producer termination requested")

        signal.signal(signal.SIGTERM, request_cleanup)
    try:
        reviewer = _require_nonempty_string(
            args.platform_operations_reviewer,
            "validated C1 platform-operations reviewer",
            maximum=64,
        )
        if _SAFE_TARGET.fullmatch(reviewer) is None:
            raise EvidenceValidationError("validated C1 platform-operations reviewer is not bounded")
        target = dict(_load_target(Path(args.scenario_input)))
        target["_platform_operations_reviewer"] = reviewer
        target["_lease_holder"] = f"story-27-4/{reviewer}/{os.getpid()}-{_utc_now_milliseconds()}"
        if checkpoint == "c4-failure-privacy-observability" and not args.disable_only:
            # Credential validation is intentionally completed before the first
            # kubectl command can inspect or mutate the qualification target.
            target["_business_bearer"] = _load_business_bearer()
        if args.disable_only:
            try:
                _run_operation(target, checkpoint, "qualification-target-identity")
            except EvidenceValidationError:
                if target.get("_cleanup_expired_lease") is not True:
                    raise
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
    finally:
        if previous_sigterm is not None:
            signal.signal(signal.SIGTERM, previous_sigterm)
        _TERMINATION_REQUESTED.clear()
