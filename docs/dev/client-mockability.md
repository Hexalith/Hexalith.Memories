<!-- Review cadence: update when `MemoriesClient` gains or loses a public method, when its `public` / `sealed` / `virtual` shape changes, when an `IMemoriesClient` interface is introduced (the D9 escape hatch), or when the `HttpClient` / `IHttpClientFactory` constructor seam changes; otherwise quarterly — whichever comes first. Last reviewed: 2026-06-25. -->

# MemoriesClient Mockability Stability Contract (Story 18.7)

This document is the authoritative description of **how a downstream test mocks `MemoriesClient`**, **which seams are supported and guaranteed stable**, and **what it costs to change that shape**. It exists so a consumer test fixture (for example Parties' `ProbingMemoriesClient`) cannot silently stop compiling when the Memories SDK evolves — a future `sealed` or a dropped `virtual` would break the consumer with no signal on the Memories side.

- **Status:** Stable contract (documentation + drift-guarded). No public API addition; no interface introduced; this publishes existing behavior.
- **Origin:** MEM-7 (Parties consumer integration intake, Sprint Change Proposal 2026-05-27).
- **Coupling:** Independent of other Epic 18 stories. Companion to the host-name stability contract ([`./public-surface-stability.md`](./public-surface-stability.md), Story 18.1) and the member-level `[Experimental]` surface ([`./experimental-apis.md`](./experimental-apis.md)). Those cover the **host project/assembly/namespace names** and **member-level `[Experimental]` diagnostics**; this document covers the **type-shape mock seam** (non-sealed / `virtual` / no-interface).

> **Code is the source of truth.** Every claim below is mirrored from `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` and proven by runnable tests, not asserted in prose. A reflection + structure-aware drift-guard test (see [Automated enforcement](#automated-enforcement)) fails the build the moment `MemoriesClient` is sealed, a public method loses `virtual`, or an `IMemoriesClient` interface appears.

---

## 1. Architecture Decision D9 — concrete class, no interface

`MemoriesClient` is intentionally a **concrete class with no `IMemoriesClient` interface**. This honors Architecture Decision **D9**:

> Safety interfaces (`IGraphQueryBuilder`) are interfaces; extensibility points are concrete classes — avoids the abstraction tax; extract an interface only when a second implementation arrives.

A REST client has exactly one implementation, so extracting `IMemoriesClient` today would add an interface that exists only to satisfy mocking frameworks — precisely the abstraction tax D9 declines to pay. **This story deliberately declines to add `IMemoriesClient`.** The supported way to substitute the client in a test is one of the two seams below, neither of which needs an interface.

The class XML doc on `MemoriesClient` already records this: *"Concrete class (no interface) per Architecture D9 — mocking happens at the `HttpClient` / `IHttpClientFactory` boundary."*

## 2. Supported mock seams

Downstream tests mock `MemoriesClient` at one of two supported seams.

### Seam 1 — the `HttpClient` / `IHttpClientFactory` boundary (recommended)

The constructor takes an `HttpClient`:

```csharp
public MemoriesClient(HttpClient httpClient, IOptions<MemoriesClientOptions> options, ILogger<MemoriesClient> logger)
```

In production that `HttpClient` is supplied by `IHttpClientFactory` (wired in `MemoriesClientServiceCollectionExtensions`). In a test you inject a scripted `DelegatingHandler` / `HttpMessageHandler` so **no Memories type is subclassed at all** — you script the HTTP response and assert the typed result:

```csharp
var handler = new TestDelegatingHandler((_, _) =>
    Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    }));
var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5000/") };
var client = new MemoriesClient(httpClient, Options.Create(new MemoriesClientOptions { Endpoint = httpClient.BaseAddress }), NullLogger<MemoriesClient>.Instance);

// Drive a real client method; assert the deserialized result.
string? id = await client.LookupMemoryUnitIdBySourceUriAsync("acme", "case-1", "file:///doc.pdf", CancellationToken.None);
```

This is the **recommended** seam: it exercises the client's real serialization, routing, and error mapping, and it stays valid even if internal method bodies change. The in-repo proof is `tests/Hexalith.Memories.Cli.Tests/TestDelegatingHandler.cs`, used by every `tests/Hexalith.Memories.Cli.Tests/ClientRest/*` test (for example `MemoriesClientLookupTests` and `MemoriesClientTests`), and by the worked example in `MemoriesClientMockabilityContractTests`.

### Seam 2 — subclass override (non-sealed + `virtual`)

Because `MemoriesClient` is a **public, non-sealed** class whose **public API methods are `virtual`**, a fixture may subclass it and `override` only the methods it needs:

```csharp
internal sealed class StubClient : MemoriesClient
{
    public StubClient()
        : base(new HttpClient { BaseAddress = new Uri("http://stub.local/") },
               Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://stub.local/") }),
               NullLogger<MemoriesClient>.Instance)
    {
    }

    public override Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct)
        => Task.FromResult(new SearchResult { Results = [], TotalCount = 0, HasIndexedMemoryUnits = true, Query = request.Query ?? string.Empty });
}
```

The in-repo proof of this seam is `tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs` (`internal class StubMemoriesClient : MemoriesClient`, overriding `SearchAsync`, `HybridSearchAsync`, `TraverseAsync`, `GetCaseAsync`, and `IngestAsync`). The consumer use case is Parties' `ProbingMemoriesClient`, which subclasses `MemoriesClient` for the same reason. When a fixture overrides a still-`[Experimental]` member (`HXL001` / `HXL002` — for example `CreateTenantAsync` (`HXL001`)), it scopes a narrow `#pragma warning disable` over the override, the same `#pragma`-scoping technique `StubMemoriesClient` uses at file scope. (`IngestAsync` itself graduated out of `HXL001` in Story 18.4, so overriding it no longer needs a suppression; the currently-experimental override path is proven by the `CreateTenantAsync` worked example in `MemoriesClientMockabilityContractTests`.)

## 3. Stability guarantee and breaking-change rule

**Guarantee.** `MemoriesClient` remains a **public, non-sealed** class whose **public API methods are `virtual`**. Subclass-based fixtures (e.g. Parties' `ProbingMemoriesClient`; mirrored in-repo by `StubMemoriesClient`) are therefore stable, and the `HttpClient` / `IHttpClientFactory` seam keeps working without any subclassing.

**Breaking-change rule.** Sealing the class, or removing `virtual` from a public member, is a **breaking change**. It must go through the **D9 escape hatch** — extract an `IMemoriesClient` interface (the documented "second implementation arrives" trigger) — **and a sprint change**, not a quiet refactor. This mirrors the additive-only stability posture cross-linked from [`./public-surface-stability.md`](./public-surface-stability.md) (ADR-7.2-001): the consumer-facing shape only grows; it does not silently narrow.

## 4. The `BaseAddress` passthrough is outside the mock seam

The single non-`virtual` public member is the get-only passthrough property:

```csharp
public Uri? BaseAddress => _httpClient.BaseAddress;
```

It is a trivial read-through to the injected `HttpClient` and is **intentionally not part of the mockable surface** — there is nothing to override, and the `HttpClient` seam already controls it. The drift-guard reflection check therefore filters property/event accessors (`IsSpecialName`) so this passthrough does not force `MemoriesClient` to declare it `virtual` for the sake of the guard.

## Automated enforcement

A drift-guard test ties this contract to the code: [`tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs`](../../tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs). It runs on every build (plain `[Fact]`s, no Docker/fixture, repo-root marker walk over `Hexalith.Memories.slnx`) and enforces:

- **Exact section ownership:** D9, supported seams, stability/breaking-change, `BaseAddress`, and companion-link claims must remain inside their exact owning ATX sections. LF and CRLF are normalized by the shared parser; vocabulary elsewhere in the document cannot satisfy the guard.
- **Reflection guard (code → contract tie):** `typeof(MemoriesClient)` is `IsPublic`, **not** `IsSealed`, implements no interface named `IMemoriesClient`, and the `Hexalith.Memories.Client.Rest` assembly exports no public `IMemoriesClient`. Every public, declared, non-special-name instance method is `IsVirtual && !IsFinal` (the offending method name is emitted on failure). The build fails the instant the class is sealed or a public method loses `virtual`.
- **Worked examples (seams proven, not asserted):** a `[Fact]` drives the `HttpClient`-boundary seam through a `TestDelegatingHandler`-scripted `HttpClient` and asserts the typed result; a second `[Fact]` subclasses `MemoriesClient`, overrides one `virtual` method, and asserts the override dispatches — proving the `ProbingMemoriesClient` shape compiles and runs.
- **Anti-corruption check:** rejects leaked `content`, `invoke`, `parameter`, or `tool_call` markup through the shared assertion-neutral helper.

The subclass seam is additionally exercised by `tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs` and the MCP tool tests that consume it, and the `HttpClient` seam by the full `tests/Hexalith.Memories.Cli.Tests/ClientRest/*` suite.

## Parties-side follow-up

Parties may keep `ProbingMemoriesClient` (now contract-guaranteed by the non-sealed/`virtual` stability rule above) **or** migrate to the recommended `HttpClient`-boundary seam at its discretion. Both are supported; neither requires an `IMemoriesClient` interface.

## References

- Story 18.7 — MemoriesClient Mockability Stability Contract (this document).
- MEM-7 — Parties consumer integration intake (Sprint Change Proposal 2026-05-27): document the supported mock seam and guarantee the non-sealed/`virtual` shape; adding `IMemoriesClient` was explicitly declined to honor D9.
- `_bmad-output/planning-artifacts/architecture.md` — Architecture Decision **D9** (safety interfaces are interfaces; extensibility points are concrete classes; extract when a second implementation arrives).
- [`./public-surface-stability.md`](./public-surface-stability.md) — Story 18.1 host-name stability companion (additive-only breaking-change posture).
- [`./experimental-apis.md`](./experimental-apis.md) — member-level `[Experimental]` (`HXL001` / `HXL002`) companion surface.
- [`./ingest-contract.md`](./ingest-contract.md) — Story 18.4 stable ingest contract (`IngestAsync` graduated out of `HXL001`).
- [`./memory-unit-id-stability.md`](./memory-unit-id-stability.md) — Story 18.6 `MemoryUnitId` stability contract companion.
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` — `public class MemoriesClient` (non-sealed); class XML doc cites D9 + the `HttpClient` / `IHttpClientFactory` seam; all public methods `virtual`; `BaseAddress` non-virtual passthrough.
- `src/Hexalith.Memories.Client.Rest/MemoriesClientServiceCollectionExtensions.cs` — `IHttpClientFactory` registration wiring (the seam's DI half).
- `tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs` — in-repo subclass-override fixture (the `ProbingMemoriesClient` equivalent).
- `tests/Hexalith.Memories.Cli.Tests/TestDelegatingHandler.cs` — scripted `DelegatingHandler` for the `HttpClient`-boundary seam.
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientLookupTests.cs` — worked-example pattern for the `HttpClient`-boundary seam.
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs` — the drift-guard + worked-example test for this contract.
