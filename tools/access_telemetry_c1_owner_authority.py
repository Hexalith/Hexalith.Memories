"""Current sole-owner C1 authorization at the lifecycle execution boundary.

The approval root is an explicit outside-plan digest supplied by the operator.
No predecessor, parsed inspection, hash label or retained preflight supplies it.
Private custody replaces institutional receipts only for this version. Legacy
wire versions and their independence rules are unchanged.
"""

from __future__ import annotations

from contextlib import ExitStack
from dataclasses import dataclass
from datetime import datetime, timezone
import hashlib
import hmac
import os
from pathlib import Path
import time
from typing import Mapping

from access_telemetry_c1_interchange import (
    JsonSnapshot, SnapshotReader, _object, _array, _digest, _instant,
    _artifact_ref, authenticate_snapshot, j1_bytes, _mutable_json,
)
from access_telemetry_c1_owner_custody import _compose_owner_run_files, _ArtifactIdentities
from access_telemetry_c1_owner_dispatch import _open_held_file, _canonical_path
from access_telemetry_c1_owner_run import _PROFILE, _WORKLOAD
from access_telemetry_c1_approval_policy import OWNER_GITHUB_PRINCIPAL

PREFIX = "hexalith.access-telemetry.c1."
SCHEMA = PREFIX + "owner-predecessor/v1"
GATES = tuple("C1." + str(i) for i in range(1, 26))
PARENTS = {
    1: (12, 16), 2: (1, 16), 3: (1, 16), 4: (1, 3), 5: (1,),
    6: (1, 4, 15), 7: (6, 20), 8: (1,), 9: (7, 8, 18),
    10: (5, 7, 18), 11: (16,), 12: (16,), 13: (9, 14, 18),
    14: (5, 10, 16), 15: (), 16: (15,), 17: (15, 16), 18: (17,),
    19: (9, 16, 20), 20: (17,), 21: (17, 20), 22: (20, 21),
    23: (*range(1, 23), 24), 24: (17, 18, 19, 21, 22), 25: tuple(range(1, 25)),
}
# Exact gate-specific observations. Each is a technical result tied to source,
# command receipts and an accepted owner decision, never a shared summary flag.
CLAIMS = {
    1: "create read update delete roundTrip", 2: "postWriteStrongRead",
    3: "matchedEtag staleEtagDenied firstWriteConflictDenied",
    4: "laterOperationFailed noPartialTransaction",
    5: "componentTtl effectiveExpiry", 6: "deactivated reactivated statePreserved",
    7: "placementDisrupted schedulerDisrupted remindersRecovered",
    8: "recordLimitEnforced requestLimitEnforced",
    9: "twoWriters acceptedRate latencyBound zeroAcknowledgedLoss",
    10: "backlog150000 drainedWithinBound zeroAcknowledgedLoss",
    11: "crossTenantDenied crossCaseDenied deniedBeforeDependency",
    12: "tlsVerifyFull atRestPosture", 13: "amplificationMeasured headroomMeasured allCohortsMeasured",
    14: "cohortAttributableReuse reclamationDeadlineMet",
    15: "runtimeIdentity controlPlaneIdentity exactProfile independentUtc",
    16: "componentIdentity backendIdentity actualConnectionLinkage",
    17: "deployedImage manifest epoch sourceBound",
    18: "nodeReserved storageReserved headroomMeasured costAccepted",
    19: "declaredPostgresFault recovered zeroAcknowledgedLoss",
    20: "exactOutOfProfileExclusions", 21: "backupDestination restoreSucceeded",
    22: "rpoMeasured rtoMeasured nonHaBoundary",
    23: "capacityCostAccepted recoveryRollbackAccepted reclamationAccepted",
    24: "nonHaAcknowledged failureDomainsAccepted", 25: "securityRisksAccepted",
}
_SCOPE = frozenset("profileId profileSha256 workloadSha256 sessionId sourceCommit targetSha256".split())
_OWNER = frozenset("ownerPrincipal assuranceMode".split())
_BASE = _SCOPE | _OWNER
_TICKS_MS = 10_000
_ENV = ("HEXALITH_C1_OWNER_PLAN", "HEXALITH_C1_APPROVED_PLAN_SHA256", "HEXALITH_C1_CUSTODY_ROOT")


