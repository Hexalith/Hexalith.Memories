// <copyright file="ImportEnvelopeReader.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Reverses the export envelope produced by <c>ExportWriter</c> back into a strongly typed
/// <see cref="ImportEnvelope"/> using a forward-only <see cref="Utf8JsonReader"/> (Story 26.2).
/// The endpoint uses <see cref="TryReadManifest"/> to validate the leading manifest before scheduling;
/// the restore workflow uses <see cref="Parse"/> to materialize the full envelope.
/// </summary>
internal static class ImportEnvelopeReader
{
    /// <summary>
    /// Reads only the leading <c>manifest</c> object so the endpoint can reject an unsupported schema
    /// version or a scope/route mismatch before staging + scheduling. Returns early after the manifest.
    /// </summary>
    /// <param name="payload">The complete UTF-8 import payload.</param>
    /// <param name="manifest">The parsed manifest when successful.</param>
    /// <param name="error">A human-readable reason when parsing fails.</param>
    /// <returns><see langword="true"/> when the manifest was read; otherwise <see langword="false"/>.</returns>
    internal static bool TryReadManifest(ReadOnlySpan<byte> payload, out ExportManifest? manifest, out string? error)
    {
        manifest = null;
        error = null;
        try
        {
            Utf8JsonReader reader = new(payload);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                error = "Import payload must be a JSON object.";
                return false;
            }

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string property = reader.GetString()!;
                if (!reader.Read())
                {
                    break;
                }

                if (string.Equals(property, "manifest", StringComparison.Ordinal))
                {
                    manifest = JsonSerializer.Deserialize<ExportManifest>(ref reader, MemoriesJsonContext.Options);
                    if (manifest is null)
                    {
                        error = "Import manifest could not be deserialized.";
                        return false;
                    }

                    return true;
                }

                reader.Skip();
            }

            error = "Import payload is missing the required manifest section (it must be the first property).";
            return false;
        }
        catch (JsonException ex)
        {
            error = $"Import payload is not valid JSON: {ex.Message}";
            return false;
        }
    }

    /// <summary>Parses the complete export envelope into a normalized <see cref="ImportEnvelope"/>.</summary>
    /// <param name="payload">The complete UTF-8 import payload.</param>
    /// <returns>The parsed envelope.</returns>
    /// <exception cref="ImportEnvelopeException">Thrown when the payload is malformed or missing the manifest.</exception>
    internal static ImportEnvelope Parse(ReadOnlySpan<byte> payload)
    {
        JsonSerializerOptions options = MemoriesJsonContext.Options;
        ExportManifest? manifest = null;
        ExportedTenantConfig? tenant = null;
        List<ImportedCase> cases = [];
        List<ExportedMemoryUnit> memoryUnits = [];
        List<ExportedEdge> edges = [];
        ExportStatistics? statistics = null;

        try
        {
            Utf8JsonReader reader = new(payload);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                throw new ImportEnvelopeException("MALFORMED_IMPORT", "Import payload must be a JSON object.");
            }

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string property = reader.GetString()!;
                if (!reader.Read())
                {
                    break;
                }

                switch (property)
                {
                    case "manifest":
                        manifest = JsonSerializer.Deserialize<ExportManifest>(ref reader, options);
                        break;
                    case "tenant":
                        tenant = JsonSerializer.Deserialize<ExportedTenantConfig>(ref reader, options);
                        break;
                    case "case":
                        cases.Add(ReadCase(JsonSerializer.Deserialize<JsonElement>(ref reader, options), options));
                        break;
                    case "cases":
                        foreach (JsonElement element in JsonSerializer.Deserialize<JsonElement>(ref reader, options).EnumerateArray())
                        {
                            cases.Add(ReadCase(element, options));
                        }

                        break;
                    case "memoryUnits":
                        memoryUnits = JsonSerializer.Deserialize<List<ExportedMemoryUnit>>(ref reader, options) ?? [];
                        break;
                    case "edges":
                        edges = JsonSerializer.Deserialize<List<ExportedEdge>>(ref reader, options) ?? [];
                        break;
                    case "statistics":
                        statistics = JsonSerializer.Deserialize<ExportStatistics>(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }
        catch (JsonException ex)
        {
            throw new ImportEnvelopeException("MALFORMED_IMPORT", $"Import payload is not valid JSON: {ex.Message}", ex);
        }

        return manifest is null
            ? throw new ImportEnvelopeException("MISSING_MANIFEST", "Import payload is missing the required manifest section.")
            : new ImportEnvelope
            {
                Manifest = manifest,
                Tenant = tenant,
                Cases = cases,
                MemoryUnits = memoryUnits,
                Edges = edges,
                Statistics = statistics,
            };
    }

    private static ImportedCase ReadCase(JsonElement element, JsonSerializerOptions options)
    {
        // The case object is the Case record's fields inlined with an appended `members` array. Deserializing
        // as Case ignores the unmapped `members` property (Web defaults skip unknown members); members are read
        // out separately.
        Case caseRecord = element.Deserialize<Case>(options)
            ?? throw new ImportEnvelopeException("MALFORMED_IMPORT", "A case object in the import payload could not be deserialized.");

        IReadOnlyList<CaseMember> members = element.TryGetProperty("members", out JsonElement membersElement)
            && membersElement.ValueKind == JsonValueKind.Array
                ? membersElement.Deserialize<List<CaseMember>>(options) ?? []
                : [];

        return new ImportedCase(caseRecord, members);
    }
}
