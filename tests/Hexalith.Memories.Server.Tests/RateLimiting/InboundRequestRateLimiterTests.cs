// <copyright file="InboundRequestRateLimiterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.RateLimiting;

using System.Threading.RateLimiting;

using Hexalith.Memories.Server.RateLimiting;

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
}
