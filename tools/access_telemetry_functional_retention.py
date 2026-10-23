"""Functional retention semantics and the Platform-owned preparation boundary.

The public entry refuses until the existing Platform/Builds runner supplies its
supported PostgreSQL test/profile/descriptor/result contract. VM provisioning,
private bundles and orchestration below are retained historical implementation;
they are unreachable from the current lifecycle CLI. Offline historical tests
and semantic validators confer no live or full qualification credit.
"""

from __future__ import annotations

from contextlib import ExitStack
from concurrent.futures import ThreadPoolExecutor
import configparser
from datetime import datetime, timedelta, timezone
import hashlib
import http.client
import json
import os
from pathlib import Path
import re
import io
import shutil
import signal
import stat
import subprocess
import sys
import tarfile
from threading import Lock
import time
import urllib.parse
import urllib.request
from typing import Callable

from access_telemetry_c1_owner_dispatch import _canonical_path, _open_held_file, _file_metadata
from access_telemetry_c1_interchange import JsonSnapshot, InterchangeFormatError
from verify_access_telemetry_lifecycle import (
    STORY_27_4_PROFILE_SHA256,
    STORY_27_4_WORKLOAD_SHA256,
)

PLAN_SCHEMA = "hexalith.access-telemetry.functional-retention-plan/v1"
RESULT_SCHEMA = "hexalith.access-telemetry.functional-retention-result/v1"
POLICY = "sole-owner-qualification/2026-10-12"
OWNER = "github:user:6775094"
ASSURANCE = "sole-owner/self-reviewed"
TARGET = {
    "host": "DESKTOP-VIOG240",
    "guest": "memories-qualification-27-4",
    "uuid": "2fe71a5c-bdda-4767-8780-8f27f5bcd520",
    "context": "owner@memories-qualification-27-4",
    "namespace": "hexalith-memories-qualification",
    "sshPort": 12274,
    "apiPort": 16443,
    "vcpus": 8,
    "memoryBytes": 40 * 1024**3,
    "osDiskBytes": 40 * 1024**3,
    "dataDiskBytes": 600 * 1024**3,
    "diskDirectory": "/mnt/c/Users/JeromePiquot/AppData/Local/Hexalith/Qualification/memories-27-4-2fe71a5c",
    "guestEgress": False,
}
LIMITS = {
    "executionSeconds": 14400, "restorationSeconds": 900,
    "setupSeconds": 5400, "launchAttempts": 2, "launchReadySeconds": 600,
    "readAttempts": 3, "readAggregateSeconds": 120,
    "emitAttempts": 2, "pollSeconds": 30, "streamBytes": 1048576,
    "evidenceBytes": 256 * 1024**2, "stagingBytes": 20 * 1024**3,
    "hostStartAvailableBytes": 56 * 1024**3,
    "hostRunAvailableBytes": 16 * 1024**3,
    "hostDiskFreeBytes": 150 * 1024**3,
}
FUNCTIONAL = {
    "writers": 1, "recordsPerCohort": 125, "retentionSeconds": 3600,
    "cohortSeparationSeconds": 1800, "purgeGraceSeconds": 900,
    "clockSources": "non-Production-development-signed",
    "imageLane": "source-Debug-local-archive",
    "exactProfileCredit": False,
}
PRIVATE_TARGET_ROOT = Path("/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc")
_FIELDS = {
    "schemaVersion", "checkpoint", "policyId", "ownerPrincipal", "assuranceMode",
    "profileSha256", "workloadSha256", "custodyRoot", "sourceRoot", "sourceFiles",
    "target", "limits", "functional", "window", "decisionPath", "statusPath",
    "preparedInputs", "blockers", "ready", "transport",
}
_HEX = re.compile(r"[0-9a-f]{64}\Z")
_IDENTITY = re.compile(r"[a-z0-9][a-z0-9-]{0,63}\Z")


def artifact_catalog() -> dict:
    """The closed public artifact set; callers cannot supply URLs or images."""
    downloads = [
        ("k3s", "https://github.com/k3s-io/k3s/releases/download/v1.35.9%2Bk3s1/k3s", "897acb21cca57d477a211102251a6591450ce767344a9faaa121a1007ca9fa9a", 78237858),
        ("k3s-airgap-images-amd64.tar.zst", "https://github.com/k3s-io/k3s/releases/download/v1.35.9%2Bk3s1/k3s-airgap-images-amd64.tar.zst", "7f1cbfea56d38f5e69aec13500a89290119096cc2f5a6d4f542e5f26599977d0", 194444185),
        ("helm.tar.gz", "https://get.helm.sh/helm-v3.22.0-linux-amd64.tar.gz", "1e4ab49e429626cf6c6958d914248b78c9730803c2751b87627e171dc800e7bb", 18616332),
        ("dapr.tgz", "https://dapr.github.io/helm-charts/dapr-1.18.1.tgz", "d518bd2e2f783c5b38b7a644596426fd278449e9cd74acf4a37be213e8812e7b", 38396),
        ("openbao.tgz", "https://github.com/openbao/openbao-helm/releases/download/openbao-0.29.6/openbao-0.29.6.tgz", "8079e985bdf608f965ada59c70051693d14dd2454ac16311229f367d0c48c4b9", 46832),
    ]
    dapr = {
        "daprd": ("b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8", "edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b"),
        "operator": ("89661f52a3d37f5d528c35dd9d2b4ac76c7b274bd459c8570d6246b6bfdda549", "73d646b18f1699f6d2a1672d4c6692032c3f094f31fe63577bf462f047d0dc7b"),
        "sentry": ("2f98508dff56c75329dbd51674c89f41ce349e06c7744ab2519cb69ba338d41f", "53dcfe7754d2178282b43b380b5e66bebd50b239d56b1ca7798f2acca3663209"),
        "injector": ("2793b954b1aef142d59bd5eae71bec4de5f71d16e9ad80fec81cbf3b4eea428c", "7a9bf21a6f48a33f78a2b2e5a4957c674729c4b11f941165d5901a86ce3b8e4f"),
        "placement": ("6caf20016d115d4a7f133b9206b739a10abd9f558d76683b27be9ab60f759e26", "5bfaa1db388feb638651ac499faf88a8d8a6503f607e418a856d29035d252785"),
        "scheduler": ("c9bb9ada0cd6a63cd92c26470da1985124e423432af4e39f09b96979fd1059c0", "da1da8cef39c9a0b1d8cb3e9fb605fb635c9e7c44b07f1907a2d0ce82bea98a4"),
    }
    images = [{"id": name, "registry": "ghcr.io", "repository": "dapr/" + name, "reference": "ghcr.io/dapr/" + name + ":1.18.1@sha256:" + pins[0], "indexSha256": pins[0], "platformManifestSha256": pins[1], "path": "assets/images/" + name + ".tar"} for name, pins in dapr.items()]
    for name, registry, repository, reference, index, platform in (
        ("postgresql", "registry-1.docker.io", "library/postgres", "docker.io/library/postgres:18.6-trixie", "5a5a84b19854a9ffaa54082c166ff4ec27473a361e496e5ea167f298f2da9722", "0377e72c5289ed2f98cf61b1a9c2db9eb9d300317fe14244492fbc94343b3d04"),
        ("openbao", "quay.io", "openbao/openbao", "quay.io/openbao/openbao:2.6.4", "cf2340fc9a22cb9358ca0defd1f39b65673836bd23fe2bb8984a07e11fe13ef4", "bd3e8b6b67b5c4c3fc1064cd0f86eb8063d9ed92a7af608b6408d6748e6eef64"),
        ("redis", "registry-1.docker.io", "redis/redis-stack-server", "docker.io/redis/redis-stack-server:7.4.0-v8", "798ab84d9f266936b034ab11c4d04a2b8e4b441884c5aa7d17ac951eefdf742a", None),
        ("falkordb", "registry-1.docker.io", "falkordb/falkordb", "docker.io/falkordb/falkordb:v4.12.0", "7927eb194df0fadf70e2deb6c20c8178743ec92aacd739c790f8a35ce5dee613", None),
    ):
        images.append({"id": name, "registry": registry, "repository": repository, "reference": reference + "@sha256:" + index, "indexSha256": index, "platformManifestSha256": platform, "path": "assets/images/" + name + ".tar"})
    for image in images:
        image.update({"type": "oci-image", "sha256": image["indexSha256"], "bytes": None, "ref": image["reference"]})
    return {"schemaVersion": "hexalith.access-telemetry.functional-assets/v1", "k3sVersion": "v1.35.9+k3s1", "downloads": [{"path": "assets/" + name, "url": url, "sha256": digest, "bytes": size, "maximumBytes": size, "type": "immutable-download"} for name, url, digest, size in downloads], "images": images}


def _staging_size(root: Path) -> int:
    total = 0
    for path in root.rglob("*"):
        _require(not path.is_symlink(), "staging-link-refused")
        if path.is_file():
            total += path.stat().st_size
    _require(total <= LIMITS["stagingBytes"], "staging-budget")
    return total


def _staging_floor(root: Path, next_bytes: int = 0) -> None:
    # The WSL staging filesystem and the native disk holding the guest must
    # each preserve the approved host floor, including the next write.
    for path in (root, Path(TARGET["diskDirectory"])):
        _require(shutil.disk_usage(path).free - next_bytes >= LIMITS["hostDiskFreeBytes"], "host-disk-floor")


def _preparation_write_guard(root: Path, next_bytes: int = 0) -> None:
    _require(type(next_bytes) is int and next_bytes >= 0 and _staging_size(root) + next_bytes <= LIMITS["stagingBytes"], "staging-budget")
    _staging_floor(root, next_bytes)


def _copy_prepared_file(source: Path, destination: Path, *, root: Path) -> tuple[str, int]:
    expected, size = _hash_file(source)
    _preparation_write_guard(root, size)
    with ExitStack() as descriptors:
        held = _open_held_file(str(source), descriptors)
        copied = 0
        with destination.open("xb", buffering=0) as output:
            os.fchmod(output.fileno(), 0o600)
            while block := os.read(held.descriptor, 1024**2):
                _require(copied + len(block) <= size, "input-drift")
                _preparation_write_guard(root, len(block))
                output.write(block)
                copied += len(block)
            os.fsync(output.fileno())
        held.revalidate()
    _require(copied == size and _hash_file(destination, custody_root=str(root)) == (expected, size), "application-copy-drift")
    return expected, size


def _download_asset(url: str, path: Path, digest: str, maximum: int, *, root: Path, authority: Callable, headers: dict | None = None) -> None:
    """Two bounded attempts for one internally selected immutable object."""
    _require(_HEX.fullmatch(digest) is not None and 0 < maximum <= LIMITS["stagingBytes"], "asset-binding")
    if path.exists():
        _require(_hash_file(path, custody_root=str(root)) == (digest, path.stat().st_size), "cached-asset-drift")
        return
    path.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
    for attempt in range(2):
        authority()
        begin = time.monotonic()
        partial = path.with_name(path.name + f".partial-{attempt}")
        try:
            baseline = _staging_size(root)
            _staging_floor(root)
            h = hashlib.sha256()
            received = 0
            request = urllib.request.Request(url, headers={"User-Agent": "hexalith-functional-retention", **(headers or {})})
            with urllib.request.urlopen(request, timeout=30) as response, partial.open("xb") as output:
                os.fchmod(output.fileno(), 0o600)
                declared = response.headers.get("Content-Length")
                _require(declared is None or (declared.isdecimal() and int(declared) <= maximum), "download-size")
                while block := response.read(1024**2):
                    tick = getattr(authority, "tick", None)
                    (tick if callable(tick) else authority)()
                    _require(time.monotonic() - begin <= 120, "download-deadline")
                    received += len(block)
                    _require(received <= maximum and baseline + received <= LIMITS["stagingBytes"], "staging-budget")
                    _staging_floor(root, len(block))
                    output.write(block)
                    h.update(block)
                output.flush()
                os.fsync(output.fileno())
            _require(h.hexdigest() == digest, "download-hash")
            authority()
            partial.rename(path)
            _require(_hash_file(path, custody_root=str(root)) == (digest, received), "download-readback")
            return
        except FunctionalRetentionError:
            # Hash/size/authority failures are semantic failures, never retries.
            raise
        except (OSError, TimeoutError):
            if attempt:
                raise FunctionalRetentionError("functional-download-transport-failed") from None
        finally:
            if partial.exists() and not partial.is_symlink():
                partial.unlink()


def _registry_headers(image: dict, authority: Callable) -> dict:
    authority()
    registry, repository = image["registry"], image["repository"]
    if registry not in {"ghcr.io", "registry-1.docker.io"}:
        return {}
    prefix = "https://ghcr.io/token?service=ghcr.io&scope=" if registry == "ghcr.io" else "https://auth.docker.io/token?service=registry.docker.io&scope="
    with urllib.request.urlopen(prefix + urllib.parse.quote("repository:" + repository + ":pull"), timeout=15) as response:
        raw = response.read(32769)
    _require(len(raw) <= 32768, "registry-token-size")
    token = json.loads(raw).get("token")
    _require(isinstance(token, str) and 0 < len(token) <= 16384, "registry-token-unavailable")
    return {"Authorization": "Bearer " + token}


