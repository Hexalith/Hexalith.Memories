---
baseline_commit: e902181dcdce599187e74fd2c3c9b12f995dcc18
approved_change: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md
---

# Story 24.9: Non-Destructive Tenant-Marker Diagnostics

Status: review

Owner: Winston / Architect, Murat / Test Architect, and Developer

Implementation source: [Story 24.9 implementation spec](spec-24-9-non-destructive-tenant-marker-diagnostics.md).
The approved requirements remain canonical here; implementation tasks, executable evidence, and lifecycle reconciliation are maintained in the linked spec and recorded below.

## Story

As an operator,
I want missing and foreign tenant markers reported with distinct, safe recovery guidance,
so that incomplete evidence is not mislabeled as confirmed leakage or remediated by broad deletion.

## Acceptance Criteria

1. Given a proven-active semantic hash has a foreign non-empty `tenantId`, when verification runs, then `SemanticIsolation` fails and `Details` identify a confirmed marker mismatch/possible contamination, the exact key, expected tenant, and observed tenant without exposing payload data.
2. Given a proven-active semantic hash has no `tenantId`, when verification runs, then `SemanticIsolation` remains fail-closed but `Details` classify the result as incomplete evidence, not confirmed cross-tenant leakage.
3. Given either failure, when `Remediation` is returned, then it directs the operator to inspect and quarantine the named key and to run tenant-scoped marker repair or reindex only after provenance verification. It never recommends blanket prefix deletion, and missing versus foreign markers receive different guidance.
4. Given the V1 `TenantIsolationCheckResult` shape, when this story completes, then distinct semantics are pinned through `Details` and `Remediation` without a breaking JSON-contract change. A machine-readable issue taxonomy requires a separate versioned-contract story.

## Historical Context Classification

| Source | Classification | Permitted use |
| :----- | :------------- | :------------ |
| Story 24.3 | `historical-reference-only` | Preserve fail-closed marker evidence while correcting diagnostic meaning and remediation. |
| `remove mismatched target-prefix hashes` | `anti-template` | Quote only in negative assertions proving broad/destructive guidance is absent. |
| V1 `Details` and `Remediation` fields | `current-narrow-pattern` | Preserve the compatible contract and pin distinct operator wording in tests. |
| Story 24.8 | `current-narrow-pattern` | Consume its proven-active family classification; do not reimplement namespace membership here. |
| Story 20.2 | `current-narrow-pattern` | Re-run denial-before-dependency evidence for the tenant verifier surface. |

## Slice Proof

