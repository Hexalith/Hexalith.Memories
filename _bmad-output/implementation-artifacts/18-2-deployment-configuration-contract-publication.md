---
baseline_commit: eac40c49f658ca315ed4faa8639213bf5990990a
---
# Story 18.2: Deployment Configuration Contract Publication

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

| Field | Value |
| :---- | :---- |
| Epic | 18 — Downstream Consumer Integration Contract Hardening |
| Story key | `18-2-deployment-configuration-contract-publication` |
| Origin | MEM-2 (Parties consumer integration intake, Sprint Change Proposal 2026-05-27, pass 9-3) |
| Lifecycle track | Engineering / Operational Readiness — Downstream Consumer Integration Hardening. **Not MVP-counted.** |
| Release impact | **None.** Docs + a drift-guard test only — NO `feat:`. Use `docs:` / `test:` commits. No `src/` public-contract change, no `tools/release-packages.json` edit, no version bump. |
| Deliverable | A published, drift-guarded deployment-config contract under `docs/operations/`; full aspirate manifest emission stays explicitly deferred. |
| Parties-side follow-up | Parties replaces the placeholder env literals in `deploy/k8s/memories/kustomization.yaml` using the published contract. |

## Story

As an operator deploying Memories into a downstream Kubernetes overlay,
I want the canonical environment, port, and OTLP configuration surface published,
so that placeholder-shaped env literals in consumer kustomizations can be replaced with real, documented values without first running aspirate.

## Acceptance Criteria

**AC1 — Publish the canonical deploy-config contract**
**Given** there is no aspirate manifest tooling in the repo today,
**When** this story completes,
**Then** `docs/operations` documents the canonical deploy config contract: the OTLP exporter endpoint variable (`OTEL_EXPORTER_OTLP_ENDPOINT`) and its enable/disable semantics, the Dapr sidecar HTTP/gRPC ports the Server and MCP expect (3500/50001 and 3600/50101 in the AppHost defaults), and the required runtime env (`PUBSUB_REDIS_HOST`, `PUBSUB_REDIS_PASSWORD`, `MEMORIES_EVENTSTORE_TOPIC`, and connection-string keys).

**AC2 — Guard the contract against drift**
**Given** the documentation must not drift from code,
**When** the contract is published,
**Then** the documented variable names are cross-checked against `ServiceDefaults`, `AppHost/Program.cs`, and `appsettings*.json`, and a test or doc-lint guards the variable-name list against silent rename.

**AC3 — Explicitly defer aspirate emission**
**Given** full aspirate emission is a larger, separable effort,
**When** this story is scoped,
**Then** aspirate manifest generation is explicitly deferred to a future story and recorded as such; this story delivers the documented contract only.

**AC4 — Document the pub/sub event-intake deployment surface**
**Given** Hexalith modules publish events through DAPR pub/sub,
**When** the deployment contract is published,
**Then** it documents the shared pub/sub component name (`pubsub`), the required `MEMORIES_EVENTSTORE_TOPIC`, the source-prefix routing map (`EventStoreIntegration:Routing:SourceToTenantMap`), and the Memories Server sidecar ports used for subscription discovery and internal delivery.

## Tasks / Subtasks