def stage_transport_assets(bundle_root: Path, authority: Callable) -> dict:
    """Fetch only the fixed catalog after live approval; never install on host.

    Third-party registry bytes are retained unchanged in bounded OCI transport
    archives. Application images continue to come only from the .NET SDK.
    """
    root = bundle_root.absolute()
    catalog = artifact_catalog()
    for item in catalog["downloads"]:
        _download_asset(item["url"], root / item["path"], item["sha256"], item["maximumBytes"], root=root, authority=authority)
    cache = root / "assets/registry-blobs"
    cache.mkdir(mode=0o700, exist_ok=True)
    accept = "application/vnd.oci.image.index.v1+json, application/vnd.docker.distribution.manifest.list.v2+json, application/vnd.oci.image.manifest.v1+json, application/vnd.docker.distribution.manifest.v2+json"
    for image in catalog["images"]:
        authority()
        base = "https://" + image["registry"] + "/v2/" + image["repository"]
        headers = {**_registry_headers(image, authority), "Accept": accept}
        included = {}

        def manifest(digest):
            path = cache / digest
            _download_asset(base + "/manifests/sha256:" + digest, path, digest, 1048576, root=root, authority=authority, headers=headers)
            raw = path.read_bytes()
            value = json.loads(raw)
            included[digest] = path
            return value, raw

        index, index_raw = manifest(image["indexSha256"])
        descriptor = {"mediaType": index["mediaType"], "size": len(index_raw), "digest": "sha256:" + image["indexSha256"], "annotations": {"io.containerd.image.name": image["reference"], "org.opencontainers.image.ref.name": image["reference"]}}
        if "manifests" in index:
            selected = [item for item in index["manifests"] if item.get("platform", {}).get("os") == "linux" and item.get("platform", {}).get("architecture") == "amd64"]
            _require(len(selected) == 1, "image-platform")
            platform_digest = selected[0]["digest"].removeprefix("sha256:")
            _require(image["platformManifestSha256"] in {None, platform_digest}, "image-platform-pin")
            payload, _ = manifest(platform_digest)
        else:
            payload = index
            _require(image["platformManifestSha256"] in {None, image["indexSha256"]}, "image-platform-pin")
        for blob in [payload["config"], *payload["layers"]]:
            digest = blob["digest"].removeprefix("sha256:")
            _require(_HEX.fullmatch(digest) is not None and type(blob["size"]) is int and 0 < blob["size"] <= LIMITS["stagingBytes"], "image-blob-binding")
            path = cache / digest
            _download_asset(base + "/blobs/sha256:" + digest, path, digest, blob["size"], root=root, authority=authority, headers=headers)
            _require(path.stat().st_size == blob["size"], "image-blob-size")
            included[digest] = path
        config = json.loads(included[payload["config"]["digest"].removeprefix("sha256:")].read_bytes())
        _require(config.get("os") == "linux" and config.get("architecture") == "amd64", "image-config-platform")
        archive = root / image["path"]
        archive.parent.mkdir(mode=0o700, exist_ok=True)
        _require(not archive.exists(), "image-archive-already-exists")
        with archive.open("xb") as raw_archive:
            os.fchmod(raw_archive.fileno(), 0o600)
            with tarfile.open(fileobj=raw_archive, mode="w") as tar:
                for name, raw in (("oci-layout", b'{"imageLayoutVersion":"1.0.0"}'), ("index.json", json.dumps({"schemaVersion": 2, "manifests": [descriptor]}, separators=(",", ":")).encode())):
                    info = tarfile.TarInfo(name)
                    info.size, info.mode = len(raw), 0o600
                    tar.addfile(info, io.BytesIO(raw))
                for digest, path in included.items():
                    authority()
                    _require(_staging_size(root) + path.stat().st_size <= LIMITS["stagingBytes"], "staging-budget")
                    _staging_floor(root, path.stat().st_size + 10240)
                    info = tarfile.TarInfo("blobs/sha256/" + digest)
                    info.size, info.mode = path.stat().st_size, 0o600
                    with path.open("rb") as body:
                        tar.addfile(info, body)
            raw_archive.flush()
            os.fsync(raw_archive.fileno())
        _staging_size(root)
        authority()
    return catalog


def fixed_boot_argv() -> tuple[str, ...]:
    """The fixed corrected vector, retained for the combined plan's review.

    A new, separately bound functional seed is required. The prior 30-minute
    seed and closed supervisors are never selected by this vector.
    """
    disk = TARGET["diskDirectory"]
    runtime = "/home/administrator/.local/state/hexalith/q27-2fe71a5c"
    return (
        "/usr/bin/timeout", "--signal=TERM", "--kill-after=60s", "15300s",
        "/usr/bin/qemu-system-x86_64", "-enable-kvm", "-machine", "q35,accel=kvm",
        "-cpu", "host", "-smp", "8", "-m", "40960", "-uuid", TARGET["uuid"],
        "-name", TARGET["guest"], "-display", "none", "-monitor", "none",
        "-serial", "none", "-pidfile", str(PRIVATE_TARGET_ROOT / "control/functional-qemu.pid"),
        "-qmp", f"unix:{runtime}/functional.sock,server=on,wait=off",
        "-drive", f"file={disk}/os.raw,if=none,id=qual-os,format=raw,cache=none,aio=threads,discard=ignore,detect-zeroes=off",
        "-device", "virtio-blk-pci,drive=qual-os,serial=memories-qual-os",
        "-drive", f"file={disk}/data.raw,if=none,id=qual-data,format=raw,cache=none,aio=threads,discard=ignore,detect-zeroes=off",
        "-device", "virtio-blk-pci,drive=qual-data,serial=memories-qual-data",
        "-drive", f"file={PRIVATE_TARGET_ROOT}/control/functional-seed.iso,media=cdrom,readonly=on",
        "-netdev", "user,id=qual-net,restrict=on,hostfwd=tcp:127.0.0.1:12274-:22,hostfwd=tcp:127.0.0.1:16443-:6443",
        "-device", "virtio-net-pci,netdev=qual-net",
    )


def fixed_record_keys(run_id: str, segment: str) -> tuple[str, ...]:
    """Use the actual runtime's SHA-256 shard, not a shardless absence key."""
    from access_telemetry_producer_common import _qualification_record_ids
    _require(_IDENTITY.fullmatch(run_id) is not None and segment in {"cohort-a", "cohort-b"}, "cohort-identity")
    return tuple(f"records/{hashlib.sha256(value.encode('ascii')).digest()[0] & 63:02d}/{value}" for value in _qualification_record_ids(run_id, segment))


def fixed_state_probe_script(run_id: str, segment: str, emitted_ms: int, *, buckets: bool = False) -> str:
    """Build only the fixed strong Dapr record/index reads, never a mutation."""
    _require(type(emitted_ms) is int and 0 < emitted_ms < 253402300799000, "cohort-time")
    keys = fixed_record_keys(run_id, segment)
    if buckets:
        keys = tuple(sorted({f"expiry-bucket/{(emitted_ms + ordinal + 3600000) // 60000:012d}/{int(key.split('/')[1]):02d}" for ordinal, key in enumerate(keys)}))
    commands = ["set -eu", "printf '['"]
    for index, key in enumerate(keys):
        url = "http://127.0.0.1:3500/v1.0/state/access-telemetry-store/" + key.replace("/", "%2F") + "?consistency=strong&metadata.partitionKey=access-telemetry"
        commands.append('value=$(wget -qO- -T 2 -t 1 --header="dapr-api-token: $DAPR_API_TOKEN" "' + url + '")')
        commands.append("printf '" + ("," if index else "") + '{"key":"' + key + '","value":%s}' + "' \"${value:-null}\"")
    commands.append("printf ']'" )
    return "; ".join(commands)


def fixed_expired_physical_query(run_id: str, segment: str) -> str:
    """Read only the exact cohort's expired PostgreSQL tuples for TTL proof."""
    keys = fixed_record_keys(run_id, segment)
    values = ",".join("'memories-access-telemetry||" + key + "'" for key in keys)
    return (
        "SELECT json_build_object('expiredRecordIds',coalesce(json_agg("
        "convert_from(value,'UTF8')::jsonb->>'recordId' ORDER BY key),'[]'::json)) "
        "FROM access_telemetry.lifecycle_state WHERE key IN (" + values + ") "
        "AND expiredate IS NOT NULL AND expiredate<=clock_timestamp();"
    )


def fixed_physical_bound_query(run_id: str) -> str:
    """Read actual A-row contents/expiry and authenticated PostgreSQL session time only."""
    keys = fixed_record_keys(run_id, "cohort-a")
    values = ",".join("'memories-access-telemetry||" + key + "'" for key in keys)
    return ("SELECT json_build_object('serverVersionNum',current_setting('server_version_num'),"
            "'ssl',(SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid()),"
            "'tlsVersion',(SELECT version FROM pg_stat_ssl WHERE pid=pg_backend_pid()),"
            "'role',current_user,'readOnly',current_setting('default_transaction_read_only'),"
            "'observedUtcMs',floor(extract(epoch FROM clock_timestamp())*1000)::bigint,"
            "'rows',(SELECT coalesce(json_agg(json_build_object('recordId',convert_from(value,'UTF8')::jsonb->>'recordId',"
            "'record',convert_from(value,'UTF8')::jsonb,'expiresUtcMs',floor(extract(epoch FROM expires_at)*1000)::bigint) ORDER BY key),'[]'::json) "
            "FROM access_telemetry.lifecycle_state WHERE key IN (" + values + ")));")


def fixed_workload_script(run_id: str, segment: str, emitted_ms: int) -> str:
    """Use the existing bodyless logger endpoint with stable retry headers."""
    fixed_record_keys(run_id, segment)
    _require(type(emitted_ms) is int and 0 < emitted_ms < 253402300799000, "cohort-time")
    return (
        'wget -qO- -T 120 -t 1 --header="dapr-api-token: $APP_API_TOKEN" '
        f'--header="X-Hexalith-Qualification-Run: {run_id}" '
        f'--header="X-Hexalith-Qualification-Segment: {segment}" '
        f'--header="X-Hexalith-Qualification-Emitted-Utc-Ms: {emitted_ms}" '
        '--post-data="" http://127.0.0.1:8080/operations/access-telemetry/qualification/fixed-workload'
    )


def owned_lease_patch(*, uid: str, resource_version: str, holder: str, release: bool) -> str:
    """A mutation must reconcile a fresh exact UID/version and same owner."""
    _require(all(isinstance(value, str) and re.fullmatch(r"[a-zA-Z0-9][a-zA-Z0-9-]{0,127}", value) for value in (uid, resource_version, holder)), "lease-identity")
    _require(type(release) is bool, "lease-operation")
    return json.dumps([
        {"op": "test", "path": "/metadata/uid", "value": uid},
        {"op": "test", "path": "/metadata/resourceVersion", "value": resource_version},
        {"op": "test", "path": "/spec/holderIdentity", "value": holder},
        {"op": "replace", "path": "/spec/holderIdentity", "value": "" if release else holder},
        {"op": "replace", "path": "/spec/leaseDurationSeconds", "value": 0 if release else 900},
    ], separators=(",", ":"))


class FunctionalRetentionError(ValueError):
    """A content-free refusal; input values never appear in diagnostics."""


class TransportUncertain(FunctionalRetentionError):
    """Only transport/readiness uncertainty permits the enumerated retry."""


def _selected_transport_category(error):
    """Classify transport failures without retaining their messages or values."""
    if isinstance(error, urllib.error.URLError) and isinstance(error.reason, BaseException):
        error = error.reason
    for kind, category in (
        (TimeoutError, "timeout"),
        (http.client.RemoteDisconnected, "remote-disconnected"),
        (http.client.IncompleteRead, "incomplete-read"),
        (http.client.BadStatusLine, "bad-status-line"),
        (ConnectionRefusedError, "connection-refused"),
        (ConnectionResetError, "connection-reset"),
        (ConnectionAbortedError, "connection-aborted"),
        (ConnectionError, "connection-error"),
        (http.client.HTTPException, "http-protocol"),
    ):
        if isinstance(error, kind):
            return category
    return "unclassified"


_SEMANTIC_FAILURES = (FunctionalRetentionError, OSError, ValueError, KeyError, IndexError, TypeError, AttributeError, subprocess.SubprocessError)


def _require(condition: bool, code: str) -> None:
    if not condition:
        raise FunctionalRetentionError("functional-" + code)


def _sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def _validate_selected_mapping(mapping: dict, plan_hash: str, handoff_hash: str) -> None:
    """Recompute the producer mapping and retain both independent hash bindings."""
    payload = {key: mapping[key] for key in ("Mode", "Root", "ActiveModule", "Entries", "HasGit", "PlatformIdentity", "IdentityFindings")}
    encoded = json.dumps(payload, separators=(",", ":"), ensure_ascii=True)
    # SourceMapping.ContentHash uses System.Text.Json's default encoder. Keep
    # literal backslash sequences intact while matching its string escapes.
    def escape(match):
        value = match.group()
        if value == '\\"':
            return r"\u0022"
        return value[:2] + value[2:].upper() if value.startswith(r"\u") else value
    encoded = re.sub(r"\\(?:u[0-9a-fA-F]{4}|.)", escape, encoded)
    encoded = encoded.translate({ord(char): rf"\u{ord(char):04X}" for char in "&'+<>`"})
    _require(_sha(encoded.encode()) == plan_hash == handoff_hash, "selected-mapping-hash")


def _utc(value: object) -> datetime:
    _require(isinstance(value, str) and value.endswith("Z"), "utc-required")
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except (ValueError, TypeError):
        raise FunctionalRetentionError("functional-utc-required") from None
    _require(parsed.utcoffset() == timedelta(0), "utc-required")
    return parsed


def _plain(value):
    """Convert the existing strict immutable JSON reader's containers."""
    if hasattr(value, "items"):
        return {key: _plain(item) for key, item in value.items()}
    if isinstance(value, tuple):
        return [_plain(item) for item in value]
    return value


def _private(path: str, root: str) -> bytes:
    try:
        _canonical_path(root)
        _canonical_path(path)
        _require(path.startswith(root + "/"), "input-outside-custody")
        with ExitStack() as descriptors:
            held = _open_held_file(path, descriptors, custody_root=root, effective_uid=os.geteuid())
            raw = held.read()
            os.lseek(held.descriptor, 0, os.SEEK_SET)
            _require(held.read() == raw, "input-drift")
            return raw
    except FunctionalRetentionError:
        raise
    except (ValueError, OSError, TypeError, AttributeError):
        raise FunctionalRetentionError("functional-private-input-unavailable") from None


def _json(raw: bytes):
    try:
        return _plain(JsonSnapshot(raw).value)
    except (InterchangeFormatError, ValueError, TypeError):
        raise FunctionalRetentionError("functional-invalid-json") from None


