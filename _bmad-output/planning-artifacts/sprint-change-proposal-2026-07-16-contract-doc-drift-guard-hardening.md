# Sprint Change Proposal — Contract-Document Drift-Guard Hardening

**Date:** 2026-07-16  
**Project:** memories  
**Mode:** Incremental  
**Approval:** Approved for implementation by the user on 2026-07-16 after incremental review of all four component edits and the complete proposal.

## 1. Issue Summary

Epic 18 retrospective action 3 asks the repository to generalize the contract-document guard lessons learned during downstream consumer hardening: prefer table-row-form anchors over bare `ShouldContain` substrings and add an anti-corruption assertion that rejects leaked tool-call markup.

The trigger was the Story 18.7 adversarial review. The published `docs/dev/client-mockability.md` ended with leaked `</content>` and `</invoke>` tags, but its guard suite still passed because it asserted only that required phrases were present. The review removed the corruption and added a local regression check. The Epic 18 retrospective then identified the broader maturation path—bare substring, structure-aware row, source/count tie, anti-corruption check—as a reusable convention. Epic 19 used stronger parsers in places but explicitly left the repository-wide contract-document convention open, and the matching `sprint-status.yaml` retrospective action remains `open`.

Core problem: several published contract-document guards can still pass when a required value survives only in unrelated prose, and most do not reject generation artifacts at all. The repository therefore lacks a consistent executable definition of a healthy contract document.

## 2. Impact Analysis

- **Epic impact:** Epic 18 and Epic 19 remain complete. This closes an existing Epic 18 retrospective action without reopening either epic, changing completed history, or adding an epic.
- **Story impact:** No acceptance criteria or completed story records change. The affected Story 18 contract documents continue to express the same contracts; only their guard quality changes.
- **PRD impact:** None. Product requirements, MVP scope, and readiness accounting are unchanged.
- **Architecture impact:** None. No component, API, data, integration, or technology decision changes. A compact testing convention is added to project context, not to the architecture specification.
- **UX impact:** None. No screen, flow, interaction, or accessibility behavior changes.
- **Technical impact:** Add reusable test-only Markdown/anti-corruption helpers; strengthen six owning guard suites covering seven Story 18 contract documents; add contributor and AI-agent guidance.
- **CI impact:** No workflow or lane changes. The affected tests continue to run in their existing Server, CLI, and Integration test projects.
- **Runtime, deployment, and release impact:** None. No production source, package contract, deployment artifact, or semantic-release surface changes.

Affected contract documents:

1. `docs/dev/public-surface-stability.md`
2. `docs/operations/deployment-configuration.md`
3. `docs/operations/route-surface.md`
4. `docs/dev/ingest-contract.md`
5. `docs/dev/memory-unit-id-stability.md`
6. `docs/dev/client-mockability.md`
7. `docs/dev/eventstore-integration.md`

Adjacent runbook and telemetry documentation tests remain outside this correction. Their parser behavior and acceptance scope are not changed.

## 3. Recommended Approach

Use a **Direct Adjustment**: document the convention, provide assertion-framework-neutral test helpers, migrate the seven contract documents' owning guards to the convention, validate the focused suites, and then close the existing retrospective action.

- **Effort:** Low
- **Risk:** Low
- **Timeline impact:** No product-delivery impact; one bounded documentation/test-hardening implementation pass
- **Rollback:** Not warranted. Reverting Story 18.7's local anti-corruption check would restore the known blind spot, while reverting completed contract work would not address the shared guard weakness.
- **MVP review:** Not applicable. Epic 18 is an operational-readiness track excluded from MVP readiness accounting, and this change does not alter product scope.

This path preserves momentum and makes the review lesson durable without introducing a production dependency or expanding the test lanes.

## 4. Detailed Change Proposals

### 4.1 Contributor and AI-agent testing convention

**Artifacts:** `CONTRIBUTING.md`, `_bmad-output/project-context.md`

OLD:

> No repository-wide contract-document guard convention.

NEW:

> Contract-document guards must assert structure rather than mere vocabulary. When a contract is represented by a Markdown table, tie authoritative values to the complete normalized row or adjacent cells—and use count or uniqueness checks where relevant—instead of a bare `ShouldContain` substring that narrative prose could satisfy. Narrative-only claims should be section-bounded or explicitly justified.
>
> Every published contract-document guard must reject leaked tool-call markup, including `</content>`, `</invoke>`, and `<parameter ...>` markers, preferably through the shared test helper.

