// <copyright file="InboundRequestRateLimiterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.RateLimiting;

using System.Threading.RateLimiting;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.RateLimiting;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>Tests ownership boundaries for the shared inbound request limiter.</summary>
public sealed class InboundRequestRateLimiterTests
{
    /// <summary>Verifies framework partition disposal does not dispose the shared endpoint-filter limiter.</summary>
    [Fact]
    public async Task CreatePartition_DisposingFrameworkPartition_KeepsSharedLimiterUsable()
    {
        await using var limiter = new InboundRequestRateLimiter(new InboundRateLimitOptions
        {
            PermitLimit = 2,
            QueueLimit = 0,
            WindowSeconds = 60,
        });
        RateLimitPartition<string> partition = limiter.CreatePartition("tenant:test");
        RateLimiter frameworkPartition = partition.Factory(partition.PartitionKey);

        frameworkPartition.Dispose();

        using RateLimitLease lease = await limiter.AcquireAsync("tenant:test", CancellationToken.None);
        lease.IsAcquired.ShouldBeTrue();
    }

    /// <summary>Verifies the real host disposes the owner shared by middleware and endpoint filters.</summary>
    [Fact]
    public async Task HostDisposal_DeniesRequestsThroughTheRegisteredGlobalLimiter()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration["Memories:Testing:UseInMemoryCommandStore"] = "true";
        builder.Configuration[$"{InboundRateLimitOptions.SectionName}:PermitLimit"] = "1";
        builder.Configuration[$"{InboundRateLimitOptions.SectionName}:WindowSeconds"] = "60";
        builder.AddMemoriesServerServices();
        WebApplication app = builder.Build();
        using PartitionedRateLimiter<HttpContext> globalLimiter =
            app.Services.GetRequiredService<IOptions<RateLimiterOptions>>().Value.GlobalLimiter.ShouldNotBeNull();
        var request = new DefaultHttpContext();
        request.Request.Path = MemoriesRoutes.Search;
        request.Items[AuthorizedTenantAccessor.HttpContextItemKey] = "test";
        InboundRequestRateLimiter owner = app.Services.GetRequiredService<InboundRequestRateLimiter>();

        try
        {
            using RateLimitLease first = await owner.AcquireAsync("tenant:test", TestContext.Current.CancellationToken);
            first.IsAcquired.ShouldBeTrue();
            using RateLimitLease deniedThroughMiddleware = globalLimiter.AttemptAcquire(request);
            deniedThroughMiddleware.IsAcquired.ShouldBeFalse();
        }
        finally
        {
            await app.DisposeAsync();
        }

        using RateLimitLease shutdown = globalLimiter.AttemptAcquire(request);
        shutdown.IsAcquired.ShouldBeFalse();
        var lateRequest = new DefaultHttpContext();
        lateRequest.Request.Path = MemoriesRoutes.Search;
        lateRequest.Items[AuthorizedTenantAccessor.HttpContextItemKey] = "late";
        using RateLimitLease late = globalLimiter.AttemptAcquire(lateRequest);
        late.IsAcquired.ShouldBeFalse();
    }

    /// <summary>Verifies queued and new requests fail safely as their common owner stops.</summary>
    [Fact]
    public async Task DisposeAsync_WithLivePartitions_DeniesQueuedAndNewRequests()
    {
        await using var limiter = new InboundRequestRateLimiter(new InboundRateLimitOptions
        {
            PermitLimit = 1,
            QueueLimit = 1,
            WindowSeconds = 60,
        });
        RateLimitPartition<string> partition = limiter.CreatePartition("tenant:test");
        using RateLimiter frameworkPartition = partition.Factory(partition.PartitionKey);
        using RateLimitLease acquired = frameworkPartition.AttemptAcquire();
        acquired.IsAcquired.ShouldBeTrue();

        Task<RateLimitLease> waiting = limiter.AcquireAsync("tenant:test", TestContext.Current.CancellationToken).AsTask();
        waiting.IsCompleted.ShouldBeFalse();

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var acquiring = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task racingRequests = Task.Run(async () =>
        {
            await start.Task;
            for (int i = 0; i < 200; i++)
            {
                using RateLimitLease immediate = frameworkPartition.AttemptAcquire();
                immediate.IsAcquired.ShouldBeFalse();
                acquiring.TrySetResult();
                using RateLimitLease asynchronous = await frameworkPartition.AcquireAsync();
                asynchronous.IsAcquired.ShouldBeFalse();
                _ = frameworkPartition.GetStatistics();
                _ = frameworkPartition.IdleDuration;
            }
        });
        Task disposal = Task.Run(async () =>
        {
            await acquiring.Task;
            await limiter.DisposeAsync();
        });
        start.SetResult();
        await Task.WhenAll(disposal, racingRequests);

        using RateLimitLease queued = await waiting.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        queued.IsAcquired.ShouldBeFalse();
        using RateLimitLease currentPartition = await frameworkPartition.AcquireAsync();
        currentPartition.IsAcquired.ShouldBeFalse();
        RateLimitPartition<string> late = limiter.CreatePartition("tenant:late");
        using RateLimiter lateFrameworkPartition = late.Factory(late.PartitionKey);
        using RateLimitLease latePartition = lateFrameworkPartition.AttemptAcquire();
        latePartition.IsAcquired.ShouldBeFalse();
        using RateLimitLease lateBodyRequest = await limiter.AcquireAsync("tenant:late", TestContext.Current.CancellationToken);
        lateBodyRequest.IsAcquired.ShouldBeFalse();
        frameworkPartition.GetStatistics().ShouldBeNull();
        frameworkPartition.IdleDuration.ShouldBeNull();
    }
}
