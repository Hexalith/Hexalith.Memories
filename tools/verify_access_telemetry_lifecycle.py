"""Fail-closed evidence primitives for the Story 27.3 adapter profile gate."""

from __future__ import annotations

from dataclasses import dataclass
from decimal import Decimal, InvalidOperation
import hashlib
import json
from pathlib import Path
import re
import subprocess
from datetime import datetime, timezone
from typing import Any, Mapping


class EnvironmentIdentityError(ValueError):
    """Raised when a Production-shaped execution identity is incomplete."""


class CapacityInputError(ValueError):
    """Raised when a capacity operand cannot be normalized safely."""


_REQUIRED_IDENTITY_FIELDS = (
    "KUBE_CONTEXT",
    "KUBE_NAMESPACE",
    "DEPLOYMENT_ID",
    "PROFILE_ID",
    "EVIDENCE_ROOT",
    "DECLARED_SINGLE_COMPONENT_FAULT",
)


@dataclass(frozen=True)
class EnvironmentIdentity:
    """Identifies the reviewed deployment without carrying credentials."""

    kube_context: str
    kube_namespace: str
    deployment_id: str
    profile_id: str
    evidence_root: str
    declared_single_component_fault: str

    @classmethod
    def from_mapping(cls, values: Mapping[str, Any]) -> "EnvironmentIdentity":
        missing = [
            name
            for name in _REQUIRED_IDENTITY_FIELDS
            if not isinstance(values.get(name), str) or not values[name].strip()
        ]
        if missing:
            raise EnvironmentIdentityError(
                "Production identity is incomplete: " + ", ".join(missing)
            )

        normalized = {
            name: values[name].strip()
            for name in _REQUIRED_IDENTITY_FIELDS
        }
        if any("\n" in value or "\r" in value for value in normalized.values()):
            raise EnvironmentIdentityError("Production identity cannot contain line breaks")

        return cls(
            kube_context=normalized["KUBE_CONTEXT"],
            kube_namespace=normalized["KUBE_NAMESPACE"],
            deployment_id=normalized["DEPLOYMENT_ID"],
            profile_id=normalized["PROFILE_ID"],
            evidence_root=normalized["EVIDENCE_ROOT"],
            declared_single_component_fault=normalized["DECLARED_SINGLE_COMPONENT_FAULT"],
        )

    def to_dict(self) -> dict[str, str]:
        """Return the non-secret identity fields in evidence-friendly form."""

        return {
            "kube_context": self.kube_context,
            "kube_namespace": self.kube_namespace,
            "deployment_id": self.deployment_id,
            "profile_id": self.profile_id,
            "evidence_root": self.evidence_root,
            "declared_single_component_fault": self.declared_single_component_fault,
        }


@dataclass(frozen=True)
class WorkloadEnvelope:
    """The deterministic two-writer workload required by ADR 27.1."""

    writer_count: int
    events_per_second_per_writer: dict[str, float]

    @property
    def total_events_per_second(self) -> int:
        total = sum(self.events_per_second_per_writer.values()) * self.writer_count
        if total != int(total):
            raise ValueError("ADR workload total must be an integer")
        return int(total)

    def to_dict(self) -> dict[str, Any]:
        return {
            "writer_count": self.writer_count,
            "events_per_second_per_writer": dict(self.events_per_second_per_writer),
            "total_events_per_second": self.total_events_per_second,
        }


ADR_TWO_WRITER_WORKLOAD = WorkloadEnvelope(
    writer_count=2,
    events_per_second_per_writer={
        "search": 200,
        "ingest": 6,
        "traverse": 10,
        "case_access": 16,
        "delete": 2,
        "tenant_lifecycle": 0.2,
        "tenant_config": 0.8,
        "case_member": 4,
        "annotation": 11,
    },
)


@dataclass(frozen=True)
class CapacityRequirement:
    """Checked capacity result for one retention horizon."""

    horizon: str
    records: int
    required_bytes: int


_BYTE_UNITS = {
    "B": (1, "none"),
    "KB": (1000, "decimal"),
    "MB": (1000**2, "decimal"),
    "GB": (1000**3, "decimal"),
    "TB": (1000**4, "decimal"),
    "KIB": (1024, "binary"),
    "MIB": (1024**2, "binary"),
    "GIB": (1024**3, "binary"),
    "TIB": (1024**4, "binary"),
}
_BYTE_VALUE = re.compile(r"^\s*([0-9]+(?:\.[0-9]+)?)\s*([A-Za-z]+)?\s*$")
_INT64_MAX = 2**63 - 1


def _normalize_integer(value: Any, name: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int) or value < 0 or value > _INT64_MAX:
        raise CapacityInputError(f"{name} must be a non-negative signed 64-bit integer")
    return value


