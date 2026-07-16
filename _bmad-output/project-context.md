---
project_name: 'Hexalith.Memories'
user_name: 'Jerome'
date: '2026-06-23'
sections_completed: ['discovery', 'technology_stack', 'language_rules', 'framework_rules', 'testing_rules', 'code_quality', 'workflow_rules', 'critical_rules']
existing_patterns_found: 18
status: 'complete'
rule_count: 94
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- **.NET 10 / C# 14** - all projects target `net10.0`; SDK is pinned by `global.json` to `10.0.302` with `rollForward=latestFeature`.
- **Central package management is mandatory** - package versions live in `Directory.Packages.props`; project files use versionless `PackageReference` entries.
- **Warnings are build failures** - `Nullable=enable`, `ImplicitUsings=enable`, and `TreatWarningsAsErrors=true` are set at repo root.
- **Dapr 1.18.4 is load-bearing** - workflows, actors, pub/sub, state, service invocation, client APIs, and Dapr AI all use the aligned `1.18.4` package set.
- **Aspire owns local orchestration** - AppHost uses `Aspire.AppHost.Sdk/13.3.3`, `Aspire.Hosting.Testing` `13.4.6`, and `CommunityToolkit.Aspire.Hosting.Dapr` `13.4.0-preview.1.260602-0230`.
- **MCP SDK 1.4.1 backs the agent surface** - `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` must remain aligned.
- **OpenTelemetry is split by package maturity** - core/exporter/hosting/in-memory packages are `1.16.0`; ASP.NET Core instrumentation is `1.15.2`; HTTP/runtime instrumentation is `1.15.1`; StackExchange.Redis instrumentation is `1.15.1-beta.2`.
- **Redis Stack + FalkorDB are the storage backends** - RediSearch, Redis Vector, Redis state/pubsub, and FalkorDB graph storage are wired through AppHost/Dapr.
- **Kreuzberg 4.9.9 handles content extraction** - ingestion activities should use the existing extraction path rather than adding parallel document parsers.
- **System.CommandLine 2.0.9 backs the CLI** - command composition follows the existing recursive/global option and async handler patterns.
- **Web UI uses FrontComposer + Fluent UI Blazor V5** - Fluent UI is pinned to `5.0.0-rc.3-26138.1`; bUnit is `2.8.4-preview`.
- **Tests use xUnit v3 + Shouldly + NSubstitute** - package pins are `xunit.v3` `3.2.2`, `Shouldly` `4.3.0`, `NSubstitute` `5.3.0`, and `Microsoft.NET.Test.Sdk` `18.6.0`.
- **Release packages are explicit** - `tools/release-packages.json` is the source of truth for the nine packable projects.
- **Root-declared submodules are required under `references/`** - follow the submodule policy from `AGENTS.md`; never initialize nested submodules recursively.

## Critical Implementation Rules

### C# Language-Specific Rules

- **Use file-scoped namespaces matching folders** - namespace shape follows `Hexalith.Memories.{Area}` and should not introduce nested namespace blocks without a clear reason.
- **Preserve the ITANEO MIT copyright header** - every hand-written `.cs` file starts with the existing project copyright block.
- **Prefer sealed records/classes** - use `sealed record` for immutable contracts and `sealed class` for services/helpers unless inheritance is intentional.
- **Validate public boundaries explicitly** - use `ArgumentNullException.ThrowIfNull()` and `ArgumentException.ThrowIfNullOrWhiteSpace()` for constructor, command, client, and endpoint inputs.
- **Async APIs carry `CancellationToken`** - public async service/client methods should accept and pass through cancellation unless framework signatures prevent it.
- **Use `ConfigureAwait(false)` in library/client code** - client and backend helper code should avoid capturing context.
- **Keep DTOs contract-focused** - contracts in `Hexalith.Memories.Contracts.V1` are serializable records with stable public shapes; avoid behavior-heavy domain logic there.
- **Treat JSON shape as contract surface** - preserve camelCase wire expectations, source-generation contexts, and `System.Text.Json` attributes when changing contract models.
- **Prefer additive contract changes** - avoid renaming/removing public properties, enum values, or response fields unless the change is intentionally breaking.
- **Keep tenant and case identifiers explicit** - tenant and case IDs must remain visible parameters/properties through workflows, storage, search, CLI, MCP, REST, telemetry, and UI contracts.
- **Use existing typed result/error models** - prefer `ErrorResponse`, workflow result records, status enums, and domain exceptions over ad hoc strings.
- **Keep composition explicit** - add dependencies through existing DI/composition roots and health-check registration patterns; avoid service locators or static global state.

