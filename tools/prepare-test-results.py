"""Lease POSIX test results and publish reports through directory descriptors."""

from contextlib import ExitStack, contextmanager
import errno
import fcntl
import json
import os
from pathlib import Path
import re
import selectors
import shutil
import signal
import stat
import subprocess
import sys
import tempfile
import time
import uuid


MARKER = ".hexalith-memories-test-results"
SIGNATURE = b"Hexalith.Memories test results v1\n"
DIRECTORY_FLAGS = os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW
WINDOWS_PATH_TIMEOUT = 10
WINDOWS_LEASE_TIMEOUT = 30
WINDOWS_SHUTDOWN_TIMEOUT = 2


class RunInterrupted(Exception):
    pass


def open_directory(parent, component):
    try:
        return os.open(component, DIRECTORY_FLAGS, dir_fd=parent)
    except FileNotFoundError:
        try:
            os.mkdir(component, dir_fd=parent)
        except FileExistsError:
            pass
        return os.open(component, DIRECTORY_FLAGS, dir_fd=parent)


def require_unchanged(bindings, results, phase="preparation"):
    for parent, component, directory in bindings:
        try:
            current = os.stat(component, dir_fd=parent, follow_symlinks=parent is None)
        except FileNotFoundError:
            current = None
        if current is None or not os.path.samestat(current, os.fstat(directory)):
            raise ValueError(f"path changed during {phase}. Refusing to use '{results}'.")


def has_ownership_marker(directory, results, descriptors):
    try:
        metadata = os.lstat(MARKER, dir_fd=directory)
        if not stat.S_ISREG(metadata.st_mode):
            return False
        descriptor = os.open(MARKER, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK, dir_fd=directory)
    except PermissionError as error:
        # A native Windows run opens this same marker exclusively on an NTFS
        # checkout also accessed from WSL. That denial arrives as EACCES.
        raise ValueError(f"could not lease '{results}'. It may be in use by another test run.") from error
    except OSError as error:
        if error.errno in (errno.ENOENT, errno.ELOOP):
            return False
        raise

    # An open marker also prevents native Windows from leasing the same NTFS
    # results directory while it is in use from WSL.
    descriptors.callback(os.close, descriptor)
    return os.path.samestat(metadata, os.fstat(descriptor)) and os.read(descriptor, len(SIGNATURE) + 1) == SIGNATURE


def needs_windows_lease(checkout, results):
    if sys.platform != "linux":
        return False
    try:
        mounts = Path("/proc/self/mountinfo").read_bytes()
    except OSError as error:
        if "WSL_DISTRO_NAME" in os.environ or "WSL_INTEROP" in os.environ:
            raise ValueError("requires readable mount information for Windows results leases.") from error
        return False
    best_depth = 0
    checkout_is_drvfs = False
    crossed_mounts = []
    for line in mounts.splitlines():
        mount, filesystem = line.split(b" - ", 1)
        mount_point = re.sub(rb"\\([0-7]{3})", lambda match: bytes([int(match[1], 8)]), mount.split()[4])
        path = Path(os.fsdecode(mount_point))
        kind, _, options = filesystem.split(maxsplit=2)
        drvfs = kind == b"drvfs" or (kind == b"9p" and b",aname=drvfs;" in options)
        if len(path.parts) >= best_depth and (path == checkout or path in checkout.parents):
            best_depth = len(path.parts)
            checkout_is_drvfs = drvfs
        if checkout in path.parents and (path == results or path in results.parents or results in path.parents):
            crossed_mounts.append(drvfs)
    if any(crossed_mounts) or (checkout_is_drvfs and crossed_mounts):
        raise ValueError("must stay within one mounted filesystem for Windows results leases.")
    return checkout_is_drvfs


def validate_windows_components(components):
    for component in components:
        device = component.partition(".")[0].rstrip().upper()
        if (
            any(char in '<>:"/\\|?*' or ord(char) < 32 for char in component)
            or component.endswith((".", " "))
            or device in {"CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$"}
            or re.fullmatch(r"(?:COM|LPT)[1-9¹²³]", device)
        ):
            raise ValueError(f"contains a component that is ambiguous on Windows: {component!r}.")


def await_windows_lease(process, request, check_interrupted):
    deadline = time.monotonic() + WINDOWS_LEASE_TIMEOUT
    received = b""
    remaining = memoryview(request)
    with selectors.DefaultSelector() as selector:
        os.set_blocking(process.stdin.fileno(), False)
        selector.register(process.stdin, selectors.EVENT_WRITE)
        selector.register(process.stdout, selectors.EVENT_READ)
        while time.monotonic() < deadline:
            check_interrupted()
            for key, _ in selector.select(0.1):
                if key.fileobj == process.stdin:
                    try:
                        written = os.write(key.fd, remaining)
                    except BrokenPipeError:
                        selector.unregister(process.stdin)
                    else:
                        remaining = remaining[written:]
                        if not remaining:
                            selector.unregister(process.stdin)
                    continue
                chunk = os.read(key.fd, 64)
                received += chunk
                if received in (b"ready\r\n", b"ready\n") and not remaining:
                    return
                if not chunk or not b"ready\r\n".startswith(received):
                    check_interrupted()
                    raise ValueError("could not acquire the Windows results lease.")
    check_interrupted()
    raise ValueError("could not acquire the Windows results lease.")


