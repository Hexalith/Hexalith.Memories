// <copyright file="ImportEnvelopeStreamProcessor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

/// <summary>Scans the canonical export envelope one record at a time with bounded memory.</summary>
internal static class ImportEnvelopeStreamProcessor
{
    /// <summary>Validates and optionally visits cases, units, and edges in export order.</summary>
    internal static async Task<ImportEnvelopeScanResult> ProcessAsync(
        Stream stream,
        Func<ImportedCase, CancellationToken, Task>? caseHandler,
        Func<ExportedMemoryUnit, CancellationToken, Task>? memoryUnitHandler,
        Func<ExportedEdge, CancellationToken, Task>? edgeHandler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ImportJsonStreamReader reader = new(stream);
        await reader.ExpectAsync((byte)'{', cancellationToken).ConfigureAwait(false);

        HashSet<string> properties = new(StringComparer.Ordinal);
        ExportManifest? manifest = null;
        ExportStatistics? statistics = null;
        int caseCount = 0;
        int memoryUnitCount = 0;
        int edgeCount = 0;

        while (await reader.PeekNonWhitespaceAsync(cancellationToken).ConfigureAwait(false) != '}')
        {
            string property = await reader.ReadPropertyNameAsync(cancellationToken).ConfigureAwait(false);
            if (!properties.Add(property))
            {
                throw new ImportEnvelopeException(
                    "DUPLICATE_SECTION",
                    $"Import payload contains duplicate top-level section '{property}'.");
            }

            if (properties.Count == 1 && !string.Equals(property, "manifest", StringComparison.Ordinal))
            {
                throw new ImportEnvelopeException(
                    "MISSING_MANIFEST",
                    "The canonical import manifest must be the first top-level property.");
            }

            await reader.ExpectAsync((byte)':', cancellationToken).ConfigureAwait(false);
            switch (property)
            {
                case "manifest":
                    manifest = Deserialize<ExportManifest>(
                        await reader.ReadRawValueAsync(cancellationToken).ConfigureAwait(false),
                        property);
                    break;
                case "tenant":
                    _ = Deserialize<ExportedTenantConfig>(
                        await reader.ReadRawValueAsync(cancellationToken).ConfigureAwait(false),
                        property);
                    break;
                case "case":
                    ImportedCase singleCase = ImportEnvelopeReader.ReadCase(
                        DeserializeElement(await reader.ReadRawValueAsync(cancellationToken).ConfigureAwait(false), property),
                        MemoriesJsonContext.Options);
                    caseCount++;
                    if (caseHandler is not null)
                    {
                        await caseHandler(singleCase, cancellationToken).ConfigureAwait(false);
                    }

                    break;
                case "cases":
                    await ReadArrayAsync(
                        reader,
                        async raw =>
                        {
                            ImportedCase importedCase = ImportEnvelopeReader.ReadCase(
                                DeserializeElement(raw, property),
                                MemoriesJsonContext.Options);
                            caseCount++;
                            if (caseHandler is not null)
                            {
                                await caseHandler(importedCase, cancellationToken).ConfigureAwait(false);
                            }
                        },
                        cancellationToken).ConfigureAwait(false);
                    break;
                case "memoryUnits":
                    await ReadArrayAsync(
                        reader,
                        async raw =>
                        {
                            ExportedMemoryUnit unit = Deserialize<ExportedMemoryUnit>(raw, property);
                            memoryUnitCount++;
                            if (memoryUnitHandler is not null)
                            {
                                await memoryUnitHandler(unit, cancellationToken).ConfigureAwait(false);
                            }
                        },
                        cancellationToken).ConfigureAwait(false);
                    break;
                case "edges":
                    await ReadArrayAsync(
                        reader,
                        async raw =>
                        {
                            ExportedEdge edge = Deserialize<ExportedEdge>(raw, property);
                            edgeCount++;
                            if (edgeHandler is not null)
                            {
                                await edgeHandler(edge, cancellationToken).ConfigureAwait(false);
                            }
                        },
                        cancellationToken).ConfigureAwait(false);
                    break;
                case "statistics":
                    statistics = Deserialize<ExportStatistics>(
                        await reader.ReadRawValueAsync(cancellationToken).ConfigureAwait(false),
                        property);
                    break;
                default:
                    _ = await reader.ReadRawValueAsync(cancellationToken).ConfigureAwait(false);
                    break;
            }

            int delimiter = await reader.PeekNonWhitespaceAsync(cancellationToken).ConfigureAwait(false);
            if (delimiter == ',')
            {
                await reader.ExpectAsync((byte)',', cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (delimiter != '}')
            {
                throw new ImportEnvelopeException("MALFORMED_IMPORT", "Expected ',' or '}' after a top-level import section.");
            }
        }

        await reader.ExpectAsync((byte)'}', cancellationToken).ConfigureAwait(false);
        if (await reader.PeekNonWhitespaceAsync(cancellationToken).ConfigureAwait(false) >= 0)
        {
            throw new ImportEnvelopeException("TRAILING_CONTENT", "Import payload contains trailing content after the top-level object.");
        }

        if (manifest is null)
        {
            throw new ImportEnvelopeException("MISSING_MANIFEST", "Import payload is missing the required manifest section.");
        }

        RequireSection(properties, "memoryUnits");
        RequireSection(properties, "edges");
        RequireSection(properties, "statistics");
        if (statistics is null)
        {
            throw new ImportEnvelopeException("MALFORMED_IMPORT", "Import statistics could not be deserialized.");
        }

        switch (manifest.Scope)
        {
            case ExportScope.Case:
                RequireSection(properties, "case");
                RejectSection(properties, "cases", manifest.Scope);
                RejectSection(properties, "tenant", manifest.Scope);
                break;
            case ExportScope.Tenant:
                RequireSection(properties, "tenant");
                RequireSection(properties, "cases");
                RejectSection(properties, "case", manifest.Scope);
                break;
            default:
                throw new ImportEnvelopeException("UNSUPPORTED_SCOPE", $"Import manifest scope '{manifest.Scope}' is not supported.");
        }

        if (statistics.MemoryUnitCount != memoryUnitCount
            || statistics.EdgeCount != edgeCount
            || statistics.CaseCount != caseCount)
        {
            throw new ImportEnvelopeException(
                "STATISTICS_MISMATCH",
                $"Import statistics ({statistics.MemoryUnitCount} units, {statistics.EdgeCount} edges, {statistics.CaseCount} cases) " +
                $"do not match the envelope ({memoryUnitCount} units, {edgeCount} edges, {caseCount} cases).");
        }

        return new ImportEnvelopeScanResult(manifest, statistics);
    }

    private static T Deserialize<T>(ReadOnlySpan<byte> raw, string section)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(raw, MemoriesJsonContext.Options)
                ?? throw new ImportEnvelopeException("MALFORMED_IMPORT", $"Import section '{section}' cannot be null.");
        }
        catch (JsonException ex)
        {
            throw new ImportEnvelopeException("MALFORMED_IMPORT", $"Import section '{section}' is malformed: {ex.Message}", ex);
        }
    }

