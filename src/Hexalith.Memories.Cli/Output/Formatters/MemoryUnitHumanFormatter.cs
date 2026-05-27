// <copyright file="MemoryUnitHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Renders a <see cref="MemoryUnit"/> for interactive viewing. Metadata fields carry their <c>[human]</c>
/// / <c>[ai]</c> origin prefix (lowercase ASCII, matches <c>MetadataOrigin</c>'s camelCase JSON spelling).
/// </summary>
public sealed class MemoryUnitHumanFormatter : IOutputFormatter<MemoryUnit>
{
    private const string DateTimeRoundTripFormat = "o";

    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(MemoryUnit value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine($"id={SanitizeLine(value.Id)}");
        writer.WriteLine($"tenantId={SanitizeLine(value.TenantId)}");
        writer.WriteLine($"caseId={SanitizeLine(value.CaseId)}");
        writer.WriteLine($"sourceUri={SanitizeLine(value.SourceUri)}");
        writer.WriteLine($"ingestedBy={SanitizeLine(value.IngestedBy)}");
        writer.WriteLine($"ingestedAt={value.IngestedAt.ToString(DateTimeRoundTripFormat, CultureInfo.InvariantCulture)}");
        writer.WriteLine($"status={value.Status}");

        if (value.Metadata.Count == 0)
        {
            writer.WriteLine("metadata: (none)");
            return;
        }

        writer.WriteLine("metadata:");
        foreach (KeyValuePair<string, MetadataField> kv in value.Metadata)
        {
            string origin = kv.Value.Origin == MetadataOrigin.Human ? "human" : "ai";
            string confidence = kv.Value.Confidence.ToString("F2", CultureInfo.InvariantCulture);
            writer.WriteLine(
                $"  {SanitizeLine(kv.Key)} = {SanitizeLine(kv.Value.Value)}  [{origin}, confidence={confidence}]");
        }
    }

    /// <summary>
    /// Replaces embedded CR/LF/TAB with a single space so the <c>key=value [origin, ...]</c> grammar stays on
    /// one logical line — the <c>grep '[human]'</c> invariant from Task 7.4 depends on the suffix being on
    /// the same line as its key.
    /// </summary>
    private static string SanitizeLine(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.AsSpan().IndexOfAny('\n', '\r', '\t') < 0)
        {
            return value;
        }

        Span<char> buffer = value.Length <= 512 ? stackalloc char[value.Length] : new char[value.Length];
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            buffer[i] = c == '\n' || c == '\r' || c == '\t' ? ' ' : c;
        }

        return new string(buffer);
    }
}
