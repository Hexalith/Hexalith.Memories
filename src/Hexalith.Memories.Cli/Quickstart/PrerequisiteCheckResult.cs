// <copyright file="PrerequisiteCheckResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Quickstart;

/// <summary>
/// Outcome of a single prerequisite sub-check (Docker, .NET SDK, ports, OS, DAPR).
/// Soft-fail checks (DAPR, OS detection) return <see cref="Passed"/> = <see langword="true"/> with
/// an informational diagnostic; hard-fail checks (Docker, .NET, any port) return
/// <see cref="Passed"/> = <see langword="false"/> with a recovery suggestion.
/// </summary>
/// <param name="Passed">True when the check succeeded (or is a soft-pass advisory).</param>
/// <param name="Diagnostic">One-line human-readable diagnostic surfaced on stdout.</param>
/// <param name="RecoverySuggestion">Actionable remediation text; null on pass.</param>
/// <param name="IsSkipped">True when the check is informational/optional and should render as <c>SKIP</c> rather than <c>OK</c>.</param>
public sealed record PrerequisiteCheckResult(bool Passed, string Diagnostic, string? RecoverySuggestion, bool IsSkipped = false);
