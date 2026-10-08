"""Read-only C1 bundle review observations; never qualification authorization.

GitHub authenticates the reviewer. Independently authenticated current policy,
manifest context, producer identities and UTC time must be supplied by the caller.
Session, custody, capture authentication and gate acceptance remain external.
"""

from __future__ import annotations

from collections.abc import Mapping
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from pathlib import Path
import importlib
import re
import sys


# Require the pinned workspace file even if another copy was cached or installed.
_PLATFORM_SOURCE = (Path(__file__).resolve().parents[1]
                    / "references/Hexalith.Platform/eng/hexalith_github_reviews.py").resolve()
if not _PLATFORM_SOURCE.is_file():
    raise ModuleNotFoundError("pinned-hexalith-github-review-source-required") from None
sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(_PLATFORM_SOURCE.parent))
_platform = importlib.import_module("hexalith_github_reviews")
if (getattr(_platform, "__file__", None) is None
        or Path(_platform.__file__).resolve() != _PLATFORM_SOURCE
        or getattr(getattr(_platform, "__spec__", None), "origin", None) != str(_PLATFORM_SOURCE)):
    raise ImportError("pinned-hexalith-github-review-origin-required") from None
from hexalith_github_reviews import (  # noqa: E402
    GitHubReviewClient, GitHubReviewRetrievalError, RequestLimits, ReviewLocator, _elapsed, _finite_number,
)
from access_telemetry_c1_approval_policy import (  # noqa: E402
    BundlePrincipalPolicyError, validate_bundle_principal_separation,
)
from access_telemetry_c1_interchange import (  # noqa: E402
    EvidenceReference, InterchangeFormatError, JsonSnapshot, authenticate_snapshot, parse_ref,
)


DECISION_SCHEMA = "hexalith.access-telemetry.c1.github-bundle-decision/v1"
_FIELDS = frozenset({"schema", "role", "decision", "manifest", "profileSha256", "workloadSha256",
                     "targetSha256", "sessionId", "sourceCommit", "policySha256", "expiresAtUtc"})
_DIGEST = re.compile(r"[0-9a-f]{64}\Z")
_COMMIT = re.compile(r"[0-9a-f]{40}\Z")
_PRINCIPAL = re.compile(r"github:user:([1-9][0-9]{0,18})\Z")
_SESSION = re.compile(r"[A-Za-z0-9][A-Za-z0-9._:-]{0,127}\Z")
_UTC = re.compile(r"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z\Z")


class GitHubApprovalError(ValueError):
    """A closed, content-free refusal of an untrusted or unsupported review."""


def _refuse(code: str) -> None:
    raise GitHubApprovalError(code) from None


def _principal(value: str) -> None:
    match = _PRINCIPAL.fullmatch(value) if type(value) is str else None
    if match is None or int(match.group(1)) > 2**63 - 1:
        _refuse("review-principal-invalid")


def _digest(value: str) -> None:
    if type(value) is not str or _DIGEST.fullmatch(value) is None:
        _refuse("review-context-digest-invalid")


def _trusted_utc(value: datetime) -> None:
    if type(value) is not datetime or value.tzinfo is None or value.utcoffset() != timedelta(0):
        _refuse("trusted-utc-time-required")


def _context_elapsed() -> float:
    """Use the same suspend-aware clock as transport, with no unsafe fallback."""
    try:
        elapsed = _elapsed()
    except GitHubReviewRetrievalError:
        _refuse("trusted-time-acquisition-invalid")
    if not _finite_number(elapsed) or elapsed < 0:
        _refuse("trusted-time-acquisition-invalid")
    return elapsed


