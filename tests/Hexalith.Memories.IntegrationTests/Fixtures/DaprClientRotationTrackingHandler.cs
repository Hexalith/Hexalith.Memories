// <copyright file="DaprClientRotationTrackingHandler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;

/// <summary>Tracks requests and disposal without opening a Dapr connection.</summary>
/// <param name="respondAsync">Optional response to send for a tracked request.</param>
internal sealed class DaprClientRotationTrackingHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? respondAsync = null) : HttpMessageHandler
{
    /// <summary>Gets the number of requests sent through this handler.</summary>
    internal int RequestCount { get; private set; }

    /// <summary>Gets the number of times this handler has been disposed.</summary>
    internal int DisposeCount { get; private set; }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(DisposeCount != 0, this);
        RequestCount++;
        return respondAsync?.Invoke(request, cancellationToken)
            ?? Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeCount++;
        }

        base.Dispose(disposing);
    }
}
