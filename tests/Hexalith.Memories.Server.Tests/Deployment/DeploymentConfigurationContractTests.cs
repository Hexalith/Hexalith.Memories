// <copyright file="DeploymentConfigurationContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.IO;
using System.Linq;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

/// <summary>Story 18.2 AC2 — drift guard for the deployment-configuration contract published at
/// <c>docs/operations/deployment-configuration.md</c>. Asserts the documented environment-variable names,
/// Dapr sidecar ports, pub/sub component name, topic, and routes stay in lock-step with the authoritative
/// code so a silent rename on either side fails the build. Mirrors the repo-root marker walk + content
/// assertion pattern of <see cref="EventStoreIntegration.DocumentationCompletenessTests"/>, and strengthens
/// it by tying the doc to the <see cref="EventIngestionController"/> constants and to the authoritative
/// source files for literals that have no C# constant.</summary>
public sealed class DeploymentConfigurationContractTests
{
    private const string DocRelativePath = "docs/operations/deployment-configuration.md";

    [Fact]
    public void DeploymentConfigurationDoc_Exists()
    {
        string path = ResolveDocPath();
        File.Exists(path).ShouldBeTrue($"Deployment configuration contract not found at {path}");
    }

    [Fact]
    public void DeploymentConfigurationDoc_HasExactContractTablesAndRowCounts()
    {
        var document = new MarkdownContractDocument(ReadDoc());
        IReadOnlyList<IReadOnlyList<string>> otlp = document.GetTableRows("OTLP telemetry export");
        IReadOnlyList<IReadOnlyList<string>> sidecars = document.GetTableRows("Dapr sidecar ports");
        IReadOnlyList<IReadOnlyList<string>> environment = document.GetTableRows("Required runtime environment");
        IReadOnlyList<IReadOnlyList<string>> pubSub = document.GetTableRows("Pub/sub event-intake deployment surface");
        IReadOnlyList<IReadOnlyList<string>> backends = document.GetTableRows("Backend and dashboard ports (for completeness)");

        document.GetTableHeader("OTLP telemetry export").ShouldBe(["Variable", "Authoritative source", "Semantics"]);
        document.GetTableHeader("Dapr sidecar ports").ShouldBe(["Service", "Dapr app-id", "HTTP port", "gRPC port", "Authoritative source"]);
        document.GetTableHeader("Required runtime environment").ShouldBe(["Variable / key", "Default", "Source-of-truth", "Env-only or appsettings?"]);
        document.GetTableHeader("Pub/sub event-intake deployment surface").ShouldBe(["Element", "Value", "Authoritative source"]);
        document.GetTableHeader("Backend and dashboard ports (for completeness)").ShouldBe(["Component", "Port", "Notes"]);

        otlp.Count.ShouldBe(1);
        sidecars.Count.ShouldBe(2);
        environment.Count.ShouldBe(7);
        pubSub.Count.ShouldBe(6);
        backends.Count.ShouldBe(4);

        otlp.Select(static row => row[0]).ShouldBe(["`OTEL_EXPORTER_OTLP_ENDPOINT`"]);
        sidecars[0].ShouldBe(["Memories Server", "`memories` (default; override with `MEMORIES_DAPR_APP_ID`)", "`3500`", "`50001`", "`AppHost/Program.cs` (`ResolveDaprAppId`, sidecar options)"]);
        sidecars[1].ShouldBe(["Memories MCP", "`memories-mcp`", "`3600`", "`50101`", "`AppHost/Program.cs` (offset so the MCP sidecar does not collide with the Server sidecar)"]);
        environment.Select(static row => row[0]).ShouldBe(
        [
            "`EnableKeycloak`",
            "`PUBSUB_REDIS_HOST`",
            "`PUBSUB_REDIS_PASSWORD`",
            "`MEMORIES_EVENTSTORE_TOPIC`",
            "`ConnectionStrings__redis`",
            "`ConnectionStrings__falkordb`",
            "`MEMORIES_DAPR_APP_ID`",
        ]);
        environment.Single(static row => row[0] == "`MEMORIES_EVENTSTORE_TOPIC`").ShouldBe(
        [
            "`MEMORIES_EVENTSTORE_TOPIC`",
            "`memories-events` (AppHost-injected convention; **required downstream** — see note)",
            "`EventIngestionController.TopicEnvVar`; value injected by `AppHost/Program.cs`",
            "**Env-only**; **required in a downstream overlay** — there is no runtime fallback (see note below). Mirrors config `EventStoreIntegration:Routing:Topic`.",
        ]);
        pubSub.Select(static row => row[0]).ShouldBe(
        [
            "Pub/sub component name",
            "Topic env var",
            "Source→tenant routing key",
            "Subscription-discovery route",
            "Delivery route",
            "Server sidecar ports (subscription + delivery)",
        ]);
        pubSub.Select(static row => row[1]).ShouldBe(
        [
            "`pubsub`",
            "`MEMORIES_EVENTSTORE_TOPIC`",
            "`EventStoreIntegration:Routing:SourceToTenantMap`",
            "`/dapr/subscribe`",
            "`POST /events/ingest`",
            "`3500` (HTTP) / `50001` (gRPC)",
        ]);
        pubSub.Select(static row => row[2]).ShouldBe(
        [
            "`EventIngestionController.PubSubName`; `deploy/dapr/components/pubsub.yaml` `metadata.name`; `TenantEventRoutingOptions.PubSubName` (a validator forces config `EventStoreIntegration:Routing:PubSubName` to equal this).",
            "`EventIngestionController.TopicEnvVar` (default value `memories-events`).",
            "`TenantEventRoutingOptions.SourceToTenantMap` — a longest-prefix, case-insensitive `Dictionary<string,string>` (empty `{}` by default).",
            "Emitted by `MapSubscribeHandler()` in the Server host; advertises the topic resolved from `MEMORIES_EVENTSTORE_TOPIC` on component `pubsub`.",
            "`EventIngestionController` (`[Route(\"events\")]` + `[HttpPost(\"ingest\")]`).",
            "See [Dapr sidecar ports](#dapr-sidecar-ports). Dapr reaches the Server through these to read `/dapr/subscribe` and deliver to `/events/ingest`.",
        ]);
        backends.Select(static row => row[0]).ShouldBe(["Redis Stack", "FalkorDB", "Aspire dashboard", "Aspire dashboard OTLP receiver"]);
        backends.Select(static row => row[1]).ShouldBe(["`6379`", "`6380`", "`18888`", "`18889`"]);
    }

