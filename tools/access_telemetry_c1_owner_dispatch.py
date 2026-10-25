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
    return (metadata.st_dev, metadata.st_ino, metadata.st_mode, metadata.st_nlink,
            metadata.st_uid, metadata.st_gid)


def _directory_identity(metadata: os.stat_result) -> tuple:
    # Creating/removing unrelated child directories changes a parent's link count.
    return (metadata.st_dev, metadata.st_ino, metadata.st_mode,
            metadata.st_uid, metadata.st_gid)


def _nanoseconds(metadata: os.stat_result, name: str) -> int:
    value = getattr(metadata, name, None)
    if type(value) is not int:
        _refuse("owner-dispatch-freshness-metadata-unavailable")
    return value


def _file_metadata(metadata: os.stat_result) -> tuple:
    # Reading may update access time; content and name changes must remain stable.
    return (_identity(metadata), metadata.st_size, _nanoseconds(metadata, "st_mtime_ns"),
            _nanoseconds(metadata, "st_ctime_ns"))


def _private(metadata: os.stat_result, effective_uid: int) -> None:
    if metadata.st_uid != effective_uid or metadata.st_mode & 0o077:
        _refuse("owner-dispatch-not-private")


class _HeldFile:
    """Bounded non-following read with descriptors retained by the caller's stack."""

    def __init__(self, descriptor: int, before: os.stat_result,
                 ancestors: list, parts: list[str], *, private_depth: int | None) -> None:
        self.descriptor = descriptor
        self.before = before
        self.ancestors = ancestors
        self.parts = parts
        self.private_depth = private_depth

    def read(self) -> bytes:
        if self.private_depth is not None:
            self.revalidate()
        remaining = MAX_ARTIFACT_BYTES + 1
        chunks = []
        while remaining:
            chunk = os.read(self.descriptor, remaining)
            if not chunk:
                break
            chunks.append(chunk)
            remaining -= len(chunk)
        raw = b"".join(chunks)
        if (len(raw) != min(self.before.st_size, MAX_ARTIFACT_BYTES + 1)
                or _file_metadata(self.before) != _file_metadata(os.fstat(self.descriptor))):
            _refuse("owner-dispatch-current-status-drifted")
        self.revalidate()
        return raw

    def revalidate(self) -> None:
        named = os.stat(self.parts[-1], dir_fd=self.ancestors[-1][0], follow_symlinks=False)
        if (_file_metadata(self.before) != _file_metadata(named)
                or _file_metadata(self.before) != _file_metadata(os.fstat(self.descriptor))):
            _refuse("owner-dispatch-current-status-drifted")
        # Leaf-to-root checks never traverse a replacement or outside symlink.
        for index in range(len(self.ancestors) - 1, 0, -1):
            descriptor, before = self.ancestors[index]
            named = os.stat(self.parts[index - 1], dir_fd=self.ancestors[index - 1][0],
                            follow_symlinks=False)
            if (_directory_identity(named) != _directory_identity(before)
                    or _directory_identity(os.fstat(descriptor)) != _directory_identity(before)):
                _refuse("owner-dispatch-current-status-drifted")
            # Private-directory ctime checks detect observed movement and restored
            # metadata changes. Public ancestor content changes are unrelated;
            # their named identities are still checked. Atime is never compared.
            if (self.private_depth is not None and index >= self.private_depth
                    and (_nanoseconds(named, "st_ctime_ns") != _nanoseconds(before, "st_ctime_ns")
                         or _nanoseconds(os.fstat(descriptor), "st_ctime_ns")
                         != _nanoseconds(before, "st_ctime_ns"))):
                _refuse("owner-dispatch-current-status-drifted")
        root, before = self.ancestors[0]
        if (_directory_identity(os.stat("/", follow_symlinks=False)) != _directory_identity(before)
                or _directory_identity(os.fstat(root)) != _directory_identity(before)
                or _file_metadata(self.before) != _file_metadata(os.fstat(self.descriptor))
                or _file_metadata(self.before) != _file_metadata(os.stat(
                    self.parts[-1], dir_fd=self.ancestors[-1][0], follow_symlinks=False))):
            _refuse("owner-dispatch-current-status-drifted")


