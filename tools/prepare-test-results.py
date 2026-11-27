"""Lease POSIX test results and publish reports through directory descriptors."""

from contextlib import ExitStack, contextmanager
import errno
import fcntl
import json
import os
from pathlib import Path
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


@contextmanager
def prepared_results(root, results):
    components = results.relative_to(root).parts
    if not components or ".." in components:
        raise ValueError("must resolve beneath the repository root.")
    if any(component.casefold() == ".git" for component in components):
        raise ValueError("must stay outside the repository's .git metadata path.")
    if not shutil.rmtree.avoids_symlink_attacks:
        raise ValueError("requires Python support for symlink-safe directory cleanup.")

    with ExitStack() as descriptors:
        # The helper process starts in the checkout and can change its own cwd during cleanup.
        checkout = os.open(".", DIRECTORY_FLAGS)
        descriptors.callback(os.close, checkout)
        directory = checkout
        current = root
        bindings = [(None, root, directory)]
        for component in components:
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

        # Lock the verified directory itself. Closing the descriptor releases
        # the lease even if the runner exits without cleaning up.
        try:
            fcntl.flock(directory, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError as error:
            raise ValueError(f"is in use by another test run. Refusing to use '{results}'.") from error

        empty = not os.listdir(directory)
        if not empty and not has_ownership_marker(directory, results, descriptors):
            raise ValueError(f"must name an empty or runner-owned directory. Refusing to clean '{results}'.")
        require_unchanged(bindings, results)
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
                    if stat.S_ISDIR(os.lstat(entry).st_mode):
                        shutil.rmtree(entry)
                    else:
                        os.unlink(entry)
            finally:
                os.fchdir(checkout)
        require_unchanged(bindings, results)
        yield directory, bindings


def publish_reports(staging, directory):
    for entry in staging.iterdir():
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
        with prepared_results(root, results) as (directory, bindings):
            if not command:
                # PowerShell runs the projects. On owner exit, EOF releases
                # the lease and discards staging without publishing reports.
                print(json.dumps(staging), flush=True)
                if sys.stdin.readline() != "publish\n":
                    return 0
                status = 0
            else:
                if interrupted is not None:
                    return 128 + interrupted
                process = subprocess.Popen(
                    command,
                    env=dict(os.environ, MEMORIES_TEST_REPORT_STAGING=staging),
                    start_new_session=True,
                )
                # Record signals even during process creation, then stop the
                # entire project group before dropping its staging directory.
                while interrupted is None:
                    try:
                        status = process.wait(timeout=0.1)
                        break
                    except subprocess.TimeoutExpired:
                        continue
                if interrupted is not None:
                    signal_project_group(process, interrupted)
                    if not wait_for_project_group(process, 2):
                        signal_project_group(process, signal.SIGKILL)
                        wait_for_project_group(process, 2)
                    return 128 + interrupted
                if status < 0:
                    status = 128 - status
            try:
                require_unchanged(bindings, results, "report generation")
                if interrupted is not None:
                    return 128 + interrupted
                publish_reports(Path(staging), directory)
                require_unchanged(bindings, results, "report generation")
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
    except (OSError, RuntimeError, ValueError) as error:
        raise SystemExit(f"{option} {error}") from None
