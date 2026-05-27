// <copyright file="OutputFormat.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output;

using System.Text.Json.Serialization;

/// <summary>Selects the output surface used by <see cref="IOutputFormatter{T}"/> implementations.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<OutputFormat>))]
public enum OutputFormat
{
    /// <summary>Default interactive text; preserves Story 7.1 byte-for-byte output for existing commands.</summary>
    Human,

    /// <summary>Schema-versioned JSON envelope for scripts and agents (ADR-7.2-001).</summary>
    Json,

    /// <summary>ASCII-aligned table for terminal viewing; not script-safe (ADR-7.2-003).</summary>
    Table,
}
