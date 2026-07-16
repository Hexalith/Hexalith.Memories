// <copyright file="RouteSurfaceContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.ServiceDefaults.Health;
using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

using MemoriesRoutes = Hexalith.Memories.Contracts.V1.MemoriesRoutes;

/// <summary>Story 18.3 AC3 — drift guard for the invocable route/operation-surface contract published at
/// <c>docs/operations/route-surface.md</c>. The novel guard over the Story 18.2
/// <see cref="DeploymentConfigurationContractTests"/> precedent is the <b>forward code → doc tie</b>: the
/// direct <c>app.MapX(MemoriesRoutes.X, …)</c> route references are regex-extracted from the authoritative
/// <c>src/Hexalith.Memories.Server/Program.cs</c> and decomposed endpoint source files (read via the
/// repo-root marker walk) and each is asserted documented, so a newly added endpoint cannot slip through
/// undocumented. A count tie defends against silent omission and phantom rows; the pub/sub, health, MCP,
/// and <c>/process</c> ties anchor the remaining surface to code constants and source text. QA gap-closure
/// additions also guard the AC2 Dapr service-invocation operation-mapping section, the AC4 publish-via-DAPR
/// statement, and the <c>HXL002</c> experimental-handler marker (code ↔ doc), promoting those from
/// review-enforced to test-enforced.</summary>
public sealed class RouteSurfaceContractTests
{
    private const string DocRelativePath = "docs/operations/route-surface.md";

    // Matches only a direct `app.MapGet(MemoriesRoutes.X, …)` route-table reference. Requiring the comma
    // immediately after the member rejects both inline literals and composed expressions such as
    // `MemoriesRoutes.Search + "/internal"`, preserving the single-source route invariant.
    // Group 1 = HTTP verb; Group 2 = MemoriesRoutes member name.
    private static readonly Regex MappedRouteRegex =
        new(@"app\.Map(Get|Post|Put|Delete|Patch)\(\s*MemoriesRoutes\.([A-Za-z0-9_]+)\s*,", RegexOptions.Compiled);

    [Fact]
    public void RouteSurfaceDoc_Exists()
    {
        string path = ResolveDocPath();
        File.Exists(path).ShouldBeTrue($"Route surface contract not found at {path}");
    }

    [Fact]
    public void EveryMappedApiRoute_IsDocumentedExactlyOnceInRestTable()
    {
        // Forward tie (code → doc): derive the route list from source so a newly added endpoint that is not
        // documented fails the build, rather than relying on a hand-maintained literal list. The assertion
        // ties to the documented row form `<VERB> <path>` (a backtick code span) so illustrative prose that
        // mentions a path substring (e.g. the Dapr `method/api/v1/…` operation example) cannot satisfy it — only
        // a real method+path table row can.
        string routeSources = ReadMappedRouteSources();
        var document = new MarkdownContractDocument(ReadDoc());
        document.GetTableHeader("REST `/api/v1/*` operation surface").ShouldBe(["Area", "Method + path", "Purpose"]);
        IReadOnlyList<IReadOnlyList<string>> rows = document.GetTableRows("REST `/api/v1/*` operation surface");

        IReadOnlyList<(string Verb, string Path)> mappedRoutes = ExtractMappedRoutes(routeSources)
            .Where(static route => route.Path.StartsWith("/api/v1/", System.StringComparison.Ordinal))
            .ToArray();
        mappedRoutes.Count.ShouldBeGreaterThan(0, "Failed to extract any app.MapX route literal from Program.cs or decomposed endpoint files — the extraction regex or marker walk is broken.");
        mappedRoutes.Distinct().Count().ShouldBe(mappedRoutes.Count, "Each source route must be registered exactly once before it can be tied uniquely to the contract table.");

        foreach ((string verb, string path) in mappedRoutes)
        {
            string documentedCell = $"`{verb.ToUpperInvariant()} {path}`";
            rows.Count(row => row.Count == 3 && string.Equals(row[1], documentedCell, System.StringComparison.Ordinal))
                .ShouldBe(1, $"Mapped route '{verb.ToUpperInvariant()} {path}' must occur exactly once as the Method + path cell in {DocRelativePath}.");
        }
    }