    private static JsonElement DeserializeElement(ReadOnlySpan<byte> raw, string section)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(raw, MemoriesJsonContext.Options);
        }
        catch (JsonException ex)
        {
            throw new ImportEnvelopeException("MALFORMED_IMPORT", $"Import section '{section}' is malformed: {ex.Message}", ex);
        }
    }

    private static async Task ReadArrayAsync(
        ImportJsonStreamReader reader,
        Func<byte[], Task> itemHandler,
        CancellationToken cancellationToken)
    {
        await reader.ExpectAsync((byte)'[', cancellationToken).ConfigureAwait(false);
        if (await reader.PeekNonWhitespaceAsync(cancellationToken).ConfigureAwait(false) == ']')
        {
            await reader.ExpectAsync((byte)']', cancellationToken).ConfigureAwait(false);
            return;
        }

        while (true)
        {
            byte[] raw = await reader.ReadRawValueAsync(cancellationToken).ConfigureAwait(false);
            await itemHandler(raw).ConfigureAwait(false);
            int delimiter = await reader.PeekNonWhitespaceAsync(cancellationToken).ConfigureAwait(false);
            if (delimiter == ',')
            {
                await reader.ExpectAsync((byte)',', cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (delimiter == ']')
            {
                await reader.ExpectAsync((byte)']', cancellationToken).ConfigureAwait(false);
                return;
            }

            throw new ImportEnvelopeException("MALFORMED_IMPORT", "Expected ',' or ']' in an import array section.");
        }
    }

    private static void RejectSection(HashSet<string> properties, string property, ExportScope scope)
    {
        if (properties.Contains(property))
        {
            throw new ImportEnvelopeException(
                "SCOPE_SECTION_MISMATCH",
                $"Import section '{property}' is not valid for {scope.ToString().ToLowerInvariant()} scope.");
        }
    }

    private static void RequireSection(HashSet<string> properties, string property)
    {
        if (!properties.Contains(property))
        {
            throw new ImportEnvelopeException("MISSING_SECTION", $"Import payload is missing the required '{property}' section.");
        }
    }
}
