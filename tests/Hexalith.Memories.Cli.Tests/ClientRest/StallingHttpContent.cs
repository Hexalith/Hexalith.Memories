// <copyright file="StallingHttpContent.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.ClientRest;

using System.Net;

/// <summary>Provides response content that stalls until the supplied cancellation token is cancelled.</summary>
internal sealed class StallingHttpContent : HttpContent
{
    /// <inheritdoc/>
    protected override Task<Stream> CreateContentReadStreamAsync()
        => Task.Delay(Timeout.InfiniteTimeSpan).ContinueWith(
            static _ => Stream.Null,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

    /// <inheritdoc/>
    protected override async Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        return Stream.Null;
    }

    /// <inheritdoc/>
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => Task.Delay(Timeout.InfiniteTimeSpan);

    /// <inheritdoc/>
    protected override Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context,
        CancellationToken cancellationToken)
        => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

    /// <inheritdoc/>
    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}
