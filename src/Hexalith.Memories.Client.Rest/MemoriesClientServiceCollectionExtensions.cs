// <copyright file="MemoriesClientServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>DI extensions for registering <see cref="MemoriesClient"/>.</summary>
public static class MemoriesClientServiceCollectionExtensions
{
    /// <summary>The default HTTP client timeout.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Registers <see cref="MemoriesClient"/>, its options, and the auth delegating handler.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the <see cref="MemoriesClientOptions"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> for further configuration.</returns>
    public static IHttpClientBuilder AddMemoriesClient(
        this IServiceCollection services,
        Action<MemoriesClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddOptions<MemoriesClientOptions>();
        services.AddTransient<MemoriesAuthHandler>();

        return services.AddHttpClient<MemoriesClient>((sp, httpClient) =>
        {
            MemoriesClientOptions opts = sp.GetRequiredService<IOptions<MemoriesClientOptions>>().Value;
            if (opts.Endpoint is not null)
            {
                httpClient.BaseAddress = opts.Endpoint;
            }

            httpClient.Timeout = DefaultTimeout;
        })
        .AddHttpMessageHandler<MemoriesAuthHandler>();
    }
}