class OwnerAuthorityError(ValueError):
    """Content-free refusal; no partial authority is returned."""


def refuse(code):
    raise OwnerAuthorityError("owner-authority-" + code) from None


def boot_identity():
    """Actual Linux boot identity; there is no wall-clock fallback."""
    return Path("/proc/sys/kernel/random/boot_id").read_text(encoding="ascii").strip()


def boottime_ns():
    return time.clock_gettime_ns(time.CLOCK_BOOTTIME)


def _integer(value, minimum=0):
    if type(value) is not int or value < minimum or value > 2**63 - 1:
        refuse("integer-invalid")
    return value


@dataclass(frozen=True)
class OwnerAuthority:
    """One fresh decision; consumers must call the verifier again before use."""

    session_id: str
    source_commit: str
    target_sha256: str
    predecessor_sha256: str
    plan_sha256: str
    now_ms: int
    continuation: object
    lineage: tuple[str, ...]
    terminal_bundle_sha256: object
    target_identity: object
    c3_execution: object


def canonical_predecessor_sha256(predecessor):
    # Summary binding is explicitly J1; retained references always bind raw bytes.
    return hashlib.sha256(j1_bytes(predecessor)).hexdigest()


def verify_owner_predecessor(predecessor, repository_root, evidence_root, *, action="observe"):
    """Re-read approved private inputs, every Ref and status with status last.

    The independent UTC provider must write an HMAC authenticated sample under
    the outside-approved key/source binding. Samples are at most 60 seconds old,
    advanced only on the same boot. Local OS custody does not authenticate UTC.
    """
    from verify_access_telemetry_lifecycle import (
        _hash_git_blob, _validate_command_ledger, _validate_secret_safe,
        _canonical_json, _normalize_repo_path, validate_current_profile_inputs,
    )
    if repository_root is None or evidence_root is None:
        refuse("repository-and-private-evidence-required")
    if any(not os.environ.get(name) for name in _ENV):
        refuse("outside-approved-plan-required")
    root = _canonical_path(os.environ[_ENV[2]])
    if str(Path(evidence_root)) != root:
        refuse("evidence-custody-mismatch")
    reader = SnapshotReader()
    held = []
    identities = _ArtifactIdentities()
    chain_start = boottime_ns()
    initial_boot = boot_identity()
    predecessor = JsonSnapshot(j1_bytes(predecessor)).value
    now_ms = None
    summary_sha = canonical_predecessor_sha256(predecessor)

    with ExitStack() as descriptors:
        retained_bytes = 0

        def read_path(path, *, json=True):
            nonlocal retained_bytes
            path = _canonical_path(path)
            if not path.startswith(root + "/"):
                refuse("reference-outside-custody")
            file = _open_held_file(path, descriptors, custody_root=root, effective_uid=os.geteuid())
            identities.claim(path, file)
            raw = file.read()
            retained_bytes += len(raw)
            if retained_bytes > 33554432:
                refuse("aggregate-byte-budget-exceeded")
            held.append((file, raw))
            return reader.read(raw) if json else raw

        decision_expiries = []
        capture_finishes = []
        decision_issues = []
        references = set()
        reference_paths = set()
        snapshots = {}

        def reference(value):
            ref = _artifact_ref(value)
            previous = snapshots.get(ref.path)
            if previous is not None:
                authenticate_snapshot(ref, previous)
                return previous
            snapshot = read_path(root + "/" + ref.path)
            authenticate_snapshot(ref, snapshot)
            snapshots[ref.path] = snapshot
            references.add(snapshot.sha256)
            reference_paths.add(ref.path)
            if len(snapshots) > 512:
                refuse("reference-count-exceeded")
            return snapshot

        def stream_reference(value):
            _object(value, "path sha256 byteLength")
            length = _integer(value["byteLength"])
            if length > 1048576:
                refuse("stream-byte-budget-exceeded")
            ref = _artifact_ref({**value, "byteLength": max(1, length)})
            raw = read_path(root + "/" + ref.path, json=False)
            if len(raw) != length or hashlib.sha256(raw).hexdigest() != ref.sha256:
                refuse("command-stream-mismatch")
            _validate_secret_safe({"stream": raw.decode("utf-8", errors="strict")}, "C1 command stream", maximum_string_length=1048576)
            references.add(ref.sha256)
            reference_paths.add(ref.path)
            if len(reference_paths) > 512:
                refuse("reference-count-exceeded")
            return raw

        # The clock paths/key/source are approved independently of the candidate.
        # Read the outside-approved plan without allowing it to select its digest.
        from access_telemetry_c1_owner_dispatch import _validated_plan
        plan_path = _canonical_path(os.environ[_ENV[0]])
        plan = _validated_plan(read_path(plan_path).raw, os.environ[_ENV[1]])
        value = plan.value
        _object(value["target"], "context namespace selector appId actorType apiServer apiCaSha256 clusterUid namespaceUid initialRuntimeIdentitySha256")
        from urllib.parse import urlsplit
        api = urlsplit(value["target"]["apiServer"])
        if api.scheme != "https" or not api.hostname or api.username is not None or api.password is not None or api.query or api.fragment:
            refuse("approved-api-tls-required")
        for name in ("apiCaSha256", "initialRuntimeIdentitySha256"):
            _digest(value["target"][name])
        if hashlib.sha256(j1_bytes(value["target"])).hexdigest() != value["targetSha256"]:
            refuse("approved-target-digest-mismatch")
        execution_target = value["executionTarget"]
        if (execution_target["kube_context"] != value["target"]["context"]
                or execution_target["namespace"] != value["target"]["namespace"]
                or execution_target["profile_sha256"] != _PROFILE
                or execution_target["kind"] != "non-production-qualification"):
            refuse("approved-execution-target-mismatch")
        if value.get("predecessorSha256") != summary_sha:
            refuse("predecessor-not-approved")
        sample = read_path(value["clockSamplePath"]).value
        key = read_path(value["clockKeyPath"], json=False)
        if hashlib.sha256(key).hexdigest() != value["clockKeySha256"]:
            refuse("clock-key-not-approved")
        if not 32 <= len(key) <= 4096:
            refuse("clock-key-invalid")
        _object(sample, "utcMs boottimeNs bootId sourceId signature")
        body = {k: v for k, v in sample.items() if k != "signature"}
        if not hmac.compare_digest(hmac.new(key, j1_bytes(body), hashlib.sha256).hexdigest(), sample["signature"]):
            refuse("clock-signature-invalid")
        sampled_boot = _integer(sample["boottimeNs"])
        age = boottime_ns() - sampled_boot
        if (sample["bootId"] != initial_boot or sample["sourceId"] != value["clockSourceId"]
                or not 0 <= age <= 60_000_000_000):
            refuse("clock-sample-stale-or-foreign")
        now_ms = _integer(sample["utcMs"], 1) + age // 1_000_000
        trusted_now = datetime.fromtimestamp(now_ms / 1000, timezone.utc)

        def scope(item, run):
            for name in _BASE:
                if item[name] != run[name]:
                    refuse("scope-mismatch")

        def decision(item, run, *, role, reviewed_ms):
            scope(item, run)
            if (item["decision"] != "accepted" or item["reviewerRole"] != role
                    or not item["reasons"] or any(type(r) is not str or not r.strip() for r in item["reasons"])):
                refuse("decision-not-accepted")
            issued = _integer(item["decidedUtcMs"], 1)
            expiry = _integer(item["expiresUtcMs"], 1)
            if not reviewed_ms < issued <= now_ms + 1000 or not issued < expiry <= issued + 604800000 or now_ms >= expiry:
                refuse("decision-window-invalid")
            if issued - reviewed_ms > 86400000:
                refuse("review-lag-exceeded")
            decision_expiries.append(expiry)
            decision_issues.append(issued)
            return issued

        def inspect(plan_snapshot, run_snapshot, withdrawn):
            run = run_snapshot.value
            for name, expected in (("profileSha256", _PROFILE), ("workloadSha256", _WORKLOAD)):
                if run[name] != expected:
                    refuse("current-profile-required")
            summary = _object(predecessor, "schemaVersion manifest approvals status productionLifecycleWrites qualificationAuthorized")
            if (summary["schemaVersion"] != SCHEMA or summary["status"] != "passed"
                    or summary["productionLifecycleWrites"] != "disabled" or summary["qualificationAuthorized"] is not True):
                refuse("summary-invalid")
            manifest_snapshot = reference(summary["manifest"])
            manifest = _object(manifest_snapshot.value, _BASE | {"schemaVersion", "createdUtcMs", "gates"})
            if manifest["schemaVersion"] != PREFIX + "owner-manifest/v1":
                refuse("manifest-version-invalid")
            scope(manifest, run)
            gates = _array(manifest["gates"])
            if len(gates) != 25:
                refuse("twenty-five-gates-required")
            contracts = value.get("gateContracts")
            if not isinstance(contracts, Mapping) or set(contracts) != set(GATES):
                refuse("approved-gate-contracts-required")
            packets = {}
            gate_hashes = set()
            for number, entry in enumerate(gates, 1):
                _object(entry, "gate artifact")
                if entry["gate"] != GATES[number - 1]:
                    refuse("gate-inventory-invalid")
                snapshot = reference(entry["artifact"])
                if snapshot.sha256 in gate_hashes:
                    refuse("distinct-gate-packets-required")
                gate_hashes.add(snapshot.sha256)
                packet = _object(snapshot.value, _BASE | {
                    "schemaVersion", "gate", "startedUtcMs", "finishedUtcMs", "decidedUtcMs", "expiresUtcMs",
                    "decision", "reviewerRole", "reasons", "parents", "sourcePath", "sourceSha256",
                    "commands", "commandStreams", "technicalCommandId", "technical", "resultCount", "failureCount", "skipCount", "observations", "cleanup",
                })
                if packet["schemaVersion"] != PREFIX + "owner-gate/v1" or packet["gate"] != entry["gate"]:
                    refuse("gate-version-or-identity-invalid")
                scope(packet, run)
                start = _integer(packet["startedUtcMs"], 1)
                finish = _integer(packet["finishedUtcMs"], 1)
                collection_start = _instant(run["notBeforeUtc"], spelling="new") // _TICKS_MS - 62135683200000
                collection_end = _instant(run["collectionEndsAtUtc"], spelling="new") // _TICKS_MS - 62135683200000
                if (not collection_start <= start <= finish < collection_end or finish - start > 86400000
                        or not -1000 <= now_ms - finish <= 345600000):
                    refuse("gate-capture-window-invalid")
                capture_finishes.append(finish)
                if (_integer(packet["resultCount"], 1) < 1 or _integer(packet["failureCount"]) != 0 or _integer(packet["skipCount"]) != 0):
                    refuse("gate-result-invalid")
                observations = _object(packet["observations"], frozenset(CLAIMS[number].split()))
                if any(item is not True for item in observations.values()):
                    refuse("gate-observation-failed")
                contract = contracts[entry["gate"]]
                if (contract["sourcePath"] != packet["sourcePath"] or contract["sourceSha256"] != packet["sourceSha256"]):
                    refuse("source-contract-not-approved")
                source = _normalize_repo_path(Path(repository_root), packet["sourcePath"], "C1 source")
                _digest(packet["sourceSha256"])
                if _hash_git_blob(Path(repository_root), run["sourceCommit"], source) != packet["sourceSha256"]:
                    refuse("source-bytes-mismatch")
                commands = [_mutable_json(c) for c in _array(packet["commands"], nonempty=True)]
                _validate_command_ledger({"commands": commands}, start, finish)
                if sum(c["result_count"] for c in commands) != packet["resultCount"]:
                    refuse("gate-command-result-count-mismatch")
                streams = packet["commandStreams"]
                if set(streams) != {c["command_id"] for c in commands}:
                    refuse("command-stream-inventory-invalid")
                technical_outputs = {}
                for command in commands:
                    pair = _object(streams[command["command_id"]], "stdout stderr")
                    for name in ("stdout", "stderr"):
                        raw = stream_reference(pair[name])
                        if name == "stdout":
                            technical_outputs[command["command_id"]] = raw
                        if hashlib.sha256(raw).hexdigest() != command[name + "_sha256"]:
                            refuse("command-stream-receipt-mismatch")
                technical_id = packet["technicalCommandId"]
                command = next((c for c in commands if c["command_id"] == technical_id), None)
                if (command is None or technical_id != contract["commandId"]
                        or command["arguments_sha256"] != contract["argumentsSha256"]):
                    refuse("technical-command-not-approved")
                output = JsonSnapshot(technical_outputs[technical_id]).value
                _object(output, _BASE | {"gate", "observations", "technical"})
                scope(output, run)
                if output["gate"] != entry["gate"] or output["observations"] != packet["observations"] or output["technical"] != packet["technical"]:
                    refuse("technical-output-mismatch")
                from access_telemetry_c1_gate_semantics import validate_gate_technical
                if number == 15:
                    for name in ("apiServer", "apiCaSha256", "clusterUid", "namespaceUid", "initialRuntimeIdentitySha256"):
                        if packet["technical"][name] != value["target"][name]:
                            refuse("captured-target-identity-mismatch")
                validate_gate_technical(number, packet["technical"], approved=contract, run=run, capture_interval=(start, finish))
                cleanup = _object(packet["cleanup"], "required receipts finalState")
                if (type(cleanup["required"]) is not bool or type(contract["cleanupRequired"]) is not bool
                        or cleanup["required"] != contract["cleanupRequired"]):
                    refuse("cleanup-kind-invalid")
                receipts = _array(cleanup["receipts"])
                if cleanup["required"]:
                    if not receipts or cleanup["finalState"] != "restored-approved-baseline":
                        refuse("cleanup-required")
                    for ref in receipts:
                        receipt = reference(ref).value
                        _object(receipt, _BASE | {"gate", "state", "finishedUtcMs", "gateBefore", "gateAfter", "leaseEmpty", "lifecycleReplicas", "clockReplicas", "ownedSyntheticRemoved"})
                        scope(receipt, run)
                        if receipt["gate"] != entry["gate"] or receipt["state"] != "restored-approved-baseline" or not start <= receipt["finishedUtcMs"] <= finish:
                            refuse("cleanup-mismatch")
                        if (receipt["gateBefore"] != "disabled" or receipt["gateAfter"] != "disabled"
                                or receipt["leaseEmpty"] is not True or receipt["ownedSyntheticRemoved"] is not True
                                or _integer(receipt["lifecycleReplicas"]) != 0 or _integer(receipt["clockReplicas"]) != 0):
                            refuse("cleanup-terminal-state-invalid")
                elif receipts or cleanup["finalState"] != "read-only-no-owned-mutation":
                    refuse("read-only-cleanup-invalid")
                role = "security" if number in (11, 12, 15, 16, 25) else "platform-operations"
                decision(packet, run, role=role, reviewed_ms=finish)
                packets[entry["gate"]] = packet
            for number, gate in enumerate(GATES, 1):
                parents = packets[gate]["parents"]
                expected = {GATES[i - 1]: next(e["artifact"]["sha256"] for e in gates if e["gate"] == GATES[i - 1]) for i in PARENTS[number]}
                if dict(parents) != expected:
                    refuse("gate-parents-mismatch")
                technical = packets[gate]["technical"]
                if number in (20, 24) and dict(technical["evidenceHashes"]) != expected:
                    refuse("failure-domain-evidence-not-bound")
                if number in (23, 25) and dict(technical["acceptedEvidenceHashes"]) != expected:
                    refuse("acceptance-evidence-not-bound")
                if number == 22:
                    hashes = {e["gate"]: e["artifact"]["sha256"] for e in gates}
                    if technical["restoreEvidenceSha256"] != hashes["C1.21"] or technical["faultEvidenceSha256"] != hashes["C1.19"]:
                        refuse("recovery-evidence-not-bound")
                if any(packets[p]["decidedUtcMs"] >= packets[gate]["startedUtcMs"] for p in parents):
                    refuse("parent-not-accepted-before-capture")
            manifest_time = _integer(manifest["createdUtcMs"], 1)
            if manifest_time < max(p["decidedUtcMs"] for p in packets.values()) or manifest_time > now_ms + 1000:
                refuse("manifest-chronology-invalid")
            roles = set()
            approvals = _array(summary["approvals"])
            if len(approvals) != 2:
                refuse("two-role-decisions-required")
            approval_hashes = set()
            for ref in approvals:
                snapshot = reference(ref)
                approval_hashes.add(snapshot.sha256)
                approval = _object(snapshot.value, _BASE | {"schemaVersion", "manifestSha256", "reviewerRole", "decision", "decidedUtcMs", "expiresUtcMs", "reasons"})
                if approval["schemaVersion"] != PREFIX + "owner-bundle-approval/v1" or approval["manifestSha256"] != manifest_snapshot.sha256:
                    refuse("approval-manifest-mismatch")
                role = approval["reviewerRole"]
                if role not in ("platform-operations", "security") or role in roles:
                    refuse("distinct-role-decisions-required")
                decision(approval, run, role=role, reviewed_ms=manifest_time)
                roles.add(role)
            if len(approval_hashes) != 2:
                refuse("distinct-role-records-required")
            return run

        run = _compose_owner_run_files(plan_path, os.environ[_ENV[1]], custody_root=root,
            now=trusted_now, action=action, inspection=inspect)
        # Completed predecessors are archival references only. Their manifest
        # scope must match; they never authorize a fresh operation themselves.
        lineage = []
        for old_reference in _array(value.get("completedPredecessors", ())):
            old = reference(old_reference)
            old_summary = _object(old.value, "schemaVersion manifest approvals status productionLifecycleWrites qualificationAuthorized")
            if old_summary["schemaVersion"] != SCHEMA:
                refuse("lineage-version-invalid")
            old_manifest = reference(old_summary["manifest"]).value
            scope(old_manifest, run)
            lineage.append(old.sha256)
        if len(lineage) != len(set(lineage)):
            refuse("lineage-not-distinct")
        # Re-read every retained Ref and control file before fresh status is read.
        for file, raw in held:
            os.lseek(file.descriptor, 0, os.SEEK_SET)
            if file.read() != raw:
                refuse("retained-bytes-drifted")
        validate_current_profile_inputs(Path(repository_root))
        status_snapshot = read_path(value["authorityStatusPath"])
        status = _object(status_snapshot.value, "planSha256 recordSha256 sessionId updatedUtcMs issuedUtcMs expiresUtcMs actions activeSha256")
        current_boot = boottime_ns()
        now_ms = sample["utcMs"] + (current_boot - sampled_boot) // 1_000_000
        if (initial_boot != boot_identity() or current_boot < chain_start or current_boot - chain_start > 300_000_000_000
                or status["planSha256"] != plan.sha256 or status["recordSha256"] != value["recordSha256"]
                or status["sessionId"] != run["sessionId"]):
            refuse("current-status-binding-invalid")
        if not 0 <= now_ms - _integer(status["updatedUtcMs"], 1) <= 300000:
            refuse("current-status-stale")
        approved_window = _object(value["authorization"], "issuedUtcMs expiresUtcMs actions")
        grant_start = _integer(approved_window["issuedUtcMs"], 1)
        grant_expiry = _integer(approved_window["expiresUtcMs"], 1)
        grant_actions = _array(approved_window["actions"], nonempty=True)
        if (not grant_start < grant_expiry <= grant_start + 604800000
                or not grant_start <= now_ms < grant_expiry
                or not set(_array(status["actions"])) <= set(grant_actions)
                or not set(grant_actions) <= set(run["actions"])):
            refuse("authorization-window-not-outside-approved")
        issued, expiry = _integer(status["issuedUtcMs"], 1), _integer(status["expiresUtcMs"], 1)
        if (not grant_start <= issued < expiry <= grant_expiry
                or not 0 <= now_ms - issued <= 900000
                or expiry > issued + 900000 or now_ms >= expiry):
            refuse("final-authorization-expired")
        active = _array(status["activeSha256"])
        if len(active) > 512 or len(active) != len(set(active)):
            refuse("current-reference-status-invalid")
        for digest in active:
            _digest(digest)
        if action not in _array(status["actions"]) or not references <= set(active):
            refuse("action-or-reference-revoked-or-unknown")
        # The base withdrawal file is last again; this also repeats run windows.
        _compose_owner_run_files(plan_path, os.environ[_ENV[1]], custody_root=root,
            now=datetime.fromtimestamp(now_ms / 1000, timezone.utc), action=action)
        file, raw = held[-1]
        os.lseek(file.descriptor, 0, os.SEEK_SET)
        if file.read() != raw:
            refuse("status-drifted")
        final_boot = boottime_ns()
        final_now = sample["utcMs"] + (final_boot - sampled_boot) // 1000000
        if (final_boot < current_boot or final_boot - chain_start > 300000000000
                or boot_identity() != initial_boot
                or any(final_now >= expiry for expiry in decision_expiries)
                or any(not -1000 <= final_now - finish <= 345600000 for finish in capture_finishes)
                or any(issued > final_now + 1000 for issued in decision_issues)
                or not 0 <= final_now - status["updatedUtcMs"] <= 300000
                or not 0 <= final_now - issued <= 900000 or final_now >= min(expiry, grant_expiry)):
            refuse("authority-expired-during-verification")
        first, collection, review, run_expiry = (
            _instant(run[name], spelling="new") // _TICKS_MS - 62135683200000
            for name in ("notBeforeUtc", "collectionEndsAtUtc", "reviewEndsAtUtc", "expiresAtUtc")
        )
        if not first <= final_now < min(review, run_expiry) or (action in {"observe", "fault", "purge", "reclaim"} and final_now >= collection):
            refuse("run-expired-during-verification")
        now_ms = final_now
        return OwnerAuthority(run["sessionId"], run["sourceCommit"], run["targetSha256"],
                              summary_sha, plan.sha256, now_ms, value.get("continuation"), tuple(lineage), value.get("terminalBundleSha256"), value["target"], value.get("c3Execution"))


