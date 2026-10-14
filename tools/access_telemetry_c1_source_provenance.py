"""Offline corroboration of fixture C1.15 sources against one local Git checkout.

No operation here approves a registration or enables a deployed binding.
"""

from __future__ import annotations

from dataclasses import dataclass, field
import os
import re
import select
import signal
import stat
import subprocess
import time

from access_telemetry_c1_interchange import JsonSnapshot, MAX_ARTIFACT_BYTES, _commit, _refuse
from access_telemetry_c1_producer_bindings import (
    RegistryInspection, RegistrationInspection, SourceInspection, SourceSnapshot, inspect_registration,
)


_DIRECTORY_FLAGS = os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW | os.O_CLOEXEC
_FILE_FLAGS = os.O_RDONLY | os.O_NOFOLLOW | os.O_CLOEXEC | os.O_NONBLOCK
_OWNER_LOCAL_ATTRIBUTE_BYTES = 32 * MAX_ARTIFACT_BYTES
_OWNER_ATTRIBUTE_ARGV_BYTES = 32768

# Trusted launcher source is passed literally to the installed interpreter;
# neither candidate files nor repository Python modules are imported. All setup
# occurs in the deadline-controlled child, never a parent preexec callback.
_OWNER_LAUNCHER = r'''
import ctypes, os, sys
try:
    if sys.platform != "linux" or os.uname().machine != "x86_64":
        raise RuntimeError()
    class Filter(ctypes.Structure):
        _fields_ = [("code", ctypes.c_ushort), ("jt", ctypes.c_ubyte),
                    ("jf", ctypes.c_ubyte), ("k", ctypes.c_uint)]
    class Program(ctypes.Structure):
        _fields_ = [("len", ctypes.c_ushort), ("filter", ctypes.POINTER(Filter))]
    libc = ctypes.CDLL(None, use_errno=True)
    git = ctypes.create_string_buffer(b"/usr/bin/git")
    argv_bytes = [b"/usr/bin/git", *(os.fsencode(a) for a in sys.argv[1:])]
    argv = (ctypes.c_char_p * (len(argv_bytes) + 1))(*argv_bytes, None)
    env_bytes = [os.fsencode(k + "=" + v) for k, v in os.environ.items()]
    env = (ctypes.c_char_p * (len(env_bytes) + 1))(*env_bytes, None)
    # Native x86-64 ABI only; the default denies unknown/new and x32 calls.
    allow, deny, kill = 0x7fff0000, 0x00050000 | 1, 0x80000000
    instructions = [(0x20, 0, 0, 4), (0x15, 1, 0, 0xc000003e),
                    (0x06, 0, 0, kill), (0x20, 0, 0, 0)]
    # Read-only file operations and ordinary single-threaded ELF/Git runtime.
    # No clone/fork/vfork, sockets, ptrace, filesystem mutation, io_uring,
    # ioctl, mount, process-group changes or filter replacement is admitted.
    for nr in (0, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 17,
               19, 21, 23, 24, 25, 28, 39, 60, 63, 78, 79,
               80, 81, 89, 95, 96, 97, 98, 99, 100, 102, 104, 107,
               108, 110, 111, 137, 138, 158, 186, 202, 204, 217,
               218, 219, 228, 229, 230, 231, 262, 267, 269,
               273, 302, 318, 332, 334):
        instructions.extend(((0x15, 0, 1, nr), (0x06, 0, 0, allow)))
    # Git startup opens /dev/null O_RDWR. Existing O_RDWR descriptors are
    # admitted, but file writes/truncation/ioctl and writable shared mappings
    # remain denied. No creating, truncating, appending or write-only open.
    for nr, offset in ((2, 24), (257, 32)):
        instructions.extend(((0x15, 0, 4, nr), (0x20, 0, 0, offset),
                             (0x45, 1, 0, 1 | 64 | 128 | 512 | 1024 | 0x400000),
                             (0x06, 0, 0, allow), (0x06, 0, 0, deny)))
    # Writes are confined to the inherited output pipe and /dev/null stderr.
    for nr in (1, 20):
        instructions.extend(((0x15, 0, 5, nr), (0x20, 0, 0, 16),
                             (0x15, 2, 0, 1), (0x15, 1, 0, 2),
                             (0x06, 0, 0, deny), (0x06, 0, 0, allow)))
    # Preserve stdout/stderr identity: closing either and reopening an existing
    # O_RDWR file must not move that file into the write-permitted slot.
    instructions.extend(((0x15, 0, 5, 3), (0x20, 0, 0, 16),
                         (0x15, 1, 0, 1), (0x15, 0, 1, 2),
                         (0x06, 0, 0, deny), (0x06, 0, 0, allow)))
    # Deny all shared mmap, including read-only mappings that a later
    # mprotect could make writable. Private mappings remain available.
    instructions.extend(((0x15, 0, 4, 9), (0x20, 0, 0, 40),
                         (0x45, 0, 1, 1), (0x06, 0, 0, deny),
                         (0x06, 0, 0, allow)))
    # Query-only fcntl: no duplication or CLOEXEC changes on inherited output.
    instructions.extend(((0x15, 0, 5, 72), (0x20, 0, 0, 24),
                         (0x15, 2, 0, 1), (0x15, 1, 0, 3),
                         (0x06, 0, 0, deny), (0x06, 0, 0, allow)))
    # Restrict execve's pathname argument to the prepared buffer address. This
    # tests pointer equality, not its contents or the installed executable.
    # The literal launcher and installed Python/Git are trusted assumptions;
    # their ordinary Git helper paths use other addresses/process creation.
    pointer = ctypes.addressof(git)
    instructions.extend(((0x15, 0, 6, 59), (0x20, 0, 0, 16),
                         (0x15, 0, 3, pointer & 0xffffffff),
                         (0x20, 0, 0, 20), (0x15, 0, 1, pointer >> 32),
                         (0x06, 0, 0, allow), (0x06, 0, 0, deny),
                         (0x06, 0, 0, deny)))
    filters = (Filter * len(instructions))(*(Filter(*i) for i in instructions))
    program = Program(len(filters), filters)
    if libc.prctl(38, 1, 0, 0, 0) != 0 or libc.prctl(22, 2, ctypes.byref(program), 0, 0) != 0:
        raise RuntimeError()
    libc.execve(ctypes.cast(git, ctypes.c_char_p), argv, env)
except BaseException:
    pass
os._exit(126)
'''

