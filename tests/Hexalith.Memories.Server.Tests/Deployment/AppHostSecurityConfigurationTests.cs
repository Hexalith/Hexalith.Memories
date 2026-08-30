// <copyright file="AppHostSecurityConfigurationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

using Shouldly;

/// <summary>
/// Drift guards for the approved 2026-06-26 AppHost security-service correction
/// and EventStore 3.100.0 catalog/restore consumption.
/// </summary>
public sealed class AppHostSecurityConfigurationTests
{
    private const string EventStoreCatalogVersion = "3.100.0";
    private const string EventStorePackagePrefix = "Hexalith.EventStore.";

    [Fact]
    public void AppHost_InitializesSharedSecurityResource()
    {
        string program = ReadRepoFile("src", "Hexalith.Memories.AppHost", "Program.cs");

        program.ShouldContain("using Hexalith.EventStore.Aspire;", Case.Sensitive);
        program.ShouldContain("builder.AddHexalithEventStoreSecurity()", Case.Sensitive);
        program.ShouldContain("HexalithEventStoreSecurityResources? security", Case.Sensitive);
        program.ShouldContain("server.WithJwtBearerSecurity(security)", Case.Sensitive);
        program.ShouldContain("mcp.WithJwtBearerSecurity(security)", Case.Sensitive);
        program.ShouldContain("PropagateJwtBearerAuthenticationEnvironment(server)", Case.Sensitive);
    }

    [Fact]
    public void AppHost_ReferencesEventStoreAspireAsHostingHelperOnly()
    {
        string appHostProject = ReadRepoFile(
            "src",
            "Hexalith.Memories.AppHost",
            "Hexalith.Memories.AppHost.csproj");

        appHostProject.ShouldContain(
            "$(HexalithEventStoreRoot)\\src\\Hexalith.EventStore.Aspire\\Hexalith.EventStore.Aspire.csproj",
            Case.Sensitive);
        appHostProject.ShouldContain("IsAspireProjectResource=\"false\"", Case.Sensitive);
    }

    [Fact]
    public void AppHost_ShipsMemoriesOwnedKeycloakRealmImport()
    {
        string realm = ReadRepoFile(
            "src",
            "Hexalith.Memories.AppHost",
            "KeycloakRealms",
            "hexalith-realm.json");

        realm.ShouldContain("\"realm\": \"hexalith\"", Case.Sensitive);
        realm.ShouldContain("\"clientId\": \"hexalith-eventstore\"", Case.Sensitive);
        realm.ShouldContain("\"claim.name\": \"tenants\"", Case.Sensitive);
    }

    [Fact]
    public void AspireIntegrationFixture_KeepsKeycloakDisabledByDefault()
    {
        string fixture = ReadRepoFile(
            "tests",
            "Hexalith.Memories.IntegrationTests",
            "Fixtures",
            "AspireIngestionPipelineFixture.cs");

        fixture.ShouldContain("EnvVarScope.Set(\"EnableKeycloak\", \"false\")", Case.Sensitive);
    }

    [Fact]
    public void BuildsCatalog_PinsEventStoreFamilyAt31000()
    {
        string catalog = ReadRepoFile(
            "references",
            "Hexalith.Builds",
            "Props",
            "Directory.Packages.props");

        catalog.ShouldContain(
            "<HexalithEventStoreVersion Condition=\"'$(HexalithEventStoreVersion)' == ''\">3.100.0</HexalithEventStoreVersion>",
            Case.Sensitive);

        XElement[] eventStoreVersions = [.. XDocument.Parse(catalog)
            .Descendants("PackageVersion")
            .Where(static element =>
                ((string?)element.Attribute("Include"))?.StartsWith(EventStorePackagePrefix, StringComparison.Ordinal) == true)];

        eventStoreVersions.Length.ShouldBeGreaterThan(
            0,
            "Builds catalog must declare Hexalith.EventStore.* PackageVersion rows.");

        string[] packageIds = [.. eventStoreVersions.Select(static element => (string)element.Attribute("Include")!)];
        packageIds.ShouldContain("Hexalith.EventStore.Client");
        packageIds.ShouldContain("Hexalith.EventStore.Aspire");

        foreach (XElement element in eventStoreVersions)
        {
            string packageId = (string)element.Attribute("Include")!;
            ((string?)element.Attribute("Version")).ShouldBe(
                "$(HexalithEventStoreVersion)",
                $"PackageVersion Include=\"{packageId}\" must use Version=\"$(HexalithEventStoreVersion)\".");
        }
    }

    [Fact]
    public void Server_ReferencesEventStoreClientWithoutInlineVersion()
    {
        string serverProject = ReadRepoFile(
            "src",
            "Hexalith.Memories.Server",
            "Hexalith.Memories.Server.csproj");

        serverProject.ShouldContain(
            "<PackageReference Include=\"Hexalith.EventStore.Client\" />",
            Case.Sensitive);
        serverProject.ShouldNotContain(
            "Hexalith.EventStore.Client\" Version=",
            Case.Sensitive);
        serverProject.ShouldNotContain(
            "Hexalith.EventStore.Client\" VersionOverride=",
            Case.Sensitive);
        AssertPackageReferenceHasNoVersionOverride(serverProject, "Hexalith.EventStore.Client");
    }

