"""Prepare a PowerShell results directory using POSIX directory descriptors."""

from contextlib import ExitStack
import errno
import os
from pathlib import Path
import shutil
import stat
import sys


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


def require_unchanged(bindings, results):
    for parent, component, directory in bindings:
        try:
            current = os.stat(component, dir_fd=parent, follow_symlinks=parent is None)
        except FileNotFoundError:
            current = None
        if current is None or not os.path.samestat(current, os.fstat(directory)):
            raise ValueError(f"path changed during preparation. Refusing to use '{results}'.")


def has_ownership_marker(directory):
    try:
        metadata = os.lstat(MARKER, dir_fd=directory)
        if not stat.S_ISREG(metadata.st_mode) or metadata.st_size != len(SIGNATURE):
            return False
        descriptor = os.open(MARKER, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK, dir_fd=directory)
    except OSError as error:
        if error.errno in (errno.ENOENT, errno.ELOOP):
            return False
        raise
    with os.fdopen(descriptor, "rb") as receipt:
        return os.path.samestat(metadata, os.fstat(descriptor)) and receipt.read(len(SIGNATURE) + 1) == SIGNATURE


def prepare_results(root, results):
    components = results.relative_to(root).parts
    if not components or ".." in components:
        raise ValueError("must resolve beneath the repository root.")
    if any(component.casefold() == ".git" for component in components):
        raise ValueError("must stay outside the repository's .git metadata path.")
    if not shutil.rmtree.avoids_symlink_attacks:
        raise ValueError("requires Python support for symlink-safe directory cleanup.")

    with ExitStack() as descriptors:
        # The helper process starts in the checkout and can change its own cwd during cleanup.
        directory = os.open(".", DIRECTORY_FLAGS)
        descriptors.callback(os.close, directory)
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

        entries = os.listdir(directory)
        if entries and not has_ownership_marker(directory):
            raise ValueError(f"must name an empty or runner-owned directory. Refusing to clean '{results}'.")

        require_unchanged(bindings, results)
        if not entries:
            descriptor = os.open(MARKER, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o666, dir_fd=directory)
            with os.fdopen(descriptor, "wb") as receipt:
                receipt.write(SIGNATURE)
        else:
            # Relative names stay anchored by this process's cwd, including on Python <3.11.
            os.fchdir(directory)
            for entry in entries:
                if entry == MARKER:
                    continue
                if stat.S_ISDIR(os.lstat(entry).st_mode):
                    shutil.rmtree(entry)
                else:
                    os.unlink(entry)
        require_unchanged(bindings, results)


if __name__ == "__main__":
    root, results, *report_option = sys.argv[1:]
    option = report_option[0] if report_option else "-ResultsDirectory"
    try:
        prepare_results(Path(root), Path(results))
    except (OSError, RuntimeError, ValueError) as error:
        raise SystemExit(f"{option} {error}") from None
