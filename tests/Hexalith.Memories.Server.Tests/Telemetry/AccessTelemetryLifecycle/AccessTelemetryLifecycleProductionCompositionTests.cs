// <copyright file="AccessTelemetryLifecycleProductionCompositionTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;
using Hexalith.Memories.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>
/// Epic 27 retro item 64: proves through the real Production composition that a disabled, unconvertible or
/// unproven lifecycle registers none of its provider, queue, HTTP clients, Dapr clients or hosted services.
/// </summary>
public sealed class AccessTelemetryLifecycleProductionCompositionTests
{
    private const string EnabledKey = "AccessTelemetryLifecycle:Enabled";

    private const string DisabledDescription = "Access telemetry lifecycle is disabled by configuration.";

    private static readonly Type[] LifecycleHostedServices =
    [
        typeof(AccessTelemetryLifecycleBootstrapService),
        typeof(AccessTelemetryDeliveryWorker),
        typeof(AccessTelemetryHeartbeatWorker),
    ];

    private static readonly string[] EveryLifecycleRegistration =
    [
        "AccessTelemetryDeliveryWorker",
        "AccessTelemetryHeartbeatWorker",
        "AccessTelemetryLifecycleBootstrapService",
        "AccessTelemetryLifecycleLoggerProvider",
        "BoundedAccessTelemetryQueue",
        "HttpClient:access-telemetry-clock",
        "HttpClient:access-telemetry-lifecycle",
        "IAccessTelemetryClockEvidenceProvider",
        "IAccessTelemetryDeliveryClient",
        "IAccessTelemetryHeartbeatClient",
    ];

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("False")]
    public async Task Disabled_RegistersNoLifecycleServiceAndReportsDisabledHealth(string? enabled)
    {
        Dictionary<string, string?> settings = enabled is null ? [] : new() { [EnabledKey] = enabled };

        WebApplicationBuilder builder = ComposeProduction(settings);

        if (enabled is null)
        {
            builder.Configuration[EnabledKey].ShouldBeNull("The absent case needs no inherited lifecycle switch.");
        }

        LifecycleRegistrations(builder.Services).ShouldBeEmpty();
        builder.Services.Count(IsEnabledValueWarning).ShouldBe(0);
        using ServiceProvider services = builder.Services.BuildServiceProvider();
        services.GetRequiredService<AccessTelemetryOptions>().Enabled.ShouldBeFalse();
        services.GetServices<ILoggerProvider>().ShouldNotContain(provider => provider is AccessTelemetryLifecycleLoggerProvider);
        HealthCheckRegistration registration = GetHealthCheckRegistration(services);
        registration.Tags.ToArray().ShouldBe(["lifecycle"]);

        HealthCheckResult result = await CheckHealthAsync(services, registration);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe(DisabledDescription);
        result.Data["cause"].ShouldBe("AccessTelemetryLifecycle:Enabled is false, absent or not a Boolean, so no lifecycle service runs.");
    }

    [Fact]
    public async Task EnabledWithUnprovenProfileHash_RegistersNoLifecycleServiceAndFailsClosed()
    {
        WebApplicationBuilder builder = ComposeProduction(EnabledProductionSettings("unproven"));

        LifecycleRegistrations(builder.Services).ShouldBeEmpty();
        using ServiceProvider services = builder.Services.BuildServiceProvider();
        services.GetServices<ILoggerProvider>().ShouldNotContain(provider => provider is AccessTelemetryLifecycleLoggerProvider);
        AccessTelemetryLifecycleStatus status = services.GetRequiredService<AccessTelemetryLifecycleStatus>();
        status.Current.Health.ShouldBe(AccessTelemetryHealthState.Unhealthy);
        status.Current.Reason.ShouldBe(AccessTelemetryReason.ConfigurationInvalid);

        // Terminal: a later poll cannot flip the invalid configuration back to healthy.
        status.Publish(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None);
        HealthCheckResult result = await CheckHealthAsync(services, GetHealthCheckRegistration(services));

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Access telemetry lifecycle is fail-closed.");
    }

    [Fact]
    public void EnabledWithExactProfileHash_RegistersEveryLifecycleService()
    {
        // Positive control: the same composition with a well-formed hash proves the negative cases above
        // observe real registrations rather than names that never match.
        WebApplicationBuilder builder = ComposeProduction(EnabledProductionSettings(new string('a', 64)));

        LifecycleRegistrations(builder.Services).ShouldBe(EveryLifecycleRegistration);
        builder.Services.Count(IsEnabledValueWarning).ShouldBe(0);
        using ServiceProvider services = builder.Services.BuildServiceProvider();
        services.GetServices<ILoggerProvider>().ShouldContain(provider => provider is AccessTelemetryLifecycleLoggerProvider);
        services.GetRequiredService<AccessTelemetryOptions>().Enabled.ShouldBeTrue();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("yes")]
    [InlineData("enabled-canary-7f3a")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UnconvertibleEnabled_IsDisabledAndWarnsOnceWithoutTheValue(string value)
    {
        var capture = new CapturingAuditLoggerProvider();

        WebApplicationBuilder builder = ComposeProduction(new() { [EnabledKey] = value });
        builder.Logging.AddProvider(capture);

        // A blank value must reach the composition as present, not be hidden as absent.
        builder.Configuration[EnabledKey].ShouldBe(value);
        LifecycleRegistrations(builder.Services).ShouldBeEmpty();
        ServiceDescriptor warning = builder.Services.Single(IsEnabledValueWarning);
        await using WebApplication app = builder.Build();
        var hostedService = (IHostedService)ActivatorUtilities.CreateInstance(app.Services, warning.ImplementationType!);
        await hostedService.StartAsync(TestContext.Current.CancellationToken);

        AuditLogCapture entry = capture.Captures
            .Where(static capture => capture.Category == typeof(AccessTelemetryLifecycleEnabledValueWarning).FullName)
            .ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Message.ShouldContain("AccessTelemetryLifecycle:Enabled", Case.Sensitive);
        entry.Message.ShouldContain("AccessTelemetryLifecycle__Enabled", Case.Sensitive);

        // Every string contains "", so the not-echoed check is meaningful only for a non-blank value.
        if (!string.IsNullOrWhiteSpace(value))
        {
            entry.Message.ShouldNotContain(value, Case.Insensitive);
        }

        HealthCheckResult result = await CheckHealthAsync(app.Services, GetHealthCheckRegistration(app.Services));
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe(DisabledDescription);
    }

    [Fact]
    public void UnconvertibleEnabledWithMalformedSibling_ComposesDisabledWithWarning()
    {
        // Retro B11: binding the whole section first crashed a disabled pod on any malformed sibling.
        WebApplicationBuilder builder = ComposeProduction(new()
        {
            [EnabledKey] = "0",
            ["AccessTelemetryLifecycle:Retention"] = "not-a-timespan",
        });

        LifecycleRegistrations(builder.Services).ShouldBeEmpty();
        builder.Services.Count(IsEnabledValueWarning).ShouldBe(1);
    }

    [Fact]
    public void MalformedValueWhileDisabled_IsIgnoredWithoutWarning()
    {
        WebApplicationBuilder builder = ComposeProduction(new()
        {
            [EnabledKey] = "false",
            ["AccessTelemetryLifecycle:Retention"] = "not-a-timespan",
        });

        LifecycleRegistrations(builder.Services).ShouldBeEmpty();
        builder.Services.Count(IsEnabledValueWarning).ShouldBe(0);
        using ServiceProvider services = builder.Services.BuildServiceProvider();
        services.GetRequiredService<AccessTelemetryOptions>().ShouldBe(new AccessTelemetryOptions());
    }

    [Fact]
    public void MalformedValueWhileEnabled_FailsCompositionFast()
    {
        Dictionary<string, string?> settings = EnabledProductionSettings(new string('a', 64));
        settings["AccessTelemetryLifecycle:Retention"] = "not-a-timespan";

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => ComposeProduction(settings));

        exception.Message.ShouldContain("AccessTelemetryLifecycle:Retention", Case.Sensitive);
    }

    private static WebApplicationBuilder ComposeProduction(Dictionary<string, string?> settings)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Production",
        });
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Logging.ClearProviders();
        builder.AddServiceDefaults(configureRedisInstrumentation: false);
        builder.AddMemoriesServerServices();
        return builder;
    }

    private static Dictionary<string, string?> EnabledProductionSettings(string componentProfileHash) => new()
    {
        [EnabledKey] = "true",
        ["AccessTelemetryLifecycle:RetentionSource"] = nameof(RetentionConfigurationSource.DaprConfiguration),
        ["AccessTelemetryLifecycle:DeploymentId"] = "production-composition",
        ["AccessTelemetryLifecycle:ConfigurationEpoch"] = "01J00000000000000000000000",
        ["AccessTelemetryLifecycle:ComponentProfileHash"] = componentProfileHash,
        ["AccessTelemetryLifecycle:MarkerKeyReference"] = "access-telemetry-marker-key",
        ["AccessTelemetryLifecycle:MarkerKeyGeneration"] = "production-key-1",
        ["AccessTelemetryLifecycle:AttestationVerificationKey"] = "offline-composition-test",
        ["AccessTelemetryLifecycle:CapacityEvidenceId"] = "production-composition",
        ["AccessTelemetryLifecycle:PhysicalReclamationEvidenceId"] = "production-composition",
        ["AccessTelemetryLifecycle:PhysicalReclamationReporterImageDigest"] = new string('d', 64),
    };

    /// <summary>Names every lifecycle-only registration present, in ordinal order, once per descriptor.</summary>
    private static string[] LifecycleRegistrations(IServiceCollection services)
    {
        List<string> found = [];
        foreach (ServiceDescriptor descriptor in services)
        {
            // Implementation members throw on keyed descriptors, so keyed ones are matched by key only.
            if (descriptor.IsKeyedService)
            {
                if (descriptor.ServiceType == typeof(HttpClient) &&
                    descriptor.ServiceKey is "access-telemetry-lifecycle" or "access-telemetry-clock")
                {
                    found.Add($"HttpClient:{descriptor.ServiceKey}");
                }

                continue;
            }

            if (descriptor.ServiceType == typeof(AccessTelemetryLifecycleLoggerProvider) ||
                descriptor.ServiceType == typeof(BoundedAccessTelemetryQueue) ||
                descriptor.ServiceType == typeof(IAccessTelemetryClockEvidenceProvider) ||
                descriptor.ServiceType == typeof(IAccessTelemetryDeliveryClient) ||
                descriptor.ServiceType == typeof(IAccessTelemetryHeartbeatClient))
            {
                found.Add(descriptor.ServiceType.Name);
            }
            else if (descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType is Type hostedService &&
                LifecycleHostedServices.Contains(hostedService))
            {
                found.Add(hostedService.Name);
            }
        }

        return [.. found.Order(StringComparer.Ordinal)];
    }

    private static bool IsEnabledValueWarning(ServiceDescriptor descriptor)
        => !descriptor.IsKeyedService &&
            descriptor.ServiceType == typeof(IHostedService) &&
            descriptor.ImplementationType == typeof(AccessTelemetryLifecycleEnabledValueWarning);

    private static HealthCheckRegistration GetHealthCheckRegistration(IServiceProvider services)
        => services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations
            .Single(static registration => registration.Name == "access-telemetry-lifecycle");

    private static Task<HealthCheckResult> CheckHealthAsync(IServiceProvider services, HealthCheckRegistration registration)
        => registration.Factory(services).CheckHealthAsync(
            new HealthCheckContext { Registration = registration },
            TestContext.Current.CancellationToken);
}
