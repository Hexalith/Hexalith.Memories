// <copyright file="RuntimeSecretStoreBootstrapTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Deployment;

using System.Diagnostics;

using Hexalith.Memories.AppHost;

using Shouldly;

[Trait("Category", "Integration")]
public sealed class RuntimeSecretStoreBootstrapTests
{
    [Fact]
    public async Task MissingBootstrap_PreventsRuntimeStoreLoadAndSuccessfulRead()
    {
        var start = new ProcessStartInfo("python3")
        {
            WorkingDirectory = RepositoryRootLocator.Resolve(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add("tests/tooling/runtime_secret_store/fail_closed.py");
        start.ArgumentList.Add("deploy/kubernetes/base/dapr/secretstore.yaml");
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the bootstrap probe.");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cts.Token);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            throw new ShouldAssertException("The fail_closed.py bootstrap probe exceeded the 60-second timeout.");
        }

        string result = await stdoutTask;
        string error = await stderrTask;
        process.ExitCode.ShouldBe(
            0,
            $"The disposable Vault component fail-closed probe failed. Stdout: {result.Trim()} | Stderr: {error.Trim()}");
        result.Contains("component failed, no successful read or loaded fallback", StringComparison.Ordinal).ShouldBeTrue();
        result.Contains("Unreachable OpenBao with synthetic bootstrap: read (HTTP 500) failed, no successful read or fallback", StringComparison.Ordinal).ShouldBeTrue();
    }
}
