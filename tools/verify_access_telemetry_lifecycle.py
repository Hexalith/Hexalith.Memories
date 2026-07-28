"""Fail-closed evidence primitives for the Story 27.3 adapter profile gate."""

from __future__ import annotations

from dataclasses import dataclass
from decimal import Decimal, InvalidOperation, localcontext
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
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
    return datetime.now(timezone.utc).isoformat()


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

    # Approved-identity comparison runs BEFORE any query against the target. Querying first
    # meant an unapproved target was contacted, and its output written into the packet,
    # regardless of the comparison result.
    preflight_reason = None
    if workload_profile != "adr-27.1-two-writer-500eps":
        preflight_reason = f"unsupported workload profile: {workload_profile}"
    elif steady_state_minutes != 30 or purge_backlog_records != 150000:
        preflight_reason = "C1 workload envelope does not match the mandatory 30-minute/150,000-record gate"
    elif identity.kube_namespace != EXPECTED_KUBE_NAMESPACE:
        preflight_reason = "execution target does not match the approved on-premises Kubernetes namespace"
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
            else:
                reason = (
                    "state.postgresql/v2 has no complete approved exact-profile Dapr behavior, load, "
                    "capacity, backup/restore, physical-reclamation, and separated-review result"
                )

    # Re-read the workload identity after the run so the packet can show the target was not
    # mutated between the first and last observation.
    after = _run_kubectl(identity, "get", "deployments", "-o", "json")
    observations.append(after)
    workload_after = _workload_identity(_items(after.payload))

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