- [x] **Task 0 — Preflight: re-verify every cited anchor before writing (Epic 18 mandate).** (AC: 1,2,4)
  - [x] Re-confirm `OTEL_EXPORTER_OTLP_ENDPOINT` gate in `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` (line ~256) and the Production-empty warning (line ~549). ✅ Confirmed exactly at `:256` and `:549`.
  - [x] Re-confirm Server sidecar ports `httpPort: 3500` / `grpcPort: 50001` (`src/Hexalith.Memories.AppHost/Program.cs` ~156-157) and MCP `httpPort: 3600` / `grpcPort: 50101` (~231-232). ✅ Confirmed at `:156-157` and `:231-232`.
  - [x] Re-confirm `EventIngestionController.PubSubName == "pubsub"` (`src/Hexalith.Memories.EventStore/EventIngestionController.cs:38`) and `TopicEnvVar == "MEMORIES_EVENTSTORE_TOPIC"` (`:43`). ✅ Confirmed at `:38` and `:43` (both `public const`, class `public sealed`).
  - [x] Re-confirm `PUBSUB_REDIS_HOST` / `PUBSUB_REDIS_PASSWORD` in `deploy/dapr/components/pubsub.yaml` (~24,26) and the real Server Dapr app-id default `"memories"` (overridable via `MEMORIES_DAPR_APP_ID`, `Program.cs` `ResolveDaprAppId`) vs MCP `"memories-mcp"`. ✅ Confirmed at `pubsub.yaml:24,26`; `ResolveDaprAppId` returns `"memories"` at `Program.cs:655`; MCP app-id `"memories-mcp"` at `:230`.
  - [x] If any anchor moved, update this story's values/line refs before authoring. ✅ No anchors moved — all 19 cited line refs match at baseline `eac40c4`.
- [x] **Task 1 — Author `docs/operations/deployment-configuration.md`.** (AC: 1,4)
  - [x] Line 1 = HTML review-cadence comment; H1 `# Deployment Configuration Contract (Story 18.2)`; one-sentence scope intro; `Origin: MEM-2 …` line.
  - [x] OTLP section: document `OTEL_EXPORTER_OTLP_ENDPOINT`, "exporter wired only when the value is non-empty; empty in Production logs a warning and collects telemetry in-process but does not export."
  - [x] Dapr sidecar ports table: Server `3500`(HTTP)/`50001`(gRPC), MCP `3600`(HTTP)/`50101`(gRPC); note these are the AppHost defaults and the canonical source is `AppHost/Program.cs`, NOT the `Hexalith.Memories.Aspire` library (parameterized; its comment mentions 3501).
  - [x] Required-runtime-env table: `PUBSUB_REDIS_HOST`, `PUBSUB_REDIS_PASSWORD` (YAML-interpolated, defaults `redis:6379` / empty), `MEMORIES_EVENTSTORE_TOPIC` (default `memories-events`), connection-string keys `ConnectionStrings__redis` / `ConnectionStrings__falkordb`. State which are env-only vs appsettings-present.
  - [x] Pub/sub event-intake section: component name `pubsub`, topic env var, `EventStoreIntegration:Routing:SourceToTenantMap` key, subscription-discovery route `/dapr/subscribe`, delivery route `POST /events/ingest`, Server sidecar ports. Cross-link `../dev/eventstore-integration.md` for routing semantics instead of duplicating.
  - [x] Backend/dashboard ports for completeness: Redis `6379`, FalkorDB `6380`, Aspire dashboard `18888`, dashboard OTLP `18889`.
  - [x] "The guarantee (rename = breaking-change-for-consumers)" section + `## References`.
  - [x] Reconcile the architecture doc's `memories-server` app-id projection with the real default `"memories"` (state the real value; note the projection as documentation drift).
