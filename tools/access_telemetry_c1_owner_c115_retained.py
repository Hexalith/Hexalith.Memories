"""Approved-plan acquisition and local corroboration of private C1.15 evidence.

Retained bytes establish point-in-time consistency and local custody only;
they grant no execution, registration, continuing lock or gate acceptance.
"""

from __future__ import annotations

from contextlib import ExitStack
from datetime import datetime
import hashlib
import os
from typing import NoReturn

from access_telemetry_c1_interchange import (
    InterchangeFormatError, JsonSnapshot, MAX_AGGREGATE_BYTES, MAX_ARTIFACT_BYTES,
    _digest, _object,
)
from access_telemetry_c1_owner_c115 import OwnerC115InspectionError, inspect_owner_c115
from access_telemetry_c1_owner_c115_provenance import (
    OwnerC115LocalSourceCorroboration, _corroborate_owner_inspection,
)
from access_telemetry_c1_owner_custody import (
    OwnerRunCustodyError, _ArtifactIdentities, _compose_owner_run_files,
)
from access_telemetry_c1_owner_dispatch import (
    OwnerRunDispatchError, _canonical_path, _open_held_file,
)
from access_telemetry_c1_owner_run import OwnerRunAnchors, OwnerRunInspectionError, _now_ticks
from access_telemetry_c1_producer_bindings import SourceSnapshot
from access_telemetry_c1_retained_files import _compose_c115_retained_files


EVIDENCE_MANIFEST_SCHEMA = "hexalith.access-telemetry.c1.owner-retained-evidence/v1"
_JSON_FIELDS = ("registry", "capture", "statement", "commandContract", "reviewRolePolicy")
_MANIFEST_FIELDS = frozenset(("schemaVersion", "sources", *_JSON_FIELDS))
_REF_FIELDS = frozenset(("path", "sha256"))
_SOURCE_FIELDS = frozenset((
    "path sourceCommit executedBytes blobBytes normalization gitMode cleanFilter "
    "workingTreeEncoding ident"
).split())


class OwnerC115RetainedFilesError(ValueError):
    """Bounded content-free refusal after descriptor and child cleanup."""


def _refuse(code: str) -> NoReturn:
    raise OwnerC115RetainedFilesError("owner-c115-retained-" + code) from None


def _inventory(snapshot: JsonSnapshot, custody_root: str, reserved: set[str]):
    """Validate every closed descriptor and reference before following any ref."""
    manifest = _object(snapshot.value, _MANIFEST_FIELDS)
    if manifest["schemaVersion"] != EVIDENCE_MANIFEST_SCHEMA:
        _refuse("manifest-schema-mismatch")
    paths = set(reserved)

    def reference(value):
        value = _object(value, _REF_FIELDS)
        path = _canonical_path(value["path"])
        _digest(value["sha256"])
        if not path.startswith(custody_root + "/"):
            _refuse("artifact-outside-custody")
        if path in paths:
            _refuse("distinct-paths-required")
        paths.add(path)
        return path, value["sha256"]

    references = {name: reference(manifest[name]) for name in _JSON_FIELDS}
    # The capture's sidecar has one fixed manifest and ordered stream names.
    # Reserve its manifest now, before following any inventory reference.
    sidecar_manifest = references["capture"][0] + ".streams/manifest.json"
    if sidecar_manifest in paths:
        _refuse("distinct-paths-required")
    paths.add(sidecar_manifest)
    values = manifest["sources"]
    if type(values) is not tuple or len(values) != 19:
        _refuse("nineteen-source-descriptors-required")
    sources, source_paths = [], set()
    for value in values:
        value = _object(value, _SOURCE_FIELDS)
        # Reuse the complete SourceSnapshot shape and supported transform rules
        # without acquiring either of its byte forms yet.
        shape = SourceSnapshot(
            value["path"], value["sourceCommit"], b"", b"", value["normalization"],
            value["gitMode"], value["cleanFilter"], value["workingTreeEncoding"], value["ident"],
        )
        if shape.path in source_paths:
            _refuse("distinct-source-paths-required")
        source_paths.add(shape.path)
        sources.append((shape, reference(value["executedBytes"]), reference(value["blobBytes"])))
    # Inventory refs cannot masquerade as the fixed ordered sidecar artifacts.
    sidecar = references["capture"][0] + ".streams/"
    if any(path.startswith(sidecar) and path != sidecar_manifest for path in paths):
        _refuse("distinct-paths-required")
    return references, tuple(sources)


