// <copyright file="CollectingLoggerProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using Microsoft.Extensions.Logging;

/// <summary>Test logger provider that counts business log writes.</summary>
internal sealed class CollectingLoggerProvider : ILoggerProvider
{
    private int _count;

    /// <summary>Gets the observed business log count.</summary>
    public int Count => Volatile.Read(ref _count);

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new CollectingLogger(this);

    /// <summary>Increments the observed business log count.</summary>
    public void Record() => _ = Interlocked.Increment(ref _count);

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
