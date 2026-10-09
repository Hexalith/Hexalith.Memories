"""Terminal offline boundary for a proposed C1 gate disposition.

Exact bytes and matching labels are necessary inputs, but no operational P1-P4
authority policy or receipt provider has been approved. This module cannot
produce an accepted gate, grant, or reusable authorization result.
"""

from __future__ import annotations

from collections.abc import Mapping
from typing import NoReturn

from access_telemetry_c1_interchange import (
    EvidenceReference, InterchangeFormatError, JsonSnapshot, authenticate_snapshot,
    parse_capture, parse_disposition, parse_ref, _instant,
)

_SCOPE_FIELDS = frozenset({"profileSha256", "workloadSha256", "targetSha256",
                           "qualificationSessionId", "sourceCommit"})
_PROFILE_SHA256 = "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe"
_WORKLOAD_SHA256 = "71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f"


class C1AuthorityUnavailableError(ValueError):
    """Content-free refusal from the unavailable deployed authority boundary."""


def _refuse(code: str) -> NoReturn:
    raise C1AuthorityUnavailableError(code) from None


def require_authenticated_disposition(
    *, disposition: JsonSnapshot, capture: JsonSnapshot, capture_reference: EvidenceReference,
    scope: Mapping[str, str],
) -> NoReturn:
    """Check asserted linkage, then refuse until P1-P4 approve actual authority.

    ``scope`` and ``capture`` are caller inputs, not authenticated target or
    execution facts. A parsed receipt URI/hash, reviewer label, or fixture
    GitHub observation must never be converted to a deployed authorization.
    """
    if (type(disposition) is not JsonSnapshot or type(capture) is not JsonSnapshot
            or type(capture_reference) is not EvidenceReference or type(scope) is not dict
            or set(scope) != _SCOPE_FIELDS or any(type(item) is not str for item in scope.values())):
        _refuse("authority-boundary-input-invalid")
    if scope["profileSha256"] != _PROFILE_SHA256 or scope["workloadSha256"] != _WORKLOAD_SHA256:
        _refuse("authority-current-pg2-identity-required")
    try:
        authenticate_snapshot(capture_reference, capture)
        capture_value = parse_capture(capture).value
    except InterchangeFormatError as error:
        _refuse("capture-" + str(error))
    try:
        value = parse_disposition(disposition).value
        asserted_capture = parse_ref(value["capture"])
    except InterchangeFormatError as error:
        _refuse("disposition-" + str(error))
    if (capture_value["producerStatus"] != "observed" or capture_value["failureCount"] != 0
            or capture_value["skipCount"] != 0 or capture_value["sourceDisposition"] != "clean"
            or capture_value["worktreeDirty"] or capture_value["finalSourceRecheck"] != "unchanged"):
        _refuse("authority-capture-not-eligible")
    if asserted_capture != capture_reference or value["gate"] != capture_value["gate"]:
        _refuse("authority-capture-or-gate-mismatch")
    expected = {
        "profileId": "PG-ONPREM-2",
        "profileSha256": scope["profileSha256"],
        "workloadSha256": scope["workloadSha256"],
        "qualificationSessionId": scope["qualificationSessionId"],
        "targetSha256": scope["targetSha256"],
    }
    if (any(value[key] != expected_value or capture_value[key] != expected_value
            for key, expected_value in expected.items())
            or capture_value["sourceCommit"] != scope["sourceCommit"]):
        _refuse("authority-scope-mismatch")
    if value["decision"] != "accepted":
        _refuse("authority-decision-not-accepted")
    if _instant(value["decidedAtUtc"]) <= _instant(
            capture_value["producer"]["finishedAtUtc"], spelling="capture"):
        _refuse("authority-review-before-capture-finish")
    # P1 lacks a selected receipt verifier/trust root; P2-P4 lack operational
    # time, session, and role policy. No adapter is invoked from this boundary.
    _refuse("authority-prerequisites-unapproved")