_OWNER_GIT_PREFIX = (
    "--no-optional-locks", "-c", "core.fsmonitor=false",
    "-c", "core.attributesFile=/dev/null", "-c", "protocol.allow=never",
    "-c", "submodule.recurse=false", "-c", "core.filemode=true",
    "-c", "core.trustctime=true", "-c", "core.checkstat=default",
    "-c", "core.ignorestat=false", "-c", "core.ignorecase=false",
    "-c", "core.preloadIndex=false", "-c", "index.threads=1",
)


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


def _owner_git(root_fd: int, *args: str) -> bytes:
    """Run only fixed Git reads under an enforced, owned Linux child group."""
    if not args or args[0] not in ("rev-parse", "ls-files", "status", "ls-tree", "cat-file", "check-attr"):
        _refuse("local-git-command-unsupported")
    environment = {
        "PATH": os.defpath, "GIT_CONFIG_NOSYSTEM": "1", "GIT_CONFIG_GLOBAL": os.devnull,
        "GIT_NO_LAZY_FETCH": "1", "GIT_NO_REPLACE_OBJECTS": "1", "GIT_OPTIONAL_LOCKS": "0",
        "GIT_TERMINAL_PROMPT": "0",
    }
    process = None
    try:
        deadline = time.monotonic() + 10
        command = ["/usr/bin/python3", "-I", "-S", "-B", "-c", _OWNER_LAUNCHER,
                   *_OWNER_GIT_PREFIX, *args]
        process = subprocess.Popen(command, cwd=f"/proc/self/fd/{root_fd}", pass_fds=(root_fd,),
                                   env=environment, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                                   stderr=subprocess.DEVNULL, start_new_session=True)
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
            # Kill the group even if its leader exited while a descendant held
            # stdout open. Production children cannot create descendants under
            # seccomp; group cleanup also contains failed/injected launchers.
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
            process.wait()
            if process.stdout is not None:
                process.stdout.close()


def _git_line(data: bytes) -> bytes:
    if not data.endswith(b"\n") or data.count(b"\n") != 1:
        _refuse("local-git-output-invalid")
    return data[:-1]


