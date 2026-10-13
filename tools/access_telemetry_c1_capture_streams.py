"""Pure consistency inspection of explicitly retained PG2 C1.15 outputs.

Caller-supplied bytes and declarations remain untrusted. A match grants no
execution, provenance, custody, registration, authority or gate acceptance.
"""

from __future__ import annotations

from dataclasses import dataclass, field
import hashlib
import json
import math
import re
import unicodedata

from access_telemetry_c1_capture_semantics import inspect_c115_command_declarations
from access_telemetry_c1_interchange import (
    InterchangeFormatError, JsonSnapshot, MAX_AGGREGATE_BYTES, MAX_ARTIFACT_BYTES,
    _CONTENT_SECRET, _QUOTED_CREDENTIAL, _SECRET_TEXT, _refuse,
    powershell_json_bytes,
)


MAX_STREAM_BYTES = MAX_ARTIFACT_BYTES
MAX_STREAM_JSON_DEPTH = 30
MAX_STREAM_INTEGER_CHARACTERS = 380
_STRING_TOKEN = re.compile(r'"(?:[^"\\]|\\.)*"')
_JSON_START = re.compile(r'[\[{]\s*(?:["\[\]{}0-9-]|true\b|false\b|null\b|NaN\b|Infinity\b)')
_SCALAR_NUMBER = re.compile(r'(?:NaN|-?Infinity|-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?)\Z')
_CREDENTIAL = re.compile(
    r'(?i)(?:authorization|dapr[-_]?api[-_]?token|password|passwd|token|secret|credential|'
    r'api[-_]?key|client[-_]?secret|(?:access|refresh|id)[-_]?token|connection[-_]?string)\Z'
)
_RAW_CREDENTIAL = re.compile(
    r'(?i)"(?:authorization|dapr[-_]?api[-_]?token|password|passwd|token|secret|credential|'
    r'api[-_]?key|client[-_]?secret|(?:access|refresh|id)[-_]?token|connection[-_]?string)"'
    r'\s*:\s*(?:"(?:[^"\\]|\\.)+"|\[\s*[^\s\]]|\{\s*[^\s}]|'
    r'(?:true|false)(?=\s|[,}\]]|$)|-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?'
    r'(?:[eE][+-]?[0-9]+)?(?=\s|[,}\]]|$))'
)
_IMAGE = r'(?:[A-Za-z0-9._:/@-]+)?sha256:[0-9a-f]{64}'


@dataclass(frozen=True, slots=True)
class C115CommandStreams:
    """One command purpose and its exact immutable stdout/stderr bytes.

    All caller content is excluded from representations. Construction checks
    closed types; inspection checks budgets, receipt binding and content.
    """

    purpose: str = field(repr=False)
    stdout: bytes = field(repr=False)
    stderr: bytes = field(repr=False)

    def __post_init__(self) -> None:
        if type(self.purpose) is not str:
            _refuse("c115-stream-purpose-required")
        if type(self.stdout) is not bytes or type(self.stderr) is not bytes:
            _refuse("c115-stream-bytes-required")


@dataclass(frozen=True, slots=True)
class C115StreamInspection:
    """Identical capture/stream bytes and complete Pod/count data; no verdict."""

    capture: JsonSnapshot = field(repr=False)
    streams: tuple[C115CommandStreams, ...] = field(repr=False)
    pod_count: int
    pod_names: tuple[str, ...]
    command_count: int


def _whitespace(character: str) -> bool:
    # .NET String.Trim/Boolean.TryParse use Char.IsWhiteSpace, which differs
    # from Python's strip for the four ASCII information separators.
    return character in "\t\n\v\f\r\x85" or unicodedata.category(character) in ("Zs", "Zl", "Zp")


def _trim(text: str, *, boolean: bool = False) -> str:
    start, finish = 0, len(text)
    while start < finish and (_whitespace(text[start]) or (boolean and text[start] == "\0")):
        start += 1
    while start < finish and (_whitespace(text[finish - 1]) or (boolean and text[finish - 1] == "\0")):
        finish -= 1
    return text[start:finish]