- [x] **Task 2 — Add the drift-guard test.** (AC: 2)
  - [x] New `[Fact]` in `tests/Hexalith.Memories.Server.Tests/` (mirror `EventStoreIntegration/DocumentationCompletenessTests.cs`; e.g. `Deployment/DeploymentConfigurationContractTests.cs`). ✅ Added `Deployment/DeploymentConfigurationContractTests.cs` (4 `[Fact]`s).
  - [x] Resolve the doc via the repo-root-walk-to-`Hexalith.Memories.slnx` idiom; assert the doc `ShouldContain` each canonical literal (`Case.Sensitive`) with a descriptive failure message. ✅ `ResolveRepoRoot()` marker walk; `DeploymentConfigurationDoc_ContainsAllCanonicalLiterals` asserts all 22 literals `Case.Sensitive`.
  - [x] Bidirectional tie for code-backed names: assert `EventIngestionController.TopicEnvVar.ShouldBe("MEMORIES_EVENTSTORE_TOPIC")` and `EventIngestionController.PubSubName.ShouldBe("pubsub")`, AND that the doc contains those same constant values — so a code rename OR a doc rename fails the build. ✅ `DeploymentConfigurationDoc_IsTiedToEventIngestionConstants`; negative-proof confirmed it fails on a doc rename.
  - [x] For literals with no C# constant (OTLP var, ports, `PUBSUB_REDIS_*`): read the authoritative source file text (`ServiceDefaults/Extensions.cs`, `AppHost/Program.cs`, `deploy/dapr/components/pubsub.yaml`) via the same marker walk and assert the literal appears in BOTH source and doc; document any items left review-enforced. ✅ `DeploymentConfigurationDoc_LiteralsMatchAuthoritativeSourceFiles`; backend/dashboard ports left review-enforced (documented in the doc's "Automated enforcement" section).
  - [x] ITANEO MIT header, file-scoped namespace, `public sealed class`, Shouldly, no `using Xunit;` (global), plain `[Fact]` (no Docker/fixture). ✅ Conforms; builds with 0 warnings.
- [x] **Task 3 — Record the aspirate deferral.** (AC: 3)
  - [x] In `_bmad-output/implementation-artifacts/deferred-work.md`: update MEM-2 (currently `carried-forward`) to add an `Evidence:` line pointing at the new doc + test, and/or add a new entry for the residual **full aspirate manifest emission** work. Use the Story 14.5 schema exactly: `ID`, `Status` ∈ {`open`|`resolved`|`accepted`|`carried-forward`}, `Source story`, `Target artifact`, `Re-open trigger`, and `Evidence:`(resolved) or `Rationale:`(accepted/carried-forward). No aspirate follow-up story id is assigned yet — keep it open-ended. ✅ MEM-2 → `resolved` with `Evidence:` (doc + test); new `MEM-2-ASPIRATE` (`carried-forward`, open-ended) tracks residual aspirate emission. Schema validated by `CiTestInventoryTests` (48/48 pass).
- [x] **Task 4 — Verify and finalize.** (AC: 1,2,4)
  - [x] Build + run the new test via the sandbox workaround (see Dev Notes); record the discovery-count delta. ✅ Built (0 warnings); new class runs 4/4 pass; full `Server.Tests` suite 1859 pass / 0 fail / 1 pre-existing skip.
  - [x] Update this file's File List, Completion Notes, and Change Log (with the test-count delta) before handoff. ✅ Below.

## Dev Notes

### Scope and intent (read first)
This is a **documentation + drift-guard test** story, not a feature. MEM-2's residual gap is precisely: *"No published deploy config contract for consumers to fill placeholders."* The config values already exist in code (OTLP is env-gated, Dapr ports are set in the AppHost) — they are simply not published anywhere a downstream operator (Parties) can read them. Do **not** add aspirate/kustomize tooling, do **not** touch public `src/` contracts, do **not** edit `.slnx` / `Directory.Packages.props` / `release-packages.json`. Commits are `docs:` / `test:` only — `feat:` would trigger an unwanted minor release (per `project-context.md` release rules and the Epic 18 release-timing note: only Story 18.4 is release-sensitive).

