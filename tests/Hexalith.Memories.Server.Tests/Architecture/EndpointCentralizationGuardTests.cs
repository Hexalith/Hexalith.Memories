// <copyright file="EndpointCentralizationGuardTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using System.IO;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>Story 25.2 guard against endpoint-local telemetry scope and activity setup drift.</summary>
public sealed class EndpointCentralizationGuardTests
{
    [Fact]
    public void EndpointClasses_DoNotStartEndpointActivitiesOrCreateTelemetryScopesDirectly()
    {
        string repoRoot = ResolveRepoRoot();
        string endpointDirectory = Path.Combine(repoRoot, "src", "Hexalith.Memories.Server", "Endpoints");
        string[] infrastructureFiles =
        [
            "EndpointTelemetryHelpers.cs",
            "EndpointValidationHelpers.cs",
            "ErrorResults.cs",
            "TenantIdValidationEndpointFilter.cs",
            "TenantStatusEndpointFilter.cs",
        ];

        List<string> violations = [];
        foreach (string file in Directory.EnumerateFiles(endpointDirectory, "*.cs", SearchOption.TopDirectoryOnly))
        {
            string fileName = Path.GetFileName(file);
            if (infrastructureFiles.Contains(fileName, StringComparer.Ordinal))
            {
                continue;
            }

            string source = File.ReadAllText(file);
            if (source.Contains("MemoriesActivitySource.Instance.StartActivity", StringComparison.Ordinal))
            {
                violations.Add($"{Path.GetRelativePath(repoRoot, file)} starts an endpoint activity directly");
            }

            if (source.Contains("new EndpointTelemetryScope", StringComparison.Ordinal))
            {
                violations.Add($"{Path.GetRelativePath(repoRoot, file)} creates an endpoint telemetry scope directly");
            }
        }

        violations.ShouldBeEmpty("Endpoint classes must use EndpointTelemetryHelpers or EndpointTelemetryFilter for shared telemetry setup.");
    }

    [Fact]
    public void EndpointMappings_UseEndpointTelemetryFilter()
    {
        string repoRoot = ResolveRepoRoot();
        string endpointDirectory = Path.Combine(repoRoot, "src", "Hexalith.Memories.Server", "Endpoints");
        string[] endpointSources = Directory
            .EnumerateFiles(endpointDirectory, "*Endpoints.cs", SearchOption.TopDirectoryOnly)
            .Select(File.ReadAllText)
            .ToArray();

        endpointSources.Any(source => source.Contains("EndpointTelemetryFilter.For", StringComparison.Ordinal))
            .ShouldBeTrue("EndpointTelemetryFilter must be used by production endpoint mappings, not only unit-tested in isolation.");
    }

    [Fact]
    public void EndpointInfrastructure_UsesCommonFactoriesForSharedErrorCodes()
    {
        string repoRoot = ResolveRepoRoot();
        string[] files =
        [
            .. Directory.EnumerateFiles(Path.Combine(repoRoot, "src", "Hexalith.Memories.Server", "Endpoints"), "*.cs", SearchOption.TopDirectoryOnly),
            .. Directory.EnumerateFiles(Path.Combine(repoRoot, "src", "Hexalith.Memories.Server", "RateLimiting"), "*.cs", SearchOption.TopDirectoryOnly),
            .. Directory.EnumerateFiles(Path.Combine(repoRoot, "src", "Hexalith.Memories.Server", "Hosting"), "*.cs", SearchOption.TopDirectoryOnly),
            .. Directory.EnumerateFiles(Path.Combine(repoRoot, "src", "Hexalith.Memories.Server", "Diagnostics"), "*.cs", SearchOption.TopDirectoryOnly),
        ];
        HashSet<string> infrastructureFiles = new(StringComparer.Ordinal)
        {
            "ErrorResults.cs",
        };
        string[] commonCodes =
        [
            "DAPR_UNAVAILABLE",
            "RATE_LIMIT_EXCEEDED",
            "LOOKUP_BACKEND_UNAVAILABLE",
            "TENANT_FORBIDDEN",
            "UNHANDLED_EXCEPTION",
        ];

        List<string> violations = [];
        foreach (string file in files)
        {
            if (infrastructureFiles.Contains(Path.GetFileName(file)))
            {
                continue;
            }

            string source = File.ReadAllText(file);
            foreach (string code in commonCodes)
            {
                if (Regex.IsMatch(
                    source,
                    $@"new\s+ErrorResponse\s*\(\s*(?:\r?\n\s*)?""{Regex.Escape(code)}""",
                    RegexOptions.CultureInvariant))
                {
                    violations.Add($"{Path.GetRelativePath(repoRoot, file)} constructs common error code {code} directly");
                }
            }
        }

        violations.ShouldBeEmpty("Common envelope codes must be created through ErrorResults so status/body drift is centralized.");
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
