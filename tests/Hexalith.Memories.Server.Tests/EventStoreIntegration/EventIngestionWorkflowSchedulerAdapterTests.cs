// <copyright file="EventIngestionWorkflowSchedulerAdapterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

public sealed class EventIngestionWorkflowSchedulerAdapterTests
{
    [Fact]
    public async Task AddMemoriesServerServices_ResolvesServerAdapterAndDelegatesExactArguments()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddServiceDefaults(configureRedisInstrumentation: false);
        IIngestionWorkflowScheduler inner = Substitute.For<IIngestionWorkflowScheduler>();
        builder.AddMemoriesServerServices();
        builder.Services.RemoveAll<IIngestionWorkflowScheduler>();
        builder.Services.AddSingleton(inner);
        using CancellationTokenSource cancellationSource = new();
        CancellationToken cancellationToken = cancellationSource.Token;
        IngestionInput input = new()
        {
            TenantId = "tenant-event",
            CaseId = "case-event",
            SourceUri = "event://source/42",
            ContentBytes = [1, 2, 3],
            ContentType = "application/json",
            SourceType = SourceType.Event,
            IngestedBy = "eventstore",
        };
        inner.ScheduleAsync("event-instance", input, cancellationToken).Returns("scheduled-event-instance");
        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        IEventIngestionWorkflowScheduler resolved =
            provider.GetRequiredService<IEventIngestionWorkflowScheduler>();
        string result = await resolved.ScheduleAsync("event-instance", input, cancellationToken);

        resolved.ShouldBeOfType<EventIngestionWorkflowSchedulerAdapter>();
        result.ShouldBe("scheduled-event-instance");
        await inner.Received(1).ScheduleAsync(
            "event-instance",
            Arg.Is<IngestionInput>(candidate => ReferenceEquals(candidate, input)),
            cancellationToken);
    }
}