    [Fact]
    public void AppHost_ReferencesEventStoreAspirePackageWithoutInlineVersion()
    {
        string appHostProject = ReadRepoFile(
            "src",
            "Hexalith.Memories.AppHost",
            "Hexalith.Memories.AppHost.csproj");

        appHostProject.ShouldContain(
            "<PackageReference Include=\"Hexalith.EventStore.Aspire\" Condition=\"'$(HexalithEventStoreFromSource)' != 'true'\" />",
            Case.Sensitive);
        appHostProject.ShouldNotContain(
            "Hexalith.EventStore.Aspire\" Version=",
            Case.Sensitive);
        appHostProject.ShouldNotContain(
            "Hexalith.EventStore.Aspire\" VersionOverride=",
            Case.Sensitive);
        AssertPackageReferenceHasNoVersionOverride(appHostProject, "Hexalith.EventStore.Aspire");
    }

    [Fact]
    public void Server_UsesEventStoreGatewaySubmitContract()
    {
        string commandStore = ReadRepoFile(
            "src",
            "Hexalith.Memories.Server",
            "EventStoreIntegration",
            "EventStoreMemoriesCommandStore.cs");
        string registration = ReadRepoFile(
            "src",
            "Hexalith.Memories.Server",
            "Hosting",
            "MemoriesServerServiceCollectionExtensions.cs");

        commandStore.ShouldContain("using Hexalith.EventStore.Client.Gateway;", Case.Sensitive);
        commandStore.ShouldContain("using Hexalith.EventStore.Contracts.Commands;", Case.Sensitive);
        commandStore.ShouldContain("IEventStoreGatewayClient gatewayClient", Case.Sensitive);
        commandStore.ShouldContain("gatewayClient.SubmitCommandAsync(", Case.Sensitive);
        commandStore.ShouldContain("new SubmitCommandRequest(", Case.Sensitive);
        registration.ShouldContain("builder.Services.AddEventStoreGatewayClient(options =>", Case.Sensitive);
    }

    [Fact]
    public void RootDirectoryPackages_DoesNotOverrideEventStoreVersion()
    {
        string rootCatalog = ReadRepoFile("Directory.Packages.props");

        rootCatalog.ShouldContain(
            "references/Hexalith.Builds/Props/Directory.Packages.props",
            Case.Sensitive);
        rootCatalog.ShouldNotContain("HexalithEventStoreVersion", Case.Sensitive);
        rootCatalog.ShouldNotContain("Hexalith.EventStore", Case.Sensitive);
    }

    [Fact]
    public void ProjectAssets_RestoreEventStorePackagesAtCatalogVersion()
    {
        string serverAssets = ReadRepoFile(
            "src",
            "Hexalith.Memories.Server",
            "obj",
            "project.assets.json");

        using JsonDocument document = JsonDocument.Parse(serverAssets);
        JsonElement libraries = document.RootElement.GetProperty("libraries");
        libraries.TryGetProperty("Hexalith.EventStore.Client/3.100.0", out JsonElement client)
            .ShouldBeTrue("Server restore must include the exact library key Hexalith.EventStore.Client/3.100.0.");
        client.GetProperty("type").GetString().ShouldBe("package");

        foreach (JsonProperty library in libraries.EnumerateObject())
        {
            if (!library.Name.StartsWith(EventStorePackagePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            int slash = library.Name.LastIndexOf('/');
            slash.ShouldBeGreaterThan(
                EventStorePackagePrefix.Length - 1,
                $"Library identity '{library.Name}' must be packageId/version.");
            string version = library.Name[(slash + 1)..];
            version.ShouldBe(
                EventStoreCatalogVersion,
                $"EventStore library '{library.Name}' must restore at exactly {EventStoreCatalogVersion}.");
        }
    }

    [Fact]
    public void AppHost_SelfHostedDaprConfigDoesNotApplyProductionMtlsAcl()
    {
        string localConfig = ReadRepoFile("deploy", "dapr", "config.yaml");
        string kubernetesConfig = ReadRepoFile("deploy", "kubernetes", "base", "dapr", "config.yaml");

        localConfig.ShouldNotContain("accessControl:", Case.Sensitive);
        localConfig.ShouldNotContain("appId: memories-mcp", Case.Sensitive);

        kubernetesConfig.ShouldContain("accessControl:", Case.Sensitive);
        kubernetesConfig.ShouldContain("defaultAction: deny", Case.Sensitive);
        kubernetesConfig.ShouldContain("appId: memories-mcp", Case.Sensitive);
        kubernetesConfig.ShouldContain("namespace: hexalith-memories", Case.Sensitive);
    }

    private static void AssertPackageReferenceHasNoVersionOverride(string projectXml, string packageId)
    {
        foreach (XElement reference in XDocument.Parse(projectXml).Descendants("PackageReference"))
        {
            if ((string?)reference.Attribute("Include") != packageId)
            {
                continue;
            }

            reference.Attribute("VersionOverride").ShouldBeNull(
                $"PackageReference Include=\"{packageId}\" must not set VersionOverride.");
            reference.Elements("VersionOverride").ShouldBeEmpty(
                $"PackageReference Include=\"{packageId}\" must not contain a VersionOverride child.");
        }
    }

    private static string ReadRepoFile(params string[] segments)
    {
        string[] parts = new string[segments.Length + 1];
        parts[0] = ResolveRepoRoot();
        Array.Copy(segments, 0, parts, 1, segments.Length);
        string path = Path.Combine(parts);
        File.Exists(path).ShouldBeTrue($"Source file not found at {path}");
        return File.ReadAllText(path);
    }

    private static string ResolveRepoRoot()
    {
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
}
