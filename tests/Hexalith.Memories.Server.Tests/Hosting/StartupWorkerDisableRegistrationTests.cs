// <copyright file="StartupWorkerDisableRegistrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Hosting;

using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Tests.Ingestion;
using Hexalith.Memories.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Shouldly;

[Collection(EmbeddingProviderDefaultsStateCollection.Name)]
public sealed class StartupWorkerDisableRegistrationTests
{
    [Theory]
    [InlineData(null, null, true, true)]
    [InlineData("false", "false", false, false)]
    [InlineData("false", null, false, true)]
    [InlineData(null, "false", true, false)]
    public void Startup_worker_switches_control_registration(
        string? orphanEnabled,
        string? backfillEnabled,
        bool expectOrphan,
        bool expectBackfill)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OrphanSemanticIndex:Enabled"] = orphanEnabled,
            ["IsStubBackfill:Enabled"] = backfillEnabled,
        });
        builder.AddServiceDefaults(configureRedisInstrumentation: false);
        builder.AddMemoriesServerServices();

        IsRegistered<OrphanSemanticIndexReconciler>(builder.Services).ShouldBe(expectOrphan);
        IsRegistered<IsStubBackfillMigrationHostedService>(builder.Services).ShouldBe(expectBackfill);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void Retry_worker_switch_binds_from_natural_language_section(string? configured, bool expected)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        if (configured is not null)
        {
            // A null in-memory value is a present key that binds to false; omission keeps the default.
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NaturalLanguage:RetryWorkerEnabled"] = configured,
            });
        }

        builder.AddServiceDefaults(configureRedisInstrumentation: false);
        builder.AddMemoriesServerServices();
        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        // Apply the registered configuration without running the production options validators.
        var options = new NaturalLanguageDescriptionOptions();
        foreach (IConfigureOptions<NaturalLanguageDescriptionOptions> configure
            in provider.GetServices<IConfigureOptions<NaturalLanguageDescriptionOptions>>())
        {
            configure.Configure(options);
        }

        options.RetryWorkerEnabled.ShouldBe(expected);
    }

    private static bool IsRegistered<T>(IServiceCollection services) where T : IHostedService
        => services.Any(descriptor => descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(T));
}
