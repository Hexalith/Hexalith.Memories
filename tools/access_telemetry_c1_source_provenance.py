"""Offline corroboration of fixture C1.15 sources against one local Git checkout.

No operation here approves a registration or enables a deployed binding.
"""

from __future__ import annotations

from dataclasses import dataclass, field
import os
import select
import stat
import subprocess
import time

from access_telemetry_c1_interchange import JsonSnapshot, MAX_ARTIFACT_BYTES, _commit, _refuse
from access_telemetry_c1_producer_bindings import (
    RegistryInspection, RegistrationInspection, SourceSnapshot, inspect_registration,
)


_DIRECTORY_FLAGS = os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW | os.O_CLOEXEC
_FILE_FLAGS = os.O_RDONLY | os.O_NOFOLLOW | os.O_CLOEXEC | os.O_NONBLOCK


@dataclass(frozen=True, slots=True)
class LocalSourceCorroboration:
    """Local bytes and Git identities; never an acceptance or approval token."""

    registration: RegistrationInspection = field(repr=False)
    source_commit: str
    repository_root: str
    paths: tuple[str, ...]
    git_blob_oids: tuple[str, ...]


def _open_directory(root: str) -> int:
    if type(root) is not str or not root.startswith("/") or root == "/":
        _refuse("repository-root-required")
    components = root[1:].split("/")
    if any(part in ("", ".", "..") for part in components):
        _refuse("repository-root-not-canonical")
    current = os.open("/", _DIRECTORY_FLAGS)
    try:
        for part in components:
            next_fd = os.open(part, _DIRECTORY_FLAGS, dir_fd=current)
            os.close(current)
            current = next_fd
        metadata = os.stat(".git", dir_fd=current, follow_symlinks=False)
        if not stat.S_ISDIR(metadata.st_mode):
            _refuse("local-git-directory-required")
        return current
    except BaseException:
        os.close(current)
        raise


def _read_source(root_fd: int, path: str) -> tuple[bytes, int, tuple[int, int]]:
    parts = path.split("/")
    parent = os.dup(root_fd)
    try:
        for part in parts[:-1]:
            next_fd = os.open(part, _DIRECTORY_FLAGS, dir_fd=parent)
            os.close(parent)
            parent = next_fd
        fd = os.open(parts[-1], _FILE_FLAGS, dir_fd=parent)
        try:
            before = os.fstat(fd)
            if not stat.S_ISREG(before.st_mode) or before.st_nlink != 1 or before.st_size > MAX_ARTIFACT_BYTES:
                _refuse("local-source-not-regular-bounded")
            chunks = []
            total = 0
            while True:
                chunk = os.read(fd, min(65536, MAX_ARTIFACT_BYTES + 1 - total))
                if not chunk:
                    break
                total += len(chunk)
                if total > MAX_ARTIFACT_BYTES:
                    _refuse("local-source-byte-budget-exceeded")
                chunks.append(chunk)
            after = os.fstat(fd)
            if (before.st_dev, before.st_ino, before.st_size, before.st_mtime_ns, before.st_ctime_ns) != (
                after.st_dev, after.st_ino, after.st_size, after.st_mtime_ns, after.st_ctime_ns
            ):
                _refuse("local-source-changed")
            return b"".join(chunks), after.st_mode, (after.st_dev, after.st_ino)
        finally:
            os.close(fd)
    finally:
        os.close(parent)


def _assert_root_identity(root_fd: int, root: str) -> None:
    descriptor = os.fstat(root_fd)
    named = os.stat(root, follow_symlinks=False)
    if (descriptor.st_dev, descriptor.st_ino) != (named.st_dev, named.st_ino):
        _refuse("repository-root-changed")


def _assert_no_alternates(root_fd: int) -> None:
    try:
        os.stat(".git/objects/info/alternates", dir_fd=root_fd, follow_symlinks=False)
    except FileNotFoundError:
        return
    _refuse("local-object-alternates-unsupported")