def _declared_dependency_roots(root: Path) -> list[Path]:
    """Use only initialized root declarations; never descend nested references."""
    permitted = {}
    for workspace in (root, root.parent):
        manifest = workspace / ".gitmodules"
        if not manifest.is_file():
            continue
        parser = configparser.ConfigParser(interpolation=None)
        with ExitStack() as descriptors:
            held = _open_held_file(str(manifest), descriptors)
            parser.read_string(held.read().decode("utf-8"))
            held.revalidate()
        declarations = [workspace / parser[section]["path"] for section in parser.sections() if "path" in parser[section]]
        if workspace != root and root not in declarations:
            continue
        for path in declarations:
            if path.name in {"Hexalith.FrontComposer", "Hexalith.EventStore", "Hexalith.Commons", "Hexalith.Tenants", "Hexalith.Builds"} and (path / ".git").exists():
                permitted.setdefault(path.name, path)
    return list(permitted.values())


def source_files(root: Path) -> dict[str, str]:
    """Hash own inputs and permitted resolved Debug dependency source/build inputs.

    The deliberately conservative closure covers the external project references
    in the Debug solution and imported shared build catalog. Skills and nested
    submodules are pruned before file discovery and are never opened.
    """
    root = root.absolute()
    paths = [root / name for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "global.json", "docs/operations/qualification/pg2-kvm-approved-profile-2026-10-16.json")]
    suffixes = {".py", ".cs", ".csproj", ".props", ".targets", ".json", ".yaml", ".yml", ".razor", ".resx", ".css", ".js", ".ts", ".html", ".xml", ".config", ".txt", ".md"}
    forbidden = {"obj", "bin", "__pycache__", ".git", ".agents", ".codex", "skills", "references", "_bmad", "_bmad-output", "node_modules"}
    roots = [(root, ("tools", "src", "deploy"))]
    roots.extend((dependency, ("src", "Props", "Targets")) for dependency in _declared_dependency_roots(root))
    for base, directories in roots:
        paths.extend(path for path in base.iterdir() if path.is_file() and (path.suffix in {".props", ".targets", ".json", ".config", ".slnx"} or path.name in {".gitmodules", ".editorconfig", ".gitattributes", "NuGet.Config"}))
        for directory in directories:
            for parent, subdirectories, names in os.walk(base / directory, followlinks=False):
                subdirectories[:] = [name for name in subdirectories if name.lower() not in forbidden]
                for name in names:
                    path = Path(parent) / name
                    if path.suffix in suffixes and name != "SKILL.md":
                        paths.append(path)
    result = {}
    for path in sorted(set(paths)):
        _require(not path.is_symlink() and path.stat().st_size <= 4 * 1024**2, "source-path-unsafe")
        key = path.relative_to(root).as_posix() if path.is_relative_to(root) else str(path)
        result[key] = _hash_file(path)[0]
    _require(0 < len(result) < 8192, "source-budget")
    return result


def image_source_files(root: Path) -> dict[str, str]:
    return {key: value for key, value in source_files(root).items() if not key.startswith(("tools/", "deploy/", "docs/"))}


def _hash_file(path: Path, *, custody_root: str | None = None) -> tuple[str, int]:
    """Hash through a held non-following descriptor, retaining named identity."""
    try:
        with ExitStack() as descriptors:
            held = _open_held_file(str(path), descriptors, custody_root=custody_root, effective_uid=os.geteuid())
            digest = hashlib.sha256()
            count = 0
            while block := os.read(held.descriptor, 1024**2):
                count += len(block)
                _require(count <= LIMITS["stagingBytes"], "staging-budget")
                digest.update(block)
            held.revalidate()
            _require(count == held.before.st_size, "input-drift")
            return digest.hexdigest(), count
    except FunctionalRetentionError:
        raise
    except (ValueError, OSError, TypeError, AttributeError):
        raise FunctionalRetentionError("functional-input-unavailable") from None


def validate_plan(plan: dict, *, source_root: Path) -> None:
    _require(type(plan) is dict and set(plan) == _FIELDS, "plan-schema")
    _require(plan["schemaVersion"] == PLAN_SCHEMA and plan["checkpoint"] == "functional-retention", "plan-version")
    _require((plan["policyId"], plan["ownerPrincipal"], plan["assuranceMode"]) == (POLICY, OWNER, ASSURANCE), "policy-mismatch")
    _require(plan["profileSha256"] == STORY_27_4_PROFILE_SHA256 and plan["workloadSha256"] == STORY_27_4_WORKLOAD_SHA256, "profile-mismatch")
    # Exact JSON types prevent true/1 equality from widening an operation bound.
    for name, expected in (("target", TARGET), ("limits", LIMITS), ("functional", FUNCTIONAL)):
        _require(json.dumps(plan[name], sort_keys=True) == json.dumps(expected, sort_keys=True), name + "-mismatch")
    _require(plan["sourceRoot"] == str(source_root.absolute()), "source-root-mismatch")
    _require(source_root.absolute() == Path(__file__).absolute().parents[1], "executing-source-root-mismatch")
    _require(type(plan["ready"]) is bool and type(plan["blockers"]) is list and len(plan["blockers"]) <= 32, "readiness-schema")
    _require(all(isinstance(code, str) and re.fullmatch(r"[a-z0-9-]{1,128}", code) for code in plan["blockers"]), "blocker-schema")
    window = plan["window"]
    _require(type(window) is dict and set(window) == {"notBefore", "latestStart", "expires"}, "window-schema")
    begin, latest, end = (_utc(window[key]) for key in ("notBefore", "latestStart", "expires"))
    _require(begin <= latest and latest + timedelta(seconds=15300) <= end and end - begin <= timedelta(hours=48), "window-bound")
    root = plan["custodyRoot"]
    _require(type(root) is str and _canonical_path(root) == root, "custody-root")
    for name in ("decisionPath", "statusPath"):
        _require(type(plan[name]) is str and _canonical_path(plan[name]) == plan[name] and plan[name].startswith(root + "/"), "control-path")
    _require(plan["decisionPath"] != plan["statusPath"], "control-alias")
    _require(type(plan["preparedInputs"]) is list and len(plan["preparedInputs"]) <= 256, "inputs-schema")
    for item in plan["preparedInputs"]:
        _require(type(item) is dict and set(item) == {"path", "sha256", "bytes"}, "input-schema")
        _require(type(item["path"]) is str and item["path"].startswith(root + "/") and _canonical_path(item["path"]) == item["path"], "input-path")
        _require(type(item["bytes"]) is int and 0 <= item["bytes"] <= LIMITS["stagingBytes"], "input-size")
        _require(isinstance(item["sha256"], str) and _HEX.fullmatch(item["sha256"]) is not None, "input-digest")
    _require(sum(item["bytes"] for item in plan["preparedInputs"]) <= LIMITS["stagingBytes"], "staging-budget")
    _validate_transport(plan["transport"], custody_root=Path(root))


def _validate_transport(transport: dict, *, custody_root: Path) -> None:
    from access_telemetry_functional_target import validate_transport
    validate_transport(transport, custody_root=custody_root)


def current_authority(plan_path: Path, approved_sha256: str, *, source_root: Path, custody_root: Path, now: datetime, initial_admission: bool = True) -> dict:
    """Fresh exact plan/decision/status/source checks, with status read last."""
    _require(isinstance(approved_sha256, str) and _HEX.fullmatch(approved_sha256) is not None, "outside-approved-digest-required")
    root = str(custody_root.absolute())
    raw = _private(str(plan_path.absolute()), root)
    _require(_sha(raw) == approved_sha256, "plan-drift")
    plan = _json(raw)
    validate_plan(plan, source_root=source_root)
    _require(plan["custodyRoot"] == root, "custody-root-mismatch")
    _require(len({str(plan_path.absolute()), plan["decisionPath"], plan["statusPath"]}) == 3, "control-alias")
    decision_raw = _private(plan["decisionPath"], root)
    decision = _json(decision_raw)
    _require(type(decision) is dict and set(decision) == {"schemaVersion", "decision", "ownerPrincipal", "assuranceMode", "planSha256", "recordedAtUtc", "restorationApproved"}, "decision-schema")
    _require(decision == {**decision, "schemaVersion": "hexalith.access-telemetry.functional-retention-decision/v1", "decision": "approved", "ownerPrincipal": OWNER, "assuranceMode": ASSURANCE, "planSha256": approved_sha256, "restorationApproved": True}, "decision-mismatch")
    _require(type(decision["restorationApproved"]) is bool, "restoration-authority-required")
    recorded = _utc(decision["recordedAtUtc"])
    _require(now.tzinfo is not None and recorded <= now < _utc(plan["window"]["expires"]) <= recorded + timedelta(hours=48), "approval-expired")
    _require(_utc(plan["window"]["notBefore"]) <= now, "start-window-closed")
    if initial_admission:
        _require(now <= _utc(plan["window"]["latestStart"]), "start-window-closed")
    _require(source_files(source_root) == plan["sourceFiles"], "source-drift")
    for item in plan["preparedInputs"]:
        _require(_hash_file(Path(item["path"]), custody_root=root) == (item["sha256"], item["bytes"]), "prepared-input-drift")
    catalog_path = root + "/asset-catalog.json"
    _require(sum(item["path"] == catalog_path for item in plan["preparedInputs"]) == 1, "asset-catalog-required")
    _require(_json(_private(catalog_path, root)) == artifact_catalog(), "asset-catalog-mismatch")
    status_raw = _private(plan["statusPath"], root)
    status = _json(status_raw)
    _require(type(status) is dict and set(status) == {"planSha256", "decisionSha256", "executableAuthority", "withdrawn", "budgetConsumed"}, "status-schema")
    _require(status == {"planSha256": approved_sha256, "decisionSha256": _sha(decision_raw), "executableAuthority": True, "withdrawn": False, "budgetConsumed": False} and all(type(status[name]) is bool for name in ("executableAuthority", "withdrawn", "budgetConsumed")), "authority-held-withdrawn-or-consumed")
    _require(_private(str(plan_path.absolute()), root) == raw and _private(plan["decisionPath"], root) == decision_raw and _private(plan["statusPath"], root) == status_raw, "authority-drift")
    return plan


def _exclusive_json(path: Path, value: dict) -> bytes:
    raw = (json.dumps(value, indent=2, sort_keys=True) + "\n").encode()
    return _exclusive_bytes(path, raw)


def _exclusive_bytes(path: Path, raw: bytes) -> bytes:
    """Use held, non-link directory descriptors for every native output type."""
    _require(path.is_absolute() and len(raw) <= 1048576 and not path.parent.is_symlink(), "output-path")
    with ExitStack() as descriptors:
        parent = os.open("/", os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)
        descriptors.callback(os.close, parent)
        for part in path.parent.parts[1:]:
            parent = os.open(part, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW, dir_fd=parent)
            descriptors.callback(os.close, parent)
        metadata = os.fstat(parent)
        _require(metadata.st_uid == os.geteuid() and metadata.st_mode & 0o077 == 0, "output-not-private")
        descriptor = os.open(path.name, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600, dir_fd=parent)
        with os.fdopen(descriptor, "wb") as stream:
            stream.write(raw)
            stream.flush()
            os.fsync(stream.fileno())
    _require(_private(str(path), str(path.parent)) == raw, "output-readback")
    return raw


def _preparation_json(root: Path, path: Path, value: dict) -> bytes:
    _preparation_write_guard(root, len((json.dumps(value, indent=2, sort_keys=True) + "\n").encode()))
    return _exclusive_json(path, value)


def _process_identity() -> dict:
    pid = os.getpid()
    fields = Path(f"/proc/{pid}/stat").read_text().rsplit(")", 1)[1].split()
    return {"pid": pid, "startTicks": fields[19], "bootId": Path("/proc/sys/kernel/random/boot_id").read_text().strip(), "uid": os.geteuid()}


