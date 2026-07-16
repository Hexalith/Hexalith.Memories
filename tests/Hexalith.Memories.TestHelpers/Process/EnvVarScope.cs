namespace Hexalith.Memories.TestHelpers.Process;

using System.Collections.Concurrent;
using System.Threading;

/// <summary>
/// Process-wide environment-variable scope with per-key serialization.
/// </summary>
/// <remarks>
/// <para>
/// Tests in this repository run in parallel across assemblies. Plain snapshot/restore helpers are unsafe when
/// two callers overlap on the same variable: the later scope can snapshot the already-mutated value and restore
/// the wrong state. This helper serializes scopes per env-var name for the lifetime of the scope so overlapping
/// same-key mutations cannot interleave.
/// </para>
/// <para>
/// Different env-var names still proceed concurrently.
/// </para>
/// <para>
/// A leaked scope (missing Dispose) or same-flow re-entry would previously deadlock the suite silently on
/// the non-reentrant semaphore. The wait now uses an upper-bound timeout and surfaces a diagnostic
/// <see cref="TimeoutException"/> that names the variable and the owning logical async flow, so the real
/// problem is observable instead of an untraceable hang.
/// </para>
/// </remarks>
public sealed class EnvVarScope : IDisposable
{
    // Windows env-var names are case-insensitive at the OS level, while Linux/macOS preserve case.
    // The per-name gate MUST match the current platform's semantics so Foo/foo serialize together on
    // Windows but remain distinct keys off Windows.
    private static readonly StringComparer GateComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static readonly ConcurrentDictionary<string, GateState> Gates = new(GateComparer);
    private static readonly AsyncLocal<long?> CurrentFlowId = new();
    private static long _nextFlowId;

    /// <summary>Upper bound on how long <see cref="Set"/> will wait for the per-name gate before failing loud.
    /// A leaked scope or cross-thread contention that exceeds this budget is a bug the suite should not hide.</summary>
    public static readonly TimeSpan AcquireTimeout = TimeSpan.FromMinutes(2);

    private readonly GateState _state;
    private readonly string _name;
    private readonly string? _previousValue;
    private bool _disposed;

    private EnvVarScope(string name, string? previousValue, GateState state)
    {
        _name = name;
        _previousValue = previousValue;
        _state = state;
    }

    /// <summary>Sets the env var to <paramref name="value"/> after snapshotting the prior value.</summary>
    /// <param name="name">Env var name.</param>
    /// <param name="value">Value to set (use <see langword="null"/> to clear within the scope).</param>
    /// <returns>A scope that restores the previous value on dispose.</returns>
    /// <exception cref="TimeoutException">Thrown when the per-name gate is held for longer than
    /// <see cref="AcquireTimeout"/> — indicates a leaked scope or deadlocked caller on another thread.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current logical async flow already holds the
    /// gate for the same name — the gate is non-reentrant; nested scopes on the same variable would otherwise deadlock.</exception>
    public static EnvVarScope Set(string name, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        GateState state = Gates.GetOrAdd(name, static _ => new GateState());
        long currentFlowId = GetOrCreateFlowId();

        if (Volatile.Read(ref state.OwnerFlowId) == currentFlowId)
        {
            throw new InvalidOperationException(
                $"EnvVarScope for '{name}' is already held by logical async flow {currentFlowId}. " +
                "The per-name gate is non-reentrant; dispose the outer scope before opening a new one.");
        }

        if (!state.Gate.Wait(AcquireTimeout))
        {
            throw new TimeoutException(
                $"Timed out after {AcquireTimeout} waiting to acquire EnvVarScope for '{name}'. " +
                $"Held by logical async flow {Volatile.Read(ref state.OwnerFlowId)} (0 means released). " +
                "This typically indicates a leaked scope (missing Dispose) in a prior test.");
        }

        try
        {
            Volatile.Write(ref state.OwnerFlowId, currentFlowId);
            string? previous = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
            return new EnvVarScope(name, previous, state);
        }
        catch
        {
            Volatile.Write(ref state.OwnerFlowId, 0);
            state.Gate.Release();
            throw;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Environment.SetEnvironmentVariable(_name, _previousValue);
        Volatile.Write(ref _state.OwnerFlowId, 0);
        _state.Gate.Release();
    }

    private static long GetOrCreateFlowId()
    {
        long? existing = CurrentFlowId.Value;
        if (existing is > 0)
        {
            return existing.Value;
        }

        long created = Interlocked.Increment(ref _nextFlowId);
        CurrentFlowId.Value = created;
        return created;
    }

    private sealed class GateState
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public long OwnerFlowId;
    }
}
