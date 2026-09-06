"""Fail-closed evidence primitives for the Story 27.3 adapter profile gate."""

from __future__ import annotations

from dataclasses import dataclass
from decimal import Decimal, InvalidOperation, localcontext
import base64
import binascii
import hashlib
import json
import os
from pathlib import Path
import re
import selectors
import subprocess
import sys
import time
from datetime import datetime, timezone
from typing import Any, Callable, Mapping, Sequence


class EnvironmentIdentityError(ValueError):
    """Raised when a Production-shaped execution identity is incomplete."""


class CapacityInputError(ValueError):
    """Raised when a capacity operand cannot be normalized safely."""


# The official `ghcr.io/dapr/daprd` image places its binary at `/daprd` and ships no
# shell and no populated `PATH`, so a bare `daprd` exec fails with `executable file not
# found in $PATH`. Probing the image layout first is what lets gate C1.15's producer emit
# an observation at all; the bare name is retained for a target that does populate `PATH`.
DAPRD_EXECUTABLE_CANDIDATES: tuple[str, ...] = ("/daprd", "daprd")

# Every branch of the runtime-identity producer emits this exact key set, so a
# reader diffing two packets sees a recorded blocker rather than a vanished field.
DAPRD_IDENTITY_FIELDS: tuple[str, ...] = (
    "daprd_version",
    "daprd_version_probe_pod",
    "daprd_executable",
    "daprd_build_info",
)

# `daprd --version` answers a bare semantic version. Anything else from an exit-0
# exec - usage text, a wrapper script's banner, a shadowing binary on `PATH` - is a
# claim rather than an observation and must not reach the packet as a version.
DAPRD_VERSION_PATTERN = re.compile(r"\A\d+\.\d+\.\d+\S*\Z")


def _is_daprd_version(payload: str) -> bool:
    return bool(DAPRD_VERSION_PATTERN.match(payload))


def _observation_cause(
    observation: "CommandObservation | None",
    empty_reason: str = "no version-shaped output",
) -> str:
    """Return a stated cause, never a bare `None`.

    `_run_command` leaves `error` unset on exit 0, so an exec that succeeds with
    empty or unusable stdout would otherwise interpolate `None` into the packet as
    the reason nothing was captured - a blocker naming no cause, which defeats the
    reopen trigger it exists to serve.
    """

    if observation is None:
        return "no candidate command was executed"
    if observation.error:
        return observation.error
    # `command[0]` is always `kubectl`; the probed executable is the argument after
    # `--`, which is what a reader needs to know. Story 27.3 code review
    # (eighth-invocation review): the previous form named `kubectl` and asserted
    # "no version-shaped output" on every branch, including `--build-info`, whose
    # output is never version-shaped.
    binary = _probed_executable(observation)
    if observation.exit_code == 0:
        return f"{binary} exited 0 with {empty_reason}"
    return f"{binary} exited {observation.exit_code}"


def _probed_executable(observation: "CommandObservation") -> str:
    """Return the executable `kubectl exec ... -- <executable> <flag>` actually probed."""

    command = list(observation.command or ())
    if "--" in command:
        index = command.index("--")
        if index + 1 < len(command):
            return command[index + 1]
    return command[0] if command else "command"


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

    def to_profile_identity(self) -> dict[str, str]:
        """Return the hashed profile identity, in `canonical_pg_onprem_profile`'s key shape.

        Story 27.3 code review (eighth-invocation review) fixed two defects here.

        First, `to_dict()` carries `evidence_root`, an invocation parameter naming
        where the operator wrote the packet. Hashing it made `profile_sha256` - the
        field AC1 calls immutable profile material and AC5 treats as drift - a
        function of the operator's working directory: the same target hashed
        differently from a relative and an absolute `EVIDENCE_ROOT`, and the
        committed packet's hash moved with no runtime change at all. `evidence_root`,
        `deployment_id` and `declared_single_component_fault` remain published in the
        packet; none of them is profile identity, so none is hashed.

        Second, the runtime profile used `to_dict()`'s snake_case keys while the
        reviewed profile uses camelCase backend keys, so the two objects were
        structurally disjoint and `runtime_matches_reviewed_profile` could never be
        `true` for any runtime, however perfect - the same failure class as the
        `done`-gate verifier regex this story already fixed once. The two key shapes
        are now the same, so the comparison is answerable.

        The three backend fields are reviewed constants rather than live readings:
        this identity binds *which profile the invocation declares it is running
        against*. Whether the live cluster actually matches is proven by the C1 gate
        rows, not by this hash.
        """

        return {
            "profileId": self.profile_id,
            "kubeContext": self.kube_context,
            "kubeNamespace": self.kube_namespace,
            "postgresqlImage": EXPECTED_POSTGRESQL_IMAGE,
            "componentType": EXPECTED_COMPONENT_TYPE,
            "maxConns": EXPECTED_MAX_CONNS,
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

# The reviewed kubeconfig context name. This is a *client-side label*, not an identity
# control: anyone can name a context anything. It is recorded as an observation and is
# overridable so another operator can run the checkpoint from their own kubeconfig, while
# the gates that actually bind identity are server-side - the namespace, the pinned
# PostgreSQL image digest, and the component type/version.
DEFAULT_REVIEWED_KUBE_CONTEXT = "jpiquot@local"
EXPECTED_KUBE_CONTEXT = os.environ.get(
    "REVIEWED_KUBE_CONTEXT",
    DEFAULT_REVIEWED_KUBE_CONTEXT,
)
EXPECTED_KUBE_NAMESPACE = "hexalith-memories"
EXPECTED_QUALIFICATION_NAMESPACE = "hexalith-memories-qualification"
EXPECTED_PROFILE_ID = (
    "postgresql-v2-dapr-1.18.1-postgresql-18.4-"
    "onprem-k8s1-openebs-local-retain-400g-v1"
)
EXPECTED_POSTGRESQL_IMAGE = (
    "docker.io/library/postgres:18.4-trixie@"
    "sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a"
)
# Shared by `canonical_pg_onprem_profile` and `EnvironmentIdentity.to_profile_identity`
# so the reviewed profile and the runtime profile cannot drift apart silently.
EXPECTED_COMPONENT_TYPE = "state.postgresql/v2"
EXPECTED_MAX_CONNS = "40"


# Approved PG-ONPREM-1 capacity thresholds (sprint-change-proposal-2026-07-20).
# The threshold table governs admission; 100% profile occupancy is NOT admissible.
PROFILE_CAPACITY_BYTES = 400 * 1024**3  # 429,496,729,600
STEADY_STATE_CAPACITY_BYTES = 300_647_710_720  # 70%
CRITICAL_CAPACITY_BYTES = 343_597_383_680  # 80%
UNHEALTHY_CAPACITY_BYTES = 386_547_056_640  # 90%
# The 7-day software maximum is measured for evidence but is never admitted
# against the exact 400 GiB single-node profile.
NON_ADMISSIBLE_HORIZONS = ("7d",)
# ADR 27.1 capacity envelope operands that must not be defaulted or inferred.
# The approved profile writes one durable copy plus its WAL/snapshot copy.
APPROVED_DURABILITY_MULTIPLIER = 2
# Rewrite/vacuum workspace floor: 128 GiB, or a quarter of the live dataset if larger.
RECLAMATION_WORKSPACE_FLOOR_BYTES = 137_438_953_472


@dataclass(frozen=True)
class CapacityRequirement:
    """Checked capacity result for one retention horizon."""

    horizon: str
    records: int
    required_bytes: int


@dataclass(frozen=True)
class CapacityAdmission:
    """Admission verdict for one horizon against the approved thresholds."""

    horizon: str
    required_bytes: int
    admitted: bool
    band: str
    reason: str


def evaluate_capacity_admission(
    requirements: "list[CapacityRequirement]",
) -> "list[CapacityAdmission]":
    """Apply the approved 70/80/90% threshold table to computed capacity results.

    Admission is fail-closed: a horizon is admitted only when its measured
    requirement stays at or below the 70% steady-state threshold. Exactly 80%
    is treated as critical, not as an admissible reclamation peak. Horizons in
    NON_ADMISSIBLE_HORIZONS are never admitted against this profile.
    """

    verdicts: list[CapacityAdmission] = []
    for requirement in requirements:
        required = _normalize_integer(requirement.required_bytes, f"required_bytes[{requirement.horizon}]")
        if required == 0:
            verdicts.append(
                CapacityAdmission(
                    requirement.horizon,
                    required,
                    False,
                    "invalid",
                    "a zero capacity requirement is not evidence; measured record/index bytes must be positive",
                )
            )
            continue
        if requirement.horizon in NON_ADMISSIBLE_HORIZONS:
            verdicts.append(
                CapacityAdmission(
                    requirement.horizon,
                    required,
                    False,
                    "out-of-profile",
                    f"horizon {requirement.horizon} is measured for evidence only and is not admissible against PG-ONPREM-1",
                )
            )
            continue
        if required > UNHEALTHY_CAPACITY_BYTES:
            band = "unhealthy"
        elif required >= CRITICAL_CAPACITY_BYTES:
            band = "critical"
        elif required > STEADY_STATE_CAPACITY_BYTES:
            band = "above-steady-state"
        else:
            band = "steady-state"
        admitted = band == "steady-state"
        reason = (
            f"{required} bytes is within the 70% steady-state threshold "
            f"({STEADY_STATE_CAPACITY_BYTES})"
            if admitted
            else (
                f"{required} bytes exceeds the 70% steady-state threshold "
                f"({STEADY_STATE_CAPACITY_BYTES}); band={band}"
            )
        )
        verdicts.append(CapacityAdmission(requirement.horizon, required, admitted, band, reason))
    return verdicts


def canonical_pg_onprem_profile() -> AdapterProfile:
    """The exact reviewed PG-ONPREM-1 profile whose hash approvals bind to."""

    return AdapterProfile(
        identity={
            "profileId": EXPECTED_PROFILE_ID,
            "kubeContext": EXPECTED_KUBE_CONTEXT,
            "kubeNamespace": EXPECTED_KUBE_NAMESPACE,
            "postgresqlImage": EXPECTED_POSTGRESQL_IMAGE,
            "componentType": EXPECTED_COMPONENT_TYPE,
            "maxConns": EXPECTED_MAX_CONNS,
        },
        capabilities={
            "actorStateStore": True,
            "strongReads": True,
            "transactionRollback": True,
            "ttl": True,
        },
        workload=ADR_TWO_WRITER_WORKLOAD.to_dict(),
    )


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
# The unit is mandatory: Task 1 requires rejecting missing units, and a bare `400` meaning
# 400 GB read as 400 bytes is exactly the silent under-provisioning the gate exists to stop.
_BYTE_VALUE = re.compile(r"^\s*([0-9]+(?:\.[0-9]+)?)\s*([A-Za-z]+)\s*$")
_INT64_MAX = 2**63 - 1
# Enough precision that a 19-digit operand is compared exactly rather than rounded to an
# integral value by the default 28-digit context, which would bypass fractional rejection.
_DECIMAL_PRECISION = 80


def _normalize_integer(value: Any, name: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int) or value < 0 or value > _INT64_MAX:
        raise CapacityInputError(f"{name} must be a non-negative signed 64-bit integer")
    return value


def _normalize_bytes(value: Any, name: str) -> tuple[int, str]:
    """Normalize one byte operand, rejecting any value without an explicit unit.

    A bare integer is rejected on purpose. Accepting it as bytes made `400` (meaning
    400 GB) normalize to 400 bytes, and it also produced a `"none"` unit system that
    excluded the operand from the decimal/binary mixing check.
    """

    if isinstance(value, bool) or isinstance(value, int) or isinstance(value, float):
        raise CapacityInputError(
            f"{name} must be a string carrying an explicit byte unit (for example '400GiB'), not a bare number"
        )
    if not isinstance(value, str):
        raise CapacityInputError(f"{name} must include an explicit byte unit")

    match = _BYTE_VALUE.fullmatch(value)
    if match is None:
        raise CapacityInputError(
            f"{name} is not a finite byte value with an explicit unit"
        )
    unit = match.group(2).upper()
    if unit not in _BYTE_UNITS:
        raise CapacityInputError(f"{name} uses an unknown byte unit: {unit}")
    with localcontext() as context:
        context.prec = _DECIMAL_PRECISION
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
    scheduler_bytes: Any,
    host_filesystem_headroom_bytes: Any,
    horizons: Mapping[str, Any] | None = None,
) -> list[CapacityRequirement]:
    """Calculate checked retention capacity without accepting ambiguous units.

    Every ADR-mandated operand is required, none is defaulted, and the reclamation
    workspace is floored at `max(RECLAMATION_WORKSPACE_FLOOR_BYTES, ceil(base / 4))`.
    The PVC request does not reserve local storage, so the host filesystem headroom
    the operator measured is a required operand rather than an assumption.
    """

    record_count = _normalize_integer(records, "records")
    multiplier = _normalize_integer(durability_multiplier, "durability_multiplier")
    if multiplier != APPROVED_DURABILITY_MULTIPLIER:
        raise CapacityInputError(
            "durability_multiplier must equal the approved PG-ONPREM-1 value "
            f"{APPROVED_DURABILITY_MULTIPLIER}; got {multiplier}"
        )

    record_bytes, record_system = _normalize_bytes(measured_record_bytes, "measured_record_bytes")
    index_bytes, index_system = _normalize_bytes(measured_index_bytes, "measured_index_bytes")
    control, control_system = _normalize_bytes(control_bytes, "control_bytes")
    workspace, workspace_system = _normalize_bytes(reclamation_workspace, "reclamation_workspace")
    scheduler, scheduler_system = _normalize_bytes(scheduler_bytes, "scheduler_bytes")
    headroom, headroom_system = _normalize_bytes(
        host_filesystem_headroom_bytes, "host_filesystem_headroom_bytes"
    )

    # A zero per-record measurement makes `required_bytes` collapse to the constant terms
    # and produces a "proof" that any horizon fits. Measured bytes must be positive.
    if record_bytes == 0:
        raise CapacityInputError("measured_record_bytes must be greater than zero")
    if index_bytes == 0:
        raise CapacityInputError("measured_index_bytes must be greater than zero")

    systems = {
        record_system,
        index_system,
        control_system,
        workspace_system,
        scheduler_system,
        headroom_system,
    } - {"none"}
    if len(systems) > 1:
        raise CapacityInputError("decimal and binary byte units cannot be mixed")

    # `horizons or {...}` silently substituted the three defaults for an explicitly empty
    # mapping, turning "measure nothing" into "measure everything".
    horizon_records = (
        {"1h": record_count, "24h": record_count, "7d": record_count}
        if horizons is None
        else dict(horizons)
    )
    if not horizon_records:
        raise CapacityInputError("at least one retention horizon must be supplied")

    results = []
    for horizon, count in horizon_records.items():
        normalized_count = _normalize_integer(count, f"records[{horizon}]")
        try:
            base = normalized_count * (record_bytes + index_bytes) * multiplier
            # ADR reclamation floor: rewrite/vacuum needs the larger of the fixed floor and
            # a quarter of the live dataset, whichever the operator's own figure exceeds.
            effective_workspace = max(
                workspace,
                RECLAMATION_WORKSPACE_FLOOR_BYTES,
                -(-base // 4),
            )
            required = base + control + effective_workspace + scheduler + headroom
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
    started_utc: str = ""
    finished_utc: str = ""
    stderr_excerpt: str = ""


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


_SECRET_LIKE = re.compile(
    r"(?i)(password|passwd|token|secret|apikey|api[-_]key|authorization|bearer|private[-_]key)"
)


def _redact(text: str, limit: int = 400) -> str:
    """Return a single-line, length-bounded excerpt with secret-like lines dropped."""

    kept = [
        line.strip()
        for line in text.splitlines()
        if line.strip() and not _SECRET_LIKE.search(line)
    ]
    excerpt = " / ".join(kept)
    excerpt = excerpt.replace("`", "'").replace("|", "\\|")
    return excerpt[:limit]


def _run_command(command: tuple[str, ...], *, parse_json: bool) -> CommandObservation:
    """Run one read-only command, never letting an execution failure escape.

    A missing binary or an unresponsive API server is precisely the fail-closed
    situation the evidence packet exists to record, so it must become an observation
    rather than a traceback that leaves no packet behind.
    """

    started = _utc_now()
    try:
        result = subprocess.run(
            command,
            check=False,
            capture_output=True,
            text=True,
            errors="replace",
            timeout=60,
        )
    except FileNotFoundError as exc:
        return CommandObservation(
            command=command,
            exit_code=127,
            stdout_sha256=_sha256(""),
            stderr_sha256=_sha256(str(exc)),
            payload=None,
            error=f"command not found: {command[0]}",
            started_utc=started,
            finished_utc=_utc_now(),
            stderr_excerpt=_redact(str(exc)),
        )
    except subprocess.TimeoutExpired:
        return CommandObservation(
            command=command,
            exit_code=124,
            stdout_sha256=_sha256(""),
            stderr_sha256=_sha256(""),
            payload=None,
            error="command timed out after 60s",
            started_utc=started,
            finished_utc=_utc_now(),
            stderr_excerpt="timeout",
        )
    except OSError as exc:
        return CommandObservation(
            command=command,
            exit_code=126,
            stdout_sha256=_sha256(""),
            stderr_sha256=_sha256(str(exc)),
            payload=None,
            error=f"command could not be executed: {exc.__class__.__name__}",
            started_utc=started,
            finished_utc=_utc_now(),
            stderr_excerpt=_redact(str(exc)),
        )

    payload = None
    error = None
    if result.returncode != 0:
        error = f"{command[0]} exited {result.returncode}"
    elif parse_json:
        try:
            payload = json.loads(result.stdout)
        except json.JSONDecodeError as exc:
            error = f"{command[0]} returned non-JSON output: {exc.msg}"
    else:
        payload = result.stdout.strip()

    return CommandObservation(
        command=command,
        exit_code=result.returncode,
        stdout_sha256=_sha256(result.stdout),
        stderr_sha256=_sha256(result.stderr),
        payload=payload,
        error=error,
        started_utc=started,
        finished_utc=_utc_now(),
        stderr_excerpt=_redact(result.stderr),
    )


def _run_kubectl(identity: EnvironmentIdentity, *arguments: str) -> CommandObservation:
    return _run_command(
        (
            "kubectl",
            "--context",
            identity.kube_context,
            "--namespace",
            identity.kube_namespace,
            *arguments,
        ),
        parse_json=True,
    )


def _run_daprd(
    identity: EnvironmentIdentity, pod_name: str, executable: str, flag: str
) -> CommandObservation:
    return _run_command(
        (
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
            executable,
            flag,
        ),
        parse_json=False,
    )


def collect_sidecar_image_identity(
    pod_summaries: Sequence[Mapping[str, Any]],
) -> tuple[list[str], list[str] | str]:
    """Return the observed daprd sidecar tags and digests for gate C1.15.

    The digest half fails closed. Kubelet reports a bare `sha256:<id>` imageID
    whenever the running image carries no repository digest - a locally built or
    side-loaded image, as `kind load docker-image` produces - and this cluster is
    already observed emitting that form on the app container. A repo-name substring
    filter drops those, and an empty list published as a positive observation is
    indistinguishable from "no daprd container was observed", in the very field AC1
    requires. When a daprd tag was seen but no digest could be bound, record the
    blocker instead.

    Story 27.3 code review (eighth-invocation review): the repo-name filter was the
    *only* way the sidecar was identified, so when the daprd container's own `image`
    was a bare `sha256:<id>` - precisely the side-loaded case named above - the tag
    set came back empty, the `images and not digests` guard could not fire, and
    `([], [])` was published as a positive observation. The container name is now
    authoritative and the repo-name substring is a fallback for summaries predating
    `container_names`. A pod set that was observed but yielded no daprd container at
    all records a blocker rather than an empty list.
    """

    def _is_daprd(position: int, summary: Mapping[str, Any]) -> bool:
        container_names = summary.get("container_names", [])
        if position < len(container_names) and container_names[position]:
            return "daprd" in container_names[position]
        # Pre-`container_names` summaries: fall back to the repo-name substring on
        # either parallel field.
        images = summary.get("container_images", [])
        identifiers = summary.get("container_image_ids", [])
        image = images[position] if position < len(images) else ""
        image_id = identifiers[position] if position < len(identifiers) else ""
        return "daprd" in image or "daprd" in image_id

    tags: set[str] = set()
    digests: set[str] = set()
    observed_daprd_container = False
    for summary in pod_summaries:
        identifiers = summary.get("container_image_ids", [])
        images = summary.get("container_images", [])
        for position in range(max(len(identifiers), len(images))):
            if not _is_daprd(position, summary):
                continue
            observed_daprd_container = True
            if position < len(images) and images[position]:
                tags.add(images[position])
            if position < len(identifiers) and identifiers[position]:
                digests.add(identifiers[position])

    if pod_summaries and not observed_daprd_container:
        return (
            sorted(tags),
            "not captured; no observed pod carried a daprd container",
        )
    if observed_daprd_container and not digests:
        return (
            sorted(tags),
            "not captured; no daprd containerStatus carried an imageID",
        )
    return (sorted(tags), sorted(digests))


def _digest_uniformity(sidecar_digests: list[str] | str) -> bool | str:
    """Return uniformity as a claim only when digests were actually observed.

    `True`/`False` are claims about the fleet; anything else is a stated blocker, so
    an empty observation can never be read as "the fleet is not uniform".
    """

    if not isinstance(sidecar_digests, list):
        # The collector already returned its own stated blocker; carry it through
        # rather than inventing a second, weaker one.
        return sidecar_digests
    if not sidecar_digests:
        return "not captured; no daprd image digest was observed"
    return len(sidecar_digests) == 1


def collect_daprd_runtime_identity(
    running_pods: Sequence[str],
    probe: Callable[[str, str, str], CommandObservation],
) -> tuple[dict[str, Any], list[CommandObservation]]:
    """Capture the running Dapr runtime identity for gate C1.15.

    `probe(pod_name, executable, flag)` runs one read-only `daprd` invocation.
    Both the executable path and the pod are probed as candidate lists: the
    official `ghcr.io/dapr/daprd` image carries no shell and no populated
    `PATH`, so a bare `daprd` exec fails with `executable file not found in
    $PATH` (surfaced by `kubectl exec` as exit 1) while `/daprd` answers - and
    a Running pod without the sidecar container must not hide a version a later
    Running pod could report. Every probe becomes a recorded observation, and a
    target that answers nowhere records the blocker rather than a claim.
    """

    def blocked(reason: str, **overrides: str) -> dict[str, str]:
        # Every branch emits the same key set. A field that silently disappears
        # is indistinguishable from a field that was never probed, which is the
        # opposite of what a fail-closed packet is for; `collect_attestations`
        # and `collect_capacity_evidence` take the same approach.
        fields = {key: f"not captured; {reason}" for key in DAPRD_IDENTITY_FIELDS}
        fields.update(overrides)
        return fields

    observations: list[CommandObservation] = []
    if not running_pods:
        return (blocked("no Running pod matched the label selector"), observations)

    fallback: CommandObservation | None = None
    fallback_pod: str | None = None
    for pod_name in running_pods:
        for executable in DAPRD_EXECUTABLE_CANDIDATES:
            version = probe(pod_name, executable, "--version")
            observations.append(version)
            payload = str(version.payload or "").strip()
            if not _is_daprd_version(payload):
                # An exit-0 answer that is not version-shaped is a claim, not an
                # observation: a wrapper script, usage text printed with exit 0, or a
                # binary shadowing the bare-name candidate would otherwise be stamped
                # into the packet as the running runtime version.
                #
                # Story 27.3 code review (eighth-invocation review): `fallback or
                # version` pinned the cause to the *first* attempt, and `/daprd`
                # always precedes bare `daprd` with a plain exit 1 when the path is
                # absent - so the shadowing-binary case this check exists to catch
                # could never reach the packet. An exit-0 non-version answer is the
                # more informative cause and now wins.
                if fallback is None or (fallback.exit_code != 0 and version.exit_code == 0):
                    fallback = version
                    fallback_pod = pod_name
                continue
            build_info = probe(pod_name, executable, "--build-info")
            observations.append(build_info)
            build_payload = str(build_info.payload or "").strip()
            return (
                {
                    "daprd_version": payload,
                    "daprd_version_probe_pod": pod_name,
                    "daprd_executable": executable,
                    # `--build-info` output is free-form, so it cannot be shape-checked
                    # the way the version is; it is bounded instead. An exit-0 answer
                    # that looks like usage text is a claim, not an observation - the
                    # same defect `_is_daprd_version` was added to close for the
                    # version, one field over.
                    "daprd_build_info": _bounded_build_info(build_payload, build_info),
                },
                observations,
            )

    return (
        blocked(
            _observation_cause(fallback),
            daprd_version_probe_pod=fallback_pod or running_pods[0],
            daprd_executable="not captured; no candidate path returned a version-shaped answer",
        ),
        observations,
    )


# `--build-info` prints a short provenance block. Usage text - which is what a
# shadowing binary or a flag-rejecting build emits on exit 0 - always announces
# itself with one of these.
_BUILD_INFO_USAGE_MARKERS = ("usage of", "usage:", "unknown flag", "flag provided but not defined")


def _bounded_build_info(payload: str, observation: "CommandObservation") -> str:
    """Return the build-info payload, or a stated blocker when it is not build info."""

    if not payload:
        return f"not captured; {_observation_cause(observation, 'no output')}"
    lowered = payload.lower()
    if any(marker in lowered for marker in _BUILD_INFO_USAGE_MARKERS):
        return (
            "not captured; "
            f"{_probed_executable(observation)} exited 0 with usage text rather than build info"
        )
    return payload


def _mapping(value: Any) -> Mapping[str, Any]:
    """Return `value` when it is a mapping, otherwise an empty one.

    A Kubernetes object can legitimately carry `spec: null`, and a partially applied or
    hand-edited object can carry a scalar where a mapping is expected. Both used to raise
    AttributeError inside these helpers, escaping before any evidence packet was written -
    which is exactly the fail-closed situation the packet exists to record.
    """

    return value if isinstance(value, Mapping) else {}


def _sequence(value: Any) -> list[Any]:
    return list(value) if isinstance(value, (list, tuple)) else []


def _container_images(pod_spec: Any) -> list[str]:
    return [
        image
        for container in _sequence(_mapping(pod_spec).get("containers"))
        if (image := _mapping(container).get("image"))
    ]


def _metadata_summary(item: Mapping[str, Any]) -> dict[str, Any]:
    metadata = _mapping(item.get("metadata"))
    return {
        "name": metadata.get("name"),
        "namespace": metadata.get("namespace"),
        "generation": metadata.get("generation"),
        "resource_version": metadata.get("resourceVersion"),
    }


def _deployment_summary(item: Mapping[str, Any]) -> dict[str, Any]:
    spec = _mapping(item.get("spec"))
    status = _mapping(item.get("status"))
    return {
        **_metadata_summary(item),
        "replicas": spec.get("replicas", 0),
        "ready_replicas": status.get("readyReplicas", 0),
        "available_replicas": status.get("availableReplicas", 0),
        "images": _container_images(_mapping(spec.get("template")).get("spec")),
    }


def _component_summary(item: Mapping[str, Any]) -> dict[str, Any]:
    spec = _mapping(item.get("spec"))
    metadata_names = [
        name
        for entry in _sequence(spec.get("metadata"))
        if (name := _mapping(entry).get("name"))
    ]
    return {
        **_metadata_summary(item),
        "type": spec.get("type"),
        "version": spec.get("version"),
        "scopes": _sequence(item.get("scopes")),
        "metadata_names": metadata_names,
    }


def _component_metadata_value(item: Mapping[str, Any] | None, name: str) -> str | None:
    """Read one non-secret component setting without publishing other metadata values."""

    spec = _mapping(_mapping(item).get("spec"))
    for entry in _sequence(spec.get("metadata")):
        metadata = _mapping(entry)
        if metadata.get("name") == name:
            value = metadata.get("value")
            return value if isinstance(value, str) else None
    return None


def _configuration_summary(item: Mapping[str, Any]) -> dict[str, Any]:
    spec = _mapping(item.get("spec"))
    access_control = _mapping(spec.get("accessControl"))
    return {
        **_metadata_summary(item),
        "features": [
            {"name": _mapping(feature).get("name"), "enabled": _mapping(feature).get("enabled")}
            for feature in _sequence(spec.get("features"))
        ],
        "access_control_default": access_control.get("defaultAction"),
        "access_control_policy_count": len(_sequence(access_control.get("policies"))),
        "secret_scope_count": len(_sequence(_mapping(spec.get("secrets")).get("scopes"))),
    }


def _statefulset_summary(item: Mapping[str, Any]) -> dict[str, Any]:
    spec = _mapping(item.get("spec"))
    status = _mapping(item.get("status"))
    return {
        **_metadata_summary(item),
        "replicas": spec.get("replicas"),
        "ready_replicas": status.get("readyReplicas", 0),
        "images": _container_images(_mapping(spec.get("template")).get("spec")),
    }


def _pod_summary(item: Mapping[str, Any]) -> dict[str, Any]:
    status = _mapping(item.get("status"))
    return {
        **_metadata_summary(item),
        "phase": status.get("phase"),
        "node": _mapping(item.get("spec")).get("nodeName"),
        # `image` carries the mutable tag; only `imageID` carries the digest of the bytes
        # actually running, which is what AC1 and gate C1.15 require. The two lists are
        # published side by side, so they are built from the same iteration and hold
        # positional parity: a Waiting container (ContainerCreating, ImagePullBackOff)
        # has an empty `imageID`, and dropping it would silently rebind every later
        # digest to the wrong container.
        # The container name is the only identifier that survives a digest-pinned or
        # side-loaded image, whose `image` and `imageID` are both a bare `sha256:<id>`.
        # Without it the sidecar can only be found by repo-name substring, which is
        # what let `collect_sidecar_image_identity` fail open. Same iteration, so this
        # list holds positional parity with the two below.
        "container_names": [
            _mapping(container).get("name") or ""
            for container in _sequence(status.get("containerStatuses"))
        ],
        "container_images": [
            _mapping(container).get("image") or ""
            for container in _sequence(status.get("containerStatuses"))
        ],
        "container_image_ids": [
            _mapping(container).get("imageID") or ""
            for container in _sequence(status.get("containerStatuses"))
        ],
    }


def _items(payload: Any) -> list[Mapping[str, Any]]:
    if not isinstance(payload, Mapping):
        return []
    values = payload.get("items", [])
    return [item for item in values if isinstance(item, Mapping)]


# Capacity operands are supplied by the operator from measured values; none is defaulted.
_CAPACITY_ENV = {
    "measured_record_bytes": "CAPACITY_MEASURED_RECORD_BYTES",
    "measured_index_bytes": "CAPACITY_MEASURED_INDEX_BYTES",
    "control_bytes": "CAPACITY_CONTROL_BYTES",
    "reclamation_workspace": "CAPACITY_RECLAMATION_WORKSPACE",
    "scheduler_bytes": "CAPACITY_SCHEDULER_BYTES",
    "host_filesystem_headroom_bytes": "CAPACITY_HOST_FILESYSTEM_HEADROOM_BYTES",
}
_CAPACITY_HORIZON_ENV = {
    "1h": "CAPACITY_RECORDS_1H",
    "24h": "CAPACITY_RECORDS_24H",
    "7d": "CAPACITY_RECORDS_7D",
}


def collect_capacity_evidence(
    environment: Mapping[str, str] | None = None,
) -> dict[str, Any]:
    """Produce gate C1.13's capacity observation, or state exactly why it cannot run.

    This wires `calculate_capacity` and `evaluate_capacity_admission` into the evidence
    path. Before this, both were defined and never called from any evidence producer, so
    C1.13 had no producer at all while the `adapter-profile` command was cited as its source.
    """

    values = os.environ if environment is None else environment
    missing = sorted(
        name
        for name in (*_CAPACITY_ENV.values(), *_CAPACITY_HORIZON_ENV.values())
        if not str(values.get(name, "")).strip()
    )
    if missing:
        return {
            "status": "blocked",
            "reason": "measured capacity operands were not supplied",
            "missing_operands": missing,
            "admissions": [],
        }

    try:
        horizons = {
            horizon: int(str(values[name]).strip())
            for horizon, name in _CAPACITY_HORIZON_ENV.items()
        }
    except ValueError as exc:
        return {
            "status": "rejected",
            "reason": f"retention horizon record counts must be integers: {exc}",
            "missing_operands": [],
            "admissions": [],
        }

    try:
        requirements = calculate_capacity(
            records=horizons["24h"],
            durability_multiplier=APPROVED_DURABILITY_MULTIPLIER,
            horizons=horizons,
            **{
                argument: str(values[name]).strip()
                for argument, name in _CAPACITY_ENV.items()
            },
        )
    except CapacityInputError as exc:
        return {
            "status": "rejected",
            "reason": f"capacity inputs were rejected: {exc}",
            "missing_operands": [],
            "admissions": [],
        }

    admissions = evaluate_capacity_admission(requirements)
    return {
        "status": "measured",
        "reason": "",
        "missing_operands": [],
        "profile_capacity_bytes": PROFILE_CAPACITY_BYTES,
        "steady_state_threshold_bytes": STEADY_STATE_CAPACITY_BYTES,
        "critical_threshold_bytes": CRITICAL_CAPACITY_BYTES,
        "unhealthy_threshold_bytes": UNHEALTHY_CAPACITY_BYTES,
        "durability_multiplier": APPROVED_DURABILITY_MULTIPLIER,
        "reclamation_workspace_floor_bytes": RECLAMATION_WORKSPACE_FLOOR_BYTES,
        "admissions": [
            {
                "horizon": verdict.horizon,
                "required_bytes": verdict.required_bytes,
                "admitted": verdict.admitted,
                "band": verdict.band,
                "reason": verdict.reason,
            }
            for verdict in admissions
        ],
    }


# AC3/AC4 fields the packet must be able to represent. They stay `unrecorded` until the
# operator supplies them; a rejection packet that cannot even hold them can never show
# what is missing.
_ATTESTATION_ENV = {
    "backup_destination": "BACKUP_DESTINATION",
    "restore_result": "RESTORE_RESULT",
    "rpo": "PUBLISHED_RPO",
    "rto": "PUBLISHED_RTO",
    "out_of_profile_statement": "OUT_OF_PROFILE_STATEMENT",
    "platform_operations_approver": "PLATFORM_OPERATIONS_APPROVER",
    "security_reviewer_approver": "SECURITY_REVIEWER_APPROVER",
}


def collect_attestations(environment: Mapping[str, str] | None = None) -> dict[str, str]:
    values = os.environ if environment is None else environment
    return {
        field: (str(values.get(name, "")).strip() or "unrecorded")
        for field, name in _ATTESTATION_ENV.items()
    }


def _reviewed_source_hashes() -> dict[str, str]:
    """Hash the reviewed verifier sources so the packet binds the code that produced it."""

    tools_dir = Path(__file__).resolve().parent
    hashes: dict[str, str] = {}
    for name in ("verify_access_telemetry_lifecycle.py", "verify-access-telemetry-lifecycle.py"):
        candidate = tools_dir / name
        try:
            hashes[name] = hashlib.sha256(candidate.read_bytes()).hexdigest()
        except OSError:
            hashes[name] = "unreadable"
    return hashes


def _write_rejection_evidence(
    path: Path,
    *,
    identity: EnvironmentIdentity,
    profile: AdapterProfile,
    reason: str,
    observations: list[CommandObservation],
    summaries: Mapping[str, Any],
    capacity: Mapping[str, Any] | None = None,
    attestations: Mapping[str, str] | None = None,
    workload_identity_before: Mapping[str, Any] | None = None,
    workload_identity_after: Mapping[str, Any] | None = None,
    runtime_identity: Mapping[str, Any] | None = None,
) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    manifest = profile.manifest()
    timestamp = datetime.now(timezone.utc).isoformat()
    reviewed_manifest = canonical_pg_onprem_profile().manifest()
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
            # Story 27.3 code review (chunk 3b): the packet published a bare
            # profile hash with no statement of what object it covered, so an
            # approval could bind the reviewed manifests or the divergent live
            # cluster and a reader could not tell which.
            # Story 27.3 code review (eighth-invocation review): this claimed the hash
            # covered "the live component query". No component data enters the hash -
            # it covers the declared profile identity, the reviewed backend constants
            # and the ADR workload envelope, and nothing else. `evidence_root`,
            # `deployment_id` and `declared_single_component_fault` are published in
            # this packet but are deliberately outside the hash.
            "- profile_hash_covers: `declared profile identity (profileId, kubeContext, kubeNamespace), the reviewed backend constants (postgresqlImage, componentType, maxConns), the C1 capability set, and the ADR two-writer workload envelope; no live query result and no invocation parameter`",
            f"- reviewed_canonical_profile_sha256: `{reviewed_manifest['profile_sha256']}`",
            "- reviewed_canonical_profile_source: `tools/verify_access_telemetry_lifecycle.py::canonical_pg_onprem_profile`",
            f"- runtime_matches_reviewed_profile: `{str(manifest['profile_sha256'] == reviewed_manifest['profile_sha256']).lower()}`",
            "",
            "## Reviewed Source Hashes",
            "",
        ]
    )
    for name, digest in _reviewed_source_hashes().items():
        lines.append(f"- `tools/{name}`: `{digest}`")

    lines.extend(["", "## Workload Identity", ""])
    lines.append(f"- pre_run: `{_canonical_json(workload_identity_before or {})}`")
    lines.append(f"- post_run: `{_canonical_json(workload_identity_after or {})}`")
    if not workload_identity_before and not workload_identity_after:
        # Both observations empty is not evidence of an unchanged workload; it means
        # neither query observed anything, so "unchanged" would be vacuously true.
        lines.append("- unchanged_during_run: `not observed`")
    else:
        lines.append(
            "- unchanged_during_run: "
            f"`{str((workload_identity_before or {}) == (workload_identity_after or {})).lower()}`"
        )

    lines.extend(["", "## Runtime and Control-Plane Identity (AC1)", ""])
    for key, value in (runtime_identity or {"status": "unrecorded"}).items():
        lines.append(f"- {key}: `{_canonical_json(value)}`")

    lines.extend(["", "## Capacity (gate C1.13)", ""])
    capacity_evidence = dict(capacity or {"status": "unrecorded", "admissions": []})
    admissions = capacity_evidence.pop("admissions", [])
    for key, value in capacity_evidence.items():
        lines.append(f"- {key}: `{_canonical_json(value)}`")
    if admissions:
        lines.extend(
            [
                "",
                "| Horizon | Required bytes | Band | Admitted | Reason |",
                "| :-- | --: | :-- | :-- | :-- |",
            ]
        )
        for verdict in admissions:
            lines.append(
                "| {} | {} | {} | {} | {} |".format(
                    verdict["horizon"],
                    verdict["required_bytes"],
                    verdict["band"],
                    str(verdict["admitted"]).lower(),
                    verdict["reason"],
                )
            )

    lines.extend(["", "## Durability, Recovery and Approval Attestations (AC3, AC4)", ""])
    for key, value in (attestations or collect_attestations()).items():
        lines.append(f"- {key}: `{value}`")

    lines.extend(
        [
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
            "| Command | Started (UTC) | Finished (UTC) | Exit | Stdout SHA-256 | Stderr SHA-256 | Stderr excerpt | Result |",
            "| :-- | :-- | :-- | --: | :-- | :-- | :-- | :-- |",
        ]
    )
    for observation in observations:
        result = observation.error or "ok"
        lines.append(
            "| `{} ` | `{}` | `{}` | {} | `{}` | `{}` | {} | {} |".format(
                " ".join(observation.command),
                observation.started_utc,
                observation.finished_utc,
                observation.exit_code,
                observation.stdout_sha256,
                observation.stderr_sha256,
                observation.stderr_excerpt or "-",
                result,
            )
        )
    lines.extend(
        [
            "",
            "The packet stores hashes, structural metadata, and redacted stderr excerpts only; it does not store secret values, backend credentials, or raw pod environment data.",
            "",
        ]
    )
    # Story 27.3 code review (chunk 3b).
    # P32: `.gitattributes` declares `* text=auto eol=crlf`, so a packet written
    # with LF produces working-tree bytes - and any hash over them - that differ
    # from a fresh checkout. Write CRLF explicitly.
    # P25: the single fixed path was rewritten on every invocation, so the
    # rejection history AC5 depends on was destroyed by the run that would claim
    # approval, and an approval was indistinguishable from a rejection by path.
    # Every run now also lands an immutable per-run copy beside it.
    body = "\r\n".join(lines) + "\r\n"
    path.write_text(body, encoding="utf-8", newline="")
    run_id = _sha256(body)[:16]
    history = path.parent / "adapter-profile-runs" / f"{path.stem}-{run_id}.md"
    history.parent.mkdir(parents=True, exist_ok=True)
    if not history.exists():
        history.write_text(body, encoding="utf-8", newline="")


