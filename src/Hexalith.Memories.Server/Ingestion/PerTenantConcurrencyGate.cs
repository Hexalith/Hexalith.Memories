// <copyright file="PerTenantConcurrencyGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Process-local per-tenant concurrency gate for CPU-bound extraction / URL-fetch activities (Story 6.2).
/// Backed by one <see cref="SemaphoreSlim"/> per tenant so one tenant's batch cannot monopolize the
/// extraction threadpool. Horizontal scale-out (distributed semaphore) is Phase 2 per architecture §5.
/// </summary>
public sealed class PerTenantConcurrencyGate : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> _queuedWaiters = new(StringComparer.Ordinal);
    private readonly ILogger<PerTenantConcurrencyGate> _logger;
    private readonly IngestionSettings _settings;
    private bool _disposed;
    private int _concurrencyClampWarningEmitted;
    private int _timeoutClampWarningEmitted;

    /// <summary>Initializes a new instance of the <see cref="PerTenantConcurrencyGate"/> class.</summary>
    /// <param name="options">Ingestion settings supplying <c>PerTenantExtractionConcurrency</c> and acquire-timeout.</param>
    /// <param name="logger">Structured log sink for gate acquire/contend/timeout events (6204-6206).</param>
    public PerTenantConcurrencyGate(IOptions<IngestionSettings> options, ILogger<PerTenantConcurrencyGate> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _settings = options.Value;
        _logger = logger;
    }

    /// <summary>Acquires a slot for the given tenant, blocking up to <c>ExtractionGateAcquireTimeoutSeconds</c>.</summary>
    /// <param name="tenantId">Tenant identifier (case-sensitive per architecture §D8).</param>
    /// <param name="cancellationToken">Caller cancellation token — propagated untouched.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> lease; release the slot by disposing it.</returns>
    /// <exception cref="TimeoutException">The gate-acquire timeout expired before a slot became available.</exception>
    /// <exception cref="OperationCanceledException">The caller token was cancelled while waiting.</exception>
    public async Task<IAsyncDisposable> AcquireAsync(string tenantId, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        int maxPerTenant = GetBoundedMaxPerTenant();
        SemaphoreSlim semaphore = _semaphores.GetOrAdd(
            tenantId,
            _ => new SemaphoreSlim(maxPerTenant, maxPerTenant));

        if (await semaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            RateLimitingLog.LogExtractionGateAcquired(_logger, tenantId, semaphore.CurrentCount);
            return new GateLease(semaphore);
        }

        int queueDepth = _queuedWaiters.AddOrUpdate(tenantId, 1, static (_, current) => current + 1);
        RateLimitingLog.LogExtractionGateContended(_logger, tenantId, queueDepth);

        int timeoutSeconds = GetValidatedTimeoutSeconds();

        using CancellationTokenSource timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await semaphore.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            RateLimitingLog.LogExtractionGateTimeout(_logger, tenantId, timeoutSeconds);
            throw new TimeoutException(
                $"Failed to acquire per-tenant extraction gate for tenant '{tenantId}' within {timeoutSeconds}s.");
        }
        finally
        {
            int remainingQueued = _queuedWaiters.AddOrUpdate(tenantId, 0, static (_, current) => Math.Max(0, current - 1));
            if (remainingQueued == 0)
            {
                _queuedWaiters.TryRemove(tenantId, out _);
            }
        }

        RateLimitingLog.LogExtractionGateAcquired(_logger, tenantId, semaphore.CurrentCount);
        return new GateLease(semaphore);
    }

    /// <summary>Gets the number of remaining slots for a tenant (defaults to the configured ceiling if the tenant has no lease history).</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <returns>Available slot count.</returns>
    public int GetAvailableCount(string tenantId) =>
        _semaphores.TryGetValue(tenantId, out SemaphoreSlim? semaphore)
            ? semaphore.CurrentCount
            : GetBoundedMaxPerTenant();

    /// <summary>
    /// Story 7.5 Task 4.5 — gets the current ingestion queue depth for a tenant. This is the number
    /// of waiters that have been blocked on the gate (not acquired yet). Safe to call from a
    /// metric-collection callback — reads an atomic counter, no semaphore mutation.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <returns>Current number of queued waiters for the tenant (0 if none or tenant unknown).</returns>
    public int GetCurrentDepth(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return _queuedWaiters.TryGetValue(tenantId, out int depth) ? depth : 0;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        foreach (SemaphoreSlim semaphore in _semaphores.Values)
        {
            semaphore.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private int GetBoundedMaxPerTenant()
    {
        int processorCount = Math.Max(1, Environment.ProcessorCount);
        int requested = _settings.PerTenantExtractionConcurrency;
        int bounded = Math.Clamp(requested, 1, processorCount);

        if (requested != bounded && Interlocked.Exchange(ref _concurrencyClampWarningEmitted, 1) == 0)
        {
            if (requested <= 0)
            {
                _logger.LogWarning(
                    "PerTenantExtractionConcurrency={Requested} is invalid; clamped to {Bounded}.",
                    requested,
                    bounded);
            }
            else
            {
                _logger.LogWarning(
                    "PerTenantExtractionConcurrency={Requested} exceeds Environment.ProcessorCount={ProcessorCount}; clamped to {Bounded}. See docs/operations/rate-limiting.md.",
                    requested,
                    processorCount,
                    bounded);
            }
        }

        return bounded;
    }

    private int GetValidatedTimeoutSeconds()
    {
        int requested = _settings.ExtractionGateAcquireTimeoutSeconds;
        int bounded = Math.Max(1, requested);

        if (requested != bounded && Interlocked.Exchange(ref _timeoutClampWarningEmitted, 1) == 0)
        {
            _logger.LogWarning(
                "ExtractionGateAcquireTimeoutSeconds={Requested} is invalid; clamped to {Bounded}s.",
                requested,
                bounded);
        }

        return bounded;
    }

    private sealed class GateLease : IAsyncDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private int _released;

        public GateLease(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                _semaphore.Release();
            }

            return ValueTask.CompletedTask;
        }
    }
}