def _wire_utc(value: str) -> datetime:
    if type(value) is not str or _UTC.fullmatch(value) is None:
        _refuse("review-utc-time-not-canonical")
    try:
        return datetime.strptime(value, "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)
    except ValueError:
        _refuse("review-utc-time-invalid")


@dataclass(frozen=True, slots=True)
class CurrentRolePolicy:
    """Caller-authenticated current role grants for one exact scope and policy.

    Construction does not authenticate or adopt a policy. Both role allowlists
    and all numerical limits are required; GitHub author association is ignored.
    """

    policy_sha256: str
    profile_sha256: str
    workload_sha256: str
    target_sha256: str
    session_id: str
    operations_principals: frozenset[str]
    security_principals: frozenset[str]
    max_status_age_seconds: float
    max_approval_lifetime_seconds: float
    max_review_lag_seconds: float

    def __post_init__(self) -> None:
        for digest in (self.policy_sha256, self.profile_sha256, self.workload_sha256, self.target_sha256):
            _digest(digest)
        if type(self.session_id) is not str or _SESSION.fullmatch(self.session_id) is None:
            _refuse("review-session-invalid")
        for principals in (self.operations_principals, self.security_principals):
            if type(principals) is not frozenset or not 1 <= len(principals) <= 1000:
                _refuse("current-role-allowlists-required")
            for principal in principals:
                _principal(principal)
        for limit in (self.max_status_age_seconds, self.max_approval_lifetime_seconds, self.max_review_lag_seconds):
            if not _finite_number(limit) or limit <= 0:
                _refuse("review-policy-limits-required")


@dataclass(frozen=True, slots=True)
class ApprovalContext:
    """Exact trusted manifest and scope supplied by an external authority adapter.

    Manifest creation time is separately authenticated because the proposed
    manifest wire schema contains no creation timestamp. This library matches
    its exact bytes, but does not admit its gates or authenticate its custody.
    """

    manifest: EvidenceReference
    manifest_bytes: bytes = field(repr=False)
    manifest_created_at_utc: datetime
    profile_sha256: str
    workload_sha256: str
    target_sha256: str
    session_id: str
    source_commit: str
    reviewed_commit: str
    policy: CurrentRolePolicy
    producer_principals: frozenset[str]
    trusted_now_utc: datetime
    expected_issuer: str
    request_limits: RequestLimits
    _time_acquired_monotonic: float = field(default_factory=_context_elapsed, repr=False, compare=False)

    def __post_init__(self) -> None:
        for digest in (self.profile_sha256, self.workload_sha256, self.target_sha256):
            _digest(digest)
        if type(self.session_id) is not str or _SESSION.fullmatch(self.session_id) is None:
            _refuse("review-session-invalid")
        for commit in (self.source_commit, self.reviewed_commit):
            if type(commit) is not str or _COMMIT.fullmatch(commit) is None:
                _refuse("review-commit-invalid")
        _trusted_utc(self.trusted_now_utc)
        _trusted_utc(self.manifest_created_at_utc)
        if (not _finite_number(self._time_acquired_monotonic)
                or not 0 <= self._time_acquired_monotonic <= _context_elapsed()):
            _refuse("trusted-time-acquisition-invalid")
        if self.manifest_created_at_utc > self.trusted_now_utc:
            _refuse("manifest-time-in-future")
        if type(self.policy) is not CurrentRolePolicy or type(self.request_limits) is not RequestLimits:
            _refuse("review-policy-and-request-limits-required")
        if (self.profile_sha256, self.workload_sha256, self.target_sha256, self.session_id) != (
                self.policy.profile_sha256, self.policy.workload_sha256, self.policy.target_sha256, self.policy.session_id):
            _refuse("current-role-policy-wrong-scope")
        if type(self.expected_issuer) is not str or not self.expected_issuer or len(self.expected_issuer) > 128:
            _refuse("trusted-review-issuer-required")
        try:
            authenticate_snapshot(self.manifest, JsonSnapshot(self.manifest_bytes))
        except InterchangeFormatError as error:
            _refuse("manifest-" + str(error))


@dataclass(frozen=True, slots=True)
class ReviewBinding:
    """Retained immutable role-decision bytes, expected reviewer and review ID."""

    locator: ReviewLocator
    expected_principal: str
    body_reference: EvidenceReference
    retained_body: bytes = field(repr=False)

    def __post_init__(self) -> None:
        if type(self.locator) is not ReviewLocator:
            _refuse("review-locator-required")
        _principal(self.expected_principal)
        try:
            authenticate_snapshot(self.body_reference, JsonSnapshot(self.retained_body))
        except InterchangeFormatError as error:
            _refuse("decision-" + str(error))


@dataclass(frozen=True, slots=True)
class ReviewObservation:
    """Checked facts for this call only; no credential, role grant or action handle."""

    issuer: str
    role: str
    reviewer_principal: str
    locator: ReviewLocator
    manifest: EvidenceReference
    profile_sha256: str
    workload_sha256: str
    target_sha256: str
    session_id: str
    source_commit: str
    reviewed_commit: str
    policy_sha256: str
    response_sha256: str
    body_sha256: str
    submitted_at_utc: datetime
    expires_at_utc: datetime
    status_request_started_at_utc: datetime
    checked_at_utc: datetime


def _decision(snapshot: JsonSnapshot, role: str, context: ApprovalContext) -> datetime:
    body = snapshot.value
    if not isinstance(body, Mapping) or set(body) != _FIELDS:
        _refuse("decision-field-set")
    if body["schema"] != DECISION_SCHEMA or body["role"] != role or body["decision"] != "approve":
        _refuse("decision-schema-role-or-state-invalid")
    try:
        if parse_ref(body["manifest"]) != context.manifest:
            _refuse("decision-manifest-mismatch")
    except InterchangeFormatError as error:
        _refuse("decision-" + str(error))
    expected = {"profileSha256": context.profile_sha256, "workloadSha256": context.workload_sha256,
                "targetSha256": context.target_sha256, "sessionId": context.session_id,
                "sourceCommit": context.source_commit, "policySha256": context.policy.policy_sha256}
    if any(type(body[key]) is not str or body[key] != value for key, value in expected.items()):
        _refuse("decision-scope-mismatch")
    return _wire_utc(body["expiresAtUtc"])


def consume_bundle_reviews(*, client: GitHubReviewClient, token: str, context: ApprovalContext,
                           operations: ReviewBinding, security: ReviewBinding) -> tuple[ReviewObservation, ReviewObservation]:
    """Re-fetch and check both distinct decisions; every refusal returns no pair.

    Trusted UTC advances from context acquisition by monotonic elapsed time, so
    context reuse and request latency cannot extend expiry or status freshness.
    Every invocation retrieves both reviews;
    no cached or offline observation can be supplied as an acceptance input.
    """
    if type(client) is not GitHubReviewClient or type(context) is not ApprovalContext:
        _refuse("trusted-review-client-and-context-required")
    if type(operations) is not ReviewBinding or type(security) is not ReviewBinding:
        _refuse("two-review-bindings-required")
    if client.issuer != context.expected_issuer:
        _refuse("review-issuer-mismatch")
    first, second = operations.locator, security.locator
    if ((first.owner, first.repository, first.pull_number) != (second.owner, second.repository, second.pull_number)
            or first.review_id == second.review_id):
        _refuse("two-distinct-reviews-on-one-pr-required")
    if (operations.expected_principal not in context.policy.operations_principals
            or security.expected_principal not in context.policy.security_principals):
        _refuse("current-review-role-denied")
    try:
        validate_bundle_principal_separation(operations.expected_principal, security.expected_principal,
                                            context.producer_principals)
    except BundlePrincipalPolicyError as error:
        _refuse(str(error))
    bindings = (("operations", operations), ("security", security))
    for role, binding in bindings:
        _decision(JsonSnapshot(binding.retained_body), role, context)
    fetched = []
    last_elapsed = context._time_acquired_monotonic
    try:
        for role, binding in bindings:
            request_started = _context_elapsed()
            if request_started < last_elapsed:
                _refuse("trusted-time-acquisition-invalid")
            last_elapsed = request_started
            raw = client.fetch_review(binding.locator, token=token, limits=context.request_limits)
            response = JsonSnapshot(raw)
            data = response.value
            if not isinstance(data, Mapping):
                _refuse("review-response-object-required")
            if (type(data.get("id")) is not int or data["id"] != binding.locator.review_id
                    or data.get("pull_request_url") != client.api_origin + binding.locator.pull_path
                    or data.get("commit_id") != context.reviewed_commit):
                _refuse("review-resource-or-commit-mismatch")
            user = data.get("user")
            if (not isinstance(user, Mapping) or user.get("type") != "User"
                    or type(user.get("id")) is not int or not 0 < user["id"] <= 2**63 - 1):
                _refuse("review-user-identity-invalid")
            principal = f"github:user:{user['id']}"
            if principal != binding.expected_principal:
                _refuse("review-principal-mismatch")
            if data.get("state") != "APPROVED":
                _refuse("review-not-currently-approved")
            submitted = _wire_utc(data.get("submitted_at"))
            if type(data.get("body")) is not str:
                _refuse("review-body-required")
            body = JsonSnapshot(data["body"].encode("utf-8"))
            authenticate_snapshot(binding.body_reference, body)
            expiry = _decision(body, role, context)
            fetched.append((role, binding, response.sha256, body.sha256, submitted, expiry, request_started))
    except (GitHubReviewRetrievalError, InterchangeFormatError) as error:
        _refuse(str(error))
    checked = _context_elapsed()
    if checked < last_elapsed:
        _refuse("trusted-time-acquisition-invalid")
    try:
        now = context.trusted_now_utc + timedelta(seconds=checked - context._time_acquired_monotonic)
        observations = []
        for role, binding, response_digest, body_digest, submitted, expiry, request_started in fetched:
            status_started = context.trusted_now_utc + timedelta(seconds=request_started - context._time_acquired_monotonic)
            if submitted > status_started:
                _refuse("review-submitted-in-future")
            lag = (submitted - context.manifest_created_at_utc).total_seconds()
            if not 0 <= lag <= context.policy.max_review_lag_seconds:
                _refuse("review-manifest-order-or-lag-invalid")
            if not 0 < (expiry - submitted).total_seconds() <= context.policy.max_approval_lifetime_seconds:
                _refuse("review-approval-lifetime-invalid")
            if now >= expiry:
                _refuse("review-expired")
            if checked - request_started > context.policy.max_status_age_seconds:
                _refuse("review-status-stale")
            observations.append(ReviewObservation(
                client.issuer, role, binding.expected_principal, binding.locator, context.manifest,
                context.profile_sha256, context.workload_sha256, context.target_sha256, context.session_id,
                context.source_commit, context.reviewed_commit, context.policy.policy_sha256,
                response_digest, body_digest, submitted, expiry, status_started, now))
    except OverflowError:
        _refuse("trusted-utc-time-out-of-range")
    return tuple(observations)
