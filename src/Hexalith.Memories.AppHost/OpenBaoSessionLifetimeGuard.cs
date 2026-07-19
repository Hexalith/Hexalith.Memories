// <copyright file="OpenBaoSessionLifetimeGuard.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

using Microsoft.Extensions.Hosting;

/// <summary>Stops a development AppHost before its non-renewable OpenBao identities expire.</summary>
internal sealed class OpenBaoSessionLifetimeGuard(IHostApplicationLifetime applicationLifetime) : BackgroundService
{
    /// <summary>Gets the enforced maximum development session, shorter than the 168-hour token lifetime.</summary>
    internal static TimeSpan MaximumSession { get; } = TimeSpan.FromHours(144);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(MaximumSession, stoppingToken).ConfigureAwait(false);
            applicationLifetime.StopApplication();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal AppHost shutdown.
        }
    }
}
