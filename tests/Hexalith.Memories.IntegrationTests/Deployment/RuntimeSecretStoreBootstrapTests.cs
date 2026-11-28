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
        };
        start.ArgumentList.Add("tests/tooling/runtime_secret_store/fail_closed.py");
        start.ArgumentList.Add("deploy/kubernetes/base/dapr/secretstore.yaml");
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the bootstrap probe.");
        string result = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        process.ExitCode.ShouldBe(
            0,
            $"The disposable Vault component must fail to load when its bootstrap is absent. Probe output: {result.Trim()}");
        result.Contains("component failed, no successful read or loaded fallback", StringComparison.Ordinal).ShouldBeTrue();
    }
}
