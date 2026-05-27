namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Security.Cryptography;
using System.Text;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Shouldly;

public class ContentExtractionClientTests
{
    private readonly ContentExtractionClient _client = new();

    [Fact]
    public async Task ExtractAsync_NullInput_ShouldThrowArgumentNullException()
    {
        _ = await Should.ThrowAsync<ArgumentNullException>(() => _client.ExtractAsync(null!));
    }

    [Fact]
    public async Task ExtractAsync_NullContentBytes_ShouldThrowArgumentNullException()
    {
        ExtractionInput input = new(
            "file:///null-bytes.txt",
            null!,
            "text/plain",
            SourceType.File);

        _ = await Should.ThrowAsync<ArgumentNullException>(() => _client.ExtractAsync(input));
    }

    [Fact]
    public async Task ExtractAsync_BlankSourceUri_ShouldThrowArgumentException()
    {
        ExtractionInput input = new(
            " ",
            Encoding.UTF8.GetBytes("test content"),
            "text/plain",
            SourceType.File);

        _ = await Should.ThrowAsync<ArgumentException>(() => _client.ExtractAsync(input));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractAsync_PlainText_ShouldReturnContentUnchanged()
    {
        // Arrange
        string originalText = "Hello, this is plain text content.";
        ExtractionInput input = new(
            "file:///test.txt",
            Encoding.UTF8.GetBytes(originalText),
            "text/plain",
            SourceType.File);

        // Act
        Contracts.V1.ExtractionResult result = await _client.ExtractAsync(input);

        // Assert — Kreuzberg may normalize whitespace
        result.ExtractedContent.Trim().ShouldBe(originalText.Trim());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractAsync_Pdf_ShouldReturnExtractedText()
    {
        // Arrange
        string fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.pdf");
        byte[] pdfBytes = File.ReadAllBytes(fixturePath);
        ExtractionInput input = new(
            "file:///sample.pdf",
            pdfBytes,
            "application/pdf",
            SourceType.File);

        // Act
        Contracts.V1.ExtractionResult result = await _client.ExtractAsync(input);

        // Assert
        result.ExtractedContent.ShouldNotBeNullOrWhiteSpace();
        result.ExtractedContent.ShouldContain("Hello");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractAsync_Markdown_ShouldPreserveStructure()
    {
        // Arrange
        string markdown = "# Heading\n\n- Item 1\n- Item 2\n\n```csharp\nvar x = 1;\n```\n";
        ExtractionInput input = new(
            "file:///test.md",
            Encoding.UTF8.GetBytes(markdown),
            "text/markdown",
            SourceType.File);

        // Act
        Contracts.V1.ExtractionResult result = await _client.ExtractAsync(input);

        // Assert — raw markdown preserved, not rendered to HTML
        result.ExtractedContent.ShouldContain("# Heading");
        result.ExtractedContent.ShouldContain("- Item 1");
        result.ExtractedContent.ShouldContain("```");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractAsync_ShouldComputeCorrectSha256Hash()
    {
        // Arrange
        string textContent = "Known content for hash verification";
        ExtractionInput input = new(
            "file:///hash-test.txt",
            Encoding.UTF8.GetBytes(textContent),
            "text/plain",
            SourceType.File);

        // Act
        Contracts.V1.ExtractionResult result = await _client.ExtractAsync(input);

        // Assert — verify hash matches independently computed value
        string expectedHash = ComputeExpectedSha256(result.ExtractedContent);
        result.ContentHash.ShouldBe(expectedHash);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractAsync_NullContentType_ShouldDefaultToOctetStream()
    {
        // Arrange
        string text = "Content with null type";
        ExtractionInput input = new(
            "file:///test.bin",
            Encoding.UTF8.GetBytes(text),
            null!,
            SourceType.File);

        // Act — should not throw, defaults to application/octet-stream
        Contracts.V1.ExtractionResult result = await _client.ExtractAsync(input);

        // Assert
        result.ExtractedContent.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractAsync_EmptyContentType_ShouldDefaultToOctetStream()
    {
        // Arrange
        string text = "Content with empty type";
        ExtractionInput input = new(
            "file:///test.bin",
            Encoding.UTF8.GetBytes(text),
            "",
            SourceType.File);

        // Act — should not throw, defaults to application/octet-stream
        Contracts.V1.ExtractionResult result = await _client.ExtractAsync(input);

        // Assert
        result.ExtractedContent.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractAsync_EmptyBytes_ShouldThrowKreuzbergValidationException()
    {
        // Arrange — Kreuzberg validates input before extraction and rejects empty data
        ExtractionInput input = new(
            "file:///empty.bin",
            [],
            "application/octet-stream",
            SourceType.File);

        // Act & Assert — Kreuzberg throws its own validation exception, which propagates
        // for DAPR Workflow retry (AC #4)
        _ = await Should.ThrowAsync<Kreuzberg.KreuzbergValidationException>(
            () => _client.ExtractAsync(input));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractAsync_ShouldSetExtractedAtTimestamp()
    {
        // Arrange
        DateTimeOffset before = DateTimeOffset.UtcNow;
        ExtractionInput input = new(
            "file:///test.txt",
            Encoding.UTF8.GetBytes("timestamp test"),
            "text/plain",
            SourceType.File);

        // Act
        Contracts.V1.ExtractionResult result = await _client.ExtractAsync(input);

        // Assert
        DateTimeOffset after = DateTimeOffset.UtcNow;
        result.ExtractedAt.ShouldBeGreaterThanOrEqualTo(before);
        result.ExtractedAt.ShouldBeLessThanOrEqualTo(after);
    }

    private static string ComputeExpectedSha256(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