    [Fact]
    public void DocumentedApiRowCount_EqualsMappedApiRouteCount()
    {
        // Count tie: defends against silent omission (fewer rows than routes) AND phantom rows (more rows
        // than routes). Both counts are emitted so the Change Log delta is visible on failure.
        string routeSources = ReadMappedRouteSources();
        IReadOnlyList<IReadOnlyList<string>> rows = new MarkdownContractDocument(ReadDoc())
            .GetTableRows("REST `/api/v1/*` operation surface");

        int sourceApiRouteCount = ExtractMappedRoutes(routeSources).Count(r => r.Path.StartsWith("/api/v1/", System.StringComparison.Ordinal));
        int documentedApiRowCount = rows.Count;

        documentedApiRowCount.ShouldBe(
            sourceApiRouteCount,
            $"Documented /api/v1/ route rows ({documentedApiRowCount}) must equal the mapped /api/v1/ route literals in Program.cs and decomposed endpoint files ({sourceApiRouteCount}). Reconcile the route-surface table in {DocRelativePath}.");

        new MarkdownContractDocument(ReadDoc()).GetSection("Automated enforcement")
            .ShouldContain($"currently **{sourceApiRouteCount}**", Case.Sensitive, "The narrative route count must stay tied to the source-derived count.");
    }

    [Fact]
    public void Program_InvokesAllDecomposedEndpointRegistrations()
    {
        string program = ReadRepoFile("src", "Hexalith.Memories.Server", "Program.cs");
        string[] expectedRegistrations =
        [
            "MapIngestionEndpoints(",
            "MapTenantLifecycleEndpoints(",
            "MapExportEndpoints(",
            "MapConsistencyEndpoints(",
            "MapCasesEndpoints(",
            "MapSearchEndpoints(",
            "MapGraphEndpoints(",
        ];

        foreach (string registration in expectedRegistrations)
        {
            program.ShouldContain(registration, Case.Sensitive, $"Program.cs must invoke {registration} so decomposed routes are registered at runtime.");
        }
    }

    [Fact]
    public void RouteExtractor_AcceptsOnlyDirectRouteTableMembers()
    {
        const string Source = """
            app.MapGet(MemoriesRoutes.Search, Handler);
            app.MapGet(MemoriesRoutes.Search + "/internal", Handler);
            app.MapGet("/api/v1/search", Handler);
            """;

        IReadOnlyList<(string Verb, string Path)> routes = ExtractMappedRoutes(Source);

        routes.Count.ShouldBe(1);
        routes[0].ShouldBe(("Get", MemoriesRoutes.Search));
    }

