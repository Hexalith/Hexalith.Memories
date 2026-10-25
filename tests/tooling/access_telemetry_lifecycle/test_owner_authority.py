"""Real private-file owner consumer/continuation tests. Fixtures grant no live credit."""
from contextlib import ExitStack
from copy import deepcopy
from datetime import datetime, timedelta, timezone
import hashlib
import hmac
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

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))
import access_telemetry_c1_owner_authority as authority
import access_telemetry_producer_common as producer
import verify_access_telemetry_lifecycle as lifecycle
from access_telemetry_c1_interchange import j1_bytes
from access_telemetry_c1_owner_run import _PROFILE, _WORKLOAD

SHA = "a" * 64
OTHER = "b" * 64

def digest(raw):
    return hashlib.sha256(raw).hexdigest()

def technical(number):
    same = {"beforeSha256": SHA, "afterSha256": SHA}
    if number == 1:
        return dict(component="state.postgresql/v2", createSha256=SHA, readSha256=SHA, updateSha256=OTHER, updatedReadSha256=OTHER, finalReadStatus="absent", consistency="strong")
    if number == 2:
        return dict(writeKeySha256=SHA, readKeySha256=SHA, acknowledgedSha256=SHA, readSha256=SHA, acknowledgedUtcMs=10, readUtcMs=11, consistency="strong")
    if number == 3:
        return dict(matchingAccepted=True, mismatchedStatus=409, staleStatus=409, priorSha256=SHA, afterConflictsSha256=SHA, firstWriteWithEtag="conflict", firstWriteWithoutEtag="accepted", catalogueWinnerSha256=SHA, afterRetrySha256=SHA, originalExpiryUtcMs=100, retryExpiryUtcMs=100)
    if number == 4:
        return dict(operationCount=2, failedOperationIndex=1, responseStatus=409, recordBeforeSha256=SHA, recordAfterSha256=SHA, indexBeforeSha256=SHA, indexAfterSha256=SHA)
    if number == 5:
        return dict(emittedUtcMs=1000, expiryUtcMs=3601000, retryExpiryUtcMs=3601000, ttlSeconds=3600, preReadUtcMs=3600000, preReadCount=125, absentReadUtcMs=3601001, absentReadCount=125, cohortCount=125, newerBeforeSha256=SHA, newerAfterSha256=SHA)
    if number in (6, 7, 19):
        result = dict(oldIdentity="old-member", newIdentity="new-member", **same)
        if number == 6:
            result.update(cursorsBeforeSha256=SHA, cursorsAfterSha256=SHA, remindersBeforeSha256=SHA, remindersAfterSha256=SHA)
        elif number == 7:
            result.update(reminderBefore=0, reminderAfter=1, duplicateSideEffects=0)
        else:
            result.update(fault="postgresql-pod-process-replacement", boundary="healthy-node-retained-volume", acknowledgedInventory=[SHA], durableInventory=[SHA])
        return result
    if number == 8:
        return dict(recordBytes=1024, transactionBytes=1048576, transactionRecords=256, storedBytes=1048576, storedRecords=256, sentSha256=SHA, storedSha256=SHA, oversizedStatus=413, overCountStatus=400)
    if number == 9:
        writers = [dict(identity=f"writer-{n}", startedUtcMs=10, finishedUtcMs=1800010, emitted=225000, acknowledged=225000, persisted=225000, inventorySha256=SHA) for n in (1, 2)]
        return dict(writers=writers, componentWindowMs=1800000, componentOperationDelta=900000, p95Microseconds=100, p99Microseconds=200, purgeBefore=0, purgeAfter=1)
    if number == 10:
        return dict(dueBefore=150000, dueAfter=0, interruptionMs=600000, drainMs=10000, oldestDueAgeSeconds=1, newerBeforeSha256=SHA, newerAfterSha256=SHA)
    if number == 11:
        surfaces = ["write", "storage", "inspect", "purge", "erase"]
        denials = [dict(surface=s, scope=c, status=403, dependencyCalls=0, disclosedCount=0, mutatedCount=0, testName=f"denial-{s}-{c}") for s in surfaces for c in ("foreign", "unknown", "malformed", "empty", "mismatched")]
        return dict(principalA="tenant-A", principalB="tenant-B", affectedSurfaces=surfaces, denials=denials)
    if number == 12:
        return dict(postgresSslMode="verify-full", postgresTls=True, openBaoTls=True, badCaStatus="denied", badHostnameStatus="denied", crossPrefixStatus=403, atRestDeviceSha256=SHA, atRestObservationSha256=OTHER)
    if number == 13:
        operands = dict(records=1, measured_record_bytes="1024B", measured_index_bytes="100B", durability_multiplier=2, control_bytes="10B", reclamation_workspace="1000B", scheduler_bytes="10B", host_filesystem_headroom_bytes="1000B", horizons={"1h": 900000, "24h": 21600000, "7d": 151200000})
        return dict(operands=operands, expectedAdmissions=[a.__dict__ for a in lifecycle.evaluate_capacity_admission(lifecycle.calculate_capacity(**operands))])
    if number == 14:
        return dict(database="memories_access_telemetry", schema="access_telemetry", table="lifecycle_state", cohortSha256=SHA, deletedCount=125, postTupleCount=0, purgedUtcMs=100, reclaimedUtcMs=101, allocatorFreeBefore=0, allocatorFreeAfter=100, osDiskShrinkClaimed=False, newerBeforeSha256=SHA, newerAfterSha256=SHA, collector="pgstattuple")
    if number == 15:
        return dict(runtimeImage=lifecycle.EXPECTED_RUNTIME_IMAGE, runtimeVersion="1.18.1", appId="memories-access-telemetry", actorType="AccessTelemetryLifecycleActor", schedulerMembers=["s1", "s2", "s3"], placementMembers=["p1", "p2", "p3"], independentClockSource="platform-clock")
    if number == 16:
        return dict(componentType="state.postgresql", componentVersion="v2", backendImage=lifecycle.EXPECTED_POSTGRESQL_IMAGE, serverVersion="18.6", actualConnectionSha256=SHA, capabilities=["ETAG", "TRANSACTIONAL", "TTL"])
    if number == 17:
        return dict(images={"app": "sha256:" + SHA}, manifestSha256=SHA, configurationEpoch="epoch", profileSha256=_PROFILE, sourceCommit="assigned", coveredInputsSha256=SHA)
    if number == 18:
        return dict(nodeIdentity="node", storageIdentity="volume", reservedBytes=429496729600, availableBytes=429496729600, requestedBytes=429496729600, cpuCount=8, memoryBytes=17179869184, costOwner="owner", quoteUtcMs=1, operatingCostMinorUnits=10, currency="EUR")
    if number in (20, 24):
        return dict(excludedFailureDomains=["node", "volume", "control-plane", "site"], highAvailability=False, ownerPrincipal="github:user:6775094", evidenceHashes={"exclusions": SHA})
    if number == 21:
        return dict(backupNode="independent", protectedNode="node", restoreTargetProduction=False, backupInventory=[SHA], restoredInventory=[SHA], erasedInventory=[OTHER], resurrectedInventory=[], rpoMs=1, rtoMs=1, destinationSha256=OTHER)
    if number == 22:
        return dict(rpoMs=1, rtoMs=1, highAvailability=False, restoreEvidenceSha256=SHA, faultEvidenceSha256=OTHER)
    return dict(role="platform-operations" if number == 23 else "security", ownerPrincipal="github:user:6775094", acceptedEvidenceHashes={"parents": SHA}, acceptedRisks=["single-node"])


