namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using System.Text;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

public class ExtractContentActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldDelegateToClientAndReturnResult()
    {
        // Arrange
        IContentExtractionClient client = Substitute.For<IContentExtractionClient>();
        ExtractionInput input = CreateTestInput();
        Contracts.V1.ExtractionResult expected = new(
            "extracted text",
            "abc123",
            DateTimeOffset.UtcNow);

        client.ExtractAsync(input, Arg.Any<CancellationToken>()).Returns(expected);

        ExtractContentActivity activity = new(client, CreateGate());
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        // Act
        Contracts.V1.ExtractionResult result = await activity.RunAsync(context, input);

        // Assert
        result.ShouldBe(expected);
        await client.Received(1).ExtractAsync(input, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithPayloadReference_ResolvesSourceBytesAndClaimChecksExtractedText()
    {
        IContentExtractionClient client = Substitute.For<IContentExtractionClient>();
        byte[] sourceBytes = Encoding.UTF8.GetBytes("raw source");
        WorkflowPayloadReference sourceReference = new(
            "mu-1:sourcebytes:source",
            "source",
            sourceBytes.Length,
            WorkflowPayloadKind.SourceBytes,
            "test-tenant",
            "mu-1");
        WorkflowPayloadReference extractedReference = new(
            "mu-1:extractedtext:extracted",
            "extracted",
            "extracted text".Length,
            WorkflowPayloadKind.ExtractedText,
            "test-tenant",
            "mu-1");
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        payloadStore
            .ReadAsync(sourceReference, "test-tenant", "mu-1", WorkflowPayloadKind.SourceBytes, Arg.Any<CancellationToken>())
            .Returns(sourceBytes);
        payloadStore
            .SaveAsync(
                "test-tenant",
                "mu-1",
                WorkflowPayloadKind.ExtractedText,
                Arg.Any<ReadOnlyMemory<byte>>(),
                null,
                Arg.Any<CancellationToken>())
            .Returns(extractedReference);
        ExtractionInput input = CreateTestInput() with
        {
            ContentBytes = [],
            MemoryUnitId = "mu-1",
            PayloadReference = sourceReference,
        };
        Contracts.V1.ExtractionResult extracted = new("extracted text", "abc123", DateTimeOffset.UtcNow);
        client.ExtractAsync(Arg.Any<ExtractionInput>(), Arg.Any<CancellationToken>()).Returns(extracted);

        ExtractContentActivity activity = new(client, CreateGate(), payloadStore);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        Contracts.V1.ExtractionResult result = await activity.RunAsync(context, input);

        result.ExtractedContent.ShouldBeEmpty();
        result.ExtractedContentReference.ShouldBe(extractedReference);
        await client.Received(1).ExtractAsync(
            Arg.Is<ExtractionInput>(effective => effective.ContentBytes.SequenceEqual(sourceBytes)),
            Arg.Any<CancellationToken>());
        await payloadStore.Received(1).SaveAsync(
            "test-tenant",
            "mu-1",
            WorkflowPayloadKind.ExtractedText,
            Arg.Is<ReadOnlyMemory<byte>>(payload => Encoding.UTF8.GetString(payload.ToArray()) == "extracted text"),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithPayloadReferenceScopedToDedupInstance_SavesExtractedTextUnderWorkflowMemoryUnit()
    {
        IContentExtractionClient client = Substitute.For<IContentExtractionClient>();
        byte[] sourceBytes = Encoding.UTF8.GetBytes("{\"eventId\":\"evt-1\"}");
        const string dedupInstanceId = "dedup:test-tenant:case-1:abc123";
        const string memoryUnitId = "mu-event-1";
        WorkflowPayloadReference sourceReference = new(
            $"{dedupInstanceId}:sourcebytes:source",
            "source",
            sourceBytes.Length,
            WorkflowPayloadKind.SourceBytes,
            "test-tenant",
            dedupInstanceId);
        WorkflowPayloadReference extractedReference = new(
            $"{memoryUnitId}:extractedtext:extracted",
            "extracted",
            "event text".Length,
            WorkflowPayloadKind.ExtractedText,
            "test-tenant",
            memoryUnitId);
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        payloadStore
            .ReadAsync(sourceReference, "test-tenant", dedupInstanceId, WorkflowPayloadKind.SourceBytes, Arg.Any<CancellationToken>())
            .Returns(sourceBytes);
        payloadStore
            .SaveAsync(
                "test-tenant",
                memoryUnitId,
                WorkflowPayloadKind.ExtractedText,
                Arg.Any<ReadOnlyMemory<byte>>(),
                null,
                Arg.Any<CancellationToken>())
            .Returns(extractedReference);
        ExtractionInput input = CreateTestInput() with
        {
            ContentBytes = [],
            MemoryUnitId = memoryUnitId,
            PayloadReference = sourceReference,
        };
        client.ExtractAsync(Arg.Any<ExtractionInput>(), Arg.Any<CancellationToken>())
            .Returns(new Contracts.V1.ExtractionResult("event text", "abc123", DateTimeOffset.UtcNow));

        ExtractContentActivity activity = new(client, CreateGate(), payloadStore);

        Contracts.V1.ExtractionResult result = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input);

        result.ExtractedContentReference.ShouldBe(extractedReference);
        await payloadStore.Received(1).ReadAsync(
            sourceReference,
            "test-tenant",
            dedupInstanceId,
            WorkflowPayloadKind.SourceBytes,
            Arg.Any<CancellationToken>());
        await payloadStore.Received(1).SaveAsync(
            "test-tenant",
            memoryUnitId,
            WorkflowPayloadKind.ExtractedText,
            Arg.Any<ReadOnlyMemory<byte>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenClientThrowsException_ShouldPropagate()
    {
        // Arrange
        IContentExtractionClient client = Substitute.For<IContentExtractionClient>();
        ExtractionInput input = CreateTestInput();

        client.ExtractAsync(input, Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Kreuzberg native crash"));

        ExtractContentActivity activity = new(client, CreateGate());
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        // Act & Assert
        await Should.ThrowAsync<IOException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_WhenClientThrowsInvalidOperationException_ShouldPropagate()
    {
        // Arrange
        IContentExtractionClient client = Substitute.For<IContentExtractionClient>();
        ExtractionInput input = CreateTestInput();

        client.ExtractAsync(input, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Empty content"));

        ExtractContentActivity activity = new(client, CreateGate());
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_MissingTenantId_ThrowsArgumentException()
    {
        IContentExtractionClient client = Substitute.For<IContentExtractionClient>();
        ExtractionInput input = new(
            "file:///test.txt",
            Encoding.UTF8.GetBytes("test content"),
            "text/plain",
            SourceType.File,
            string.Empty);

        ExtractContentActivity activity = new(client, CreateGate());
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await Should.ThrowAsync<ArgumentException>(() => activity.RunAsync(context, input));
    }

    private static ExtractionInput CreateTestInput()
    {
        return new ExtractionInput(
            "file:///test.txt",
            Encoding.UTF8.GetBytes("test content"),
            "text/plain",
            SourceType.File,
            "test-tenant");
    }

    private static PerTenantConcurrencyGate CreateGate()
    {
        IngestionSettings settings = new()
        {
            PerTenantExtractionConcurrency = 4,
            ExtractionGateAcquireTimeoutSeconds = 10,
        };
        return new PerTenantConcurrencyGate(
            Options.Create(settings),
            NullLogger<PerTenantConcurrencyGate>.Instance);
    }
}
