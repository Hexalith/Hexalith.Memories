// <copyright file="AccessTelemetryLifecycleEnabledValueWarning.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

/// <summary>
/// Warns once at startup that the lifecycle switch holds a value that is not a Boolean, so the lifecycle stayed disabled.
/// </summary>
/// <remarks>The warning names the configuration key only and never logs the configured value.</remarks>
/// <param name="logger">The logger that receives the warning.</param>
internal sealed partial class AccessTelemetryLifecycleEnabledValueWarning(
    ILogger<AccessTelemetryLifecycleEnabledValueWarning> logger) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        LogEnabledValueNotBoolean(logger);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = 7600,
        Level = LogLevel.Warning,
        Message = "Access telemetry lifecycle is disabled because AccessTelemetryLifecycle:Enabled (environment variable AccessTelemetryLifecycle__Enabled) is not a Boolean. Set it to true or false.")]
    private static partial void LogEnabledValueNotBoolean(ILogger logger);
}
