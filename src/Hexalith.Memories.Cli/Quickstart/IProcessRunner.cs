// <copyright file="IProcessRunner.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Quickstart;

/// <summary>
/// Abstraction over <see cref="System.Diagnostics.Process"/> so unit tests can inject scripted
/// responses without spawning real subprocesses (see 7.4 Task 2.2).
/// </summary>
internal interface IProcessRunner
{
    /// <summary>Runs <paramref name="fileName"/> with <paramref name="arguments"/> and returns the result.</summary>
    /// <param name="fileName">The executable name or path (resolved via PATH).</param>
    /// <param name="arguments">The command-line arguments (joined with spaces).</param>
    /// <param name="timeout">Per-call timeout. On timeout the subprocess is killed and the result carries <see cref="ProcessResult.TimedOut"/>.</param>
    /// <param name="ct">Caller cancellation token — linked with <paramref name="timeout"/> so either signal kills the subprocess within ~1 second.</param>
    /// <returns>The process result.</returns>
    Task<ProcessResult> RunAsync(string fileName, string arguments, TimeSpan timeout, CancellationToken ct);
}

/// <summary>
/// Outcome of a subprocess invocation. <see cref="TimedOut"/> is true when the per-call timeout
/// fired (the subprocess was killed); <see cref="NotFound"/> is true when the executable could not
/// be launched (usually because it is not installed / not on PATH).
/// </summary>
/// <param name="ExitCode">Subprocess exit code (0 on success; synthetic -1 when the subprocess could not launch or was killed).</param>
/// <param name="StdOut">Captured standard output (may be empty).</param>
/// <param name="StdErr">Captured standard error (may be empty).</param>
/// <param name="Elapsed">Wall-clock time from launch to exit/kill.</param>
/// <param name="TimedOut">True when the per-call timeout fired.</param>
/// <param name="NotFound">True when the executable could not be launched (e.g., not installed).</param>
internal sealed record ProcessResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    TimeSpan Elapsed,
    bool TimedOut = false,
    bool NotFound = false);
