"""Gate-specific checks over exact retained producer output for owner C1 v1.

These checks judge observations; no function in this module grants authority.
The owner consumer separately binds source/commands, approved contracts, custody,
current status, review and dependency order.
"""

from __future__ import annotations

from access_telemetry_c1_interchange import _object, _array, _digest
from access_telemetry_campaign_profile import (
    selected_campaign_profile, active_profile_sha256, active_runtime_image, active_runtime_version,
)


def validate_gate_technical(number, value, *, approved, run, capture_interval):
    """Evaluate technical facts rather than trusting a packet's pass labels."""
    from verify_access_telemetry_lifecycle import (
        EvidenceValidationError, EXPECTED_POSTGRESQL_IMAGE, EXPECTED_RUNTIME_IMAGE,
        calculate_capacity, evaluate_capacity_admission, PROFILE_CAPACITY_BYTES,
    )

    def require(condition):
        if not condition:
            raise EvidenceValidationError("C1 technical observation failed")

    def integer(v, minimum=0):
        require(type(v) is int and minimum <= v <= 2**63 - 1)
        return v

    def same_hash(first, last):
        _digest(first); _digest(last)
        require(first == last)

    def changed(first, last):
        require(type(first) is str and type(last) is str and first and last and first != last)

    def fields(names):
        return _object(value, names)

    def count_inventory(names):
        items = _array(names, nonempty=True)
        require(len(items) == len(set(items)))
        for digest in items:
            _digest(digest)
        return len(items)

    def typed_measurements(item, name=""):
        if isinstance(item, dict) or hasattr(item, "items"):
            for key, child in item.items():
                typed_measurements(child, key)
        elif isinstance(item, (tuple, list)):
            for child in item:
                typed_measurements(child, name)
        elif type(item) is bool:
            require(name in {"matchingAccepted", "osDiskShrinkClaimed", "postgresTls", "openBaoTls", "highAvailability", "restoreTargetProduction", "admitted", "continuousSevenDayCredit", "historical400GiBCredit"})
        elif name.endswith("UtcMs"):
            require(type(item) is int and capture_interval[0] <= item <= capture_interval[1])
    typed_measurements(value)

    if number == 1:
        v = fields("component createSha256 readSha256 updateSha256 updatedReadSha256 finalReadStatus consistency")
        require(v["component"] == "state.postgresql/v2" and v["consistency"] == "strong" and v["finalReadStatus"] == "absent")
        same_hash(v["createSha256"], v["readSha256"])
        same_hash(v["updateSha256"], v["updatedReadSha256"])
        require(v["createSha256"] != v["updateSha256"])
    elif number == 2:
        v = fields("writeKeySha256 readKeySha256 acknowledgedSha256 readSha256 acknowledgedUtcMs readUtcMs consistency")
        same_hash(v["writeKeySha256"], v["readKeySha256"])
        same_hash(v["acknowledgedSha256"], v["readSha256"])
        require(v["consistency"] == "strong" and integer(v["readUtcMs"], 1) >= integer(v["acknowledgedUtcMs"], 1))
    elif number == 3:
        v = fields("matchingAccepted mismatchedStatus staleStatus priorSha256 afterConflictsSha256 firstWriteWithEtag firstWriteWithoutEtag catalogueWinnerSha256 afterRetrySha256 originalExpiryUtcMs retryExpiryUtcMs")
        require(v["matchingAccepted"] is True and v["mismatchedStatus"] == 409 and v["staleStatus"] == 409)
        # Raw FirstWrite observations are recorded; lifecycle safety comes from
        # the catalogue fence and exact unchanged winner/expiry, not insert-only assumptions.
        require(v["firstWriteWithEtag"] in ("accepted", "conflict") and v["firstWriteWithoutEtag"] in ("accepted", "conflict"))
        same_hash(v["priorSha256"], v["afterConflictsSha256"])
        same_hash(v["catalogueWinnerSha256"], v["afterRetrySha256"])
        require(integer(v["originalExpiryUtcMs"], 1) == integer(v["retryExpiryUtcMs"], 1))
    elif number == 4:
        v = fields("operationCount failedOperationIndex responseStatus recordBeforeSha256 recordAfterSha256 indexBeforeSha256 indexAfterSha256")
        require(1 <= integer(v["failedOperationIndex"], 1) < integer(v["operationCount"], 2) and v["responseStatus"] >= 400)
        same_hash(v["recordBeforeSha256"], v["recordAfterSha256"])
        same_hash(v["indexBeforeSha256"], v["indexAfterSha256"])
    elif number == 5:
        v = fields("emittedUtcMs expiryUtcMs retryExpiryUtcMs ttlSeconds preReadUtcMs preReadCount absentReadUtcMs absentReadCount cohortCount newerBeforeSha256 newerAfterSha256")
        duration = integer(v["expiryUtcMs"], 1) - integer(v["emittedUtcMs"], 1)
        require(3600000 <= duration <= 604800000 and integer(v["ttlSeconds"], 1) == (duration + 999) // 1000)
        require(v["retryExpiryUtcMs"] == v["expiryUtcMs"] and v["emittedUtcMs"] <= integer(v["preReadUtcMs"], 1) < v["expiryUtcMs"])
        require(v["expiryUtcMs"] <= integer(v["absentReadUtcMs"], 1) <= v["expiryUtcMs"] + 900000)
        require(integer(v["preReadCount"], 1) == integer(v["absentReadCount"], 1) == integer(v["cohortCount"], 1))
        same_hash(v["newerBeforeSha256"], v["newerAfterSha256"])
    elif number in (6, 7, 19):
        names = "oldIdentity newIdentity beforeSha256 afterSha256"
        names += " cursorsBeforeSha256 cursorsAfterSha256 remindersBeforeSha256 remindersAfterSha256" if number == 6 else " reminderBefore reminderAfter duplicateSideEffects" if number == 7 else " fault boundary acknowledgedInventory durableInventory"
        v = fields(names)
        changed(v["oldIdentity"], v["newIdentity"])
        same_hash(v["beforeSha256"], v["afterSha256"])
        if number == 6:
            same_hash(v["cursorsBeforeSha256"], v["cursorsAfterSha256"])
            same_hash(v["remindersBeforeSha256"], v["remindersAfterSha256"])
        elif number == 7:
            require(integer(v["reminderAfter"], 1) > integer(v["reminderBefore"]) and v["duplicateSideEffects"] == 0)
        else:
            require(v["fault"] == "postgresql-pod-process-replacement" and v["boundary"] == "healthy-node-retained-volume")
            require(count_inventory(v["acknowledgedInventory"]) == count_inventory(v["durableInventory"]) and set(v["acknowledgedInventory"]) == set(v["durableInventory"]))
    elif number == 8:
        v = fields("recordBytes transactionBytes transactionRecords storedBytes storedRecords sentSha256 storedSha256 oversizedStatus overCountStatus")
        require(v["recordBytes"] == 1024 and v["transactionBytes"] == 1048576 and v["transactionRecords"] == 256)
        require(v["storedBytes"] == 1048576 and v["storedRecords"] == 256 and v["oversizedStatus"] in (400, 413) and v["overCountStatus"] in (400, 413))
        same_hash(v["sentSha256"], v["storedSha256"])
    elif number == 9:
        v = fields("writers componentWindowMs componentOperationDelta p95Microseconds p99Microseconds purgeBefore purgeAfter")
        writers = _array(v["writers"])
        require(len(writers) == 2 and v["componentWindowMs"] == 1800000 and integer(v["componentOperationDelta"], 1) >= 900000)
        identities, starts, finishes = set(), [], []
        for writer in writers:
            _object(writer, "identity startedUtcMs finishedUtcMs emitted acknowledged persisted inventorySha256")
            identities.add(writer["identity"])
            starts.append(integer(writer["startedUtcMs"], 1)); finishes.append(integer(writer["finishedUtcMs"], 1))
            require(writer["emitted"] == writer["acknowledged"] == writer["persisted"] == 225000)
            _digest(writer["inventorySha256"])
        require(len(identities) == 2 and min(finishes) - max(starts) >= 1800000)
        # Exact approved latency thresholds are outside-plan contract operands.
        limits = _object(approved["latencyLimits"], "p95Microseconds p99Microseconds")
        require(0 < integer(v["p95Microseconds"], 1) <= integer(limits["p95Microseconds"], 1))
        require(v["p95Microseconds"] <= integer(v["p99Microseconds"], 1) <= integer(limits["p99Microseconds"], 1))
        require(integer(v["purgeAfter"], 1) > integer(v["purgeBefore"]))
    elif number == 10:
        v = fields("dueBefore dueAfter interruptionMs drainMs oldestDueAgeSeconds newerBeforeSha256 newerAfterSha256")
        require(v["dueBefore"] == 150000 and v["dueAfter"] == 0 and v["interruptionMs"] == 600000)
        require(integer(v["drainMs"], 1) <= integer(approved["maxDrainMs"], 1) and integer(v["oldestDueAgeSeconds"]) <= 900)
        same_hash(v["newerBeforeSha256"], v["newerAfterSha256"])
    elif number == 11:
        v = fields("principalA principalB affectedSurfaces denials")
        changed(v["principalA"], v["principalB"])
        require(set(v["affectedSurfaces"]) == {"write", "storage", "inspect", "purge", "erase"})
        denials = _array(v["denials"])
        require(len(denials) == 25)
        combinations = set()
        for row in denials:
            _object(row, "surface scope status dependencyCalls disclosedCount mutatedCount testName")
            require(row["surface"] in v["affectedSurfaces"] and row["scope"] in {"foreign", "unknown", "malformed", "empty", "mismatched"})
            require(row["status"] in (400, 403) and row["dependencyCalls"] == row["disclosedCount"] == row["mutatedCount"] == 0 and type(row["testName"]) is str and bool(row["testName"]))
            combinations.add((row["surface"], row["scope"]))
        require(len(combinations) == 25)
    elif number == 12:
        v = fields("postgresSslMode postgresTls openBaoTls badCaStatus badHostnameStatus crossPrefixStatus atRestDeviceSha256 atRestObservationSha256")
        require(v["postgresSslMode"] == "verify-full" and v["postgresTls"] is True and v["openBaoTls"] is True)
        require(v["badCaStatus"] == v["badHostnameStatus"] == "denied" and v["crossPrefixStatus"] == 403)
        _digest(v["atRestDeviceSha256"]); _digest(v["atRestObservationSha256"])
    elif number == 13:
        campaign = selected_campaign_profile()
        if campaign is not None:
            v = fields("semantics profileSha256 requirements cohortRecords continuousSevenDayCredit historical400GiBCredit")
            from access_telemetry_c1_interchange import _mutable_json
            require(v["semantics"] == "finite-campaign/v1" and v["profileSha256"] == run["profileSha256"] == active_profile_sha256())
            require(_mutable_json(v["requirements"]) == campaign["sizing"]
                    and dict(v["cohortRecords"]) == {"1h": 125, "24h": 125, "168h": 125}
                    and v["continuousSevenDayCredit"] is False and v["historical400GiBCredit"] is False)
            return
        # Existing capacity evaluator recomputes every operand, including exact
        # 400 GiB admission and the seven-day non-admissible horizon boundary.
        v = fields("operands expectedAdmissions")
        require(dict(v["operands"]["horizons"]) == {"1h": 900000, "24h": 21600000, "7d": 151200000})
        requirements = calculate_capacity(**dict(v["operands"]))
        admissions = evaluate_capacity_admission(requirements)
        require({r.horizon for r in requirements} == {"1h", "24h", "7d"})
        from access_telemetry_c1_interchange import _mutable_json
        require([a.__dict__ for a in admissions] == _mutable_json(v["expectedAdmissions"]))
    elif number == 14:
        v = fields("database schema table cohortSha256 deletedCount postTupleCount purgedUtcMs reclaimedUtcMs allocatorFreeBefore allocatorFreeAfter osDiskShrinkClaimed newerBeforeSha256 newerAfterSha256 collector")
        require(v["database"] == "memories_access_telemetry" and v["schema"] == "access_telemetry" and v["table"] == "lifecycle_state" and v["collector"] == "pgstattuple")
        _digest(v["cohortSha256"])
        require(integer(v["deletedCount"], 1) > 0 and v["postTupleCount"] == 0 and v["osDiskShrinkClaimed"] is False)
        require(integer(v["allocatorFreeAfter"], 1) > integer(v["allocatorFreeBefore"]))
        require(0 <= integer(v["reclaimedUtcMs"], 1) - integer(v["purgedUtcMs"], 1) <= 86400000)
        same_hash(v["newerBeforeSha256"], v["newerAfterSha256"])
    elif number in (15, 16, 17):
        names = "runtimeImage runtimeVersion appId actorType schedulerMembers placementMembers independentClockSource apiServer apiCaSha256 clusterUid namespaceUid initialRuntimeIdentitySha256" if number == 15 else "componentType componentVersion backendImage serverVersion actualConnectionSha256 capabilities" if number == 16 else "images manifestSha256 configurationEpoch profileSha256 sourceCommit coveredInputsSha256"
        v = fields(names)
        if number == 15:
            require(v["runtimeImage"] == active_runtime_image() and v["runtimeVersion"] == active_runtime_version() and v["appId"] == "memories-access-telemetry" and v["actorType"] == "AccessTelemetryLifecycleActor")
            require(len(set(v["schedulerMembers"])) == len(set(v["placementMembers"])) == 3 and type(v["independentClockSource"]) is str and bool(v["independentClockSource"]))
        elif number == 16:
            require(v["componentType"] == "state.postgresql" and v["componentVersion"] == "v2" and v["backendImage"] == EXPECTED_POSTGRESQL_IMAGE and v["serverVersion"] == "18.6")
            _digest(v["actualConnectionSha256"])
            require(set(v["capabilities"]) >= {"ETAG", "TRANSACTIONAL", "TTL"})
        else:
            require(v["profileSha256"] == run["profileSha256"] and v["sourceCommit"] == run["sourceCommit"] and v["configurationEpoch"] == approved["configurationEpoch"])
            require(dict(v["images"]) == dict(approved["images"]) and bool(v["images"]))
            import re
            require(all(type(image) is str and re.fullmatch(r"(?:.+@)?sha256:[0-9a-f]{64}", image) is not None for image in v["images"].values()))
            for name in ("manifestSha256", "coveredInputsSha256"):
                same_hash(v[name], approved[name])
    elif number == 18:
        v = fields("nodeIdentity storageIdentity reservedBytes availableBytes requestedBytes cpuCount memoryBytes costOwner quoteUtcMs operatingCostMinorUnits currency")
        campaign = selected_campaign_profile()
        if campaign is not None:
            requirements = campaign["sizing"]
            require(integer(v["reservedBytes"]) >= requirements["primaryBytes"] and integer(v["availableBytes"]) >= requirements["primaryBytes"]
                    and integer(v["requestedBytes"]) == requirements["primaryBytes"]
                    and integer(v["cpuCount"]) * 1000 >= requirements["cpuMilli"] and integer(v["memoryBytes"]) >= requirements["memoryBytes"])
        else:
            require(v["reservedBytes"] >= PROFILE_CAPACITY_BYTES and v["availableBytes"] >= PROFILE_CAPACITY_BYTES and v["cpuCount"] >= 8 and v["memoryBytes"] >= 17179869184)
        require(v["nodeIdentity"] == approved["nodeIdentity"] and v["storageIdentity"] == approved["storageIdentity"])
        require(integer(v["quoteUtcMs"], 1) > 0 and integer(v["operatingCostMinorUnits"], 1) > 0 and bool(v["costOwner"]) and len(v["currency"]) == 3)
    elif number in (20, 24):
        v = fields("excludedFailureDomains highAvailability ownerPrincipal evidenceHashes")
        require(set(v["excludedFailureDomains"]) == {"node", "volume", "control-plane", "site"} and v["highAvailability"] is False and v["ownerPrincipal"] == run["ownerPrincipal"])
        require(bool(v["evidenceHashes"]))
        for digest in v["evidenceHashes"].values():
            _digest(digest)
    elif number == 21:
        v = fields("backupNode protectedNode restoreTargetProduction backupInventory restoredInventory erasedInventory resurrectedInventory rpoMs rtoMs destinationSha256")
        changed(v["backupNode"], v["protectedNode"])
        require(v["restoreTargetProduction"] is False and count_inventory(v["backupInventory"]) == count_inventory(v["restoredInventory"]) and set(v["backupInventory"]) == set(v["restoredInventory"]))
        require(not v["resurrectedInventory"] and not set(v["erasedInventory"]) & set(v["restoredInventory"]))
        require(0 < integer(v["rpoMs"], 1) <= integer(approved["maxRpoMs"], 1) and 0 < integer(v["rtoMs"], 1) <= integer(approved["maxRtoMs"], 1))
        _digest(v["destinationSha256"])
    elif number == 22:
        v = fields("rpoMs rtoMs highAvailability restoreEvidenceSha256 faultEvidenceSha256")
        require(integer(v["rpoMs"], 1) > 0 and integer(v["rtoMs"], 1) > 0 and v["highAvailability"] is False)
        _digest(v["restoreEvidenceSha256"]); _digest(v["faultEvidenceSha256"])
    elif number in (23, 25):
        v = fields("role ownerPrincipal acceptedEvidenceHashes acceptedRisks")
        require(v["role"] == ("platform-operations" if number == 23 else "security") and v["ownerPrincipal"] == run["ownerPrincipal"] and bool(v["acceptedRisks"]))
        require(bool(v["acceptedEvidenceHashes"]))
        for digest in v["acceptedEvidenceHashes"].values():
            _digest(digest)
    else:
        require(False)