def _observation_utc_milliseconds(value: str, fallback: int) -> int:
    if not value:
        return fallback
    try:
        return int(datetime.fromisoformat(value.replace("Z", "+00:00")).timestamp() * 1000)
    except ValueError as exc:
        raise EvidenceValidationError("adapter-profile command timestamp is invalid") from exc


def _write_c0_adapter_profile_pass(
    *,
    adapter_path: Path,
    wrapper_path: Path,
    repository_root: Path,
    evidence_root: Path,
    identity: EnvironmentIdentity,
    summaries: Mapping[str, Any],
    runtime_identity: Mapping[str, Any],
    workload_identity_before: Mapping[str, Any],
    workload_identity_after: Mapping[str, Any],
    observations: Sequence[CommandObservation],
    started_utc_ms: int,
) -> None:
    """Write the immutable source-bound adapter packet and its C0 chain wrapper."""

    approved_root = _validated_evidence_root(evidence_root, repository_root)
    adapter_path = _require_evidence_path(
        adapter_path, approved_root, "adapter-profile output", must_exist=False
    )
    wrapper_path = _require_evidence_path(wrapper_path, approved_root, "C0 wrapper output", must_exist=False)
    source_commit = _git_checked(repository_root, "rev-parse", "--verify", "HEAD^{commit}")
    source_paths = tuple(sorted(_expected_source_paths("adapter-profile")))
    source_hashes: dict[str, str] = {}
    for relative in source_paths:
        digest = _hash_git_blob(repository_root, source_commit, relative)
        if hashlib.sha256((repository_root / relative).read_bytes()).hexdigest() != digest:
            raise EvidenceValidationError(f"C0 producer worktree bytes differ from source HEAD: {relative}")
        source_hashes[relative] = digest

    runtime_path = adapter_path.with_name(f"{adapter_path.stem}-runtime-observation.json")
    runtime_path = _require_evidence_path(
        runtime_path, approved_root, "adapter-profile runtime observation", must_exist=False
    )
    # The read-only summaries are already value-redacted, but the historical
    # Markdown packet includes a field whose *name* contains ``secret``.  JSON
    # close-out artifacts deliberately reject even secret-shaped aliases, so
    # omit that non-essential count from the immutable runtime observation.
    evidence_summaries = json.loads(_canonical_json(summaries))
    for configuration in evidence_summaries.get("configurations", []):
        if isinstance(configuration, dict):
            configuration.pop("secret_scope_count", None)
    runtime_artifact = {
        "schema_version": 1,
        "profile_sha256": STORY_27_4_PROFILE_SHA256,
        "workload_sha256": STORY_27_4_WORKLOAD_SHA256,
        "deployment_id_sha256": _sha256(identity.deployment_id),
        "summaries": evidence_summaries,
        "runtime_identity": runtime_identity,
        "workload_identity_before": workload_identity_before,
        "workload_identity_after": workload_identity_after,
    }
    _validate_secret_safe(runtime_artifact, "C0 runtime observation")
    _write_json_exclusive(runtime_path, runtime_artifact)
    runtime_sha256 = hashlib.sha256(runtime_path.read_bytes()).hexdigest()

    finished_utc_ms = _utc_now_milliseconds()
    producer_arguments = {
        "deployment_id_sha256": _sha256(identity.deployment_id),
        "target_sha256": _sha256(
            _canonical_json(
                {
                    "kube_context": identity.kube_context,
                    "namespace": identity.kube_namespace,
                    "profile_id": identity.profile_id,
                }
            )
        ),
        "workload_sha256": STORY_27_4_WORKLOAD_SHA256,
    }
    producer_path = STORY_27_4_PRODUCERS["adapter-profile"][1]
    producer_command = {
        "command_id": STORY_27_4_PRODUCERS["adapter-profile"][0],
        "arguments": producer_arguments,
        "arguments_sha256": _sha256(_canonical_json(producer_arguments)),
        "started_utc_ms": started_utc_ms,
        "finished_utc_ms": finished_utc_ms,
        "exit_code": 0,
        "stdout_sha256": runtime_sha256,
        "stderr_sha256": _sha256(""),
        "result_count": 1,
    }
    commands = [producer_command]
    for index, observation in enumerate(observations, 1):
        command_id = f"adapter-read-{index:02d}"
        arguments = {"operation": command_id}
        commands.append(
            {
                "command_id": command_id,
                "arguments": arguments,
                "arguments_sha256": _sha256(_canonical_json(arguments)),
                "started_utc_ms": _observation_utc_milliseconds(
                    observation.started_utc, started_utc_ms
                ),
                "finished_utc_ms": _observation_utc_milliseconds(
                    observation.finished_utc, finished_utc_ms
                ),
                "exit_code": observation.exit_code,
                "stdout_sha256": observation.stdout_sha256,
                "stderr_sha256": observation.stderr_sha256,
                "result_count": 1,
            }
        )
    common = {
        "schema_version": 1,
        "profile_sha256": STORY_27_4_PROFILE_SHA256,
        "workload_sha256": STORY_27_4_WORKLOAD_SHA256,
        "source_commit": source_commit,
        "source_hashes": source_hashes,
        "owner": "hexalith-platform-operations",
        "started_utc": started_utc_ms,
        "finished_utc": finished_utc_ms,
        "failure_count": 0,
        "skip_count": 0,
        "failures": [],
        "skipped": [],
        "commands": commands,
        "producer": {
            "command_id": STORY_27_4_PRODUCERS["adapter-profile"][0],
            "path": producer_path,
            "source_sha256": source_hashes[producer_path],
            "arguments": producer_arguments,
            "arguments_sha256": _sha256(_canonical_json(producer_arguments)),
        },
        "result_count": sum(command["result_count"] for command in commands),
    }
    adapter_packet = {
        **common,
        "checkpoint": "adapter-profile",
        "status": "passed",
        "production_lifecycle_writes": "disabled",
        "results": {
            "profile_id": EXPECTED_PROFILE_ID,
            "profile_complete": True,
            "runtime_matches_reviewed_profile": True,
            "immutable_artifacts": {
                "runtime-profile": {
                    "path": runtime_path.relative_to(approved_root).as_posix(),
                    "sha256": runtime_sha256,
                }
            },
        },
    }
    adapter_packet["packet_sha256"] = _sha256(_canonical_json(adapter_packet))
    _write_json_exclusive(adapter_path, adapter_packet)
    _validate_c0_adapter_profile(adapter_path, repository_root, approved_root)

    adapter_sha256 = hashlib.sha256(adapter_path.read_bytes()).hexdigest()
    wrapper_command = {
        **producer_command,
        "stdout_sha256": adapter_sha256,
        "finished_utc_ms": _utc_now_milliseconds(),
    }
    c0_packet = {
        **common,
        "checkpoint": "C0",
        "status": "passed",
        "finished_utc": wrapper_command["finished_utc_ms"],
        "commands": [wrapper_command, *commands[1:]],
        "results": {
            "adapter_profile_path": adapter_path.relative_to(approved_root).as_posix(),
            "adapter_profile_sha256": adapter_sha256,
        },
    }
    _validate_common_checkpoint("C0", {key: value for key, value in c0_packet.items() if key != "status"}, repository_root)
    _write_json_exclusive(wrapper_path, c0_packet)


