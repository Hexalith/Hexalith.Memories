// <copyright file="GenerateNaturalLanguageDescriptionActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using System.Collections;
using System.Globalization;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;

using Dapr;
using Dapr.AI.Conversation;
using Dapr.AI.Conversation.ConversationRoles;
using Dapr.Workflow;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Story 9.2 Task 2.5 — DAPR Workflow activity that authors a single-sentence natural-language
/// description of an event payload via the DAPR Conversation API. The description feeds the second
/// embedding call (business-meaning axis) on the dual-embedding pipeline.</summary>
/// <remarks>
/// <para>Idempotency is delegated to DAPR Workflow replay: the activity output is checkpointed on first
/// success, so replays return the cached result without re-invoking the LLM. On failure the activity
/// throws <see cref="NaturalLanguageDescriptionUnavailableException"/> so the workflow can narrow its
/// <c>catch</c> to the degraded-path branch (Risk #5).</para>
/// <para>Exception surface (Spike 0.3 safe baseline): <see cref="DaprException"/>,
/// <see cref="RpcException"/>, <see cref="HttpRequestException"/>, <see cref="OperationCanceledException"/>,
/// <see cref="TaskCanceledException"/> — each is wrapped and re-thrown as
/// <see cref="NaturalLanguageDescriptionUnavailableException"/>. Dapr.AI 1.17.6 does not expose a
/// first-class <c>MaxTokens</c> property on <see cref="ConversationOptions"/>, so the 80-token ceiling is
/// forwarded best-effort via the provider <c>Parameters</c> bag when that bag accepts dictionary-style
/// assignment. Dapr.AI 1.17.6 also does not expose a <c>ConversationException</c> type; catching the
/// above is sufficient per Session 1 Spike 0.3.</para>
/// </remarks>
public sealed class GenerateNaturalLanguageDescriptionActivity
    : WorkflowActivity<NaturalLanguageDescriptionInput, NaturalLanguageDescriptionResult>
{
    internal const string SystemPrompt =
        "You are an event summarizer. Given a JSON event payload of type {EventType}, write a single "
        + "natural-language sentence (≤40 words) describing what business action occurred. "
        + "Do NOT repeat field names. Focus on domain meaning. Return ONLY the sentence, no preamble "
        + "or JSON.";

    internal const string EchoComponentName = "conversation.echo";

    private const int MaxTokens = 80;
    private const float Temperature = 0.1f;

    private readonly DaprConversationClient _conversationClient;
    private readonly IOptions<NaturalLanguageDescriptionOptions> _options;
    private readonly ILogger<GenerateNaturalLanguageDescriptionActivity> _logger;
    private readonly IWorkflowPayloadStore? _payloadStore;

    /// <summary>Initializes a new instance of the
    /// <see cref="GenerateNaturalLanguageDescriptionActivity"/> class.</summary>
    /// <param name="conversationClient">The DAPR Conversation client used to invoke the LLM.</param>
    /// <param name="options">NL-description options (timeout, payload cap, component name).</param>
    /// <param name="logger">Logger for 9150-9199 structured events.</param>
    public GenerateNaturalLanguageDescriptionActivity(
        DaprConversationClient conversationClient,
        IOptions<NaturalLanguageDescriptionOptions> options,
        ILogger<GenerateNaturalLanguageDescriptionActivity> logger,
        IWorkflowPayloadStore? payloadStore = null)
    {
        ArgumentNullException.ThrowIfNull(conversationClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _conversationClient = conversationClient;
        _options = options;
        _logger = logger;
        _payloadStore = payloadStore;
    }

    /// <inheritdoc/>
    public override async Task<NaturalLanguageDescriptionResult> RunAsync(
        WorkflowActivityContext context,
        NaturalLanguageDescriptionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MemoryUnitId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.EventType);

        NaturalLanguageDescriptionOptions options = _options.Value;
        string componentName = options.DaprComponentName;

        if (string.Equals(componentName, EchoComponentName, StringComparison.OrdinalIgnoreCase))
        {
            NaturalLanguageIntegrationLog.ConversationApiIsEchoComponent(_logger, componentName);
        }

        string rawJsonPayload = input.RawJsonPayload;
        if (input.RawPayloadReference is not null)
        {
            byte[] rawBytes = await RequirePayloadStore()
                .ReadAsync(
                    input.RawPayloadReference,
                    input.TenantId,
                    input.MemoryUnitId,
                    WorkflowPayloadKind.SourceBytes,
                    CancellationToken.None)
                .ConfigureAwait(false);
            rawJsonPayload = System.Text.Encoding.UTF8.GetString(rawBytes);
        }

        string truncatedPayload = TruncatePayload(rawJsonPayload, options.MaxPayloadChars);

        IReadOnlyList<IConversationMessage> messages = BuildMessages(
            input.EventType,
            input.AggregateType,
            truncatedPayload);

        ConversationInput conversationInput = new(messages, ScrubPII: null);
        ConversationOptions conversationOptions = CreateConversationOptions(componentName);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(options.LlmRequestTimeoutSeconds));

        using Activity? activity = MemoriesActivitySource.Instance.StartActivity(
            MemoriesActivitySource.NaturalLanguageDescriptionGeneration,
            ActivityKind.Client);
        activity?.SetTag(MemoriesActivitySource.TagTenantId, input.TenantId);
        activity?.SetTag(MemoriesActivitySource.TagMemoryUnitId, input.MemoryUnitId);
        activity?.SetTag("memories.natural_language.component", componentName);
        activity?.SetTag("memories.natural_language.event_type", input.EventType);

        Stopwatch stopwatch = Stopwatch.StartNew();
        ConversationResponse response;
        try
        {
            response = await _conversationClient
                .ConverseAsync([conversationInput], conversationOptions, cts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            // Only our per-call timeout tripped. Host shutdown / workflow termination would cancel an
            // ambient token that is not `cts.Token`, and in that case we rethrow so the memory unit is
            // not spuriously queued for retry during a clean shutdown.
            NaturalLanguageIntegrationLog.NaturalLanguageDescriptionSkippedLlmUnavailable(
                _logger,
                input.TenantId,
                input.MemoryUnitId,
                $"timeout after {options.LlmRequestTimeoutSeconds}s");
            activity?.SetTag(MemoriesActivitySource.TagOutcome, "error");
            activity?.SetTag(MemoriesActivitySource.TagErrorCode, "timeout");
            activity?.SetStatus(ActivityStatusCode.Error, "llm-timeout");
            throw new NaturalLanguageDescriptionUnavailableException(
                $"LLM call timed out after {options.LlmRequestTimeoutSeconds}s.",
                componentName,
                ex,
                context.InstanceId);
        }
        catch (RpcException ex)
        {
            NaturalLanguageIntegrationLog.NaturalLanguageDescriptionSkippedLlmUnavailable(
                _logger,
                input.TenantId,
                input.MemoryUnitId,
                $"gRPC {ex.StatusCode}");
            activity?.SetTag(MemoriesActivitySource.TagOutcome, "error");
            activity?.SetTag(MemoriesActivitySource.TagErrorCode, $"grpc-{ex.StatusCode}");
            activity?.SetStatus(ActivityStatusCode.Error, "llm-grpc-failure");
            throw new NaturalLanguageDescriptionUnavailableException(
                $"DAPR Conversation gRPC call failed: {ex.StatusCode} {ex.Status.Detail}",
                componentName,
                ex,
                context.InstanceId);
        }
        catch (DaprException ex)
        {
            NaturalLanguageIntegrationLog.NaturalLanguageDescriptionSkippedLlmUnavailable(
                _logger,
                input.TenantId,
                input.MemoryUnitId,
                "dapr-exception");
            activity?.SetTag(MemoriesActivitySource.TagOutcome, "error");
            activity?.SetTag(MemoriesActivitySource.TagErrorCode, "dapr-exception");
            activity?.SetStatus(ActivityStatusCode.Error, "llm-dapr-failure");
            throw new NaturalLanguageDescriptionUnavailableException(
                "DAPR Conversation call failed.",
                componentName,
                ex,
                context.InstanceId);
        }
        catch (HttpRequestException ex)
        {
            NaturalLanguageIntegrationLog.NaturalLanguageDescriptionSkippedLlmUnavailable(
                _logger,
                input.TenantId,
                input.MemoryUnitId,
                "http-exception");
            activity?.SetTag(MemoriesActivitySource.TagOutcome, "error");
            activity?.SetTag(MemoriesActivitySource.TagErrorCode, "http-exception");
            activity?.SetStatus(ActivityStatusCode.Error, "llm-http-failure");
            throw new NaturalLanguageDescriptionUnavailableException(
                "DAPR Conversation transport error.",
                componentName,
                ex,
                context.InstanceId);
        }

        finally
        {
            stopwatch.Stop();
            TelemetryMetricsRecorder.RecordNaturalLanguageDescriptionDuration(
                input.TenantId,
                stopwatch.Elapsed.TotalMilliseconds);
        }

        string rawResponseText = ExtractFirstChoiceText(response);

        if (!NaturalLanguageResponseCleaner.TryClean(rawResponseText, out string cleanedDescription))
        {
            NaturalLanguageIntegrationLog.NaturalLanguageDescriptionSkippedLlmUnavailable(
                _logger,
                input.TenantId,
                input.MemoryUnitId,
                "cleaner-rejected-empty-or-malformed-response");
            activity?.SetTag(MemoriesActivitySource.TagOutcome, "error");
            activity?.SetTag(MemoriesActivitySource.TagErrorCode, "cleaner-rejected");
            activity?.SetStatus(ActivityStatusCode.Error, "cleaner-rejected");
            throw new NaturalLanguageDescriptionUnavailableException(
                "NL response cleaner rejected the LLM response as empty or malformed.",
                componentName,
                context.InstanceId);
        }

        string llmProvider = componentName;
        string llmModel = ExtractModel(response) ?? "unknown";

        // Dapr.AI 1.17.6 does NOT expose logprobs on ConversationResultChoice (only FinishReason, Index,
        // Message). Review Finding D1 was resolved as "formally defer" — see deferred-work.md entry
        // "Story 9.2 D1 — logprobs-based confidence extraction" gated on a future Dapr.AI SDK surface
        // exposing logprobs on the response shape. Until then, ConfidenceSource is always Constant +
        // EstimatedConfidence is null, so UIs render "AI-inferred (unmeasured)" and never a pseudo-number.
        ConfidenceSource confidenceSource = ConfidenceSource.Constant;
        float? estimatedConfidence = null;

        NaturalLanguageIntegrationLog.NaturalLanguageDescriptionGenerated(
            _logger,
            input.TenantId,
            input.MemoryUnitId,
            llmProvider,
            llmModel,
            stopwatch.ElapsedMilliseconds,
            confidenceSource.ToString().ToLowerInvariant());

        activity?.SetTag("memories.natural_language.llm_model", llmModel);
        activity?.SetTag("memories.natural_language.duration_ms", stopwatch.ElapsedMilliseconds);
        activity?.SetTag("memories.natural_language.confidence_source", confidenceSource.ToString().ToLowerInvariant());
        activity?.SetTag(MemoriesActivitySource.TagOutcome, "ok");
        activity?.SetStatus(ActivityStatusCode.Ok);

        return new NaturalLanguageDescriptionResult(
            cleanedDescription,
            estimatedConfidence,
            confidenceSource,
            llmProvider,
            llmModel);
    }

    private static ConversationOptions CreateConversationOptions(string componentName)
    {
        ConversationOptions conversationOptions = new(componentName)
        {
            Temperature = Temperature,
        };

        TryApplyMaxTokenHint(conversationOptions, MaxTokens);
        return conversationOptions;
    }

    private IWorkflowPayloadStore RequirePayloadStore()
        => _payloadStore ?? throw new WorkflowPayloadException("PAYLOAD_STORE_UNAVAILABLE", "nl-raw-event");

    private static string TruncatePayload(string? rawPayload, int maxChars)
    {
        if (string.IsNullOrEmpty(rawPayload) || maxChars <= 0)
        {
            return string.Empty;
        }

        if (rawPayload.Length <= maxChars)
        {
            return rawPayload;
        }

        // Avoid splitting a surrogate pair at the truncation boundary — cutting between the high
        // and low halves produces malformed UTF-16 that Dapr gRPC / the LLM provider may reject.
        int cut = maxChars;
        if (char.IsHighSurrogate(rawPayload[cut - 1]) && cut < rawPayload.Length && char.IsLowSurrogate(rawPayload[cut]))
        {
            cut--;
        }

        return rawPayload[..cut];
    }

    private static void TryApplyMaxTokenHint(ConversationOptions conversationOptions, int maxTokens)
    {
        ArgumentNullException.ThrowIfNull(conversationOptions);

        PropertyInfo? parametersProperty = typeof(ConversationOptions).GetProperty(nameof(ConversationOptions.Parameters));
        if (parametersProperty is null || !parametersProperty.CanWrite)
        {
            return;
        }

        string maxTokenText = maxTokens.ToString(CultureInfo.InvariantCulture);
        object? currentParameters = parametersProperty.CanRead
            ? parametersProperty.GetValue(conversationOptions)
            : null;

        if (TryPopulateParameterBag(currentParameters, maxTokenText, maxTokens))
        {
            return;
        }

        Any maxTokenValue = Any.Pack(new Int32Value { Value = maxTokens });
        object[] candidates =
        [
            new Dictionary<string, Any>(StringComparer.Ordinal)
            {
                ["max_tokens"] = maxTokenValue,
                ["maxTokens"] = maxTokenValue,
            },
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["max_tokens"] = maxTokenText,
                ["maxTokens"] = maxTokenText,
            },
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["max_tokens"] = maxTokens,
                ["maxTokens"] = maxTokens,
            },
        ];

        foreach (object candidate in candidates)
        {
            if (parametersProperty.PropertyType.IsAssignableFrom(candidate.GetType()))
            {
                parametersProperty.SetValue(conversationOptions, candidate);
                return;
            }
        }

        // Review P20: no candidate shape matched — the MaxTokens ceiling is NOT applied for this SDK
        // version. Left silent, an upstream Dapr.AI rename blows past the 80-token cost envelope with
        // zero signal. Emit a span event so the drift surfaces in tracing without needing ILogger on
        // a static path.
        Activity.Current?.AddEvent(new ActivityEvent(
            "memories.natural_language.max_tokens_hint_skipped",
            tags: new ActivityTagsCollection
            {
                { "parameters_type", parametersProperty.PropertyType.FullName ?? "unknown" },
            }));
    }

    private static bool TryPopulateParameterBag(object? parameterBag, string maxTokenText, int maxTokens)
    {
        Any maxTokenValue = Any.Pack(new Int32Value { Value = maxTokens });

        switch (parameterBag)
        {
            case IDictionary<string, Any> anyDictionary:
                anyDictionary["max_tokens"] = maxTokenValue;
                anyDictionary["maxTokens"] = maxTokenValue;
                return true;
            case IDictionary<string, string> stringDictionary:
                stringDictionary["max_tokens"] = maxTokenText;
                stringDictionary["maxTokens"] = maxTokenText;
                return true;
            case IDictionary<string, object?> objectDictionary:
                objectDictionary["max_tokens"] = maxTokens;
                objectDictionary["maxTokens"] = maxTokens;
                return true;
            case IDictionary dictionary:
                try
                {
                    dictionary["max_tokens"] = maxTokenValue;
                    dictionary["maxTokens"] = maxTokenValue;
                    return true;
                }
                catch (ArgumentException)
                {
                    return false;
                }
            default:
                return false;
        }
    }

    private static IReadOnlyList<IConversationMessage> BuildMessages(
        string eventType,
        string? aggregateType,
        string truncatedPayload)
    {
        string aggregateLabel = string.IsNullOrWhiteSpace(aggregateType)
            ? "(unspecified)"
            : aggregateType;

        // Review D8: the `{EventType}` placeholder is replaced into the system prompt verbatim. If the
        // caller-supplied eventType contains additional instruction text or template markers, an
        // attacker-controlled CloudEvent.Type value would alter the LLM instructions. CloudEvents
        // spec constrains `type` to non-empty strings but does not forbid arbitrary text, so we
        // defensively restrict to a safe event-name character set and sanitize otherwise.
        string safeEventType = SanitizeEventTypeForPrompt(eventType);

        string systemPromptResolved = SystemPrompt.Replace(
            "{EventType}",
            safeEventType,
            StringComparison.Ordinal);

        SystemMessage systemMessage = new()
        {
            Content = [new MessageContent(systemPromptResolved)],
        };

        UserMessage userMessage = new()
        {
            Content =
            [
                new MessageContent(
                    $"Event type: {safeEventType}\n"
                    + $"Aggregate: {aggregateLabel}\n"
                    + $"Payload:\n{truncatedPayload}"),
            ],
        };

        return [systemMessage, userMessage];
    }

    private static string SanitizeEventTypeForPrompt(string eventType)
    {
        // Conservative allow-list: CloudEvents `type` values in this project are domain-typed names
        // like `com.itaneo.memories.MemoryUnitIngestedV1`. Allow letters, digits, dot, dash,
        // underscore, and colon (colon is used by reverse-DNS-style type identifiers). Other chars
        // are replaced with `_` so the LLM cannot read attacker-controlled template/instruction
        // text through the `{EventType}` expansion or the user-message echo.
        Span<char> buffer = stackalloc char[Math.Min(eventType.Length, 256)];
        int written = 0;
        foreach (char c in eventType)
        {
            if (written >= buffer.Length)
            {
                break;
            }

            bool safe = char.IsAsciiLetterOrDigit(c)
                || c == '.'
                || c == '-'
                || c == '_'
                || c == ':';
            buffer[written++] = safe ? c : '_';
        }

        return written == 0 ? "unknown" : new string(buffer[..written]);
    }

    private static string ExtractFirstChoiceText(ConversationResponse response)
    {
        if (response?.Outputs is null || response.Outputs.Count == 0)
        {
            return string.Empty;
        }

        ConversationResponseResult firstOutput = response.Outputs[0];
        if (firstOutput?.Choices is null || firstOutput.Choices.Count == 0)
        {
            return string.Empty;
        }

        ResultMessage? message = firstOutput.Choices[0]?.Message;
        return message?.Content ?? string.Empty;
    }

    private static string? ExtractModel(ConversationResponse response)
    {
        if (response?.Outputs is null || response.Outputs.Count == 0)
        {
            return null;
        }

        string? model = response.Outputs[0]?.Model;
        return string.IsNullOrWhiteSpace(model) ? null : model;
    }
}
