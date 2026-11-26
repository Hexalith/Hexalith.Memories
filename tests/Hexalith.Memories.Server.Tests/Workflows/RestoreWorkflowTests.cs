// <copyright file="RestoreWorkflowTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Restore;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

public sealed class RestoreWorkflowTests
{
    [Fact]
    public async Task RunAsync_LargeRestorePagesAllUnitsBeforeCleaningUp()
    {
        const int UnitCount = 100_003;
        const int PageSize = 100;
        const string Tenant = "restore-boundary";
        const string StagingKey = "large-upload";
        RestoreWorkflowInput input = new(Tenant, null, StagingKey, "restore-operator");
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.CreateReplaySafeLogger<RestoreWorkflow>().Returns(NullLogger.Instance);
        context.CallActivityAsync<RestoreDataPlaneResult>(
                nameof(RestoreDataPlaneActivity), new RestoreDataPlaneInput(Tenant, null, StagingKey, input.RequestedBy),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new RestoreDataPlaneResult(UnitCount, 2, 3, 4));

        List<RestoreReindexBatchInput> pages = [];
        TaskCompletionSource<RestoreReindexBatchResult> lastPage = new(TaskCreationOptions.RunContinuationsAsynchronously);
        context.CallActivityAsync<RestoreReindexBatchResult>(
                nameof(RestoreReindexBatchActivity), Arg.Any<RestoreReindexBatchInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(call =>
            {
                RestoreReindexBatchInput page = call.Arg<RestoreReindexBatchInput>();
                pages.Add(page);
                return page.Offset == 100_000
                    ? lastPage.Task
                    : Task.FromResult(new RestoreReindexBatchResult(page.BatchSize, page.BatchSize));
            });
        context.CallActivityAsync<bool>(nameof(DeleteRestoreStagingActivity), StagingKey, Arg.Any<WorkflowTaskOptions>())
            .Returns(_ =>
            {
                pages.Count.ShouldBe(1_001);
                return true;
            });

        Task<RestoreWorkflowResult> restore = new RestoreWorkflow().RunAsync(context, input);
        try
        {
            pages.Count.ShouldBe(1_001);
            restore.IsCompleted.ShouldBeFalse();
            await context.DidNotReceive().CallActivityAsync<bool>(
                nameof(DeleteRestoreStagingActivity), StagingKey, Arg.Any<WorkflowTaskOptions>());
        }
        finally
        {
            lastPage.TrySetResult(new RestoreReindexBatchResult(3, 3));
        }

        RestoreWorkflowResult result = await restore;

        pages.ShouldBe(Enumerable.Range(0, 1_001).Select(page =>
            new RestoreReindexBatchInput(Tenant, StagingKey, page * PageSize, page == 1_000 ? 3 : PageSize)));
        result.ShouldBe(new RestoreWorkflowResult(UnitCount, 2, 3, 4));
        await context.Received(1).CallActivityAsync<bool>(
            nameof(DeleteRestoreStagingActivity), StagingKey, Arg.Any<WorkflowTaskOptions>());
    }
}