class FunctionalAuthority:
    """One exact current grant and exclusive process-bound execution budget."""

    def __init__(self, plan_path: Path, digest: str, source_root: Path, custody_root: Path):
        self.plan_path, self.digest = plan_path.absolute(), digest
        self.source_root, self.custody_root = source_root.absolute(), custody_root.absolute()
        self.claim_path = self.custody_root / "run.claim"
        self.claim_bytes = None
        self.decision_bytes = None
        self.plan_bytes = None
        self.status_bytes = None
        self.metadata = None
        self.admitted = False
        self.plan = None

    def _metadata(self, plan):
        snapshot = {}
        paths = [(self.source_root / name, False) for name in plan["sourceFiles"]]
        paths.extend((Path(item["path"]), True) for item in plan["preparedInputs"])
        for path, private in paths:
            with ExitStack() as descriptors:
                held = _open_held_file(str(path), descriptors, custody_root=str(self.custody_root) if private else None, effective_uid=os.geteuid())
                held.revalidate()
                snapshot[str(path)] = _file_metadata(held.before)
        return snapshot

    def tick(self) -> dict:
        """Fresh control/status/expiry checks inside an already admitted action.

        Full byte hashing still precedes every new process/download/action.
        Ongoing bounded streams use held-file metadata and non-following path
        checks to detect drift without rehashing hundreds of MiB per chunk.
        """
        _require(self.admitted and self.metadata is not None, "initial-admission-required")
        _require(_private(str(self.plan_path), str(self.custody_root)) == self.plan_bytes, "plan-drift")
        _require(_private(self.plan["decisionPath"], str(self.custody_root)) == self.decision_bytes, "decision-drift")
        now = datetime.now(timezone.utc)
        _require(_utc(self.plan["window"]["notBefore"]) <= now < _utc(self.plan["window"]["expires"]), "approval-expired")
        _require(self._metadata(self.plan) == self.metadata, "input-metadata-drift")
        if self.claim_bytes is None:
            _require(not self.claim_path.exists() and not self.claim_path.is_symlink(), "execution-budget-already-claimed")
        else:
            _require(_private(str(self.claim_path), str(self.custody_root)) == self.claim_bytes and _json(self.claim_bytes)["process"] == _process_identity(), "execution-claim-drift")
        # Withdrawal/status is checked last, including any change while source
        # and input identities were being checked. Nothing is cached as active.
        _require(_private(self.plan["statusPath"], str(self.custody_root)) == self.status_bytes, "authority-held-withdrawn-or-changed")
        return self.plan

    def __call__(self) -> dict:
        before = self._metadata(self.plan) if self.plan is not None else None
        plan = current_authority(self.plan_path, self.digest, source_root=self.source_root, custody_root=self.custody_root, now=datetime.now(timezone.utc), initial_admission=not self.admitted)
        decision = _private(plan["decisionPath"], str(self.custody_root))
        _require(self.decision_bytes is None or decision == self.decision_bytes, "decision-drift")
        self.decision_bytes = decision
        if self.claim_bytes is None:
            _require(not self.claim_path.exists() and not self.claim_path.is_symlink(), "execution-budget-already-claimed")
        else:
            _require(_private(str(self.claim_path), str(self.custody_root)) == self.claim_bytes, "execution-claim-drift")
            _require(_json(self.claim_bytes)["process"] == _process_identity(), "execution-process-drift")
        after = self._metadata(plan)
        if before is None:
            # Close the first hash→metadata race with one second full read.
            current_authority(self.plan_path, self.digest, source_root=self.source_root, custody_root=self.custody_root, now=datetime.now(timezone.utc), initial_admission=not self.admitted)
            _require(self._metadata(plan) == after, "input-metadata-drift")
        else:
            _require(after == before == self.metadata, "input-metadata-drift")
        self.metadata = after
        plan_bytes = _private(str(self.plan_path), str(self.custody_root))
        status_bytes = _private(plan["statusPath"], str(self.custody_root))
        _require(_sha(plan_bytes) == self.digest and (self.plan_bytes is None or plan_bytes == self.plan_bytes), "plan-drift")
        _require(_json(status_bytes) == {"planSha256": self.digest, "decisionSha256": _sha(self.decision_bytes), "executableAuthority": True, "withdrawn": False, "budgetConsumed": False} and (self.status_bytes is None or self.status_bytes == status_bytes), "authority-held-withdrawn-or-changed")
        _require(_private(plan["decisionPath"], str(self.custody_root)) == self.decision_bytes and _private(plan["statusPath"], str(self.custody_root)) == status_bytes, "authority-drift")
        self.plan_bytes = plan_bytes
        self.status_bytes = status_bytes
        self.plan, self.admitted = plan, True
        return plan

    def claim(self) -> bool:
        self()
        _require(self.claim_bytes is None, "execution-budget-already-claimed")
        value = {"schemaVersion": "hexalith.access-telemetry.functional-execution-claim/v1", "planSha256": self.digest, "process": _process_identity(), "claimedAtUtc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")}
        try:
            self.claim_bytes = _exclusive_json(self.claim_path, value)
        except FileExistsError:
            raise FunctionalRetentionError("functional-execution-budget-already-claimed") from None
        self()
        return True

    def finalize(self) -> None:
        """Close only this claimed budget, preserving actual withdrawal state."""
        _require(self.claim_bytes is not None and _private(str(self.claim_path), str(self.custody_root)) == self.claim_bytes, "execution-claim-required")
        status_path = Path(self.plan["statusPath"])
        raw = _private(str(status_path), str(self.custody_root))
        status = _json(raw)
        _require(type(status) is dict and set(status) == {"planSha256", "decisionSha256", "executableAuthority", "withdrawn", "budgetConsumed"} and status["planSha256"] == self.digest and status["decisionSha256"] == _sha(self.decision_bytes), "final-status-mismatch")
        replacement = status_path.with_name("status.closed.json")
        _exclusive_json(replacement, {**status, "executableAuthority": False, "budgetConsumed": True})
        _require(_private(str(status_path), str(self.custody_root)) == raw, "final-status-drift")
        os.replace(replacement, status_path)
        _require(_json(_private(str(status_path), str(self.custody_root)))["executableAuthority"] is False, "final-status-readback")


def validate_cohort(observation: dict, *, run_id: str, segment: str, emitted_ms: int, expected: dict | None = None) -> dict:
    """Require every real strong-read record, its exact identity/hash/expiry."""
    from access_telemetry_producer_common import _qualification_record_ids
    _require(_IDENTITY.fullmatch(run_id) is not None and segment in {"cohort-a", "cohort-b"}, "cohort-identity")
    _require(type(observation) is dict and set(observation) == {"consistency", "records"} and observation["consistency"] == "strong", "strong-read-required")
    records = observation["records"]
    ids = _qualification_record_ids(run_id, segment)
    _require(type(records) is list and len(records) == 125, "cohort-count")
    normalized = {}
    for ordinal, record_id in enumerate(ids):
        item = records[ordinal]
        _require(type(item) is dict and set(item) == {"recordId", "envelopeHash", "emittedUtcMs", "expiresUtcMs", "acceptedUtcMs"}, "record-schema")
        _require(item["recordId"] == record_id and isinstance(item["envelopeHash"], str) and _HEX.fullmatch(item["envelopeHash"]) is not None, "record-identity")
        _require(all(type(item[name]) is int for name in ("emittedUtcMs", "expiresUtcMs", "acceptedUtcMs")), "record-time-type")
        _require(item["emittedUtcMs"] == emitted_ms + ordinal and item["expiresUtcMs"] == emitted_ms + ordinal + 3600000 and emitted_ms <= item["acceptedUtcMs"] <= emitted_ms + 300000, "record-time-mismatch")
        normalized[record_id] = item
    _require(expected is None or normalized == expected, "newer-cohort-changed")
    return normalized


def validate_accounting(result: dict, run_id: str, segment: str, emitted_ms: int) -> str:
    from access_telemetry_producer_common import _qualification_record_ids
    _require(type(result) is dict and set(result) == {"runId", "segmentId", "writer", "startedUtcMs", "finishedUtcMs", "emitFinishedUtcMs", "acknowledgedUtcMs", "attempted", "enqueued", "acknowledged", "persisted", "conflicted", "transactionAcknowledgements", "rejected", "dropped", "recordIds", "resultCount"}, "accounting-schema")
    _require(result["runId"] == run_id and result["segmentId"] == segment and result["recordIds"] == _qualification_record_ids(run_id, segment), "accounting-identity")
    writer = result["writer"]
    _require(isinstance(writer, str) and re.fullmatch(r"[a-z0-9][a-z0-9-]{0,252}", writer) is not None, "writer-identity")
    _require(all(type(result[name]) is int and result[name] == count for name, count in (("attempted", 125), ("enqueued", 125), ("acknowledged", 125), ("persisted", 125), ("transactionAcknowledgements", 125), ("conflicted", 0), ("rejected", 0), ("dropped", 0))), "accounting-loss")
    _require(all(type(result[name]) is int for name in ("startedUtcMs", "finishedUtcMs", "emitFinishedUtcMs", "acknowledgedUtcMs", "resultCount")), "accounting-time-type")
    _require(emitted_ms - 1000 <= result["startedUtcMs"] <= result["emitFinishedUtcMs"] == result["finishedUtcMs"] <= result["acknowledgedUtcMs"] <= emitted_ms + 300000 and result["resultCount"] > 0, "accounting-time-mismatch")
    return writer


def validate_purge(observation: dict, records: dict, *, now_ms: int) -> bool:
    _require(type(observation) is dict and set(observation) == {"consistency", "absentRecordIds", "removedIndexIds", "reminderSequenceBefore", "reminderSequenceAfter", "progressObservedUtcMs", "actorDeletedRecordIds", "ttlAbsentRecordIds"}, "purge-schema")
    _require(observation["consistency"] == "strong", "strong-read-required")
    ids = set(records)
    for name in ("absentRecordIds", "removedIndexIds", "actorDeletedRecordIds", "ttlAbsentRecordIds"):
        values = observation[name]
        _require(type(values) is list and all(type(value) is str for value in values) and len(values) == len(set(values)) and set(values) <= ids, "purge-identity")
    maximum_expiry = max(record["expiresUtcMs"] for record in records.values())
    minimum_expiry = min(record["expiresUtcMs"] for record in records.values())
    _require(now_ms <= minimum_expiry + 900000, "purge-deadline")
    _require(now_ms >= maximum_expiry, "purge-before-expiry")
    _require(all(type(observation[name]) is int for name in ("reminderSequenceBefore", "reminderSequenceAfter", "progressObservedUtcMs")), "purge-progress-type")
    _require(observation["reminderSequenceBefore"] >= 0 and observation["reminderSequenceAfter"] >= observation["reminderSequenceBefore"] and 0 <= observation["progressObservedUtcMs"] <= now_ms, "purge-progress-malformed")
    if observation["reminderSequenceAfter"] == observation["reminderSequenceBefore"] or observation["progressObservedUtcMs"] < maximum_expiry:
        _require(now_ms < minimum_expiry + 900000, "purge-deadline")
        return False
    deleted, ttl = set(observation["actorDeletedRecordIds"]), set(observation["ttlAbsentRecordIds"])
    _require(not deleted & ttl, "purge-disposition-overlap")
    return set(observation["absentRecordIds"]) == ids and set(observation["removedIndexIds"]) == ids and deleted | ttl == ids


def _run_sequence(driver, authority: Callable[[], None], *, run_id: str, utc_ms: Callable[[], int], monotonic: Callable[[], float], sleep: Callable[[float], None]) -> dict:
    """Historical VM/gate/Lease sequence, used only by offline regressions."""
    _require(getattr(driver, "offline_only", None) is True, "historical-vm-route-retired")
    _require(_IDENTITY.fullmatch(run_id) is not None, "run-identity")
    begin = monotonic()
    wall_begin = utc_ms()
    last_clock = [begin, wall_begin]
    spawned = False
    acquired = False
    result = {"schemaVersion": RESULT_SCHEMA, "checkpoint": "functional-retention", "status": "incomplete", "qualificationCredit": False, "assuranceMode": ASSURANCE}

    def check():
        authority()
        current_mono, current_utc = monotonic(), utc_ms()
        _require(current_mono >= last_clock[0] and current_utc >= last_clock[1] and abs(current_utc - wall_begin - (current_mono - begin) * 1000) <= 1000, "clock-discontinuity")
        last_clock[:] = [current_mono, current_utc]
        _require(0 <= current_mono - begin < LIMITS["executionSeconds"], "aggregate-deadline")

    def action(name, *arguments):
        read_only = name in {"preflight", "guest_identity", "resource_floors", "read_cohort", "observe_purge", "stopped_readback"}
        started = monotonic()
        for attempt in range(3 if read_only else 1):
            check()
            try:
                result = getattr(driver, name)(*arguments)
                if read_only:
                    _require(monotonic() - started <= 120, "read-aggregate-deadline")
                return result
            except TransportUncertain:
                if not read_only or attempt == 2:
                    raise
                delay = (2, 5)[attempt]
                _require(monotonic() - started + delay < 120, "read-aggregate-deadline")
                sleep(delay)

    def until(timestamp, monotonic_deadline=None):
        while utc_ms() < timestamp or (monotonic_deadline is not None and monotonic() < monotonic_deadline):
            check()
            _require(action("resource_floors") is True, "resource-floor-unverified")
            _require(action("renew_owned_gate") is True, "gate-renewal-unverified")
            remaining = max((timestamp - utc_ms()) / 1000, 0 if monotonic_deadline is None else monotonic_deadline - monotonic())
            sleep(max(0, min(30, remaining)))
        check()

    try:
        _require(action("claim_single_run_budget") is True, "budget-unverified")
        _require(action("preflight") is True, "preflight-unverified")
        for attempt in range(2):
            try:
                spawned = True
                _require(action("launch", attempt + 1, 600) is True, "launch-unverified")
                _require(action("guest_identity") is True, "guest-identity-unverified")
                break
            except TransportUncertain:
                action("stop_owned_guest")
                _require(action("stopped_readback") is True, "retry-stop-unverified")
                _require(attempt == 0, "launch-budget-exhausted")
        acquired = True  # partially applied setup must still attempt owned cleanup
        _require(action("setup") is True, "setup-unverified")
        _require(monotonic() - begin <= LIMITS["setupSeconds"], "setup-deadline")
        _require(action("acquire_owned_gate") is True, "gate-unverified")
        _require(action("startup_probes") is True, "startup-unverified")
        _require(monotonic() - begin <= LIMITS["setupSeconds"], "setup-deadline")
        emitted = {}
        emitted_mono = {}
        cohorts = {}
        writer = None
        for segment in ("cohort-a", "cohort-b"):
            if segment == "cohort-b":
                until(emitted["cohort-a"] + 1800000, emitted_mono["cohort-a"] + 1800)
            check()
            emitted[segment] = utc_ms()
            emitted_mono[segment] = monotonic()
            for request in range(2):
                try:
                    accounting = action("emit", run_id, segment, emitted[segment])
                    break
                except TransportUncertain:
                    _require(request == 0, "emit-budget-exhausted")
            observed_writer = validate_accounting(accounting, run_id, segment, emitted[segment])
            _require(writer is None or observed_writer == writer, "writer-changed")
            writer = observed_writer
            cohorts[segment] = validate_cohort(action("read_cohort", run_id, segment), run_id=run_id, segment=segment, emitted_ms=emitted[segment])
            result.setdefault("cohorts", {})[segment] = {"emittedUtcMs": emitted[segment], "recordCount": len(cohorts[segment]), "firstExpiryUtcMs": min(item["expiresUtcMs"] for item in cohorts[segment].values()), "lastExpiryUtcMs": max(item["expiresUtcMs"] for item in cohorts[segment].values()), "writer": writer}
        until(emitted["cohort-a"] + 3590000, emitted_mono["cohort-a"] + 3590)
        validate_cohort(action("read_cohort", run_id, "cohort-a"), run_id=run_id, segment="cohort-a", emitted_ms=emitted["cohort-a"], expected=cohorts["cohort-a"])
        until(emitted["cohort-a"] + 3600124, emitted_mono["cohort-a"] + 3600.124)
        while True:
            check()
            purged = validate_purge(action("observe_purge", run_id, "cohort-a"), cohorts["cohort-a"], now_ms=utc_ms())
            validate_cohort(action("read_cohort", run_id, "cohort-b"), run_id=run_id, segment="cohort-b", emitted_ms=emitted["cohort-b"], expected=cohorts["cohort-b"])
            if purged:
                break
            until(utc_ms() + 30000)
        result["status"] = "offline-orchestration-passed" if driver.offline_only else "functional-passed"
        result["purgeAttribution"] = "only-explicit-actor-receipt-or-positive-expired-physical-tuple-with-strong-absence"
    except _SEMANTIC_FAILURES as error:
        result["status"] = "rejected"
        result["reason"] = str(error) if isinstance(error, FunctionalRetentionError) else "functional-action-failed"
    finally:
        if spawned:
            restoration_begin = monotonic()
            cleanup_failed = False
            try:
                # The initial grant separately permits owned-resource restoration
                # after withdrawal/expiry; it never enables another test action.
                if acquired:
                    for name in ("disable_owned_gate", "release_owned_lease", "scale_owned_services_zero", "terminal_api_readback"):
                        try:
                            _require(monotonic() - restoration_begin < 900, "restoration-deadline")
                            _require(getattr(driver, name)() is True, "cleanup-unverified")
                        except _SEMANTIC_FAILURES:
                            cleanup_failed = True
                # The independent owned-guest stop is still attempted after an
                # API cleanup failure; this cannot claim terminal API success.
                _require(driver.stop_owned_guest() is True and driver.stopped_readback() is True, "guest-stop-unverified")
                _require(monotonic() - restoration_begin <= 900 and not cleanup_failed, "restoration-incomplete")
            except _SEMANTIC_FAILURES:
                result["status"] = "incomplete"
    return result


