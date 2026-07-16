// <copyright file="EmbeddingProviderSecret.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Text.RegularExpressions;

/// <summary>Secret-store entry used by provider-specific Aspire integration tests.</summary>
public sealed partial record EmbeddingProviderSecret
{
    /// <summary>Initializes a new instance of the <see cref="EmbeddingProviderSecret"/> record.</summary>
    /// <param name="name">The DAPR secret name. Constrained to identifier characters because it is interpolated into a YAML allowed-secrets list.</param>
    /// <param name="value">The secret value written to the local test secret store.</param>
    public EmbeddingProviderSecret(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);
        if (!IdentifierPattern().IsMatch(name))
        {
            throw new ArgumentException(
                $"Secret name '{name}' must match {IdentifierPattern()} to prevent YAML injection in DAPR config overrides.",
                nameof(name));
        }

        Name = name;
        Value = value;
    }

    /// <summary>Gets the DAPR secret name referenced by tenant embedding configuration.</summary>
    public string Name { get; }

    /// <summary>Gets the secret value written to the local test secret store.</summary>
    public string Value { get; }

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPattern();
}
