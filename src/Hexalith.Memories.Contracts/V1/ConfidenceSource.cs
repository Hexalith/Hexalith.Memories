// <copyright file="ConfidenceSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Story 9.2 Task 2.2 — classifies how the confidence accompanying an LLM-authored natural-
/// language description was derived. Promoted from deferred (Dr. Quinn + Freya) via Occam refinement:
/// a constant numeric indistinguishable from a real measurement weaponizes the UX signal; a nullable
/// <c>EstimatedConfidence</c> paired with this enum makes "measured vs. unmeasured" structurally
/// distinct so UIs cannot render a default as a measurement.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<ConfidenceSource>))]
public enum ConfidenceSource
{
    /// <summary>The confidence value is a computed aggregate of LLM token log-probabilities
    /// (<c>exp(avg(logprob))</c> over the response tokens). Only emitted when the provider exposes
    /// logprobs (e.g., OpenAI via <c>conversation.openai</c> with <c>logprobs=true</c>).</summary>
    Logprobs,

    /// <summary>The confidence value is absent because the provider does not expose a numeric signal
    /// (e.g., Anthropic Claude, <c>conversation.echo</c> in local dev). The paired
    /// <see cref="V1"/>.<c>NaturalLanguageDescriptionResult.EstimatedConfidence</c> is
    /// <see langword="null"/>. UIs render this distinctly (e.g., "AI-inferred (estimate unavailable)").</summary>
    Constant,

    /// <summary>The confidence value is absent because the LLM response was malformed, partial, or the
    /// activity failed to extract a usable signal. Paired <c>EstimatedConfidence</c> is
    /// <see langword="null"/>. Unlike <see cref="Constant"/>, this state indicates a fault rather than a
    /// provider capability mismatch.</summary>
    Unknown,
}