def _assert_revision_and_clean(root_fd: int, root: str, commit: str, *, git=None) -> None:
    git = _git if git is None else git
    if _git_line(git(root_fd, "rev-parse", "--show-toplevel")) != os.fsencode(root):
        _refuse("foreign-repository-root")
    if _git_line(git(root_fd, "rev-parse", "--verify", "HEAD^{commit}")) != commit.encode("ascii"):
        _refuse("local-head-commit-mismatch")
    tracked = git(root_fd, "ls-files", "-v", "-z")
    if not tracked.endswith(b"\0") or any(
        not record.startswith(b"H ") for record in tracked[:-1].split(b"\0")
    ):
        _refuse("local-repository-index-flags-unsupported")
    ignored = "all" if git is _owner_git else "none"
    if git(root_fd, "status", "--porcelain=v1", "--untracked-files=all", "--ignore-submodules=" + ignored):
        _refuse("local-repository-dirty")


def _assert_index_flags(root_fd: int, path: str, *, git=None) -> None:
    git = _git if git is None else git
    if git(root_fd, "ls-files", "-v", "-z", "--", path) != b"H " + path.encode("utf-8") + b"\0":
        _refuse("local-source-index-flags-unsupported")


def _tree_blob(root_fd: int, commit: str, path: str, *, missing_code: str = "local-source-blob-mismatch", git=None) -> tuple[str, str]:
    git = _git if git is None else git
    tree = git(root_fd, "ls-tree", "-z", commit, "--", path)
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


def _assert_attributes(root_fd: int, path: str, *, git=None) -> None:
    git = _git if git is None else git
    names = ("text", "eol", "filter", "ident", "working-tree-encoding")
    data = git(root_fd, "check-attr", "-z", *names, "--", path).split(b"\0")
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


def _uninitialized_gitlink(root_fd: int, path: str) -> tuple[int, int] | None:
    """Accept only an absent checkout or an empty no-follow Git-link directory."""
    current = os.dup(root_fd)
    try:
        for part in path.split("/"):
            try:
                next_fd = os.open(part, _DIRECTORY_FLAGS, dir_fd=current)
            except FileNotFoundError:
                return None
            os.close(current)
            current = next_fd
        with os.scandir(current) as entries:
            if next(entries, None) is not None:
                _refuse("local-gitlinks-unsupported")
        metadata = os.fstat(current)
        return metadata.st_dev, metadata.st_ino
    finally:
        os.close(current)


def _assert_safe_config(raw: bytes) -> None:
    """Inspect a closed Git config grammar without invoking Git or includes.

    Ordinary Git-written core/user and quoted remote/branch sections are
    supported. Continuations, legacy dotted sections, extra scopes and command
    keys fail closed. Values admit only Git's documented simple quoted escapes.
    """
    safe_keys = frozenset((
        "core.repositoryformatversion", "core.filemode", "core.bare", "core.logallrefupdates",
        "core.ignorecase", "core.precomposeunicode", "core.quotepath", "core.abbrev",
        "core.autocrlf", "core.eol", "core.safecrlf", "core.whitespace", "core.hookspath",
        "core.checkstat", "core.trustctime", "core.fsmonitor", "user.name", "user.email",
    ))
    section = None
    text = raw.decode("utf-8", errors="strict")
    if "\x00" in text or any(ord(c) < 32 and c not in "\r\n\t" for c in text):
        _refuse("local-git-configuration-unsupported")
    for line in text.splitlines():
        line = line.strip()
        if not line or line.startswith(("#", ";")):
            continue
        if line.startswith("["):
            match = re.fullmatch(r'\[([A-Za-z][A-Za-z0-9-]*)(?:[ \t]+"((?:[^"\\\x00-\x1f\x7f]|\\["\\])+)")?\][ \t]*(?:[#;].*)?', line)
            if match is None:
                _refuse("local-git-configuration-unsupported")
            section = (match[1].lower(), match[2])
            if (section[0] in ("core", "user") and section[1] is None
                    or section[0] in ("remote", "branch") and section[1] is not None):
                continue
            _refuse("local-git-configuration-unsupported")
        match = re.fullmatch(r'([A-Za-z][A-Za-z0-9-]*)[ \t]*=[ \t]*(.*)', line)
        if section is None or match is None:
            _refuse("local-git-configuration-unsupported")
        key, value = match[1].lower(), match[2]
        if section[1] is None:
            permitted = section[0] + "." + key in safe_keys
        else:
            permitted = key in (("url", "fetch", "pushurl") if section[0] == "remote" else
                                ("remote", "merge", "rebase", "vscode-merge-base"))
        if not permitted:
            _refuse("local-git-configuration-unsupported")
        quoted = escaped = False
        for character in value:
            if escaped:
                if character not in 'ntb"\\':
                    _refuse("local-git-configuration-unsupported")
                escaped = False
            elif character == "\\":
                escaped = True
            elif character == '"':
                quoted = not quoted
            elif character in "#;" and not quoted:
                break
        if quoted or escaped:
            _refuse("local-git-configuration-unsupported")
        # Standalone SHA-1 repositories only. Other layout/object versions
        # are outside the supported closed config even if Git can parse them.
        if section[0] == "core" and key in ("bare", "repositoryformatversion"):
            plain = re.split(r"[#;]", value, maxsplit=1)[0].strip().strip('"').lower()
            if plain not in (("false", "no", "off", "0") if key == "bare" else ("0",)):
                _refuse("local-git-configuration-unsupported")


