// <copyright file="ErrorTranslation.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Errors;

/// <summary>
/// Catalog entry that maps a server <c>ErrorResponse.Code</c> (or synthetic CLI transport code) to the
/// CLI's rendered message, suggestion, and process exit code. A <see langword="null"/>
/// <paramref name="CliMessage"/> means "use the server's <c>ErrorResponse.Message</c> verbatim" (after
/// sanitization). A <see langword="null"/> <paramref name="CliSuggestion"/> means "use the server's
/// <c>ErrorResponse.Suggestion</c> verbatim" (after sanitization). <paramref name="ExitCode"/> is always
/// set explicitly so contributors must classify each new code as domain (1) or plumbing (2).
/// </summary>
/// <param name="CliMessage">CLI-side override for the server message; <see langword="null"/> means keep the server text.</param>
/// <param name="CliSuggestion">CLI-side override for the server suggestion; <see langword="null"/> means keep the server text.</param>
/// <param name="ExitCode">Process exit code this error yields. <c>1</c> = domain, <c>2</c> = plumbing.</param>
public sealed record ErrorTranslation(string? CliMessage, string? CliSuggestion, int ExitCode);
