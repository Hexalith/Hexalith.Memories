# Offline PG2 C1.15 declared-observation pin inspection

Prepared on **2026-10-08** under
`spec-pg2-c1-15-offline-observation-pins.md`. The pure library compares retained
capture declarations with the existing [published C1.15 pins](access-telemetry-c1-pg2-c1-15-contract.md#invocation-and-identity).
It returns inspection data only. Integrated I2–I6 and P1–P7 remain incomplete;
Story 27.4/A41, historical dispositions and disabled Production retain their holds.

## Actual API and retained output

`tools/access_telemetry_c1_capture_semantics.py` exposes:

```python
inspect_c115_observation_pins(snapshot: JsonSnapshot) -> C115ObservationInspection
```

The caller supplies one explicit `JsonSnapshot` from the unchanged
`tools/access_telemetry_c1_interchange.py`. Its bytes and recursively frozen tree
are immutable; construction enforces the existing 1 MiB, strict UTF-8/no BOM,
duplicate-field, depth, string and integer bounds. The inspector first calls
unchanged `parse_capture(snapshot)` and then requires `producerStatus: observed`.
Malformed or contradictory captures refuse through that reader; a structurally
valid blocked capture refuses at the observed-only boundary.

The returned `C115ObservationInspection` is a frozen, slotted dataclass with
exactly these fields:

| Field | Meaning |
| --- | --- |
| `capture` | The identical supplied `JsonSnapshot`, retaining exact bytes, SHA-256 and frozen declarations; excluded from the inspection representation. |
| `pod_count` | Independently derived `len(observations.pods)`, after structural checks. |
| `pod_names` | Immutable tuple derived from each Pod's `pod` value, in input Pod order. |

The retained capture is neither normalized nor reserialized. Summary array order
does not determine Pod order. There is no pass flag, accepted artifact, authority
receipt or execution handle. Refusals reuse `InterchangeFormatError` and bounded,
content-free codes: existing reader codes, `artifact-literal-mismatch` for exact
literal/observed-only mismatches, and `c115-image-pin-mismatch` for an unapproved
per-Pod digest. No mismatching input content is included in the message.

## Exact declared pins

These constants are local to the gate-specific library; no operational verifier,
collector, configuration or deployed registry is imported or consulted.

| Capture field | Required literal |
| --- | --- |
| `profileId` | `PG-ONPREM-2` |
| `profileIdentity` | `postgresql-v2-dapr-1.18.1-postgresql-18.6-onprem-k8s1-openebs-local-retain-400g-v2` |
| `profileSha256` | `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` |
| `workloadId` | `adr-27.1-two-writer-500eps` |
| `workloadSha256` | `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f` |
| `target.context` | `jpiquot@local` |
| `target.namespace` | `hexalith-memories` |
| `target.selector` | `app.kubernetes.io/name=memories-access-telemetry` |
| `target.appId` | `memories-access-telemetry` |
| `target.actorType` | `AccessTelemetryLifecycleActor` |
| Every Pod's `runtimeVersion` | `1.18.1` |
| Every Pod's `sidecarImageDigest` | Either `sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8` (OCI index) or `sha256:edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b` (published authenticated linux/amd64 child). |

Comparison is exact, with no trimming, case folding, Unicode normalization or
runtime-version interpretation. Each Pod is checked, including later Pods.
Approved index and child may coexist, and either approved digest may repeat.

Raw `sidecarImageId` keeps the structural reader's syntax:
`(?:[A-Za-z0-9._:/@-]+)?sha256:[0-9a-f]{64}`, with a digest suffix exactly matching
that Pod's `sidecarImageDigest`. Bare digests, containerd/docker and repository
prefixes remain supported per Pod; no shared raw representation or repository
prefix allowlist is added. A matching suffix cannot authenticate its registry,
platform lineage, image content or the declared raw prefix.

`profileId` was already fixed by `parse_capture`; a changed ID is an existing
structural refusal. That reader also requires identical runtime declarations
across Pods. A one-Pod runtime change is therefore a structural contradiction;
a consistent change to all Pods is admitted structurally and refused by the new
runtime pin. New semantic-negative fixtures first establish structural success
for the four other profile/workload fields, all five target fields, consistent
runtime substitutions and every per-Pod unapproved image digest.

## Proof limits and remaining prerequisites

An observed `dirty-development` capture can be inspected here. Source cleanliness
and retained source byte identity are separate [I2 inspection work](c1-producer-binding-inspection-contract.md).
`inspect_sources` independently refuses dirty captures; a pin match changes no
source eligibility. The tests first demonstrate a complete clean retained-source
fixture, then inspect its dirty declaration and prove source eligibility refuses.

These are untrusted declarations. Structural command receipts, arbitrary neutral
`qualificationSessionId`/`sourceCommit` labels, an evidence-directory argument in
`producer.arguments` and simplified fixture command arguments can still be
inspected. No execution replay, registered command grammar, actual target
identity, Pod-selection completeness, complete stream output, genuine source
execution, image provenance, source custody, freshness or independent review is
proved. The API performs no filesystem, process, network, ambient configuration
or clock reads and calls no collector or qualification consumer.

| Work | Current assessment after this preparation |
| --- | --- |
| I2 | Offline registry/source byte inspection is prepared; deployed lookup still unconditionally refuses and accepting registrations remain absent. |
| I3 | Separate GitHub authority preparations exist; complete deployed authority/session consumption remains incomplete. |
| I4 | Only the published C1.15 declared pins are added here. Registered schema dispatch, execution/command/cleanup proofs, custody and integrated authenticated gate verification remain incomplete. |
| I5 | Full all-gate assembly, immutable accepted output and strict migration of every predecessor consumer remain incomplete. |
| I6 | This bounded lane is verified; the integrated positive case and complete authenticated negative matrix/handoff remain incomplete. |
| P1 | Complete operational provider adoption, trust and authenticated producer/gate/session facts remain unresolved. |
| P2 | Approved numeric freshness, lifetime, clock/skew and status policy remains unresolved; this API adopts none. |
| P3 | Actual cluster/session authority, protected custody/source/stream support and cleanup ownership remain unresolved. |
| P4 | The named-owner two-role exception remains a partial decision; complete role/delegation/dependency policy remains unresolved. |
| P5 | Exact approved producer/verifier/command/cleanup registrations and done gate ownership remain unresolved; this library registers none. |
| P6 | C1.16 eligible session/custody or fresh renewal and its recorded maintenance limitations remain unresolved. |
| P7 | Strict predecessor/v2 dispatch and legacy refusal for all consumers remain unresolved. The already prepared PG2 runtime profile correction does not complete consumer migration. |

Publication of the separately prepared Platform authority corrections and an
authorized root gitlink advance remain outstanding. Fixtures make no operational
grants, live calls, gate disposition, checkpoint credit or status transition.

## Reproducible offline verification

From the repository root:

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_capture_semantics.py' -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v
git diff --check
```

Every new test installs counted denial barriers before any API case, covering
filesystem reads/writes/access/removal/rename, process launch, socket/DNS/HTTP,
environment and clock access. Known dependency aliases already bound in the
inspector namespace are replaced too. Successful and refused inspection and
source eligibility API cases record zero dependency attempts. Dedicated barrier
probes demonstrate detection of prebound clock/environment/filesystem aliases,
`os.access`, `io.FileIO` and removal/rename/write attempts without live calls.
The unchanged full lane includes its existing isolated local TLS authority
fixtures; no live authority or target is contacted.

Executed results and independent review triage are recorded in the separate spec
and appended task ledger. After final verification, the durable supporting
archive is
[`_bmad-output/implementation-artifacts/tests/pg2-c1-15-offline-observation-pins/verification-receipts.zip`](../../_bmad-output/implementation-artifacts/tests/pg2-c1-15-offline-observation-pins/verification-receipts.zip).
It retains the original working-byte baseline, exact ledger prefix, preservation
checker, executed logs and source/hash receipts. Exact archive and source/hash
identities are recorded in `spec-pg2-c1-15-offline-observation-pins.md`.
`/tmp/pg2-c1-i4-observations-41meebiy/` is the original execution directory;
the supporting archive supplies durable reproduction inputs.

To recheck preservation, remain at the repository root and extract that archive
into a fresh external temporary directory. Its root must contain
`recheck-preservation.py`, `baseline.json` and `implementation-tasks-before.bin`.
The checker reads both receipts beside its own path and compares the current
repository working files without importing the inspection API:

```bash
observation_receipts=$(mktemp -d /tmp/pg2-c1-observation-receipts.XXXXXX)
python3 -m zipfile -e _bmad-output/implementation-artifacts/tests/pg2-c1-15-offline-observation-pins/verification-receipts.zip "$observation_receipts"
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 "$observation_receipts/recheck-preservation.py"
```

A successful recheck exits zero and reports no changed existing nonledger files,
`ledger_prior_prefix_matches: true`, `head_matches: true`,
`root_gitlinks_match: true` and no unexpected changed paths. It compares all
6,137 original nonledger working-file hashes against the 6,138-file baseline,
requires the exact original 21,169-byte ledger prefix, and allows the supporting
archive in its changed-path inventory. Any failed assertion is a preservation
failure; the receipts must not be regenerated to hide it. Protected Story
27.4/A41, Production, sprint and historical bytes receive no changes from this
preparation.