def _owner_metadata(root_fd: int) -> tuple:
    """Bounded no-follow traversal of Git-owned directories and files.

    Each component is opened relative to a checked descriptor. Record identity,
    type/mode, size and timestamps for final comparison; no metadata symlink,
    hard-link file, device, FIFO or aliased directory is accepted.
    """
    records = []
    identities = set()
    byte_count = 0

    def visit(parent, name, path):
        nonlocal byte_count
        if path.count("/") > 64:
            _refuse("local-git-metadata-budget-exceeded")
        before = os.stat(name, dir_fd=parent, follow_symlinks=False)
        if stat.S_ISDIR(before.st_mode):
            flags = _DIRECTORY_FLAGS
        elif stat.S_ISREG(before.st_mode) and before.st_nlink == 1:
            flags = _FILE_FLAGS
        else:
            _refuse("local-git-metadata-unsupported")
        fd = os.open(name, flags, dir_fd=parent)
        try:
            after = os.fstat(fd)
            state = (after.st_dev, after.st_ino, after.st_mode, after.st_size,
                     after.st_mtime_ns, after.st_ctime_ns)
            if state != (before.st_dev, before.st_ino, before.st_mode, before.st_size,
                         before.st_mtime_ns, before.st_ctime_ns):
                _refuse("local-git-metadata-changed")
            identity = state[:2]
            if identity in identities:
                _refuse("local-git-metadata-unsupported")
            identities.add(identity)
            byte_count += len(os.fsencode(path)) + 64
            if len(records) >= 8192 or byte_count > MAX_ARTIFACT_BYTES:
                _refuse("local-git-metadata-budget-exceeded")
            records.append((path, state))
            if stat.S_ISDIR(after.st_mode):
                with os.scandir(fd) as entries:
                    for entry in entries:
                        visit(fd, entry.name, path + "/" + entry.name)
                final = os.fstat(fd)
                if (final.st_dev, final.st_ino, final.st_mode, final.st_size,
                        final.st_mtime_ns, final.st_ctime_ns) != state:
                    _refuse("local-git-metadata-changed")
        finally:
            os.close(fd)

    visit(root_fd, ".git", ".git")
    return tuple(sorted(records))


def _native_index_path(raw: bytes) -> str:
    """Accept bounded relative native UTF-8 paths, independently of evidence Refs."""
    path = raw.decode("utf-8", errors="strict")
    if (not raw or len(raw) > 4096 or b"\0" in raw or path.startswith("/")
            or any(part in ("", ".", "..", ".git") for part in path.split("/"))):
        _refuse("local-tracked-paths-unsupported")
    return path


