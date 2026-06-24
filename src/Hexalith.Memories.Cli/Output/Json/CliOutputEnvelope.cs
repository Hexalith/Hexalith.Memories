// <copyright file="CliOutputEnvelope.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Json;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Stable JSON envelope returned by every <c>--format json</c> command (ADR-7.2-001). Exactly four
/// top-level fields — <c>schemaVersion</c>, <c>command</c>, <c>data</c>, <c>error</c>. The <c>data</c>
/// slot carries the success payload; the <c>error</c> slot carries the translated
/// <see cref="CliErrorPayload"/> on failure. Both are optional in serialization
/// (<c>JsonIgnoreCondition.WhenWritingNull</c>) — success envelopes emit only
/// <c>{ schemaVersion, command, data }</c> as before, error envelopes emit only
/// <c>{ schemaVersion, command, error }</c>. Adding <c>error</c> is additive per ADR-7.2-001 and stays
/// on <see cref="CurrentSchemaVersion"/>.
/// <para>
/// <b>Mutual-exclusivity invariant:</b> <c>Data == null ⇔ Error != null</c> — exactly one of the two
/// slots is populated, never both, never neither. Enforced via a <see cref="Debug.Assert(bool)"/> in
/// DEBUG builds with zero RELEASE cost. A future contributor must not emit
/// <c>{ data: null, error: null }</c> (meaningless) or <c>{ data: ..., error: ... }</c>
/// (contradictory — which wins?).
/// </para>
/// <para>
/// <b>Field-ordering convention (ADR-7.2-001 amendment, Story 7.3):</b> future optional fields
/// (<c>traceId</c>, etc.) MUST be appended after <c>error</c>, never inserted between existing fields.
/// <c>WriteIndented = true</c> serializes in primary-constructor order; reordering churns every
/// story's golden-file snapshots for no semantic gain.
/// </para>
/// </summary>
/// <typeparam name="T">The payload type carried in the <c>data</c> slot.</typeparam>
public sealed record CliOutputEnvelope<T>
    where T : class
{
    /// <summary>The envelope schema version shipped by Story 7.2.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="CliOutputEnvelope{T}"/> class.
    /// </summary>
    /// <param name="schemaVersion">The envelope schema version.</param>
    /// <param name="command">The invoked command name (e.g., <c>tenant list</c>).</param>
    /// <param name="data">The command-specific payload, or <see langword="null"/> on error.</param>
    /// <param name="error">The translated error payload, or <see langword="null"/> on success.</param>
    /// <param name="evidencePacket">
    /// Optional canonical Evidence Packet projection for <c>search query</c> error responses (Story 2.7 /
    /// CR10). Appended after <c>error</c> per the field-ordering convention; <see langword="null"/> (and
    /// suppressed) for every other command and for success envelopes, which already carry the packet
    /// inside <c>data</c>.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when both <paramref name="data"/> and <paramref name="error"/> are populated, or both are null.</exception>
    public CliOutputEnvelope(int schemaVersion, string command, T? data, CliErrorPayload? error = null, EvidencePacket? evidencePacket = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        if ((data is null) == (error is null))
        {
            throw new ArgumentException("Exactly one of data or error must be populated.");
        }

        SchemaVersion = schemaVersion;
        Command = command;
        Data = data;
        Error = error;
        EvidencePacket = evidencePacket;
    }

    /// <summary>Gets the envelope schema version.</summary>
    public int SchemaVersion { get; }

    /// <summary>Gets the invoked command name.</summary>
    public string Command { get; }

    /// <summary>Gets the success payload, or <see langword="null"/> for error envelopes.</summary>
    public T? Data { get; }

    /// <summary>Gets the error payload, or <see langword="null"/> for success envelopes.</summary>
    public CliErrorPayload? Error { get; }

    /// <summary>
    /// Gets the optional Evidence Packet projection for <c>search query</c> error responses, or
    /// <see langword="null"/> for all other commands and success envelopes. Declared last so it
    /// serializes after <c>error</c> per the field-ordering convention.
    /// </summary>
    public EvidencePacket? EvidencePacket { get; }

    /// <summary>
    /// Initializes a success envelope (<paramref name="data"/> non-null, error slot null). Preserves
    /// Story 7.2 call-site signature so <c>JsonEnvelopeWriter.Write&lt;T&gt;</c> remains source-compatible.
    /// </summary>
    /// <param name="schemaVersion">The envelope schema version.</param>
    /// <param name="command">The invoked command name.</param>
    /// <param name="data">The success payload.</param>
    public CliOutputEnvelope(int schemaVersion, string command, T data)
        : this(schemaVersion, command, data, error: null)
    {
        ArgumentNullException.ThrowIfNull(data);
    }

    /// <summary>
    /// Factory for an error envelope. Enforces the mutual-exclusivity invariant: <c>data</c> must be
    /// <see langword="null"/>, <c>error</c> must be non-null.
    /// </summary>
    /// <param name="command">The invoked command name.</param>
    /// <param name="error">The translated error payload.</param>
    /// <param name="evidencePacket">Optional Evidence Packet projection (Story 2.7 / CR10), or <see langword="null"/>.</param>
    /// <returns>A new error envelope with <c>data</c> suppressed.</returns>
    public static CliOutputEnvelope<T> ForError(string command, CliErrorPayload error, EvidencePacket? evidencePacket = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(error);
        return new CliOutputEnvelope<T>(CurrentSchemaVersion, command, data: null, error: error, evidencePacket: evidencePacket);
    }
}
