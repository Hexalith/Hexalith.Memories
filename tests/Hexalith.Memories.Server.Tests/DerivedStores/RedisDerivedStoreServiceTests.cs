// <copyright file="RedisDerivedStoreServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.DerivedStores;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Contracts.V1.DerivedStores;
using Hexalith.Memories.Server.DerivedStores;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class RedisDerivedStoreServiceTests
{
    private const string AssociationId = "association-1";
    private const string IntakeId = "intake-1";
    private const string PriorCaseId = "case-prior";
    private const string TenantId = "tenant-a";
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FinalizeBindingAsync_CompleteMessageAndAttachments_PublishesOnlyThroughTransaction()
    {
        DerivedStoreBindingEntry[] entries =
        [
            new(DerivedStoreRecordKind.Message, 0, "mu-message"),
            new(DerivedStoreRecordKind.Attachment, 1, "mu-attachment-1"),
            new(DerivedStoreRecordKind.Attachment, 2, "mu-attachment-2"),
        ];
        FinalizeDerivedStoreBindingRequest request = CreateFinalizeRequest(entries, expectedAttachmentCount: 2);
        (RedisDerivedStoreService service, IDatabase database, ITransaction transaction) = CreateBindingService(entries);

        DerivedStoreBinding binding = await service.FinalizeBindingAsync(TenantId, request, CancellationToken.None);

        binding.Entries.ShouldBe(entries);
        binding.FinalizedAtUtc.ShouldBe(Now);
        CountStringSetCalls(
            transaction,
            RedisDerivedStoreService.BuildBindingKey(TenantId, AssociationId, IntakeId)).ShouldBe(1);
        await transaction.Received(1).ExecuteAsync(Arg.Any<CommandFlags>());
        await database.DidNotReceiveWithAnyArgs().StringSetAsync(default, default, default(TimeSpan?), default, default);
    }

    public static TheoryData<DerivedStoreBindingEntry[], int, string> InvalidManifests => new()
    {
        {
            [new(DerivedStoreRecordKind.Message, 0, "mu-message")],
            1,
            "BINDING_MANIFEST_COUNT_INVALID"
        },
        {
            [
                new(DerivedStoreRecordKind.Message, 0, "mu-message"),
                new(DerivedStoreRecordKind.Attachment, 2, "mu-attachment"),
            ],
            1,
            "BINDING_MANIFEST_ORDER_INVALID"
        },
        {
            [
                new(DerivedStoreRecordKind.Message, 0, "mu-duplicate"),
                new(DerivedStoreRecordKind.Attachment, 1, "mu-duplicate"),
            ],
            1,
            "BINDING_MANIFEST_ORDER_INVALID"
        },
        {
            [
                new(DerivedStoreRecordKind.Message, 0, "mu-message"),
                new((DerivedStoreRecordKind)99, 1, "mu-attachment"),
            ],
            1,
            "BINDING_MANIFEST_ORDER_INVALID"
        },
    };

    [Theory]
    [MemberData(nameof(InvalidManifests))]
    public async Task FinalizeBindingAsync_InvalidManifest_PublishesNothing(
        DerivedStoreBindingEntry[] entries,
        int expectedAttachmentCount,
        string expectedCode)
    {
        (RedisDerivedStoreService service, IDatabase database, _) = CreateBindingService([]);

        DerivedStoreStateException exception = await Should.ThrowAsync<DerivedStoreStateException>(() => service.FinalizeBindingAsync(
            TenantId,
            CreateFinalizeRequest(entries, expectedAttachmentCount),
            CancellationToken.None));

        exception.Code.ShouldBe(expectedCode);
        database.DidNotReceiveWithAnyArgs().CreateTransaction(default);
    }

    [Theory]
    [InlineData("tenant-b", PriorCaseId, "BINDING_SOURCE_ARTIFACT_MISMATCH")]
    [InlineData(TenantId, "case-other", "BINDING_SOURCE_ARTIFACT_MISMATCH")]
    public async Task FinalizeBindingAsync_CrossTenantOrCrossCaseArtifact_PublishesNothing(
        string artifactTenant,
        string artifactCase,
        string expectedCode)
    {
        DerivedStoreBindingEntry[] entries = [new(DerivedStoreRecordKind.Message, 0, "mu-message")];
        (RedisDerivedStoreService service, _, ITransaction transaction) = CreateBindingService(
            entries,
            artifactFactory: entry => CreateArtifact(entry.MemoryUnitId, artifactTenant, artifactCase));

        DerivedStoreStateException exception = await Should.ThrowAsync<DerivedStoreStateException>(() => service.FinalizeBindingAsync(
            TenantId,
            CreateFinalizeRequest(entries, expectedAttachmentCount: 0),
            CancellationToken.None));

        exception.Code.ShouldBe(expectedCode);
        await transaction.DidNotReceiveWithAnyArgs().ExecuteAsync(default);
    }

    [Fact]
    public async Task FinalizeBindingAsync_MissingArtifact_PublishesNothing()
    {
        DerivedStoreBindingEntry[] entries = [new(DerivedStoreRecordKind.Message, 0, "mu-message")];
        (RedisDerivedStoreService service, _, ITransaction transaction) = CreateBindingService(entries, missingArtifactId: "mu-message");

        DerivedStoreStateException exception = await Should.ThrowAsync<DerivedStoreStateException>(() => service.FinalizeBindingAsync(
            TenantId,
            CreateFinalizeRequest(entries, expectedAttachmentCount: 0),
            CancellationToken.None));

        exception.Code.ShouldBe("BINDING_SOURCE_ARTIFACT_MISSING");
        await transaction.DidNotReceiveWithAnyArgs().ExecuteAsync(default);
    }

    [Theory]
    [InlineData("tenant-b", PriorCaseId)]
    [InlineData(TenantId, "case-other")]
    public async Task FinalizeBindingAsync_CrossTenantOrCrossCaseMemoryUnit_PublishesNothing(
        string memoryUnitTenant,
        string memoryUnitCase)
    {
        DerivedStoreBindingEntry[] entries = [new(DerivedStoreRecordKind.Message, 0, "mu-message")];
        (RedisDerivedStoreService service, _, ITransaction transaction) = CreateBindingService(
            entries,
            identityFactory: entry => [memoryUnitTenant, memoryUnitCase, entry.MemoryUnitId]);

        DerivedStoreStateException exception = await Should.ThrowAsync<DerivedStoreStateException>(() => service.FinalizeBindingAsync(
            TenantId,
            CreateFinalizeRequest(entries, expectedAttachmentCount: 0),
            CancellationToken.None));

        exception.Code.ShouldBe("BINDING_MEMORY_UNIT_MISMATCH");
        await transaction.DidNotReceiveWithAnyArgs().ExecuteAsync(default);
    }

    [Theory]
    [InlineData(DerivedStoreCorrectionState.Failed)]
    [InlineData(DerivedStoreCorrectionState.TimedOut)]
    public async Task StartCorrectionAsync_TerminalFailureOrTimeout_ResetsSameOperationForRetry(
        DerivedStoreCorrectionState terminalState)
    {
        StartDerivedStoreCorrectionRequest request = new(AssociationId, IntakeId, "correction-1", 1, "case-corrected");
        string operationId = RedisDerivedStoreService.BuildOperationId(TenantId, request);
        DerivedStoreBinding binding = CreateBinding([new(DerivedStoreRecordKind.Message, 0, "mu-message")]);
        var failed = new DerivedStoreCorrectionStatus(
            operationId,
            terminalState,
            AssociationId,
            IntakeId,
            request.CorrectionId,
            request.SourceVersion,
            PriorCaseId,
            request.CorrectedCaseId,
            0,
            0,
            false,
            Now.AddMinutes(-1),
            Now.AddMinutes(-2),
            "backend_failed");
        (RedisDerivedStoreService service, IDatabase database, ITransaction transaction) = CreateService();
        database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(call =>
        {
            string key = call.ArgAt<RedisKey>(0).ToString();
            return key == RedisDerivedStoreService.BuildBindingKey(TenantId, AssociationId, IntakeId)
                ? Serialize(binding)
                : key == RedisDerivedStoreService.BuildStatusKey(TenantId, operationId)
                    ? Serialize(failed)
                    : RedisValue.Null;
        });
        database.HashGetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)TenantId);

        DerivedStoreCorrectionStartResult result = await service.StartCorrectionAsync(TenantId, request, CancellationToken.None);

        result.Status.OperationId.ShouldBe(operationId);
        result.Status.State.ShouldBe(DerivedStoreCorrectionState.Pending);
        result.Status.CompletedAtUtc.ShouldBeNull();
        result.Status.FailureReasonCode.ShouldBeNull();
        result.Status.DeadlineUtc.ShouldBe(Now.AddMinutes(60));
        result.ShouldSchedule.ShouldBeTrue();
        result.WorkflowInstanceId.ShouldContain(operationId);
        await transaction.Received(1).ExecuteAsync(Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ApplyCorrectionAsync_CompletedUnitFence_SkipsRepeatedEffectsBeforeIntakeFence()
    {
        DerivedStoreBindingEntry entry = new(DerivedStoreRecordKind.Message, 0, "mu-message");
        DerivedStoreBinding binding = CreateBinding([entry]);
        DerivedStoreCorrectionStatus pending = CreatePendingStatus();
        (RedisDerivedStoreService service, IDatabase database, ITransaction transaction) = CreateService();
        ConfigureCorrectionReads(database, binding, pending, new Dictionary<string, RedisValue>
        {
            [RedisDerivedStoreService.BuildUnitFenceKey(TenantId, entry.MemoryUnitId)] = "1",
        });
        int regenerateCalls = 0;

        DerivedStoreCorrectionStatus result = await service.ApplyCorrectionAsync(
            TenantId,
            pending.OperationId,
            (_, _, _) =>
            {
                regenerateCalls++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        result.State.ShouldBe(DerivedStoreCorrectionState.Succeeded);
        regenerateCalls.ShouldBe(0);
        CountStringSetCalls(
            transaction,
            RedisDerivedStoreService.BuildIntakeFenceKey(TenantId, AssociationId, IntakeId)).ShouldBe(1);
        CountStringSetCalls(
            transaction,
            RedisDerivedStoreService.BuildBindingKey(TenantId, AssociationId, IntakeId)).ShouldBe(1);
    }

    [Fact]
    public async Task ApplyCorrectionAsync_LaterUnitFails_DoesNotPublishIntakeFenceOrRepeatCompletedUnit()
    {
        DerivedStoreBindingEntry first = new(DerivedStoreRecordKind.Message, 0, "mu-message");
        DerivedStoreBindingEntry second = new(DerivedStoreRecordKind.Attachment, 1, "mu-attachment");
        DerivedStoreBinding binding = CreateBinding([first, second]);
        DerivedStoreCorrectionStatus pending = CreatePendingStatus();
        (RedisDerivedStoreService service, IDatabase database, ITransaction transaction) = CreateService();
        ConfigureCorrectionReads(database, binding, pending, new Dictionary<string, RedisValue>
        {
            [RedisDerivedStoreService.BuildUnitFenceKey(TenantId, first.MemoryUnitId)] = "1",
            [RedisDerivedStoreService.BuildUnitFenceKey(TenantId, second.MemoryUnitId)] = RedisValue.Null,
            [RedisDerivedStoreService.BuildSourceArtifactKey(TenantId, second.MemoryUnitId)] = RedisValue.Null,
        });
        int regenerateCalls = 0;

        DerivedStoreCorrectionStatus result = await service.ApplyCorrectionAsync(
            TenantId,
            pending.OperationId,
            (_, _, _) =>
            {
                regenerateCalls++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        result.State.ShouldBe(DerivedStoreCorrectionState.Failed);
        result.FailureReasonCode.ShouldBe("correction_source_artifact_missing");
        regenerateCalls.ShouldBe(0);
        CountStringSetCalls(
            transaction,
            RedisDerivedStoreService.BuildIntakeFenceKey(TenantId, AssociationId, IntakeId)).ShouldBe(0);
    }

    private static void ConfigureCorrectionReads(
        IDatabase database,
        DerivedStoreBinding binding,
        DerivedStoreCorrectionStatus status,
        IReadOnlyDictionary<string, RedisValue> extraValues)
    {
        database.PingAsync(Arg.Any<CommandFlags>()).Returns(TimeSpan.Zero);
        database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(call =>
        {
            string key = call.ArgAt<RedisKey>(0).ToString();
            if (key == RedisDerivedStoreService.BuildStatusKey(TenantId, status.OperationId))
            {
                return Serialize(status);
            }

            if (key == RedisDerivedStoreService.BuildBindingKey(TenantId, AssociationId, IntakeId))
            {
                return Serialize(binding);
            }

            return extraValues.TryGetValue(key, out RedisValue value) ? value : RedisValue.Null;
        });
    }

    private static (RedisDerivedStoreService Service, IDatabase Database, ITransaction Transaction) CreateBindingService(
        IReadOnlyList<DerivedStoreBindingEntry> entries,
        Func<DerivedStoreBindingEntry, DurableDerivedStoreSourceArtifact>? artifactFactory = null,
        Func<DerivedStoreBindingEntry, RedisValue[]>? identityFactory = null,
        string? missingArtifactId = null)
    {
        (RedisDerivedStoreService service, IDatabase database, ITransaction transaction) = CreateService();
        database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(call =>
        {
            string key = call.ArgAt<RedisKey>(0).ToString();
            DerivedStoreBindingEntry? entry = entries.FirstOrDefault(candidate =>
                key == RedisDerivedStoreService.BuildSourceArtifactKey(TenantId, candidate.MemoryUnitId));
            if (entry is null || string.Equals(entry.MemoryUnitId, missingArtifactId, StringComparison.Ordinal))
            {
                return RedisValue.Null;
            }

            DurableDerivedStoreSourceArtifact artifact = artifactFactory?.Invoke(entry) ?? CreateArtifact(entry.MemoryUnitId);
            return Serialize(artifact);
        });
        database.HashGetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>()).Returns(call =>
            call.ArgAt<RedisValue>(1) == "tenantId" ? (RedisValue)TenantId : RedisValue.Null);
        database.HashGetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>()).Returns(call =>
        {
            string key = call.ArgAt<RedisKey>(0).ToString();
            DerivedStoreBindingEntry entry = entries.Single(candidate =>
                key == IndexSchemaDefinitions.BuildSyntacticKey(TenantId, candidate.MemoryUnitId));
            return identityFactory?.Invoke(entry) ?? [TenantId, PriorCaseId, entry.MemoryUnitId];
        });
        return (service, database, transaction);
    }

    private static (RedisDerivedStoreService Service, IDatabase Database, ITransaction Transaction) CreateService()
    {
        IDatabase database = Substitute.For<IDatabase>();
        ITransaction transaction = Substitute.For<ITransaction>();
        transaction.ExecuteAsync(Arg.Any<CommandFlags>()).Returns(true);
        database.CreateTransaction(Arg.Any<object?>()).Returns(transaction);
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);
        IDatabase falkorDatabase = Substitute.For<IDatabase>();
        falkorDatabase.PingAsync(Arg.Any<CommandFlags>()).Returns(TimeSpan.Zero);
        IConnectionMultiplexer falkor = Substitute.For<IConnectionMultiplexer>();
        falkor.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(falkorDatabase);
        return (new RedisDerivedStoreService(redis, falkor, new FakeTimeProvider(Now)), database, transaction);
    }

    private static FinalizeDerivedStoreBindingRequest CreateFinalizeRequest(
        IReadOnlyList<DerivedStoreBindingEntry> entries,
        int expectedAttachmentCount)
        => new(AssociationId, IntakeId, 1, PriorCaseId, expectedAttachmentCount, entries);

    private static DerivedStoreBinding CreateBinding(IReadOnlyList<DerivedStoreBindingEntry> entries)
        => new(TenantId, AssociationId, IntakeId, 1, PriorCaseId, entries.Count - 1, entries, Now);

    private static DerivedStoreCorrectionStatus CreatePendingStatus()
    {
        StartDerivedStoreCorrectionRequest request = new(AssociationId, IntakeId, "correction-1", 1, "case-corrected");
        return new DerivedStoreCorrectionStatus(
            RedisDerivedStoreService.BuildOperationId(TenantId, request),
            DerivedStoreCorrectionState.Pending,
            AssociationId,
            IntakeId,
            request.CorrectionId,
            request.SourceVersion,
            PriorCaseId,
            request.CorrectedCaseId,
            0,
            0,
            false,
            Now.AddMinutes(60),
            null,
            null);
    }

    private static DurableDerivedStoreSourceArtifact CreateArtifact(
        string memoryUnitId,
        string tenantId = TenantId,
        string caseId = PriorCaseId)
        => new(
            tenantId,
            memoryUnitId,
            caseId,
            $"memory://{memoryUnitId}",
            SourceType.File,
            "text/plain",
            [1, 2, 3],
            "provider",
            "model",
            3,
            [],
            "tests",
            null,
            null,
            "{}",
            Now);

    private static RedisValue Serialize<T>(T value)
        => JsonSerializer.Serialize(value, MemoriesJsonContext.Options);

    private static int CountStringSetCalls(ITransaction transaction, string expectedKey)
        => transaction.ReceivedCalls().Count(call =>
            string.Equals(call.GetMethodInfo().Name, nameof(ITransaction.StringSetAsync), StringComparison.Ordinal)
            && string.Equals(call.GetArguments()[0]?.ToString(), expectedKey, StringComparison.Ordinal));
}
