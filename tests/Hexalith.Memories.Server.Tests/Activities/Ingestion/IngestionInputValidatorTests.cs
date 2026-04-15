// <copyright file="IngestionInputValidatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.TestHelpers.Factories;

using Shouldly;

/// <summary>Story 6.1 Task 1.4: source-type-aware content bytes validation rules.</summary>
public class IngestionInputValidatorTests
{
    [Fact]
    public void Validate_File_WithNullBytes_Throws()
    {
        IngestionInput input = IngestionInputFactory.Create() with { ContentBytes = null };

        ArgumentException ex = Should.Throw<ArgumentException>(() => IngestionInputValidator.Validate(input));
        ex.Message.ShouldContain("ContentBytes is required");
    }

    [Fact]
    public void Validate_File_WithEmptyBytes_Throws()
    {
        IngestionInput input = IngestionInputFactory.Create(contentBytes: []);

        Should.Throw<ArgumentException>(() => IngestionInputValidator.Validate(input))
            .Message.ShouldContain("must not be empty");
    }

    [Fact]
    public void Validate_File_WithOversizedBytes_Throws()
    {
        IngestionInput input = IngestionInputFactory.Create(contentBytes: new byte[1024 * 1024 + 1]);

        Should.Throw<ArgumentException>(() => IngestionInputValidator.Validate(input))
            .Message.ShouldContain("1 MB");
    }

    [Fact]
    public void Validate_File_WithValidBytes_DoesNotThrow()
    {
        IngestionInput input = IngestionInputFactory.Create(contentBytes: [1, 2, 3]);

        Should.NotThrow(() => IngestionInputValidator.Validate(input));
    }

    [Fact]
    public void Validate_Url_WithNullBytes_AndAbsoluteHttpsUri_DoesNotThrow()
    {
        IngestionInput input = IngestionInputFactory.Create() with
        {
            SourceType = SourceType.Url,
            ContentBytes = null,
            SourceUri = "https://example.com/doc.pdf",
        };

        Should.NotThrow(() => IngestionInputValidator.Validate(input));
    }

    [Fact]
    public void Validate_Url_WithEmptyBytes_DoesNotThrow()
    {
        IngestionInput input = IngestionInputFactory.Create() with
        {
            SourceType = SourceType.Url,
            ContentBytes = [],
            SourceUri = "https://example.com/doc.pdf",
        };

        Should.NotThrow(() => IngestionInputValidator.Validate(input));
    }

    [Fact]
    public void Validate_Url_WithNonEmptyBytes_Throws()
    {
        IngestionInput input = IngestionInputFactory.Create() with
        {
            SourceType = SourceType.Url,
            ContentBytes = [1, 2, 3],
            SourceUri = "https://example.com/doc.pdf",
        };

        Should.Throw<ArgumentException>(() => IngestionInputValidator.Validate(input))
            .Message.ShouldContain("must be null for SourceType=Url");
    }

    [Fact]
    public void Validate_Url_WithFileScheme_Throws()
    {
        IngestionInput input = IngestionInputFactory.Create() with
        {
            SourceType = SourceType.Url,
            ContentBytes = null,
            SourceUri = "file:///etc/passwd",
        };

        Should.Throw<ArgumentException>(() => IngestionInputValidator.Validate(input))
            .Message.ShouldContain("absolute http(s) URL");
    }

    [Fact]
    public void Validate_Url_WithMalformedUri_Throws()
    {
        IngestionInput input = IngestionInputFactory.Create() with
        {
            SourceType = SourceType.Url,
            ContentBytes = null,
            SourceUri = "not-a-url",
        };

        Should.Throw<ArgumentException>(() => IngestionInputValidator.Validate(input));
    }

    [Fact]
    public void Validate_Event_WithBytes_ThrowsDefensively()
    {
        IngestionInput input = IngestionInputFactory.Create(
            sourceType: SourceType.Event,
            contentBytes: [1]);

        Should.Throw<ArgumentException>(() => IngestionInputValidator.Validate(input))
            .Message.ShouldContain("must be null or empty for SourceType=Event");
    }
}
