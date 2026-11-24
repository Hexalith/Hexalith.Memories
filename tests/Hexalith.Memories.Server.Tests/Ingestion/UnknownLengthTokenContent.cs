// <copyright file="UnknownLengthTokenContent.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Net;
using System.Net.Http.Headers;
using System.Text;

/// <summary>Represents a token response sent without a declared content length.</summary>
internal sealed class UnknownLengthTokenContent : HttpContent
{
    private readonly byte[] _bytes;

    /// <summary>Initializes content containing a UTF-8 JSON response.</summary>
    /// <param name="json">The response body.</param>
    public UnknownLengthTokenContent(string json)
    {
        _bytes = Encoding.UTF8.GetBytes(json);
        Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }

    /// <inheritdoc />
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => stream.WriteAsync(_bytes.AsMemory()).AsTask();

    /// <inheritdoc />
    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}
