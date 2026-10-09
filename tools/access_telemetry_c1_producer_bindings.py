"""Bounded offline C1 registry and retained-source inspection.

All paths, commit labels and transform metadata are supplied untrusted facts.
These functions perform no I/O and confer no registration or execution authority.
"""

from __future__ import annotations

from dataclasses import dataclass, field
import hashlib
from typing import Any

from access_telemetry_c1_interchange import (
    EvidenceReference, JsonSnapshot, MAX_AGGREGATE_BYTES, MAX_ARTIFACT_BYTES,
    _array, _artifact_ref, _boolean, _commit, _gate, _literal, _object, _path,
    _refuse, _text, authenticate_snapshot, j1_bytes, parse_capture,
)


_ENTRY_FIELDS = frozenset((
    "gate profileId registeredStory registrationReceipt producerPath helperPaths "
    "inputPaths captureSchema verifierPath verifierSchema commandContract "
    "cleanupRequired reviewRolePolicy"
).split())
_C115_SCHEMA = "hexalith.access-telemetry.c1.evidence/v2"
_C115_HELPERS = frozenset({
    "tools/access-telemetry-c1-profile.ps1",
    "tools/access-telemetry-c1-component-backend.ps1",
})
_REGISTRATION_SCHEMA = "hexalith.access-telemetry.c1.fixture-registration/v1"
_COMMAND_SCHEMA = "hexalith.access-telemetry.c1.fixture-command-contract/v1"
_ROLE_SCHEMA = "hexalith.access-telemetry.c1.fixture-review-role-policy/v1"
_REGISTRATION_FIELDS = (_ENTRY_FIELDS - {"registrationReceipt"}) | {"schemaVersion", "sourceCommit"}
_COMMAND_FIELDS = frozenset("schemaVersion gate profileId producerPath executable purposes readOnly".split())
_ROLE_FIELDS = frozenset("schemaVersion gate profileId reviewerRole producerExcluded".split())
_C115_PURPOSES = (
    "current-context", "lifecycle-pods", "daprd-version", "metadata",
    "alpha-opt-in", "lifecycle-pods-recheck",
)


@dataclass(frozen=True, slots=True)
class RegistryEntryInspection:
    """Closed inventory entry; its Refs and labels remain unauthenticated."""

    gate: str
    profile_id: str
    registered_story: str
    registration_receipt: EvidenceReference
    producer_path: str
    helper_paths: tuple[str, ...]
    input_paths: tuple[str, ...]
    capture_schema: str
    verifier_path: str
    verifier_schema: str
    command_contract: EvidenceReference
    cleanup_required: bool
    review_role_policy: EvidenceReference
    registry_entry_sha256: str


@dataclass(frozen=True, slots=True)
class RegistryInspection:
    """Immutable ordered inventory, J1 hash, and separately retained JSON bytes."""

    snapshot: JsonSnapshot = field(repr=False)
    entries: tuple[RegistryEntryInspection, ...]
    registry_sha256: str


def _paths(value: Any) -> tuple[str, ...]:
    paths = _array(value)
    for path in paths:
        _path(path)
    if len(set(paths)) != len(paths) or tuple(sorted(paths)) != paths:
        _refuse("registry-paths-not-ordered-unique")
    return paths


def _entry(value: Any) -> RegistryEntryInspection:
    entry = _object(value, _ENTRY_FIELDS)
    _gate(entry["gate"])
    _literal(entry["profileId"], "PG-ONPREM-2")
    producer = _path(entry["producerPath"])
    helpers = _paths(entry["helperPaths"])
    inputs = _paths(entry["inputPaths"])
    # A retained regular file cannot also be an ancestor of another source.
    paths = (producer,) + helpers + inputs
    unique = set(paths)
    if len(unique) != len(paths) or any(
        "/".join(path.split("/")[:length]) in unique
        for path in paths for length in range(1, len(path.split("/")))
    ):
        _refuse("registry-source-path-overlap")
    return RegistryEntryInspection(
        entry["gate"], entry["profileId"], _path(entry["registeredStory"]),
        _artifact_ref(entry["registrationReceipt"]), producer, helpers, inputs,
        _text(entry["captureSchema"]), _path(entry["verifierPath"]),
        _text(entry["verifierSchema"]), _artifact_ref(entry["commandContract"]),
        _boolean(entry["cleanupRequired"]), _artifact_ref(entry["reviewRolePolicy"]),
        hashlib.sha256(j1_bytes(entry)).hexdigest(),
    )


