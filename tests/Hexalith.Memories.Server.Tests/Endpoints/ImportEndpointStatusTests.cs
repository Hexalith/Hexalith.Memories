// <copyright file="ImportEndpointStatusTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using Dapr.Workflow;

using Hexalith.Memories.Server.Endpoints;

using Shouldly;

/// <summary>Tests restore workflow status projection.</summary>
[Trait("Category", "Unit")]
public sealed class ImportEndpointStatusTests
{
    [Theory]
    [InlineData(WorkflowRuntimeStatus.Completed, "reindexing", "Completed")]
    [InlineData(WorkflowRuntimeStatus.Failed, "restoring-data-plane", "Failed")]
    [InlineData(WorkflowRuntimeStatus.Canceled, "cleaning-up", "Canceled")]
    [InlineData(WorkflowRuntimeStatus.Terminated, "reindexing", "Terminated")]
    [InlineData(WorkflowRuntimeStatus.Running, "reindexing", "reindexing")]
    [InlineData(WorkflowRuntimeStatus.Pending, null, "Pending")]
    public void ResolveReportedStatus_TerminalRuntimeStateTakesPrecedenceOverCustomProgress(
        WorkflowRuntimeStatus runtimeStatus,
        string? customStatus,
        string expected)
        => ImportEndpoints.ResolveReportedStatus(runtimeStatus, customStatus).ShouldBe(expected);

    [Fact]
    public void ResolveFailureDiagnostics_KnownEmbeddingFailure_ReturnsSanitizedGuidance()
    {
        var details = new WorkflowTaskFailureDetails(
            "System.InvalidOperationException",
            "IMPORT_EMBEDDING_MODEL_MISMATCH api-key=do-not-expose",
            "secret stack trace");

        (string? code, string? message, string? suggestion) = ImportEndpoints.ResolveFailureDiagnostics(
            WorkflowRuntimeStatus.Failed,
            details);

        code.ShouldBe("RESTORE_EMBEDDING_CONFIGURATION_MISMATCH");
        message.ShouldNotBeNull().ShouldNotContain("api-key", Shouldly.Case.Insensitive);
        suggestion.ShouldNotBeNull().ShouldNotContain("do-not-expose", Shouldly.Case.Insensitive);
    }

    [Theory]
    [InlineData(WorkflowRuntimeStatus.Canceled, "RESTORE_WORKFLOW_CANCELED")]
    [InlineData(WorkflowRuntimeStatus.Terminated, "RESTORE_WORKFLOW_TERMINATED")]
    public void ResolveFailureDiagnostics_TerminalInterruption_ReturnsStableCode(
        WorkflowRuntimeStatus runtimeStatus,
        string expectedCode)
    {
        (string? code, string? message, string? suggestion) = ImportEndpoints.ResolveFailureDiagnostics(
            runtimeStatus,
            failureDetails: null);

        code.ShouldBe(expectedCode);
        message.ShouldNotBeNullOrWhiteSpace();
        suggestion.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ResolveFailureDiagnostics_Running_ReturnsNoFailureFields()
    {
        (string? code, string? message, string? suggestion) = ImportEndpoints.ResolveFailureDiagnostics(
            WorkflowRuntimeStatus.Running,
            failureDetails: null);

        code.ShouldBeNull();
        message.ShouldBeNull();
        suggestion.ShouldBeNull();
    }
}
