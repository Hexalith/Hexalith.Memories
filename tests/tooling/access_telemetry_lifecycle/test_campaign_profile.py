"""Real profile/consumer/file paths; simulated capacity earns no campaign credit."""
from copy import deepcopy
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
PLATFORM = ROOT / "references/Hexalith.Platform/eng/helm-qualification"
sys.path.insert(0, str(ROOT / "tools"))
sys.path.insert(0, str(PLATFORM))
import campaign_capacity as capacity
from test_campaign_capacity import bound_fixture
from test_owner_authority import OwnerFixture, technical
import access_telemetry_campaign_profile as profiles
import access_telemetry_c1_gate_semantics as gates
import access_telemetry_producer_common as producer
import verify_access_telemetry_lifecycle as lifecycle


class CampaignProfileTests(unittest.TestCase):
    def setUp(self):
        # These tests isolate existing capacity/authority semantics. The lifecycle
        # handoff itself is exercised with the real Builds reader in its own suite.
        boundary = patch.object(profiles, "validate_campaign_environment_binding", return_value={"qualified": False})
        boundary.start(); self.addCleanup(boundary.stop)
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        capacity_root = self.root / "capacity"; capacity_root.mkdir()
        self.profile, self.now, self.write = bound_fixture(capacity_root)
        outer = self.write("profile.json", self.profile)
        self.env = {"HEXALITH_CAMPAIGN_PROFILE": outer["path"],
                    "HEXALITH_CAMPAIGN_APPROVED_PROFILE_SHA256": outer["sha256"],
                    "HEXALITH_C1_REPOSITORY_ROOT": str(ROOT)}
        current = self.write("current.json", {"profileSha256": capacity.digest(self.profile),
                                             "compute": self.profile["compute"], "storage": self.profile["storage"]})
        self.env.update(HEXALITH_CAMPAIGN_CAPACITY_RECEIPT=current["path"], HEXALITH_CAMPAIGN_APPROVED_CAPACITY_SHA256=current["sha256"])
        with patch.dict(os.environ, self.env):
            self.sha = profiles.active_profile_sha256(ROOT)

    def facts(self, number):
        value = technical(number)
        if number == 13:
            return {"semantics": "finite-campaign/v1", "profileSha256": self.sha,
                    "requirements": self.profile["sizing"], "cohortRecords": {"1h": 125, "24h": 125, "168h": 125},
                    "continuousSevenDayCredit": False, "historical400GiBCredit": False}
        if number == 15:
            value.update(runtimeImage="ghcr.io/dapr/daprd:1.18.7@sha256:" + "a" * 64, runtimeVersion="1.18.7")
        if number == 17:
            value["profileSha256"] = self.sha
        if number == 18:
            value.update(reservedBytes=self.profile["sizing"]["primaryBytes"], availableBytes=self.profile["sizing"]["primaryBytes"],
                         requestedBytes=self.profile["sizing"]["primaryBytes"], cpuCount=17, memoryBytes=31 * capacity.GIB)
        return value

    def owner_fixture(self):
        root = self.root / "owner"; root.mkdir()
        return OwnerFixture(root, now=datetime.fromtimestamp(self.now / 1000, timezone.utc),
                            profile_sha256=self.sha, profile_id=capacity.PROFILE_ID, technical_factory=self.facts,
                            target_override={"context": self.profile["target"]["context"], "namespace": self.profile["target"]["namespace"]},
                            campaign_schedule=self.profile["renewalSchedule"], campaign_origin_ms=self.now - 48 * 3600000,
                            renewal_round=self.profile["renewalSchedule"]["rounds"][1])

    def test_historical_identity_and_seven_day_exclusion_remain_separate(self):
        self.assertEqual(lifecycle.CURRENT_PROFILE_SHA256, lifecycle.canonical_pg_onprem_2_profile().manifest()["profile_sha256"])
        self.assertNotEqual(self.sha, lifecycle.CURRENT_PROFILE_SHA256)
        with patch.dict(os.environ, self.env):
            self.assertEqual(capacity.PROFILE_ID, profiles.active_profile_id(ROOT))
            lifecycle.validate_current_profile_inputs(ROOT, launch_admission=True)
        old = lifecycle.evaluate_capacity_admission([lifecycle.CapacityRequirement("7d", 125, 1)])
        self.assertFalse(old[0].admitted)
        self.assertEqual(137438953472, lifecycle.RECLAMATION_WORKSPACE_FLOOR_BYTES)

    def test_unbound_missing_pin_tampered_or_changed_input_refuses(self):
        for change in ({"HEXALITH_CAMPAIGN_APPROVED_PROFILE_SHA256": ""}, {"HEXALITH_CAMPAIGN_APPROVED_PROFILE_SHA256": "a" * 64}):
            with patch.dict(os.environ, {**self.env, **change}), self.assertRaises(lifecycle.EvidenceValidationError):
                profiles.active_profile_sha256(ROOT)
        Path(self.profile["inputs"]["components"][0]["path"]).write_bytes(b'{}')
        with patch.dict(os.environ, self.env), self.assertRaises(lifecycle.EvidenceValidationError):
            profiles.active_profile_sha256(ROOT)

    def test_finite_c1_13_recomputes_exact_inventory_and_confers_no_continuous_credit(self):
        with patch.dict(os.environ, self.env):
            value = self.facts(13)
            gates.validate_gate_technical(13, value, approved={}, run={"profileSha256": self.sha}, capture_interval=(1, 2))
            for changed in ({**value, "cohortRecords": {"1h": 125, "24h": 125, "168h": 126}},
                            {**value, "continuousSevenDayCredit": True}, {**value, "historical400GiBCredit": True},
                            technical(13)):
                with self.assertRaises((ValueError, KeyError)):
                    gates.validate_gate_technical(13, changed, approved={}, run={"profileSha256": self.sha}, capture_interval=(1, 2))
            evidence = lifecycle.collect_capacity_evidence()
            self.assertEqual("finite-campaign/v1", evidence["semantics"])
            self.assertEqual(self.sha, evidence["profile_sha256"])

    def test_all_25_actual_owner_packets_support_the_explicit_new_profile(self):
        with patch.dict(os.environ, self.env):
            fixture = self.owner_fixture()
            result = fixture.verify()
            self.assertEqual("fixture-session", result.session_id)
            self.assertEqual(25, len(fixture.gates))
            fixture.gates["C1.13"]["technical"]["requirements"]["cumulativeRecords"] -= 1
            fixture.rebind()
            with self.assertRaises(lifecycle.EvidenceValidationError):
                fixture.verify()

    def test_actual_authority_requires_the_bound_round_and_unmoved_capture_windows(self):
        with patch.dict(os.environ, self.env):
            fixture = self.owner_fixture()
            self.assertEqual("fixture-session", fixture.verify().session_id)
            for field, replacement in (("renewalRound", "round-0"), ("campaignOriginUtcMs", fixture.plan["campaignOriginUtcMs"] - 1000)):
                before = fixture.plan[field]; fixture.plan[field] = replacement; fixture.approve()
                with self.assertRaises(lifecycle.EvidenceValidationError):
                    fixture.verify()
                fixture.plan[field] = before

    def test_historical_authority_cannot_qualify_selected_finite_profile(self):
        root = self.root / "old-owner"; root.mkdir()
        fixture = OwnerFixture(root)
        with patch.dict(os.environ, self.env), self.assertRaises(lifecycle.EvidenceValidationError):
            fixture.verify()

    def test_compute_gate_requires_aggregate_allocation(self):
        with patch.dict(os.environ, self.env):
            facts = self.facts(18)
            approved = dict(nodeIdentity="node", storageIdentity="volume")
            for key, value in (("cpuCount", 16), ("memoryBytes", 30 * capacity.GIB), ("reservedBytes", 24 * capacity.GIB), ("requestedBytes", 400 * capacity.GIB)):
                candidate = {**facts, key: value}
                with self.subTest(key=key), self.assertRaises(lifecycle.EvidenceValidationError):
                    gates.validate_gate_technical(18, candidate, approved=approved, run={}, capture_interval=(0, 2))

    def test_missing_fresh_capacity_refuses_before_producer_or_target_operations(self):
        environment = {**self.env, "HEXALITH_CAMPAIGN_CAPACITY_RECEIPT": ""}
        with patch.dict(os.environ, environment), patch.object(lifecycle, "_run_bounded_process") as child:
            result = lifecycle.run_story_27_4_producer_checkpoint(checkpoint="c2-production-replacement",
                scenario_input_path=self.root / "absent-scenario.json", predecessor_path=self.root / "absent-predecessor.json",
                evidence_path=self.root / "evidence.json", owner="owner", repository_root=ROOT, evidence_root=self.root)
            self.assertEqual(1, result)
            child.assert_not_called()
            with self.assertRaises(lifecycle.EvidenceValidationError):
                lifecycle.validate_current_profile_inputs(ROOT, launch_admission=True)

    def test_frozen_cli_selection_loads_bound_platform_inputs_and_refuses_foreign_target(self):
        frozen = self.root / "frozen"; frozen.mkdir()
        sources = {"tools/access_telemetry_c2_producer.py", "tools/access_telemetry_producer_common.py", "tools/verify_access_telemetry_lifecycle.py", *lifecycle.OWNER_AUTHORITY_SOURCES}
        for relative in sources:
            (frozen / Path(relative).name).write_bytes((ROOT / relative).read_bytes())
        target = dict(kind="non-production-qualification", kube_context=self.profile["target"]["context"],
                      namespace=self.profile["target"]["namespace"], profile_sha256=self.sha)
        scenario = self.write("scenario.json", {"schema_version": 1, "target": target})
        command = [sys.executable, "-B", "-c", "from pathlib import Path; from access_telemetry_producer_common import _load_target; import sys; print(_load_target(Path(sys.argv[1]))['profile_sha256'])", scenario["path"]]
        environment = {**os.environ, **self.env, "PYTHONPATH": str(frozen)}
        result = subprocess.run(command, cwd=ROOT, env=environment, capture_output=True, text=True, timeout=10)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(self.sha, result.stdout.strip())
        target["kube_context"] = "kind-foreign"
        self.write("scenario.json", {"schema_version": 1, "target": target})
        result = subprocess.run(command, cwd=ROOT, env=environment, capture_output=True, text=True, timeout=10)
        self.assertNotEqual(0, result.returncode)
        self.assertIn("bound finite profile", result.stderr)

    def test_values_profile_descriptor_and_actual_target_readback_have_no_hash_cycle(self):
        from test_retention_verification import install_fake_kubectl
        with patch.dict(os.environ, self.env):
            original_sha = profiles.active_profile_sha256(ROOT)
            values = json.loads(Path(self.profile["inputs"]["values"][0]["path"]).read_bytes())
            reference = values["bindings"]["memories/server"]["configuration"]["AccessTelemetryLifecycle:ComponentProfileHash"]
            self.assertEqual({"configMapKeyRef": {"name": "access-telemetry-campaign-identity", "key": "profileSha256"}}, reference)
            self.assertNotIn(original_sha, json.dumps(values))
            binding = capacity.runtime_binding(self.profile, original_sha)
            self.assertEqual(original_sha, profiles.validate_campaign_runtime_binding(binding)["profileSha256"])
            self.assertEqual(original_sha, profiles.active_profile_sha256(ROOT))
            fake = self.root / "bin"; fake.mkdir(); install_fake_kubectl(fake)
            script = fake / "kubectl"
            base = script.read_text().replace(lifecycle.STORY_27_4_PROFILE_SHA256, self.sha)
            base = base.replace(lifecycle.EXPECTED_RUNTIME_IMAGE, "ghcr.io/dapr/daprd:1.18.7@sha256:" + "a" * 64)
            base = base.replace(lifecycle.EXPECTED_RUNTIME_LINUX_AMD64_MANIFEST, "sha256:" + "b" * 64)
            target = dict(kind="non-production-qualification", kube_context=self.profile["target"]["context"],
                          namespace=self.profile["target"]["namespace"], profile_sha256=self.sha,
                          _lease_holder="fixture", _platform_operations_reviewer="owner-6775094")
            environment = {**self.env, "PATH": str(fake) + os.pathsep + os.environ.get("PATH", ""),
                           "HEXALITH_STORY_27_4_HOST_CADENCE_QUANTUM": "0",
                           "QUALIFICATION_DAPR_DIGEST": "a" * 64,
                           "HEXALITH_STORY_27_4_INLINE_KUBECTL": str(script), "HEXALITH_STORY_27_4_INLINE_KUBECTL_UNITTEST": "1"}
            for invalid in (False, True):
                current_script = fake / ("kubectl-foreign" if invalid else "kubectl-current")
                observed = deepcopy(binding)
                if invalid:
                    observed["data"]["profileSha256"] = lifecycle.STORY_27_4_PROFILE_SHA256
                intercept = "\nif 'get' in args and 'configmap' in args and 'access-telemetry-campaign-identity' in args:\n    print(" + repr(json.dumps(observed)) + "); raise SystemExit(0)\n"
                current_script.write_text(base.replace("args = sys.argv[1:]", "args = sys.argv[1:]" + intercept))
                with patch.dict(os.environ, {**environment, "HEXALITH_STORY_27_4_INLINE_KUBECTL": str(current_script)}):
                    if invalid:
                        with self.assertRaisesRegex(lifecycle.EvidenceValidationError, "runtime binding"):
                            producer._run_operation(target, "c2-production-replacement", "qualification-target-identity")
                    else:
                        result, _ = producer._run_operation(target, "c2-production-replacement", "qualification-target-identity")
                        self.assertEqual(64, len(result["runtime_identity_sha256"]))


if __name__ == "__main__":
    unittest.main()
