# Story 15.6: Scaffolding Hardening Sweep

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want the 15 patch findings from the 2026-05-16 fresh re-review of Story 1.1's scaffolding to be triaged and applied with proper file scope,
so that the AppHost boot orchestration, ServiceDefaults health/telemetry surface, and DAPR component templates are hardened without retroactively flipping the released Story 1.1 to `in-progress`.

The re-review uncovered issues spanning code originally added by Stories 1.1, 5.4, 6.1, 6.4, 7.5, 8.4, 8.5, 9.1, 9.2, and 10.1 that all live in the AppHost/ServiceDefaults/deploy-yaml surface. Bundling them under Story 1.1 would breach File Scope on a released story; this story owns them as a single hardening pass with its own scope.

## Acceptance Criteria

1. **Given** the 15 patch findings recorded in `_bmad-output/implementation-artifacts/1-1-project-scaffolding-and-single-command-boot.md` under `### Review Findings (Re-Review 2026-05-16)`, **when** this story runs, **then** each finding is either applied, downgraded to defer with rationale captured in `deferred-work.md`, or explicitly dismissed in this story's Review Findings section. No finding is silently dropped.

2. **Given** the AppHost generates DAPR component YAML at runtime, **when** the implementation lands, **then** the sidecar start-event awaits the `OnResourceReady` rewrite (not just the Redis PING) and concurrent AppHost runs use a per-invocation temp directory (e.g., PID-suffixed) so two `dotnet run` invocations cannot corrupt each other's `statestore.yaml`/`pubsub.yaml`.

3. **Given** `Directory.Build.props` is the build gate for missing submodules, **when** the implementation lands, **then** the `CheckSubmodules` MSBuild target validates every entry in `.gitmodules` (Hexalith.Commons, Hexalith.EventStore, Hexalith.AI.Tools, Hexalith.Tenants, Hexalith.FrontComposer), not only the original two from Story 1.1.

4. **Given** `ServiceDefaults.AddDefaultHealthChecks` is the canonical health-check entry point, **when** the implementation lands, **then** `/ready` returns 503 when Redis is unreachable (a `ready`-tagged check is registered by default) and the AppHost `memories-server` resource waits for the `secretstore` and `llm` DAPR components in addition to `redis` and `falkordb`.

5. **Given** the production `deploy/dapr/components/statestore.yaml`, `secretstore.yaml`, and `conversation-llm.yaml` templates ship to Kubernetes deployments, **when** the implementation lands, **then** the statestore template uses env-var interpolation for `redisPassword` matching the existing `pubsub.yaml` pattern, the secretstore template uses an absolute path with a documented volume mount, and the conversation component uses the DAPR Conversation API's documented `cacheTTL` metadata key (verified against the DAPR 1.17 schema).

6. **Given** Story 1.1's spec at `_bmad-output/implementation-artifacts/1-1-project-scaffolding-and-single-command-boot.md` literally calls for `AppPort=5000` in `WithDaprSidecar()` but the current code intentionally omits it for Aspire-Testing port randomization, **when** this story runs, **then** Story 1.1's spec receives a Scope-Override block in its File Scope section (precedent: Stories 15.2/15.4) recording the testability decision and amends the Completion Notes accordingly. No code change to AppPort handling is required by this story.

7. **Given** safety regressions are easy to introduce in boot orchestration, **when** the implementation lands, **then** at least one targeted test or fixture exercises the new submodule guard (an unknown submodule entry causes a clear MSBuild error), the new ready-tagged Redis health check (`/ready` returns 503 when the keyed multiplexer is unreachable), and the component-file rewrite ordering (sidecar does not start before the rewrite completes).

## Tasks / Subtasks

- [x] Task 0 — Establish the implementation baseline (AC: 1)
  - [x] Read `_bmad-output/implementation-artifacts/1-1-project-scaffolding-and-single-command-boot.md` Review Findings (Re-Review 2026-05-16) section end-to-end.
  - [x] Read the seven `1.1-RR*` entries in `_bmad-output/implementation-artifacts/deferred-work.md` to understand which findings were already accepted/deferred and do NOT need code change.
  - [x] Build a small inventory mapping each patch finding to (file:line, change shape, test impact). Record in this story's Dev Agent Record.