def _patterns(text: str) -> None:
    if any(pattern.search(text) for pattern in (_SECRET_TEXT, _CONTENT_SECRET, _QUOTED_CREDENTIAL, _RAW_CREDENTIAL)):
        _refuse("c115-stream-secret-shaped-content")


def _depth(text: str) -> None:
    depth, quoted, escaped = 0, False, False
    for character in text:
        if quoted:
            if escaped:
                escaped = False
            elif character == "\\":
                escaped = True
            elif character == '"':
                quoted = False
        elif character == '"':
            quoted = True
        elif character in "[{":
            depth += 1
            if depth > MAX_STREAM_JSON_DEPTH:
                _refuse("c115-stream-json-depth-exceeded")
        elif character in "]}":
            depth = max(0, depth - 1)


def _pairs(pairs: list[tuple[str, object]]) -> dict:
    result = {}
    for key, value in pairs:
        if key in result:
            _refuse("c115-stream-duplicate-json-field")
        result[key] = value
    return result


def _float(text: str) -> float:
    number = float(text)
    if not math.isfinite(number):
        _refuse("c115-stream-nonfinite-json-number")
    return number


def _constant(_: str) -> None:
    _refuse("c115-stream-nonfinite-json-number")


def _integer(text: str) -> int:
    if len(text) > MAX_STREAM_INTEGER_CHARACTERS:
        _refuse("c115-stream-integer-lexeme-too-long")
    # Avoid Python's configurable string-to-integer conversion limit entirely.
    negative = text.startswith("-")
    number = 0
    for character in text[1:] if negative else text:
        number = number * 10 + ord(character) - ord("0")
    return -number if negative else number


_DECODER = json.JSONDecoder(object_pairs_hook=_pairs, parse_int=_integer, parse_float=_float, parse_constant=_constant)


def _decode(text: str, offset: int = 0) -> tuple[object, int]:
    try:
        return _DECODER.raw_decode(text, offset)
    except InterchangeFormatError:
        raise
    except (ValueError, RecursionError):
        pass
    # Leave the handler before refusing: decoder exceptions retain raw text.
    _refuse("c115-stream-invalid-json")


def _utf8(raw: bytes) -> str:
    try:
        return raw.decode("utf-8", errors="strict")
    except UnicodeDecodeError:
        pass
    # Suppression alone would still expose the stream via __context__.
    _refuse("c115-stream-invalid-utf8")


def _nonempty(value: object) -> bool:
    return value is not None and (bool(value) if type(value) in (str, list, dict) else True)


def _tree(value: object, nesting: int, depth: int = 0) -> None:
    if type(value) in (dict, list) and depth >= MAX_STREAM_JSON_DEPTH:
        _refuse("c115-stream-json-depth-exceeded")
    if type(value) is dict:
        names = [child for key, child in value.items() if key.lower() == "name"]
        values = [child for key, child in value.items() if key.lower() == "value"]
        if (any(type(name) is str and _CREDENTIAL.fullmatch(name) for name in names)
                and any(child is not None and (type(child) is not str or child != "") for child in values)):
            _refuse("c115-stream-secret-shaped-content")
        for key, child in value.items():
            if _CREDENTIAL.fullmatch(key) and _nonempty(child):
                _refuse("c115-stream-secret-shaped-content")
            _scan(key, nesting)
            _tree(child, nesting, depth + 1)
    elif type(value) is list:
        for child in value:
            _tree(child, nesting, depth + 1)
    elif type(value) is str:
        _scan(value, nesting)


def _diagnostic_string(text: str, offset: int) -> str | None:
    try:
        value, _ = _DECODER.raw_decode(text, offset)
        return value
    except (ValueError, RecursionError):
        pass
    # Ordinary quoted diagnostics need not use JSON backslash escapes.
    return None