def corroborate_owner_c115_retained_files(
    plan_path: str,
    approved_plan_sha256: str,
    *,
    custody_root: str,
    repository_root: str,
    now: datetime,
) -> OwnerC115LocalSourceCorroboration:
    """Acquire approved private evidence, inspect once, then corroborate Git.

    Fresh owner ``observe`` authority and withdrawal checks precede evidence
    acquisition. The plan must bind the manifest's canonical path/exact SHA-256
    and the registry's exact SHA-256. All evidence, control and ancestor file
    descriptors survive inspection, isolated Git and exact readback; the outer
    owner checks finish with status last. Each artifact/control manifest is at
    most 1 MiB; run, five JSON materials, both source forms and every stream
    occurrence share an incremental 32 MiB budget. ``now`` is explicit aware
    UTC. Only an existing clean standalone checkout can be corroborated.
    """
    refusal = None
    try:
        _now_ticks(now)
        artifacts = _ArtifactIdentities()

        def composition(plan, run, withdrawn):
            value = plan.value
            if not {"evidenceManifestPath", "evidenceManifestSha256"} <= set(value):
                _refuse("manifest-binding-required")
            manifest_path = _canonical_path(value["evidenceManifestPath"])
            _digest(value["evidenceManifestSha256"])
            if "registrySha256" not in value:
                _refuse("registry-binding-required")
            _digest(value["registrySha256"])
            effective_uid = os.geteuid()
            with ExitStack() as descriptors:
                retained = []
                total = len(run.raw)

                def read(path, *, manifest=False):
                    nonlocal total
                    path = _canonical_path(path)
                    if not path.startswith(custody_root + "/"):
                        _refuse("artifact-outside-custody")
                    if path in artifacts.paths:
                        _refuse("distinct-paths-required")
                    held = _open_held_file(path, descriptors, custody_root=custody_root,
                                           effective_uid=effective_uid)
                    artifacts.claim(path, held)
                    size = held.before.st_size
                    if size > MAX_ARTIFACT_BYTES:
                        _refuse("artifact-byte-budget-exceeded")
                    if not manifest:
                        if total + size > MAX_AGGREGATE_BYTES:
                            _refuse("aggregate-byte-budget-exceeded")
                        total += size
                    raw = held.read()
                    retained.append((held, raw))
                    return held, raw

                _, raw = read(manifest_path, manifest=True)
                # Authenticate exact manifest bytes before parsing or following
                # any of its evidence paths, including the capture's sidecar.
                if hashlib.sha256(raw).hexdigest() != value["evidenceManifestSha256"]:
                    _refuse("manifest-digest-mismatch")
                references, descriptors_inventory = _inventory(
                    JsonSnapshot(raw), custody_root, artifacts.paths,
                )

                def acquire(reference):
                    path, digest = reference
                    _, raw = read(path)
                    if hashlib.sha256(raw).hexdigest() != digest:
                        _refuse("artifact-digest-mismatch")
                    return raw

                materials = {name: JsonSnapshot(acquire(references[name])) for name in _JSON_FIELDS
                             if name != "capture"}
                sources = tuple(SourceSnapshot(
                    shape.path, shape.source_commit, acquire(executed), acquire(blob),
                    shape.normalization, shape.git_mode, shape.clean_filter,
                    shape.working_tree_encoding, shape.ident,
                ) for shape, executed, blob in descriptors_inventory)
                anchors = OwnerRunAnchors(
                    value["recordSha256"], value["policySha256"], value["sessionId"],
                    value["sourceCommit"], value["targetSha256"], value["custodyRoot"],
                )

                def inspect_and_corroborate(capture, streams):
                    if capture.sha256 != references["capture"][1]:
                        _refuse("artifact-digest-mismatch")
                    inspection = inspect_owner_c115(
                        materials["registry"], capture, sources, streams, run, anchors,
                        materials["statement"], materials["commandContract"],
                        materials["reviewRolePolicy"], value["registrySha256"],
                        now=now, withdrawn=withdrawn,
                    )
                    return _corroborate_owner_inspection(inspection, repository_root)

                def stream_reader(path, *, manifest=False):
                    held, raw = read(path, manifest=manifest)
                    # Bind the packet digest before the stream seam parses it
                    # or follows its sidecar references.
                    if path == references["capture"][0] and hashlib.sha256(raw).hexdigest() != references["capture"][1]:
                        _refuse("artifact-digest-mismatch")
                    return held, raw

                result = _compose_c115_retained_files(
                    references["capture"][0], custody_root=custody_root,
                    inspection=inspect_and_corroborate, reader=stream_reader,
                )
                for held, original in retained:
                    os.lseek(held.descriptor, 0, os.SEEK_SET)
                    if held.read() != original:
                        _refuse("artifact-bytes-drifted")
                if os.geteuid() != effective_uid:
                    _refuse("effective-user-drifted")
                return result

        result = _compose_owner_run_files(
            plan_path, approved_plan_sha256, custody_root=custody_root,
            now=now, action="observe", inspection=composition, artifacts=artifacts,
        )
    except (OwnerRunCustodyError, OwnerRunDispatchError, OwnerRunInspectionError,
            OwnerC115InspectionError, InterchangeFormatError) as error:
        code = str(error)
        prefixes = ("owner-custody-", "owner-dispatch-", "owner-run-", "owner-c115-", "c115-files-")
        while any(code.startswith(prefix) for prefix in prefixes):
            code = next(code[len(prefix):] for prefix in prefixes if code.startswith(prefix))
        refusal = "owner-c115-retained-" + code
    except OwnerC115RetainedFilesError as error:
        refusal = str(error)
    except (OSError, ValueError, NotImplementedError, AttributeError, TypeError):
        refusal = "owner-c115-retained-unavailable"
    # Both descriptor stacks, all Git children and every exception handler have
    # finished before creating the public error. No partial result can escape.
    if refusal is not None:
        raise OwnerC115RetainedFilesError(refusal) from None
    return result
