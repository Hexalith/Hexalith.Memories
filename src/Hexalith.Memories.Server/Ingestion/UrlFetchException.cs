// <copyright file="UrlFetchException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Thrown by UrlContentFetcher to classify HTTP/network failures with a pinned error code.</summary>
public sealed class UrlFetchException : Exception
{
    public UrlFetchException(string errorCode, string message, Exception? inner = null, int? httpStatusCode = null)
        : base(FormatMessage(errorCode, message), inner)
    {
        ErrorCode = errorCode;
        DetailMessage = message;
        HttpStatusCode = httpStatusCode;
    }

    /// <summary>Gets the pinned error code. Values: URL_NETWORK_ERROR, URL_CLIENT_ERROR, URL_SERVER_ERROR,
    /// URL_TIMEOUT, TOO_MANY_REDIRECTS, PAYLOAD_TOO_LARGE, UNSUPPORTED_CONTENT_TYPE, INVALID_URL.</summary>
    public string ErrorCode { get; }

    /// <summary>Gets the original error detail without the serialized error-code prefix.</summary>
    public string DetailMessage { get; }

    /// <summary>Gets the HTTP status code associated with the failure when one exists.</summary>
    public int? HttpStatusCode { get; }

    /// <summary>Returns false for codes whose retry will not succeed (size / URL validity / content-type drift).</summary>
    public static bool IsRetryable(string errorCode)
        => errorCode is not ("PAYLOAD_TOO_LARGE" or "UNSUPPORTED_CONTENT_TYPE" or "INVALID_URL" or "TOO_MANY_REDIRECTS");

    internal static bool TryExtractErrorCode(string? message, out string errorCode)
    {
        errorCode = string.Empty;
        if (string.IsNullOrWhiteSpace(message) || message[0] != '[')
        {
            return false;
        }

        int endIndex = message.IndexOf(']');
        if (endIndex <= 1)
        {
            return false;
        }

        string candidate = message[1..endIndex];
        for (int i = 0; i < candidate.Length; i++)
        {
            char character = candidate[i];
            if (!char.IsUpper(character) && character != '_')
            {
                return false;
            }
        }

        errorCode = candidate;
        return true;
    }

    private static string FormatMessage(string errorCode, string message)
        => $"[{errorCode}] {message}";
}