def close_windows_lease(process):
    process.stdin.close()
    process.stdout.close()
    try:
        return process.wait(timeout=WINDOWS_SHUTDOWN_TIMEOUT) == 0
    except subprocess.TimeoutExpired:
        process.kill()
        try:
            process.wait(timeout=WINDOWS_SHUTDOWN_TIMEOUT)
        except subprocess.TimeoutExpired:
            pass
        return False


@contextmanager
def windows_results_lease(checkout, components, check_interrupted):
    if not needs_windows_lease(checkout, checkout.joinpath(*components)):
        yield check_interrupted
        return

    validate_windows_components(components)
    powershell = shutil.which("powershell.exe")
    if powershell is None:
        raise ValueError("requires Windows PowerShell interop to lease results on a WSL-mounted Windows drive.")

    def windows_path(path):
        check_interrupted()
        try:
            return subprocess.check_output(
                ["wslpath", "-w", str(path)], text=True, timeout=WINDOWS_PATH_TIMEOUT,
            ).rstrip("\r\n")
        except (OSError, subprocess.SubprocessError) as error:
            check_interrupted()
            raise ValueError(f"could not translate '{path}' for the Windows results lease.") from error

    script = windows_path(Path(__file__).resolve().with_suffix(".ps1"))
    request = json.dumps({"root": windows_path(checkout), "components": components})
    check_interrupted()
    process = subprocess.Popen(
        [powershell, "-NoProfile", "-NonInteractive", "-File", script, "-LeaseOnly"],
        stdin=subprocess.PIPE, stdout=subprocess.PIPE, bufsize=0,
    )

    def require_lease():
        check_interrupted()
        if process.poll() is not None:
            raise ValueError("lost the Windows results lease.")

    completed = False
    try:
        await_windows_lease(process, (request + "\n").encode("ascii"), check_interrupted)
        require_lease()
        yield require_lease
        completed = True
    finally:
        # EOF releases the handles on normal completion and if WSL kills this process.
        clean_exit = close_windows_lease(process)
        check_interrupted()
        if completed and not clean_exit:
            raise ValueError("Windows results lease helper exited abnormally.")


@contextmanager
def prepared_results(root, results, check_interrupted):
    components = results.relative_to(root).parts
    if not components or ".." in components:
        raise ValueError("must resolve beneath the repository root.")
    if any(component.casefold() == ".git" for component in components):
        raise ValueError("must stay outside the repository's .git metadata path.")
    if not shutil.rmtree.avoids_symlink_attacks:
        raise ValueError("requires Python support for symlink-safe directory cleanup.")

    with windows_results_lease(Path.cwd(), components, check_interrupted) as require_lease, ExitStack() as descriptors:
        # The helper process starts in the checkout and can change its own cwd during cleanup.
        checkout = os.open(".", DIRECTORY_FLAGS)
        descriptors.callback(os.close, checkout)
        directory = checkout
        current = root
        bindings = [(None, root, directory)]
        for index, component in enumerate(components, start=1):
            require_lease()
            parent = directory
            current /= component
            try:
                directory = open_directory(parent, component)
            except OSError as error:
                if error.errno in (errno.ELOOP, errno.ENOTDIR):
                    raise ValueError(
                        "must resolve beneath the repository root without traversing a symbolic link "
                        f"or non-directory at '{current}'."
                    ) from error
                raise
            descriptors.callback(os.close, directory)
            bindings.append((parent, component, directory))

            # Hold shared ancestor leases before walking farther. An exclusive
            # destination lease then prevents cleanup by an ancestor and writes
            # by a descendant. All leases expire when the descriptors close.
            mode = fcntl.LOCK_EX if index == len(components) else fcntl.LOCK_SH
            try:
                fcntl.flock(directory, mode | fcntl.LOCK_NB)
            except BlockingIOError as error:
                raise ValueError(f"is in use by another test run. Refusing to use '{results}'.") from error

        empty = not os.listdir(directory)
        if not empty and not has_ownership_marker(directory, results, descriptors):
            raise ValueError(f"must name an empty or runner-owned directory. Refusing to clean '{results}'.")
        require_unchanged(bindings, results)
        require_lease()
        if empty:
            try:
                marker = os.open(MARKER, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o666, dir_fd=directory)
            except FileExistsError as error:
                raise ValueError(f"is in use by another test run. Refusing to use '{results}'.") from error
            descriptors.callback(os.close, marker)
            if os.write(marker, SIGNATURE) != len(SIGNATURE):
                raise OSError("could not write the full test results ownership marker.")
        else:
            # Relative names stay anchored by this process's cwd, including on Python <3.11.
            os.fchdir(directory)
            try:
                for entry in os.listdir(directory):
                    if entry == MARKER:
                        continue
                    require_lease()
                    if stat.S_ISDIR(os.lstat(entry).st_mode):
                        shutil.rmtree(entry)
                    else:
                        os.unlink(entry)
            finally:
                os.fchdir(checkout)
        require_unchanged(bindings, results)
        require_lease()
        yield directory, bindings, require_lease


