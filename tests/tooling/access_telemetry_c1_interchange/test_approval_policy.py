"""Owner-role policy fixtures; no fixture authenticates or accepts a C1 gate."""

from pathlib import Path
import socket
import subprocess
import sys
import unittest
from unittest.mock import patch


REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO_ROOT / "tools"))

from access_telemetry_c1_approval_policy import (  # noqa: E402
    BundlePrincipalPolicyError,
    MAX_BUNDLE_PRODUCERS,
    OWNER_GITHUB_PRINCIPAL,
    validate_bundle_principal_separation,
)


OPERATIONS = "github:user:1001"
SECURITY = "github:user:1002"
PRODUCERS = frozenset({"github:user:1003", "github:user:1004"})


class BundlePrincipalPolicyTests(unittest.TestCase):
    def test_two_different_reviewers_preserve_existing_separation(self):
        self.assertIsNone(validate_bundle_principal_separation(OPERATIONS, SECURITY, PRODUCERS))

    def test_named_owner_can_supply_both_roles(self):
        self.assertEqual(OWNER_GITHUB_PRINCIPAL, "github:user:6775094")
        self.assertIsNone(validate_bundle_principal_separation(
            OWNER_GITHUB_PRINCIPAL, OWNER_GITHUB_PRINCIPAL, PRODUCERS))

    def test_owner_can_supply_either_role_alongside_another_reviewer(self):
        for reviewers in ((OWNER_GITHUB_PRINCIPAL, SECURITY), (OPERATIONS, OWNER_GITHUB_PRINCIPAL)):
            with self.subTest(reviewers=reviewers):
                validate_bundle_principal_separation(*reviewers, PRODUCERS)

    def test_another_account_cannot_supply_both_roles(self):
        for principal in (OPERATIONS, SECURITY, "github:user:6775095"):
            with self.subTest(principal=principal), self.assertRaisesRegex(
                BundlePrincipalPolicyError, "^bundle-distinct-reviewers-required$"):
                validate_bundle_principal_separation(principal, principal, PRODUCERS)

    def test_either_review_role_cannot_self_approve_any_capture(self):
        for producer in PRODUCERS:
            for reviewers in ((producer, SECURITY), (OPERATIONS, producer)):
                with self.subTest(reviewers=reviewers), self.assertRaisesRegex(
                    BundlePrincipalPolicyError, "^bundle-producer-self-approval$"):
                    validate_bundle_principal_separation(*reviewers, PRODUCERS)

    def test_owner_exception_does_not_allow_producer_self_approval(self):
        producers = PRODUCERS | {OWNER_GITHUB_PRINCIPAL}
        for reviewers in ((OWNER_GITHUB_PRINCIPAL, OWNER_GITHUB_PRINCIPAL),
                          (OWNER_GITHUB_PRINCIPAL, SECURITY), (OPERATIONS, OWNER_GITHUB_PRINCIPAL)):
            with self.subTest(reviewers=reviewers), self.assertRaisesRegex(
                BundlePrincipalPolicyError, "^bundle-producer-self-approval$"):
                validate_bundle_principal_separation(*reviewers, producers)

    def test_missing_or_mutable_producer_sets_refuse(self):
        for producers in (None, frozenset(), set(PRODUCERS), list(PRODUCERS), tuple(PRODUCERS), True):
            with self.subTest(kind=type(producers).__name__), self.assertRaisesRegex(
                BundlePrincipalPolicyError, "^producer-principals-required$"):
                validate_bundle_principal_separation(OPERATIONS, SECURITY, producers)

    def test_alias_display_name_and_malformed_principals_refuse_everywhere(self):
        values = ("jpiquot", "Jérôme Piquot", "github:login:jpiquot", "github:user:06775094",
                  "github:user:0", "github:user:-1", "github:user:6775094\n", "GITHUB:user:6775094",
                  "github:user:9223372036854775808", "github:user:" + "1" * 500, "", None, True, 6775094)
        for value in values:
            for position in range(3):
                arguments = [OPERATIONS, SECURITY, PRODUCERS]
                arguments[position] = frozenset({value}) if position == 2 else value
                with self.subTest(value=value, position=position), self.assertRaisesRegex(
                    BundlePrincipalPolicyError, "^principal-format-invalid$"):
                    validate_bundle_principal_separation(*arguments)

    def test_canonical_numeric_identity_boundary_is_valid(self):
        validate_bundle_principal_separation("github:user:1", "github:user:9223372036854775807", PRODUCERS)

    def test_all_25_gate_producers_fit_but_excess_principals_refuse(self):
        producers = frozenset(f"github:user:{2000 + number}" for number in range(MAX_BUNDLE_PRODUCERS))
        validate_bundle_principal_separation(OPERATIONS, SECURITY, producers)
        with self.assertRaisesRegex(BundlePrincipalPolicyError, "^producer-principals-required$"):
            validate_bundle_principal_separation(OPERATIONS, SECURITY, producers | {"github:user:3000"})

    def test_refusal_diagnostics_do_not_echo_credential_canaries(self):
        secret = "bearer owner-secret-canary"
        with self.assertRaises(BundlePrincipalPolicyError) as caught:
            validate_bundle_principal_separation(secret, SECURITY, PRODUCERS)
        self.assertEqual(str(caught.exception), "principal-format-invalid")
        self.assertNotIn(secret, str(caught.exception))

    def test_separation_predicate_does_not_call_authority_or_target_dependencies(self):
        def forbidden(*args, **kwargs):
            raise AssertionError("pure separation predicate called a dependency")

        with patch("builtins.open", side_effect=forbidden), patch.object(socket, "socket", side_effect=forbidden), \
                patch.object(subprocess, "run", side_effect=forbidden), patch.object(subprocess, "Popen", side_effect=forbidden):
            validate_bundle_principal_separation(OWNER_GITHUB_PRINCIPAL, OWNER_GITHUB_PRINCIPAL, PRODUCERS)
            with self.assertRaises(BundlePrincipalPolicyError):
                validate_bundle_principal_separation(OWNER_GITHUB_PRINCIPAL, OWNER_GITHUB_PRINCIPAL,
                                                    PRODUCERS | {OWNER_GITHUB_PRINCIPAL})


if __name__ == "__main__":
    unittest.main()
