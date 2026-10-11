namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Small state store that disables retention after its first completed deletion.</summary>
internal sealed class ScriptedRetentionStateStore(DateTimeOffset expiry, Action afterFirstDelete) : IAccessTelemetryStateStore
{
    private readonly List<AccessTelemetryExpiryEntry> _entries =
    [
        new("record-a", 0, 0, "hash-a", Format(expiry)),
        new("record-b", 0, 0, "hash-b", Format(expiry)),
    ];

    /// <summary>Gets the number of undeleted records.</summary>
    public int Remaining => _entries.Count;

    /// <inheritdoc/>
    public Task<AccessTelemetryStoreWriteStatus> WriteRecordAndIndexAsync(
        AccessTelemetryRecord record,
        AccessTelemetryExpiryEntry expiryEntry,
        int ttlInSeconds,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<(IReadOnlyList<AccessTelemetryExpiryEntry> Entries, bool HasMoreDueEntries)> GetDueEntriesAsync(
        long dueMinute,
        int limit,
        CancellationToken cancellationToken)
        => Task.FromResult<(IReadOnlyList<AccessTelemetryExpiryEntry>, bool)>(([.. _entries], false));

    /// <inheritdoc/>
    public Task<AccessTelemetryDeleteStatus> DeleteAndVerifyAsync(
        AccessTelemetryExpiryEntry entry,
        CancellationToken cancellationToken)
    {
        _entries.Remove(entry);
        if (_entries.Count == 1)
        {
            afterFirstDelete();
        }

        return Task.FromResult(AccessTelemetryDeleteStatus.Deleted);
    }

    private static string Format(DateTimeOffset value)
        => value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture);
}