`CONTRIBUTING.md` receives the explanatory convention under Tests. `_bmad-output/project-context.md` receives the compact enforceable rule under Testing Rules so future implementation agents inherit it.

Rationale: the convention must be visible both to human contributors and to agents that rely on project context.

### 4.2 Shared anti-corruption guard

**New test-helper artifact:** `tests/Hexalith.Memories.TestHelpers/Documentation/ContractDocumentGuard.cs`

OLD:

- Only `MemoriesClientMockabilityContractTests` rejects leaked tool-call markup.
- Its checks are inline and limited to three known fragments.
- Other Story 18 contract documents can contain generation artifacts while their positive assertions still pass.

NEW:

- Add an assertion-framework-neutral helper that reports leaked opening, closing, or incomplete tool-call tags, including the known `content`, `invoke`, and `parameter` forms plus equivalent `tool_call` forms.
- Keep matching boundary-aware so ordinary Markdown and code spans are not rejected merely for containing a similar word.
- Each owning suite asserts that every contract document it reads has zero reported markers.
- Replace the client suite's bespoke checks with the shared helper.
- Add synthetic clean/corrupt fixture tests so the negative behavior is proven without mutating a published document.

Owning suites:

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/DocumentationCompletenessTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs`

Synthetic helper coverage belongs in a focused Server test artifact such as `tests/Hexalith.Memories.Server.Tests/Documentation/ContractDocumentGuardTests.cs`.

Rationale: centralizing the corruption vocabulary prevents six subtly different implementations while leaving Shouldly assertions in the consuming test projects.

### 4.3 Structure-aware row, heading, and section guards

**New test-helper artifact:** `tests/Hexalith.Memories.TestHelpers/Documentation/MarkdownContractDocument.cs`

The helper normalizes Markdown table cells and extracts sections from exact headings. It is not a general Markdown renderer; it supports only the narrow structural operations required by the guard suites.

OLD:

- Multiple suites search the entire document with bare `ShouldContain` assertions.
- Required vocabulary can move from its contract table or owning section into unrelated prose and still satisfy the test.
- Route-surface row/count ties demonstrate the stronger pattern, but it is not consistently applied.

NEW:

- **Public surface:** assert the complete normalized Server and MCP contract rows, including project name, assembly name, root namespace, PackageId, and Aspire symbol. Preserve runtime/generated-symbol ties and tie the MCP PackageId to its project source rather than leaving it review-only.
- **Deployment configuration:** replace the catch-all canonical-literal loop with normalized rows from the OTLP, Dapr sidecar-port, required-runtime-environment, pub/sub surface, and backend-port tables. Retain authoritative source-side constant/literal checks.
- **Route surface:** require every source-derived API route to appear exactly once as the method/path cell of an API table row. Use exact rows for pub/sub operations, health probes, and experimental handler routes. Preserve the existing source route count and constant ties.
- **EventStore integration:** replace loose heading fragments with exact headings. Use normalized rows for environment defaults, CloudEvent envelope fields, delivery outcomes, and alert IDs. Bound the cross-module flow, route, threat-model, and worked-example narrative checks to their owning sections.
- **Memory-unit stability:** use exact rows for the opaque/not-source-derived/not-ULID contract. Bound lifetime, failure-mode, lookup, token, and decision-label prose to the corresponding numbered sections. Apply the same section-bounded approach to the table-free `ingest-contract.md` guarantees.
- **Client mockability:** because the document has no tables, bind D9, seam, breaking-change, BaseAddress, and companion-link claims to their exact numbered sections instead of searching the entire document.
- Preserve bare substring assertions only for genuinely narrative claims inside an already bounded section, with a comment explaining why no structural anchor exists.
- Retain or add row-count and uniqueness checks wherever an omitted, duplicate, or phantom row could otherwise pass.

Rationale: normalized rows reject prose-satisfied false positives without making harmless Markdown column alignment changes fail tests. Exact section bounds provide the equivalent protection for narrative contracts.

### 4.4 Validation and sprint tracking

**Tracking artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
status: open
```

NEW, only after the implementation evidence is green:

```yaml
status: done  # 2026-07-16: contract-doc convention documented; seven Story 18 documents use shared markup rejection and structure-aware guards; focused Server, CLI, and Integration guard tests passed; see sprint-change-proposal-2026-07-16-contract-doc-drift-guard-hardening.md.
```

