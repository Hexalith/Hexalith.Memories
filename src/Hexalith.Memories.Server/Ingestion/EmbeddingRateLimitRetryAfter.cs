// <copyright file="EmbeddingRateLimitRetryAfter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Globalization;

/// <summary>Normalizes and transports sanitized provider retry-after values across DAPR workflow failure details.</summary>
internal static class EmbeddingRateLimitRetryAfter
{
    /// <summary>The fallback delay when a provider 429 has no usable Retry-After value.</summary>
    public const int DefaultSeconds = 30;

    /// <summary>The maximum durable wait accepted from a provider Retry-After value.</summary>
    public const int MaxSeconds = 3600;

    private const string Marker = "ProviderRetryAfterSeconds=";

    /// <summary>Appends a sanitized provider retry-after marker to an activity exception message.</summary>
    /// <param name="message">The existing sanitized message.</param>
    /// <param name="retryAfterSeconds">The effective retry-after seconds.</param>
    /// <returns>The message carrying a workflow-parseable retry-after marker.</returns>
    public static string AppendProviderMarker(string message, int retryAfterSeconds)
        => $"{message} {Marker}{NormalizeSeconds(retryAfterSeconds).ToString(CultureInfo.InvariantCulture)}.";

    /// <summary>Normalizes a retry-after value to the provider 429 workflow contract.</summary>
    /// <param name="retryAfterSeconds">The provider-supplied or activity-effective retry-after seconds.</param>
    /// <returns><see cref="DefaultSeconds"/> for absent/non-positive values, otherwise clamped to [1, 3600].</returns>
    public static int NormalizeSeconds(int retryAfterSeconds)
        => retryAfterSeconds <= 0
            ? DefaultSeconds
            : Math.Clamp(retryAfterSeconds, 1, MaxSeconds);

    /// <summary>Attempts to extract a sanitized provider retry-after value from DAPR failure details.</summary>
    /// <param name="errorMessage">The workflow task failure message.</param>
    /// <param name="retryAfterSeconds">The normalized retry-after value when a provider marker is present.</param>
    /// <returns><c>true</c> when the failure details identify a provider 429, otherwise <c>false</c>.</returns>
    public static bool TryExtractProviderSeconds(string? errorMessage, out int retryAfterSeconds)
    {
        retryAfterSeconds = DefaultSeconds;
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return false;
        }

        int markerIndex = errorMessage.IndexOf(Marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return false;
        }

        int valueStart = markerIndex + Marker.Length;
        int valueLength = 0;
        while (valueStart + valueLength < errorMessage.Length
            && (char.IsDigit(errorMessage[valueStart + valueLength])
                || (valueLength == 0 && errorMessage[valueStart + valueLength] == '-')))
        {
            valueLength++;
        }

        if (valueLength == 0
            || !int.TryParse(
                errorMessage.AsSpan(valueStart, valueLength),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int parsed))
        {
            retryAfterSeconds = DefaultSeconds;
            return true;
        }

        retryAfterSeconds = NormalizeSeconds(parsed);
        return true;
    }
}
