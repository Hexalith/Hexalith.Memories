"""Offline owner inspection from fresh, privately held canonical files.

OS-user ownership proves local custody only, never GitHub authentication. The
outside approved digest still comes from retained owner approval. This wrapper
returns inspection only and has no process, network or execution dependency.
"""

from __future__ import annotations

from contextlib import ExitStack
from datetime import datetime
import os
from typing import NoReturn

from access_telemetry_c1_interchange import InterchangeFormatError, JsonSnapshot, _digest
from access_telemetry_c1_owner_dispatch import (
    OwnerRunDispatchError, _canonical_path, _dispatch_owner_run, _open_held_file, _validated_plan,
)
from access_telemetry_c1_owner_run import OwnerRunInspection, OwnerRunInspectionError


class OwnerRunCustodyError(ValueError):
    """Content-free file-custody refusal, without a partial inspection."""


def _refuse(code: str) -> NoReturn:
    raise OwnerRunCustodyError(code) from None


def dispatch_owner_run_files(
    plan_path: str,
    approved_plan_sha256: str,
    *,
    custody_root: str,
    now: datetime,
    action: str,
) -> OwnerRunInspection:
    """Read and inspect the approved plan's current record and withdrawal state.

    Paths must be canonical, distinct and strictly below the exact caller/plan
    custody root. Its three named artifacts and their directories at/below the
    root must be owned by the effective OS user with no group/other permissions;
    files must be regular and single-link. All file and ancestor descriptors
    survive inspection. Bounded exact-byte readback and named identity/metadata
    validation finish with status last before any result escapes.
    Nothing is cached across calls. ``now`` is explicit aware UTC caller data.
    """
    refusal = None
    inspection_refusal = None
    try:
        custody_root = _canonical_path(custody_root)
        plan_path = _canonical_path(plan_path)
        _digest(approved_plan_sha256)
        if not plan_path.startswith(custody_root + "/"):
            _refuse("owner-custody-plan-outside-custody")
        if not hasattr(os, "geteuid"):
            _refuse("owner-custody-filesystem-unavailable")
        effective_uid = os.geteuid()
        with ExitStack() as descriptors:
            held_plan = _open_held_file(plan_path, descriptors, custody_root=custody_root,
                                       effective_uid=effective_uid)
            plan_bytes = held_plan.read()
            plan = _validated_plan(plan_bytes, approved_plan_sha256).value
            if plan["custodyRoot"] != custody_root:
                _refuse("owner-custody-root-mismatch")
            if "recordPath" not in plan:
                _refuse("owner-custody-record-binding-required")
            record_path = _canonical_path(plan["recordPath"])
            status_path = plan["statusPath"]
            if not record_path.startswith(custody_root + "/"):
                _refuse("owner-custody-record-outside-custody")
            if len({plan_path, record_path, status_path}) != 3:
                _refuse("owner-custody-distinct-paths-required")
            held_record = _open_held_file(record_path, descriptors, custody_root=custody_root,
                                         effective_uid=effective_uid)
            record = JsonSnapshot(held_record.read())
            held_status = None
            status_bytes = None

            def read_status(path: str) -> bytes:
                nonlocal held_status, status_bytes
                held_status = _open_held_file(path, descriptors, custody_root=custody_root,
                                             effective_uid=effective_uid)
                files = (held_plan, held_record, held_status)
                if len({(held.before.st_dev, held.before.st_ino) for held in files}) != 3:
                    _refuse("owner-custody-distinct-files-required")
                status_bytes = held_status.read()
                return status_bytes

            result = _dispatch_owner_run(record, plan_bytes, approved_plan_sha256,
                                         now=now, action=action, status_reader=read_status)
            # Metadata can repeat on a coarse-timestamp filesystem. Compare exact
            # bounded bytes too, and finish with withdrawal state after the other
            # artifacts' final checks. Each read revalidates its full named chain.
            for held, original in ((held_plan, plan_bytes), (held_record, record.raw),
                                   (held_status, status_bytes)):
                os.lseek(held.descriptor, 0, os.SEEK_SET)
                if held.read() != original:
                    _refuse("owner-custody-artifact-bytes-drifted")
            if os.geteuid() != effective_uid:
                _refuse("owner-custody-effective-user-drifted")
    except OwnerRunInspectionError as error:
        inspection_refusal = str(error)
    except OwnerRunCustodyError as error:
        refusal = str(error)
    except OwnerRunDispatchError as error:
        refusal = str(error).replace("owner-dispatch-", "owner-custody-", 1)
    except InterchangeFormatError as error:
        refusal = "owner-custody-" + str(error)
    except (OSError, NotImplementedError, TypeError, AttributeError):
        refusal = "owner-custody-filesystem-unavailable"
    # Normalize only after both descriptor cleanup and exception handlers finish;
    # suppressing display alone would leave untrusted internal exception data.
    if refusal is not None:
        _refuse(refusal)
    if inspection_refusal is not None:
        raise OwnerRunInspectionError(inspection_refusal) from None
    return result
