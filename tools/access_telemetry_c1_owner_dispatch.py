"""Offline dispatch from retained owner-plan bytes and fresh local status.

The outside plan digest is a caller-supplied approval assertion. Matching it
establishes the retained plan's exact bytes, not independent authentication or
permission to execute. Resource and cleanup fields grant nothing in this slice.
"""

from __future__ import annotations

from collections.abc import Mapping
from contextlib import ExitStack
from datetime import datetime
import os
import stat
from typing import NoReturn

from access_telemetry_c1_interchange import (
    InterchangeFormatError, JsonSnapshot, MAX_ARTIFACT_BYTES, MAX_PATH_CHARACTERS,
    _SESSION, _SESSION_SECRET, _commit, _digest, _object, _text,
)
from access_telemetry_c1_owner_run import (
    OwnerRunAnchors, OwnerRunInspection, OwnerRunInspectionError, inspect_owner_run,
)


_PLAN_FIELDS = frozenset((
    "recordSha256 policySha256 sessionId sourceCommit targetSha256 custodyRoot statusPath"
).split())
_STATUS_FIELDS = frozenset("planSha256 recordSha256 sessionId withdrawn".split())


class OwnerRunDispatchError(ValueError):
    """A content-free refusal with no partial inspection or execution authority."""


def _refuse(code: str) -> NoReturn:
    raise OwnerRunDispatchError(code) from None


def _session(value: object) -> None:
    _text(value, limit=128)
    if _SESSION.fullmatch(value) is None or _SESSION_SECRET.search(value):
        _refuse("owner-dispatch-session-invalid")


def _canonical_path(value: object) -> str:
    _text(value, limit=MAX_PATH_CHARACTERS)
    if (not value.startswith("/") or value == "/" or "\\" in value
            or any(part in ("", ".", "..") for part in value[1:].split("/"))):
        _refuse("owner-dispatch-path-not-canonical")
    return value


def _identity(metadata: os.stat_result) -> tuple:
    return metadata.st_dev, metadata.st_ino, metadata.st_mode, metadata.st_nlink


def _file_metadata(metadata: os.stat_result) -> tuple:
    # Reading may update access time; content and name changes must remain stable.
    return (_identity(metadata), metadata.st_size, metadata.st_mtime_ns, metadata.st_ctime_ns)


def _read_current_status(status_path: str) -> bytes:
    """Read bounded stable bytes, then revalidate their canonical named location.

    Keep the original ancestor descriptors until every non-following named-entry
    check finishes. Replacement, unlink, rewrite or ancestor movement refuses;
    an obsolete open descriptor cannot stand in for the current plan-named file.
    """
    if any(not hasattr(os, name) for name in ("O_DIRECTORY", "O_NOFOLLOW", "O_NONBLOCK")):
        _refuse("owner-dispatch-current-status-unavailable")
    directory_flags = os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW
    file_flags = os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK
    parts = status_path[1:].split("/")
    unavailable = False
    try:
        with ExitStack() as descriptors:
            parent = os.open("/", directory_flags)
            descriptors.callback(os.close, parent)
            ancestors = [(parent, os.fstat(parent))]
            for part in parts[:-1]:
                parent = os.open(part, directory_flags, dir_fd=parent)
                descriptors.callback(os.close, parent)
                ancestors.append((parent, os.fstat(parent)))
            descriptor = os.open(parts[-1], file_flags, dir_fd=parent)
            descriptors.callback(os.close, descriptor)
            before = os.fstat(descriptor)
            if not stat.S_ISREG(before.st_mode):
                _refuse("owner-dispatch-current-status-unavailable")
            remaining = MAX_ARTIFACT_BYTES + 1
            chunks = []
            while remaining:
                chunk = os.read(descriptor, remaining)
                if not chunk:
                    break
                chunks.append(chunk)
                remaining -= len(chunk)
            raw = b"".join(chunks)
            if (len(raw) != min(before.st_size, MAX_ARTIFACT_BYTES + 1)
                    or _file_metadata(before) != _file_metadata(os.fstat(descriptor))):
                _refuse("owner-dispatch-current-status-drifted")
            named_file = os.stat(parts[-1], dir_fd=parent, follow_symlinks=False)
            if _file_metadata(before) != _file_metadata(named_file):
                _refuse("owner-dispatch-current-status-drifted")
            # Validate the name-to-descriptor chain from leaf back to root so
            # moving an opened directory outside custody never preserves credit.
            for index in range(len(ancestors) - 1, 0, -1):
                named_directory = os.stat(parts[index - 1], dir_fd=ancestors[index - 1][0],
                                          follow_symlinks=False)
                if _identity(named_directory) != _identity(ancestors[index][1]):
                    _refuse("owner-dispatch-current-status-drifted")
            if (_identity(os.stat("/", follow_symlinks=False)) != _identity(ancestors[0][1])
                    or _file_metadata(before) != _file_metadata(os.fstat(descriptor))):
                _refuse("owner-dispatch-current-status-drifted")
    except (OSError, NotImplementedError):
        unavailable = True
    if unavailable:
        # Raise after the handler ends so raw paths and internal failures are
        # absent from the refusal's cause/context chain, not merely hidden.
        _refuse("owner-dispatch-current-status-unavailable")
    return raw


