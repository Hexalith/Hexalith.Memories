---
title: 'Run all tests and fix failures'
type: 'bugfix'
created: '2026-07-18'
status: 'done'
review_loop_iteration: 0
baseline_commit: '02fe7932a0ad7c506cb754d406b234a5d00d3125'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Running the full owned test surface (docker-free unit/contract lane, fast Docker-backed integration lane, and Python tooling tests) surfaces 5 genuine xUnit failures plus a repo-config gap that cascade-fails 17 tooling tests — all traced to concrete root causes, not environment flakiness.

**Approach:** Fix each root cause directly — a version-drift constant, a deferred-work ledger field-label mismatch, a CRLF-handling bug in a shared test helper, a test-isolation gap around a static telemetry counter, and a stale non-packable-project inventory — then re-run the affected lanes to confirm green.

## Boundaries & Constraints

**Always:** Fix only the 5 xUnit failures and the `release-packages.json` gap identified below; re-run each affected lane after its fix; preserve CRLF line endings, warnings-as-errors, and every currently-passing test's behavior.

**Ask First:** If unblocking any fix requires touching a file outside the Code Map below (e.g. the `deferred-work.md` schema/parser regex itself, rather than the one non-conforming entry).

**Never:** Widen the `Target artifact` parser to also accept the plural label — that would mask the one bad ledger entry instead of fixing it. Change `AccessTelemetryLifecycleMetrics` production emission code — only test isolation should change. Run, fix, or otherwise touch the nightly-only `IntegrationSlow`, `Performance`, or `Benchmark` lanes — out of scope, and no failures were found there.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| SDK prerequisite check | Installed SDK is 10.0.300 or 10.0.301 | Diagnostic reads "No .NET SDK 10.0.302 or newer" | N/A |
| Deferred-work field parse | Ledger entry `20.5-A41-ACCESS-TELEMETRY-RETENTION` | Parser reads a non-null `Target artifact` value | N/A |
| Contract-doc section extraction | Same Markdown content, CRLF vs LF line endings | `GetSection(heading)` returns identical text for both | N/A |
| Lifecycle counter isolation | Full unit-contract lane run with default parallelism | `MeterListener` observes only its own `Record` call's tags | N/A |
| Release-package inventory | 3 `AccessTelemetry*` projects with `IsPackable=false` | `validate-release-packages.ps1`'s discovered set matches `nonPackableProjects` | N/A |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs:27` -- `MinimumDotnetSdkVersion` drifted to `new(10, 0, 300)`; must match `10.0.302` used in `ErrorMessageCatalog.cs:148` and elsewhere.
- `_bmad-output/implementation-artifacts/deferred-work.md:87` -- entry `20.5-A41-ACCESS-TELEMETRY-RETENTION` uses non-conforming `Target artifacts:` (plural); schema (line 20) and 110 other entries use singular `Target artifact:`.
- `tests/Hexalith.Memories.TestHelpers/Documentation/MarkdownContractDocument.cs:366` (`NormalizeLineEndings`, called once from the constructor before `GetSection`/`GetSectionBounds` ever run) -- the old two-step `Replace("\r\n","\n").Replace('\r','\n')` mishandles repeated `\r` runs (e.g. `"\r\r\n"`), turning them into an extra blank line instead of collapsing to one `\n`.
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Capability/CapabilityAndObservabilityCheckpointTests.cs:147-168` and `tests/Hexalith.Memories.AccessTelemetry.Tests/Lifecycle/LifecycleActorCheckpointTests.cs` -- both exercise the same static `Records` counter (`src/Hexalith.Memories.AccessTelemetry/Observability/AccessTelemetryLifecycleMetrics.cs:19`) and can run concurrently.
- `tools/release-packages.json` -- `nonPackableProjects` is missing the 3 `Hexalith.Memories.AccessTelemetry*` projects, so `tools/validate-release-packages.ps1:99` throws and cascade-fails `tests/tooling/release_packages/release_packages_test.py` (13 tests) and `tests/tooling/publish_nuget/publish_nuget_test.py` (4 tests).

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs` -- change `MinimumDotnetSdkVersion` to `new(10, 0, 302)` -- aligns with the SDK pin used everywhere else in the CLI's error messaging.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- rename the line-87 `Target artifacts:` label to `Target artifact:`, and the line-89 `Re-open/claim trigger:` label to `Re-open trigger:` (the same entry's second non-conforming label, only visible once the first was fixed, since the parser's field-validation order failed on `Target artifact` first) -- conforms to the documented schema and the other 110 entries; value text unchanged.
- [x] `tests/Hexalith.Memories.TestHelpers/Documentation/MarkdownContractDocument.cs` -- rewrite `NormalizeLineEndings` to collapse any run of consecutive `\r` (optionally followed by `\n`) into a single `\n`, so `GetSection`/`GetSectionBounds` see identical normalized input regardless of line-ending pathology -- removes the extra blank lines the old two-step `Replace` produced for repeated-CR input.
- [x] `tests/Hexalith.Memories.Server.Tests/Documentation/ContractDocumentGuardTests.cs` -- add `GetSection_DoubledCarriageReturn_CollapsesToSingleLineBreakLikeLf`, a regression test asserting a doubled-`\r` (`"\r\r\n"`) input normalizes identically to the LF baseline -- added during review (both the adversarial and verification-gap review layers independently flagged that the original fix shipped without a test for the exact pathological input its own code comment cites, mirroring a corruption mode this repo's own CRLF-normalization tooling can produce).
- [x] `tests/Hexalith.Memories.AccessTelemetry.Tests/Capability/CapabilityAndObservabilityCheckpointTests.cs` + `.../Lifecycle/LifecycleActorCheckpointTests.cs` -- isolate the two classes from each other (shared non-parallel `[Collection]`) -- stops cross-test pollution of the process-wide static counter.
- [x] `tools/release-packages.json` -- add the 3 `Hexalith.Memories.AccessTelemetry*` project paths to `nonPackableProjects` -- matches the real `IsPackable=false` set the validator discovers.

**Acceptance Criteria:**
- Given the unit-contract lane (`tools/test.sh --filter "Category!=Integration"`), when run after the fixes, then all 7 projects report 0 failures (only the 1 pre-existing intentional skip remains).
- Given `tests/tooling/release_packages` and `tests/tooling/publish_nuget`, when run after the `release-packages.json` fix, then all tests pass.
- Given the fast integration lane and the other 8 tooling folders (already green), when re-run after all fixes, then they show no regression.

## Spec Change Log

## Design Notes

`LifecycleCounter_EmitsOnlyBoundedStateAndReasonLabels` starts a `MeterListener` scoped to the whole process-wide static `Records` counter. Since xUnit v3 parallelizes test classes by default, `LifecycleActorCheckpointTests` (which exercises `AccessTelemetryLifecycleProcessor`, emitting `Record(Persisted, None)`) can run concurrently and gets captured too — confirmed by re-running the failing test alone (`-parallel none`), which passes cleanly. Serialize the two classes against each other via a shared `[Collection("AccessTelemetryLifecycleMetrics")]` rather than disabling parallelism assembly-wide.

## Verification

**Commands:**
- `dotnet build Hexalith.Memories.slnx --configuration Debug` -- expected: 0 errors.
- Per unit/contract project: build then `dotnet exec <test-dll> ... ` with `DiffEngine_Disabled=true` (or `bash ./tools/test.sh --filter "Category!=Integration" --configuration Release` if the sandbox allows it) -- expected: 0 failures across all 7 projects.
- `python3 -m unittest discover -s tests/tooling/release_packages -p "*_test.py"` -- expected: OK.
- `python3 -m unittest discover -s tests/tooling/publish_nuget -p "*_test.py"` -- expected: OK.
- `bash ./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance" --configuration Release` -- expected: stays green (232 passed, 8 pre-existing skips).

## Suggested Review Order

**CRLF-handling bug (test helper)**

- Old two-step `Replace` mishandled a doubled `\r` run — rewritten as a single-pass scan.
  [`MarkdownContractDocument.cs:366`](../../tests/Hexalith.Memories.TestHelpers/Documentation/MarkdownContractDocument.cs#L366)

- Regression test pinning the exact pathological input the fix defends against.
  [`ContractDocumentGuardTests.cs:114`](../../tests/Hexalith.Memories.Server.Tests/Documentation/ContractDocumentGuardTests.cs#L114)

**Test-isolation gap (static telemetry counter)**

- New non-parallel collection so concurrent tests can't pollute the shared static counter.
  [`AccessTelemetryLifecycleMetricsTestCollection.cs:21`](../../tests/Hexalith.Memories.AccessTelemetry.Tests/Observability/AccessTelemetryLifecycleMetricsTestCollection.cs#L21)

- Applies the collection to the test asserting on captured tags.
  [`CapabilityAndObservabilityCheckpointTests.cs:18`](../../tests/Hexalith.Memories.AccessTelemetry.Tests/Capability/CapabilityAndObservabilityCheckpointTests.cs#L18)

- Applies the same collection to the concurrent emitter that was polluting it.
  [`LifecycleActorCheckpointTests.cs:17`](../../tests/Hexalith.Memories.AccessTelemetry.Tests/Lifecycle/LifecycleActorCheckpointTests.cs#L17)

**Deferred-work ledger schema conformance**

- Two non-canonical labels on the same entry, both required by the CI-inventory parser's closed vocabulary.
  [`deferred-work.md:87`](deferred-work.md#L87)
  [`deferred-work.md:89`](deferred-work.md#L89)

**Release-package inventory gap**

- Three `IsPackable=false` projects missing from the validator's expected set.
  [`release-packages.json:45`](../../tools/release-packages.json#L45)

**Peripherals**

- SDK-version constant drifted from the message strings that already said `10.0.302`.
  [`PrerequisiteChecks.cs:27`](../../src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs#L27)
