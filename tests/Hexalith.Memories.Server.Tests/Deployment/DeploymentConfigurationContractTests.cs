// <copyright file="DeploymentConfigurationContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.IO;

using Hexalith.Memories.EventStore;

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
    public void DeploymentConfigurationDoc_ContainsAllCanonicalLiterals()
    {
        string content = ReadDoc();

        // Every canonical literal a downstream operator fills must remain documented (Case.Sensitive so a
        // case-only drift such as `pubSub` or `Pubsub` is also caught).
        string[] canonicalLiterals =
        [
            "OTEL_EXPORTER_OTLP_ENDPOINT",
            "3500",
            "50001",
            "3600",
            "50101",
            "PUBSUB_REDIS_HOST",
            "PUBSUB_REDIS_PASSWORD",
            "MEMORIES_EVENTSTORE_TOPIC",
            "memories-events",
            "ConnectionStrings__redis",
            "ConnectionStrings__falkordb",
            "pubsub",
            "EventStoreIntegration:Routing:SourceToTenantMap",
            "/dapr/subscribe",
            "POST /events/ingest",
            "6379",
            "6380",
            "18888",
            "18889",
            "MEMORIES_DAPR_APP_ID",
            "memories-mcp",
        ];

        foreach (string literal in canonicalLiterals)
        {
            content.ShouldContain(literal, Case.Sensitive, $"Canonical deploy-config literal '{literal}' must remain documented in {DocRelativePath}.");
        }
    }

    [Fact]
    public void DeploymentConfigurationDoc_IsTiedToEventIngestionConstants()
    {
        // Bidirectional tie: a code-side rename of either constant OR a doc-side rename fails the build.
        EventIngestionController.TopicEnvVar.ShouldBe("MEMORIES_EVENTSTORE_TOPIC", "Topic env var constant must not drift.");
        EventIngestionController.PubSubName.ShouldBe("pubsub", "Pub/sub component name constant must not drift.");

        string content = ReadDoc();
        content.ShouldContain(EventIngestionController.TopicEnvVar, Case.Sensitive, $"Doc must publish the {nameof(EventIngestionController.TopicEnvVar)} constant value.");
        content.ShouldContain(EventIngestionController.PubSubName, Case.Sensitive, $"Doc must publish the {nameof(EventIngestionController.PubSubName)} constant value.");
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

        string content = ReadDoc();
        content.ShouldContain(new TenantEventRoutingOptions().PubSubName, Case.Sensitive, "Doc must publish the TenantEventRoutingOptions.PubSubName default value.");
    }

    [Fact]
    public void DeploymentConfigurationDoc_TiesServerAppIdDefaultToResolveDaprAppId()
    {
        // AC2 headline reconciliation: the real Server Dapr app-id default is `memories` (ResolveDaprAppId),
        // NOT the architecture-doc projection `memories-server`. Tie the default to its authoritative source
        // text so a code-side rename of the default fails the build, and keep the reconciliation note
        // (mentioning the `memories-server` projection) from being silently dropped from the doc.
        string appHost = ReadRepoFile("src", "Hexalith.Memories.AppHost", "Program.cs");
        appHost.ShouldContain("return \"memories\";", Case.Sensitive, "ResolveDaprAppId in AppHost/Program.cs must keep returning the documented default app-id 'memories'.");

        string doc = ReadDoc();
        doc.ShouldContain("`memories`", Case.Sensitive, "Doc must document the real Server Dapr app-id default `memories` (the value ResolveDaprAppId emits).");
        doc.ShouldContain("memories-server", Case.Sensitive, "Doc must retain the reconciliation note that the architecture projection `memories-server` differs from the real default.");
    }

    [Fact]
    public void DeploymentConfigurationDoc_LiteralsMatchAuthoritativeSourceFiles()
    {
        string doc = ReadDoc();

        // OTLP exporter env gate (no C# constant — assert the literal in both the source and the doc).
        string serviceDefaults = ReadRepoFile("src", "Hexalith.Memories.ServiceDefaults", "Extensions.cs");
        ShouldAppearInBoth("OTEL_EXPORTER_OTLP_ENDPOINT", doc, serviceDefaults, "src/Hexalith.Memories.ServiceDefaults/Extensions.cs");

        // The doc names the hosted service that logs the Production-empty-endpoint warning; tie that name to
        // its authoritative source so a rename of the warning service does not silently rot the doc.
        ShouldAppearInBoth("OtlpExporterWarningHostedService", doc, serviceDefaults, "src/Hexalith.Memories.ServiceDefaults/Extensions.cs");

        // Dapr sidecar ports, topic value, connection-string keys, MCP app-id, and the app-id override var.
        string appHost = ReadRepoFile("src", "Hexalith.Memories.AppHost", "Program.cs");
        string[] appHostLiterals =
        [
            "3500",
            "50001",
            "3600",
            "50101",
            "memories-events",
            "ConnectionStrings__redis",
            "ConnectionStrings__falkordb",
            "memories-mcp",
            "MEMORIES_DAPR_APP_ID",
        ];
        foreach (string literal in appHostLiterals)
        {
            ShouldAppearInBoth(literal, doc, appHost, "src/Hexalith.Memories.AppHost/Program.cs");
        }

        // Pub/sub redis interpolation variables (YAML-only — no C# constant).
        string pubSubComponent = ReadRepoFile("deploy", "dapr", "components", "pubsub.yaml");
        ShouldAppearInBoth("PUBSUB_REDIS_HOST", doc, pubSubComponent, "deploy/dapr/components/pubsub.yaml");
        ShouldAppearInBoth("PUBSUB_REDIS_PASSWORD", doc, pubSubComponent, "deploy/dapr/components/pubsub.yaml");

        // Pub/sub component name: the doc references the yaml metadata.name; assert it is still `pubsub` so a
        // rename of the Dapr component (which would break every consumer subscription) fails the build.
        pubSubComponent.ShouldContain("name: pubsub", Case.Sensitive, "The Dapr pub/sub component metadata.name in deploy/dapr/components/pubsub.yaml must remain 'pubsub' to match the documented component name.");

        // Source→tenant routing option name; the option itself is tied to code here.
        string routingOptions = ReadRepoFile("src", "Hexalith.Memories.EventStore", "TenantEventRoutingOptions.cs");
        ShouldAppearInBoth("SourceToTenantMap", doc, routingOptions, "src/Hexalith.Memories.EventStore/TenantEventRoutingOptions.cs");

        // The colon-joined config-section prefix the routing options bind from: tied to the binding call so
        // the documented `EventStoreIntegration:Routing:*` keys cannot diverge from the section code reads.
        string integrationExtensions = ReadRepoFile("src", "Hexalith.Memories.EventStore", "EventStoreIntegrationServiceCollectionExtensions.cs");
        ShouldAppearInBoth("EventStoreIntegration:Routing", doc, integrationExtensions, "src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs");

        // Ingest delivery route attributes: the doc derives `POST /events/ingest` from these attributes, so
        // tie the attribute strings to the controller source to catch a route rename.
        string ingestionController = ReadRepoFile("src", "Hexalith.Memories.EventStore", "EventIngestionController.cs");
        ShouldAppearInBoth("[Route(\"events\")]", doc, ingestionController, "src/Hexalith.Memories.EventStore/EventIngestionController.cs");
        ShouldAppearInBoth("[HttpPost(\"ingest\")]", doc, ingestionController, "src/Hexalith.Memories.EventStore/EventIngestionController.cs");
    }

    private static void ShouldAppearInBoth(string literal, string doc, string source, string sourceName)
    {
        source.ShouldContain(literal, Case.Sensitive, $"'{literal}' must remain in its authoritative source {sourceName}.");
        doc.ShouldContain(literal, Case.Sensitive, $"'{literal}' is documented in {DocRelativePath} but no longer present in {sourceName} — or vice versa; reconcile the deploy-config contract.");
    }

    private static string ReadDoc() => File.ReadAllText(ResolveDocPath());

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