def inspect_registry(snapshot: JsonSnapshot) -> RegistryInspection:
    """Inspect an exact thirteen-field JSON inventory, including an empty one.

    Entries must be unique and numerically ordered C1.1 through C1.25 labels.
    Unknown schema identifiers can be inventoried; none becomes eligible.
    """
    if type(snapshot) is not JsonSnapshot:
        _refuse("registry-snapshot-required")
    values = _array(snapshot.value)
    if len(values) > 25:
        _refuse("registry-gate-count-exceeded")
    numbers = []
    for value in values:
        entry = _object(value, _ENTRY_FIELDS)
        _gate(entry["gate"])
        numbers.append(int(entry["gate"][3:]))
    if len(set(numbers)) != len(numbers) or sorted(numbers) != numbers:
        _refuse("registry-gates-not-ordered-unique")
    entries = tuple(_entry(value) for value in values)
    return RegistryInspection(snapshot, entries, hashlib.sha256(j1_bytes(values)).hexdigest())


def _source_shape(source: SourceSnapshot) -> None:
    # Bound both byte forms before any decoding or digest computation.
    for raw in (source.executed_bytes, source.blob_bytes):
        if type(raw) is not bytes:
            _refuse("immutable-source-bytes-required")
        if len(raw) > MAX_ARTIFACT_BYTES:
            _refuse("source-byte-budget-exceeded")
    _path(source.path)
    _commit(source.source_commit)
    if type(source.normalization) is not str or source.normalization != "utf8-crlf-to-lf":
        _refuse("source-normalization-unsupported")
    if type(source.git_mode) is not str or source.git_mode not in ("100644", "100755"):
        _refuse("source-mode-unsupported")
    if (source.clean_filter is not None or source.working_tree_encoding is not None
            or source.ident is not False):
        _refuse("source-transform-unsupported")


@dataclass(frozen=True, slots=True)
class SourceSnapshot:
    """Explicit retained byte pair and untrusted Git/normalization metadata.

    Every field is required. Supported metadata is ``utf8-crlf-to-lf``, regular
    Git mode 100644/100755, no clean filter or working-tree encoding, ident false.
    Construction bounds bytes; UTF-8 and identity checks occur in inspection.
    """

    path: str
    source_commit: str
    executed_bytes: bytes = field(repr=False)
    blob_bytes: bytes = field(repr=False)
    normalization: str
    git_mode: str
    clean_filter: str | None
    working_tree_encoding: str | None
    ident: bool

    def __post_init__(self) -> None:
        _source_shape(self)


class _RetainedBudget:
    """One call's bounded, deduplicated JSON and source-byte accounting."""

    def __init__(self) -> None:
        self._retained: set[bytes] = set()
        self.byte_count = 0

    def add(self, raw: bytes) -> None:
        if type(raw) is not bytes:
            _refuse("immutable-bytes-required")
        if len(raw) > MAX_ARTIFACT_BYTES:
            _refuse("artifact-byte-budget-exceeded")
        if raw not in self._retained:
            if self.byte_count + len(raw) > MAX_AGGREGATE_BYTES:
                _refuse("aggregate-byte-budget-exceeded")
            self._retained.add(raw)
            self.byte_count += len(raw)


@dataclass(frozen=True, slots=True)
class SourceByteInspection:
    """Recomputed byte identities that matched one neutral capture receipt."""

    path: str
    source_commit: str
    executed_bytes_sha256: str
    git_normalized_sha256: str
    git_blob_oid: str


@dataclass(frozen=True, slots=True)
class SourceInspection:
    """Byte-comparison result; it supplies no accepted-gate or pass verdict."""

    registry: RegistryInspection = field(repr=False)
    capture: JsonSnapshot = field(repr=False)
    entry: RegistryEntryInspection
    source_commit: str
    sources: tuple[SourceByteInspection, ...]
    unique_retained_byte_count: int


