"""Owner-bound C1.15 evidence from one explicit clean local Git checkout.

Matching local bytes establish no execution, custody, registration or acceptance.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime

from access_telemetry_c1_capture_streams import C115CommandStreams
from access_telemetry_c1_interchange import InterchangeFormatError, JsonSnapshot
from access_telemetry_c1_owner_c115 import (
    OwnerC115Inspection, OwnerC115InspectionError, inspect_owner_c115,
)
from access_telemetry_c1_owner_c115_plan_files import (
    OwnerC115PlanFilesError, _compose_owner_c115_plan_files,
)
from access_telemetry_c1_owner_run import OwnerRunAnchors
from access_telemetry_c1_producer_bindings import SourceSnapshot
from access_telemetry_c1_source_provenance import _corroborate_inspected_sources


class OwnerC115LocalSourceCorroborationError(ValueError):
    """Bounded content-free refusal with no cause, context or partial result."""


class OwnerC115PlanSourceCorroborationError(ValueError):
    """Content-free current-plan refusal after file and child cleanup."""


@dataclass(frozen=True, slots=True)
class OwnerC115LocalSourceCorroboration:
    """Point-in-time local source identities with explicit owner self-review."""

    inspection: OwnerC115Inspection = field(repr=False)
    source_commit: str
    repository_root: str
    paths: tuple[str, ...]
    git_blob_oids: tuple[str, ...]
    owner_principal: str
    assurance_mode: str


def corroborate_owner_c115_sources(
    registry: JsonSnapshot,
    capture: JsonSnapshot,
    sources: tuple[SourceSnapshot, ...],
    streams: tuple[C115CommandStreams, ...],
    run: JsonSnapshot,
    anchors: OwnerRunAnchors,
    statement: JsonSnapshot,
    command_contract: JsonSnapshot,
    review_role_policy: JsonSnapshot,
    approved_registry_sha256: str,
    repository_root: str,
    *,
    now: datetime,
    withdrawn: bool,
) -> OwnerC115LocalSourceCorroboration:
    """Reinspect every owner input before reading a standalone local checkout.

    ``repository_root`` is an explicit canonical absolute path with a real
    ``.git`` directory. The bounded owner safety preflight precedes each status
    check in the shared isolated local Git lane. Owner Git children require the
    supported Linux seccomp runtime and refuse if enforcement cannot install.
    The concealed inspection retains the exact checked bytes and detached source
    records. Current time, withdrawal status and approval anchors are supplied by
    the caller; no acceptance verdict or execution authority is returned.
    """
    refusal = None
    try:
        inspection = inspect_owner_c115(
            registry, capture, sources, streams, run, anchors,
            statement, command_contract, review_role_policy, approved_registry_sha256,
            now=now, withdrawn=withdrawn,
        )
        result = _corroborate_owner_inspection(inspection, repository_root)
    except OwnerC115InspectionError as error:
        refusal = str(error)
    except InterchangeFormatError as error:
        refusal = "owner-c115-" + str(error)
    except (OSError, ValueError, AttributeError):
        refusal = "owner-c115-local-source-unavailable"
    # Raise after leaving handlers: decoder and OS exceptions may retain raw
    # candidate bytes, local paths or process details in their original context.
    if refusal is not None:
        raise OwnerC115LocalSourceCorroborationError(refusal) from None
    return result


def _corroborate_owner_inspection(
    inspection: OwnerC115Inspection,
    repository_root: str,
) -> OwnerC115LocalSourceCorroboration:
    """Corroborate only checked detached sources using the enforced owner lane."""
    source = inspection.registration.source
    identities = _corroborate_inspected_sources(source, inspection.sources, repository_root,
                                               owner_safety_preflight=True)
    return OwnerC115LocalSourceCorroboration(
        inspection, source.source_commit, repository_root,
        tuple(item.path for item in source.sources), identities,
        inspection.owner_principal, inspection.assurance_mode,
    )


def corroborate_owner_c115_sources_from_plan_files(
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
    repository_root: str,
    now: datetime,
) -> OwnerC115LocalSourceCorroboration:
    """Corroborate local sources while current approved-plan files stay held.

    Run, anchors, raw registry digest and withdrawal come only from fresh held
    files bound to the outside approved plan digest. Exactly one complete C1.15
    inspection precedes the existing bounded owner Git lane. Plan/run/status
    and ancestor descriptors survive local corroboration and exact status-last
    readback; no computed result escapes before cleanup completes. ``now`` is
    explicit aware UTC. Only an existing clean standalone checkout is supported.
    Matching bytes convey no execution, target authority, complete evidence
    custody, simultaneous snapshot, continuing lock or gate acceptance.
    """
    refusal = None
    try:
        result = _compose_owner_c115_plan_files(
            plan_path, approved_plan_sha256, custody_root=custody_root,
            registry=registry, capture=capture, sources=sources, streams=streams,
            statement=statement, command_contract=command_contract,
            review_role_policy=review_role_policy, now=now,
            checked_inspection=lambda inspection: _corroborate_owner_inspection(inspection, repository_root),
        )
    except (OwnerC115PlanFilesError, OwnerC115InspectionError) as error:
        code = str(error)
        for prefix in ("owner-c115-plan-files-", "owner-c115-"):
            if code.startswith(prefix):
                code = code[len(prefix):]
                break
        refusal = "owner-c115-plan-source-" + code
    except InterchangeFormatError as error:
        refusal = "owner-c115-plan-source-" + str(error)
    except (OSError, ValueError, AttributeError, TypeError):
        refusal = "owner-c115-plan-source-local-source-unavailable"
    # Leave both descriptor/child cleanup and exception handlers before raising;
    # untrusted exceptions can otherwise retain candidate bytes or process data.
    if refusal is not None:
        raise OwnerC115PlanSourceCorroborationError(refusal) from None
    return result
