---
baseline_commit: 2661387
---
# Story 18.7: MemoriesClient Mockability Stability Contract

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

| Field | Value |
| :---- | :---- |
| Epic | 18 - Downstream Consumer Integration Contract Hardening |
| Story key | `18-7-memories-client-mockability-stability-contract` |
| Origin | MEM-7 (Parties consumer integration intake, Sprint Change Proposal 2026-05-27, pass 9-6 / 2nd pass) |
| Lifecycle track | Engineering / Operational Readiness - Downstream Consumer Integration Hardening. **Not MVP-counted.** |
| Release impact | **None.** Documentation + drift-guard/example tests only. Use `docs:` / `test:` commits. No public API addition, no `feat:`, no `IMemoriesClient` interface, no `tools/release-packages.json` edit, and no package-version change. |
| Deliverable | A published, drift-guarded `MemoriesClient` mockability stability contract that (1) reaffirms Architecture Decision D9 (concrete class, no interface), (2) documents the two supported mock seams — the `HttpClient` / `IHttpClientFactory` boundary and the non-sealed/`virtual` subclass override — each backed by a real repo example test, and (3) records the breaking-change rule: sealing `MemoriesClient` or removing `virtual` requires the D9 escape hatch (extract `IMemoriesClient`) plus a sprint change. |
| Coupling | Independent. No code-behavior coupling to other Epic 18 stories. Companion to the member-level `[Experimental]` surface (`docs/dev/experimental-apis.md`) and the host-name stability contract (`docs/dev/public-surface-stability.md`, Story 18.1). |
| Parties-side follow-up | Parties keeps `ProbingMemoriesClient` (now contract-guaranteed) or migrates to the documented `HttpClient`-boundary seam at its discretion. |

## Story

