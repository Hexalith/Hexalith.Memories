"""Read-only GitHub-backed C1 policy/session/review authority observations.

The independent bootstrap root authenticates policy before policy can delegate
session review. Actual capture execution, target identity, manifest creation and
custody remain independently authenticated caller facts. No target is contacted.
"""

from __future__ import annotations

from collections.abc import Mapping
from dataclasses import dataclass, field
from datetime import datetime, timedelta
import importlib
from pathlib import Path
import re
import sys


_PLATFORM_SOURCE = (Path(__file__).resolve().parents[1]
                    / "references/Hexalith.Platform/eng/hexalith_github_decisions.py").resolve()
if not _PLATFORM_SOURCE.is_file():
    raise ModuleNotFoundError("pinned-hexalith-github-decision-source-required") from None
sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(_PLATFORM_SOURCE.parent))
_platform = importlib.import_module("hexalith_github_decisions")
if (getattr(_platform, "__file__", None) is None
        or Path(_platform.__file__).resolve() != _PLATFORM_SOURCE
        or getattr(getattr(_platform, "__spec__", None), "origin", None) != str(_PLATFORM_SOURCE)):
    raise ImportError("pinned-hexalith-github-decision-origin-required") from None
from hexalith_github_decisions import (  # noqa: E402
    AuthenticatedReview, ExactReviewBinding, GitHubDecisionError, authenticate_review, require_principal, wire_utc,
)
# A cached sibling from another checkout must not choose this adapter's rules.
# Check policy/wire first, before the approvals module can import either one.
for _name, _label in (("access_telemetry_c1_approval_policy", "c1-principal-policy"),
                      ("access_telemetry_c1_interchange", "c1-wire"),
                      ("access_telemetry_c1_github_approvals", "c1-github-approvals")):
    _source = Path(__file__).resolve().with_name(_name + ".py")
    if not _source.is_file():
        raise ModuleNotFoundError("pinned-" + _label + "-source-required") from None
    _dependency = importlib.import_module(_name)
    if (getattr(_dependency, "__file__", None) is None
            or Path(_dependency.__file__).resolve() != _source
            or getattr(getattr(_dependency, "__spec__", None), "origin", None) != str(_source)):
        raise ImportError("pinned-" + _label + "-origin-required") from None
from access_telemetry_c1_github_approvals import (  # noqa: E402
    ApprovalContext, CurrentRolePolicy, GitHubApprovalError, GitHubReviewClient, RequestLimits,
    ReviewBinding, ReviewLocator, ReviewObservation, consume_bundle_reviews, _elapsed, _finite_number,
)
from access_telemetry_c1_approval_policy import (  # noqa: E402
    BundlePrincipalPolicyError, validate_bundle_principal_separation,
)
from access_telemetry_c1_interchange import (  # noqa: E402
    EvidenceReference, InterchangeFormatError, JsonSnapshot, authenticate_snapshot, parse_ref,
)


POLICY_SCHEMA = "hexalith.access-telemetry.c1.github-policy/v1"
SESSION_SCHEMA = "hexalith.access-telemetry.c1.github-session/v1"
GATE_SCHEMA = "hexalith.access-telemetry.c1.github-gate-review/v1"
# Approved current identities, independently published by the PG2 contract.
# Keep this read-only adapter independent of the legacy accepting verifier.
PG2_PROFILE_SHA256 = "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe"
PG2_WORKLOAD_SHA256 = "71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f"
_SCOPE_FIELDS = frozenset({"profileId", "profileSha256", "workloadSha256", "targetSha256",
                           "tenantId", "sessionId", "sourceCommit"})
_COMMON_FIELDS = _SCOPE_FIELDS | {"schema", "decision", "issuedAtUtc", "expiresAtUtc"}
_POLICY_FIELDS = _COMMON_FIELDS | {"sessionReviewers", "reviewerGrants", "permittedGates", "permittedActions",
                                    "gateDependencies", "maxStatusAgeSeconds", "maxDecisionLifetimeSeconds",
                                    "maxReviewLagSeconds"}
