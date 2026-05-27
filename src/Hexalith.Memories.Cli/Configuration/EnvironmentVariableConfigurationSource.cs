// <copyright file="EnvironmentVariableConfigurationSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Configuration;

/// <summary>Reads <c>HEXALITH_MEMORIES_ENDPOINT</c> and <c>HEXALITH_MEMORIES_API_TOKEN</c>.</summary>
public sealed class EnvironmentVariableConfigurationSource : IConfigurationSource
{
    /// <summary>The endpoint environment variable.</summary>
    public const string EndpointVariableName = "HEXALITH_MEMORIES_ENDPOINT";

    /// <summary>The API token environment variable.</summary>
    public const string ApiTokenVariableName = "HEXALITH_MEMORIES_API_TOKEN";

    private readonly Func<string, string?> _readEnvironment;

    /// <summary>Initializes a new instance with the default process-wide environment.</summary>
    public EnvironmentVariableConfigurationSource()
        : this(Environment.GetEnvironmentVariable)
    {
    }

    /// <summary>Initializes a new instance with a custom environment reader (used by tests).</summary>
    /// <param name="readEnvironment">Delegate that resolves an environment variable name to its value.</param>
    public EnvironmentVariableConfigurationSource(Func<string, string?> readEnvironment)
    {
        ArgumentNullException.ThrowIfNull(readEnvironment);
        _readEnvironment = readEnvironment;
    }

    /// <inheritdoc />
    public string SourceName => nameof(EnvironmentVariableConfigurationSource);

    /// <inheritdoc />
    public bool TryResolve(out Uri? endpoint, out string? apiToken)
    {
        endpoint = null;
        apiToken = null;

        string? endpointValue = _readEnvironment(EndpointVariableName);
        if (!string.IsNullOrWhiteSpace(endpointValue))
        {
            endpointValue = endpointValue.Trim();
            if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out Uri? parsed))
            {
                throw new InvalidConfigurationException(
                    EndpointVariableName,
                    $"environment variable '{EndpointVariableName}' must be an absolute URI.");
            }

            endpoint = parsed;
        }

        string? tokenValue = _readEnvironment(ApiTokenVariableName);
        if (!string.IsNullOrWhiteSpace(tokenValue))
        {
            apiToken = tokenValue;
        }

        return endpoint is not null || apiToken is not null;
    }
}