- [x] Task 1 — AppHost component-file ordering and isolation (AC: 1, 2, 7)
  - [x] In `src/Hexalith.Memories.AppHost/Program.cs`, introduce a `TaskCompletionSource` set by `OnResourceReady` after `WriteDaprRedisComponentFiles` completes; have the `BeforeResourceStartedEvent` handler await it (in addition to `WaitForRedisPingAsync`) before allowing any of the four matched sidecar resources to start.
  - [x] Replace `Path.GetTempPath()/hexalith-memories-dapr/{daprAppId}/` with a per-invocation directory that includes the AppHost process ID (e.g., `{daprAppId}-{Process.GetCurrentProcess().Id}`); ensure cleanup on AppHost shutdown via a `Disposed` callback or `AppDomain.CurrentDomain.ProcessExit`.
  - [x] Add YAML-safe escaping for `secretsFile` path interpolation: handle `"`, `\n`, and other YAML-special characters or switch to a YAML serializer / single-quoted-scalar form.
  - [x] Fix `WaitForRedisPingAsync` to loop on `ReadAsync` until `\r\n` is observed (use `ReadExactlyAsync` or accumulate until the terminator) instead of relying on a single read of `>= 5` bytes.
  - [x] Add `.WaitFor(secretStore).WaitFor(conversationLlm)` to the `memories-server` project resource.
  - [x] On Linux/macOS, call `File.SetUnixFileMode(secretsFile, OwnerRead | OwnerWrite)` after creating `secrets.json` so the file is not world-readable.
  - [x] Replace `Contains("appendonly yes", OrdinalIgnoreCase)` in `ResolveRedisConfigPath` with a line-by-line parser that skips lines beginning with `#`.

- [x] Task 2 — Submodule guard expansion (AC: 1, 3, 7)
  - [x] Extend `Directory.Build.props` `CheckSubmodules` MSBuild target to validate all five entries currently in `.gitmodules` (Hexalith.Commons, Hexalith.EventStore, Hexalith.AI.Tools, Hexalith.Tenants, Hexalith.FrontComposer). Prefer an `ItemGroup`-driven iteration so future additions to `.gitmodules` only require one line in the target rather than another `<Error>` element.
  - [x] Verify the new errors fire by temporarily renaming a submodule `.git` directory and running `dotnet build`; capture the error text in this story's Dev Agent Record.

- [x] Task 3 — ServiceDefaults `/ready` becomes a real readiness gate (AC: 1, 4, 7)
  - [x] Extend `AddDefaultHealthChecks` in `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` to register a `redis-ping` check tagged `ready` that resolves the keyed `IConnectionMultiplexer` (`Extensions.RedisConnectionKey`) and pings it. The check must return `Unhealthy` when the keyed service is not registered (callers without Redis can opt out via a bool parameter mirroring `configureRedisInstrumentation`).
  - [x] Add an XML doc remark and an ADR-style comment explaining why the default check exists (closes the gap surfaced by the Story 1.1 re-review).
  - [x] In `ConfigureOpenTelemetry` exporter wiring, emit a `Warning` log when `Environment.EnvironmentName == "Production"` and `OTEL_EXPORTER_OTLP_ENDPOINT` is empty.

- [x] Task 4 — Production DAPR component templates (AC: 1, 5)
  - [x] In `deploy/dapr/components/statestore.yaml`, replace the hardcoded `redisPassword: ""` with an env-var interpolation pattern matching the existing `pubsub.yaml` (`${STATESTORE_REDIS_PASSWORD:-}` or equivalent). Add a 2-line file header clarifying that this YAML is a production deployment template and local dev uses AppHost-generated YAML.
  - [x] In `deploy/dapr/components/secretstore.yaml`, replace `./secrets.json` with an absolute path (suggested: `/etc/dapr/secrets/secrets.json`) and add a comment documenting the expected volume mount.
  - [x] In `deploy/dapr/components/conversation-llm.yaml`, verify the metadata key name against the DAPR 1.17 Conversation API schema. If `responseCacheTTL` is wrong, rename to the documented key (`cacheTTL`). Cite the DAPR docs URL in the file.