def _literal_opening(text: str, container: re.Match, token: re.Match) -> bool:
    opening = text[container.start()]
    marker = container.start() + 1
    while marker < token.end() - 1 and text[marker] in " \t\r\n":
        marker += 1
    if marker != token.end() - 1:
        return False
    key = _STRING_TOKEN.match(text, marker)
    if key is None:
        return True
    finish = key.end()
    while finish < len(text) and text[finish] in " \t\r\n":
        finish += 1
    # A closing quote after a literal brace is prose. A complete key/value
    # prefix or JSON delimiter beyond it identifies an actual overlapping frame.
    return finish < len(text) and text[finish] not in (":}" if opening == "{" else ",]:}")


def _scan(text: str, nesting: int = 0) -> None:
    if any(0xD800 <= ord(character) <= 0xDFFF for character in text):
        _refuse("c115-stream-unpaired-unicode-surrogate")
    if nesting > MAX_STREAM_JSON_DEPTH:
        _refuse("c115-stream-json-depth-exceeded")
    _depth(text)
    _patterns(text)
    # Match the producer's raw Unicode escape safety pass, including diagnostics
    # that are not themselves a JSON document.
    decoded = re.sub(r"\\u([0-9a-fA-F]{4})", lambda match: chr(int(match[1], 16)), text)
    _patterns(decoded)
    leading = _trim(text)
    if nesting == 0 and leading in ("{", "["):
        _decode(leading)
    if nesting == 0 and _SCALAR_NUMBER.fullmatch(leading):
        _decode(leading)
    tokens = iter(_STRING_TOKEN.finditer(text))
    containers = iter(_JSON_START.finditer(text))
    token = next(tokens, None)
    container = next(containers, None)
    position = 0
    while position < len(text):
        while token is not None and token.start() < position:
            token = next(tokens, None)
        while container is not None and container.start() < position:
            container = next(containers, None)
        if container is not None and (token is None or container.start() < token.start()):
            # Parse every JSON fragment in diagnostics and every successive Pod
            # frame. A later duplicate or hidden discarded field is still unsafe.
            child, end = _decode(text, container.start())
            _tree(child, nesting + 1)
            position = end
        elif token is not None:
            # Check every overlapping fragment before advancing past a token.
            # An earlier harmless object must not hide later malformed or unsafe
            # JSON behind an unmatched diagnostic quote.
            while container is not None and container.start() < token.end():
                if not _literal_opening(text, container, token):
                    child, end = _decode(text, container.start())
                    _tree(child, nesting + 1)
                    while container is not None and container.start() < end:
                        container = next(containers, None)
                else:
                    container = next(containers, None)
            child = _diagnostic_string(text, token.start())
            if child is not None:
                _tree(child, nesting + 1)
            position = token.end()
        else:
            break


def _json(text: str, *, sequence: bool = False) -> list:
    _depth(text)
    values = []
    position = 0
    while position < len(text):
        # JSON's internal whitespace is deliberately narrower than producer Trim.
        while position < len(text) and text[position] in " \t\r\n":
            position += 1
        if position == len(text):
            break
        value, position = _decode(text, position)
        values.append(value)
        if not sequence:
            if text[position:].strip(" \t\r\n"):
                _refuse("c115-stream-invalid-json")
            break
    if not values:
        _refuse("c115-stream-invalid-json")
    return values


def _object(value: object) -> dict:
    if type(value) is not dict:
        _refuse("c115-stream-observation-mismatch")
    return value


def _array(value: object) -> list:
    if type(value) is not list:
        _refuse("c115-stream-observation-mismatch")
    return value


def _string(value: object, pattern: str, limit: int = 512) -> str:
    if type(value) is not str or len(value) > limit or re.fullmatch(pattern, value) is None:
        _refuse("c115-stream-observation-mismatch")
    return value