    [Fact]
    public void DeploymentConfigurationDoc_IsTiedToEventIngestionConstants()
    {
        // Bidirectional tie: a code-side rename of either constant OR a doc-side rename fails the build.
        EventIngestionController.TopicEnvVar.ShouldBe("MEMORIES_EVENTSTORE_TOPIC", "Topic env var constant must not drift.");
        EventIngestionController.PubSubName.ShouldBe("pubsub", "Pub/sub component name constant must not drift.");

        IReadOnlyList<string> topicRow = ReadContractRow("Topic env var", "Pub/sub event-intake deployment surface");
        topicRow[1].ShouldBe($"`{EventIngestionController.TopicEnvVar}`");
        IReadOnlyList<string> pubSubRow = ReadContractRow("Pub/sub component name", "Pub/sub event-intake deployment surface");
        pubSubRow[1].ShouldBe($"`{EventIngestionController.PubSubName}`");
    }

    [Fact]
    public void DeploymentConfigurationDoc_IsTiedToRoutingOptionDefaults()
    {
        // The doc states the pub/sub component name `pubsub` is agreed across three sources:
        // EventIngestionController.PubSubName (tied above), the pubsub.yaml metadata.name (tied in
        // DeploymentConfigurationDoc_LiteralsMatchAuthoritativeSourceFiles), and the runtime-bindable
        // TenantEventRoutingOptions.PubSubName default. Tie the third source here so a drift in the
        // options default (which the TenantEventRoutingOptionsValidator forces config to match) also
        // fails the build.
        new TenantEventRoutingOptions().PubSubName.ShouldBe("pubsub", "TenantEventRoutingOptions.PubSubName default must not drift from the documented component name.");

        ReadContractRow("Pub/sub component name", "Pub/sub event-intake deployment surface")[1]
            .ShouldBe($"`{new TenantEventRoutingOptions().PubSubName}`");
    }

    [Fact]
    public void DeploymentConfigurationDoc_TiesServerAppIdDefaultToResolveDaprAppId()
    {
        // AC2 headline reconciliation: the real Server Dapr app-id default is `memories` (ResolveDaprAppId),
        // NOT the architecture-doc projection `memories-server`. Tie the default to its authoritative source
        // text so a code-side rename of the default fails the build, and keep the reconciliation note
        // (mentioning the `memories-server` projection) from being silently dropped from the doc.
        string appHost = ReadRepoFile("src", "Hexalith.Memories.AppHost", "Program.cs");
        ShouldHaveExactSourceLine(appHost, "return \"memories\";", "ResolveDaprAppId in AppHost/Program.cs must keep returning the documented default app-id 'memories'.");

        string sidecarSection = new MarkdownContractDocument(ReadDoc()).GetSection("Dapr sidecar ports");
        sidecarSection.ShouldContain("`memories`", Case.Sensitive, "The Dapr sidecar section must document the real Server app-id default.");
        sidecarSection.ShouldContain("memories-server", Case.Sensitive, "The Dapr sidecar section must retain the architecture-projection reconciliation note.");
    }