Historical Epic 18/19 retrospectives and completed story files are not rewritten. The sprint-status breadcrumb is the only retrospective tracking change.

Required validation:

1. Build `tests/Hexalith.Memories.TestHelpers/Hexalith.Memories.TestHelpers.csproj`.
2. Run focused Server tests for deployment configuration, route surface, EventStore documentation, memory-unit/ingest contracts, and synthetic helper rejection fixtures.
3. Run focused CLI client-mockability contract tests.
4. Run the Docker-free Integration public-surface guard tests.
5. Build `Hexalith.Memories.slnx` in Release.
6. Record exact pass/fail/skip counts before changing the retrospective action to `done`.

## 5. Implementation Handoff

**Scope classification:** Minor  
**Primary recipient:** Developer agent  
**Review recipient:** Test Architect

Developer responsibilities:

1. Add the contributor/project-context convention.
2. Add the assertion-framework-neutral anti-corruption and Markdown-structure helpers.
3. Add synthetic rejection/clean-content fixtures.
4. Migrate the six owning suites without changing their underlying product contracts.
5. Run and record focused validation plus the Release solution build.
6. Mark the existing Epic 18 retrospective action `done` only after evidence is green.

Test Architect responsibilities:

- Review marker coverage and boundary behavior.
- Confirm table assertions are row/cell based rather than prose-satisfied.
- Confirm narrative assertions are heading-bounded or explicitly justified.
- Confirm count/uniqueness protection remains on enumerated route and table surfaces.

Success criteria:

- A documented contract-document guard convention exists in contributor and agent guidance.
- All seven affected Story 18 contract documents reject leaked tool-call markup through the shared helper.
- Table-backed guarantees cannot pass because their vocabulary survives only in unrelated prose.
- Narrative-only guarantees are checked within their owning sections.
- Synthetic corrupt content fails and ordinary Markdown succeeds.
- Focused Server, CLI, and Integration guard tests pass, and the Release solution build succeeds.
- The matching Epic 18 retrospective action is closed with a short evidence breadcrumb.
- No production, PRD, architecture, UX, CI, deployment, package, submodule, or completed-history artifact changes.

## Change Analysis Checklist

- [x] 1.1 Trigger identified as Story 18.7 review H1 and Epic 18 retrospective action 3.
- [x] 1.2 Core problem classified as a technical/process guard limitation discovered during implementation review.
- [x] 1.3 Evidence confirmed in the Story 18.7 artifact, Epic 18 retrospective, Epic 19 retrospective follow-through, current tests, and open sprint-status action.
- [x] 2.1 Epic 18 can remain complete as originally delivered.
- [x] 2.2 No epic scope modification, addition, removal, or redefinition is required.
- [x] 2.3 Remaining planned epics and their dependencies are unaffected.
- [N/A] 2.4 No future epic is invalidated and no new epic is required.
- [N/A] 2.5 No epic resequencing or priority change is required.
- [x] 3.1 PRD and MVP remain achievable without modification.
- [x] 3.2 Architecture has no conflict and requires no edit.
- [N/A] 3.3 UI/UX is unaffected.
- [x] 3.4 Testing strategy, contributor guidance, project context, and sprint tracking require the bounded changes above; CI topology does not.
- [x] 4.1 Direct Adjustment is viable with low effort and low risk.
- [N/A] 4.2 Rollback does not simplify or solve the issue.
- [N/A] 4.3 PRD/MVP review is not required.
- [x] 4.4 Direct Adjustment selected for maintainability, low risk, and zero product-scope impact.
- [x] 5.1 Issue summary includes discovery context and concrete evidence.
- [x] 5.2 Epic and artifact impacts are explicit.
- [x] 5.3 Recommended approach, rationale, trade-offs, effort, and risk are documented.
- [x] 5.4 MVP impact, implementation sequence, and validation dependencies are defined.
- [x] 5.5 Developer and Test Architect responsibilities are defined.
- [x] 6.1 All applicable checklist sections and workflow approval gates are complete.
- [x] 6.2 Proposal is consistent with repository evidence and the four incrementally approved edits.
- [x] 6.3 The user explicitly approved the complete Sprint Change Proposal for implementation on 2026-07-16.
- [N/A] 6.4 No epic or story status changes are required; the existing retrospective action changes only after implementation validation.
- [x] 6.5 Handoff roles, sequence, and success criteria are explicit.
