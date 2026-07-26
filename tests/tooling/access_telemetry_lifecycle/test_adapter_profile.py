import importlib
import json
import os
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO_ROOT / "tools"))


adapter_profile = importlib.import_module("verify_access_telemetry_lifecycle")


class AdapterProfileTests(unittest.TestCase):
    def test_environment_identity_is_complete(self):
        identity = adapter_profile.EnvironmentIdentity.from_mapping(
            {
                "KUBE_CONTEXT": "jpiquot@local",
                "KUBE_NAMESPACE": "hexalith-memories",
                "DEPLOYMENT_ID": "deployment-27-3",
                "PROFILE_ID": adapter_profile.EXPECTED_PROFILE_ID,
                "EVIDENCE_ROOT": "_bmad-output/implementation-artifacts/tests",
                "DECLARED_SINGLE_COMPONENT_FAULT": "postgresql-pod-replacement",
            }
        )

        self.assertEqual("jpiquot@local", identity.kube_context)
        self.assertEqual("hexalith-memories", identity.kube_namespace)
        self.assertEqual("deployment-27-3", identity.deployment_id)
        self.assertEqual(adapter_profile.EXPECTED_PROFILE_ID, identity.profile_id)
        self.assertEqual("postgresql-pod-replacement", identity.declared_single_component_fault)
        self.assertEqual("jpiquot@local", adapter_profile.EXPECTED_KUBE_CONTEXT)
        self.assertEqual("hexalith-memories", adapter_profile.EXPECTED_KUBE_NAMESPACE)
        self.assertIn("postgres:18.4-trixie@sha256:", adapter_profile.EXPECTED_POSTGRESQL_IMAGE)

    def test_capacity_inputs_fail_closed(self):
        invalid_inputs = (
            {"records": 1.5, "record_bytes": 100, "index_bytes": 20, "durability_multiplier": 2},
            {"records": -1, "record_bytes": 100, "index_bytes": 20, "durability_multiplier": 2},
            {"records": "NaN", "record_bytes": 100, "index_bytes": 20, "durability_multiplier": 2},
            {"records": 1, "record_bytes": "1KiB", "index_bytes": "1KB", "durability_multiplier": 2},
        )

        for values in invalid_inputs:
            with self.subTest(values=values):
                with self.assertRaises(adapter_profile.CapacityInputError):
                    adapter_profile.calculate_capacity(
                        records=values["records"],
                        measured_record_bytes=values["record_bytes"],
                        measured_index_bytes=values["index_bytes"],
                        durability_multiplier=values["durability_multiplier"],
                        control_bytes=0,
                        reclamation_workspace=0,
                    )

    def test_workload_matches_adr_operation_envelope(self):
        envelope = adapter_profile.ADR_TWO_WRITER_WORKLOAD

        self.assertEqual(2, envelope.writer_count)
        self.assertEqual(500, envelope.total_events_per_second)
        self.assertEqual(
            {
                "search": 200,
                "ingest": 6,
                "traverse": 10,
                "case_access": 16,
                "delete": 2,
                "tenant_lifecycle": 0.2,
                "tenant_config": 0.8,
                "case_member": 4,
                "annotation": 11,
            },
            envelope.events_per_second_per_writer,
        )

    def test_profile_and_mutation_manifest_are_immutable(self):
        profile = adapter_profile.AdapterProfile(
            identity={"profileId": "profile-27-3", "runtime": "dapr-1.18.1"},
            capabilities={"strongReads": True, "transactionRollback": False},
            workload=adapter_profile.ADR_TWO_WRITER_WORKLOAD.to_dict(),
        )

        first = profile.manifest()
        second = profile.manifest()

        self.assertEqual(first, second)
        self.assertEqual(first["profile_sha256"], second["profile_sha256"])
        self.assertEqual(first["mutation_manifest_sha256"], second["mutation_manifest_sha256"])
        self.assertEqual(first["canonical_profile"], json.loads(first["canonical_profile_json"]))


    def test_capacity_result_is_admitted_against_profile_thresholds(self):
        """A computed capacity result must be judged against the approved 70/80/90% table.

        Added 2026-07-26 by code review (chunk 3b). The pre-existing
        `test_capacity_inputs_fail_closed` only proves that bad *inputs* raise;
        nothing proved that a well-formed but over-capacity *result* is
        rejected, so gate C1.13 had no producer that could fail.
        """

        def required(total_bytes):
            return [adapter_profile.CapacityRequirement("24h", 1, total_bytes)]

        # Well under the 70% steady-state threshold -> admitted.
        admitted = adapter_profile.evaluate_capacity_admission(
            required(adapter_profile.STEADY_STATE_CAPACITY_BYTES)
        )
        self.assertTrue(admitted[0].admitted)
        self.assertEqual("steady-state", admitted[0].band)

        # One byte over steady state -> rejected.
        over = adapter_profile.evaluate_capacity_admission(
            required(adapter_profile.STEADY_STATE_CAPACITY_BYTES + 1)
        )
        self.assertFalse(over[0].admitted)
        self.assertEqual("above-steady-state", over[0].band)

        # Exactly 80% is critical, not an admissible reclamation peak.
        critical = adapter_profile.evaluate_capacity_admission(
            required(adapter_profile.CRITICAL_CAPACITY_BYTES)
        )
        self.assertFalse(critical[0].admitted)
        self.assertEqual("critical", critical[0].band)

        # Above 90% -> unhealthy.
        unhealthy = adapter_profile.evaluate_capacity_admission(
            required(adapter_profile.UNHEALTHY_CAPACITY_BYTES + 1)
        )
        self.assertFalse(unhealthy[0].admitted)
        self.assertEqual("unhealthy", unhealthy[0].band)

        # A 100% "fits the 400 GiB profile" result is NOT admissible.
        full = adapter_profile.evaluate_capacity_admission(
            required(adapter_profile.PROFILE_CAPACITY_BYTES)
        )
        self.assertFalse(full[0].admitted)

        # A zero requirement is not a proof of fit.
        zero = adapter_profile.evaluate_capacity_admission(required(0))
        self.assertFalse(zero[0].admitted)
        self.assertEqual("invalid", zero[0].band)

        # The 7-day software maximum is measured but never admitted.
        seven_day = adapter_profile.evaluate_capacity_admission(
            [adapter_profile.CapacityRequirement("7d", 1, 1024)]
        )
        self.assertFalse(seven_day[0].admitted)
        self.assertEqual("out-of-profile", seven_day[0].band)

    def test_canonical_pg_onprem_profile_hash_is_pinned(self):
        """The reviewed PG-ONPREM-1 profile must hash to a pinned value.

        Added 2026-07-26 by code review (chunk 3b). The pre-existing
        `test_profile_and_mutation_manifest_are_immutable` hashes a synthetic
        profile twice and asserts the two results are equal, which holds under
        any canonicalization or profile change. This case pins the real
        profile, so a field change or a serialization change fails here.
        """

        manifest = adapter_profile.canonical_pg_onprem_profile().manifest()

        self.assertEqual(
            "dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14",
            manifest["profile_sha256"],
        )
        self.assertEqual(
            "2983ccdebedbd12e34bb1aec363335eb825301ce92d1c4ed87f8956d9c176b84",
            manifest["mutation_manifest_sha256"],
        )
        self.assertEqual([], json.loads(manifest["canonical_profile_json"]).get("allowed_mutations", []))
        self.assertEqual(
            adapter_profile.EXPECTED_PROFILE_ID,
            manifest["canonical_profile"]["identity"]["profileId"],
        )
        # The approved profile pins maxConns 40, not the ADR's stale 64.
        self.assertEqual("40", manifest["canonical_profile"]["identity"]["maxConns"])


if __name__ == "__main__":
    unittest.main()