_SESSION_FIELDS = _COMMON_FIELDS | {"policy", "reviewerGrants", "permittedGates", "permittedActions",
                                     "producerPrincipals", "notBeforeUtc", "collectionEndsAtUtc", "reviewEndsAtUtc"}
_GATE_FIELDS = _COMMON_FIELDS | {"policy", "session", "gate", "role", "capture", "parents"}
_GRANT_FIELDS = frozenset({"principal", "gates", "actions", "roles"})
_ACTIONS = frozenset({"review-gate", "review-bundle"})
_ROLES = frozenset({"operations", "security"})
_GATE = re.compile(r"C1\.(?:[1-9]|1[0-9]|2[0-5])\Z")
_ID = re.compile(r"[A-Za-z0-9][A-Za-z0-9._:-]{0,127}\Z")
_DIGEST = re.compile(r"[0-9a-f]{64}\Z")
_COMMIT = re.compile(r"[0-9a-f]{40}\Z")


class GitHubAuthorityError(ValueError):
    """A bounded, content-free refusal with no partial result."""


def _refuse(code: str) -> None:
    raise GitHubAuthorityError(code) from None


def _clock() -> float:
    try:
        value = _elapsed()
    except ValueError:
        _refuse("authority-time-unavailable")
    if not _finite_number(value) or value < 0:
        _refuse("authority-time-invalid")
    return value


def _utc(value: datetime) -> None:
    if type(value) is not datetime or value.tzinfo is None or value.utcoffset() != timedelta(0):
        _refuse("authority-trusted-utc-required")


def _positive(value: object) -> None:
    if not _finite_number(value) or value <= 0:
        _refuse("authority-explicit-limits-required")


def _principal_set(value: object) -> frozenset[str]:
    if type(value) not in (tuple, frozenset) or not 1 <= len(value) <= 100:
        _refuse("authority-principals-required")
    for principal in value:
        require_principal(principal)
    if len(set(value)) != len(value):
        _refuse("authority-duplicate-principal")
    return frozenset(value)


def _set(value: object, permitted: frozenset | None = None, *, gates: bool = False) -> frozenset[str]:
    if type(value) is not tuple or not 1 <= len(value) <= 25 or any(type(item) is not str for item in value):
        _refuse("authority-permissions-invalid")
    result = frozenset(value)
    if len(result) != len(value) or permitted is not None and not result <= permitted:
        _refuse("authority-permission-escalation")
    if gates and any(_GATE.fullmatch(item) is None for item in result):
        _refuse("authority-gate-invalid")
    return result


@dataclass(frozen=True, slots=True)
class AuthorityScope:
    """Independently authenticated expected PG2 target/tenant/source/session scope."""

    profile_sha256: str
    workload_sha256: str
    target_sha256: str
    tenant_id: str
    session_id: str
    source_commit: str

    def __post_init__(self) -> None:
        for digest in (self.profile_sha256, self.workload_sha256, self.target_sha256):
            if type(digest) is not str or _DIGEST.fullmatch(digest) is None:
                _refuse("authority-scope-digest-invalid")
        if (self.profile_sha256, self.workload_sha256) != (PG2_PROFILE_SHA256, PG2_WORKLOAD_SHA256):
            _refuse("authority-current-pg2-identity-required")
        for identifier in (self.tenant_id, self.session_id):
            if type(identifier) is not str or _ID.fullmatch(identifier) is None:
                _refuse("authority-scope-identifier-invalid")
        if type(self.source_commit) is not str or _COMMIT.fullmatch(self.source_commit) is None:
            _refuse("authority-source-commit-invalid")

    def wire(self) -> dict:
        """Expected exact machine-body fields; this establishes no target identity."""
        return {"profileId": "PG-ONPREM-2", "profileSha256": self.profile_sha256,
                "workloadSha256": self.workload_sha256, "targetSha256": self.target_sha256,
                "tenantId": self.tenant_id, "sessionId": self.session_id, "sourceCommit": self.source_commit}


