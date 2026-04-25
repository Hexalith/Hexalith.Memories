// <copyright file="HandlersMismatchesCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Story 9.3 — builds <c>memories handlers mismatches --tenant X</c>. Reads the server's
/// mismatch report via <see cref="MemoriesClient.GetHandlerMismatchesAsync"/> and renders filtered
/// output for human/table consumers. JSON output returns the unfiltered report so downstream
/// consumers can apply their own filters.</summary>
public static class HandlersMismatchesCommand
{
    /// <summary>Command name used in JSON error envelopes (ADR-7.3-002).</summary>
    public const string CommandName = "handlers mismatches";

    private const string MismatchesCommandDescription = """
Detect handler-routing mismatches for a tenant.

Examples:
    memories handlers mismatches --tenant acme
    memories handlers mismatches --tenant acme --severity warning
    memories handlers mismatches --tenant acme --exclude-stale
""";

    /// <summary>Builds the <c>mismatches</c> subcommand.</summary>
    /// <param name="services">The DI service provider.</param>
    /// <returns>The configured command.</returns>
    public static Command Build(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Option<string> tenantOption = new("--tenant")
        {
            Description = "Tenant identifier to detect mismatches for (required).",
            Required = true,
        };

        Option<string?> severityOption = new("--severity")
        {
            Description = "Filter to info or warning severity (human/table output only).",
        };

        Option<bool> onlyWarningOption = new("--only-warning")
        {
            Description = "Shorthand for --severity warning.",
        };

        Option<bool> excludeStaleOption = new("--exclude-stale")
        {
            Description = "Suppress StaleHandler category entries in human/table output.",
        };

        Command command = new("mismatches", MismatchesCommandDescription);
        command.Options.Add(tenantOption);
        command.Options.Add(severityOption);
        command.Options.Add(onlyWarningOption);
        command.Options.Add(excludeStaleOption);

        command.SetAction(async (parseResult, ct) =>
        {
            string tenantId = parseResult.GetValue(tenantOption) ?? string.Empty;
            string? severity = parseResult.GetValue(severityOption);
            bool onlyWarning = parseResult.GetValue(onlyWarningOption);
            bool excludeStale = parseResult.GetValue(excludeStaleOption);

            return await ExecuteAsync(
                services, tenantId, severity, onlyWarning, excludeStale, ct).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider services,
        string tenantId,
        string? severity,
        bool onlyWarning,
        bool excludeStale,
        CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();
        OutputFormatterRouter router = services.GetRequiredService<OutputFormatterRouter>();

        return await executor.ExecuteAsync(CommandName, async (config, innerCt) =>
        {
            MemoriesClient client = services.GetRequiredService<MemoriesClient>();

#pragma warning disable HXL002
            HandlerMismatchReport report = await client
                .GetHandlerMismatchesAsync(tenantId, innerCt)
                .ConfigureAwait(false);
#pragma warning restore HXL002

            if (console.Format == OutputFormat.Json)
            {
                // JSON is unfiltered — consumers apply their own filters.
                router.Write(console.Format, report, console.Out);
                return CliExitCodes.Success;
            }

            HandlerMismatchSeverity? severityFilter = ResolveSeverityFilter(severity, onlyWarning);
            IReadOnlyList<HandlerMismatch> filtered = FilterMismatches(report.Mismatches, severityFilter, excludeStale);

            if (filtered.Count == 0)
            {
                WriteHealthyMessage(console, report, tenantId);
                return CliExitCodes.Success;
            }

            HandlerMismatchReport filteredReport = report with { Mismatches = filtered };
            router.Write(console.Format, filteredReport, console.Out);

            return CliExitCodes.Success;
        }, ct).ConfigureAwait(false);
    }

    private static HandlerMismatchSeverity? ResolveSeverityFilter(string? severity, bool onlyWarning)
    {
        if (onlyWarning)
        {
            return HandlerMismatchSeverity.Warning;
        }

        if (string.IsNullOrWhiteSpace(severity))
        {
            return null;
        }

        return severity.ToLowerInvariant() switch
        {
            "warning" => HandlerMismatchSeverity.Warning,
            "info" => HandlerMismatchSeverity.Info,
            _ => null,
        };
    }

    private static IReadOnlyList<HandlerMismatch> FilterMismatches(
        IReadOnlyList<HandlerMismatch> source,
        HandlerMismatchSeverity? severityFilter,
        bool excludeStale)
    {
        IEnumerable<HandlerMismatch> q = source;
        if (severityFilter is { } sev)
        {
            q = q.Where(m => m.Severity == sev);
        }

        if (excludeStale)
        {
            q = q.Where(m => m.Category != HandlerMismatchCategory.StaleHandler);
        }

        return q.ToList();
    }

    private static void WriteHealthyMessage(CliConsole console, HandlerMismatchReport report, string tenantId)
    {
        string message = string.Create(
            CultureInfo.InvariantCulture,
            $"No handler mismatches detected in the last {report.WindowHours}h for tenant '{tenantId}' — this is the healthy state. Summary: {report.Summary.RoutesConfigured} routes configured, {report.Summary.ObservationsChecked} observations examined.");
        console.Out.WriteLine(message);
    }
}
