// <copyright file="CliOutputEnvelope.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Json;

/// <summary>
/// Stable JSON envelope returned by every <c>--format json</c> command (ADR-7.2-001). Exactly three
/// top-level fields — <c>schemaVersion</c>, <c>command</c>, <c>data</c>. Additive changes remain on
/// <see cref="CurrentSchemaVersion"/>; rename/removal requires bumping.
/// </summary>
/// <typeparam name="T">The payload type carried in the <c>data</c> slot.</typeparam>
/// <param name="SchemaVersion">The envelope schema version (pinned at <see cref="CurrentSchemaVersion"/>).</param>
/// <param name="Command">The invoked command name (e.g., <c>tenant list</c>).</param>
/// <param name="Data">The command-specific payload.</param>
public sealed record CliOutputEnvelope<T>(int SchemaVersion, string Command, T Data)
{
    /// <summary>The envelope schema version shipped by Story 7.2.</summary>
    public const int CurrentSchemaVersion = 1;
}
