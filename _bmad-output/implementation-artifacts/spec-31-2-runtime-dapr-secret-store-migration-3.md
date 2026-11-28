---
title: '31.2 review fixes for the runtime Dapr secret store'
type: 'bugfix'
created: '2026-11-28'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '31-2-runtime-dapr-secret-store-migration'
baseline_commit: '860a9622559a7fdaac2838250cc504b9cc39303c'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md'
---

<frozen-after-approval reason="human-owned intent">

## Intent

Resolve the 17 review patches recorded for Story 31.2. Make the probe and inventory checks reliable and reconcile the owner decisions, operating guidance, and verification record so the story can return to `review`.

## Boundaries & Constraints

Use the recorded 2026-11-28 owner decisions and sanitized live evidence. Keep the live replay tied to the bytes that ran, and verify this patch locally with synthetic data. Preserve the deployed cluster, Secret contents, tracked deployment manifests, Story 31.1 platform sections, and CiCd-owned workflows. Keep the remaining platform work with its recorded owners.

## I/O & Edge-Case Matrix

| Scenario | Expected result |
| --- | --- |
| Two or three arguments, matching app and runtime identity, and the expected policy or read result | Exit `0`. Accept hyphens in prereleases. Vault checks take an optional version. |
| Valid invocation with wrong identity, runtime, or result | Exit `1` with sanitized diagnostics. |
| Wrong argument count, malformed version or `+build` suffix, or missing `builtin` version | Exit `2` before any request. |
| Either dated inventory has a duplicate, missing entry, or mismatched name or type | The inventory guard fails even if the combined July/November inventory would match. |

</frozen-after-approval>

## Code Map

- Reuse the BusyBox request transport and awk parser in `tools/probe-dapr-secret-scope.sh`, `ScopeProbeTests.replay` in `tests/tooling/runtime_secret_store/test_scope_probe.py`, and `MarkdownContractDocument` in `tests/Hexalith.Memories.Server.Tests/Deployment/RuntimeSecretStoreMigrationTests.cs`.
- `_bmad-output/implementation-artifacts/tests/31-2-runtime-secret-store-evidence.md` holds the restore, rollout, probe matrix, residue and dated inventory sections. `docs/operations/openbao.md` publishes the inventory and operator cautions. The three Kubernetes overlays and base RBAC supply tracked-state evidence.
- The parent story's `Review Findings` records Administrator's Option 1 decisions. Its ledger, File List and File Scope are governed by `_bmad/custom/story-phase-ledger.md` and `tools/check-story-review-readiness.py`. The prior rollout spec holds the original recovery and residue approvals.

## Tasks & Acceptance

- [x] `tools/probe-dapr-secret-scope.sh` and `tests/tooling/runtime_secret_store/test_scope_probe.py` — check arity first, share the BusyBox awk runtime-version pattern, invoke BusyBox date and document required applets. Assert exact exit codes, build suffix and arity rejection, prerelease acceptance, and secret-safe output. Shut down, join, then close the test server.
- [x] `tests/Hexalith.Memories.Server.Tests/Deployment/RuntimeSecretStoreMigrationTests.cs` — assert 14 distinct name/type rows on each November date, exact parity and membership, and retain July and operations-inventory coverage.
- [x] `_bmad-output/planning-artifacts/epics.md`, the parent story, and the evidence file — attribute the control-plane and recovery prerequisites to Platform Operations. Record dated sole-owner C2, residue retention, metadata-inspection acknowledgment and restore caveat dispositions. Link the November 28 Vault replay and refresh the five app-ID declarations in the story.
- [x] `docs/operations/openbao.md` and `_bmad-output/implementation-artifacts/deferred-work.md` — distinguish live grants and the HTTP 403 metadata blocker from tracked overlays, warn about qualification and production apply effects, and cite November 28 Redis reads. Assign DW-781 through DW-789, close the stale built-in-path deferral, and consolidate probe CI entries with their provenance.
- [x] Parent story, evidence file, and `_bmad-output/implementation-artifacts/sprint-status.yaml` — correct `spec-2` scope, reconcile the historical 22 paths and test deltas, then append current counts and scope. Attribute the overlay to `17483c2f0a9310c5e27043fe7a38048b726784de`. Account for external `d67ae5d728f87555542d30e3d28751cca02a248a`, `de86d4dae59cc1b6b9e991eb6dc0b6251b0369ab` and `2ec5b64226a0da96821a282050753136186563bf`. Individually exclude the ten gitlinks from `8caf3e576498a4f927291b677d86f343e0d65fb4`. Add this spec and the epic hunk to allowed scope. Check off verified patches and set both statuses to `review` after verification.

**Acceptance criteria**

