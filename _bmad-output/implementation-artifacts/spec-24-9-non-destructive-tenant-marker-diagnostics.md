---
title: 'Story 24.9: Non-Destructive Tenant-Marker Diagnostics'
type: 'feature'
created: '2026-08-31'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'bcfd84012f346efc83fa1f13b1dbe3413ef6f52a'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-24-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md'
  - '{project-root}/_bmad/custom/story-phase-ledger.md'
  - '{project-root}/_bmad/custom/remediation-runtime-checklist.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** For a proven-active semantic hash, missing and foreign `tenantId` markers share one `markerMismatches` list and one shared `Remediation` string in `TenantIsolationVerifier`; foreign markers are not distinguished from missing markers in `Details`, and the shared marker-mismatch `Remediation` branch still tells operators to "remove mismatched target-prefix hashes" — the exact blanket-deletion guidance this story must stop giving.

**Approach:** Carry a missing-vs-foreign kind alongside each marker-evidence entry captured in `ScanSemanticHashPrefixForTenantEvidenceAsync`, then branch `CheckSemanticIsolationAsync`'s `Details`/`Remediation` construction on that kind so foreign markers read as confirmed mismatch/possible contamination and missing markers read as incomplete evidence, each with distinct non-destructive remediation naming the exact key. Leave the classification-gap and dimension-mismatch branches, the V1 JSON shape, and Story 24.8's classifier untouched.

## Boundaries & Constraints

**Always:** Preserve Story 24.8's classify-before-marker-evidence ordering, fail-closed behavior on every path, cursor cancellation, cross-endpoint deduplication, authorization, read-only verification (no writes/deletes/reindex), and the `TenantIsolationCheckResult.Details`/`Remediation` plain-string V1 shape.

**Ask First:** Whether to also resolve deferred ledger item `24.6-F8-W9` (classification-gap vs. dimension-mismatch `Remediation` suppression when both co-occur) while touching the same ternary — it is a different axis than this story's AC and not required to close it.