@dataclass(frozen=True, slots=True)
class RegistrationInspection:
    """Exact retained Ref materials and one fixture statement; no approval."""

    source: SourceInspection = field(repr=False)
    statement: JsonSnapshot = field(repr=False)
    command_contract: JsonSnapshot = field(repr=False)
    review_role_policy: JsonSnapshot = field(repr=False)
    gate: str
    source_commit: str
    registration_sha256: str
    command_sha256: str
    role_policy_sha256: str


def inspect_sources(
    registry: RegistryInspection,
    capture: JsonSnapshot,
    sources: tuple[SourceSnapshot, ...],
    expected_source_commit: str,
) -> SourceInspection:
    """Compare all C1.15 receipts to explicit retained source-byte snapshots.

    The required full commit is compared as a label only. No Git object or
    repository, source custody, real attributes, execution or Ref is authenticated.
    JSON and both byte forms share a 32 MiB unique-retained-byte budget.
    """
    if type(registry) is not RegistryInspection or type(capture) is not JsonSnapshot:
        _refuse("registry-and-capture-inspections-required")
    if type(registry.snapshot) is not JsonSnapshot:
        _refuse("registry-snapshot-required")
    _commit(expected_source_commit)
    if type(sources) is not tuple:
        _refuse("immutable-source-tuple-required")
    if len(sources) != 19:
        _refuse("retained-source-count-mismatch")
    budget = _RetainedBudget()
    budget.add(registry.snapshot.raw)
    budget.add(capture.raw)
    retained: dict[str, SourceSnapshot] = {}
    for source in sources:
        if type(source) is not SourceSnapshot:
            _refuse("source-snapshot-required")
        _source_shape(source)
        budget.add(source.executed_bytes)
        budget.add(source.blob_bytes)
        if source.path in retained:
            _refuse("duplicate-retained-source")
        retained[source.path] = source
    # Reinspect the retained registry tree rather than trusting constructed
    # result fields supplied by a caller as a new registration assertion.
    registry = inspect_registry(registry.snapshot)
    parse_capture(capture)
    value = capture.value
    if (value["producerStatus"] != "observed" or value["sourceDisposition"] != "clean"
            or value["worktreeDirty"] is not False or value["finalSourceRecheck"] != "unchanged"):
        _refuse("observed-clean-unchanged-capture-required")
    if value["sourceCommit"] != expected_source_commit:
        _refuse("capture-source-commit-mismatch")
    entry = next((item for item in registry.entries if item.gate == value["gate"]), None)
    if entry is None:
        _refuse("capture-registry-entry-missing")
    if entry.capture_schema != _C115_SCHEMA:
        _refuse("source-capture-schema-unsupported")
    receipts = {receipt["source"]: receipt for receipt in value["producerSources"]}
    expected_paths = {entry.producer_path, *entry.helper_paths, *entry.input_paths}
    if (entry.producer_path != value["producer"]["path"]
            or set(entry.helper_paths) != _C115_HELPERS or expected_paths != set(receipts)):
        _refuse("registry-capture-source-set-mismatch")
    if set(retained) != expected_paths:
        _refuse("retained-source-set-mismatch")
    inspected = []
    for path in sorted(expected_paths):
        source = retained[path]
        receipt = receipts[path]
        if source.source_commit != expected_source_commit:
            _refuse("retained-source-commit-mismatch")
        if (receipt["trackedAtHead"] is not True or receipt["modified"] is not False
                or receipt["matchesHead"] is not True):
            _refuse("clean-source-receipt-required")
        invalid_utf8 = False
        try:
            normalized = source.executed_bytes.decode("utf-8", errors="strict").replace("\r\n", "\n").encode("utf-8")
            source.blob_bytes.decode("utf-8", errors="strict")
        except UnicodeDecodeError:
            invalid_utf8 = True
        if invalid_utf8:
            _refuse("invalid-source-utf8")
        if normalized != source.blob_bytes:
            _refuse("source-normalized-bytes-mismatch")
        executed_digest = hashlib.sha256(source.executed_bytes).hexdigest()
        blob_digest = hashlib.sha256(source.blob_bytes).hexdigest()
        header = ("blob " + str(len(source.blob_bytes)) + "\0").encode("ascii")
        oid = None
        try:
            oid = hashlib.sha1(header + source.blob_bytes, usedforsecurity=False).hexdigest()
        except ValueError:
            pass
        if oid is None:
            _refuse("git-blob-checksum-unavailable")
        if (executed_digest != receipt["executedBytesSha256"]
                or blob_digest != receipt["gitNormalizedSha256"]
                or blob_digest != receipt["headGitNormalizedSha256"]
                or oid != receipt["gitBlobOid"]):
            _refuse("source-byte-identity-mismatch")
        inspected.append(SourceByteInspection(path, expected_source_commit, executed_digest, blob_digest, oid))
    return SourceInspection(registry, capture, entry, expected_source_commit, tuple(inspected), budget.byte_count)


