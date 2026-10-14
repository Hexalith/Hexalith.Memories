"""Pure owner-bound C1.15 byte inspection; no registration or gate acceptance.

Outside-run and registry anchors are caller assertions from the approved plan.
Matching them authenticates no owner, execution, custody or target authority.
"""

from __future__ import annotations

from dataclasses import dataclass, field, replace
from datetime import datetime
from typing import NoReturn

from access_telemetry_c1_capture_semantics import (
    C115CommandInspection, inspect_c115_command_declarations,
)
from access_telemetry_c1_capture_streams import (
    C115CommandStreams, C115StreamInspection, inspect_c115_retained_streams,
)
from access_telemetry_c1_interchange import (
    InterchangeFormatError, JsonSnapshot, MAX_AGGREGATE_BYTES, MAX_ARTIFACT_BYTES,
    _array, _artifact_ref, _digest, _instant, _literal, _object,
    authenticate_snapshot, parse_capture,
)
from access_telemetry_c1_owner_run import (
    OwnerRunAnchors, OwnerRunInspection, OwnerRunInspectionError,
    _now_ticks, inspect_owner_run,
)
from access_telemetry_c1_producer_bindings import (
    SourceInspection, SourceSnapshot, _COMMAND_FIELDS, _C115_PURPOSES,
    _REGISTRATION_FIELDS, inspect_registry, inspect_sources,
)


_PREFIX = "hexalith.access-telemetry.c1."
_OWNER_FIELDS = frozenset("policyId policySha256 ownerPrincipal assuranceMode".split())
_REGISTRATION = _REGISTRATION_FIELDS | _OWNER_FIELDS
_ROLE = frozenset("schemaVersion gate profileId".split()) | _OWNER_FIELDS


class OwnerC115InspectionError(ValueError):
    """Bounded content-free refusal; no partial inspection is returned."""


def _refuse(code: str) -> NoReturn:
    raise OwnerC115InspectionError(code) from None


@dataclass(frozen=True, slots=True)
class OwnerRegistrationInspection:
    """Three exact owner declaration materials bound to the inspected sources."""

    source: SourceInspection = field(repr=False)
    statement: JsonSnapshot = field(repr=False)
    command_contract: JsonSnapshot = field(repr=False)
    review_role_policy: JsonSnapshot = field(repr=False)


@dataclass(frozen=True, slots=True)
class OwnerC115Inspection:
    """Combined identical bytes/run and explicit self-review; no pass verdict.

    Source pairs and streams are detached immutable tuples with concealed bytes.
    ``retained_byte_count`` charges every supplied occurrence, without deduping.
    """

    owner_run: OwnerRunInspection = field(repr=False)
    registration: OwnerRegistrationInspection = field(repr=False)
    commands: C115CommandInspection = field(repr=False)
    retained_streams: C115StreamInspection = field(repr=False)
    sources: tuple[SourceSnapshot, ...] = field(repr=False)
    retained_byte_count: int
    owner_principal: str
    assurance_mode: str


def _budget(
    snapshots: tuple[JsonSnapshot, ...],
    sources: tuple[SourceSnapshot, ...],
    streams: tuple[C115CommandStreams, ...],
) -> int:
    """Check closed containers and all raw occurrences before parsing/hashing."""
    if any(type(item) is not JsonSnapshot for item in snapshots):
        _refuse("owner-c115-snapshots-required")
    if type(sources) is not tuple or len(sources) != 19:
        _refuse("owner-c115-nineteen-source-tuple-required")
    if type(streams) is not tuple:
        _refuse("owner-c115-stream-tuple-required")
    # A 1 MiB capture cannot describe more than this many complete receipts.
    # Bound even zero-byte adversarial stream containers before walking them.
    if len(streams) > MAX_ARTIFACT_BYTES:
        _refuse("owner-c115-stream-count-exceeded")
    if any(type(source) is not SourceSnapshot for source in sources):
        _refuse("owner-c115-source-snapshot-required")
    if any(type(stream) is not C115CommandStreams for stream in streams):
        _refuse("owner-c115-stream-pair-required")
    total = 0

    def add(raw: bytes) -> None:
        nonlocal total
        if type(raw) is not bytes:
            _refuse("owner-c115-immutable-bytes-required")
        if len(raw) > MAX_ARTIFACT_BYTES:
            _refuse("owner-c115-artifact-byte-budget-exceeded")
        total += len(raw)
        if total > MAX_AGGREGATE_BYTES:
            _refuse("owner-c115-aggregate-byte-budget-exceeded")

    for snapshot in snapshots:
        add(snapshot.raw)
    for source in sources:
        add(source.executed_bytes)
        add(source.blob_bytes)
    for stream in streams:
        if type(stream.purpose) is not str:
            _refuse("owner-c115-stream-purpose-required")
        add(stream.stdout)
        add(stream.stderr)
    return total


