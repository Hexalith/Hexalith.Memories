"""Offline wire primitives for future authenticated C1 interchange.

Parsing and digest matching establish bytes and shape only. These helpers do not
authenticate a producer, reviewer, session, custody root or accepted gate. No
existing qualification consumer uses this module yet.
"""

from __future__ import annotations

from collections.abc import Mapping
from dataclasses import dataclass, field
import hashlib
import json
import re
from types import MappingProxyType
from typing import Any
import unicodedata


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
