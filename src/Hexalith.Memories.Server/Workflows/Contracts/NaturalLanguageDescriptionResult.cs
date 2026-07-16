// <copyright file="NaturalLanguageDescriptionResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Story 9.2 Task 2.2 — output of <c>GenerateNaturalLanguageDescriptionActivity</c>. Contains the
/// LLM-authored single-sentence summary, a nullable confidence proxy, and provenance for operator
/// inspection.</summary>
/// <param name="Description">The cleaned natural-language sentence. Always non-empty on success
/// (activity throws <c>NaturalLanguageDescriptionUnavailableException</c> when cleaner rejects the
/// response).</param>
/// <param name="EstimatedConfidence">A numeric confidence in <c>[0, 1]</c> when
/// <paramref name="ConfidenceSource"/> is <see cref="V1.ConfidenceSource.Logprobs"/>;
/// <see langword="null"/> when the provider does not expose a usable signal
/// (<see cref="V1.ConfidenceSource.Constant"/>) or extraction failed
/// (<see cref="V1.ConfidenceSource.Unknown"/>). Nullable by design: UIs must render
/// "measured vs. unmeasured" as structurally distinct — a default numeric would weaponize the signal.</param>
/// <param name="ConfidenceSource">Classifies how <paramref name="EstimatedConfidence"/> was derived. See
/// <see cref="V1.ConfidenceSource"/>.</param>
/// <param name="LlmProvider">The provider name — best-effort value extracted from the response metadata
/// or falling back to the resolved DAPR Conversation component name.</param>
/// <param name="LlmModel">The model identifier — e.g., <c>"gpt-4o-mini"</c>. Falls back to
/// <c>"unknown"</c> when the response does not expose it.</param>
public sealed record NaturalLanguageDescriptionResult(
    string Description,
    float? EstimatedConfidence,
    ConfidenceSource ConfidenceSource,
    string LlmProvider,
    string LlmModel);
