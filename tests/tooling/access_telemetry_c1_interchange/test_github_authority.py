"""Real TLS policy/session/gate/bundle chains; fixtures grant no live authority."""

from contextlib import redirect_stderr, redirect_stdout
import copy
from dataclasses import FrozenInstanceError, replace
from datetime import timedelta
import hashlib
import io
import json
import multiprocessing
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

import test_github_approvals as tls

from access_telemetry_c1_github_authority import (
    AuthorityBinding, AuthorityContext, AuthorityScope, BootstrapRoot, GATE_SCHEMA,
    GateFacts, GateReviewRequest, GitHubAuthorityError, ManifestFacts, POLICY_SCHEMA, SESSION_SCHEMA,
    consume_authorized_bundle_reviews, consume_gate_reviews,
)
import access_telemetry_c1_github_authority as authority
import access_telemetry_c1_github_approvals as approvals
import hexalith_github_decisions as decisions
from hexalith_github_reviews import _elapsed


ROOT_REVIEWER = "github:user:1001"
SESSION_REVIEWER = "github:user:1002"
# Independent published identity literals, never read from the implementation.
PROFILE = "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe"
WORKLOAD = "71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f"


class GitHubAuthorityTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        tls.GitHubReviewContractTests.setUpClass.__func__(cls)

    def setUp(self):
        self.fixture = tls.FixtureServer(self.cert, self.key)
        self.addCleanup(self.fixture.close)
        self.client = tls.GitHubReviewClient(self.fixture.port, self.ca_pem)
        self.scope = AuthorityScope(PROFILE, WORKLOAD, tls.TARGET, "fixture-tenant", "fixture-session", tls.SOURCE)
        self.root = BootstrapRoot(self.client.issuer, "fixture-owner", "fixture-evidence",
                                  frozenset({ROOT_REVIEWER}), tls.NOW, _elapsed(), tls.RequestLimits(16_384, 3),
                                  10, 3600, 120, 10)
        common = {**self.scope.wire(), "decision": "approve", "expiresAtUtc": tls.wire_time(tls.NOW + timedelta(seconds=1200))}
        grants = [{"principal": tls.OWNER, "gates": ["C1.1", "C1.2"],
                   "actions": ["review-gate", "review-bundle"], "roles": ["operations", "security"]}]
        self.policy_body = {**common, "schema": POLICY_SCHEMA, "issuedAtUtc": tls.wire_time(tls.NOW - timedelta(seconds=180)),
                            "sessionReviewers": [SESSION_REVIEWER], "reviewerGrants": copy.deepcopy(grants),
                            "permittedGates": ["C1.1", "C1.2"], "permittedActions": ["review-gate", "review-bundle"],
                            "gateDependencies": {"C1.1": [], "C1.2": ["C1.1"]}, "maxStatusAgeSeconds": 10,
                            "maxDecisionLifetimeSeconds": 3600, "maxReviewLagSeconds": 120}
        self.session_body = {**common, "schema": SESSION_SCHEMA, "issuedAtUtc": tls.wire_time(tls.NOW - timedelta(seconds=160)),
                             "reviewerGrants": copy.deepcopy(grants), "permittedGates": ["C1.1", "C1.2"],
                             "permittedActions": ["review-gate", "review-bundle"], "producerPrincipals": [tls.PRODUCER],
                             "notBeforeUtc": tls.wire_time(tls.NOW - timedelta(seconds=140)),
                             "collectionEndsAtUtc": tls.wire_time(tls.NOW - timedelta(seconds=10)),
                             "reviewEndsAtUtc": tls.wire_time(tls.NOW + timedelta(seconds=900))}
        self.gate_bodies = {
            "C1.1": {**common, "schema": GATE_SCHEMA, "issuedAtUtc": tls.wire_time(tls.NOW - timedelta(seconds=80)),
                     "gate": "C1.1", "role": "operations"},
            "C1.2": {**common, "schema": GATE_SCHEMA, "issuedAtUtc": tls.wire_time(tls.NOW - timedelta(seconds=70)),
                     "gate": "C1.2", "role": "security"},
        }
        self.submissions = {"policy": -175, "session": -155, "C1.1": -75, "C1.2": -65, "operations": -30, "security": -25}
        self.review_principals = {"policy": ROOT_REVIEWER, "session": SESSION_REVIEWER,
                                  "C1.1": tls.OWNER, "C1.2": tls.OWNER, "operations": tls.OWNER, "security": tls.OWNER}
        self.rebuild()

    def ref_wire(self, ref):
        return {"path": ref.path, "sha256": ref.sha256, "byteLength": ref.byte_length}

    def binding(self, tag, body, review_id, principal=None):
        raw = tls.encode(body)
        binding = tls.ReviewBinding(tls.ReviewLocator(self.root.owner, self.root.repository, 7, review_id),
                                    principal or self.review_principals[tag], tls.reference("retained/" + tag + ".json", raw), raw)
        remote = {"id": review_id, "user": {"id": int(binding.expected_principal.split(":")[-1]), "type": "User"},
                  "state": "APPROVED", "body": raw.decode(), "commit_id": tls.REVIEWED,
                  "submitted_at": tls.wire_time(tls.NOW + timedelta(seconds=self.submissions[tag])),
                  "pull_request_url": self.client.api_origin + binding.locator.pull_path, "author_association": "OWNER"}
        self.fixture.responses[binding.locator.review_path] = {"json": remote}
        return binding

    def rebuild(self):
        self.policy = AuthorityBinding(self.binding("policy", self.policy_body, 201), tls.REVIEWED)
        self.session_body["policy"] = self.ref_wire(self.policy.review.body_reference)
        self.session = AuthorityBinding(self.binding("session", self.session_body, 202), tls.REVIEWED)
        self.context = AuthorityContext(self.root, self.scope, self.policy, self.session)
        requests = []
        by_gate = {}
        for index, (gate, body) in enumerate(self.gate_bodies.items()):
            capture_raw = tls.encode({"fixture": "independent capture provenance required", "gate": gate})
            capture = tls.reference("captures/" + gate + ".json", capture_raw)
            body.update({"policy": self.ref_wire(self.policy.review.body_reference),
                         "session": self.ref_wire(self.session.review.body_reference), "capture": self.ref_wire(capture),
                         "parents": [self.ref_wire(by_gate[parent].binding.review.body_reference)
                                     for parent in self.policy_body["gateDependencies"][gate] if parent in by_gate]})
            review_id = 203 + index if index < 2 else 300 + index
            binding = AuthorityBinding(self.binding(gate, body, review_id), tls.REVIEWED)
            facts = GateFacts(gate, capture, capture_raw, tls.NOW - timedelta(seconds=90), frozenset({tls.PRODUCER}))
            request = GateReviewRequest(binding, facts)
            requests.append(request)
            by_gate[gate] = request
        self.requests = tuple(requests)
        manifest_raw = tls.encode({"fixture": "opaque manifest, no admitted gates"})
        self.manifest = ManifestFacts(tls.reference("retained/manifest.json", manifest_raw), manifest_raw,
                                     tls.NOW - timedelta(seconds=60), frozenset({tls.PRODUCER}), tls.REVIEWED)
        bundle = {"schema": approvals.DECISION_SCHEMA, "decision": "approve", "manifest": self.ref_wire(self.manifest.manifest),
                  "profileSha256": PROFILE, "workloadSha256": WORKLOAD, "targetSha256": tls.TARGET,
                  "sessionId": self.scope.session_id, "sourceCommit": tls.SOURCE,
                  "policySha256": self.policy.review.body_reference.sha256,
                  "expiresAtUtc": tls.wire_time(tls.NOW + timedelta(seconds=1000))}
        self.operations = self.binding("operations", {**bundle, "role": "operations"}, 205)
        self.security = self.binding("security", {**bundle, "role": "security"}, 206)

    def configure_gate_graph(self, graph):
        gates = list(graph)
        self.policy_body["permittedGates"] = gates
        self.session_body["permittedGates"] = gates
        self.policy_body["gateDependencies"] = graph
        self.policy_body["reviewerGrants"][0]["gates"] = gates
        self.session_body["reviewerGrants"][0]["gates"] = gates
        prototype = copy.deepcopy(self.gate_bodies["C1.1"])
        self.gate_bodies = {}
        depths = {}
        for gate, parents in graph.items():
            depths[gate] = 1 + max((depths[parent] for parent in parents), default=-1)
            issued = -80 + 10 * depths[gate]
            self.gate_bodies[gate] = {**copy.deepcopy(prototype), "gate": gate,
                                      "issuedAtUtc": tls.wire_time(tls.NOW + timedelta(seconds=issued))}
            self.submissions[gate] = issued + 5
            self.review_principals[gate] = tls.OWNER
        self.rebuild()

    def call(self, **overrides):
        arguments = dict(client=self.client, token=tls.TOKEN, context=self.context, manifest=self.manifest,
                         operations=self.operations, security=self.security, gate_reviews=self.requests)
        arguments.update(overrides)
        return consume_authorized_bundle_reviews(**arguments)

    def gate_call(self, **overrides):
        arguments = dict(client=self.client, token=tls.TOKEN, context=self.context, requests=self.requests)
        arguments.update(overrides)
        return consume_gate_reviews(**arguments)

    def refused(self, code=None, *, gate=False, **overrides):
        with self.assertRaises(GitHubAuthorityError) as raised:
            (self.gate_call if gate else self.call)(**overrides)
        message = str(raised.exception)
        self.assertRegex(message, r"^[a-z0-9-]{1,100}$")
        self.assertNotIn(tls.TOKEN, message)
        if code:
            self.assertEqual(message, code)
        self.assertEqual(multiprocessing.active_children(), [])

    def paths(self):
        return [request["path"] for request in self.fixture.requests]

    def test_real_tls_root_policy_session_parent_gate_and_owner_bundle_chain(self):
        with patch("subprocess.run", side_effect=AssertionError("target command attempted")), \
                patch("subprocess.Popen", side_effect=AssertionError("target command attempted")):
            result = self.call()
        self.assertEqual([item.kind for item in result.authority], ["policy", "session", "gate", "gate"])
        self.assertEqual([item.gate for item in result.authority[2:]], ["C1.1", "C1.2"])
        self.assertEqual([item.role for item in result.bundle_reviews], ["operations", "security"])
        self.assertEqual(result.effective_expires_at_utc, tls.NOW + timedelta(seconds=900))
        self.assertEqual(len(self.fixture.requests), 6)
        self.assertTrue(all(request["authenticated"] for request in self.fixture.requests))
        self.assertNotIn(tls.TOKEN, repr(result))
        with self.assertRaises(FrozenInstanceError):
            result.checked_at_utc = tls.NOW
        with self.assertRaises(FrozenInstanceError):
            result.authority[0].kind = "grant"
        self.assertFalse(hasattr(result, "accepted_gate"))
        self.assertFalse(hasattr(result, "execution_handle"))

    def test_gate_only_tls_chain_topological_order_and_no_bundle_acceptance(self):
        result = self.gate_call(requests=tuple(reversed(self.requests)))
        self.assertEqual([item.gate for item in result.authority[2:]], ["C1.1", "C1.2"])
        self.assertEqual(result.bundle_reviews, ())
        self.assertEqual(len(self.fixture.requests), 4)

    def test_reordered_diamond_fetches_shared_parent_once_at_dependency_budget_equality(self):
        self.root = replace(self.root, max_dependencies=6)
        self.configure_gate_graph({"C1.1": [], "C1.2": ["C1.1"], "C1.3": ["C1.1"],
                                   "C1.4": ["C1.2", "C1.3"]})
        result = self.gate_call(requests=tuple(reversed(self.requests)))
        self.assertEqual([item.gate for item in result.authority[2:]], ["C1.1", "C1.2", "C1.3", "C1.4"])
        self.assertEqual(len(self.fixture.requests), self.root.max_dependencies)
        shared_parent_path = self.requests[0].binding.review.locator.review_path
        self.assertEqual(self.paths().count(shared_parent_path), 1)
        self.assertTrue(all(request["authenticated"] for request in self.fixture.requests))

    def test_supported_25_gate_requests_pass_over_tls_at_count_and_dependency_ceiling(self):
        self.root = replace(self.root, max_dependencies=27)
        self.configure_gate_graph({f"C1.{number}": [] for number in range(1, 26)})
        result = self.gate_call(requests=tuple(reversed(self.requests)))
        self.assertEqual(len(result.authority), 27)
        self.assertEqual({item.gate for item in result.authority[2:]}, {f"C1.{number}" for number in range(1, 26)})
        self.assertEqual(len(self.fixture.requests), self.root.max_dependencies)
        self.assertEqual(len(set(self.paths())), 27)
        self.assertTrue(all(request["authenticated"] for request in self.fixture.requests))

    def test_26_gate_requests_refuse_at_both_public_boundaries_before_https(self):
        root = replace(self.root, max_dependencies=100)
        context = replace(self.context, root=root)
        invalid = (self.requests[0],) * 26
        self.refused("authority-gate-requests-required", gate=True, requests=invalid, context=context)
        self.refused("authority-manifest-and-two-reviews-required", gate_reviews=invalid, context=context)
        self.assertEqual(self.paths(), [])

    def test_later_manifest_uses_equivalent_advanced_utc_anchor_and_future_manifest_refuses(self):
        acquisition = self.root.time_acquired_elapsed
        later = replace(self.manifest, manifest_created_at_utc=tls.NOW + timedelta(seconds=1))
        for binding, seconds in ((self.operations, 2), (self.security, 3)):
            self.fixture.responses[binding.locator.review_path]["json"]["submitted_at"] = tls.wire_time(
                tls.NOW + timedelta(seconds=seconds))
        with patch.object(authority, "_clock", return_value=acquisition + 5), \
                patch.object(decisions, "_elapsed", return_value=acquisition + 5), \
                patch.object(approvals, "_elapsed", return_value=acquisition + 5):
            result = self.call(manifest=later, gate_reviews=())
            self.assertEqual(result.checked_at_utc, tls.NOW + timedelta(seconds=5))
            self.assertEqual(len(result.bundle_reviews), 2)
            self.assertEqual(len(self.fixture.requests), 4)
            self.fixture.requests.clear()
            future = replace(later, manifest_created_at_utc=tls.NOW + timedelta(seconds=6))
            self.refused("authority-manifest-order-invalid", manifest=future, gate_reviews=())
            self.assertEqual(self.paths(), [self.policy.review.locator.review_path, self.session.review.locator.review_path])
        self.assertEqual(self.root.trusted_now_utc, tls.NOW)
        self.assertEqual(self.root.time_acquired_elapsed, acquisition)

    def test_verified_grants_drive_distinct_bundle_reviewers(self):
        other = "github:user:1004"
        grant = copy.deepcopy(self.policy_body["reviewerGrants"][0])
        grant["principal"] = other
        grant["roles"] = ["security"]
        self.policy_body["reviewerGrants"].append(copy.deepcopy(grant))
        self.session_body["reviewerGrants"].append(copy.deepcopy(grant))
        self.review_principals["security"] = other
        self.rebuild()
        self.assertEqual(self.call().bundle_reviews[1].reviewer_principal, other)

    def test_each_call_refetches_every_dependency_and_withdrawal_refuses_before_children(self):
        self.call()
        self.fixture.requests.clear()
        self.call()
        self.assertEqual(len(self.fixture.requests), 6)
        self.fixture.requests.clear()
        self.fixture.responses[self.policy.review.locator.review_path]["json"]["state"] = "DISMISSED"
        self.refused("decision-not-approved")
        self.assertEqual(self.paths(), [self.policy.review.locator.review_path])

    def test_root_policy_identity_is_independent_and_policy_cannot_appoint_root(self):
        denied = replace(self.root, policy_reviewers=frozenset({"github:user:1004"}))
        self.refused("authority-policy-reviewer-denied", context=replace(self.context, root=denied))
        self.assertEqual(self.paths(), [])
        self.policy_body["policyReviewers"] = [ROOT_REVIEWER]
        self.rebuild()
        self.refused("authority-body-field-set")
        self.assertEqual(self.paths(), [])

    def test_scope_cannot_label_historical_or_inexact_profile_workload_hashes_pg2(self):
        for changes in ({"profile_sha256": "dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14"},
                        {"profile_sha256": "0" * 64}, {"workload_sha256": "1" * 64},
                        {"profile_sha256": PROFILE[:-1] + "0"}):
            with self.subTest(fields=tuple(changes)), self.assertRaisesRegex(
                    GitHubAuthorityError, "^authority-current-pg2-identity-required$"):
                replace(self.scope, **changes)
        self.assertEqual(self.paths(), [])

    def test_missing_root_or_role_or_session_refuses_without_later_requests(self):
        self.refused("authority-client-and-context-required", context=None)
        self.assertEqual(self.paths(), [])
        self.review_principals["session"] = "github:user:1004"
        self.rebuild()
        self.refused("authority-session-reviewer-denied")
        self.assertEqual(self.paths(), [self.policy.review.locator.review_path])
        self.fixture.requests.clear()
        self.review_principals["session"] = SESSION_REVIEWER
        self.session_body["reviewerGrants"][0]["roles"] = ["operations"]
        self.rebuild()
        self.refused("authority-review-role-denied")
        self.assertEqual(len(self.fixture.requests), 2)

    def test_cross_target_tenant_session_profile_workload_and_source_denials_before_dependents(self):
        cases = {"targetSha256": "0" * 64, "tenantId": "other-tenant", "sessionId": "other-session",
                 "profileId": "PG-ONPREM-1", "profileSha256": "1" * 64,
                 "workloadSha256": "2" * 64, "sourceCommit": "0" * 40}
        for kind in ("policy", "session", "C1.1"):
            body = self.policy_body if kind == "policy" else self.session_body if kind == "session" else self.gate_bodies[kind]
            for key, value in cases.items():
                with self.subTest(kind=kind, field=key):
                    original = body[key]
                    body[key] = value
                    self.rebuild()
                    self.fixture.requests.clear()
                    self.refused("authority-scope-mismatch")
                    self.assertEqual(len(self.fixture.requests), {"policy": 0, "session": 1, "C1.1": 2}[kind])
                    body[key] = original
            self.rebuild()

    def test_session_grant_principal_role_gate_action_and_limit_escalation_refuse(self):
        mutations = (
            ("principal", "github:user:1004"), ("roles", ["administrator"]),
            ("gates", ["C1.3"]), ("actions", ["capture"]),
        )
        for key, value in mutations:
            with self.subTest(field=key):
                original = self.session_body["reviewerGrants"][0][key]
                self.session_body["reviewerGrants"][0][key] = value
                self.rebuild()
                self.fixture.requests.clear()
                self.refused()
                self.assertEqual(len(self.fixture.requests), 1)
                self.session_body["reviewerGrants"][0][key] = original
        self.policy_body["maxStatusAgeSeconds"] = 11
        self.rebuild()
        self.fixture.requests.clear()
        self.refused("authority-limit-escalation")
        self.assertEqual(self.paths(), [])

    def test_known_principal_cannot_expand_supported_roles_gates_or_actions_withheld_by_policy(self):
        for field, permitted in (("roles", ["operations"]), ("gates", ["C1.1"]), ("actions", ["review-gate"])):
            with self.subTest(permission=field):
                original = self.policy_body["reviewerGrants"][0][field]
                self.policy_body["reviewerGrants"][0][field] = permitted
                self.rebuild()
                self.fixture.requests.clear()
                self.refused("authority-grant-escalation")
                self.assertEqual(self.paths(), [self.policy.review.locator.review_path])
                self.policy_body["reviewerGrants"][0][field] = original

    def test_gate_and_bundle_action_denial_precedes_their_review_requests(self):
        self.session_body["permittedActions"] = ["review-gate"]
        self.session_body["reviewerGrants"][0]["actions"] = ["review-gate"]
        self.rebuild()
        self.refused("authority-review-role-denied", gate_reviews=())
        self.assertEqual(len(self.fixture.requests), 2)
        self.session_body["permittedActions"] = ["review-bundle"]
        self.session_body["reviewerGrants"][0]["actions"] = ["review-bundle"]
        self.rebuild()
        self.fixture.requests.clear()
        self.refused("authority-review-role-denied")
        self.assertEqual(len(self.fixture.requests), 2)

    def test_reviewer_producer_overlap_including_root_session_and_owner_refuses(self):
        for producer in (ROOT_REVIEWER, SESSION_REVIEWER, tls.OWNER):
            with self.subTest(producer=producer):
                manifest = replace(self.manifest, producer_principals=frozenset({producer}))
                self.fixture.requests.clear()
                self.refused(gate_reviews=(), manifest=manifest)
                self.assertLessEqual(len(self.fixture.requests), 1)

    def test_non_owner_same_bundle_reviewer_refuses_even_with_two_verified_roles(self):
        other = "github:user:1004"
        self.policy_body["reviewerGrants"][0]["principal"] = other
        self.session_body["reviewerGrants"][0]["principal"] = other
        for tag in ("C1.1", "C1.2", "operations", "security"):
            self.review_principals[tag] = other
        self.rebuild()
        self.refused("bundle-distinct-reviewers-required", gate_reviews=())
        self.assertEqual(len(self.fixture.requests), 2)

    def test_edited_dismissed_deleted_unavailable_wrong_commit_or_principal_dependencies_refuse(self):
        for binding in (self.policy, self.session, self.requests[0].binding):
            path = binding.review.locator.review_path
            original = copy.deepcopy(self.fixture.responses[path])
            cases = ({"state": "DISMISSED"}, {"body": original["json"]["body"] + " "},
                     {"commit_id": "0" * 40}, {"id": 999},
                     {"user": {"id": 999, "type": "User"}}, {"pull_request_url": "https://invalid.example/"})
            for change in cases:
                with self.subTest(review=binding.review.locator.review_id, fields=tuple(change)):
                    self.fixture.responses[path] = {"json": {**original["json"], **change}}
                    self.fixture.requests.clear()
                    self.refused()
                    self.assertEqual(self.paths()[-1], path)
            for status in (301, 403, 404, 429, 500):
                self.fixture.responses[path] = {"status": status, "raw": tls.TOKEN.encode()}
                self.fixture.requests.clear()
                self.refused("review-http-refused")
                self.assertEqual(self.paths()[-1], path)
            self.fixture.responses[path] = original

    def test_closed_schema_unknown_fields_and_types_before_dependent_reviews(self):
        for kind in ("policy", "session", "C1.1"):
            body = self.policy_body if kind == "policy" else self.session_body if kind == "session" else self.gate_bodies[kind]
            for field, value in (("unknown", tls.TOKEN), ("schema", "unsupported/v99"), ("decision", "withdraw"),
                                 ("tenantId", True)):
                with self.subTest(kind=kind, field=field):
                    existed, original = field in body, body.get(field)
                    body[field] = value
                    self.rebuild()
                    self.fixture.requests.clear()
                    self.refused()
                    self.assertEqual(len(self.fixture.requests), {"policy": 0, "session": 1, "C1.1": 2}[kind])
                    if existed:
                        body[field] = original
                    else:
                        del body[field]
            self.rebuild()

    def test_missing_wrong_snapshot_refs_and_required_parent_gate_refuse(self):
        self.refused("authority-parent-review-required", requests=(self.requests[1],), gate=True)
        self.assertEqual(len(self.fixture.requests), 2)
        body = copy.deepcopy(self.gate_bodies["C1.2"])
        body["parents"] = []
        changed = replace(self.requests[1], binding=AuthorityBinding(self.binding("C1.2", body, 204), tls.REVIEWED))
        self.fixture.requests.clear()
        self.refused("authority-parent-gate-set-mismatch", requests=(self.requests[0], changed), gate=True)
        self.assertEqual(len(self.fixture.requests), 2)
        for key in ("policy", "session", "capture"):
            with self.subTest(reference=key):
                body = copy.deepcopy(self.gate_bodies["C1.1"])
                body[key]["sha256"] = "0" * 64
                changed = replace(self.requests[0], binding=AuthorityBinding(self.binding("C1.1", body, 203), tls.REVIEWED))
                self.fixture.requests.clear()
                self.refused("authority-parent-reference-mismatch", requests=(changed,), gate=True)
                self.assertEqual(len(self.fixture.requests), 2)

    def test_policy_dependency_cycle_refuses_before_https_and_no_partial_chain(self):
        self.policy_body["gateDependencies"]["C1.1"] = ["C1.2"]
        self.rebuild()
        self.refused("authority-dependency-cycle")
        self.assertEqual(self.paths(), [])

    def test_duplicate_roles_principals_permissions_refs_or_review_ids_refuse(self):
        self.session_body["reviewerGrants"].append(copy.deepcopy(self.session_body["reviewerGrants"][0]))
        self.rebuild()
        self.refused("authority-duplicate-principal")
        self.assertEqual(len(self.fixture.requests), 1)
        self.session_body["reviewerGrants"].pop()
        self.rebuild()
        self.fixture.requests.clear()
        self.refused("authority-gate-or-reference-reused", gate=True, requests=(self.requests[0], self.requests[0]))
        self.assertEqual(len(self.fixture.requests), 2)
        self.fixture.requests.clear()
        self.refused("two-distinct-reviews-on-one-pr-required", security=self.operations, gate_reviews=())
        self.assertEqual(len(self.fixture.requests), 2)

    def test_root_repository_issuer_and_dependency_budget_are_explicit(self):
        for root, code in ((replace(self.root, expected_issuer="https://api.github.com"), "authority-issuer-mismatch"),
                           (replace(self.root, repository="other-evidence"), "authority-repository-mismatch"),
                           (replace(self.root, max_dependencies=2), "authority-dependency-budget-exceeded")):
            with self.subTest(code=code):
                self.refused(code, context=replace(self.context, root=root))
                self.assertEqual(self.paths(), [])
        for changes in ({"policy_reviewers": frozenset()}, {"max_status_age_seconds": 0},
                        {"max_review_lag_seconds": float("inf")}, {"max_dependencies": True},
                        {"time_acquired_elapsed": -1}, {"request_limits": None}):
            with self.subTest(fields=tuple(changes)), self.assertRaises(GitHubAuthorityError):
                replace(self.root, **changes)

    def test_canonical_time_future_and_parent_order_lifetime_expiry_boundaries(self):
        for key, value in (("issuedAtUtc", "2026-10-07T12:00:00+00:00"),
                           ("issuedAtUtc", tls.wire_time(tls.NOW + timedelta(seconds=5))),
                           ("expiresAtUtc", tls.wire_time(tls.NOW)),
                           ("expiresAtUtc", tls.wire_time(tls.NOW + timedelta(seconds=4000)))):
            with self.subTest(field=key, value=value):
                original = self.policy_body[key]
                self.policy_body[key] = value
                self.rebuild()
                self.fixture.requests.clear()
                self.refused()
                self.assertEqual(self.paths(), [])
                self.policy_body[key] = original
        self.session_body["issuedAtUtc"] = tls.wire_time(tls.NOW - timedelta(seconds=176))
        self.rebuild()
        self.fixture.requests.clear()
        self.refused("authority-decision-order-or-lifetime-invalid")
        self.assertEqual(len(self.fixture.requests), 1)

    def test_submission_future_before_issue_and_excessive_lag_refuse_over_tls(self):
        path = self.policy.review.locator.review_path
        remote = self.fixture.responses[path]["json"]
        for seconds in (5, -181, -1):
            with self.subTest(seconds=seconds):
                remote["submitted_at"] = tls.wire_time(tls.NOW + timedelta(seconds=seconds))
                self.fixture.requests.clear()
                self.refused()
                self.assertEqual(self.paths(), [path])

    def test_session_window_and_capture_finish_order_are_enforced(self):
        invalid = replace(self.requests[0], facts=replace(self.requests[0].facts,
                          capture_finished_at_utc=tls.NOW - timedelta(seconds=150)))
        self.refused("authority-capture-window-denied", gate=True, requests=(invalid,))
        self.assertEqual(len(self.fixture.requests), 2)
        self.gate_bodies["C1.1"]["issuedAtUtc"] = tls.wire_time(tls.NOW - timedelta(seconds=91))
        self.rebuild()
        self.fixture.requests.clear()
        self.refused("authority-decision-order-or-lifetime-invalid", gate=True)
        self.assertEqual(len(self.fixture.requests), 2)

    def test_capture_collection_end_equality_refuses_and_just_before_passes_over_tls(self):
        collection_end = tls.NOW - timedelta(seconds=10)
        self.gate_bodies["C1.1"]["issuedAtUtc"] = tls.wire_time(collection_end)
        self.submissions["C1.1"] = -5
        self.rebuild()
        equality = replace(self.requests[0], facts=replace(self.requests[0].facts,
                           capture_finished_at_utc=collection_end))
        self.refused("authority-capture-window-denied", gate=True, requests=(equality,))
        self.assertEqual(len(self.fixture.requests), 2)
        self.fixture.requests.clear()
        before = replace(equality, facts=replace(equality.facts,
                         capture_finished_at_utc=collection_end - timedelta(microseconds=1)))
        result = self.gate_call(requests=(before,))
        self.assertEqual(result.authority[-1].gate, "C1.1")
        self.assertEqual(len(self.fixture.requests), 3)

    def test_capture_review_lag_is_bounded_from_actual_finish(self):
        self.policy_body["maxReviewLagSeconds"] = 10
        self.rebuild()
        self.refused("authority-capture-review-lag-exceeded", gate=True)
        self.assertEqual(len(self.fixture.requests), 3)

    def test_policy_expiry_equality_and_status_age_equality_use_same_trusted_anchor(self):
        self.policy_body["expiresAtUtc"] = tls.wire_time(tls.NOW + timedelta(seconds=2))
        self.rebuild()
        with patch.object(authority, "_clock", return_value=self.root.time_acquired_elapsed + 2):
            self.refused("authority-expired", gate=True)
        self.assertEqual(self.paths(), [])
        self.policy_body["expiresAtUtc"] = tls.wire_time(tls.NOW + timedelta(seconds=1200))
        self.rebuild()
        clock = [self.root.time_acquired_elapsed]
        original = tls.GitHubReviewClient.fetch_review

        def advancing(client, locator, **kwargs):
            raw = original(client, locator, **kwargs)
            if locator.review_id == 204:
                clock[0] += 10
            return raw

        with patch.object(tls.GitHubReviewClient, "fetch_review", advancing), \
                patch.object(authority, "_clock", side_effect=lambda: clock[0]), \
                patch.object(decisions, "_elapsed", side_effect=lambda: clock[0]):
            self.assertEqual(len(self.gate_call().authority), 4)

    def test_wrong_session_parent_ref_and_changed_retained_snapshot_are_refused(self):
        body = copy.deepcopy(self.session_body)
        body["policy"]["byteLength"] += 1
        session = AuthorityBinding(self.binding("session", body, 202), tls.REVIEWED)
        self.refused("authority-parent-reference-mismatch", context=replace(self.context, session=session))
        self.assertEqual(len(self.fixture.requests), 1)
        with self.assertRaises(approvals.GitHubApprovalError):
            replace(self.policy.review, retained_body=self.policy.review.retained_body + b" ")

    def test_missing_capture_manifest_producer_facts_and_mixed_producers_never_pass(self):
        self.refused("authority-manifest-and-two-reviews-required", manifest=None)
        self.assertEqual(self.paths(), [])
        with self.assertRaises(GitHubAuthorityError):
            replace(self.requests[0].facts, producer_principals=frozenset())
        changed = replace(self.requests[0], facts=replace(self.requests[0].facts,
                          producer_principals=frozenset({"github:user:1004"})))
        self.refused("authority-manifest-producer-mismatch", gate_reviews=(changed,))
        self.assertEqual(self.paths(), [])

    def test_generic_authenticated_body_is_immutable_and_application_neutral(self):
        observed = decisions.authenticate_review(client=self.client, token=tls.TOKEN,
                                                 binding=self.policy.exact(), expected_issuer=self.root.expected_issuer,
                                                 limits=self.root.request_limits)
        with self.assertRaises(TypeError):
            observed.body["decision"] = "grant"
        with self.assertRaises(TypeError):
            observed.body["reviewerGrants"][0]["principal"] = tls.PRODUCER
        self.assertFalse(hasattr(observed, "accepted_gate"))
        source = (tls.REPO_ROOT / "references/Hexalith.Platform/eng/hexalith_github_decisions.py").read_text()
        self.assertNotIn("access_telemetry", source)
        self.assertNotIn("PG-ONPREM", source)

    def test_generic_invalid_starting_elapsed_refuses_before_tls_retrieval(self):
        for invalid in (-1, float("nan"), float("inf"), True, 10**400):
            with self.subTest(value_type=type(invalid).__name__), \
                    patch.object(decisions, "_elapsed", return_value=invalid), \
                    self.assertRaisesRegex(decisions.GitHubDecisionError, "^decision-elapsed-invalid$"):
                decisions.authenticate_review(client=self.client, token=tls.TOKEN, binding=self.policy.exact(),
                                               expected_issuer=self.root.expected_issuer, limits=self.root.request_limits)
        self.assertEqual(self.paths(), [])

    def test_tls_trust_failure_at_root_cannot_contact_later_dependencies(self):
        client = tls.GitHubReviewClient(self.fixture.port, self.wrong_cert.read_text())
        self.refused("review-transport-unavailable", client=client)
        self.assertEqual(self.paths(), [])

    def test_earliest_policy_expiry_during_bundle_retrieval_refuses_entire_result(self):
        self.policy_body["expiresAtUtc"] = tls.wire_time(tls.NOW + timedelta(seconds=2))
        self.rebuild()
        clock = [self.root.time_acquired_elapsed]
        original = tls.GitHubReviewClient.fetch_review

        def advancing(client, locator, **kwargs):
            raw = original(client, locator, **kwargs)
            if locator.review_id == 206:
                clock[0] += 3
            return raw

        with patch.object(tls.GitHubReviewClient, "fetch_review", advancing), \
                patch.object(authority, "_clock", side_effect=lambda: clock[0]), \
                patch.object(decisions, "_elapsed", side_effect=lambda: clock[0]), \
                patch.object(approvals, "_elapsed", side_effect=lambda: clock[0]):
            self.refused("authority-expired")
        self.assertEqual(len(self.fixture.requests), 6)

    def test_gate_session_expiry_and_separate_review_end_equality_after_real_bundle_retrieval(self):
        original_fetch = tls.GitHubReviewClient.fetch_review
        original_gate_expiry = self.gate_bodies["C1.1"]["expiresAtUtc"]
        original_session_expiry = self.session_body["expiresAtUtc"]
        original_review_end = self.session_body["reviewEndsAtUtc"]
        boundary = tls.wire_time(tls.NOW + timedelta(seconds=2))
        for kind, code in (("gate", "authority-expired"), ("session", "authority-expired"),
                           ("review-window", "authority-session-window-denied")):
            with self.subTest(boundary=kind):
                self.gate_bodies["C1.1"]["expiresAtUtc"] = boundary if kind == "gate" else original_gate_expiry
                self.session_body["expiresAtUtc"] = boundary if kind == "session" else original_session_expiry
                self.session_body["reviewEndsAtUtc"] = boundary if kind != "gate" else original_review_end
                self.rebuild()
                self.fixture.requests.clear()
                clock = [self.root.time_acquired_elapsed]

                def advancing(client, locator, **kwargs):
                    raw = original_fetch(client, locator, **kwargs)
                    if locator.review_id == 206:
                        clock[0] += 2
                    return raw

                with patch.object(tls.GitHubReviewClient, "fetch_review", advancing), \
                        patch.object(authority, "_clock", side_effect=lambda: clock[0]), \
                        patch.object(decisions, "_elapsed", side_effect=lambda: clock[0]), \
                        patch.object(approvals, "_elapsed", side_effect=lambda: clock[0]):
                    self.refused(code)
                self.assertEqual(len(self.fixture.requests), 6)
                self.assertTrue(all(request["authenticated"] for request in self.fixture.requests))

    def test_first_policy_status_ages_across_bundle_latency_and_host_suspension(self):
        clock = [self.root.time_acquired_elapsed]
        bundle_started = [False]
        original = tls.GitHubReviewClient.fetch_review

        def advancing(client, locator, **kwargs):
            raw = original(client, locator, **kwargs)
            if locator.review_id == 206:
                clock[0] += 3
            return raw

        def bundle_clock():
            if not bundle_started[0]:
                bundle_started[0] = True
                clock[0] += 8
            return clock[0]

        with patch.object(tls.GitHubReviewClient, "fetch_review", advancing), \
                patch.object(authority, "_clock", side_effect=lambda: clock[0]), \
                patch.object(decisions, "_elapsed", side_effect=lambda: clock[0]), \
                patch.object(approvals, "_elapsed", side_effect=bundle_clock):
            self.refused("authority-status-stale")
        self.assertEqual(len(self.fixture.requests), 6)

    def test_reused_or_replaced_context_keeps_authenticated_time_anchor(self):
        self.gate_call()
        reused = replace(self.context, root=replace(self.root))
        self.assertEqual(reused.root.time_acquired_elapsed, self.root.time_acquired_elapsed)
        self.fixture.requests.clear()
        with patch.object(authority, "_clock", return_value=self.root.time_acquired_elapsed + 1200):
            self.refused("authority-expired", context=reused)
        self.assertEqual(self.paths(), [])

    def test_elapsed_regression_unsupported_clock_and_unavailable_time_refuse(self):
        with patch.object(authority, "_clock", return_value=self.root.time_acquired_elapsed - 1):
            self.refused("authority-elapsed-regression")
        with patch.object(authority, "_elapsed", return_value=float("nan")):
            self.refused("authority-time-invalid")
        with patch.object(tls.hexalith_github_reviews.time, "CLOCK_BOOTTIME", None):
            self.refused("authority-time-unavailable")
        self.assertEqual(self.paths(), [])
        clock = [self.root.time_acquired_elapsed]
        bundle_finished = [False]
        original = tls.GitHubReviewClient.fetch_review

        def advancing(client, locator, **kwargs):
            raw = original(client, locator, **kwargs)
            if locator.review_id == 206:
                clock[0] += 2
                bundle_finished[0] = True
            return raw

        # A final sample may regress relative to the bundle while remaining
        # above the earlier gate sample; compare across both consumer seams.
        with patch.object(tls.GitHubReviewClient, "fetch_review", advancing), \
                patch.object(authority, "_clock", side_effect=lambda: clock[0] - int(bundle_finished[0])), \
                patch.object(decisions, "_elapsed", side_effect=lambda: clock[0]), \
                patch.object(approvals, "_elapsed", side_effect=lambda: clock[0]):
            self.refused("authority-elapsed-regression")
        self.assertEqual(len(self.fixture.requests), 6)

    def test_strict_generic_envelopes_duplicate_bom_float_surrogate_and_unknown_fields(self):
        path = self.policy.review.locator.review_path
        original = tls.encode(self.fixture.responses[path]["json"])
        for raw in (b'{}{}', b'{"id":201,' + original[1:], b"\xef\xbb\xbf" + original,
                    original[:-1] + b',"extra":1}', original[:-1] + b',"node_id":1.5}',
                    original[:-1] + b',"node_id":"\\ud800"}', b'"nonobject"', b'\xff',
                    original[:-1] + b',"node_id":"' + b"x" * 4097 + b'"}', b"[" * 15 + b"0" + b"]" * 15):
            with self.subTest(digest=hashlib.sha256(raw).hexdigest()):
                self.fixture.responses[path] = {"raw": raw}
                self.fixture.requests.clear()
                self.refused()
                self.assertEqual(self.paths(), [path])

    def test_http_deadline_failure_late_in_chain_has_no_partial_result(self):
        path = self.requests[1].binding.review.locator.review_path
        self.fixture.responses[path]["header_delay"] = 1
        root = replace(self.root, request_limits=tls.RequestLimits(16_384, 0.35))
        self.refused("review-request-deadline-exceeded", context=replace(self.context, root=root), gate=True)
        self.assertEqual(self.paths()[-1], path)

    def test_content_and_credentials_are_absent_from_refusal_observations_and_process_output(self):
        output = io.StringIO()
        with redirect_stdout(output), redirect_stderr(output):
            self.fixture.responses[self.session.review.locator.review_path] = {"status": 403, "raw": tls.TOKEN.encode()}
            self.refused("review-http-refused")
        self.assertEqual(output.getvalue(), "")
        self.assertNotIn(tls.TOKEN, repr(self.root) + repr(self.context) + repr(self.client))

    def test_isolated_repository_imports_require_exact_pinned_platform_sources(self):
        code = "import sys; sys.path.insert(0, sys.argv[1]); import tools.access_telemetry_c1_github_authority"
        result = subprocess.run([sys.executable, "-I", "-c", code, str(tls.REPO_ROOT)], capture_output=True, text=True, timeout=5)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout + result.stderr, "")
        with tempfile.TemporaryDirectory(prefix="c1-authority-pinned-") as directory:
            root = Path(directory)
            (root / "tools").mkdir()
            for name in ("access_telemetry_c1_github_authority.py", "access_telemetry_c1_github_approvals.py",
                         "access_telemetry_c1_interchange.py", "access_telemetry_c1_approval_policy.py"):
                shutil.copyfile(tls.REPO_ROOT / "tools" / name, root / "tools" / name)
            result = subprocess.run([sys.executable, "-I", "-c", code, str(root)], capture_output=True, text=True, timeout=5)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("pinned-hexalith-github-decision-source-required", result.stderr)
            eng = root / "references/Hexalith.Platform/eng"
            eng.mkdir(parents=True)
            for name in ("hexalith_github_decisions.py", "hexalith_github_reviews.py"):
                shutil.copyfile(tls.REPO_ROOT / "references/Hexalith.Platform/eng" / name, eng / name)
            unrelated = root / "unrelated"
            unrelated.mkdir()
            (unrelated / "hexalith_github_decisions.py").write_text('marker = "unrelated"\n')
            spoof = """import sys
sys.path[:0] = [sys.argv[1], sys.argv[2]]
import hexalith_github_decisions
if sys.argv[3] == "spoof":
    hexalith_github_decisions.__file__ = sys.argv[4]
import tools.access_telemetry_c1_github_authority
"""
            for mode in ("cached", "spoof"):
                result = subprocess.run([sys.executable, "-I", "-c", spoof, str(root), str(unrelated), mode,
                                         str(eng / "hexalith_github_decisions.py")], capture_output=True, text=True, timeout=5)
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("pinned-hexalith-github-decision-origin-required", result.stderr)

            sibling_spoof = """import importlib, sys
sys.path[:0] = [sys.argv[1], sys.argv[2]]
dependency = importlib.import_module(sys.argv[3])
if sys.argv[4] == "spoof-file":
    dependency.__file__ = sys.argv[5]
if sys.argv[4] == "spoof-origin":
    dependency.__spec__.origin = sys.argv[5]
import tools.access_telemetry_c1_github_authority
"""
            for module_name, diagnostic in (("access_telemetry_c1_github_approvals", "pinned-c1-github-approvals-origin-required"),
                                            ("access_telemetry_c1_approval_policy", "pinned-c1-principal-policy-origin-required"),
                                            ("access_telemetry_c1_interchange", "pinned-c1-wire-origin-required")):
                (unrelated / (module_name + ".py")).write_text('marker = "unrelated"\n')
                for mode in ("cached", "spoof-file", "spoof-origin"):
                    with self.subTest(dependency=module_name, mode=mode):
                        result = subprocess.run([sys.executable, "-I", "-c", sibling_spoof, str(root), str(unrelated),
                                                 module_name, mode, str(root / "tools" / (module_name + ".py"))],
                                                capture_output=True, text=True, timeout=5)
                        self.assertNotEqual(result.returncode, 0)
                        self.assertIn(diagnostic, result.stderr)


if __name__ == "__main__":
    unittest.main()
