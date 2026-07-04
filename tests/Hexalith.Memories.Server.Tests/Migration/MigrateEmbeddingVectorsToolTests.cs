// <copyright file="MigrateEmbeddingVectorsToolTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Migration;

using System.Diagnostics;

using Shouldly;

public sealed class MigrateEmbeddingVectorsToolTests
{
    [Fact]
    public async Task Help_IncludesAbortAndBlueGreenWording()
    {
        ToolRunResult result = await RunToolAsync("--help");

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldContain("--abort");
        result.Output.ShouldContain("blue/green");
        result.Output.ShouldNotContain("drops and recreates active semantic indexes");
    }

    [Fact]
    public async Task MultipleModes_ReturnsParserErrorIncludingAbort()
    {
        ToolRunResult result = await RunToolAsync("--dry-run", "--abort");

        result.ExitCode.ShouldNotBe(0);
        result.Error.ShouldContain("Select exactly one mode");
        result.Error.ShouldContain("--abort");
    }

    private static async Task<ToolRunResult> RunToolAsync(params string[] args)
    {
        string repoRoot = ResolveRepoRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "MigrateEmbeddingVectors", "bin", "Debug", "net10.0", "MigrateEmbeddingVectors.dll");
        File.Exists(toolPath).ShouldBeTrue("Build the solution before running migration tool parser tests.");

        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            },
        };
        process.StartInfo.ArgumentList.Add("exec");
        process.StartInfo.ArgumentList.Add(toolPath);
        foreach (string arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        string error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);
        return new ToolRunResult(process.ExitCode, output, error);
    }

    private static string ResolveRepoRoot()
    {
        string candidate = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        return AppContext.BaseDirectory;
    }

    private sealed record ToolRunResult(int ExitCode, string Output, string Error);
}