class OwnerFixture:
    def __init__(self, root, *, now=None, runtime_sha256=SHA):
        self.root = root
        root.chmod(0o700)
        self.now = datetime.now(timezone.utc) if now is None else now
        self.ms = int(self.now.timestamp() * 1000)
        self.commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
        self.source = "tools/access_telemetry_c1_owner_run.py"
        source_sha = digest(subprocess.check_output(["git", "show", f"{self.commit}:{self.source}"], cwd=ROOT))
        self.target = dict(apiServer="https://fixture.invalid:6443", apiCaSha256=digest(b"fixture-api-ca"), clusterUid="fixture-cluster", namespaceUid="fixture-namespace", initialRuntimeIdentitySha256=runtime_sha256, context="operator@local", namespace="memories-qualification", selector="app=qualification", appId="memories-access-telemetry", actorType="AccessTelemetryLifecycleActor")
        self.scope = dict(profileId="PG-ONPREM-2", profileSha256=_PROFILE, workloadSha256=_WORKLOAD, sessionId="fixture-session", sourceCommit=self.commit, targetSha256=digest(j1_bytes(self.target)), ownerPrincipal="github:user:6775094", assuranceMode="sole-owner/self-reviewed")
        self.active = set()
        self.contracts = {}
        self.gates = {}
        self.refs = {}
        pending = set(range(1, 26))
        order = []
        while pending:
            number = next(n for n in sorted(pending) if set(authority.PARENTS[n]) <= set(order))
            order.append(number); pending.remove(number)
        for index, number in enumerate(order):
            name = f"C1.{number}"
            start = self.ms - 70 * 3600000 + index * 7200000
            facts = technical(number)
            if number == 17:
                facts["sourceCommit"] = self.commit
            if number == 15:
                facts.update({key: self.target[key] for key in ("apiServer", "apiCaSha256", "clusterUid", "namespaceUid", "initialRuntimeIdentitySha256")})
            def shift_times(item):
                if isinstance(item, dict):
                    return {key: child + start if key.endswith("UtcMs") else shift_times(child) for key, child in item.items()}
                if isinstance(item, list):
                    return [shift_times(child) for child in item]
                return item
            facts = shift_times(facts)
            observations = {key: True for key in authority.CLAIMS[number].split()}
            output = dict(gate=name, observations=observations, technical=facts, **self.scope)
            stdout = self.write(f"stdout-{number}.json", output)
            stderr = self.write(f"stderr-{number}.txt", b"")
            args = dict(gate=name, target_sha256=self.scope["targetSha256"], session_id=self.scope["sessionId"])
            command = dict(command_id=name + "-observe", arguments=args, arguments_sha256=digest(j1_bytes(args)), started_utc_ms=start, finished_utc_ms=start + 5400000, exit_code=0, stdout_sha256=stdout["sha256"], stderr_sha256=stderr["sha256"], result_count=1)
            contract = dict(sourcePath=self.source, sourceSha256=source_sha, commandId=command["command_id"], argumentsSha256=command["arguments_sha256"], cleanupRequired=True)
            contract.update(latencyLimits=dict(p95Microseconds=1000, p99Microseconds=2000), maxDrainMs=600000, configurationEpoch="epoch", images={"app": "sha256:" + SHA}, manifestSha256=SHA, coveredInputsSha256=SHA, nodeIdentity="node", storageIdentity="volume", maxRpoMs=1000, maxRtoMs=1000)
            self.contracts[name] = contract
            cleanup = self.write(f"cleanup-{number}.json", dict(gate=name, state="restored-approved-baseline", finishedUtcMs=start + 5400000, gateBefore="disabled", gateAfter="disabled", leaseEmpty=True, lifecycleReplicas=0, clockReplicas=0, ownedSyntheticRemoved=True, **self.scope))
            parent_hashes = {f"C1.{p}": self.refs[f"C1.{p}"]["sha256"] for p in authority.PARENTS[number]}
            if number in (20, 24):
                facts["evidenceHashes"] = parent_hashes
            if number in (23, 25):
                facts["acceptedEvidenceHashes"] = parent_hashes
            if number == 22:
                facts["restoreEvidenceSha256"] = self.refs["C1.21"]["sha256"]
                facts["faultEvidenceSha256"] = self.refs["C1.19"]["sha256"]
            # Technical stdout is the exact same structured result as the packet.
            stdout = self.write(f"stdout-{number}.json", dict(gate=name, observations=observations, technical=facts, **self.scope))
            command["stdout_sha256"] = stdout["sha256"]
            packet = dict(schemaVersion=authority.PREFIX + "owner-gate/v1", gate=name, startedUtcMs=start, finishedUtcMs=start + 5400000, decidedUtcMs=start + 5400100, expiresUtcMs=self.ms + 86400000, decision="accepted", reviewerRole="security" if number in (11, 12, 15, 16, 25) else "platform-operations", reasons=["measured synthetic fixture"], parents={f"C1.{p}": self.refs[f"C1.{p}"]["sha256"] for p in authority.PARENTS[number]}, sourcePath=self.source, sourceSha256=source_sha, commands=[command], commandStreams={command["command_id"]: dict(stdout=stdout, stderr=stderr)}, technicalCommandId=command["command_id"], technical=facts, resultCount=1, failureCount=0, skipCount=0, observations=observations, cleanup=dict(required=True, receipts=[cleanup], finalState="restored-approved-baseline"), **self.scope)
            self.gates[name] = packet
            self.refs[name] = self.write(f"gate-{number}.json", packet)
        manifest = dict(schemaVersion=authority.PREFIX + "owner-manifest/v1", createdUtcMs=self.ms - 3000000, gates=[dict(gate=g, artifact=self.refs[g]) for g in authority.GATES], **self.scope)
        manifest_ref = self.write("manifest.json", manifest)
        approvals = [self.write(f"approval-{role}.json", dict(schemaVersion=authority.PREFIX + "owner-bundle-approval/v1", manifestSha256=manifest_ref["sha256"], reviewerRole=role, decision="accepted", decidedUtcMs=self.ms - 2999000, expiresUtcMs=self.ms + 86400000, reasons=["fixture review"], **self.scope)) for role in ("platform-operations", "security")]
        self.predecessor = dict(schemaVersion=authority.SCHEMA, manifest=manifest_ref, approvals=approvals, status="passed", productionLifecycleWrites="disabled", qualificationAuthorized=True)
        self.write("predecessor.json", self.predecessor, active=False)
        def utc(dt):
            return dt.isoformat(timespec="milliseconds").replace("+00:00", "Z")
        self.record = dict(schemaVersion=authority.PREFIX + "owner-run/v1", policyId="sole-owner-qualification/2026-10-12", policySha256=SHA, environment="non-production", custodyRoot=str(root), actions=["observe", "fault", "purge", "reclaim", "close-out", "publish"], notBeforeUtc=utc(self.now - timedelta(hours=70)), collectionEndsAtUtc=utc(self.now + timedelta(hours=2)), reviewEndsAtUtc=utc(self.now + timedelta(hours=26)), expiresAtUtc=utc(self.now + timedelta(days=7) - timedelta(hours=70)), status="approved", **self.scope)
        # owner-run uses exactly its own scope fields, no additional wire fields.
        self.record.pop("technical", None)
        record_ref = self.write("record.json", self.record, active=False)
        self.plan = dict(recordSha256=record_ref["sha256"], policySha256=SHA, sessionId=self.scope["sessionId"], sourceCommit=self.commit, targetSha256=self.scope["targetSha256"], custodyRoot=str(root), statusPath=str(root / "withdrawal.json"), recordPath=str(root / "record.json"), predecessorSha256=authority.canonical_predecessor_sha256(self.predecessor), clockSamplePath=str(root / "clock.json"), clockKeyPath=str(root / "clock.key"), clockSourceId="independent-platform-clock", authorityStatusPath=str(root / "authority-status.json"), gateContracts=self.contracts, target=self.target, executionTarget=dict(kind="non-production-qualification", kube_context=self.target["context"], namespace=self.target["namespace"], profile_sha256=_PROFILE))
        self.key = b"independent-UTC-fixture-key-32-bytes"
        self.plan["clockKeySha256"] = digest(self.key)
        self.write("clock.key", self.key, active=False)
        self.approve()

    def write(self, name, value, *, active=True):
        raw = value if type(value) is bytes else j1_bytes(value)
        path = self.root / name
        path.write_bytes(raw); path.chmod(0o600)
        result = dict(path=name, sha256=digest(raw), byteLength=len(raw))
        if active:
            self.active.add(result["sha256"])
        return result

    def approve(self):
        self.plan["authorization"] = dict(issuedUtcMs=self.ms, expiresUtcMs=self.ms + 3600000, actions=["observe", "fault", "purge", "reclaim", "close-out", "publish"])
        ref = self.write("plan.json", self.plan, active=False)
        self.env = dict(HEXALITH_C1_OWNER_PLAN=str(self.root / "plan.json"), HEXALITH_C1_APPROVED_PLAN_SHA256=ref["sha256"], HEXALITH_C1_CUSTODY_ROOT=str(self.root))
        self.write("withdrawal.json", dict(planSha256=ref["sha256"], recordSha256=self.plan["recordSha256"], sessionId=self.plan["sessionId"], withdrawn=False), active=False)
        self.status = dict(planSha256=ref["sha256"], recordSha256=self.plan["recordSha256"], sessionId=self.plan["sessionId"], updatedUtcMs=self.ms, issuedUtcMs=self.ms, expiresUtcMs=self.ms + 900000, actions=["observe", "fault", "purge", "reclaim", "close-out", "publish"], activeSha256=sorted(self.active))
        self.write("authority-status.json", self.status, active=False)
        self.sample = dict(utcMs=self.ms, boottimeNs=authority.boottime_ns(), bootId=authority.boot_identity(), sourceId="independent-platform-clock")
        self.clock()

    def clock(self):
        signature = hmac.new(self.key, j1_bytes(self.sample), hashlib.sha256).hexdigest()
        self.write("clock.json", dict(signature=signature, **self.sample), active=False)

    def rebind(self):
        """A fresh fixture approval cannot override technical/dependency policy."""
        self.active = set()
        for name, packet in self.gates.items():
            number = int(name.split(".")[1])
            packet["parents"] = {f"C1.{p}": self.refs[f"C1.{p}"]["sha256"] for p in authority.PARENTS[number]}
            if number in (20, 24):
                packet["technical"]["evidenceHashes"] = dict(packet["parents"])
            if number in (23, 25):
                packet["technical"]["acceptedEvidenceHashes"] = dict(packet["parents"])
            if number == 22:
                packet["technical"]["restoreEvidenceSha256"] = self.refs["C1.21"]["sha256"]
                packet["technical"]["faultEvidenceSha256"] = self.refs["C1.19"]["sha256"]
            output = dict(gate=name, observations=packet["observations"], technical=packet["technical"], **self.scope)
            command = packet["commands"][0]
            stdout = self.write(f"stdout-{number}.json", output)
            stderr = self.write(f"stderr-{number}.txt", b"")
            command["stdout_sha256"] = stdout["sha256"]
            command["stderr_sha256"] = stderr["sha256"]
            packet["commandStreams"][command["command_id"]] = dict(stdout=stdout, stderr=stderr)
            self.active.update(r["sha256"] for r in packet["cleanup"]["receipts"])
            self.refs[name] = self.write(f"gate-{number}.json", packet)
        manifest = dict(schemaVersion=authority.PREFIX + "owner-manifest/v1", createdUtcMs=self.ms - 3000000, gates=[dict(gate=g, artifact=self.refs[g]) for g in authority.GATES], **self.scope)
        manifest_ref = self.write("manifest.json", manifest)
        approvals = [self.write(f"approval-{role}.json", dict(schemaVersion=authority.PREFIX + "owner-bundle-approval/v1", manifestSha256=manifest_ref["sha256"], reviewerRole=role, decision="accepted", decidedUtcMs=self.ms - 2999000, expiresUtcMs=self.ms + 86400000, reasons=["fixture review"], **self.scope)) for role in ("platform-operations", "security")]
        self.predecessor.update(manifest=manifest_ref, approvals=approvals)
        self.write("predecessor.json", self.predecessor, active=False)
        self.plan["predecessorSha256"] = authority.canonical_predecessor_sha256(self.predecessor)
        self.approve()

    def verify(self, **kwargs):
        with patch.dict(os.environ, self.env):
            return lifecycle._validate_predecessor(self.predecessor, ROOT, self.root, **kwargs)


class OwnerAuthorityTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.fixture = OwnerFixture(Path(self.temporary.name))

    def test_all_25_real_private_packets_allow_exact_owner_action(self):
        result = self.fixture.verify()
        self.assertEqual("fixture-session", result.session_id)
        self.assertEqual(25, len(self.fixture.gates))
        self.assertEqual(result.session_id, self.fixture.verify(require_authorization_freshness=False).session_id)

    def test_summary_cannot_appoint_own_outside_approval_root(self):
        with patch.dict(os.environ, {}, clear=True):
            with self.assertRaises(lifecycle.EvidenceValidationError):
                lifecycle._validate_predecessor(self.fixture.predecessor, ROOT, self.fixture.root)
        self.fixture.env["HEXALITH_C1_APPROVED_PLAN_SHA256"] = SHA
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.fixture.verify()

    def test_every_packet_and_command_byte_is_required(self):
        for number in range(1, 26):
            path = self.fixture.root / f"gate-{number}.json"
            raw = path.read_bytes(); path.write_bytes(raw + b" ")
            with self.subTest(gate=number), self.assertRaises(lifecycle.EvidenceValidationError):
                self.fixture.verify()
            path.write_bytes(raw)
        path = self.fixture.root / "stdout-1.json"
        path.write_bytes(b"{\"unrelated\":true}")
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.fixture.verify()

    def test_wrong_owner_profile_session_source_target_and_phase_refuse(self):
        for field, replacement in (("ownerPrincipal", "github:user:1"), ("profileSha256", SHA), ("sessionId", "foreign-session"), ("sourceCommit", "a" * 40), ("targetSha256", OTHER)):
            original = deepcopy(self.fixture.record)
            self.fixture.record[field] = replacement
            self.fixture.plan["recordSha256"] = self.fixture.write("record.json", self.fixture.record, active=False)["sha256"]
            self.fixture.approve()
            with self.subTest(field=field), self.assertRaises(lifecycle.EvidenceValidationError):
                self.fixture.verify()
            self.fixture.record = original
        self.fixture.plan["recordSha256"] = self.fixture.write("record.json", self.fixture.record, active=False)["sha256"]
        self.fixture.approve()
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.fixture.verify(action="publish")

    def test_current_reference_revocation_unknown_expiry_clock_and_withdrawal_refuse(self):
        original = deepcopy(self.fixture.status)
        for changes in (dict(activeSha256=[]), dict(updatedUtcMs=self.fixture.ms - 300001), dict(expiresUtcMs=self.fixture.ms), dict(issuedUtcMs=self.fixture.ms + 100, expiresUtcMs=self.fixture.ms + 3600001), dict(actions=[]), dict(sessionId="foreign-session")):
            self.fixture.write("authority-status.json", dict(original, **changes), active=False)
            with self.subTest(changes=changes), self.assertRaises(lifecycle.EvidenceValidationError):
                self.fixture.verify()
        self.fixture.write("authority-status.json", original, active=False)
        self.fixture.sample["boottimeNs"] -= 60000000001
        self.fixture.clock()
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.fixture.verify()
        self.fixture.approve()
        withdrawal = json.loads((self.fixture.root / "withdrawal.json").read_text()); withdrawal["withdrawn"] = True
        self.fixture.write("withdrawal.json", withdrawal, active=False)
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.fixture.verify()

    def test_replaced_clock_key_and_resigned_time_cannot_appoint_trusted_utc(self):
        self.fixture.key = b"attacker-replacement-key-32-bytes-xxxx"
        self.fixture.write("clock.key", self.fixture.key, active=False)
        self.fixture.clock()
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.fixture.verify()

    def test_duplicate_bundle_role_and_read_only_inspection_never_grant_authority(self):
        self.fixture.predecessor["approvals"][1] = self.fixture.predecessor["approvals"][0]
        self.fixture.plan["predecessorSha256"] = authority.canonical_predecessor_sha256(self.fixture.predecessor)
        self.fixture.approve()
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.fixture.verify()
        inspection = {"schemaVersion": authority.PREFIX + "owner-run/v1", "recordSha256": self.fixture.plan["recordSha256"], "qualificationAuthorized": True}
        with self.assertRaises(lifecycle.EvidenceValidationError):
            lifecycle._validate_predecessor(inspection, ROOT, self.fixture.root)

    def test_private_custody_symlink_or_shared_permissions_refuse(self):
        path = self.fixture.root / "gate-1.json"
        path.chmod(0o644)
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.fixture.verify()
        path.chmod(0o600)
        original = self.fixture.root / "saved-gate-1.json"
        path.rename(original); path.symlink_to(original)
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.fixture.verify()

    def test_rehashed_gate_facts_cannot_override_technical_policy(self):
        invalid = {
            1: ("readSha256", OTHER), 2: ("consistency", "eventual"),
            3: ("retryExpiryUtcMs", 101), 4: ("indexAfterSha256", OTHER),
            5: ("absentReadUtcMs", 99999999), 6: ("newIdentity", "old-member"),
            7: ("duplicateSideEffects", 1), 8: ("storedRecords", 255),
            9: ("componentOperationDelta", 899999), 10: ("dueBefore", 149999),
            11: ("principalB", "tenant-A"), 12: ("postgresSslMode", "require"),
            13: ("expectedAdmissions", []), 14: ("allocatorFreeAfter", 0),
            15: ("runtimeVersion", "1.18.7"), 16: ("serverVersion", "18.4"),
            17: ("sourceCommit", "f" * 40), 18: ("reservedBytes", 1),
            19: ("boundary", "site-loss"), 20: ("highAvailability", True),
            21: ("restoreTargetProduction", True), 22: ("rpoMs", 0),
            23: ("role", "security"), 24: ("highAvailability", True), 25: ("acceptedRisks", []),
        }
        for number, (field, value) in invalid.items():
            packet = self.fixture.gates[f"C1.{number}"]
            original = deepcopy(packet["technical"])
            packet["technical"][field] = value
            self.fixture.rebind()
            with self.subTest(gate=number), self.assertRaises(lifecycle.EvidenceValidationError):
                self.fixture.verify()
            packet["technical"] = original
        self.fixture.rebind()
        self.fixture.verify()

    def test_counts_distinct_role_records_cleanup_and_parent_order_are_enforced(self):
        packet = self.fixture.gates["C1.25"]
        for field, value in (("failureCount", False), ("skipCount", False), ("resultCount", 2), ("reviewerRole", "platform-operations")):
            original = packet[field]
            packet[field] = value
            self.fixture.rebind()
            with self.subTest(field=field), self.assertRaises(lifecycle.EvidenceValidationError):
                self.fixture.verify()
            packet[field] = original
        original_cleanup = deepcopy(packet["cleanup"])
        packet["cleanup"]["required"] = False
        packet["cleanup"]["receipts"] = []
        packet["cleanup"]["finalState"] = "read-only-no-owned-mutation"
        self.fixture.rebind()
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.fixture.verify()
        packet["cleanup"] = original_cleanup
        # The source/output-derived parent graph also rejects a review before C1.24.
        packet = self.fixture.gates["C1.23"]
        packet["startedUtcMs"] = self.fixture.gates["C1.24"]["decidedUtcMs"]
        self.fixture.rebind()
        with patch.dict(os.environ, self.fixture.env), self.assertRaisesRegex(authority.OwnerAuthorityError, "parent-not-accepted"):
            authority.verify_owner_predecessor(self.fixture.predecessor, ROOT, self.fixture.root)

    def test_chain_end_rechecks_expiry_and_whole_chain_budget(self):
        original_check = lifecycle.validate_current_profile_inputs
        fixed_boot = authority.boottime_ns()
        for elapsed in (300000000001, 900000000001):
            clock = [fixed_boot]
            self.fixture.sample["boottimeNs"] = fixed_boot
            self.fixture.clock()
            def delayed_profile(root):
                original_check(root)
                clock[0] += elapsed
            with self.subTest(elapsed=elapsed), patch.object(authority, "boottime_ns", lambda: clock[0]), patch.object(lifecycle, "validate_current_profile_inputs", delayed_profile):
                with self.assertRaises(lifecycle.EvidenceValidationError):
                    self.fixture.verify()
        # An accepted gate expires during otherwise fresh verification.
        packet = self.fixture.gates["C1.25"]
        packet["expiresUtcMs"] = self.fixture.ms + 1
        self.fixture.rebind()
        clock = [fixed_boot]
        self.fixture.sample["boottimeNs"] = fixed_boot
        self.fixture.clock()
        def cross_expiry(root):
            original_check(root)
            clock[0] += 2000000
        with patch.object(authority, "boottime_ns", lambda: clock[0]), patch.object(lifecycle, "validate_current_profile_inputs", cross_expiry):
            with self.assertRaises(lifecycle.EvidenceValidationError):
                self.fixture.verify()

    def test_offline_checkpoint_uses_actual_consumer_and_refuses_mixed_session(self):
        from test_retention_verification import c2_payload
        payload = c2_payload()
        payload["source_commit"] = self.fixture.commit
        args = payload["producer"]["arguments"]
        args.update(authority_mode="sole-owner", authority_session_id=self.fixture.scope["sessionId"], authority_target_sha256=self.fixture.scope["targetSha256"])
        payload["producer"]["arguments_sha256"] = digest(j1_bytes(args))
        payload["commands"][0]["arguments_sha256"] = digest(j1_bytes(payload["commands"][0]["arguments"]))
        source_paths = set(payload["source_hashes"]) | set(lifecycle.OWNER_AUTHORITY_SOURCES)
        # These source files have intentionally not been committed by the task.
        # Model the committed source resolver with a real frozen byte directory;
        # every source digest is still recomputed through the actual validator.
        with tempfile.TemporaryDirectory() as directory:
            frozen = Path(directory)
            for relative in source_paths:
                destination = frozen / relative
                destination.parent.mkdir(parents=True, exist_ok=True)
                destination.write_bytes((ROOT / relative).read_bytes())
            payload["source_hashes"] = {p: digest((frozen / p).read_bytes()) for p in source_paths}
            payload["producer"]["source_sha256"] = payload["source_hashes"][payload["producer"]["path"]]
            original = lifecycle._hash_git_blob
            def frozen_blob(root, commit, relative):
                if commit == self.fixture.commit and relative in source_paths:
                    return digest((frozen / relative).read_bytes())
                return original(root, commit, relative)
            for foreign in (False, True):
                if foreign:
                    args["authority_session_id"] = "foreign-session"
                    payload["producer"]["arguments_sha256"] = digest(j1_bytes(args))
                    payload["commands"][0]["arguments_sha256"] = digest(j1_bytes(payload["commands"][0]["arguments"]))
                self.fixture.write("payload.json", payload, active=False)
                output = self.fixture.root / ("foreign-result.json" if foreign else "offline-result.json")
                with patch.dict(os.environ, self.fixture.env), patch.object(lifecycle, "_hash_git_blob", frozen_blob), patch.object(lifecycle, "_run_bounded_process", side_effect=AssertionError("target contacted")):
                    code = lifecycle.run_story_27_4_checkpoint(checkpoint="c2-production-replacement", input_path=self.fixture.root / "payload.json", predecessor_path=self.fixture.root / "predecessor.json", evidence_path=output, owner="owner-6775094", repository_root=ROOT, evidence_root=self.fixture.root)
                self.assertEqual(1 if foreign else 0, code, output.read_text())
                packet = json.loads(output.read_text())
                self.assertEqual("rejected" if foreign else "passed", packet["status"])
                self.assertEqual("offline-validation", packet["evidence_mode"])

    def test_cli_denies_foreign_plan_before_target_or_dependent_files(self):
        output = self.fixture.root / "cli-refused.json"
        command = [sys.executable, "-B", str(ROOT / "tools/verify-access-telemetry-lifecycle.py"),
                   "--checkpoint", "c2-production-replacement", "--owner-plan", self.fixture.env["HEXALITH_C1_OWNER_PLAN"],
                   "--approved-owner-plan-sha256", SHA, "--owner-custody-root", str(self.fixture.root),
                   "--predecessor", str(self.fixture.root / "predecessor.json"), "--evidence-root", str(self.fixture.root),
                   "--evidence", str(output), "--owner", "fixture", "--scenario-input", str(self.fixture.root / "missing-target.json")]
        result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, timeout=10)
        self.assertEqual(1, result.returncode)
        self.assertEqual("rejected", json.loads(output.read_text())["status"])
        self.assertIn("C1 owner authority refused", output.read_text())
        self.assertFalse((self.fixture.root / "missing-target.json").exists())

    def test_actual_api_ca_cluster_and_namespace_mismatch_deny_before_mutation(self):
        expected = self.fixture.target
        import base64
        valid = [(0, (expected["apiServer"] + "\n" + base64.b64encode(b"fixture-api-ca").decode() + "\n").encode(), b""),
                 (0, j1_bytes(dict(metadata=dict(name="kube-system", uid=expected["clusterUid"]))), b""),
                 (0, j1_bytes(dict(metadata=dict(name=self.fixture.target["namespace"], uid=expected["namespaceUid"]))), b"")]
        for position, raw in ((0, b"https://foreign.invalid\nZm9yZWlnbg==\n"),
                              (0, (expected["apiServer"] + "\nZm9yZWlnbg==\n").encode()),
                              (1, j1_bytes(dict(metadata=dict(name="kube-system", uid="foreign-cluster")))),
                              (2, j1_bytes(dict(metadata=dict(name=self.fixture.target["namespace"], uid="foreign-namespace"))))):
            observed = list(valid); observed[position] = (0, raw, b"")
            target = {**self.fixture.plan["executionTarget"], "_authority_required": True}
            with self.subTest(position=position), patch.dict(os.environ, dict(self.fixture.env, HEXALITH_C1_PREDECESSOR_PATH=str(self.fixture.root / "predecessor.json"), HEXALITH_C1_REPOSITORY_ROOT=str(ROOT))), patch.object(producer, "_run_bounded_process", side_effect=observed), patch.object(producer, "_run_locked_operation", side_effect=AssertionError("mutation accessed")):
                with self.assertRaises(lifecycle.EvidenceValidationError):
                    producer._run_operation(target, "c3-retention-reclamation", "qualification-enable")

    def test_changed_initial_runtime_refuses_before_enable(self):
        from test_retention_verification import install_fake_kubectl
        fake_bin = self.fixture.root / "bin"
        fake_bin.mkdir(); install_fake_kubectl(fake_bin)
        target_path = self.fixture.root / "target.json"
        self.fixture.write("target.json", dict(schema_version=1, target=self.fixture.plan["executionTarget"]), active=False)
        log = self.fixture.root / "operations.log"
        environment = dict(os.environ, **self.fixture.env, HEXALITH_C1_PREDECESSOR_PATH=str(self.fixture.root / "predecessor.json"), HEXALITH_C1_REPOSITORY_ROOT=str(ROOT), QUALIFICATION_OPERATION_LOG=str(log), HEXALITH_STORY_27_4_INLINE_KUBECTL=str(fake_bin / "kubectl"), HEXALITH_STORY_27_4_INLINE_KUBECTL_UNITTEST="1")
        environment["PATH"] = str(fake_bin) + os.pathsep + environment.get("PATH", "")
        result = subprocess.run([sys.executable, "-B", str(ROOT / "tools/access_telemetry_c2_producer.py"), "--scenario-input", str(target_path), "--platform-operations-reviewer", "owner-6775094"], cwd=ROOT, env=environment, capture_output=True, text=True, timeout=15)
        self.assertEqual(1, result.returncode)
        self.assertIn("approved initial input", result.stderr)
        self.assertEqual(["qualification-target-identity"], log.read_text().splitlines())

    def test_frozen_source_import_closure_is_complete(self):
        with tempfile.TemporaryDirectory() as directory:
            frozen = Path(directory)
            sources = {"tools/access_telemetry_c2_producer.py", "tools/access_telemetry_producer_common.py", "tools/verify_access_telemetry_lifecycle.py", *lifecycle.OWNER_AUTHORITY_SOURCES}
            for relative in sources:
                (frozen / Path(relative).name).write_bytes((ROOT / relative).read_bytes())
            result = subprocess.run([sys.executable, "-B", str(frozen / "access_telemetry_c2_producer.py"), "--help"], cwd=frozen, capture_output=True, text=True, timeout=10)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn("--scenario-input", result.stdout)

    def test_long_wait_rechecks_action_and_stops_on_revocation(self):
        status = deepcopy(self.fixture.status)
        status["actions"].remove("fault")
        timer = threading.Timer(0.8, lambda: self.fixture.write("authority-status.json", status, active=False))
        environment = dict(os.environ, **self.fixture.env, HEXALITH_C1_PREDECESSOR_PATH=str(self.fixture.root / "predecessor.json"), HEXALITH_C1_REPOSITORY_ROOT=str(ROOT), HEXALITH_C1_CHECK_EXECUTION="1", HEXALITH_C1_REQUIRED_ACTION="fault")
        timer.start()
        try:
            with patch.dict(os.environ, self.fixture.env), self.assertRaises(lifecycle.EvidenceValidationError):
                lifecycle._run_bounded_process((sys.executable, "-c", "import time; time.sleep(15)"), cwd=ROOT, timeout_seconds=12, environment=environment)
        finally:
            timer.join()

    def test_child_wait_cannot_reset_the_original_c3_deadline(self):
        environment = dict(os.environ, **self.fixture.env, HEXALITH_C1_PREDECESSOR_PATH=str(self.fixture.root / "predecessor.json"), HEXALITH_C1_REPOSITORY_ROOT=str(ROOT), HEXALITH_C1_CHECK_EXECUTION="1", HEXALITH_C3_EXECUTION_DEADLINE_MS=str(self.fixture.ms + 1500))
        started = time.monotonic()
        with patch.dict(os.environ, self.fixture.env), self.assertRaises(lifecycle.EvidenceValidationError):
            lifecycle._run_bounded_process((sys.executable, "-c", "import time; time.sleep(15)"), cwd=ROOT, timeout_seconds=20, environment=environment)
        self.assertLess(time.monotonic() - started, 4)

    def test_legacy_v2_still_refuses(self):
        proposed = dict(self.fixture.predecessor, schemaVersion=authority.PREFIX + "predecessor/v2")
        with self.assertRaises(lifecycle.EvidenceValidationError):
            lifecycle._validate_predecessor(proposed, ROOT, self.fixture.root)