    [Fact]
    public void DeploymentConfigurationDoc_LiteralsMatchAuthoritativeSourceFiles()
    {
        // OTLP exporter env gate (no C# constant — assert the literal in both the source and the doc).
        string serviceDefaults = ReadRepoFile("src", "Hexalith.Memories.ServiceDefaults", "Extensions.cs");
        IReadOnlyList<string> otlpRow = ReadContractRow("`OTEL_EXPORTER_OTLP_ENDPOINT`", "OTLP telemetry export");
        ShouldAppearInBoth("OTEL_EXPORTER_OTLP_ENDPOINT", otlpRow, serviceDefaults, "src/Hexalith.Memories.ServiceDefaults/Extensions.cs");

        // The doc names the hosted service that logs the Production-empty-endpoint warning; tie that name to
        // its authoritative source so a rename of the warning service does not silently rot the doc.
        ShouldAppearInBoth("OtlpExporterWarningHostedService", otlpRow, serviceDefaults, "src/Hexalith.Memories.ServiceDefaults/Extensions.cs");

        // Dapr sidecar ports, topic value, connection-string keys, MCP app-id, and the app-id override var.
        string appHost = ReadRepoFile("src", "Hexalith.Memories.AppHost", "Program.cs");
        IReadOnlyList<string> serverSidecar = ReadContractRow("Memories Server", "Dapr sidecar ports");
        IReadOnlyList<string> mcpSidecar = ReadContractRow("Memories MCP", "Dapr sidecar ports");
        ShouldAppearInBoth("3500", serverSidecar, appHost, "src/Hexalith.Memories.AppHost/Program.cs");
        ShouldAppearInBoth("50001", serverSidecar, appHost, "src/Hexalith.Memories.AppHost/Program.cs");
        ShouldAppearInBoth("3600", mcpSidecar, appHost, "src/Hexalith.Memories.AppHost/Program.cs");
        ShouldAppearInBoth("50101", mcpSidecar, appHost, "src/Hexalith.Memories.AppHost/Program.cs");
        ShouldAppearInBoth("memories-mcp", mcpSidecar, appHost, "src/Hexalith.Memories.AppHost/Program.cs");
        ShouldAppearInBoth("MEMORIES_DAPR_APP_ID", serverSidecar, appHost, "src/Hexalith.Memories.AppHost/Program.cs");

        ShouldHaveExactSourceLine(appHost, "appId: daprAppId,", "The Server sidecar must use the resolved documented Dapr app-id.");
        ShouldHaveExactSourceLine(appHost, "httpPort: 3500,", "The Server sidecar HTTP port assignment must match its contract row.");
        ShouldHaveExactSourceLine(appHost, "grpcPort: 50001,", "The Server sidecar gRPC port assignment must match its contract row.");
        ShouldHaveExactSourceLine(appHost, "appId: \"memories-mcp\",", "The MCP sidecar app-id assignment must match its contract row.");
        ShouldHaveExactSourceLine(appHost, "httpPort: 3600,", "The MCP sidecar HTTP port assignment must match its contract row.");
        ShouldHaveExactSourceLine(appHost, "grpcPort: 50101,", "The MCP sidecar gRPC port assignment must match its contract row.");
        ShouldHaveExactSourceLine(appHost, "server = server.WithEnvironment(\"MEMORIES_EVENTSTORE_TOPIC\", \"memories-events\");", "The AppHost topic assignment must match the topic-default contract row.");

        ShouldAppearInBoth("memories-events", ReadContractRow("`MEMORIES_EVENTSTORE_TOPIC`", "Required runtime environment"), appHost, "src/Hexalith.Memories.AppHost/Program.cs");
        ShouldAppearInBoth("ConnectionStrings__redis", ReadContractRow("`ConnectionStrings__redis`", "Required runtime environment"), appHost, "src/Hexalith.Memories.AppHost/Program.cs");
        ShouldAppearInBoth("ConnectionStrings__falkordb", ReadContractRow("`ConnectionStrings__falkordb`", "Required runtime environment"), appHost, "src/Hexalith.Memories.AppHost/Program.cs");
        ShouldAppearInBoth("MEMORIES_DAPR_APP_ID", ReadContractRow("`MEMORIES_DAPR_APP_ID`", "Required runtime environment"), appHost, "src/Hexalith.Memories.AppHost/Program.cs");

        // Pub/sub redis interpolation variables (YAML-only — no C# constant).
        string pubSubComponent = ReadRepoFile("deploy", "dapr", "components", "pubsub.yaml");
        ShouldAppearInBoth("PUBSUB_REDIS_HOST", ReadContractRow("`PUBSUB_REDIS_HOST`", "Required runtime environment"), pubSubComponent, "deploy/dapr/components/pubsub.yaml");
        ShouldAppearInBoth("PUBSUB_REDIS_PASSWORD", ReadContractRow("`PUBSUB_REDIS_PASSWORD`", "Required runtime environment"), pubSubComponent, "deploy/dapr/components/pubsub.yaml");

        // Pub/sub component name: the doc references the yaml metadata.name; assert it is still `pubsub` so a
        // rename of the Dapr component (which would break every consumer subscription) fails the build.
        ShouldHaveExactSourceLine(pubSubComponent, "name: pubsub", "The Dapr pub/sub component metadata.name must remain 'pubsub' to match its contract row.");

        // Source→tenant routing option name; the option itself is tied to code here.
        string routingOptions = ReadRepoFile("src", "Hexalith.Memories.EventStore", "TenantEventRoutingOptions.cs");
        IReadOnlyList<string> routingRow = ReadContractRow("Source→tenant routing key", "Pub/sub event-intake deployment surface");
        ShouldAppearInBoth("SourceToTenantMap", routingRow, routingOptions, "src/Hexalith.Memories.EventStore/TenantEventRoutingOptions.cs");

        // The colon-joined config-section prefix the routing options bind from: tied to the binding call so
        // the documented `EventStoreIntegration:Routing:*` keys cannot diverge from the section code reads.
        string integrationExtensions = ReadRepoFile("src", "Hexalith.Memories.EventStore", "EventStoreIntegrationServiceCollectionExtensions.cs");
        ShouldAppearInBoth("EventStoreIntegration:Routing", routingRow, integrationExtensions, "src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs");

        // Ingest delivery route attributes: the doc derives `POST /events/ingest` from these attributes, so
        // tie the attribute strings to the controller source to catch a route rename.
        string ingestionController = ReadRepoFile("src", "Hexalith.Memories.EventStore", "EventIngestionController.cs");
        IReadOnlyList<string> deliveryRow = ReadContractRow("Delivery route", "Pub/sub event-intake deployment surface");
        ShouldAppearInBoth("[Route(\"events\")]", deliveryRow, ingestionController, "src/Hexalith.Memories.EventStore/EventIngestionController.cs");
        ShouldAppearInBoth("[HttpPost(\"ingest\")]", deliveryRow, ingestionController, "src/Hexalith.Memories.EventStore/EventIngestionController.cs");
    }

