# Test Automation Summary — Story 17.2 (Recovery and Feedback State Grammar)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Feature under test:** Memories Web recovery state grammar — `RecoveryStateMapper`,
  `MemoriesRecoveryActionPanel`, and its composition inside `MemoriesEvidenceCockpit`.
- **Date:** 2026-06-24
- **Framework detected:** xUnit v3 (`xunit.v3` 3.2.2) + bUnit (2.8.4-preview) + Shouldly + FrontComposer
  `Hexalith.FrontComposer.Testing` (`FrontComposerTestBase`). Matched the project's existing test stack;
  no new framework introduced.
- **Run command (sandbox):** built with serialized MSBuild (`-m:1`); executed the xUnit v3 in-process
  executable directly with `DiffEngine_Disabled=true` (project `dotnet test`/VSTest socket is blocked in
  this sandbox, per the story's Dev Agent Record).

## Result

| | Tests | Errors | Failed | Skipped |
|---|---|---|---|---|
| Baseline (existing) | 75 | 0 | 0 | 0 |
| **After gap auto-apply** | **100** | **0** | **0** | **0** |

All 100 tests pass. `git diff --check` is clean for every file touched by this workflow. The
`Hexalith.Memories.Web` source project builds clean under `TreatWarningsAsErrors=true` (0/0).

## Gaps discovered and auto-applied

The feature already shipped with focused tests. This pass mapped Story 17.2's acceptance criteria and
Task 5 test requirements against existing coverage and filled the remaining gaps. **+25 test cases.**

### Mapper coverage — `Components/Recovery/RecoveryStateMapperGapTests.cs` (new)

- **G1 — Exhaustive state × isolation precedence/safety sweep.** Cross-product of every
  `EvidencePacketState` × `EvidencePacketIsolationStatus`: asserts the mapper never throws, is
  deterministic, always traces to named contract sources, never emits `WrongCase`, collapses every
  restrictive/unauthorized scope to `Unauthorized`, suppresses risk markers + omitted-detail hints under
  restrictive scope, and never leaks count-bearing clue axes when unauthorized. (Task 5 "exhaustive
  state/precedence matrix … unknown/future enum values"; AC3 side-channel safety.)
- **G2 — Stale + compressed combination.** `StaleMemory` stays primary with a `compressed` secondary
  risk marker and the omitted detail group remains visible. (Task 5 "stale/compressed/conflict
  combinations".)
- **G3 — Stale + degraded + sources combination.** Conflict wins precedence (`Conflicting`) while
  staleness remains a visible `stale` risk marker. (Task 5 combinations; AC3 no-confident-answer.)
- **G4 — Sanitization sweep over every fixture.** Flattens all dynamic view-model strings (clue, tenant,
  case, omitted names, expansions, action labels/guidance/targets) and proves no fixture — including the
  sensitive ones — leaks bearer tokens, local paths, connection strings, JWTs, or secrets. (Task 5
  negative-leakage, broadened from 2 fixtures to all.)
- **G5 — Whitelisted diagnostic-clue shape over every fixture.** Every state yields a non-empty clue
  matching the `code=token` whitelist shape. (AC1 diagnostic clue.)

### Component coverage — `Components/Recovery/RecoveryActionPanelStateGrammarTests.cs` (new)

- **G6 — Per-state full grammar render (14 theory cases).** Renders the panel for every actionable state
  (weak, stale, degraded ×2, conflicting ×2, no-match, not-ingested, graph-gap, insufficient ×2,
  compressed, unauthorized, unknown) and asserts each shows title, explanation, diagnostic clue, a
  text-bearing severity badge, and a text-bearing affected-capability badge — never color alone — with no
  sensitive markup leak. (Task 5 "each state renders title, explanation, diagnostic clue, severity,
  affected capability"; AC4 color-is-never-the-only-signal.)
- **G7 — State-transition accessibility (4 tests).** Re-renders across packet changes:
  unauthorized→allowed (assertive `alert` → polite `status`), complete→degraded (hidden → conflicting),
  conflicting→resolved (shown → hidden), compressed→expanded (omitted-detail grammar dropped). (Task 4 +
  Advanced Elicitation transition coverage: loading→no-result, unauthorized→allowed, complete→degraded,
  compressed→expanded, conflicting→resolved.)

### Integration coverage — `Components/Evidence/EvidenceCockpitRecoveryTransitionTests.cs` (new)

- **G8 — Loading→result transition.** While loading the recovery panel is absent; once a packet arrives
  the panel appears for the resolved state. (Task 4 loading→result transition at the cockpit boundary.)
- **G9 — Dual-announcement wiring.** An unauthorized packet renders the assertive restrictive banner
  (`role=alert`) alongside the recovery panel announced politely (`role=status`, `AnnounceAssertively=false`)
  so the two live regions do not compete; only the safe `CheckAuthorization` action surfaces and no
  restricted content leaks. (Task 2 routing + AC4 announcements.)

### Supporting fixtures — `Components/Recovery/RecoveryPacketFixtures.cs` (extended)

- Added `StaleAndCompressed()` and `StaleDegradedWithSources()`, built on the canonical Story 2.7-aligned
  Evidence Packet fixtures, for the new combination tests.

## Coverage map (Story 17.2)

- **AC1** (state grammar: title/explanation/clue/severity/capability/safest action): G6 (all states) +
  existing panel tests.
- **AC2** (no-result distinctions): existing state matrix + G1 sweep; `WrongCase` proven unreachable.
- **AC3** (conflict not smoothed into a confident answer): G1, G3 + existing disagreement tests.
- **AC4** (keyboard/AT readable, color never the only signal, transition announcements): G6, G7, G8, G9 +
  existing keyboard/localization tests.

## Notes / scope boundaries

- No API endpoint tests apply — this is a Razor Component Library slice with no new HTTP surface; the
  mapper unit/contract tests are the API-equivalent layer.
- No runnable web host exists for this RCL-only slice, so Playwright/axe viewport validation remains not
  applicable (consistent with the story's Dev Agent Record); component-level accessibility is asserted via
  semantic roles, `aria-live`, and accessible names in bUnit.
- The pre-existing `git diff --check` trailing-whitespace warnings are in unrelated Story 2.7 markdown/YAML
  artifacts modified before this task; no file changed by this workflow has whitespace errors.

## Next steps

- Run the Web test lane in CI alongside the rest of the solution.
- When a runnable web host lands (later Epic 17 stories), add Playwright + axe viewport checks at 360/768/
  1024/1440px to complete Task 6's browser-level validation.

---

# Test Automation Summary — Story 17.3 (Contract-Aware Web Interaction Patterns)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-06-24
- **Story:** `_bmad-output/implementation-artifacts/17-3-contract-aware-web-interaction-patterns.md`
- **Framework detected:** xUnit v3 (3.2.2) + bUnit (2.8.4-preview) + Shouldly + `Hexalith.FrontComposer.Testing`
  (`FrontComposerTestBase`). Matched the existing stack; no new framework introduced.
- **Run command (sandbox-safe in-process runner):**
  `DiffEngine_Disabled=true ./tests/Hexalith.Memories.Web.Tests/bin/Debug/net10.0/Hexalith.Memories.Web.Tests`
  (`dotnet test`/VSTest socket is blocked in this sandbox, per the story's Dev Agent Record.)

## Result

| | Tests | Errors | Failed | Skipped |
|---|---|---|---|---|
| Baseline (existing) | 156 | 0 | 0 | 0 |
| **After gap auto-apply** | **212** | **0** | **0** | **0** |

All 212 tests pass (**+56**). The test project builds clean under `TreatWarningsAsErrors=true` (0 warnings /
0 errors), and `git diff --check` is clean for every added file.

## Scope note — API vs E2E

- **API tests:** Not applicable. Story 17.3 is an RCL-only web-interaction slice over the shared Evidence
  Packet contract; it adds no runnable API endpoints or HTTP surface. The pure mappers/validators
  (`FilterInspectionMapper`, `ContractAwareFormValidator`, `InteractionContextValidator`,
  `MemoriesCommandSurfaceMapper`, `ConfirmationPromptMapper`, `CompactGridColumnPlanner`) are the
  API-equivalent layer and are unit-tested directly.
- **Browser E2E (Playwright/axe):** Not applicable here — no runnable web host is shipped by this slice (as the
  story records). The project's component/interaction test surface is **bUnit**, which is what these gap tests use.

## Gaps discovered and auto-applied

Mapped Story 17.3's six ACs and Task 5 test requirements against existing coverage and filled the remaining
gaps, following the repo's `*GapTests.cs` convention. Tests use `data-testid`/accessible locators (no CSS-class
selectors), canonical `EvidencePacketFixtures`, no sleeps, and each builds its own fixtures (order-independent).

### Filters (AC2) — `FilterInspectionMapperGapTests.cs`, `MemoriesFilterSummaryGapTests.cs`
- Empty-state reason branches not previously exercised: `NotIngested`, `DegradedBackend`, `StaleMemory`,
  `InsufficientEvidence`, plus `Unknown` isolation → `InaccessibleScope`.
- Per-effect chip trust severity mapping; sensitive chip value redaction (mapper + rendered chip).
- No-filters render path (`mem-filter-none`); null-argument guards; component surfaces distinct empty reasons.

### Forms (AC1) — `ContractAwareFormValidatorGapTests.cs`
- Required case / text / enum / range blank-value paths → field-associated `CaseRequired` / `FieldRequired`.
- Optional text never blocks; unbounded range accepts a finite value; `Infinity`/`-Infinity` blocks dispatch.

### Grid (AC6) — `MemoriesEvidenceGridGapTests.cs`
- Planner non-compact path + guard paths (negative cap, null columns).
- Multi-source render (row count + per-row action); sensitive source URI redaction (no `C:\`, no `Bearer `).
- Non-restrictive empty → `NoMatch`; `Unknown` isolation → no rows + `InaccessibleScope`.

### Navigation / Overlays / Confirmations / Commands (AC3–AC5) — tenant isolation & stale context
`InteractionContextValidatorGapTests.cs`, `MemoriesCommandSurfaceGapTests.cs`,
`MemoriesConfirmationAndNavigationGapTests.cs`
- Missing-tenant guards (blank snapshot/active tenant).
- **Cross-tenant / cross-case leakage:** snapshot matches the active scope but the live packet belongs to
  another tenant/case → `TenantChanged` / `CaseChanged`.
- Graph/activity target existence (known graph valid; unknown → `MissingTarget`; activity w/o id valid).
- **Contract-version mismatch** disables every command (incl. tenant verification) — at the mapper and the
  rendered surface; empty graph disables only Open Graph.
- Confirmation accept/cancel transitions invoke `OnConfirm`/`OnCancel`; tenant-wide (null case) copy names
  "tenant-wide"; mapper null guards; navigation context sanitization + stale-context disabled-reason surface.

## Files added

- `tests/Hexalith.Memories.Web.Tests/Components/Filters/FilterInspectionMapperGapTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Filters/MemoriesFilterSummaryGapTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Forms/ContractAwareFormValidatorGapTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Grid/MemoriesEvidenceGridGapTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Interaction/InteractionContextValidatorGapTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Interaction/MemoriesCommandSurfaceGapTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Interaction/MemoriesConfirmationAndNavigationGapTests.cs`

## Coverage map (Story 17.3)

- **AC1** forms (scope-first, contract-aware validation, acknowledgement, dispatch gating): forms gap tests + existing.
- **AC2** inspectable filters (axes, trust effects, empty-state distinctions, contract-boundary unknowns): filters gap tests + existing.
- **AC3** navigation context preservation + return path: validator + navigation gap tests + existing.
- **AC4** safety-gated confirmations (tenant/case/object/consequence/recovery; accept/cancel): confirmation gap tests + existing.
- **AC5** command access (availability, disabled reasons, stale/version revalidation): command-surface gap tests + existing.
- **AC6** data grid (compact column priority, trust-critical columns, row actions, empty/restricted states): grid gap tests + existing.

## Next steps

- Run the Web test lane in CI alongside the rest of the solution.
- When a runnable web host lands (later Epic 17 stories), add Playwright + axe viewport checks at
  360/768/1024/1440px to complete Task 6's browser-level validation; not applicable for this RCL-only slice.

---

# Test Automation Summary — Story 17.4 (Role-Specific Web Inspection Lenses)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-06-24
- **Story:** `_bmad-output/implementation-artifacts/17-4-role-specific-web-inspection-lenses.md`
- **Framework detected:** xUnit v3 (3.2.2) + bUnit (2.8.4-preview) + Shouldly + `Hexalith.FrontComposer.Testing`
  (`FrontComposerTestBase`). Matched the existing stack; no new framework introduced.
- **Run command (sandbox-safe in-process runner):**
  `./tests/Hexalith.Memories.Web.Tests/bin/Debug/net10.0/Hexalith.Memories.Web.Tests`
  (`dotnet test`/VSTest socket is blocked in this sandbox, per the story's Dev Agent Record.)

## Result

| | Tests | Errors | Failed | Skipped |
|---|---|---|---|---|
| Baseline (existing) | 256 | 0 | 0 | 0 |
| **After gap auto-apply** | **291** | **0** | **0** | **0** |

All 291 tests pass (**+35**). The test project builds clean under `TreatWarningsAsErrors=true`
(0 warnings / 0 errors), and `git diff --check` is clean.

## Scope note — API vs E2E

- **API tests:** Not applicable. Story 17.4 is a **consume-only RCL** web slice over the shared Evidence
  Packet contract; it adds no runnable API endpoints. The pure mappers (`LensShellMapper`,
  `CaseActivityTrailMapper`, `IngestionLifecycleMapper`, `OperatorHealthMatrixMapper`,
  `BenchmarkResultComparatorMapper`, `AgentPacketInspectorMapper`) are the API-equivalent layer and are
  unit-tested directly.
- **Browser E2E (Playwright/axe):** Not applicable — no runnable web host ships with this slice (as the
  story records). The project's component test surface is **bUnit**; keyboard/accessibility/responsive
  behavior is asserted there.

## Gaps discovered and auto-applied

Mapped Story 17.4 Task 6 / Task 7 required coverage against existing tests and filled the remaining gaps.
Tests use `data-testid`/accessible locators (no CSS-class selectors), the canonical bounded
`LensPacketFixtures` inventory, no sleeps, and each builds its own fixtures (order-independent).

### Cross-cutting guardrails — `Components/Lenses/LensCrossCuttingTests.cs` (new, 20 cases)

- **Cross-lens consistency:** same packet → identical shell state / severity / affected capability /
  confidence / freshness / contract version / scope across all five lenses (5 fixtures); confidence
  suppressed identically under a restrictive scope.
- **Tenant isolation:** cross-tenant packet carries the foreign tenant/case across every lens; the Agent
  Packet copy payload / command target repartition with **no originating-tenant residue**.
- **Role-density invariance:** Ingestion / Operator Health / Benchmark / Agent Packet projections preserve
  packet semantics across roles (only ordering/density differs); **no role broadens authorization** on an
  unauthorized packet (4 roles × 5 lenses).
- **Fail-closed:** unknown isolation scope is treated as restrictively as unauthorized across every lens.
- **Contract version:** every lens always reports the supported contract version (incl. schema-mismatch /
  cross-tenant packets).
- **Stale/changed-context revalidation:** a previously-expandable compressed packet and a recoverable
  degraded packet have their expansion / recovery commands **disabled before activation** once the scope
  becomes restrictive.

### Per-lens state matrix completion (added to existing mapper test files, 13 cases)

- **Case Activity (AC1):** empty (trust-state continuity preserved), degraded explicit.
- **Ingestion (AC2):** empty (no fabricated unit), schema-mismatch fail-closed without leak.
- **Operator Health (AC3):** empty (no trust-blocking), redacted (DetailCompleteness caution, no producer
  action), schema-mismatch safe with fixed 6-check set.
- **Benchmark (AC4):** empty (MissingBaseline, nothing inferred), sensitive-axis scrubbing, schema-mismatch,
  and a sweep proving benchmark-only states (`Regression`/`Inconclusive`/`Unreproducible`) and NDCG@10 are
  **never inferred** from the bounded inventory.
- **Agent Packet (AC5):** empty (NoMatch signalled without raw-JSON inspection), redacted (omitted groups
  announced, not an error).

### Component (bUnit) — `Components/Lenses/MemoriesLensComponentsTests.cs` (2 cases)

- Cross-tenant packet renders the foreign tenant/case in the rendered shell (no tenant-a residue).
- The return action is reachable and emits the sanitized return route (keyboard-reachable return for AC1).

## Coverage map (Story 17.4)

- **AC1–AC5:** each lens has mapper + component coverage across populated / empty / degraded / unauthorized /
  unknown-scope / redacted-sensitive / schema-mismatch states.
- **Cross-cutting:** shared-shell consistency, tenant isolation, role-density invariance, fail-closed
  authorization, contract-version stability, stale-context command revalidation, and copy/diagnostics
  sanitization are covered across all five lenses.

## Next steps

- When **Story 2.7** lands canonical benchmark / ingestion-stage / MCP-tool-name / freshness / last-checked
  contract fields, replace the deferred `NoContractSource` unavailable boundaries with populated-value tests
  and add `Regression`/`Inconclusive`/`Unreproducible` benchmark coverage.
- When a runnable web host lands, add one Playwright + axe smoke path per lens at 360/768/1024/1440px
  (Task 7 viewport set) using role/label or `data-testid` selectors.

---

# Test Automation Summary — Story 18.1 (AppHost Project-Resolution Guard & Public-Surface Stability Contract)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-06-24
- **Story:** `_bmad-output/implementation-artifacts/18-1-apphost-project-resolution-guard-and-public-surface-stability-contract.md`
- **Framework detected:** xUnit v3 (3.2.2) + Shouldly (4.3.0) in `tests/Hexalith.Memories.IntegrationTests`,
  default (no-Docker) lane. Matched the existing stack; no new framework introduced.
- **Run command (sandbox):** `DiffEngine_Disabled=true dotnet exec
  tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll
  -class …AppHostProjectResolutionTests -class …PublicSurfaceStabilityTests` (`dotnet test`/VSTest socket is
  blocked in this sandbox, per the story's Dev Agent Record).

## Result

| | Discovered (IntegrationTests assembly) | Target run | Failed | Skipped |
|---|---|---|---|---|
| Baseline (existing) | 237 | 1 (AC1 guard) | 0 | 0 |
| **After gap auto-apply** | **239** | **3** | **0** | **0** |

All 3 target tests pass (**+2** new `[Fact]`s). The IntegrationTests project builds clean under
`TreatWarningsAsErrors=true` (0 warnings / 0 errors).

## Scope note — API vs E2E

- **API / Browser E2E tests:** Not applicable. Story 18.1 is a **test + docs** story; its "feature" is a
  public-surface **stability contract** (`docs/dev/public-surface-stability.md`), not a runnable HTTP/API or
  UI surface. The applicable automated tests are buildable, no-Docker **guard tests** over that contract. The
  AC1 constraint forbids `DistributedApplicationTestingBuilder` / Testcontainers, so all tests stay plain
  `[Fact]`s in the default lane.

## Gaps discovered and auto-applied

Audited the existing single AC1 guard test against the **full** documented contract. The contract doc itself
flagged that the assembly-name / root-namespace / PackageId half was "enforced by review" only — two of those
three are reflectable with no Docker, so they were untested-but-testable gaps.

- **G1 — Server assembly name + root namespace** (`Hexalith.Memories.Server`): reflectable, previously
  review-only. → new `PublicSurfaceStabilityTests.ServerAssembly_KeepsStableNameAndRootNamespace`.
- **G2 — Mcp assembly name + root namespace** (`Hexalith.Memories.Mcp`): same. → new
  `PublicSurfaceStabilityTests.McpAssembly_KeepsStableNameAndRootNamespace`.
- **G3 — Aspire symbol *shape*** (`Projects.Hexalith_Memories_*`, the dots→underscores rule the doc calls
  load-bearing): only `ProjectPath` was asserted, not the generated type name/namespace. → strengthened
  `AppHostProjectResolutionTests` with `GetType().Namespace`/`.Name` assertions.

**Not auto-applied (correctly out of reach):** the **Mcp PackageId** half of the contract is a pack-time
NuGet property, not embedded in a built assembly, so it is not reflectable at runtime. It remains
review-enforced; the contract doc was updated to state that precisely instead of lumping it with the
now-test-enforced assembly-name/root-namespace half.

## Files added / modified

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs` — **added** (2 `[Fact]`s;
  reflects over a stable public anchor type from each assembly: `IGraphQueryBuilder` for Server,
  `MemoriesMcpAuthenticationOptions` for Mcp).
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostProjectResolutionTests.cs` — **strengthened**
  (AC1 `[Fact]` now also asserts the generated symbol shape).
- `docs/dev/public-surface-stability.md` — **modified** ("Automated enforcement" section synced: assembly-name
  / root-namespace half now test-enforced; only PackageId remains review-enforced).

## Coverage map (Story 18.1)

- **AC1** (buildable project-resolution guard, no Docker): existing compile-time reference + ProjectPath
  assertions, now also the symbol-shape assertions (G3). Still a plain `[Fact]`, no fixture.
- **AC2** (EventStore wiring surface / stale-pin finding): documentation-only AC (`eventstore-integration.md`
  §1.2.1) — no runtime surface to test; left as-is.
- **AC3** (public-surface stability contract): 5/6 contract items now automated — symbol resolution, symbol
  shape, Server assembly name+namespace (G1), Mcp assembly name+namespace (G2), ProjectPath csproj. Mcp
  PackageId (6th) is review-enforced by design.

## Constraints honored

- No Docker / no `DistributedApplicationTestingBuilder` / no Testcontainers — all new tests are default-lane
  `[Fact]`s (AC1).
- No production `src/` code changed, no submodule touched, no `.slnx` / `Directory.Packages.props` /
  `release-packages.json` edits, no PublicAPI analyzer added.
- ITANEO MIT header, file-scoped namespace, global `using Xunit;` (not re-added), Shouldly assertions,
  4-space C#, final newline.

## Next steps

- Run the new tests in CI's default (no-Docker) lane alongside the rest of `Hexalith.Memories.IntegrationTests`.
- Commit as `test:` (guard tests) + `docs:` (doc sync) — **never `feat:`**; Story 18.1 has no release impact.