def _git(root_fd: int, *args: str) -> bytes:
    environment = {
        "PATH": os.defpath,
        "GIT_CONFIG_NOSYSTEM": "1",
        "GIT_CONFIG_GLOBAL": os.devnull,
        "GIT_NO_LAZY_FETCH": "1",
        "GIT_NO_REPLACE_OBJECTS": "1",
        "GIT_OPTIONAL_LOCKS": "0",
        "GIT_TERMINAL_PROMPT": "0",
    }
    command = ["git", "--no-optional-locks", "-c", "core.fsmonitor=false",
               "-c", "core.attributesFile=/dev/null", "-c", "protocol.allow=never",
               "-c", "submodule.recurse=false", *args]
    process = None
    try:
        deadline = time.monotonic() + 10
        process = subprocess.Popen(command, cwd=f"/proc/self/fd/{root_fd}", pass_fds=(root_fd,),
                                   env=environment, stdout=subprocess.PIPE,
                                   stderr=subprocess.DEVNULL)
        if process.stdout is None:
            _refuse("local-git-unavailable")
        output = bytearray()
        while True:
            remaining = deadline - time.monotonic()
            if remaining <= 0 or not select.select([process.stdout], [], [], remaining)[0]:
                _refuse("local-git-unavailable")
            chunk = os.read(process.stdout.fileno(), min(65536, MAX_ARTIFACT_BYTES + 1 - len(output)))
            if not chunk:
                break
            output.extend(chunk)
            if len(output) > MAX_ARTIFACT_BYTES:
                _refuse("local-git-output-budget-exceeded")
        if process.wait(timeout=max(0, deadline - time.monotonic())) != 0:
            _refuse("local-git-unavailable")
        return bytes(output)
    except (OSError, subprocess.TimeoutExpired):
        _refuse("local-git-unavailable")
    finally:
        if process is not None:
            if process.poll() is None:
                process.kill()
            process.wait()
            if process.stdout is not None:
                process.stdout.close()


def _git_line(data: bytes) -> bytes:
    if not data.endswith(b"\n") or data.count(b"\n") != 1:
        _refuse("local-git-output-invalid")
    return data[:-1]


def _assert_revision_and_clean(root_fd: int, root: str, commit: str) -> None:
    if _git_line(_git(root_fd, "rev-parse", "--show-toplevel")) != os.fsencode(root):
        _refuse("foreign-repository-root")
    if _git_line(_git(root_fd, "rev-parse", "--verify", "HEAD^{commit}")) != commit.encode("ascii"):
        _refuse("local-head-commit-mismatch")
    tracked = _git(root_fd, "ls-files", "-v", "-z")
    if not tracked.endswith(b"\0") or any(
        not record.startswith(b"H ") for record in tracked[:-1].split(b"\0")
    ):
        _refuse("local-repository-index-flags-unsupported")
    if _git(root_fd, "status", "--porcelain=v1", "--untracked-files=all", "--ignore-submodules=none"):
        _refuse("local-repository-dirty")


def _assert_index_flags(root_fd: int, path: str) -> None:
    if _git(root_fd, "ls-files", "-v", "-z", "--", path) != b"H " + path.encode("utf-8") + b"\0":
        _refuse("local-source-index-flags-unsupported")


def _tree_blob(root_fd: int, commit: str, path: str, *, missing_code: str = "local-source-blob-mismatch") -> tuple[str, str]:
    tree = _git(root_fd, "ls-tree", "-z", commit, "--", path)
    if not tree:
        _refuse(missing_code)
    if not tree.endswith(b"\0") or tree.count(b"\0") != 1:
        _refuse("local-source-blob-mismatch")
    try:
        head, actual_path = tree[:-1].split(b"\t", 1)
        mode, kind, oid = head.split(b" ")
        if (actual_path != path.encode("utf-8") or mode not in (b"100644", b"100755")
                or kind != b"blob" or len(oid) != 40
                or any(character not in b"0123456789abcdef" for character in oid)):
            _refuse("local-source-blob-mismatch")
        return mode.decode("ascii"), oid.decode("ascii")
    except ValueError:
        _refuse("local-source-blob-mismatch")


def _mode(mode: int) -> str:
    return "100755" if mode & stat.S_IXUSR else "100644"


def _assert_attributes(root_fd: int, path: str) -> None:
    names = ("text", "eol", "filter", "ident", "working-tree-encoding")
    data = _git(root_fd, "check-attr", "-z", *names, "--", path).split(b"\0")
    if len(data) != 16 or data[-1] != b"":
        _refuse("local-source-attributes-unsupported")
    attributes = {}
    for index, name in enumerate(names):
        triple = data[index * 3:index * 3 + 3]
        if triple[0] != path.encode("utf-8") or triple[1] != name.encode("ascii"):
            _refuse("local-source-attributes-unsupported")
        attributes[name] = triple[2]
    if (attributes["text"] not in (b"set", b"auto")
            or attributes["eol"] not in (b"lf", b"crlf")
            or attributes["filter"] != b"unspecified"
            or attributes["ident"] not in (b"unspecified", b"unset")
            or attributes["working-tree-encoding"] != b"unspecified"):
        _refuse("local-source-attributes-unsupported")