    [Fact]
    public void PubSubOperationSurface_IsTiedToCodeAndDocumented()
    {
        // Bidirectional pub/sub tie: a code-side rename of the component constant OR a doc-side rename fails.
        EventIngestionController.PubSubName.ShouldBe("pubsub", "Pub/sub component name constant must not drift.");

        string controller = ReadRepoFile("src", "Hexalith.Memories.EventStore", "EventIngestionController.cs");
        controller.ShouldContain("[Route(\"events\")]", Case.Sensitive, "EventIngestionController must keep the [Route(\"events\")] attribute that composes POST /events/ingest.");
        controller.ShouldContain("[HttpPost(\"ingest\")]", Case.Sensitive, "EventIngestionController must keep the [HttpPost(\"ingest\")] attribute that composes POST /events/ingest.");

        string program = ReadRepoFile("src", "Hexalith.Memories.Server", "Program.cs");
        program.ShouldContain("MapSubscribeHandler(", Case.Sensitive, "Program.cs must keep app.MapSubscribeHandler() that emits the /dapr/subscribe discovery route.");

        var document = new MarkdownContractDocument(ReadDoc());
        document.GetTableHeader("Pub/sub event-intake operation surface")
            .ShouldBe(["Operation", "Method + path", "Authoritative source", "Notes"]);
        IReadOnlyList<IReadOnlyList<string>> rows = document.GetTableRows("Pub/sub event-intake operation surface");
        rows.Count.ShouldBe(2);
        rows[0].ShouldBe(["Subscription discovery", "`GET /dapr/subscribe`", "`app.MapSubscribeHandler()` — `Program.cs`", "Framework-emitted; advertises the topic + route `/events/ingest` on component `pubsub`."]);
        rows[1].ShouldBe(["Pub/sub delivery", "`POST /events/ingest`", "`EventIngestionController` — `[Route(\"events\")]` + `[HttpPost(\"ingest\")]`", "CloudEvents intake; content types `application/json`, `application/cloudevents+json`. Topic resolved from `MEMORIES_EVENTSTORE_TOPIC` on component `pubsub`."]);
        rows.SelectMany(static row => row).Count(cell => cell.Contains(EventIngestionController.PubSubName, System.StringComparison.Ordinal))
            .ShouldBeGreaterThan(0, $"{DocRelativePath} must publish the pub/sub component name in the pub/sub table.");
        rows.SelectMany(static row => row).Count(cell => cell.Contains(EventIngestionController.TopicEnvVar, System.StringComparison.Ordinal))
            .ShouldBeGreaterThan(0, $"{DocRelativePath} must publish the topic env var in the pub/sub table.");
    }

    [Fact]
    public void HealthProbePaths_AreDocumentedFromConstants()
    {
        // Reflect the HealthEndpointPaths constants so a code-side rename of a probe path fails the doc. Pin to
        // the authoritative health table row (the path-value cell adjacent to its constant-name cell) rather
        // than a bare path substring: `/health`, `/alive`, and `/ready` also appear in MCP prose, the
        // ../dev/health-checks.md cross-links, and the References section, so a bare ShouldContain would still
        // pass even if the health table itself were deleted. Requiring the `<path>` | `HealthEndpointPaths.<Name>`
        // row form closes that doc-side-deletion gap while still catching a code-side path rename.
        var document = new MarkdownContractDocument(ReadDoc());
        document.GetTableHeader("Health and infrastructure probes").ShouldBe(["Probe", "Path", "Constant", "Semantics"]);
        IReadOnlyList<IReadOnlyList<string>> rows = document.GetTableRows("Health and infrastructure probes");
        rows.Count.ShouldBe(3);
        rows[0].ShouldBe(["Aggregate health", $"`{HealthEndpointPaths.Health}`", "`HealthEndpointPaths.Health`", "Surfaces every registered health check."]);
        rows[1].ShouldBe(["Liveness", $"`{HealthEndpointPaths.Alive}`", "`HealthEndpointPaths.Alive`", "Runs only checks tagged `live`."]);
        rows[2].ShouldBe(["Readiness", $"`{HealthEndpointPaths.Ready}`", "`HealthEndpointPaths.Ready`", "Runs only checks tagged `ready`."]);
    }

    [Fact]
    public void NoProcessOperation_IsAbsentFromCodeAndRefutedInDoc()
    {
        // AC1 refutation, code-tied: the negative claim is enforced against source, not just prose.
        string program = ReadMappedRouteSources();
        string controller = ReadRepoFile("src", "Hexalith.Memories.EventStore", "EventIngestionController.cs");

        program.ShouldNotContain("/process", Case.Sensitive, "A '/process' route literal appeared in Program.cs or decomposed endpoint files — the route-surface refutation in the doc is now false and must be reconciled.");
        controller.ShouldNotContain("/process", Case.Sensitive, "A '/process' route literal appeared in EventIngestionController.cs — the route-surface refutation in the doc is now false and must be reconciled.");

        string section = new MarkdownContractDocument(ReadDoc()).GetSection("No `/process` operation exists");
        section.ShouldContain("no `/process` operation anywhere", Case.Insensitive, $"{DocRelativePath} must keep the explicit '/process' refutation in its owning section.");
    }

