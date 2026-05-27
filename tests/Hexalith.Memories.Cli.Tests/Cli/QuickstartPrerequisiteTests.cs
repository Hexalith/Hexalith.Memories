// <copyright file="QuickstartPrerequisiteTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using Hexalith.Memories.Cli.Quickstart;

using Shouldly;

public sealed class QuickstartPrerequisiteTests
{
    [Fact]
    public async Task CheckDocker_Success_WhenExitCodeZero()
    {
        var runner = new FakeProcessRunner();
        runner.Register("docker", new ProcessResult(0, string.Empty, string.Empty, TimeSpan.FromMilliseconds(50)));
        var checks = new PrerequisiteChecks(runner);

        PrerequisiteCheckResult result = await checks.CheckDockerAsync(CancellationToken.None);

        result.Passed.ShouldBeTrue();
        result.Diagnostic.ShouldContain("Docker reachable");
        result.RecoverySuggestion.ShouldBeNull();
    }

    [Fact]
    public async Task CheckDocker_Fails_WhenExitCodeNonZero()
    {
        var runner = new FakeProcessRunner();
        runner.Register("docker", new ProcessResult(1, string.Empty, "error", TimeSpan.FromMilliseconds(50)));
        var checks = new PrerequisiteChecks(runner);

        PrerequisiteCheckResult result = await checks.CheckDockerAsync(CancellationToken.None);

        result.Passed.ShouldBeFalse();
        result.Diagnostic.ShouldContain("Docker daemon not reachable");
        result.RecoverySuggestion.ShouldNotBeNull();
        result.RecoverySuggestion.ShouldContain("Docker Desktop");
    }

    [Fact]
    public async Task CheckDocker_Fails_WhenNotFound()
    {
        var runner = new FakeProcessRunner();
        runner.Register("docker", new ProcessResult(-1, string.Empty, "not found", TimeSpan.Zero, NotFound: true));
        var checks = new PrerequisiteChecks(runner);

        PrerequisiteCheckResult result = await checks.CheckDockerAsync(CancellationToken.None);

        result.Passed.ShouldBeFalse();
        result.Diagnostic.ShouldContain("Docker command not found");
    }

    [Fact]
    public async Task CheckDocker_Fails_WhenTimedOut()
    {
        var runner = new FakeProcessRunner();
        runner.Register("docker", new ProcessResult(-1, string.Empty, string.Empty, TimeSpan.FromSeconds(5), TimedOut: true));
        var checks = new PrerequisiteChecks(runner);

        PrerequisiteCheckResult result = await checks.CheckDockerAsync(CancellationToken.None);

        result.Passed.ShouldBeFalse();
        result.Diagnostic.ShouldContain("timed out");
    }

    [Fact]
    public async Task CheckDotnetSdk_Passes_WhenDotnet10FeatureBandPresent()
    {
        var runner = new FakeProcessRunner();
        runner.Register("dotnet", new ProcessResult(0, "9.0.100\n10.0.203\n10.0.300\n", string.Empty, TimeSpan.FromMilliseconds(30)));
        var checks = new PrerequisiteChecks(runner);

        PrerequisiteCheckResult result = await checks.CheckDotnetSdkAsync(CancellationToken.None);

        result.Passed.ShouldBeTrue();
        result.Diagnostic.ShouldContain("10.0.300");
    }

    [Fact]
    public async Task CheckDotnetSdk_Fails_WhenOnlyOlderFeatureBands()
    {
        var runner = new FakeProcessRunner();
        runner.Register("dotnet", new ProcessResult(0, "9.0.100\n10.0.203\n", string.Empty, TimeSpan.Zero));
        var checks = new PrerequisiteChecks(runner);

        PrerequisiteCheckResult result = await checks.CheckDotnetSdkAsync(CancellationToken.None);

        result.Passed.ShouldBeFalse();
        result.Diagnostic.ShouldContain("No .NET SDK 10.0.300 or newer");
    }

    [Fact]
    public async Task CheckDotnetSdk_Fails_WhenDotnetMissing()
    {
        var runner = new FakeProcessRunner();
        runner.Register("dotnet", new ProcessResult(-1, string.Empty, "not found", TimeSpan.Zero, NotFound: true));
        var checks = new PrerequisiteChecks(runner);

        PrerequisiteCheckResult result = await checks.CheckDotnetSdkAsync(CancellationToken.None);

        result.Passed.ShouldBeFalse();
        result.Diagnostic.ShouldContain("not found");
    }

    [Fact]
    public async Task CheckDotnetSdk_AdvisoryPass_WhenParseFailsOnWeirdLocale()
    {
        var runner = new FakeProcessRunner();
        runner.Register("dotnet", new ProcessResult(0, "(no recognized output)\n", string.Empty, TimeSpan.Zero));
        var checks = new PrerequisiteChecks(runner);

        PrerequisiteCheckResult result = await checks.CheckDotnetSdkAsync(CancellationToken.None);

        result.Passed.ShouldBeTrue();
        result.Diagnostic.ShouldContain("skipping version check");
    }

    [Fact]
    public async Task CheckPortAvailability_AllPass_WhenPortsFree()
    {
        var runner = new FakeProcessRunner();
        var checks = new PrerequisiteChecks(runner);

        // Use random high ports that are almost certainly free; re-run bound to any loopback port.
        int[] probePorts = [GetFreePort(), GetFreePort(), GetFreePort()];
        PrerequisiteCheckResult result = await checks.CheckPortAvailabilityAsync(probePorts, CancellationToken.None);

        result.Passed.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckPortAvailability_Fails_WhenPortInUse()
    {
        var runner = new FakeProcessRunner();
        var checks = new PrerequisiteChecks(runner);

        int usedPort = GetFreePort();
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, usedPort);
        listener.Start();

        try
        {
            PrerequisiteCheckResult result = await checks.CheckPortAvailabilityAsync([usedPort], CancellationToken.None);

            result.Passed.ShouldBeFalse();
            result.Diagnostic.ShouldContain("in use");
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public void CheckOsPlatform_AlwaysPasses()
    {
        var runner = new FakeProcessRunner();
        var checks = new PrerequisiteChecks(runner);

        PrerequisiteCheckResult result = checks.CheckOsPlatform();

        result.Passed.ShouldBeTrue();
        result.Diagnostic.ShouldContain("OS detected");
    }

    [Fact]
    public async Task CheckDaprCli_SoftPass_WhenMissing()
    {
        var runner = new FakeProcessRunner();
        runner.Register("dapr", new ProcessResult(-1, string.Empty, string.Empty, TimeSpan.Zero, NotFound: true));
        var checks = new PrerequisiteChecks(runner);

        PrerequisiteCheckResult result = await checks.CheckDaprCliAsync(CancellationToken.None);

        result.Passed.ShouldBeTrue();
        result.Diagnostic.ShouldContain("not installed");
    }

    [Fact]
    public async Task CheckDaprCli_Pass_WhenPresent()
    {
        var runner = new FakeProcessRunner();
        runner.Register("dapr", new ProcessResult(0, "CLI version: 1.15.0\n", string.Empty, TimeSpan.FromMilliseconds(10)));
        var checks = new PrerequisiteChecks(runner);

        PrerequisiteCheckResult result = await checks.CheckDaprCliAsync(CancellationToken.None);

        result.Passed.ShouldBeTrue();
        result.Diagnostic.ShouldContain("DAPR CLI");
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