def _owner_filter_batches(root_fd: int, paths: list[str]) -> None:
    """Bound argv bytes/pointers conservatively even at Linux's minimum ARG_MAX."""
    batches = []
    batch = []
    size = 0
    for path in paths:
        cost = len(path.encode("utf-8")) + 1 + 8
        if batch and size + cost > _OWNER_ATTRIBUTE_ARGV_BYTES:
            batches.append(tuple(batch))
            batch, size = [], 0
        batch.append(path)
        size += cost
    if batch:
        batches.append(tuple(batch))
    for batch in batches:
        attributes = _owner_git(root_fd, "check-attr", "-z", "filter", "--", *batch).split(b"\0")
        if len(attributes) != 3 * len(batch) + 1 or attributes[-1] != b"":
            _refuse("local-checkout-filter-unsupported")
        for number, path in enumerate(batch):
            actual_path, name, value = attributes[number * 3:number * 3 + 3]
            if (actual_path != path.encode("utf-8") or name != b"filter"
                    or value not in (b"unspecified", b"unset")):
                _refuse("local-checkout-filter-unsupported")


def _assert_owner_checkout_safe(root_fd: int) -> tuple:
    """Independent config/metadata checks before any repository-aware Git."""
    metadata = _owner_metadata(root_fd)
    for path in (".git/commondir", ".git/config.worktree"):
        try:
            os.stat(path, dir_fd=root_fd, follow_symlinks=False)
        except FileNotFoundError:
            continue
        _refuse("local-git-layout-unsupported")
    _assert_no_alternates(root_fd)
    original_config = _read_source(root_fd, ".git/config")
    _assert_safe_config(original_config[0])
    try:
        info_attributes = _read_source(root_fd, ".git/info/attributes")
    except FileNotFoundError:
        info_attributes = None
    retained_attributes = [(".git/info/attributes", info_attributes)]
    attribute_bytes = 0 if info_attributes is None else len(info_attributes[0])
    tracked = _owner_git(root_fd, "ls-files", "--stage", "-z")
    if not tracked or not tracked.endswith(b"\0"):
        _refuse("local-tracked-paths-unsupported")
    paths = []
    gitlinks = []
    attribute_paths = {".gitattributes", ".git/info/attributes"}
    attribute_path_bytes = sum(len(path.encode("utf-8")) + 64 for path in attribute_paths)

    def retain_attribute_path(path):
        nonlocal attribute_path_bytes
        if path not in attribute_paths:
            attribute_path_bytes += len(path.encode("utf-8")) + 64
            if len(attribute_paths) >= 8192 or attribute_path_bytes > MAX_ARTIFACT_BYTES:
                _refuse("local-checkout-attribute-budget-exceeded")
            attribute_paths.add(path)

    for record in tracked[:-1].split(b"\0"):
        head, separator, raw_path = record.partition(b"\t")
        parts = head.split(b" ")
        if (not separator or len(parts) != 3 or parts[0] not in (b"100644", b"100755", b"160000")
                or parts[2] != b"0" or len(parts[1]) != 40
                or any(character not in b"0123456789abcdef" for character in parts[1])):
            _refuse("local-tracked-paths-unsupported")
        path = _native_index_path(raw_path)
        paths.append(path)
        if parts[0] == b"160000":
            directory = _uninitialized_gitlink(root_fd, path)
            if directory is None:
                _refuse("local-repository-dirty")
            gitlinks.append((path, directory))
            if _owner_git(root_fd, "ls-tree", "-z", "HEAD", "--", path) != (
                    b"160000 commit " + parts[1] + b"\t" + raw_path + b"\0"):
                _refuse("local-repository-dirty")
        else:
            components = path.split("/")[:-1]
            for count in range(1, len(components) + 1):
                retain_attribute_path("/".join(components[:count]) + "/.gitattributes")
    if len(set(paths)) != len(paths):
        _refuse("local-tracked-paths-unsupported")
    for path in sorted(attribute_paths):
        if path == ".git/info/attributes":
            continue
        try:
            value = _read_source(root_fd, path)
        except FileNotFoundError:
            value = None
        if value is not None:
            attribute_bytes += len(value[0])
            if attribute_bytes > _OWNER_LOCAL_ATTRIBUTE_BYTES:
                _refuse("local-checkout-attribute-budget-exceeded")
        retained_attributes.append((path, value))
    _owner_filter_batches(root_fd, paths)
    if _read_source(root_fd, ".git/config") != original_config:
        _refuse("local-git-configuration-changed")
    if _owner_metadata(root_fd) != metadata:
        _refuse("local-git-metadata-changed")
    return original_config, metadata, tuple(gitlinks), tuple(retained_attributes), tracked


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
    identities = _corroborate_inspected_sources(registration.source, sources, repository_root)
    return LocalSourceCorroboration(
        registration, expected_source_commit, repository_root,
        tuple(item.path for item in registration.source.sources), identities,
    )


