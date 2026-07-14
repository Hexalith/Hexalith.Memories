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
/// The endpoint uses <see cref="TryReadManifest"/> to validate the manifest before scheduling;
/// the restore workflow uses <see cref="Parse"/> to materialize the full envelope.
/// </summary>
internal static class ImportEnvelopeReader
{
    /// <summary>
    /// Reads the <c>manifest</c> object while validating the complete canonical envelope. The endpoint must not
    /// accept a prefix that looks valid while a duplicate manifest or malformed trailing section changes what
    /// the restore workflow later consumes.
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
            manifest = Parse(payload).Manifest;
            return true;
        }
        catch (ImportEnvelopeException ex)
        {
            error = ex.Message;
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
        HashSet<string> properties = new(StringComparer.Ordinal);
        bool ended = false;

        try
        {
            Utf8JsonReader reader = new(payload, new JsonReaderOptions { AllowMultipleValues = true });
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                throw new ImportEnvelopeException("MALFORMED_IMPORT", "Import payload must be a JSON object.");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    ended = true;
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new ImportEnvelopeException(
                        "MALFORMED_IMPORT",
                        $"Expected a top-level property name but found {reader.TokenType}.");
                }

                string property = reader.GetString()!;
                if (!properties.Add(property))
                {
                    throw new ImportEnvelopeException(
                        "DUPLICATE_SECTION",
                        $"Import payload contains duplicate top-level section '{property}'.");
                }

                if (!reader.Read())
                {
                    throw new ImportEnvelopeException(
                        "MALFORMED_IMPORT",
                        $"Import payload ended before section '{property}' had a value.");
                }

                switch (property)
                {
                    case "manifest":
                        EnsureKind(reader.TokenType, JsonTokenType.StartObject, property);
                        manifest = JsonSerializer.Deserialize<ExportManifest>(ref reader, options);
                        break;
                    case "tenant":
                        EnsureKind(reader.TokenType, JsonTokenType.StartObject, property);
                        tenant = JsonSerializer.Deserialize<ExportedTenantConfig>(ref reader, options);
                        break;
                    case "case":
                        EnsureKind(reader.TokenType, JsonTokenType.StartObject, property);
                        cases.Add(ReadCase(JsonSerializer.Deserialize<JsonElement>(ref reader, options), options));
                        break;
                    case "cases":
                        EnsureKind(reader.TokenType, JsonTokenType.StartArray, property);
                        foreach (JsonElement element in JsonSerializer.Deserialize<JsonElement>(ref reader, options).EnumerateArray())
                        {
                            cases.Add(ReadCase(element, options));
                        }

                        break;
                    case "memoryUnits":
                        EnsureKind(reader.TokenType, JsonTokenType.StartArray, property);
                        memoryUnits = JsonSerializer.Deserialize<List<ExportedMemoryUnit>>(ref reader, options)
                            ?? throw new ImportEnvelopeException("MALFORMED_IMPORT", "Section 'memoryUnits' cannot be null.");
                        break;
                    case "edges":
                        EnsureKind(reader.TokenType, JsonTokenType.StartArray, property);
                        edges = JsonSerializer.Deserialize<List<ExportedEdge>>(ref reader, options)
                            ?? throw new ImportEnvelopeException("MALFORMED_IMPORT", "Section 'edges' cannot be null.");
                        break;
                    case "statistics":
                        EnsureKind(reader.TokenType, JsonTokenType.StartObject, property);
                        statistics = JsonSerializer.Deserialize<ExportStatistics>(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            if (!ended)
            {
                throw new ImportEnvelopeException("MALFORMED_IMPORT", "Import payload is missing the closing top-level object token.");
            }

            if (reader.Read())
            {
                throw new ImportEnvelopeException("TRAILING_CONTENT", "Import payload contains trailing content after the top-level object.");
            }
        }
        catch (JsonException ex)
        {
            throw new ImportEnvelopeException("MALFORMED_IMPORT", $"Import payload is not valid JSON: {ex.Message}", ex);
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

        if (statistics.MemoryUnitCount != memoryUnits.Count
            || statistics.EdgeCount != edges.Count
            || statistics.CaseCount != cases.Count)
        {
            throw new ImportEnvelopeException(
                "STATISTICS_MISMATCH",
                $"Import statistics ({statistics.MemoryUnitCount} units, {statistics.EdgeCount} edges, {statistics.CaseCount} cases) " +
                $"do not match the envelope ({memoryUnits.Count} units, {edges.Count} edges, {cases.Count} cases).");
        }

        return new ImportEnvelope
        {
            Manifest = manifest,
            Tenant = tenant,
            Cases = cases,
            MemoryUnits = memoryUnits,
            Edges = edges,
            Statistics = statistics,
        };
    }

    internal static ImportedCase ReadCase(JsonElement element, JsonSerializerOptions options)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new ImportEnvelopeException("MALFORMED_IMPORT", "Every case entry must be a JSON object.");
        }

        // The case object is the Case record's fields inlined with an appended `members` array. Deserializing
        // as Case ignores the unmapped `members` property (Web defaults skip unknown members); members are read
        // out separately.
        Case caseRecord = element.Deserialize<Case>(options)
            ?? throw new ImportEnvelopeException("MALFORMED_IMPORT", "A case object in the import payload could not be deserialized.");

        if (!element.TryGetProperty("members", out JsonElement membersElement)
            || membersElement.ValueKind != JsonValueKind.Array)
        {
            throw new ImportEnvelopeException("MALFORMED_IMPORT", "Every case entry must contain a 'members' array.");
        }

        IReadOnlyList<CaseMember> members = membersElement.Deserialize<List<CaseMember>>(options)
            ?? throw new ImportEnvelopeException("MALFORMED_IMPORT", "A case members array cannot be null.");

        return new ImportedCase(caseRecord, members);
    }

    private static void EnsureKind(JsonTokenType actual, JsonTokenType expected, string property)
    {
        if (actual != expected)
        {
            throw new ImportEnvelopeException(
                "MALFORMED_IMPORT",
                $"Import section '{property}' must be a {(expected == JsonTokenType.StartArray ? "JSON array" : "JSON object")}.");
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
            throw new ImportEnvelopeException(
                "MISSING_SECTION",
                $"Import payload is missing the required '{property}' section.");
        }
    }
}