def _normalize_bytes(value: Any, name: str) -> tuple[int, str]:
    if isinstance(value, bool):
        raise CapacityInputError(f"{name} must be an integer byte value")
    if isinstance(value, int):
        return _normalize_integer(value, name), "none"
    if not isinstance(value, str):
        raise CapacityInputError(f"{name} must include an explicit byte unit")

    match = _BYTE_VALUE.fullmatch(value)
    if match is None:
        raise CapacityInputError(f"{name} is not a finite byte value")
    unit = (match.group(2) or "B").upper()
    if unit not in _BYTE_UNITS:
        raise CapacityInputError(f"{name} uses an unknown byte unit: {unit}")
    try:
        amount = Decimal(match.group(1))
    except InvalidOperation as exc:
        raise CapacityInputError(f"{name} is not a finite byte value") from exc
    multiplier, system = _BYTE_UNITS[unit]
    result = amount * multiplier
    if not result.is_finite() or result != result.to_integral_value():
        raise CapacityInputError(f"{name} must normalize to an integer byte value")
    integer = int(result)
    if integer < 0 or integer > _INT64_MAX:
        raise CapacityInputError(f"{name} exceeds the signed 64-bit input bound")
    return integer, system


def calculate_capacity(
    *,
    records: Any,
    measured_record_bytes: Any,
    measured_index_bytes: Any,
    durability_multiplier: Any,
    control_bytes: Any,
    reclamation_workspace: Any,
    horizons: Mapping[str, Any] | None = None,
) -> list[CapacityRequirement]:
    """Calculate checked retention capacity without accepting ambiguous units."""

    record_count = _normalize_integer(records, "records")
    multiplier = _normalize_integer(durability_multiplier, "durability_multiplier")
    if multiplier == 0:
        raise CapacityInputError("durability_multiplier must be greater than zero")

    record_bytes, record_system = _normalize_bytes(measured_record_bytes, "measured_record_bytes")
    index_bytes, index_system = _normalize_bytes(measured_index_bytes, "measured_index_bytes")
    control, control_system = _normalize_bytes(control_bytes, "control_bytes")
    workspace, workspace_system = _normalize_bytes(reclamation_workspace, "reclamation_workspace")
    systems = {record_system, index_system, control_system, workspace_system} - {"none"}
    if len(systems) > 1:
        raise CapacityInputError("decimal and binary byte units cannot be mixed")

    horizon_records = horizons or {"1h": record_count, "24h": record_count, "7d": record_count}
    results = []
    for horizon, count in horizon_records.items():
        normalized_count = _normalize_integer(count, f"records[{horizon}]")
        try:
            required = (
                normalized_count * (record_bytes + index_bytes) * multiplier
                + control
                + workspace
            )
        except OverflowError as exc:
            raise CapacityInputError(f"capacity arithmetic overflowed for {horizon}") from exc
        if required < 0 or required > _INT64_MAX:
            raise CapacityInputError(f"capacity arithmetic exceeded the signed 64-bit bound for {horizon}")
        results.append(CapacityRequirement(horizon, normalized_count, required))
    return results


def _canonical_json(value: Any) -> str:
    return json.dumps(value, ensure_ascii=True, sort_keys=True, separators=(",", ":"))


def _sha256(value: str) -> str:
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


@dataclass(frozen=True)
class AdapterProfile:
    """Immutable, hashable profile input for adapter approval evidence."""

    identity: Mapping[str, Any]
    capabilities: Mapping[str, Any]
    workload: Mapping[str, Any]

    def manifest(self) -> dict[str, Any]:
        canonical_profile = {
            "capabilities": dict(self.capabilities),
            "identity": dict(self.identity),
            "workload": dict(self.workload),
        }
        canonical_profile_json = _canonical_json(canonical_profile)
        mutation_manifest = {
            "allowed_mutations": [],
            "profile_sha256": _sha256(canonical_profile_json),
        }
        mutation_manifest_json = _canonical_json(mutation_manifest)
        return {
            "canonical_profile": canonical_profile,
            "canonical_profile_json": canonical_profile_json,
            "profile_sha256": _sha256(canonical_profile_json),
            "mutation_manifest_sha256": _sha256(mutation_manifest_json),
        }


@dataclass(frozen=True)
class CommandObservation:
    """Redacted result metadata for one read-only deployment query."""

    command: tuple[str, ...]
    exit_code: int
    stdout_sha256: str
    stderr_sha256: str
    payload: Any | None
    error: str | None = None


