// <copyright file="MemoriesClientMockabilityContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.ClientRest;

using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.TestHelpers.Documentation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>
/// Story 18.7 — drift guard + worked examples for the <c>MemoriesClient</c> mockability stability contract
/// published at <c>docs/dev/client-mockability.md</c>. Mirrors the Story 18.3 <c>RouteSurfaceContractTests</c> /
/// Story 18.6 <c>MemoryUnitIdStabilityContractTests</c> doc-contract pattern: plain <c>[Fact]</c>s (no
/// Docker/fixture, repo-root marker walk over <c>Hexalith.Memories.slnx</c>) that (a) assert the doc exists and
/// publishes its mandatory claims, (b) reflect over <see cref="MemoriesClient"/> to tie the code shape to the
/// contract — the build fails the instant the class is sealed, a public method drops <c>virtual</c>, or an
/// <c>IMemoriesClient</c> interface appears — and (c) run the documented seams as worked examples so they are proven,
/// not asserted: the <c>HttpClient</c> handler boundary, its <c>IHttpClientFactory</c>/DI registration half, subclass
/// override of a stable <c>virtual</c>, and subclass override of an <c>[Experimental]</c> member. The
/// <c>BaseAddress</c> passthrough (doc §4) and the companion-doc cross-links (AC6) are tied to code too. Narrative
/// claims are bound to their exact owning sections and the shared anti-corruption guard rejects leaked tool markup.
/// </summary>
public sealed class MemoriesClientMockabilityContractTests
{
    private const string DocRelativePath = "docs/dev/client-mockability.md";

    private static readonly Uri Endpoint = new("http://127.0.0.1:5000/");

    [Fact]
    public void MockabilityContractDoc_Exists()
    {
        string path = ResolveDocPath();
        File.Exists(path).ShouldBeTrue($"MemoriesClient mockability contract not found at {path}");
    }

