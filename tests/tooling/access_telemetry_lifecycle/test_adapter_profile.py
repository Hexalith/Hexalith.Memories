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
                "PROFILE_ID": "profile-27-3",
                "EVIDENCE_ROOT": "_bmad-output/implementation-artifacts/tests",
                "DECLARED_SINGLE_COMPONENT_FAULT": "dapr-sidecar-restart",
            }
        )

        self.assertEqual("jpiquot@local", identity.kube_context)
        self.assertEqual("hexalith-memories", identity.kube_namespace)
        self.assertEqual("deployment-27-3", identity.deployment_id)
        self.assertEqual("profile-27-3", identity.profile_id)
        self.assertEqual("dapr-sidecar-restart", identity.declared_single_component_fault)

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


if __name__ == "__main__":
    unittest.main()
