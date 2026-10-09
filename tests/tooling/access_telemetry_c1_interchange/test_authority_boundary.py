"""Offline I3 refusals: shape, hashes and fixture reviews grant no authority."""

from copy import deepcopy
import hashlib
import os
from pathlib import Path
import socket
import subprocess
import sys
import unittest
from unittest.mock import patch

from test_artifact_readers import SCOPE, blocked_fixture, capture_fixture, fixtures, snapshot


ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))

from access_telemetry_c1_authority_boundary import (  # noqa: E402
    C1AuthorityUnavailableError, require_authenticated_disposition,
)
from access_telemetry_c1_interchange import EvidenceReference, JsonSnapshot  # noqa: E402


def reference(path, retained):
    return EvidenceReference(path, hashlib.sha256(retained.raw).hexdigest(), len(retained.raw))


class AuthorityBoundaryTests(unittest.TestCase):
    def setUp(self):
        capture_value = capture_fixture()
        self.capture = snapshot(capture_value)
        self.capture_reference = reference("fixtures/capture.json", self.capture)
        self.disposition_value = deepcopy(fixtures()[1][1])
        self.disposition_value["targetSha256"] = capture_value["targetSha256"]
        self.disposition_value["capture"] = {
            "path": self.capture_reference.path,
            "sha256": self.capture_reference.sha256,
            "byteLength": self.capture_reference.byte_length,
        }
        self.scope = {
            "profileSha256": SCOPE["profileSha256"], "workloadSha256": SCOPE["workloadSha256"],
            "targetSha256": capture_value["targetSha256"],
            "qualificationSessionId": SCOPE["qualificationSessionId"],
            "sourceCommit": capture_value["sourceCommit"],
        }

    def refuse(self, code, *, value=None, capture=None, capture_reference=None, scope=None):
        with self.assertRaises(C1AuthorityUnavailableError) as caught:
            require_authenticated_disposition(
                disposition=snapshot(self.disposition_value if value is None else value),
                capture=self.capture if capture is None else capture,
                capture_reference=self.capture_reference if capture_reference is None else capture_reference,
                scope=self.scope if scope is None else scope,
            )
        self.assertEqual(code, str(caught.exception))

    def test_matching_claim_and_fixture_receipt_refuse_without_provider_or_target_calls(self):
        with (patch("socket.create_connection", side_effect=AssertionError("network called")),
              patch.object(socket.socket, "connect", side_effect=AssertionError("network called")),
              patch("subprocess.Popen", side_effect=AssertionError("target called")),
              patch.object(subprocess, "run", side_effect=AssertionError("target called")),
              patch.object(os, "system", side_effect=AssertionError("target called"))):
            self.refuse("authority-prerequisites-unapproved")

    def test_forged_issuer_digest_and_reviewer_labels_remain_unapproved(self):
        for field, changed in (
            ("reviewerPrincipal", "github:user:999"),
            ("reviewerRole", "security"),
            ("authorityReceipt", {"uri": "https://fixture.invalid/forged", "sha256": "f" * 64,
                                  "issuer": "claimed-trusted-issuer", "decisionId": "claimed-decision"}),
        ):
            with self.subTest(field=field):
                value = deepcopy(self.disposition_value)
                value[field] = changed
                self.refuse("authority-prerequisites-unapproved", value=value)

    def test_capture_bytes_and_reference_must_match(self):
        altered = JsonSnapshot(self.capture.raw.replace(b"2026-10-08T10:00:00.0000001+00:00",
                                                        b"2026-10-08T10:00:03.0000001+00:00", 1))
        self.refuse("capture-reference-digest-mismatch", capture=altered)
        value = deepcopy(self.disposition_value)
        value["capture"]["sha256"] = "0" * 64
        self.refuse("authority-capture-or-gate-mismatch", value=value)

    def test_scope_and_gate_mismatch_refuse_before_authority(self):
        for field, changed in (("gate", "C1.14"), ("targetSha256", "2" * 64),
                               ("qualificationSessionId", "different-session")):
            with self.subTest(field=field):
                value = deepcopy(self.disposition_value)
                value[field] = changed
                self.refuse("authority-capture-or-gate-mismatch" if field == "gate"
                            else "authority-scope-mismatch", value=value)

    def test_negative_decision_and_self_review_refuse(self):
        value = deepcopy(self.disposition_value)
        value["decision"] = "needs-evidence"
        self.refuse("authority-decision-not-accepted", value=value)
        value = deepcopy(self.disposition_value)
        value["reviewerPrincipal"] = value["producerPrincipal"]
        self.refuse("disposition-artifact-accepted-independence-contradiction", value=value)

    def test_blocked_capture_and_review_before_finish_refuse(self):
        blocked = snapshot(blocked_fixture())
        blocked_reference = reference("fixtures/blocked.json", blocked)
        value = deepcopy(self.disposition_value)
        value["capture"] = {"path": blocked_reference.path, "sha256": blocked_reference.sha256,
                            "byteLength": blocked_reference.byte_length}
        self.refuse("authority-capture-not-eligible", value=value, capture=blocked,
                    capture_reference=blocked_reference)
        value = deepcopy(self.disposition_value)
        value["decidedAtUtc"] = "2026-10-08T10:00:00.500Z"
        self.refuse("authority-review-before-capture-finish", value=value)

    def test_unpinned_profile_and_missing_scope_refuse(self):
        self.refuse("authority-current-pg2-identity-required",
                    scope={**self.scope, "profileSha256": "a" * 64})
        missing = dict(self.scope)
        del missing["sourceCommit"]
        self.refuse("authority-boundary-input-invalid", scope=missing)

    def test_unavailable_scope_and_malformed_disposition_refuse(self):
        self.refuse("authority-boundary-input-invalid", scope=object())
        value = deepcopy(self.disposition_value)
        value["authorityReceipt"] = {"uri": "https://fixture.invalid/review", "sha256": "c" * 64}
        self.refuse("disposition-artifact-field-set", value=value)


if __name__ == "__main__":
    unittest.main()
