// <copyright file="StartupWorkerDisableRegistrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Hosting;

using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.Tests.Ingestion;
using Hexalith.Memories.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

    private static bool IsRegistered<T>(IServiceCollection services) where T : IHostedService
        => services.Any(descriptor => descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(T));
}
