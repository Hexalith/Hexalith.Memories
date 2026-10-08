"""Offline wire primitives for future authenticated C1 interchange.

Parsing and digest matching establish bytes and shape only. These helpers do not
authenticate a producer, reviewer, session, custody root or accepted gate. No
existing qualification consumer uses this module yet.
"""

from __future__ import annotations

from collections.abc import Mapping
from dataclasses import dataclass, field
from datetime import date
import hashlib
import json
import re
from types import MappingProxyType
from typing import Any
import unicodedata
from urllib.parse import unquote, unquote_plus, urlsplit


MAX_ARTIFACT_BYTES = 1_048_576
MAX_AGGREGATE_BYTES = 33_554_432
MAX_JSON_DEPTH = 14
MAX_STRING_CHARACTERS = 4096
MAX_PATH_CHARACTERS = 512
MIN_INTEGER = -(2**63)
MAX_INTEGER = 2**63 - 1
_DIGEST = re.compile(r"[0-9a-f]{64}\Z")
_SECRET_TEXT = re.compile(
    r"(?i)(-----BEGIN [A-Z ]*PRIVATE KEY-----|\bbearer\s+\S+|"
    r"(?:password|passwd|api[_-]?key|client[_-]?secret|authorization|"
    r"(?:access|refresh|id)[_-]?token)\s*[:=]\s*\S+)"
)


class InterchangeFormatError(ValueError):
    """A bounded, content-free refusal of untrusted wire data."""


def _refuse(code: str) -> None:
    raise InterchangeFormatError(code) from None


def _validate_string(value: str) -> None:
    if len(value) > MAX_STRING_CHARACTERS:
        _refuse("string-budget-exceeded")
    if any(0xD800 <= ord(character) <= 0xDFFF for character in value):
        _refuse("unpaired-unicode-surrogate")


def _validate_tree(value: Any, depth: int = 0, *, ascii_fields: bool = False) -> None:
    if isinstance(value, Mapping):
        if depth >= MAX_JSON_DEPTH:
            _refuse("json-depth-exceeded")
        for key, child in value.items():
            if type(key) is not str:
                _refuse("non-string-field")
            _validate_string(key)
            if ascii_fields and not key.isascii():
                _refuse("non-ascii-j1-field")
            _validate_tree(child, depth + 1, ascii_fields=ascii_fields)
    elif type(value) in (list, tuple):
        if depth >= MAX_JSON_DEPTH:
            _refuse("json-depth-exceeded")
        for child in value:
            _validate_tree(child, depth + 1, ascii_fields=ascii_fields)
    elif type(value) is str:
        _validate_string(value)
    elif type(value) is int:
        if not MIN_INTEGER <= value <= MAX_INTEGER:
            _refuse("integer-out-of-range")
    elif value is not None and type(value) is not bool:
        _refuse("unsupported-json-type")


def _check_depth_before_decode(text: str) -> None:
    depth = 0
    in_string = False
    escaped = False
    for character in text:
        if in_string:
            if escaped:
                escaped = False
            elif character == "\\":
                escaped = True
            elif character == '"':
                in_string = False
        elif character == '"':
            in_string = True
        elif character in "[{":
            depth += 1
            if depth > MAX_JSON_DEPTH:
                _refuse("json-depth-exceeded")
        elif character in "]}":
            depth -= 1


