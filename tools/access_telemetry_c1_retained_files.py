"""Offline private-file readback of one PG2 C1.15 packet and its exact streams.

Local OS ownership establishes a custody boundary only. Matching retained bytes
grants no execution, provenance, authenticated authority or gate acceptance.
"""

from __future__ import annotations

from contextlib import ExitStack
import os

from access_telemetry_c1_capture_semantics import inspect_c115_command_declarations
from access_telemetry_c1_capture_streams import (
    C115CommandStreams, C115StreamInspection, inspect_c115_retained_streams,
)
from access_telemetry_c1_interchange import (
    InterchangeFormatError, JsonSnapshot, MAX_AGGREGATE_BYTES, MAX_ARTIFACT_BYTES,
    _digest, _object, _refuse,
)
from access_telemetry_c1_owner_dispatch import (
    OwnerRunDispatchError, _canonical_path, _open_held_file,
)


STREAM_MANIFEST_SCHEMA = "hexalith.access-telemetry.c1.retained-streams/v1"
_MANIFEST_FIELDS = frozenset(("schemaVersion", "captureSha256", "receipts"))
_RECEIPT_FIELDS = frozenset(("index", "purpose", "stdoutSha256", "stderrSha256"))


def _entries(descriptor: int, expected: set[str]) -> None:
    # Stop on the first surplus entry instead of collecting an unbounded listing.
    names = set()
    with os.scandir(descriptor) as entries:
        for entry in entries:
            if entry.name not in expected or entry.name in names:
                _refuse("c115-files-sidecar-set-mismatch")
            names.add(entry.name)
    if names != expected:
        _refuse("c115-files-sidecar-set-mismatch")


def inspect_c115_retained_files(packet_path: str, *, custody_root: str) -> C115StreamInspection:
    """Read a fresh private packet/sidecar, inspect, and recheck exact bytes.

    Canonical absolute paths must put the packet strictly below custody_root.
    Directories at/below that root and every regular single-link artifact must
    belong to the effective OS user and have no group/other permissions. Held
    non-following descriptors bind all names and ancestors through final bounded
    readback. The manifest is capped separately at 1 MiB; packet plus every
    stdout/stderr occurrence is capped at 32 MiB, with each artifact at 1 MiB.
    No process, network, target, configuration or clock dependency is used.
    """
    return _compose_c115_retained_files(packet_path, custody_root=custody_root)


def _compose_c115_retained_files(packet_path: str, *, custody_root: str,
                                 inspection=None, reader=None):
    """Keep packet/stream descriptors through a private callback and readback.

    A composing reader may own a longer-lived descriptor stack and shared
    identity/budget accounting. The public wrapper keeps its original behavior.
    """
    refusal = None
    try:
        custody_root = _canonical_path(custody_root)
        packet_path = _canonical_path(packet_path)
        if not packet_path.startswith(custody_root + "/"):
            _refuse("c115-files-packet-outside-custody")
        if not hasattr(os, "geteuid"):
            _refuse("c115-files-filesystem-unavailable")
        effective_uid = os.geteuid()
        with ExitStack() as descriptors:
            artifacts = []
            identities = set()
            total = 0

            def read(path: str, *, manifest: bool = False):
                nonlocal total
                if reader is not None:
                    held, raw = reader(path, manifest=manifest)
                    artifacts.append((held, raw))
                    return held, raw
                held = _open_held_file(path, descriptors, custody_root=custody_root,
                                       effective_uid=effective_uid)
                identity = (held.before.st_dev, held.before.st_ino)
                if identity in identities:
                    _refuse("c115-files-distinct-files-required")
                identities.add(identity)
                if held.before.st_size > MAX_ARTIFACT_BYTES:
                    _refuse("c115-files-artifact-byte-budget-exceeded")
                if not manifest:
                    total += held.before.st_size
                    if total > MAX_AGGREGATE_BYTES:
                        _refuse("c115-files-aggregate-byte-budget-exceeded")
                raw = held.read()
                artifacts.append((held, raw))
                return held, raw

            held_packet, packet_raw = read(packet_path)
            capture = JsonSnapshot(packet_raw)
            commands = inspect_c115_command_declarations(capture)
            if commands.command_count > 10_000:
                _refuse("c115-files-sidecar-set-mismatch")
            sidecar = packet_path + ".streams"
            held_manifest, manifest_raw = read(sidecar + "/manifest.json", manifest=True)
            manifest = _object(JsonSnapshot(manifest_raw).value, _MANIFEST_FIELDS)
            _digest(manifest["captureSha256"])
            if manifest["schemaVersion"] != STREAM_MANIFEST_SCHEMA:
                _refuse("c115-files-manifest-schema-mismatch")
            if manifest["captureSha256"] != capture.sha256:
                _refuse("c115-files-capture-hash-mismatch")
            receipts = manifest["receipts"]
            if type(receipts) is not tuple or len(receipts) != commands.command_count:
                _refuse("c115-files-manifest-receipts-mismatch")
            expected = {"manifest.json"}
            for index in range(commands.command_count):
                expected.update((f"{index:04d}.stdout", f"{index:04d}.stderr"))
            sidecar_descriptor = held_manifest.ancestors[-1][0]
            _entries(sidecar_descriptor, expected)
            for index, (receipt, command) in enumerate(zip(receipts, capture.value["commands"])):
                receipt = _object(receipt, _RECEIPT_FIELDS)
                if (type(receipt["index"]) is not int or receipt["index"] != index
                        or receipt["purpose"] != command["purpose"]):
                    _refuse("c115-files-manifest-receipts-mismatch")
                for name in ("stdoutSha256", "stderrSha256"):
                    _digest(receipt[name])
                    if receipt[name] != command[name]:
                        _refuse("c115-files-manifest-receipts-mismatch")
            pairs = []
            for index, command in enumerate(capture.value["commands"]):
                _, stdout = read(sidecar + f"/{index:04d}.stdout")
                _, stderr = read(sidecar + f"/{index:04d}.stderr")
                pairs.append(C115CommandStreams(command["purpose"], stdout, stderr))
            result = (inspect_c115_retained_streams(capture, tuple(pairs))
                      if inspection is None else inspection(capture, tuple(pairs)))
            # A valid semantic result never escapes if bytes/names/custody changed
            # during inspection. Recheck the packet last, then every held name.
            for held, original in artifacts[1:] + artifacts[:1]:
                os.lseek(held.descriptor, 0, os.SEEK_SET)
                if held.read() != original:
                    _refuse("c115-files-artifact-bytes-drifted")
            _entries(sidecar_descriptor, expected)
            for held, _ in artifacts:
                held.revalidate()
            if os.geteuid() != effective_uid:
                _refuse("c115-files-effective-user-drifted")
    except InterchangeFormatError as error:
        refusal = str(error)
    except OwnerRunDispatchError as error:
        refusal = str(error).replace("owner-dispatch-", "c115-files-", 1)
    except (OSError, NotImplementedError, TypeError, AttributeError):
        refusal = "c115-files-filesystem-unavailable"
    # Finish cleanup and exception handlers before raising: internal exception
    # chains can contain paths or raw decoder bytes even with suppressed display.
    if refusal is not None:
        _refuse(refusal)
    return result
