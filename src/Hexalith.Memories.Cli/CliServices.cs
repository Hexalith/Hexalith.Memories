// <copyright file="CliServices.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Composes the CLI's DI container. No DAPR, no Aspire hosting — plain console tool only (AC #9).</summary>
public static class CliServices
{
    /// <summary>Builds the service provider used by the CLI.</summary>
    /// <returns>A fully-configured <see cref="IServiceProvider"/>.</returns>
    public static ServiceProvider Build() => BuildCollection().BuildServiceProvider();

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

        return services;
    }
}