- [x] Task 5 — Story 1.1 Scope-Override for AppPort omission (AC: 1, 6)
  - [x] Amend `_bmad-output/implementation-artifacts/1-1-project-scaffolding-and-single-command-boot.md` File Scope section with a `### Scope-Override (added 2026-05-XX)` block (precedent: Stories 15.2/15.4) recording that `WithDaprSidecar()` intentionally omits `AppPort=5000` so Aspire Testing can auto-detect the randomized project port. Cite the in-code comment at `src/Hexalith.Memories.AppHost/Program.cs:103-115`.
  - [x] Update Story 1.1 Completion Notes to reference the Scope-Override block and clarify that AC #1's "DAPR sidecar (app port 5000, ...)" requirement is satisfied operationally (sidecar reaches the app on the project-allocated port) even though the literal `AppPort` config is auto-detected rather than pinned.
  - [x] Check the relevant unresolved Decision items in the Story 1.1 Re-Review Findings section as resolved by this story.

- [x] Task 6 — Regression coverage (AC: 7)
  - [x] Add a unit test asserting the new ready-tagged Redis health check returns Unhealthy when the keyed multiplexer is absent and Healthy when a stub connects (mirror existing `Tier-2` test patterns in `tests/Hexalith.Memories.Server.Tests/Telemetry`).
  - [x] Add an AppHost integration test (or extend the existing Aspire-Testing fixture) asserting the sidecar does not start until the component-file rewrite has produced a non-`127.0.0.1` host in `statestore.yaml`.
  - [x] Add (or extend) the existing submodule-detection test/fixture so renaming a tracked submodule `.git` directory produces the expected MSBuild error referencing the missing module by name.

- [x] Task 7 — Update story-1.1 Re-Review Findings checkmarks (AC: 1)
  - [x] For each patch finding addressed by Tasks 1-5, check the `- [ ]` to `- [x]` in `_bmad-output/implementation-artifacts/1-1-project-scaffolding-and-single-command-boot.md` Re-Review section and append a one-line note pointing to this story (e.g., "applied by Story 15.6").
  - [x] For any finding downgraded to defer during implementation, move it to `deferred-work.md` under the existing `## Deferred from: code review of 1-1-project-scaffolding-and-single-command-boot (2026-05-16)` heading with structured fields (new `1.1-RR*` ID).

- [x] Task 8 — Validation (AC: 1, 7)
  - [x] `dotnet build` over the full solution — zero warnings, zero errors.
  - [x] `dotnet test` for the focused slice: `tests/Hexalith.Memories.Server.Tests`, `tests/Hexalith.Memories.IntegrationTests` (AppHost-touching subset only), `tests/Hexalith.Memories.Contracts.Tests`. Record pass/fail counts.
  - [x] Run `dotnet run --project src/Hexalith.Memories.AppHost` from a clean checkout, confirm the dashboard reports Redis + FalkorDB + memories-server + memories-mcp + DAPR sidecars healthy, and that `/ready` returns 200 when Redis is up and 503 when Redis is stopped.
  - [x] Record commands, outputs, and any deviations in the Dev Agent Record.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.AppHost/Program.cs` — UPDATE. Component-file ordering, per-invocation temp dir, YAML escape, PING fix, WaitFor expansion, Unix file mode, `appendonly yes` line parser.
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` — UPDATE. Default ready-tagged Redis ping, Production OTLP-endpoint warning.
- `Directory.Build.props` — UPDATE. Submodule guard expanded to all `.gitmodules` entries.
- `deploy/dapr/components/statestore.yaml` — UPDATE. Env-var interpolation for `redisPassword`, production-template header.
- `deploy/dapr/components/secretstore.yaml` — UPDATE. Absolute path + volume-mount documentation comment.
- `deploy/dapr/components/conversation-llm.yaml` — UPDATE. Verify and correct DAPR Conversation metadata key name.
- `_bmad-output/implementation-artifacts/1-1-project-scaffolding-and-single-command-boot.md` — UPDATE. Scope-Override block, Completion Notes amendment, Re-Review checkmark updates (Tasks 5 and 7).
- `_bmad-output/implementation-artifacts/deferred-work.md` — UPDATE only if a finding is downgraded to defer during implementation (Task 7).
- `_bmad-output/implementation-artifacts/15-6-scaffolding-hardening-sweep.md` — UPDATE. This story file.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — UPDATE only through BMad workflow/status transitions.
- `tests/Hexalith.Memories.Server.Tests/**` — UPDATE for the ready-tagged health-check regression test (Task 6).
- `tests/Hexalith.Memories.IntegrationTests/**` — UPDATE for the AppHost component-file ordering regression test (Task 6).
- `src/Hexalith.Memories.Server/Ingestion/ContentExtractionClient.cs` — UPDATE (post-Docker validation fix requested by user). Preserve raw Markdown content after the restored package set exposed Kreuzberg normalization in the broad server test pass.