@dataclass(frozen=True, slots=True)
class BootstrapRoot:
    """Independent issuer/repository/policy-reviewer trust and time acquisition.

    Every field is mandatory. A policy body cannot select or modify this root.
    The UTC sample and suspend-aware acquisition anchor travel together and are
    preserved on reuse/replacement; they require independent authentication.
    """

    expected_issuer: str
    owner: str
    repository: str
    policy_reviewers: frozenset[str]
    trusted_now_utc: datetime
    time_acquired_elapsed: float
    request_limits: RequestLimits
    max_status_age_seconds: float
    max_decision_lifetime_seconds: float
    max_review_lag_seconds: float
    max_dependencies: int

    def __post_init__(self) -> None:
        if type(self.expected_issuer) is not str or not self.expected_issuer or len(self.expected_issuer) > 128:
            _refuse("authority-independent-issuer-required")
        try:
            ReviewLocator(self.owner, self.repository, 1, 1)
            if type(self.policy_reviewers) is not frozenset:
                _refuse("authority-independent-policy-reviewers-required")
            _principal_set(self.policy_reviewers)
        except (GitHubDecisionError, ValueError):
            _refuse("authority-bootstrap-invalid")
        _utc(self.trusted_now_utc)
        if not _finite_number(self.time_acquired_elapsed) or not 0 <= self.time_acquired_elapsed <= _clock():
            _refuse("authority-time-acquisition-invalid")
        if type(self.request_limits) is not RequestLimits:
            _refuse("authority-explicit-request-limits-required")
        for limit in (self.max_status_age_seconds, self.max_decision_lifetime_seconds, self.max_review_lag_seconds):
            _positive(limit)
        if type(self.max_dependencies) is not int or not 2 <= self.max_dependencies <= 100:
            _refuse("authority-dependency-limit-invalid")


@dataclass(frozen=True, slots=True)
class AuthorityBinding:
    """An exact retained body Ref plus independently selected review/PR commit."""

    review: ReviewBinding
    reviewed_commit: str

    def __post_init__(self) -> None:
        if type(self.review) is not ReviewBinding:
            _refuse("authority-review-binding-required")
        try:
            self.exact()
        except GitHubDecisionError as error:
            _refuse(str(error))

    def exact(self) -> ExactReviewBinding:
        """Convert retained snapshot facts to the generic Platform boundary."""
        return ExactReviewBinding(self.review.locator, self.review.expected_principal, self.reviewed_commit,
                                 self.review.body_reference.sha256, self.review.body_reference.byte_length,
                                 self.review.retained_body)


@dataclass(frozen=True, slots=True)
class AuthorityContext:
    """Untrusted retained policy/session inputs anchored by an independent root."""

    root: BootstrapRoot
    scope: AuthorityScope
    policy: AuthorityBinding
    session: AuthorityBinding

    def __post_init__(self) -> None:
        if (type(self.root) is not BootstrapRoot or type(self.scope) is not AuthorityScope
                or type(self.policy) is not AuthorityBinding or type(self.session) is not AuthorityBinding):
            _refuse("authority-root-scope-and-bindings-required")


@dataclass(frozen=True, slots=True)
class GateFacts:
    """Independent actual capture snapshot, finish time and producer identities.

    Construction checks bytes/shape only. Execution provenance and custody must
    be authenticated independently; a review cannot reconstruct either fact.
    """

    gate: str
    capture: EvidenceReference
    capture_bytes: bytes = field(repr=False)
    capture_finished_at_utc: datetime
    producer_principals: frozenset[str]

    def __post_init__(self) -> None:
        if type(self.gate) is not str or _GATE.fullmatch(self.gate) is None:
            _refuse("authority-gate-invalid")
        _utc(self.capture_finished_at_utc)
        if type(self.producer_principals) is not frozenset:
            _refuse("authority-producer-principals-required")
        try:
            _principal_set(self.producer_principals)
            authenticate_snapshot(self.capture, JsonSnapshot(self.capture_bytes))
        except (GitHubDecisionError, InterchangeFormatError) as error:
            _refuse(str(error))