def corroborate_local_sources(
    registry: RegistryInspection,
    capture: JsonSnapshot,
    sources: tuple[SourceSnapshot, ...],
    expected_source_commit: str,
    statement: JsonSnapshot,
    command_contract: JsonSnapshot,
    review_role_policy: JsonSnapshot,
    repository_root: str,
) -> LocalSourceCorroboration:
    """Bind exact retained bytes to a clean explicit local checkout and commit.

    All retained and registration checks precede filesystem or Git calls.
    Source descriptors reject aliases; the final sweep checks checkout drift.
    """
    registration = inspect_registration(
        registry, capture, sources, expected_source_commit,
        statement, command_contract, review_role_policy,
    )
    _commit(expected_source_commit)
    try:
        root_fd = _open_directory(repository_root)
        try:
            _assert_root_identity(root_fd, repository_root)
            _assert_no_alternates(root_fd)
            _assert_revision_and_clean(root_fd, repository_root, expected_source_commit)
            by_path = {source.path: source for source in sources}
            identities = []
            inodes = set()
            for inspected in registration.source.sources:
                source = by_path[inspected.path]
                _assert_index_flags(root_fd, inspected.path)
                mode_at_head, oid_at_head = _tree_blob(root_fd, expected_source_commit, inspected.path)
                if (mode_at_head, oid_at_head) != (source.git_mode, inspected.git_blob_oid):
                    _refuse("local-source-blob-mismatch")
                blob = _git(root_fd, "cat-file", "blob", inspected.git_blob_oid)
                if blob != source.blob_bytes:
                    _refuse("local-source-blob-mismatch")
                _assert_attributes(root_fd, inspected.path)
                raw, mode, inode = _read_source(root_fd, inspected.path)
                if raw != source.executed_bytes or _mode(mode) != source.git_mode:
                    _refuse("local-source-executed-bytes-mismatch")
                if inode in inodes:
                    _refuse("local-source-alias")
                inodes.add(inode)
                identities.append(inspected.git_blob_oid)
            auxiliary = (registration.source.entry.registered_story,
                         registration.source.entry.verifier_path)
            if len(set(auxiliary)) != 2 or any(path in by_path for path in auxiliary):
                _refuse("local-auxiliary-source-alias")
            auxiliary_bytes = {}
            for path in auxiliary:
                mode_at_head, oid_at_head = _tree_blob(root_fd, expected_source_commit, path,
                                                       missing_code="local-auxiliary-source-missing")
                _assert_index_flags(root_fd, path)
                blob = _git(root_fd, "cat-file", "blob", oid_at_head)
                _assert_attributes(root_fd, path)
                raw, mode, inode = _read_source(root_fd, path)
                try:
                    normalized = raw.decode("utf-8", errors="strict").replace("\r\n", "\n").encode("utf-8")
                except UnicodeDecodeError:
                    _refuse("local-auxiliary-source-utf8-invalid")
                if normalized != blob or _mode(mode) != mode_at_head:
                    _refuse("local-auxiliary-source-mismatch")
                if inode in inodes:
                    _refuse("local-auxiliary-source-alias")
                inodes.add(inode)
                auxiliary_bytes[path] = raw
            for inspected in registration.source.sources:
                raw, mode, _ = _read_source(root_fd, inspected.path)
                source = by_path[inspected.path]
                if raw != source.executed_bytes or _mode(mode) != source.git_mode:
                    _refuse("local-source-changed")
                _assert_index_flags(root_fd, inspected.path)
                _assert_attributes(root_fd, inspected.path)
            for path in auxiliary:
                raw, _, _ = _read_source(root_fd, path)
                if raw != auxiliary_bytes[path]:
                    _refuse("local-auxiliary-source-changed")
                _assert_index_flags(root_fd, path)
                _assert_attributes(root_fd, path)
            _assert_revision_and_clean(root_fd, repository_root, expected_source_commit)
            _assert_no_alternates(root_fd)
            _assert_root_identity(root_fd, repository_root)
            return LocalSourceCorroboration(
                registration, expected_source_commit, repository_root,
                tuple(item.path for item in registration.source.sources), tuple(identities),
            )
        finally:
            os.close(root_fd)
    except (OSError, UnicodeError):
        _refuse("local-source-unavailable")
