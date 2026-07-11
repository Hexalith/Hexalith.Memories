---
title: 'Story 25.5: CLI Consolidation'
type: 'refactor'
created: '2026-07-11T00:00:00+02:00'
status: 'done'
baseline_revision: '94eb4c8192e86446e49cea2ea77a55054d8d2f81'
final_revision: 'a17e456cf156c1932bdb3d7fea5e8716d2cff7e1'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** CLI transport already runs through `Hexalith.Memories.Client.Rest`, but its success-output layer still contains 14 nearly identical JSON formatter classes. That duplication lets command names, envelope validation, and registrations drift.

**Approach:** Replace the 14 wrappers with one typed `JsonEnvelopeFormatter<T>` while retaining the existing `JsonEnvelopeWriter`, source-generated metadata, router, command transport, and every observable output/exit-code behavior. Add regression guards for both the formatter consolidation and the already-satisfied Client.Rest boundary.

## Boundaries & Constraints

**Always:** Keep commands and quickstart services on `MemoriesClient`; preserve JSON schema version, camelCase/indentation/null handling, stdout/stderr routing, trailing newline, command names, human/table output, and exit codes `0/1/2/4/130`. Use command-name constants where available. Preserve the receipt formatter's value-derived verify/repair name and the existing workflow-state mapping to `consistency verify`.

**Block If:** A payload cannot be rendered through `JsonEnvelopeWriter` and current `CliJsonContext` metadata without changing its wire shape, or consolidation requires a new public transport abstraction.

