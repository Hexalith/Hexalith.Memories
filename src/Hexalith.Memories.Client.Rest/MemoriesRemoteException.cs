// <copyright file="MemoriesRemoteException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Client.Rest;

using System.Net;

using Hexalith.Memories.Contracts.V1;

/// <summary>Thrown when the Memories Server returns a non-2xx response to a client call.</summary>
public sealed class MemoriesRemoteException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="MemoriesRemoteException"/> class.</summary>
    /// <param name="statusCode">The HTTP status code returned by the server.</param>
    /// <param name="error">The parsed error payload, or a synthetic one when the body could not be decoded.</param>
    /// <param name="innerException">An optional inner exception (e.g., JSON deserialization failure).</param>
    public MemoriesRemoteException(HttpStatusCode statusCode, ErrorResponse error, Exception? innerException = null)
        : base(FormatMessage(statusCode, error), innerException)
    {
        StatusCode = statusCode;
        Error = error;
    }

    /// <summary>Gets the HTTP status code returned by the server.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Gets the parsed error payload from the server.</summary>
    public ErrorResponse Error { get; }

    private static string FormatMessage(HttpStatusCode statusCode, ErrorResponse error)
        => $"Memories Server returned {(int)statusCode} {statusCode}: {error.Code} - {error.Message}";
}
