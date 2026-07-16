// <copyright file="ValidateContentActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.TestHelpers.Factories;

using NSubstitute;

using Shouldly;

public class ValidateContentActivityTests
{
    [Fact]
    public async Task RunAsync_ValidInput_ShouldReturnIsValidTrue()
    {
        ValidateContentActivity activity = new();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        IngestionInput input = IngestionInputFactory.Create();

        ValidateResult result = await activity.RunAsync(context, input);

        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunAsync_InvalidTenantId_ShouldThrowArgumentException(string? tenantId)
    {
        ValidateContentActivity activity = new();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        IngestionInput input = IngestionInputFactory.Create(tenantId: tenantId ?? "placeholder")
            with
        { TenantId = tenantId! };

        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunAsync_InvalidCaseId_ShouldThrowArgumentException(string? caseId)
    {
        ValidateContentActivity activity = new();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        IngestionInput input = IngestionInputFactory.Create(caseId: caseId ?? "placeholder")
            with
        { CaseId = caseId! };

        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunAsync_InvalidSourceUri_ShouldThrowArgumentException(string? sourceUri)
    {
        ValidateContentActivity activity = new();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        IngestionInput input = IngestionInputFactory.Create(sourceUri: sourceUri ?? "placeholder")
            with
        { SourceUri = sourceUri! };

        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunAsync_InvalidContentType_ShouldThrowArgumentException(string? contentType)
    {
        ValidateContentActivity activity = new();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        IngestionInput input = IngestionInputFactory.Create(contentType: contentType ?? "placeholder")
            with
        { ContentType = contentType! };

        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_NullContentBytes_ShouldThrowArgumentNullException()
    {
        ValidateContentActivity activity = new();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        IngestionInput input = IngestionInputFactory.Create() with { ContentBytes = null! };

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));

        ex.ShouldBeOfType<ArgumentException>();
    }

    [Fact]
    public async Task RunAsync_EmptyContentBytes_ShouldThrowArgumentException()
    {
        ValidateContentActivity activity = new();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        IngestionInput input = IngestionInputFactory.Create(contentBytes: []);

        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunAsync_InvalidIngestedBy_ShouldThrowArgumentException(string? ingestedBy)
    {
        ValidateContentActivity activity = new();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        IngestionInput input = IngestionInputFactory.Create(ingestedBy: ingestedBy ?? "placeholder")
            with
        { IngestedBy = ingestedBy! };

        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task RunAsync_InvalidSourceType_ShouldThrowArgumentOutOfRangeException(int sourceType)
    {
        ValidateContentActivity activity = new();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        IngestionInput input = IngestionInputFactory.Create(sourceType: SourceType.File)
            with
        { SourceType = (SourceType)sourceType };

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));

        ex.ShouldBeOfType<ArgumentException>();
    }

    [Fact]
    public async Task RunAsync_TenantIdWithInvalidCharacters_ShouldThrowArgumentException()
    {
        ValidateContentActivity activity = new();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        IngestionInput input = IngestionInputFactory.Create(tenantId: "tenant/invalid");

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));

        ex.Message.ShouldContain("TenantId contains invalid characters");
    }

    [Fact]
    public async Task RunAsync_ContentBytesLargerThanOneMegabyte_ShouldThrowArgumentException()
    {
        ValidateContentActivity activity = new();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        IngestionInput input = IngestionInputFactory.Create(contentBytes: new byte[(1024 * 1024) + 1]);

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));

        ex.Message.ShouldContain("1 MB");
    }

    [Theory]
    [MemberData(nameof(GetInvalidConfidences))]
    public async Task RunAsync_MetadataConfidenceOutsideRange_ShouldThrowArgumentException(float confidence)
    {
        ValidateContentActivity activity = new();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        IngestionInput input = IngestionInputFactory.Create();
        input.Metadata["priority"] = new MetadataField("urgent", MetadataOrigin.Human, confidence);

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));

        ex.Message.ShouldContain("confidence must be between 0.0 and 1.0");
    }

    public static IEnumerable<object[]> GetInvalidConfidences()
    {
        yield return [-0.1f];
        yield return [1.1f];
        yield return [float.NaN];
        yield return [float.PositiveInfinity];
    }
}