def publish_reports(staging, directory, require_lease):
    for entry in staging.iterdir():
        require_lease()
        try:
            os.rename(entry, entry.name, dst_dir_fd=directory)
        except OSError as error:
            if error.errno != errno.EXDEV:
                raise
            # Stage on the destination filesystem before renaming if the scratch
            # directory is mounted elsewhere. The cwd is still descriptor-bound.
            os.fchdir(directory)
            transfer = Path(f".memories-report-transfer-{uuid.uuid4().hex}")
            os.mkdir(transfer, mode=0o700, dir_fd=directory)
            try:
                copied = transfer / entry.name
                shutil.copytree(entry, copied, symlinks=True)
                require_lease()
                os.rename(copied, entry.name, dst_dir_fd=directory)
            finally:
                shutil.rmtree(transfer)


def signal_project_group(process, signum):
    try:
        os.killpg(process.pid, signum)
        return True
    except ProcessLookupError:
        return False


def wait_for_project_group(process, timeout):
    deadline = time.monotonic() + timeout
    while True:
        process.poll()
        if not signal_project_group(process, 0):
            return True
        if time.monotonic() >= deadline:
            return False
        time.sleep(0.05)


def run_with_reports(root, results, command):
    interrupted = None

    def request_stop(signum, _frame):
        nonlocal interrupted
        if interrupted is None:
            interrupted = signum

    def check_interrupted():
        if interrupted is not None:
            raise RunInterrupted(128 + interrupted)

    if command:
        for signum in (signal.SIGHUP, signal.SIGINT, signal.SIGTERM):
            signal.signal(signum, request_stop)

    scratch = Path(tempfile.gettempdir()).resolve()
    checkout = Path.cwd()
    if scratch == checkout or checkout in scratch.parents:
        raise ValueError("requires a temporary directory outside the repository.")

    # dotnet resolves a relative results path to an absolute name before
    # launching the test application. Give it a private path independent of
    # the checkout, then publish to the directory held open since cleanup.
    with tempfile.TemporaryDirectory(prefix="memories-test-reports-", dir=scratch) as staging:
        with prepared_results(root, results, check_interrupted) as (directory, bindings, require_lease):
            if not command:
                # PowerShell runs the projects. On owner exit, EOF releases
                # the lease and discards staging without publishing reports.
                print(json.dumps(staging), flush=True)
                if sys.stdin.readline() != "publish\n":
                    return 0
                status = 0
            else:
                require_lease()
                process = subprocess.Popen(
                    command,
                    env=dict(os.environ, MEMORIES_TEST_REPORT_STAGING=staging),
                    start_new_session=True,
                )
                try:
                    while True:
                        require_lease()
                        try:
                            status = process.wait(timeout=0.1)
                            break
                        except subprocess.TimeoutExpired:
                            continue
                    require_lease()
                except (RunInterrupted, ValueError):
                    signal_project_group(process, interrupted or signal.SIGTERM)
                    if not wait_for_project_group(process, 2):
                        signal_project_group(process, signal.SIGKILL)
                        wait_for_project_group(process, 2)
                    raise
                if status < 0:
                    status = 128 - status
            require_lease()
            try:
                require_unchanged(bindings, results, "report generation")
                publish_reports(Path(staging), directory, require_lease)
                require_unchanged(bindings, results, "report generation")
                require_lease()
            except (OSError, RuntimeError, ValueError) as error:
                if status == 0:
                    raise
                print(f"--results-directory {error}", file=sys.stderr)
            else:
                print(f"Test results directory is '{results}'.")
            return status


if __name__ == "__main__":
    root, results, *arguments = sys.argv[1:]
    option, *command = arguments
    try:
        raise SystemExit(run_with_reports(Path(root), Path(results), command))
    except RunInterrupted as error:
        raise SystemExit(*error.args) from None
    except (OSError, RuntimeError, ValueError) as error:
        raise SystemExit(f"{option} {error}") from None
