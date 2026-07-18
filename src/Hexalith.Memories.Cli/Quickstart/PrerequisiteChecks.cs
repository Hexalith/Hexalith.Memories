// <copyright file="PrerequisiteChecks.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Quickstart;

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

/// <summary>
/// Sub-checks for wizard step 1 (prerequisite verification). Docker, .NET 10 SDK, and port
/// availability are hard-fail signals; OS platform is informational; DAPR CLI is soft-fail
/// (its absence is expected for local Aspire-managed dev).
/// </summary>
internal sealed partial class PrerequisiteChecks
{
    /// <summary>The set of ports the quickstart verifies for availability.</summary>
    public static readonly IReadOnlyList<int> DefaultPorts = [5000, 6379, 6380, 3500, 50001];

    private static readonly TimeSpan DockerTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DotnetTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DaprTimeout = TimeSpan.FromSeconds(3);
    private static readonly Version MinimumDotnetSdkVersion = new(10, 0, 302);

    private readonly IProcessRunner _processRunner;

    /// <summary>Initializes a new instance of the <see cref="PrerequisiteChecks"/> class.</summary>
    /// <param name="processRunner">The process runner abstraction (inject a fake in tests).</param>
    public PrerequisiteChecks(IProcessRunner processRunner)
    {
        ArgumentNullException.ThrowIfNull(processRunner);
        _processRunner = processRunner;
    }

    /// <summary>Checks that the Docker daemon responds to <c>docker ps</c> within 5 seconds.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The sub-check result.</returns>
    public async Task<PrerequisiteCheckResult> CheckDockerAsync(CancellationToken ct)
    {
        ProcessResult result = await _processRunner.RunAsync("docker", "ps", DockerTimeout, ct).ConfigureAwait(false);

        if (result.NotFound)
        {
            return new PrerequisiteCheckResult(
                Passed: false,
                Diagnostic: "Docker command not found on PATH.",
                RecoverySuggestion: "Install Docker Desktop (https://docs.docker.com/desktop/) or add the docker CLI to PATH, then retry. See docs/dev/quickstart.md for OS-specific setup.");
        }

        if (result.TimedOut)
        {
            return new PrerequisiteCheckResult(
                Passed: false,
                Diagnostic: $"'docker ps' timed out after {DockerTimeout.TotalSeconds:F0}s.",
                RecoverySuggestion: "Start Docker Desktop or an existing Docker daemon, then retry. See docs/dev/quickstart.md for OS-specific setup.");
        }

        if (result.ExitCode != 0)
        {
            return new PrerequisiteCheckResult(
                Passed: false,
                Diagnostic: $"Docker daemon not reachable (exit {result.ExitCode}).",
                RecoverySuggestion: "Install Docker Desktop (https://docs.docker.com/desktop/) or start an existing daemon, then retry. See docs/dev/quickstart.md for OS-specific setup.");
        }

        return new PrerequisiteCheckResult(
            Passed: true,
            Diagnostic: $"Docker reachable ({result.Elapsed.TotalMilliseconds:F0}ms).",
            RecoverySuggestion: null);
    }

    /// <summary>Checks that at least one compatible .NET 10 SDK is installed via <c>dotnet --list-sdks</c>.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The sub-check result.</returns>
    public async Task<PrerequisiteCheckResult> CheckDotnetSdkAsync(CancellationToken ct)
    {
        ProcessResult result = await _processRunner.RunAsync("dotnet", "--list-sdks", DotnetTimeout, ct).ConfigureAwait(false);

        if (result.NotFound)
        {
            return new PrerequisiteCheckResult(
                Passed: false,
                Diagnostic: ".NET SDK (dotnet) not found on PATH.",
                RecoverySuggestion: "Install .NET SDK 10.0.302 or newer from https://dotnet.microsoft.com/download/dotnet/10.0, then retry.");
        }

        if (result.TimedOut)
        {
            return new PrerequisiteCheckResult(
                Passed: false,
                Diagnostic: $"'dotnet --list-sdks' timed out after {DotnetTimeout.TotalSeconds:F0}s.",
                RecoverySuggestion: "The dotnet CLI is present but hung. Check for slow network shares, hung MSBuild processes, or profile-init issues. Retry after a reboot if needed.");
        }

        if (result.ExitCode != 0)
        {
            return new PrerequisiteCheckResult(
                Passed: false,
                Diagnostic: $"'dotnet --list-sdks' failed (exit {result.ExitCode}).",
                RecoverySuggestion: "Verify .NET SDK 10.0.302 or newer is installed: run 'dotnet --version'. Install from https://dotnet.microsoft.com/download/dotnet/10.0 if missing.");
        }

        string sdkListing = result.StdOut.Trim();
        MatchCollection matches = SdkVersionPattern().Matches(result.StdOut);
        if (matches.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(sdkListing))
            {
                return new PrerequisiteCheckResult(
                    Passed: false,
                    Diagnostic: "No .NET SDKs were reported by 'dotnet --list-sdks'.",
                    RecoverySuggestion: "Install .NET SDK 10.0.302 or newer from https://dotnet.microsoft.com/download/dotnet/10.0, then retry.");
            }

            // Parse-fail advisory per Risk #4 — unusual locales or formats. Pass with advisory rather
            // than fail hard.
            return new PrerequisiteCheckResult(
                Passed: true,
                Diagnostic: "Unable to parse 'dotnet --list-sdks' output; skipping version check.",
                RecoverySuggestion: null,
                IsSkipped: true);
        }