Possible files only if analysis proves they are necessary:

- `tests/Hexalith.Memories.TestHelpers/**` — UPDATE only if a shared fixture is required by Task 6 tests; otherwise inline the helper in the test class.
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` — UPDATE only if a new dependency is required for the per-invocation temp dir cleanup (unlikely).
- `src/Hexalith.Memories.Redis/FalkorDbCompatibilityExtensions.cs` — ADD (post-Docker validation fix requested by user). Preserve the pre-1.0.6 graph-id `QueryAsync` call shape used by server code after restored packages resolve `NFalkorDB` 1.0.6.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/15-1-release-edge-case-preflight-hardening.md` through `15-5-deferred-register-triage-sweep.md` (precedent for Scope-Override pattern)
- `.gitmodules`
- `Hexalith.Memories.slnx`
- `Directory.Packages.props`
- `CONTRIBUTING.md`
- `docs/operations/embedding-providers.md`

Forbidden by default:

- `.github/**`
- `src/**` except the AppHost and ServiceDefaults paths listed above
- `tools/**`
- `Directory.Packages.props`
- `NuGet.config`
- `Hexalith.AI.Tools/**`
- `Hexalith.Commons/**`
- `Hexalith.EventStore/**`
- `Hexalith.Tenants/**`
- `Hexalith.FrontComposer/**`
- Any submodule pointer change

## Definition of Done

1. Every one of the 15 patch findings is either checked off in the Story 1.1 Re-Review section or moved to `deferred-work.md` with a structured `1.1-RR*` entry and rationale. No silent drops.
2. `dotnet build` zero warnings, zero errors. Focused test slice all green; counts recorded in Dev Agent Record.
3. `dotnet run --project src/Hexalith.Memories.AppHost` boots a fully healthy dashboard from a clean checkout; `/ready` returns 503 when Redis is intentionally stopped.
4. Story 1.1's Scope-Override block is in place and Completion Notes reference it; the AppPort omission is no longer a literal spec deviation.
5. Production DAPR YAMLs no longer hardcode an empty Redis password or a CWD-relative secrets path; `conversation-llm.yaml` uses the documented DAPR Conversation metadata key.
6. New regression tests exercise the submodule guard, the ready-tagged Redis health check, and the component-file ordering invariant.

## Dev Notes

### Source of Truth

- **Re-Review findings inventory:** `_bmad-output/implementation-artifacts/1-1-project-scaffolding-and-single-command-boot.md` → `### Review Findings (Re-Review 2026-05-16)`.
- **Deferred entries that intentionally remain accepted:** `_bmad-output/implementation-artifacts/deferred-work.md` → `## Deferred from: code review of 1-1-project-scaffolding-and-single-command-boot (2026-05-16)` (`1.1-RR1` through `1.1-RR7`).

### Implementation Order Suggestion