def _object_without_duplicates(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        if key in result:
            _refuse("duplicate-json-field")
        result[key] = value
    return result


def _parse_integer(text: str) -> int:
    if len(text.lstrip("-")) > 19:
        _refuse("integer-out-of-range")
    value = int(text)
    if not MIN_INTEGER <= value <= MAX_INTEGER:
        _refuse("integer-out-of-range")
    return value


def _reject_number(_: str) -> Any:
    _refuse("non-integer-json-number")


def _freeze(value: Any) -> Any:
    if type(value) is dict:
        return MappingProxyType({key: _freeze(child) for key, child in value.items()})
    if type(value) is list:
        return tuple(_freeze(child) for child in value)
    return value


def _mutable_json(value: Any) -> Any:
    if isinstance(value, Mapping):
        return {key: _mutable_json(child) for key, child in value.items()}
    if type(value) in (list, tuple):
        return [_mutable_json(child) for child in value]
    return value


@dataclass(frozen=True, slots=True)
class JsonSnapshot:
    """Exact immutable bytes, their digest, and an immutable parsed JSON tree.

    The generic tree is untrusted data, not artifact-schema admission. Unknown
    fields are rejected by the particular schema reader, such as ``parse_ref``.
    """

    raw: bytes = field(repr=False)
    sha256: str = field(init=False)
    value: Any = field(init=False, repr=False)

    def __post_init__(self) -> None:
        if type(self.raw) is not bytes:
            _refuse("immutable-bytes-required")
        if len(self.raw) > MAX_ARTIFACT_BYTES:
            _refuse("artifact-byte-budget-exceeded")
        if self.raw.startswith(b"\xef\xbb\xbf"):
            _refuse("utf8-bom-forbidden")
        try:
            text = self.raw.decode("utf-8", errors="strict")
        except UnicodeDecodeError:
            _refuse("invalid-utf8")
        _check_depth_before_decode(text)
        try:
            value = json.loads(
                text,
                object_pairs_hook=_object_without_duplicates,
                parse_int=_parse_integer,
                parse_float=_reject_number,
                parse_constant=_reject_number,
            )
        except (json.JSONDecodeError, RecursionError):
            _refuse("invalid-json")
        _validate_tree(value)
        object.__setattr__(self, "sha256", hashlib.sha256(self.raw).hexdigest())
        object.__setattr__(self, "value", _freeze(value))


class SnapshotReader:
    """One validation's 32 MiB budget for unique retained JSON snapshots.

    This consumes bytes, not paths: filesystem custody and safe traversal remain
    separate prerequisites. Repeated identical bytes are charged only once.
    """

    def __init__(self) -> None:
        self._snapshots: dict[str, JsonSnapshot] = {}
        self._byte_count = 0

    @property
    def byte_count(self) -> int:
        """Total exact bytes retained by this reader."""
        return self._byte_count

    def read(self, raw: bytes) -> JsonSnapshot:
        """Parse one snapshot without exceeding artifact or aggregate budgets."""
        if type(raw) is not bytes:
            _refuse("immutable-bytes-required")
        if len(raw) > MAX_ARTIFACT_BYTES:
            _refuse("artifact-byte-budget-exceeded")
        digest = hashlib.sha256(raw).hexdigest()
        previous = self._snapshots.get(digest)
        if previous is not None:
            if previous.raw != raw:
                _refuse("digest-collision")
            return previous
        if self._byte_count + len(raw) > MAX_AGGREGATE_BYTES:
            _refuse("aggregate-byte-budget-exceeded")
        snapshot = JsonSnapshot(raw)
        self._snapshots[digest] = snapshot
        self._byte_count += len(raw)
        return snapshot


def j1_bytes(value: Any) -> bytes:
    """Encode bounded J1 JSON; this is not PowerShell capture argument hashing."""
    _validate_tree(value, ascii_fields=True)
    encoder = json.JSONEncoder(
        ensure_ascii=False,
        sort_keys=True,
        separators=(",", ":"),
        allow_nan=False,
    )
    encoded = bytearray()
    for chunk in encoder.iterencode(_mutable_json(value)):
        chunk_bytes = chunk.encode("utf-8")
        if len(encoded) + len(chunk_bytes) > MAX_ARTIFACT_BYTES:
            _refuse("artifact-byte-budget-exceeded")
        encoded.extend(chunk_bytes)
    return bytes(encoded)


@dataclass(frozen=True, slots=True)
class EvidenceReference:
    """A lexically validated Ref; it grants no filesystem or review authority."""

    path: str
    sha256: str
    byte_length: int

    def __post_init__(self) -> None:
        if type(self.path) is not str:
            _refuse("reference-path-type")
        _validate_string(self.path)
        if not self.path or len(self.path) > MAX_PATH_CHARACTERS:
            _refuse("reference-path-length")
        if (
            self.path.startswith(("/", "~"))
            or "\\" in self.path
            or ":" in self.path
            or any(part in ("", ".", "..") for part in self.path.split("/"))
            or any(unicodedata.category(character) == "Cc" for character in self.path)
        ):
            _refuse("reference-path-not-canonical")
        if _SECRET_TEXT.search(self.path):
            _refuse("reference-path-unsafe")
        if type(self.sha256) is not str or _DIGEST.fullmatch(self.sha256) is None:
            _refuse("reference-digest-not-canonical")
        if type(self.byte_length) is not int or not 0 < self.byte_length <= MAX_ARTIFACT_BYTES:
            _refuse("reference-byte-length")


def parse_ref(value: Any) -> EvidenceReference:
    """Validate exactly the Ref field set and each field's closed type."""
    if not isinstance(value, Mapping) or set(value) != {"path", "sha256", "byteLength"}:
        _refuse("reference-field-set")
    return EvidenceReference(value["path"], value["sha256"], value["byteLength"])


def authenticate_snapshot(reference: EvidenceReference, snapshot: JsonSnapshot) -> None:
    """Match length/digest to retained bytes; this does not authenticate authority."""
    if not isinstance(reference, EvidenceReference) or not isinstance(snapshot, JsonSnapshot):
        _refuse("reference-and-snapshot-required")
    if reference.byte_length != len(snapshot.raw):
        _refuse("reference-length-mismatch")
    if reference.sha256 != snapshot.sha256:
        _refuse("reference-digest-mismatch")


# These field sets describe inspection only. No entry registers a producer or
# supplies a policy, principal, custody root or acceptance verdict.
_PREFIX = "hexalith.access-telemetry.c1."
_SCOPE_FIELDS = "profileId profileSha256 workloadSha256 qualificationSessionId targetSha256"
_CAPTURE_FIELDS = frozenset((
    "schemaVersion gate profileId capturedAtUtc context namespace targetSelector "
    "producerStatus gateStatus productionGatePassed productionLifecycleWrites "
    "observations blockers sources commands profileIdentity profileSha256 workloadId "
    "workloadSha256 qualificationSessionId target targetSha256 producer sourceCommands "
    "sourceCommit sourceDisposition worktreeDirty producerSources finalSourceRecheck "
    "resultCount failureCount skipCount independentDisposition"
).split())
_DISPOSITION_FIELDS = frozenset((
    "schemaVersion capture gate " + _SCOPE_FIELDS + " producerPrincipal reviewerPrincipal "
    "reviewerRole independence decision reasons decidedAtUtc expiresAtUtc authorityReceipt"
).split())
_GATE_FIELDS = frozenset((
    "schemaVersion gate status " + _SCOPE_FIELDS + " registryEntrySha256 capture disposition "
    "sourceCommit producerPath startedAtUtc finishedAtUtc resultCount failureCount skipCount cleanup"
).split())
_MANIFEST_FIELDS = frozenset((
    "schemaVersion checkpoint " + _SCOPE_FIELDS + " registrySha256 policySha256 sessionAuthority gates"
).split())
_APPROVAL_FIELDS = frozenset((
    "schemaVersion manifest " + _SCOPE_FIELDS + " reviewerPrincipal reviewerRole decision reasons "
    "decidedAtUtc expiresAtUtc authorityReceipt"
).split())
_PREDECESSOR_FIELDS = frozenset((
    "schemaVersion manifest approvals status productionLifecycleWrites qualificationAuthorized"
).split())
_RECEIPT_FIELDS = frozenset((
    "executable arguments argumentsSha256 startedAtUtc finishedAtUtc exitCode "
    "stdoutSha256 stderrSha256 streamSafety resultCount failureCount skipCount"
).split())
_OBSERVATION_FIELDS = frozenset((
    "pods runtimeVersions sidecarImageIds sidecarImageDigests appIds "
    "schedulerConnectedAddresses actorTypes enabledFeatures alphaOptIn"
).split())
_POD_FIELDS = frozenset((
    "pod podUid runtimeVersion sidecarImageId sidecarImageDigest appId "
    "schedulerConnectedAddresses actorTypes enabledFeatures alphaOptIn"
).split())
_SOURCE_FIELDS = frozenset((
    "source gitNormalizedSha256 executedBytesSha256 gitBlobOid headGitNormalizedSha256 "
    "trackedAtHead modified matchesHead"
).split())
_PRODUCER_PATH = "tools/verify-access-telemetry-c1.ps1"
_PRODUCER_SOURCE_PATHS = frozenset({
    _PRODUCER_PATH,
    "tools/access-telemetry-c1-profile.ps1",
    "tools/access-telemetry-c1-component-backend.ps1",
    "deploy/dapr/components/access-telemetry-config.yaml",
    "deploy/dapr/components/access-telemetry-secrets.yaml",
    "deploy/dapr/components/access-telemetry-store.yaml",
    "deploy/kubernetes/base/access-telemetry-deployments.yaml",
    "deploy/kubernetes/base/access-telemetry-postgresql.yaml",
    "deploy/kubernetes/base/dapr/access-telemetry-clock-config.yaml",
    "deploy/kubernetes/base/dapr/access-telemetry-config-store.yaml",
    "deploy/kubernetes/base/dapr/access-telemetry-lifecycle-config.yaml",
    "deploy/kubernetes/base/dapr/access-telemetry-secrets.yaml",
    "deploy/kubernetes/base/dapr/access-telemetry-store.yaml",
    "deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml",
    "deploy/kubernetes/overlays/production/kustomization.yaml",
    "deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml",
    "deploy/openbao/service-account-hardening.yaml",
    "deploy/openbao/smoke-test.yaml",
    "deploy/openbao/values.yaml",
})
_COMMIT = re.compile(r"[0-9a-f]{40}\Z")
_SESSION = re.compile(r"[A-Za-z0-9][A-Za-z0-9._-]{0,127}\Z")
_GATE = re.compile(r"C1\.(?:[1-9]|1[0-9]|2[0-5])\Z")
_UTC = re.compile(r"([0-9]{4})-([0-9]{2})-([0-9]{2})T([0-9]{2}):([0-9]{2}):([0-9]{2})(?:\.([0-9]{1,7}))?(Z|\+00:00)\Z")
_SESSION_SECRET = re.compile(r"(?i)(token|secret|password|authorization|credential|connectionstring)")
_CONTENT_SECRET = re.compile(
    r"(?i)(C1[_-]?SECRET[_-]?CANARY|SECRET[_-]?CANARY|\b(?:hvs|hvb|hvr)\.[A-Za-z0-9_-]{8,}|"
    r"\b(?:token|secret|credential|dapr[-_]?api[-_]?token|connection[-_]?string)\s*[:=]\s*[\"']?[^\s\"']+|"
    r"postgres(?:ql)?://[^\s:/]+:[^\s@]+@)"
)
_QUERY_CREDENTIAL = re.compile(r"(?i)(password|passwd|token|secret|credential|authorization|api.?key|signature|^sig$|connection.?string)")
_CREDENTIAL_FIELD = re.compile(
    r"(?i)(?:authorization|dapr[-_]?api[-_]?token|password|passwd|token|secret|"
    r"api[-_]?key|client[-_]?secret|(?:access|refresh|id)[-_]?token|connection[-_]?string)\Z"
)
_QUOTED_CREDENTIAL = re.compile(
    r'(?i)"(?:authorization|dapr[-_]?api[-_]?token|password|passwd|token|secret|'
    r'api[-_]?key|client[-_]?secret|(?:access|refresh|id)[-_]?token|connection[-_]?string)"'
    r'\s*:\s*"(?:\\.|[^"\\])+"'
)
# The producer records this entire probe, with variable names and expansions,
# never the runtime credential. No partial probe or other argument is exempt.
_METADATA_PROBE = 'if [ -z "${DAPR_API_TOKEN:-}" ]; then echo "required runtime credential unavailable" >&2; exit 72; fi; metadata="$(wget -qO- --timeout=5 --header="dapr-api-token: ${DAPR_API_TOKEN}" http://127.0.0.1:3500/v1.0/metadata)" || exit $?; case "$metadata" in *"$DAPR_API_TOKEN"*) echo "secret-shaped-output" >&2; exit 73;; esac; printf "%s" "$metadata"'


def _object(value: Any, fields: frozenset[str] | str) -> Mapping:
    expected = frozenset(fields.split()) if type(fields) is str else fields
    if not isinstance(value, Mapping) or set(value) != expected:
        _refuse("artifact-field-set")
    return value


def _array(value: Any, *, nonempty: bool = False) -> tuple:
    if type(value) is not tuple or (nonempty and not value):
        _refuse("artifact-array-required")
    return value


def _content(value: str, depth: int = 0) -> None:
    if _SECRET_TEXT.search(value) or _CONTENT_SECRET.search(value) or _QUOTED_CREDENTIAL.search(value):
        _refuse("artifact-secret-shaped-content")
    # Content may itself describe decoded JSON, including name/value metadata.
    # Check every pair before a decoder could overwrite a duplicate property.
    def credential_pairs(pairs: list[tuple[str, Any]]) -> dict:
        def nonempty(child: Any) -> bool:
            return child is not None and (len(child) > 0 if type(child) in (str, list, dict) else True)
        for key, child in pairs:
            if _CREDENTIAL_FIELD.fullmatch(key) and nonempty(child):
                _refuse("artifact-secret-shaped-content")
        if (any(key == "name" and type(child) is str and _CREDENTIAL_FIELD.fullmatch(child) for key, child in pairs)
                and any(key == "value" and nonempty(child) for key, child in pairs)):
            _refuse("artifact-secret-shaped-content")
        for _, child in pairs:
            if type(child) is str:
                if depth >= MAX_JSON_DEPTH:
                    _refuse("artifact-content-depth-exceeded")
                _content(child, depth + 1)
        return dict(pairs)
    decoder = json.JSONDecoder(object_pairs_hook=credential_pairs)
    for match in re.finditer(r'[\{\[]\s*(?:"|\{|\[)', value):
        try:
            decoder.raw_decode(value, match.start())
        except (json.JSONDecodeError, RecursionError):
            continue


def _text(value: Any, *, limit: int = MAX_STRING_CHARACTERS, controls: bool = False,
          metadata_probe: bool = False, empty: bool = False) -> str:
    if type(value) is not str or (not empty and not value.strip()) or len(value) > limit:
        _refuse("artifact-string-required")
    _validate_string(value)
    if not controls and any(unicodedata.category(c) == "Cc" for c in value):
        _refuse("artifact-string-control")
    if not (metadata_probe and value == _METADATA_PROBE):
        _content(value)
    return value


def _literal(value: Any, expected: Any) -> None:
    if type(value) is not type(expected) or value != expected:
        _refuse("artifact-literal-mismatch")


def _boolean(value: Any) -> bool:
    if type(value) is not bool:
        _refuse("artifact-boolean-required")
    return value


def _digest(value: Any) -> None:
    if type(value) is not str or _DIGEST.fullmatch(value) is None:
        _refuse("artifact-digest-not-canonical")


def _commit(value: Any) -> None:
    if type(value) is not str or _COMMIT.fullmatch(value) is None:
        _refuse("artifact-commit-not-canonical")


def _path(value: Any) -> str:
    # Reuse exactly the existing lexical path rules, without reading a path.
    return _text(EvidenceReference(value, "0" * 64, 1).path, limit=MAX_PATH_CHARACTERS)


def _artifact_ref(value: Any) -> EvidenceReference:
    reference = parse_ref(value)
    _text(reference.path, limit=MAX_PATH_CHARACTERS)
    return reference


def _count(value: Any, *, positive: bool = False) -> int:
    if type(value) is not int or not (1 if positive else 0) <= value <= MAX_INTEGER:
        _refuse("artifact-count-required")
    return value


def _gate(value: Any) -> None:
    if type(value) is not str or _GATE.fullmatch(value) is None:
        _refuse("artifact-gate-invalid")


def _scope(value: Mapping) -> None:
    _text(value["profileId"])
    for field_name in ("profileSha256", "workloadSha256", "targetSha256"):
        _digest(value[field_name])
    session = value["qualificationSessionId"]
    if type(session) is not str or _SESSION.fullmatch(session) is None or _SESSION_SECRET.search(session):
        _refuse("artifact-session-invalid")


def _instant(value: Any, *, spelling: str = "inspection") -> int:
    """Lexical UTC instant as 100 ns ticks, preserving the seventh digit."""
    if type(value) is not str:
        _refuse("artifact-utc-instant-required")
    match = _UTC.fullmatch(value)
    if match is None:
        _refuse("artifact-utc-instant-required")
    year, month, day, hour, minute, second = map(int, match.groups()[:6])
    fraction, offset = match.groups()[6:]
    if (spelling == "capture" and (fraction is None or len(fraction) != 7 or offset != "+00:00")) or (
        spelling == "new" and (fraction is None or len(fraction) != 3 or offset != "Z")
    ):
        _refuse("artifact-utc-spelling")
    try:
        ordinal = date(year, month, day).toordinal()
    except ValueError:
        _refuse("artifact-utc-instant-required")
    if hour > 23 or minute > 59 or second > 59:
        _refuse("artifact-utc-instant-required")
    return ((ordinal * 86400 + hour * 3600 + minute * 60 + second) * 10_000_000
            + int((fraction or "").ljust(7, "0")))


def _interval(value: Mapping, start: str, finish: str, *, spelling: str,
              enclosing: tuple[int, int] | None = None, strict: bool = False) -> tuple[int, int]:
    first = _instant(value[start], spelling=spelling)
    last = _instant(value[finish], spelling=spelling)
    if last < first or (strict and last == first):
        _refuse("artifact-interval-order")
    if enclosing is not None and not enclosing[0] <= first <= last <= enclosing[1]:
        _refuse("artifact-interval-outside-producer")
    return first, last


def powershell_json_bytes(value: Any) -> bytes:
    """Bounded PowerShell default compact JSON; insertion order is significant.

    This separate encoder models ConvertTo-Json for the JSON-only target and
    argument trees. It does not replace J1 or support arbitrary PowerShell types.
    """
    _validate_tree(value)
    encoder = json.JSONEncoder(ensure_ascii=False, separators=(",", ":"), allow_nan=False)
    encoded = bytearray()
    for chunk in encoder.iterencode(_mutable_json(value)):
        for character in ("\x85", "\u2028", "\u2029"):
            chunk = chunk.replace(character, "\\u" + format(ord(character), "04x"))
        data = chunk.encode("utf-8")
        if len(encoded) + len(data) > MAX_ARTIFACT_BYTES:
            _refuse("artifact-byte-budget-exceeded")
        encoded.extend(data)
    return bytes(encoded)


def _json_hash(value: Any, digest: Any) -> None:
    _digest(digest)
    if hashlib.sha256(powershell_json_bytes(value)).hexdigest() != digest:
        _refuse("artifact-json-hash-mismatch")


def _arguments(value: Any, digest: Any) -> tuple:
    arguments = _array(value, nonempty=True)
    for argument in arguments:
        # Whitespace and empty members remain exact process arguments. NUL has
        # no process-argument representation and must never be admitted.
        _text(argument, controls=True, metadata_probe=True, empty=True)
        if "\x00" in argument:
            _refuse("artifact-argument-nul")
    _json_hash(arguments, digest)
    return arguments


def _producer_arguments(value: Mapping, producer: Mapping) -> None:
    arguments = _arguments(producer["arguments"], producer["argumentsSha256"])
    # These are the producer's documented effective parameter receipts. Checking
    # their repeated packet facts is separate from authorizing child grammar.
    if len(arguments) != 10:
        _refuse("artifact-producer-argument-shape")
    for position, expected in enumerate(("-Gate", value["gate"], "-ProfileId", value["profileId"],
                                        "-QualificationSessionId", value["qualificationSessionId"],
                                        "-EvidenceDirectory")):
        _literal(arguments[position], expected)
    _text(arguments[7])
    _literal(arguments[8], "-CommandTimeoutSeconds")
    if re.fullmatch(r"[1-9][0-9]{0,2}", arguments[9]) is None or int(arguments[9]) > 300:
        _refuse("artifact-producer-timeout-invalid")


def _receipt(value: Any, enclosing: tuple[int, int], *, target: bool) -> Mapping:
    fields = _RECEIPT_FIELDS | {"purpose", "sha256"} if target else _RECEIPT_FIELDS
    receipt = _object(value, fields)
    _literal(receipt["executable"], "kubectl" if target else "git")
    arguments = _arguments(receipt["arguments"], receipt["argumentsSha256"])
    _interval(receipt, "startedAtUtc", "finishedAtUtc", spelling="capture", enclosing=enclosing)
    exit_code = receipt["exitCode"]
    if exit_code is not None and (type(exit_code) is not int or not MIN_INTEGER <= exit_code <= MAX_INTEGER):
        _refuse("artifact-exit-code-required")
    safety = receipt["streamSafety"]
    validated = "validated" if target else "hash-only-source-provenance"
    if safety not in (validated, "not-validated") or type(safety) is not str:
        _refuse("artifact-stream-safety-invalid")
    for field_name in ("stdoutSha256", "stderrSha256"):
        if safety == validated:
            _digest(receipt[field_name])
        else:
            _literal(receipt[field_name], None)
    result = _count(receipt["resultCount"])
    failure = _count(receipt["failureCount"])
    _literal(receipt["skipCount"], 0)
    if (result, failure) not in ((1, 0), (0, 1)):
        _refuse("artifact-command-count-summary")
    if result and (exit_code != 0 or safety != validated or (
        target and receipt["stdoutSha256"] == hashlib.sha256(b"").hexdigest()
    )):
        _refuse("artifact-command-result-contradiction")
    if not target and exit_code == 0 and safety == validated and not result:
        _refuse("artifact-command-result-contradiction")
    if target:
        purpose = _text(receipt["purpose"])
        if re.fullmatch(r"(?:current-context|lifecycle-pods(?:-recheck)?|(?:daprd-version|metadata|alpha-opt-in):[a-z0-9][a-z0-9.-]{0,252})", purpose) is None:
            _refuse("artifact-command-purpose-invalid")
        _digest(receipt["sha256"])
        legacy = hashlib.sha256(("kubectl " + "\x1f".join(arguments)).encode("utf-8")).hexdigest()
        if receipt["sha256"] != legacy:
            _refuse("artifact-command-hash-mismatch")
    return receipt


def _serial_receipts(value: tuple) -> list[tuple[int, int]]:
    intervals = []
    for receipt in value:
        interval = _interval(receipt, "startedAtUtc", "finishedAtUtc", spelling="capture")
        if intervals and interval[0] < intervals[-1][1]:
            _refuse("artifact-command-chronology")
        intervals.append(interval)
    return intervals


def _strings(value: Any, *, pattern: str | None = None, limit: int = MAX_STRING_CHARACTERS,
             nonempty: bool = False) -> tuple:
    items = _array(value, nonempty=nonempty)
    for item in items:
        _text(item, limit=limit)
        if pattern is not None and re.fullmatch(pattern, item) is None:
            _refuse("artifact-observation-string-invalid")
    if len(set(items)) != len(items):
        _refuse("artifact-duplicate-array-item")
    return items


def _alpha(value: Any, *, blocked: bool = False) -> Mapping:
    pair = _object(value, "componentIsAlpha allowAlphaComponent")
    for item in pair.values():
        if blocked:
            _literal(item, None)
        else:
            _boolean(item)
    if not blocked and pair["componentIsAlpha"] and not pair["allowAlphaComponent"]:
        _refuse("artifact-alpha-summary-contradiction")
    return pair


def _observations(value: Any, target: Mapping, *, blocked: bool) -> tuple:
    observations = _object(value, _OBSERVATION_FIELDS)
    pods = _array(observations["pods"], nonempty=not blocked)
    _alpha(observations["alphaOptIn"], blocked=blocked)
    summaries = {
        "runtimeVersions": "runtimeVersion", "sidecarImageIds": "sidecarImageId",
        "sidecarImageDigests": "sidecarImageDigest", "appIds": "appId",
        "schedulerConnectedAddresses": "schedulerConnectedAddresses", "actorTypes": "actorTypes",
        "enabledFeatures": "enabledFeatures",
    }
    for field_name in summaries:
        _strings(observations[field_name])
    if blocked:
        if pods or any(observations[key] for key in summaries):
            _refuse("artifact-blocked-observations")
        return pods
    names: set[str] = set()
    uids: set[str] = set()
    for item in pods:
        pod = _object(item, _POD_FIELDS)
        for key, pattern in (("pod", r"[a-z0-9][a-z0-9.-]{0,252}"),
                             ("podUid", r"[A-Za-z0-9][A-Za-z0-9-]{0,127}"),
                             ("runtimeVersion", r"[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?")):
            if re.fullmatch(pattern, _text(pod[key])) is None:
                _refuse("artifact-pod-identity-invalid")
        if pod["pod"] in names or pod["podUid"] in uids:
            _refuse("artifact-duplicate-pod-identity")
        names.add(pod["pod"]); uids.add(pod["podUid"])
        image = _text(pod["sidecarImageId"])
        digest = _text(pod["sidecarImageDigest"])
        if re.fullmatch(r"(?:[A-Za-z0-9._:/@-]+)?sha256:[0-9a-f]{64}", image) is None or (
            re.fullmatch(r"sha256:[0-9a-f]{64}", digest) is None or not image.endswith(digest)
        ):
            _refuse("artifact-image-digest-contradiction")
        _literal(_text(pod["appId"]), target["appId"])
        addresses = _strings(pod["schedulerConnectedAddresses"], pattern=r"[A-Za-z0-9][A-Za-z0-9._-]{0,252}:[0-9]{1,5}", limit=259, nonempty=True)
        if any(not 1 <= int(address.rsplit(":", 1)[1]) <= 65535 for address in addresses):
            _refuse("artifact-scheduler-port-invalid")
        _strings(pod["actorTypes"], pattern=r"[A-Za-z][A-Za-z0-9._-]{0,127}", limit=128, nonempty=True)
        if target["actorType"] not in pod["actorTypes"]:
            _refuse("artifact-declared-actor-missing")
        _strings(pod["enabledFeatures"], pattern=r"[A-Za-z][A-Za-z0-9._/-]{0,127}", limit=128)
        if _alpha(pod["alphaOptIn"]) != observations["alphaOptIn"]:
            _refuse("artifact-alpha-summary-contradiction")
    for summary, field_name in summaries.items():
        expected = set()
        for pod in pods:
            child = pod[field_name]
            expected.update(child if type(child) is tuple else (child,))
        if set(observations[summary]) != expected:
            _refuse("artifact-observation-summary-contradiction")
        if field_name in ("runtimeVersion", "appId", "schedulerConnectedAddresses", "actorTypes", "enabledFeatures"):
            if any(pod[field_name] != pods[0][field_name] for pod in pods):
                _refuse("artifact-pod-summary-contradiction")
    return pods


def _producer_sources(value: Any, *, clean: bool) -> dict[str, Mapping]:
    sources: dict[str, Mapping] = {}
    for item in _array(value, nonempty=True):
        source = _object(item, _SOURCE_FIELDS)
        path = _path(source["source"])
        if path not in _PRODUCER_SOURCE_PATHS or path in sources:
            _refuse("artifact-producer-source-set")
        for key in ("gitNormalizedSha256", "executedBytesSha256"):
            _digest(source[key])
        _commit(source["gitBlobOid"])
        tracked = _boolean(source["trackedAtHead"])
        modified = _boolean(source["modified"])
        matches = _boolean(source["matchesHead"])
        head = source["headGitNormalizedSha256"]
        if tracked:
            _digest(head)
        else:
            _literal(head, None)
        if matches != (head is not None and head == source["gitNormalizedSha256"]):
            _refuse("artifact-source-head-contradiction")
        if clean and (not tracked or modified or not matches):
            _refuse("artifact-clean-source-contradiction")
        sources[path] = source
    if set(sources) != _PRODUCER_SOURCE_PATHS:
        _refuse("artifact-producer-source-set")
    return sources


def _source_receipts(value: Any, receipts: dict[str, Mapping], producer_sources: dict[str, Mapping],
                     pods: tuple, *, observed: bool) -> None:
    sources: dict[str, str] = {}
    for item in _array(value, nonempty=True):
        source = _object(item, "source sha256")
        name = _text(source["source"])
        _digest(source["sha256"])
        if name in sources:
            _refuse("artifact-duplicate-observation-source")
        sources[name] = source["sha256"]
        if name == _PRODUCER_PATH:
            _literal(source["sha256"], producer_sources[name]["executedBytesSha256"])
            continue
        match = re.fullmatch(r"kubectl:(current-context|lifecycle-pods(?:-recheck)?|(?:daprd-version|metadata|alpha-opt-in):[a-z0-9][a-z0-9.-]{0,252}):(stdout|stderr|identity|allowlisted)", name)
        if match is None or match[1] not in receipts:
            _refuse("artifact-observation-source-invalid")
        purpose, stream = match.groups()
        receipt = receipts[purpose]
        if receipt["streamSafety"] != "validated":
            _refuse("artifact-source-stream-contradiction")
        if stream in ("stdout", "stderr"):
            if purpose.startswith("metadata:") or purpose.startswith("lifecycle-pods"):
                _refuse("artifact-observation-source-invalid")
            _literal(source["sha256"], receipt[stream + "Sha256"])
        elif stream == "identity":
            if purpose not in ("lifecycle-pods", "lifecycle-pods-recheck"):
                _refuse("artifact-observation-source-invalid")
            _literal(source["sha256"], receipt["stdoutSha256"])
        elif not purpose.startswith("metadata:"):
            _refuse("artifact-observation-source-invalid")
        elif observed:
            pod = next((pod for pod in pods if purpose == "metadata:" + pod["pod"]), None)
            if pod is None:
                _refuse("artifact-observation-source-invalid")
            projection = {"id": pod["appId"], "runtimeVersion": pod["runtimeVersion"],
                          "schedulerConnectedAddresses": pod["schedulerConnectedAddresses"],
                          "actorTypes": pod["actorTypes"], "enabledFeatures": pod["enabledFeatures"]}
            _json_hash(projection, source["sha256"])
    if _PRODUCER_PATH not in sources:
        _refuse("artifact-producer-source-missing")
    if observed:
        expected = {_PRODUCER_PATH, "kubectl:current-context:stdout", "kubectl:current-context:stderr",
                    "kubectl:lifecycle-pods:identity", "kubectl:lifecycle-pods-recheck:identity"}
        purposes = {"current-context", "lifecycle-pods", "lifecycle-pods-recheck"}
        for pod in pods:
            name = pod["pod"]
            for stem in ("daprd-version", "alpha-opt-in"):
                purposes.add(stem + ":" + name)
                expected.update("kubectl:" + stem + ":" + name + ":" + stream for stream in ("stdout", "stderr"))
            purposes.add("metadata:" + name)
            expected.add("kubectl:metadata:" + name + ":allowlisted")
        if set(sources) != expected or set(receipts) != purposes:
            _refuse("artifact-observation-receipt-summary")


def _root(snapshot: Any, schema: str, fields: frozenset[str]) -> Mapping:
    if type(snapshot) is not JsonSnapshot:
        _refuse("artifact-snapshot-required")
    value = _object(snapshot.value, fields)
    _literal(value["schemaVersion"], _PREFIX + schema)
    return value


def parse_capture(snapshot: JsonSnapshot) -> JsonSnapshot:
    """Inspect the unchanged PG2 C1.15 v2 shape; retain even blocked captures."""
    value = _root(snapshot, "evidence/v2", _CAPTURE_FIELDS)
    _literal(value["gate"], "C1.15")
    _literal(value["profileId"], "PG-ONPREM-2")
    _scope(value)
    _text(value["profileIdentity"]); _text(value["workloadId"])
    _literal(value["gateStatus"], "not-evaluated")
    _literal(value["productionGatePassed"], False)
    _literal(value["productionLifecycleWrites"], "not-evaluated")
    _literal(value["independentDisposition"], "pending")
    status = value["producerStatus"]
    if type(status) is not str or status not in ("observed", "blocked"):
        _refuse("artifact-producer-status-invalid")
    observed = status == "observed"
    target = _object(value["target"], "context namespace selector appId actorType")
    for item in target.values():
        _text(item)
    # Hash order is documented, independently of incoming object order.
    _json_hash({key: target[key] for key in ("context", "namespace", "selector", "appId", "actorType")}, value["targetSha256"])
    _literal(_text(value["namespace"]), target["namespace"])
    _literal(_text(value["targetSelector"]), target["selector"])
    if value["context"] is not None:
        _text(value["context"])
    if observed:
        _literal(value["context"], target["context"])
    producer = _object(value["producer"], "path identity identityAuthentication arguments argumentsSha256 startedAtUtc finishedAtUtc exitCode")
    _literal(producer["path"], _PRODUCER_PATH)
    _literal(producer["identity"], "repository-collector")
    _literal(producer["identityAuthentication"], "not-evaluated")
    _producer_arguments(value, producer)
    interval = _interval(producer, "startedAtUtc", "finishedAtUtc", spelling="capture")
    _literal(value["capturedAtUtc"], producer["startedAtUtc"])
    _literal(producer["exitCode"], 0 if observed else 1)
    _commit(value["sourceCommit"])
    disposition = value["sourceDisposition"]
    if type(disposition) is not str or disposition not in ("clean", "dirty-development"):
        _refuse("artifact-source-disposition-invalid")
    dirty = _boolean(value["worktreeDirty"])
    if disposition == "clean" and dirty:
        _refuse("artifact-clean-source-contradiction")
    producer_sources = _producer_sources(value["producerSources"], clean=disposition == "clean")
    recheck = value["finalSourceRecheck"]
    if type(recheck) is not str or recheck not in ("unchanged", "changed-or-unavailable"):
        _refuse("artifact-source-recheck-invalid")
    blockers = _strings(value["blockers"], pattern=r"[a-z0-9:-]+", nonempty=not observed)
    if (recheck == "changed-or-unavailable") != ("producer-source-changed" in blockers):
        _refuse("artifact-source-recheck-contradiction")
    pods = _observations(value["observations"], target, blocked=not observed)
    _literal(_count(value["resultCount"]), len(pods))
    _literal(_count(value["failureCount"]), len(blockers))
    _literal(value["skipCount"], 0)
    if observed and (blockers or recheck != "unchanged"):
        _refuse("artifact-observed-status-contradiction")
    receipts: dict[str, Mapping] = {}
    for item in _array(value["commands"], nonempty=True):
        receipt = _receipt(item, interval, target=True)
        if receipt["purpose"] in receipts:
            _refuse("artifact-duplicate-command-purpose")
        receipts[receipt["purpose"]] = receipt
        if observed and receipt["failureCount"]:
            _refuse("artifact-observed-command-failure")
    for item in _array(value["sourceCommands"], nonempty=True):
        receipt = _receipt(item, interval, target=False)
        if observed and receipt["failureCount"]:
            _refuse("artifact-observed-source-failure")
    child_intervals = _serial_receipts(value["commands"]) + _serial_receipts(value["sourceCommands"])
    ordered = sorted(child_intervals)
    if any(current[0] < previous[1] for previous, current in zip(ordered, ordered[1:])):
        _refuse("artifact-command-chronology")
    _source_receipts(value["sources"], receipts, producer_sources, pods, observed=observed)
    return snapshot


def _authority_receipt(value: Any) -> None:
    receipt = _object(value, "uri sha256 issuer decisionId")
    _digest(receipt["sha256"])
    _text(receipt["issuer"]); _text(receipt["decisionId"])
    uri = _text(receipt["uri"])
    if (re.fullmatch(r"[A-Za-z][A-Za-z0-9+.-]*:[A-Za-z0-9._~:/?#\[\]@!$&'()*+,;=%-]+", uri) is None
            or re.search(r"%(?![0-9A-Fa-f]{2})", uri)):
        _refuse("artifact-receipt-uri-invalid")
    try:
        decoded = unquote(uri, encoding="utf-8", errors="strict")
        raw_parts = urlsplit(uri)
        authority = unquote(raw_parts.netloc, encoding="utf-8", errors="strict")
        if not raw_parts.scheme or "@" in authority or (
            uri.split(":", 1)[1].startswith("//") and not raw_parts.netloc
        ) or not (raw_parts.netloc or raw_parts.path):
            _refuse("artifact-receipt-uri-invalid")
        if raw_parts.netloc:
            # Generic URI authority syntax only, with no scheme/origin policy.
            if not raw_parts.hostname or re.fullmatch(
                r"(?:\[[^\]]+\]|[A-Za-z0-9._~!$&'()*+,;=%-]+)(?::[0-9]*)?", raw_parts.netloc
            ) is None:
                _refuse("artifact-receipt-uri-invalid")
            raw_parts.port  # Validate numeric port syntax/range without contact.
        for component, pattern in ((raw_parts.path, r"[A-Za-z0-9._~!$&'()*+,;=:@%/-]*"),
                                   (raw_parts.query, r"[A-Za-z0-9._~!$&'()*+,;=:@%/?-]*"),
                                   (raw_parts.fragment, r"[A-Za-z0-9._~!$&'()*+,;=:@%/?-]*")):
            if re.fullmatch(pattern, component) is None:
                _refuse("artifact-receipt-uri-invalid")
        _text(decoded)
        if any(c.isspace() for c in decoded):
            _refuse("artifact-receipt-uri-invalid")
        if "\\" in decoded:
            _refuse("artifact-receipt-uri-invalid")
        for component in (raw_parts.query, raw_parts.fragment):
            for parameter in re.split(r"[&;]", component):
                key, _, content = parameter.partition("=")
                key = unquote_plus(key, encoding="utf-8", errors="strict")
                content = unquote_plus(content, encoding="utf-8", errors="strict")
                if _QUERY_CREDENTIAL.search(key):
                    _refuse("artifact-receipt-uri-credentials")
                _text(key, empty=True); _text(content, empty=True)
                # Inspect the common plus-separated representation as content;
                # the exact URI remains retained and no retrieval rule is chosen.
                _text(content.replace("+", " "), empty=True)
    except (UnicodeDecodeError, ValueError):
        _refuse("artifact-receipt-uri-invalid")


def _decision(value: Mapping, *, spelling: str) -> None:
    decision = value["decision"]
    if type(decision) is not str or decision not in ("accepted", "rejected", "needs-evidence"):
        _refuse("artifact-decision-invalid")
    for reason in _array(value["reasons"], nonempty=True):
        _text(reason)
        if not any(c.isalnum() for c in reason):
            _refuse("artifact-reason-not-substantive")
    _text(value["reviewerPrincipal"], limit=256)
    _text(value["reviewerRole"], limit=256)
    _interval(value, "decidedAtUtc", "expiresAtUtc", spelling=spelling, strict=True)
    _authority_receipt(value["authorityReceipt"])


def parse_disposition(snapshot: JsonSnapshot) -> JsonSnapshot:
    """Inspect asserted review facts without authenticating their authority."""
    value = _root(snapshot, "disposition/v1", _DISPOSITION_FIELDS)
    _scope(value); _gate(value["gate"])
    _artifact_ref(value["capture"])
    _text(value["producerPrincipal"], limit=256)
    independent = _boolean(value["independence"])
    _decision(value, spelling="inspection")
    if ((independent and value["producerPrincipal"] == value["reviewerPrincipal"])
            or (value["decision"] == "accepted" and not independent)):
        _refuse("artifact-accepted-independence-contradiction")
    return snapshot


def _distinct_references(value: Any, *, length: int | None = None) -> tuple[EvidenceReference, ...]:
    references = tuple(_artifact_ref(item) for item in _array(value))
    if length is not None and len(references) != length:
        _refuse("artifact-reference-count")
    if (len({ref.path for ref in references}) != len(references)
            or len({ref.sha256 for ref in references}) != len(references)):
        _refuse("artifact-references-not-distinct")
    return references


def parse_accepted_gate(snapshot: JsonSnapshot) -> JsonSnapshot:
    """Inspect a claimed passed gate; hashes and status confer no acceptance."""
    value = _root(snapshot, "accepted-gate/v1", _GATE_FIELDS)
    _scope(value); _gate(value["gate"])
    _literal(value["status"], "passed")
    _digest(value["registryEntrySha256"]); _commit(value["sourceCommit"])
    _path(value["producerPath"])
    capture = _artifact_ref(value["capture"])
    disposition = _artifact_ref(value["disposition"])
    if capture.path == disposition.path or capture.sha256 == disposition.sha256:
        _refuse("artifact-references-not-distinct")
    _interval(value, "startedAtUtc", "finishedAtUtc", spelling="new")
    _count(value["resultCount"], positive=True)
    _literal(value["failureCount"], 0); _literal(value["skipCount"], 0)
    cleanup = _object(value["cleanup"], "required receipts finalState")
    required = _boolean(cleanup["required"])
    receipts = _distinct_references(cleanup["receipts"])
    if required:
        if not receipts:
            _refuse("artifact-cleanup-receipts-required")
        _literal(cleanup["finalState"], "restored-approved-baseline")
    else:
        if receipts:
            _refuse("artifact-read-only-cleanup-receipts")
        _literal(cleanup["finalState"], "read-only-no-owned-mutation")
    return snapshot


def parse_manifest(snapshot: JsonSnapshot) -> JsonSnapshot:
    """Inspect the exact ordered C1 inventory without loading its references."""
    value = _root(snapshot, "manifest/v1", _MANIFEST_FIELDS)
    _scope(value); _literal(value["checkpoint"], "C1")
    _digest(value["registrySha256"]); _digest(value["policySha256"])
    _artifact_ref(value["sessionAuthority"])
    gates = _array(value["gates"])
    if len(gates) != 25:
        _refuse("artifact-gate-inventory-count")
    references = []
    for number, item in enumerate(gates, 1):
        gate = _object(item, "gate artifact")
        _literal(gate["gate"], "C1." + str(number))
        references.append(gate["artifact"])
    _distinct_references(tuple(references), length=25)
    return snapshot


def parse_bundle_approval(snapshot: JsonSnapshot) -> JsonSnapshot:
    """Inspect one bundle review; two-role principal policy stays separate."""
    value = _root(snapshot, "bundle-approval/v1", _APPROVAL_FIELDS)
    _scope(value); _artifact_ref(value["manifest"])
    _decision(value, spelling="new")
    if value["reviewerRole"] not in ("platform-operations", "security"):
        _refuse("artifact-bundle-role-invalid")
    return snapshot


def parse_predecessor(snapshot: JsonSnapshot) -> JsonSnapshot:
    """Inspect a predecessor summary without emitting execution authority."""
    value = _root(snapshot, "predecessor/v2", _PREDECESSOR_FIELDS)
    _artifact_ref(value["manifest"])
    _distinct_references(value["approvals"], length=2)
    _literal(value["status"], "passed")
    _literal(value["productionLifecycleWrites"], "disabled")
    _literal(value["qualificationAuthorized"], True)
    return snapshot


def parse_artifact(snapshot: JsonSnapshot) -> JsonSnapshot:
    """Strict six-schema inspection dispatch; legacy and other versions refuse."""
    if type(snapshot) is not JsonSnapshot or not isinstance(snapshot.value, Mapping):
        _refuse("artifact-snapshot-required")
    schema = snapshot.value.get("schemaVersion")
    if type(schema) is not str:
        _refuse("artifact-schema-unsupported")
    readers = {
        _PREFIX + "evidence/v2": parse_capture,
        _PREFIX + "disposition/v1": parse_disposition,
        _PREFIX + "accepted-gate/v1": parse_accepted_gate,
        _PREFIX + "manifest/v1": parse_manifest,
        _PREFIX + "bundle-approval/v1": parse_bundle_approval,
        _PREFIX + "predecessor/v2": parse_predecessor,
    }
    reader = readers.get(schema)
    if reader is None:
        _refuse("artifact-schema-unsupported")
    return reader(snapshot)