def qualification_lease_holder(authority, execution_id):
    """Stable exact holder for this approved owner/session/source/physical target."""
    if type(execution_id) is not str or len(execution_id) != 32 or any(c not in "0123456789abcdef" for c in execution_id):
        refuse("original-execution-identity-required")
    binding = "/".join((authority.session_id, authority.source_commit, authority.target_sha256, execution_id))
    return "story-27-4/owner-6775094/" + hashlib.sha256(binding.encode()).hexdigest()[:32]


def c3_remaining_seconds(authority):
    """Authenticate the approved original UTC/elapsed origin without resetting it."""
    origin = _object(authority.c3_execution, "startedUtcMs startedBoottimeNs bootId journalPath")
    start = _integer(origin["startedUtcMs"], 1)
    elapsed_start = _integer(origin["startedBoottimeNs"])
    elapsed_now = boottime_ns()
    if (origin["bootId"] != boot_identity() or elapsed_now < elapsed_start
            or authority.now_ms < start
            or abs((authority.now_ms - start) - (elapsed_now - elapsed_start) // 1000000) > 1000):
        refuse("c3-origin-or-boot-invalid")
    remaining = min((start + 777600000 - authority.now_ms) / 1000,
                    (elapsed_start + 777600000000000 - elapsed_now) / 1000000000)
    if remaining <= 0:
        refuse("c3-original-budget-exhausted")
    return remaining


def verify_owner_cleanup(repository_root, evidence_root):
    """Authenticate original cleanup scope without granting execution authority.

    Cleanup intentionally survives grant expiry and withdrawal. It accepts only
    an outside-approved private plan/record and returns their exact physical
    target/session binding; callers must verify that target and the exact owned
    Lease before fixed disable/final-state operations. No gate, fault, renewal,
    collection, close-out or publication permission is returned.
    """
    from access_telemetry_c1_owner_dispatch import _validated_plan
    from access_telemetry_c1_owner_run import OwnerRunAnchors, inspect_owner_run
    from verify_access_telemetry_lifecycle import validate_current_profile_inputs
    if any(not os.environ.get(name) for name in _ENV):
        refuse("outside-approved-cleanup-plan-required")
    root = _canonical_path(os.environ[_ENV[2]])
    if str(Path(evidence_root)) != root:
        refuse("cleanup-custody-mismatch")
    identities = _ArtifactIdentities()
    with ExitStack() as stack:
        held = []
        def read(path):
            path = _canonical_path(path)
            if not path.startswith(root + "/"):
                refuse("cleanup-reference-outside-custody")
            file = _open_held_file(path, stack, custody_root=root, effective_uid=os.geteuid())
            identities.claim(path, file)
            raw = file.read(); held.append((file, raw))
            return JsonSnapshot(raw)
        plan = _validated_plan(read(os.environ[_ENV[0]]).raw, os.environ[_ENV[1]])
        value = plan.value
        if value["custodyRoot"] != root:
            refuse("cleanup-custody-mismatch")
        record = read(value["recordPath"])
        anchors = OwnerRunAnchors(value["recordSha256"], value["policySha256"], value["sessionId"], value["sourceCommit"], value["targetSha256"], root)
        # Historical inspection checks the immutable adopted owner/version/scope;
        # this instant is never presented as current qualification permission.
        start_ms = _instant(record.value["notBeforeUtc"], spelling="new") // _TICKS_MS - 62135683200000
        inspect_owner_run(record, anchors, now=datetime.fromtimestamp(start_ms / 1000, timezone.utc), withdrawn=False, action="observe")
        if hashlib.sha256(j1_bytes(value["target"])).hexdigest() != value["targetSha256"]:
            refuse("cleanup-target-not-approved")
        validate_current_profile_inputs(Path(repository_root))
        for file, raw in held:
            os.lseek(file.descriptor, 0, os.SEEK_SET)
            if file.read() != raw:
                refuse("cleanup-plan-drifted")
        return OwnerAuthority(value["sessionId"], value["sourceCommit"], value["targetSha256"],
                              value["predecessorSha256"], plan.sha256, start_ms, None, (), None,
                              value["target"], value.get("c3Execution"))
