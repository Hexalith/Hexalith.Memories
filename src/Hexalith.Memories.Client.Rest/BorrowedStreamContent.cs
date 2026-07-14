// <copyright file="BorrowedStreamContent.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Client.Rest;

using System.Net;

/// <summary>Streams caller-owned content without disposing the borrowed source stream.</summary>
internal sealed class BorrowedStreamContent(Stream source) : HttpContent
{
    /// <inheritdoc/>
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => source.CopyToAsync(stream);

    /// <inheritdoc/>
    protected override Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context,
        CancellationToken cancellationToken)
        => source.CopyToAsync(stream, cancellationToken);

    /// <inheritdoc/>
    protected override bool TryComputeLength(out long length)
    {
        if (source.CanSeek)
        {
            length = source.Length - source.Position;
            return true;
        }

        length = 0;
        return false;
    }
}
