// <copyright file="ThrowingHttpContent.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.ClientRest;

using System.Net;

/// <summary>Provides response content that fails when the client attempts to read its stream.</summary>
/// <param name="exceptionFactory">Creates the exception raised for each read attempt.</param>
internal sealed class ThrowingHttpContent(Func<Exception> exceptionFactory) : HttpContent
{
    /// <inheritdoc/>
    protected override Task<Stream> CreateContentReadStreamAsync()
        => Task.FromException<Stream>(exceptionFactory());

    /// <inheritdoc/>
    protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
        => Task.FromException<Stream>(exceptionFactory());

    /// <inheritdoc/>
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => Task.FromException(exceptionFactory());

    /// <inheritdoc/>
    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}