def exercise_offline_sequence(driver, authority: Callable[[], None], *, run_id: str, utc_ms: Callable[[], int], monotonic: Callable[[], float], sleep: Callable[[float], None]) -> dict:
    """An explicitly offline seam; no public CLI can select a test double."""
    _require(getattr(driver, "offline_only", None) is True, "offline-double-required")
    return _run_sequence(driver, authority, run_id=run_id, utc_ms=utc_ms, monotonic=monotonic, sleep=sleep)


def source_image_build_commands(source_root: Path) -> dict:
    """The actual local SDK archive commands retained with this preparation."""
    projects = {
        "server": "src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj",
        "lifecycle": "src/Hexalith.Memories.AccessTelemetry/Hexalith.Memories.AccessTelemetry.csproj",
        "clock": "src/Hexalith.Memories.AccessTelemetry.Clock/Hexalith.Memories.AccessTelemetry.Clock.csproj",
    }
    commands = {}
    for name, project in projects.items():
        commands[name] = ["dotnet", "publish", project, "--configuration", "Debug", "--no-restore", "-t:PublishContainer", "-p:UseHexalithProjectReferences=true", "-p:LocalSourceContainerArchive=true", f"-p:ContainerArchiveOutputPath=/tmp/story-27-4-{name}-functional-oci.tar.gz", "-p:ContainerImageFormat=OCI", "-p:ContainerRuntimeIdentifier=linux-musl-x64", "-p:ContainerImageTag=story-27-4-functional", "-p:ContainerBaseImage=mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:eca1355b1fefb0a93617b7d01749dc8ac821e0eadcedbcf7d64e4b59cad60104", "-m:1"]
        if name == "server":
            commands[name].append("-p:FunctionalRetentionArchive=true")
    return commands


def _retain_image_provenance(root: Path, source_root: Path, applications: dict) -> None:
    provenance_root = Path("/tmp/story-27-4-functional-source-build")
    path = provenance_root / "source-image-build.json"
    record = _json(_private(str(path), str(provenance_root)))
    _require(set(record) == {"schemaVersion", "sdkVersion", "sourceRoot", "sourceFiles", "commands", "images", "qualificationCredit"}, "build-provenance-schema")
    _require(record["schemaVersion"] == "hexalith.access-telemetry.functional-source-image-build/v1" and record["sourceRoot"] == str(source_root.absolute()) and record["sdkVersion"] == "10.0.401" and record["qualificationCredit"] is False, "build-provenance-binding")
    _require(record["commands"] == source_image_build_commands(source_root), "build-command-drift")
    image_sources = image_source_files(source_root)
    _require(record["sourceFiles"] == image_sources, "build-source-drift")
    _require(set(record["images"]) == {"server", "lifecycle", "clock"}, "build-image-schema")
    folder = root / "build-provenance"
    folder.mkdir(mode=0o700)
    for name, app in (("server", "memories"), ("lifecycle", "memories-access-telemetry"), ("clock", "memories-access-telemetry-clock")):
        image = record["images"][name]
        _require(set(image) == {"archiveSha256", "manifestDigest", "bytes", "exitCode", "startedUtc", "finishedUtc", "outputSha256"}, "build-image-schema")
        _require(all(image[key] == applications[app][key] for key in ("archiveSha256", "manifestDigest", "bytes")) and type(image["exitCode"]) is int and image["exitCode"] == 0 and _utc(image["startedUtc"]) <= _utc(image["finishedUtc"]), "build-image-drift")
        output = _private(str(provenance_root / (name + ".log")), str(provenance_root))
        _require(_sha(output) == image["outputSha256"], "build-output-drift")
        target = folder / (name + ".log")
        _copy_prepared_file(provenance_root / (name + ".log"), target, root=root)
        _require(_private(str(target), str(root)) == output, "build-output-readback")
    _preparation_json(root, folder / "source-image-build.json", record)


def _historical_prepare_vm_bundle(bundle_root: Path, source_root: Path) -> dict:
    """Retired VM preparation; original implementation is preserved below."""
    raise FunctionalRetentionError("historical-vm-route-retired")

    from access_telemetry_functional_target import prepare_transport_inputs

    root = bundle_root.absolute()
    parent = PRIVATE_TARGET_ROOT / "evidence"
    _require(root.parent == parent and re.fullmatch(r"functional-preparation-[a-zA-Z0-9-]{1,64}", root.name) is not None, "preparation-root")
    _require(not root.exists() and not root.is_symlink() and not parent.is_symlink(), "preparation-root-exists-or-unsafe")
    root.mkdir(mode=0o700)
    _preparation_write_guard(root)
    assets = root / "assets"
    assets.mkdir(mode=0o700)
    applications = {}
    for app, temporary in (("memories", "server"), ("memories-access-telemetry", "lifecycle"), ("memories-access-telemetry-clock", "clock")):
        original = Path("/tmp") / f"story-27-4-{temporary}-functional-oci.tar.gz"
        destination = assets / f"{app}.tar"
        expected, size = _copy_prepared_file(original, destination, root=root)
        with tarfile.open(destination) as archive:
            members = archive.getmembers()
            _require(len(members) <= 256 and all(item.isfile() and item.name not in {"", "/"} and not item.name.startswith("/") and ".." not in Path(item.name).parts for item in members), "application-archive-shape")
            index = json.load(archive.extractfile("index.json"))
            _require(index.get("schemaVersion") == 2 and len(index["manifests"]) == 1, "application-archive-index")
            descriptor = index["manifests"][0]
            digest = descriptor["digest"].removeprefix("sha256:")
            _require(_HEX.fullmatch(digest) is not None, "application-manifest-digest")
            manifest_raw = archive.extractfile("blobs/sha256/" + digest).read()
            _require(_sha(manifest_raw) == digest and len(manifest_raw) == descriptor["size"], "application-manifest-drift")
            manifest = json.loads(manifest_raw)
            for blob in (manifest["config"], *manifest["layers"]):
                blob_digest = blob["digest"].removeprefix("sha256:")
                _require(_HEX.fullmatch(blob_digest) is not None, "application-blob-digest")
                h, length = hashlib.sha256(), 0
                with archive.extractfile("blobs/sha256/" + blob_digest) as stream:
                    while block := stream.read(1024**2):
                        length += len(block)
                        h.update(block)
                _require(h.hexdigest() == blob_digest and length == blob["size"], "application-blob-drift")
            config = json.load(archive.extractfile("blobs/sha256/" + manifest["config"]["digest"].removeprefix("sha256:")))
            _require(config["os"] == "linux" and config["architecture"] == "amd64", "application-platform")
            applications[app] = {"archivePath": str(destination), "archiveSha256": expected, "manifestDigest": "sha256:" + digest, "bytes": size}
    _retain_image_provenance(root, source_root, applications)
    _preparation_json(root, root / "asset-catalog.json", artifact_catalog())
    _preparation_json(root, root / "application-images.json", applications)
    _preparation_write_guard(root, 16 * 1024**2)
    transport, blockers = prepare_transport_inputs(root, source_root, applications)
    _preparation_write_guard(root)
    if blockers:
        result = {"schemaVersion": "hexalith.access-telemetry.functional-preparation/v1", "custodyRoot": str(root), "ready": False, "blockers": blockers, "approval": "absent", "executableAuthority": False, "qualificationCredit": False}
        _preparation_json(root, root / "blocked-preparation.json", result)
        return result
    now = datetime.now(timezone.utc).replace(microsecond=0)
    expires = min(now + timedelta(hours=48), _utc(transport["credentialExpiresUtc"]))
    latest = expires - timedelta(seconds=15300)
    _require(now <= latest, "preparation-window-insufficient")
    inputs = []
    for path in sorted(root.rglob("*")):
        if path.is_file():
            digest, count = _hash_file(path, custody_root=str(root))
            inputs.append({"path": str(path), "sha256": digest, "bytes": count})
    plan = {
        "schemaVersion": PLAN_SCHEMA, "checkpoint": "functional-retention",
        "policyId": POLICY, "ownerPrincipal": OWNER, "assuranceMode": ASSURANCE,
        "profileSha256": STORY_27_4_PROFILE_SHA256, "workloadSha256": STORY_27_4_WORKLOAD_SHA256,
        "custodyRoot": str(root), "sourceRoot": str(source_root.absolute()),
        "sourceFiles": source_files(source_root), "target": TARGET, "limits": LIMITS,
        "functional": FUNCTIONAL,
        "window": {"notBefore": now.isoformat().replace("+00:00", "Z"), "latestStart": latest.isoformat().replace("+00:00", "Z"), "expires": expires.isoformat().replace("+00:00", "Z")},
        "decisionPath": str(root / "actual-owner-decision.json"), "statusPath": str(root / "status.json"),
        "preparedInputs": inputs, "transport": transport,
        "ready": not blockers, "blockers": blockers,
    }
    validate_plan(plan, source_root=source_root)
    raw = _preparation_json(root, root / "plan.json", plan)
    digest = _sha(raw)
    _preparation_json(root, root / "status.json", {"planSha256": digest, "decisionSha256": None, "executableAuthority": False, "withdrawn": False, "budgetConsumed": False})
    command = ["/usr/bin/timeout", "--signal=TERM", "--kill-after=900s", "14400s", str(Path(sys.executable).resolve()), "-B", str(source_root.absolute() / "tools/verify-access-telemetry-lifecycle.py"), "--checkpoint", "functional-retention", "--functional-plan", str(root / "plan.json"), "--approved-plan-sha256", digest, "--evidence-root", str(root), "--repository-root", str(source_root.absolute())]
    _preparation_json(root, root / "invocation.json", {"argv": command, "operationalApproval": "absent", "executableAuthority": False, "qualificationCredit": False})
    return {"planPath": str(root / "plan.json"), "planSha256": digest, "ready": not blockers, "blockers": blockers, "argv": command, "approval": "absent"}


def _historical_run_vm_retention(*, plan_path: Path, approved_plan_sha256: str, source_root: Path, custody_root: Path) -> int:
    """Retired VM execution; original implementation is preserved below."""
    raise FunctionalRetentionError("historical-vm-route-retired")

    grant = FunctionalAuthority(plan_path, approved_plan_sha256, source_root, custody_root)
    result = None
    driver = None
    finalized = False
    previous_handlers = {}
    try:
        plan = grant()
        _require(plan["ready"] is True and not plan["blockers"], "preparation-blocked")
        from access_telemetry_functional_target import FixedFunctionalTarget
        driver = FixedFunctionalTarget(plan, grant)

        def stop(_signum, _frame):
            raise FunctionalRetentionError("functional-stop-requested")

        for signum in (signal.SIGTERM, signal.SIGINT):
            previous_handlers[signum] = signal.signal(signum, stop)
        result = _run_sequence(driver, grant, run_id="functional-" + approved_plan_sha256[:24], utc_ms=lambda: int(time.time() * 1000), monotonic=time.monotonic, sleep=time.sleep)
        result.update({"planSha256": approved_plan_sha256, "decisionSha256": _sha(grant.decision_bytes), "sourceClosureSha256": _sha(json.dumps(plan["sourceFiles"], sort_keys=True, separators=(",", ":")).encode()), "profileReferenceSha256": STORY_27_4_PROFILE_SHA256, "workloadReferenceSha256": STORY_27_4_WORKLOAD_SHA256, "target": plan["target"], "limits": plan["limits"], "applicationImages": plan["transport"]["applicationImages"], "renderedObjectsSha256": plan["transport"]["renderedObjectsSha256"], "executionFinishedUtc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"), "fullQualification": "pending-separate-full-profile-C1-C4", "functionalDeltas": ["source-built-Debug-images", "non-Production-development-signed-clock", "K3s-v1.35.9+k3s1-functional-only", "static-local-Retain-storage-functional-only", "single-file-backed-OpenBao-functional-only"], "deletionAttributionLimit": "missing-physical-tuples-without-an-explicit-actor-receipt-remain-unresolved-and-cannot-pass"})
        if result["status"] == "functional-passed":
            try:
                grant()
            except FunctionalRetentionError as error:
                result.update({"status": "rejected", "reason": str(error)})
        grant.finalize()
        finalized = True
        _exclusive_json(custody_root / "functional-result.json", result)
    except _SEMANTIC_FAILURES as error:
        code = str(error) if isinstance(error, FunctionalRetentionError) else "functional-input-refused"
        result = {"schemaVersion": RESULT_SCHEMA, "checkpoint": "functional-retention", "status": "rejected" if driver is None else "incomplete", "reason": code, "targetProcessSpawns": 0 if driver is None else getattr(driver, "process_spawns", None), "targetCalls": 0 if driver is None else getattr(driver, "target_calls", None), "qualificationCredit": False, "assuranceMode": ASSURANCE}
    finally:
        if grant.claim_bytes is not None and not finalized:
            try:
                grant.finalize()
            except _SEMANTIC_FAILURES:
                if result is not None:
                    result.update({"status": "incomplete", "reason": "functional-budget-finalization-unverified"})
        for signum, handler in previous_handlers.items():
            signal.signal(signum, handler)
    print(json.dumps(result, sort_keys=True))
    return 0 if result["status"] == "functional-passed" else 1