def dispatch_owner_run(
    snapshot: JsonSnapshot,
    plan_bytes: bytes,
    approved_plan_sha256: str,
    *,
    now: datetime,
    action: str,
) -> OwnerRunInspection:
    """Inspect exactly one action after retained-plan and fresh-status binding.

    The caller provides current record bytes and the plan digest retained outside
    that record. The plan schema stays open; only its seven binding fields are
    consumed. Every call freshly reads the plan-named status file immediately
    before the pure inspector, with no cached withdrawal value or fallback.
    """
    if type(snapshot) is not JsonSnapshot:
        _refuse("owner-dispatch-record-snapshot-required")
    format_refusal = None
    try:
        _digest(approved_plan_sha256)
        retained_plan = JsonSnapshot(plan_bytes)
        if retained_plan.sha256 != approved_plan_sha256:
            _refuse("owner-dispatch-plan-digest-mismatch")
        plan = retained_plan.value
        if not isinstance(plan, Mapping) or not _PLAN_FIELDS <= set(plan):
            _refuse("owner-dispatch-plan-bindings-required")
        for name in ("recordSha256", "policySha256", "targetSha256"):
            _digest(plan[name])
        _session(plan["sessionId"])
        _commit(plan["sourceCommit"])
        custody_root = _canonical_path(plan["custodyRoot"])
        status_path = _canonical_path(plan["statusPath"])
        if not status_path.startswith(custody_root + "/"):
            _refuse("owner-dispatch-status-outside-custody")
        retained_record = JsonSnapshot(snapshot.raw)
        if retained_record.sha256 != plan["recordSha256"]:
            _refuse("owner-dispatch-record-digest-mismatch")
        anchors = OwnerRunAnchors(
            plan["recordSha256"], plan["policySha256"], plan["sessionId"],
            plan["sourceCommit"], plan["targetSha256"], custody_root,
        )

        # This is deliberately the last input read before inspection. Unknown
        # state is never turned into False, and no status survives this call.
        status = _object(JsonSnapshot(_read_current_status(status_path)).value, _STATUS_FIELDS)
        for name in ("planSha256", "recordSha256"):
            _digest(status[name])
        _session(status["sessionId"])
        if (status["planSha256"] != retained_plan.sha256
                or status["recordSha256"] != anchors.record_sha256
                or status["sessionId"] != anchors.session_id):
            _refuse("owner-dispatch-status-binding-mismatch")
        if type(status["withdrawn"]) is not bool:
            _refuse("owner-dispatch-current-status-unavailable")
    except InterchangeFormatError as error:
        format_refusal = "owner-dispatch-" + str(error)
    if format_refusal is not None:
        _refuse(format_refusal)
    try:
        return inspect_owner_run(retained_record, anchors, now=now,
                                 withdrawn=status["withdrawn"], action=action)
    except OwnerRunInspectionError as error:
        inspection_refusal = str(error)
    # Preserve the inspector's public refusal type/code while dropping any
    # internal parser exception that may contain untrusted document bytes.
    raise OwnerRunInspectionError(inspection_refusal) from None
