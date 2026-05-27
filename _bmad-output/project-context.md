---
project_name: 'Hexalith.Memories'
user_name: 'Jerome'
date: '2026-05-10'
sections_completed: ['discovery', 'technology_stack', 'language_rules', 'framework_rules', 'testing_rules', 'code_quality', 'workflow_rules', 'critical_rules']
existing_patterns_found: 14
status: 'complete'
rule_count: 82
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- **.NET 10 / C# 14** - all projects target `net10.0`; SDK pinned by `global.json` to `10.0.300` with `rollForward=latestFeature`.
- **Central package management is mandatory** - package versions live in `Directory.Packages.props`; project files use versionless `PackageReference` entries.
- **Warnings are build failures** - `Nullable=enable`, `ImplicitUsings=enable`, and `TreatWarningsAsErrors=true` are set at repo root.
- **DAPR 1.17.6 is load-bearing** - use DAPR Workflow for durable multi-step orchestration, Actors for per-tenant state, service invocation for internal calls, and DAPR AI/Conversation only where already scoped.
- **Aspire 13.1.3 hosts local orchestration** - AppHost owns DAPR component generation, Redis wiring, and local sidecar options.
- **Redis Stack + FalkorDB are the storage backends** - RediSearch for syntactic search, Redis Vector for semantic search, FalkorDB for graph traversal.
- **OpenTelemetry 1.15.x versions move together** - keep core/exporter/instrumentation versions aligned with the documented comments in `Directory.Packages.props`.
- **MCP SDK 1.2.0 backs the agent tool surface** - MCP code should preserve token-aware, structured tool responses.
- **System.CommandLine is intentionally prerelease** - keep CLI command composition aligned with existing recursive/global option patterns.
- **Submodules are required root dependencies** - `Hexalith.Commons`, `Hexalith.EventStore`, and `Hexalith.AI.Tools` are root-level submodules; do not initialize/update nested submodules unless explicitly requested.

## Critical Implementation Rules

### C# Language-Specific Rules

- **Use file-scoped namespaces matching folders** - namespace shape follows `Hexalith.Memories.{Area}` and should not introduce unnecessary nested namespace blocks.
- **Preserve the ITANEO MIT copyright header** - every hand-written `.cs` file starts with the existing project copyright block.
- **Prefer sealed records/classes** - use `sealed record` for immutable contracts and `sealed class` for services/helpers unless inheritance is intentional.
- **Validate public boundaries explicitly** - use `ArgumentNullException.ThrowIfNull()` and `ArgumentException.ThrowIfNullOrWhiteSpace()` for constructor, method, command, and client inputs.
- **Async APIs carry `CancellationToken`** - public async service/client methods should accept and pass through cancellation unless framework signatures prevent it.
- **Use `ConfigureAwait(false)` in library/client code** - existing client and backend helper code generally avoids capturing context.
- **Keep DTOs contract-focused** - contracts in `Hexalith.Memories.Contracts.V1` are serializable records with stable public shapes; avoid behavior-heavy domain logic there.
- **JSON shape is part of the contract** - preserve camelCase API expectations and existing `System.Text.Json` attributes/source-generation patterns when changing contract models.
- **Use source-compatible additive changes for contracts** - avoid renaming/removing public properties or enum values unless the change is intentionally breaking.
- **Do not hide tenant/case identifiers** - tenant and case IDs must remain explicit parameters/properties through workflows, storage, search, CLI, MCP, and telemetry paths.
- **Prefer strongly typed result/error models** - use existing `ErrorResponse`, workflow result records, status enums, and domain exceptions instead of ad hoc strings.
- **Keep `Program.cs` composition explicit** - add dependencies through the existing DI and health-check registration style; avoid service locator patterns or static global state.

### Framework-Specific Rules

