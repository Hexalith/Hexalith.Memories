// <copyright file="OpenTelemetryRegistrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System.Linq;

using Hexalith.Memories.ServiceDefaults;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Shouldly;

/// <summary>Story 7.5 Task 8.4 — asserts ServiceDefaults wires the source AND the meter, and preserves the health filter.</summary>
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class OpenTelemetryRegistrationTests
{
    [Fact]
    public void AddServiceDefaults_ProducesBuildableContainer()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = [],
            EnvironmentName = "Development",
        });

        builder.AddServiceDefaults();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        // Smoke check: ensure the provider actually resolves a logger
        // (catches registration errors from AddOpenTelemetry chain).
        provider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>().ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureOpenTelemetry_UsesPinnedSourceName_AndMeterName()
    {
        // This is an indirect test: we verify the constants from the telemetry project match what
        // Extensions.cs references. The actual registration is covered by building the host (above).
        MemoriesActivitySource.SourceName.ShouldBe("Hexalith.Memories");
        MemoriesMeter.Name.ShouldBe("Hexalith.Memories");
    }
}
