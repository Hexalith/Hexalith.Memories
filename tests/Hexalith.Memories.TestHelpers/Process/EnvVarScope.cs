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
/// </remarks>
public sealed class EnvVarScope : IDisposable
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.Ordinal);

    private readonly SemaphoreSlim _gate;
    private readonly string _name;
    private readonly string? _previousValue;
    private bool _disposed;

    private EnvVarScope(string name, string? previousValue, SemaphoreSlim gate)
    {
        _name = name;
        _previousValue = previousValue;
        _gate = gate;
    }

    /// <summary>Sets the env var to <paramref name="value"/> after snapshotting the prior value.</summary>
    /// <param name="name">Env var name.</param>
    /// <param name="value">Value to set (use <see langword="null"/> to clear within the scope).</param>
    /// <returns>A scope that restores the previous value on dispose.</returns>
    public static EnvVarScope Set(string name, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        SemaphoreSlim gate = Gates.GetOrAdd(name, static _ => new SemaphoreSlim(1, 1));
        gate.Wait();
        try
        {
            string? previous = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
            return new EnvVarScope(name, previous, gate);
        }
        catch
        {
            gate.Release();
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
        _gate.Release();
    }
}