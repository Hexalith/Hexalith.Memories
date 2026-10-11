namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using Dapr.Actors;
using Dapr.Actors.Runtime;

using Hexalith.Memories.AccessTelemetry.Lifecycle;

using Microsoft.Extensions.Configuration;

using Shouldly;

/// <summary>Verifies a registered actor's retention reminder obeys the live disable control.</summary>
public sealed class AccessTelemetryRetentionDisableTests
{
    [Fact]
    public async Task ExistingReminder_Disabled_SkipsPurgeAndCanBeReenabled()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccessTelemetryLifecycle:Enabled"] = "false",
            })
            .Build();
        ActorHost host = ActorHost.CreateForTest<AccessTelemetryLifecycleActor>(
            new ActorTestOptions { ActorId = new ActorId("global") });
        var actor = new AccessTelemetryLifecycleActor(
            host,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            configuration);

        await actor.ReceiveReminderAsync("bounded-purge", [], TimeSpan.Zero, TimeSpan.FromMinutes(1));
        AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration).ShouldBeFalse();

        configuration["AccessTelemetryLifecycle:Enabled"] = "true";
        AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration).ShouldBeTrue();
    }
}