def inspect_registration(
    registry: RegistryInspection,
    capture: JsonSnapshot,
    sources: tuple[SourceSnapshot, ...],
    expected_source_commit: str,
    statement: JsonSnapshot,
    command_contract: JsonSnapshot,
    review_role_policy: JsonSnapshot,
) -> RegistrationInspection:
    """Corroborate a proposed fixture statement and all three exact Ref bytes.

    This validates local retained declarations only. No owner, signer, status,
    authority provider or deployed registry is consulted or inferred.
    """
    if any(type(item) is not JsonSnapshot for item in (statement, command_contract, review_role_policy)):
        _refuse("registration-snapshots-required")
    source = inspect_sources(registry, capture, sources, expected_source_commit)
    entry = source.entry
    authenticate_snapshot(entry.registration_receipt, statement)
    authenticate_snapshot(entry.command_contract, command_contract)
    authenticate_snapshot(entry.review_role_policy, review_role_policy)
    value = _object(statement.value, _REGISTRATION_FIELDS)
    _literal(value["schemaVersion"], _REGISTRATION_SCHEMA)
    _commit(value["sourceCommit"])
    _literal(value["sourceCommit"], source.source_commit)
    budget = _RetainedBudget()
    for raw in (registry.snapshot.raw, capture.raw, statement.raw,
                command_contract.raw, review_role_policy.raw):
        budget.add(raw)
    for retained_source in sources:
        budget.add(retained_source.executed_bytes)
        budget.add(retained_source.blob_bytes)
    fields = {
        "gate": entry.gate, "profileId": entry.profile_id,
        "registeredStory": entry.registered_story, "producerPath": entry.producer_path,
        "helperPaths": entry.helper_paths, "inputPaths": entry.input_paths,
        "captureSchema": entry.capture_schema, "verifierPath": entry.verifier_path,
        "verifierSchema": entry.verifier_schema, "cleanupRequired": entry.cleanup_required,
    }
    for name, expected in fields.items():
        _literal(value[name], expected)
    for name, expected in (("commandContract", entry.command_contract),
                           ("reviewRolePolicy", entry.review_role_policy)):
        actual = _artifact_ref(value[name])
        if actual != expected:
            _refuse("registration-entry-mismatch")
    command = _object(command_contract.value, _COMMAND_FIELDS)
    for name, expected in (("schemaVersion", _COMMAND_SCHEMA), ("gate", entry.gate),
                           ("profileId", entry.profile_id), ("producerPath", entry.producer_path),
                           ("executable", "kubectl"), ("readOnly", True)):
        _literal(command[name], expected)
    purposes = _array(command["purposes"])
    if purposes != _C115_PURPOSES:
        _refuse("command-contract-unsupported")
    observed = tuple(item["purpose"].split(":", 1)[0] for item in capture.value["commands"])
    if observed != purposes or entry.cleanup_required:
        _refuse("command-contract-unsupported")
    policy = _object(review_role_policy.value, _ROLE_FIELDS)
    for name, expected in (("schemaVersion", _ROLE_SCHEMA), ("gate", entry.gate),
                           ("profileId", entry.profile_id), ("reviewerRole", "independent-security-reviewer"),
                           ("producerExcluded", True)):
        _literal(policy[name], expected)
    return RegistrationInspection(
        source, statement, command_contract, review_role_policy, entry.gate,
        source.source_commit, statement.sha256, command_contract.sha256, review_role_policy.sha256,
    )


def lookup_deployed_binding(
    gate: str, profile_id: str, inventory: RegistryInspection | None = None,
) -> RegistryEntryInspection:
    """Always refuse: no inventory, fixture, label or Ref registers deployment."""
    _refuse("deployed-producer-binding-unavailable")
