// <copyright file="DaprTokenStartupValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.ServiceDefaults.Security;

using Microsoft.Extensions.Hosting;

/// <summary>Fails production startup when either required DAPR authentication token is absent.</summary>
internal sealed class DaprTokenStartupValidator(IHostEnvironment environment) : IHostedService
{
    internal const string DaprApiTokenEnvironmentVariable = "DAPR_API_TOKEN";

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsProduction())
        {
            return Task.CompletedTask;
        }

        List<string> missing = [];
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
            DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable)))
        {
            missing.Add(DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable);
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DaprApiTokenEnvironmentVariable)))
        {
            missing.Add(DaprApiTokenEnvironmentVariable);
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Production DAPR token authentication requires non-empty {string.Join(" and ", missing)} values.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