**Never:** Rewrite commands, `CliCommandExecutor`, Client.Rest registration/lifecycle, routes, contracts, error-envelope writers, quickstart's bespoke final envelope, or export streaming. Do not remove source-generation registrations, change public output, add packages, edit submodules, or fold in Story 25.8 dead-code cleanup.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Fixed command | Any registered success payload | One schema-v1 JSON envelope with the registered command and unchanged `data` | No error expected |
| Consistency receipt | `Kind` is `repair` or any other current value | Command is `consistency repair` for repair and `consistency verify` otherwise | Preserve existing fallback semantics |
| Invalid formatter input | Null payload/writer, blank fixed command, or null selector | Fail at the same local boundary before output is written | Standard argument exception |
| Non-JSON/export/error path | Human/table, raw export, quickstart final output, or command failure | Existing formatter/writer and exit behavior remains unchanged | Existing `CliCommandExecutor`/error envelope mapping |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.Cli/CliServices.cs` -- registers one human/table/JSON formatter per payload and owns command-name selection.
- `src/Hexalith.Memories.Cli/Output/Formatters/*JsonFormatter.cs` -- 14 success-envelope wrappers (342 lines); only the consistency receipt has value-dependent behavior.
- `src/Hexalith.Memories.Cli/Output/Formatters/JsonEnvelopeWriter.cs` -- canonical AOT-safe success-envelope serializer to retain.
- `src/Hexalith.Memories.Cli/Output/Json/CliJsonContext.cs` -- source-generated envelope metadata that must remain registered.
- `src/Hexalith.Memories.Cli/Commands/` and `src/Hexalith.Memories.Cli/Quickstart/` -- already consume `MemoriesClient`; no command-local HTTP/JSON transport exists.
- `tests/Hexalith.Memories.Cli.Tests/Cli/` -- command, formatter, error, routing, and exit-code coverage.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Memories.Cli/Output/Formatters/JsonEnvelopeFormatter.cs` -- add `IOutputFormatter<T> where T : class` with fixed-command and `Func<T,string>` construction, argument validation, `Format == Json`, and delegation to `JsonEnvelopeWriter`.
- `src/Hexalith.Memories.Cli/CliServices.cs` -- replace all 14 concrete JSON registrations with typed generic instances using command constants; use a selector for `ConsistencyCommandReceipt` and preserve workflow-state's verify mapping.
- `src/Hexalith.Memories.Cli/Output/Formatters/{ConfigShow,ConsistencyInspection,ConsistencyReceipt,ConsistencyRepairResult,ConsistencyVerificationResult,ConsistencyWorkflowState,HandlerMismatchReport,HandlerRegistrationSnapshot,HybridSearchResult,MemoryUnitIdLookup,MemoryUnit,SearchResult,StatusTelemetry,TenantList}JsonFormatter.cs` -- delete the superseded wrappers only.
- `tests/Hexalith.Memories.Cli.Tests/Cli/{ConfigShowFormatter,JsonErrorEnvelope,MemoryUnitFormatter,SearchResultFormatter,TenantListFormatter}Tests.cs` -- replace old formatter construction with the generic while retaining exact envelope assertions.
- `tests/Hexalith.Memories.Cli.Tests/Cli/JsonEnvelopeFormatterTests.cs` -- cover fixed and selector command names, exact schema/data shape, `Format`, and null/blank guards.
- `tests/Hexalith.Memories.Cli.Tests/Cli/ConsistencyRepairCommandTests.cs` -- pin repair receipt/result JSON command names; retain existing verify coverage.
- `tests/Hexalith.Memories.Cli.Tests/Cli/CliConsolidationGuardTests.cs` -- assert no payload-specific success JSON formatter classes remain and no command/quickstart code performs direct HTTP request/response serialization instead of using `MemoriesClient`.

**Acceptance Criteria:**
- Given any registered CLI success payload, when `--format json` renders it, then the envelope's schema, exact command, data shape, serialization options, writer, and newline behavior match the pre-refactor output.
- Given verify and repair consistency receipts, when JSON output is rendered, then each reports its own exact command name despite sharing one payload type.
- Given the CLI service collection, when formatters are resolved by payload and format, then exactly one JSON formatter is available per registered payload and human/table routing is unchanged.
- Given CLI command and quickstart production sources, when the consolidation guard scans them, then network operations remain delegated to Client.Rest `MemoriesClient` and no payload-specific success JSON formatter class remains.
- Given existing command success, remote error, transport error, lookup-miss, and cancellation tests, when the CLI test project runs, then output streams and exit codes remain unchanged.

## Spec Change Log

## Review Triage Log

### 2026-07-11 -- Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 5: (high 0, medium 2, low 3)
- defer: 0
- reject: 7: (high 0, medium 1, low 6)
- addressed_findings:
  - `[medium]` `[patch]` DI registration tests counted JSON formatters without exercising their command selectors; the guard now pins every registered payload's exact command mapping and generic formatter implementation.
  - `[medium]` `[patch]` The selector test covered only repair receipts; it now covers verify and unknown-kind fallback behavior as well.
  - `[low]` `[patch]` The registered payload list was hard-coded only as calls that a new payload could evade; the guard now discovers the DI payload set and compares it with the expected contract set.
  - `[low]` `[patch]` Human/table registration preservation was not asserted; the guard now pins the ordered format set for every payload, including the intentional no-table lookup payload.
  - `[low]` `[patch]` Wrapper removal was guarded by top-level filenames; the guard now inspects all CLI formatter implementation types and permits only the generic JSON formatter plus human/table formatters.

## Design Notes

The fixed-name constructor should delegate to the selector constructor. The selector is necessary because `ConsistencyCommandReceipt.Kind` determines whether the shared payload belongs to `consistency verify` or `consistency repair`; a type-only command map would regress that behavior.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release -m:1 /nr:false` -- expected: succeeds with zero warnings/errors.
- `dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Release/net10.0/Hexalith.Memories.Cli.Tests.dll -parallel none -noLogo` -- expected: the complete CLI/Client.Rest test assembly passes.
- `dotnet build Hexalith.Memories.slnx --configuration Release -m:1 /nr:false` -- expected: succeeds with zero warnings/errors.
- `git diff --check -- src/Hexalith.Memories.Cli tests/Hexalith.Memories.Cli.Tests _bmad-output/implementation-artifacts/spec-25-5-cli-consolidation.md` -- expected: no whitespace errors.

## Auto Run Result

Status: done

Summary: Replaced 14 payload-specific CLI success JSON formatters with one typed `JsonEnvelopeFormatter<T>`, retained the existing Client.Rest transport boundary and observable CLI behavior, and hardened DI, command-name, format-routing, and transport-boundary regression coverage.

Files changed:
- `src/Hexalith.Memories.Cli/CliServices.cs` -- registers the generic JSON formatter for all 14 payloads with exact command constants and the receipt selector.
- `src/Hexalith.Memories.Cli/Output/Formatters/JsonEnvelopeFormatter.cs` -- adds the fixed-name/value-selected generic success-envelope formatter.
- `src/Hexalith.Memories.Cli/Output/Formatters/*JsonFormatter.cs` -- removes the 14 superseded payload-specific wrappers.
- `tests/Hexalith.Memories.Cli.Tests/Cli/{ConfigShowFormatter,JsonErrorEnvelope,MemoryUnitFormatter,SearchResultFormatter,TenantListFormatter}Tests.cs` -- migrates direct formatter tests to the generic implementation.
- `tests/Hexalith.Memories.Cli.Tests/Cli/ConsistencyRepairCommandTests.cs` -- pins repair receipt and result JSON command names at the command surface.
- `tests/Hexalith.Memories.Cli.Tests/Cli/JsonEnvelopeFormatterTests.cs` -- covers exact wire shape, fixed/selected commands, fallback behavior, and argument guards.
- `tests/Hexalith.Memories.Cli.Tests/Cli/CliConsolidationGuardTests.cs` -- pins the complete payload/format registration set, exact DI command mappings, generic implementation, formatter topology, and Client.Rest command boundary.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- advances Story 25.5 to review.
- `_bmad-output/implementation-artifacts/spec-25-5-cli-consolidation.md` -- records the implementation contract, review triage, verification, and result.

Review findings breakdown: patched 5 findings (2 medium, 3 low), deferred 0 findings, rejected 7 findings (1 medium, 6 low). Follow-up review recommended: false; all review-driven changes were localized test hardening with no production behavior change.

Verification performed:
- CLI test project Release build passed with 0 warnings and 0 errors.
- Complete CLI/Client.Rest assembly passed 439/439 tests with 0 skipped.
- Review-focused formatter and consolidation guards passed 13/13 tests.
- Full `Hexalith.Memories.slnx` Release build passed with 0 warnings and 0 errors.
- Scoped `git diff --check` passed.

Residual risks: The CLI assembly is packable and the 14 removed formatter wrappers were public, although they were undocumented implementation types and their removal is the explicit consolidation outcome; unsupported code consumers referencing those concrete wrappers must move to `JsonEnvelopeFormatter<T>`. Docker/Aspire integration lanes were not run because this refactor does not alter transport execution or server behavior.
