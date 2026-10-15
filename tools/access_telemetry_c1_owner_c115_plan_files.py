"""Offline C1.15 inspection within approved plan/run/status file custody.

Only the three acquired private files receive held-descriptor checks. Supplied
evidence receives exact-byte consistency checks, never execution or acceptance.
"""

from __future__ import annotations

from dataclasses import replace
from datetime import datetime
from typing import NoReturn

from access_telemetry_c1_capture_streams import C115CommandStreams
from access_telemetry_c1_interchange import InterchangeFormatError, JsonSnapshot, _digest
from access_telemetry_c1_owner_c115 import (
    OwnerC115Inspection, OwnerC115InspectionError, _budget, inspect_owner_c115,
)
from access_telemetry_c1_owner_custody import OwnerRunCustodyError, _compose_owner_run_files
from access_telemetry_c1_owner_run import OwnerRunAnchors, OwnerRunInspectionError, _now_ticks
from access_telemetry_c1_producer_bindings import SourceSnapshot


class OwnerC115PlanFilesError(ValueError):
    """Bounded content-free refusal after cleanup, without a partial result."""


def _refuse(code: str) -> NoReturn:
    raise OwnerC115PlanFilesError(code) from None


def inspect_owner_c115_from_plan_files(
    plan_path: str,
    approved_plan_sha256: str,
    *,
    custody_root: str,
    registry: JsonSnapshot,
    capture: JsonSnapshot,
    sources: tuple[SourceSnapshot, ...],
    streams: tuple[C115CommandStreams, ...],
    statement: JsonSnapshot,
    command_contract: JsonSnapshot,
    review_role_policy: JsonSnapshot,
    now: datetime,
) -> OwnerC115Inspection:
    """Inspect supplied evidence using only current outside-approved plan anchors.

    Preflight occurrence budgets and detach supplied records/snapshots before
    any filesystem callback. Acquire the private plan, its record and its closed
    status in that order; hold all descriptors through full ``observe`` C1.15
    inspection and bounded exact readback, ending with status. The acquired run
    counts in the existing C1.15 budget. No result conveys continuing custody,
    a simultaneous snapshot, target authority or complete evidence custody.
    ``now`` is caller-supplied aware UTC; no ambient clock or Git is consulted.
    """
    return _compose_owner_c115_plan_files(
        plan_path, approved_plan_sha256, custody_root=custody_root,
        registry=registry, capture=capture, sources=sources, streams=streams,
        statement=statement, command_contract=command_contract,
        review_role_policy=review_role_policy, now=now,
    )


def _compose_owner_c115_plan_files(
    plan_path: str,
    approved_plan_sha256: str,
    *,
    custody_root: str,
    registry: JsonSnapshot,
    capture: JsonSnapshot,
    sources: tuple[SourceSnapshot, ...],
    streams: tuple[C115CommandStreams, ...],
    statement: JsonSnapshot,
    command_contract: JsonSnapshot,
    review_role_policy: JsonSnapshot,
    now: datetime,
    checked_inspection=None,
):
    """Continue after full inspection, within held custody and final readback.

    The private callback receives only the completed detached inspection. Its
    result cannot escape until the existing status-last readback and cleanup.
    """
    refusal = None
    try:
        supplied = (registry, capture, statement, command_contract, review_role_policy)
        _budget(supplied, sources, streams)
        _now_ticks(now)
        sources = tuple(replace(source) for source in sources)
        streams = tuple(replace(stream) for stream in streams)
        registry, capture, statement, command_contract, review_role_policy = (
            JsonSnapshot(item.raw) for item in supplied
        )

        def inspection(plan: JsonSnapshot, run: JsonSnapshot, withdrawn: bool):
            value = plan.value
            if "registrySha256" not in value:
                _refuse("owner-c115-plan-files-registry-binding-required")
            _digest(value["registrySha256"])
            anchors = OwnerRunAnchors(
                value["recordSha256"], value["policySha256"], value["sessionId"],
                value["sourceCommit"], value["targetSha256"], value["custodyRoot"],
            )
            inspected = inspect_owner_c115(
                registry, capture, sources, streams, run, anchors, statement,
                command_contract, review_role_policy, value["registrySha256"],
                now=now, withdrawn=withdrawn,
            )
            return inspected if checked_inspection is None else checked_inspection(inspected)

        result = _compose_owner_run_files(
            plan_path, approved_plan_sha256, custody_root=custody_root,
            now=now, action="observe", inspection=inspection,
        )
    except (OwnerC115InspectionError, OwnerRunCustodyError, OwnerRunInspectionError) as error:
        code = str(error)
        for prefix in ("owner-c115-", "owner-custody-", "owner-run-"):
            if code.startswith(prefix):
                code = code[len(prefix):]
                break
        refusal = "owner-c115-plan-files-" + code
    except InterchangeFormatError as error:
        refusal = "owner-c115-plan-files-" + str(error)
    except OwnerC115PlanFilesError as error:
        refusal = str(error)
    except (AttributeError, TypeError):
        refusal = "owner-c115-plan-files-required-field-missing"
    # Raise after the held stack and all handlers have finished. No untrusted
    # decoder/OS exception survives in the cause/context chain.
    if refusal is not None:
        _refuse(refusal)
    return result
