// <copyright file="DefaultConfigurationSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Configuration;

/// <summary>Fallback tier — supplies <c>http://127.0.0.1:5000/</c> and no token.</summary>
public sealed class DefaultConfigurationSource : IConfigurationSource
{
    /// <summary>The built-in default endpoint (AC #3a tier 4).</summary>
    public static readonly Uri DefaultEndpoint = new("http://127.0.0.1:5000/");

    /// <inheritdoc />
    public string SourceName => nameof(DefaultConfigurationSource);

    /// <inheritdoc />
    public bool TryResolve(out Uri? endpoint, out string? apiToken)
    {
        endpoint = DefaultEndpoint;
        apiToken = null;
        return true;
    }
}