- **DAPR Workflow owns orchestration** - multi-step ingestion, tenant provisioning/deletion, consistency repair, retry, and compensation logic belongs in workflows/activities, not custom queues or background state machines.
- **Workflow code must be replay-safe** - use `context.CurrentUtcDateTime`, `context.CreateReplaySafeLogger<T>()`, deterministic IDs/status transitions, and activity calls for side effects.
- **DAPR Actors own per-tenant stateful singletons** - rate limits, tenant configuration, corpus stats, and counters should use actor IDs scoped by tenant; do not replace them with static/global caches.
- **Persist actor state before returning observable results** - actor state is durable project state, not an in-memory optimization.
- **DAPR pub/sub is at-least-once and unordered** - handlers must be idempotent and tolerate duplicates/out-of-order events.
- **Tenant isolation is physical, not just filtered** - use tenant-scoped RediSearch indexes, Redis Vector indexes, FalkorDB databases/graphs, actor IDs, and telemetry tags.
- **Graph queries must be built safely** - use the graph query builder/executor patterns and parameterized values; do not concatenate tenant/user input into Cypher/Falkor queries.
- **Search fusion must stay deterministic** - keep normalization/fusion logic pure and testable; graph search remains optional/degradable.
- **Aspire AppHost owns local infrastructure wiring** - keep DAPR component generation, Redis endpoint discovery, sidecar options, and local token propagation in AppHost patterns.
- **MCP tools are agent-facing contracts** - preserve token-budget-aware responses, structured errors, and tenant authorization filters; do not expose operational CLI-only capabilities casually.
- **CLI commands must use existing output routing** - human/table/json formatting goes through registered formatters and command payloads, not direct ad hoc console writes.
- **Telemetry uses named constants and low-cardinality tags** - use `MemoriesActivitySource`, `MemoriesMeter`, and existing semantic attribute processors; avoid high-cardinality unbounded metric labels.

### Testing Rules

- **Use xUnit + Shouldly** - write assertions with `ShouldBe`, `ShouldNotBeNull`, `Should.Throw`, and `Should.ThrowAsync`; avoid raw `Assert.*`.
- **Use NSubstitute for mocks** - follow existing substitute setup/verification style for services, workflow contexts, actors, and HTTP abstractions.
- **Test names are descriptive PascalCase** - keep method names behavior-focused, e.g. `RunAsync_InvalidTenantId_ThrowsArgumentException`.
- **Test folders mirror product areas** - place tests under matching project/feature folders such as `Activities/Ingestion`, `Workflows`, `Cli`, `ClientRest`, `Tenants`, or `Telemetry`.
- **Global Xunit using already exists** - avoid adding `using Xunit;` in test files unless a local project lacks the shared `tests/Directory.Build.props` import.
- **Cover success, validation, and failure paths** - workflow/activity tests should include invalid inputs, backend exceptions, duplicate/idempotent cases, and cancellation where relevant.
- **Workflow tests verify orchestration behavior** - assert activity order, compensation behavior, custom status, retry-sensitive behavior, and replay-safe assumptions.
- **Tenant isolation needs integration coverage** - changes to tenant routing, index names, graph database selection, actor IDs, or authorization require tenant-crossing negative tests.
- **CLI tests must verify formats and exit behavior** - test human/table/json output via existing formatter/router patterns and command executor behavior.
- **Golden/fixture data belongs under test project fixtures** - keep copied fixture content declared in the test `.csproj`; avoid hidden dependencies on local machine files.
- **Integration tests may use Testcontainers/Aspire fixtures** - keep Docker-dependent tests isolated from pure unit tests and make external service assumptions explicit.
- **Benchmarks validate retrieval quality** - preserve NDCG/scoring tests and synthetic corpus conventions when changing fusion, normalization, or retrieval ranking.

### Code Quality & Style Rules

- **Build configuration is the style gate** - root `Directory.Build.props` sets warnings as errors; do not silence warnings globally to get a build green.
- **Analyzer suppressions must stay narrow** - project-specific `NoWarn` entries need justification comments like the scoped DAPR Conversation suppression in Server.
- **Keep package versions centralized** - never add `Version` attributes to `.csproj` package references.
- **Use `.editorconfig` conventions** - 4-space C# indentation, 2-space XML/JSON/YAML indentation, CRLF, UTF-8, final newline, trimmed whitespace outside Markdown.
- **Private fields use `_camelCase`** - follow the repo naming rule; interfaces use `I` prefix; async methods end with `Async`.
- **Use explicit composition roots** - register services/extensions in existing composition root patterns; avoid reflection registration unless already established.
- **Do not commit generated build artifacts** - ignore `bin/`, `obj/`, `TestResults/`, and generated coverage/output files unless intentionally tracked fixtures.
- **Documentation comments are expected on public surface** - keep XML docs on public contracts, services, commands, and options that are part of external or package-facing APIs.
- **Comments should explain project-specific constraints** - preserve comments that document ADR/story rationale, version pins, security gates, and workaround removal conditions.
- **Keep contracts versioned under `V1`** - new externally visible request/response shapes should respect the existing versioned contracts layout.
- **Use existing README/docs placement** - developer docs belong under `docs/dev`, operations docs under `docs/operations`, governance/security notes under `docs/governance`.
- **Prefer small feature-focused files** - keep one primary type per file and place helpers near the feature they support.