### Framework-Specific Rules

- **Dapr Workflow owns durable orchestration** - ingestion, tenant provisioning/deletion, consistency repair, retries, and compensation belong in workflows/activities, not custom queues or background state machines.
- **Workflow orchestration must be replay-safe** - use `context.CurrentUtcDateTime`, `context.CreateReplaySafeLogger<T>()`, deterministic IDs/status transitions, and activity calls for all side effects.
- **Dapr Actors own per-tenant stateful singletons** - rate limits, tenant configuration, corpus stats, and counters should use tenant-scoped actor IDs rather than static/global caches.
- **Persist actor state before returning observable results** - actor state is durable project state, not an in-memory optimization.
- **Dapr pub/sub is at-least-once and unordered** - handlers must be idempotent and tolerate duplicate, late, and out-of-order events.
- **Tenant isolation is physical, not just filtered** - use tenant-scoped RediSearch indexes, Redis Vector indexes, FalkorDB databases/graphs, actor IDs, authorization filters, and telemetry tags.
- **Graph queries must use builders/parameters** - use `IGraphQueryBuilder` and parameterized values; never concatenate tenant/user input into Cypher/Falkor queries.
- **Search fusion must stay deterministic** - keep normalization/fusion logic pure and testable; graph search remains optional/degradable.
- **Aspire AppHost owns local infrastructure wiring** - keep Dapr component generation, Redis endpoint discovery, sidecar ports/options, secrets, and token propagation in AppHost patterns.
- **MCP tools are agent-facing contracts** - preserve token-budget-aware responses, tenant authorization filtering, structured errors, and evidence packet mapping.
- **CLI commands must use existing output routing** - human/table/json formatting goes through registered formatters and command payloads, not direct ad hoc console writes.
- **Web UI must use FrontComposer + Fluent UI V5** - prefer FrontComposer/Fluent components and Fluent 2 tokens; do not redefine theme or hand-roll raw CSS/HTML when a component exists.
- **Telemetry uses named constants and low-cardinality tags** - use `MemoriesActivitySource`, `MemoriesMeter`, and existing semantic attributes; avoid unbounded metric label values.

### Testing Rules

- **Contract-document guards are structure-aware and anti-corruption checked** - bind table guarantees to normalized exact rows/cells with count or uniqueness ties, bind narrative claims to their exact ATX section, and reject leaked `content`/`invoke`/`parameter`/`tool_call` markup through the shared assertion-neutral test helper; do not let whole-document vocabulary satisfy an authoritative contract.
- **Use xUnit v3 + Shouldly** - write assertions with `ShouldBe`, `ShouldNotBeNull`, `Should.Throw`, and `Should.ThrowAsync`; avoid raw `Assert.*`.
- **Use NSubstitute for mocks** - follow existing substitute setup/verification style for services, workflow contexts, actors, HTTP abstractions, and Dapr-facing collaborators.
- **Test names are descriptive PascalCase** - keep method names behavior-focused, e.g. `RunAsync_InvalidTenantId_ThrowsArgumentException`.
- **Test folders mirror product areas** - place tests under matching feature folders such as `Activities/Ingestion`, `Workflows`, `Cli`, `ClientRest`, `Tenants`, `Mcp`, `Web`, or `Telemetry`.
- **Global Xunit using already exists** - avoid adding `using Xunit;` in test files unless a project intentionally does not import `tests/Directory.Build.props`.
- **Cover success, validation, and failure paths** - workflow/activity changes should include invalid inputs, backend exceptions, duplicate/idempotent cases, cancellation, and compensation where relevant.
- **Workflow tests verify orchestration behavior** - assert activity order, fan-out behavior, custom status, compensation behavior, retry-sensitive behavior, and replay-safe assumptions.
- **Tenant isolation requires attached negative evidence** - any change to tenant/case routing, endpoint filters or auth claims, tenant status, index/key/graph selection, actor IDs, storage/query selectors, MCP authorization/execution, evidence scope display, verifier markers, attribution, or tenant-scoped data movement must name the affected surfaces and attach focused cross-tenant denial or fail-closed test names, command, and result to its story/spec plus completion or review record. Cite Story 20.2 denial-before-dependency and Story 24.3 verifier/tenant-marker evidence when applicable, or link the newer canonical replacement. If proof cannot run, record an accepted blocker with owner, consequence, and reopen trigger. Do not close on happy-path, broad-suite, build-only, or refactor-green evidence alone.
- **CLI tests must verify output formats and exit behavior** - test human/table/json output through formatter/router patterns and command executor behavior.
- **MCP tests must verify tool contracts** - cover validation errors, authorization filters, token-budget handling, structured error mapping, and evidence packet shaping.
- **Web tests use bUnit and FrontComposer testing helpers** - component tests should verify rendered states, accessibility-relevant attributes, restrictive evidence states, and Fluent/FrontComposer conformance where available.
- **Golden/fixture data belongs under test project fixtures** - keep copied fixture content declared in the test `.csproj`; avoid hidden dependencies on local machine files.
- **Integration tests may use Aspire/Testcontainers fixtures** - keep Docker-dependent tests isolated from pure unit tests and make external service assumptions explicit.
- **Benchmarks validate retrieval quality** - preserve NDCG/scoring tests and synthetic corpus conventions when changing fusion, normalization, semantic search, graph search, or ranking behavior.