def _workload_identity(deployments: list[Mapping[str, Any]]) -> dict[str, Any]:
    return {
        summary["name"]: {"generation": summary["generation"], "images": summary["images"]}
        for summary in (_deployment_summary(item) for item in deployments)
        if summary["name"]
    }


def run_adapter_profile_checkpoint(
    *,
    identity: EnvironmentIdentity,
    workload_profile: str,
    steady_state_minutes: int,
    purge_backlog_records: int,
    evidence_path: Path,
    repository_root: Path | None = None,
    c0_wrapper_path: Path | None = None,
) -> int:
    """Collect the C1 read-only profile and reject any unproven adapter.

    Role, stated precisely (narrowed 2026-07-26 per the Administrator decision): this
    command captures identity and rejects fail-closed. It is NOT a behavioural prober, and
    it is not the producer for the C1 gate rows that require CRUD, strong-read, ETag,
    transaction-fault, TTL, actor/Scheduler, request-bound, throughput, purge-backlog,
    isolation, encryption or reclamation observations.

    Corrected 2026-07-27 per the Administrator decision of that date: those thirteen rows
    (C1.1-C1.12 and C1.14) are recorded as **blocked** in the story's C1 Gate Evidence
    Table, not as naming their own command. No operator-executable producer can exist for
    them while the PG-ONPREM-1 lifecycle Deployments are scaled to zero. The earlier
    wording pointed at commands the table never held, so the cross-reference resolved in
    neither direction; each row now carries its own blocker with owner and reopen trigger.
    """

    run_started = _utc_now_milliseconds()

    # The evidence path must be usable before anything else runs; an unusable path used to
    # raise IsADirectoryError/NotADirectoryError after the target had already been queried.
    if not str(evidence_path).strip():
        print("evidence path is empty", file=sys.stderr)
        return 2
    if evidence_path.is_dir():
        print(f"evidence path is a directory: {evidence_path}", file=sys.stderr)
        return 2
    try:
        evidence_path.parent.mkdir(parents=True, exist_ok=True)
    except OSError as exc:
        print(f"evidence path is not writable: {exc}", file=sys.stderr)
        return 2
    if c0_wrapper_path is not None:
        if repository_root is None:
            print("repository root is required for a source-bound C0 packet", file=sys.stderr)
            return 2
        try:
            approved_root = _validated_evidence_root(Path(identity.evidence_root), repository_root)
            evidence_path = _require_evidence_path(
                evidence_path, approved_root, "adapter-profile output", must_exist=False
            )
            c0_wrapper_path = _require_evidence_path(
                c0_wrapper_path, approved_root, "C0 wrapper output", must_exist=False
            )
            if evidence_path.exists() or c0_wrapper_path.exists():
                raise EvidenceValidationError("C0 outputs are immutable and must not already exist")
        except (EvidenceValidationError, OSError) as exc:
            print(str(exc), file=sys.stderr)
            return 2

    # Approved-identity comparison runs BEFORE any query against the target. Querying first
    # meant an unapproved target was contacted, and its output written into the packet,
    # regardless of the comparison result.
    preflight_reason = None
    if workload_profile != "adr-27.1-two-writer-500eps":
        preflight_reason = f"unsupported workload profile: {workload_profile}"
    elif steady_state_minutes != 30 or purge_backlog_records != 150000:
        preflight_reason = "C1 workload envelope does not match the mandatory 30-minute/150,000-record gate"
    elif identity.kube_namespace != (
        EXPECTED_QUALIFICATION_NAMESPACE if c0_wrapper_path is not None else EXPECTED_KUBE_NAMESPACE
    ):
        preflight_reason = (
            "source-bound C0 must observe the isolated qualification namespace"
            if c0_wrapper_path is not None
            else "execution target does not match the approved on-premises Kubernetes namespace"
        )
    elif identity.profile_id != EXPECTED_PROFILE_ID:
        preflight_reason = "profile identity does not match the approved immutable PG-ONPREM-1 profile"

    capacity = collect_capacity_evidence()
    attestations = collect_attestations()
    # Capabilities use the reviewed profile's key set so the two manifests are
    # comparable. Every value is `False` because no C1 gate has passed: the runtime
    # profile hash equals the reviewed one only once the gates actually prove these
    # capabilities, which is the semantics `runtime_matches_reviewed_profile` was
    # always meant to carry. The packet's separate `production_lifecycle_writes` and
    # `evidence_is_approval` lines remain the fail-closed markers.
    profile = AdapterProfile(
        identity=identity.to_profile_identity(),
        capabilities={
            "actorStateStore": False,
            "strongReads": False,
            "transactionRollback": False,
            "ttl": False,
        },
        workload=ADR_TWO_WRITER_WORKLOAD.to_dict(),
    )

    if preflight_reason is not None:
        _write_rejection_evidence(
            evidence_path,
            identity=identity,
            profile=profile,
            reason=preflight_reason,
            observations=[],
            summaries={},
            capacity=capacity,
            attestations=attestations,
            runtime_identity={"status": "not captured; identity preflight rejected before any query"},
        )
        print(f"C1 adapter-profile: rejected ({preflight_reason})")
        print(f"Evidence: {evidence_path}")
        return 1

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
    workload_before = _workload_identity(deployments)

    summaries = {
        "deployments": [_deployment_summary(item) for item in deployments],
        "components": [_component_summary(item) for item in components],
        "configurations": [_configuration_summary(item) for item in configurations],
        "statefulsets": [_statefulset_summary(item) for item in statefulsets],
        "pods": [_pod_summary(item) for item in pods],
    }

    # AC1 requires the running Dapr runtime identity, captured from the deployment rather
    # than inferred from .NET package pins. This is gate C1.15's producer.
    # Story 27.3 code review (eighth-invocation review): the digest set used to union
    # *every* pod while the version was read from Running pods only, so a terminated
    # old-ReplicaSet pod could report the fleet as divergent while the running fleet
    # was uniform. Both now observe the same population, and the packet names it.
    running_pod_summaries = [summary for summary in summaries["pods"] if summary["phase"] == "Running"]
    sidecar_images, sidecar_digests = collect_sidecar_image_identity(running_pod_summaries)
    runtime_identity: dict[str, Any] = {
        "kube_context_observed": identity.kube_context,
        "kube_context_is_reviewed_label": identity.kube_context == EXPECTED_KUBE_CONTEXT,
        "sidecar_identity_population": f"{len(running_pod_summaries)} Running pod(s)",
        "sidecar_images": sidecar_images,
        "sidecar_image_digests": sidecar_digests,
    }
    running_pods = [summary["name"] for summary in running_pod_summaries]
    daprd_identity, daprd_observations = collect_daprd_runtime_identity(
        running_pods,
        lambda pod_name, executable, flag: _run_daprd(identity, pod_name, executable, flag),
    )
    observations.extend(daprd_observations)
    runtime_identity.update(daprd_identity)
    # The version is read from the first Running pod that answers while the digest set
    # unions the whole Running population, so a mid-rollout fleet would otherwise be
    # reported as one runtime version running two different images with nothing saying
    # so.
    #
    # Story 27.3 code review (eighth-invocation review): this collapsed a tri-state
    # into a boolean. "Nothing was captured" and "the blocker string was returned"
    # both rendered as `false`, which reads as the positive claim *the fleet is not
    # uniform* - indistinguishable from a real mid-rollout divergence, and the exact
    # claim-versus-blocker confusion the sibling fields fail closed to avoid.
    runtime_identity["sidecar_digest_is_uniform"] = _digest_uniformity(sidecar_digests)

    if any(observation.exit_code != 0 or observation.payload is None for observation in observations[:5]):
        reason = "deployment identity could not be captured from every required read-only Kubernetes query"
    else:
        statefulsets_by_name = {
            _mapping(item.get("metadata")).get("name"): item for item in statefulsets
        }
        postgresql = statefulsets_by_name.get("access-telemetry-postgresql")
        postgresql_spec = _mapping(_mapping(postgresql).get("spec"))
        postgresql_status = _mapping(_mapping(postgresql).get("status"))
        postgresql_images = _container_images(_mapping(postgresql_spec.get("template")).get("spec"))
        by_name = {_mapping(item.get("metadata")).get("name"): item for item in deployments}
        lifecycle = by_name.get("memories-access-telemetry")
        lifecycle_replicas = _mapping(_mapping(lifecycle).get("spec")).get("replicas", 0) or 0
        if postgresql is None:
            reason = "the approved access-telemetry PostgreSQL StatefulSet is missing"
        elif postgresql_spec.get("replicas") != 1 or postgresql_status.get("readyReplicas", 0) != 1:
            reason = "the approved access-telemetry PostgreSQL StatefulSet is not exactly 1/1 Ready"
        elif postgresql_images != [EXPECTED_POSTGRESQL_IMAGE]:
            reason = "the running PostgreSQL image does not match the approved immutable digest"
        elif lifecycle is None or lifecycle_replicas < 1:
            reason = "lifecycle deployment is disabled; Production writes remain fail-closed"
        else:
            store = next(
                (
                    item
                    for item in components
                    if _mapping(item.get("metadata")).get("name") == "access-telemetry-store"
                ),
                None,
            )
            store_spec = _mapping(_mapping(store).get("spec"))
            if store_spec.get("type") != "state.postgresql" or store_spec.get("version") != "v2":
                reason = "exact Production state-store component identity is missing"
            elif c0_wrapper_path is None:
                reason = (
                    "state.postgresql/v2 has no complete approved exact-profile Dapr behavior, load, "
                    "capacity, backup/restore, physical-reclamation, and separated-review result"
                )
            elif _component_metadata_value(store, "maxConns") != EXPECTED_MAX_CONNS:
                reason = "the running state component maxConns differs from PG-ONPREM-1"
            elif runtime_identity.get("daprd_version") != "1.18.1":
                reason = "the running Dapr version differs from PG-ONPREM-1"
            elif runtime_identity.get("sidecar_digest_is_uniform") is not True:
                reason = "the running Dapr sidecar digest is absent or non-uniform"
            else:
                reason = ""

    # Re-read the workload identity after the run so the packet can show the target was not
    # mutated between the first and last observation.
    after = _run_kubectl(identity, "get", "deployments", "-o", "json")
    observations.append(after)
    workload_after = _workload_identity(_items(after.payload))

    if not reason and workload_before != workload_after:
        reason = "deployment workload identity changed during C0 collection"

    if not reason and c0_wrapper_path is not None and repository_root is not None:
        try:
            _write_c0_adapter_profile_pass(
                adapter_path=evidence_path,
                wrapper_path=c0_wrapper_path,
                repository_root=repository_root,
                evidence_root=Path(identity.evidence_root),
                identity=identity,
                summaries=summaries,
                runtime_identity=runtime_identity,
                workload_identity_before=workload_before,
                workload_identity_after=workload_after,
                observations=observations,
                started_utc_ms=run_started,
            )
            print("C0 adapter-profile: passed")
            print(f"Adapter evidence: {evidence_path}")
            print(f"C0 wrapper: {c0_wrapper_path}")
            return 0
        except (EvidenceValidationError, OSError, ValueError) as exc:
            reason = f"source-bound C0 packet could not be written: {_bounded_reason(exc)}"

    _write_rejection_evidence(
        evidence_path,
        identity=identity,
        profile=profile,
        reason=reason,
        observations=observations,
        summaries=summaries,
        capacity=capacity,
        attestations=attestations,
        workload_identity_before=workload_before,
        workload_identity_after=workload_after,
        runtime_identity=runtime_identity,
    )
    print(f"C1 adapter-profile: rejected ({reason})")
    print(f"Evidence: {evidence_path}")
    return 1


class EvidenceValidationError(ValueError):
    """Raised when lifecycle or close-out evidence is not independently usable."""


STORY_27_4_CHECKPOINTS: tuple[str, ...] = (
    "c2-production-replacement",
    "c3-retention-reclamation",
    "c4-failure-privacy-observability",
)
STORY_27_4_PROFILE_SHA256 = canonical_pg_onprem_profile().manifest()["profile_sha256"]
STORY_27_4_WORKLOAD_SHA256 = _sha256(_canonical_json(ADR_TWO_WRITER_WORKLOAD.to_dict()))
REQUIRED_REPLACEMENTS: tuple[str, ...] = (
    "actor-activation",
    "clock-service",
    "clock-service-dapr-sidecar",
    "lifecycle-service",
    "lifecycle-service-dapr-sidecar",
    "placement-member-1",
    "placement-member-2",
    "placement-member-3",
    "scheduler-member-1",
    "scheduler-member-2",
    "scheduler-member-3",
    "server-writer-1",
    "server-writer-1-dapr-sidecar",
    "server-writer-2",
    "server-writer-2-dapr-sidecar",
)
REQUIRED_FAILURE_SCENARIOS: tuple[str, ...] = (
    "actor-failover",
    "application-outage",
    "approved-adapter-fault",
    "bad-configuration",
    "bad-key",
    "capacity-pressure",
    "clock-outage",
    "dapr-outage",
    "degraded-rollback",
    "etag-failure",
    "profile-drift",
    "queue-byte-exhaustion",
    "queue-record-exhaustion",
    "reconnect",
    "reminder-delay",
    "retry-exhaustion",
    "shutdown",
    "stale-attestation",
    "state-outage",
    "ttl-failure",
    "transaction-failure",
)
STORY_27_4_PRODUCERS: Mapping[str, tuple[str, str]] = {
    "C0": ("adapter-profile/v1", "tools/verify-access-telemetry-lifecycle.py"),
    "adapter-profile": ("adapter-profile/v1", "tools/verify-access-telemetry-lifecycle.py"),
    "c2-production-replacement": (
        "c2-production-replacement/v1",
        "tools/access_telemetry_c2_producer.py",
    ),
    "c3-retention-reclamation": (
        "c3-retention-reclamation/v1",
        "tools/access_telemetry_c3_producer.py",
    ),
    "c4-failure-privacy-observability": (
        "c4-failure-privacy-observability/v1",
        "tools/access_telemetry_c4_producer.py",
    ),
    "C5": ("c5-operations-approval/v1", "tools/verify-access-telemetry-lifecycle.py"),
    "C6": ("c6-security-approval/v1", "tools/verify-access-telemetry-lifecycle.py"),
    "terminal": ("terminal-validation/v1", "tools/verify-access-telemetry-lifecycle.py"),
}
REQUIRED_LIFECYCLE_SIGNALS: tuple[str, ...] = (
    "accepted",
    "dropped",
    "enqueued",
    "expired",
    "failed",
    "persisted",
    "purged",
    "rejected",
    "retried",
)
REQUIRED_TENANT_DENIAL_TESTS: tuple[str, ...] = (
    "SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies",
    "TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState",
    "TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies",
    "VerifyAsync_DetectsMissingSemanticTenantId_ReturnsFailed",
    "VerifyAsync_DetectsSemanticTenantIdMismatch_ReturnsFailed",
    "VerifyAsync_DetectsSyntacticTenantIdMismatch_ReturnsFailed",
)
FORBIDDEN_LABELS = frozenset(
    {
        "case_id",
        "component_backend_id",
        "memory_unit_id",
        "process_epoch",
        "query",
        "record_id",
        "service_instance_id",
        "source_uri",
        "span_id",
        "subject",
        "tenant_id",
        "trace_id",
        "user_id",
    }
)

# This is the complete set that an independently approved close-out manifest may
# mutate. The inventory also records every other A41 reference, but those paths are
# read-only inputs to the chain.
A41_ALLOWED_MUTATION_PATHS: tuple[str, ...] = (
    "_bmad-output/implementation-artifacts/deferred-work.md",
    "_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md",
    "_bmad-output/project-context.md",
    "docs/dev/telemetry.md",
)
A41_PROTECTED_PATHS: tuple[str, ...] = (
    "_bmad-output/implementation-artifacts/20-5-inbound-rate-limiting-quotas-and-audit-completeness.md",
    "_bmad-output/implementation-artifacts/epic-20-retro-2026-07-04.md",
    "_bmad-output/implementation-artifacts/sprint-status.yaml",
    "_bmad-output/planning-artifacts/epics.md",
)

_STORY_PACKET_SCHEMA_VERSION = 1
_MAX_EVIDENCE_BYTES = 1_048_576
_MAX_SNAPSHOT_BYTES = 4_194_304
_MAX_STDERR_BYTES = 65_536
_CHECKPOINT_FRESHNESS_MILLISECONDS = 15 * 60 * 1000
_FUTURE_SKEW_MILLISECONDS = 1_000
_HEX64 = re.compile(r"\A[0-9a-f]{64}\Z")
_COMMIT_ID = re.compile(r"\A[0-9a-f]{40,64}\Z")
_SAFE_NAME = re.compile(r"\A[a-z0-9][a-z0-9._:/-]{0,127}\Z")
_SAFE_GIT_REMOTE = re.compile(r"\A[A-Za-z0-9][A-Za-z0-9._/-]{0,127}\Z")
_UNSAFE_EVIDENCE_KEY = re.compile(
    r"(?i)(authorization|bearer|credential|password|passwd|privatekey|secretvalue|tokenvalue|"
    r"rawquery|rawsubject|rawsourceuri|rawtenant|rawuser|rawcase|payloadcontent)"
)
_UNSAFE_EVIDENCE_VALUE = re.compile(
    r"(?i)(-----BEGIN [A-Z ]*PRIVATE KEY-----|\bbearer\s+[A-Za-z0-9._~+/=-]{8,}|"
    r"(?:password|passwd|api[_-]?key|client[_-]?secret)\s*[:=]\s*\S+)"
)
_COMMON_CHECKPOINT_FIELDS = frozenset(
    {
        "schema_version",
        "checkpoint",
        "profile_sha256",
        "workload_sha256",
        "source_commit",
        "source_hashes",
        "owner",
        "started_utc",
        "finished_utc",
        "failure_count",
        "skip_count",
        "failures",
        "skipped",
        "commands",
        "producer",
        "result_count",
        "results",
    }
)

A41_SEMANTIC_TRANSITIONS: Mapping[str, Mapping[str, tuple[str, ...]]] = {
    "_bmad-output/implementation-artifacts/deferred-work.md": {
        "required": ("20.5-A41-ACCESS-TELEMETRY-RETENTION", "status: resolved", "published-close-out-verified"),
        "forbidden": ("status: carried-forward",),
    },
    "_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md": {
        "required": ("published-close-out-verified", "C6"),
        "forbidden": ("operator-pending",),
    },
    "_bmad-output/project-context.md": {
        "required": ("20.5-A41-ACCESS-TELEMETRY-RETENTION", "published-close-out-verified"),
        "forbidden": ("partially closed",),
    },
    "docs/dev/telemetry.md": {
        "required": ("20.5-A41-ACCESS-TELEMETRY-RETENTION", "published-close-out-verified"),
        "forbidden": ("carried forward",),
    },
}


class _EvidenceAggregateBudget:
    """Account distinct immutable evidence files against one closed-chain bound."""

    def __init__(self, evidence_root: Path, maximum_bytes: int = _MAX_SNAPSHOT_BYTES) -> None:
        self._evidence_root = evidence_root
        self._maximum_bytes = maximum_bytes
        self._accounted_paths: set[Path] = set()
        self._aggregate_bytes = 0

    def account(self, path: Path, name: str) -> Path:
        """Authenticate and account one distinct evidence file."""

        resolved_path = _safe_input_path(path, approved_root=self._evidence_root)
        if resolved_path not in self._accounted_paths:
            self._aggregate_bytes += resolved_path.stat().st_size
            if self._aggregate_bytes > self._maximum_bytes:
                raise EvidenceValidationError(
                    f"terminal evidence chain exceeds the {self._maximum_bytes}-byte aggregate bound at {name}"
                )
            self._accounted_paths.add(resolved_path)
        return resolved_path


