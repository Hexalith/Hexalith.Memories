// <copyright file="NaturalLanguageDescriptionOptionsSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

using Microsoft.Extensions.Options;

/// <summary>Story 9.2 Task 5.7 — process-global snapshot of <see cref="NaturalLanguageDescriptionOptions"/>
/// for consumption by <see cref="Workflows.IngestionWorkflow"/>. Workflows cannot take constructor
/// dependencies (DAPR activates them with <c>new()</c>), so a static snapshot follows the same
/// pattern as <see cref="Ingestion.RetryPolicyBuilder"/>.</summary>
public static class NaturalLanguageDescriptionOptionsSnapshot
{
    private static NaturalLanguageDescriptionOptions _value = new();

    /// <summary>Gets the current snapshot. Defaults to a fresh options object with defaults before
    /// <see cref="Initialize"/> has run.</summary>
    public static NaturalLanguageDescriptionOptions Value => _value;

    /// <summary>Publishes the bound options at host startup.</summary>
    /// <param name="options">The resolved options instance.</param>
    public static void Initialize(IOptions<NaturalLanguageDescriptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _value = options.Value;
    }

    /// <summary>Resets to defaults — for test isolation only.</summary>
    internal static void ResetToDefaults() => _value = new NaturalLanguageDescriptionOptions();
}
