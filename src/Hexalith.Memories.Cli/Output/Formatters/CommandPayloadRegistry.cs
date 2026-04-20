// <copyright file="CommandPayloadRegistry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Output.Json;
using Hexalith.Memories.Cli.Quickstart;
using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Maps a CLI command name (e.g. <c>tenant list</c>, <c>search query</c>) to a
/// type-specific <see cref="JsonErrorEnvelopeWriter.Write{TPayload}(TextWriter, string, CliErrorPayload)"/>
/// invocation. Keeps the <see cref="CliJsonSourceGenerationContext"/> AOT-registered envelope shape
/// identical across success and error paths (per Task 2.4).
/// </summary>
internal static class CommandPayloadRegistry
{
    /// <summary>
    /// Default writer used when the invoked command cannot be determined (pre-handler failures — the
    /// command name is <c>memories</c> or similar root placeholder). The choice of payload shape is
    /// irrelevant in the error case because <c>Data</c> is <see langword="null"/> and suppressed via
    /// <c>JsonIgnoreCondition.WhenWritingNull</c>; only the source-gen registration must resolve.
    /// </summary>
    private static readonly Action<TextWriter, string, CliErrorPayload> DefaultWriter =
        JsonErrorEnvelopeWriter.Write<IReadOnlyList<TenantSummary>>;

    private static readonly IReadOnlyDictionary<string, Action<TextWriter, string, CliErrorPayload>> Writers =
        new Dictionary<string, Action<TextWriter, string, CliErrorPayload>>(StringComparer.Ordinal)
        {
            ["tenant list"] = JsonErrorEnvelopeWriter.Write<IReadOnlyList<TenantSummary>>,
            ["config show"] = JsonErrorEnvelopeWriter.Write<ConfigShowData>,
            ["search query"] = JsonErrorEnvelopeWriter.Write<HybridSearchResult>,
            ["search inspect"] = JsonErrorEnvelopeWriter.Write<MemoryUnit>,
            ["status telemetry"] = JsonErrorEnvelopeWriter.Write<TelemetrySummary>,
            ["quickstart"] = JsonErrorEnvelopeWriter.Write<QuickstartEnvelopeData>,
            ["consistency verify"] = JsonErrorEnvelopeWriter.Write<ConsistencyVerificationResult>,
            ["consistency inspect"] = JsonErrorEnvelopeWriter.Write<ConsistencyInspectionResult>,
            ["consistency repair"] = JsonErrorEnvelopeWriter.Write<ConsistencyRepairResult>,
        };

    /// <summary>
    /// Resolves the <see cref="JsonErrorEnvelopeWriter"/> writer action for <paramref name="command"/>,
    /// returning <see cref="DefaultWriter"/> for unknown names (e.g. pre-handler failures).
    /// </summary>
    /// <param name="command">The CLI command name (e.g., <c>search query</c>).</param>
    /// <returns>The writer action bound to the matching envelope shape.</returns>
    public static Action<TextWriter, string, CliErrorPayload> ResolveWriter(string command)
    {
        if (!string.IsNullOrEmpty(command) && Writers.TryGetValue(command, out Action<TextWriter, string, CliErrorPayload>? writer))
        {
            return writer;
        }

        return DefaultWriter;
    }
}
