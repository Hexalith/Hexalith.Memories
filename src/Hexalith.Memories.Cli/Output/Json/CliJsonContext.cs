// <copyright file="CliJsonContext.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Json;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Quickstart;
using Hexalith.Memories.Contracts.V1;

/// <summary>Source-generated JSON metadata for CLI envelope shapes (Task 2.2).</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(CliOutputEnvelope<IReadOnlyList<TenantSummary>>))]
[JsonSerializable(typeof(CliOutputEnvelope<ConfigShowData>))]
[JsonSerializable(typeof(CliOutputEnvelope<TelemetrySummary>))]
[JsonSerializable(typeof(CliOutputEnvelope<HandlerRegistrationSnapshot>))]
[JsonSerializable(typeof(CliOutputEnvelope<HandlerMismatchReport>))]
[JsonSerializable(typeof(CliOutputEnvelope<HybridSearchResult>))]
[JsonSerializable(typeof(CliOutputEnvelope<SearchResult>))]
[JsonSerializable(typeof(CliOutputEnvelope<MemoryUnit>))]
[JsonSerializable(typeof(CliOutputEnvelope<MemoryUnitIdLookupResponse>))]
[JsonSerializable(typeof(CliOutputEnvelope<QuickstartEnvelopeData>))]
// Story 8.2 CLI envelopes.
[JsonSerializable(typeof(CliOutputEnvelope<ConsistencyInspectionResult>))]
[JsonSerializable(typeof(CliOutputEnvelope<ConsistencyVerificationResult>))]
[JsonSerializable(typeof(CliOutputEnvelope<ConsistencyRepairResult>))]
[JsonSerializable(typeof(CliOutputEnvelope<ConsistencyWorkflowState>))]
[JsonSerializable(typeof(CliOutputEnvelope<ConsistencyCommandReceipt>))]
[JsonSerializable(typeof(ConfigShowData))]
[JsonSerializable(typeof(CliErrorPayload))]
[JsonSerializable(typeof(QuickstartEnvelopeData))]
[JsonSerializable(typeof(QuickstartStepResult))]
[JsonSerializable(typeof(QuickstartStepStatus))]
[JsonSerializable(typeof(ConsistencyCommandReceipt))]
internal sealed partial class CliJsonSourceGenerationContext : JsonSerializerContext;

/// <summary>Shared CLI JSON serialization options — envelope-first, indented, camelCase.</summary>
public static class CliJsonContext
{
    /// <summary>Gets the shared serializer options for CLI JSON output (<c>--format json</c>).</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        IJsonTypeInfoResolver contractsResolver = MemoriesJsonContext.Options.TypeInfoResolver
            ?? new DefaultJsonTypeInfoResolver();

        return new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                CliJsonSourceGenerationContext.Default,
                contractsResolver),
        };
    }
}