@dataclass(frozen=True, slots=True)
class GateReviewRequest:
    """One retained review with independently authenticated capture facts."""

    binding: AuthorityBinding
    facts: GateFacts

    def __post_init__(self) -> None:
        if type(self.binding) is not AuthorityBinding or type(self.facts) is not GateFacts:
            _refuse("authority-gate-binding-and-facts-required")


@dataclass(frozen=True, slots=True)
class ManifestFacts:
    """Independent manifest bytes/creation/producers and selected bundle PR commit."""

    manifest: EvidenceReference
    manifest_bytes: bytes = field(repr=False)
    manifest_created_at_utc: datetime
    producer_principals: frozenset[str]
    reviewed_commit: str

    def __post_init__(self) -> None:
        _utc(self.manifest_created_at_utc)
        if type(self.producer_principals) is not frozenset:
            _refuse("authority-producer-principals-required")
        try:
            _principal_set(self.producer_principals)
            authenticate_snapshot(self.manifest, JsonSnapshot(self.manifest_bytes))
        except (GitHubDecisionError, InterchangeFormatError) as error:
            _refuse(str(error))
        if type(self.reviewed_commit) is not str or _COMMIT.fullmatch(self.reviewed_commit) is None:
            _refuse("authority-reviewed-commit-invalid")


@dataclass(frozen=True, slots=True)
class AuthorityObservation:
    """Checked facts for this invocation, never accepted evidence or execution."""

    kind: str
    scope: AuthorityScope
    body_reference: EvidenceReference
    issuer: str
    reviewer_principal: str
    locator: ReviewLocator
    reviewed_commit: str
    response_sha256: str
    submitted_at_utc: datetime
    expires_at_utc: datetime
    effective_expires_at_utc: datetime
    status_request_started_at_utc: datetime
    checked_at_utc: datetime
    gate: str | None
    role: str | None


@dataclass(frozen=True, slots=True)
class AuthorityChainObservation:
    """Immutable policy/session/gate/bundle facts, with no usable grant handle."""

    authority: tuple[AuthorityObservation, ...]
    bundle_reviews: tuple[ReviewObservation, ...]
    effective_expires_at_utc: datetime
    checked_at_utc: datetime


def _body(binding: AuthorityBinding, fields: frozenset, schema: str, scope: AuthorityScope) -> Mapping:
    try:
        value = JsonSnapshot(binding.review.retained_body).value
    except InterchangeFormatError as error:
        _refuse(str(error))
    if not isinstance(value, Mapping) or set(value) != fields:
        _refuse("authority-body-field-set")
    if value["schema"] != schema or value["decision"] != "approve":
        _refuse("authority-schema-or-decision-invalid")
    if any(type(value[key]) is not str or value[key] != expected for key, expected in scope.wire().items()):
        _refuse("authority-scope-mismatch")
    return value


def _reference(value: object, expected: EvidenceReference) -> None:
    if parse_ref(value) != expected:
        _refuse("authority-parent-reference-mismatch")


def _grants(value: object, gates: frozenset, actions: frozenset) -> dict:
    if type(value) is not tuple or not 1 <= len(value) <= 100:
        _refuse("authority-reviewer-grants-required")
    grants = {}
    for grant in value:
        if not isinstance(grant, Mapping) or set(grant) != _GRANT_FIELDS:
            _refuse("authority-grant-field-set")
        require_principal(grant["principal"])
        principal = grant["principal"]
        if principal in grants:
            _refuse("authority-duplicate-principal")
        grants[principal] = (_set(grant["gates"], gates, gates=True),
                             _set(grant["actions"], actions), _set(grant["roles"], _ROLES))
    return grants


