// <copyright file="DefaultConfigurationSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Configuration;

/// <summary>Fallback tier — supplies <c>http://127.0.0.1:5000/</c> and no token.</summary>
/// <remarks>spec-infrastructure-dependency-abstraction (F3, Decision D30): the tier-4 default endpoint is
/// config-sourced from <see cref="DefaultEndpointVariableName"/> so the host/port is no longer a fixed
/// pin; the literal remains only as the documented, overridable fallback (identical effective value when
/// unset). The CLI's minimal direct-HTTP adapter is a sanctioned D30 exception.</remarks>
public sealed class DefaultConfigurationSource : IConfigurationSource
{
    /// <summary>Environment variable that overrides the built-in default endpoint.</summary>
    public const string DefaultEndpointVariableName = "HEXALITH_MEMORIES_DEFAULT_ENDPOINT";

    /// <summary>The built-in fallback endpoint used when no override is configured (AC #3a tier 4).</summary>
    public static readonly Uri DefaultEndpoint = new("http://127.0.0.1:5000/");

    private readonly Func<string, string?> _readEnvironment;

    /// <summary>Initializes a new instance with the default process-wide environment.</summary>
    public DefaultConfigurationSource()
        : this(Environment.GetEnvironmentVariable)
    {
    }

    /// <summary>Initializes a new instance with a custom environment reader (used by tests).</summary>
    /// <param name="readEnvironment">Delegate that resolves an environment variable name to its value.</param>
    public DefaultConfigurationSource(Func<string, string?> readEnvironment)
    {
        ArgumentNullException.ThrowIfNull(readEnvironment);
        _readEnvironment = readEnvironment;
    }

    /// <inheritdoc />
    public string SourceName => nameof(DefaultConfigurationSource);

    /// <inheritdoc />
    public bool TryResolve(out Uri? endpoint, out string? apiToken)
    {
        endpoint = ResolveDefaultEndpoint();
        apiToken = null;
        return true;
    }

    private Uri ResolveDefaultEndpoint()
    {
        string? configured = _readEnvironment(DefaultEndpointVariableName);
        return !string.IsNullOrWhiteSpace(configured)
            && Uri.TryCreate(configured.Trim(), UriKind.Absolute, out Uri? parsed)
            ? parsed
            : DefaultEndpoint;
    }
}