def _pods(text: str, app_id: str, *, initial: bool) -> tuple[tuple, ...]:
    identities = []
    names, uids = set(), set()
    for frame in _json(text, sequence=True):
        if type(frame) is not list or len(frame) != 2:
            _refuse("c115-stream-observation-mismatch")
        metadata, status = map(_object, frame)
        if not ("name" in metadata or "uid" in metadata) or "phase" not in status:
            _refuse("c115-stream-observation-mismatch")
        phase = _string(status["phase"], r"[A-Za-z]+")
        if initial:
            _array(status.get("conditions"))
            _array(status.get("containerStatuses"))
        if phase != "Running":
            continue
        name = _string(metadata.get("name"), r"[a-z0-9][a-z0-9.-]{0,252}")
        uid = _string(metadata.get("uid"), r"[A-Za-z0-9][A-Za-z0-9-]{0,127}")
        if name in names or uid in uids:
            _refuse("c115-stream-observation-mismatch")
        names.add(name)
        uids.add(uid)
        label = _object(metadata.get("labels")).get("app.kubernetes.io/name")
        if label != app_id or metadata.get("deletionTimestamp") is not None:
            _refuse("c115-stream-observation-mismatch")
        ready = []
        for condition in _array(status.get("conditions")):
            condition = _object(condition)
            condition_type = _string(condition.get("type"), r"[A-Za-z]+")
            if condition_type == "Ready":
                ready.append(condition)
        if len(ready) != 1 or ready[0].get("status") != "True":
            _refuse("c115-stream-observation-mismatch")
        containers = {}
        for container in _array(status.get("containerStatuses")):
            container = _object(container)
            container_name = _string(container.get("name"), r"[a-z0-9][a-z0-9-]{0,62}")
            if container_name not in ("lifecycle", "daprd"):
                continue
            if container_name in containers or container.get("ready") is not True:
                _refuse("c115-stream-observation-mismatch")
            containers[container_name] = _string(container.get("imageID"), _IMAGE)
        if set(containers) != {"lifecycle", "daprd"}:
            _refuse("c115-stream-observation-mismatch")
        identities.append((name, uid, label, containers["lifecycle"], containers["daprd"]))
    return tuple(identities)


def _collection(value: object, declared: tuple, pattern: str, *, limit: int, nonempty: bool = True) -> tuple:
    items = _array(value)
    for item in items:
        _string(item, pattern, limit)
    if (nonempty and not items) or len(set(items)) != len(items) or set(items) != set(declared):
        _refuse("c115-stream-observation-mismatch")
    # The producer normalizes duplicate-free arrays using case-sensitive culture
    # sorting. Compare exact membership without consulting ambient culture; retain
    # the declared normalized order for its independently checked projection hash.
    return declared


def _metadata(text: str, pod: object, sources: dict, purpose: str) -> None:
    metadata = _object(_json(text)[0])
    if metadata.get("id") != pod["appId"] or metadata.get("runtimeVersion") != pod["runtimeVersion"]:
        _refuse("c115-stream-observation-mismatch")
    scheduler = _object(metadata.get("scheduler"))
    spellings = set(scheduler) & {"connectedAddresses", "connected_addresses"}
    if len(spellings) != 1:
        _refuse("c115-stream-observation-mismatch")
    addresses = _collection(scheduler[next(iter(spellings))], pod["schedulerConnectedAddresses"],
                            r"[A-Za-z0-9][A-Za-z0-9._-]{0,252}:[0-9]{1,5}", limit=259)
    if any(not 1 <= int(address.rsplit(":", 1)[1]) <= 65535 for address in addresses):
        _refuse("c115-stream-observation-mismatch")
    actors = [_object(actor).get("type") for actor in _array(metadata.get("actors"))]
    actor_types = _collection(actors, pod["actorTypes"], r"[A-Za-z][A-Za-z0-9._-]{0,127}", limit=128)
    features = _collection(metadata.get("enabledFeatures"), pod["enabledFeatures"],
                           r"[A-Za-z][A-Za-z0-9._/-]{0,127}", limit=128, nonempty=False)
    projection = {"id": metadata["id"], "runtimeVersion": metadata["runtimeVersion"],
                  "schedulerConnectedAddresses": addresses, "actorTypes": actor_types, "enabledFeatures": features}
    if hashlib.sha256(powershell_json_bytes(projection)).hexdigest() != sources["kubectl:" + purpose + ":allowlisted"]:
        _refuse("c115-stream-observation-mismatch")