### Canonical values to publish — code is the source of truth (all re-verified at baseline `eac40c4`)
| Contract element | Exact literal | Authoritative source (file:line) | Notes |
| :--- | :--- | :--- | :--- |
| OTLP endpoint var | `OTEL_EXPORTER_OTLP_ENDPOINT` | `src/Hexalith.Memories.ServiceDefaults/Extensions.cs:256` | Exporter wired only when non-empty (`!string.IsNullOrWhiteSpace`). Empty in Production → `OtlpExporterWarningHostedService` warns (`:549`): "telemetry collected in-process but not exported". Not present in any appsettings — env-only. |
| Server Dapr sidecar HTTP | `3500` | `AppHost/Program.cs:156` | AppHost default. |
| Server Dapr sidecar gRPC | `50001` | `AppHost/Program.cs:157` | AppHost default. |
| MCP Dapr sidecar HTTP | `3600` | `AppHost/Program.cs:231` | Offset to avoid colliding with Server (`:217` comment). |
| MCP Dapr sidecar gRPC | `50101` | `AppHost/Program.cs:232` | AppHost default. |
| Pub/sub component name | `pubsub` | `EventIngestionController.cs:38` (`const PubSubName`) | Also `deploy/dapr/components/pubsub.yaml:18`, `TenantEventRoutingOptions.cs:14`; validator forces config `Routing:PubSubName` to equal this. |
| Events topic env var | `MEMORIES_EVENTSTORE_TOPIC` | `EventIngestionController.cs:43` (`const TopicEnvVar`) | Set by AppHost (`Program.cs:193`) to value `memories-events`; mirrors config `EventStoreIntegration:Routing:Topic`. |
| Pub/sub redis host var | `PUBSUB_REDIS_HOST` | `deploy/dapr/components/pubsub.yaml:24` | **YAML-only** (default `redis:6379`); no C# const. Doc comment at `AppHost/Program.cs:113`. |
| Pub/sub redis password var | `PUBSUB_REDIS_PASSWORD` | `deploy/dapr/components/pubsub.yaml:26` | **YAML-only** (default empty); injected from secrets in production. |
| Source→tenant routing key | `EventStoreIntegration:Routing:SourceToTenantMap` | `TenantEventRoutingOptions.cs:21` | `Dictionary<string,string>`, longest-prefix, case-insensitive. Empty `{}` in current appsettings. |
| Connection-string keys | `ConnectionStrings__redis`, `ConnectionStrings__falkordb` | `AppHost/Program.cs:167,170`; consumed `Server/Program.cs:3166-3168` | Env-only; AppHost injects from endpoints. |
| Ingest delivery route | `POST /events/ingest` | `EventIngestionController.cs:32` (`[Route("events")]`) + `:56` (`[HttpPost("ingest")]`) | |
| Subscription discovery route | `/dapr/subscribe` | `MapSubscribeHandler()` (host wiring) | Advertises topic from `MEMORIES_EVENTSTORE_TOPIC` on component `pubsub`, route `/events/ingest`. |
| Backend / dashboard ports | Redis `6379`, FalkorDB `6380`, Aspire dashboard `18888`, OTLP `18889` | `architecture.md` §Deployment Topology (240-267) | Projection; verify against AppHost at write time. |

**App-id reconciliation (important drift):** the Server Dapr app-id default is **`"memories"`** (`AppHost/Program.cs` `ResolveDaprAppId`, overridable via env `MEMORIES_DAPR_APP_ID`), and MCP is **`"memories-mcp"`** (`:230`). `architecture.md` calls the server `memories-server` — that is an unreconciled documentation projection. Publish the **real** values and note the architecture-doc divergence (this is exactly the kind of drift AC2 exists to stop).

### Doc placement and house style (mirror these precedents)
- **Location:** `docs/operations/deployment-configuration.md` (operator-facing → operations, alongside `embedding-providers.md` / `pipeline-persistence.md`). The 18.1 stability contract lives in `docs/dev/` because it is a developer/consumer contract; 18.2 is operations.
- **Shape to mirror** — `docs/dev/public-surface-stability.md` (Story 18.1): line-1 `<!-- Review cadence: … Last reviewed: 2026-06-24 -->`, single H1 citing the story, intro paragraph, `Origin:` line, canonical **contract tables**, an explicit **"The guarantee (breaking-change rule)"** section, an **"Automated enforcement"** section naming the guard test by path, and a `## References` section with `[file.md](./file.md)` cross-links.
- **Operations house style** — titles `# <Title Case> (Story N.N)`, intro sentence, heavy tables, "Known Limitations", closing `## References` (see `docs/operations/embedding-providers.md`).
- **Avoid duplication:** the deep pub/sub routing semantics already live in `docs/dev/eventstore-integration.md` (§1.3 broker wiring, §1.4 routing config, §1.6 route surface). Cross-link it; publish only the *deployment-config contract* view here.

