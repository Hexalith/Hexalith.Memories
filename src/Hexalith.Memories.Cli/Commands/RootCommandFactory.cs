// <copyright file="RootCommandFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;

using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Composes the root command and all top-level subcommand groups advertised by AC #2.</summary>
public static class RootCommandFactory
{
    private const string RootDescription = """
Hexalith.Memories CLI (preview) — foundation tool shipped by Story 7.1.

Example:
    memories tenant list
""";

    private const string TenantCommandDescription = """
Tenant administration commands.

Example:
    memories tenant list
""";

    /// <summary>Top-level command groups surfaced in root help (FR53).</summary>
    public static readonly IReadOnlyList<(string Name, string Description, string StoryId)> CommandGroups =
    [
        ("ingest", "Ingest memories from files, URLs, or directories.", "7.2"),
        ("search", "Search memories with three-axis hybrid fusion.", "7.2"),
        ("traverse", "Walk the causal graph from a seed memory.", "7.2"),
        ("case", "Create, list, and manage cases.", "7.2"),
        ("status", "Inspect ingestion pipeline status.", "7.2"),
        ("explore", "Interactive exploration of memories and cases.", "7.2"),
        ("handlers", "List registered event handlers.", "7.2"),
        ("quickstart", "Guided onboarding flow (quickstart wizard).", "7.4"),
    ];

    /// <summary>Builds the root command tree.</summary>
    /// <param name="services">The DI service provider.</param>
    /// <param name="globalOptions">The three global options shared across subcommands.</param>
    /// <returns>The configured root command.</returns>
    public static RootCommand Build(IServiceProvider services, CliGlobalOptions globalOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(globalOptions);

        var root = new RootCommand(RootDescription);
        root.Options.Add(globalOptions.EndpointOption);
        root.Options.Add(globalOptions.TokenOption);
        root.Options.Add(globalOptions.VerboseOption);

        // tenant — fully wired group in 7.1.
        var tenantCommand = new Command("tenant", TenantCommandDescription);
        tenantCommand.Subcommands.Add(TenantListCommand.Build(services));
        tenantCommand.SetAction(_ => tenantCommand.Parse("--help").Invoke());
        root.Subcommands.Add(tenantCommand);

        // config — diagnostic group in 7.1.
        var configCommand = new Command("config", "Inspect and diagnose CLI configuration.");
        configCommand.Subcommands.Add(ConfigShowCommand.Build(services));
        configCommand.SetAction(_ => configCommand.Parse("--help").Invoke());
        root.Subcommands.Add(configCommand);

        // Stubbed groups (7.2–7.4).
        foreach ((string name, string description, string storyId) in CommandGroups)
        {
            root.Subcommands.Add(NotImplementedCommand.Create(services, name, description, storyId));
        }

        return root;
    }

    /// <summary>
    /// Pre-populates the <see cref="FlagConfigurationSource"/> from the parsed root options before any
    /// subcommand handler runs. Called once at invocation time.
    /// </summary>
    /// <param name="services">The DI service provider.</param>
    /// <param name="parseResult">The parsed root args.</param>
    /// <param name="globalOptions">The three global options.</param>
    public static void ApplyGlobalOptions(
        IServiceProvider services,
        System.CommandLine.ParseResult parseResult,
        CliGlobalOptions globalOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(globalOptions);

        FlagConfigurationSource flag = services.GetRequiredService<FlagConfigurationSource>();
        CliConsole console = services.GetRequiredService<CliConsole>();

        string? endpointRaw = parseResult.GetValue(globalOptions.EndpointOption);
        if (!string.IsNullOrEmpty(endpointRaw))
        {
            if (!Uri.TryCreate(endpointRaw, UriKind.Absolute, out Uri? parsedEndpoint))
            {
                throw new Configuration.InvalidConfigurationException(
                    filePath: "--endpoint",
                    message: $"value '{endpointRaw}' is not an absolute URI.");
            }

            flag.Endpoint = parsedEndpoint;
        }

        flag.ApiToken = parseResult.GetValue(globalOptions.TokenOption);
        console.Verbose = parseResult.GetValue(globalOptions.VerboseOption);
    }
}