    [Fact]
    public void McpTransportRoute_IsTiedToSourceAndDocumented()
    {
        // The /mcp route lives in a project Server.Tests does not reference, so tie it via source text.
        string mcpProgram = ReadRepoFile("src", "Hexalith.Memories.Mcp", "Program.cs");
        mcpProgram.ShouldContain("MapMcp(\"/mcp\")", Case.Sensitive, "Mcp/Program.cs must keep app.MapMcp(\"/mcp\") — the MCP transport route.");

        string section = new MarkdownContractDocument(ReadDoc()).GetSection("MCP transport surface (separate app-id)");
        section.ShouldContain("POST /mcp", Case.Sensitive, $"{DocRelativePath} must document the MCP transport route in its owning section.");
        section.ShouldContain("memories-mcp", Case.Sensitive, $"{DocRelativePath} must document the MCP app-id in its owning section.");
    }

    [Fact]
    public void DaprServiceInvocationOperationMapping_IsDocumented()
    {
        // AC2 requires the surface be published covering "method, path, and Dapr operation semantics". The
        // forward + count ties above enforce method + path; this guards the Dapr operation-semantics half so
        // the section an ACL author translates each row through cannot be silently deleted. It asserts the
        // canonical service-invocation form and the worked translation example (previously review-enforced).
        string section = new MarkdownContractDocument(ReadDoc()).GetSection("Dapr ACL framing and operation semantics");
        section.ShouldContain("/v1.0/invoke/memories/method/", Case.Sensitive, $"{DocRelativePath} must document the Dapr service-invocation mapping in its owning section.");
        section.ShouldContain("method/api/v1/search", Case.Sensitive, $"{DocRelativePath} must keep the worked Dapr-operation translation example in its owning section.");
    }

    [Fact]
    public void PublishViaDaprStatement_IsDocumented()
    {
        // AC4 requires an explicit statement that domain modules publish CloudEvents to DAPR rather than
        // invoking the Memories REST ingestion routes for event streams. The pub/sub route/constant tie above
        // does not assert this sentence, so guard both halves of the required AC4 claim here.
        string section = new MarkdownContractDocument(ReadDoc()).GetSection("Pub/sub event-intake operation surface");
        section.ShouldContain("publish CloudEvents to DAPR", Case.Sensitive, $"{DocRelativePath} must state the AC4 publish path in its owning section.");
        section.ShouldContain("REST ingestion routes for event streams", Case.Sensitive, $"{DocRelativePath} must state the AC4 negative claim in its owning section.");
    }

    [Fact]
    public void ExperimentalHandlersSurface_IsTiedToCodeAndDocumented()
    {
        // The two `Handlers` rows are part of the ACL-verifiable surface but provisional: the Server stamps
        // the `X-Memories-API-Experimental: HXL002` response header on those routes. Tie the experimental
        // marker bidirectionally (code ↔ doc) so a code-side removal of the experimental gate OR a doc-side
        // drop of the HXL002 framing fails the build — strengthening the previously review-only enforcement.
        string routeSources = ReadMappedRouteSources();
        routeSources.ShouldContain("X-Memories-API-Experimental", Case.Sensitive, "Program.cs or decomposed endpoint files must keep stamping the X-Memories-API-Experimental header on the experimental Handlers routes.");
        routeSources.ShouldContain("HXL002", Case.Sensitive, "Program.cs or decomposed endpoint files must keep the HXL002 experimental marker for the Handlers routes.");

        var document = new MarkdownContractDocument(ReadDoc());
        string restSection = document.GetSection("REST `/api/v1/*` operation surface");
        restSection.ShouldContain("X-Memories-API-Experimental: HXL002", Case.Sensitive, $"{DocRelativePath} must document the HXL002 response header in the REST section.");

        IReadOnlyList<IReadOnlyList<string>> handlerRows = document
            .GetTableRows("REST `/api/v1/*` operation surface")
            .Where(static row => row[0] == "Handlers")
            .ToArray();
        handlerRows.Count.ShouldBe(2);
        handlerRows[0].ShouldBe(["Handlers", "`GET /api/v1/handlers`", "Inspect the registered-handler snapshot. **Experimental (`HXL002`).**"]);
        handlerRows[1].ShouldBe(["Handlers", "`GET /api/v1/tenants/{tenantId}/handlers/mismatches`", "Detect handler routing mismatches. **Experimental (`HXL002`).**"]);
    }

