// <copyright file="AccessTelemetryQualificationGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Globalization;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Reads the short-lived, file-mounted non-Production qualification gate.</summary>
internal sealed class AccessTelemetryQualificationGate(
    IHostEnvironment environment,
    IConfiguration configuration,
    TimeProvider timeProvider)
{
    /// <summary>The sole approved PG-ONPREM-2 Story 27.4 profile hash.</summary>
    public const string ApprovedProfileSha256 = "95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb";

    /// <summary>Gets whether any local selected binding was supplied, including partial intent.</summary>
    public bool HasLocalIntent =>
        !string.IsNullOrEmpty(configuration["AccessTelemetryLifecycle:LocalFunctionalRunId"]) ||
        !string.IsNullOrEmpty(configuration["AccessTelemetryLifecycle:LocalFunctionalProfileJson"]) ||
        !string.IsNullOrEmpty(configuration["AccessTelemetryLifecycle:LocalFunctionalSourceMappingHash"]) ||
        !string.IsNullOrEmpty(configuration["AccessTelemetryQualification:LocalProfileHash"]);

    /// <summary>Validates the current gate without caching a past authorization.</summary>
    /// <param name="reason">A bounded reason when the gate is closed.</param>
    /// <returns><see langword="true"/> only for a current exact-profile Qualification gate.</returns>
    public bool TryValidate(out string reason)
    {
        if (HasLocalIntent)
        {
            _ = bool.TryParse(configuration["AccessTelemetryLifecycle:Enabled"], out bool lifecycleEnabled);
            _ = TimeSpan.TryParse(configuration["AccessTelemetryLifecycle:Retention"], CultureInfo.InvariantCulture, out TimeSpan retention);
            var options = new AccessTelemetryOptions
            {
                Enabled = lifecycleEnabled,
                Retention = retention,
                DeploymentId = configuration["AccessTelemetryLifecycle:DeploymentId"] ?? string.Empty,
                ComponentProfileHash = configuration["AccessTelemetryLifecycle:ComponentProfileHash"] ?? string.Empty,
                LocalFunctionalRunId = configuration["AccessTelemetryLifecycle:LocalFunctionalRunId"] ?? string.Empty,
                LocalFunctionalProfileJson = configuration["AccessTelemetryLifecycle:LocalFunctionalProfileJson"] ?? string.Empty,
                LocalFunctionalSourceMappingHash = configuration["AccessTelemetryLifecycle:LocalFunctionalSourceMappingHash"] ?? string.Empty,
            };
            _ = DateTimeOffset.TryParse(configuration["AccessTelemetryLifecycle:CapabilityEvidence:ValidUntilUtc"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset deadline);
            bool enabled = AccessTelemetryQualificationGateFile.IsLocalEnabled(
                options, environment.EnvironmentName, configuration["HEXALITH_G4_RUN_ID"], configuration["HEXALITH_SOURCE_MAPPING_HASH"],
                configuration["AccessTelemetryQualification:LocalProfileHash"], configuration["AccessTelemetryQualification:GatePath"],
                deadline, timeProvider.GetUtcNow());
            reason = enabled ? "none" : "qualification_gate_invalid_or_expired";
            return enabled;
        }

        if (!environment.IsEnvironment("Qualification"))
        {
            reason = "qualification_environment_required";
            return false;
        }

        return AccessTelemetryQualificationGateFile.TryValidate(
            configuration["AccessTelemetryQualification:GatePath"],
            ApprovedProfileSha256,
            timeProvider.GetUtcNow(),
            out reason);
    }
}
