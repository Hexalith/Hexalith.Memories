// <copyright file="DaprTokenStartupValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.ServiceDefaults.Security;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Fails production startup when either required DAPR authentication token is absent, or when the
/// test-host application-token override is configured.
/// </summary>
/// <param name="environment">The host environment. Only Production is validated.</param>
/// <param name="configuration">Host configuration checked for the application-token override.</param>
internal sealed class DaprTokenStartupValidator(IHostEnvironment environment, IConfiguration configuration) : IHostedService
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

        // Epic 27 retro B1: the middleware already ignores the override in Production. Failing startup
        // surfaces the misconfiguration instead of leaving an operator believing the override applies.
        // The message names the key only; the value may be a token. Operators usually set it as an
        // environment variable, so both spellings are given.
        if (DaprApplicationTokenMiddleware.ReadApplicationTokenOverride(configuration) is not null)
        {
            const string Key = DaprApplicationTokenMiddleware.AppApiTokenOverrideConfigurationKey;
            throw new InvalidOperationException(
                $"Production hosts must not configure {Key} (environment variable "
                + $"{Key.Replace(":", "__", StringComparison.Ordinal)}). "
                + "It is a test-host seam; remove it so the DAPR application token is enforced from "
                + $"{DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable}.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
