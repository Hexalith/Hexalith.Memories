// <copyright file="DefaultProcessRunner.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Quickstart;

using System.ComponentModel;
using System.Diagnostics;

/// <summary>
/// Default <see cref="IProcessRunner"/> that launches a real subprocess via
/// <see cref="Process.Start(ProcessStartInfo)"/>. Links caller cancellation and the per-call timeout
/// so either signal kills the subprocess in bounded time (Task 2.2 cancellation semantics).
/// </summary>
internal sealed class DefaultProcessRunner : IProcessRunner
{
    /// <inheritdoc/>
    public async Task<ProcessResult> RunAsync(string fileName, string arguments, TimeSpan timeout, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(arguments);

        var info = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        Process? process;
        long startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            process = Process.Start(info);
        }
        catch (Win32Exception notFound)
        {
            // Executable not on PATH (or not installed).
            return new ProcessResult(
                ExitCode: -1,
                StdOut: string.Empty,
                StdErr: notFound.Message,
                Elapsed: Stopwatch.GetElapsedTime(startTimestamp),
                NotFound: true);
        }
        catch (FileNotFoundException notFound)
        {
            return new ProcessResult(
                ExitCode: -1,
                StdOut: string.Empty,
                StdErr: notFound.Message,
                Elapsed: Stopwatch.GetElapsedTime(startTimestamp),
                NotFound: true);
        }

        if (process is null)
        {
            return new ProcessResult(
                ExitCode: -1,
                StdOut: string.Empty,
                StdErr: "Process.Start returned null.",
                Elapsed: Stopwatch.GetElapsedTime(startTimestamp),
                NotFound: true);
        }

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(timeout);

        using (process)
        {
            // Reads use CancellationToken.None so buffered output survives a kill — we drain
            // explicitly after WaitForExit (success) or after TryKill (timeout/caller-cancel).
            Task<string> stdOutTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
            Task<string> stdErrTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

            try
            {
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
                string stdOut = await stdOutTask.ConfigureAwait(false);
                string stdErr = await stdErrTask.ConfigureAwait(false);
                return new ProcessResult(
                    ExitCode: process.ExitCode,
                    StdOut: stdOut,
                    StdErr: stdErr,
                    Elapsed: Stopwatch.GetElapsedTime(startTimestamp));
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested)
            {
                TryKill(process);

                // After kill, drain the read tasks with a bounded wait so we still surface whatever
                // the subprocess wrote before being killed. Using CancellationToken.None on the
                // reads (above) keeps the tasks alive past the cancel; Task.WhenAny with a short
                // timeout prevents hangs if the streams never close.
                (string stdOut, string stdErr) = await DrainReadsAsync(stdOutTask, stdErrTask).ConfigureAwait(false);

                // Caller-cancelled branch surfaces OperationCanceledException to preserve normal
                // cancellation propagation semantics; timeout branch returns a synthetic result so
                // PrerequisiteChecks can report a recovery suggestion.
                if (ct.IsCancellationRequested)
                {
                    throw;
                }

                return new ProcessResult(
                    ExitCode: -1,
                    StdOut: stdOut,
                    StdErr: stdErr,
                    Elapsed: Stopwatch.GetElapsedTime(startTimestamp),
                    TimedOut: true);
            }
        }
    }

    private static async Task<(string StdOut, string StdErr)> DrainReadsAsync(Task<string> stdOutTask, Task<string> stdErrTask)
    {
        Task drainDeadline = Task.Delay(TimeSpan.FromSeconds(1));
        Task drainAll = Task.WhenAll(stdOutTask, stdErrTask);
        _ = await Task.WhenAny(drainAll, drainDeadline).ConfigureAwait(false);
        return (SafeAwait(stdOutTask), SafeAwait(stdErrTask));
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited between the HasExited check and Kill — ignore.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Racing kill-after-exit on Windows — ignore.
        }
        catch (UnauthorizedAccessException)
        {
            // Linux: process owned by another user or PID-namespace denies SIGKILL — ignore.
        }
        catch (NotSupportedException)
        {
            // Platform does not support killing the process tree — ignore.
        }
    }

    private static string SafeAwait(Task<string> task)
    {
        try
        {
            return task.IsCompletedSuccessfully ? task.Result : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
