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
