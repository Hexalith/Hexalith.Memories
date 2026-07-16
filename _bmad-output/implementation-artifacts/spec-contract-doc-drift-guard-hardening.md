---
title: 'Harden contract-document drift guards'
type: 'bugfix'
created: '2026-07-16'
status: 'done'
review_loop_iteration: 0
baseline_commit: '7c5b6790972e11bb60e2894dae162aef9a02e780'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-contract-doc-drift-guard-hardening.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Seven Story 18 contract documents rely mainly on whole-document substring assertions, allowing required vocabulary outside its authoritative table/section and leaked tool-call markup to pass.

**Approach:** Add assertion-neutral Markdown/anti-corruption helpers, migrate the six owning suites to exact heading-scoped rows and sections with count/uniqueness ties, and publish the convention for contributors and agents.

## Boundaries & Constraints

**Always:** Preserve existing source/runtime ties; fail closed on missing or duplicate exact headings; support LF/CRLF and ignore headings/tables inside fences; preserve table cells for exact row/cell checks; report opening, closing, incomplete, mixed-case, or attributed `content`, `invoke`, `parameter`, and `tool_call` markers without Shouldly/xUnit; accept ordinary Markdown, HTML, code spans, and similar tag names; record exact pass/fail/skip counts.

**Ask First:** Any production, package/reference, solution, CI, adjacent-document, completed-history, contract-value, or submodule change.

**Never:** Build a general Markdown renderer; add test-framework dependencies to TestHelpers; allow unrelated prose to satisfy a contract; weaken source/count/uniqueness checks; close the Epic 18 action before every required test and Release build passes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Clean | Ordinary Markdown or similar tags such as `<contention>` | No marker diagnostics; exact sections/rows returned | No false positive |
| Corrupt | Any supported raw tag form | Diagnostics identify marker/location | Owning test fails |
| Drifted | Missing/duplicate heading or row, phantom route, table-shaped prose elsewhere | Parser or count/uniqueness assertion fails | No global fallback |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Memories.TestHelpers/Documentation/{ContractDocumentGuard,MarkdownContractDocument}.cs` -- shared assertion-neutral scanning and narrow structural parsing.
- `tests/Hexalith.Memories.Server.Tests/Documentation/ContractDocumentGuardTests.cs` -- synthetic marker/parser boundaries.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs` -- public identity rows and project-source ties.
- `tests/Hexalith.Memories.Server.Tests/{Deployment,EventStoreIntegration,Ingestion}/**/*ContractTests.cs` -- table/section guards for deployment, routes, EventStore, memory-id, and ingest contracts.
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs` -- D9/seam/stability/reference sections.
- `CONTRIBUTING.md`, `_bmad-output/project-context.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml` -- convention and post-green tracking.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.Memories.TestHelpers/Documentation/{ContractDocumentGuard,MarkdownContractDocument}.cs` -- implement public, documented helpers with the frozen edge behavior.
- [x] `tests/Hexalith.Memories.Server.Tests/Documentation/ContractDocumentGuardTests.cs` -- cover clean/corrupt/incomplete/mixed-case/similar-name markers, fences, normalization, and missing/duplicate headings.
- [x] `tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs` -- require exactly two complete contract rows and tie Server packability/MCP PackageId to project sources while retaining runtime/Aspire evidence.
- [x] `tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs` -- scope the five contract tables, preserve source ties, and require row counts `1/2/7/6/4`.
- [x] `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` -- require each unique source route exactly once in the REST table, equal counts, exact pub/sub/health/HXL002 rows, and bounded prose.
- [x] `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/DocumentationCompletenessTests.cs` -- use exact headings, normalized environment/envelope/outcome/alert rows, and owning sections.
- [x] `tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs` and `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs` -- section-bind narrative/table claims, preserve code/reflection proofs, and use shared markup rejection.
- [x] `CONTRIBUTING.md`, `_bmad-output/project-context.md`, and only stale enforcement prose in affected docs -- add/reconcile guard guidance without changing contract semantics.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- after green evidence, close only the matching Epic 18 action with dated counts and proposal breadcrumb.

