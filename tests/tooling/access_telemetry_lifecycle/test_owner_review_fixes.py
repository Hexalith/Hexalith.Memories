"""Regression proofs for the single ordinary review; local fixtures only."""
from contextlib import ExitStack
from copy import deepcopy
from datetime import datetime, timedelta, timezone
import functools
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import threading
import time
import unittest
from unittest.mock import patch

from test_owner_authority import OwnerFixture, ROOT, SHA, authority, producer, lifecycle, digest, j1_bytes
from test_retention_verification import (install_fake_kubectl, qualification_test_env,
    c2_payload, c3_payload, c4_payload, common, manifest)


class ReviewFixTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.custody = self.base / "custody"; self.custody.mkdir(mode=0o700)
        self.fixture = OwnerFixture(self.custody)
        self.env = dict(os.environ, **self.fixture.env,
                        HEXALITH_C1_PREDECESSOR_PATH=str(self.custody / "predecessor.json"),
                        HEXALITH_C1_REPOSITORY_ROOT=str(ROOT), HEXALITH_C1_EXECUTION_ID="e" * 32)

    def source_fixture(self, stack, child_bytes=None):
        """Frozen real byte sources; no repository commit/index mutation."""
        original_git = lifecycle._git_checked
        original_blob = lifecycle._hash_git_blob
        original_bytes = lifecycle._authenticated_worktree_bytes
        def git(root, *args):
            return "" if args[0] == "status" else original_git(root, *args)
        def blob(root, commit, relative):
            if child_bytes is not None and relative == child_bytes[0]:
                return digest(child_bytes[1])
            if commit == self.fixture.commit and (ROOT / relative).is_file():
                return digest((ROOT / relative).read_bytes().replace(b"\r\n", b"\n"))
            return original_blob(root, commit, relative)
        def worktree(root, relative, expected):
            if child_bytes is not None and relative == child_bytes[0]:
                return child_bytes[1]
            return original_bytes(root, relative, expected)
        stack.enter_context(patch.object(lifecycle, "_git_checked", git))
        stack.enter_context(patch.object(lifecycle, "_hash_git_blob", functools.lru_cache(maxsize=100)(blob)))
        stack.enter_context(patch.object(lifecycle, "_authenticated_worktree_bytes", worktree))

    def update_plan(self, plan, env=None):
        env = self.env if env is None else env
        raw = j1_bytes(plan); (Path(env["HEXALITH_C1_OWNER_PLAN"])).write_bytes(raw)
        env["HEXALITH_C1_APPROVED_PLAN_SHA256"] = digest(raw)
        custody = Path(env["HEXALITH_C1_CUSTODY_ROOT"])
        for name in ("withdrawal.json", "authority-status.json"):
            path = custody / name; body = json.loads(path.read_text())
            body["planSha256"] = digest(raw); path.write_bytes(j1_bytes(body))

    def test_c2_supervised_producer_crosses_renewed_short_verdict(self):
        # Finite one-hour grant covers the required uninterrupted 1800s C2
        # fixture. A real supervised local child crosses its first short verdict.
        self.fixture.plan["authorization"]["issuedUtcMs"] -= 3600000
        self.fixture.plan["authorization"]["expiresUtcMs"] = self.fixture.ms + 3600000
        self.update_plan(self.fixture.plan)
        status = deepcopy(self.fixture.status)
        status["planSha256"] = self.env["HEXALITH_C1_APPROVED_PLAN_SHA256"]
        status["expiresUtcMs"] = self.fixture.ms + 2500
        self.fixture.write("authority-status.json", status, active=False)
        payload = c2_payload()
        payload["commands"] = payload["commands"][1:]
        payload["result_count"] = sum(c["result_count"] for c in payload["commands"])
        data = self.custody / "local-c2-payload.json"; data.write_bytes(j1_bytes(payload))
        child = b"import pathlib,sys,time\ntime.sleep(6)\nsys.stdout.buffer.write(pathlib.Path(" + repr(str(data)).encode() + b").read_bytes())\n"
        self.fixture.write("scenario.json", {"schema_version": 1, "target": self.fixture.plan["executionTarget"]}, active=False)
        def renew():
            now = int(time.time() * 1000)
            fresh = dict(status, issuedUtcMs=now, expiresUtcMs=now + 900000, updatedUtcMs=now)
            self.fixture.write("authority-status.json", fresh, active=False)
        timer = threading.Timer(1.7, renew); timer.start()
        output = self.custody / "C2-result.json"
        with ExitStack() as stack:
            stack.enter_context(patch.dict(os.environ, self.env))
            self.source_fixture(stack, ("tools/access_telemetry_c2_producer.py", child))
            # Fixture packet chronology models 30min; no live elapsed credit.
            calls = iter([self.fixture.ms - 1801000])
            stack.enter_context(patch.object(lifecycle, "_utc_now_milliseconds", lambda: next(calls, self.fixture.ms + 7000)))
            code = lifecycle.run_story_27_4_producer_checkpoint(checkpoint="c2-production-replacement", scenario_input_path=self.custody / "scenario.json", predecessor_path=self.custody / "predecessor.json", evidence_path=output, owner="owner-6775094", repository_root=ROOT, evidence_root=self.custody)
        timer.join()
        self.assertEqual(0, code, output.read_text())
        self.assertEqual("passed", json.loads(output.read_text())["status"])
        self.assertGreater(json.loads((self.custody / "authority-status.json").read_text())["issuedUtcMs"], status["issuedUtcMs"])

    def test_renewed_verdict_cannot_extend_grant_or_change_session_actions(self):
        self.fixture.verify()
        base = deepcopy(self.fixture.status)
        cases = [dict(expiresUtcMs=self.fixture.plan["authorization"]["expiresUtcMs"] + 1),
                 dict(sessionId="foreign-session"), dict(actions=["production"])]
        for fields in cases:
            self.fixture.write("authority-status.json", dict(base, **fields), active=False)
            with self.subTest(fields=fields), self.assertRaises(lifecycle.EvidenceValidationError):
                self.fixture.verify()

    def cleanup_fixture(self, **extra):
        fake = self.base / "bin"; fake.mkdir(exist_ok=True); install_fake_kubectl(fake)
        env = qualification_test_env(fake, **extra)
        env["HEXALITH_C1_REPOSITORY_ROOT"] = str(ROOT)
        env["HEXALITH_C1_EXECUTION_ID"] = "e" * 32
        root = Path(env["HEXALITH_C1_CUSTODY_ROOT"])
        plan = json.loads((root / "plan.json").read_text())
        scenario = self.base / "scenario.json"
        scenario.write_bytes(j1_bytes({"schema_version": 1, "target": plan["executionTarget"]}))
        return env, root, plan, scenario

    def run_cleanup(self, env, scenario):
        return subprocess.run([sys.executable, "-B", str(ROOT / "tools/access_telemetry_c2_producer.py"), "--scenario-input", str(scenario), "--platform-operations-reviewer", "owner-6775094", "--disable-only"], cwd=ROOT, env=env, capture_output=True, text=True, timeout=15)

    def test_cleanup_survives_expired_and_withdrawn_execution(self):
        log = self.base / "operations.log"
        env, root, plan, scenario = self.cleanup_fixture(QUALIFICATION_OPERATION_LOG=str(log))
        plan["authorization"].update(issuedUtcMs=int(time.time() * 1000) - 3600000, expiresUtcMs=int(time.time() * 1000) - 1)
        self.update_plan(plan, env)
        for mode in ("expired", "withdrawn"):
            status = json.loads((root / "authority-status.json").read_text())
            status["expiresUtcMs"] = 1
            (root / "authority-status.json").write_bytes(j1_bytes(status))
            withdrawal = json.loads((root / "withdrawal.json").read_text())
            withdrawal["withdrawn"] = mode == "withdrawn"
            (root / "withdrawal.json").write_bytes(j1_bytes(withdrawal))
            log.unlink(missing_ok=True)
            result = self.run_cleanup(env, scenario)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual(["qualification-target-identity", "qualification-disable", "qualification-final-state"], log.read_text().splitlines())

    def test_cleanup_denies_active_foreign_invocation_and_changed_target(self):
        log = self.base / "operations.log"
        env, root, plan, scenario = self.cleanup_fixture(QUALIFICATION_OPERATION_LOG=str(log), QUALIFICATION_ACTIVE_FOREIGN_LEASE="1")
        result = self.run_cleanup(env, scenario)
        self.assertEqual(1, result.returncode)
        self.assertEqual(["qualification-target-identity"], log.read_text().splitlines())
        # An active holder from the same session but a different execution is foreign.
        from access_telemetry_c1_owner_authority import qualification_lease_holder
        with patch.dict(os.environ, env):
            original = authority.verify_owner_cleanup(ROOT, root)
        foreign_holder = qualification_lease_holder(original, "f" * 32)
        self.assertNotEqual(qualification_lease_holder(original, "e" * 32), foreign_holder)
        script = self.base / "bin/kubectl"
        script.write_text(script.read_text().replace("foreign-controller", foreign_holder))
        log.unlink(missing_ok=True)
        result = self.run_cleanup(env, scenario)
        self.assertEqual(1, result.returncode, result.stderr)
        self.assertEqual(["qualification-target-identity"], log.read_text().splitlines())
        # The exact original invocation can recover that same active Lease.
        env["HEXALITH_C1_EXECUTION_ID"] = "f" * 32
        self.assertEqual(0, self.run_cleanup(env, scenario).returncode)
        env.pop("QUALIFICATION_ACTIVE_FOREIGN_LEASE")
        script.write_text(script.read_text().replace("fixture-namespace", "foreign-namespace"))
        log.unlink(missing_ok=True)
        result = self.run_cleanup(env, scenario)
        self.assertEqual(1, result.returncode)
        self.assertFalse(log.exists())

    def test_parent_fallback_retains_original_cleanup_context(self):
        log = self.base / "fallback-operations.log"
        env, root, plan, scenario = self.cleanup_fixture(QUALIFICATION_OPERATION_LOG=str(log))
        scenario = root / "scenario.json"
        scenario.write_bytes(j1_bytes({"schema_version": 1, "target": plan["executionTarget"]})); scenario.chmod(0o600)
        child_bytes = b"import sys\nif '--disable-only' in sys.argv:\n from access_telemetry_producer_common import run\n raise SystemExit(run('c2-production-replacement'))\nraise SystemExit(9)\n"
        calls = []
        run_process = lifecycle._run_bounded_process
        def supervised(command, **kwargs):
            calls.append((command, kwargs))
            return run_process(command, **kwargs)
        with ExitStack() as stack:
            stack.enter_context(patch.dict(os.environ, env)); self.source_fixture(stack, ("tools/access_telemetry_c2_producer.py", child_bytes))
            stack.enter_context(patch.object(lifecycle, "_run_bounded_process", supervised))
            code = lifecycle.run_story_27_4_producer_checkpoint(checkpoint="c2-production-replacement", scenario_input_path=scenario, predecessor_path=root / "predecessor.json", evidence_path=root / "fallback.json", owner="owner", repository_root=ROOT, evidence_root=root)
        self.assertEqual(1, code)
        self.assertEqual(2, len(calls))
        primary_env, fallback_env = calls[0][1]["environment"], calls[1][1]["environment"]
        for name in ("HEXALITH_C1_OWNER_PLAN", "HEXALITH_C1_APPROVED_PLAN_SHA256", "HEXALITH_C1_CUSTODY_ROOT", "HEXALITH_C1_PREDECESSOR_PATH", "HEXALITH_C1_REPOSITORY_ROOT", "HEXALITH_C1_EXECUTION_ID"):
            self.assertEqual(primary_env[name], fallback_env[name])
        self.assertNotIn("HEXALITH_C1_CHECK_EXECUTION", fallback_env)
        self.assertEqual(0, json.loads((root / "fallback.json").read_text())["observation"]["disable_exit_code"])
        self.assertEqual(["qualification-target-identity", "qualification-disable", "qualification-final-state"], log.read_text().splitlines())

    def test_parent_supervisor_clamps_original_c3_origin(self):
        now = int(time.time() * 1000)
        real_boot = authority.boottime_ns
        shifted_boot = lambda: real_boot() + 777600000000000
        self.fixture.sample["boottimeNs"] = shifted_boot(); self.fixture.clock()
        self.fixture.plan["c3Execution"] = dict(startedUtcMs=now - 777599000, startedBoottimeNs=shifted_boot() - 777599000000000, bootId=authority.boot_identity(), journalPath=str(self.custody / "c3-retention-reclamation.journal.jsonl"))
        self.update_plan(self.fixture.plan)
        self.fixture.write("scenario.json", {"schema_version": 1, "target": self.fixture.plan["executionTarget"]}, active=False)
        marker = self.custody / "launched.txt"; marker.write_text("")
        child = b"import pathlib,sys,time\nif '--disable-only' in sys.argv: raise SystemExit(0)\npathlib.Path(" + repr(str(marker)).encode() + b").write_text('launched')\ntime.sleep(2)\nprint('{}')\n"
        output = self.custody / "budget.json"
        with ExitStack() as stack:
            stack.enter_context(patch.dict(os.environ, self.env))
            stack.enter_context(patch.object(authority, "boottime_ns", shifted_boot))
            self.source_fixture(stack, ("tools/access_telemetry_c3_producer.py", child))
            code = lifecycle.run_story_27_4_producer_checkpoint(checkpoint="c3-retention-reclamation", scenario_input_path=self.custody / "scenario.json", predecessor_path=self.custody / "predecessor.json", evidence_path=output, owner="owner", repository_root=ROOT, evidence_root=self.custody)
        self.assertEqual(1, code)
        self.assertEqual("launched", marker.read_text())
        self.assertIn("bounded timeout", json.loads(output.read_text())["reason"])
        self.fixture.plan["c3Execution"]["startedUtcMs"] -= 10000
        self.fixture.plan["c3Execution"]["startedBoottimeNs"] -= 10000000000
        self.update_plan(self.fixture.plan)
        with patch.dict(os.environ, self.env), patch.object(authority, "boottime_ns", shifted_boot), patch.object(lifecycle, "_run_bounded_process", side_effect=AssertionError("child started")):
            self.assertEqual(1, lifecycle.run_story_27_4_producer_checkpoint(checkpoint="c3-retention-reclamation", scenario_input_path=self.custody / "scenario.json", predecessor_path=self.custody / "predecessor.json", evidence_path=self.custody / "exhausted.json", owner="owner", repository_root=ROOT, evidence_root=self.custody))

    def test_actual_c3_prefix_resumes_multi_day_without_seed_replay(self):
        log = self.base / "operations.log"
        env, root, plan, scenario = self.cleanup_fixture(QUALIFICATION_OPERATION_LOG=str(log), QUALIFICATION_FAIL_OPERATION="cohort-1h-wait")
        path = root / "c3-retention-reclamation.journal.jsonl"
        original_ms = int(time.time() * 1000); original_boot = authority.boottime_ns()
        plan["c3Execution"] = dict(startedUtcMs=original_ms, startedBoottimeNs=original_boot, bootId=authority.boot_identity(), journalPath=str(path))
        self.update_plan(plan, env)
        resolver = functools.lru_cache(maxsize=100)(lifecycle._hash_git_blob)
        with patch.object(lifecycle, "_hash_git_blob", resolver), patch.dict(os.environ, env, clear=True):
            target = {**plan["executionTarget"], "_authority_required": True, "_platform_operations_reviewer": "owner-6775094"}
            producer._authorize_target(target)
            with self.assertRaisesRegex(lifecycle.EvidenceValidationError, "cohort-1h-wait"):
                producer._c3(target, path)
        prefix = [json.loads(line)["command_id"] for line in path.read_text().splitlines()]
        self.assertEqual(["retention-controls", "c3-empty-preflight", "cohort-168h-seed", "cohort-24h-seed", "cohort-1h-seed"], prefix)
        exact = path.read_bytes()
        state_path = path.with_name(path.name + ".state.json")
        continuation = dict(journalSha256=digest(exact), stateSha256=digest(state_path.read_bytes()))
        runtime = plan["target"]["initialRuntimeIdentitySha256"]
        future = datetime.fromtimestamp((original_ms + 3 * 86400000) / 1000, timezone.utc)
        with patch.object(authority, "boottime_ns", return_value=original_boot + 3 * 86400000000000):
            renewed = OwnerFixture(root, now=future, runtime_sha256=runtime)
            renewed.plan.update(c3Execution=plan["c3Execution"], continuation=continuation)
            renewed.approve(); env.update(renewed.env); env["HEXALITH_C1_EXECUTION_ID"] = "f" * 32
            env["HEXALITH_C1_PREDECESSOR_PATH"] = str(root / "predecessor.json")
            with patch.object(producer, "_utc_now_milliseconds", return_value=original_ms + 3 * 86400000), patch.object(lifecycle, "_hash_git_blob", resolver), patch.dict(os.environ, env, clear=True):
                target = {**renewed.plan["executionTarget"], "_authority_required": True, "_platform_operations_reviewer": "owner-6775094"}
                producer._authorize_target(target)
                with self.assertRaisesRegex(lifecycle.EvidenceValidationError, "cohort-1h-wait"):
                    producer._c3(target, path)
        self.assertEqual(exact, path.read_bytes())
        self.assertEqual(original_ms, json.loads(state_path.read_text())["started_utc_ms"])
        self.assertEqual(1, log.read_text().splitlines().count("cohort-168h-seed"))

    def test_owner_terminal_preflight_and_postflight_positive_authorization(self):
        # Use real source/packet/snapshot files and the actual complete consumers.
        # Only Git's clean/index observations are fixtures; no repository is staged.
        model = self.base / "model"; model.mkdir()
        profile = lifecycle.canonical_pg_onprem_2_profile().identity
        paths = set(profile["securityPlatform"]["secretAndConfigurationInputs"])
        paths.add("deploy/openbao/values.yaml")
        paths.update(("deploy/kubernetes/base/access-telemetry-postgresql.yaml", "deploy/kubernetes/base/access-telemetry-deployments.yaml", "deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml"))
        for checkpoint in (*lifecycle.STORY_27_4_CHECKPOINTS, "C0", "C5", "C6", "terminal", "adapter-profile"):
            paths.update(lifecycle._expected_source_paths(checkpoint))
        paths.update(lifecycle.OWNER_AUTHORITY_SOURCES)
        for path in paths:
            destination = model / path; destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes((ROOT / path).read_bytes())
        old_blobs = {}
        candidate = {}
        for path in lifecycle.A41_ALLOWED_MUTATION_PATHS:
            destination = model / path; destination.parent.mkdir(parents=True, exist_ok=True)
            old_blobs[path] = b"open fixture before close-out\n"; destination.write_bytes(old_blobs[path])
            candidate[path] = ("\n".join(lifecycle.A41_SEMANTIC_TRANSITIONS[path]["required"]) + "\n").encode()
        transition = manifest({path: digest(raw) for path, raw in candidate.items()})
        self.fixture.write("mutations.json", transition, active=False)
        def bind(payload):
            payload["source_commit"] = self.fixture.commit
            if payload["checkpoint"] in lifecycle.STORY_27_4_CHECKPOINTS:
                args = payload["producer"]["arguments"]
                args.update(authority_mode="sole-owner", authority_session_id=self.fixture.scope["sessionId"], authority_target_sha256=self.fixture.scope["targetSha256"])
                payload["producer"]["arguments_sha256"] = digest(j1_bytes(args))
                payload["commands"][0]["arguments_sha256"] = digest(j1_bytes(payload["commands"][0]["arguments"]))
                payload["source_hashes"].update({p: SHA for p in lifecycle.OWNER_AUTHORITY_SOURCES})
            payload["source_hashes"] = {p: digest((model / p).read_bytes().replace(b"\r\n", b"\n")) for p in payload["source_hashes"]}
            payload["producer"]["source_sha256"] = payload["source_hashes"][payload["producer"]["path"]]
            return payload
        raw_observation = self.fixture.write("adapter-observation.json", {"fixture": "same-profile observation"}, active=False)
        adapter = bind(common("adapter-profile"))
        adapter["results"] = dict(profile_id=lifecycle.EXPECTED_PROFILE_ID, profile_complete=True, runtime_matches_reviewed_profile=True, immutable_artifacts={"observed": dict(path=raw_observation["path"], sha256=raw_observation["sha256"])})
        adapter.update(status="passed", production_lifecycle_writes="disabled")
        adapter["packet_sha256"] = digest(j1_bytes(adapter))
        adapter_ref = self.fixture.write("adapter.json", adapter, active=False)
        c0 = bind(common("C0")); c0["status"] = "passed"
        c0["results"] = dict(adapter_profile_path=adapter_ref["path"], adapter_profile_sha256=adapter_ref["sha256"])
        artifacts = {"C0": self.fixture.write("C0.json", c0, active=False),
                     "C1": self.fixture.write("predecessor.json", self.fixture.predecessor, active=False)}
        for index, (name, factory) in enumerate((("C2", c2_payload), ("C3", c3_payload), ("C4", c4_payload))):
            payload = bind(factory())
            wrapper = lifecycle._checkpoint_packet(payload["checkpoint"], "passed", "owner", "fixture observed", observation=dict(input_sha256=SHA, producer_payload_sha256=digest(j1_bytes(payload)), producer_payload=payload, predecessor_sha256=artifacts["C1"]["sha256"], source_commit=self.fixture.commit, result_count=payload["result_count"]), evidence_mode="controlled-producer")
            wrapper["captured_utc"] = datetime.fromtimestamp((self.fixture.ms - 3000 + index * 1000) / 1000, timezone.utc).isoformat().replace("+00:00", "Z")
            wrapper.pop("packet_sha256"); wrapper["packet_sha256"] = digest(j1_bytes(wrapper))
            artifacts[name] = self.fixture.write(name + ".json", wrapper, active=False)
        accepted = {name: artifacts[name]["sha256"] for name in ("C0", "C1", "C2", "C3", "C4")}
        for name, role in (("C5", "platform-operations"), ("C6", "security")):
            payload = bind(common(name)); payload["status"] = "passed"
            payload["results"] = dict(role=role, approver="github:user:6775094", assurance_mode="sole-owner/self-reviewed", approved_utc_ms=self.fixture.ms, accepted_checkpoint_hashes=accepted)
            artifacts[name] = self.fixture.write(name + ".json", payload, active=False)
        terminal = bind(common("terminal")); terminal["status"] = "passed"
        terminal["results"] = dict(checkpoint_hashes={name: artifacts[name]["sha256"] for name in ("C0", "C1", "C2", "C3", "C4", "C5", "C6")}, failure_count=0, skip_count=0, result_count=7)
        artifacts["terminal"] = self.fixture.write("terminal.json", terminal, active=False)
        bundle = dict(profile_sha256=lifecycle.STORY_27_4_PROFILE_SHA256, checkpoints={name: dict(status="passed", profile_sha256=lifecycle.STORY_27_4_PROFILE_SHA256, artifact_path=ref["path"], artifact_sha256=ref["sha256"]) for name, ref in artifacts.items()})
        self.fixture.write("bundle.json", bundle, active=False)
        self.fixture.record["collectionEndsAtUtc"] = (self.fixture.now - timedelta(minutes=30)).isoformat(timespec="milliseconds").replace("+00:00", "Z")
        self.fixture.record["reviewEndsAtUtc"] = (self.fixture.now + timedelta(hours=23)).isoformat(timespec="milliseconds").replace("+00:00", "Z")
        self.fixture.plan["recordSha256"] = self.fixture.write("record.json", self.fixture.record, active=False)["sha256"]
        self.fixture.plan["terminalBundleSha256"] = digest(j1_bytes(bundle)); self.fixture.approve()
        state = {"staged": False}
        def git(root, *args):
            if args[:2] == ("rev-parse", "--verify"): return self.fixture.commit
            if args[0] == "status": return ""
            if args[0] == "branch": return "main"
            if args == ("remote",): return "origin"
            if args[:2] == ("remote", "get-url"): return "https://example.invalid/owner/repository.git"
            if args[:3] == ("diff", "--cached", "--name-only"): return "\0".join(lifecycle.A41_ALLOWED_MUTATION_PATHS) if state["staged"] else ""
            if args[0] in ("diff", "ls-files"): return ""
            raise AssertionError(args)
        inventory = dict(source_head=self.fixture.commit, allowed_mutations=list(lifecycle.A41_ALLOWED_MUTATION_PATHS), references=[dict(path=p, classification="close-out-mutable", sha256=digest(raw)) for p, raw in old_blobs.items()])
        pre = self.custody / "preflight.json"; snapshot = self.custody / "snapshot.json"
        with ExitStack() as stack:
            stack.enter_context(patch.dict(os.environ, self.fixture.env))
            stack.enter_context(patch.object(lifecycle, "_git_checked", git))
            stack.enter_context(patch.object(lifecycle, "_hash_git_blob", lambda root, commit, relative: digest((model / relative).read_bytes().replace(b"\r\n", b"\n"))))
            stack.enter_context(patch.object(lifecycle, "_collect_a41_inventory_and_blobs", return_value=(inventory, old_blobs)))
            stack.enter_context(patch.object(lifecycle, "_staged_blob_hash", lambda root, relative: digest((model / relative).read_bytes())))
            lifecycle._validate_terminal_bundle(bundle, model, self.custody)
            code = lifecycle.run_close_out_preflight(repository_root=model, bundle_path=self.custody / "bundle.json", mutation_manifest_path=self.custody / "mutations.json", snapshot_path=snapshot, evidence_path=pre, evidence_root=self.custody, remote="origin", branch="main")
            self.assertEqual(0, code, pre.read_text())
            for path, raw in candidate.items(): (model / path).write_bytes(raw)
            state["staged"] = True
            post = self.custody / "postflight.json"
            code = lifecycle.run_close_out_postflight(repository_root=model, preflight_path=pre, mutation_manifest_path=self.custody / "mutations.json", snapshot_path=snapshot, evidence_path=post, evidence_root=self.custody)
            self.assertEqual(0, code, post.read_text())
            self.fixture.plan["terminalBundleSha256"] = "f" * 64
            self.fixture.approve(); os.environ.update(self.fixture.env)
            with self.assertRaises(lifecycle.EvidenceValidationError): lifecycle._validate_terminal_bundle(bundle, model, self.custody)
