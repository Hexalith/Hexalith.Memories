// <copyright file="OpenBaoSessionLifetimeGuard.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

using Microsoft.Extensions.Hosting;

/// <summary>Stops a development AppHost before its non-renewable OpenBao identities expire.</summary>
internal sealed class OpenBaoSessionLifetimeGuard : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="OpenBaoSessionLifetimeGuard"/> class.</summary>
    /// <param name="applicationLifetime">The AppHost lifetime to stop when the session expires.</param>
    public OpenBaoSessionLifetimeGuard(IHostApplicationLifetime applicationLifetime)
        : this(applicationLifetime, TimeProvider.System)
    {
    }

    /// <summary>Initializes a testable instance of the <see cref="OpenBaoSessionLifetimeGuard"/> class.</summary>
    /// <param name="applicationLifetime">The AppHost lifetime to stop when the session expires.</param>
    /// <param name="timeProvider">The clock used for the expiry delay.</param>
    internal OpenBaoSessionLifetimeGuard(
        IHostApplicationLifetime applicationLifetime,
        TimeProvider timeProvider)
    {
        _applicationLifetime = applicationLifetime;
        _timeProvider = timeProvider;
    }

    /// <summary>Gets the enforced maximum development session, shorter than the 168-hour token lifetime.</summary>
    internal static TimeSpan MaximumSession { get; } = TimeSpan.FromHours(144);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(MaximumSession, _timeProvider, stoppingToken).ConfigureAwait(false);
            _applicationLifetime.StopApplication();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal AppHost shutdown.
        }
    }
}