def _require_mapping(value: Any, name: str) -> Mapping[str, Any]:
    if not isinstance(value, Mapping):
        raise EvidenceValidationError(f"{name} must be an object")
    return value


def _require_sequence(value: Any, name: str) -> list[Any]:
    if not isinstance(value, list):
        raise EvidenceValidationError(f"{name} must be an array")
    return value


def _require_exact_fields(value: Mapping[str, Any], allowed: frozenset[str], name: str) -> None:
    unknown = sorted(set(value) - allowed)
    missing = sorted(allowed - set(value))
    if unknown or missing:
        details = []
        if missing:
            details.append("missing " + ", ".join(missing))
        if unknown:
            details.append("unknown " + ", ".join(unknown))
        raise EvidenceValidationError(f"{name} has a closed schema ({'; '.join(details)})")


def _require_bool(value: Any, name: str, expected: bool | None = None) -> bool:
    if not isinstance(value, bool):
        raise EvidenceValidationError(f"{name} must be a Boolean")
    if expected is not None and value is not expected:
        raise EvidenceValidationError(f"{name} must be {str(expected).lower()}")
    return value


def _require_integer(
    value: Any,
    name: str,
    *,
    minimum: int = 0,
    maximum: int = _INT64_MAX,
) -> int:
    if isinstance(value, bool) or not isinstance(value, int):
        raise EvidenceValidationError(f"{name} must be an integer, not a Boolean or number string")
    if value < minimum or value > maximum:
        raise EvidenceValidationError(f"{name} must be between {minimum} and {maximum}")
    return value


def _require_nonzero_integer(value: Any, name: str) -> int:
    return _require_integer(value, name, minimum=1)


def _require_nonempty_string(value: Any, name: str, *, maximum: int = 256) -> str:
    if not isinstance(value, str) or not value.strip():
        raise EvidenceValidationError(f"{name} must be a non-empty string")
    normalized = value.strip()
    if len(normalized) > maximum or "\r" in normalized or "\n" in normalized:
        raise EvidenceValidationError(f"{name} is not a bounded single-line string")
    return normalized


def _require_hex64(value: Any, name: str) -> str:
    normalized = _require_nonempty_string(value, name, maximum=64)
    if _HEX64.fullmatch(normalized) is None:
        raise EvidenceValidationError(f"{name} must be a lowercase SHA-256 digest")
    return normalized


def _require_git_remote(value: Any) -> str:
    normalized = _require_nonempty_string(value, "remote", maximum=128)
    if _SAFE_GIT_REMOTE.fullmatch(normalized) is None or ".." in normalized or "//" in normalized:
        raise EvidenceValidationError("remote must be a canonical configured Git remote name")
    return normalized


def _parse_utc(value: Any, name: str) -> datetime:
    normalized = _require_nonempty_string(value, name, maximum=35)
    if not normalized.endswith("Z"):
        raise EvidenceValidationError(f"{name} must be canonical UTC ending in Z")
    try:
        parsed = datetime.fromisoformat(normalized[:-1] + "+00:00")
    except ValueError as exc:
        raise EvidenceValidationError(f"{name} is not a valid UTC timestamp") from exc
    if parsed.utcoffset() != timezone.utc.utcoffset(parsed):
        raise EvidenceValidationError(f"{name} must be UTC")
    return parsed


def _utc_now_milliseconds() -> int:
    return int(datetime.now(timezone.utc).timestamp() * 1000)


def _require_utc_milliseconds(value: Any, name: str) -> int:
    return _require_integer(value, name, minimum=946_684_800_000, maximum=4_102_444_800_000)


def _validate_fresh_run(
    started: int,
    finished: int,
    name: str,
    *,
    maximum_duration_ms: int = 86_400_000,
) -> None:
    now = _utc_now_milliseconds()
    if finished < started or finished - started > maximum_duration_ms:
        raise EvidenceValidationError(f"{name} timestamps are out of order or exceed the allowed duration")
    if finished > now + _FUTURE_SKEW_MILLISECONDS:
        raise EvidenceValidationError(f"{name} finished more than one second in the future")
    if finished < now - _CHECKPOINT_FRESHNESS_MILLISECONDS:
        raise EvidenceValidationError(f"{name} is older than the 15-minute acceptance window")


def _validate_secret_safe(
    value: Any,
    name: str = "evidence",
    *,
    maximum_string_length: int = 4096,
) -> None:
    """Reject secret aliases, non-finite numbers, and raw-value-shaped evidence."""

    if isinstance(value, Mapping):
        for key, child in value.items():
            if not isinstance(key, str):
                raise EvidenceValidationError(f"{name} contains a non-string field name")
            normalized = re.sub(r"[^a-z0-9]", "", key.lower())
            assurance_fields = {"rawvaluesabsent", "secretvaluesabsent"}
            secret_alias = any(
                fragment in normalized
                for fragment in (
                    "authorization",
                    "bearer",
                    "credential",
                    "password",
                    "passwd",
                    "privatekey",
                    "secret",
                    "token",
                )
            )
            if normalized not in assurance_fields and (
                normalized.startswith("raw") or secret_alias or _UNSAFE_EVIDENCE_KEY.search(normalized)
            ):
                raise EvidenceValidationError(f"{name} contains prohibited field alias {key!r}")
            _validate_secret_safe(
                child,
                f"{name}.{key}",
                maximum_string_length=maximum_string_length,
            )
        return
    if isinstance(value, list):
        for index, child in enumerate(value):
            _validate_secret_safe(
                child,
                f"{name}[{index}]",
                maximum_string_length=maximum_string_length,
            )
        return
    if isinstance(value, float):
        if value != value or value in {float("inf"), float("-inf")}:
            raise EvidenceValidationError(f"{name} contains a non-finite number")
        return
    if isinstance(value, str):
        if len(value) > maximum_string_length:
            raise EvidenceValidationError(f"{name} contains an oversized string")
        if _UNSAFE_EVIDENCE_VALUE.search(value):
            raise EvidenceValidationError(f"{name} contains secret-shaped content")


def _json_without_duplicates(text: str, source: str) -> Any:
    def pairs(values: list[tuple[str, Any]]) -> dict[str, Any]:
        result: dict[str, Any] = {}
        for key, value in values:
            if key in result:
                raise EvidenceValidationError(f"{source} contains duplicate JSON field {key!r}")
            result[key] = value
        return result

    try:
        return json.loads(
            text,
            object_pairs_hook=pairs,
            parse_constant=lambda value: (_ for _ in ()).throw(
                EvidenceValidationError(f"{source} contains non-finite number {value}")
            ),
        )
    except json.JSONDecodeError as exc:
        raise EvidenceValidationError(f"{source} is not valid JSON: {exc.msg}") from exc


def _safe_input_path(path: Path, *, approved_root: Path | None = None) -> Path:
    if path.is_symlink():
        raise EvidenceValidationError(f"symlink evidence input is prohibited: {path}")
    try:
        resolved = path.resolve(strict=True)
    except OSError as exc:
        raise EvidenceValidationError(f"evidence input is unavailable: {path}") from exc
    if not resolved.is_file():
        raise EvidenceValidationError(f"evidence input is not a file: {path}")
    if approved_root is not None:
        if path.absolute() != resolved:
            raise EvidenceValidationError(f"evidence input uses a path alias: {path}")
        try:
            resolved.relative_to(approved_root.resolve(strict=True))
        except (OSError, ValueError) as exc:
            raise EvidenceValidationError(f"evidence input escapes the approved root: {path}") from exc
    return resolved


def _read_bounded_json(
    path: Path,
    *,
    approved_root: Path | None = None,
    maximum_bytes: int = _MAX_EVIDENCE_BYTES,
    maximum_string_length: int = 4096,
) -> Mapping[str, Any]:
    resolved = _safe_input_path(path, approved_root=approved_root)
    payload = resolved.read_bytes()
    if not payload or len(payload) > maximum_bytes:
        raise EvidenceValidationError(f"evidence input must contain 1..{maximum_bytes} bytes")
    if payload.startswith(b"\xef\xbb\xbf"):
        raise EvidenceValidationError("evidence input must not carry a UTF-8 byte-order mark")
    try:
        text = payload.decode("utf-8", errors="strict")
    except UnicodeDecodeError as exc:
        raise EvidenceValidationError("evidence input is not canonical UTF-8") from exc
    if text.encode("utf-8") != payload:
        raise EvidenceValidationError("evidence input is not canonical UTF-8")
    result = _require_mapping(_json_without_duplicates(text, str(path)), str(path))
    _validate_secret_safe(result, maximum_string_length=maximum_string_length)
    return result


def _git_checked(repository_root: Path, *arguments: str) -> str:
    try:
        result = subprocess.run(
            ("git", "-C", str(repository_root), *arguments),
            check=False,
            capture_output=True,
            text=True,
            errors="strict",
            timeout=30,
        )
    except subprocess.TimeoutExpired as exc:
        raise EvidenceValidationError(f"git {' '.join(arguments)} timed out") from exc
    if result.returncode != 0:
        raise EvidenceValidationError(
            f"git {' '.join(arguments)} failed with exit {result.returncode}: {_redact(result.stderr)}"
        )
    return result.stdout.strip()


def _normalize_repo_path(repository_root: Path, value: Any, name: str) -> str:
    relative = _require_nonempty_string(value, name, maximum=512).replace("\\", "/")
    candidate = Path(relative)
    if candidate.is_absolute() or ".." in candidate.parts or relative.startswith("./"):
        raise EvidenceValidationError(f"{name} must be a canonical repository-relative path")
    resolved_root = repository_root.resolve(strict=True)
    try:
        resolved = (resolved_root / candidate).resolve(strict=False)
        resolved.relative_to(resolved_root)
    except ValueError as exc:
        raise EvidenceValidationError(f"{name} escapes the repository") from exc
    if (resolved_root / candidate).is_symlink():
        raise EvidenceValidationError(f"{name} must not be a symlink")
    return relative


def _hash_git_blob(repository_root: Path, commit: str, relative: str) -> str:
    try:
        result = subprocess.run(
            ("git", "-C", str(repository_root), "show", f"{commit}:{relative}"),
            check=False,
            capture_output=True,
            timeout=30,
        )
    except subprocess.TimeoutExpired as exc:
        raise EvidenceValidationError(f"git source read timed out for {relative}") from exc
    if result.returncode != 0:
        raise EvidenceValidationError(f"source path {relative!r} does not exist at commit {commit}")
    return hashlib.sha256(result.stdout).hexdigest()


def _expected_source_paths(checkpoint: Any) -> frozenset[str]:
    identity = STORY_27_4_PRODUCERS.get(checkpoint)
    if identity is None:
        raise EvidenceValidationError("checkpoint has no closed source registry entry")
    paths = {identity[1], "tools/verify_access_telemetry_lifecycle.py"}
    if checkpoint in STORY_27_4_CHECKPOINTS:
        paths.add("tools/access_telemetry_producer_common.py")
    return frozenset(paths)


def _validate_source_identity(payload: Mapping[str, Any], repository_root: Path | None) -> None:
    commit = _require_nonempty_string(payload.get("source_commit"), "source_commit", maximum=64)
    if _COMMIT_ID.fullmatch(commit) is None:
        raise EvidenceValidationError("source_commit must be a full lowercase commit identifier")
    hashes = _require_mapping(payload.get("source_hashes"), "source_hashes")
    if not hashes:
        raise EvidenceValidationError("source_hashes must bind at least one reviewed producer")
    if set(hashes) != set(_expected_source_paths(payload.get("checkpoint"))):
        raise EvidenceValidationError("source_hashes do not bind the exact reviewed source set")
    for path, digest in hashes.items():
        _require_hex64(digest, f"source_hashes[{path!r}]")
    producer = _require_mapping(payload.get("producer"), "producer")
    _require_exact_fields(
        producer,
        frozenset({"command_id", "path", "source_sha256", "arguments", "arguments_sha256"}),
        "producer",
    )
    command_id = _require_nonempty_string(producer["command_id"], "producer.command_id", maximum=128)
    raw_path = _require_nonempty_string(producer["path"], "producer.path", maximum=512).replace("\\", "/")
    if Path(raw_path).is_absolute() or ".." in Path(raw_path).parts or raw_path.startswith("./"):
        raise EvidenceValidationError("producer.path must be canonical and repository-relative")
    path = _normalize_repo_path(repository_root, raw_path, "producer.path") if repository_root else raw_path
    source_sha256 = _require_hex64(producer["source_sha256"], "producer.source_sha256")
    if hashes.get(path) != source_sha256:
        raise EvidenceValidationError("executed producer is not bound by source_hashes")
    expected_identity = STORY_27_4_PRODUCERS.get(payload.get("checkpoint"))
    if expected_identity != (command_id, path):
        raise EvidenceValidationError("producer is not the closed registered scenario command")
    arguments = _require_mapping(producer["arguments"], "producer.arguments")
    _validate_secret_safe(arguments, "producer.arguments")
    if producer["arguments_sha256"] != _sha256(_canonical_json(arguments)):
        raise EvidenceValidationError("producer argument hash mismatch")
    if repository_root is None:
        return
    resolved_commit = _git_checked(repository_root, "rev-parse", "--verify", f"{commit}^{{commit}}")
    if resolved_commit != commit:
        raise EvidenceValidationError("source_commit is abbreviated or does not resolve exactly")
    for source_path, digest in hashes.items():
        relative = _normalize_repo_path(repository_root, source_path, "source_hashes path")
        if _hash_git_blob(repository_root, commit, relative) != digest:
            raise EvidenceValidationError(f"source hash mismatch for {relative}")


def _validate_command_ledger(payload: Mapping[str, Any], started: int, finished: int) -> None:
    commands = _require_sequence(payload.get("commands"), "commands")
    if not commands:
        raise EvidenceValidationError("commands must contain producer-controlled observations")
    command_ids: set[str] = set()
    for index, item in enumerate(commands):
        command = _require_mapping(item, f"commands[{index}]")
        _require_exact_fields(
            command,
            frozenset(
                {
                    "command_id",
                    "arguments",
                    "arguments_sha256",
                    "started_utc_ms",
                    "finished_utc_ms",
                    "exit_code",
                    "stdout_sha256",
                    "stderr_sha256",
                    "result_count",
                }
            ),
            f"commands[{index}]",
        )
        command_id = _require_nonempty_string(
            command["command_id"], f"commands[{index}].command_id", maximum=128
        )
        if command_id in command_ids:
            raise EvidenceValidationError("commands contain a duplicate command_id")
        command_ids.add(command_id)
        arguments = _require_mapping(command["arguments"], f"commands[{index}].arguments")
        _validate_secret_safe(arguments, f"commands[{index}].arguments")
        if command["arguments_sha256"] != _sha256(_canonical_json(arguments)):
            raise EvidenceValidationError(f"commands[{index}] argument hash mismatch")
        command_started = _require_utc_milliseconds(
            command["started_utc_ms"], f"commands[{index}].started_utc_ms"
        )
        command_finished = _require_utc_milliseconds(
            command["finished_utc_ms"], f"commands[{index}].finished_utc_ms"
        )
        if not (started <= command_started <= command_finished <= finished):
            raise EvidenceValidationError(f"commands[{index}] is outside the producer-controlled run")
        if _require_integer(command["exit_code"], f"commands[{index}].exit_code", maximum=255) != 0:
            raise EvidenceValidationError(f"commands[{index}] did not succeed")
        _require_hex64(command["stdout_sha256"], f"commands[{index}].stdout_sha256")
        _require_hex64(command["stderr_sha256"], f"commands[{index}].stderr_sha256")
        _require_nonzero_integer(command["result_count"], f"commands[{index}].result_count")


def _canonical_c1_gate_ids() -> tuple[str, ...]:
    return tuple(f"C1.{index}" for index in range(1, 26))


def _validate_predecessor(
    predecessor: Mapping[str, Any],
    repository_root: Path | None = None,
    evidence_root: Path | None = None,
) -> None:
    common_fields = frozenset(
        {
            "checkpoint",
            "status",
            "profile_sha256",
            "production_lifecycle_writes",
            "qualification_authorized",
            "evidence_is_approval",
            "approvals",
        }
    )
    uses_successors = "gates" not in predecessor
    _require_exact_fields(
        predecessor,
        common_fields | frozenset({"successors" if uses_successors else "gates"}),
        "C1 predecessor",
    )
    if predecessor.get("checkpoint") != "C1" or predecessor.get("status") != "passed":
        raise EvidenceValidationError("C1 predecessor has not passed")
    if predecessor.get("profile_sha256") != STORY_27_4_PROFILE_SHA256:
        raise EvidenceValidationError("C1 predecessor profile differs from PG-ONPREM-1")
    if predecessor.get("production_lifecycle_writes") != "disabled":
        raise EvidenceValidationError("C1 must preserve disabled Production lifecycle writes")
    _require_bool(predecessor.get("qualification_authorized"), "C1.qualification_authorized", True)
    _require_bool(predecessor.get("evidence_is_approval"), "C1.evidence_is_approval", True)

    gates_value = predecessor.get("gates")
    if not isinstance(gates_value, Mapping):
        successors = predecessor.get("successors")
        if isinstance(successors, list):
            gates_value = {}
            for index, item in enumerate(successors):
                successor = _require_mapping(item, f"C1.successors[{index}]")
                successor_id = _require_nonempty_string(
                    successor.get("gate_id"), f"C1.successors[{index}].gate_id"
                )
                if successor_id in gates_value:
                    raise EvidenceValidationError("C1 successors contain a duplicate gate_id")
                gates_value[successor_id] = successor
    gates = _require_mapping(gates_value, "C1.gates")
    required_gate_ids = set(_canonical_c1_gate_ids())
    if set(gates) != required_gate_ids:
        missing = sorted(required_gate_ids - set(gates))
        extra = sorted(set(gates) - required_gate_ids)
        raise EvidenceValidationError(
            f"C1 predecessor must contain the canonical 25 gates; missing={missing}, extra={extra}"
        )
    artifact_paths: set[str] = set()
    artifact_hashes: set[str] = set()
    for gate_id in _canonical_c1_gate_ids():
        gate = _require_mapping(gates[gate_id], f"C1.gates.{gate_id}")
        gate_fields = frozenset(
            {
                "status",
                "artifact_path",
                "artifact_sha256",
                "source_commit",
                "source_path",
                "source_sha256",
                "started_utc_ms",
                "finished_utc_ms",
                "result_count",
                "command",
            }
        )
        _require_exact_fields(
            gate,
            gate_fields | (frozenset({"gate_id"}) if uses_successors else frozenset()),
            f"C1.gates.{gate_id}",
        )
        if uses_successors and gate.get("gate_id") != gate_id:
            raise EvidenceValidationError(f"C1 successor {gate_id} has a mismatched gate_id")
        if gate.get("status") != "passed":
            raise EvidenceValidationError(f"C1 gate {gate_id} has not passed")
        _require_hex64(gate.get("artifact_sha256"), f"C1.gates.{gate_id}.artifact_sha256")
        source_commit = _require_nonempty_string(
            gate.get("source_commit"), f"C1.gates.{gate_id}.source_commit", maximum=64
        )
        if _COMMIT_ID.fullmatch(source_commit) is None:
            raise EvidenceValidationError(f"C1 gate {gate_id} source commit is not canonical")
        _require_hex64(gate.get("source_sha256"), f"C1.gates.{gate_id}.source_sha256")
        started = _require_utc_milliseconds(gate.get("started_utc_ms"), f"C1.gates.{gate_id}.started_utc_ms")
        finished = _require_utc_milliseconds(gate.get("finished_utc_ms"), f"C1.gates.{gate_id}.finished_utc_ms")
        _validate_fresh_run(started, finished, f"C1 gate {gate_id}")
        _require_nonzero_integer(gate.get("result_count"), f"C1.gates.{gate_id}.result_count")
        _validate_command_ledger({"commands": [gate.get("command")]}, started, finished)
        artifact_path_value = _require_nonempty_string(
            gate.get("artifact_path"), f"C1.gates.{gate_id}.artifact_path", maximum=512
        ).replace("\\", "/")
        if (
            Path(artifact_path_value).is_absolute()
            or ".." in Path(artifact_path_value).parts
            or artifact_path_value.startswith("./")
        ):
            raise EvidenceValidationError(f"C1 gate {gate_id} artifact path must be evidence-root relative")
        artifact_sha256 = gate["artifact_sha256"]
        if artifact_path_value in artifact_paths or artifact_sha256 in artifact_hashes:
            raise EvidenceValidationError(
                "each C1 gate requires its own artifact path and artifact hash"
            )
        artifact_paths.add(artifact_path_value)
        artifact_hashes.add(artifact_sha256)
        if repository_root is not None:
            if evidence_root is None:
                raise EvidenceValidationError("C1 artifact validation requires an external evidence root")
            artifact = _safe_input_path(
                evidence_root / artifact_path_value,
                approved_root=evidence_root,
            )
            if hashlib.sha256(artifact.read_bytes()).hexdigest() != gate["artifact_sha256"]:
                raise EvidenceValidationError(f"C1 gate {gate_id} artifact hash mismatch")
            source_path = _normalize_repo_path(
                repository_root, gate.get("source_path"), f"C1.gates.{gate_id}.source_path"
            )
            resolved_commit = _git_checked(
                repository_root, "rev-parse", "--verify", f"{source_commit}^{{commit}}"
            )
            if resolved_commit != source_commit:
                raise EvidenceValidationError(f"C1 gate {gate_id} source commit is abbreviated")
            if _hash_git_blob(repository_root, source_commit, source_path) != gate["source_sha256"]:
                raise EvidenceValidationError(f"C1 gate {gate_id} source hash mismatch")

    approvals = _require_sequence(predecessor.get("approvals"), "C1.approvals")
    if len(approvals) != 2:
        raise EvidenceValidationError("C1 requires exactly two independent approvals")
    by_role: dict[str, Mapping[str, Any]] = {}
    reviewers: set[str] = set()
    for index, item in enumerate(approvals):
        approval = _require_mapping(item, f"C1.approvals[{index}]")
        _require_exact_fields(
            approval,
            frozenset({"role", "reviewer", "state", "profile_sha256"}),
            f"C1.approvals[{index}]",
        )
        role = _require_nonempty_string(approval.get("role"), f"C1.approvals[{index}].role")
        if role not in {"platform-operations", "security"} or role in by_role:
            raise EvidenceValidationError("C1 approvals must contain Platform Operations and security once")
        reviewer = _require_nonempty_string(
            approval.get("reviewer"), f"C1.approvals[{index}].reviewer"
        )
        if reviewer in reviewers:
            raise EvidenceValidationError("C1 approvals must be made by independent reviewers")
        if approval.get("state") != "approved":
            raise EvidenceValidationError(f"C1 {role} approval is not approved")
        if approval.get("profile_sha256") != STORY_27_4_PROFILE_SHA256:
            raise EvidenceValidationError(f"C1 {role} approval is bound to another profile")
        reviewers.add(reviewer)
        by_role[role] = approval


