// <copyright file="DaprApiTokenConfiguration.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

/// <summary>Resolves the opt-in Dapr API token pair from the AppHost environment.</summary>
internal static class DaprApiTokenConfiguration
{
    /// <summary>The environment variable that selects the token mode.</summary>
    internal const string ModeVariable = "DAPR_API_TOKEN_MODE";

    /// <summary>The mode value that requires Dapr API tokens.</summary>
    internal const string EnabledMode = "enabled";

    /// <summary>The explicit mode value that keeps Dapr API tokens disabled.</summary>
    internal const string DisabledMode = "disabled";

    /// <summary>Resolves the sidecar and application tokens.</summary>
    /// <param name="readEnvironment">Reads an environment variable by name.</param>
    /// <returns>Both tokens when the mode is enabled; otherwise two null values.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the mode is not recognized, or when the enabled mode lacks a token.</exception>
    /// <remarks>
    /// An unset, blank or <c>disabled</c> mode keeps local development and the integration fixture token-free.
    /// Any other value stops the AppHost, because treating a typo as disabled would silently start the sidecars
    /// without token authentication. The rejected value is not echoed in case it holds a misplaced secret.
    /// </remarks>
    internal static (string? DaprApiToken, string? AppApiToken) Resolve(Func<string, string?> readEnvironment)
    {
        ArgumentNullException.ThrowIfNull(readEnvironment);
        string? mode = readEnvironment(ModeVariable);
        if (string.IsNullOrWhiteSpace(mode) || string.Equals(mode, DisabledMode, StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        if (!string.Equals(mode, EnabledMode, StringComparison.OrdinalIgnoreCase))
        {
            string trimmed = mode.Trim();
            string whitespaceHint = string.Equals(trimmed, EnabledMode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, DisabledMode, StringComparison.OrdinalIgnoreCase)
                ? " The value has leading or trailing whitespace; remove it."
                : string.Empty;
            throw new InvalidOperationException(
                $"{ModeVariable} has an unrecognized value.{whitespaceHint} Set it to '{EnabledMode}' to require Dapr API tokens, " +
                $"or to '{DisabledMode}' or leave it unset for local development.");
        }

        string? daprToken = readEnvironment("DAPR_API_TOKEN");
        string? appToken = readEnvironment("APP_API_TOKEN");
        if (string.IsNullOrWhiteSpace(daprToken) || string.IsNullOrWhiteSpace(appToken))
        {
            throw new InvalidOperationException(
                "DAPR_API_TOKEN_MODE=enabled requires both DAPR_API_TOKEN and APP_API_TOKEN environment variables to be set.");
        }

        return (daprToken, appToken);
    }
}
