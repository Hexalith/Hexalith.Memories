// <copyright file="AccessTelemetryLocalPurgeGateTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Runtime;

using Hexalith.Memories.AccessTelemetry.Capability;
using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

/// <summary>Proves local retained resources stop background mutation at existing gate boundaries.</summary>
public sealed class AccessTelemetryLocalPurgeGateTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("disabled")]
    [InlineData("expired")]
    [InlineData("withdrawn")]
    [InlineData("foreign")]
    [InlineData("deadline")]
    public async Task ExistingLocalReminder_ClosureBetweenDeletesPreservesRemainingRecordAndSkipsBookkeeping(string closure)
    {
        string path = Path.GetTempFileName();
        try
        {
            IConfigurationRoot configuration = CreateConfiguration(path);
            var time = new FakeTimeProvider(Now);
            var store = new ScriptedRetentionStateStore(Now.AddMinutes(-1), () => Close(closure, configuration, path, time));
            AccessTelemetryLifecycleActor actor = CreateActor(configuration, time, new AccessTelemetryLifecycleProcessor(store, time));

            AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration, time, "Development").ShouldBeTrue();
            // There is no Dapr state provider: any post-closure bookkeeping would fail this call.
            await actor.ReceiveReminderAsync("bounded-purge", [], TimeSpan.Zero, TimeSpan.FromMinutes(1));
            store.Remaining.ShouldBe(1);
            AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration, time, "Development").ShouldBeFalse();
            await actor.ReceiveReminderAsync("bounded-purge", [], TimeSpan.Zero, TimeSpan.FromMinutes(1));
            store.Remaining.ShouldBe(1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Qualification")]
    [InlineData("Development")]
    public async Task OrdinaryProfiles_IgnoreUnrelatedGateWithdrawal(string environmentName)
    {
        string path = Path.GetTempFileName();
        try
        {
            IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccessTelemetryLifecycle:Enabled"] = "true",
                ["AccessTelemetryQualification:GatePath"] = path,
            }).Build();
            var time = new FakeTimeProvider(Now);
            var store = new ScriptedRetentionStateStore(Now.AddMinutes(-1), () => File.Delete(path));
            var processor = new AccessTelemetryLifecycleProcessor(store, time);

            AccessTelemetryPurgeResult result = await processor.PurgeAsync(Now, CancellationToken.None,
                () => AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration, time, environmentName));

            result.Purged.ShouldBe(2);
            store.Remaining.ShouldBe(0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("missing-run")]
    [InlineData("foreign-run")]
    [InlineData("missing-profile")]
    [InlineData("foreign-source")]
    [InlineData("foreign-profile")]
    [InlineData("retention")]
    [InlineData("missing-deadline")]
    [InlineData("missing-path")]
    [InlineData("deadline")]
    [InlineData("malformed")]
    [InlineData("extended")]
    [InlineData("missing")]
    [InlineData("Production")]
    [InlineData("Qualification")]
    public async Task InvalidLocalIntent_RefusesReminderAndHeartbeatBeforeAnyDependency(string invalid)
    {
        string path = Path.GetTempFileName();
        try
        {
            IConfigurationRoot configuration = CreateConfiguration(path);
            var time = new FakeTimeProvider(Now);
            string environmentName = "Development";
            switch (invalid)
            {
                case "missing-run": configuration["AccessTelemetryLifecycle:LocalFunctionalRunId"] = null; break;
                case "foreign-run": configuration["HEXALITH_G4_RUN_ID"] = "foreign"; break;
                case "missing-profile": configuration["AccessTelemetryLifecycle:LocalFunctionalProfileJson"] = null; break;
                case "foreign-source": configuration["HEXALITH_SOURCE_MAPPING_HASH"] = new string('b', 64); break;
                case "foreign-profile": configuration["AccessTelemetryQualification:LocalProfileHash"] = new string('b', 64); break;
                case "retention": configuration["AccessTelemetryLifecycle:Retention"] = "02:00:00"; break;
                case "missing-deadline": configuration["AccessTelemetryLifecycle:CapabilityEvidence:ValidUntilUtc"] = null; break;
                case "missing-path": configuration["AccessTelemetryQualification:GatePath"] = null; break;
                case "deadline": time.Advance(TimeSpan.FromHours(2)); break;
                case "malformed": File.WriteAllText(path, "{}"); break;
                case "extended": WriteGate(path, configuration, Now.AddMinutes(16)); break;
                case "missing": File.Delete(path); break;
                default: environmentName = invalid; break;
            }

            var actor = new AccessTelemetryLifecycleActor(
                ActorHost.CreateForTest<AccessTelemetryLifecycleActor>(new ActorTestOptions { ActorId = new ActorId("global") }),
                null!, null!, null!, new AccessTelemetryRuntimeOptionsProvider(new AccessTelemetryOptions()), null!, time, configuration,
                new HostingEnvironment { EnvironmentName = environmentName });

            AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration, time, environmentName).ShouldBeFalse();
            await actor.PurgeAsync();
            WriterHeartbeatResponse response = await actor.HeartbeatAsync(new WriterHeartbeatRequest { Heartbeat = null!, ClockAttestation = null! });
            response.Accepted.ShouldBeFalse();
            response.Reason.ShouldBe(AccessTelemetryReason.ConfigurationInvalid);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task DisabledStartup_EnablingGateInitializesBeforeFirstNormalHeartbeat()
    {
        string path = Path.GetTempFileName();
        try
        {
            IConfigurationRoot configuration = CreateConfiguration(path);
            WriteGate(path, configuration, Now.AddMinutes(10), "disabled");
            var time = new FakeTimeProvider(Now);
            AccessTelemetryOptions options = configuration.GetSection("AccessTelemetryLifecycle").Get<AccessTelemetryOptions>()! with
            {
                MarkerKeyGeneration = "mk-a",
            };
            var clock = new LocalPurgeTestClockGate(Now);
            var evidence = new LocalPurgeTestClockEvidenceProvider();
            var runtime = new AccessTelemetryRuntimeGate(time);
            runtime.Publish(new AccessTelemetryCapabilityGateResult(true, true, AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None)
            {
                ValidUntilUtc = Now.AddHours(2),
            });
            ActorTimerManager timers = Substitute.For<ActorTimerManager>();
            IActorStateManager states = Substitute.For<IActorStateManager>();
            AccessTelemetryLifecycleActorState? saved = null;
            states.TryGetStateAsync<AccessTelemetryLifecycleActorState>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(_ => new ConditionalValue<AccessTelemetryLifecycleActorState>(saved is not null, saved!));
            states.SetStateAsync(Arg.Any<string>(), Arg.Any<AccessTelemetryLifecycleActorState>(), Arg.Any<CancellationToken>())
                .Returns(call => { saved = call.ArgAt<AccessTelemetryLifecycleActorState>(1); return Task.CompletedTask; });
            var optionsProvider = new AccessTelemetryRuntimeOptionsProvider(options);
            optionsProvider.Publish(options);
            var actor = new AccessTelemetryLifecycleActor(
                ActorHost.CreateForTest<AccessTelemetryLifecycleActor>(new ActorTestOptions { ActorId = new ActorId("global"), TimerManager = timers }),
                null!, clock, runtime, optionsProvider, evidence, time, configuration,
                new HostingEnvironment { EnvironmentName = "Development" });
            typeof(Actor).GetProperty(nameof(Actor.StateManager))!.SetValue(actor, states);
            MethodInfo activate = typeof(AccessTelemetryLifecycleActor).GetMethod("OnActivateAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

            await ((Task)activate.Invoke(actor, null)!);
            saved.ShouldBeNull();
            await timers.DidNotReceive().RegisterReminderAsync(Arg.Any<ActorReminder>());
            WriteGate(path, configuration, Now.AddMinutes(10));
            WriterHeartbeatResponse response = await actor.HeartbeatAsync(new WriterHeartbeatRequest
            {
                ClockAttestation = null!,
                Heartbeat = new WriterHeartbeat
                {
                    DeploymentId = "owned-run", ServiceInstanceId = "01K00000000000000000000001", ProcessEpoch = "01K00000000000000000000002",
                    MarkerKeyGeneration = "mk-a", OldKeyQueueCount = 0, LeaseExpiresAtUnixMilliseconds = Now.AddSeconds(30).ToUnixTimeMilliseconds(),
                },
            });

            response.Accepted.ShouldBeTrue();
            saved.ShouldNotBeNull().Configuration.ShouldNotBeNull();
            saved!.Writers.Count.ShouldBe(1);
            await timers.Received(1).RegisterReminderAsync(Arg.Any<ActorReminder>());
            WriteGate(path, configuration, Now.AddMinutes(10), "disabled");
            int mutations = states.ReceivedCalls().Count();
            await ((Task)activate.Invoke(actor, null)!);
            states.ReceivedCalls().Count().ShouldBe(mutations);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("closed", 0, 0, 0)]
    [InlineData("catalog-read", 0, 0, 0)]
    [InlineData("catalog-commit", 0, 1, 0)]
    [InlineData("record-commit", 1, 2, 0)]
    [InlineData("bookkeeping", 2, 3, 0)]
    [InlineData("Development", 2, 3, 1)]
    [InlineData("Production", 2, 3, 1)]
    [InlineData("Qualification", 2, 3, 1)]
    public async Task ActorBatch_StopsAtLocalGateMutationBoundariesAndPreservesOrdinaryWrites(string closure, int accepted, int transactions, int controlWrites)
    {
        string path = Path.GetTempFileName();
        try
        {
            IConfigurationRoot configuration = CreateConfiguration(path);
            bool ordinary = closure is "Development" or "Production" or "Qualification";
            if (ordinary)
            {
                foreach (string key in new[] { "LocalFunctionalRunId", "LocalFunctionalProfileJson", "LocalFunctionalSourceMappingHash" })
                {
                    configuration["AccessTelemetryLifecycle:" + key] = null;
                }

                configuration["AccessTelemetryQualification:LocalProfileHash"] = null;
            }

            var time = new FakeTimeProvider(Now);
            AccessTelemetryOptions options = configuration.GetSection("AccessTelemetryLifecycle").Get<AccessTelemetryOptions>()! with
            {
                ConfigurationEpoch = "01K00000000000000000000000", MarkerKeyGeneration = "mk-2026a",
            };
            var provider = new AccessTelemetryRuntimeOptionsProvider(options);
            provider.Publish(options);
            var runtime = new AccessTelemetryRuntimeGate(time);
            runtime.Publish(new AccessTelemetryCapabilityGateResult(true, true, AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None, Now.AddHours(2)));
            var state = new TransactionalDaprState();
            if (closure == "closed") WriteGate(path, configuration, Now.AddMinutes(10), "disabled");
            if (closure == "catalog-read")
            {
                state.Client.When(client => client.GetStateAndETagAsync<AccessTelemetryExpiryCatalog>(
                    Arg.Any<string>(), Arg.Any<string>(), Dapr.Client.ConsistencyMode.Strong, Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>()))
                    .Do(_ => WriteGate(path, configuration, Now.AddMinutes(10), "disabled"));
            }

            state.BeforeTransaction = () =>
            {
                if ((closure == "catalog-commit" && state.Transactions.Count == 0)
                    || ((closure == "record-commit" || ordinary) && state.Transactions.Count == 1))
                {
                    WriteGate(path, configuration, Now.AddMinutes(10), "disabled");
                }
            };
            var control = new AccessTelemetryLifecycleActorState
            {
                Configuration = new LifecycleConfigurationEpoch
                {
                    Epoch = options.ConfigurationEpoch, SchemaVersion = 1, ComponentProfileHash = options.ComponentProfileHash,
                    RetentionSeconds = 3600, MarkerKeyGeneration = options.MarkerKeyGeneration,
                },
            };
            IActorStateManager states = Substitute.For<IActorStateManager>();
            int reads = 0;
            states.TryGetStateAsync<AccessTelemetryLifecycleActorState>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                if (++reads == 2 && closure == "bookkeeping") WriteGate(path, configuration, Now.AddMinutes(10), "disabled");
                return new ConditionalValue<AccessTelemetryLifecycleActorState>(true, control);
            });
            states.SetStateAsync(Arg.Any<string>(), Arg.Any<AccessTelemetryLifecycleActorState>(), Arg.Any<CancellationToken>())
                .Returns(call => { control = call.ArgAt<AccessTelemetryLifecycleActorState>(1); return Task.CompletedTask; });
            var environment = new HostingEnvironment { EnvironmentName = ordinary ? closure : "Development" };
            var store = new DaprAccessTelemetryStateStore(state.Client, time, configuration, environment);
            var actor = new AccessTelemetryLifecycleActor(
                ActorHost.CreateForTest<AccessTelemetryLifecycleActor>(new ActorTestOptions { ActorId = new ActorId("global") }),
                new AccessTelemetryLifecycleProcessor(store, time, options), new LocalPurgeTestClockGate(Now), runtime, provider,
                new LocalPurgeTestClockEvidenceProvider(), time, configuration, environment);
            typeof(Actor).GetProperty(nameof(Actor.StateManager))!.SetValue(actor, states);
            AccessTelemetryRecord[] records = Enumerable.Range(1, 2).Select(ordinal =>
            {
                AccessTelemetryRecord record = AccessTelemetryStateStoreTestRecords.CreateRecord("01K0000000000000000000000" + ordinal) with
                {
                    AcceptedAtUtc = AccessTelemetryStateStoreTestRecords.Format(Now), EmittedAtUtc = AccessTelemetryStateStoreTestRecords.Format(Now),
                    ExpiresAtUtc = AccessTelemetryStateStoreTestRecords.Format(Now.AddHours(1)), EnvelopeHash = string.Empty,
                };
                return record with { EnvelopeHash = AccessTelemetryCanonicalizer.CalculateEnvelopeHash(record) };
            }).ToArray();
            var request = new AccessTelemetryWriteBatchRequest
            {
                ConfigurationEpoch = options.ConfigurationEpoch, ComponentProfileHash = options.ComponentProfileHash,
                RequestingProcessEpoch = "process", RequestingServiceInstanceId = "instance", Records = records,
                ClockAttestation = new SignedClockAttestation
                {
                    DeploymentId = "owned-run", AppId = "memories", ServiceInstanceId = "clock", ProcessEpoch = "clock-process",
                    ComponentProfileHash = options.ComponentProfileHash, RequestingProcessEpoch = "process", RequestingServiceInstanceId = "instance",
                    Nonce = "nonce", NotBeforeUnixMilliseconds = Now.ToUnixTimeMilliseconds(), NotAfterUnixMilliseconds = Now.ToUnixTimeMilliseconds(),
                    IssuedAtUnixMilliseconds = Now.ToUnixTimeMilliseconds(), ExpiresAtUnixMilliseconds = Now.AddSeconds(10).ToUnixTimeMilliseconds(),
                    SignerKeyEpoch = "clock-key", Signature = "offline-clock-double",
                },
            };

            AccessTelemetryWriteBatchResponse response = await actor.WriteBatchAsync(request);

            response.Accepted.ShouldBe(accepted);
            response.Rejected.ShouldBe(2 - accepted);
            response.Reason.ShouldBe(ordinary ? AccessTelemetryReason.None : AccessTelemetryReason.ConfigurationInvalid);
            state.Transactions.Count.ShouldBe(transactions);
            state.Contains(AccessTelemetryStateStoreTestRecords.RecordKey(records[0].RecordId)).ShouldBe(accepted > 0);
            state.Contains(AccessTelemetryStateStoreTestRecords.RecordKey(records[1].RecordId)).ShouldBe(accepted == 2);
            await states.Received(controlWrites).SetStateAsync(Arg.Any<string>(), Arg.Any<AccessTelemetryLifecycleActorState>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LocalGate_KeepsFifteenMinuteMaximumAndImmutableDeadline()
    {
        string path = Path.GetTempFileName();
        try
        {
            IConfigurationRoot configuration = CreateConfiguration(path);
            var time = new FakeTimeProvider(Now);
            WriteGate(path, configuration, Now.AddMinutes(15));
            AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration, time, "Development").ShouldBeTrue();
            WriteGate(path, configuration, Now.AddMinutes(15).AddSeconds(6));
            AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration, time, "Development").ShouldBeFalse();
            time.Advance(TimeSpan.FromHours(2));
            WriteGate(path, configuration, time.GetUtcNow().AddMinutes(10));
            AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration, time, "Development").ShouldBeFalse();
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static AccessTelemetryLifecycleActor CreateActor(IConfiguration configuration, FakeTimeProvider time, AccessTelemetryLifecycleProcessor processor)
    {
        var clock = new LocalPurgeTestClockGate(Now);
        var evidence = new LocalPurgeTestClockEvidenceProvider();
        return new AccessTelemetryLifecycleActor(
            ActorHost.CreateForTest<AccessTelemetryLifecycleActor>(new ActorTestOptions { ActorId = new ActorId("global") }),
            processor, clock, null!, new AccessTelemetryRuntimeOptionsProvider(new AccessTelemetryOptions()), evidence, time, configuration,
            new HostingEnvironment { EnvironmentName = "Development" });
    }

    private static IConfigurationRoot CreateConfiguration(string path)
    {
        string sourceHash = new('a', 64);
        string profile = JsonSerializer.Serialize(new
        {
            purpose = "local-functional-retention", runId = "owned-run", sourceMappingHash = sourceHash, componentType = "state.postgresql/v2",
            runtimeImage = "ghcr.io/dapr/daprd:1.18.1@sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8",
            postgresqlImage = "docker.io/library/postgres:18.6-trixie@sha256:5a5a84b19854a9ffaa54082c166ff4ec27473a361e496e5ea167f298f2da9722",
            sslMode = "verify-full", retentionSeconds = 3600, qualificationCredit = false,
        });
        string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(profile)));
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AccessTelemetryLifecycle:Enabled"] = "true",
            ["AccessTelemetryLifecycle:Retention"] = "01:00:00",
            ["AccessTelemetryLifecycle:DeploymentId"] = "owned-run",
            ["AccessTelemetryLifecycle:ComponentProfileHash"] = hash,
            ["AccessTelemetryLifecycle:LocalFunctionalRunId"] = "owned-run",
            ["AccessTelemetryLifecycle:LocalFunctionalProfileJson"] = profile,
            ["AccessTelemetryLifecycle:LocalFunctionalSourceMappingHash"] = sourceHash,
            ["AccessTelemetryLifecycle:CapabilityEvidence:ValidUntilUtc"] = Now.AddHours(2).ToString("O", CultureInfo.InvariantCulture),
            ["AccessTelemetryQualification:GatePath"] = path,
            ["AccessTelemetryQualification:LocalProfileHash"] = hash,
            ["HEXALITH_G4_RUN_ID"] = "owned-run",
            ["HEXALITH_SOURCE_MAPPING_HASH"] = sourceHash,
        }).Build();
        WriteGate(path, configuration, Now.AddMinutes(10));
        return configuration;
    }

    private static void WriteGate(string path, IConfiguration configuration, DateTimeOffset expiry, string state = "enabled")
        => File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schemaVersion = 1, state, profileSha256 = configuration["AccessTelemetryLifecycle:ComponentProfileHash"], expiresUtcMs = expiry.ToUnixTimeMilliseconds(),
        }));

    private static void Close(string closure, IConfiguration configuration, string path, FakeTimeProvider time)
    {
        switch (closure)
        {
            case "disabled": WriteGate(path, configuration, Now.AddMinutes(10), "disabled"); break;
            case "expired": time.Advance(TimeSpan.FromMinutes(10)); break;
            case "withdrawn": File.Delete(path); break;
            case "foreign": configuration["HEXALITH_G4_RUN_ID"] = "foreign"; break;
            case "deadline": time.Advance(TimeSpan.FromHours(2)); break;
        }
    }
}
