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
        client.ConverseAsync(
            Arg.Any<IReadOnlyList<ConversationInput>>(),
            Arg.Any<ConversationOptions>(),
            Arg.Any<CancellationToken>())
            .Throws(new TaskCanceledException("deliberate timeout"));

        GenerateNaturalLanguageDescriptionActivity activity = CreateActivity(client);

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
        DaprConversationClient client)
    {
        NaturalLanguageDescriptionOptions opts = new()
        {
            DaprComponentName = "llm",
            MaxPayloadChars = 8000,
            LlmRequestTimeoutSeconds = 15,
        };
        IOptions<NaturalLanguageDescriptionOptions> options = Options.Create(opts);
        return new GenerateNaturalLanguageDescriptionActivity(
            client,
            options,
            NullLogger<GenerateNaturalLanguageDescriptionActivity>.Instance);
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