    [Fact]
    public void Doc_ReaffirmsD9_AndExplicitlyDeclinesIMemoriesClient()
    {
        // AC1 — D9 is reaffirmed and IMemoriesClient is explicitly declined (no interface introduced).
        string section = ReadDocument().GetSection("1. Architecture Decision D9 — concrete class, no interface");

        section.ShouldContain("D9", Shouldly.Case.Sensitive);
        section.ShouldContain("abstraction tax", Shouldly.Case.Sensitive);
        section.ShouldContain("IMemoriesClient", Shouldly.Case.Sensitive);
        section.ShouldContain("declines to add", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void Doc_DocumentsBothSupportedSeams()
    {
        // AC1 + AC2 + AC3 — both seams are named: the HttpClient/IHttpClientFactory boundary and subclass override.
        string section = ReadDocument().GetSection("2. Supported mock seams");

        section.ShouldContain("HttpClient", Shouldly.Case.Sensitive);
        section.ShouldContain("IHttpClientFactory", Shouldly.Case.Sensitive);
        section.ShouldContain("StubMemoriesClient", Shouldly.Case.Sensitive);
        section.ShouldContain("ProbingMemoriesClient", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void Doc_StatesNonSealedVirtualGuaranteeAndBreakingChangeRule()
    {
        // AC2 — the non-sealed/virtual guarantee plus the D9-escape-hatch breaking-change rule.
        string guarantee = ReadDocument().GetSection("3. Stability guarantee and breaking-change rule");
        string baseAddress = ReadDocument().GetSection("4. The `BaseAddress` passthrough is outside the mock seam");

        guarantee.ShouldContain("non-sealed", Shouldly.Case.Sensitive);
        guarantee.ShouldContain("virtual", Shouldly.Case.Sensitive);
        guarantee.ShouldContain("breaking change", Shouldly.Case.Sensitive);
        guarantee.ShouldContain("escape hatch", Shouldly.Case.Sensitive);
        guarantee.ShouldContain("sprint change", Shouldly.Case.Sensitive);
        baseAddress.ShouldContain("BaseAddress", Shouldly.Case.Sensitive);
        baseAddress.ShouldContain("intentionally not part of the mockable surface", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void MemoriesClient_IsPublicNonSealed_WithNoIMemoriesClientInterface()
    {
        // Reflection tie (AC2/AC4): the consumer-facing shape — public, non-sealed, concrete (no interface).
        Type type = typeof(MemoriesClient);

        type.IsPublic.ShouldBeTrue("MemoriesClient must remain a public type so downstream fixtures can reference it.");
        type.IsSealed.ShouldBeFalse("MemoriesClient must remain non-sealed so consumer subclass-based fixtures (e.g. Parties' ProbingMemoriesClient) keep compiling. Sealing it is a breaking change requiring the D9 escape hatch (extract IMemoriesClient) and a sprint change.");
        type.IsAbstract.ShouldBeFalse("MemoriesClient must remain a concrete class.");

        type.GetInterfaces()
            .ShouldNotContain(
                static i => string.Equals(i.Name, "IMemoriesClient", StringComparison.Ordinal),
                "MemoriesClient must not implement an IMemoriesClient interface — Architecture Decision D9 declines the abstraction tax; the supported seams are the HttpClient boundary and subclass override.");
    }

    [Fact]
    public void ClientRestAssembly_ExportsNoPublicIMemoriesClient()
    {
        // AC1 enforcement: the no-interface decision is enforced at the assembly level, not just on the class.
        typeof(MemoriesClient).Assembly.GetExportedTypes()
            .ShouldNotContain(
                static t => string.Equals(t.Name, "IMemoriesClient", StringComparison.Ordinal),
                "Hexalith.Memories.Client.Rest must export no public IMemoriesClient. Adding it was explicitly declined to honor D9; introducing it is the deliberate escape-hatch path, not an incidental change.");
    }

    [Fact]
    public void EveryPublicDeclaredInstanceMethod_IsOverridable()
    {
        // The load-bearing guard: every public, declared, non-accessor instance method must stay overridable
        // (virtual && !final). Property/event accessors (IsSpecialName) are filtered so the intentional
        // get-only BaseAddress passthrough does not force a production change.
        MethodInfo[] methods = typeof(MemoriesClient)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(static m => !m.IsSpecialName)
            .ToArray();

        methods.ShouldNotBeEmpty("Expected MemoriesClient to declare public instance methods — the reflection guard or binding flags are broken.");

        foreach (MethodInfo method in methods)
        {
            (method.IsVirtual && !method.IsFinal).ShouldBeTrue(
                $"MemoriesClient.{method.Name} must stay overridable (virtual, non-final) so subclass-based consumer fixtures (e.g. Parties' ProbingMemoriesClient) keep compiling. Removing virtual is a breaking change requiring the D9 escape hatch (extract IMemoriesClient) and a sprint change.");
        }
    }

    [Fact]
    public async Task HttpClientBoundarySeam_ScriptedHandler_ReturnsTypedResult()
    {
        // Worked example — seam 1 (recommended): inject a scripted DelegatingHandler into the HttpClient the
        // MemoriesClient constructor receives. No Memories type is subclassed; the typed result is asserted.
        string json = JsonSerializer.Serialize(
            new MemoryUnitIdLookupResponse { MemoryUnitId = "mu-18-7" },
            MemoriesJsonContext.Options);
        MemoriesClient client = CreateScriptedClient(HttpStatusCode.OK, json);

        string? id = await client.LookupMemoryUnitIdBySourceUriAsync("acme", "case-1", "file:///doc.pdf", CancellationToken.None);

        id.ShouldBe("mu-18-7");
    }

    [Fact]
    public async Task SubclassOverrideSeam_OverriddenVirtual_Dispatches()
    {
        // Worked example — seam 2: subclass MemoriesClient and override one virtual method. Proves the
        // ProbingMemoriesClient shape compiles and that the override (not the base HTTP path) dispatches.
        var probe = new ProbingClient();

        SearchResult result = await probe.SearchAsync(
            new SearchRequest(TenantId: "acme", Axis: "syntactic", Query: "needle"),
            CancellationToken.None);

        result.TotalCount.ShouldBe(42);
        probe.SearchCalls.ShouldBe(1);
    }

    [Fact]
    public async Task IHttpClientFactorySeam_DiRegistration_ResolvesUsableClient()
    {
        // Worked example — seam 1's IHttpClientFactory half. The doc names the seam the "HttpClient /
        // IHttpClientFactory boundary" and says it is wired in MemoriesClientServiceCollectionExtensions, but the
        // other worked example constructs the client by hand. This proves AddMemoriesClient registers MemoriesClient
        // as a typed client backed by IHttpClientFactory: script the primary handler, resolve the client from DI,
        // drive a real method, and assert the typed result — no subclassing and no IMemoriesClient interface.
        string json = JsonSerializer.Serialize(
            new MemoryUnitIdLookupResponse { MemoryUnitId = "mu-di" },
            MemoriesJsonContext.Options);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoriesClient(o => o.Endpoint = Endpoint)
            .ConfigurePrimaryHttpMessageHandler(() => new TestDelegatingHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                })));

        using ServiceProvider provider = services.BuildServiceProvider();
        MemoriesClient client = provider.GetRequiredService<MemoriesClient>();

        client.BaseAddress.ShouldBe(Endpoint, "The IHttpClientFactory typed-client configuration must flow the configured Endpoint into the resolved client.");
        string? id = await client.LookupMemoryUnitIdBySourceUriAsync("acme", "case-1", "file:///doc.pdf", CancellationToken.None);
        id.ShouldBe("mu-di");
    }

    [Fact]
    public async Task SubclassOverrideSeam_ExperimentalVirtualMember_Dispatches()
    {
        // Worked example — the subclass seam also covers [Experimental] (HXL001 / HXL002) virtual members, exactly
        // as the doc states StubMemoriesClient does. The fixture scopes a narrow #pragma warning disable and
        // overrides the experimental method it needs; no IMemoriesClient interface is involved. The repo's
        // StubMemoriesClient no longer proves this (its IngestAsync override graduated out of HXL001 in Story 18.4),
        // so the currently-experimental override path is proven here.
        var probe = new ExperimentalProbingClient();

#pragma warning disable HXL001 // Calling the overridden [Experimental] member to prove dispatch through the subclass seam.
        string instanceId = await probe.CreateTenantAsync("acme", "Acme Inc", CancellationToken.None);
#pragma warning restore HXL001

        instanceId.ShouldBe("wf-acme");
        probe.CreateTenantCalls.ShouldBe(1);
    }

    [Fact]
    public void BaseAddress_IsTheSoleNonVirtualPublicMember_AndAGetOnlyPassthrough()
    {
        // Ties doc §4 to code: the single non-virtual public member is the get-only BaseAddress passthrough, which is
        // deliberately outside the mock seam. This also proves the IsSpecialName filter in
        // EveryPublicDeclaredInstanceMethod_IsOverridable is load-bearing — it excludes exactly this accessor and is
        // not silently masking a real non-virtual method that would break the subclass seam.
        Type type = typeof(MemoriesClient);

        PropertyInfo? baseAddress = type.GetProperty(
            "BaseAddress",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        baseAddress.ShouldNotBeNull("MemoriesClient must keep the BaseAddress passthrough property the contract documents.");
        baseAddress.CanRead.ShouldBeTrue("BaseAddress must stay readable.");
        baseAddress.CanWrite.ShouldBeFalse("BaseAddress must stay get-only — it is a read-through to the injected HttpClient, not mutable client state.");
        baseAddress.GetMethod.ShouldNotBeNull();
        baseAddress.GetMethod!.IsVirtual.ShouldBeFalse("BaseAddress is intentionally non-virtual and outside the mock seam (doc §4); the HttpClient seam already controls it.");

        // The ONLY non-overridable public declared instance method must be the BaseAddress getter accessor — any
        // other non-virtual public method would be a silent breaking change to the subclass seam.
        MethodInfo[] nonOverridable = type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(static m => !(m.IsVirtual && !m.IsFinal))
            .ToArray();

        MethodInfo soleNonOverridable = nonOverridable.ShouldHaveSingleItem();
        soleNonOverridable.IsSpecialName.ShouldBeTrue("The only non-overridable public declared instance method must be a property accessor, not a real method.");
        soleNonOverridable.Name.ShouldBe(
            "get_BaseAddress",
            "The only non-overridable public declared instance method must be the BaseAddress getter; any other non-virtual public method is a breaking change to the subclass seam requiring the D9 escape hatch.");
    }

    [Fact]
    public void Doc_CrossLinksCompanionStabilityDocs()
    {
        // AC6 — doc cohesion: the contract cross-links its companion stability docs so a consumer can navigate the
        // full surface (host names, member-level [Experimental], ingest, memory-unit id). Keeps the companions
        // discoverable and guards the cross-link set against silent removal.
        string references = ReadDocument().GetSection("References");

        references.ShouldContain("public-surface-stability.md", Shouldly.Case.Sensitive);
        references.ShouldContain("experimental-apis.md", Shouldly.Case.Sensitive);
        references.ShouldContain("ingest-contract.md", Shouldly.Case.Sensitive);
        references.ShouldContain("memory-unit-id-stability.md", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void Doc_ContainsNoLeakedToolCallArtifacts()
    {
        IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(ReadDoc());

        diagnostics.ShouldBeEmpty($"{DocRelativePath} contains leaked tool-call markup: {string.Join("; ", diagnostics)}");
    }

    private static MemoriesClient CreateScriptedClient(HttpStatusCode status, string body)
    {
        var handler = new TestDelegatingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            }));
        var httpClient = new HttpClient(handler) { BaseAddress = Endpoint };
        IOptions<MemoriesClientOptions> options = Options.Create(new MemoriesClientOptions { Endpoint = Endpoint });
        return new MemoriesClient(httpClient, options, NullLogger<MemoriesClient>.Instance);
    }

    private static string ReadDoc() => File.ReadAllText(ResolveDocPath());

    private static MarkdownContractDocument ReadDocument() => new(ReadDoc());

    private static string ResolveDocPath()
        => Path.Combine(ResolveRepoRoot(), "docs", "dev", "client-mockability.md");

    private static string ResolveRepoRoot()
    {
        // Walk up from the test binary to the repo root identified by the Hexalith.Memories.slnx marker.
        string candidate = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        return AppContext.BaseDirectory;
    }

    /// <summary>
    /// Minimal in-test subclass mirroring the Parties <c>ProbingMemoriesClient</c> shape: subclass the
    /// non-sealed <see cref="MemoriesClient"/> and override the one <c>virtual</c> method the fixture needs.
    /// </summary>
    private sealed class ProbingClient : MemoriesClient
    {
        public ProbingClient()
            : base(
                new HttpClient { BaseAddress = new Uri("http://stub.local/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://stub.local/") }),
                NullLogger<MemoriesClient>.Instance)
        {
        }

        public int SearchCalls { get; private set; }

        public override Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct)
        {
            this.SearchCalls++;
            return Task.FromResult(new SearchResult
            {
                Results = [],
                TotalCount = 42,
                HasIndexedMemoryUnits = true,
                Query = request.Query ?? string.Empty,
            });
        }
    }

    /// <summary>
    /// Subclass-seam fixture that overrides an <c>[Experimental]</c> (<c>HXL001</c>) <c>virtual</c> member, proving the
    /// contract's claim that the seam covers experimental members when a narrow <c>#pragma warning disable</c> is
    /// scoped — the same technique <c>StubMemoriesClient</c> uses.
    /// </summary>
#pragma warning disable HXL001 // Overriding the [Experimental] CreateTenantAsync to prove the experimental subclass-override seam.
    private sealed class ExperimentalProbingClient : MemoriesClient
    {
        public ExperimentalProbingClient()
            : base(
                new HttpClient { BaseAddress = new Uri("http://stub.local/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://stub.local/") }),
                NullLogger<MemoriesClient>.Instance)
        {
        }

        public int CreateTenantCalls { get; private set; }

        public override Task<string> CreateTenantAsync(string tenantId, string displayName, CancellationToken ct)
        {
            this.CreateTenantCalls++;
            return Task.FromResult($"wf-{tenantId}");
        }
    }
#pragma warning restore HXL001
}