**Acceptance Criteria:**
- Given an affected document, when its owner runs, then shared marker diagnostics are empty.
- Given required table vocabulary survives only in prose, when its guard runs, then it fails.
- Given an omitted, duplicate, or phantom row/route, when guards run, then row/cell and count/uniqueness checks fail.
- Given narrative words remain outside their exact owning section, when the section loses them, then the guard fails.
- Given focused Server, CLI, Docker-free Integration tests and the Release build pass, when tracking updates, then only the matching action becomes `done` with exact evidence.

## Spec Change Log

## Design Notes

Exact ATX sections include subordinate headings and stop at the next equal/higher heading outside fences. Table parsing recognizes header/delimiter rows, returns trimmed data cells, and preserves inline Markdown; consuming suites own Shouldly assertions.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.TestHelpers/Hexalith.Memories.TestHelpers.csproj --configuration Release` -- zero warnings/errors.
- Build `Hexalith.Memories.Server.Tests`, then run its built xUnit assembly with repeated `-class` filters for the five affected classes -- all selected tests pass with exact counts.
- Build `Hexalith.Memories.Cli.Tests` and `Hexalith.Memories.IntegrationTests`, then directly run the mockability and public-surface classes -- all pass; Integration remains Docker-free.
- `dotnet build Hexalith.Memories.slnx --configuration Release` -- zero warnings/errors.

**Evidence (2026-07-16):** TestHelpers built with 0 warnings/errors. Focused Release runs passed Server 49/0/0, CLI 14/0/0, and Docker-free Integration 5/0/0; the Server result includes clean/corrupt/drifted matrix coverage plus escaped-backtick, multiline-code-span, CommonMark indentation, and HTML-comment boundaries. The Release solution build passed with 0 warnings/errors using the spec-baseline `Hexalith.Builds@2044475` props. An unpinned rerun against a concurrent user-owned pointer change to `Hexalith.Builds@802ddce` is separately blocked at restore by NU1605 (`OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.17.0 versus the repository's `OpenTelemetry` 1.16.0 pin); no scoped file caused or changes that dependency state.

## Suggested Review Order

**Shared guard foundation**

- Scan raw marker forms while respecting real fences and valid inline-code boundaries.
  [`ContractDocumentGuard.cs:20`](../../tests/Hexalith.Memories.TestHelpers/Documentation/ContractDocumentGuard.cs#L20)

- Resolve exact sections, headers, and normalized rows while excluding non-structural Markdown.
  [`MarkdownContractDocument.cs:36`](../../tests/Hexalith.Memories.TestHelpers/Documentation/MarkdownContractDocument.cs#L36)

**Authoritative contract ties**

- Bind deployment literals to exact headers, rows, and executable AppHost assignments.
  [`DeploymentConfigurationContractTests.cs:35`](../../tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs#L35)

- Derive REST routes from source and require unique rows plus synchronized narrative count.
  [`RouteSurfaceContractTests.cs:52`](../../tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs#L52)

- Pin every environment, envelope, outcome, and alert row under its owning heading.
  [`DocumentationCompletenessTests.cs:69`](../../tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/DocumentationCompletenessTests.cs#L69)

- Preserve complete opaque-ID rows and the exact duplicate-delivery guarantee.
  [`MemoryUnitIdStabilityContractTests.cs:38`](../../tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs#L38)

- Tie both public host identities to complete rows and their Aspire symbols.
  [`PublicSurfaceStabilityTests.cs:34`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs#L34)

- Bound mockability guarantees to exact sections and the shared markup rejection.
  [`MemoriesClientMockabilityContractTests.cs:260`](../../tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs#L260)

**Verification and contributor policy**

- Exercise clean, corrupt, escaped, multiline, indentation, comment, and structural-drift boundaries.
  [`ContractDocumentGuardTests.cs:18`](../../tests/Hexalith.Memories.Server.Tests/Documentation/ContractDocumentGuardTests.cs#L18)

- Publish the required structure-aware guard pattern for contributors.
  [`CONTRIBUTING.md:199`](../../CONTRIBUTING.md#L199)

- Record the concise agent rule without changing contract semantics.
  [`project-context.md:70`](../project-context.md#L70)

- Close the matching Epic 18 action only after exact green evidence.
  [`sprint-status.yaml:471`](sprint-status.yaml#L471)

- Preserve broader route-source limitations as explicit deferred work.
  [`deferred-work.md:2213`](deferred-work.md#L2213)