def _dependencies(value: object, gates: frozenset) -> dict:
    if not isinstance(value, Mapping) or set(value) != gates:
        _refuse("authority-gate-dependency-field-set")
    graph = {}
    for gate, parents in value.items():
        if type(parents) is not tuple or len(parents) > 25 or any(type(parent) is not str for parent in parents):
            _refuse("authority-gate-dependencies-invalid")
        graph[gate] = frozenset(parents)
        if len(graph[gate]) != len(parents) or not graph[gate] <= gates:
            _refuse("authority-gate-dependencies-invalid")
    visited, active = set(), set()

    def visit(gate):
        if gate in active:
            _refuse("authority-dependency-cycle")
        if gate in visited:
            return
        active.add(gate)
        for parent in graph[gate]:
            visit(parent)
        active.remove(gate)
        visited.add(gate)

    for gate in graph:
        visit(gate)
    return graph


class _Chain:
    """Invocation-local verified state, discarded completely on refusal."""

    def __init__(self, client, token, context, producers, count):
        if type(client) is not GitHubReviewClient or type(context) is not AuthorityContext:
            _refuse("authority-client-and-context-required")
        self.client, self.token, self.context = client, token, context
        self.root = context.root
        if client.issuer != self.root.expected_issuer:
            _refuse("authority-issuer-mismatch")
        if count > self.root.max_dependencies:
            _refuse("authority-dependency-budget-exceeded")
        if type(producers) is not frozenset:
            _refuse("authority-producer-principals-required")
        _principal_set(producers)
        self.producers = producers
        self.status_limit = self.root.max_status_age_seconds
        self.lifetime_limit = self.root.max_decision_lifetime_seconds
        self.lag_limit = self.root.max_review_lag_seconds
        self.last_elapsed = self.root.time_acquired_elapsed
        self.checked = self.root.trusted_now_utc
        self.records = []
        self.seen_reviews = set()

    def now(self):
        elapsed = _clock()
        if elapsed < self.last_elapsed:
            _refuse("authority-elapsed-regression")
        self.last_elapsed = elapsed
        try:
            now = self.root.trusted_now_utc + timedelta(seconds=elapsed - self.root.time_acquired_elapsed)
        except OverflowError:
            _refuse("authority-time-out-of-range")
        return elapsed, now

    def check(self):
        elapsed, now = self.now()
        for _, _, review, _, expiry, status_limit in self.records:
            if now >= expiry:
                _refuse("authority-expired")
            if elapsed - review.request_started_elapsed > min(status_limit, self.status_limit):
                _refuse("authority-status-stale")
        if hasattr(self, "session_window") and not self.session_window[0] <= now < self.session_window[2]:
            _refuse("authority-session-window-denied")
        self.checked = now
        return now

    def fetch(self, kind, binding, body, after):
        locator = binding.review.locator
        if (locator.owner, locator.repository) != (self.root.owner, self.root.repository):
            _refuse("authority-repository-mismatch")
        key = (locator.owner, locator.repository, locator.pull_number, locator.review_id)
        if key in self.seen_reviews:
            _refuse("authority-review-reused")
        if binding.review.expected_principal in self.producers:
            _refuse("authority-reviewer-producer-overlap")
        issued, expiry = wire_utc(body["issuedAtUtc"]), wire_utc(body["expiresAtUtc"])
        now = self.check()
        if not after <= issued <= now or not 0 < (expiry - issued).total_seconds() <= self.lifetime_limit:
            _refuse("authority-decision-order-or-lifetime-invalid")
        if now >= expiry:
            _refuse("authority-expired")
        prior_elapsed = self.last_elapsed
        review = authenticate_review(client=self.client, token=self.token, binding=binding.exact(),
                                     expected_issuer=self.root.expected_issuer, limits=self.root.request_limits)
        if not prior_elapsed <= review.request_started_elapsed <= review.checked_elapsed:
            _refuse("authority-elapsed-regression")
        self.last_elapsed = review.checked_elapsed
        status_utc = self.root.trusted_now_utc + timedelta(
            seconds=review.request_started_elapsed - self.root.time_acquired_elapsed)
        if not issued <= review.submitted_at_utc <= status_utc:
            _refuse("authority-submission-order-invalid")
        if (review.submitted_at_utc - issued).total_seconds() > self.lag_limit or review.submitted_at_utc >= expiry:
            _refuse("authority-review-lag-or-expiry-invalid")
        self.records.append((kind, binding, review, body, expiry, self.status_limit))
        self.seen_reviews.add(key)
        self.check()
        return review

    def bootstrap(self):
        context = self.context
        if context.policy.review.expected_principal not in self.root.policy_reviewers:
            _refuse("authority-policy-reviewer-denied")
        policy = _body(context.policy, _POLICY_FIELDS, POLICY_SCHEMA, context.scope)
        gates = _set(policy["permittedGates"], gates=True)
        actions = _set(policy["permittedActions"], _ACTIONS)
        reviewers = _principal_set(policy["sessionReviewers"])
        self.policy_grants = _grants(policy["reviewerGrants"], gates, actions)
        self.dependencies = _dependencies(policy["gateDependencies"], gates)
        policy_limits = [policy[key] for key in ("maxStatusAgeSeconds", "maxDecisionLifetimeSeconds", "maxReviewLagSeconds")]
        for limit in policy_limits:
            # Wire limits are positive integers; floats are refused by snapshots.
            if type(limit) is not int:
                _refuse("authority-explicit-limits-required")
            _positive(limit)
        if any(value > root for value, root in zip(policy_limits, (self.status_limit, self.lifetime_limit, self.lag_limit))):
            _refuse("authority-limit-escalation")
        self.status_limit, self.lifetime_limit, self.lag_limit = policy_limits
        policy_review = self.fetch("policy", context.policy, policy, datetime.min.replace(tzinfo=self.root.trusted_now_utc.tzinfo))
        if context.session.review.expected_principal not in reviewers:
            _refuse("authority-session-reviewer-denied")
        session = _body(context.session, _SESSION_FIELDS, SESSION_SCHEMA, context.scope)
        _reference(session["policy"], context.policy.review.body_reference)
        self.gates = _set(session["permittedGates"], gates, gates=True)
        self.actions = _set(session["permittedActions"], actions)
        self.grants = _grants(session["reviewerGrants"], self.gates, self.actions)
        for principal, grant in self.grants.items():
            allowed = self.policy_grants.get(principal)
            if allowed is None or any(not current <= maximum for current, maximum in zip(grant, allowed)):
                _refuse("authority-grant-escalation")
        allowed_producers = _principal_set(session["producerPrincipals"])
        if not self.producers <= allowed_producers:
            _refuse("authority-producer-denied")
        if any(principal in allowed_producers for principal in self.grants):
            _refuse("authority-reviewer-producer-overlap")
        start, collection_end, review_end = (wire_utc(session[key]) for key in
                                              ("notBeforeUtc", "collectionEndsAtUtc", "reviewEndsAtUtc"))
        issued, expiry = wire_utc(session["issuedAtUtc"]), wire_utc(session["expiresAtUtc"])
        if not issued <= start < collection_end <= review_end <= expiry:
            _refuse("authority-session-window-order-invalid")
        session_review = self.fetch("session", context.session, session, policy_review.submitted_at_utc)
        if session_review.submitted_at_utc > start:
            _refuse("authority-session-granted-after-window")
        self.session_window = (start, collection_end, review_end)
        self.session_submitted = session_review.submitted_at_utc
        self.check()

    def role(self, principal, action, role, gate=None):
        grant = self.grants.get(principal)
        if (grant is None or action not in self.actions or action not in grant[1]
                or role not in grant[2] or gate is not None and (gate not in self.gates or gate not in grant[0])):
            _refuse("authority-review-role-denied")
        if principal in self.producers:
            _refuse("authority-reviewer-producer-overlap")

    def observations(self, extra_expiries=()):
        now = self.check()
        expiries = [record[4] for record in self.records] + list(extra_expiries) + [self.session_window[2]]
        earliest = min(expiries)
        if now >= earliest:
            _refuse("authority-expired")
        observations = []
        for kind, binding, review, body, expiry, _ in self.records:
            start = self.root.trusted_now_utc + timedelta(
                seconds=review.request_started_elapsed - self.root.time_acquired_elapsed)
            observations.append(AuthorityObservation(kind, self.context.scope, binding.review.body_reference,
                                review.issuer, review.reviewer_principal, review.locator, review.reviewed_commit,
                                review.response_sha256, review.submitted_at_utc, expiry, earliest, start, now,
                                body.get("gate"), body.get("role")))
        return tuple(observations), earliest, now