### Drift-guard test — the established pattern
The repo's doc↔code enforcement mechanism is **a content-asserting test**, not a markdown linter (no root markdownlint/doc-lint config exists). Two direct precedents, both build-on-every-run:
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/DocumentationCompletenessTests.cs` — walks up to the `Hexalith.Memories.slnx` marker, reads the markdown, asserts `content.ShouldContain("MEMORIES_EVENTSTORE_TOPIC", Case.Sensitive, "<message>")` etc. **This is the closest analogue — copy its structure.**
- `tests/Hexalith.Memories.Server.Tests/Telemetry/InstrumentationInventoryTests.cs` — parses a doc table and cross-checks names against code (same marker-walk idiom).
Place the new test in `Hexalith.Memories.Server.Tests` (it references Server → EventStore, so the `EventIngestionController` consts resolve for free; central package management — add no versions). Strengthen beyond the precedent by tying the doc to the **constants** (`TopicEnvVar`, `PubSubName`) so a code-side rename also fails. For literals without a const (OTLP var, ports, `PUBSUB_REDIS_*`), assert the literal appears in both the doc and the authoritative source file (read via the marker walk). State in the doc's "Automated enforcement" section exactly which items are test-enforced vs review-enforced (the 18.1 pattern: automate the reflectable ones, name the rest as review-enforced).

### Deferral recording (AC3)
The aspirate deferral is **already recorded** as MEM-2's `Rationale` in `_bmad-output/implementation-artifacts/deferred-work.md` (lines ~1408-1413, `carried-forward`). Keep it consistent: add an `Evidence:` line pointing at the new doc + test once published, and/or add a new entry for the residual **full aspirate manifest emission**. Honor the Story 14.5 schema (closed `Status` vocabulary; a parser validates the `ID` token verbatim — use `MEM-2` / a clean new id). No aspirate follow-up story id is assigned — the deferral is open-ended ("a separate future story").

### Running tests in this sandbox (mandatory workaround)
`dotnet test` fails here with `SocketException (13)` (VSTest TCP-listener limitation). Build, then run the xUnit v3 dll directly:
```bash
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj
DiffEngine_Disabled=true dotnet exec <…>/Hexalith.Memories.Server.Tests.dll \
  -class Hexalith.Memories.Server.Tests.Deployment.DeploymentConfigurationContractTests
