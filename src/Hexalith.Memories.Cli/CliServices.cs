// <copyright file="CliServices.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Cli.Output.Json;
using Hexalith.Memories.Cli.Quickstart;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Composes the CLI's DI container. No DAPR, no Aspire hosting — plain console tool only (AC #9).</summary>
public static class CliServices
{
    /// <summary>Builds the service provider used by the CLI.</summary>
    /// <returns>A fully-configured <see cref="IServiceProvider"/>.</returns>
    public static ServiceProvider Build() => BuildCollection().BuildServiceProvider();

    /// <summary>Builds the service provider, additionally registering opt-in telemetry (Story 7.5).</summary>
    /// <param name="telemetryFlag">Whether the global <c>--telemetry</c> flag was passed.</param>
    /// <returns>A fully-configured <see cref="IServiceProvider"/>.</returns>
    public static ServiceProvider Build(bool telemetryFlag)
    {
        IServiceCollection services = BuildCollection();
        Execution.CliTelemetryBootstrap.TryRegister(services, telemetryFlag);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Builds the service collection (exposed for tests that want to inject fakes before building the
    /// container).
    /// </summary>
    /// <returns>The configured collection.</returns>
    public static IServiceCollection BuildCollection()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        // Console abstraction — tests swap writers.
        services.AddSingleton<CliConsole>();

        // Global options instance (shared by root command builder and handler wiring).
        services.AddSingleton<CliGlobalOptions>();

        // Endpoint resolver sources — registration ORDER is the precedence (flag > env > file > default).
        services.AddSingleton<FlagConfigurationSource>();
        services.AddSingleton<IConfigurationSource>(sp => sp.GetRequiredService<FlagConfigurationSource>());
        services.AddSingleton<IConfigurationSource>(new EnvironmentVariableConfigurationSource());
        services.AddSingleton<IConfigurationSource>(new FileConfigurationSource());
        services.AddSingleton<IConfigurationSource>(new DefaultConfigurationSource());
        services.AddSingleton<ResolvedConfigPipeline>();

        // Options mutator + MemoriesClientOptions source.
        services.AddSingleton<MemoriesClientOptionsMutator>();
        services.AddSingleton<CliCommandExecutor.IOptionsMutator>(sp => sp.GetRequiredService<MemoriesClientOptionsMutator>());
        services.AddSingleton<IOptionsMonitor<MemoriesClientOptions>>(sp =>
        {
            MemoriesClientOptionsMutator mutator = sp.GetRequiredService<MemoriesClientOptionsMutator>();
            return new LiveOptionsMonitor<MemoriesClientOptions>(mutator.Options);
        });

        // Client.Rest registration — executor-owned, no handler touches MemoriesClient directly.
        services.AddTransient<MemoriesAuthHandler>();
        services.AddHttpClient<MemoriesClient>((sp, httpClient) =>
        {
            MemoriesClientOptions opts = sp.GetRequiredService<IOptionsMonitor<MemoriesClientOptions>>().CurrentValue;
            if (opts.Endpoint is not null)
            {
                httpClient.BaseAddress = opts.Endpoint;
            }

            httpClient.Timeout = MemoriesClientServiceCollectionExtensions.DefaultTimeout;
        })
        .AddHttpMessageHandler<MemoriesAuthHandler>();

        // Executor.
        services.AddSingleton<CliCommandExecutor>();

        // Story 7.2: output-format infrastructure.
        services.AddSingleton<OutputFormatterRouter>();

        RegisterFormatters<IReadOnlyList<TenantSummary>>(
            services,
            new TenantListHumanFormatter(),
            new JsonEnvelopeFormatter<IReadOnlyList<TenantSummary>>(TenantListCommand.CommandName),
            new TenantListTableFormatter());

        RegisterFormatters<ConfigShowData>(
            services,
            new ConfigShowHumanFormatter(),
            new JsonEnvelopeFormatter<ConfigShowData>(ConfigShowCommand.CommandName),
            new ConfigShowTableFormatter());

        RegisterFormatters<TelemetrySummary>(
            services,
            new StatusTelemetryHumanFormatter(),
            new JsonEnvelopeFormatter<TelemetrySummary>(StatusTelemetryCommand.CommandName),
            new StatusTelemetryTableFormatter());

        RegisterFormatters<HandlerRegistrationSnapshot>(
            services,
            new HandlerRegistrationSnapshotHumanFormatter(),
            new JsonEnvelopeFormatter<HandlerRegistrationSnapshot>(HandlersListCommand.CommandName),
            new HandlerRegistrationSnapshotTableFormatter());

        RegisterFormatters<HandlerMismatchReport>(
            services,
            new HandlerMismatchReportHumanFormatter(),
            new JsonEnvelopeFormatter<HandlerMismatchReport>(HandlersMismatchesCommand.CommandName),
            new HandlerMismatchReportTableFormatter());

        RegisterFormatters<HybridSearchResult>(
            services,
            new HybridSearchResultHumanFormatter(),
            new JsonEnvelopeFormatter<HybridSearchResult>(SearchQueryCommand.CommandName),
            new HybridSearchResultTableFormatter());

        RegisterFormatters<SearchResult>(
            services,
            new SearchResultHumanFormatter(),
            new JsonEnvelopeFormatter<SearchResult>(SearchQueryCommand.CommandName),
            new SearchResultTableFormatter());

        RegisterFormatters<MemoryUnit>(
            services,
            new MemoryUnitHumanFormatter(),
            new JsonEnvelopeFormatter<MemoryUnit>(SearchInspectCommand.CommandName),
            new MemoryUnitTableFormatter());

        // Story 18.5: source-URI lookup result formatters (human + json).
        RegisterFormatters<MemoryUnitIdLookupResponse>(
            services,
            new MemoryUnitIdLookupHumanFormatter(),
            new JsonEnvelopeFormatter<MemoryUnitIdLookupResponse>(SearchLookupCommand.CommandName));

        // Story 8.2: consistency formatters.
        RegisterFormatters<ConsistencyInspectionResult>(
            services,
            new ConsistencyInspectionHumanFormatter(),
            new JsonEnvelopeFormatter<ConsistencyInspectionResult>(ConsistencyInspectCommand.CommandName),
            new ConsistencyInspectionTableFormatter());

        RegisterFormatters<ConsistencyVerificationResult>(
            services,
            new ConsistencyVerificationResultHumanFormatter(),
            new JsonEnvelopeFormatter<ConsistencyVerificationResult>(ConsistencyVerifyCommand.CommandName),
            new ConsistencyVerificationResultTableFormatter());

        RegisterFormatters<ConsistencyRepairResult>(
            services,
            new ConsistencyRepairResultHumanFormatter(),
            new JsonEnvelopeFormatter<ConsistencyRepairResult>(ConsistencyRepairCommand.CommandName),
            new ConsistencyRepairResultTableFormatter());

        RegisterFormatters<ConsistencyWorkflowState>(
            services,
            new ConsistencyWorkflowStateHumanFormatter(),
            new JsonEnvelopeFormatter<ConsistencyWorkflowState>(ConsistencyVerifyCommand.CommandName),
            new ConsistencyWorkflowStateTableFormatter());

        RegisterFormatters<ConsistencyCommandReceipt>(
            services,
            new ConsistencyReceiptHumanFormatter(),
            new JsonEnvelopeFormatter<ConsistencyCommandReceipt>(receipt =>
                receipt.Kind == "repair"
                    ? ConsistencyRepairCommand.CommandName
                    : ConsistencyVerifyCommand.CommandName),
            new ConsistencyReceiptTableFormatter());

        // Story 8.2: consistency verify/repair poll cadence — overridable by tests.
        services.AddOptions<ConsistencyPollOptions>();

        // Story 7.4: quickstart wizard services.
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IProcessRunner, DefaultProcessRunner>();
        services.AddSingleton<PrerequisiteChecks>();
        services.AddSingleton<HealthProbe>();
        services.AddSingleton<QuickstartTenantProvisioner>();
        services.AddSingleton<QuickstartSampleFlow>();

        return services;
    }

    private static void RegisterFormatters<T>(
        IServiceCollection services,
        params IOutputFormatter<T>[] formatters)
    {
        foreach (IOutputFormatter<T> formatter in formatters)
        {
            services.AddSingleton(formatter);
        }
    }
}