        Version? highestCompatibleVersion = null;
        Version? highestParsedVersion = null;
        string highestVersion = string.Empty;
        int olderCount = 0;
        int parsedVersionCount = 0;
        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups[1].ValueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out int major)
                || !int.TryParse(match.Groups[2].ValueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minor)
                || !int.TryParse(match.Groups[3].ValueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out int patch))
            {
                continue;
            }

            parsedVersionCount++;
            var version = new Version(major, minor, patch);
            if (highestParsedVersion is null || version.CompareTo(highestParsedVersion) > 0)
            {
                highestParsedVersion = version;
            }

            if (version.CompareTo(MinimumDotnetSdkVersion) >= 0
                && (highestCompatibleVersion is null || version.CompareTo(highestCompatibleVersion) > 0))
            {
                highestCompatibleVersion = version;
                highestVersion = match.Value;
            }
            else if (version.CompareTo(MinimumDotnetSdkVersion) < 0)
            {
                olderCount++;
            }
        }

        if (parsedVersionCount == 0)
        {
            return new PrerequisiteCheckResult(
                Passed: true,
                Diagnostic: "Unable to parse 'dotnet --list-sdks' output; skipping version check.",
                RecoverySuggestion: null,
                IsSkipped: true);
        }

        if (highestCompatibleVersion is null)
        {
            return new PrerequisiteCheckResult(
                Passed: false,
                Diagnostic: $"No .NET SDK {MinimumDotnetSdkVersion} or newer found. Highest installed SDK: {highestParsedVersion}.",
                RecoverySuggestion: "Install .NET SDK 10.0.302 or newer from https://dotnet.microsoft.com/download/dotnet/10.0, then retry.");
        }

        string suffix = olderCount == 0
            ? string.Empty
            : $" (and {olderCount} older).";
        return new PrerequisiteCheckResult(
            Passed: true,
            Diagnostic: $".NET SDK {highestVersion}{suffix}",
            RecoverySuggestion: null);
    }

    /// <summary>Attempts to bind each port on the loopback interface to verify availability.</summary>
    /// <param name="ports">The ports to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The sub-check result. Fails fast on the first in-use port.</returns>
    public Task<PrerequisiteCheckResult> CheckPortAvailabilityAsync(IReadOnlyCollection<int> ports, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ports);
        ct.ThrowIfCancellationRequested();

        foreach (int port in ports)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
            }
            catch (SocketException)
            {
                return Task.FromResult(new PrerequisiteCheckResult(
                    Passed: false,
                    Diagnostic: $"Port {port} in use.",
                    RecoverySuggestion: $"Port {port} appears in use. Find the owner: 'lsof -i :{port}' (macOS/Linux) or 'netstat -ano | findstr :{port}' (Windows). Stop that process or reconfigure the conflicting service."));
            }
        }

        return Task.FromResult(new PrerequisiteCheckResult(
            Passed: true,
            Diagnostic: $"Ports {string.Join(", ", ports)} available.",
            RecoverySuggestion: null));
    }

    /// <summary>Detects the current OS platform. Purely informational — always passes.</summary>
    /// <returns>The sub-check result (always <see cref="PrerequisiteCheckResult.Passed"/> = <see langword="true"/>).</returns>
    public PrerequisiteCheckResult CheckOsPlatform()
    {
        string platform = DetectPlatformName();
        return new PrerequisiteCheckResult(
            Passed: true,
            Diagnostic: $"OS detected: {platform} ({RuntimeInformation.OSDescription}).",
            RecoverySuggestion: null);
    }

    /// <summary>Checks for an installed DAPR CLI. Soft-fail — missing DAPR is acceptable.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The sub-check result (always <see cref="PrerequisiteCheckResult.Passed"/> = <see langword="true"/>).</returns>
    public async Task<PrerequisiteCheckResult> CheckDaprCliAsync(CancellationToken ct)
    {
        ProcessResult result = await _processRunner.RunAsync("dapr", "--version", DaprTimeout, ct).ConfigureAwait(false);

        if (result.NotFound || result.TimedOut || result.ExitCode != 0)
        {
            return new PrerequisiteCheckResult(
                Passed: true,
                Diagnostic: "DAPR CLI not installed (optional for local dev; Aspire manages the sidecar).",
                RecoverySuggestion: null,
                IsSkipped: true);
        }

        string parsedVersion = ExtractFirstVersionToken(result.StdOut);
        string versionDisplay = string.IsNullOrWhiteSpace(parsedVersion) ? "detected" : parsedVersion;
        return new PrerequisiteCheckResult(
            Passed: true,
            Diagnostic: $"DAPR CLI {versionDisplay} (optional).",
            RecoverySuggestion: null);
    }

    private static string DetectPlatformName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Windows";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "Linux";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "macOS";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            return "FreeBSD";
        }

        return RuntimeInformation.OSDescription;
    }

    private static string ExtractFirstVersionToken(string stdOut)
    {
        Match match = SdkVersionPattern().Match(stdOut);
        return match.Success ? match.Value : string.Empty;
    }

    [GeneratedRegex(@"^(\d+)\.(\d+)\.(\d+)", RegexOptions.Multiline)]
    private static partial Regex SdkVersionPattern();
}
