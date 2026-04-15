// <copyright file="DirectoryIngestionPathValidationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Server.Ingestion;

using Shouldly;

/// <summary>
/// Story 6.1 Task 7.6 — path validation unit tests (no DAPR dependency). Covers the security-critical
/// path-traversal / allow-list enforcement before enumeration ever runs.
/// </summary>
public class DirectoryIngestionPathValidationTests : IDisposable
{
    private readonly string _root;
    private readonly string _outside;

    public DirectoryIngestionPathValidationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "memories-dir-tests-" + Guid.NewGuid().ToString("N"));
        _outside = Path.Combine(Path.GetTempPath(), "memories-outside-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_outside);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); }
        catch { /* ignore */ }
        try { Directory.Delete(_outside, recursive: true); }
        catch { /* ignore */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Validate_EmptyAllowList_ReturnsDisabled()
    {
        string? code = DirectoryIngestionService.ValidateDirectoryPath(_root, [], out _);

        code.ShouldBe("DIRECTORY_INGESTION_DISABLED");
    }

    [Fact]
    public void Validate_NullPath_ReturnsInvalid()
    {
        string? code = DirectoryIngestionService.ValidateDirectoryPath(null, [_root], out _);

        code.ShouldBe("INVALID_DIRECTORY_PATH");
    }

    [Fact]
    public void Validate_WhitespacePath_ReturnsInvalid()
    {
        string? code = DirectoryIngestionService.ValidateDirectoryPath("   ", [_root], out _);

        code.ShouldBe("INVALID_DIRECTORY_PATH");
    }

    [Fact]
    public void Validate_RelativePath_ReturnsInvalid()
    {
        string? code = DirectoryIngestionService.ValidateDirectoryPath("./foo", [_root], out _);

        code.ShouldBe("INVALID_DIRECTORY_PATH");
    }

    [Fact]
    public void Validate_NonExistent_ReturnsInvalid()
    {
        string missing = Path.Combine(_root, "does-not-exist");

        string? code = DirectoryIngestionService.ValidateDirectoryPath(missing, [_root], out _);

        code.ShouldBe("INVALID_DIRECTORY_PATH");
    }

    [Fact]
    public void Validate_OutsideAllowList_ReturnsInvalid()
    {
        string? code = DirectoryIngestionService.ValidateDirectoryPath(_outside, [_root], out _);

        code.ShouldBe("INVALID_DIRECTORY_PATH");
    }

    [Fact]
    public void Validate_MatchesRootExactly_ReturnsNull()
    {
        string? code = DirectoryIngestionService.ValidateDirectoryPath(_root, [_root], out string canonical);

        code.ShouldBeNull();
        canonical.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Validate_SubdirectoryOfRoot_ReturnsNull()
    {
        string sub = Path.Combine(_root, "sub");
        Directory.CreateDirectory(sub);

        string? code = DirectoryIngestionService.ValidateDirectoryPath(sub, [_root], out _);

        code.ShouldBeNull();
    }

    [Fact]
    public void Validate_SimilarRootPrefix_DoesNotMatch()
    {
        // /data/memories must NOT match /data/memories-secret as a prefix.
        string similar = _root + "-secret";
        Directory.CreateDirectory(similar);
        try
        {
            string? code = DirectoryIngestionService.ValidateDirectoryPath(similar, [_root], out _);

            code.ShouldBe("INVALID_DIRECTORY_PATH");
        }
        finally
        {
            Directory.Delete(similar, recursive: true);
        }
    }

    [Fact]
    public void Validate_TraversalAttempt_ReturnsInvalid()
    {
        // Traversal above the allow-list root.
        string traversal = Path.Combine(_root, "..", Path.GetFileName(_outside));

        string? code = DirectoryIngestionService.ValidateDirectoryPath(traversal, [_root], out _);

        code.ShouldBe("INVALID_DIRECTORY_PATH");
    }

    [Theory]
    [InlineData(".md", "text/markdown")]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData(".unknown", "application/octet-stream")]
    [InlineData(".JSON", "application/json")]
    public void InferContentType_MapsExtensions(string ext, string expected)
    {
        DirectoryIngestionService.InferContentType("/tmp/file" + ext).ShouldBe(expected);
    }
}