def _gates(chain: _Chain, requests: tuple[GateReviewRequest, ...]):
    if type(requests) is not tuple or len(requests) > 25 or any(type(item) is not GateReviewRequest for item in requests):
        _refuse("authority-gate-requests-invalid")
    entries, gate_names = {}, set()
    for request in requests:
        ref = request.binding.review.body_reference
        if ref in entries or request.facts.gate in gate_names:
            _refuse("authority-gate-or-reference-reused")
        entries[ref] = request
        gate_names.add(request.facts.gate)
    ordered, active, done = [], set(), set()

    def visit(ref):
        if ref in active:
            _refuse("authority-dependency-cycle")
        if ref in done:
            return
        if ref not in entries:
            _refuse("authority-parent-review-required")
        active.add(ref)
        request = entries[ref]
        body = _body(request.binding, _GATE_FIELDS, GATE_SCHEMA, chain.context.scope)
        _reference(body["policy"], chain.context.policy.review.body_reference)
        _reference(body["session"], chain.context.session.review.body_reference)
        _reference(body["capture"], request.facts.capture)
        if body["gate"] != request.facts.gate or body["role"] not in _ROLES:
            _refuse("authority-gate-or-role-mismatch")
        chain.role(request.binding.review.expected_principal, "review-gate", body["role"], request.facts.gate)
        parents = body["parents"]
        if type(parents) is not tuple or len(parents) > 25:
            _refuse("authority-parent-references-invalid")
        refs = tuple(parse_ref(parent) for parent in parents)
        if len(set(refs)) != len(refs):
            _refuse("authority-parent-reference-reused")
        for parent in refs:
            visit(parent)
        if frozenset(entries[parent].facts.gate for parent in refs) != chain.dependencies[request.facts.gate]:
            _refuse("authority-parent-gate-set-mismatch")
        active.remove(ref)
        done.add(ref)
        ordered.append((request, body, refs))

    for ref in entries:
        visit(ref)
    submitted = {}
    for request, body, refs in ordered:
        finish = request.facts.capture_finished_at_utc
        if not chain.session_window[0] <= finish < chain.session_window[1]:
            _refuse("authority-capture-window-denied")
        after = max([finish, chain.session_submitted] + [submitted[ref] for ref in refs])
        review = chain.fetch("gate", request.binding, body, after)
        if (review.submitted_at_utc - finish).total_seconds() > chain.lag_limit:
            _refuse("authority-capture-review-lag-exceeded")
        if review.submitted_at_utc > chain.session_window[2]:
            _refuse("authority-review-window-denied")
        submitted[request.binding.review.body_reference] = review.submitted_at_utc