1. Task 0 (inventory) → Task 2 (submodule guard — trivial, builds confidence).
2. Task 3 (ServiceDefaults — modest surface, unlocks AC #4).
3. Task 1 (AppHost — largest surface, depends on Task 3's keyed multiplexer pattern for the await-ordering test in Task 6).
4. Task 4 (deploy YAMLs — small, deploy-side only).
5. Task 5 (Story 1.1 spec amendment — paperwork; close out Decision items).
6. Task 6 (tests) and Task 7 (checkmark updates) — finish in parallel.
7. Task 8 (validation) — gates close-out.

### Cross-References

- Story 1.1 — `_bmad-output/implementation-artifacts/1-1-project-scaffolding-and-single-command-boot.md`. Original scaffolding deliverables; remains `done`.
- Scope-Override precedent — Story 15.2 (`_bmad-output/implementation-artifacts/15-2-provider-model-dimension-registry.md`), Story 15.4 (`_bmad-output/implementation-artifacts/15-4-token-endpoint-transport-policy.md`), Story 15.5 (`_bmad-output/implementation-artifacts/15-5-deferred-register-triage-sweep.md`).
- DAPR Conversation API metadata schema — verify against the DAPR 1.17 release docs before renaming the `responseCacheTTL` key.

### Anti-Patterns to Avoid

- **DO NOT** flip Story 1.1 to `in-progress`. Story 1.1 is `done`; this story owns the hardening pass.
- **DO NOT** touch submodule pointers. Any submodule advancement is a separate, explicit decision and requires a Scope-Override block.
- **DO NOT** apply patches to code introduced by later stories (5.4 tokens, 6.4 redis durability, 8.4/8.5 OTEL telemetry, 9.x pub/sub, 10.1 MCP) without verifying the change is in this story's File Scope. If a fix would require touching a forbidden path, defer the finding rather than breaching scope.
- **DO NOT** add a new global mutation pattern. The deferred `1.1-RR1` process-env mutation is intentional and accepted; do not extend that pattern.
- **DO NOT** silence the OTLP-endpoint warning behind a feature flag. The warning is the diagnostic surface that closes the silent-loss gap.

## Change Log

- 2026-05-16: Story drafted from the 2026-05-16 Re-Review of Story 1.1. Captures 13 direct patches plus 2 resolved decisions (D1 → spec Scope-Override; D2 → default ready-tagged Redis ping in ServiceDefaults).
- 2026-05-18: Applied scaffolding hardening sweep; Docker/AppHost runtime checks pass, `/ready` gates Redis reachability, and story is ready for review.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Task 0 inventory: 15 active re-review findings were triaged. Applied by code/docs: D1 AppPort Scope-Override, D2 default Redis `/ready`, submodule guard expansion, AppHost rewrite ordering, per-process DAPR temp directory, YAML scalar escaping, Unix `secrets.json` mode, Redis PING framing, Redis AOF config parser, statestore password env interpolation, secretstore absolute path, production OTLP warning, server waits for `secretstore`/`llm`, statestore production-template header. Explicitly dismissed: `conversation-llm.yaml` cache key rename because DAPR 1.17 docs use `responseCacheTTL`; `cacheTTL` appears only as a legacy alias in component metadata parsing.
- Accepted deferred items read and left unchanged: `1.1-RR1` through `1.1-RR7` in `deferred-work.md`.
- Submodule guard proof: temporarily moved `Hexalith.Tenants/.git`, ran `dotnet build .\src\Hexalith.Memories.ServiceDefaults\Hexalith.Memories.ServiceDefaults.csproj --no-restore`, restored the `.git` marker in `finally`. Expected error captured: `Git submodule 'Hexalith.Tenants' is missing. Run: git submodule update --init Hexalith.Tenants`.
- DAPR docs verification: `https://docs.dapr.io/reference/components-reference/supported-conversation/openai/` documents `responseCacheTTL`; no production-template rename to `cacheTTL` was made.
- Validation: `dotnet build .\Hexalith.Memories.slnx --no-restore` passed with 0 warnings, 0 errors.
- Validation: `dotnet test .\tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --no-build` passed 1822/1822.
- Validation: `dotnet test .\tests\Hexalith.Memories.Contracts.Tests\Hexalith.Memories.Contracts.Tests.csproj --no-build` passed 470/470.
- Validation: `dotnet test .\tests\Hexalith.Memories.IntegrationTests\Hexalith.Memories.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~AppHostComponentFileOrderingTests"` passed 1/1.
- Docker/AppHost validation after Docker recovered: `aspire doctor` passed Docker (`Docker v29.4.3: running`). Restored AppHost build initially exposed the `NFalkorDB` 1.0.6 graph-bound `QueryAsync` API; `src/Hexalith.Memories.Redis/FalkorDbCompatibilityExtensions.cs` preserves the previous graph-id query call shape and the AppHost build passed.
- AppHost runtime validation: `dotnet run --project .\src\Hexalith.Memories.AppHost\Hexalith.Memories.AppHost.csproj --no-build` produced a healthy Aspire resource graph (Redis, FalkorDB, memories-server, memories-mcp, and both DAPR sidecars). Redis and FalkorDB endpoints were dynamically allocated (`tcp://localhost:54878`, `tcp://localhost:54876`) while existing `dapr_redis` continued to own host port 6379.
- `/ready` runtime validation: `curl http://localhost:54967/ready` returned 200 with `redis-ping` Healthy; stopping Aspire resource `redis-vnuanhcf` made `/ready` return 503 with `redis-ping` Unhealthy; restarting Redis restored `/ready` to 200.
- Validation follow-up: broad `dotnet test .\tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --no-build` surfaced a Markdown extraction regression from the restored Kreuzberg package set. `ContentExtractionClient` now preserves raw Markdown content for Markdown MIME types/extensions. Re-run passed 1822/1822.

### Completion Notes List

- AppHost now waits for the Redis component-file rewrite task before DAPR sidecars start, uses a process-suffixed generated component directory, cleans it on shutdown/process exit, escapes YAML double-quoted scalar paths, restricts newly created `secrets.json` permissions on Unix, reads Redis PING responses until CRLF, parses `appendonly yes` only from uncommented Redis config lines, and lets Aspire dynamically allocate Redis/FalkorDB host ports so existing local DAPR Redis on 6379 cannot poison component rewrites.
- `memories-server` now waits for `secretstore` and `llm` DAPR components in addition to Redis and FalkorDB.
- `ServiceDefaults.AddDefaultHealthChecks` now registers a default `redis-ping` readiness check that fails closed when the keyed Redis multiplexer is absent/unreachable, with an opt-out parameter for hosts without Redis; production hosts with no OTLP endpoint now register a warning log at startup.
- Root submodule validation now covers all five `.gitmodules` entries through `RequiredRootSubmodule` items.
- Production DAPR templates now avoid passwordless statestore defaults, use an absolute secretstore path with a documented volume mount, and cite DAPR docs for the retained `responseCacheTTL` key.
- Story 1.1 now records the AppPort Scope-Override and all 15 active re-review findings have an explicit disposition.
- Docker/AppHost runtime verification is complete; `/ready` returns 200 when Redis is running and 503 when Redis is intentionally stopped.
- Markdown extraction now preserves raw Markdown for `text/markdown`, `text/x-markdown`, `.md`, and `.markdown` inputs, fixing the broad server test failure surfaced during validation.

### File List

- `Directory.Build.props`
- `_bmad-output/implementation-artifacts/1-1-project-scaffolding-and-single-command-boot.md`
- `_bmad-output/implementation-artifacts/15-6-scaffolding-hardening-sweep.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `deploy/dapr/components/conversation-llm.yaml`
- `deploy/dapr/components/secretstore.yaml`
- `deploy/dapr/components/statestore.yaml`
- `src/Hexalith.Memories.AppHost/Program.cs`
- `src/Hexalith.Memories.Redis/FalkorDbCompatibilityExtensions.cs`
- `src/Hexalith.Memories.Server/Ingestion/ContentExtractionClient.cs`
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostComponentFileOrderingTests.cs`
- `tests/Hexalith.Memories.Server.Tests/HealthChecks/DefaultRedisReadyHealthCheckTests.cs`
- `tests/Hexalith.Memories.Server.Tests/HealthChecks/ProgramHealthCheckRegistrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/DaprComponentTemplateTests.cs`
- `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/SubmoduleGuardTests.cs`