def _alpha(text: str, declared: object) -> None:
    lines = re.split(r"\r?\n", text)
    if len(lines) != 2:
        _refuse("c115-stream-observation-mismatch")
    values = []
    for line in lines:
        normalized = _trim(line, boolean=True).lower()
        if normalized not in ("true", "false"):
            _refuse("c115-stream-observation-mismatch")
        values.append(normalized == "true")
    if tuple(values) != (declared["componentIsAlpha"], declared["allowAlphaComponent"]):
        _refuse("c115-stream-observation-mismatch")


def inspect_c115_retained_streams(snapshot: JsonSnapshot, streams: tuple[C115CommandStreams, ...]) -> C115StreamInspection:
    """Bind every exact stream and derive all C1.15 declared observations.

    Require an exact built-in tuple in receipt order. Hash untrimmed bytes;
    trim only for producer-compatible parsing. Each stream is at most 1 MiB,
    and capture plus every occurrence (including repeated bytes) is at most
    32 MiB. No filesystem, process, network, configuration or clock access.
    """
    commands = inspect_c115_command_declarations(snapshot)
    if type(streams) is not tuple:
        _refuse("c115-stream-tuple-required")
    if len(streams) != commands.command_count:
        _refuse("c115-stream-sequence-mismatch")
    if any(type(stream) is not C115CommandStreams for stream in streams):
        _refuse("c115-stream-tuple-required")
    if tuple(stream.purpose for stream in streams) != commands.command_purposes:
        _refuse("c115-stream-sequence-mismatch")
    total = len(commands.capture.raw)
    for stream in streams:
        for raw in (stream.stdout, stream.stderr):
            if type(raw) is not bytes:
                _refuse("c115-stream-bytes-required")
            if len(raw) > MAX_STREAM_BYTES:
                _refuse("c115-stream-byte-budget-exceeded")
            total += len(raw)
            if total > MAX_AGGREGATE_BYTES:
                _refuse("c115-stream-aggregate-byte-budget-exceeded")
    outputs = []
    for receipt, stream in zip(commands.capture.value["commands"], streams):
        text_pair = []
        for name, raw in (("stdout", stream.stdout), ("stderr", stream.stderr)):
            if hashlib.sha256(raw).hexdigest() != receipt[name + "Sha256"]:
                _refuse("c115-stream-hash-mismatch")
            text = _utf8(raw)
            _scan(text)
            text_pair.append(_trim(text))
        outputs.append(text_pair[0])
    value = commands.capture.value
    if outputs[0] != value["target"]["context"]:
        _refuse("c115-stream-observation-mismatch")
    initial = _pods(outputs[1], value["target"]["appId"], initial=True)
    if tuple(identity[0] for identity in initial) != commands.pod_names:
        _refuse("c115-stream-observation-mismatch")
    sources = {source["source"]: source["sha256"] for source in value["sources"]}
    for number, (identity, pod) in enumerate(zip(initial, value["observations"]["pods"])):
        if identity[1] != pod["podUid"] or identity[4] != pod["sidecarImageId"] or not identity[4].endswith(pod["sidecarImageDigest"]):
            _refuse("c115-stream-observation-mismatch")
        offset = 2 + 3 * number
        if outputs[offset] != pod["runtimeVersion"]:
            _refuse("c115-stream-observation-mismatch")
        _metadata(outputs[offset + 1], pod, sources, streams[offset + 1].purpose)
        _alpha(outputs[offset + 2], pod["alphaOptIn"])
    rechecked = _pods(outputs[-1], value["target"]["appId"], initial=False)
    if {identity[0]: identity for identity in initial} != {identity[0]: identity for identity in rechecked}:
        _refuse("c115-stream-recheck-mismatch")
    return C115StreamInspection(commands.capture, streams, len(initial), commands.pod_names, commands.command_count)