def _validate_common_checkpoint(
    checkpoint: str,
    payload: Mapping[str, Any],
    repository_root: Path | None,
) -> None:
    _require_exact_fields(payload, _COMMON_CHECKPOINT_FIELDS, checkpoint)
    if _require_integer(payload["schema_version"], "schema_version", minimum=1, maximum=1) != 1:
        raise EvidenceValidationError("schema_version must be 1")
    if payload["checkpoint"] != checkpoint:
        raise EvidenceValidationError("checkpoint payload does not match the requested mode")
    if payload["profile_sha256"] != STORY_27_4_PROFILE_SHA256:
        raise EvidenceValidationError("checkpoint profile differs from the approved immutable profile")
    if payload["workload_sha256"] != STORY_27_4_WORKLOAD_SHA256:
        raise EvidenceValidationError("checkpoint workload differs from the approved immutable workload")
    _require_nonempty_string(payload["owner"], "owner")
    started = _require_utc_milliseconds(payload["started_utc"], "started_utc")
    finished = _require_utc_milliseconds(payload["finished_utc"], "finished_utc")
    _validate_fresh_run(
        started,
        finished,
        "checkpoint",
        maximum_duration_ms=720_000_000 if checkpoint == "c3-retention-reclamation" else 86_400_000,
    )
    if _require_integer(payload["failure_count"], "failure_count") != 0:
        raise EvidenceValidationError("checkpoint contains failures")
    if _require_integer(payload["skip_count"], "skip_count") != 0:
        raise EvidenceValidationError("checkpoint contains skipped observations")
    if _require_sequence(payload["failures"], "failures"):
        raise EvidenceValidationError("checkpoint failure list is not empty")
    if _require_sequence(payload["skipped"], "skipped"):
        raise EvidenceValidationError("checkpoint skipped list is not empty")
    _require_nonzero_integer(payload["result_count"], "result_count")
    _validate_source_identity(payload, repository_root)
    _validate_command_ledger(payload, started, finished)


def _require_true_fields(value: Mapping[str, Any], prefix: str, fields: Sequence[str]) -> None:
    for field in fields:
        _require_bool(value.get(field), f"{prefix}.{field}", True)


def _validate_result_observation(value: Any, name: str, expected_command_id: str | None = None) -> None:
    observation = _require_mapping(value, name)
    _require_exact_fields(
        observation,
        frozenset({"command_id", "output_sha256", "result_count"}),
        name,
    )
    command_id = _require_nonempty_string(observation["command_id"], f"{name}.command_id", maximum=128)
    if expected_command_id is not None and command_id != expected_command_id:
        raise EvidenceValidationError(f"{name} is bound to the wrong command")
    _require_hex64(observation["output_sha256"], f"{name}.output_sha256")
    _require_nonzero_integer(observation["result_count"], f"{name}.result_count")


def _validate_observation_bindings(payload: Mapping[str, Any]) -> None:
    commands = {
        command["command_id"]: command
        for command in _require_sequence(payload.get("commands"), "commands")
        if isinstance(command, Mapping)
    }
    producer = _require_mapping(payload.get("producer"), "producer")
    child_commands = set(commands) - {producer.get("command_id")}
    observations: list[Mapping[str, Any]] = []

    def visit(value: Any) -> None:
        if isinstance(value, Mapping):
            if set(value) == {"command_id", "output_sha256", "result_count"}:
                observations.append(value)
                return
            for nested in value.values():
                visit(nested)
        elif isinstance(value, list):
            for nested in value:
                visit(nested)

    visit(payload.get("results"))
    observed_commands = {observation.get("command_id") for observation in observations}
    if observed_commands != child_commands:
        raise EvidenceValidationError("result observations do not cover the exact child-command transcript")
    for observation in observations:
        command = commands.get(observation.get("command_id"))
        if command is None or observation.get("output_sha256") != command.get("stdout_sha256") or \
                observation.get("result_count") != command.get("result_count"):
            raise EvidenceValidationError("result observation differs from its command transcript")


def _validate_c2(results: Mapping[str, Any]) -> None:
    _require_exact_fields(
        results,
        frozenset(
            {
                "writers",
                "replacements",
                "adapter_fault",
                "console_continuity",
                "otlp_configured",
                "otlp_continuity",
                "continuity_observation",
                "qualification_transition",
            }
        ),
        "results",
    )
    writers = _require_mapping(results["writers"], "results.writers")
    _require_exact_fields(
        writers,
        frozenset(
            {
                "steady_state_minutes",
                "cluster_accepted_records_per_second",
                "component_operations_per_second",
                "writer_results",
                "acknowledged_loss",
                "actor_serialized",
                "idempotent_retry",
                "conflict_rejected",
                "idempotence_conflict_observation",
                "transaction_acknowledged",
                "reconstructed",
                "reconnected",
                "direct_backend_dependencies",
            }
        ),
        "results.writers",
    )
    if _require_integer(writers["steady_state_minutes"], "steady_state_minutes") != 30:
        raise EvidenceValidationError("C2 steady-state duration must be exactly 30 minutes")
    if _require_integer(
        writers["cluster_accepted_records_per_second"], "cluster_accepted_records_per_second"
    ) != 250:
        raise EvidenceValidationError("C2 cluster accepted rate must be exactly 250 records/s")
    if _require_integer(
        writers["component_operations_per_second"], "component_operations_per_second"
    ) < 500:
        raise EvidenceValidationError("C2 component throughput must be at least 500 operations/s")
    writer_results = _require_sequence(writers["writer_results"], "writer_results")
    if len(writer_results) != 2:
        raise EvidenceValidationError("C2 requires exactly two Server writer results")
    total_acknowledged = 0
    for index, item in enumerate(writer_results):
        writer = _require_mapping(item, f"writer_results[{index}]")
        _require_exact_fields(
            writer,
            frozenset(
                {
                    "writer",
                    "attempted",
                    "acknowledged",
                    "persisted",
                    "conflicted",
                    "transaction_acknowledgements",
                    "observation",
                }
            ),
            f"writer_results[{index}]",
        )
        if writer["writer"] != f"server-writer-{index + 1}":
            raise EvidenceValidationError("C2 writer identities must be the closed two-writer inventory")
        attempted = _require_nonzero_integer(writer["attempted"], f"writer_results[{index}].attempted")
        acknowledged = _require_nonzero_integer(
            writer["acknowledged"], f"writer_results[{index}].acknowledged"
        )
        persisted = _require_nonzero_integer(writer["persisted"], f"writer_results[{index}].persisted")
        conflicted = _require_integer(writer["conflicted"], f"writer_results[{index}].conflicted")
        transaction_acks = _require_nonzero_integer(
            writer["transaction_acknowledgements"],
            f"writer_results[{index}].transaction_acknowledgements",
        )
        if attempted != acknowledged + conflicted or persisted != acknowledged or transaction_acks != acknowledged:
            raise EvidenceValidationError("C2 per-writer accounting is not exact")
        _validate_result_observation(
            writer["observation"], f"writer_results[{index}].observation", f"writer-{index + 1}"
        )
        total_acknowledged += acknowledged
    if total_acknowledged != 250 * 30 * 60:
        raise EvidenceValidationError("C2 did not acknowledge the exact fixed 30-minute workload")
    if _require_integer(writers["acknowledged_loss"], "acknowledged_loss") != 0:
        raise EvidenceValidationError("C2 lost acknowledged records")
    _require_true_fields(
        writers,
        "results.writers",
        (
            "actor_serialized",
            "idempotent_retry",
            "conflict_rejected",
            "transaction_acknowledged",
            "reconstructed",
            "reconnected",
        ),
    )
    _validate_result_observation(
        writers["idempotence_conflict_observation"],
        "results.writers.idempotence_conflict_observation",
        "idempotence-conflict-proof",
    )
    if _require_sequence(writers["direct_backend_dependencies"], "direct_backend_dependencies"):
        raise EvidenceValidationError("C2 observed a direct backend dependency")

    replacements = _require_mapping(results["replacements"], "results.replacements")
    if set(replacements) != set(REQUIRED_REPLACEMENTS):
        raise EvidenceValidationError("C2 must exercise every declared replacement exactly once")
    for name in REQUIRED_REPLACEMENTS:
        replacement = _require_mapping(replacements[name], f"replacements.{name}")
        _require_exact_fields(
            replacement,
            frozenset({"exercised", "recovered", "acknowledged_loss", "continuity_observed", "observation"}),
            f"replacements.{name}",
        )
        _require_true_fields(replacement, f"replacements.{name}", ("exercised", "recovered", "continuity_observed"))
        if _require_integer(replacement["acknowledged_loss"], f"replacements.{name}.acknowledged_loss") != 0:
            raise EvidenceValidationError(f"C2 replacement {name} lost an acknowledged record")
        _validate_result_observation(
            replacement["observation"], f"replacements.{name}.observation", f"replace-{name}"
        )

    adapter_fault = _require_mapping(results["adapter_fault"], "results.adapter_fault")
    _require_exact_fields(
        adapter_fault,
        frozenset({"exercised", "profile_unchanged", "acknowledged_loss", "recovered", "observation"}),
        "results.adapter_fault",
    )
    _require_true_fields(adapter_fault, "results.adapter_fault", ("exercised", "profile_unchanged", "recovered"))
    if _require_integer(adapter_fault["acknowledged_loss"], "adapter_fault.acknowledged_loss") != 0:
        raise EvidenceValidationError("C2 adapter fault lost an acknowledged record")
    _validate_result_observation(
        adapter_fault["observation"], "adapter_fault.observation", "approved-adapter-fault"
    )
    _require_bool(results["console_continuity"], "console_continuity", True)
    otlp_configured = _require_bool(results["otlp_configured"], "otlp_configured")
    otlp_continuity = _require_bool(results["otlp_continuity"], "otlp_continuity")
    if otlp_configured and not otlp_continuity:
        raise EvidenceValidationError("configured OTLP continuity was not proved")
    _validate_result_observation(
        results["continuity_observation"], "continuity_observation", "continuity"
    )
    transition = _require_mapping(results["qualification_transition"], "qualification_transition")
    _require_exact_fields(
        transition,
        frozenset(
            {
                "non_production",
                "identity_observation",
                "initial_writes_state",
                "enable_observation",
                "disable_observation",
                "final_observation",
                "final_writes_state",
            }
        ),
        "qualification_transition",
    )
    _require_bool(transition["non_production"], "qualification_transition.non_production", True)
    _validate_result_observation(
        transition["identity_observation"],
        "qualification_transition.identity_observation",
        "qualification-target-identity",
    )
    if transition["initial_writes_state"] != "disabled":
        raise EvidenceValidationError("qualification target was not initially disabled")
    _validate_result_observation(
        transition["enable_observation"], "qualification_transition.enable_observation", "qualification-enable"
    )
    _validate_result_observation(
        transition["disable_observation"], "qualification_transition.disable_observation", "qualification-disable"
    )
    _validate_result_observation(
        transition["final_observation"],
        "qualification_transition.final_observation",
        "qualification-final-state",
    )
    if transition["final_writes_state"] != "disabled":
        raise EvidenceValidationError("qualification target was not restored to disabled")


def _validate_c3(results: Mapping[str, Any]) -> None:
    _require_exact_fields(
        results,
        frozenset(
            {
                "retention",
                "retention_observation",
                "retention_transition",
                "empty_preflight_observation",
                "final_newer_control",
                "cohorts",
            }
        ),
        "results",
    )

    def validate_segment_transition(value: Any, context: str, command_id: str) -> None:
        transition = _require_mapping(value, context)
        _require_exact_fields(
            transition,
            frozenset(
                {
                    "non_production",
                    "identity_observation",
                    "initial_writes_state",
                    "enable_observation",
                    "disable_observation",
                    "final_observation",
                    "final_writes_state",
                }
            ),
            context,
        )
        _require_bool(transition["non_production"], f"{context}.non_production", True)
        if transition["initial_writes_state"] != "disabled" or transition["final_writes_state"] != "disabled":
            raise EvidenceValidationError(f"{context} did not bound an exact disabled-to-disabled session")
        for field, suffix in (
            ("identity_observation", "qualification-target-identity"),
            ("enable_observation", "qualification-enable"),
            ("disable_observation", "qualification-disable"),
            ("final_observation", "qualification-final-state"),
        ):
            _validate_result_observation(
                transition[field], f"{context}.{field}", f"{command_id}:{suffix}"
            )
    retention = _require_mapping(results["retention"], "results.retention")
    _require_exact_fields(
        retention,
        frozenset(
            {
                "maximum_clock_delta_ms",
                "late_record_remaining_lifetime",
                "already_expired_rejected",
                "attestation_freshness_rejected",
                "attestation_replay_rejected",
                "attestation_identity_rejected",
                "logical_expiry_millisecond",
                "ttl_defense_in_depth",
            }
        ),
        "results.retention",
    )
    _require_integer(retention["maximum_clock_delta_ms"], "maximum_clock_delta_ms", maximum=1000)
    _require_true_fields(
        retention,
        "results.retention",
        (
            "late_record_remaining_lifetime",
            "already_expired_rejected",
            "attestation_freshness_rejected",
            "attestation_replay_rejected",
            "attestation_identity_rejected",
            "logical_expiry_millisecond",
            "ttl_defense_in_depth",
        ),
    )
    _validate_result_observation(
        results["retention_observation"], "retention_observation", "retention-controls"
    )
    validate_segment_transition(
        results["retention_transition"], "retention_transition", "retention-controls"
    )
    _validate_result_observation(
        results["empty_preflight_observation"],
        "empty_preflight_observation",
        "c3-empty-preflight",
    )
    final_control = _require_mapping(results["final_newer_control"], "results.final_newer_control")
    _require_exact_fields(
        final_control,
        frozenset({"record_count", "newer_record_names", "observation", "transition"}),
        "results.final_newer_control",
    )
    if _require_integer(final_control["record_count"], "final_newer_control.record_count") != 125:
        raise EvidenceValidationError("C3 final newer-record control must contain exactly 125 records")
    control_names = _require_sequence(
        final_control["newer_record_names"], "final_newer_control.newer_record_names"
    )
    if not control_names or any(
        not isinstance(name, str) or re.fullmatch(r"newer-control-[0-9]{3}", name) is None
        for name in control_names
    ):
        raise EvidenceValidationError("C3 final newer-record control aliases are not bounded")
    _validate_result_observation(
        final_control["observation"], "final_newer_control.observation", "newer-control-seed"
    )
    validate_segment_transition(
        final_control["transition"], "final_newer_control.transition", "newer-control-seed"
    )

    cohorts = _require_sequence(results["cohorts"], "results.cohorts")
    if len(cohorts) != 3:
        raise EvidenceValidationError("C3 requires exactly three independent retention cohorts")
    for index, hours in enumerate((1, 24, 168)):
        cohort = _require_mapping(cohorts[index], f"cohorts[{index}]")
        _require_exact_fields(
            cohort,
            frozenset(
                {
                    "retention_hours",
                    "cohort_id",
                    "database",
                    "schema",
                    "table",
                    "accepted_utc_ms",
                    "emitted_utc_ms",
                    "expires_utc_ms",
                    "purged_utc_ms",
                    "reclaimed_utc_ms",
                    "pre_tuple_count",
                    "post_tuple_count",
                    "candidate_count",
                    "deleted_count",
                    "already_absent_count",
                    "index_removal_count",
                    "logical_absence",
                    "newer_record_names",
                    "newer_records_preserved",
                    "interrupted_recovery",
                    "restart_recovery",
                    "allocator_free_bytes_before",
                    "allocator_free_bytes_after",
                    "os_disk_shrink_claimed",
                    "seed_observation",
                    "wait_observation",
                    "expiry_observation",
                    "purge_observation",
                    "reclamation_observation",
                    "seed_transition",
                    "purge_transition",
                    "reclamation_transition",
                }
            ),
            f"cohorts[{index}]",
        )
        if type(cohort["retention_hours"]) is not int or cohort["retention_hours"] != hours:
            raise EvidenceValidationError("C3 retention horizons must be exact integers 1, 24, and 168")
        if cohort["cohort_id"] != f"retention-{hours}h":
            raise EvidenceValidationError("C3 cohort identifiers are not the closed horizon inventory")
        if cohort["database"] != "memories_access_telemetry" or cohort["schema"] != "access_telemetry":
            raise EvidenceValidationError("C3 database/schema attribution differs from PG-ONPREM-1")
        _require_nonempty_string(cohort["table"], f"cohorts[{index}].table", maximum=128)
        accepted = _require_utc_milliseconds(cohort["accepted_utc_ms"], f"cohorts[{index}].accepted_utc_ms")
        emitted = _require_utc_milliseconds(cohort["emitted_utc_ms"], f"cohorts[{index}].emitted_utc_ms")
        expires = _require_utc_milliseconds(cohort["expires_utc_ms"], f"cohorts[{index}].expires_utc_ms")
        purged = _require_utc_milliseconds(cohort["purged_utc_ms"], f"cohorts[{index}].purged_utc_ms")
        reclaimed = _require_utc_milliseconds(cohort["reclaimed_utc_ms"], f"cohorts[{index}].reclaimed_utc_ms")
        if accepted > expires or expires - emitted != hours * 3_600_000:
            raise EvidenceValidationError("C3 cohort expiry does not equal its exact horizon")
        if not expires <= purged <= expires + 900_000 or not purged <= reclaimed <= purged + 86_400_000:
            raise EvidenceValidationError("C3 purge or physical-reclamation bound was not met")
        pre_count = _require_nonzero_integer(cohort["pre_tuple_count"], f"cohorts[{index}].pre_tuple_count")
        post_count = _require_integer(cohort["post_tuple_count"], f"cohorts[{index}].post_tuple_count")
        candidates = _require_nonzero_integer(cohort["candidate_count"], f"cohorts[{index}].candidate_count")
        deleted = _require_integer(cohort["deleted_count"], f"cohorts[{index}].deleted_count")
        absent = _require_integer(cohort["already_absent_count"], f"cohorts[{index}].already_absent_count")
        removed = _require_nonzero_integer(cohort["index_removal_count"], f"cohorts[{index}].index_removal_count")
        if deleted + absent != candidates or removed != candidates or pre_count - post_count != candidates:
            raise EvidenceValidationError("C3 cohort tuple and purge accounting is not exact")
        _require_true_fields(
            cohort,
            f"cohorts[{index}]",
            ("logical_absence", "newer_records_preserved", "interrupted_recovery", "restart_recovery"),
        )
        names = _require_sequence(cohort["newer_record_names"], f"cohorts[{index}].newer_record_names")
        if not names or any(not isinstance(name, str) or _SAFE_NAME.fullmatch(name) is None for name in names):
            raise EvidenceValidationError("C3 must name bounded newer-record fixtures")
        if hours == 168 and list(names) != list(control_names):
            raise EvidenceValidationError("C3 final horizon is not bound to the newer-record control")
        before = _require_integer(
            cohort["allocator_free_bytes_before"], f"cohorts[{index}].allocator_free_bytes_before"
        )
        after = _require_nonzero_integer(
            cohort["allocator_free_bytes_after"], f"cohorts[{index}].allocator_free_bytes_after"
        )
        if after <= before:
            raise EvidenceValidationError("C3 physical reclamation did not increase reusable free space")
        _require_bool(cohort["os_disk_shrink_claimed"], f"cohorts[{index}].os_disk_shrink_claimed", False)
        for observation_name in (
            "seed_observation",
            "wait_observation",
            "expiry_observation",
            "purge_observation",
            "reclamation_observation",
        ):
            stage = observation_name.removesuffix("_observation")
            _validate_result_observation(
                cohort[observation_name],
                f"cohorts[{index}].{observation_name}",
                f"cohort-{hours}h-{stage}",
            )
        for stage in ("seed", "purge", "reclamation"):
            validate_segment_transition(
                cohort[f"{stage}_transition"],
                f"cohorts[{index}].{stage}_transition",
                f"cohort-{hours}h-{stage}",
            )


