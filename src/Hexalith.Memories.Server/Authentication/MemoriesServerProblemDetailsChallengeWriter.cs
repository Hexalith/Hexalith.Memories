// <copyright file="MemoriesServerProblemDetailsChallengeWriter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

/// <summary>Writes RFC 6750-compatible Server authentication challenge responses.</summary>
internal static class MemoriesServerProblemDetailsChallengeWriter
{
    /// <summary>Writes a sanitized 401 ProblemDetails response.</summary>
    /// <param name="context">The challenge context.</param>
    /// <returns>A task that completes when the response has been written.</returns>
    public static Task WriteAsync(JwtBearerChallengeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.HandleResponse();

        ChallengeKind kind = GetChallengeKind(context.Error, context.AuthenticateFailure);
        context.Response.Headers.WWWAuthenticate = GetWwwAuthenticateHeader(kind);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = kind == ChallengeKind.ExpiredToken
                ? "https://hexalith.dev/problems/token-expired"
                : "https://hexalith.dev/problems/authentication-required",
            Detail = GetDetailMessage(kind),
            Instance = context.Request.Path,
        };

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: CancellationToken.None);
    }

    private static ChallengeKind GetChallengeKind(string? error, Exception? failure)
    {
        if (failure is SecurityTokenExpiredException)
        {
            return ChallengeKind.ExpiredToken;
        }

        if (failure is not null || string.Equals(error, "invalid_token", StringComparison.OrdinalIgnoreCase))
        {
            return ChallengeKind.InvalidToken;
        }

        return ChallengeKind.MissingToken;
    }

    private static string GetDetailMessage(ChallengeKind kind) => kind switch
    {
        ChallengeKind.ExpiredToken => "The provided Memories Server bearer token has expired.",
        ChallengeKind.InvalidToken => "The provided Memories Server bearer token is invalid.",
        _ => "Bearer authentication is required to access the Memories Server API.",
    };

    private static string GetWwwAuthenticateHeader(ChallengeKind kind) => kind switch
    {
        ChallengeKind.ExpiredToken => "Bearer realm=\"hexalith-memories-server\", error=\"invalid_token\", error_description=\"The token has expired\"",
        ChallengeKind.InvalidToken => "Bearer realm=\"hexalith-memories-server\", error=\"invalid_token\", error_description=\"The token is invalid\"",
        _ => "Bearer realm=\"hexalith-memories-server\"",
    };

    private enum ChallengeKind
    {
        MissingToken,
        InvalidToken,
        ExpiredToken,
    }
}
