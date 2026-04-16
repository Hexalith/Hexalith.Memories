// <copyright file="JsonErrorEnvelopeWriter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Hexalith.Memories.Cli.Output.Json;
using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Writes a <c>{ schemaVersion, command, error }</c> envelope to a <see cref="TextWriter"/> for
/// <c>--format json</c> error reporting (ADR-7.3-002 — JSON-mode errors land on stdout).
/// <para>
/// Uses the <em>same</em> <see cref="CliOutputEnvelope{T}"/> source-gen registration as the success
/// path. The <typeparamref name="TPayload"/> parameter names the payload type the success path would
/// have used (e.g., <see cref="HybridSearchResult"/> for <c>search query</c>). When <c>Data</c> is
/// <see langword="null"/>, <c>JsonIgnoreCondition.WhenWritingNull</c> suppresses the <c>data</c>
/// property so downstream consumers see only <c>{ schemaVersion, command, error }</c>.
/// </para>
/// </summary>
internal static class JsonErrorEnvelopeWriter
{
    /// <summary>
    /// Writes a JSON error envelope using the source-gen type metadata for
    /// <see cref="CliOutputEnvelope{TPayload}"/>.
    /// </summary>
    /// <typeparam name="TPayload">
    /// The success-path payload type for this command — used to select the right AOT-registered
    /// envelope shape. Must match an entry registered in <see cref="CliJsonSourceGenerationContext"/>.
    /// </typeparam>
    /// <param name="writer">The target writer (typically <c>console.Out</c> — stdout in JSON mode).</param>
    /// <param name="command">The invoked command name (e.g., <c>tenant list</c>).</param>
    /// <param name="error">The translated error payload.</param>
    public static void Write<TPayload>(TextWriter writer, string command, CliErrorPayload error)
        where TPayload : class
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(error);

        CliOutputEnvelope<TPayload> envelope = CliOutputEnvelope<TPayload>.ForError(command, error);
        JsonTypeInfo typeInfo = CliJsonContext.Options.GetTypeInfo(typeof(CliOutputEnvelope<TPayload>));
        string json = JsonSerializer.Serialize(envelope, typeInfo);
        writer.WriteLine(json);
    }

    /// <summary>
    /// Writes an error envelope routed through <see cref="CommandPayloadRegistry"/> for a dynamically
    /// resolved command name. Falls back to the tenant-list registration when no command context is
    /// available (pre-handler failures — the root command name is used as a safe default per Task 3.8).
    /// </summary>
    /// <param name="writer">The target writer (typically <c>console.Out</c>).</param>
    /// <param name="command">The invoked command name (e.g., <c>search query</c>), or <c>memories</c> for pre-handler.</param>
    /// <param name="error">The translated error payload.</param>
    public static void WriteForCommand(TextWriter writer, string command, CliErrorPayload error)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(error);

        Action<TextWriter, string, CliErrorPayload> writeAction =
            CommandPayloadRegistry.ResolveWriter(command);
        writeAction(writer, command, error);
    }
}
