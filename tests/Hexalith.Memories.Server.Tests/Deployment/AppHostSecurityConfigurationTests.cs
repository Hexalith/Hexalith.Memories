// <copyright file="AppHostSecurityConfigurationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.IO;

using Shouldly;

/// <summary>
/// Drift guards for the approved 2026-06-26 AppHost security-service correction.
/// </summary>
public sealed class AppHostSecurityConfigurationTests
{
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
