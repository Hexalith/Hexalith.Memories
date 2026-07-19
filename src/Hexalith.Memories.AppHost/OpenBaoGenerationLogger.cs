// <copyright file="OpenBaoGenerationLogger.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

using Microsoft.Extensions.Logging;

/// <summary>Provides secret-safe structured diagnostics for OpenBao generation recovery.</summary>
internal static partial class OpenBaoGenerationLogger
{
    /// <summary>Logs a generation failure before the disposable AppHost is stopped.</summary>
    /// <param name="logger">The AppHost logger.</param>
    /// <param name="generationNumber">The failed generation number.</param>
    /// <param name="exception">The secret-safe exception.</param>
    [LoggerMessage(
        EventId = 2901,
        Level = LogLevel.Critical,
        Message = "OpenBao generation {GenerationNumber} failed; stopping the disposable AppHost to tear down unknown bootstrap state.")]
    internal static partial void GenerationFailed(
        ILogger logger,
        int generationNumber,
        Exception exception);

    /// <summary>Logs a recovery-watcher failure before the disposable AppHost is stopped.</summary>
    /// <param name="logger">The AppHost logger.</param>
    /// <param name="exception">The secret-safe exception.</param>
    [LoggerMessage(
        EventId = 2902,
        Level = LogLevel.Critical,
        Message = "OpenBao generation recovery watcher failed; stopping the disposable AppHost.")]
    internal static partial void RecoveryWatcherFailed(
        ILogger logger,
        Exception exception);
}
