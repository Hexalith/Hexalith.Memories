namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using System.Text;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;

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

        ExtractContentActivity activity = new(client);
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

        ExtractContentActivity activity = new(client);
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

        ExtractContentActivity activity = new(client);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => activity.RunAsync(context, input));
    }

    private static ExtractionInput CreateTestInput()
    {
        return new ExtractionInput(
            "file:///test.txt",
            Encoding.UTF8.GetBytes("test content"),
            "text/plain",
            SourceType.File);
    }
}
