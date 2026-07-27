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


if __name__ == "__main__":
    unittest.main()
