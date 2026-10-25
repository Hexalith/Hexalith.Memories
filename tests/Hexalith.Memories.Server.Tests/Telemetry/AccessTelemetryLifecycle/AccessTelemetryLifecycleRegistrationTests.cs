// <copyright file="AccessTelemetryLifecycleRegistrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle;

using System.Globalization;
using System.Text.Json;

using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;
using Hexalith.Memories.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>Verifies the registered lifecycle provider and qualification runner use the production composition.</summary>
public sealed class AccessTelemetryLifecycleRegistrationTests
{
    [Fact]
    public void RegisteredProvider_AccountsForTheActualGeneratedEmitter()
    {
        WebApplicationBuilder builder = CreateBuilder();
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

    [Fact]
    public async Task RegisteredRunner_IsStatusAndBootstrapAwareInTheRealComposition()
    {
        string gatePath = Path.GetTempFileName();
        File.WriteAllText(
            gatePath,
            "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" + AccessTelemetryQualificationGate.ApprovedProfileSha256 +
            "\",\"expiresUtcMs\":" + DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) + "}");
        try
        {
            WebApplicationBuilder builder = CreateBuilder();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["AccessTelemetryQualification:GatePath"] = gatePath });

            // Only the default console providers are removed; the lifecycle provider is registered later by the composition.
            builder.Logging.ClearProviders();
            builder.AddServiceDefaults(configureRedisInstrumentation: false);
            builder.AddMemoriesServerServices();

            // The hosted bootstrap is not started, so no sanitizer is published and every record is rejected.
            using ServiceProvider services = builder.Services.BuildServiceProvider();
            var runner = services.GetRequiredService<AccessTelemetryQualificationWorkloadRunner>();

            AccessTelemetryQualificationWorkloadResult result = await runner.RunAsync(
                "composition-run",
                "cohort-a",
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                TestContext.Current.CancellationToken);

            result.Attempted.ShouldBe(125);
            result.Rejected.ShouldBe(125);

            // Serialize with the composed HTTP JSON options the mapped endpoint uses: the selected verifier requires every
            // breakdown and bootstrap field, including explicit nulls, so an ignore-null policy would fail correct runs.
            JsonSerializerOptions http = services.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions;
            JsonElement breakdown = JsonSerializer.SerializeToElement(result, http).GetProperty("rejectionBreakdown");
            breakdown.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ShouldBe(
                ["bootstrap", "lifecycleHealth", "lifecycleReason", "sanitationFailed", "sanitizerUnavailable", "sanitizerUnavailableBootstrapStages", "sanitizerUnavailableLifecycleReasons"]);
            JsonElement bootstrap = breakdown.GetProperty("bootstrap");
            bootstrap.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ShouldBe(
                ["attempts", "failureKind", "failureStage", "firstPublishedUtcMs", "stage"]);
            foreach (string name in new[] { "failureKind", "failureStage", "firstPublishedUtcMs" })
            {
                bootstrap.GetProperty(name).ValueKind.ShouldBe(JsonValueKind.Null, name);
            }

            // A runner built without the status or bootstrap progress would report "Unobserved" here.
            breakdown.GetProperty("lifecycleHealth").GetString().ShouldBe("Degraded");
            breakdown.GetProperty("lifecycleReason").GetString().ShouldBe("RemoteValidationPending");
            breakdown.GetProperty("bootstrap").GetProperty("stage").GetString().ShouldBe("NotStarted");
            breakdown.GetProperty("bootstrap").GetProperty("attempts").GetInt64().ShouldBe(0);

            // The registered logger provider also receives the bootstrap progress for per-rejection stage counts.
            breakdown.GetProperty("sanitizerUnavailableBootstrapStages").GetProperty("NotStarted").GetInt64().ShouldBe(125);
            breakdown.GetProperty("sanitizerUnavailableLifecycleReasons").GetProperty("RemoteValidationPending").GetInt64().ShouldBe(125);
        }
        finally
        {
            File.Delete(gatePath);
        }
    }

    private static WebApplicationBuilder CreateBuilder()
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
        return builder;
    }
}