# `-list methods` prints the discovery count for the Change Log delta.
```
`DiffEngine_Disabled=true` stops snapshot tooling from launching a diff tool. (Epic 17 retro Action Item 4; user auto-memory `running-dotnet-tests-in-sandbox.md`.)

### Process guardrails (Epic 17 retro carry-forwards)
- Track the test-count delta in the **Change Log at every phase** (Action Item 5) — dev/QA/review count drift was a recurring review finding.
- Keep the **File List current through the QA phase** (Action Item 4 / recurring challenge #4) — QA gap-closure that adds tests after the Dev Agent Record is written caused omissions on prior stories.
- Respect `.editorconfig` (4-space C#, CRLF, UTF-8, final newline) and the ITANEO MIT header on any new `.cs`.

### Project Structure Notes
- New doc: `docs/operations/deployment-configuration.md` (operations is the correct folder per `project-context.md` docs-placement rule).
- New test: `tests/Hexalith.Memories.Server.Tests/` (new `Deployment/` subfolder), reusing the existing project's references and central package pins — zero new wiring.
- No production `src/` change expected. A non-fixture `[Fact]` does not turn the project into a Docker test.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 18.2] — story statement, ACs, Parties-side follow-up, Epic 18 preflight mandate and release-timing note.
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-27-parties-consumer-integration-contract-hardening.md] — MEM-2 evidence row (line ~45), locked decision "document now, defer aspirate" (line ~110), 18.2 = doc + drift-guard test (line ~90).
- [Source: _bmad-output/implementation-artifacts/deferred-work.md] — MEM-2 entry (~1408-1413, `carried-forward`); Story 14.5 deferred-entry schema.
- [Source: docs/dev/public-surface-stability.md] — Story 18.1 contract-doc style precedent (review-cadence comment, contract table, guarantee, automated-enforcement, references).
- [Source: docs/dev/eventstore-integration.md] — pub/sub broker wiring (§1.3), routing config (§1.4), route surface (§1.6); cross-link target.
- [Source: tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/DocumentationCompletenessTests.cs] — doc-completeness drift-guard pattern (slnx-marker walk + `ShouldContain`).
- [Source: tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs] — Story 18.1 name-stability guard (`ShouldBe("<literal>")` reflection pattern).
- [Source: src/Hexalith.Memories.ServiceDefaults/Extensions.cs:256,549] — OTLP env gate + Production-empty warning.
- [Source: src/Hexalith.Memories.AppHost/Program.cs:156-157,231-232,193,167-170] — Dapr sidecar ports, topic env, connection-string keys; `ResolveDaprAppId` app-id default.
- [Source: src/Hexalith.Memories.EventStore/EventIngestionController.cs:38,43,32,56] — `PubSubName` / `TopicEnvVar` consts; ingest route.
- [Source: deploy/dapr/components/pubsub.yaml:18,24,26] — `pubsub` component, `PUBSUB_REDIS_HOST` / `PUBSUB_REDIS_PASSWORD` interpolation.
- [Source: _bmad-output/planning-artifacts/architecture.md (240-267)] — Deployment Topology Baseline (backend/dashboard ports, app IDs); reconcile `memories-server` projection with code.
- [Source: _bmad-output/project-context.md] — release-type rules, docs placement, central package management, MIT header, CRLF/editorconfig.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`

### Debug Log References

- Preflight (Task 0): all 19 cited code anchors re-verified at baseline `eac40c4`; none moved. OTLP gate `Extensions.cs:256` / warning `:549`; Server sidecar `3500`/`50001` (`AppHost/Program.cs:156-157`), MCP `3600`/`50101` (`:231-232`); `EventIngestionController.PubSubName` `:38`, `TopicEnvVar` `:43`; `pubsub.yaml:18,24,26`; `ResolveDaprAppId` → `"memories"` `Program.cs:655`, MCP `"memories-mcp"` `:230`.
- Build: `dotnet build tests/Hexalith.Memories.Server.Tests/...csproj` → succeeded, 0 warnings / 0 errors (EventStore resolves transitively via Server reference).
- New test run: `DiffEngine_Disabled=true dotnet exec Hexalith.Memories.Server.Tests.dll -class …Deployment.DeploymentConfigurationContractTests` → **Total 4, Failed 0**.
- Full regression: same dll, no filter → **Total 1859, Failed 0, Errors 0, Skipped 1** (pre-existing `SubmoduleGuardTests` skip, unrelated).
- Schema validation: `…Cli.Tests…CiTestInventoryTests` → **48/48 pass** (parses the real `deferred-work.md`, validating the MEM-2 / MEM-2-ASPIRATE edits).
- Drift-guard negative-proof (AC2): renaming `MEMORIES_EVENTSTORE_TOPIC` in the doc failed 2 tests (`ContainsAllCanonicalLiterals` + bidirectional `IsTiedToEventIngestionConstants`); all 4 pass again after restore. Note: `ShouldContain` is substring-based, so an *append*-style rename (suffix added) is not caught — a token-removing rename is. This matches the established `DocumentationCompletenessTests` precedent.

### Completion Notes List