class C3ContinuationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.boot_ns = 1000000000000
        self.started = datetime.now(timezone.utc)
        self.now = self.started
        self.stack = ExitStack()
        self.addCleanup(self.stack.close)
        self.stack.enter_context(patch.object(authority, "boottime_ns", lambda: self.boot_ns))
        self.stack.enter_context(patch.object(producer, "_utc_now_milliseconds", lambda: int(self.now.timestamp() * 1000)))
        self.fixture = OwnerFixture(self.root, now=self.now)
        self.env_patch = self.stack.enter_context(patch.dict(os.environ, self.fixture.env))
        os.environ["HEXALITH_C1_PREDECESSOR_PATH"] = str(self.root / "predecessor.json")
        os.environ["HEXALITH_C1_REPOSITORY_ROOT"] = str(ROOT)
        self.context = dict(session="fixture-session", source=self.fixture.commit, target=self.fixture.scope["targetSha256"])
        self.path = self.root / "c3-retention-reclamation.journal.jsonl"
        self.fixture.plan["c3Execution"] = dict(startedUtcMs=self.fixture.ms, startedBoottimeNs=self.boot_ns, bootId=authority.boot_identity(), journalPath=str(self.path))
        self.fixture.approve()
        os.environ.update(self.fixture.env)
        self.target = {**self.fixture.plan["executionTarget"], "_authority_required": True}

    def open(self):
        journal = producer._C3Journal(self.path, self.context)
        journal._target_for_authority = self.target
        try:
            journal.enable_execution(self.fixture.verify())
        except BaseException:
            journal.close()
            raise
        return journal

    def renew(self, seconds):
        self.boot_ns += seconds * 1000000000
        self.now += timedelta(seconds=seconds)
        continuation = dict(journalSha256=digest(self.path.read_bytes()), stateSha256=digest(self.path.with_name(self.path.name + ".state.json").read_bytes()))
        origin = self.fixture.plan["c3Execution"]
        self.fixture = OwnerFixture(self.root, now=self.now)
        self.fixture.plan["c3Execution"] = origin
        self.fixture.plan["continuation"] = continuation
        self.fixture.approve()
        os.environ.update(self.fixture.env)

    def result(self, name):
        now = int(self.now.timestamp() * 1000)
        args = dict(operation=name)
        return dict(result_count=1, _command=dict(command_id=name, arguments=args, arguments_sha256=digest(j1_bytes(args)), started_utc_ms=now, finished_utc_ms=now, exit_code=0, stdout_sha256=SHA, stderr_sha256=SHA, result_count=1))

    def test_three_day_resume_retains_origin_budget_and_exact_completed_prefix(self):
        with self.open() as journal:
            journal.begin("cohort-1h-expiry")
            journal.append("cohort-1h-expiry", self.result("cohort-1h-expiry"))
            origin = journal._execution["started_utc_ms"]
            journal.begin("cohort-168h-wait")
        self.renew(3 * 86400)
        with self.open() as journal:
            self.assertEqual(origin, journal._execution["started_utc_ms"])
            self.assertEqual(6 * 86400, journal.remaining_seconds)
            self.assertEqual(["cohort-1h-expiry"], list(journal.resume_results))
            journal.begin("cohort-168h-wait")
            journal.append("cohort-168h-wait", self.result("cohort-168h-wait"))
            self.assertEqual(2, len(journal.resume_results))

    def test_uncertain_mutation_cannot_repeat_even_with_approved_new_plan(self):
        with self.open() as journal:
            journal.begin("cohort-1h-seed")
        self.renew(3600)
        with self.assertRaisesRegex(lifecycle.EvidenceValidationError, "uncertain mutation"):
            self.open()

    def test_original_nine_day_budget_exhaustion_and_reboot_refuse(self):
        with self.open():
            pass
        self.renew(9 * 86400)
        with self.assertRaisesRegex(lifecycle.EvidenceValidationError, "exhausted"):
            self.open()
        self.boot_ns -= 9 * 86400 * 1000000000
        self.now = self.started
        with patch.object(authority, "boot_identity", return_value="foreign-boot"):
            with self.assertRaises(lifecycle.EvidenceValidationError):
                self.open()

    def test_truncation_tampering_or_missing_explicit_continuation_refuse(self):
        with self.open() as journal:
            journal.begin("cohort-1h-expiry")
            journal.append("cohort-1h-expiry", self.result("cohort-1h-expiry"))
        with self.assertRaisesRegex(lifecycle.EvidenceValidationError, "explicit approved"):
            self.open()
        self.renew(1)
        original = self.path.read_bytes()
        self.path.write_bytes(b"")
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.open()
        self.path.write_bytes(original)
        state_path = self.path.with_name(self.path.name + ".state.json")
        state_path.write_bytes(state_path.read_bytes() + b" ")
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.open()

    def test_wrong_context_concurrent_run_and_clock_regression_refuse(self):
        with self.open() as journal:
            with self.assertRaises(lifecycle.EvidenceValidationError):
                producer._C3Journal(self.path, self.context)
            with self.assertRaisesRegex(lifecycle.EvidenceValidationError, "regression"):
                journal.check_execution(journal._execution["started_utc_ms"] - 1)
        self.renew(1)
        self.context["session"] = "foreign-session"
        with self.assertRaises(lifecycle.EvidenceValidationError):
            self.open()

    def test_withdrawal_before_operation_and_wait_refuses_without_target_work(self):
        with self.open() as journal:
            withdrawal = json.loads((self.root / "withdrawal.json").read_text())
            withdrawal["withdrawn"] = True
            self.fixture.write("withdrawal.json", withdrawal, active=False)
            with patch.object(producer, "_run_locked_operation", side_effect=AssertionError("target accessed")):
                with self.assertRaises(lifecycle.EvidenceValidationError):
                    producer._run_operation(self.target, "c3-retention-reclamation", "cohort-1h-purge")
            environment = dict(os.environ, HEXALITH_C1_CHECK_EXECUTION="1")
            with patch.object(lifecycle.subprocess, "Popen", side_effect=AssertionError("child launched")):
                with self.assertRaises(lifecycle.EvidenceValidationError):
                    lifecycle._run_bounded_process(("unreachable",), cwd=ROOT, timeout_seconds=10, environment=environment)


if __name__ == "__main__":
    unittest.main()
