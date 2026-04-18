// <copyright file="RootCommandFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;

using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Composes the root command and all top-level subcommand groups advertised by AC #2.</summary>
public static class RootCommandFactory
{
    private const string RootDescription = """
Hexalith.Memories CLI (preview) — foundation tool shipped by Story 7.1.

Examples:
    memories tenant list
    memories quickstart
""";

    private const string TenantCommandDescription = """
Tenant administration commands.

Example:
    memories tenant list
""";

    private const string ConfigCommandDescription = """
Inspect and diagnose CLI configuration.

Example:
    memories config show
""";

    private const string SearchCommandDescription = """
Search memories with three-axis hybrid fusion.

Examples:
    memories search query --tenant acme --query "first search"
    memories search inspect --tenant acme --case case-1 --id mu-abc
""";

    private const string StatusCommandDescription = """
Inspect server + pipeline status (telemetry summary, indexes, queue depth).

Example:
    memories status telemetry --tenant acme
""";

    /// <summary>Top-level command groups surfaced in root help (FR53).</summary>
    public static readonly IReadOnlyList<(string Name, string Description, string StoryId)> CommandGroups =
    [
        ("ingest", "Ingest memories from files, URLs, or directories.", "7.2"),
        ("traverse", "Walk the causal graph from a seed memory.", "7.2"),
        ("case", "Create, list, and manage cases.", "7.2"),
        ("explore", "Interactive exploration of memories and cases.", "7.2"),
        ("handlers", "List registered event handlers.", "7.2"),
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
        root.Options.Add(globalOptions.FormatOption);

        // tenant — fully wired group in 7.1.
        var tenantCommand = new Command("tenant", TenantCommandDescription);
        tenantCommand.Subcommands.Add(TenantListCommand.Build(services));
        tenantCommand.SetAction(_ => tenantCommand.Parse("--help").Invoke());
        root.Subcommands.Add(tenantCommand);

        // config — diagnostic group in 7.1.
        var configCommand = new Command("config", ConfigCommandDescription);
        configCommand.Subcommands.Add(ConfigShowCommand.Build(services));
        configCommand.SetAction(_ => configCommand.Parse("--help").Invoke());
        root.Subcommands.Add(configCommand);

        // search — wired group in 7.2 (query + inspect).
        var searchCommand = new Command("search", SearchCommandDescription);
        searchCommand.Subcommands.Add(SearchQueryCommand.Build(services));
        searchCommand.Subcommands.Add(SearchInspectCommand.Build(services));
        searchCommand.SetAction(_ => searchCommand.Parse("--help").Invoke());
        root.Subcommands.Add(searchCommand);

        // quickstart — wired guided wizard in 7.4.
        root.Subcommands.Add(QuickstartCommand.Build(services));

        // Story 7.5 — status command group (currently only the telemetry subcommand is wired).
        var statusCommand = new Command("status", StatusCommandDescription);
        statusCommand.Subcommands.Add(StatusTelemetryCommand.Build(services));
        statusCommand.SetAction(_ => statusCommand.Parse("--help").Invoke());
        root.Subcommands.Add(statusCommand);

        // Story 7.5: add the global --telemetry flag to the root command.
        root.Options.Add(globalOptions.TelemetryOption);

        // Stubbed groups (7.x — remaining stories).
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

        // Story 7.2: resolve --format. When --help or --version is present, skip validation so an invalid
        // --format value never blocks help output (Task 1.5).
        string? formatRaw = parseResult.GetValue(globalOptions.FormatOption);
        if (string.IsNullOrEmpty(formatRaw))
        {
            console.Format = OutputFormat.Human;
            return;
        }

        if (IsHelpOrVersionInvocation(parseResult))
        {
            console.Format = OutputFormat.Human;
            return;
        }

        if (!TryParseFormat(formatRaw, out OutputFormat parsed))
        {
            throw new Configuration.InvalidConfigurationException(
                filePath: "--format",
                message: $"Unknown format '{formatRaw}'. Use human, json, or table.");
        }

        console.Format = parsed;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the parse carries a real help or version option. Walks the parsed
    /// symbol tree (not raw tokens) so an option argument whose value is literally <c>--help</c> /
    /// <c>--version</c> does not spuriously bypass <c>--format</c> validation.
    /// </summary>
    private static bool IsHelpOrVersionInvocation(System.CommandLine.ParseResult parseResult)
    {
        System.CommandLine.Parsing.CommandResult? current = parseResult.CommandResult;
        while (current is not null)
        {
            foreach (System.CommandLine.Parsing.SymbolResult child in current.Children)
            {
                if (child is System.CommandLine.Parsing.OptionResult optionResult
                    && (optionResult.Option is System.CommandLine.Help.HelpOption
                        || optionResult.Option is System.CommandLine.VersionOption))
                {
                    return true;
                }
            }

            current = current.Parent as System.CommandLine.Parsing.CommandResult;
        }

        return false;
    }

    private static bool TryParseFormat(string raw, out OutputFormat format)
    {
        return Enum.TryParse(raw, ignoreCase: true, out format)
            && Enum.IsDefined(format);
    }
}
