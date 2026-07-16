// <copyright file="NaturalLanguageConsistencyState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Consistency;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

/// <summary>Consistency helpers for the natural-language semantic sibling hash.</summary>
internal static class NaturalLanguageConsistencyState
{
    public const string EmbeddingStatusMetadataKey = "event.naturalLanguageEmbeddingStatus";

    public static NaturalLanguageEmbeddingStatus ReadStatus(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return NaturalLanguageEmbeddingStatus.NotApplicable;
        }

        try
        {
            Dictionary<string, MetadataField>? metadata = JsonSerializer.Deserialize<Dictionary<string, MetadataField>>(
                metadataJson,
                MemoriesJsonContext.Options);

            if (metadata is not null
                && metadata.TryGetValue(EmbeddingStatusMetadataKey, out MetadataField? field)
                && Enum.TryParse(field.Value, ignoreCase: true, out NaturalLanguageEmbeddingStatus parsed))
            {
                return parsed;
            }
        }
        catch (JsonException)
        {
            return NaturalLanguageEmbeddingStatus.NotApplicable;
        }

        return NaturalLanguageEmbeddingStatus.NotApplicable;
    }

    public static string? BuildConsistencyNote(
        NaturalLanguageEmbeddingStatus status,
        bool naturalLanguageSemanticPresent)
        => status switch
        {
            NaturalLanguageEmbeddingStatus.Queued when !naturalLanguageSemanticPresent
                => "Natural-language semantic hash pending queued retry.",
            NaturalLanguageEmbeddingStatus.Indexed when !naturalLanguageSemanticPresent
                => "Missing backends: semantic-nl",
            _ => null,
        };

    /// <summary>Story 9.2 Review D7 — typed canonical identifier for the informational note emitted by
    /// <see cref="BuildConsistencyNote"/>. Paired so the free-form string keeps its on-the-wire shape
    /// for existing consumers while typed filtering now works without string parsing.</summary>
    public static ConsistencyNoteKind BuildConsistencyNoteKind(
        NaturalLanguageEmbeddingStatus status,
        bool naturalLanguageSemanticPresent)
        => status switch
        {
            NaturalLanguageEmbeddingStatus.Queued when !naturalLanguageSemanticPresent
                => ConsistencyNoteKind.NaturalLanguageEmbeddingQueued,
            NaturalLanguageEmbeddingStatus.Indexed when !naturalLanguageSemanticPresent
                => ConsistencyNoteKind.NaturalLanguageEmbeddingMissing,
            _ => ConsistencyNoteKind.None,
        };
}