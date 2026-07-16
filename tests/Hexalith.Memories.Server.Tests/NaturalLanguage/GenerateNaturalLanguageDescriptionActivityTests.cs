// <copyright file="GenerateNaturalLanguageDescriptionActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

// Dapr.AI 1.17.6 marks DaprConversationClient [Experimental("DAPR_CONVERSATION")]. The test file needs to
// construct / substitute the type to mock its behavior; the suppression is narrowly scoped to this file
// only (Story 9.2 Risk #1 — NoWarn on Directory.Build.props is still forbidden).
#pragma warning disable DAPR_CONVERSATION

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using Dapr;
using Dapr.AI.Conversation;
using Dapr.Workflow;

using Grpc.Core;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>Story 9.2 Task 2.8 — tests for <see cref="GenerateNaturalLanguageDescriptionActivity"/>.
/// Covers AC #1, #5, Risk #5 (degraded path), and exception-surface safe-baseline (Spike 0.3).</summary>
public sealed class GenerateNaturalLanguageDescriptionActivityTests
{
    private const string TenantId = "t-1";
    private const string MemoryUnitId = "mu-1";
    private const string EventType = "CounterIncrementedV1";
    private const string RawJsonPayload = "{\"counterId\":\"c-1\",\"increment\":42}";

    [Fact]
    public async Task SuccessPath_ReturnsDescription_WithConfidenceSourceConstant()
    {
        string expected = "A counter named c-1 was incremented by 42.";
        DaprConversationClient client = CreateClientReturning(expected, model: "gpt-4o-mini");

        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        NaturalLanguageDescriptionResult result = await activity.RunAsync(
            context,
            new NaturalLanguageDescriptionInput(TenantId, MemoryUnitId, RawJsonPayload, EventType, "Counter"));

        result.Description.ShouldBe(expected);
        result.EstimatedConfidence.ShouldBeNull();
        result.ConfidenceSource.ShouldBe(ConfidenceSource.Constant);
        result.LlmProvider.ShouldBe("llm");
        result.LlmModel.ShouldBe("gpt-4o-mini");
    }

    [Fact]
    public async Task SuccessPath_EmitsNaturalLanguageDescriptionGenerationSpan()
    {
        // Story 9.2 Review D5 — the distributed-trace span named "memories.natural_language.description"
        // MUST be emitted on every LLM call so operators can attribute latency and failure rates per
        // tenant in traces (Risk #2 diagnostics). The span carries tenant_id, memory_unit_id, and an
        // outcome tag.
        DaprConversationClient client = CreateClientReturning("A user signed in.", model: "gpt-4o-mini");
        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client);