- **AC1 (publish contract):** `docs/operations/deployment-configuration.md` documents OTLP (`OTEL_EXPORTER_OTLP_ENDPOINT` + enable/disable semantics), Dapr sidecar ports (Server `3500`/`50001`, MCP `3600`/`50101` — AppHost defaults), and required runtime env (`PUBSUB_REDIS_HOST`, `PUBSUB_REDIS_PASSWORD`, `MEMORIES_EVENTSTORE_TOPIC`, `ConnectionStrings__redis`/`ConnectionStrings__falkordb`).
- **AC2 (drift guard):** documented names cross-checked against `ServiceDefaults`, `AppHost/Program.cs`, and `pubsub.yaml`; guarded by `Deployment/DeploymentConfigurationContractTests.cs` with a bidirectional constant tie and source↔doc cross-checks. The doc's "Automated enforcement" section states which literals are test-enforced vs review-enforced (backend/dashboard ports).
- **AC3 (defer aspirate):** full aspirate manifest emission explicitly deferred and recorded — MEM-2 marked `resolved` (Evidence = doc + test), residual tracked as new open-ended `MEM-2-ASPIRATE` (`carried-forward`).
- **AC4 (pub/sub intake surface):** documented component name `pubsub`, `MEMORIES_EVENTSTORE_TOPIC`, routing key `EventStoreIntegration:Routing:SourceToTenantMap`, discovery route `/dapr/subscribe`, delivery route `POST /events/ingest`, and Server sidecar ports; deep routing semantics cross-linked to `docs/dev/eventstore-integration.md` rather than duplicated.
- **App-id drift surfaced:** the doc states the real Server app-id default `memories` (`ResolveDaprAppId`, override `MEMORIES_DAPR_APP_ID`) and flags `architecture.md`'s `memories-server` projection as documentation drift.
- **Release posture:** docs + test only — no `src/` public-contract change, no `release-packages.json` / version-bump edit. Commit types: `docs:` / `test:`.

### File List

- `docs/operations/deployment-configuration.md` (new; QA-extended; review-corrected) — deployment-config contract; "Automated enforcement" section synced in QA to describe the strengthened guards (no contract value changed). Review pass corrected the `MEMORIES_EVENTSTORE_TOPIC` "no runtime fallback" semantics and the FalkorDB host-port wording (no canonical literal changed; all 6 drift-guard `[Fact]`s still pass).
- `tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs` (new; QA-extended) — drift-guard test, **6 `[Fact]`s** (4 dev + 2 QA gap-closure; `LiteralsMatchAuthoritativeSourceFiles` also extended in QA).
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified) — QA `bmad-qa-generate-e2e-tests` run summary for Story 18.2 appended.
- `_bmad-output/implementation-artifacts/deferred-work.md` (modified) — MEM-2 → `resolved` + Evidence; added `MEM-2-ASPIRATE`.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified) — `18-2` status transitions.
- `_bmad-output/implementation-artifacts/18-2-deployment-configuration-contract-publication.md` (modified) — task checkboxes, Dev Agent Record, Change Log, Status (this file).

## Senior Developer Review (AI)

**Reviewer:** Jerome — 2026-06-24. **Outcome:** Approve (auto-fix applied). **Story status → done** (0 CRITICAL issues remain).

Adversarial validation of every story claim against the actual implementation at baseline `eac40c4`:

- **All 19 cited code anchors re-verified** — `EventIngestionController.PubSubName`/`TopicEnvVar`/route attrs, `ServiceDefaults/Extensions.cs` OTLP gate (`:256`) + `OtlpExporterWarningHostedService` (`:540/:549`), AppHost sidecar ports (`:156-157`, `:231-232`), `ConnectionStrings__*` (`:167-171`), topic value `memories-events` (`:193`), `ResolveDaprAppId → "memories"` (`:655`), MCP app-id (`:230`), `pubsub.yaml` (`name: pubsub`, `:24/:26`), `TenantEventRoutingOptions` defaults, `EventStoreIntegration:Routing` binding. None drifted.
- **AC1–AC4 all implemented.** Drift guard present and **strong** (bidirectional const ties + source↔doc cross-checks). Build 0 warnings; the 6-`[Fact]` drift-guard class passes **6/6**; full `Server.Tests` regression **1861 pass / 0 fail / 1 pre-existing skip** — exactly matching the Change Log delta.
- **File List matches git reality**; deferred-work MEM-2 → `resolved` + `MEM-2-ASPIRATE` `carried-forward` follow the Story 14.5 schema; cross-links (`../dev/eventstore-integration.md`, `../dev/public-surface-stability.md`) resolve; OTLP var confirmed absent from all `appsettings*.json` (doc claim holds).

