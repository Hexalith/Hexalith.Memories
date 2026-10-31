"""Focused ordinary-review fixes; authenticated fixtures provide no live credit."""
from contextlib import ExitStack
from copy import deepcopy
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import unittest
from unittest.mock import patch

import test_campaign_profile as base
import test_owner_review_fixes as reviewed
from test_owner_authority import OwnerFixture
from test_retention_verification import common, install_fake_kubectl
from test_campaign_capacity import initial_admission_fixture, renewal_evidence_fixture

capacity, profiles, lifecycle, producer = base.capacity, base.profiles, base.lifecycle, base.producer
ROOT = base.ROOT


class CampaignReviewFixTests(unittest.TestCase):
    setUp = base.CampaignProfileTests.setUp
    facts = base.CampaignProfileTests.facts

    def test_changed_platform_source_refuses_before_module_body_executes(self):
        workspace = self.root / "private-workspace"
        module = workspace / "references/Hexalith.Platform/eng/helm-qualification/campaign_capacity.py"
        module.parent.mkdir(parents=True)
        marker = self.root / "unexpected-import-effect"
        module.write_text("from pathlib import Path\nPath(" + repr(str(marker)) + ").write_text('executed')\n")
        proposed = deepcopy(self.profile)
        proposed["inputs"]["source"] = [{"path": str(module), "sha256": "a" * 64}]
        outer = self.write("changed-platform-profile.json", proposed)
        with patch.dict(os.environ, {**self.env,
                "HEXALITH_CAMPAIGN_PROFILE": outer["path"],
                "HEXALITH_CAMPAIGN_APPROVED_PROFILE_SHA256": outer["sha256"]}), self.assertRaises(lifecycle.EvidenceValidationError):
            profiles.selected_campaign_profile(workspace)
        self.assertFalse(marker.exists())

    def refresh(self, now):
        current = dict(profileSha256=capacity.digest(self.profile), compute=deepcopy(self.profile["compute"]), storage=deepcopy(self.profile["storage"]))
        for index, item in enumerate([current["compute"], *current["storage"]]):
            item["observedUtcMs"] = now
            item["evidence"] = self.write(f"refresh-{index}.json", {k: v for k, v in item.items() if k != "evidence"})
        ref = self.write("current.json", current)
        self.env.update(HEXALITH_CAMPAIGN_CAPACITY_RECEIPT=ref["path"], HEXALITH_CAMPAIGN_APPROVED_CAPACITY_SHA256=ref["sha256"])

    def owner(self, *, now=None, runtime_sha="a" * 64):
        root = self.root / "owner"; root.mkdir(exist_ok=True)
        origin = self.now - (self.profile["renewalSchedule"]["phases"]["c3"][0] * 1000 if now is not None else 48 * 3600000)
        elapsed = ((self.now if now is None else now) - origin) // 1000
        round_ = next(r for r in reversed(self.profile["renewalSchedule"]["rounds"]) if r["readySeconds"] <= elapsed < r["collectionEndSeconds"])
        return OwnerFixture(root, repository_root=self.repository_root, now=datetime.fromtimestamp((self.now if now is None else now) / 1000, timezone.utc),
            runtime_sha256=runtime_sha, profile_sha256=self.sha, profile_id=capacity.PROFILE_ID, technical_factory=self.facts,
            target_override={"context": self.profile["target"]["context"], "namespace": self.profile["target"]["namespace"]},
            campaign_schedule=self.profile["renewalSchedule"], campaign_origin_ms=origin, renewal_round=round_)

    def bind_original(self, fixture, original):
        ref = fixture.write("initial-capacity-admission.json", original["admission"], active=False)
        fixture.write("capacity-admission.key", original["key"], active=False)
        fixture.plan.update(initialCapacityAdmission=ref, capacityAdmissionIssuer=original["issuer"],
            capacityAdmissionKeyPath=str(fixture.root / "capacity-admission.key"), capacityAdmissionKeySha256=original["key_sha256"])
        fixture.approve()
        self.env.update(fixture.env)

    def test_actual_first_use_and_cli_refuse_48_hour_measurements(self):
        measured = self.profile["measurements"]
        measured["observedUtcMs"] -= 48 * 3600000
        measured["evidence"] = self.write("measured.json", {k: v for k, v in measured.items() if k != "evidence"})
        outer = self.write("profile.json", self.profile)
        self.env["HEXALITH_CAMPAIGN_APPROVED_PROFILE_SHA256"] = outer["sha256"]
        self.refresh(self.now)
        with patch.dict(os.environ, self.env), self.assertRaises(lifecycle.EvidenceValidationError):
            profiles.selected_campaign_profile(self.repository_root, now_ms=self.now, launch=True)
        result = subprocess.run([sys.executable, "-B", str(self.platform / "campaign_capacity.py"),
            "--profile", outer["path"], "--approved-sha256", outer["sha256"],
            "--current-capacity-receipt", self.env["HEXALITH_CAMPAIGN_CAPACITY_RECEIPT"],
            "--approved-capacity-sha256", self.env["HEXALITH_CAMPAIGN_APPROVED_CAPACITY_SHA256"]],
            cwd=self.repository_root, capture_output=True, text=True, timeout=10)
        self.assertEqual(1, result.returncode)
        self.assertFalse(json.loads(result.stdout)["capacityAdmitted"])

    def test_c3_requires_original_before_dispatch_and_authenticated_three_day_selection_keeps_origin(self):
        original = initial_admission_fixture(self.profile, self.now)
        stamp = json.loads(original["admission"])
        future = self.now + 3 * 86400000
        tick = stamp["admittedBoottimeNs"] + 3 * 86400000000000
        real_clock = capacity.time.clock_gettime_ns
        clock = lambda kind: tick if kind == capacity.time.CLOCK_BOOTTIME else real_clock(kind)
        with patch.dict(os.environ, self.env), self.assertRaises(lifecycle.EvidenceValidationError):
            lifecycle.validate_current_profile_inputs(self.repository_root, launch_admission=True, require_original_admission=True)
        with patch.dict(os.environ, self.env), patch.object(lifecycle, "_run_bounded_process") as child:
            self.assertEqual(1, lifecycle.run_story_27_4_producer_checkpoint(checkpoint="c3-retention-reclamation",
                scenario_input_path=self.root / "unread-scenario.json", predecessor_path=self.root / "unread-predecessor.json",
                evidence_path=self.root / "unwritten.json", owner="owner", repository_root=self.repository_root, evidence_root=self.root))
            child.assert_not_called()
        with patch.object(capacity.time, "clock_gettime_ns", clock), patch.object(capacity.time, "time", return_value=future / 1000):
            fixture = self.owner(now=future)
            origin = dict(startedUtcMs=self.now, startedBoottimeNs=stamp["admittedBoottimeNs"],
                          bootId=stamp["bootId"], journalPath=str(fixture.root / "c3.journal"))
            fixture.plan["c3Execution"] = deepcopy(origin)
            self.bind_original(fixture, original)
            self.refresh(future)
            with patch.dict(os.environ, self.env):
                selected = profiles.selected_campaign_profile(self.repository_root, now_ms=future, launch=True, require_original=True)
                self.assertEqual(capacity.digest(self.profile), capacity.digest(selected))
                current = fixture.verify()
                from access_telemetry_c1_owner_authority import c3_remaining_seconds
                self.assertAlmostEqual(777600 - 3 * 86400, c3_remaining_seconds(current), delta=1)
            self.assertEqual(origin, json.loads((fixture.root / "plan.json").read_bytes())["c3Execution"])
            for field, wrong in (("capacityAdmissionIssuer", "foreign-issuer"), ("initialCapacityAdmission", {**fixture.plan["initialCapacityAdmission"], "sha256": "a" * 64})):
                saved = fixture.plan[field]; fixture.plan[field] = wrong; fixture.approve(); self.env.update(fixture.env)
                with patch.dict(os.environ, self.env), self.assertRaises(lifecycle.EvidenceValidationError):
                    profiles.selected_campaign_profile(self.repository_root, now_ms=future, launch=True, require_original=True)
                fixture.plan[field] = saved

    def fake_runtime(self):
        fake = self.root / "bin"; fake.mkdir(exist_ok=True); install_fake_kubectl(fake)
        script = fake / "kubectl"
        body = script.read_text().replace(lifecycle.STORY_27_4_PROFILE_SHA256, self.sha)
        descriptor = capacity.runtime_binding(self.profile, self.sha)
        intercept = "\nif 'get' in args and 'configmap' in args and 'access-telemetry-campaign-identity' in args:\n print(" + repr(json.dumps(descriptor)) + "); raise SystemExit(0)\n"
        script.write_text(body.replace("args = sys.argv[1:]", "args = sys.argv[1:]" + intercept)); script.chmod(0o755)
        env = {**self.env, "PATH": str(fake) + os.pathsep + os.environ.get("PATH", ""),
               "QUALIFICATION_DAPR_DIGEST": "a" * 64, "HEXALITH_STORY_27_4_HOST_CADENCE_QUANTUM": "0",
               "HEXALITH_STORY_27_4_INLINE_KUBECTL": str(script), "HEXALITH_STORY_27_4_INLINE_KUBECTL_UNITTEST": "1"}
        target = dict(kind="non-production-qualification", kube_context=self.profile["target"]["context"],
                      namespace=self.profile["target"]["namespace"], profile_sha256=self.sha,
                      _lease_holder="fixture", _platform_operations_reviewer="owner-6775094")
        with patch.dict(os.environ, env):
            result, _ = producer._run_operation(target, "c2-production-replacement", "qualification-target-identity")
        fixture = self.owner(runtime_sha=result["runtime_identity_sha256"])
        env.update(fixture.env, HEXALITH_C1_PREDECESSOR_PATH=str(fixture.root / "predecessor.json"),
                   HEXALITH_C1_REPOSITORY_ROOT=str(self.repository_root), HEXALITH_C1_EXECUTION_ID="e" * 32)
        scenario = fixture.root / "scenario.json"
        fixture.write("scenario.json", {"schema_version": 1, "target": fixture.plan["executionTarget"]}, active=False)
        return env, fixture, scenario, script

    def cleanup(self, env, scenario):
        return subprocess.run([sys.executable, "-B", str(ROOT / "tools/access_telemetry_c2_producer.py"),
            "--scenario-input", str(scenario), "--platform-operations-reviewer", "owner-6775094", "--disable-only"],
            cwd=self.repository_root, env={**os.environ, **env}, capture_output=True, text=True, timeout=20)

    def test_actual_disable_only_survives_component_and_measurement_drift_but_denies_foreign_scope(self):
        env, fixture, scenario, script = self.fake_runtime()
        log = self.root / "cleanup.log"; env["QUALIFICATION_OPERATION_LOG"] = str(log)
        for path in (Path(self.profile["inputs"]["components"][0]["path"]), Path(self.profile["measurements"]["evidence"]["path"])):
            raw = path.read_bytes(); path.write_bytes(raw + b" ")
            with patch.dict(os.environ, env), self.assertRaises(lifecycle.EvidenceValidationError):
                profiles.selected_campaign_profile(self.repository_root, launch=True)
            result = self.cleanup(env, scenario)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn("qualification-disable", log.read_text())
            path.write_bytes(raw)
        # Drift does not grant cleanup over another physical target or Lease.
        Path(self.profile["inputs"]["components"][0]["path"]).write_bytes(b"drift")
        foreign = self.cleanup({**env, "QUALIFICATION_ACTIVE_FOREIGN_LEASE": "1"}, scenario)
        self.assertEqual(1, foreign.returncode)
        original = script.read_text(); script.write_text(original.replace("fixture-cluster", "foreign-cluster"))
        self.assertEqual(1, self.cleanup(env, scenario).returncode)
        script.write_text(original)
        fixture.plan["sessionId"] = "foreign-session"; fixture.approve(); env.update(fixture.env)
        self.assertEqual(1, self.cleanup(env, scenario).returncode)

    def test_parent_fallback_and_failure_packet_survive_drift(self):
        # Bind the real registered common source for this actual parent launch.
        source = self.repository_root / "tools/access_telemetry_producer_common.py"
        source_ref = dict(path=str(source), sha256=hashlib.sha256(source.read_bytes()).hexdigest())
        self.profile["inputs"]["source"].append(source_ref)
        plan_ref = self.profile["inputs"]["workloadPlan"][0]
        workload = json.loads(Path(plan_ref["path"]).read_bytes())
        workload["sourceInputsSha256"] = capacity.digest(self.profile["inputs"]["source"])
        for row in workload["commandInventory"]:
            if row["commandId"].startswith("C2."):
                row["sourceSha256"] = source_ref["sha256"]
        self.profile["inputs"]["workloadPlan"] = [self.write("workloadPlan.json", workload)]
        self.profile["inputs"]["renewalEvidence"] = [self.write("renewalEvidence.json", renewal_evidence_fixture(self.profile, workload, self.write))]
        measured = self.profile["measurements"]; measured["scopeSha256"] = capacity.scope(self.profile)
        measured["evidence"] = self.write("measured.json", {k: v for k, v in measured.items() if k != "evidence"})
        outer = self.write("profile.json", self.profile); self.env["HEXALITH_CAMPAIGN_APPROVED_PROFILE_SHA256"] = outer["sha256"]
        self.refresh(self.now)
        with patch.dict(os.environ, self.env): self.sha = profiles.active_profile_sha256(self.repository_root)
        env, self.fixture, scenario, _ = self.fake_runtime()
        log = self.root / "fallback.log"; env["QUALIFICATION_OPERATION_LOG"] = str(log)
        drift = self.profile["inputs"]["components"][0]["path"]
        child = ("import pathlib,sys\nif '--disable-only' in sys.argv:\n from access_telemetry_producer_common import run\n raise SystemExit(run('c2-production-replacement'))\npathlib.Path(" + repr(drift) + ").write_bytes(b'drift')\nraise SystemExit(9)\n").encode()
        output = self.fixture.root / "fallback-result.json"
        with ExitStack() as stack:
            stack.enter_context(patch.dict(os.environ, env))
            reviewed.ReviewFixTests.source_fixture(self, stack, ("tools/access_telemetry_c2_producer.py", child))
            code = lifecycle.run_story_27_4_producer_checkpoint(checkpoint="c2-production-replacement",
                scenario_input_path=scenario, predecessor_path=self.fixture.root / "predecessor.json",
                evidence_path=output, owner="owner-6775094", repository_root=self.repository_root, evidence_root=self.fixture.root)
        self.assertEqual(1, code)
        packet = json.loads(output.read_bytes())
        self.assertEqual(self.sha, packet["profile_sha256"])
        self.assertEqual(0, packet["observation"]["disable_exit_code"], packet)
        self.assertIn("qualification-disable", log.read_text())

    def test_authenticated_historical_closure_and_current_import_requirements(self):
        commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
        original_read = lifecycle._read_git_blob

        def historical_source(root, commit_id, relative):
            raw = original_read(root, commit_id, relative)
            if relative == "tools/verify_access_telemetry_lifecycle.py":
                raw = raw.replace(
                    b"from access_telemetry_campaign_profile import (",
                    b"from access_telemetry_campaign_profile_absent import (",
                )
            return raw

        with patch.object(lifecycle, "_read_git_blob", historical_source):
            for checkpoint in ("C0", *lifecycle.STORY_27_4_CHECKPOINTS):
                payload = common(checkpoint)
                payload["source_commit"] = commit
                paths = lifecycle._expected_source_paths(checkpoint, closure_version="historical/v1")
                payload["source_hashes"] = {path: lifecycle._hash_git_blob(ROOT, commit, path) for path in paths}
                payload["producer"]["source_sha256"] = payload["source_hashes"][payload["producer"]["path"]]
                lifecycle._validate_source_identity(payload, ROOT)
                payload["profile_sha256"] = self.sha
                with self.assertRaisesRegex(lifecycle.EvidenceValidationError, "historical"):
                    lifecycle._validate_source_identity(payload, ROOT)
        payload = common("c2-production-replacement"); payload["source_commit"] = commit
        payload["source_hashes"] = {path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
                                    for path in lifecycle._expected_source_paths(payload["checkpoint"])}
        payload["producer"]["source_sha256"] = payload["source_hashes"][payload["producer"]["path"]]
        with patch.object(lifecycle, "_read_git_blob", lambda root, commit, relative: (ROOT / relative).read_bytes()):
            lifecycle._validate_source_identity(payload, ROOT)
            del payload["source_hashes"]["tools/access_telemetry_campaign_profile.py"]
            with self.assertRaisesRegex(lifecycle.EvidenceValidationError, "exact reviewed source"):
                lifecycle._validate_source_identity(payload, ROOT)


if __name__ == "__main__":
    unittest.main()