def _validate_c4(results: Mapping[str, Any]) -> None:
    _require_exact_fields(
        results,
        frozenset({"failure_scenarios", "observability", "privacy", "qualification_transition"}),
        "results",
    )
    failures = _require_mapping(results["failure_scenarios"], "results.failure_scenarios")
    if set(failures) != set(REQUIRED_FAILURE_SCENARIOS):
        raise EvidenceValidationError("C4 must exercise every declared dependency failure exactly once")
    for name in REQUIRED_FAILURE_SCENARIOS:
        result = _require_mapping(failures[name], f"failure_scenarios.{name}")
        _require_exact_fields(
            result,
            frozenset(
                {
                    "exercised",
                    "lifecycle_fail_closed",
                    "business_readiness_available",
                    "business_requests",
                    "business_failures",
                    "audit_continuity",
                    "lifecycle_attempts",
                    "lifecycle_persisted",
                    "lifecycle_rejected",
                    "lifecycle_dropped",
                    "observation",
                }
            ),
            f"failure_scenarios.{name}",
        )
        _require_true_fields(
            result,
            f"failure_scenarios.{name}",
            ("exercised", "lifecycle_fail_closed", "business_readiness_available", "audit_continuity"),
        )
        _require_nonzero_integer(result["business_requests"], f"failure_scenarios.{name}.business_requests")
        if _require_integer(result["business_failures"], f"failure_scenarios.{name}.business_failures") != 0:
            raise EvidenceValidationError(f"C4 failure {name} changed business behavior")
        attempts = _require_nonzero_integer(
            result["lifecycle_attempts"], f"failure_scenarios.{name}.lifecycle_attempts"
        )
        accounted = sum(
            _require_integer(result[field], f"failure_scenarios.{name}.{field}")
            for field in ("lifecycle_persisted", "lifecycle_rejected", "lifecycle_dropped")
        )
        if attempts != accounted:
            raise EvidenceValidationError(f"C4 failure {name} lifecycle accounting is not exact")
        _validate_result_observation(
            result["observation"], f"failure_scenarios.{name}.observation", f"failure-{name}"
        )

    observability = _require_mapping(results["observability"], "results.observability")
    _require_exact_fields(
        observability,
        frozenset(
            {
                "signals",
                "labels",
                "alerts_passed",
                "bounded_labels",
                "health_precedence",
                "no_data_passed",
                "last_evidence_timestamp_gauge",
                "json_console_continuity",
                "otlp_configured",
                "otlp_continuity",
                "observation",
            }
        ),
        "results.observability",
    )
    signals = _require_sequence(observability["signals"], "observability.signals")
    if signals != list(REQUIRED_LIFECYCLE_SIGNALS):
        raise EvidenceValidationError("C4 lifecycle signals are incomplete or out of canonical order")
    labels = _require_sequence(observability["labels"], "observability.labels")
    if labels != ["state", "reason", "outcome"] or any(label in FORBIDDEN_LABELS for label in labels):
        raise EvidenceValidationError("C4 labels must be exactly state, reason, and outcome")
    _require_true_fields(
        observability,
        "results.observability",
        (
            "alerts_passed",
            "bounded_labels",
            "health_precedence",
            "no_data_passed",
            "last_evidence_timestamp_gauge",
            "json_console_continuity",
        ),
    )
    otlp_configured = _require_bool(observability["otlp_configured"], "observability.otlp_configured")
    otlp_continuity = _require_bool(observability["otlp_continuity"], "observability.otlp_continuity")
    if otlp_configured and not otlp_continuity:
        raise EvidenceValidationError("C4 configured OTLP continuity was not proved")
    _validate_result_observation(
        observability["observation"], "observability.observation", "observability"
    )

    privacy = _require_mapping(results["privacy"], "results.privacy")
    _require_exact_fields(
        privacy,
        frozenset(
            {
                "inspection_least_privilege",
                "no_tenant_read_route",
                "raw_values_absent",
                "secret_values_absent",
                "tenant_denial_before_dependencies",
                "dependency_calls_after_denial",
                "tenant_denial_tests",
                "observation",
            }
        ),
        "results.privacy",
    )
    _require_true_fields(
        privacy,
        "results.privacy",
        (
            "inspection_least_privilege",
            "no_tenant_read_route",
            "raw_values_absent",
            "secret_values_absent",
            "tenant_denial_before_dependencies",
        ),
    )
    if _require_integer(privacy["dependency_calls_after_denial"], "dependency_calls_after_denial") != 0:
        raise EvidenceValidationError("C4 tenant denial occurred after a dependency call")
    denial_tests = _require_sequence(privacy["tenant_denial_tests"], "tenant_denial_tests")
    if denial_tests != list(REQUIRED_TENANT_DENIAL_TESTS):
        raise EvidenceValidationError("C4 tenant/privacy denial tests are incomplete")
    _validate_result_observation(privacy["observation"], "privacy.observation", "privacy-denial")

    transition = _require_mapping(results["qualification_transition"], "qualification_transition")
    _require_exact_fields(
        transition,
        frozenset(
            {
                "non_production",
                "identity_observation",
                "initial_writes_state",
                "enable_observation",
                "disable_observation",
                "final_observation",
                "final_writes_state",
            }
        ),
        "qualification_transition",
    )
    _require_bool(transition["non_production"], "qualification_transition.non_production", True)
    _validate_result_observation(
        transition["identity_observation"],
        "qualification_transition.identity_observation",
        "qualification-target-identity",
    )
    if transition["initial_writes_state"] != "disabled":
        raise EvidenceValidationError("qualification target was not initially disabled")
    _validate_result_observation(
        transition["enable_observation"], "qualification_transition.enable_observation", "qualification-enable"
    )
    _validate_result_observation(
        transition["disable_observation"], "qualification_transition.disable_observation", "qualification-disable"
    )
    _validate_result_observation(
        transition["final_observation"],
        "qualification_transition.final_observation",
        "qualification-final-state",
    )
    if transition["final_writes_state"] != "disabled":
        raise EvidenceValidationError("qualification target was not restored to disabled")


def validate_story_27_4_checkpoint(
    checkpoint: str,
    payload: Mapping[str, Any],
    predecessor: Mapping[str, Any],
    *,
    repository_root: Path | None = None,
    evidence_root: Path | None = None,
) -> None:
    """Validate one complete, same-profile C2-C4 production checkpoint."""

    if checkpoint not in STORY_27_4_CHECKPOINTS:
        raise EvidenceValidationError(f"unsupported Story 27.4 checkpoint: {checkpoint}")
    _validate_secret_safe(payload)
    _validate_secret_safe(predecessor, "predecessor")
    _validate_predecessor(predecessor, repository_root, evidence_root)
    _validate_common_checkpoint(checkpoint, payload, repository_root)
    results = _require_mapping(payload["results"], "results")
    if checkpoint == "c2-production-replacement":
        _validate_c2(results)
    elif checkpoint == "c3-retention-reclamation":
        _validate_c3(results)
    else:
        _validate_c4(results)
    _validate_observation_bindings(payload)


def _bounded_reason(error: BaseException) -> str:
    reason = re.sub(r"\s+", " ", str(error)).strip()
    reason = _UNSAFE_EVIDENCE_VALUE.sub("[redacted]", reason)
    return reason[:512] or error.__class__.__name__


def _write_json_exclusive(
    path: Path,
    payload: Mapping[str, Any],
    *,
    maximum_bytes: int = _MAX_EVIDENCE_BYTES,
) -> None:
    if path.is_symlink():
        raise EvidenceValidationError(f"refusing symlink evidence output: {path}")
    path.parent.mkdir(parents=True, exist_ok=True)
    encoded = (_canonical_json(payload) + "\n").encode("utf-8")
    if not encoded or len(encoded) > maximum_bytes:
        raise EvidenceValidationError(
            f"evidence output must contain 1..{maximum_bytes} bytes"
        )
    try:
        with path.open("xb") as stream:
            stream.write(encoded)
    except FileExistsError as exc:
        raise EvidenceValidationError(f"immutable evidence path already exists: {path}") from exc


def _validated_evidence_root(evidence_root: Path, repository_root: Path) -> Path:
    if not evidence_root.is_absolute():
        raise EvidenceValidationError("evidence root must be an explicit absolute path")
    if evidence_root.is_symlink():
        raise EvidenceValidationError("evidence root must not be a symlink")
    root = evidence_root.resolve(strict=True)
    if evidence_root.absolute() != root:
        raise EvidenceValidationError("evidence root must not use a path alias")
    if not root.is_dir():
        raise EvidenceValidationError("evidence root must be an existing directory")
    repository = repository_root.resolve(strict=True)
    try:
        root.relative_to(repository)
    except ValueError:
        pass
    else:
        raise EvidenceValidationError("evidence root must be outside the repository")
    return root


def _require_evidence_path(path: Path, evidence_root: Path, name: str, *, must_exist: bool) -> Path:
    if path.is_symlink():
        raise EvidenceValidationError(f"{name} must not be a symlink")
    if any(part in {".", ".."} for part in path.parts):
        raise EvidenceValidationError(f"{name} must not use a path alias")
    candidate = path if path.is_absolute() else evidence_root / path
    absolute = candidate.absolute()
    resolved = candidate.resolve(strict=must_exist)
    if resolved != absolute:
        raise EvidenceValidationError(f"{name} must not use a path alias or symlink ancestor")
    try:
        relative = resolved.relative_to(evidence_root)
    except ValueError as exc:
        raise EvidenceValidationError(f"{name} escapes the evidence root") from exc
    if not relative.parts or any(part in {".", ".."} for part in relative.parts):
        raise EvidenceValidationError(f"{name} must be a canonical evidence-root path")
    return resolved


def _checkpoint_packet(
    checkpoint: str,
    status: str,
    owner: str,
    reason: str,
    *,
    observation: Mapping[str, Any] | None = None,
    evidence_mode: str = "offline-validation",
) -> dict[str, Any]:
    packet = {
        "schema_version": _STORY_PACKET_SCHEMA_VERSION,
        "producer": "verify-access-telemetry-lifecycle/v1",
        "checkpoint": checkpoint,
        "status": status,
        "evidence_mode": evidence_mode,
        "profile_sha256": STORY_27_4_PROFILE_SHA256,
        "owner": owner,
        "captured_utc": _utc_now(),
        "reason": reason,
        "observation": dict(observation or {}),
        "production_lifecycle_writes": "disabled",
        "a41_status": "open",
        "evidence_is_approval": False,
    }
    packet["packet_sha256"] = _sha256(_canonical_json(packet))
    return packet


def run_story_27_4_checkpoint(
    *,
    checkpoint: str,
    input_path: Path,
    predecessor_path: Path,
    evidence_path: Path,
    owner: str,
    repository_root: Path | None = None,
    evidence_root: Path | None = None,
    evidence_mode: str = "offline-validation",
) -> int:
    """Validate a producer-created packet and write immutable pass/rejection evidence.

    This entry point never contacts a target. The production CLI executes a reviewed
    producer first and passes its captured stdout through this validator; direct input
    use is the deterministic offline-validation seam.
    """

    try:
        normalized_owner = _require_nonempty_string(owner, "owner")
        if repository_root is None or evidence_root is None:
            raise EvidenceValidationError("checkpoint validation requires repository and external evidence roots")
        approved_root = _validated_evidence_root(evidence_root, repository_root)
        resolved_input = _require_evidence_path(input_path, approved_root, "input", must_exist=True)
        resolved_predecessor = _require_evidence_path(
            predecessor_path, approved_root, "predecessor", must_exist=True
        )
        resolved_evidence = _require_evidence_path(
            evidence_path, approved_root, "evidence output", must_exist=False
        )
        payload = _read_bounded_json(resolved_input, approved_root=approved_root)
        predecessor = _read_bounded_json(resolved_predecessor, approved_root=approved_root)
        validate_story_27_4_checkpoint(
            checkpoint,
            payload,
            predecessor,
            repository_root=repository_root,
            evidence_root=approved_root,
        )
        packet = _checkpoint_packet(
            checkpoint,
            "passed",
            normalized_owner,
            "producer-controlled checkpoint validated",
            observation={
                "input_sha256": hashlib.sha256(resolved_input.read_bytes()).hexdigest(),
                "producer_payload_sha256": _sha256(_canonical_json(payload)),
                "producer_payload": payload,
                "predecessor_sha256": hashlib.sha256(
                    resolved_predecessor.read_bytes()
                ).hexdigest(),
                "source_commit": payload["source_commit"],
                "result_count": payload["result_count"],
            },
            evidence_mode=evidence_mode,
        )
        _write_json_exclusive(resolved_evidence, packet)
        return 0
    except (EvidenceValidationError, OSError, ValueError) as exc:
        normalized_owner = owner.strip()[:256] if isinstance(owner, str) and owner.strip() else "unattributed"
        packet = _checkpoint_packet(
            checkpoint,
            "rejected",
            normalized_owner,
            _bounded_reason(exc),
            evidence_mode=evidence_mode,
        )
        try:
            rejection_path = evidence_path
            if repository_root is not None and evidence_root is not None:
                try:
                    approved_root = _validated_evidence_root(evidence_root, repository_root)
                    rejection_path = _require_evidence_path(
                        evidence_path, approved_root, "evidence output", must_exist=False
                    )
                except EvidenceValidationError:
                    return 1
            _write_json_exclusive(rejection_path, packet)
        except EvidenceValidationError:
            pass
        return 1


def _run_bounded_process(
    command: Sequence[str],
    *,
    cwd: Path,
    timeout_seconds: int,
    environment: Mapping[str, str] | None = None,
) -> tuple[int, bytes, bytes]:
    """Stream one child with deterministic limits and terminate on the first exceeded bound."""

    try:
        process = subprocess.Popen(
            tuple(command),
            cwd=cwd,
            stdin=subprocess.DEVNULL,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            env=environment,
        )
    except OSError as exc:
        raise EvidenceValidationError("reviewed producer could not be started") from exc
    stdout = bytearray()
    stderr = bytearray()
    selector = selectors.DefaultSelector()
    assert process.stdout is not None
    assert process.stderr is not None
    selector.register(process.stdout, selectors.EVENT_READ, (stdout, _MAX_EVIDENCE_BYTES, "stdout"))
    selector.register(process.stderr, selectors.EVENT_READ, (stderr, _MAX_STDERR_BYTES, "stderr"))
    deadline = time.monotonic() + timeout_seconds
    try:
        while selector.get_map():
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                raise EvidenceValidationError("reviewed producer exceeded its bounded timeout")
            for key, _ in selector.select(min(remaining, 0.25)):
                chunk = os.read(key.fileobj.fileno(), 8192)
                if not chunk:
                    selector.unregister(key.fileobj)
                    continue
                buffer, maximum, stream_name = key.data
                if len(buffer) + len(chunk) > maximum:
                    raise EvidenceValidationError(
                        f"reviewed producer {stream_name} exceeded {maximum} bytes"
                    )
                buffer.extend(chunk)
        return_code = process.wait(timeout=max(0.1, deadline - time.monotonic()))
        return return_code, bytes(stdout), bytes(stderr)
    except (OSError, subprocess.TimeoutExpired) as exc:
        raise EvidenceValidationError("reviewed producer execution failed or timed out") from exc
    finally:
        selector.close()
        if process.poll() is None:
            process.kill()
            process.wait(timeout=5)
        process.stdout.close()
        process.stderr.close()


def run_story_27_4_producer_checkpoint(
    *,
    checkpoint: str,
    scenario_input_path: Path,
    predecessor_path: Path,
    evidence_path: Path,
    owner: str,
    repository_root: Path,
    evidence_root: Path,
) -> int:
    """Execute the one repository-owned producer registered for a checkpoint."""

    temporary: Path | None = None
    resolved_evidence: Path | None = None
    producer_path: Path | None = None
    resolved_input: Path | None = None
    producer_authenticated = False
    try:
        normalized_owner = _require_nonempty_string(owner, "owner")
        registered = STORY_27_4_PRODUCERS.get(checkpoint)
        if registered is None:
            raise EvidenceValidationError("checkpoint has no registered producer")
        command_id, relative_producer = registered
        root = repository_root.resolve(strict=True)
        approved_root = _validated_evidence_root(evidence_root, root)
        resolved_input = _require_evidence_path(
            scenario_input_path, approved_root, "scenario input", must_exist=True
        )
        resolved_predecessor = _require_evidence_path(
            predecessor_path, approved_root, "predecessor", must_exist=True
        )
        resolved_evidence = _require_evidence_path(
            evidence_path, approved_root, "evidence output", must_exist=False
        )
        if resolved_evidence.exists() or resolved_evidence.is_symlink():
            raise EvidenceValidationError("immutable evidence output already exists")
        if _git_checked(root, "status", "--porcelain=v1", "--untracked-files=all"):
            raise EvidenceValidationError(
                "controlled producers require a clean tracked and untracked source worktree"
            )
        predecessor = _read_bounded_json(resolved_predecessor, approved_root=approved_root)
        _validate_predecessor(predecessor, root, approved_root)
        platform_operations_reviewer = next(
            _require_nonempty_string(item.get("reviewer"), "C1 platform-operations reviewer")
            for item in _require_sequence(predecessor.get("approvals"), "C1.approvals")
            if isinstance(item, Mapping) and item.get("role") == "platform-operations"
        )
        producer_path = root / relative_producer
        if not producer_path.is_file() or producer_path.is_symlink():
            raise EvidenceValidationError("registered producer is unavailable or is a symlink")
        source_commit = _git_checked(root, "rev-parse", "--verify", "HEAD^{commit}")
        source_sha256 = _hash_git_blob(root, source_commit, relative_producer)
        if hashlib.sha256(producer_path.read_bytes()).hexdigest() != source_sha256:
            raise EvidenceValidationError("registered producer worktree bytes differ from source HEAD")
        common_producer = "tools/access_telemetry_producer_common.py"
        common_sha256 = _hash_git_blob(root, source_commit, common_producer)
        if hashlib.sha256((root / common_producer).read_bytes()).hexdigest() != common_sha256:
            raise EvidenceValidationError("producer support bytes differ from source HEAD")
        verifier_source = "tools/verify_access_telemetry_lifecycle.py"
        verifier_sha256 = _hash_git_blob(root, source_commit, verifier_source)
        if hashlib.sha256((root / verifier_source).read_bytes()).hexdigest() != verifier_sha256:
            raise EvidenceValidationError("producer verifier bytes differ from source HEAD")
        producer_authenticated = True
        input_sha256 = hashlib.sha256(resolved_input.read_bytes()).hexdigest()
        started = _utc_now_milliseconds()
        producer_command = [
                sys.executable,
                "-B",
                str(producer_path),
                "--scenario-input",
                str(resolved_input),
                "--platform-operations-reviewer",
                platform_operations_reviewer,
        ]
        if checkpoint == "c3-retention-reclamation":
            producer_command.extend(
                ("--journal", str(approved_root / "c3-retention-reclamation.journal.jsonl"))
            )
        return_code, stdout, stderr = _run_bounded_process(
            tuple(producer_command),
            cwd=root,
            timeout_seconds=720_000 if checkpoint == "c3-retention-reclamation" else 50_400,
        )
        finished = _utc_now_milliseconds()
        if return_code != 0:
            raise EvidenceValidationError(
                f"reviewed producer exited {return_code}; stderr_sha256={hashlib.sha256(stderr).hexdigest()}"
            )
        temporary = approved_root / f".{resolved_evidence.name}.{os.getpid()}.producer.json"
        if temporary.exists() or temporary.is_symlink():
            raise EvidenceValidationError("temporary producer path already exists")
        with temporary.open("xb") as stream:
            stream.write(stdout)
        payload = _read_bounded_json(temporary, approved_root=approved_root)
        mutable = dict(payload)
        child_commands = _require_sequence(mutable.get("commands"), "commands")
        packet_started = started
        if checkpoint == "c3-retention-reclamation":
            # A resumed C3 packet includes the authenticated command prefix from
            # its exclusive journal.  Bind the wrapper to the complete observed
            # interval rather than pretending all multi-day observations were
            # made by this final process invocation.
            packet_started = min(
                [started]
                + [
                    _require_utc_milliseconds(
                        _require_mapping(command, f"commands[{index}]").get("started_utc_ms"),
                        f"commands[{index}].started_utc_ms",
                    )
                    for index, command in enumerate(child_commands)
                ]
            )
        arguments = {
            "scenario_input_sha256": input_sha256,
            "target_kind": "non-production-qualification",
        }
        mutable["source_commit"] = source_commit
        mutable["source_hashes"] = {
            relative_producer: source_sha256,
            common_producer: common_sha256,
            verifier_source: verifier_sha256,
        }
        mutable["producer"] = {
            "command_id": command_id,
            "path": relative_producer,
            "source_sha256": source_sha256,
            "arguments": arguments,
            "arguments_sha256": _sha256(_canonical_json(arguments)),
        }
        mutable["started_utc"] = packet_started
        mutable["finished_utc"] = finished
        mutable["commands"] = [
            {
                "command_id": command_id,
                "arguments": arguments,
                "arguments_sha256": _sha256(_canonical_json(arguments)),
                "started_utc_ms": started,
                "finished_utc_ms": finished,
                "exit_code": return_code,
                "stdout_sha256": hashlib.sha256(stdout).hexdigest(),
                "stderr_sha256": hashlib.sha256(stderr).hexdigest(),
                "result_count": _require_nonzero_integer(mutable.get("result_count"), "result_count"),
            },
            *child_commands,
        ]
        with temporary.open("wb") as stream:
            stream.write((_canonical_json(mutable) + "\n").encode("utf-8"))
        return run_story_27_4_checkpoint(
            checkpoint=checkpoint,
            input_path=temporary,
            predecessor_path=resolved_predecessor,
            evidence_path=resolved_evidence,
            owner=normalized_owner,
            repository_root=root,
            evidence_root=approved_root,
            evidence_mode="controlled-producer",
        )
    except (EvidenceValidationError, OSError, ValueError) as exc:
        if resolved_evidence is not None:
            disable_observation: dict[str, Any] = {}
            if producer_authenticated and producer_path is not None and resolved_input is not None:
                try:
                    disable_code, disable_stdout, disable_stderr = _run_bounded_process(
                        (
                            sys.executable,
                            "-B",
                            str(producer_path),
                            "--scenario-input",
                            str(resolved_input),
                            "--platform-operations-reviewer",
                            platform_operations_reviewer,
                            "--disable-only",
                        ),
                        cwd=repository_root,
                        timeout_seconds=60,
                    )
                    disable_observation = {
                        "disable_exit_code": disable_code,
                        "disable_stdout_sha256": hashlib.sha256(disable_stdout).hexdigest(),
                        "disable_stderr_sha256": hashlib.sha256(disable_stderr).hexdigest(),
                    }
                except EvidenceValidationError as disable_error:
                    disable_observation = {"disable_error": _bounded_reason(disable_error)}
            try:
                _write_json_exclusive(
                    resolved_evidence,
                    _checkpoint_packet(
                        checkpoint,
                        "rejected",
                        owner if isinstance(owner, str) and owner.strip() else "unattributed",
                        _bounded_reason(exc),
                        observation=disable_observation,
                        evidence_mode="controlled-producer",
                    ),
                )
            except EvidenceValidationError:
                pass
        return 1
    finally:
        if temporary is not None:
            try:
                temporary.unlink()
            except FileNotFoundError:
                pass


def _path_hash(path: Path) -> str | None:
    if not path.exists():
        return None
    if path.is_symlink() or not path.is_file():
        raise EvidenceValidationError(f"A41 inventory path must be a regular file: {path}")
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _read_git_blob(repository_root: Path, commit: str, relative: str) -> bytes | None:
    try:
        result = subprocess.run(
            ("git", "-C", str(repository_root), "show", f"{commit}:{relative}"),
            check=False,
            capture_output=True,
            timeout=30,
        )
    except subprocess.TimeoutExpired as exc:
        raise EvidenceValidationError(f"git blob read timed out for {relative}") from exc
    if result.returncode != 0:
        return None
    return result.stdout