    [Fact]
    public void RouteSurfaceDoc_ContainsNoLeakedToolCallMarkup()
    {
        IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(ReadDoc());

        diagnostics.ShouldBeEmpty($"{DocRelativePath} contains leaked tool-call markup: {string.Join("; ", diagnostics)}");
    }

    private static IReadOnlyList<(string Verb, string Path)> ExtractMappedRoutes(string program)
    {
        // Story 25.3: routes are registered directly against MemoriesRoutes constants rather than inline or
        // composed paths, so a matched `MemoriesRoutes.X` reference is resolved to its concrete value.
        IReadOnlyDictionary<string, string> routeConstants = RouteConstantsByName();
        List<(string Verb, string Path)> routes = [];
        foreach (Match match in MappedRouteRegex.Matches(program))
        {
            string verb = match.Groups[1].Value;
            string member = match.Groups[2].Value;
            routeConstants.ContainsKey(member).ShouldBeTrue(
                $"A route registration references MemoriesRoutes.{member}, but no matching public string constant exists on MemoriesRoutes — the route table and its consumers have drifted.");
            string path = routeConstants[member];

            routes.Add((verb, path));
        }

        return routes;
    }

    // Reflects the public string constants declared on MemoriesRoutes into a name → value map so the extractor
    // can resolve `MemoriesRoutes.X` route references back to their concrete `/api/v1/…` templates.
    private static IReadOnlyDictionary<string, string> RouteConstantsByName()
        => typeof(Hexalith.Memories.Contracts.V1.MemoriesRoutes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .ToDictionary(static f => f.Name, static f => (string)f.GetRawConstantValue()!, System.StringComparer.Ordinal);

    private static string ReadMappedRouteSources()
    {
        string serverRoot = Path.Combine(ResolveRepoRoot(), "src", "Hexalith.Memories.Server");
        string program = ReadRepoFile("src", "Hexalith.Memories.Server", "Program.cs");
        string endpointsRoot = Path.Combine(serverRoot, "Endpoints");
        Directory.Exists(endpointsRoot).ShouldBeTrue($"Endpoint source folder not found at {endpointsRoot}");

        IEnumerable<string> endpointSources = Directory
            .EnumerateFiles(endpointsRoot, "*Endpoints.cs", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, System.StringComparer.Ordinal)
            .Select(File.ReadAllText);

        return string.Join(System.Environment.NewLine, new[] { program }.Concat(endpointSources));
    }

    private static string ReadDoc() => File.ReadAllText(ResolveDocPath());

    private static string ResolveDocPath()
        => Path.Combine(ResolveRepoRoot(), "docs", "operations", "route-surface.md");

    private static string ReadRepoFile(params string[] segments)
    {
        string[] parts = new string[segments.Length + 1];
        parts[0] = ResolveRepoRoot();
        System.Array.Copy(segments, 0, parts, 1, segments.Length);
        string path = Path.Combine(parts);
        File.Exists(path).ShouldBeTrue($"Authoritative source file not found at {path}");
        return File.ReadAllText(path);
    }

    private static string ResolveRepoRoot()
    {
        // Walk up from the test binary to the repo root identified by the Hexalith.Memories.slnx marker.
        string candidate = System.AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        return System.AppContext.BaseDirectory;
    }
}
