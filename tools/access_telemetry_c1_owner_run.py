"""Pure inspection of the owner-approved non-Production PG2 run shape.

The caller supplies retained-plan anchors and freshly read withdrawal state.
Matching those assertions is not independent authentication, accepted C1
evidence, or permission to launch a target. Existing consumers remain unchanged.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from typing import NoReturn

from access_telemetry_c1_approval_policy import OWNER_GITHUB_PRINCIPAL
from access_telemetry_c1_interchange import (
    InterchangeFormatError, JsonSnapshot, MAX_PATH_CHARACTERS,
    _SESSION, _SESSION_SECRET, _array, _commit, _digest, _instant,
    _literal, _object, _text,
)


_SCHEMA = "hexalith.access-telemetry.c1.owner-run/v1"
_POLICY = "sole-owner-qualification/2026-10-12"
_ASSURANCE = "sole-owner/self-reviewed"
_PROFILE = "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe"
_WORKLOAD = "71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f"
_FIELDS = frozenset((
    "schemaVersion policyId policySha256 ownerPrincipal assuranceMode environment "
    "profileId profileSha256 workloadSha256 sessionId sourceCommit targetSha256 "
    "custodyRoot actions notBeforeUtc collectionEndsAtUtc reviewEndsAtUtc "
    "expiresAtUtc status"
).split())
_COLLECTION_ACTIONS = frozenset({"observe", "fault", "purge", "reclaim"})
_LATER_ACTIONS = frozenset({"close-out", "publish"})
_ACTIONS = _COLLECTION_ACTIONS | _LATER_ACTIONS
_TICKS_PER_SECOND = 10_000_000


class OwnerRunInspectionError(ValueError):
    """A content-free refusal; no partial inspection or authority is returned."""


def _refuse(code: str) -> NoReturn:
    raise OwnerRunInspectionError(code) from None


@dataclass(frozen=True, slots=True)
class OwnerRunAnchors:
    """Outside-record assertions from the retained owner-approved run plan.

    Construction authenticates nothing. Inspection checks every field's closed
    type and spelling before comparing it with recomputed record bytes.
    """

    record_sha256: str
    policy_sha256: str
    session_id: str
    source_commit: str
    target_sha256: str
    custody_root: str


@dataclass(frozen=True, slots=True)
class OwnerRunInspection:
    """Exact inspected bytes and disclosed self-review, never an execution handle.

    The observation is specific to its action and supplied instant. The caller
    must re-read the record and withdrawal state before any later use.
    """

    snapshot: JsonSnapshot = field(repr=False)
    requested_action: str
    inspected_at_utc: datetime
    owner_principal: str
    assurance_mode: str

    @property
    def record_sha256(self) -> str:
        """SHA-256 recomputed from the exact retained raw bytes."""
        return self.snapshot.sha256


def _session(value: object) -> None:
    _text(value, limit=128)
    if _SESSION.fullmatch(value) is None or _SESSION_SECRET.search(value):
        _refuse("owner-run-session-invalid")


def _custody(value: object) -> None:
    _text(value, limit=MAX_PATH_CHARACTERS)
    if (not value.startswith("/") or value == "/" or "\\" in value
            or any(part in ("", ".", "..") for part in value[1:].split("/"))):
        _refuse("owner-run-custody-not-canonical")


def _now_ticks(now: datetime) -> int:
    if type(now) is not datetime or type(now.tzinfo) is not timezone:
        _refuse("owner-run-aware-utc-required")
    if now.utcoffset() != timedelta(0):
        _refuse("owner-run-aware-utc-required")
    return ((now.toordinal() * 86400 + now.hour * 3600 + now.minute * 60
             + now.second) * _TICKS_PER_SECOND + now.microsecond * 10)


def inspect_owner_run(
    snapshot: JsonSnapshot,
    anchors: OwnerRunAnchors,
    *,
    now: datetime,
    withdrawn: bool,
    action: str,
) -> OwnerRunInspection:
    """Inspect one exact owner record without I/O or deployed admission.

    ``anchors`` must come from outside the untrusted record. ``now`` and
    ``withdrawn`` are explicit current caller facts, not inferred authority or
    lifecycle clock evidence. Each action is checked independently.
    """
    if type(snapshot) is not JsonSnapshot or type(anchors) is not OwnerRunAnchors:
        _refuse("owner-run-snapshot-and-anchors-required")
    if type(withdrawn) is not bool:
        _refuse("owner-run-current-status-unavailable")
    if withdrawn:
        _refuse("owner-run-withdrawn")
    current = _now_ticks(now)
    try:
        _text(action)
        if action not in _ACTIONS:
            _refuse("owner-run-action-unknown")
        for digest in (anchors.record_sha256, anchors.policy_sha256, anchors.target_sha256):
            _digest(digest)
        _session(anchors.session_id)
        _commit(anchors.source_commit)
        _custody(anchors.custody_root)

        # Reparse raw bytes; do not trust cached fields of a caller-made snapshot.
        retained = JsonSnapshot(snapshot.raw)
        if retained.sha256 != anchors.record_sha256:
            _refuse("owner-run-record-digest-mismatch")
        value = _object(retained.value, _FIELDS)
        for name, expected in (
            ("schemaVersion", _SCHEMA), ("policyId", _POLICY),
            ("ownerPrincipal", OWNER_GITHUB_PRINCIPAL),
            ("assuranceMode", _ASSURANCE), ("environment", "non-production"),
            ("profileId", "PG-ONPREM-2"), ("profileSha256", _PROFILE),
            ("workloadSha256", _WORKLOAD), ("status", "approved"),
        ):
            _text(value[name])
            _literal(value[name], expected)
        for name, expected in (
            ("policySha256", anchors.policy_sha256),
            ("targetSha256", anchors.target_sha256),
        ):
            _digest(value[name])
            if value[name] != expected:
                _refuse("owner-run-anchor-mismatch")
        _session(value["sessionId"])
        _commit(value["sourceCommit"])
        _custody(value["custodyRoot"])
        for name, expected in (
            ("sessionId", anchors.session_id), ("sourceCommit", anchors.source_commit),
            ("custodyRoot", anchors.custody_root),
        ):
            if value[name] != expected:
                _refuse("owner-run-anchor-mismatch")

        actions = _array(value["actions"], nonempty=True)
        if len(actions) > len(_ACTIONS):
            _refuse("owner-run-actions-invalid")
        for declared in actions:
            _text(declared)
            if declared not in _ACTIONS:
                _refuse("owner-run-actions-invalid")
        if len(set(actions)) != len(actions):
            _refuse("owner-run-actions-invalid")
        if action not in actions:
            _refuse("owner-run-action-not-included")

        start, collection, review, expiry = (
            _instant(value[name], spelling="new") for name in
            ("notBeforeUtc", "collectionEndsAtUtc", "reviewEndsAtUtc", "expiresAtUtc")
        )
        if (not 0 < collection - start <= 72 * 3600 * _TICKS_PER_SECOND
                or not 0 < review - collection <= 24 * 3600 * _TICKS_PER_SECOND
                or expiry < review or expiry - start > 7 * 86400 * _TICKS_PER_SECOND):
            _refuse("owner-run-window-invalid")
        if current < start or current >= min(review, expiry):
            _refuse("owner-run-outside-use-window")
        if ((action in _COLLECTION_ACTIONS and current >= collection)
                or (action in _LATER_ACTIONS and current < collection)):
            _refuse("owner-run-action-outside-phase")
    except InterchangeFormatError as error:
        _refuse("owner-run-" + str(error))
    return OwnerRunInspection(retained, action, now.replace(tzinfo=timezone.utc, fold=0),
                              OWNER_GITHUB_PRINCIPAL, _ASSURANCE)
