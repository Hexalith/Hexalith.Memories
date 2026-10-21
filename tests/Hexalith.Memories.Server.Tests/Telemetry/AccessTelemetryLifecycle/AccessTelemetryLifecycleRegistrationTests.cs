// <copyright file="AccessTelemetryLifecycleRegistrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;
using Hexalith.Memories.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

/// <summary>Verifies the registered lifecycle provider receives the production generated emitter.</summary>
public sealed class AccessTelemetryLifecycleRegistrationTests
{
    [Fact]
    public void RegisteredProvider_AccountsForTheActualGeneratedEmitter()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Qualification",
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AccessTelemetryLifecycle:Enabled"] = "true",
            ["AccessTelemetryLifecycle:Retention"] = "1.00:00:00",
            ["AccessTelemetryLifecycle:DeploymentId"] = "development",
            ["AccessTelemetryLifecycle:ConfigurationEpoch"] = "01J00000000000000000000000",
            ["AccessTelemetryLifecycle:ComponentProfileHash"] = new string('a', 64),
            ["AccessTelemetryLifecycle:MarkerKeyReference"] = "access-telemetry-marker-key",
            ["AccessTelemetryLifecycle:MarkerKeyGeneration"] = "development-key-1",
            ["AccessTelemetryLifecycle:AttestationVerificationKey"] = "offline-registration-test",
            ["AccessTelemetryLifecycle:CapacityEvidenceId"] = "development",
            ["AccessTelemetryLifecycle:PhysicalReclamationEvidenceId"] = "development",
            ["AccessTelemetryLifecycle:PhysicalReclamationReporterImageDigest"] = new string('d', 64),
        });
        builder.AddServiceDefaults(configureRedisInstrumentation: false);
        builder.AddMemoriesServerServices();
        using ServiceProvider services = builder.Services.BuildServiceProvider();
        services.GetServices<ILoggerProvider>().ShouldContain(provider => provider is AccessTelemetryLifecycleLoggerProvider);
        var accounting = services.GetRequiredService<AccessTelemetryQualificationAccounting>();
        ILogger<AccessTelemetryCategory> logger = services.GetRequiredService<ILogger<AccessTelemetryCategory>>();
        var source = AccessTelemetryLog.CreateEvent(
            7506, "qualification-tenant", AccessTelemetryLog.OperationTenantLifecycle,
            null, "qualification-runner", new Dictionary<string, object?>
            {
                ["operation"] = "tenant-create",
                ["state"] = "completed",
                ["workflowInstanceIdPrefix"] = AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenIdentity,
            }, null, 0, AccessTelemetryLog.OutcomeOk, null, null);

        AccessTelemetryLog.LogTenantLifecycleAccess(logger, source);

        accounting.Current.Attempted.ShouldBe(1);
        accounting.Current.Rejected.ShouldBe(1);
        accounting.ForCorrelation("qualification-0123456789abcdef0123456789abcdef").Rejected.ShouldBe(1);
    }
}