def _corroborate_inspected_sources(
    source_inspection: SourceInspection,
    sources: tuple[SourceSnapshot, ...],
    repository_root: str,
    *,
    owner_safety_preflight: bool = False,
) -> tuple[str, ...]:
    """Reuse the bounded local lane after the caller's complete pure inspection.

    Owner callers must supply their detached reinspected sources. This helper
    neither chooses a registration policy nor performs target operations.
    """
    expected_source_commit = source_inspection.source_commit
    _commit(expected_source_commit)
    try:
        root_fd = _open_directory(repository_root)
        try:
            _assert_root_identity(root_fd, repository_root)
            git = _owner_git if owner_safety_preflight else _git
            safety = None
            if owner_safety_preflight:
                safety = _assert_owner_checkout_safe(root_fd)
            else:
                _assert_no_alternates(root_fd)
            _assert_revision_and_clean(root_fd, repository_root, expected_source_commit, git=git)
            by_path = {source.path: source for source in sources}
            identities = []
            inodes = set()
            for inspected in source_inspection.sources:
                source = by_path[inspected.path]
                _assert_index_flags(root_fd, inspected.path, git=git)
                mode_at_head, oid_at_head = _tree_blob(root_fd, expected_source_commit, inspected.path, git=git)
                if (mode_at_head, oid_at_head) != (source.git_mode, inspected.git_blob_oid):
                    _refuse("local-source-blob-mismatch")
                blob = git(root_fd, "cat-file", "blob", inspected.git_blob_oid)
                if blob != source.blob_bytes:
                    _refuse("local-source-blob-mismatch")
                _assert_attributes(root_fd, inspected.path, git=git)
                raw, mode, inode = _read_source(root_fd, inspected.path)
                if raw != source.executed_bytes or _mode(mode) != source.git_mode:
                    _refuse("local-source-executed-bytes-mismatch")
                if inode in inodes:
                    _refuse("local-source-alias")
                inodes.add(inode)
                identities.append(inspected.git_blob_oid)
            auxiliary = (source_inspection.entry.registered_story,
                         source_inspection.entry.verifier_path)
            if len(set(auxiliary)) != 2 or any(path in by_path for path in auxiliary):
                _refuse("local-auxiliary-source-alias")
            auxiliary_bytes = {}
            for path in auxiliary:
                mode_at_head, oid_at_head = _tree_blob(root_fd, expected_source_commit, path,
                                                       missing_code="local-auxiliary-source-missing", git=git)
                _assert_index_flags(root_fd, path, git=git)
                blob = git(root_fd, "cat-file", "blob", oid_at_head)
                _assert_attributes(root_fd, path, git=git)
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
                auxiliary_bytes[path] = (raw, mode_at_head)
            for inspected in source_inspection.sources:
                raw, mode, _ = _read_source(root_fd, inspected.path)
                source = by_path[inspected.path]
                if raw != source.executed_bytes or _mode(mode) != source.git_mode:
                    _refuse("local-source-changed")
                _assert_index_flags(root_fd, inspected.path, git=git)
                _assert_attributes(root_fd, inspected.path, git=git)
            for path in auxiliary:
                raw, mode, _ = _read_source(root_fd, path)
                if (raw, _mode(mode)) != auxiliary_bytes[path]:
                    _refuse("local-auxiliary-source-changed")
                _assert_index_flags(root_fd, path, git=git)
                _assert_attributes(root_fd, path, git=git)
            if owner_safety_preflight:
                if _assert_owner_checkout_safe(root_fd) != safety:
                    _refuse("local-checkout-safety-changed")
            _assert_revision_and_clean(root_fd, repository_root, expected_source_commit, git=git)
            _assert_no_alternates(root_fd)
            if owner_safety_preflight and _assert_owner_checkout_safe(root_fd) != safety:
                _refuse("local-checkout-safety-changed")
            _assert_root_identity(root_fd, repository_root)
            return tuple(identities)
        finally:
            os.close(root_fd)
    except (OSError, UnicodeError):
        _refuse("local-source-unavailable")