- One independently demonstrable outcome: safe, distinct marker diagnosis and recovery semantics.
- Demonstration boundary: focused verifier and serialization/endpoint tests cover healthy, missing, and foreign proven-active markers and prove blanket-delete guidance is absent.
- Excluded: executing repair, bulk backfill, changing the V1 JSON shape, family classification owned by Story 24.8, and physical enforcement.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md` — UPDATE lifecycle, evidence, phase ledger, scope, and File List.
- `_bmad-output/implementation-artifacts/deferred-work.md` — UPDATE only to close `24.3-MARKER-REMEDIATION` with this story's evidence.
- `_bmad-output/implementation-artifacts/spec-24-9-non-destructive-tenant-marker-diagnostics.md` — UPDATE the Spec Change Log with the executed implementation record.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — UPDATE only through workflow status synchronization.
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` — UPDATE to carry a missing/foreign marker-defect kind per `ScanSemanticHashPrefixForTenantEvidenceAsync` entry and branch `CheckSemanticIsolationAsync`'s non-classification-gap `Details`/`Remediation` construction on that kind.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs` — UPDATE existing mismatch/missing/theory coverage to the new wording and ADD missing, foreign, and mixed-markers cases.
- `tests/Hexalith.Memories.Contracts.Tests/V1/TenantIsolationCheckResultSerializationTests.cs` — UPDATE with new-wording round-trip coverage pinning the unchanged V1 `Details`/`Remediation` plain-string shape (AC4 evidence; the story's originally cited `tests/Hexalith.Memories.Server.Tests/Tenants/TenantEndpointTests.cs` does not exist in this repository — see Cross-Tenant Negative Evidence below).
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` — rerun only, as denial-before-dependency evidence; no functional change expected.

## Dev Notes

Story 24.9 depends on Story 24.8 because diagnostic meaning is valid only after active-family membership is proven. A foreign marker is possible contamination; a missing marker is incomplete evidence. Both fail closed, but neither is permission to delete a prefix. Keep exact keys and marker values payload-safe in `Details`; recovery is named-key inspection/quarantine followed by tenant-scoped repair or reindex after provenance is established.

Affected tenant-sensitive surfaces are `TenantIsolationVerifier`, `TenantIsolationCheckResult`, tenant verification endpoint/serialization, CLI/MCP/operator evidence rendering, and remediation documentation. Completion must retain Story 20.2 denial-before-dependency evidence and Story 24.3 fail-closed/tenant-marker evidence. Results are now executed and recorded below; see Cross-Tenant Negative Evidence and Planned Verification.

**Corrected during dev-story:** this story's originally drafted evidence table named a class `tests/Hexalith.Memories.Server.Tests/Tenants/TenantEndpointTests.cs` and a test method `ServerEndpointAuthorizationTests.TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState`. Neither exists in this repository (verified via `git log --all --diff-filter=A -- "*TenantEndpointTests.cs"`, empty, and a direct grep of `ServerEndpointAuthorizationTests.cs`). The actual, already-existing denial-before-dependency test for this endpoint is `ServerEndpointAuthorizationTests.TenantVerifyPost_WithMismatchedTenant_ReturnsTenantForbiddenBeforeDependencies`; AC4's V1 contract/endpoint-compatibility evidence is instead recorded through `Hexalith.Memories.Contracts.Tests.V1.TenantIsolationCheckResultSerializationTests`, the pre-existing V1 shape-pinning test the spec's own Code Map cites. Both references below are corrected accordingly.

### Epic AC Verification

Verified 2026-08-04 against `e902181dcdce599187e74fd2c3c9b12f995dcc18`.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| Story 24.3 currently treats missing and foreign markers through one mismatch collection. | implementation | `rg -n 'missing tenantId field|remove mismatched target-prefix hashes' src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` | Missing markers join mismatch evidence and broad removal guidance is emitted. | confirmed |
| V1 exposes compatible free-text diagnostic and remediation fields. | contract | `sed -n '1,90p' src/Hexalith.Memories.Contracts/V1/TenantIsolationCheckResult.cs` | Nullable `Details` and `Remediation` exist; no machine-readable issue taxonomy exists. | confirmed |
| Story 24.8 must decide active-family membership before marker meaning is assigned. | dependency | `_bmad-output/implementation-artifacts/24-8-semantic-isolation-key-family-classification.md` | Story 24.8 is registered as the predecessor and remains backlog. | confirmed |

## Cross-Tenant Negative Evidence

- **Surfaces:** `TenantIsolationVerifier`, V1 tenant-isolation result serialization, tenant verification endpoint, operator evidence rendering, and remediation text.
- **Tests:** `TenantIsolationVerifierTests.VerifyAsync_MissingActiveMarker_ReturnsIncompleteEvidenceWithoutDeleteGuidance`, `TenantIsolationVerifierTests.VerifyAsync_ForeignActiveMarker_ReturnsPossibleContaminationWithNamedKey`, `TenantIsolationVerifierTests.VerifyAsync_MixedMissingAndForeignActiveMarkers_PreservesBothDistinctDiagnoses`, `TenantIsolationVerifierTests.VerifyAsync_TwoForeignActiveMarkersSameCheck_JoinsBothKeysInOneSentence`, `TenantIsolationVerifierTests.VerifyAsync_TwoMissingActiveMarkersSameCheck_JoinsBothKeysInOneSentence`, `TenantIsolationVerifierTests.VerifyAsync_DimensionMismatchAndForeignActiveMarkerSameCheck_ComposesBothRemediationSentences`, `TenantIsolationVerifierTests.VerifyAsync_ForeignActiveMarkerWithExtraPayloadFields_DoesNotExposePayloadContent`, `TenantIsolationVerifierTests.VerifyAsync_ClassificationGapAndActiveMarkerDefect_PreservesBothDiagnostics` (now also pins `Remediation`), and `ServerEndpointAuthorizationTests.TenantVerifyPost_WithMismatchedTenant_ReturnsTenantForbiddenBeforeDependencies` (corrected name — see Dev Notes).
- **Command:** `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests`.
- **Result:** passed 2026-08-31 (post code-review-patch) — 98/98 test cases, 0 failed/skipped, in 7.478 seconds. Covers healthy, missing-marker (incomplete evidence), foreign-marker (possible contamination), mixed missing+foreign, two-same-kind (both foreign / both missing key-joining), dimension-mismatch-plus-marker composition, extra-payload-field non-leakage, and classification-gap-plus-marker (pinned classification-gap-only `Remediation`) proven-active cases, each with named-key non-destructive `Remediation` and a negative assertion that no marker `Remediation` contains `"remove mismatched target-prefix hashes"`; the existing `TenantVerifyPost_WithMismatchedTenant_ReturnsTenantForbiddenBeforeDependencies` denial-before-dependency case passed unchanged.

## Planned Verification

| Focused evidence | Command | Required result | Status |
| :--------------- | :------ | :-------------- | :----- |
| Distinct marker diagnostics | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests` | Healthy passes; missing fails as incomplete evidence; foreign fails as possible contamination; exact named-key guidance is non-destructive. | passed 2026-08-31 (post code-review-patch) — 67/67 test cases, 0 failed/skipped, in 0.688 seconds |
| Contract compatibility (AC4) | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.TenantIsolationCheckResultSerializationTests` | V1 JSON shape is unchanged and new wording round-trips without payload leakage. Replaces the originally cited, nonexistent `TenantEndpointTests` class — see Dev Notes. | passed 2026-08-31 — 7/7 test cases, 0 failed/skipped, in 0.144 seconds |
| Story 20.2 denial-before-dependency | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` | Unauthorized tenant verification is denied before verifier/backend access where applicable. | passed 2026-08-31 — covered by the combined 98/98 lane above; `TenantVerifyPost_WithMismatchedTenant_ReturnsTenantForbiddenBeforeDependencies` passed |

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-08-31 | create-story | Adopted the canonical phase ledger at the implementation handoff, linked the approved implementation spec, aligned the story baseline and lifecycle status, and preserved the approved Story and Acceptance Criteria unchanged. Owner: Amelia / Developer. Earlier deltas are not reconstructed; this runner-observed point is the story baseline. | Phase delta **+0 test cases**; cumulative story delta **+0**. Baseline discovery (`TenantIsolationVerifierTests` + `ServerEndpointAuthorizationTests`, pre-change test sources against the post-implementation `TenantIsolationVerifier.cs`): **91 test cases**, 9 failed (expected — pre-change assertions against new wording), 0 skipped. Baseline discovery (`TenantIsolationCheckResultSerializationTests`, pre-change): **5 test cases**, 0 failed, 0 skipped. Commands: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests`; `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.TenantIsolationCheckResultSerializationTests`. | `matched 2/2` against baseline `e902181dcdce599187e74fd2c3c9b12f995dcc18`: the canonical story and its approved implementation spec were the pre-implementation lifecycle artifacts. |
| 2026-08-31 | dev-story | Carried a `Missing`/`Foreign` marker-defect kind on each `ScanSemanticHashPrefixForTenantEvidenceAsync` entry (new internal `MarkerDefectKind` enum and `MarkerMismatchEvidence` record struct, never exposed through the public V1 contract) and branched `CheckSemanticIsolationAsync`'s non-classification-gap `Remediation` construction on that kind through a new `BuildSemanticIsolationRemediation` helper: foreign markers report a confirmed marker mismatch/possible contamination naming the exact key, expected tenant, and observed tenant; missing markers report incomplete evidence, not confirmed leakage; a mixed check preserves both distinct diagnoses; the anti-template "remove mismatched target-prefix hashes" wording is removed from every marker-related path. The classification-gap branch, dimension-mismatch problem collection, the V1 JSON shape, and Story 24.8's classifier were left untouched. Added `VerifyAsync_MissingActiveMarker_ReturnsIncompleteEvidenceWithoutDeleteGuidance`, `VerifyAsync_ForeignActiveMarker_ReturnsPossibleContaminationWithNamedKey`, and `VerifyAsync_MixedMissingAndForeignActiveMarkers_PreservesBothDistinctDiagnoses`; extended the pre-existing mismatch/missing/theory tests (609-747 at story-authoring time) to assert the new wording and the absence of blanket-deletion guidance; added `MarkerDiagnosticWording_ShouldRoundTripWithoutShapeBreak` (2 theory cases) to `TenantIsolationCheckResultSerializationTests` as AC4 evidence, since the story's originally cited `TenantEndpointTests.cs` does not exist in this repository. | Phase delta **+3 test cases** (`TenantIsolationVerifierTests`/`ServerEndpointAuthorizationTests` lane: 91 -> 94) and **+2 test cases** (`TenantIsolationCheckResultSerializationTests`: 5 -> 7); cumulative story delta **+5**. Final runs: 94/94 passed (0 failed/skipped, 10.539s) and 7/7 passed (0 failed/skipped, 0.203s). Normal build: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` succeeded with 0 warnings, 0 errors; same for the Contracts.Tests project. | `matched 7/7` against baseline `e902181dcdce599187e74fd2c3c9b12f995dcc18`: all seven Story 24.9 File List paths below reconcile against `git status --short` / `git diff --stat` for this story's changes; no unrelated concurrent-session paths were absorbed into this story's scope. |
| 2026-08-31 | code-review | Applied 7 patch findings from the first code-review pass, none of which changed the frozen Intent/Boundaries (zero intent-gap/bad-spec findings): (1) normalized `24-9-non-destructive-tenant-marker-diagnostics.md` and `spec-24-9-non-destructive-tenant-marker-diagnostics.md` from LF-only to CRLF via the idempotent two-step `sed`; (2) added `VerifyAsync_DimensionMismatchAndForeignActiveMarkerSameCheck_ComposesBothRemediationSentences`, proving a non-marker (dimension) problem and a marker mismatch in the same check compose both `Remediation` sentences; (3) pinned `Remediation` in `VerifyAsync_ClassificationGapAndActiveMarkerDefect_PreservesBothDiagnostics` to the exact classification-gap-only sentence, documenting that marker guidance is intentionally excluded in that co-occurring case (pre-existing `24.6-F8-W9`, unchanged, out of scope); (4) replaced `BuildSemanticIsolationRemediation`'s fragile `problemCount > markerMismatches.Count` inference with an explicit `bool hasNonMarkerProblem` computed by the caller directly from the non-marker problem source (a `problems.Count > 0` snapshot taken before classification gaps or marker details are appended), and updated the helper's signature/XML docs accordingly; (5) added `VerifyAsync_ForeignActiveMarkerWithExtraPayloadFields_DoesNotExposePayloadContent` plus a `sensitivePayloadContent` parameter on `SetupSemanticRecord` (backed by a `HashGetAllAsync` setup the verifier never invokes), proving payload/content-like data never surfaces in `Details`/`Remediation`; (6) added a "Scope precision" clause to the `24.3-MARKER-REMEDIATION` deferred-work Evidence noting the discharge applies only when no classification gap co-occurs, cross-referencing `24.6-F8-W9` (left otherwise unchanged); (7) added `VerifyAsync_TwoForeignActiveMarkersSameCheck_JoinsBothKeysInOneSentence` and `VerifyAsync_TwoMissingActiveMarkersSameCheck_JoinsBothKeysInOneSentence`, each pinning the exact comma-joined multi-key sentence for two same-kind markers in one check. | Phase delta **+4 test cases** (`TenantIsolationVerifierTests`/`ServerEndpointAuthorizationTests` lane: 94 -> 98); cumulative story delta **+9**. `TenantIsolationVerifierTests` alone: 63 -> 67. `TenantIsolationCheckResultSerializationTests` unchanged at 7 (no patch touched that project). Final runs: 98/98 passed (0 failed/skipped, 7.478s, combined lane); 67/67 passed (0 failed/skipped, 0.688s, `TenantIsolationVerifierTests` alone); 7/7 passed (0 failed/skipped, 0.144s, contract lane). Normal build: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` and the equivalent Contracts.Tests command both succeeded with 0 warnings, 0 errors. Commands: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests`; `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.TenantIsolationCheckResultSerializationTests`. | `matched 7/7` against baseline `e902181dcdce599187e74fd2c3c9b12f995dcc18`: the same seven Story 24.9 File List paths reconcile; no new path was added or removed by this patch round, and no path outside the declared File List was touched. |

### Final Gate Evidence

- Build gate: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` — succeeded 2026-08-31 (post code-review-patch) with 0 warnings, 0 errors. Same command for `tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj` also succeeded with 0 warnings, 0 errors.
- Focused lane: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` — 98/98 passed, 0 failed/skipped, 7.478 seconds.
- Contract lane: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.TenantIsolationCheckResultSerializationTests` — 7/7 passed, 0 failed/skipped, 0.144 seconds.
- Whitespace: `git diff --check` passed with no whitespace errors on every file this story touched, after normalizing the two LF-only markdown files to CRLF.
- Story gates (rerun post-patch): `check-story-file-scope.py`, `check-story-slice-scope.py --require-record`, `check-tenant-isolation-evidence.py`, and `check-story-review-readiness.py` all passed against the same 7-path File List.

## File List

- `_bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-24-9-non-destructive-tenant-marker-diagnostics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/TenantIsolationCheckResultSerializationTests.cs`

`tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` was rerun (denial-before-dependency evidence, no functional change) and is not listed as a changed path.
