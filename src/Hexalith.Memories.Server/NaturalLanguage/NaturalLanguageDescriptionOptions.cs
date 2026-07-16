// <copyright file="NaturalLanguageDescriptionOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

/// <summary>Options for the Story 9.2 dual-embedding pipeline — specifically the LLM-authored natural-
/// language description path via the DAPR Conversation API.</summary>
/// <remarks>
/// This is a <see langword="sealed"/> <see langword="class"/> (not a record) with a parameterless
/// constructor and settable properties: <see cref="Microsoft.Extensions.Configuration.IConfiguration.Bind(object)"/>
/// requires this shape for reliable section binding. Positional records bind silently-empty — see Amelia's
/// Party Mode gotcha in the Story 9.2 Review Findings Log.
/// </remarks>
public sealed class NaturalLanguageDescriptionOptions
{
    /// <summary>Gets or sets the DAPR Conversation component name the server resolves at runtime.
    /// Default <c>"llm"</c>. MUST NOT be <c>"conversation.echo"</c> in Production (Risk #10 — validator
    /// fails fast with log event 9161 Critical).</summary>
    public string DaprComponentName { get; set; } = "llm";

    /// <summary>Gets or sets the maximum number of characters of the raw JSON payload sent to the LLM.
    /// Larger payloads are truncated before the prompt is built to stay under provider context windows
    /// and to bound per-call cost.</summary>
    public int MaxPayloadChars { get; set; } = 8000;

    /// <summary>Gets or sets the retry interval (seconds) for
    /// <c>NaturalLanguageEmbeddingRetryHostedService</c>. Tests override to 1s to avoid 60s flaky waits.</summary>
    public int RetryIntervalSeconds { get; set; } = 60;

    /// <summary>Gets or sets the batch size dequeued per retry tick per tenant. Keeps the retry path from
    /// starving live ingestion by throttling NL API volume.</summary>
    public int BatchSize { get; set; } = 5;

    /// <summary>Gets or sets the maximum retry attempts before a record moves to the dead-letter
    /// (<c>nl-embedding-retry-dead:{tenantId}</c>) sorted set for operator triage.</summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>Gets or sets the per-<c>ConverseAsync</c>-call timeout (seconds). Fires the cancellation
    /// token when exceeded so the workflow falls through to the degraded queue path rather than blocking
    /// on a chronic outage (Risk #2 — NFR6 relaxation envelope).</summary>
    public int LlmRequestTimeoutSeconds { get; set; } = 15;

    /// <summary>Gets or sets whether the LLM-authored description is additionally persisted to
    /// <c>metadata["event.naturalLanguageDescription"]</c>. Default <see langword="false"/> (ADR 9.2-F —
    /// storage economy; operators with FT.SEARCH-heavy inspection can opt in).</summary>
    public bool PersistInMetadata { get; set; } = false;

    /// <summary>Gets or sets the sliding-window (seconds) the rate-limiter sizing validator uses to decide
    /// whether a tenant's <c>EmbeddingRateLimiterActor</c> ceiling is under-sized for sustained dual-
    /// embedding traffic. First-event bursts do NOT trigger the warning — only sustained under-sizing
    /// across this window (Risk #6 / Improvement AB).</summary>
    public int RateLimiterSizingWindowSeconds { get; set; } = 900;

    /// <summary>Gets or sets the maximum bytes of raw JSON payload carried on each entry in
    /// <c>nl-embedding-retry:{tenantId}</c> when Spike 0.1 resolves to bounded payload-by-value
    /// (the default after the 2026-04-23 UNCLEAR determination). Bounded count × bounded bytes = bounded
    /// Redis memory, protecting pre-mortem Failure δ.</summary>
    public int QueuedPayloadMaxBytes { get; set; } = 4096;

    /// <summary>Gets or sets the maximum live retry entries retained per tenant.</summary>
    public int LiveRetryQueueMaxEntries { get; set; } = 1000;

    /// <summary>Gets or sets the maximum dead-letter retry entries retained per tenant.</summary>
    public int DeadRetryQueueMaxEntries { get; set; } = 1000;

    /// <summary>Gets or sets whether operators have explicitly acknowledged cross-tenant LLM-response cache
    /// sharing at the DAPR sidecar level (Risk #16). REQUIRED to be <see langword="true"/> when the
    /// resolved component YAML specifies a non-zero <c>responseCacheTTL</c>. Default <see langword="false"/>.
    /// The options validator fails fast with log event 9164 Critical if the YAML has non-zero TTL without
    /// this acknowledgment (or its env-var twin <c>HEXALITH_ACCEPT_CROSS_TENANT_CACHE_SHARING=1</c>).</summary>
    public bool AcceptCrossTenantCacheSharing { get; set; } = false;
}