    [Fact]
    public void DeploymentConfigurationDoc_ContainsNoLeakedToolCallMarkup()
    {
        IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(ReadDoc());

        diagnostics.ShouldBeEmpty($"{DocRelativePath} contains leaked tool-call markup: {string.Join("; ", diagnostics)}");
    }

    private static void ShouldAppearInBoth(string literal, IReadOnlyList<string> contractRow, string source, string sourceName)
    {
        source.ShouldContain(literal, Case.Sensitive, $"'{literal}' must remain in its authoritative source {sourceName}.");
        string.Join('\n', contractRow).ShouldContain(literal, Case.Sensitive, $"'{literal}' must remain in its authoritative table row in {DocRelativePath}; reconcile it with {sourceName}.");
    }

    private static void ShouldHaveExactSourceLine(string source, string expectedLine, string message)
        => source.Replace("\r\n", "\n", System.StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Count(line => string.Equals(line.Trim(), expectedLine, System.StringComparison.Ordinal))
            .ShouldBe(1, message);

    private static string ReadDoc() => File.ReadAllText(ResolveDocPath());

    private static IReadOnlyList<string> ReadContractRow(string key, string heading)
        => new MarkdownContractDocument(ReadDoc()).GetTableRows(heading)
            .Single(row => string.Equals(row[0], key, System.StringComparison.Ordinal));

    private static string ResolveDocPath()
        => Path.Combine(ResolveRepoRoot(), "docs", "operations", "deployment-configuration.md");

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
