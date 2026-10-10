"""Offline successor identity, configuration and qualification refusal checks."""

import json
import os
from pathlib import Path
import shutil
import sys
import subprocess
import tempfile
import unittest
from unittest.mock import patch


REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO_ROOT / "tools"))

import access_telemetry_producer_common as producer
import verify_access_telemetry_lifecycle as lifecycle
from test_retention_verification import common, predecessor, install_fake_kubectl, install_tools, qualification_test_env


CANDIDATE = REPO_ROOT / "_bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/profile-candidate"
HISTORICAL_HASH = "dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14"
CURRENT_HASH = "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe"
COPIES = {
    "openbao-values.candidate.yaml": "deploy/openbao/values.yaml",
    "postgresql.candidate.yaml": "deploy/kubernetes/base/access-telemetry-postgresql.yaml",
    "lifecycle-deployments.candidate.yaml": "deploy/kubernetes/base/access-telemetry-deployments.yaml",
    "physical-reporter.candidate.yaml": "deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml",
}


class ApprovedProfileAdoptionTests(unittest.TestCase):
    @staticmethod
    def c0_command(repository, evidence):
        return [sys.executable, "-B", str(repository / "tools/verify-access-telemetry-lifecycle.py"),
                "--repository-root", str(repository), "--checkpoint", "adapter-profile",
                "--kube-context", "operator@local", "--namespace", "hexalith-memories-qualification",
                "--deployment-id", "fixture-c0", "--profile-id", lifecycle.EXPECTED_PROFILE_ID,
                "--workload-profile", "adr-27.1-two-writer-500eps", "--steady-state-minutes", "30",
                "--purge-backlog-records", "150000", "--declared-single-component-fault", "postgresql-pod-replacement",
                "--evidence-root", str(evidence), "--evidence", str(evidence / "adapter.json"),
                "--c0-wrapper", str(evidence / "C0.json")]

    def test_exact_current_manifest_and_historical_manifest_are_independent(self):
        proposal = json.loads((CANDIDATE / "candidate-profile.json").read_text())
        current = lifecycle.canonical_pg_onprem_2_profile().manifest()
        historical = lifecycle.canonical_pg_onprem_profile().manifest()
        self.assertEqual(proposal["manifest"], current)
        self.assertEqual(HISTORICAL_HASH, historical["profile_sha256"])
        self.assertEqual(CURRENT_HASH, current["profile_sha256"])
        self.assertEqual(CURRENT_HASH, lifecycle.STORY_27_4_PROFILE_SHA256)
        self.assertEqual(historical["canonical_profile"]["capabilities"], current["canonical_profile"]["capabilities"])
        self.assertEqual(historical["canonical_profile"]["workload"], current["canonical_profile"]["workload"])
        self.assertEqual(500, current["canonical_profile"]["workload"]["total_events_per_second"])
        profile = lifecycle.canonical_pg_onprem_2_profile()
        profile.identity["securityPlatform"]["chartVersion"] = "drift"
        self.assertEqual(current, lifecycle.canonical_pg_onprem_2_profile().manifest())

    def test_drifted_current_selectors_are_refused_without_changing_either_manifest(self):
        current = lifecycle.canonical_pg_onprem_2_profile().manifest()
        historical = lifecycle.canonical_pg_onprem_profile().manifest()
        for name in ("EXPECTED_PROFILE_ID", "EXPECTED_POSTGRESQL_IMAGE", "EXPECTED_POSTGRESQL_LINUX_AMD64_MANIFEST",
                     "EXPECTED_RUNTIME_IMAGE", "EXPECTED_RUNTIME_LINUX_AMD64_MANIFEST"):
            with self.subTest(name=name), patch.object(lifecycle, name, "unapproved-selector"):
                with self.assertRaisesRegex(ValueError, "selector identity drifted"):
                    lifecycle.validate_current_profile_inputs(REPO_ROOT)
                self.assertEqual(current, lifecycle.canonical_pg_onprem_2_profile().manifest())
                self.assertEqual(historical, lifecycle.canonical_pg_onprem_profile().manifest())

    def test_exact_approved_copies_and_all_bound_inputs_reject_each_drift(self):
        for candidate, active in COPIES.items():
            with self.subTest(candidate=candidate):
                self.assertEqual((CANDIDATE / candidate).read_bytes(), (REPO_ROOT / active).read_bytes())
        lifecycle.validate_current_profile_inputs(REPO_ROOT)
        identity = lifecycle.canonical_pg_onprem_2_profile().identity
        paths = list(identity["securityPlatform"]["secretAndConfigurationInputs"]) + list(COPIES.values())
        self.assertEqual(16, len(paths))
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for relative in paths:
                target = root / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(REPO_ROOT / relative, target)
            lifecycle.validate_current_profile_inputs(root)
            for relative in paths:
                with self.subTest(relative=relative):
                    target = root / relative
                    approved = target.read_bytes()
                    target.write_bytes(approved + b"\n# drift\n")
                    with self.assertRaisesRegex(ValueError, "input drifted"):
                        lifecycle.validate_current_profile_inputs(root)
                    target.write_bytes(approved)

    def test_historical_predecessor_inspection_and_c0_reject_old_or_mixed_evidence(self):
        self.assertIsNone(lifecycle._inspect_legacy_predecessor(predecessor()))
        for kind in ("predecessor", "operations-approval", "security-approval"):
            with self.subTest(kind=kind):
                packet = predecessor()
                if kind == "predecessor":
                    packet["profile_sha256"] = HISTORICAL_HASH
                else:
                    packet["approvals"][0 if kind == "operations-approval" else 1]["profile_sha256"] = HISTORICAL_HASH
                with self.assertRaises(ValueError):
                    lifecycle._inspect_legacy_predecessor(packet)
        for checkpoint in ("C0", "c2-production-replacement", "c3-retention-reclamation", "c4-failure-privacy-observability"):
            with self.subTest(checkpoint=checkpoint):
                packet = common(checkpoint)
                packet["profile_sha256"] = HISTORICAL_HASH
                packet["results"] = {}
                with self.assertRaisesRegex(ValueError, "profile"):
                    lifecycle._validate_common_checkpoint(checkpoint, packet, None)
        with tempfile.TemporaryDirectory() as temporary:
            target = Path(temporary) / "target.json"
            target.write_text(json.dumps({"schema_version": 1, "target": {
                "kind": "non-production-qualification", "kube_context": "operator@local",
                "namespace": "memories-qualification", "profile_sha256": HISTORICAL_HASH,
            }}))
            with self.assertRaisesRegex(ValueError, "PG-ONPREM-2"):
                producer._load_target(target)

    def test_downstream_producer_refuses_wrong_declared_and_running_pg_or_dapr_before_enablement(self):
        cases = (
            {"QUALIFICATION_POSTGRES_IMAGE": lifecycle.HISTORICAL_POSTGRESQL_IMAGE},
            {"QUALIFICATION_POSTGRES_RUNNING_ID": "containerd://sha256:" + "a" * 64},
            {"QUALIFICATION_DAPR_DIGEST": "a" * 64},
        )
        cases += tuple({"QUALIFICATION_MUTATION_WORKLOAD": workload, "QUALIFICATION_CONTAINER_MUTATION": mutation}
            for workload in ("memories", "memories-access-telemetry", "memories-access-telemetry-clock", "access-telemetry-postgresql")
            for mutation in ("missing", "renamed", "duplicate"))
        for overrides in cases:
            with self.subTest(overrides=overrides), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                fake_bin = root / "bin"
                fake_bin.mkdir()
                install_fake_kubectl(fake_bin)
                target = root / "target.json"
                target.write_text(json.dumps({"schema_version": 1, "target": {
                    "kind": "non-production-qualification", "kube_context": "operator@local",
                    "namespace": "memories-qualification", "profile_sha256": CURRENT_HASH,
                }}))
                log = root / "calls.log"
                env = qualification_test_env(fake_bin, QUALIFICATION_OPERATION_LOG=str(log), **overrides)
                result = subprocess.run([sys.executable, "-B", str(REPO_ROOT / "tools/access_telemetry_c2_producer.py"),
                    "--scenario-input", str(target), "--platform-operations-reviewer", "fixture-reviewer"],
                    cwd=REPO_ROOT, env=env, text=True, capture_output=True, timeout=20)
                self.assertNotEqual(0, result.returncode)
                self.assertRegex(result.stderr, "PG-ONPREM-2|running pod identity is incomplete")
                self.assertEqual("", result.stdout)
                self.assertEqual(["qualification-target-identity"], log.read_text().splitlines())

    def test_c0_refuses_wrong_dapr_digest_without_adapter_or_wrapper_credit(self):
        cases = ({"QUALIFICATION_DAPR_DIGEST": "a" * 64},) + tuple(
            {"QUALIFICATION_MUTATION_WORKLOAD": "memories", "QUALIFICATION_CONTAINER_MUTATION": mutation}
            for mutation in ("missing", "renamed", "duplicate"))
        for overrides in cases:
            with self.subTest(overrides=overrides), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                fake_bin = root / "bin"
                fake_bin.mkdir()
                install_fake_kubectl(fake_bin)
                evidence = root / "adapter.json"
                wrapper = root / "C0.json"
                env = qualification_test_env(fake_bin, **overrides)
                result = subprocess.run([sys.executable, "-B", str(REPO_ROOT / "tools/verify-access-telemetry-lifecycle.py"),
                    "--repository-root", str(REPO_ROOT), "--checkpoint", "adapter-profile",
                    "--kube-context", "operator@local", "--namespace", "hexalith-memories-qualification",
                    "--deployment-id", "fixture-c0", "--profile-id", lifecycle.EXPECTED_PROFILE_ID,
                    "--workload-profile", "adr-27.1-two-writer-500eps", "--steady-state-minutes", "30",
                    "--purge-backlog-records", "150000", "--declared-single-component-fault", "postgresql-pod-replacement",
                    "--evidence-root", str(root), "--evidence", str(evidence), "--c0-wrapper", str(wrapper)],
                    cwd=REPO_ROOT, env=env, text=True, capture_output=True, timeout=20)
                self.assertNotEqual(0, result.returncode)
                self.assertRegex(result.stdout, "running daprd image differs from the approved PG-ONPREM-2 pin|daprd container identity is missing or duplicated")
                self.assertFalse(wrapper.exists())
                self.assertIn("status: `rejected`", evidence.read_text())

    def test_c0_rejects_backend_and_observed_profile_workload_runtime_faults(self):
        cases = [
            {"QUALIFICATION_POSTGRES_RUNNING_ID": lifecycle.HISTORICAL_POSTGRESQL_IMAGE},
            {"QUALIFICATION_POSTGRES_RUNNING_ID": "sha256:" + "a" * 64},
        ]
        for workload in lifecycle._C0_PROFILE_CONTAINERS:
            cases.append({"QUALIFICATION_MISSING_WORKLOAD": workload})
            cases.append({"QUALIFICATION_MUTATION_WORKLOAD": workload, "QUALIFICATION_MUTATION_IMAGE_ID": "sha256:" + "a" * 64})
            cases.extend({"QUALIFICATION_MUTATION_WORKLOAD": workload, "QUALIFICATION_CONTAINER_MUTATION": mutation}
                         for mutation in ("missing", "renamed", "duplicate"))
            cases.append({"QUALIFICATION_RUNTIME_DRIFT_AFTER": workload})
        for overrides in cases:
            with self.subTest(overrides=overrides), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                fake_bin = root / "bin"
                fake_bin.mkdir()
                install_fake_kubectl(fake_bin)
                result = subprocess.run(self.c0_command(REPO_ROOT, root), cwd=REPO_ROOT,
                    env=qualification_test_env(fake_bin, QUALIFICATION_KUBECTL_LOG=str(root / "calls.jsonl"), **overrides),
                    text=True, capture_output=True, timeout=20)
                self.assertNotEqual(0, result.returncode, result.stdout + result.stderr)
                self.assertNotIn("C0 adapter-profile: passed", result.stdout)
                self.assertFalse((root / "C0.json").exists())
                self.assertIn("status: `rejected`", (root / "adapter.json").read_text())
                self.assertRegex(result.stdout, "running .*PG-ONPREM-2|profile pod identity changed")

    def test_both_python_cli_preflights_refuse_bound_input_drift_before_any_kubectl(self):
        for boundary in ("C0", "downstream-producer"):
            with self.subTest(boundary=boundary), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                repository = root / "repository"
                repository.mkdir()
                install_tools(repository)
                drift = repository / "deploy/dapr/components/access-telemetry-config.yaml"
                drift.write_bytes(drift.read_bytes() + b"\n# unapproved configuration drift\n")
                fake_bin = root / "bin"
                fake_bin.mkdir()
                install_fake_kubectl(fake_bin)
                log = root / "calls.jsonl"
                if boundary == "C0":
                    command = self.c0_command(repository, root)
                else:
                    target = root / "target.json"
                    target.write_text(json.dumps({"schema_version": 1, "target": {
                        "kind": "non-production-qualification", "kube_context": "operator@local",
                        "namespace": "memories-qualification", "profile_sha256": CURRENT_HASH}}))
                    command = [sys.executable, "-B", str(repository / "tools/access_telemetry_c2_producer.py"),
                               "--scenario-input", str(target), "--platform-operations-reviewer", "fixture-reviewer"]
                result = subprocess.run(command, cwd=repository,
                    env=qualification_test_env(fake_bin, QUALIFICATION_KUBECTL_LOG=str(log)),
                    text=True, capture_output=True, timeout=20)
                self.assertNotEqual(0, result.returncode)
                self.assertIn("input drifted", result.stdout + result.stderr)
                self.assertFalse(log.exists(), "Source drift must refuse before the first kubectl invocation")
                self.assertFalse((root / "C0.json").exists())
                if boundary == "C0":
                    self.assertIn("status: `rejected`", (root / "adapter.json").read_text())
                    self.assertNotIn("C0 adapter-profile: passed", result.stdout)
                else:
                    self.assertEqual("", result.stdout)

    def test_authenticated_platform_child_runtime_passes_c0_and_downstream_inventory(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            repository = root / "repository"
            repository.mkdir()
            install_tools(repository)
            for args in (("init", "-q"), ("config", "user.email", "test@example.invalid"),
                         ("config", "user.name", "Test"), ("add", "."),
                         ("commit", "-q", "-m", "test: create source baseline")):
                subprocess.run(["git", "-C", str(repository), *args], check=True, capture_output=True)
            fake_bin = root / "bin"
            fake_bin.mkdir()
            install_fake_kubectl(fake_bin)
            child_env = qualification_test_env(fake_bin,
                QUALIFICATION_DAPR_DIGEST=lifecycle.EXPECTED_RUNTIME_LINUX_AMD64_MANIFEST.removeprefix("sha256:"),
                QUALIFICATION_POSTGRES_RUNNING_ID="containerd://" + lifecycle.EXPECTED_POSTGRESQL_LINUX_AMD64_MANIFEST,
                QUALIFICATION_KUBECTL_LOG=str(root / "calls.jsonl"))
            result = subprocess.run(self.c0_command(repository, root), cwd=repository,
                                    env=child_env, text=True, capture_output=True, timeout=20)
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("C0 adapter-profile: passed", result.stdout)
            adapter = json.loads((root / "adapter.json").read_text())
            wrapper = json.loads((root / "C0.json").read_text())
            runtime = json.loads((root / "adapter-runtime-observation.json").read_text())
            self.assertEqual("passed", adapter["status"])
            self.assertEqual("C0", wrapper["checkpoint"])
            self.assertEqual("disabled", adapter["production_lifecycle_writes"])
            self.assertEqual(CURRENT_HASH, wrapper["profile_sha256"])
            self.assertEqual(["ghcr.io/dapr/daprd@" + lifecycle.EXPECTED_RUNTIME_LINUX_AMD64_MANIFEST],
                             runtime["runtime_identity"]["sidecar_image_digests"])
            backend = next(pod for pod in runtime["summaries"]["pods"] if pod["name"] == "postgres-0")
            self.assertEqual(["containerd://" + lifecycle.EXPECTED_POSTGRESQL_LINUX_AMD64_MANIFEST], backend["container_image_ids"])
            calls = [json.loads(line) for line in (root / "calls.jsonl").read_text().splitlines()]
            selectors = [call[call.index("-l") + 1] for call in calls if "-l" in call]
            for workload in lifecycle._C0_PROFILE_CONTAINERS:
                self.assertEqual(2, selectors.count("app.kubernetes.io/name=" + workload))
            target = {"kube_context": "operator@local", "namespace": "memories-qualification",
                      "_platform_operations_reviewer": "fixture-reviewer", "_lease_holder": "story-27-4/reviewer/test"}
            with patch.dict(os.environ, child_env):
                child_identity, child_command = producer._run_operation(target, "qualification", "qualification-target-identity")
            with patch.dict(os.environ, qualification_test_env(fake_bin)):
                index_identity, _ = producer._run_operation(dict(target), "qualification", "qualification-target-identity")
            self.assertEqual("disabled", child_identity["writes_state"])
            self.assertEqual(CURRENT_HASH, child_identity["profile_sha256"])
            self.assertEqual(target["_runtime_identity_sha256"], child_identity["runtime_identity_sha256"])
            self.assertNotEqual(index_identity["runtime_identity_sha256"], child_identity["runtime_identity_sha256"])
            self.assertEqual(0, child_command["exit_code"])
            self.assertGreater(child_command["result_count"], 0)

    def test_smoke_client_apphost_and_fault_capacity_boundaries_are_preserved(self):
        smoke = (REPO_ROOT / "deploy/openbao/smoke-test.yaml").read_text()
        apphost = (REPO_ROOT / "src/Hexalith.Memories.AppHost/OpenBaoDevelopmentProfile.cs").read_text()
        for source in (smoke, apphost):
            self.assertIn("2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653", source)
        capacity = lifecycle.canonical_pg_onprem_2_profile().identity["capacityBoundary"]
        self.assertEqual(429496729600, capacity["capacityBytes"])
        self.assertEqual(300647710720, capacity["steadyStateBytes"])
        self.assertEqual(343597383680, capacity["criticalBytes"])
        self.assertEqual(386547056640, capacity["unhealthyBytes"])
        self.assertEqual(["postgresql-pod-replacement", "postgresql-process-replacement"], capacity["zeroLossFaults"])
        self.assertEqual(["node-loss", "volume-loss", "control-plane-loss", "site-loss"], capacity["excludedFaults"])
        self.assertFalse(capacity["nodeHa"])


if __name__ == "__main__":
    unittest.main()
