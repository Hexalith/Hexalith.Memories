// <copyright file="IngestDedupReservationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using StackExchange.Redis;

/// <summary>Real-Redis concurrency coverage for the REST-ingress dedup reservation.</summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public sealed class IngestDedupReservationIntegrationTests
{
    /// <summary>Rounds of the two-contender race. A single round can be won by a non-atomic
    /// check-then-set implementation whenever the two calls happen not to overlap, so the race is
    /// repeated over fresh identities until a lost-update regression is forced to surface.</summary>
    private const int RaceRounds = 25;

    private static readonly TimeSpan ReservationTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RendezvousTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TtlObservationAllowance = TimeSpan.FromSeconds(30);

    private readonly RedisStackFixture _redis;

    public IngestDedupReservationIntegrationTests(RedisStackFixture redis) => _redis = redis;

    [Fact]
    public async Task TryReserveAsync_TwoConcurrentRealRedisCallers_ExactlyOneWinsAndLoserObservesWinnerId()
    {
        IDatabase database = _redis.Connection.GetDatabase();
        IngestDedupReservation reservation = new(
            _redis.Connection,
            NullLogger<IngestDedupReservation>.Instance);

        for (int round = 0; round < RaceRounds; round++)
        {
            string uniqueSuffix = Guid.NewGuid().ToString("N");
            string tenantId = $"tenant-{uniqueSuffix}";
            string caseId = $"case-{uniqueSuffix}";
            string sourceUri = $"file:///ingest-race-{uniqueSuffix}.pdf";
            string candidateA = $"workflow-a-{uniqueSuffix}";
            string candidateB = $"workflow-b-{uniqueSuffix}";
            string reservationKey = "ingest-reserve:" + DedupKeyBuilder.BuildIdentityKey(
                tenantId,
                caseId,
                sourceUri,
                idempotencyToken: null);

            try
            {
                using Barrier rendezvous = new(participantCount: 2);
                Task<IngestReservationResult> contenderA = Task.Run(
                    () => ReserveAfterRendezvousAsync(
                        rendezvous,
                        reservation,
                        tenantId,
                        caseId,
                        sourceUri,
                        candidateA,
                        RendezvousTimeout));
                Task<IngestReservationResult> contenderB = Task.Run(
                    () => ReserveAfterRendezvousAsync(
                        rendezvous,
                        reservation,
                        tenantId,
                        caseId,
                        sourceUri,
                        candidateB,
                        RendezvousTimeout));

                IngestReservationResult[] results = await Task.WhenAll(contenderA, contenderB);

                results
                    .Count(result => result.Outcome == IngestReservationOutcome.Reserved)
                    .ShouldBe(1, $"race round {round} must have exactly one winner");
                results
                    .Count(result => result.Outcome == IngestReservationOutcome.DuplicateInFlight)
                    .ShouldBe(1, $"race round {round} must have exactly one duplicate loser");
                results
                    .Count(result => result.Outcome == IngestReservationOutcome.FailOpen)
                    .ShouldBe(0, $"race round {round} must not fail open");

                IngestReservationResult winner = results.Single(
                    result => result.Outcome == IngestReservationOutcome.Reserved);
                IngestReservationResult loser = results.Single(
                    result => result.Outcome == IngestReservationOutcome.DuplicateInFlight);
                string expectedWinningInstanceId = results[0].Outcome == IngestReservationOutcome.Reserved
                    ? candidateA
                    : candidateB;
                string winningInstanceId = winner.ExistingInstanceId.ShouldNotBeNull();

                winningInstanceId.ShouldBe(expectedWinningInstanceId);
                loser.ExistingInstanceId.ShouldBe(winningInstanceId);

                RedisValue persistedWinner = await database.StringGetAsync(reservationKey);
                TimeSpan? persistedTtl = await database.KeyTimeToLiveAsync(reservationKey);

                persistedWinner.ToString().ShouldBe(winningInstanceId);
                TimeSpan liveTtl = persistedTtl.ShouldNotBeNull();
                liveTtl.ShouldBeGreaterThan(TimeSpan.Zero);
                liveTtl.ShouldBeGreaterThanOrEqualTo(ReservationTtl - TtlObservationAllowance);
                liveTtl.ShouldBeLessThanOrEqualTo(ReservationTtl);
            }
            finally
            {
                await reservation.ReleaseAsync(
                    tenantId,
                    caseId,
                    sourceUri,
                    idempotencyToken: null,
                    CancellationToken.None);
            }

            (await database.KeyExistsAsync(reservationKey)).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task ReleaseAsync_AfterBoundedRendezvousTimeout_TimesOutAndDeletesOnlyTheReservationKey()
    {
        string uniqueSuffix = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-{uniqueSuffix}";
        string caseId = $"case-{uniqueSuffix}";
        string sourceUri = $"file:///ingest-rendezvous-{uniqueSuffix}.pdf";
        string reservationKey = "ingest-reserve:" + DedupKeyBuilder.BuildIdentityKey(
            tenantId,
            caseId,
            sourceUri,
            idempotencyToken: null);
        string sentinelKey = $"{reservationKey}:sentinel";
        IDatabase database = _redis.Connection.GetDatabase();
        IngestDedupReservation reservation = new(
            _redis.Connection,
            NullLogger<IngestDedupReservation>.Instance);

        try
        {
            try
            {
                bool reservationStored = await database.StringSetAsync(
                    reservationKey,
                    "cleanup-target",
                    ReservationTtl);
                reservationStored.ShouldBeTrue();

                bool sentinelStored = await database.StringSetAsync(
                    sentinelKey,
                    "must-survive",
                    ReservationTtl);
                sentinelStored.ShouldBeTrue();

                using Barrier rendezvous = new(participantCount: 2);

                _ = await Should.ThrowAsync<TimeoutException>(() => ReserveAfterRendezvousAsync(
                    rendezvous,
                    reservation,
                    tenantId,
                    caseId,
                    sourceUri,
                    $"workflow-{uniqueSuffix}",
                    TimeSpan.FromMilliseconds(100)));
            }
            finally
            {
                // The deletion under proof is the production release path, not a key computed by the test:
                // a prefix-scoped or over-broad delete inside ReleaseAsync must fail the sentinel assertion.
                await reservation.ReleaseAsync(
                    tenantId,
                    caseId,
                    sourceUri,
                    idempotencyToken: null,
                    CancellationToken.None);
            }

            (await database.KeyExistsAsync(reservationKey)).ShouldBeFalse();
            (await database.StringGetAsync(sentinelKey)).ToString().ShouldBe("must-survive");
        }
        finally
        {
            _ = await database.KeyDeleteAsync(sentinelKey);
        }
    }

    private static async Task<IngestReservationResult> ReserveAfterRendezvousAsync(
        Barrier rendezvous,
        IngestDedupReservation reservation,
        string tenantId,
        string caseId,
        string sourceUri,
        string instanceId,
        TimeSpan rendezvousTimeout)
    {
        if (!rendezvous.SignalAndWait(rendezvousTimeout))
        {
            throw new TimeoutException("Both Redis reservation contenders did not reach the rendezvous in time.");
        }

        return await reservation.TryReserveAsync(
            tenantId,
            caseId,
            sourceUri,
            idempotencyToken: null,
            instanceId,
            ReservationTtl,
            CancellationToken.None).ConfigureAwait(false);
    }
}
