// <copyright file="OidcTokenAcquisitionException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Net;

/// <summary>Thrown when an OIDC client credentials token cannot be acquired or parsed.</summary>
public sealed class OidcTokenAcquisitionException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="OidcTokenAcquisitionException"/> class.</summary>
    /// <param name="statusCode">The token endpoint status code, when an HTTP response was received.</param>
    /// <param name="responseBodyPreview">A sanitized and bounded response body preview.</param>
    /// <param name="tokenEndpoint">The token endpoint used for acquisition.</param>
    /// <param name="clientId">The OIDC client identifier.</param>
    /// <param name="correlationId">The generated correlation identifier for log correlation.</param>
    /// <param name="reason">The sanitized failure reason.</param>
    /// <param name="innerException">The optional inner exception.</param>
    public OidcTokenAcquisitionException(
        HttpStatusCode? statusCode,
        string responseBodyPreview,
        string tokenEndpoint,
        string clientId,
        string correlationId,
        string reason,
        Exception? innerException = null)
        : base(FormatMessage(statusCode, tokenEndpoint, clientId, correlationId, reason), innerException)
    {
        StatusCode = statusCode;
        ResponseBodyPreview = responseBodyPreview;
        TokenEndpoint = tokenEndpoint;
        ClientId = clientId;
        CorrelationId = correlationId;
    }

    /// <summary>Gets the HTTP status code returned by the token endpoint, if available.</summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>Gets the sanitized response body preview, capped by the provider before construction.</summary>
    public string ResponseBodyPreview { get; }

    /// <summary>Gets the token endpoint used for acquisition.</summary>
    public string TokenEndpoint { get; }

    /// <summary>Gets the OIDC client identifier.</summary>
    public string ClientId { get; }

    /// <summary>Gets the generated correlation identifier.</summary>
    public string CorrelationId { get; }

    private static string FormatMessage(
        HttpStatusCode? statusCode,
        string tokenEndpoint,
        string clientId,
        string correlationId,
        string reason)
    {
        string status = statusCode.HasValue
            ? $"{(int)statusCode.Value} {statusCode.Value}"
            : "none";
        return $"OIDC token acquisition failed for client '{clientId}' at '{tokenEndpoint}' "
            + $"(status: {status}, correlationId: {correlationId}): {reason}";
    }
}
