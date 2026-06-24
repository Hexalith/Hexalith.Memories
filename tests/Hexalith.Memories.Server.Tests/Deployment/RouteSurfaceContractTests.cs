// <copyright file="RouteSurfaceContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.ServiceDefaults.Health;

using Shouldly;

/// <summary>Story 18.3 AC3 — drift guard for the invocable route/operation-surface contract published at
/// <c>docs/operations/route-surface.md</c>. The novel guard over the Story 18.2
/// <see cref="DeploymentConfigurationContractTests"/> precedent is the <b>forward code → doc tie</b>: the
/// <c>app.MapX("/api/…")</c> route literals are regex-extracted from the authoritative
/// <c>src/Hexalith.Memories.Server/Program.cs</c> source (read via the repo-root marker walk) and each is
/// asserted documented, so a newly added endpoint cannot slip through undocumented. A count tie defends
/// against silent omission and phantom rows; the pub/sub, health, MCP, and <c>/process</c> ties anchor the
/// remaining surface to code constants and source text. QA gap-closure additions also guard the AC2 Dapr
/// service-invocation operation-mapping section, the AC4 publish-via-DAPR statement, and the <c>HXL002</c>
/// experimental-handler marker (code ↔ doc), promoting those from review-enforced to test-enforced.</summary>
public sealed class RouteSurfaceContractTests
{
    private const string DocRelativePath = "docs/operations/route-surface.md";

    // Matches `app.MapGet("/api/…"`, `app.MapPost("/…"`, etc. Captures the HTTP verb word and route template.
    private static readonly Regex MappedRouteRegex =
        new(@"app\.Map(Get|Post|Put|Delete|Patch)\(\s*""(/[^""]+)""", RegexOptions.Compiled);

    // Matches a documented route row's backtick-wrapped `METHOD /api/…` code span.
    private static readonly Regex DocumentedApiRowRegex =
        new(@"`(?:GET|POST|PUT|DELETE|PATCH) (/api/[^`]+)`", RegexOptions.Compiled);

    [Fact]
    public void RouteSurfaceDoc_Exists()
    {
        string path = ResolveDocPath();
        File.Exists(path).ShouldBeTrue($"Route surface contract not found at {path}");
    }

    [Fact]
    public void EveryMappedApiRoute_IsDocumented()
    {
        // Forward tie (code → doc): derive the route list from source so a newly added endpoint that is not
        // documented fails the build, rather than relying on a hand-maintained literal list. The assertion
        // ties to the documented row form `<VERB> <path>` (a backtick code span) so illustrative prose that
        // mentions a path substring (e.g. the Dapr `method/api/…` operation example) cannot satisfy it — only
        // a real method+path table row can.
        string program = ReadRepoFile("src", "Hexalith.Memories.Server", "Program.cs");
        string doc = ReadDoc();

        IReadOnlyList<(string Verb, string Path)> mappedRoutes = ExtractMappedRoutes(program);
        mappedRoutes.Count.ShouldBeGreaterThan(0, "Failed to extract any app.MapX route literal from Program.cs — the extraction regex or marker walk is broken.");

        foreach ((string verb, string path) in mappedRoutes)
        {
            string documentedSpan = $"`{verb.ToUpperInvariant()} {path}`";
            doc.ShouldContain(documentedSpan, Case.Sensitive, $"Mapped route '{verb.ToUpperInvariant()} {path}' from Program.cs is not documented as a row in {DocRelativePath}. Add it to the route-surface table.");
        }
    }

    [Fact]
    public void DocumentedApiRowCount_EqualsMappedApiRouteCount()
    {
        // Count tie: defends against silent omission (fewer rows than routes) AND phantom rows (more rows
        // than routes). Both counts are emitted so the Change Log delta is visible on failure.
        string program = ReadRepoFile("src", "Hexalith.Memories.Server", "Program.cs");
        string doc = ReadDoc();

        int sourceApiRouteCount = ExtractMappedRoutes(program).Count(r => r.Path.StartsWith("/api/", System.StringComparison.Ordinal));
        int documentedApiRowCount = DocumentedApiRowRegex.Matches(doc).Count;

        documentedApiRowCount.ShouldBe(
            sourceApiRouteCount,
            $"Documented /api/ route rows ({documentedApiRowCount}) must equal the mapped /api/ route literals in Program.cs ({sourceApiRouteCount}). Reconcile the route-surface table in {DocRelativePath}.");
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

        string doc = ReadDoc();
        doc.ShouldContain("POST /events/ingest", Case.Sensitive, $"{DocRelativePath} must document the pub/sub delivery route POST /events/ingest.");
        doc.ShouldContain("/dapr/subscribe", Case.Sensitive, $"{DocRelativePath} must document the subscription-discovery route /dapr/subscribe.");
        doc.ShouldContain(EventIngestionController.PubSubName, Case.Sensitive, $"{DocRelativePath} must document the pub/sub component name '{EventIngestionController.PubSubName}'.");
        doc.ShouldContain(EventIngestionController.TopicEnvVar, Case.Sensitive, $"{DocRelativePath} must document the topic env var '{EventIngestionController.TopicEnvVar}'.");
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
        string doc = ReadDoc();
        doc.ShouldContain($"`{HealthEndpointPaths.Health}` | `HealthEndpointPaths.Health`", Case.Sensitive, $"{DocRelativePath} must document the aggregate-health probe path '{HealthEndpointPaths.Health}' in the health table row tied to HealthEndpointPaths.Health.");
        doc.ShouldContain($"`{HealthEndpointPaths.Alive}` | `HealthEndpointPaths.Alive`", Case.Sensitive, $"{DocRelativePath} must document the liveness probe path '{HealthEndpointPaths.Alive}' in the health table row tied to HealthEndpointPaths.Alive.");
        doc.ShouldContain($"`{HealthEndpointPaths.Ready}` | `HealthEndpointPaths.Ready`", Case.Sensitive, $"{DocRelativePath} must document the readiness probe path '{HealthEndpointPaths.Ready}' in the health table row tied to HealthEndpointPaths.Ready.");
    }

