# Test Automation Summary — Story 18.7

**Feature:** MemoriesClient Mockability Stability Contract
**Story:** `18-7-memories-client-mockability-stability-contract`
**Workflow:** `bmad-qa-generate-e2e-tests` (gap-fill mode — story already implemented at status `review`)
**Date:** 2026-06-25
**Framework detected:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + NSubstitute (5.3.0). Matched the
project's existing test stack; no new framework introduced.
**Run command (sandbox):** built the test project, then ran the xUnit v3 assembly directly with
`DiffEngine_Disabled=true dotnet exec <test.dll> -class <FQN>` (`dotnet test`/VSTest socket is blocked
here — `SocketException 13`, per the story's Dev Agent Record).

## Scope

Story 18.7 is a **documentation + drift-guard** story (no UI, no new public API), so coverage is at the
**doc-contract + reflection + worked-example level** — there is no browser E2E layer to generate. The story
landed with `MemoriesClientMockabilityContractTests` already green (9 `[Fact]`s: doc presence + mandatory
claims, the `IsPublic`/`!IsSealed`/no-`IMemoriesClient` reflection ties, the every-public-method-overridable
guard, and one worked example per seam). This QA pass scanned the **published contract claims** against their
tests and found four claims that the doc **asserts in prose but no runnable test proves** (AC3 explicitly
requires the seams be *proven, not asserted*), plus one acceptance criterion (AC6) with no guard. All gaps
were **auto-applied** into the existing contract test class.

## Gaps Discovered and Applied

| # | Layer | Untested claim | Contract / AC reference | Test added |
| - | ----- | -------------- | ----------------------- | ---------- |
| 1 | DI / `IHttpClientFactory` | The doc names the seam the "`HttpClient` / **`IHttpClientFactory`** boundary" and says it is "wired in `MemoriesClientServiceCollectionExtensions`", but the only worked example **hand-constructs** the client. The IHttpClientFactory half — `AddMemoriesClient(...)` registering `MemoriesClient` as a typed client and resolving a *usable* instance from DI — was asserted in prose only. | Doc §2 Seam 1; AC1 + AC3 ("seam … proven by example tests, not asserted") | `IHttpClientFactorySeam_DiRegistration_ResolvesUsableClient` |
| 2 | Reflection — `BaseAddress` | Doc §4 states the **single** non-virtual public member is the get-only `BaseAddress` passthrough. The existing overridable-guard **filters out** accessors (`IsSpecialName`) but nothing positively pins that `BaseAddress` is get-only + non-virtual, nor that it is the *sole* non-overridable public declared method — so the filter could silently mask a real non-virtual method. | Doc §4; AC2/AC4 enforcement | `BaseAddress_IsTheSoleNonVirtualPublicMember_AndAGetOnlyPassthrough` |
| 3 | Subclass seam — `[Experimental]` | Doc §2 Seam 2 claims a fixture may override `[Experimental]` (`HXL001`/`HXL002`) members under a narrow `#pragma warning disable`, "exactly as `StubMemoriesClient` does for `IngestAsync`". But `IngestAsync` **graduated out of `HXL001`** in Story 18.4, so no in-repo fixture overrides a *currently*-experimental virtual. The experimental-override path was unproven. | Doc §2 Seam 2; AC2 + AC3 | `SubclassOverrideSeam_ExperimentalVirtualMember_Dispatches` |
| 4 | Doc cohesion (cross-links) | AC6 requires the contract cross-link its four companion stability docs. No test guarded the cross-link set against silent removal. | AC6 (Parties follow-up + doc cohesion) | `Doc_CrossLinksCompanionStabilityDocs` |

## Generated Tests

### Drift guard + worked examples — `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs`

- [x] Existing (no gap, unchanged): doc exists; reaffirms `D9` + declines `IMemoriesClient`; both seams named;
  non-sealed/`virtual` guarantee + breaking-change rule + `BaseAddress` note; `IsPublic`/`!IsSealed`/no
  `IMemoriesClient` on the type; assembly exports no public `IMemoriesClient`; every public declared
  non-accessor instance method `IsVirtual && !IsFinal`; worked example per seam (scripted `HttpClient` handler;
  subclass override of the stable `SearchAsync`).
- [x] **Added (4):** DI resolution proves the `IHttpClientFactory` typed-client half of Seam 1 end-to-end
  (`AddMemoriesClient` → `ConfigurePrimaryHttpMessageHandler` scripted → `GetRequiredService<MemoriesClient>()`
  → typed result + flowed `BaseAddress`); `BaseAddress` positively pinned as the sole non-virtual public
  member and a get-only passthrough (complements the accessor-filtered guard and proves that filter is
  load-bearing); subclass override of the `[Experimental]` `CreateTenantAsync` (`HXL001`) dispatches under a
  narrow scoped pragma; the four companion-doc cross-links are asserted present.

### Subclass-seam consumer proof — `tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs` (reviewed, no gap)

- [x] `internal class StubMemoriesClient : MemoriesClient` overriding `SearchAsync`, `HybridSearchAsync`,
  `TraverseAsync`, `GetCaseAsync`, `IngestAsync` — the in-repo `ProbingMemoriesClient` equivalent, exercised by
  the MCP tool tests. Left unchanged (the currently-experimental override path is now covered by gap #3 in the
  contract class).

### `HttpClient`-boundary seam suite — `tests/Hexalith.Memories.Cli.Tests/ClientRest/*` (reviewed, no gap)

- [x] `MemoriesClientLookupTests`, `MemoriesClientTests`, `MemoriesClientSearchTests`,
  `MemoriesClientTraverseTests`, `MemoriesClientConsistencyTests`, `MemoriesClientExportTests`,
  `MemoriesAuthHandlerTests` — all drive the scripted `TestDelegatingHandler` through `new HttpClient(handler)`
  (hit / 404 / backend-error / empty-body / unparseable-body / URL-encoding). Covered.

## Coverage by Acceptance Criterion

| AC | Description | Status |
| -- | ----------- | ------ |
| AC1 | D9 reaffirmed; `HttpClient`/`IHttpClientFactory` seam documented + worked; no `IMemoriesClient` | Covered (**+1**: DI/`IHttpClientFactory` half now proven, not asserted) |
| AC2 | Non-sealed + `virtual` subclass seam guaranteed; `BaseAddress` outside the seam | Covered (**+1** `BaseAddress` pin; **+1** experimental-member override) |
| AC3 | Both seams proven by example tests, not asserted | Covered (**+2** worked examples: DI half + experimental override) |
| AC4 | Reflection drift guard ties contract to code | Covered (**+1** sole-non-virtual-member assertion) |
| AC5 | No production behavior change; no interface introduced | Covered (no production code touched in this pass) |
| AC6 | Parties follow-up + companion doc cross-links | Covered (**+1**: cross-link set now guarded) |

## Results

| Test class run | Build | Result |
| -------------- | ----- | ------ |
| `MemoriesClientMockabilityContractTests` (drift guard + worked examples) | 0 warnings | **13 passed, 0 failed, 0 skipped** (was 9; **+4**) |
| `CiTestInventoryTests` (deferred-work schema) | 0 warnings | **48 passed, 0 failed, 0 skipped** (unaffected) |
| **Full `Hexalith.Memories.Cli.Tests` assembly** | 0 warnings | **414 passed, 0 failed, 0 skipped** (was 410; **+4**) |

4 new `[Fact]` tests added; the project builds clean under the warnings-as-errors gate; full regression green.
No production source changed — the gaps were proof gaps, not behavior gaps. New test code normalized to CRLF
per `.editorconfig`; the published doc is unchanged (the new tests prove existing claims, they do not add new
ones).

## Next Steps

- A real Parties-side compile check (its actual `ProbingMemoriesClient` against a freshly published
  `Hexalith.Memories.Client.Rest` package) belongs in the downstream consumer-integration lane, not this
  sandboxed unit layer — the in-repo `ExperimentalProbingClient` / `StubMemoriesClient` subclasses pin the
  same shape deterministically here.
- No further gaps identified — every claim the contract publishes (D9 + no-interface, both seams including the
  `IHttpClientFactory`/DI half and the `[Experimental]`-member override, the non-sealed/`virtual` reflection
  guarantee, the `BaseAddress` exclusion, and the companion cross-links) is now proven by a runnable test in
  addition to the doc-text drift guards.