def _run_kubectl(identity: EnvironmentIdentity, *arguments: str) -> CommandObservation:
    command = (
        "kubectl",
        "--context",
        identity.kube_context,
        "--namespace",
        identity.kube_namespace,
        *arguments,
    )
    result = subprocess.run(
        command,
        check=False,
        capture_output=True,
        text=True,
        timeout=60,
    )
    payload = None
    error = None
    if result.returncode == 0:
        try:
            payload = json.loads(result.stdout)
        except json.JSONDecodeError as exc:
            error = f"kubectl returned non-JSON output: {exc.msg}"
    else:
        error = f"kubectl exited {result.returncode}"
    return CommandObservation(
        command=command,
        exit_code=result.returncode,
        stdout_sha256=_sha256(result.stdout),
        stderr_sha256=_sha256(result.stderr),
        payload=payload,
        error=error,
    )


def _run_dapr_version(identity: EnvironmentIdentity, pod_name: str) -> CommandObservation:
    command = (
        "kubectl",
        "--context",
        identity.kube_context,
        "--namespace",
        identity.kube_namespace,
        "exec",
        pod_name,
        "-c",
        "daprd",
        "--",
        "daprd",
        "--version",
    )
    result = subprocess.run(
        command,
        check=False,
        capture_output=True,
        text=True,
        timeout=60,
    )
    return CommandObservation(
        command=command,
        exit_code=result.returncode,
        stdout_sha256=_sha256(result.stdout),
        stderr_sha256=_sha256(result.stderr),
        payload=result.stdout.strip() if result.returncode == 0 else None,
        error=None if result.returncode == 0 else f"kubectl exec exited {result.returncode}",
    )


def _metadata_summary(item: Mapping[str, Any]) -> dict[str, Any]:
    metadata = item.get("metadata", {})
    return {
        "name": metadata.get("name"),
        "namespace": metadata.get("namespace"),
        "generation": metadata.get("generation"),
        "resource_version": metadata.get("resourceVersion"),
    }


def _deployment_summary(item: Mapping[str, Any]) -> dict[str, Any]:
    spec = item.get("spec", {})
    status = item.get("status", {})
    images = [
        container.get("image")
        for container in spec.get("template", {}).get("spec", {}).get("containers", [])
        if container.get("image")
    ]
    return {
        **_metadata_summary(item),
        "replicas": spec.get("replicas", 0),
        "ready_replicas": status.get("readyReplicas", 0),
        "available_replicas": status.get("availableReplicas", 0),
        "images": images,
    }


def _component_summary(item: Mapping[str, Any]) -> dict[str, Any]:
    spec = item.get("spec", {})
    metadata_names = [
        entry.get("name")
        for entry in spec.get("metadata", [])
        if entry.get("name")
    ]
    return {
        **_metadata_summary(item),
        "type": spec.get("type"),
        "version": spec.get("version"),
        "scopes": item.get("scopes", []),
        "metadata_names": metadata_names,
    }


def _configuration_summary(item: Mapping[str, Any]) -> dict[str, Any]:
    spec = item.get("spec", {})
    return {
        **_metadata_summary(item),
        "features": [
            {"name": feature.get("name"), "enabled": feature.get("enabled")}
            for feature in spec.get("features", [])
        ],
        "access_control_default": spec.get("accessControl", {}).get("defaultAction"),
        "access_control_policy_count": len(spec.get("accessControl", {}).get("policies", [])),
        "secret_scope_count": len(spec.get("secrets", {}).get("scopes", [])),
    }


def _statefulset_summary(item: Mapping[str, Any]) -> dict[str, Any]:
    spec = item.get("spec", {})
    images = [
        container.get("image")
        for container in spec.get("template", {}).get("spec", {}).get("containers", [])
        if container.get("image")
    ]
    return {
        **_metadata_summary(item),
        "replicas": spec.get("replicas"),
        "images": images,
    }


def _pod_summary(item: Mapping[str, Any]) -> dict[str, Any]:
    status = item.get("status", {})
    return {
        **_metadata_summary(item),
        "phase": status.get("phase"),
        "node": item.get("spec", {}).get("nodeName"),
        "container_images": [
            container.get("image")
            for container in status.get("containerStatuses", [])
            if container.get("image")
        ],
    }


def _items(payload: Any) -> list[Mapping[str, Any]]:
    if not isinstance(payload, Mapping):
        return []
    values = payload.get("items", [])
    return [item for item in values if isinstance(item, Mapping)]