    [Fact]
    public void NoProcessOperation_IsAbsentFromCodeAndRefutedInDoc()
    {
        // AC1 refutation, code-tied: the negative claim is enforced against source, not just prose.
        string program = ReadRepoFile("src", "Hexalith.Memories.Server", "Program.cs");
        string controller = ReadRepoFile("src", "Hexalith.Memories.EventStore", "EventIngestionController.cs");

        program.ShouldNotContain("/process", Case.Sensitive, "A '/process' route literal appeared in Program.cs — the route-surface refutation in the doc is now false and must be reconciled.");
        controller.ShouldNotContain("/process", Case.Sensitive, "A '/process' route literal appeared in EventIngestionController.cs — the route-surface refutation in the doc is now false and must be reconciled.");

        string doc = ReadDoc();
        doc.ShouldContain("No `/process` operation exists", Case.Sensitive, $"{DocRelativePath} must keep the explicit '/process' refutation section.");
    }

    [Fact]
    public void McpTransportRoute_IsTiedToSourceAndDocumented()
    {
        // The /mcp route lives in a project Server.Tests does not reference, so tie it via source text.
        string mcpProgram = ReadRepoFile("src", "Hexalith.Memories.Mcp", "Program.cs");
        mcpProgram.ShouldContain("MapMcp(\"/mcp\")", Case.Sensitive, "Mcp/Program.cs must keep app.MapMcp(\"/mcp\") — the MCP transport route.");

        string doc = ReadDoc();
        doc.ShouldContain("/mcp", Case.Sensitive, $"{DocRelativePath} must document the MCP transport route /mcp.");
        doc.ShouldContain("memories-mcp", Case.Sensitive, $"{DocRelativePath} must document that /mcp runs under the separate 'memories-mcp' app-id, not the 'memories' ACL target.");
    }

    [Fact]
    public void DaprServiceInvocationOperationMapping_IsDocumented()
    {
        // AC2 requires the surface be published covering "method, path, and Dapr operation semantics". The
        // forward + count ties above enforce method + path; this guards the Dapr operation-semantics half so
        // the section an ACL author translates each row through cannot be silently deleted. It asserts the
        // canonical service-invocation form and the worked translation example (previously review-enforced).
        string doc = ReadDoc();
        doc.ShouldContain("/v1.0/invoke/memories/method/", Case.Sensitive, $"{DocRelativePath} must document the Dapr service-invocation operation mapping (/v1.0/invoke/memories/method/<path>) that satisfies AC2's 'Dapr operation semantics'.");
        doc.ShouldContain("method/api/search", Case.Sensitive, $"{DocRelativePath} must keep the worked Dapr-operation translation example (operation 'method/api/search') so an ACL author can map a table row to an operation.");
    }

    [Fact]
    public void PublishViaDaprStatement_IsDocumented()
    {
        // AC4 requires an explicit statement that domain modules publish CloudEvents to DAPR rather than
        // invoking the Memories REST ingestion routes for event streams. The pub/sub route/constant tie above
        // does not assert this sentence, so guard both halves of the required AC4 claim here.
        string doc = ReadDoc();
        doc.ShouldContain("publish CloudEvents to DAPR", Case.Sensitive, $"{DocRelativePath} must state that domain modules publish CloudEvents to DAPR (AC4).");
        doc.ShouldContain("REST ingestion routes for event streams", Case.Sensitive, $"{DocRelativePath} must state that domain modules do NOT invoke the Memories REST ingestion routes for event streams (AC4).");
    }

    [Fact]
    public void ExperimentalHandlersSurface_IsTiedToCodeAndDocumented()
    {
        // The two `Handlers` rows are part of the ACL-verifiable surface but provisional: the Server stamps
        // the `X-Memories-API-Experimental: HXL002` response header on those routes. Tie the experimental
        // marker bidirectionally (code ↔ doc) so a code-side removal of the experimental gate OR a doc-side
        // drop of the HXL002 framing fails the build — strengthening the previously review-only enforcement.
        string program = ReadRepoFile("src", "Hexalith.Memories.Server", "Program.cs");
        program.ShouldContain("X-Memories-API-Experimental", Case.Sensitive, "Program.cs must keep stamping the X-Memories-API-Experimental header on the experimental Handlers routes.");
        program.ShouldContain("HXL002", Case.Sensitive, "Program.cs must keep the HXL002 experimental marker for the Handlers routes.");

        string doc = ReadDoc();
        doc.ShouldContain("X-Memories-API-Experimental: HXL002", Case.Sensitive, $"{DocRelativePath} must document the X-Memories-API-Experimental: HXL002 response header for the experimental Handlers routes.");
        doc.ShouldContain("Experimental (`HXL002`)", Case.Sensitive, $"{DocRelativePath} must keep marking the two Handlers rows Experimental (`HXL002`).");
    }

    private static IReadOnlyList<(string Verb, string Path)> ExtractMappedRoutes(string program)
        => MappedRouteRegex.Matches(program).Select(static m => (m.Groups[1].Value, m.Groups[2].Value)).ToList();

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