**Findings (2 — both auto-fixed in the doc; no CRITICAL/HIGH):**

- 🟡 **MEDIUM — misleading `MEMORIES_EVENTSTORE_TOPIC` default.** The row labelled `memories-events` as a "Default", but there is **no runtime fallback**: `EnvironmentTopicAttribute.ResolveTopic` and `EventStoreIntegrationServiceCollectionExtensions.ResolveConfiguredTopic` both return `null` when the var (and the `EventStoreIntegration:Routing:Topic` config key) are unset. `memories-events` is injected only by the AppHost (`Program.cs:193`), which is absent in the downstream k8s overlay this contract targets; the "operator supplies … directly" sentence also omitted the var. A downstream operator could leave it unset and silently break event intake. **Fixed:** the row, the operator-supplies sentence, and a new explicit "no runtime default" note now state the var is required downstream.
- 🟢 **LOW — FalkorDB host-port overstatement.** "container-internal `6379` mapped to host `6380` by the AppHost" overstated: `AppHost/Program.cs:143` `.WithEndpoint(targetPort: 6379, name: "falkordb")` does not pin host port `6380` (Aspire assigns dynamically); `6380` is the architecture-baseline convention. **Fixed:** wording now attributes `6380` to the baseline convention and notes the dynamic host-port behaviour.

Both fixes are doc-prose only — no canonical literal changed, so the drift-guard contract stays green and the release posture stays `docs:`/`test:` (no `src/`, no version bump).

## Change Log

| Date | Phase | Change | Test count |
| :--- | :--- | :--- | :--- |
| 2026-06-24 | create-story | Initial story context created (ready-for-dev). Documentation + drift-guard test scope; canonical deploy-config values verified against code at baseline `eac40c4`. | n/a (no tests added yet) |
| 2026-06-24 | dev-story | Authored `docs/operations/deployment-configuration.md`; added `Deployment/DeploymentConfigurationContractTests.cs` (+4 tests); recorded aspirate deferral (MEM-2 `resolved`, new `MEM-2-ASPIRATE`). Status → review. | Server.Tests 1855 → **1859** (+4); Cli.Tests 48 (unchanged, re-validated schema) |
| 2026-06-24 | qa-generate-e2e-tests | Drift-guard gap audit: 5 documented elements were not source-tied. Auto-applied — app-id default `memories` tie (new `[Fact]`), `OtlpExporterWarningHostedService` tie, `TenantEventRoutingOptions.PubSubName` + yaml `metadata.name` ties (new `[Fact]`), ingest route-attribute ties, `EventStoreIntegration:Routing` section-prefix tie (was "review-enforced"). Doc "Automated enforcement" section synced. Negative-proofed (drift caught) + reverted. | Server.Tests 1859 → **1861** (+2; class 4 → 6 `[Fact]`s) |
| 2026-06-24 | review | Adversarial review: all anchors/ACs/tasks re-verified against code; full regression **1861 pass / 0 fail / 1 skip** reproduced. 2 doc findings auto-fixed (MEDIUM: `MEMORIES_EVENTSTORE_TOPIC` has no runtime fallback — required downstream; LOW: FalkorDB host-port wording). No canonical literal changed — drift-guard class still **6/6**. Status → done. | Server.Tests **1861** (unchanged; doc-prose fixes only) |