def _registration(
    source: SourceInspection,
    run: OwnerRunInspection,
    statement: JsonSnapshot,
    command_contract: JsonSnapshot,
    review_role_policy: JsonSnapshot,
) -> OwnerRegistrationInspection:
    """Inspect closed owner schemas and all three exact registry Ref bindings."""
    entry = source.entry
    for reference, snapshot in (
        (entry.registration_receipt, statement), (entry.command_contract, command_contract),
        (entry.review_role_policy, review_role_policy),
    ):
        authenticate_snapshot(reference, snapshot)
    value = _object(statement.value, _REGISTRATION)
    _literal(value["schemaVersion"], _PREFIX + "owner-registration/v1")
    fields = {
        "gate": entry.gate, "profileId": entry.profile_id,
        "registeredStory": entry.registered_story, "producerPath": entry.producer_path,
        "helperPaths": entry.helper_paths, "inputPaths": entry.input_paths,
        "captureSchema": entry.capture_schema, "verifierPath": entry.verifier_path,
        "verifierSchema": entry.verifier_schema, "cleanupRequired": False,
        "sourceCommit": source.source_commit,
    }
    for name, expected in fields.items():
        _literal(value[name], expected)
    if entry.cleanup_required:
        _refuse("owner-c115-read-only-cleanup-required")
    for name, expected in (("commandContract", entry.command_contract),
                           ("reviewRolePolicy", entry.review_role_policy)):
        if _artifact_ref(value[name]) != expected:
            _refuse("owner-c115-registration-entry-mismatch")
    policy = _object(review_role_policy.value, _ROLE)
    for material in (value, policy):
        for name in _OWNER_FIELDS:
            _literal(material[name], run.snapshot.value[name])
    for name, expected in (("schemaVersion", _PREFIX + "owner-review-role-policy/v1"),
                           ("gate", entry.gate), ("profileId", entry.profile_id)):
        _literal(policy[name], expected)
    command = _object(command_contract.value, _COMMAND_FIELDS)
    for name, expected in (
        ("schemaVersion", _PREFIX + "owner-command-contract/v1"),
        ("gate", entry.gate), ("profileId", entry.profile_id),
        ("producerPath", entry.producer_path), ("executable", "kubectl"), ("readOnly", True),
    ):
        _literal(command[name], expected)
    if _array(command["purposes"]) != _C115_PURPOSES:
        _refuse("owner-c115-command-contract-unsupported")
    return OwnerRegistrationInspection(source, statement, command_contract, review_role_policy)


def inspect_owner_c115(
    registry: JsonSnapshot,
    capture: JsonSnapshot,
    sources: tuple[SourceSnapshot, ...],
    streams: tuple[C115CommandStreams, ...],
    run: JsonSnapshot,
    anchors: OwnerRunAnchors,
    statement: JsonSnapshot,
    command_contract: JsonSnapshot,
    review_role_policy: JsonSnapshot,
    approved_registry_sha256: str,
    *,
    now: datetime,
    withdrawn: bool,
) -> OwnerC115Inspection:
    """Reinspect explicit retained inputs against outside owner-plan assertions.

    The approved registry digest covers its exact raw bytes, not canonical J1.
    ``now`` must be aware UTC and ``withdrawn`` a freshly supplied boolean.
    Require the run's observe action; both capture endpoints must be within its
    collection window and finish at/before ``now``. No I/O, execution, live
    provenance, registration, target authorization or acceptance is performed.
    """
    refusal = None
    try:
        total = _budget((registry, capture, run, statement, command_contract, review_role_policy),
                        sources, streams)
        # Detach records as well as their tuple containers. Immutable byte
        # objects keep their identities, while caller-forged field changes can
        # no longer change the inspected/returned source pairs or streams.
        sources = tuple(replace(source) for source in sources)
        streams = tuple(replace(stream) for stream in streams)
        # Cached trees and hashes are caller-writable through object.__setattr__;
        # rebuild every snapshot from bounded bytes before using any cached fact.
        registry, capture, run, statement, command_contract, review_role_policy = (
            JsonSnapshot(item.raw) for item in
            (registry, capture, run, statement, command_contract, review_role_policy)
        )
        _digest(approved_registry_sha256)
        if registry.sha256 != approved_registry_sha256:
            _refuse("owner-c115-approved-registry-digest-mismatch")
        owner_run = inspect_owner_run(run, anchors, now=now, withdrawn=withdrawn, action="observe")
        parse_capture(capture)
        value = capture.value
        for capture_name, run_name in (
            ("profileId", "profileId"), ("profileSha256", "profileSha256"),
            ("workloadSha256", "workloadSha256"), ("qualificationSessionId", "sessionId"),
            ("targetSha256", "targetSha256"), ("sourceCommit", "sourceCommit"),
        ):
            if value[capture_name] != owner_run.snapshot.value[run_name]:
                _refuse("owner-c115-capture-run-scope-mismatch")
        start, finish = (_instant(value["producer"][name], spelling="capture")
                         for name in ("startedAtUtc", "finishedAtUtc"))
        first, last = (_instant(owner_run.snapshot.value[name], spelling="new")
                       for name in ("notBeforeUtc", "collectionEndsAtUtc"))
        if not first <= start <= finish <= last or finish > _now_ticks(now):
            _refuse("owner-c115-capture-outside-collection")
        source = inspect_sources(inspect_registry(registry), capture, sources,
                                 owner_run.snapshot.value["sourceCommit"])
        registration = _registration(source, owner_run, statement, command_contract, review_role_policy)
        commands = inspect_c115_command_declarations(capture)
        retained_streams = inspect_c115_retained_streams(capture, streams)
    except (InterchangeFormatError, OwnerRunInspectionError) as error:
        refusal = "owner-c115-" + str(error)
    except OwnerC115InspectionError as error:
        refusal = str(error)
    except AttributeError:
        # Exact dataclass instances can be constructed without initialized slots
        # or have slots removed via object.__delattr__, including in preflight.
        refusal = "owner-c115-required-field-missing"
    # Raise outside handlers: chained decoder exceptions can retain candidate
    # bytes, even when their printed context is suppressed.
    if refusal is not None:
        _refuse(refusal)
    return OwnerC115Inspection(owner_run, registration, commands, retained_streams,
                               sources, total, owner_run.owner_principal, owner_run.assurance_mode)