def _recorded_platform_preparation_status() -> dict:
    """Report demonstrated owner gaps without reading a grant or touching a target.

    This is preparation output in the existing format, never an environment
    descriptor or result accepted from a caller. Source locations are the
    inspected 2026-10-16 contract; they must be rechecked when owners implement
    the missing boundary. No schema is invented to stand in for that contract.
    """
    return {
        "schemaVersion": "hexalith.access-telemetry.functional-preparation/v1",
        "checkpoint": "functional-retention", "status": "blocked", "ready": False,
        "reason": "functional-platform-runner-integration-required",
        "environmentOwner": "Hexalith.Platform", "runnerOwner": "Hexalith.Builds",
        "assuranceMode": ASSURANCE, "executableAuthority": False,
        "qualificationCredit": False, "targetProcessSpawns": 0, "targetCalls": 0,
        "legacyVmRoute": "historical-only; no budget or decision is read or changed",
        "blockerStatus": "recorded-implementation-blockers; current source is unverified",
        "blockerObservation": {
            "observedUtc": "2026-10-16T17:23:28.947458Z",
            "platformRevision": "40ede27ab6f6ccde5628b5ad644c968d155d757e",
            "buildsRevision": "a573a793e0d32db3304c3779ad062edaa51c6441",
            "directMemoriesRevision": "2a2daacaf39f08503a3f8bd93c665912a68d4abd",
            "inspectionReceiptSha256": "6b46089d0dcee22e802d3f10d3a5a46206c54b65319ddf75510efd34653277f6",
            "reinspectionRequired": True,
            "scope": "Recorded standalone Platform inspection; no supplied source root is inspected or bound by this output.",
        },
        "recordedSupportedContracts": [
            {"id": "platform-v2-test-selection", "owner": "Hexalith.Builds",
             "source": "schemas/hexalith.module-manifest.v2.json; src/libraries/Hexalith.Builds.Tooling/Runtime/{ModuleTestSelection,NativeTestExecutor,NativeTestHandoff}.cs",
             "support": "Declared v2 tests select a module-owned python3 entry through --profile, bind manifest/entry hashes and hand off selected plan/readiness/source-mapping paths. The Python executor supplies no CLI arguments; Memories has not enrolled a supported retention test in its declaration."},
            {"id": "platform-test-lifetime", "owner": "Hexalith.Builds",
             "source": "src/libraries/Hexalith.Builds.Tooling/Runtime/{ModuleTestSelection,ModuleTestExecutionBudget,NativeTestExecutor}.cs",
             "support": "Selected tests carry up to 14400 seconds from the initial request, a separate stop bound up to 900 seconds and matching credential expiry. The legacy 15-minute default does not limit this selected path; no retention test lifetime or operational window has been approved."},
            {"id": "platform-test-outcomes", "owner": "Hexalith.Builds",
             "source": "src/libraries/Hexalith.Builds.Tooling/Runtime/{ModuleCommandExecutionService,CompositionEngine,ModuleTestRunRecord}.cs",
             "support": "Selected tests record the first terminal outcome, owner withdrawal, local failure retention, CI/success cleanup and retained/removed/remaining/unknown resource disposition. Attachments are refused. Observed candidate/backend identities remain null and qualification credit is false."},
            {"id": "platform-direct-candidate-baseline", "owner": "Hexalith.Platform / Hexalith.Memories",
             "source": "active-root direct references/Hexalith.Memories; module/hexalith.module-manifest.v2.json",
             "support": "At inspection the direct Memories checkout and this workspace both started at 2a2daacaf39f08503a3f8bd93c665912a68d4abd with identical helper/CLI/declaration bytes. This is source alignment at that observation, not loaded-artifact evidence or automatic alignment of later local changes."},
        ],
        "blockers": [
            {"id": "platform-descriptor-result", "owner": "Hexalith.Builds / Hexalith.Platform",
             "source": "src/libraries/Hexalith.Builds.Tooling/Runtime/{NativeTestHandoff,NativeTestExecutor,SelectedNativeTestReport,CompositionRunPlan,CompositionReadiness,ModuleTestRunRecord,CompositionEngine}.cs; src/libraries/Hexalith.Builds.Tooling/RunEvidence/ModuleRunEvidence.cs",
             "gap": "Existing handoff/readiness lacks Memories workload, strong Dapr, reminder and fixed read-only PostgreSQL observation access and actual loaded candidate/runtime/backend identities.",
             "implementationConstraint": "ModuleTestRunRecord exposes ObservedCandidateHash and ObservedBackendIdentity, but CompositionEngine.RecordTestOutcomeAsync writes both as null. Selected test metadata, source hashes and successful cleanup cannot establish those missing observations. NativeTestExecutor also requires a native TRX report at HEXALITH_G4_TEST_REPORT_PATH, while the existing verifier emits JSON.",
             "nextImplementation": "Extend the existing runner-owned observation and identity fields with actual Platform measurements and fixed scoped access, then consume that supported encoding in Memories. Produce the existing native TRX report and verify missing, malformed and failed reports through the runner's existing report validation. Reuse implemented selection, lifetime, withdrawal and outcome policy; no new report protocol, Memories descriptor or caller-authored success.",
             "reopen": "Existing handoff/readiness/result supplies actual matching Memories/Dapr/PostgreSQL identities and observation access, with tested missing/foreign/stale/drift refusals."},
            {"id": "platform-postgresql-profile", "owner": "Hexalith.Platform",
             "source": "src/Hexalith.Platform.Composition/PlatformDaprBindings.cs:Type; src/Hexalith.Platform.Composition/PlatformCompositionOptions.cs; DaprComponents/reference/telemetry-state.yaml; references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Runtime/CompositionRunPlan.cs",
             "gap": "Telemetry state resolves to state.redis; no runner-selectable run-owned PostgreSQL 18.6 verify-full TLS profile or immutable Dapr 1.18.1 identity is described.",
             "implementationConstraint": "ModuleTestSelection chooses the test entry and lifetime, not a backend profile. ComposeRun supplies authentication overrides only, and PlatformDaprBindings.Type still resolves every state role to Redis. The existing plan cannot select or describe the required PostgreSQL resources/TLS/runtime binding.",
             "nextImplementation": "Extend Platform's existing options, Dapr binding and run-owned resource seams together with existing runner plan/observations for the immutable PostgreSQL/TLS profile; leave default Redis bindings unchanged. A standalone unused PostgreSQL template is insufficient.",
             "reopen": "Existing runner selects and describes the real PostgreSQL composition, resources, TLS identity and immutable runtime/backend pins; keep other consumers' Redis defaults."},
            {"id": "platform-local-capability-binding", "owner": "Hexalith.Platform / Hexalith.Memories",
             "source": "src/Hexalith.Platform.Composition/Declarations/local-profile.json; src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationGate.cs",
             "gap": "Local lifecycle is disabled with placeholder capability evidence, and server/clock/telemetry each use 86400-second retention. The workload gate requires Qualification and the KVM profile; neither toggle nor hash reuse establishes a truthful local binding.",
             "implementationConstraint": "No supported local profile identity or run-owned workload authorization is supplied by the runner; accepting a caller hash or switching the environment name would bypass the existing capability/qualification gate. The existing 15-minute MaximumGateLifetime cannot cover the one-hour experiment without supported renewal.",
             "nextImplementation": "Bind a real Platform local profile and capability/clock probe results to the existing Memories workload authorization and fixed observation path. Configure and verify consistent 3600-second retention in server, clock and telemetry. Supply supported bounded run-owned gate renewal with fresh expiry/withdrawal checks, preserving MaximumGateLifetime, the absolute run lifetime and cohort clocks; preserve Production and full-profile refusal.",
             "reopen": "Supported local profile enables real startup probes, signed clock validation and normal workload/observation access without bypassing capabilities or Production gates."},
            {"id": "platform-tool-source-alignment", "owner": "Hexalith.Platform / Hexalith.Memories",
             "source": ".config/dotnet-tools.json; active-root direct references/Hexalith.Memories; module/hexalith.module-manifest.v2.json; tools/verify-access-telemetry-lifecycle.py:_parser; tools/access_telemetry_functional_retention.py",
             "gap": "The selected Platform checkout has no tool pin; actual loaded artifacts and the eventual retention consumer candidate remain unbound despite matching starting Memories source.",
             "implementationConstraint": "The direct helper now exists and matches the starting workspace, but neither Memories declaration enrolls this retention test. Selected Python receives no arguments, the existing verifier requires --checkpoint and the helper has no main entry. Subsequent local changes are not automatically the direct candidate, and no supported installed tool or observed loaded artifacts are established. Dependency updates and sibling/ancestor fallback remain outside preparation authority.",
             "nextImplementation": "After profile/observation/local gate support exists, adapt the fixed selected entry/checkpoint through the existing verifier and enroll it through the supported declaration path. Use the owner tool-pin/provenance workflow and bind exact direct candidate/loaded bytes; no new runner or accepting implementation is supplied by this diagnostic, and the no-dependency-update boundary remains.",
             "reopen": "Bind the supported pinned tool, selected Platform/Builds revisions and direct Memories candidate; no sibling, ancestor or nested fallback."},
        ],
        "nextAction": "Reinspect the recorded owning sources before relying on this observation. Complete the existing Platform PostgreSQL profile, runner observation/loaded-identity binding and local capability/workload authorization, then enroll the real consumer and bind the supported tool/direct candidate. No current readiness or operational approval is asserted.",
    }


def platform_preparation_status() -> dict:
    """Keep historical inspection facts while naming current unobserved prerequisites.

    This side-effect-free diagnostic does not inspect a checkout or issue an
    operational approval. The selected entry separately validates real runner
    observations; declarations and offline tests cannot establish readiness.
    """
    result = _recorded_platform_preparation_status()
    result["recordedBlockers"] = result.pop("blockers")
    result["blockerStatus"] = "prepared-source; actual startup and tool/artifact alignment unverified"
    result["blockers"] = [
        {"id": "platform-tool-source-alignment", "owner": "Hexalith.Platform / Hexalith.Builds",
         "source": ".config/dotnet-tools.json; src/libraries/Hexalith.Builds.Tooling/Runtime/CompositionPrerequisiteProbe.cs",
         "gap": "The selected Platform checkout has no supported tool pin; a complete Platform build is blocked by the existing JwtBearer 10.0.13/10.0.12 CS1705 mismatch.",
         "reopen": "Use the owner tool/provenance workflow and resolve the existing dependency alignment under separate authorization; build the actual selected source without changing dependencies in preparation."},
        {"id": "platform-runtime-artifact", "owner": "Hexalith.Platform / Hexalith.Builds",
         "source": "PlatformRetentionProfile.ObserveAsync; CompositionRuntimeArtifactVerifier.Verify; <DaprHome>/.dapr/runtime-image.oci.tar",
         "gap": "No retained immutable Dapr 1.18.1 OCI archive links the configured native daprd to the approved image and amd64 manifest.",
         "reopen": "Retain the exact digest-verified OCI index, manifest and layers through the authorized artifact workflow; verify the actually running daprd inode against their executable."},
        {"id": "platform-startup-observations", "owner": "Hexalith.Platform / Hexalith.Memories",
         "source": "PlatformRetentionProfile; PlatformPostgresqlHealthCheck; RunReadiness; AccessTelemetryCapabilityProbeRunner",
         "gap": "The prepared PostgreSQL/TLS/local-capability/3600-second profile has never been started. Resource fit, actual app/runtime/backend identities, all three retention configurations and actual startup probes remain unobserved.",
         "reopen": "After prerequisites and exact operational approval, obtain complete fresh matching runner-owned observations; refuse missing/foreign mappings, unavailable proc mappings, failed TLS/probes and partial startup."},
        {"id": "platform-real-functional-outcome", "owner": "Hexalith.Builds / Hexalith.Memories",
         "source": "NativeTestExecutor; CompositionSelectedTestGate; CompositionEngine.RecordTestOutcomeAsync; tools/verify-access-telemetry-lifecycle.py",
         "gap": "The fixed selected entry and native TRX consumer are prepared, but no real retention run, owner withdrawal or outcome-aware cleanup has occurred.",
         "reopen": "Complete the approved two-cohort observation and retain the actual Platform terminal outcome and cleanup disposition; offline doubles grant no interoperability or qualification credit."},
    ]
    result["nextAction"] = "Complete focused source review and offline validation, align the supported tool and immutable runtime artifact, then prepare a usable exact operational approval. No workload authority or readiness is asserted."
    return result


def prepare_local_bundle(bundle_root: Path, source_root: Path) -> dict:
    """Refuse legacy VM preparation without creating files or changing authority."""
    return platform_preparation_status()


def run_functional_retention(*, plan_path: Path | None = None, approved_plan_sha256: str | None = None, source_root: Path | None = None, custody_root: Path | None = None) -> int:
    """Refuse current execution before file reads, process spawn or target access.

    An old exact approval or a caller-authored Platform-shaped object cannot
    select the retained VM driver or replace the missing owning runner contract.
    """
    print(json.dumps(platform_preparation_status(), sort_keys=True))
    return 1