**Never:** Recommend blanket prefix/hash deletion in any marker `Remediation` text; mutate, delete, or reindex tenant data; expose payload/vector content in `Details`; change the public V1 JSON response shape; reimplement Story 24.8 family classification; modify the syntactic-only `ScanHashPrefixForTenantFieldMismatchesAsync` scanner (a separate, non-semantic check outside this AC's `SemanticIsolation` scope).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Foreign marker on proven-active record | Active raw/NL semantic hash has a non-empty `tenantId` differing from the requested tenant | `SemanticIsolation` fails; `Details` states confirmed marker mismatch/possible contamination, naming the exact key, expected tenant, and observed tenant | No payload/vector data included |
| Missing marker on proven-active record | Active raw/NL semantic hash has no `tenantId` field | `SemanticIsolation` still fails closed; `Details` classifies the result as incomplete evidence, not confirmed leakage | No payload/vector data included |
| Remediation for foreign marker | Foreign-marker `Details` present | `Remediation` directs inspecting/quarantining the named key, then tenant-scoped marker repair or reindex only after provenance verification | Never recommends blanket prefix deletion |
| Remediation for missing marker | Missing-marker `Details` present | `Remediation` gives wording distinct from the foreign-marker case, same non-destructive inspect/quarantine pattern | Never recommends blanket prefix deletion |
| Mixed markers in one check | Both missing and foreign markers found across scanned keys in the same verification | Both distinct diagnoses are preserved in `Details`/`Remediation`, not collapsed into one generic message | Existing classification-gap handling unaffected |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:602-645` -- `ScanSemanticHashPrefixForTenantEvidenceAsync`: classifies each hash via `SemanticKeyFamilyClassifier.Classify` (602) before marker checks, excludes non-active families (618-622), and captures missing-marker (633-638) and foreign-marker (640-645) entries into one shared `markerMismatches` list with no kind distinction today.
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:369-385` -- `CheckSemanticIsolationAsync`: merges classification-gap and marker-mismatch problems into one `problems` list; the `Remediation` ternary (382-384) keys only on `hasClassificationGap`, and its non-gap branch currently ends "...and remove mismatched target-prefix hashes" — the anti-template phrase AC3 forbids continuing.
- `src/Hexalith.Memories.Server/Infrastructure/SemanticKeyFamily.cs:9`, `SemanticKeyFamilyClassifier.cs:34,134` -- `Classify(...)` and `IsActiveMarkerEvidenceFamily(...)`, the existing Story 24.8 integration points; call, do not reimplement.
- `src/Hexalith.Memories.Contracts/V1/TenantIsolationCheckResult.cs:21,25` -- `Details`/`Remediation` are plain nullable strings with `JsonIgnore(WhenWritingNull)`; no shape change needed.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs:609-633,667-688,690-747,827,855` -- existing foreign/missing/classification-gap coverage to extend under the story's own planned names below. Redis is fully mocked via NSubstitute (`CreateVerifier`/`SetupSemanticRecord`/`SetupRedisKeyScan` helpers in this file) — no live backend needed.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantEndpointTests.cs` -- extend for V1 contract/endpoint compatibility evidence (named in the story's Cross-Tenant Negative Evidence).
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` -- rerun for Story 20.2 denial-before-dependency evidence; no change expected.
- `tests/Hexalith.Memories.Contracts.Tests/V1/TenantIsolationCheckResultSerializationTests.cs` -- existing V1 contract pinning test; must still pass unchanged, proving no JSON-shape break.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- `24.3-MARKER-REMEDIATION` (~line 1925) is the ledger item this story closes; `24.6-F8-W9` (~line 3444) is the orthogonal classification-gap/dimension-mismatch prioritization concern — out of this story's AC scope per Boundaries.
- `_bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md` -- lacks a `## File Scope` section (unlike Stories 24.7/24.8); add one during implementation naming the files above, matching this story's own Dev Notes "Affected tenant-sensitive surfaces" list.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` -- carry a missing/foreign kind with each `ScanSemanticHashPrefixForTenantEvidenceAsync` marker entry (633-645) and branch `CheckSemanticIsolationAsync`'s `Details`/`Remediation` construction (369-385) on that kind, replacing the shared "remove mismatched target-prefix hashes" wording with distinct, non-destructive, named-key inspect/quarantine/tenant-scoped-repair-after-provenance-verification guidance for each -- implements AC1-AC3.
- [x] `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs` -- add `VerifyAsync_MissingActiveMarker_ReturnsIncompleteEvidenceWithoutDeleteGuidance` and `VerifyAsync_ForeignActiveMarker_ReturnsPossibleContaminationWithNamedKey` (named in the story's Cross-Tenant Negative Evidence), extend the existing mismatch/missing/theory tests at 609-747 to assert the new `Remediation` wording, add a mixed-markers case, and assert no marker `Remediation` text contains blanket prefix-deletion language -- implements AC1-AC4 evidence.
- [x] `tests/Hexalith.Memories.Contracts.Tests/V1/TenantIsolationCheckResultSerializationTests.cs` -- add a round-trip case pinning that the V1 shape still serializes the unchanged `Details`/`Remediation` string fields with the new wording -- implements AC4. (Spec drift found and corrected during dev-story: the originally cited `tests/Hexalith.Memories.Server.Tests/Tenants/TenantEndpointTests.cs` does not exist in this repository; AC4 evidence was moved here instead, per the story's Dev Notes.)
- [x] `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` -- rerun as denial-before-dependency evidence; no functional change expected. (The originally cited test method name did not exist; the real, already-passing method is `TenantVerifyPost_WithMismatchedTenant_ReturnsTenantForbiddenBeforeDependencies`.)
- [x] `_bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md` -- add a `## File Scope` section, flip Dev Notes' "Planned results are `pending`" once executed, and record verification evidence/File List per the story-gate workflow.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- close `24.3-MARKER-REMEDIATION` with this story's evidence; do not touch `24.6-F8-W9` or unrelated entries.
- [x] `sprint-status.yaml` -- flip `24-9-non-destructive-tenant-marker-diagnostics` from `backlog` once implementation and review complete. (Now `review`; `done` is set at the end of the review phase.)

**Acceptance Criteria:**
- Given a proven-active semantic hash has a foreign non-empty `tenantId`, when verification runs, then `SemanticIsolation` fails and `Details` identify a confirmed marker mismatch/possible contamination, the exact key, expected tenant, and observed tenant without exposing payload data.
- Given a proven-active semantic hash has no `tenantId`, when verification runs, then `SemanticIsolation` remains fail-closed but `Details` classify the result as incomplete evidence, not confirmed cross-tenant leakage.
- Given either failure, when `Remediation` is returned, then it directs the operator to inspect and quarantine the named key and to run tenant-scoped marker repair or reindex only after provenance verification, never recommends blanket prefix deletion, and missing vs. foreign markers receive different guidance.
- Given the V1 `TenantIsolationCheckResult` shape, when this story completes, then distinct semantics are pinned through `Details` and `Remediation` without a breaking JSON-contract change.

## Design Notes

Introduce a small internal (non-public) marker-defect kind — e.g. an enum with `Missing`/`Foreign` values — carried on each `markerMismatches` entry, since a single check can surface both kinds at once (the mixed-markers I/O row) and `Details`/`Remediation` must branch per-entry, not per-check. Keep it entirely internal to `TenantIsolationVerifier`; it must not appear in the public contract.

## Spec Change Log

- 2026-08-31 (dev-story): Implemented per this spec's frozen Intent/Boundaries/Design Notes without renegotiation. `TenantIsolationVerifier.ScanSemanticHashPrefixForTenantEvidenceAsync` now carries an internal `MarkerDefectKind` (`Missing`/`Foreign`) on each `MarkerMismatchEvidence` entry; `CheckSemanticIsolationAsync`'s non-classification-gap `Remediation` branches on that kind via a new `BuildSemanticIsolationRemediation` helper, naming the exact key(s) per kind and never recommending blanket prefix deletion. Classification-gap wording, dimension-mismatch problem collection, the V1 JSON shape, and Story 24.8's classifier are unchanged. Corrected two stale evidence-table references inherited from the story draft: `tests/Hexalith.Memories.Server.Tests/Tenants/TenantEndpointTests.cs` does not exist in this repository (AC4 evidence moved to the pre-existing `Hexalith.Memories.Contracts.Tests.V1.TenantIsolationCheckResultSerializationTests`, extended with `MarkerDiagnosticWording_ShouldRoundTripWithoutShapeBreak`), and `ServerEndpointAuthorizationTests.TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState` does not exist (the real, already-passing denial-before-dependency test is `TenantVerifyPost_WithMismatchedTenant_ReturnsTenantForbiddenBeforeDependencies`). Verification evidence, File Scope, and Change Log are recorded in `24-9-non-destructive-tenant-marker-diagnostics.md`. The Boundaries "Ask First" item (whether to also resolve `24.6-F8-W9`, the classification-gap-vs-dimension-mismatch `Remediation` priority when both co-occur) was left unresolved and out of scope, per the spec's own instruction that it is a different axis than this story's AC.

## Verification

**Commands:**
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` -- expected: zero warnings/errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Tenants.TenantEndpointTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` -- expected: all cases pass, 0 failed/skipped.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.TenantIsolationCheckResultSerializationTests` -- expected: passes unchanged, proving no JSON-shape break.
- `python3 tools/check-story-file-scope.py --story-key 24-9-non-destructive-tenant-marker-diagnostics --changed-files-file /tmp/hexalith-story-24-9-owned-paths.txt && python3 tools/check-story-slice-scope.py --story-key 24-9-non-destructive-tenant-marker-diagnostics --changed-files-file /tmp/hexalith-story-24-9-owned-paths.txt --require-record && python3 tools/check-tenant-isolation-evidence.py --story-key 24-9-non-destructive-tenant-marker-diagnostics --changed-files-file /tmp/hexalith-story-24-9-owned-paths.txt && python3 tools/check-story-review-readiness.py --story-key 24-9-non-destructive-tenant-marker-diagnostics --changed-files-file /tmp/hexalith-story-24-9-owned-paths.txt` -- expected: all four gates pass over the story's declared File Scope.
- `git diff --check` -- expected: no whitespace errors.

## Suggested Review Order

**Marker-defect classification**

- Entry point: the missing/foreign distinction the whole story hinges on, never exposed publicly.
  [`TenantIsolationVerifier.cs:670`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L670)

- One classified defect (kind, key, payload-safe detail) replaces the old bare string list.
  [`TenantIsolationVerifier.cs:685`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L685)

- Missing marker captured as incomplete evidence, not confirmed leakage.
  [`TenantIsolationVerifier.cs:645`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L645)

- Foreign marker captured as confirmed mismatch/possible contamination, naming both tenants.
  [`TenantIsolationVerifier.cs:655`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L655)

**Non-destructive remediation composition**

- Builds distinct, named-key guidance per kind; never recommends blanket prefix deletion.
  [`TenantIsolationVerifier.cs:694`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L694)

- `hasNonMarkerProblem` is snapshotted explicitly before gaps/markers are appended — not inferred by count subtraction.
  [`TenantIsolationVerifier.cs:373`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L373)

- Wires the classified evidence into the check result; classification-gap branch untouched.
  [`TenantIsolationVerifier.cs:389`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L389)

**Boundary proofs**

- Foreign marker: confirmed mismatch wording, named key, no blanket-deletion language.
  [`TenantIsolationVerifierTests.cs:798`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L798)

- Missing marker: incomplete-evidence wording, distinct from the foreign case.
  [`TenantIsolationVerifierTests.cs:767`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L767)

- Mixed missing+foreign in one check preserves both distinct diagnoses.
  [`TenantIsolationVerifierTests.cs:831`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L831)

- Dimension mismatch plus a marker defect compose both remediation sentences together.
  [`TenantIsolationVerifierTests.cs:939`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L939)

- Payload/content fields never leak into Details or Remediation.
  [`TenantIsolationVerifierTests.cs:975`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L975)

- Pins the accepted, out-of-scope classification-gap-vs-marker priority (tracked separately as `24.6-F8-W9`).
  [`TenantIsolationVerifierTests.cs:1117`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L1117)

- V1 `Details`/`Remediation` plain-string shape round-trips unchanged with the new wording.
  [`TenantIsolationCheckResultSerializationTests.cs:92`](../../tests/Hexalith.Memories.Contracts.Tests/V1/TenantIsolationCheckResultSerializationTests.cs#L92)

**Peripherals**

- Two same-kind markers join into one sentence rather than one-per-key.
  [`TenantIsolationVerifierTests.cs:870`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L870)

- Ledger closure for `24.3-MARKER-REMEDIATION`, scoped precisely against the co-occurrence caveat above.
  [`deferred-work.md:1925`](deferred-work.md#L1925)

- Lifecycle evidence, File Scope, and Change Log for the story.
  [`24-9-non-destructive-tenant-marker-diagnostics.md`](24-9-non-destructive-tenant-marker-diagnostics.md)