def consume_gate_reviews(*, client: GitHubReviewClient, token: str, context: AuthorityContext,
                         requests: tuple[GateReviewRequest, ...]) -> AuthorityChainObservation:
    """Re-fetch the complete policy/session/parent chain and observe gate reviews.

    Independently supplied capture facts remain a prerequisite, not a product of
    this call. No semantic gate verdict or accepted predecessor is returned.
    """
    try:
        if (type(requests) is not tuple or not 1 <= len(requests) <= 25
                or any(type(item) is not GateReviewRequest for item in requests)):
            _refuse("authority-gate-requests-required")
        producers = frozenset(principal for request in requests for principal in request.facts.producer_principals)
        chain = _Chain(client, token, context, producers, 2 + len(requests))
        chain.bootstrap()
        _gates(chain, requests)
        authority, expiry, checked = chain.observations()
        return AuthorityChainObservation(authority, (), expiry, checked)
    except (GitHubDecisionError, InterchangeFormatError, GitHubApprovalError) as error:
        _refuse(str(error))
    except (OverflowError, TypeError, KeyError):
        _refuse("authority-input-invalid")


def consume_authorized_bundle_reviews(*, client: GitHubReviewClient, token: str, context: AuthorityContext,
                                      manifest: ManifestFacts, operations: ReviewBinding, security: ReviewBinding,
                                      gate_reviews: tuple[GateReviewRequest, ...] = ()) -> AuthorityChainObservation:
    """Derive current bundle roles from verified grants and recheck every dependency.

    Manifest creation/producers/custody/target authentication remain external.
    Optional gate observations supply no admitted manifest gate or capture proof.
    Every call re-fetches policy, session, supplied parents and both bundle IDs.
    """
    try:
        if (type(manifest) is not ManifestFacts or type(operations) is not ReviewBinding
                or type(security) is not ReviewBinding or type(gate_reviews) is not tuple
                or len(gate_reviews) > 25
                or any(type(item) is not GateReviewRequest for item in gate_reviews)):
            _refuse("authority-manifest-and-two-reviews-required")
        if any(not request.facts.producer_principals <= manifest.producer_principals for request in gate_reviews):
            _refuse("authority-manifest-producer-mismatch")
        chain = _Chain(client, token, context, manifest.producer_principals, 4 + len(gate_reviews))
        chain.bootstrap()
        _gates(chain, gate_reviews)
        chain.role(operations.expected_principal, "review-bundle", "operations")
        chain.role(security.expected_principal, "review-bundle", "security")
        validate_bundle_principal_separation(operations.expected_principal, security.expected_principal,
                                            manifest.producer_principals)
        for review in (operations, security):
            locator = review.locator
            if (locator.owner, locator.repository) != (chain.root.owner, chain.root.repository):
                _refuse("authority-repository-mismatch")
            if (locator.owner, locator.repository, locator.pull_number, locator.review_id) in chain.seen_reviews:
                _refuse("authority-review-reused")
        now = chain.check()
        if not chain.session_submitted <= manifest.manifest_created_at_utc <= now:
            _refuse("authority-manifest-order-invalid")
        if not chain.session_window[0] <= manifest.manifest_created_at_utc <= chain.session_window[2]:
            _refuse("authority-manifest-window-denied")
        policy = CurrentRolePolicy(context.policy.review.body_reference.sha256, context.scope.profile_sha256,
                                   context.scope.workload_sha256, context.scope.target_sha256, context.scope.session_id,
                                   frozenset(p for p, grant in chain.grants.items() if "review-bundle" in grant[1] and "operations" in grant[2]),
                                   frozenset(p for p, grant in chain.grants.items() if "review-bundle" in grant[1] and "security" in grant[2]),
                                   chain.status_limit, chain.lifetime_limit, chain.lag_limit)
        approval = ApprovalContext(manifest.manifest, manifest.manifest_bytes, manifest.manifest_created_at_utc,
                                   context.scope.profile_sha256, context.scope.workload_sha256, context.scope.target_sha256,
                                   context.scope.session_id, context.scope.source_commit, manifest.reviewed_commit, policy,
                                   manifest.producer_principals, now, chain.root.expected_issuer,
                                   chain.root.request_limits, chain.last_elapsed)
        bundle = consume_bundle_reviews(client=client, token=token, context=approval, operations=operations, security=security)
        if any(review.status_request_started_at_utc < now or review.checked_at_utc < now for review in bundle):
            _refuse("authority-elapsed-regression")
        if any(review.submitted_at_utc > chain.session_window[2] for review in bundle):
            _refuse("authority-review-window-denied")
        authority, expiry, checked = chain.observations(review.expires_at_utc for review in bundle)
        for review in bundle:
            if checked < review.checked_at_utc:
                _refuse("authority-elapsed-regression")
            if (checked - review.status_request_started_at_utc).total_seconds() > chain.status_limit:
                _refuse("authority-status-stale")
        return AuthorityChainObservation(authority, bundle, expiry, checked)
    except (GitHubDecisionError, InterchangeFormatError, GitHubApprovalError, BundlePrincipalPolicyError) as error:
        _refuse(str(error))
    except (OverflowError, TypeError, KeyError):
        _refuse("authority-input-invalid")
