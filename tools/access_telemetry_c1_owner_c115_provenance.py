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
from access_telemetry_c1_owner_run import OwnerRunAnchors
from access_telemetry_c1_producer_bindings import SourceSnapshot
from access_telemetry_c1_source_provenance import _corroborate_inspected_sources


class OwnerC115LocalSourceCorroborationError(ValueError):
    """Bounded content-free refusal with no cause, context or partial result."""


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
        source = inspection.registration.source
        identities = _corroborate_inspected_sources(source, inspection.sources, repository_root,
                                                   owner_safety_preflight=True)
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
    return OwnerC115LocalSourceCorroboration(
        inspection, source.source_commit, repository_root,
        tuple(item.path for item in source.sources), identities,
        inspection.owner_principal, inspection.assurance_mode,
    )