def run_selected_platform_retention() -> int:
    """Consume the existing selected-test handoff; own no environment lifecycle."""
    import xml.etree.ElementTree as xml
    result = {"checkpoint": "functional-retention", "status": "rejected", "qualificationCredit": False, "assuranceMode": ASSURANCE}
    report = None
    loaded_descriptors = ExitStack()
    try:
        _require(os.environ.get("HEXALITH_G4_TEST_NAME") == "functional-retention" and os.environ.get("HEXALITH_G4_TEST_OWNER") == "memories" and os.environ.get("HEXALITH_G4_TEST_MODE") == "source", "selected-test-required")
        plan_path = Path(os.environ["HEXALITH_G4_PLAN_PATH"])
        root = plan_path.parent
        _require(plan_path.is_absolute() and plan_path.name == "plan.json", "selected-plan-path")
        candidate_report = Path(os.environ["HEXALITH_G4_TEST_REPORT_PATH"])
        _require(candidate_report == root / "native-tests" / "native.trx", "selected-report-path")
        _canonical_path(str(candidate_report.parent))
        report = candidate_report
        plan_bytes = _private(str(plan_path), str(root))
        plan = json.loads(plan_bytes)
        selection = plan["selectedTest"]
        _require(plan["schema"] == "hexalith.g4-run-plan.v1" and plan["runId"] == os.environ["HEXALITH_G4_RUN_ID"], "selected-plan-identity")
        _require(selection["moduleId"] == "memories" and selection["name"] == "functional-retention" and selection["entry"] == "tools/verify-access-telemetry-lifecycle.py" and selection["mode"] == "source" and selection["daprRuntimeVersion"] == "1.18.1", "selected-entry")
        _require(selection["executionTimeoutSeconds"] == 14400 and selection["stopTimeoutSeconds"] == 900, "selected-lifetime")
        candidate = Path(plan["sourceMapping"]["root"]) / "references" / "Hexalith.Memories"
        _require(candidate == Path(__file__).resolve().parent.parent, "selected-direct-candidate")
        for relative, key, envelope in (("module/hexalith.module-manifest.v2.json", "manifestHash", "HEXALITH_G4_TEST_MANIFEST_HASH"), (selection["entry"], "entryHash", "HEXALITH_G4_TEST_ENTRY_HASH")):
            digest = _sha((candidate / relative).read_bytes())
            _require(digest == selection[key].lower() == os.environ[envelope].lower(), "selected-source-drift")
        readiness_path = root / "readiness.json"
        _require(os.environ["HEXALITH_G4_READINESS_PATH"] == str(readiness_path), "selected-readiness-path")
        readiness_bytes = _private(str(readiness_path), str(root))
        readiness = json.loads(readiness_bytes)
        _require(readiness["schema"] == "hexalith.g4-run-readiness.v1" and readiness["runId"] == plan["runId"] and readiness["selectedTest"] == selection, "selected-readiness-identity")
        _require(readiness["resources"] and all(resource["state"] == "Healthy" for resource in readiness["resources"]), "partial-startup")
        observation = readiness["testObservation"]
        _require(observation["isComplete"] is True and observation["runId"] == plan["runId"] and observation["moduleId"] == "memories" and observation["runtimeArtifactVerified"] is True, "selected-observations-unavailable")
        profile = json.loads(observation["profileJson"])
        _require(_sha(observation["profileJson"].encode()) == observation["profileHash"] and profile["purpose"] == "local-functional-retention" and profile["runId"] == plan["runId"] and profile["sourceMappingHash"] == plan["sourceMappingHash"] and profile["retentionSeconds"] == 3600 and profile["sslMode"] == "verify-full" and profile["qualificationCredit"] is False, "selected-profile")
        _require(profile["runtimeImage"] == next(image["reference"] for image in artifact_catalog()["images"] if image["id"] == "daprd") and profile["postgresqlImage"] == next(image["reference"] for image in artifact_catalog()["images"] if image["id"] == "postgresql") and re.fullmatch(re.escape(profile["postgresqlImage"]) + r"/180006\|t\|TLSv1\.[23]\|telemetry_observer\|on/sha256:[a-f0-9]{64}", observation["observedBackendIdentity"]) is not None, "selected-backend")
        _require(_HEX.fullmatch(observation["observedCandidateHash"]) and _HEX.fullmatch(observation["observedRuntimeHash"]) and observation["loadedArtifactHashes"], "selected-loaded-artifacts")
        _require(observation["serviceRetentionSeconds"] == {name: 3600 for name in ("memories", "memories-access-telemetry", "memories-access-telemetry-clock")}, "selected-observed-retention")
        mapping_path = root / "source-mapping" / "mapping.json"
        mapping = json.loads(_private(str(mapping_path), str(root)))
        _validate_selected_mapping(mapping, plan["sourceMappingHash"], os.environ["HEXALITH_SOURCE_MAPPING_HASH"])
        _require(mapping["Root"] == str(candidate.parent.parent) and mapping["Mode"] == "source" and [entry for entry in mapping["Entries"] if entry["Identity"] == "Hexalith.Memories"] == [{"Identity": "Hexalith.Memories", "Origin": "source", "Path": str(candidate)}], "selected-direct-mapping")
        aggregate = [{"Key": key, "Value": value} for key, value in sorted(observation["loadedArtifactHashes"].items())]
        _require(_sha(json.dumps(aggregate, separators=(",", ":")).encode()) == observation["observedCandidateHash"], "selected-loaded-aggregate")
        required_entries = {"memories": "Hexalith.Memories.Server.dll", "memories-access-telemetry": "Hexalith.Memories.AccessTelemetry.dll", "memories-access-telemetry-clock": "Hexalith.Memories.AccessTelemetry.Clock.dll"}
        _require(all(any(key.startswith(resource + "/") and Path(key).name == assembly for key in observation["loadedArtifactHashes"]) for resource, assembly in required_entries.items()), "selected-required-loaded-entry")
        loaded_inputs = []
        for relative, digest in observation["loadedArtifactHashes"].items():
            parts = relative.split("/")
            _require(len(parts) >= 3 and not any(part in {"", ".", ".."} for part in parts), "selected-loaded-artifact-path")
            path = candidate.joinpath(*parts[1:])
            _require(path.resolve().is_relative_to(candidate.resolve()) and not any(parent.is_symlink() for parent in (path, *path.parents)), "selected-loaded-artifact-path")
            # Executables exceed the strict JSON reader's 1-MiB evidence cap.
            # Keep complete streaming, byte bounds and held identity checks.
            held = _open_held_file(str(path), loaded_descriptors)
            _require(_hash_file(path)[0] == digest, "selected-loaded-artifact-drift")
            # Bind the identity on both sides of the hash read, then retain it
            # throughout execution. Revalidation never substitutes a new hash.
            held.revalidate()
            loaded_inputs.append(held)
        deadline = datetime.fromisoformat(selection["executionDeadlineUtc"].replace("Z", "+00:00"))
        now = datetime.now(timezone.utc)
        observed_at = datetime.fromisoformat(observation["observedAtUtc"].replace("Z", "+00:00"))
        _require(timedelta(0) <= now - observed_at <= timedelta(seconds=60) and deadline - now >= timedelta(seconds=4501), "selected-stale-or-short-lifetime")
        access_path = root / "platform-components" / "retention" / "access.json"
        _require(observation["accessPath"] == str(access_path) and observation["gatePath"] == str(access_path.with_name("gate.json")), "selected-access-path")
        access = json.loads(_private(str(access_path), str(root)))
        consumer = _PlatformRetentionConsumer(root, plan, plan_bytes, readiness_bytes, observation, access, deadline, loaded_inputs)
        observed = consumer.execute()
        consumer.check()
        result.update(observed)
        _exclusive_json(root / "native-tests" / "functional-observations.json", result)
    except _SEMANTIC_FAILURES as error:
        result["status"] = "rejected"
        result["reason"] = str(error) if isinstance(error, FunctionalRetentionError) else "functional-selected-input-refused"
    finally:
        loaded_descriptors.close()
    if report is not None:
        try:
            # A native report is a test outcome. Platform's subsequent real
            # cleanup/retention outcome remains authoritative for integration.
            passed = result["status"] == "functional-observations-passed"
            document = xml.Element("TestRun", xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
            results = xml.SubElement(document, "Results")
            reason = result.get("reason", "functional-selected-assertion-failed")
            safe_reason = reason if isinstance(reason, str) and re.fullmatch(r"functional-[a-z0-9-]{1,100}", reason) else "functional-selected-assertion-failed"
            # Identify the failed assertion in native test metadata. The runner
            # inspects this value for secrets and retains names, but removes all
            # captured Output, including ErrorInfo, from both passing and failed TRX.
            test_name = "PostgreSQL functional retention" + (" / " + safe_reason if not passed else "")
            native_result = xml.SubElement(results, "UnitTestResult", testName=test_name, outcome="Passed" if passed else "Failed")
            if not passed:
                output = xml.SubElement(native_result, "Output")
                error_info = xml.SubElement(output, "ErrorInfo")
                xml.SubElement(error_info, "Message").text = safe_reason
            summary = xml.SubElement(document, "ResultSummary", outcome="Completed" if passed else "Failed")
            xml.SubElement(summary, "Counters", total="1", executed="1", passed="1" if passed else "0", failed="0" if passed else "1", notExecuted="0")
            _exclusive_bytes(report, xml.tostring(document, encoding="utf-8", xml_declaration=True))
        except _SEMANTIC_FAILURES:
            result["status"] = "rejected"
            result["reason"] = "functional-selected-native-output-refused"
    print(json.dumps(result, sort_keys=True))
    return 0 if result["status"] == "functional-observations-passed" else 1


def _revalidate_selected_loaded_input(held):
    """Check only the bound file metadata and the named ancestor identities."""
    expected = _file_metadata(held.before)
    named = os.stat(held.parts[-1], dir_fd=held.ancestors[-1][0], follow_symlinks=False)
    _require(expected == _file_metadata(named) == _file_metadata(os.fstat(held.descriptor)), "selected-loaded-artifact-drift")

    def directory_identity(metadata):
        return metadata.st_dev, metadata.st_ino, stat.S_IFMT(metadata.st_mode)

    for index, (descriptor, before) in enumerate(held.ancestors):
        named = os.stat("/", follow_symlinks=False) if index == 0 else os.stat(
            held.parts[index - 1], dir_fd=held.ancestors[index - 1][0], follow_symlinks=False)
        _require(directory_identity(before) == directory_identity(named) == directory_identity(os.fstat(descriptor)), "selected-loaded-artifact-drift")


class _PlatformRetentionConsumer:
    """Fixed workload and observation access only; Platform owns gate/lifecycle."""

    def __init__(self, root, plan, plan_bytes, readiness_bytes, observation, access, deadline, loaded_inputs):
        self.root, self.plan, self.plan_bytes, self.readiness_bytes = root, plan, plan_bytes, readiness_bytes
        self.observation, self.access, self.deadline = observation, access, deadline
        self.loaded_inputs = tuple(loaded_inputs)
        self.started = time.monotonic()
        self.wall = time.time()
        self.segments = {}
        self.ttl_absent = set()
        self.before_sequence = 0
        self.full_records = {}
        endpoints = observation["endpoints"]
        _require(set(endpoints) == {name + suffix for name in ("memories", "memories-access-telemetry", "memories-access-telemetry-clock") for suffix in ("", "/dapr")}, "selected-endpoints")
        for address in endpoints.values():
            uri = urllib.parse.urlsplit(address)
            _require(uri.scheme == "http" and uri.hostname in {"localhost", "127.0.0.1", "::1"} and uri.port and not uri.username and not uri.password and uri.path in {"", "/"} and not uri.query and not uri.fragment, "selected-endpoint-scope")
        _require(re.fullmatch(r"[0-9a-f]{64}", observation["postgresqlContainerId"]) is not None, "selected-backend-container")

    def check(self):
        _require(datetime.now(timezone.utc) < self.deadline and abs(time.time() - self.wall - (time.monotonic() - self.started)) <= 1, "selected-expiry-or-clock-step")
        try:
            for held in self.loaded_inputs:
                _revalidate_selected_loaded_input(held)
        except (ValueError, OSError):
            raise FunctionalRetentionError("functional-selected-loaded-artifact-drift") from None
        _require(_private(str(self.root / "plan.json"), str(self.root)) == self.plan_bytes and _private(str(self.root / "readiness.json"), str(self.root)) == self.readiness_bytes, "selected-descriptor-drift")
        gate = json.loads(_private(self.observation["gatePath"], str(self.root)))
        now_ms = int(time.time() * 1000)
        _require(set(gate) == {"schemaVersion", "state", "profileSha256", "expiresUtcMs"} and gate["schemaVersion"] == 1 and gate["state"] == "enabled" and gate["profileSha256"] == self.observation["profileHash"] and now_ms < gate["expiresUtcMs"] <= min(now_ms + 905000, int(self.deadline.timestamp() * 1000)), "selected-gate-withdrawn-or-expired")

    def request(self, resource, path, *, data=None, headers=None, missing=False, raw_response=False, read_deadline=None):
        self.check()
        credentials = self.access[resource.removesuffix("/dapr")]
        token = credentials["DAPR_API_TOKEN" if resource.endswith("/dapr") else "APP_API_TOKEN"]
        request = urllib.request.Request(self.observation["endpoints"][resource].rstrip("/") + path, data=data, headers={"dapr-api-token": token, **(headers or {})})
        class NoRedirect(urllib.request.HTTPRedirectHandler):
            def redirect_request(self, *args, **kwargs):
                return None
        opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
        timeout = 120 if data is not None else 5
        if read_deadline is not None:
            remaining = read_deadline - time.monotonic()
            _require(data is None and 0 < remaining <= LIMITS["readAggregateSeconds"], "selected-read-aggregate-deadline")
            timeout = min(timeout, remaining)
        phase = "open"
        try:
            with opener.open(request, timeout=timeout) as response:
                phase = "body"
                raw = response.read(1048577)
                _require(len(raw) <= 1048576, "selected-output-limit")
                return (response.status, raw) if raw_response else json.loads(raw) if raw else None
        except urllib.error.HTTPError as error:
            if missing and error.code in {204, 404}:
                return None
            raise FunctionalRetentionError("functional-selected-request-failed") from None
        except (urllib.error.URLError, TimeoutError, ConnectionError, http.client.HTTPException) as error:
            uncertain = TransportUncertain("functional-selected-transport-uncertain")
            uncertain.category = _selected_transport_category(error)
            uncertain.phase = phase
            raise uncertain from None

    def states(self, run_id, segment, buckets=False):
        # One budget covers all keys, batches and retries in this observation.
        # Reuse the existing read-only attempt count/backoff; writes are unchanged.
        started = time.monotonic()
        deadline = started + LIMITS["readAggregateSeconds"]
        failures = []
        failure_lock = Lock()
        emitted = self.segments[segment]
        keys = fixed_record_keys(run_id, segment)
        if buckets:
            keys = tuple(sorted({f"expiry-bucket/{(emitted + ordinal + 3600000) // 60000:012d}/{int(key.split('/')[1]):02d}" for ordinal, key in enumerate(keys)}))
        def check_read():
            self.check()
            remaining = deadline - time.monotonic()
            _require(remaining > 0, "selected-read-aggregate-deadline")
            return remaining

        def read(item):
            ordinal, key = item
            path = "/v1.0/state/access-telemetry-store/" + urllib.parse.quote(key, safe="") + "?consistency=strong&metadata.partitionKey=access-telemetry"
            for attempt in range(LIMITS["readAttempts"]):
                check_read()
                try:
                    value = self.request("memories-access-telemetry/dapr", path, missing=True, read_deadline=deadline)
                except TransportUncertain as error:
                    category = getattr(error, "category", "unclassified")
                    phase = getattr(error, "phase", "unknown")
                    with failure_lock:
                        failures.append({"ordinal": ordinal, "attempt": attempt + 1,
                                         "observedUtcMs": int(time.time() * 1000),
                                         "elapsedMilliseconds": int((time.monotonic() - started) * 1000),
                                         "category": category if type(category) is str and category in {"timeout", "remote-disconnected", "incomplete-read", "bad-status-line", "connection-refused", "connection-reset", "connection-aborted", "connection-error", "http-protocol"} else "unclassified",
                                         "phase": phase if type(phase) is str and phase in {"open", "body"} else "unknown"})
                    remaining = check_read()
                    if attempt == LIMITS["readAttempts"] - 1:
                        raise TransportUncertain("functional-selected-state-read-transport-exhausted") from None
                    delay = (2, 5)[attempt]
                    _require(delay < remaining, "selected-read-aggregate-deadline")
                    time.sleep(delay)
                    check_read()
                else:
                    check_read()
                    return {"key": key, "value": value}
        observed = []
        try:
            with ThreadPoolExecutor(max_workers=16) as reads:
                for offset in range(0, len(keys), 16):
                    check_read()
                    observed.extend(reads.map(read, enumerate(keys[offset:offset + 16], start=offset + 1)))
            check_read()
        except _SEMANTIC_FAILURES:
            if failures:
                # At most 125 fixed keys * three attempts. Never retain keys,
                # endpoints, credentials, exception text or response bytes.
                _require(_IDENTITY.fullmatch(run_id) is not None and segment in {"cohort-a", "cohort-b"}
                         and len(failures) <= FUNCTIONAL["recordsPerCohort"] * LIMITS["readAttempts"], "selected-transport-observation-scope")
                _exclusive_json(self.root / "native-tests" / "state-read-transport-failure.json",
                                {"runId": run_id, "segmentId": segment, "kind": "expiry-bucket" if buckets else "record",
                                 "attemptLimit": LIMITS["readAttempts"], "aggregateLimitSeconds": LIMITS["readAggregateSeconds"],
                                 "failures": sorted(failures, key=lambda failure: (failure["ordinal"], failure["attempt"]))})
            raise
        return observed

    def cohort(self, run_id, segment):
        records = []
        full = {}
        for item in self.states(run_id, segment):
            record = item["value"]
            _require(type(record) is dict, "selected-record-missing")
            full[record["recordId"]] = json.loads(json.dumps(record, sort_keys=True))
            records.append({"recordId": record["recordId"], "envelopeHash": record["envelopeHash"], **{output: int(datetime.fromisoformat(record[source].replace("Z", "+00:00")).timestamp() * 1000) for output, source in (("emittedUtcMs", "emittedAtUtc"), ("expiresUtcMs", "expiresAtUtc"), ("acceptedUtcMs", "acceptedAtUtc"))}})
        _require(segment not in self.full_records or self.full_records[segment] == full, "selected-canonical-cohort-changed")
        self.full_records.setdefault(segment, full)
        return {"consistency": "strong", "records": records}

    def physical(self, run_id):
        self.check()
        argv = ["docker", "exec", "--env", "PGSSLMODE=verify-full", "--env", "PGSSLROOTCERT=/run/retention/ca.pem", "--env", "PGPASSFILE=/run/retention/pgpass", self.observation["postgresqlContainerId"], "psql", "-h", "127.0.0.1", "-w", "-XAt", "-U", "telemetry_observer", "-d", "telemetry", "-c", fixed_physical_bound_query(run_id)]
        physical = subprocess.run(argv, capture_output=True, timeout=10, check=True)
        _require(len(physical.stdout) + len(physical.stderr) <= 1048576, "selected-postgres-output-limit")
        value = json.loads(physical.stdout)
        _require(value["serverVersionNum"] == "180006" and value["ssl"] is True and value["tlsVersion"] in {"TLSv1.2", "TLSv1.3"} and value["role"] == "telemetry_observer" and value["readOnly"] == "on", "selected-postgres-session")
        _require(type(value["observedUtcMs"]) is int and abs(value["observedUtcMs"] - int(time.time() * 1000)) <= 1000 and type(value["rows"]) is list, "selected-postgres-clock")
        return value

    def actor(self, *, initial=False):
        status, raw = self.request("memories-access-telemetry/dapr", "/v1.0/actors/AccessTelemetryLifecycleActor/global/state/lifecycle-control", raw_response=True)
        if initial and status == 204 and raw == b"":
            return None
        _require(status == 200 and bool(raw), "selected-actor-state")
        value = json.loads(raw)
        _require(type(value) is dict, "selected-actor-state")
        state = {name: value.get(name, value.get(name[0].lower() + name[1:])) for name in ("ReminderSequence", "LastPurgeUnixMilliseconds")}
        _require(type(state["ReminderSequence"]) is int and state["ReminderSequence"] >= 0
                 and (state["LastPurgeUnixMilliseconds"] is None or type(state["LastPurgeUnixMilliseconds"]) is int and state["LastPurgeUnixMilliseconds"] >= 0), "selected-actor-state")
        return state

    def wait(self, wall_ms, monotonic_deadline):
        while int(time.time() * 1000) < wall_ms or time.monotonic() < monotonic_deadline:
            self.check()
            time.sleep(min(30, max(0.01, max((wall_ms - time.time() * 1000) / 1000, monotonic_deadline - time.monotonic()))))
        self.check()

    def record_accounting_observation(self, segment, accounting):
        """Retain bounded response counters before validation can reject them."""
        self.check()
        run_id = self.plan["runId"]
        _require(_IDENTITY.fullmatch(run_id) is not None and segment in {"cohort-a", "cohort-b"}, "selected-accounting-observation-scope")
        counters = {}
        if type(accounting) is dict and accounting.get("runId") == run_id and accounting.get("segmentId") == segment:
            for name in ("attempted", "enqueued", "acknowledged", "persisted", "transactionAcknowledgements", "conflicted", "rejected", "dropped"):
                value = accounting.get(name)
                if type(value) is int and 0 <= value <= FUNCTIONAL["recordsPerCohort"]:
                    counters[name] = value
        _exclusive_json(self.root / "native-tests" / (segment + "-accounting.json"),
                        {"runId": run_id, "segmentId": segment, "observedUtcMs": int(time.time() * 1000), "counters": counters})

    def execute(self):
        run_id = self.plan["runId"]
        _require(_IDENTITY.fullmatch(run_id) is not None, "selected-run-id")
        preflight = self.physical(run_id)
        _require(not preflight["rows"], "selected-preflight-rows-present")
        initial_actor = self.actor(initial=True)
        # GetStateAsync uses a new AccessTelemetryLifecycleActorState when the durable
        # control state is absent. Its long ReminderSequence starts at zero; this is
        # an explicit absent baseline, never evidence of a reminder or purge.
        self.before_sequence = 0 if initial_actor is None else initial_actor["ReminderSequence"]
        initial_actor_observation = {"httpStatus": 204 if initial_actor is None else 200, "statePresent": initial_actor is not None,
                                     "reminderSequence": self.before_sequence, "sequenceSource": "state-contract-initial-zero" if initial_actor is None else "stored-state"}
        cohorts, clocks = {}, {}
        writer = None
        for segment in ("cohort-a", "cohort-b"):
            if segment == "cohort-b":
                self.wait(self.segments["cohort-a"] + 1800000, clocks["cohort-a"] + 1800)
            self.segments[segment], clocks[segment] = int(time.time() * 1000), time.monotonic()
            for attempt in range(2):
                try:
                    accounting = self.request("memories", "/operations/access-telemetry/qualification/fixed-workload", data=b"", headers={"X-Hexalith-Qualification-Run": run_id, "X-Hexalith-Qualification-Segment": segment, "X-Hexalith-Qualification-Emitted-Utc-Ms": str(self.segments[segment])})
                    break
                except TransportUncertain:
                    _require(attempt == 0, "selected-emit-budget")
            self.record_accounting_observation(segment, accounting)
            observed_writer = validate_accounting(accounting, run_id, segment, self.segments[segment])
            _require(writer is None or observed_writer == writer, "selected-writer-drift")
            writer = observed_writer
            cohorts[segment] = validate_cohort(self.cohort(run_id, segment), run_id=run_id, segment=segment, emitted_ms=self.segments[segment])
        self.wait(self.segments["cohort-a"] + 3540000, clocks["cohort-a"] + 3540)
        validate_cohort(self.cohort(run_id, "cohort-a"), run_id=run_id, segment="cohort-a", emitted_ms=self.segments["cohort-a"], expected=cohorts["cohort-a"])
        bound = self.physical(run_id)
        bound_rows = {row["recordId"]: row for row in bound["rows"]}
        _require(len(bound["rows"]) == len(bound_rows) == 125 and set(bound_rows) == set(cohorts["cohort-a"]), "selected-ttl-bound-missing")
        for identity, row in bound_rows.items():
            expiry = cohorts["cohort-a"][identity]["expiresUtcMs"]
            _require(row["record"] == self.full_records["cohort-a"][identity] and type(row["expiresUtcMs"]) is int and expiry <= row["expiresUtcMs"] <= expiry + 900000 and bound["observedUtcMs"] < expiry, "selected-ttl-bound-drift")
        self.wait(self.segments["cohort-a"] + 3590000, clocks["cohort-a"] + 3590)
        validate_cohort(self.cohort(run_id, "cohort-a"), run_id=run_id, segment="cohort-a", emitted_ms=self.segments["cohort-a"], expected=cohorts["cohort-a"])
        self.wait(self.segments["cohort-a"] + 3600124, clocks["cohort-a"] + 3600.124)
        while True:
            from access_telemetry_producer_common import _qualification_record_ids
            ids = _qualification_record_ids(run_id, "cohort-a")
            absent = [record_id for record_id, item in zip(ids, self.states(run_id, "cohort-a")) if item["value"] is None]
            self.check()
            physical = self.physical(run_id)
            for row in physical["rows"]:
                _require(row["recordId"] in bound_rows and row == bound_rows[row["recordId"]], "selected-ttl-bound-drift")
            # Local evidence proves actual pre-expiry stored-row/TTL binding,
            # elapsed PostgreSQL expiry and strong absence. It does not identify
            # a physical deletion mechanism or claim full reclamation proof.
            self.ttl_absent.update(identity for identity in absent if physical["observedUtcMs"] >= bound_rows[identity]["expiresUtcMs"])
            indexed = {entry["recordId"] for bucket in self.states(run_id, "cohort-a", buckets=True) if bucket["value"] is not None for entry in bucket["value"]["entries"]}
            actor = self.actor()
            observation = {"consistency": "strong", "absentRecordIds": absent, "removedIndexIds": [record_id for record_id in ids if record_id not in indexed], "reminderSequenceBefore": self.before_sequence, "reminderSequenceAfter": actor["ReminderSequence"], "progressObservedUtcMs": actor["LastPurgeUnixMilliseconds"] or 0, "actorDeletedRecordIds": [], "ttlAbsentRecordIds": sorted(self.ttl_absent)}
            passed = validate_purge(observation, cohorts["cohort-a"], now_ms=int(time.time() * 1000))
            validate_cohort(self.cohort(run_id, "cohort-b"), run_id=run_id, segment="cohort-b", emitted_ms=self.segments["cohort-b"], expected=cohorts["cohort-b"])
            if passed:
                return {"status": "functional-observations-passed", "runId": run_id, "profileSha256": self.observation["profileHash"], "initialActorState": initial_actor_observation, "cohorts": cohorts, "canonicalRecords": self.full_records, "cohortClocks": {segment: {"utcMs": self.segments[segment], "monotonicSeconds": clocks[segment]} for segment in clocks}, "ttlEvidence": {"claim": "local-pre-expiry-row-and-ttl-bound-plus-elapsed-postgresql-clock-and-strong-absence", "preflight": preflight, "preExpiry": bound, "lastObservation": physical}, "purge": observation, "lifecycleOutcome": "pending-real-Platform-outcome"}
            self.wait(int(time.time() * 1000) + 30000, time.monotonic() + 30)
