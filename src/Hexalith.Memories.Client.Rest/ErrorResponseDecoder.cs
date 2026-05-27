// <copyright file="ErrorResponseDecoder.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Client.Rest;

using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;

/// <summary>Parses <see cref="ErrorResponse"/> bodies from non-2xx server responses.</summary>
internal static class ErrorResponseDecoder
{
    /// <summary>
    /// Attempts to deserialize the response body as an <see cref="ErrorResponse"/>; returns a synthetic
    /// payload if the body is missing, empty, or malformed. Centralizing the failure shape here lets
    /// Story 7.3 re-use it to render actionable messages.
    /// </summary>
    /// <param name="response">The non-2xx response.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A parsed or synthetic <see cref="ErrorResponse"/>.</returns>
    public static async Task<ErrorResponse> DecodeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            ErrorResponse? parsed = await response.Content.ReadFromJsonAsync<ErrorResponse>(
                MemoriesJsonContext.Options,
                ct).ConfigureAwait(false);
            if (parsed is not null && !string.IsNullOrEmpty(parsed.Code))
            {
                return parsed;
            }
        }
        catch (NotSupportedException)
        {
            // Content-Type not JSON — fall through to synthetic response below.
        }
        catch (System.Text.Json.JsonException)
        {
            // Malformed JSON — fall through to synthetic response below.
        }

        string fallbackBody = await SafeReadBodyAsync(response, ct).ConfigureAwait(false);
        return new ErrorResponse(
            Code: "HTTP_" + ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture),
            Message: string.IsNullOrWhiteSpace(fallbackBody) ? response.ReasonPhrase ?? "Unknown error" : fallbackBody,
            Suggestion: string.Empty);
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            return string.Empty;
        }
    }
}
