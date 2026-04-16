// <copyright file="CliExitCodes.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Execution;

/// <summary>
/// Exit codes emitted by the CLI. Code <c>1</c> is reserved for Story 7.3's domain-error surface and is not
/// emitted in 7.1. See the exit-code table in the story Implementation Contracts section.
/// </summary>
public static class CliExitCodes
{
    /// <summary>Success — includes <c>--help</c>, <c>--version</c>, and any successful command.</summary>
    public const int Success = 0;

    /// <summary>Reserved for Story 7.3 domain/business errors (e.g., <c>CASE_NOT_FOUND</c>).</summary>
    public const int DomainError = 1;

    /// <summary>Plumbing/config error: connection failure, bad URI, TLS failure, token-over-http guard.</summary>
    public const int Plumbing = 2;

    /// <summary>User cancellation (Ctrl-C / SIGINT).</summary>
    public const int Cancelled = 130;
}