def _collect_a41_inventory_and_blobs(
    repository_root: Path,
) -> tuple[dict[str, Any], dict[str, bytes | None]]:
    """Read each authenticated source-HEAD blob once for inventory and recovery."""

    root = repository_root.resolve(strict=True)
    head = _git_checked(root, "rev-parse", "--verify", "HEAD^{commit}")
    try:
        matched = subprocess.run(
            (
                "git",
                "-C",
                str(root),
                "grep",
                "-Il",
                "-e",
                "20.5-A41-ACCESS-TELEMETRY-RETENTION",
                "-e",
                "A41",
                head,
                "--",
                ":!references/**",
            ),
            check=False,
            capture_output=True,
            text=True,
            errors="strict",
            timeout=30,
        )
    except subprocess.TimeoutExpired as exc:
        raise EvidenceValidationError("A41 Git inventory scan timed out") from exc
    if matched.returncode not in {0, 1}:
        raise EvidenceValidationError("A41 Git inventory scan failed")
    tracked = [line.split(":", 1)[-1] for line in matched.stdout.splitlines() if line]
    candidates = set(A41_ALLOWED_MUTATION_PATHS) | set(A41_PROTECTED_PATHS)
    for relative in tracked:
        if relative.startswith("references/"):
            continue
        candidates.add(relative)

    references = []
    source_blobs: dict[str, bytes | None] = {}
    for relative in sorted(candidates):
        if relative in A41_ALLOWED_MUTATION_PATHS:
            classification = "close-out-mutable"
        elif relative in A41_PROTECTED_PATHS:
            classification = "historical-or-orchestrator-read-only"
        else:
            classification = "a41-reference-read-only"
        blob = _read_git_blob(root, head, relative)
        source_blobs[relative] = blob
        references.append(
            {
                "path": relative,
                "classification": classification,
                "sha256": hashlib.sha256(blob).hexdigest() if blob is not None else None,
            }
        )
    return (
        {
            "schema_version": 1,
            "source_head": head,
            "allowed_mutations": list(A41_ALLOWED_MUTATION_PATHS),
            "references": references,
        },
        source_blobs,
    )


def collect_a41_inventory(repository_root: Path) -> dict[str, Any]:
    """Return an exhaustive, hash-bound inventory of tracked A41 references."""

    inventory, _ = _collect_a41_inventory_and_blobs(repository_root)
    return inventory


def write_a41_inventory(repository_root: Path, evidence_path: Path, evidence_root: Path) -> int:
    try:
        approved_root = _validated_evidence_root(evidence_root, repository_root)
        resolved_evidence = _require_evidence_path(
            evidence_path, approved_root, "inventory output", must_exist=False
        )
        inventory = collect_a41_inventory(repository_root)
        _write_json_exclusive(resolved_evidence, inventory)
        return 0
    except (EvidenceValidationError, OSError) as exc:
        try:
            approved_root = _validated_evidence_root(evidence_root, repository_root)
            resolved_evidence = _require_evidence_path(
                evidence_path, approved_root, "inventory output", must_exist=False
            )
            _write_json_exclusive(
                resolved_evidence,
                _checkpoint_packet("a41-inventory", "rejected", "repository", _bounded_reason(exc)),
            )
        except EvidenceValidationError:
            pass
        return 1


def _validate_mutation_manifest(manifest: Mapping[str, Any]) -> Mapping[str, str]:
    _require_exact_fields(manifest, frozenset({"paths", "semantics"}), "mutation manifest")
    paths = _require_mapping(manifest["paths"], "mutation manifest paths")
    if tuple(sorted(paths)) != tuple(sorted(A41_ALLOWED_MUTATION_PATHS)):
        raise EvidenceValidationError("mutation manifest must name the exact complete A41 mutation set")
    for relative, digest in paths.items():
        _require_hex64(digest, f"mutation manifest path {relative}")
    semantics = _require_mapping(manifest["semantics"], "mutation manifest semantics")
    if set(semantics) != set(A41_ALLOWED_MUTATION_PATHS):
        raise EvidenceValidationError("mutation manifest semantics must cover every allowed path exactly")
    for relative, value in semantics.items():
        rule = _require_mapping(value, f"semantics[{relative}]")
        _require_exact_fields(rule, frozenset({"required", "forbidden"}), f"semantics[{relative}]")
        for field in ("required", "forbidden"):
            fragments = _require_sequence(rule[field], f"semantics[{relative}].{field}")
            if not fragments or any(not isinstance(item, str) or not item or len(item) > 512 for item in fragments):
                raise EvidenceValidationError(f"semantics[{relative}].{field} contains an invalid fragment")
            if tuple(fragments) != A41_SEMANTIC_TRANSITIONS[relative][field]:
                raise EvidenceValidationError(
                    f"semantics[{relative}].{field} differs from the verifier-owned transition"
                )
    return {str(path): str(digest) for path, digest in paths.items()}


def _validate_c0_adapter_profile(
    artifact_path: Path,
    repository_root: Path,
    evidence_root: Path,
) -> None:
    """Authenticate the full source-bound adapter-profile packet referenced by C0."""

    packet = _read_bounded_json(artifact_path, approved_root=evidence_root)
    _require_exact_fields(
        packet,
        _COMMON_CHECKPOINT_FIELDS |
        frozenset({"status", "production_lifecycle_writes", "packet_sha256"}),
        "C0 adapter-profile packet",
    )
    if packet.get("status") != "passed" or packet.get("production_lifecycle_writes") != "disabled":
        raise EvidenceValidationError("C0 adapter-profile packet is not a disabled-state pass")
    supplied_hash = _require_hex64(packet.get("packet_sha256"), "C0 adapter-profile packet_sha256")
    unhashed = dict(packet)
    del unhashed["packet_sha256"]
    if supplied_hash != _sha256(_canonical_json(unhashed)):
        raise EvidenceValidationError("C0 adapter-profile packet hash mismatch")
    core = dict(packet)
    del core["status"]
    del core["production_lifecycle_writes"]
    del core["packet_sha256"]
    _validate_common_checkpoint("adapter-profile", core, repository_root)
    results = _require_mapping(packet.get("results"), "C0 adapter-profile results")
    _require_exact_fields(
        results,
        frozenset(
            {
                "profile_id",
                "profile_complete",
                "runtime_matches_reviewed_profile",
                "immutable_artifacts",
            }
        ),
        "C0 adapter-profile results",
    )
    if results["profile_id"] != EXPECTED_PROFILE_ID:
        raise EvidenceValidationError("C0 adapter-profile ID differs from PG-ONPREM-1")
    _require_bool(results["profile_complete"], "C0.profile_complete", True)
    _require_bool(
        results["runtime_matches_reviewed_profile"],
        "C0.runtime_matches_reviewed_profile",
        True,
    )
    immutable_artifacts = _require_mapping(
        results["immutable_artifacts"], "C0.immutable_artifacts"
    )
    if not immutable_artifacts:
        raise EvidenceValidationError("C0 requires immutable adapter-profile observations")
    seen_paths: set[str] = set()
    for name, value in immutable_artifacts.items():
        _require_nonempty_string(name, "C0 immutable artifact name", maximum=128)
        item = _require_mapping(value, f"C0.immutable_artifacts[{name!r}]")
        _require_exact_fields(
            item,
            frozenset({"path", "sha256"}),
            f"C0.immutable_artifacts[{name!r}]",
        )
        relative = _require_nonempty_string(
            item["path"], f"C0.immutable_artifacts[{name!r}].path", maximum=512
        ).replace("\\", "/")
        if (
            Path(relative).is_absolute()
            or ".." in Path(relative).parts
            or relative.startswith("./")
            or relative in seen_paths
        ):
            raise EvidenceValidationError("C0 immutable artifact paths must be unique and evidence-root relative")
        seen_paths.add(relative)
        immutable_path = _require_evidence_path(
            Path(relative), evidence_root, f"C0 immutable artifact {name}", must_exist=True
        )
        if hashlib.sha256(immutable_path.read_bytes()).hexdigest() != _require_hex64(
            item["sha256"], f"C0.immutable_artifacts[{name!r}].sha256"
        ):
            raise EvidenceValidationError(f"C0 immutable artifact hash mismatch for {name}")


def _validate_terminal_bundle(
    bundle: Mapping[str, Any],
    repository_root: Path,
    evidence_root: Path,
) -> None:
    aggregate_budget = _EvidenceAggregateBudget(evidence_root)

    _require_exact_fields(bundle, frozenset({"profile_sha256", "checkpoints"}), "terminal bundle")
    if bundle["profile_sha256"] != STORY_27_4_PROFILE_SHA256:
        raise EvidenceValidationError("terminal bundle profile drifted from PG-ONPREM-1")
    checkpoints = _require_mapping(bundle["checkpoints"], "terminal bundle checkpoints")
    expected = {"C0", "C1", "C2", "C3", "C4", "C5", "C6", "terminal"}
    if set(checkpoints) != expected:
        raise EvidenceValidationError("terminal bundle must contain exactly C0-C6 and terminal validation")
    c1_artifact_payload: Mapping[str, Any] | None = None
    behavioral_artifacts: dict[str, Mapping[str, Any]] = {}
    artifact_hashes: dict[str, str] = {}
    chain_payloads: dict[str, Mapping[str, Any]] = {}
    for name in sorted(expected):
        item = _require_mapping(checkpoints[name], f"terminal bundle {name}")
        _require_exact_fields(
            item,
            frozenset({"status", "profile_sha256", "artifact_path", "artifact_sha256"}),
            f"terminal bundle {name}",
        )
        if item.get("status") != "passed" or item.get("profile_sha256") != STORY_27_4_PROFILE_SHA256:
            raise EvidenceValidationError(f"terminal bundle checkpoint {name} is not a same-profile pass")
        artifact_path = _require_nonempty_string(
            item.get("artifact_path"), f"{name}.artifact_path", maximum=512
        ).replace("\\", "/")
        if (
            Path(artifact_path).is_absolute()
            or ".." in Path(artifact_path).parts
            or artifact_path.startswith("./")
        ):
            raise EvidenceValidationError(f"terminal artifact {name} path must be evidence-root relative")
        artifact_sha = _require_hex64(item.get("artifact_sha256"), f"{name}.artifact_sha256")
        artifact = aggregate_budget.account(evidence_root / artifact_path, f"terminal artifact {name}")
        if hashlib.sha256(artifact.read_bytes()).hexdigest() != artifact_sha:
            raise EvidenceValidationError(f"terminal bundle checkpoint {name} artifact hash mismatch")
        artifact_payload = _read_bounded_json(artifact, approved_root=evidence_root)
        artifact_hashes[name] = artifact_sha
        chain_payloads[name] = artifact_payload
        expected_checkpoint = {
            "C0": {"C0", "adapter-profile"},
            "C1": {"C1"},
            "C2": {"C2", "c2-production-replacement"},
            "C3": {"C3", "c3-retention-reclamation"},
            "C4": {"C4", "c4-failure-privacy-observability"},
            "C5": {"C5", "operations-acceptance"},
            "C6": {"C6", "security-acceptance"},
            "terminal": {"terminal", "terminal-validation"},
        }[name]
        if artifact_payload.get("checkpoint") not in expected_checkpoint:
            raise EvidenceValidationError(f"terminal artifact {name} carries another checkpoint")
        if artifact_payload.get("status") != "passed":
            raise EvidenceValidationError(f"terminal artifact {name} is not passing by content")
        if artifact_payload.get("profile_sha256") != STORY_27_4_PROFILE_SHA256:
            raise EvidenceValidationError(f"terminal artifact {name} profile differs by content")
        if name == "C1":
            _validate_predecessor(artifact_payload, repository_root, evidence_root)
            gate_items: Sequence[Any]
            if isinstance(artifact_payload.get("gates"), Mapping):
                gate_items = list(artifact_payload["gates"].values())
            else:
                gate_items = _require_sequence(
                    artifact_payload.get("successors"),
                    "C1.successors",
                )
            for index, gate_value in enumerate(gate_items):
                gate = _require_mapping(gate_value, f"C1 artifact[{index}]")
                relative = _require_nonempty_string(
                    gate.get("artifact_path"),
                    f"C1 artifact[{index}].artifact_path",
                    maximum=512,
                ).replace("\\", "/")
                aggregate_budget.account(
                    evidence_root / relative,
                    f"C1 artifact[{index}]",
                )
            c1_artifact_payload = artifact_payload
        elif name in {"C2", "C3", "C4"}:
            _require_exact_fields(
                artifact_payload,
                frozenset(
                    {
                        "schema_version",
                        "producer",
                        "checkpoint",
                        "status",
                        "evidence_mode",
                        "profile_sha256",
                        "owner",
                        "captured_utc",
                        "reason",
                        "observation",
                        "production_lifecycle_writes",
                        "a41_status",
                        "evidence_is_approval",
                        "packet_sha256",
                    }
                ),
                f"terminal artifact {name}",
            )
            if (
                artifact_payload.get("schema_version") != _STORY_PACKET_SCHEMA_VERSION
                or artifact_payload.get("producer") != "verify-access-telemetry-lifecycle/v1"
                or artifact_payload.get("evidence_mode") != "controlled-producer"
                or artifact_payload.get("a41_status") != "open"
                or artifact_payload.get("production_lifecycle_writes") != "disabled"
                or artifact_payload.get("evidence_is_approval") is not False
            ):
                raise EvidenceValidationError(
                    f"terminal artifact {name} is not controlled-producer evidence"
                )
            supplied_packet_hash = _require_hex64(
                artifact_payload.get("packet_sha256"), f"terminal artifact {name}.packet_sha256"
            )
            unhashed_artifact = dict(artifact_payload)
            del unhashed_artifact["packet_sha256"]
            if supplied_packet_hash != _sha256(_canonical_json(unhashed_artifact)):
                raise EvidenceValidationError(f"terminal artifact {name} packet hash mismatch")
            observation = _require_mapping(
                artifact_payload.get("observation"), f"terminal artifact {name}.observation"
            )
            _require_exact_fields(
                observation,
                frozenset(
                    {
                        "input_sha256",
                        "producer_payload_sha256",
                        "producer_payload",
                        "predecessor_sha256",
                        "source_commit",
                        "result_count",
                    }
                ),
                f"terminal artifact {name}.observation",
            )
            _require_hex64(observation.get("input_sha256"), f"terminal artifact {name}.input_sha256")
            _require_hex64(
                observation.get("predecessor_sha256"),
                f"terminal artifact {name}.predecessor_sha256",
            )
            _require_nonempty_string(artifact_payload.get("owner"), f"terminal artifact {name}.owner")
            _require_nonempty_string(artifact_payload.get("reason"), f"terminal artifact {name}.reason")
            captured = int(
                _parse_utc(
                    artifact_payload.get("captured_utc"),
                    f"terminal artifact {name}.captured_utc",
                ).timestamp()
                * 1000
            )
            _validate_fresh_run(captured, captured, f"terminal artifact {name}")
            payload = _require_mapping(
                observation.get("producer_payload"), f"terminal artifact {name}.producer_payload"
            )
            if observation.get("producer_payload_sha256") != _sha256(_canonical_json(payload)):
                raise EvidenceValidationError(f"terminal artifact {name} producer payload hash mismatch")
            behavioral_artifacts[name] = payload
    if c1_artifact_payload is None:
        raise EvidenceValidationError("terminal bundle has no authenticated C1 artifact")
    for name in ("C2", "C3", "C4"):
        observation = _require_mapping(
            chain_payloads[name].get("observation"),
            f"terminal artifact {name}.observation",
        )
        if observation.get("predecessor_sha256") != artifact_hashes["C1"]:
            raise EvidenceValidationError(
                f"terminal artifact {name} does not bind the bundled C1 predecessor"
            )
    for name, checkpoint in (
        ("C2", "c2-production-replacement"),
        ("C3", "c3-retention-reclamation"),
        ("C4", "c4-failure-privacy-observability"),
    ):
        validate_story_27_4_checkpoint(
            checkpoint,
            behavioral_artifacts[name],
            c1_artifact_payload,
            repository_root=repository_root,
            evidence_root=evidence_root,
        )
    for name in ("C0", "C5", "C6", "terminal"):
        artifact_payload = chain_payloads[name]
        core = dict(artifact_payload)
        status = core.pop("status", None)
        if status != "passed":
            raise EvidenceValidationError(f"terminal artifact {name} is not passing")
        _validate_common_checkpoint(name, core, repository_root)
    c0_results = _require_mapping(chain_payloads["C0"].get("results"), "C0.results")
    _require_exact_fields(
        c0_results,
        frozenset({"adapter_profile_path", "adapter_profile_sha256"}),
        "C0.results",
    )
    adapter_relative = _require_nonempty_string(
        c0_results["adapter_profile_path"], "C0.adapter_profile_path", maximum=512
    ).replace("\\", "/")
    if (
        Path(adapter_relative).is_absolute()
        or ".." in Path(adapter_relative).parts
        or adapter_relative.startswith("./")
    ):
        raise EvidenceValidationError("C0 adapter-profile path must be evidence-root relative")
    adapter_artifact = aggregate_budget.account(
        evidence_root / adapter_relative,
        "C0 adapter-profile artifact",
    )
    if hashlib.sha256(adapter_artifact.read_bytes()).hexdigest() != _require_hex64(
        c0_results["adapter_profile_sha256"], "C0.adapter_profile_sha256"
    ):
        raise EvidenceValidationError("C0 adapter-profile artifact hash mismatch")
    _validate_c0_adapter_profile(adapter_artifact, repository_root, evidence_root)
    adapter_packet = _read_bounded_json(adapter_artifact, approved_root=evidence_root)
    adapter_results = _require_mapping(adapter_packet.get("results"), "C0 adapter-profile results")
    immutable_artifacts = _require_mapping(
        adapter_results.get("immutable_artifacts"),
        "C0 immutable artifacts",
    )
    for name, value in immutable_artifacts.items():
        item = _require_mapping(value, f"C0 immutable artifact {name}")
        relative = _require_nonempty_string(
            item.get("path"),
            f"C0 immutable artifact {name}.path",
            maximum=512,
        ).replace("\\", "/")
        aggregate_budget.account(
            evidence_root / relative,
            f"C0 immutable artifact {name}",
        )

    c1_reviewers = {
        _require_nonempty_string(item.get("reviewer"), "C1 approval reviewer")
        for item in _require_sequence(c1_artifact_payload.get("approvals"), "C1.approvals")
        if isinstance(item, Mapping)
    }
    post_reviewers: set[str] = set()
    expected_roles = {"C5": "platform-operations", "C6": "security"}
    expected_acceptance_hashes = {name: artifact_hashes[name] for name in ("C0", "C1", "C2", "C3", "C4")}
    for name, role in expected_roles.items():
        approval = _require_mapping(chain_payloads[name].get("results"), f"{name}.results")
        _require_exact_fields(
            approval,
            frozenset({"role", "approver", "approved_utc_ms", "accepted_checkpoint_hashes"}),
            f"{name}.results",
        )
        if approval["role"] != role:
            raise EvidenceValidationError(f"{name} has the wrong approval role")
        approver = _require_nonempty_string(approval["approver"], f"{name}.approver")
        if approver in c1_reviewers or approver in post_reviewers:
            raise EvidenceValidationError("post-evidence approvals must be mutually independent and new")
        post_reviewers.add(approver)
        approved = _require_utc_milliseconds(approval["approved_utc_ms"], f"{name}.approved_utc_ms")
        _validate_fresh_run(approved, approved, f"{name} approval")
        accepted = _require_mapping(
            approval["accepted_checkpoint_hashes"], f"{name}.accepted_checkpoint_hashes"
        )
        if dict(accepted) != expected_acceptance_hashes:
            raise EvidenceValidationError(f"{name} approval does not bind the exact C0-C4 artifacts")

    terminal = _require_mapping(chain_payloads["terminal"].get("results"), "terminal.results")
    _require_exact_fields(
        terminal,
        frozenset({"checkpoint_hashes", "failure_count", "skip_count", "result_count"}),
        "terminal.results",
    )
    checkpoint_hashes = _require_mapping(terminal["checkpoint_hashes"], "terminal.checkpoint_hashes")
    if dict(checkpoint_hashes) != {name: artifact_hashes[name] for name in ("C0", "C1", "C2", "C3", "C4", "C5", "C6")}:
        raise EvidenceValidationError("terminal validation does not bind the exact C0-C6 artifacts")
    if _require_integer(terminal["failure_count"], "terminal.failure_count") != 0:
        raise EvidenceValidationError("terminal validation contains failures")
    if _require_integer(terminal["skip_count"], "terminal.skip_count") != 0:
        raise EvidenceValidationError("terminal validation contains skips")
    _require_nonzero_integer(terminal["result_count"], "terminal.result_count")


def create_recoverable_snapshot(
    repository_root: Path,
    snapshot_path: Path,
    inventory: Mapping[str, Any],
    source_blobs: Mapping[str, bytes | None] | None = None,
) -> dict[str, Any]:
    """Write the exact pre-mutation A41 bytes to an exclusive recovery packet."""

    root = repository_root.resolve(strict=True)
    encoded_paths: dict[str, Any] = {}
    inventory_hashes = {
        item["path"]: item["sha256"]
        for item in _require_sequence(inventory.get("references"), "inventory.references")
        if isinstance(item, Mapping)
    }
    for relative in A41_ALLOWED_MUTATION_PATHS:
        if source_blobs is not None and relative not in source_blobs:
            raise EvidenceValidationError(
                f"authenticated source blob is missing from the snapshot input: {relative}"
            )
        data = (
            source_blobs.get(relative)
            if source_blobs is not None
            else _read_git_blob(root, inventory["source_head"], relative)
        )
        if data is not None:
            digest = hashlib.sha256(data).hexdigest()
            if inventory_hashes.get(relative) != digest:
                raise EvidenceValidationError(f"A41 inventory/snapshot hash mismatch for {relative}")
            encoded_paths[relative] = {
                "exists": True,
                "sha256": digest,
                "content_base64": base64.b64encode(data).decode("ascii"),
            }
        else:
            encoded_paths[relative] = {"exists": False, "sha256": None, "content_base64": None}
    snapshot = {
        "schema_version": 1,
        "source_head": inventory["source_head"],
        "paths": encoded_paths,
    }
    if len((_canonical_json(snapshot) + "\n").encode("utf-8")) > _MAX_SNAPSHOT_BYTES:
        raise EvidenceValidationError("recoverable snapshot exceeds the 4 MiB aggregate bound")
    _write_json_exclusive(snapshot_path, snapshot, maximum_bytes=_MAX_SNAPSHOT_BYTES)
    return snapshot


def _close_out_packet(checkpoint: str, status: str, reason: str, **fields: Any) -> dict[str, Any]:
    packet = {
        "schema_version": 1,
        "producer": "verify-access-telemetry-lifecycle/v1",
        "checkpoint": checkpoint,
        "status": status,
        "profile_sha256": STORY_27_4_PROFILE_SHA256,
        "captured_utc": _utc_now(),
        "reason": reason,
        "a41_status": "open" if status != "published" else "published-close-out-verified",
        "production_lifecycle_writes": "disabled",
    }
    packet.update(fields)
    packet["packet_sha256"] = _sha256(_canonical_json(packet))
    return packet


