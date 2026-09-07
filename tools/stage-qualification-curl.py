#!/usr/bin/env python3
"""Stage a pinned musl-static curl into a Server publish directory.

SDK-style Alpine containers cannot RUN apk. Qualification C4 canaries therefore
stage stunnel/static-curl 8.20.0 during `dotnet publish` for linux-musl RIDs
only, never during `dotnet test`.

Pinned archives are about 3.8 MiB. Downloads fail closed above
MAXIMUM_ARCHIVE_BYTES (8 MiB) before the cache file is replaced.
"""

from __future__ import annotations

import argparse
import hashlib
import os
import stat
import sys
import tarfile
import urllib.request
from pathlib import Path


RELEASE_BASE = "https://github.com/stunnel/static-curl/releases/download/8.20.0"
MAXIMUM_ARCHIVE_BYTES = 8 * 1024 * 1024
ARTIFACTS: dict[str, tuple[str, str]] = {
    "linux-musl-x64": (
        "curl-linux-x86_64-musl-8.20.0.tar.xz",
        "58c6fab6e3f62d39d23224d752de1302cb717d997288d0f23d6fa7e79c393c1f",
    ),
    "linux-musl-arm64": (
        "curl-linux-aarch64-musl-8.20.0.tar.xz",
        "32799692a41e88f9f2be85348c2230baf5a0a29ded2d6c086e49e5cbab22b3f4",
    ),
}


class StagingError(RuntimeError):
    """Raised when the pinned curl artifact cannot be staged."""


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runtime-identifier", required=True)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--cache-dir", required=True, type=Path)
    return parser


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        while True:
            chunk = handle.read(1024 * 1024)
            if not chunk:
                break
            digest.update(chunk)
    return digest.hexdigest()


def _download(url: str, destination: Path) -> None:
    request = urllib.request.Request(
        url,
        headers={"User-Agent": "hexalith-memories-qualification-curl"},
    )
    with urllib.request.urlopen(request, timeout=120) as response, destination.open("wb") as handle:
        declared = response.headers.get("Content-Length")
        if declared is not None:
            try:
                content_length = int(declared)
            except ValueError as exc:
                raise StagingError("pinned curl archive declared an invalid Content-Length") from exc
            if content_length > MAXIMUM_ARCHIVE_BYTES:
                raise StagingError(
                    f"pinned curl archive exceeds {MAXIMUM_ARCHIVE_BYTES} bytes"
                )
        received = 0
        while True:
            chunk = response.read(1024 * 1024)
            if not chunk:
                break
            received += len(chunk)
            if received > MAXIMUM_ARCHIVE_BYTES:
                raise StagingError(
                    f"pinned curl archive exceeds {MAXIMUM_ARCHIVE_BYTES} bytes"
                )
            handle.write(chunk)


def _extract_curl(archive: Path, destination: Path) -> None:
    with tarfile.open(archive, mode="r:xz") as tarball:
        members = [
            member
            for member in tarball.getmembers()
            if member.isfile() and Path(member.name).name == "curl"
        ]
        if len(members) != 1:
            raise StagingError("pinned curl archive did not contain exactly one curl binary")
        extracted = tarball.extractfile(members[0])
        if extracted is None:
            raise StagingError("pinned curl archive member could not be read")
        destination.parent.mkdir(parents=True, exist_ok=True)
        staging = destination.with_name(destination.name + ".staging")
        try:
            with staging.open("wb") as handle:
                while True:
                    chunk = extracted.read(1024 * 1024)
                    if not chunk:
                        break
                    handle.write(chunk)
            staging.chmod(stat.S_IRUSR | stat.S_IWUSR | stat.S_IXUSR | stat.S_IRGRP | stat.S_IXGRP | stat.S_IROTH | stat.S_IXOTH)
            staging.replace(destination)
        finally:
            if staging.exists():
                staging.unlink()


def stage(runtime_identifier: str, output: Path, cache_dir: Path) -> None:
    artifact = ARTIFACTS.get(runtime_identifier)
    if artifact is None:
        raise StagingError(f"unsupported qualification curl runtime identifier: {runtime_identifier}")
    name, expected_sha256 = artifact
    cache_dir.mkdir(parents=True, exist_ok=True)
    archive = cache_dir / name
    if not archive.is_file() or _sha256(archive) != expected_sha256:
        staging = archive.with_name(archive.name + ".download")
        try:
            _download(f"{RELEASE_BASE}/{name}", staging)
            actual = _sha256(staging)
            if actual != expected_sha256:
                raise StagingError(
                    f"pinned curl archive hash mismatch for {name}: {actual}"
                )
            staging.replace(archive)
        finally:
            if staging.exists():
                staging.unlink()
    _extract_curl(archive, output)
    if not output.is_file() or not os.access(output, os.X_OK):
        raise StagingError("staged curl is missing or not executable")


def main(argv: list[str] | None = None) -> int:
    arguments = _parser().parse_args(argv)
    try:
        stage(arguments.runtime_identifier, arguments.output, arguments.cache_dir)
    except StagingError as exc:
        print(str(exc), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