### Development Workflow Rules

- **Use conventional commits** - commit messages must satisfy commitlint/semantic-release; use `feat`, `fix`, `docs`, `test`, `refactor`, `build`, `ci`, or `chore` intentionally.
- **Commit type affects release behavior** - `feat` triggers a minor release, `fix` triggers a patch release, and `BREAKING CHANGE:` triggers a major release.
- **Do not label refactors as features** - internal reshaping without public capability should be `refactor`, not `feat`.
- **Verify before handoff/commit** - run the narrowest relevant tests for small changes; run `dotnet build` and broader tests for shared contracts, workflows, storage, or release-impacting changes.
- **Respect root-level submodule policy** - initialize/update only root-level submodules by default; never use recursive submodule updates unless the user explicitly asks for nested submodules.
- **Do not modify submodule contents casually** - `Hexalith.Commons`, `Hexalith.EventStore`, and `Hexalith.AI.Tools` are shared dependencies; changes there require explicit intent and separate commits in the submodule.
- **Keep dependency bumps deliberate** - update `Directory.Packages.props` with comments when versions are pinned for advisories, prerelease dependencies, or cross-package compatibility.
- **CI/release files are part of the product** - changes to `.github`, `.releaserc.json`, commitlint, NuGet packaging, or release scripts need tests or dry-run validation where practical.
- **Use feature branches and PRs for publishable work** - avoid direct commits to main/release branches.
- **Story/ADR comments are sticky context** - preserve story IDs, ADR references, deferred-work links, and removal triggers when editing implementation around them.

### Critical Don't-Miss Rules

- **Never weaken tenant isolation** - tenant ID must be validated and carried through API, workflow, actor, storage, graph, search, CLI/MCP, and telemetry paths; filters alone are not a substitute for physical isolation.
- **Never hand-roll durable orchestration** - use DAPR Workflow for resumable multi-step operations with retries and compensation.
- **Never use recursive submodule commands by default** - root submodules only unless nested submodules are explicitly requested.
- **Never add package versions to `.csproj` files** - use `Directory.Packages.props`.
- **Never make workflow logic nondeterministic** - no random IDs, wall-clock time, network calls, or direct I/O inside workflow orchestration code; move side effects to activities.
- **Never assume DAPR events are unique or ordered** - every event ingestion and graph update must be duplicate-safe and late-event-safe.
- **Never concatenate user or tenant input into graph queries** - use parameterized/builder-based query paths.
- **Never expose secrets in CLI output, telemetry, logs, or test snapshots** - preserve token redaction behavior and secure transport checks.
- **Never turn a degraded backend into total service failure unless the feature requires it** - search and health behavior should reflect graceful degradation where the architecture allows it.
- **Never bypass structured errors** - external-facing failures should map to `ErrorResponse`, actionable CLI guidance, or MCP structured errors.
- **Never break JSON contract shape casually** - contract changes can affect CLI, MCP, REST, tests, package consumers, and semantic-release impact.
- **Never silence warnings globally** - warnings are errors by design; scoped suppressions need a documented reason and removal condition.
- **Never bypass existing formatter/router paths** - CLI output must remain format-selectable and testable.
- **Never skip focused tests on tenant, workflow, search, auth, serialization, or release changes** - those areas carry the highest regression risk.

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing any code.
- Follow all rules exactly as documented.
- When in doubt, prefer the more restrictive option.
- Update this file if new durable patterns emerge.

**For Humans:**

- Keep this file lean and focused on agent needs.
- Update it when technology stack or architecture decisions change.
- Review periodically for outdated rules.
- Remove rules that become obvious over time.

Last Updated: 2026-05-10
