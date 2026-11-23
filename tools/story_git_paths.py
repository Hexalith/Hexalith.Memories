"""Read exact Git paths for story and evidence checks."""

import os
import subprocess
from pathlib import Path


class GitPathError(Exception):
    """Git paths could not be collected or parsed."""


def parse_git_paths(data: bytes) -> list[str]:
    """Parse git diff --name-only -z output, including both rename endpoints."""
    if not data:
        return []

    paths = data.split(b"\0")
    if paths[-1] or b"" in paths[:-1]:
        raise GitPathError("Changed paths must be nonempty NUL-terminated Git records.")
    try:
        return [path.decode("utf-8") for path in paths[:-1]]
    except UnicodeDecodeError as error:
        raise GitPathError("Git changed paths must be UTF-8 encoded.") from error


def read_git_paths(path: Path) -> list[str]:
    """Read a saved Git diff path list."""
    try:
        return parse_git_paths(path.read_bytes())
    except OSError as error:
        location = display_git_path(display_path(path))
        raise GitPathError(f"Cannot read Git changed paths from {location} ({error.strerror}).") from error


def git_diff_paths(*revisions: str) -> list[str]:
    """Read exact changed paths from the supplied Git index or revision range."""
    try:
        result = subprocess.run(
            ["git", "diff", "--no-renames", "--name-only", "-z", *revisions, "--"],
            capture_output=True,
            check=False,
        )
    except OSError as error:
        raise GitPathError(f"Cannot run git diff ({error.strerror}). Check that Git is on PATH.") from error
    if result.returncode:
        detail = result.stderr.decode("utf-8", errors="replace").strip()
        raise GitPathError(detail or f"git diff exited with status {result.returncode}.")
    return parse_git_paths(result.stdout)


def display_git_path(path: str) -> str:
    """Keep paths containing control characters on one diagnostic line."""
    return path if path.isprintable() else repr(path)


def display_path(path: Path) -> str:
    """Render local paths relative to the working directory without drive prefixes."""
    try:
        return os.path.relpath(path, Path.cwd()).replace("\\", "/")
    except ValueError:
        # Windows cannot relativize paths on different drives. Keep the location below the anchor.
        return "<external>/" + "/".join(path.parts[1:])
