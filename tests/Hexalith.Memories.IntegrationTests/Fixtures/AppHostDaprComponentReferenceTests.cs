// <copyright file="AppHostDaprComponentReferenceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Memories.TestHelpers.Process;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>
/// Builds, but never starts, the AppHost model and verifies the Dapr component wiring of every sidecar.
/// The CommunityToolkit Dapr lifecycle hook reads component references only from the sidecar resource,
/// so a reference left on a project resource would be silently ignored.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AppHostDaprComponentReferenceTests
{
    private static readonly Dictionary<string, string[]> _expectedSidecarComponents = new(StringComparer.Ordinal)
    {
        ["eventstore"] = ["pubsub", "statestore"],
        ["memories"] = ["access-telemetry-config", "access-telemetry-secrets", "llm", "pubsub", "secretstore", "statestore"],
        ["memories-access-telemetry"] = ["access-telemetry-config", "access-telemetry-secrets", "access-telemetry-store"],
        ["memories-access-telemetry-clock"] = ["access-telemetry-secrets"],
        ["memories-mcp"] = [],
    };

    [Fact]
    public async Task DaprComponentReferences_AreDeclaredOnlyOnTheExpectedSidecars()
    {
        using EnvVarScope aspNetCoreEnvironment = EnvVarScope.Set("ASPNETCORE_ENVIRONMENT", "Development");
        using EnvVarScope dotNetEnvironment = EnvVarScope.Set("DOTNET_ENVIRONMENT", "Development");
        using EnvVarScope enableKeycloak = EnvVarScope.Set("EnableKeycloak", "false");
        using EnvVarScope randomizePorts = EnvVarScope.Set("MEMORIES_ASPIRE_RANDOMIZE_PROJECT_PORTS", "true");

        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Memories_AppHost>();
        await using DistributedApplication app = await builder.BuildAsync();
        DistributedApplicationModel model = app.Services.GetRequiredService<DistributedApplicationModel>();

        IResource[] sidecarOwners = [.. model.Resources.Where(static r => r.Annotations.OfType<DaprSidecarAnnotation>().Any())];
        sidecarOwners.Select(static r => r.Name).Order(StringComparer.Ordinal)
            .ShouldBe(_expectedSidecarComponents.Keys.Order(StringComparer.Ordinal));

        foreach (IResource owner in sidecarOwners)
        {
            IDaprSidecarResource sidecar = owner.Annotations.OfType<DaprSidecarAnnotation>().ShouldHaveSingleItem().Sidecar;
            string[] componentNames = [.. sidecar.Annotations
                .OfType<DaprComponentReferenceAnnotation>()
                .Select(static a => a.Component.Name)
                .Order(StringComparer.Ordinal)];
            componentNames.ShouldBe(
                _expectedSidecarComponents[owner.Name],
                $"Dapr sidecar of '{owner.Name}' must reference exactly its owned components.");
        }

        model.Resources
            .Where(static r => r is not IDaprSidecarResource)
            .Where(static r => r.Annotations.OfType<DaprComponentReferenceAnnotation>().Any())
            .Select(static r => r.Name)
            .ShouldBeEmpty("Only sidecar resources may carry Dapr component references.");
    }
}
