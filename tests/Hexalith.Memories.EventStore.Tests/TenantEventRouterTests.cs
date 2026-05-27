// <copyright file="TenantEventRouterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System.Collections.Concurrent;
using System.Text.Json;

using Hexalith.Memories.EventStore;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public sealed class TenantEventRouterTests
{
    private static readonly JsonElement EmptyData = JsonDocument.Parse("{}").RootElement;

    private static TenantEventRouter BuildRouter(
        TenantEventRoutingOptions options,
        ITenantStatusAccessor statusAccessor,
        ICaseCreationService caseCreationService,
        IAggregateCaseMappingStore? caseMapStore = null)
    {
        IOptionsMonitor<TenantEventRoutingOptions> optionsMonitor = Substitute.For<IOptionsMonitor<TenantEventRoutingOptions>>();
        optionsMonitor.CurrentValue.Returns(options);
        return new TenantEventRouter(
            optionsMonitor,
            statusAccessor,
            caseCreationService,
            caseMapStore ?? new InMemoryAggregateCaseMappingStore());
    }

    private static CloudEventEnvelope Envelope(string source, string type)
        => new("evt-1", source, type, Subject: "agg-1", Time: null, DataContentType: null, Data: EmptyData);

    [Fact]
    public async Task ResolveAsync_UnknownSource_ReturnsUnknownSource()
    {
        TenantEventRoutingOptions options = new() { Topic = "t" };

        ITenantStatusAccessor statusAccessor = Substitute.For<ITenantStatusAccessor>();
        ICaseCreationService cases = Substitute.For<ICaseCreationService>();

        TenantEventRouter router = BuildRouter(options, statusAccessor, cases);
        TenantEventRouteResolution resolution = await router
            .ResolveAsync(Envelope("/nowhere", "a.b.c"), CancellationToken.None);

        resolution.Status.ShouldBe(TenantEventRouteResolutionStatus.UnknownSource);
        resolution.TenantId.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_SourcePrefixMatch_IsCaseInsensitive()
    {
        TenantEventRoutingOptions options = new() { Topic = "t" };
        options.SourceToTenantMap["/enterprise/hr"] = "hr-tenant";

        ITenantStatusAccessor statusAccessor = Substitute.For<ITenantStatusAccessor>();
        statusAccessor.GetStatusAsync("hr-tenant", Arg.Any<CancellationToken>())
            .Returns(EventStoreTenantStatus.Active);

        ICaseCreationService cases = Substitute.For<ICaseCreationService>();
        cases.CreateCaseAsync("hr-tenant", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("case-1");

        TenantEventRouter router = BuildRouter(options, statusAccessor, cases);
        TenantEventRouteResolution resolution = await router
            .ResolveAsync(Envelope("/ENTERPRISE/HR/payroll", "a.b.c"), CancellationToken.None);

        resolution.Status.ShouldBe(TenantEventRouteResolutionStatus.Accepted);
        resolution.Route!.TenantId.ShouldBe("hr-tenant");
    }

    [Fact]
    public async Task ResolveAsync_LongestPrefixWins()
    {
        TenantEventRoutingOptions options = new() { Topic = "t" };
        options.SourceToTenantMap["/enterprise"] = "enterprise-tenant";
        options.SourceToTenantMap["/enterprise/hr"] = "hr-tenant";

        ITenantStatusAccessor statusAccessor = Substitute.For<ITenantStatusAccessor>();
        statusAccessor.GetStatusAsync("hr-tenant", Arg.Any<CancellationToken>())
            .Returns(EventStoreTenantStatus.Active);

        ICaseCreationService cases = Substitute.For<ICaseCreationService>();
        cases.CreateCaseAsync("hr-tenant", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("case-1");

        TenantEventRouter router = BuildRouter(options, statusAccessor, cases);
        TenantEventRouteResolution resolution = await router
            .ResolveAsync(Envelope("/enterprise/hr/payroll", "a.b.c"), CancellationToken.None);

        resolution.Route!.TenantId.ShouldBe("hr-tenant");
    }

    [Fact]
    public async Task ResolveAsync_TenantNotFound_ReturnsTenantNotFound()
    {
        TenantEventRoutingOptions options = new() { Topic = "t" };
        options.SourceToTenantMap["/ghost"] = "ghost-tenant";

        ITenantStatusAccessor statusAccessor = Substitute.For<ITenantStatusAccessor>();
        statusAccessor.GetStatusAsync("ghost-tenant", Arg.Any<CancellationToken>())
            .Returns((EventStoreTenantStatus?)null);

        TenantEventRouter router = BuildRouter(options, statusAccessor, Substitute.For<ICaseCreationService>());
        TenantEventRouteResolution resolution = await router
            .ResolveAsync(Envelope("/ghost", "a.b.c"), CancellationToken.None);

        resolution.Status.ShouldBe(TenantEventRouteResolutionStatus.TenantNotFound);
        resolution.TenantId.ShouldBe("ghost-tenant");
    }

    [Fact]
    public async Task ResolveAsync_ProvisioningTenant_ReturnsRetryableOutcome()
    {
        TenantEventRoutingOptions options = new() { Topic = "t" };
        options.SourceToTenantMap["/hr"] = "hr-tenant";

        ITenantStatusAccessor statusAccessor = Substitute.For<ITenantStatusAccessor>();
        statusAccessor.GetStatusAsync("hr-tenant", Arg.Any<CancellationToken>())
            .Returns(EventStoreTenantStatus.Provisioning);

        TenantEventRouter router = BuildRouter(options, statusAccessor, Substitute.For<ICaseCreationService>());
        TenantEventRouteResolution resolution = await router
            .ResolveAsync(Envelope("/hr", "a.b.c"), CancellationToken.None);

        resolution.Status.ShouldBe(TenantEventRouteResolutionStatus.TenantProvisioning);
    }

    [Fact]
    public async Task ResolveAsync_DeletingTenant_ReturnsDropOutcome()
    {
        TenantEventRoutingOptions options = new() { Topic = "t" };
        options.SourceToTenantMap["/hr"] = "hr-tenant";

        ITenantStatusAccessor statusAccessor = Substitute.For<ITenantStatusAccessor>();
        statusAccessor.GetStatusAsync("hr-tenant", Arg.Any<CancellationToken>())
            .Returns(EventStoreTenantStatus.Deleting);

        TenantEventRouter router = BuildRouter(options, statusAccessor, Substitute.For<ICaseCreationService>());
        TenantEventRouteResolution resolution = await router
            .ResolveAsync(Envelope("/hr", "a.b.c"), CancellationToken.None);

        resolution.Status.ShouldBe(TenantEventRouteResolutionStatus.TenantDeleting);
    }

    [Fact]
    public async Task ResolveAsync_AutoCreateDisabled_MissingCase_ReturnsAutoCreateDisabled()
    {
        TenantEventRoutingOptions options = new() { Topic = "t", AutoCreateCases = false };
        options.SourceToTenantMap["/hr"] = "hr-tenant";

        ITenantStatusAccessor statusAccessor = Substitute.For<ITenantStatusAccessor>();
        statusAccessor.GetStatusAsync("hr-tenant", Arg.Any<CancellationToken>())
            .Returns(EventStoreTenantStatus.Active);

        ICaseCreationService cases = Substitute.For<ICaseCreationService>();

        TenantEventRouter router = BuildRouter(options, statusAccessor, cases);
        TenantEventRouteResolution resolution = await router
            .ResolveAsync(Envelope("/hr", "a.b.c"), CancellationToken.None);

        resolution.Status.ShouldBe(TenantEventRouteResolutionStatus.AutoCreateDisabled);
        await cases.DidNotReceive().CreateCaseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveAsync_CaseCapExceeded_ReturnsInvariantFailure()
    {
        TenantEventRoutingOptions options = new()
        {
            Topic = "t",
            AutoCreateCases = true,
            MaxAutoCreatedCasesPerTenant = 1,
        };
        options.SourceToTenantMap["/hr"] = "hr-tenant";

        ITenantStatusAccessor statusAccessor = Substitute.For<ITenantStatusAccessor>();
        statusAccessor.GetStatusAsync("hr-tenant", Arg.Any<CancellationToken>())
            .Returns(EventStoreTenantStatus.Active);

        ICaseCreationService cases = Substitute.For<ICaseCreationService>();
        cases.CreateCaseAsync("hr-tenant", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => "case-" + callInfo.ArgAt<string>(1));

        TenantEventRouter router = BuildRouter(options, statusAccessor, cases);

        TenantEventRouteResolution first = await router
            .ResolveAsync(Envelope("/hr", "a.Claims.X"), CancellationToken.None);
        first.Status.ShouldBe(TenantEventRouteResolutionStatus.Accepted);

        TenantEventRouteResolution second = await router
            .ResolveAsync(Envelope("/hr", "a.Orders.X"), CancellationToken.None);
        second.Status.ShouldBe(TenantEventRouteResolutionStatus.CaseCapExceeded);
    }

    [Fact]
    public async Task ResolveAsync_ConcurrentFirstEvents_ResolveSameCase()
    {
        TenantEventRoutingOptions options = new() { Topic = "t" };
        options.SourceToTenantMap["/hr"] = "hr-tenant";

        ITenantStatusAccessor statusAccessor = Substitute.For<ITenantStatusAccessor>();
        statusAccessor.GetStatusAsync("hr-tenant", Arg.Any<CancellationToken>())
            .Returns(EventStoreTenantStatus.Active);

        int createCount = 0;
        ICaseCreationService cases = Substitute.For<ICaseCreationService>();
        cases.CreateCaseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref createCount);
                return Task.FromResult("case-alpha");
            });

        TenantEventRouter router = BuildRouter(options, statusAccessor, cases);

        Task<TenantEventRouteResolution>[] tasks = Enumerable.Range(0, 16)
            .Select(_ => router.ResolveAsync(Envelope("/hr", "a.Claims.X"), CancellationToken.None))
            .ToArray();

        TenantEventRouteResolution[] results = await Task.WhenAll(tasks);

        results.ShouldAllBe(r => r.Status == TenantEventRouteResolutionStatus.Accepted);
        results.Select(r => r.Route!.CaseId).Distinct().Count().ShouldBe(1);
        createCount.ShouldBe(1);
    }

    [Fact]
    public async Task ResolveAsync_PersistedCaseMap_ReusesExistingCaseAcrossRouterInstances()
    {
        TenantEventRoutingOptions options = new() { Topic = "t" };
        options.SourceToTenantMap["/hr"] = "hr-tenant";

        ITenantStatusAccessor statusAccessor = Substitute.For<ITenantStatusAccessor>();
        statusAccessor.GetStatusAsync("hr-tenant", Arg.Any<CancellationToken>())
            .Returns(EventStoreTenantStatus.Active);

        int createCount = 0;
        ICaseCreationService cases = Substitute.For<ICaseCreationService>();
        cases.CreateCaseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref createCount);
                return Task.FromResult("case-shared");
            });

        InMemoryAggregateCaseMappingStore sharedStore = new();
        TenantEventRouter firstRouter = BuildRouter(options, statusAccessor, cases, sharedStore);
        TenantEventRouter secondRouter = BuildRouter(options, statusAccessor, cases, sharedStore);

        TenantEventRouteResolution first = await firstRouter.ResolveAsync(Envelope("/hr", "a.Claims.X"), CancellationToken.None);
        TenantEventRouteResolution second = await secondRouter.ResolveAsync(Envelope("/hr", "a.Claims.X"), CancellationToken.None);

        first.Status.ShouldBe(TenantEventRouteResolutionStatus.Accepted);
        second.Status.ShouldBe(TenantEventRouteResolutionStatus.Accepted);
        second.Route!.CaseId.ShouldBe(first.Route!.CaseId);
        createCount.ShouldBe(1);
    }

    [Fact]
    public async Task ResolveAsync_CaseCreationFailure_EvictsReservation_SoRetrySucceeds()
    {
        TenantEventRoutingOptions options = new() { Topic = "t" };
        options.SourceToTenantMap["/hr"] = "hr-tenant";

        ITenantStatusAccessor statusAccessor = Substitute.For<ITenantStatusAccessor>();
        statusAccessor.GetStatusAsync("hr-tenant", Arg.Any<CancellationToken>())
            .Returns(EventStoreTenantStatus.Active);

        int attempt = 0;
        ICaseCreationService cases = Substitute.For<ICaseCreationService>();
        cases.CreateCaseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                attempt++;
                return attempt == 1
                    ? Task.FromException<string>(new InvalidOperationException("transient"))
                    : Task.FromResult("case-1");
            });

        TenantEventRouter router = BuildRouter(options, statusAccessor, cases);

        await Should.ThrowAsync<InvalidOperationException>(
            () => router.ResolveAsync(Envelope("/hr", "a.Claims.X"), CancellationToken.None));

        TenantEventRouteResolution retry = await router
            .ResolveAsync(Envelope("/hr", "a.Claims.X"), CancellationToken.None);

        retry.Status.ShouldBe(TenantEventRouteResolutionStatus.Accepted);
        retry.Route!.CaseId.ShouldBe("case-1");
    }

    private sealed class InMemoryAggregateCaseMappingStore : IAggregateCaseMappingStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _maps = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, byte> _locks = new(StringComparer.Ordinal);

        public Task<string?> GetCaseIdAsync(string tenantId, string aggregateType, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult(
                _maps.TryGetValue(tenantId, out ConcurrentDictionary<string, string>? map)
                    && map.TryGetValue(aggregateType, out string? caseId)
                    ? caseId
                    : null);
        }

        public Task<long> GetAggregateCountAsync(string tenantId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            long count = _maps.TryGetValue(tenantId, out ConcurrentDictionary<string, string>? map) ? map.Count : 0;
            return Task.FromResult(count);
        }

        public Task<bool> TryAcquireCreationLockAsync(string tenantId, string aggregateType, TimeSpan leaseTtl, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _ = leaseTtl;
            return Task.FromResult(_locks.TryAdd($"{tenantId}:{aggregateType}", 0));
        }

        public Task ReleaseCreationLockAsync(string tenantId, string aggregateType, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _ = _locks.TryRemove($"{tenantId}:{aggregateType}", out _);
            return Task.CompletedTask;
        }

        public Task<bool> TryStoreCaseIdAsync(string tenantId, string aggregateType, string caseId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            ConcurrentDictionary<string, string> map = _maps.GetOrAdd(
                tenantId,
                static _ => new ConcurrentDictionary<string, string>(StringComparer.Ordinal));
            return Task.FromResult(map.TryAdd(aggregateType, caseId));
        }
    }
}
