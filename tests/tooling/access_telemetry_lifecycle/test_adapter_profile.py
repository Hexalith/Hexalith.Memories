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
        self.assertEqual("hexalith-memories", adapter_profile.EXPECTED_KUBE_NAMESPACE)
        self.assertIn("postgres:18.4-trixie@sha256:", adapter_profile.EXPECTED_POSTGRESQL_IMAGE)

    def test_reviewed_kube_context_is_a_label_not_a_hardcoded_identity_control(self):
        """Another operator must be able to run the checkpoint from their own kubeconfig.

        The reviewed context name was a hardcoded constant asserted verbatim by this fixture,
        so no other operator could execute C1. A client-side context name is not an identity
        control either; the server-side gates (namespace, pinned image digest, component
        type/version) are what bind the target.
        """

        self.assertEqual("jpiquot@local", adapter_profile.DEFAULT_REVIEWED_KUBE_CONTEXT)

        previous = os.environ.get("REVIEWED_KUBE_CONTEXT")
        os.environ["REVIEWED_KUBE_CONTEXT"] = "another-operator@local"
        try:
            reloaded = importlib.reload(adapter_profile)
            self.assertEqual("another-operator@local", reloaded.EXPECTED_KUBE_CONTEXT)
        finally:
            if previous is None:
                del os.environ["REVIEWED_KUBE_CONTEXT"]
            else:
                os.environ["REVIEWED_KUBE_CONTEXT"] = previous
            importlib.reload(adapter_profile)

        self.assertEqual(
            adapter_profile.DEFAULT_REVIEWED_KUBE_CONTEXT,
            adapter_profile.EXPECTED_KUBE_CONTEXT,
        )

    # Valid operands, each carrying an explicit unit. Individual cases override one field.
    VALID_CAPACITY = {
        "records": 1,
        "measured_record_bytes": "100B",
        "measured_index_bytes": "20B",
        "durability_multiplier": 2,
        "control_bytes": "0B",
        "reclamation_workspace": "0B",
        "scheduler_bytes": "0B",
        "host_filesystem_headroom_bytes": "0B",
    }

    def _capacity(self, **overrides):
        values = dict(self.VALID_CAPACITY)
        values.update(overrides)
        return adapter_profile.calculate_capacity(**values)

    def test_capacity_inputs_fail_closed(self):
        invalid_inputs = (
            ("fractional record count", {"records": 1.5}),
            ("negative record count", {"records": -1}),
            ("non-numeric record count", {"records": "NaN"}),
            ("mixed decimal and binary units", {"measured_record_bytes": "1KiB", "measured_index_bytes": "1KB"}),
            # Task 1 requires rejecting missing units: a bare 400 meaning 400 GB must not
            # normalize to 400 bytes, and a unit-less string must not default to bytes.
            ("bare integer record bytes", {"measured_record_bytes": 100}),
            ("unit-less string record bytes", {"measured_record_bytes": "400"}),
            ("bare integer beside a united operand", {"control_bytes": 1024, "measured_record_bytes": "1KiB"}),
            ("unknown unit", {"measured_record_bytes": "12PB"}),
            ("fractional byte result", {"measured_record_bytes": "1.5B"}),
            # A zero measurement collapses the requirement to the constant terms and
            # manufactures a "fits any horizon" proof.
            ("zero record bytes", {"measured_record_bytes": "0B"}),
            ("zero index bytes", {"measured_index_bytes": "0B"}),
            # The durability multiplier is pinned to the approved profile value.
            ("unpinned durability multiplier", {"durability_multiplier": 1}),
            ("zero durability multiplier", {"durability_multiplier": 0}),
            # An explicitly empty horizon mapping must not silently become the three defaults.
            ("empty horizon mapping", {"horizons": {}}),
            ("fractional horizon count", {"horizons": {"24h": 1.5}}),
            # A 19-digit operand must still be compared exactly rather than rounded to
            # integral by the default 28-digit decimal context.
            ("operand above the signed 64-bit bound", {"measured_record_bytes": "9223372036854775808B"}),
        )

        for label, overrides in invalid_inputs:
            with self.subTest(label=label):
                with self.assertRaises(adapter_profile.CapacityInputError):
                    self._capacity(**overrides)

    def test_capacity_includes_every_adr_mandated_operand(self):
        """Every ADR operand must move the result, and the reclamation floor must apply.

        Added 2026-07-26. The previous arithmetic had no scheduler term, no host-filesystem
        headroom, and no reclamation-workspace floor, so three ADR-mandated operands could be
        dropped with the capacity "proof" unchanged.
        """

        # base = 1 record x (100 + 20) bytes x 2 = 240 bytes; the reclamation floor dominates.
        baseline = self._capacity(horizons={"24h": 1})[0].required_bytes
        self.assertEqual(240 + adapter_profile.RECLAMATION_WORKSPACE_FLOOR_BYTES, baseline)

        for operand in ("control_bytes", "scheduler_bytes", "host_filesystem_headroom_bytes"):
            with self.subTest(operand=operand):
                moved = self._capacity(horizons={"24h": 1}, **{operand: "1KiB"})[0].required_bytes
                self.assertEqual(baseline + 1024, moved, f"{operand} must contribute to the requirement")

        # A quarter of a large dataset exceeds the fixed floor and becomes the workspace.
        large = self._capacity(
            measured_record_bytes="1GiB",
            measured_index_bytes="1GiB",
            horizons={"24h": 1024},
        )[0].required_bytes
        base = 1024 * (1024**3 + 1024**3) * 2
        self.assertEqual(base + (base // 4), large)

    def test_capacity_evidence_is_blocked_when_operands_are_absent(self):
        """Gate C1.13's producer must state exactly which measured operands are missing."""

        blocked = adapter_profile.collect_capacity_evidence({})

        self.assertEqual("blocked", blocked["status"])
        self.assertIn("CAPACITY_MEASURED_RECORD_BYTES", blocked["missing_operands"])
        self.assertIn("CAPACITY_RECORDS_24H", blocked["missing_operands"])
        self.assertEqual([], blocked["admissions"])

    def test_capacity_evidence_measures_and_judges_supplied_operands(self):
        measured = adapter_profile.collect_capacity_evidence(
            {
                "CAPACITY_MEASURED_RECORD_BYTES": "512B",
                "CAPACITY_MEASURED_INDEX_BYTES": "128B",
                "CAPACITY_CONTROL_BYTES": "1GiB",
                "CAPACITY_RECLAMATION_WORKSPACE": "0B",
                "CAPACITY_SCHEDULER_BYTES": "1GiB",
                "CAPACITY_HOST_FILESYSTEM_HEADROOM_BYTES": "1GiB",
                "CAPACITY_RECORDS_1H": "1000",
                "CAPACITY_RECORDS_24H": "24000",
                "CAPACITY_RECORDS_7D": "168000",
            }
        )

        self.assertEqual("measured", measured["status"])
        by_horizon = {verdict["horizon"]: verdict for verdict in measured["admissions"]}
        self.assertTrue(by_horizon["24h"]["admitted"])
        self.assertEqual("steady-state", by_horizon["24h"]["band"])
        # The 7-day software maximum is measured but never admitted against this profile.
        self.assertFalse(by_horizon["7d"]["admitted"])
        self.assertEqual("out-of-profile", by_horizon["7d"]["band"])

    def test_attestation_fields_default_to_unrecorded_never_to_a_claim(self):
        """AC3/AC4 fields must be representable and must never synthesize an attestation."""

        empty = adapter_profile.collect_attestations({})

        self.assertEqual("unrecorded", empty["backup_destination"])
        self.assertEqual("unrecorded", empty["restore_result"])
        self.assertEqual("unrecorded", empty["rpo"])
        self.assertEqual("unrecorded", empty["rto"])
        self.assertEqual("unrecorded", empty["out_of_profile_statement"])
        self.assertEqual("unrecorded", empty["platform_operations_approver"])
        self.assertEqual("unrecorded", empty["security_reviewer_approver"])

        supplied = adapter_profile.collect_attestations({"BACKUP_DESTINATION": "offsite-a"})
        self.assertEqual("offsite-a", supplied["backup_destination"])
        self.assertEqual("unrecorded", supplied["security_reviewer_approver"])

    def test_summaries_survive_null_and_non_mapping_kubernetes_objects(self):
        """A null spec or a scalar where a mapping is expected must not escape as a traceback."""

        self.assertEqual(
            {"name": None, "namespace": None, "generation": None, "resource_version": None},
            adapter_profile._metadata_summary({"metadata": None}),
        )
        self.assertEqual([], adapter_profile._deployment_summary({"spec": None})["images"])
        self.assertEqual([], adapter_profile._component_summary({"spec": "scalar"})["metadata_names"])
        self.assertEqual(
            0,
            adapter_profile._configuration_summary({"spec": {"accessControl": None}})["access_control_policy_count"],
        )
        self.assertIsNone(adapter_profile._statefulset_summary({"spec": []})["replicas"])
        self.assertEqual([], adapter_profile._pod_summary({"status": None})["container_images"])

    def test_read_only_command_failures_become_observations(self):
        """A missing binary must become a recorded observation, never an escaping exception."""

        observation = adapter_profile._run_command(
            ("hexalith-no-such-binary-27-3", "--version"), parse_json=False
        )

        self.assertEqual(127, observation.exit_code)
        self.assertIsNone(observation.payload)
        self.assertIn("command not found", observation.error)
        self.assertTrue(observation.started_utc)
        self.assertTrue(observation.finished_utc)

    def test_invalid_utf8_output_becomes_an_observation_not_a_traceback(self):
        """Fail-closed means never an escaping exception, including on invalid UTF-8."""

        observation = adapter_profile._run_command(
            ("/bin/sh", "-c", "printf '\\xff\\xfe not real utf-8'"),
            parse_json=False,
        )

        self.assertEqual(0, observation.exit_code)

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

    def _daprd_probe(
        self,
        *,
        executable_that_works,
        version_payload="1.18.1",
        build_info_payload="Version: 1.18.1\nGit Commit: 4cef924a",
    ):
        """Return a probe modelling the real `ghcr.io/dapr/daprd` container.

        The official image carries no shell and no populated `PATH`, so a bare
        `daprd` exec fails with `executable file not found in $PATH` while the
        image-layout path succeeds. `kubectl exec` surfaces that as exit 1.
        """

        calls = []

        def probe(pod_name, executable, flag):
            calls.append((pod_name, executable, flag))
            if executable != executable_that_works:
                return adapter_profile.CommandObservation(
                    command=("kubectl", "exec", pod_name, "--", executable, flag),
                    exit_code=1,
                    stdout_sha256="",
                    stderr_sha256="",
                    payload=None,
                    error="kubectl exited 1",
                )
            if flag == "--version":
                payload = version_payload
            else:
                payload = build_info_payload
            return adapter_profile.CommandObservation(
                command=("kubectl", "exec", pod_name, "--", executable, flag),
                exit_code=0,
                stdout_sha256="",
                stderr_sha256="",
                payload=payload,
            )

        return probe, calls

    def test_daprd_identity_probe_falls_back_to_the_image_layout_path(self):
        """Gate C1.15's producer must be able to emit its observation.

        `daprd` is not on the sidecar container's `PATH`, so probing only the
        bare name recorded `not captured; kubectl exited 1` against a live
        target that answers `1.18.1` on `/daprd`. A gate whose named producer
        cannot emit stays `not complete` regardless of the environment.
        """

        probe, calls = self._daprd_probe(executable_that_works="/daprd")

        fields, observations = adapter_profile.collect_daprd_runtime_identity(
            ["memories-b667844cf-6s9j7"], probe
        )

        self.assertEqual("1.18.1", fields["daprd_version"])
        self.assertEqual("/daprd", fields["daprd_executable"])
        self.assertEqual("memories-b667844cf-6s9j7", fields["daprd_version_probe_pod"])
        self.assertIn("Git Commit: 4cef924a", fields["daprd_build_info"])
        self.assertIn("--build-info", [flag for _, _, flag in calls])
        self.assertTrue(observations)

    def test_daprd_identity_probe_tries_every_running_pod_and_stays_fail_closed(self):
        """No candidate executable anywhere must record a blocker, never a claim."""

        probe, calls = self._daprd_probe(executable_that_works="/nowhere")

        fields, observations = adapter_profile.collect_daprd_runtime_identity(
            ["pod-a", "pod-b"], probe
        )

        self.assertIn("not captured", fields["daprd_version"])
        self.assertEqual(
            sorted({pod for pod, _, _ in calls}), ["pod-a", "pod-b"]
        )
        # Pinned, not derived from the constant under test: deriving the expected
        # count from DAPRD_EXECUTABLE_CANDIDATES made shrinking the candidate list
        # green by construction.
        self.assertEqual(("/daprd", "daprd"), adapter_profile.DAPRD_EXECUTABLE_CANDIDATES)
        self.assertEqual(4, len(observations))
        # Every branch publishes the same key set, so a field can never silently
        # vanish from the packet in place of a recorded blocker.
        self.assertEqual(set(adapter_profile.DAPRD_IDENTITY_FIELDS), set(fields))
        self.assertIn("not captured", fields["daprd_executable"])
        self.assertEqual("pod-a", fields["daprd_version_probe_pod"])
        self.assertIn("not captured", fields["daprd_build_info"])

        empty_fields, empty_observations = adapter_profile.collect_daprd_runtime_identity([], probe)
        self.assertIn("not captured", empty_fields["daprd_version"])
        self.assertEqual(set(adapter_profile.DAPRD_IDENTITY_FIELDS), set(empty_fields))
        self.assertEqual([], empty_observations)

    def test_daprd_probe_records_both_flags_as_observations_and_binds_argv_order(self):
        """The packet's command ledger must show every exec that touched the target.

        `--build-info` runs against the production target, so it needs its own row.
        The argv order is pinned here because no test constructs the real command
        line: swapping `executable` and `flag` restores `not captured` on every live
        run while the fixture stays green.
        """

        probe, _ = self._daprd_probe(executable_that_works="/daprd")
        _, observations = adapter_profile.collect_daprd_runtime_identity(["pod-a"], probe)

        self.assertEqual(["--version", "--build-info"], [o.command[-1] for o in observations])
        self.assertEqual(("/daprd", "--build-info"), tuple(observations[-1].command[-2:]))

        identity = adapter_profile.EnvironmentIdentity.from_mapping(
            {
                "KUBE_CONTEXT": "ctx",
                "KUBE_NAMESPACE": adapter_profile.EXPECTED_KUBE_NAMESPACE,
                "DEPLOYMENT_ID": "d",
                "PROFILE_ID": adapter_profile.EXPECTED_PROFILE_ID,
                "EVIDENCE_ROOT": "root",
                "DECLARED_SINGLE_COMPONENT_FAULT": "postgresql-pod-replacement",
            }
        )
        command = adapter_profile._run_daprd(identity, "pod-a", "/daprd", "--version").command
        self.assertEqual(("--", "/daprd", "--version"), command[-3:])
        self.assertIn("-c", command)
        self.assertIn("daprd", command)

    def test_daprd_build_info_failure_records_a_blocker_not_the_version(self):
        """A version that answers and a build-info that does not must not be merged."""

        probe, _ = self._daprd_probe(executable_that_works="/daprd", build_info_payload="")
        fields, _ = adapter_profile.collect_daprd_runtime_identity(["pod-a"], probe)

        self.assertEqual("1.18.1", fields["daprd_version"])
        self.assertTrue(fields["daprd_build_info"].startswith("not captured;"))
        # A blocker must name a cause; `_run_command` leaves `error` unset on exit 0.
        self.assertNotIn("None", fields["daprd_build_info"])

    def test_daprd_probe_rejects_an_exit_zero_answer_that_is_not_a_version(self):
        """Usage text or a shadowing binary must not be stamped in as the version."""

        probe, _ = self._daprd_probe(
            executable_that_works="/daprd", version_payload="Error: unknown flag --version"
        )
        fields, _ = adapter_profile.collect_daprd_runtime_identity(["pod-a"], probe)

        self.assertTrue(fields["daprd_version"].startswith("not captured;"))
        self.assertNotIn("None", fields["daprd_version"])
        self.assertTrue(adapter_profile._is_daprd_version("1.18.1"))
        self.assertTrue(adapter_profile._is_daprd_version("1.18.1-rc.1"))
        self.assertFalse(adapter_profile._is_daprd_version(""))
        self.assertFalse(adapter_profile._is_daprd_version("Usage of daprd:"))

    def test_sidecar_digests_are_bound_to_the_digest_field_and_fail_closed(self):
        """The published digest field must carry digests, and never silently empty.

        Building it from `container_images` reintroduces the exact tag-not-digest
        defect this change exists to fix, one layer above `_pod_summary`.
        """

        pods = [
            {
                "container_images": ["ghcr.io/dapr/daprd:1.18.1", "registry/memories:1"],
                "container_image_ids": [
                    "ghcr.io/dapr/daprd@sha256:b7f7d296",
                    "registry/memories@sha256:71e49b6e",
                ],
            }
        ]
        images, digests = adapter_profile.collect_sidecar_image_identity(pods)
        self.assertEqual(["ghcr.io/dapr/daprd:1.18.1"], images)
        self.assertEqual(["ghcr.io/dapr/daprd@sha256:b7f7d296"], digests)

        # Kubelet emits a bare `sha256:<id>` for an image with no repo digest; it is
        # bound through its parallel tag rather than dropped.
        bare = [
            {
                "container_images": ["ghcr.io/dapr/daprd:1.18.1"],
                "container_image_ids": ["sha256:c68e099f4bee"],
            }
        ]
        self.assertEqual(["sha256:c68e099f4bee"], adapter_profile.collect_sidecar_image_identity(bare)[1])

        # A daprd tag seen with no imageID at all records a blocker, never `[]`.
        missing = [{"container_images": ["ghcr.io/dapr/daprd:1.18.1"], "container_image_ids": [""]}]
        self.assertIn("not captured", adapter_profile.collect_sidecar_image_identity(missing)[1])
        self.assertEqual(([], []), adapter_profile.collect_sidecar_image_identity([]))

    def test_pod_summary_records_the_running_image_digest_not_only_its_tag(self):
        """AC1 and gate C1.15 require the sidecar image *digest*.

        `spec`/`status` `image` carries the mutable tag (`daprd:1.18.1`);
        only `status.containerStatuses[].imageID` carries the digest that
        identifies the bytes actually running.
        """

        summary = adapter_profile._pod_summary(
            {
                "status": {
                    "phase": "Running",
                    "containerStatuses": [
                        {
                            "name": "daprd",
                            "image": "ghcr.io/dapr/daprd:1.18.1",
                            "imageID": "ghcr.io/dapr/daprd@sha256:b7f7d296",
                        }
                    ],
                }
            }
        )

        self.assertEqual(["ghcr.io/dapr/daprd:1.18.1"], summary["container_images"])
        self.assertEqual(["ghcr.io/dapr/daprd@sha256:b7f7d296"], summary["container_image_ids"])
        self.assertEqual([], adapter_profile._pod_summary({"status": None})["container_image_ids"])

        # The two lists are published side by side, so a container with no imageID
        # (Waiting: ContainerCreating / ImagePullBackOff) must hold its slot rather
        # than rebinding every later digest to the wrong container.
        waiting = adapter_profile._pod_summary(
            {
                "status": {
                    "phase": "Running",
                    "containerStatuses": [
                        {"name": "daprd", "image": "ghcr.io/dapr/daprd:1.18.1", "imageID": ""},
                        {
                            "name": "memories",
                            "image": "registry/memories:1",
                            "imageID": "registry/memories@sha256:71e4",
                        },
                    ],
                }
            }
        )
        self.assertEqual(
            len(waiting["container_images"]), len(waiting["container_image_ids"])
        )
        self.assertEqual("", waiting["container_image_ids"][0])

    # --- Story 27.3 code review, eighth-invocation review -------------------------

    def test_profile_hash_ignores_the_evidence_root_invocation_parameter(self):
        """`EVIDENCE_ROOT` names where the packet is written, not which profile ran.

        Hashing it made `profile_sha256` - the field AC1 calls immutable profile
        material and AC5 treats as drift - a function of the operator's working
        directory: the committed packet's hash moved from `9b29cca6...` to
        `01f6ce9f...` with no runtime change at all, only a relative-to-absolute
        `EVIDENCE_ROOT`. Two invocations differing in nothing else must agree.
        """

        def identity_for(evidence_root):
            return adapter_profile.EnvironmentIdentity.from_mapping(
                {
                    "KUBE_CONTEXT": adapter_profile.EXPECTED_KUBE_CONTEXT,
                    "KUBE_NAMESPACE": adapter_profile.EXPECTED_KUBE_NAMESPACE,
                    "DEPLOYMENT_ID": "d1",
                    "PROFILE_ID": adapter_profile.EXPECTED_PROFILE_ID,
                    "EVIDENCE_ROOT": evidence_root,
                    "DECLARED_SINGLE_COMPONENT_FAULT": "postgresql-pod-replacement",
                }
            )

        relative = identity_for("_bmad-output/implementation-artifacts/tests")
        absolute = identity_for("/home/operator/repo/_bmad-output/implementation-artifacts/tests")
        trailing = identity_for("_bmad-output/implementation-artifacts/tests/")

        self.assertEqual(relative.to_profile_identity(), absolute.to_profile_identity())
        self.assertEqual(relative.to_profile_identity(), trailing.to_profile_identity())
        self.assertNotIn("evidence_root", relative.to_profile_identity())
        # `evidence_root` is still published for the reader; it is only unhashed.
        self.assertIn("evidence_root", relative.to_dict())

    def test_runtime_profile_is_comparable_to_the_reviewed_profile(self):
        """`runtime_matches_reviewed_profile` must be answerable, not structurally false.

        The runtime profile used `to_dict()`'s snake_case keys while the reviewed
        profile uses camelCase backend keys, so the two objects were disjoint and the
        published boolean could never be `true` for any runtime, however perfect -
        the same failure class as the `done`-gate verifier regex this story already
        fixed once.
        """

        reviewed = adapter_profile.canonical_pg_onprem_profile()
        identity = adapter_profile.EnvironmentIdentity.from_mapping(
            {
                "KUBE_CONTEXT": adapter_profile.EXPECTED_KUBE_CONTEXT,
                "KUBE_NAMESPACE": adapter_profile.EXPECTED_KUBE_NAMESPACE,
                "DEPLOYMENT_ID": "d1",
                "PROFILE_ID": adapter_profile.EXPECTED_PROFILE_ID,
                "EVIDENCE_ROOT": "root",
                "DECLARED_SINGLE_COMPONENT_FAULT": "postgresql-pod-replacement",
            }
        )

        self.assertEqual(
            sorted(reviewed.identity), sorted(identity.to_profile_identity())
        )
        # An approved profile whose capabilities are all proven hashes identically to
        # the reviewed one; the gate can therefore actually go green.
        proven = adapter_profile.AdapterProfile(
            identity=identity.to_profile_identity(),
            capabilities=dict(reviewed.capabilities),
            workload=adapter_profile.ADR_TWO_WRITER_WORKLOAD.to_dict(),
        )
        self.assertEqual(
            reviewed.manifest()["profile_sha256"], proven.manifest()["profile_sha256"]
        )

    def test_sidecar_is_identified_by_container_name_not_by_image_string(self):
        """A digest-pinned sidecar must not vanish because its image has no repo name.

        The repo-name substring was the only identifier, so when the daprd container's
        own `image` was a bare `sha256:<id>` - the `kind load docker-image` case the
        collector's docstring names as its motivation - the tag set came back empty,
        the `images and not digests` guard could not fire, and `([], [])` shipped as a
        positive observation.
        """

        side_loaded = [
            {
                "container_names": ["memories", "daprd"],
                "container_images": ["sha256:c68e099f4bee", "sha256:aa11bb22"],
                "container_image_ids": ["sha256:c68e099f4bee", "sha256:aa11bb22"],
            }
        ]
        images, digests = adapter_profile.collect_sidecar_image_identity(side_loaded)
        self.assertEqual(["sha256:aa11bb22"], digests)
        self.assertNotIn("sha256:c68e099f4bee", digests)
        self.assertEqual(["sha256:aa11bb22"], images)

        # A mirrored registry path that does not contain "daprd" is still found.
        mirrored = [
            {
                "container_names": ["memories", "daprd"],
                "container_images": ["registry/memories:1", "mcr.microsoft.com/dapr/sidecar:1.18.1"],
                "container_image_ids": [
                    "registry/memories@sha256:71e49b6e",
                    "mcr.microsoft.com/dapr/sidecar@sha256:9f9f9f9f",
                ],
            }
        ]
        self.assertEqual(
            ["mcr.microsoft.com/dapr/sidecar@sha256:9f9f9f9f"],
            adapter_profile.collect_sidecar_image_identity(mirrored)[1],
        )

        # Pods observed, but none carrying a daprd container, is a blocker - not `[]`.
        sidecarless = [
            {
                "container_names": ["memories"],
                "container_images": ["registry/memories:1"],
                "container_image_ids": ["registry/memories@sha256:71e49b6e"],
            }
        ]
        self.assertIn(
            "not captured", adapter_profile.collect_sidecar_image_identity(sidecarless)[1]
        )

    def test_digest_uniformity_is_a_claim_only_when_digests_were_observed(self):
        """`false` must mean "the fleet diverges", never "nothing was captured".

        The boolean collapsed a tri-state: zero digests and the collector's blocker
        string both rendered `false`, which reads as the positive claim that the fleet
        is not uniform - indistinguishable from a real mid-rollout divergence.
        """

        self.assertIs(True, adapter_profile._digest_uniformity(["ghcr.io/dapr/daprd@sha256:b7f7"]))
        self.assertIs(
            False,
            adapter_profile._digest_uniformity(
                ["ghcr.io/dapr/daprd@sha256:b7f7", "ghcr.io/dapr/daprd@sha256:c0c0"]
            ),
        )
        for empty in ([], "not captured; no daprd containerStatus carried an imageID"):
            verdict = adapter_profile._digest_uniformity(empty)
            self.assertNotIsInstance(verdict, bool)
            self.assertIn("not captured", verdict)

    def test_build_info_usage_text_is_rejected_like_a_non_version(self):
        """An exit-0 answer that is usage text is a claim, not an observation.

        `_is_daprd_version` closed this for `daprd_version`; `daprd_build_info` one
        field over accepted any exit-0 stdout verbatim.
        """

        probe, _ = self._daprd_probe(
            executable_that_works="/daprd",
            build_info_payload="Usage of /daprd:\n  -app-id string",
        )
        fields, _ = adapter_profile.collect_daprd_runtime_identity(["pod-a"], probe)

        self.assertEqual("1.18.1", fields["daprd_version"])
        self.assertTrue(fields["daprd_build_info"].startswith("not captured;"))
        self.assertIn("usage text", fields["daprd_build_info"])
        # The cause names the probed executable, never `kubectl`.
        self.assertIn("/daprd", fields["daprd_build_info"])
        self.assertNotIn("kubectl", fields["daprd_build_info"])

    def test_blocker_cause_names_the_probed_executable_and_the_right_reason(self):
        """`command[0]` is always `kubectl`; the reader needs the probed binary.

        The build-info branch also asserted "no version-shaped output", which its
        output never is.
        """

        observation = adapter_profile.CommandObservation(
            command=("kubectl", "exec", "pod-a", "-c", "daprd", "--", "/daprd", "--build-info"),
            exit_code=0,
            stdout_sha256="",
            stderr_sha256="",
            payload="",
        )
        cause = adapter_profile._observation_cause(observation, "no output")
        self.assertIn("/daprd", cause)
        self.assertNotIn("kubectl exited", cause)
        self.assertIn("no output", cause)
        self.assertNotIn("version-shaped", cause)
        self.assertEqual("/daprd", adapter_profile._probed_executable(observation))

    def test_exit_zero_non_version_wins_over_an_earlier_nonzero_exit_as_the_cause(self):
        """The shadowing-binary case must be able to reach the packet.

        `/daprd` always precedes bare `daprd` and exits 1 when absent, so
        `fallback or version` pinned the cause to that plain exit 1 and the exit-0
        usage-text answer - the informative one, and the whole reason
        `_is_daprd_version` exists - could never be reported.
        """

        def probe(pod_name, executable, flag):
            if executable == "/daprd":
                return adapter_profile.CommandObservation(
                    command=("kubectl", "exec", pod_name, "--", executable, flag),
                    exit_code=1,
                    stdout_sha256="",
                    stderr_sha256="",
                    payload=None,
                    error="kubectl exited 1",
                )
            return adapter_profile.CommandObservation(
                command=("kubectl", "exec", pod_name, "--", executable, flag),
                exit_code=0,
                stdout_sha256="",
                stderr_sha256="",
                payload="Usage of daprd:",
            )

        fields, _ = adapter_profile.collect_daprd_runtime_identity(["pod-a"], probe)

        self.assertTrue(fields["daprd_version"].startswith("not captured;"))
        self.assertIn("exited 0", fields["daprd_version"])
        self.assertIn("daprd", fields["daprd_version"])
        self.assertNotIn("exited 1", fields["daprd_version"])

    def test_blocked_probe_names_the_pod_the_retained_cause_came_from(self):
        """`daprd_version_probe_pod` was pinned to `running_pods[0]` unconditionally."""

        def probe(pod_name, executable, flag):
            if pod_name == "pod-b" and executable == "daprd":
                return adapter_profile.CommandObservation(
                    command=("kubectl", "exec", pod_name, "--", executable, flag),
                    exit_code=0,
                    stdout_sha256="",
                    stderr_sha256="",
                    payload="Usage of daprd:",
                )
            return adapter_profile.CommandObservation(
                command=("kubectl", "exec", pod_name, "--", executable, flag),
                exit_code=1,
                stdout_sha256="",
                stderr_sha256="",
                payload=None,
                error="kubectl exited 1",
            )

        fields, _ = adapter_profile.collect_daprd_runtime_identity(["pod-a", "pod-b"], probe)

        self.assertEqual("pod-b", fields["daprd_version_probe_pod"])

    def test_pod_summary_carries_container_names_in_positional_parity(self):
        """The name list is what makes the sidecar findable; parity keeps it bindable."""

        summary = adapter_profile._pod_summary(
            {
                "metadata": {"name": "memories-1"},
                "status": {
                    "phase": "Running",
                    "containerStatuses": [
                        {"name": "memories", "image": "registry/memories:1", "imageID": "sha256:aa"},
                        {"name": "daprd", "image": "ghcr.io/dapr/daprd:1.18.1", "imageID": ""},
                    ],
                },
            }
        )

        self.assertEqual(["memories", "daprd"], summary["container_names"])
        self.assertEqual(
            len(summary["container_names"]), len(summary["container_image_ids"])
        )
        self.assertEqual(len(summary["container_names"]), len(summary["container_images"]))


class AdapterProfileCheckpointPacketTests(unittest.TestCase):
    """Drive `run_adapter_profile_checkpoint` and read the packet it writes.

    Story 27.3 code review (eighth-invocation review): no test called this function
    at all, so every collector was verified one layer below the artifact and the
    wiring between them was unverified. Two mutations shipped green against the
    21-case suite - swapping the tag and digest assignments, which republishes the
    mutable tag in the field AC1/C1.15 require to be a digest and is the exact
    pre-change defect one layer up; and deleting the daprd observation append, which
    erases both `kubectl exec` rows from the packet's command ledger while the daprd
    fields still claim a captured version.
    """

    POD = {
        "metadata": {"name": "memories-1"},
        "status": {
            "phase": "Running",
            "containerStatuses": [
                {
                    "name": "memories",
                    "image": "registry/memories:1",
                    "imageID": "registry/memories@sha256:71e49b6e",
                },
                {
                    "name": "daprd",
                    "image": "ghcr.io/dapr/daprd:1.18.1",
                    "imageID": "ghcr.io/dapr/daprd@sha256:b7f7d296",
                },
            ],
        },
    }

    def _run(self, evidence_path):
        identity = adapter_profile.EnvironmentIdentity.from_mapping(
            {
                "KUBE_CONTEXT": adapter_profile.EXPECTED_KUBE_CONTEXT,
                "KUBE_NAMESPACE": adapter_profile.EXPECTED_KUBE_NAMESPACE,
                "DEPLOYMENT_ID": "memories-access-telemetry",
                "PROFILE_ID": adapter_profile.EXPECTED_PROFILE_ID,
                "EVIDENCE_ROOT": str(evidence_path.parent),
                "DECLARED_SINGLE_COMPONENT_FAULT": "postgresql-pod-replacement",
            }
        )

        def fake_kubectl(_identity, *arguments):
            items = [self.POD] if "pods" in arguments else []
            return adapter_profile.CommandObservation(
                command=("kubectl", *arguments),
                exit_code=0,
                stdout_sha256="",
                stderr_sha256="",
                payload={"items": items},
            )

        def fake_daprd(_identity, pod_name, executable, flag):
            payload = "1.18.1" if flag == "--version" else "Version: 1.18.1\nGit Commit: 4cef924a"
            return adapter_profile.CommandObservation(
                command=("kubectl", "exec", pod_name, "-c", "daprd", "--", executable, flag),
                exit_code=0,
                stdout_sha256="",
                stderr_sha256="",
                payload=payload,
            )

        original_kubectl = adapter_profile._run_kubectl
        original_daprd = adapter_profile._run_daprd
        adapter_profile._run_kubectl = fake_kubectl
        adapter_profile._run_daprd = fake_daprd
        try:
            exit_code = adapter_profile.run_adapter_profile_checkpoint(
                identity=identity,
                workload_profile="adr-27.1-two-writer-500eps",
                steady_state_minutes=30,
                purge_backlog_records=150000,
                evidence_path=evidence_path,
            )
        finally:
            adapter_profile._run_kubectl = original_kubectl
            adapter_profile._run_daprd = original_daprd
        return exit_code, evidence_path.read_text(encoding="utf-8")

    def test_packet_binds_each_collector_output_to_its_named_field(self):
        import tempfile

        with tempfile.TemporaryDirectory() as directory:
            evidence_path = Path(directory) / "packet.md"
            exit_code, packet = self._run(evidence_path)

        # Lifecycle Deployments are absent from the stubbed target, so C1 is rejected.
        self.assertEqual(1, exit_code)
        self.assertIn("evidence_is_approval: `false`", packet)

        # The digest field carries a digest and the tag field carries the tag. A swap
        # publishes the mutable tag as the digest, which is what AC1 forbids.
        self.assertIn("sidecar_image_digests: `[\"ghcr.io/dapr/daprd@sha256:b7f7d296\"]`", packet)
        self.assertIn("sidecar_images: `[\"ghcr.io/dapr/daprd:1.18.1\"]`", packet)

        # Both execs that touched the target appear as command-ledger rows.
        self.assertIn("/daprd --version", packet)
        self.assertIn("/daprd --build-info", packet)

        # The captured runtime identity is published, not a blocker.
        self.assertIn("daprd_version: `\"1.18.1\"`", packet)
        self.assertIn("sidecar_digest_is_uniform: `true`", packet)

    def test_packet_hash_is_stable_across_evidence_roots(self):
        """The same target written to two roots must publish the same profile hash."""

        import tempfile

        hashes = []
        for name in ("first", "second/nested"):
            with tempfile.TemporaryDirectory() as directory:
                evidence_path = Path(directory) / name / "packet.md"
                _, packet = self._run(evidence_path)
            line = [row for row in packet.splitlines() if row.startswith("- profile_sha256:")]
            self.assertEqual(1, len(line))
            hashes.append(line[0])

        self.assertEqual(hashes[0], hashes[1])


if __name__ == "__main__":
    unittest.main()