As a downstream test author,
I want the supported mocking seam for `MemoriesClient` documented and guaranteed stable,
so that consumer test fixtures (for example Parties' `ProbingMemoriesClient`) do not break if the SDK evolves.

## Acceptance Criteria

1. **D9 reaffirmed; `HttpClient`/`IHttpClientFactory` seam documented with a worked example; no `IMemoriesClient`.** The published contract reaffirms Architecture Decision D9 ("Safety interfaces (IGraphQueryBuilder) are interfaces; extensibility points are concrete classes - avoids abstraction tax; extract when a second implementation arrives") and documents the supported mock seam as the `HttpClient` / `IHttpClientFactory` boundary with a worked example. It explicitly **does not** introduce an `IMemoriesClient` interface. _(Epic AC1)_

2. **Non-sealed + `virtual` subclass seam guaranteed.** The contract records a stability guarantee that `MemoriesClient` remains a **public, non-sealed** class whose **public API methods are `virtual`**, so consumer subclass-based fixtures (Parties' `ProbingMemoriesClient`; mirrored in-repo by `tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs`) keep compiling. It states that sealing the class or removing `virtual` from a public member is a **breaking change** requiring the D9 escape hatch (extract `IMemoriesClient`) and a sprint change. The non-virtual `BaseAddress` passthrough property is documented as intentionally outside the mockable surface. _(Epic AC2)_

3. **Both seams proven by example tests, not asserted.** The documented `HttpClient`-boundary seam is backed by a real example test in the repo (a new worked example plus the existing `tests/Hexalith.Memories.Cli.Tests/ClientRest/*` tests that drive a scripted `DelegatingHandler` through `HttpClient`), and the subclass-override seam is backed by `StubMemoriesClient`. The doc's worked examples mirror runnable tests so the seam is proven, not asserted. _(Epic AC3)_

4. **Drift guard ties the contract to code.** A drift-guard test asserts (a) the doc exists and contains its mandatory claims, and (b) reflects over `MemoriesClient` to assert it is **public**, **non-sealed**, declares **no `IMemoriesClient`** interface, and that **every public declared instance method is overridable** (`IsVirtual && !IsFinal`, excluding property/event accessors). The reflection guard fails the build the moment the class is sealed or a public method loses `virtual`. _(Epic AC1 + AC2 enforcement)_

5. **No production behavior change; no interface introduced.** This is a documentation + drift-guard story. No `IMemoriesClient` is added; `MemoriesClient` is **not modified** (it already satisfies the contract — `public class MemoriesClient`, class XML doc already cites D9 and the `HttpClient`/`IHttpClientFactory` seam, and all public API methods are already `virtual`); no DTO, route, or package change; no `tools/release-packages.json` edit; commits are `docs:` / `test:` only. The class is changed **only** if Task 0 preflight discovers genuine drift from the contract. _(Release-impact invariant)_

6. **Parties-side follow-up and doc cohesion captured.** The contract records that Parties keeps `ProbingMemoriesClient` (now contract-guaranteed) or migrates to the documented `HttpClient`-boundary seam at its discretion, and cross-links the companion stability docs (`public-surface-stability.md`, `experimental-apis.md`, `ingest-contract.md`, `memory-unit-id-stability.md`). _(Epic Parties follow-up + doc cohesion)_

7. **MEM-7 ledger is closed with evidence.** `_bmad-output/implementation-artifacts/deferred-work.md` flips `MEM-7` from `Status: carried-forward` to `Status: resolved` with an `Evidence:` line naming the published contract doc and the guard/example tests. Keep the Story 14.5 schema valid for `CiTestInventoryTests`. _(Process)_

8. **Focused validation passes; test-count deltas recorded.** New/changed tests pass under the sandbox xUnit v3 workaround. At minimum: the new mockability contract/example test class and `CiTestInventoryTests` after the `deferred-work.md` edit. Record test-count deltas in the Change Log. _(Process)_

## Tasks / Subtasks

- [x] **Task 0 - Preflight: re-verify live anchors before editing.** (AC: 1,2,3,4,5)
  - [x] Re-confirm `MemoriesClient` in `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` is `public class MemoriesClient` (non-sealed), with a class XML doc that already states it is a concrete class (no interface) per Architecture D9 and that mocking happens at the `HttpClient` / `IHttpClientFactory` boundary.
  - [x] Re-confirm every public API **method** is `virtual` (e.g. `ListTenantsAsync`, `SearchAsync`, `HybridSearchAsync`, `TraverseAsync`, `GetCaseAsync`, `IngestAsync` (both overloads), `LookupMemoryUnitIdBySourceUriAsync`, the `[Experimental]` `CreateTenantAsync`/`CreateCaseAsync`/`GetTelemetrySummaryAsync`/`ListHandlersAsync`/`GetHandlerMismatchesAsync`, the consistency/export methods, `ProbeHealthAsync`). Confirm the only non-`virtual` public member is the `BaseAddress` get-only passthrough property (a property accessor, deliberately not part of the mock seam).
  - [x] Re-confirm the subclass-override fixture `tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs` still `: MemoriesClient` and `override`s public virtuals (the in-repo equivalent of Parties' `ProbingMemoriesClient`).
  - [x] Re-confirm the `HttpClient`-boundary tests still drive a scripted `DelegatingHandler` (`tests/Hexalith.Memories.Cli.Tests/TestDelegatingHandler.cs`) through `new HttpClient(handler)` — see `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientLookupTests.cs` and `MemoriesClientTests.cs`.
  - [x] Re-confirm Architecture Decision **D9** wording in `_bmad-output/planning-artifacts/architecture.md` (decision-register rows; currently lines ~397 and ~579) is unchanged, and that no `IMemoriesClient` type exists in `src/Hexalith.Memories.Client.Rest/`.
  - [x] Re-confirm `MEM-7` in `_bmad-output/implementation-artifacts/deferred-work.md` is still `Status: carried-forward`.
  - [x] If any anchor moved or behavior changed since baseline `2661387`, update this story before implementing.

- [x] **Task 1 - Publish the `MemoriesClient` mockability stability contract.** (AC: 1,2,3,6)
  - [x] Add `docs/dev/client-mockability.md` (NEW, recommended) using the Story 18.1/18.6 doc-contract style: leading HTML review-cadence comment, H1 naming Story 18.7, `Origin: MEM-7`, contract section(s), guarantee/breaking-change rule, automated-enforcement section, and references.
  - [x] **Reaffirm D9** verbatim enough to be unambiguous: safety interfaces stay interfaces; extensibility points stay concrete; avoid abstraction tax; extract an interface only when a second implementation arrives. State plainly that this story **declines** to add `IMemoriesClient`.
  - [x] Document **seam 1 - `HttpClient` / `IHttpClientFactory` boundary** as the recommended seam, with a worked example (inject a test `DelegatingHandler` into the `HttpClient` the `MemoriesClient` constructor receives; script the response; assert the typed result). Reference the real example test added in Task 3 and the existing `ClientRest/*` tests.
  - [x] Document **seam 2 - subclass override**: because `MemoriesClient` is non-sealed with `virtual` public methods, a fixture may subclass and `override` the methods it needs. Name `StubMemoriesClient` as the in-repo proof and Parties' `ProbingMemoriesClient` as the consumer use case.
  - [x] State the **stability guarantee + breaking-change rule**: `MemoriesClient` stays public and non-sealed; its public methods stay `virtual`; sealing the class or removing `virtual` is a breaking change that must go through the D9 escape hatch (extract `IMemoriesClient`) and a sprint change. Note the `BaseAddress` passthrough property is intentionally non-virtual and not part of the mock seam. Mirror the additive-only posture cross-linked from `public-surface-stability.md` / ADR-7.2-001.
  - [x] Cross-link `docs/dev/public-surface-stability.md` (18.1, host-name companion), `docs/dev/experimental-apis.md` (member-level `[Experimental]` companion), `docs/dev/ingest-contract.md` (18.4), and `docs/dev/memory-unit-id-stability.md` (18.6). Record the Parties-side follow-up.

- [x] **Task 2 - Cross-link companion docs (additive, recommended).** (AC: 6)
  - [x] Add a one-line "see also" reference to `docs/dev/client-mockability.md` in the References section of `docs/dev/public-surface-stability.md` (the member-level mock-seam companion to the host-name contract). Optionally add the same cross-link in `docs/dev/experimental-apis.md`.
  - [x] Keep edits additive (no rewording of existing guarantees); preserve existing review-cadence comments and References ordering.

- [x] **Task 3 - Add the drift-guard + worked-example test.** (AC: 2,3,4,8)
  - [x] Add `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs` (NEW) in the `Hexalith.Memories.Cli.Tests` project (the project that already references `Hexalith.Memories.Client.Rest` and hosts every other `MemoriesClient` test).
  - [x] **Doc presence + mandatory claims:** use the established repo-root marker walk (`Hexalith.Memories.slnx`) and Shouldly content assertions (mirroring `RouteSurfaceContractTests` / `DeploymentConfigurationContractTests`). Assert the doc exists at `docs/dev/client-mockability.md` and contains the mandatory literals/claims: `D9`, `HttpClient`, `IHttpClientFactory`, "non-sealed" (and/or "not sealed"), `virtual`, the explicit no-`IMemoriesClient` decision, the breaking-change rule, and the `ProbingMemoriesClient` / `StubMemoriesClient` references.
  - [x] **Reflection guard (code -> contract tie):** reflect over `typeof(MemoriesClient)` and assert `IsPublic`, `!IsSealed`, and that no implemented interface is named `IMemoriesClient` (and that the `Client.Rest` assembly defines no public `IMemoriesClient`). Enumerate `GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)`, exclude `IsSpecialName` (property/event accessors) and `object`-inherited members, and assert each remaining method is `IsVirtual && !IsFinal`. Emit the offending method name on failure.
  - [x] **Worked example - `HttpClient`-boundary seam:** add a `[Fact]` that constructs a `MemoriesClient` over a `TestDelegatingHandler`-scripted `HttpClient`, invokes a public method, and asserts the typed result — proving the seam the doc recommends (no interface needed).
  - [x] **Worked example - subclass seam:** add a `[Fact]` with a small local subclass of `MemoriesClient` overriding one `virtual` method, proving subclass-based fixtures compile and dispatch (the `ProbingMemoriesClient` shape). Reuse the `StubMemoriesClient` pattern; if `IngestAsync`/experimental members are touched, scope `#pragma warning disable HXL001`/`HXL002` narrowly as the existing fixtures do.
  - [x] Keep all assertions EOL-agnostic (substring checks) so the doc's line endings do not affect the guard.

- [x] **Task 4 - Resolve MEM-7 in deferred work.** (AC: 7)
  - [x] In `_bmad-output/implementation-artifacts/deferred-work.md`, update `MEM-7` from `Status: carried-forward` to `Status: resolved`.
  - [x] Replace the `Rationale:` line with an `Evidence:` line naming `docs/dev/client-mockability.md`, the reaffirmed-D9 + no-`IMemoriesClient` decision, the non-sealed/`virtual` guarantee, and the guard/example test `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs` (plus the existing `StubMemoriesClient` and `ClientRest/*` proofs).
  - [x] Keep the entry schema exactly like adjacent resolved MEM entries (`MEM-5`, `MEM-6`): `ID` / `Status` / `Source story` / `Target artifact` / `Re-open trigger` / `Evidence`. `CiTestInventoryTests` parses this file.

- [x] **Task 5 - Verify and finalize.** (AC: 8)
  - [x] Line endings: normalize new/edited `.cs` files to **CRLF** per `.editorconfig` (`sed -i 's/$/\r/'` on files written with LF); keep new/edited `.md` as **LF** to match every sibling in `docs/dev` (the drift-guard asserts are EOL-agnostic substring checks). This mirrors the Story 18.6 decision.
  - [x] Build `tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj`.
  - [x] Run the new test class with the sandbox workaround:
    ```bash
    DiffEngine_Disabled=true dotnet exec \
      tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll \
      -class Hexalith.Memories.Cli.Tests.ClientRest.MemoriesClientMockabilityContractTests
    ```
  - [x] Run `CiTestInventoryTests` (same Cli.Tests assembly) after the `deferred-work.md` edit.
  - [x] Optionally re-run the existing `ClientRest` seam tests (`MemoriesClientLookupTests`, `MemoriesClientTests`) and the `StubMemoriesClient`-backed `Mcp.Tests` to confirm no regression in the proven seams.
  - [x] Update this story's Dev Agent Record, File List, Completion Notes, and Change Log with test counts.

## Dev Notes

### Scope and intent

This is a **documentation + drift-guard** story. Do **not** add an `IMemoriesClient` interface, do not change `MemoriesClient` (it already satisfies the contract), do not change any route/DTO/package, and do not add package references. The residual MEM-7 gap is that the mockability behavior is real but not published as a consumer contract and not guarded against silent regression (a future `sealed` or a dropped `virtual`).

The downstream failure being prevented is specific: Parties' test fixture `ProbingMemoriesClient` subclasses the concrete `MemoriesClient` and relies on `virtual` members. If a future Memories change seals the class or removes `virtual` from a public member, that consumer fixture stops compiling with no signal on the Memories side. The safe contract is: D9 keeps the client concrete (no interface tax); the supported seams are the `HttpClient`/`IHttpClientFactory` boundary (preferred) and subclass override (non-sealed + `virtual`); and a reflection guard fails the Memories build the moment either property is broken, forcing a deliberate D9 escape-hatch decision.

### Current behavior to preserve (verified at baseline `2661387`)

- `MemoriesClient` is `public class MemoriesClient` (non-sealed) in `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`. Its class XML doc **already** states: "Concrete class (no interface) per Architecture D9 - mocking happens at the `HttpClient` / `IHttpClientFactory` boundary."
- The constructor is `MemoriesClient(HttpClient httpClient, IOptions<MemoriesClientOptions> options, ILogger<MemoriesClient> logger)` — the `HttpClient` is the injection seam; `IHttpClientFactory` wiring lives in `MemoriesClientServiceCollectionExtensions`.
- **Every public API method is `virtual`** (read/list/search/traverse/case/ingest/lookup/consistency/export/health/handlers/telemetry). The `[Experimental]` methods (`CreateTenantAsync`, `CreateCaseAsync`, `GetTelemetrySummaryAsync` — `HXL001`; `ListHandlersAsync`, `GetHandlerMismatchesAsync` — `HXL002`) are also `virtual`. `IngestAsync` graduated out of `HXL001` in Story 18.4 and is stable.
- The **only** non-`virtual` public member is `public Uri? BaseAddress => _httpClient.BaseAddress;` — a trivial get-only passthrough. It is intentionally not overridable and is **excluded** from the mock-seam surface. The reflection guard must filter property accessors (`IsSpecialName`) so this passthrough does not force a production change.
- `StubMemoriesClient` (`tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs`) is the in-repo proof of the subclass seam: `internal class StubMemoriesClient : MemoriesClient` overriding `SearchAsync`, `HybridSearchAsync`, `TraverseAsync`, `GetCaseAsync`, and `IngestAsync` (with a narrow `#pragma warning disable HXL001`).
- `TestDelegatingHandler` (`tests/Hexalith.Memories.Cli.Tests/TestDelegatingHandler.cs`) is the in-repo proof of the `HttpClient`-boundary seam, used by every `ClientRest/*` test (e.g. `MemoriesClientLookupTests.CreateClient` / `CreateCapturingClient`).

### Contract wording to publish

Use precise language close to this:

> `MemoriesClient` is intentionally a concrete class with no `IMemoriesClient` interface (Architecture Decision D9: avoid the abstraction tax; extract an interface only when a second implementation arrives). Downstream tests mock it at one of two supported seams: (1) the recommended `HttpClient` / `IHttpClientFactory` boundary — inject a test `DelegatingHandler`/`HttpMessageHandler` so no Memories type is subclassed; or (2) subclass `MemoriesClient` and `override` the `virtual` methods the fixture needs.

Also publish the guarantee + breaking-change rule:

> `MemoriesClient` remains a public, non-sealed class whose public API methods are `virtual`. Subclass-based fixtures (e.g. Parties' `ProbingMemoriesClient`) are therefore stable. Sealing the class, or removing `virtual` from a public member, is a breaking change: it must go through the D9 escape hatch (extract `IMemoriesClient`) and a sprint change, not a quiet refactor. The non-virtual `BaseAddress` passthrough property is not part of the mock seam.

### Stale / adjacent wording to respect

- Architecture Decision **D9** appears twice in `architecture.md` (decision register + MVP-decisions table). Do **not** rewrite the architecture doc; cite D9 and let the new contract carry the consumer-facing detail. Do not conflate this with Memories Architecture Decision **D1** (FalkorDB for MVP) — Story 18.6 already disambiguated the unrelated Parties-side "decision D1" label.
- `public-surface-stability.md` (18.1) covers **host project/assembly/namespace names**; `experimental-apis.md` covers **member-level `[Experimental]` diagnostics**; this new doc covers the **type-shape mock seam** (non-sealed/`virtual`/no-interface). Keep the three as distinct, cross-linked companions.

### What not to change

- Do **not** add `IMemoriesClient` or any interface to `Client.Rest`. Adding it was explicitly declined to honor D9 (see MEM-7 rationale).
- Do not modify `MemoriesClient.cs` unless Task 0 preflight exposes a genuine mismatch between the contract and the current `public`/non-sealed/`virtual` shape.
- Do not seal the class, add `sealed`/`AssemblyName`/`RootNamespace` overrides, or remove `virtual`.
- Do not change `tools/release-packages.json`, `.slnx`, `Directory.Packages.props`, or submodule contents.
- Do not add a new endpoint, DTO, or CLI/MCP surface.

### Testing strategy

Use xUnit v3 + Shouldly patterns already present in the repo. The new test is a plain `[Fact]`/reflection class in `Hexalith.Memories.Cli.Tests` (no Docker/fixture, no `using Xunit;` — global usings cover it).

Recommended test assertions:

- **Doc exists** at `docs/dev/client-mockability.md` and contains: `D9`, `HttpClient`, `IHttpClientFactory`, non-sealed/`virtual`, the explicit no-`IMemoriesClient` decision, the breaking-change rule, and `ProbingMemoriesClient` / `StubMemoriesClient`.
- **Reflection:** `typeof(MemoriesClient).IsPublic` true; `IsSealed` false; no implemented interface named `IMemoriesClient`; `Client.Rest` assembly defines no public `IMemoriesClient`; every public, declared, non-special-name instance method `IsVirtual && !IsFinal` (emit the offending method on failure).
- **Worked example (HttpClient seam):** drive a `TestDelegatingHandler`-scripted `HttpClient` through a `MemoriesClient` call and assert the typed result.
- **Worked example (subclass seam):** a local `MemoriesClient` subclass overriding one `virtual` method returns the overridden value, proving the `ProbingMemoriesClient` shape compiles and dispatches.

Reflection over methods rather than all members is deliberate: the only non-virtual public member is the `BaseAddress` property accessor, which is an intentional passthrough and out of the mock seam. Filtering `IsSpecialName` keeps the guard honest without forcing a production change.

### Running tests in this sandbox

`dotnet test` can fail in this sandbox with `SocketException (13)` because VSTest opens a TCP listener. Build the project, then run the xUnit v3 assembly directly:

```bash
dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj
DiffEngine_Disabled=true dotnet exec \
  tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll \
  -class Hexalith.Memories.Cli.Tests.ClientRest.MemoriesClientMockabilityContractTests
```

Repeat for `CiTestInventoryTests` (same assembly) after the `deferred-work.md` edit. Use `-list methods` when you need the discovery count for the Change Log.

### Previous story intelligence

- Story 18.6 (`docs:` only) published `docs/dev/memory-unit-id-stability.md` and added `MemoryUnitIdStabilityContractTests` using the repo-root `Hexalith.Memories.slnx` marker walk + Shouldly content ties; it resolved MEM-6 (`carried-forward` -> `resolved`, `Rationale:` -> `Evidence:`) keeping the schema identical to adjacent entries so `CiTestInventoryTests` stays green. Mirror that close-out exactly for MEM-7.
- Story 18.1 (`test:`) published `docs/dev/public-surface-stability.md` and added compile-time + reflection guards (`AppHostProjectResolutionTests`, `PublicSurfaceStabilityTests`). The reflection-over-assembly approach there is the precedent for this story's `IsSealed`/`IsVirtual` guard.
- Story 18.5 (`feat:`) added `LookupMemoryUnitIdBySourceUriAsync` and `MemoriesClientLookupTests`, whose `TestDelegatingHandler` + `CreateClient`/`CreateCapturingClient` helpers are the exact `HttpClient`-boundary worked-example pattern to reuse here.
- Recent commits are story-scoped and conventional (`feat(story-18.5)`, `docs(story-18.6)`). This story is `docs:` + `test:` only — no `feat:`.

### Project Structure Notes

- New doc: `docs/dev/client-mockability.md` (developer/consumer contract).
- Existing docs to cross-link (additive): `docs/dev/public-surface-stability.md`, optionally `docs/dev/experimental-apis.md`.
- New test: `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs` (Cli.Tests is the only test project that already references `Client.Rest` and hosts the other `MemoriesClient` tests; the `Client.Rest` library has no dedicated test project, so do not create one).
- Deferred-work edit: `_bmad-output/implementation-artifacts/deferred-work.md` (`MEM-7` only).
- No production source change is expected unless Task 0 preflight discovers actual drift.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-18.7] - story statement, acceptance criteria, Parties follow-up.
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-18] - preflight mandate, release-timing note (only 18.4 is release-sensitive), and the per-story `(MEM-n)` linkage.
- [Source: _bmad-output/planning-artifacts/architecture.md] - Architecture Decision D9 (safety interfaces are interfaces; extensibility points are concrete classes; extract when a second implementation arrives).
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-27-parties-consumer-integration-contract-hardening.md] - MEM-7 origin: Parties `ProbingMemoriesClient` relies on a documented, stable mock seam.
- [Source: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs] - `public class MemoriesClient` (non-sealed); class XML doc cites D9 + the `HttpClient`/`IHttpClientFactory` seam; all public methods `virtual`; `BaseAddress` non-virtual passthrough.
- [Source: src/Hexalith.Memories.Client.Rest/MemoriesClientServiceCollectionExtensions.cs] - `IHttpClientFactory` registration wiring (the seam's DI half).
- [Source: tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs] - in-repo subclass-override fixture (the `ProbingMemoriesClient` equivalent).
- [Source: tests/Hexalith.Memories.Cli.Tests/TestDelegatingHandler.cs] - scripted `DelegatingHandler` for the `HttpClient`-boundary seam.
- [Source: tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientLookupTests.cs] - worked-example pattern for the `HttpClient`-boundary seam (`CreateClient` / `CreateCapturingClient`).
- [Source: tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs] - doc-contract drift-guard pattern (repo-root marker walk, Shouldly content ties).
- [Source: docs/dev/public-surface-stability.md] - Story 18.1 host-name stability companion (additive-only breaking-change posture).
- [Source: docs/dev/experimental-apis.md] - member-level `[Experimental]` companion surface.
- [Source: _bmad-output/implementation-artifacts/deferred-work.md] - MEM-7 entry to flip to `resolved` (schema must match MEM-5/MEM-6).
- [Source: _bmad-output/implementation-artifacts/18-6-memory-unit-id-stability-contract.md] - immediate-predecessor doc-contract + drift-guard + MEM close-out pattern to mirror.
- [Source: _bmad-output/project-context.md] - release rules, docs placement (`docs/dev`), central package management, CRLF/`.editorconfig`, xUnit v3, Shouldly, NSubstitute.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`

### Debug Log References

- Build: `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj` — succeeded, 0 warnings / 0 errors (warnings are build failures repo-wide via `TreatWarningsAsErrors`).
- Two transient compile fixes on the new test before green: (1) removed a stray non-source line that leaked to EOF; (2) disambiguated the Shouldly `Case.Sensitive` enum (collided with `Hexalith.Memories.Contracts.V1.Case`) by fully qualifying as `Shouldly.Case.Sensitive`.
- New class run (sandbox xUnit v3 workaround, `DiffEngine_Disabled=true dotnet exec … -class …MemoriesClientMockabilityContractTests`): Total 9, Failed 0.
- `CiTestInventoryTests` after the `deferred-work.md` MEM-7 edit: Total 48, Failed 0 (deferred-work schema stays valid).
- Regression: full `Hexalith.Memories.Cli.Tests` assembly Total 410, Failed 0 (+9 vs the pre-story 401); `Hexalith.Memories.Mcp.Tests` (StubMemoriesClient subclass-seam consumer) Total 83, Failed 0.

### Completion Notes List

- **Task 0 preflight — zero drift.** `MemoriesClient` is already `public class MemoriesClient` (non-sealed); every public API method is `virtual`; the only non-virtual public member is the get-only `BaseAddress` passthrough property; the class XML doc already cites D9 + the `HttpClient`/`IHttpClientFactory` seam; no `IMemoriesClient` exists in `Client.Rest`; D9 wording in `architecture.md` (lines ~397 / ~579) is unchanged; `StubMemoriesClient` and the `ClientRest/*` `TestDelegatingHandler` seam tests are intact; `MEM-7` was still `carried-forward`. **No production code changed** (AC5 satisfied) — the class already satisfies the contract.
- **AC1/AC2/AC3/AC6** — published `docs/dev/client-mockability.md`: reaffirms D9, explicitly declines `IMemoriesClient`, documents both supported seams (recommended `HttpClient`/`IHttpClientFactory` boundary with a worked example + subclass override), states the non-sealed/`virtual` guarantee and the breaking-change rule (D9 escape hatch + sprint change), notes the `BaseAddress` passthrough is outside the seam, and cross-links the companion docs. Added additive "see also" cross-links in `public-surface-stability.md` and `experimental-apis.md`.
- **AC4** — drift-guard `MemoriesClientMockabilityContractTests` ties code to contract: doc presence + mandatory-claims content asserts; reflection guard (`IsPublic`, `!IsSealed`, no `IMemoriesClient` on the type or exported by the assembly, every public declared non-special-name instance method `IsVirtual && !IsFinal` with the offending name emitted on failure); plus a worked-example `[Fact]` per seam. Doc asserts are EOL-agnostic substrings.
- **AC7** — `MEM-7` flipped `carried-forward` → `resolved`; `Rationale:` replaced by a single-line `Evidence:` naming the doc, the reaffirmed-D9 + no-`IMemoriesClient` decision, the non-sealed/`virtual` guarantee, and the guard/example test (plus the existing `StubMemoriesClient` / `ClientRest/*` proofs). Schema kept identical to MEM-5/MEM-6 so `CiTestInventoryTests` stays green.
- **AC8** — new/changed tests pass under the sandbox xUnit v3 workaround; test-count deltas recorded in the Change Log.
- **Line endings** — new/edited `.cs` normalized to CRLF (`.editorconfig`); new/edited `.md` kept LF to match `docs/dev` siblings (the drift-guard asserts are EOL-agnostic). `experimental-apis.md` and `deferred-work.md` were already CRLF and stayed uniformly CRLF (no mixed endings).

### File List

- `docs/dev/client-mockability.md` — NEW. The published MemoriesClient mockability stability contract (Story 18.7). Review pass: removed leaked `</content>`/`</invoke>` tool-call tags at EOF (H1) and corrected the stale `IngestAsync`/`HXL001` seam-2 example to `CreateTenantAsync` (M1).
- `docs/dev/public-surface-stability.md` — MODIFIED. Additive "see also" cross-link to `client-mockability.md` in References.
- `docs/dev/experimental-apis.md` — MODIFIED. Additive "See also" cross-link to `client-mockability.md`.
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs` — NEW (dev) / EXTENDED (QA gap-fill + review). Drift-guard + worked-example test (doc claims, reflection guard, both seams). QA pass added 4 tests: `IHttpClientFactory`/DI seam-half resolution, `BaseAddress` sole-non-virtual pin, `[Experimental]`-member subclass override, companion cross-link guard. Review pass added 1 test: `Doc_ContainsNoLeakedToolCallArtifacts` (L1 regression guard). Class total 14.
- `_bmad-output/implementation-artifacts/tests/test-summary-18-7-memories-client-mockability.md` — NEW (QA). `bmad-qa-generate-e2e-tests` gap-fill summary for this pass.
- `_bmad-output/implementation-artifacts/deferred-work.md` — MODIFIED. `MEM-7` `carried-forward` → `resolved` with an `Evidence:` line.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — MODIFIED. `18-7-…` `ready-for-dev` → `in-progress` → `review`; `last_updated` 2026-06-25.

## Change Log

| Date | Version | Description | Author |
| :--- | :------ | :---------- | :----- |
| 2026-06-25 | 0.1 | Story drafted via create-story (ultimate context engine analysis). Status -> ready-for-dev. | Bob (SM) |
| 2026-06-25 | 1.0 | Implemented (docs + drift-guard only; no production code change — preflight found zero drift). Published `docs/dev/client-mockability.md`; added companion cross-links; added `MemoriesClientMockabilityContractTests` (9 tests, all pass); resolved `MEM-7` (carried-forward → resolved). Test deltas: Cli.Tests 401 → 410 (+9, 0 failed); CiTestInventoryTests 48 (0 failed); Mcp.Tests 83 (0 failed). Status -> review. | Amelia (Dev) |
| 2026-06-25 | 1.1 | QA gap-fill pass (`bmad-qa-generate-e2e-tests`). Found 4 contract claims proven in prose only (AC3 requires proven-not-asserted) plus AC6 unguarded; auto-applied 4 tests to `MemoriesClientMockabilityContractTests`: `IHttpClientFactory`/DI seam-half resolution, `BaseAddress` sole-non-virtual passthrough pin, `[Experimental]` (`HXL001`) member subclass override, and companion-doc cross-link guard. No production code changed (proof gaps, not behavior gaps). Test deltas: contract class 9 → 13 (+4); Cli.Tests 410 → 414 (+4, 0 failed); CiTestInventoryTests 48 (0 failed). Summary: `_bmad-output/implementation-artifacts/tests/test-summary-18-7-memories-client-mockability.md`. | QA (gap-fill) |
| 2026-06-25 | 1.2 | Adversarial review (`bmad-story-automator-review`), auto-fix. Fixed H1 (leaked `</content>`/`</invoke>` tool-call tags at EOF of published `docs/dev/client-mockability.md`), M1 (doc §2 cited `StubMemoriesClient`'s `#pragma warning disable HXL001` "for `IngestAsync`" — stale: `IngestAsync` graduated out of `HXL001` in Story 18.4; reworded to the still-experimental `CreateTenantAsync` example, matching the test's `ExperimentalProbingClient`), and L1 (added `Doc_ContainsNoLeakedToolCallArtifacts` regression guard so the leaked-tag class of defect fails the build). No production code changed. Test deltas: contract class 13 → 14 (+1); Cli.Tests 414 → 415 (+1, 0 failed); CiTestInventoryTests 48 (0 failed). Status → done (0 CRITICAL). | Senior Reviewer (AI) |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-25 · **Outcome:** Approve (after auto-fix) · **Status:** done

Adversarial review of all 8 ACs against the implementation and git reality. Build clean (0 warn / 0 err). Re-verified the dev/QA pass claims independently: `MemoriesClientMockabilityContractTests` and `CiTestInventoryTests` both green; `MemoriesClient.cs` is unchanged and already satisfies the contract (AC5 holds — confirmed `public class`, non-sealed, every public method `virtual`, sole non-virtual member is the `BaseAddress` get-only passthrough). File List is accurate for all 7 in-scope files; the only extra git changes (`.claude/scheduled_tasks.lock`, `_bmad-output/story-automator/orchestration-*.md`) are automation artifacts excluded from review. **0 CRITICAL.**

Findings (auto-fixed):

- **[HIGH] H1 — leaked tool-call artifacts in the published deliverable.** `docs/dev/client-mockability.md` ended with stray `</content>` / `</invoke>` tags (the doc-generation Write tool leaked its closing markup to EOF). The drift-guard asserts only substring *presence*, so it passed despite the corruption. **Fixed:** removed the two trailing lines.
- **[MEDIUM] M1 — stale `IngestAsync`/`HXL001` claim, self-contradicting.** Doc §2 (seam 2) said a fixture scopes `#pragma warning disable` "exactly as `StubMemoriesClient` does for `IngestAsync` (`HXL001`)", but `IngestAsync` graduated out of `HXL001` in Story 18.4 (`MemoriesClient.cs` carries no `[Experimental]` on either overload) — contradicting the doc's own References line and the test's own comment. **Fixed:** reworded to the still-experimental `CreateTenantAsync` (`HXL001`) example, aligned with the proven `ExperimentalProbingClient` worked example.
- **[LOW] L1 — drift guard blind to artifact corruption** (let H1 slip). **Fixed:** added `Doc_ContainsNoLeakedToolCallArtifacts` asserting the doc contains no `</invoke>` / `</content>` / `<parameter` markup.
- **[LOW] L2 — out of scope, no fix.** `tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs:14` keeps a now-stale `#pragma warning disable HXL001 // …IngestAsync is HXL001-experimental.`. Pre-existing, not in this story's File List, and harmless (an unused suppression raises no diagnostic). Left untouched to honor the docs-only scope; flagged for a future `StubMemoriesClient` cleanup.

Verification after fixes: contract class 14/14, Cli.Tests 415/415, `CiTestInventoryTests` 48/48 — all 0 failed.
