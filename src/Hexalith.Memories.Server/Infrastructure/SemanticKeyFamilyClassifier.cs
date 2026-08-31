// <copyright file="SemanticKeyFamilyClassifier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Infrastructure;

using System.Globalization;

using StackExchange.Redis;

/// <summary>Classifies semantic hashes through exact canonical key reconstruction and bounded record shape.</summary>
internal static class SemanticKeyFamilyClassifier
{
    private static readonly SemanticKeyFamily[] RegisteredFamilyValues = IndexSchemaDefinitions
        .SemanticKeyNamespaceRegistrations
        .Values
        .SelectMany(static families => families)
        .Distinct()
        .ToArray();

    /// <summary>Gets every explicit, currently registered semantic key family.</summary>
    public static IReadOnlyList<SemanticKeyFamily> RegisteredFamilies => RegisteredFamilyValues;

    /// <summary>Classifies one semantic hash without using its tenant marker as family evidence.</summary>
    /// <param name="tenantId">The requested tenant identifier.</param>
    /// <param name="key">The discovered Redis hash key.</param>
    /// <param name="memoryUnitId">The stored memory-unit identifier.</param>
    /// <param name="chunkSequence">The stored chunk sequence, when present.</param>
    /// <param name="chunkStartOffset">The stored chunk start offset, when present.</param>
    /// <param name="chunkEndOffset">The stored chunk end offset, when present.</param>
    /// <param name="hasNaturalLanguageDescription">Whether the natural-language discriminator field exists.</param>
    /// <returns>The unique registered family, or an unknown/ambiguous classification gap.</returns>
    public static SemanticKeyFamily Classify(
        string tenantId,
        RedisKey key,
        RedisValue memoryUnitId,
        RedisValue chunkSequence,
        RedisValue chunkStartOffset,
        RedisValue chunkEndOffset,
        bool hasNaturalLanguageDescription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        string? keyText = key.ToString();
        string? storedMemoryUnitId = memoryUnitId.IsNull ? null : memoryUnitId.ToString();
        if (string.IsNullOrWhiteSpace(keyText) || string.IsNullOrWhiteSpace(storedMemoryUnitId))
        {
            return SemanticKeyFamily.Unknown;
        }

        bool hasAnyChunkField = !chunkSequence.IsNull || !chunkStartOffset.IsNull || !chunkEndOffset.IsNull;
        bool hasCompleteChunkShape = TryParseNonNegativeInt(chunkSequence, out int sequence)
            && TryParseNonNegativeInt(chunkStartOffset, out int startOffset)
            && TryParseNonNegativeInt(chunkEndOffset, out int endOffset)
            && endOffset > startOffset;
        List<SemanticKeyFamily> matches = [];

        if (!hasNaturalLanguageDescription && !hasAnyChunkField
            && string.Equals(
                keyText,
                IndexSchemaDefinitions.BuildSemanticKey(tenantId, storedMemoryUnitId),
                StringComparison.Ordinal))
        {
            matches.Add(SemanticKeyFamily.ActiveRawBase);
        }

        if (!hasNaturalLanguageDescription && hasCompleteChunkShape
            && string.Equals(
                keyText,
                IndexSchemaDefinitions.BuildSemanticChunkKey(tenantId, storedMemoryUnitId, sequence),
                StringComparison.Ordinal))
        {
            matches.Add(SemanticKeyFamily.ActiveRawChunk);
        }

        if (hasNaturalLanguageDescription && !hasAnyChunkField
            && string.Equals(
                keyText,
                IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, storedMemoryUnitId),
                StringComparison.Ordinal))
        {
            matches.Add(SemanticKeyFamily.ActiveNaturalLanguage);
        }

        if (!hasNaturalLanguageDescription && !hasAnyChunkField
            && IndexSchemaDefinitions.TryParseSemanticStagingVersion(
                tenantId,
                key,
                storedMemoryUnitId,
                out _))
        {
            matches.Add(SemanticKeyFamily.RawStaging);
        }

        if (hasNaturalLanguageDescription && !hasAnyChunkField
            && IndexSchemaDefinitions.TryParseNaturalLanguageSemanticStagingVersion(
                tenantId,
                key,
                storedMemoryUnitId,
                out _))
        {
            matches.Add(SemanticKeyFamily.NaturalLanguageStaging);
        }

        if (hasNaturalLanguageDescription && !hasAnyChunkField
            && string.Equals(
                keyText,
                IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenantId, storedMemoryUnitId),
                StringComparison.Ordinal))
        {
            matches.Add(SemanticKeyFamily.LegacyNaturalLanguage);
        }

        // SemanticKeyFamily.Ambiguous is provably unreachable under the current registry: every non-chunk raw
        // family (ActiveRawBase, RawStaging) and every non-chunk NL family (ActiveNaturalLanguage,
        // NaturalLanguageStaging, LegacyNaturalLanguage) reconstructs against a fixed-length namespace prefix
        // (":vec:" / ":vecnl:" / ":vec:staging:" / ":vecnl:staging:" / ":vec:nl:"), and those prefixes all have
        // distinct fixed lengths, so a key text of the form tenantId + shortPrefix + memoryUnitId can never also
        // satisfy the length/position checks for a longer staging prefix built from the same memoryUnitId — and
        // ActiveRawChunk is mutually exclusive with every non-chunk family by the hasAnyChunkField gate above.
        // The branch stays as an explicit fail-closed guard for any future family that breaks this invariant.
        return matches.Count switch
        {
            0 => SemanticKeyFamily.Unknown,
            1 => matches[0],
            _ => SemanticKeyFamily.Ambiguous,
        };
    }

    /// <summary>Determines whether a classified family participates in active tenant-marker evidence.</summary>
    /// <param name="family">The classified family.</param>
    /// <returns><see langword="true"/> only for active raw base/chunk and current natural-language hashes.</returns>
    public static bool IsActiveMarkerEvidenceFamily(SemanticKeyFamily family)
        => family switch
        {
            SemanticKeyFamily.ActiveRawBase
                or SemanticKeyFamily.ActiveRawChunk
                or SemanticKeyFamily.ActiveNaturalLanguage => true,
            SemanticKeyFamily.RawStaging
                or SemanticKeyFamily.NaturalLanguageStaging
                or SemanticKeyFamily.LegacyNaturalLanguage
                or SemanticKeyFamily.Unknown
                or SemanticKeyFamily.Ambiguous => false,
            _ => throw new ArgumentOutOfRangeException(
                nameof(family),
                family,
                "The semantic key family is not registered with the classifier."),
        };

    private static bool TryParseNonNegativeInt(RedisValue value, out int parsed)
    {
        parsed = 0;
        if (value.IsNull)
        {
            return false;
        }

        string valueText = value.ToString();
        return int.TryParse(valueText, NumberStyles.None, CultureInfo.InvariantCulture, out parsed)
            && parsed >= 0
            && string.Equals(
                valueText,
                parsed.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal);
    }
}
