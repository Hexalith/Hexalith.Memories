// <copyright file="NaturalLanguageDescriptionUnavailableException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

/// <summary>Story 9.2 Task 2.3 — thrown by <c>GenerateNaturalLanguageDescriptionActivity</c> when the
/// LLM call cannot produce a valid natural-language description after the activity's internal retry
/// exhausts (timeout, gRPC / HTTP fault, malformed-or-empty response after cleaning).</summary>
/// <remarks>This is a TYPED exception distinct from a transient <see cref="Exception"/> so the
/// ingestion workflow's <c>catch</c> can narrow to the degraded-path branch without also swallowing
/// unrelated activity failures (Risk #5). The workflow catches ONLY this exception and falls through to
/// the retry queue; all other exceptions propagate for workflow-level retry under the standard policy.
/// </remarks>
public sealed class NaturalLanguageDescriptionUnavailableException : Exception
{
    /// <summary>Initializes a new instance of the
    /// <see cref="NaturalLanguageDescriptionUnavailableException"/> class.</summary>
    /// <param name="message">A human-readable message describing the failure point.</param>
    /// <param name="llmProvider">The resolved LLM provider / DAPR Conversation component name.</param>
    /// <param name="correlationId">The optional DAPR / workflow correlation identifier.</param>
    public NaturalLanguageDescriptionUnavailableException(
        string message,
        string llmProvider,
        string? correlationId = null)
        : base(message)
    {
        LlmProvider = llmProvider;
        CorrelationId = correlationId;
    }

    /// <summary>Initializes a new instance of the
    /// <see cref="NaturalLanguageDescriptionUnavailableException"/> class with an inner exception.</summary>
    /// <param name="message">A human-readable message describing the failure point.</param>
    /// <param name="llmProvider">The resolved LLM provider / DAPR Conversation component name.</param>
    /// <param name="innerException">The original exception thrown by the LLM call.</param>
    /// <param name="correlationId">The optional DAPR / workflow correlation identifier.</param>
    public NaturalLanguageDescriptionUnavailableException(
        string message,
        string llmProvider,
        Exception innerException,
        string? correlationId = null)
        : base(message, innerException)
    {
        LlmProvider = llmProvider;
        CorrelationId = correlationId;
    }

    /// <summary>Gets the resolved LLM provider (DAPR Conversation component name) that was called when
    /// the failure occurred. Always non-null for diagnostic triage.</summary>
    public string LlmProvider { get; }

    /// <summary>Gets an optional DAPR / workflow correlation identifier, when available.</summary>
    public string? CorrelationId { get; }
}