def _write_rejection_evidence(
    path: Path,
    *,
    identity: EnvironmentIdentity,
    profile: AdapterProfile,
    reason: str,
    observations: list[CommandObservation],
    summaries: Mapping[str, Any],
) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    manifest = profile.manifest()
    timestamp = datetime.now(timezone.utc).isoformat()
    lines = [
        "# Story 27.3 C1 Adapter Profile Evidence",
        "",
        f"- captured_utc: `{timestamp}`",
        "- checkpoint: `adapter-profile`",
        "- status: `rejected`",
        f"- rejection_reason: {reason}",
        "- production_lifecycle_writes: `disabled`",
        "- evidence_is_approval: `false`",
        "",
        "## Reviewed Identity",
        "",
    ]
    for key, value in identity.to_dict().items():
        lines.append(f"- {key}: `{value}`")
    lines.extend(
        [
            "",
            "## Immutable Profile Material",
            "",
            f"- profile_sha256: `{manifest['profile_sha256']}`",
            f"- mutation_manifest_sha256: `{manifest['mutation_manifest_sha256']}`",
            "- allowed_mutations: `[]`",
            "",
            "## Safe Deployment Observations",
            "",
            "| Observation | Value |",
            "| :-- | :-- |",
        ]
    )
    for name, value in summaries.items():
        lines.append(f"| {name} | `{_canonical_json(value)}` |")
    lines.extend(
        [
            "",
            "## Read-only Child Commands",
            "",
            "| Command | Exit | Stdout SHA-256 | Stderr SHA-256 | Result |",
            "| :-- | --: | :-- | :-- | :-- |",
        ]
    )
    for observation in observations:
        result = observation.error or "ok"
        lines.append(
            "| `{} ` | {} | `{}` | `{}` | {} |".format(
                " ".join(observation.command),
                observation.exit_code,
                observation.stdout_sha256,
                observation.stderr_sha256,
                result,
            )
        )
    lines.extend(
        [
            "",
            "The packet intentionally stores hashes and structural metadata only; it does not store secret values, backend credentials, or raw pod environment data.",
            "",
        ]
    )
    path.write_text("\n".join(lines), encoding="utf-8")


def run_adapter_profile_checkpoint(
    *,
    identity: EnvironmentIdentity,
    workload_profile: str,
    steady_state_minutes: int,
    purge_backlog_records: int,
    evidence_path: Path,
) -> int:
    """Collect the C1 read-only profile and reject any unproven adapter."""

    observations = [
        _run_kubectl(identity, "get", "deployments", "-o", "json"),
        _run_kubectl(identity, "get", "components.dapr.io", "-o", "json"),
        _run_kubectl(identity, "get", "configurations.dapr.io", "-o", "json"),
        _run_kubectl(identity, "get", "statefulsets", "-o", "json"),
        _run_kubectl(
            identity,
            "get",
            "pods",
            "-l",
            "app.kubernetes.io/name=memories",
            "-o",
            "json",
        ),
    ]
    deployments = _items(observations[0].payload)
    components = _items(observations[1].payload)
    configurations = _items(observations[2].payload)
    statefulsets = _items(observations[3].payload)
    pods = _items(observations[4].payload)

    summaries = {
        "deployments": [_deployment_summary(item) for item in deployments],
        "components": [_component_summary(item) for item in components],
        "configurations": [_configuration_summary(item) for item in configurations],
        "statefulsets": [_statefulset_summary(item) for item in statefulsets],
        "pods": [_pod_summary(item) for item in pods],
    }

    if workload_profile != "adr-27.1-two-writer-500eps":
        reason = f"unsupported workload profile: {workload_profile}"
    elif steady_state_minutes != 30 or purge_backlog_records != 150000:
        reason = "C1 workload envelope does not match the mandatory 30-minute/150,000-record gate"
    elif any(observation.exit_code != 0 or observation.payload is None for observation in observations):
        reason = "deployment identity could not be captured from every required read-only Kubernetes query"
    else:
        by_name = {item.get("metadata", {}).get("name"): item for item in deployments}
        lifecycle = by_name.get("memories-access-telemetry")
        lifecycle_replicas = lifecycle.get("spec", {}).get("replicas", 0) if lifecycle else 0
        if lifecycle is None or lifecycle_replicas < 1:
            reason = "lifecycle deployment is disabled; Production writes remain fail-closed"
        else:
            store = next(
                (item for item in components if item.get("metadata", {}).get("name") == "access-telemetry-store"),
                None,
            )
            store_type = store.get("spec", {}).get("type") if store else None
            if store_type != "state.redis":
                reason = "exact Production state-store component identity is missing"
            else:
                reason = (
                    "state.redis adapter has no approved exact-profile rollback, zero-loss, "
                    "capacity, and physical-reclamation probe result"
                )

    profile = AdapterProfile(
        identity=identity.to_dict(),
        capabilities={"lifecycle_writes": False, "approval": False},
        workload=ADR_TWO_WRITER_WORKLOAD.to_dict(),
    )
    _write_rejection_evidence(
        evidence_path,
        identity=identity,
        profile=profile,
        reason=reason,
        observations=observations,
        summaries=summaries,
    )
    print(f"C1 adapter-profile: rejected ({reason})")
    print(f"Evidence: {evidence_path}")
    return 1
