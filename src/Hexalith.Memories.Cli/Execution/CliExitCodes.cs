// <copyright file="CliExitCodes.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Execution;

/// <summary>
/// Exit codes emitted by the CLI. Code <c>1</c> (<see cref="DomainError"/>) is emitted by domain/business
/// failures since Story 7.3; see <c>ErrorMessageCatalog</c> for per-code classification.
/// </summary>
public static class CliExitCodes
{
    /// <summary>Success — includes <c>--help</c>, <c>--version</c>, and any successful command.</summary>
    public const int Success = 0;

    /// <summary>
    /// Domain/business error from server (e.g., <c>CASE_NOT_FOUND</c>, <c>TENANT_NOT_FOUND</c>,
    /// <c>INVALID_INPUT</c>). Used since Story 7.3 — see <c>ErrorMessageCatalog</c> for the full
    /// classification.
    /// </summary>
    public const int DomainError = 1;

    /// <summary>Plumbing/config error: connection failure, bad URI, TLS failure, token-over-http guard.</summary>
    public const int Plumbing = 2;

    /// <summary>User cancellation (Ctrl-C / SIGINT).</summary>
    public const int Cancelled = 130;
}