### Code Quality & Style Rules

- **Build configuration is the style gate** - root `Directory.Build.props` sets warnings as errors; do not silence warnings globally to get a build green.
- **Analyzer suppressions must stay narrow** - project-specific `NoWarn` entries need justification comments and removal conditions, like the scoped Dapr Conversation suppression.
- **Keep package versions centralized** - never add `Version` attributes to `.csproj` package references.
- **Use the repository line-ending policy** - `.gitattributes` is authoritative: Git stores text with LF and materializes CRLF by default, while shell/Bash, Python, YAML, `Dockerfile`, `*.dockerfile`, and `.gitattributes` stay LF; `.editorconfig` mirrors these editor-facing conventions alongside indentation, UTF-8, final-newline, and whitespace rules.
- **Private fields use `_camelCase`** - follow the repo naming rule; interfaces use `I` prefix; async methods end with `Async`.
- **Keep public surface documented** - maintain XML docs on public contracts, services, commands, options, and package-facing APIs.
- **Use explicit composition roots** - register services/extensions in existing composition root patterns; avoid reflection registration unless already established.
- **Keep contracts versioned under `V1`** - new externally visible request/response shapes should respect the existing versioned contracts layout.
- **Prefer small feature-focused files** - keep one primary type per file and place helpers near the feature they support.
- **Preserve project-specific comments** - keep comments that document story IDs, ADR decisions, security gates, version pins, and workaround removal triggers.
- **UI CSS must not recreate Fluent theme primitives** - use Fluent components, parameters, and Fluent 2 tokens; legacy Fluent v4/FAST tokens should be treated as migration debt, not copied into new code.
- **Do not commit generated build artifacts** - ignore `bin/`, `obj/`, `TestResults/`, coverage output, and generated artifacts unless intentionally tracked fixtures.
- **Use existing docs placement** - developer docs belong under `docs/dev`, operations docs under `docs/operations`, governance/security notes under `docs/governance`.

### Development Workflow Rules