def _open_held_file(path: str, descriptors: ExitStack, *,
                    custody_root: str | None = None, effective_uid: int | None = None) -> _HeldFile:
    """Open a canonical path without following links; optionally enforce custody."""
    if any(not hasattr(os, name) for name in ("O_DIRECTORY", "O_NOFOLLOW", "O_NONBLOCK")):
        _refuse("owner-dispatch-current-status-unavailable")
    directory_flags = os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW
    file_flags = os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK
    parts = path[1:].split("/")
    parent = os.open("/", directory_flags)
    descriptors.callback(os.close, parent)
    ancestors = [(parent, os.fstat(parent))]
    private = custody_root is not None
    root_depth = len(custody_root[1:].split("/")) if private else len(parts)
    for depth, part in enumerate(parts[:-1], 1):
        parent = os.open(part, directory_flags, dir_fd=parent)
        descriptors.callback(os.close, parent)
        metadata = os.fstat(parent)
        if not stat.S_ISDIR(metadata.st_mode):
            _refuse("owner-dispatch-current-status-unavailable")
        if private and depth >= root_depth:
            _private(metadata, effective_uid)
            _nanoseconds(metadata, "st_ctime_ns")
        ancestors.append((parent, metadata))
    descriptor = os.open(parts[-1], file_flags, dir_fd=parent)
    descriptors.callback(os.close, descriptor)
    before = os.fstat(descriptor)
    if not stat.S_ISREG(before.st_mode):
        _refuse("owner-dispatch-current-status-unavailable")
    if private:
        _private(before, effective_uid)
        if before.st_nlink != 1:
            _refuse("owner-dispatch-single-link-required")
    _file_metadata(before)
    return _HeldFile(descriptor, before, ancestors, parts,
                     private_depth=root_depth if private else None)


def _read_current_status(status_path: str) -> bytes:
    """Read bounded stable bytes, then revalidate their canonical named location."""
    unavailable = False
    try:
        with ExitStack() as descriptors:
            raw = _open_held_file(status_path, descriptors).read()
    except (OSError, NotImplementedError):
        unavailable = True
    if unavailable:
        # Raise after the handler ends so raw paths and internal failures are
        # absent from the refusal's cause/context chain, not merely hidden.
        _refuse("owner-dispatch-current-status-unavailable")
    return raw


def _validated_plan(plan_bytes: bytes, approved_plan_sha256: str) -> JsonSnapshot:
    """Validate the byte dispatcher's seven bindings, leaving other fields open."""
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
    return retained_plan


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
    return _dispatch_owner_run(snapshot, plan_bytes, approved_plan_sha256,
                               now=now, action=action, status_reader=_read_current_status)


def _dispatch_owner_run(snapshot: JsonSnapshot, plan_bytes: bytes, approved_plan_sha256: str,
                        *, now: datetime, action: str, status_reader, profile_binding=None) -> OwnerRunInspection:
    """Shared binding/inspection path with a per-call current status reader."""
    if type(snapshot) is not JsonSnapshot:
        _refuse("owner-dispatch-record-snapshot-required")
    format_refusal = None
    try:
        retained_plan = _validated_plan(plan_bytes, approved_plan_sha256)
        plan = retained_plan.value
        custody_root = plan["custodyRoot"]
        status_path = plan["statusPath"]
        retained_record = JsonSnapshot(snapshot.raw)
        if retained_record.sha256 != plan["recordSha256"]:
            _refuse("owner-dispatch-record-digest-mismatch")
        anchors = OwnerRunAnchors(
            plan["recordSha256"], plan["policySha256"], plan["sessionId"],
            plan["sourceCommit"], plan["targetSha256"], custody_root,
        )

        # This is deliberately the last input read before inspection. Unknown
        # state is never turned into False, and no status survives this call.
        status = _object(JsonSnapshot(status_reader(status_path)).value, _STATUS_FIELDS)
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
                                 withdrawn=status["withdrawn"], action=action, profile_binding=profile_binding)
    except OwnerRunInspectionError as error:
        inspection_refusal = str(error)
    # Preserve the inspector's public refusal type/code while dropping any
    # internal parser exception that may contain untrusted document bytes.
    raise OwnerRunInspectionError(inspection_refusal) from None