def _validate_close_out_packet_hash(packet: Mapping[str, Any], name: str) -> None:
    supplied = _require_hex64(packet.get("packet_sha256"), f"{name}.packet_sha256")
    unhashed = dict(packet)
    del unhashed["packet_sha256"]
    if supplied != _sha256(_canonical_json(unhashed)):
        raise EvidenceValidationError(f"{name} packet hash mismatch")


def _write_close_out_rejection(evidence_path: Path, checkpoint: str, error: BaseException) -> int:
    try:
        _write_json_exclusive(
            evidence_path,
            _close_out_packet(checkpoint, "rejected", _bounded_reason(error)),
        )
    except EvidenceValidationError:
        pass
    return 1


def _assert_clean_repository(repository_root: Path) -> str:
    status = _git_checked(repository_root, "status", "--porcelain=v1", "--untracked-files=all")
    if status:
        raise EvidenceValidationError("close-out preflight requires a clean index and worktree")
    return _git_checked(repository_root, "rev-parse", "--verify", "HEAD^{commit}")


def run_close_out_preflight(
    *,
    repository_root: Path,
    bundle_path: Path,
    mutation_manifest_path: Path,
    snapshot_path: Path,
    evidence_path: Path,
    evidence_root: Path,
    remote: str,
    branch: str,
) -> int:
    """Authenticate terminal inputs at a clean open commit and snapshot A41 bytes."""

    try:
        root = repository_root.resolve(strict=True)
        approved_root = _validated_evidence_root(evidence_root, root)
        resolved_bundle = _require_evidence_path(bundle_path, approved_root, "terminal bundle", must_exist=True)
        resolved_manifest = _require_evidence_path(
            mutation_manifest_path, approved_root, "mutation manifest", must_exist=True
        )
        resolved_snapshot = _require_evidence_path(snapshot_path, approved_root, "snapshot", must_exist=False)
        resolved_evidence = _require_evidence_path(evidence_path, approved_root, "preflight output", must_exist=False)
        if resolved_snapshot.exists() or resolved_snapshot.is_symlink():
            raise EvidenceValidationError("recoverable snapshot path already exists")
        head = _assert_clean_repository(root)
        bundle = _read_bounded_json(resolved_bundle, approved_root=approved_root)
        manifest = _read_bounded_json(resolved_manifest, approved_root=approved_root)
        _validate_terminal_bundle(bundle, root, approved_root)
        _validate_mutation_manifest(manifest)
        normalized_branch = _require_nonempty_string(branch, "branch", maximum=256)
        if normalized_branch != _git_checked(root, "branch", "--show-current"):
            raise EvidenceValidationError("intended close-out branch is not the current branch")
        normalized_remote = _require_git_remote(remote)
        if normalized_remote not in _git_checked(root, "remote").splitlines():
            raise EvidenceValidationError("intended close-out remote is not configured")
        remote_url = _git_checked(root, "remote", "get-url", normalized_remote).strip().rstrip("/")
        if not remote_url:
            raise EvidenceValidationError("intended close-out remote has no configured URL")
        inventory, source_blobs = _collect_a41_inventory_and_blobs(root)
        if inventory["source_head"] != head:
            raise EvidenceValidationError("A41 inventory head changed during preflight")
        create_recoverable_snapshot(root, resolved_snapshot, inventory, source_blobs)
        if _assert_clean_repository(root) != head:
            raise EvidenceValidationError("repository changed while close-out preflight was captured")
        packet = _close_out_packet(
            "close-out-preflight",
            "passed",
            "terminal bundle, approvals, inventory, and recovery snapshot authenticated",
            source_head=head,
            branch=normalized_branch,
            remote=normalized_remote,
            remote_url_sha256=_sha256(remote_url),
            bundle_sha256=hashlib.sha256(resolved_bundle.read_bytes()).hexdigest(),
            mutation_manifest_sha256=hashlib.sha256(
                resolved_manifest.read_bytes()
            ).hexdigest(),
            inventory_sha256=_sha256(_canonical_json(inventory)),
            inventory=inventory,
            snapshot_sha256=hashlib.sha256(resolved_snapshot.read_bytes()).hexdigest(),
            allowed_mutations=list(A41_ALLOWED_MUTATION_PATHS),
        )
        _write_json_exclusive(resolved_evidence, packet)
        return 0
    except (EvidenceValidationError, OSError, ValueError) as exc:
        try:
            root = repository_root.resolve(strict=True)
            approved_root = _validated_evidence_root(evidence_root, root)
            resolved_evidence = _require_evidence_path(
                evidence_path, approved_root, "preflight output", must_exist=False
            )
        except EvidenceValidationError:
            return 1
        return _write_close_out_rejection(resolved_evidence, "close-out-preflight", exc)


def _authenticate_preflight(
    preflight_path: Path,
    manifest_path: Path,
    evidence_root: Path,
) -> tuple[Mapping[str, Any], Mapping[str, Any], str]:
    preflight = _read_bounded_json(preflight_path, approved_root=evidence_root)
    manifest = _read_bounded_json(manifest_path, approved_root=evidence_root)
    _validate_mutation_manifest(manifest)
    _validate_close_out_packet_hash(preflight, "preflight")
    _require_exact_fields(
        preflight,
        frozenset(
            {
                "schema_version",
                "producer",
                "checkpoint",
                "status",
                "profile_sha256",
                "captured_utc",
                "reason",
                "a41_status",
                "production_lifecycle_writes",
                "source_head",
                "branch",
                "remote",
                "remote_url_sha256",
                "bundle_sha256",
                "mutation_manifest_sha256",
                "inventory_sha256",
                "inventory",
                "snapshot_sha256",
                "allowed_mutations",
                "packet_sha256",
            }
        ),
        "preflight packet",
    )
    if (
        preflight.get("producer") != "verify-access-telemetry-lifecycle/v1"
        or preflight.get("checkpoint") != "close-out-preflight"
        or preflight.get("status") != "passed"
        or preflight.get("profile_sha256") != STORY_27_4_PROFILE_SHA256
    ):
        raise EvidenceValidationError("preflight packet is not authentic or passing")
    if preflight.get("a41_status") != "open" or preflight.get("production_lifecycle_writes") != "disabled":
        raise EvidenceValidationError("preflight packet altered A41 or Production state")
    if preflight.get("allowed_mutations") != list(A41_ALLOWED_MUTATION_PATHS):
        raise EvidenceValidationError("preflight packet mutation set is not exact")
    inventory = _require_mapping(preflight.get("inventory"), "preflight.inventory")
    if preflight.get("inventory_sha256") != _sha256(_canonical_json(inventory)):
        raise EvidenceValidationError("preflight inventory hash mismatch")
    _require_nonempty_string(preflight.get("remote"), "preflight.remote", maximum=128)
    _require_hex64(preflight.get("remote_url_sha256"), "preflight.remote_url_sha256")
    manifest_sha = hashlib.sha256(_safe_input_path(manifest_path, approved_root=evidence_root).read_bytes()).hexdigest()
    if preflight.get("mutation_manifest_sha256") != manifest_sha:
        raise EvidenceValidationError("mutation manifest differs from the preflight-approved bytes")
    preflight_sha = hashlib.sha256(_safe_input_path(preflight_path, approved_root=evidence_root).read_bytes()).hexdigest()
    return preflight, manifest, preflight_sha


def _authenticate_snapshot(
    snapshot_path: Path,
    preflight: Mapping[str, Any],
    evidence_root: Path,
) -> Mapping[str, Any]:
    snapshot = _read_bounded_json(
        snapshot_path,
        approved_root=evidence_root,
        maximum_bytes=_MAX_SNAPSHOT_BYTES,
        maximum_string_length=_MAX_SNAPSHOT_BYTES,
    )
    snapshot_sha = hashlib.sha256(
        _safe_input_path(snapshot_path, approved_root=evidence_root).read_bytes()
    ).hexdigest()
    if snapshot_sha != preflight.get("snapshot_sha256"):
        raise EvidenceValidationError("recoverable snapshot differs from preflight-approved bytes")
    _require_exact_fields(snapshot, frozenset({"schema_version", "source_head", "paths"}), "snapshot")
    if snapshot.get("schema_version") != 1 or snapshot.get("source_head") != preflight.get("source_head"):
        raise EvidenceValidationError("recoverable snapshot source identity mismatch")
    paths = _require_mapping(snapshot.get("paths"), "snapshot.paths")
    if set(paths) != set(A41_ALLOWED_MUTATION_PATHS):
        raise EvidenceValidationError("recoverable snapshot does not cover the exact mutation set")
    for relative, value in paths.items():
        item = _require_mapping(value, f"snapshot.paths[{relative}]")
        _require_exact_fields(item, frozenset({"exists", "sha256", "content_base64"}), f"snapshot.paths[{relative}]")
        exists = _require_bool(item.get("exists"), f"snapshot.paths[{relative}].exists")
        if not exists:
            if item.get("sha256") is not None or item.get("content_base64") is not None:
                raise EvidenceValidationError(f"absent snapshot path carries content: {relative}")
            continue
        digest = _require_hex64(item.get("sha256"), f"snapshot.paths[{relative}].sha256")
        encoded = _require_nonempty_string(
            item.get("content_base64"), f"snapshot.paths[{relative}].content_base64", maximum=_MAX_SNAPSHOT_BYTES
        )
        try:
            content = base64.b64decode(encoded, validate=True)
        except (ValueError, binascii.Error) as exc:
            raise EvidenceValidationError(f"snapshot path has invalid base64: {relative}") from exc
        if hashlib.sha256(content).hexdigest() != digest:
            raise EvidenceValidationError(f"snapshot path content hash mismatch: {relative}")
    return snapshot


def _staged_blob_hash(repository_root: Path, relative: str) -> str:
    result = subprocess.run(
        ("git", "-C", str(repository_root), "show", f":{relative}"),
        check=False,
        capture_output=True,
        timeout=30,
    )
    if result.returncode != 0:
        raise EvidenceValidationError(f"allowed A41 path is absent from the index: {relative}")
    return hashlib.sha256(result.stdout).hexdigest()


def _validate_manifest_semantics(
    repository_root: Path,
    manifest: Mapping[str, Any],
) -> None:
    semantics = _require_mapping(manifest["semantics"], "mutation manifest semantics")
    for relative in A41_ALLOWED_MUTATION_PATHS:
        try:
            text = (repository_root / relative).read_text(encoding="utf-8", errors="strict")
        except (OSError, UnicodeError) as exc:
            raise EvidenceValidationError(f"close-out path is not canonical UTF-8: {relative}") from exc
        rule = _require_mapping(semantics[relative], f"semantics[{relative}]")
        for fragment in _require_sequence(rule["required"], f"semantics[{relative}].required"):
            if fragment not in text:
                raise EvidenceValidationError(f"approved close-out transition is absent from {relative}")
        for fragment in _require_sequence(rule["forbidden"], f"semantics[{relative}].forbidden"):
            if fragment in text:
                raise EvidenceValidationError(f"forbidden open-state transition remains in {relative}")


def run_close_out_postflight(
    *,
    repository_root: Path,
    preflight_path: Path,
    mutation_manifest_path: Path,
    snapshot_path: Path,
    evidence_path: Path,
    evidence_root: Path,
) -> int:
    """Bind the actual index and worktree to an authentic clean-open preflight."""

    try:
        root = repository_root.resolve(strict=True)
        approved_root = _validated_evidence_root(evidence_root, root)
        resolved_preflight = _require_evidence_path(
            preflight_path, approved_root, "preflight", must_exist=True
        )
        resolved_manifest = _require_evidence_path(
            mutation_manifest_path, approved_root, "mutation manifest", must_exist=True
        )
        resolved_snapshot = _require_evidence_path(snapshot_path, approved_root, "snapshot", must_exist=True)
        resolved_evidence = _require_evidence_path(
            evidence_path, approved_root, "postflight output", must_exist=False
        )
        preflight, manifest, preflight_sha = _authenticate_preflight(
            resolved_preflight, resolved_manifest, approved_root
        )
        _authenticate_snapshot(resolved_snapshot, preflight, approved_root)
        head = _git_checked(root, "rev-parse", "--verify", "HEAD^{commit}")
        if head != preflight.get("source_head"):
            raise EvidenceValidationError("source HEAD changed after close-out preflight")
        branch = _git_checked(root, "branch", "--show-current")
        if branch != preflight.get("branch") or not branch:
            raise EvidenceValidationError("close-out branch changed or is detached")
        paths = _validate_mutation_manifest(manifest)
        staged = [
            line
            for line in _git_checked(root, "diff", "--cached", "--name-only", "-z").split("\0")
            if line
        ]
        if set(staged) != set(A41_ALLOWED_MUTATION_PATHS):
            raise EvidenceValidationError("staged tree differs from the exact A41 mutation set")
        unstaged = _git_checked(root, "diff", "--name-only", "-z")
        untracked = _git_checked(root, "ls-files", "--others", "--exclude-standard", "-z")
        if unstaged or untracked:
            raise EvidenceValidationError("worktree contains unstaged or untracked drift")
        for relative, expected in paths.items():
            staged_hash = _staged_blob_hash(root, relative)
            worktree_hash = _path_hash(root / relative)
            if staged_hash != expected or worktree_hash != expected:
                raise EvidenceValidationError(f"index/worktree hash mismatch for {relative}")
        inventory = _require_mapping(preflight.get("inventory"), "preflight.inventory")
        for item in _require_sequence(inventory.get("references"), "preflight.inventory.references"):
            reference = _require_mapping(item, "preflight inventory reference")
            if reference.get("classification") == "historical-or-orchestrator-read-only":
                relative = _normalize_repo_path(root, reference.get("path"), "protected path")
                if _path_hash(root / relative) != reference.get("sha256"):
                    raise EvidenceValidationError(f"protected historical path changed: {relative}")
        _validate_manifest_semantics(root, manifest)
        packet = _close_out_packet(
            "close-out-postflight",
            "passed",
            "exact approved A41 index and worktree bytes authenticated",
            source_head=head,
            branch=branch,
            preflight_sha256=preflight_sha,
            mutation_manifest_sha256=preflight["mutation_manifest_sha256"],
            snapshot_sha256=preflight["snapshot_sha256"],
            staged_paths=paths,
            protected_paths={
                item["path"]: item["sha256"]
                for item in inventory["references"]
                if item["classification"] == "historical-or-orchestrator-read-only"
            },
        )
        _write_json_exclusive(resolved_evidence, packet)
        return 0
    except (EvidenceValidationError, OSError, ValueError) as exc:
        try:
            root = repository_root.resolve(strict=True)
            approved_root = _validated_evidence_root(evidence_root, root)
            resolved_evidence = _require_evidence_path(
                evidence_path, approved_root, "postflight output", must_exist=False
            )
        except EvidenceValidationError:
            return 1
        return _write_close_out_rejection(resolved_evidence, "close-out-postflight", exc)


def _authenticate_postflight(
    postflight_path: Path,
    preflight_path: Path,
    manifest_path: Path,
    snapshot_path: Path,
    evidence_root: Path,
) -> tuple[Mapping[str, Any], Mapping[str, Any], Mapping[str, Any], str]:
    preflight, manifest, preflight_sha = _authenticate_preflight(
        preflight_path, manifest_path, evidence_root
    )
    _authenticate_snapshot(snapshot_path, preflight, evidence_root)
    postflight = _read_bounded_json(postflight_path, approved_root=evidence_root)
    _validate_close_out_packet_hash(postflight, "postflight")
    _require_exact_fields(
        postflight,
        frozenset(
            {
                "schema_version",
                "producer",
                "checkpoint",
                "status",
                "profile_sha256",
                "captured_utc",
                "reason",
                "a41_status",
                "production_lifecycle_writes",
                "source_head",
                "branch",
                "preflight_sha256",
                "mutation_manifest_sha256",
                "snapshot_sha256",
                "staged_paths",
                "protected_paths",
                "packet_sha256",
            }
        ),
        "postflight packet",
    )
    if (
        postflight.get("producer") != "verify-access-telemetry-lifecycle/v1"
        or postflight.get("checkpoint") != "close-out-postflight"
        or postflight.get("status") != "passed"
        or postflight.get("profile_sha256") != STORY_27_4_PROFILE_SHA256
        or postflight.get("preflight_sha256") != preflight_sha
        or postflight.get("mutation_manifest_sha256") != preflight.get("mutation_manifest_sha256")
        or postflight.get("snapshot_sha256") != preflight.get("snapshot_sha256")
    ):
        raise EvidenceValidationError("postflight/preflight chain is not authentic")
    postflight_sha = hashlib.sha256(
        _safe_input_path(postflight_path, approved_root=evidence_root).read_bytes()
    ).hexdigest()
    return postflight, preflight, manifest, postflight_sha


def run_publish_verification(
    *,
    repository_root: Path,
    commit: str,
    mutation_manifest_path: Path,
    evidence_path: Path,
    preflight_path: Path,
    postflight_path: Path,
    snapshot_path: Path,
    remote: str,
    branch: str,
    evidence_root: Path,
) -> int:
    """Prove the reviewed close-out commit is contained by the intended remote branch."""

    temporary_ref: str | None = None
    try:
        root = repository_root.resolve(strict=True)
        approved_root = _validated_evidence_root(evidence_root, root)
        resolved_postflight = _require_evidence_path(
            postflight_path, approved_root, "postflight", must_exist=True
        )
        resolved_preflight = _require_evidence_path(
            preflight_path, approved_root, "preflight", must_exist=True
        )
        resolved_manifest = _require_evidence_path(
            mutation_manifest_path, approved_root, "mutation manifest", must_exist=True
        )
        resolved_snapshot = _require_evidence_path(snapshot_path, approved_root, "snapshot", must_exist=True)
        resolved_evidence = _require_evidence_path(
            evidence_path, approved_root, "publish output", must_exist=False
        )
        postflight, preflight, manifest, postflight_sha = _authenticate_postflight(
            resolved_postflight,
            resolved_preflight,
            resolved_manifest,
            resolved_snapshot,
            approved_root,
        )
        commit_id = _require_nonempty_string(commit, "commit", maximum=64)
        if _COMMIT_ID.fullmatch(commit_id) is None:
            raise EvidenceValidationError("publish commit must be a full lowercase commit identifier")
        resolved = _git_checked(root, "rev-parse", "--verify", f"{commit_id}^{{commit}}")
        if resolved != commit_id:
            raise EvidenceValidationError("publish commit does not resolve exactly")
        if _git_checked(root, "rev-parse", "--verify", f"{commit_id}^") != preflight["source_head"]:
            raise EvidenceValidationError("published close-out commit is not based on the preflight source head")
        normalized_remote = _require_git_remote(remote)
        normalized_branch = _require_nonempty_string(branch, "branch", maximum=256)
        if normalized_remote != preflight.get("remote"):
            raise EvidenceValidationError("publish remote differs from the preflight-approved remote")
        if normalized_branch != preflight["branch"] or normalized_branch != postflight["branch"]:
            raise EvidenceValidationError("publish branch differs from the authenticated close-out branch")
        remote_url = _git_checked(root, "remote", "get-url", normalized_remote).strip().rstrip("/")
        if not remote_url:
            raise EvidenceValidationError("publish remote has no configured URL")
        if _sha256(remote_url) != preflight.get("remote_url_sha256"):
            raise EvidenceValidationError("publish remote URL differs from preflight-approved identity")
        ref_identity = hashlib.sha256((normalized_remote + normalized_branch).encode()).hexdigest()[:16]
        temporary_ref = f"refs/codex/a41-publish-{ref_identity}-{os.getpid()}-{time.monotonic_ns():x}"
        _git_checked(
            root,
            "fetch",
            "--no-tags",
            "--force",
            normalized_remote,
            f"refs/heads/{normalized_branch}:{temporary_ref}",
        )
        remote_head = _git_checked(root, "rev-parse", "--verify", f"{temporary_ref}^{{commit}}")
        try:
            containment = subprocess.run(
                ("git", "-C", str(root), "merge-base", "--is-ancestor", commit_id, remote_head),
                check=False,
                capture_output=True,
                timeout=30,
            )
        except subprocess.TimeoutExpired as exc:
            raise EvidenceValidationError("remote descendant containment check timed out") from exc
        if containment.returncode != 0:
            raise EvidenceValidationError("intended remote branch does not contain the close-out commit")
        paths = _validate_mutation_manifest(manifest)
        changed = {
            line
            for line in _git_checked(
                root,
                "diff-tree",
                "--no-commit-id",
                "--name-only",
                "-r",
                commit_id,
            ).splitlines()
            if line
        }
        if changed != set(A41_ALLOWED_MUTATION_PATHS):
            raise EvidenceValidationError("published commit changed paths outside the exact A41 mutation set")
        for relative, expected in paths.items():
            if _hash_git_blob(root, commit_id, relative) != expected:
                raise EvidenceValidationError(f"published commit bytes differ for {relative}")
        protected = _require_mapping(postflight.get("protected_paths"), "postflight.protected_paths")
        for relative, expected in protected.items():
            if expected is None:
                absent = subprocess.run(
                    ("git", "-C", str(root), "cat-file", "-e", f"{commit_id}:{relative}"),
                    check=False,
                    capture_output=True,
                    timeout=30,
                ).returncode != 0
                if not absent:
                    raise EvidenceValidationError(f"published commit created protected history: {relative}")
                continue
            if _hash_git_blob(root, commit_id, relative) != expected:
                raise EvidenceValidationError(f"published commit changed protected history: {relative}")
        packet = _close_out_packet(
            "publish-verification",
            "published",
            "remote containment and exact close-out commit bytes verified",
            commit=commit_id,
            remote=normalized_remote,
            branch=normalized_branch,
            remote_url_sha256=_sha256(remote_url),
            preflight_sha256=hashlib.sha256(resolved_preflight.read_bytes()).hexdigest(),
            postflight_sha256=postflight_sha,
            mutation_manifest_sha256=preflight["mutation_manifest_sha256"],
        )
        _write_json_exclusive(resolved_evidence, packet)
        return 0
    except (EvidenceValidationError, OSError, ValueError) as exc:
        try:
            root = repository_root.resolve(strict=True)
            approved_root = _validated_evidence_root(evidence_root, root)
            resolved_evidence = _require_evidence_path(
                evidence_path, approved_root, "publish output", must_exist=False
            )
        except EvidenceValidationError:
            return 1
        return _write_close_out_rejection(resolved_evidence, "publish-verification", exc)
    finally:
        if temporary_ref is not None:
            try:
                _git_checked(repository_root, "update-ref", "-d", temporary_ref)
            except EvidenceValidationError:
                pass