        List<System.Diagnostics.Activity> observed = [];
        using System.Diagnostics.ActivityListener listener = new()
        {
            ShouldListenTo = source =>
                source.Name == Hexalith.Memories.Telemetry.MemoriesActivitySource.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _)
                => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = a =>
            {
                if (a.OperationName == Hexalith.Memories.Telemetry.MemoriesActivitySource.NaturalLanguageDescriptionGeneration)
                {
                    observed.Add(a);
                }
            },
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new NaturalLanguageDescriptionInput(TenantId, MemoryUnitId, RawJsonPayload, EventType, "Counter"));

        System.Diagnostics.Activity span = observed.ShouldHaveSingleItem();
        span.Tags.ShouldContain(t =>
            t.Key == Hexalith.Memories.Telemetry.MemoriesActivitySource.TagTenantId
            && t.Value == TenantId);
        span.Tags.ShouldContain(t =>
            t.Key == Hexalith.Memories.Telemetry.MemoriesActivitySource.TagMemoryUnitId
            && t.Value == MemoryUnitId);
        span.Tags.ShouldContain(t =>
            t.Key == Hexalith.Memories.Telemetry.MemoriesActivitySource.TagOutcome
            && t.Value == "ok");
        span.Status.ShouldBe(System.Diagnostics.ActivityStatusCode.Ok);
    }

    [Fact]
    public async Task UnknownModel_FallsBackToUnknownString()
    {
        DaprConversationClient client = CreateClientReturning(
            "A policy renewal was completed for customer c-42.",
            model: null);

        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client);

        NaturalLanguageDescriptionResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new NaturalLanguageDescriptionInput(TenantId, MemoryUnitId, RawJsonPayload, EventType, null));

        result.LlmModel.ShouldBe("unknown");
    }

    [Fact]
    public async Task DaprException_ThrowsUnavailableException()
    {
        DaprConversationClient client = Substitute.For<DaprConversationClient>(
            null,
            new HttpClient(),
            null);
        client.ConverseAsync(
            Arg.Any<IReadOnlyList<ConversationInput>>(),
            Arg.Any<ConversationOptions>(),
            Arg.Any<CancellationToken>())
            .Throws(new DaprException("sidecar unavailable"));

        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client);

        NaturalLanguageDescriptionUnavailableException ex =
            await Should.ThrowAsync<NaturalLanguageDescriptionUnavailableException>(
                () => activity.RunAsync(
                    Substitute.For<WorkflowActivityContext>(),
                    new NaturalLanguageDescriptionInput(TenantId, MemoryUnitId, RawJsonPayload, EventType, null)));

        ex.LlmProvider.ShouldBe("llm");
        ex.InnerException.ShouldBeOfType<DaprException>();
    }

    [Fact]
    public async Task RpcException_ThrowsUnavailableException()
    {
        DaprConversationClient client = Substitute.For<DaprConversationClient>(
            null,
            new HttpClient(),
            null);
        client.ConverseAsync(
            Arg.Any<IReadOnlyList<ConversationInput>>(),
            Arg.Any<ConversationOptions>(),
            Arg.Any<CancellationToken>())
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "grpc down")));

        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client);

        NaturalLanguageDescriptionUnavailableException ex =
            await Should.ThrowAsync<NaturalLanguageDescriptionUnavailableException>(
                () => activity.RunAsync(
                    Substitute.For<WorkflowActivityContext>(),
                    new NaturalLanguageDescriptionInput(TenantId, MemoryUnitId, RawJsonPayload, EventType, null)));

        ex.InnerException.ShouldBeOfType<RpcException>();
    }

    [Fact]
    public async Task Timeout_ThrowsUnavailableException()
    {
        DaprConversationClient client = Substitute.For<DaprConversationClient>(
            null,
            new HttpClient(),
            null);

        // Simulate a real LLM hang: await the caller-supplied token so the activity's own timeout
        // cts triggers the OperationCanceledException. This exercises the production catch filter
        // `when (cts.Token.IsCancellationRequested)` — any other source of cancellation rethrows.
        client.ConverseAsync(
            Arg.Any<IReadOnlyList<ConversationInput>>(),
            Arg.Any<ConversationOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                CancellationToken ct = call.Arg<CancellationToken>();
                await Task.Delay(Timeout.Infinite, ct);
                return null!; // unreachable — Task.Delay throws on cancellation.
            });

        // Force a very short per-call timeout so the cts fires promptly under test.
        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client, llmRequestTimeoutSeconds: 1);

        NaturalLanguageDescriptionUnavailableException ex =
            await Should.ThrowAsync<NaturalLanguageDescriptionUnavailableException>(
                () => activity.RunAsync(
                    Substitute.For<WorkflowActivityContext>(),
                    new NaturalLanguageDescriptionInput(TenantId, MemoryUnitId, RawJsonPayload, EventType, null)));

        ex.Message.ShouldContain("timed out");
    }

    [Fact]
    public async Task HttpRequestException_ThrowsUnavailableException()
    {
        DaprConversationClient client = Substitute.For<DaprConversationClient>(
            null,
            new HttpClient(),
            null);
        client.ConverseAsync(
            Arg.Any<IReadOnlyList<ConversationInput>>(),
            Arg.Any<ConversationOptions>(),
            Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("network failure"));

        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client);

        await Should.ThrowAsync<NaturalLanguageDescriptionUnavailableException>(
            () => activity.RunAsync(
                Substitute.For<WorkflowActivityContext>(),
                new NaturalLanguageDescriptionInput(TenantId, MemoryUnitId, RawJsonPayload, EventType, null)));
    }

    [Fact]
    public async Task EmptyResponseAfterCleaning_ThrowsUnavailableException()
    {
        DaprConversationClient client = CreateClientReturning(string.Empty, model: "gpt-4o-mini");

        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client);

        NaturalLanguageDescriptionUnavailableException ex =
            await Should.ThrowAsync<NaturalLanguageDescriptionUnavailableException>(
                () => activity.RunAsync(
                    Substitute.For<WorkflowActivityContext>(),
                    new NaturalLanguageDescriptionInput(TenantId, MemoryUnitId, RawJsonPayload, EventType, null)));

        ex.Message.ShouldContain("cleaner rejected", Shouldly.Case.Insensitive);
    }

    [Fact]
    public async Task MarkdownFencedResponse_IsCleaned()
    {
        string fenced = "```\nA shipment was dispatched to customer c-7.\n```";
        DaprConversationClient client = CreateClientReturning(fenced, model: "gpt-4o-mini");

        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client);

        NaturalLanguageDescriptionResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new NaturalLanguageDescriptionInput(TenantId, MemoryUnitId, RawJsonPayload, EventType, null));

        result.Description.ShouldBe("A shipment was dispatched to customer c-7.");
    }

    [Fact]
    public async Task PayloadExceedsMaxChars_IsTruncated_AndStillSucceeds()
    {
        string hugePayload = new('x', 9000);
        DaprConversationClient client = CreateClientReturning(
            "Payload truncation did not affect summarization.",
            model: "gpt-4o-mini");

        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client);

        NaturalLanguageDescriptionResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new NaturalLanguageDescriptionInput(TenantId, MemoryUnitId, hugePayload, EventType, null));

        result.Description.ShouldContain("Payload truncation");
    }

    [Fact]
    public async Task SuccessPath_ConfiguresConversationOptionsWithPinnedTemperature()
    {
        DaprConversationClient client = CreateClientReturning(
            "A counter named c-1 was incremented by 42.",
            model: "gpt-4o-mini");

        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client);

        _ = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new NaturalLanguageDescriptionInput(TenantId, MemoryUnitId, RawJsonPayload, EventType, "Counter"));

        await client.Received(1).ConverseAsync(
            Arg.Any<IReadOnlyList<ConversationInput>>(),
            Arg.Is<ConversationOptions>(options =>
                options.Temperature.HasValue
                && Math.Abs(options.Temperature.Value - 0.1d) < 0.0001d),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuccessPath_WithRawPayloadReferenceScopedToDedupInstance_ReadsUsingReferenceScope()
    {
        const string sourcePayloadScopeId = "dedup:t-1:case-1:abc123";
        byte[] rawPayload = System.Text.Encoding.UTF8.GetBytes(RawJsonPayload);
        WorkflowPayloadReference reference = new(
            $"{sourcePayloadScopeId}:sourcebytes:hash",
            "hash",
            rawPayload.Length,
            WorkflowPayloadKind.SourceBytes,
            TenantId,
            sourcePayloadScopeId);
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        payloadStore
            .ReadAsync(reference, TenantId, sourcePayloadScopeId, WorkflowPayloadKind.SourceBytes, Arg.Any<CancellationToken>())
            .Returns(rawPayload);
        DaprConversationClient client = CreateClientReturning("A counter was incremented.", model: "gpt-4o-mini");
        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client, payloadStore: payloadStore);

        NaturalLanguageDescriptionResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new NaturalLanguageDescriptionInput(TenantId, MemoryUnitId, string.Empty, EventType, "Counter", reference));

        result.Description.ShouldBe("A counter was incremented.");
        await payloadStore.Received(1).ReadAsync(
            reference,
            TenantId,
            sourcePayloadScopeId,
            WorkflowPayloadKind.SourceBytes,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void PromptContainsHallucinationGuardance()
    {
        // Structural test — verifies the prompt string contains the hallucination-avoidance constraints
        // so future edits cannot silently drop them. (Task 2.8 / Risk #8 documentation-control test.)
        GenerateNaturalLanguageDescriptionActivity.SystemPrompt.ShouldContain("Focus on domain meaning");
        GenerateNaturalLanguageDescriptionActivity.SystemPrompt.ShouldContain("Return ONLY");
        GenerateNaturalLanguageDescriptionActivity.SystemPrompt.ShouldContain("no preamble");
    }

    [Fact]
    public void PayloadWithCustomerPii_SummaryMayContainPii_DocumentedBehavior()
    {
        // Documentation-control test (Murat / Task 2.8). The name IS the warning: NL descriptions inherit
        // any PII present in the raw payload unless operators enable `scrubPII: true` via DAPR component
        // metadata. Task 10.1.10 PII_ACKNOWLEDGMENT.md tracks explicit sign-off. This test's existence
        // signals that the risk is known and tracked.
        GenerateNaturalLanguageDescriptionActivity.SystemPrompt.ShouldNotContain("redact");
        GenerateNaturalLanguageDescriptionActivity.SystemPrompt.ShouldNotContain("scrub");
    }

    private static GenerateNaturalLanguageDescriptionActivity CreateActivity(
        DaprConversationClient client,
        int llmRequestTimeoutSeconds = 15,
        IWorkflowPayloadStore? payloadStore = null)
    {
        NaturalLanguageDescriptionOptions opts = new()
        {
            DaprComponentName = "llm",
            MaxPayloadChars = 8000,
            LlmRequestTimeoutSeconds = llmRequestTimeoutSeconds,
        };
        IOptions<NaturalLanguageDescriptionOptions> options = Options.Create(opts);
        return new GenerateNaturalLanguageDescriptionActivity(
            client,
            options,
            NullLogger<GenerateNaturalLanguageDescriptionActivity>.Instance,
            payloadStore);
    }

    private static DaprConversationClient CreateClientReturning(string content, string? model)
    {
        DaprConversationClient client = Substitute.For<DaprConversationClient>(
            null,
            new HttpClient(),
            null);

        ResultMessage message = new(content);
        ConversationResultChoice choice = new(null, 0, message);
        ConversationResponseResult output = new([choice])
        {
            Model = model!,
        };
        ConversationResponse response = new([output], string.Empty);

        client.ConverseAsync(
            Arg.Any<IReadOnlyList<ConversationInput>>(),
            Arg.Any<ConversationOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        return client;
    }
}