- **Use conventional commits** - commit messages must satisfy commitlint/semantic-release; use `feat`, `fix`, `docs`, `test`, `refactor`, `build`, `ci`, or `chore` intentionally.
- **Commit type affects release behavior** - `feat` triggers a minor release, `fix` triggers a patch release, and `BREAKING CHANGE:` triggers a major release.
- **Do not label refactors as features** - internal reshaping without public capability should be `refactor`, not `feat`.
- **`tools/release-packages.json` controls publish scope** - package additions/removals must update this file and related release tests/scripts.
- **Verify before handoff/commit** - run focused tests for small changes; run `dotnet build` and broader tests for shared contracts, workflows, storage, MCP, Web, release, or tenant-isolation changes.
- **Keep one-shot traces out of story accounting** - a bounded one-shot artifact self-identifies with `route: one-shot` and `status: done` and receives no `development_status` row. Use a normal spec for multi-stage non-epic work; register epic-owned work in both `epics.md` and `development_status` before implementation. Generated automation artifacts are supporting evidence only. This convention applies prospectively from 2026-07-16; older one-shot traces retain historical metadata but do not establish precedent.
- **Respect root-declared `references/` submodule policy** - initialize/update only root-declared submodules under `references/` by default; never use recursive submodule updates unless the user explicitly asks for nested submodules.
- **Do not modify submodule contents casually** - shared dependencies such as `references/Hexalith.Commons`, `references/Hexalith.EventStore`, `references/Hexalith.AI.Tools`, `references/Hexalith.Tenants`, and `references/Hexalith.FrontComposer` require explicit intent and separate submodule commits.
- **Keep dependency bumps deliberate** - update `Directory.Packages.props` with comments when versions are pinned for advisories, prerelease dependencies, or cross-package compatibility.
- **CI/release files are product code** - changes to `.github`, `.releaserc.json`, commitlint, NuGet packaging, release scripts, or package maps need tests or dry-run validation where practical.
- **Use feature branches and PRs for publishable work** - avoid direct commits to main/release branches.
- **Story/ADR comments are sticky context** - preserve story IDs, ADR references, deferred-work links, and removal triggers when editing implementation around them.

### Critical Don't-Miss Rules

- **Never weaken tenant isolation** - tenant ID must be validated and carried through API, workflow, actor, storage, graph, search, CLI, MCP, telemetry, and UI paths; filters alone are not a substitute for physical isolation.
- **Never hand-roll durable orchestration** - use Dapr Workflow for resumable multi-step operations with retries, compensation, and persisted state.
- **Never make workflow logic nondeterministic** - no random IDs, wall-clock time, network calls, direct I/O, or hidden mutable state inside workflow orchestration code; move side effects to activities.
- **Never assume Dapr events are unique or ordered** - event ingestion, dedup, case counters, and graph updates must be duplicate-safe and late-event-safe.
- **Never concatenate tenant/user input into graph queries** - use `IGraphQueryBuilder` and parameterized/builder-based query paths.
- **Never break contract JSON shape casually** - contract changes can affect CLI, MCP, REST, Web, tests, package consumers, and semantic-release impact.
- **Never bypass structured errors** - external-facing failures should map to `ErrorResponse`, actionable CLI guidance, MCP structured errors, or evidence packet restrictive/degraded states.
- **Never expose secrets** - redact tokens and provider credentials in CLI output, MCP responses, telemetry, logs, UI rendering, and test snapshots.
- **Never hide or over-close the access-telemetry retention residual** - keep `20.5-A41-ACCESS-TELEMETRY-RETENTION` carried forward and its sprint action open until bounded retention/TTL is implemented and validated or an explicit accepted-debt disposition records a named approver/owner, scope, rationale, risk/consequence, compensating controls, and a time-bounded review/expiry date or measurable reopen trigger. Until then, describe A41 as partially closed.
- **Never turn a degraded backend into total service failure unless required** - search, evidence, MCP, and health behavior should preserve graceful degradation where the architecture allows it.
- **Never bypass existing formatter/router paths** - CLI output must remain format-selectable and testable.
- **Never copy legacy Fluent tokens into new UI** - use FrontComposer, Fluent UI V5 components, and Fluent 2 tokens; track legacy token cleanup as migration debt.
- **Never add package versions to `.csproj` files** - use `Directory.Packages.props`.
- **Never use recursive submodule commands by default** - initialize/update only root-declared `references/` submodules unless nested submodules are explicitly requested.
- **Never skip focused tests on tenant, workflow, search, auth, serialization, MCP, Web evidence states, release packaging, or telemetry changes** - these areas carry the highest regression risk.

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing any code.
- Follow ALL rules exactly as documented.
- When in doubt, prefer the more restrictive option.
- Update this file if new durable patterns emerge.

**For Humans:**

- Keep this file lean and focused on agent needs.
- Update it when technology stack or architecture decisions change.
- Review periodically for outdated rules.
- Remove rules that become obvious over time.

Last Updated: 2026-07-16