- Given synthetic probe responses and either dated projection, when the focused suites run, then the matrix is enforced and no canary reaches output.
- Given the recorded rollout and review decisions, when operators read the updated records, then live, tracked and deferred states agree and each retained residue Secret has an owner, consequence and reopen trigger.
- Given the creation and patch baselines, when counts and paths are reconciled and the readiness tool runs at `review`, then story-owned deltas, external deltas and named exclusions are reproducible and the gate passes.

## Implementation Notes

- Completed all 17 review patches. Live evidence and local probe verification use separate digests. The parent story and sprint entry are at `review`.
- Verified 177 xUnit cases, 3 Python methods with 56 probe subtests, 11 inventory mutations and 23 cumulative story paths. [Verification record](tests/31-2-runtime-secret-store-evidence.md#review-patch-verification-on-november-28).
- Applied the three review layers' findings. The amended probe checks the full prerelease grammar and outputs fixed markers for unverified metadata. The BusyBox-only stdin suite verifies request ordering and diagnostics. All 66 probe subtests and 177 xUnit cases passed, and the inventory guard rejected 13 scratch controls.

## Spec Change Log

## Review Triage Log

| Layer and finding | Verdict | Route and evidence |
| --- | --- | --- |
| Blind hunter · SemVer validation accepts malformed input | medium | Patch. Matching malformed argument and metadata values pass the old pattern and reach both endpoints. |
| Blind hunter · Runtime diagnostics expose version-shaped metadata | high | Patch. A loopback mismatch can echo a version-shaped canary verbatim. |
| Blind hunter · Metadata-gate request order is untested | medium | Patch. Exit 1 alone cannot show whether a secret request followed an identity, runtime or store-registration failure. |
| Blind hunter · July inventory duplicates collapse before validation | low | Patch. The test converts the July names to a set before its count assertion. |
| Blind hunter · Full candidate apply effects are missing | medium | Patch. The candidate inherits production workload scaling and addresses a different telemetry scope and bootstrap combination from the live deployment. |
| Blind hunter · Residue deletion conditions are ambiguous | medium | Patch. Each row combines an appearance, a rotation and metadata confirmation with the deletion instruction. Fresh zero-consumer evidence and authorized metadata confirmation are both required. |
| Blind hunter · Path recipe absorbs unrelated worktree changes | medium | Patch. It adds the entire worktree delta and all untracked files to its candidates. |
| Blind hunter · Shared-artifact sub-scope lacks hunk evidence | low | Patch. Filename scope validation cannot confirm that the epic and sprint edits are limited to their Story 31.2 hunks. |
| Blind hunter · The rollout ledger presents an inferred before total as measured | medium | Patch. The recorded before total was inferred from the unchanged discovery surface. Distinguish that from observed measurements at identified revisions. |
| Blind hunter · Historical closure sits under unrelated DW-735 | low | Patch. The free-form November 22 entry is positioned within the capacity entry. Preserve its closure under DW-787. |
| Blind hunter · The isolated upgrade pin ignores a later script fix | medium | Patch. ACL remediation can use the existing pin. A save-script remediation needs the verified build that contains it. |
| Edge-case hunter · Empty prerelease identifier can pass | medium | Patch. The shared pattern allowed consecutive or leading dots. Covered by the SemVer fix. |
| Edge-case hunter · A version-shaped credential can be emitted | high | Patch. The previous mismatch path printed the response-derived version. Covered by the fixed diagnostic markers. |
| Edge-case hunter · Overlay alignment drops the pinned sidecar image requirement | medium | Patch. The previous flat deferral required the pin and DW-787 initially omitted it. |
| Verification gap · Bare date dependency survives the test | medium | Patch. The bare-date mutation passed the inherited-PATH suite and failed with a POSIX stdin launcher and a BusyBox-only PATH. |
| Verification gap · Malformed metadata classification can regress silently | medium | Patch. The metadata-only suffix mutation still returned 1 on mismatch and printed the malformed value. Assert the fixed unrecognized marker. |

## Verification

- Run `sh -n` and `python3 -B -m unittest discover -s tests/tooling/runtime_secret_store -p '*.py' -v` with BusyBox 1.37.0 on PATH. Record method and subtest deltas separately.
- Build Server.Tests in Debug with project references and `-m:1`. Run xUnit discovery and the migration, OpenBao platform, production deployment and operational runbook classes directly. Expect 177 cases. Check duplicate, missing and type-drift inventory controls in scratch.
- Reconcile historical Debug discovery as `2475 + 7 story + 119 external = 2601` and Deployment as `133 + 7 + 2 = 142`. The historical Python transition is `2 → 3` methods and `13 → 46` subtests. The cumulative 3 methods include 1 external method.
- Record exact name-status comparisons and live versus local probe hashes. Run `git diff --check`, tenant-isolation evidence and story scope with the actual patch paths. Run story review readiness with the independent cumulative path list and record the exact command and final output.
