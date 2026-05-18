// <copyright file="AppHostComponentFileOrderingTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.IO;

using Shouldly;

/// <summary>Story 15.6 source-level guard for AppHost component-file rewrite ordering.</summary>
public sealed class AppHostComponentFileOrderingTests
{
    [Fact]
    public void AppHostSidecarStart_AwaitsRedisComponentRewriteBeforeRedisPing()
    {
        string content = File.ReadAllText(LocateRepoFile(Path.Combine(
            "src",
            "Hexalith.Memories.AppHost",
            "Program.cs")));

        int signalIndex = content.IndexOf("redisComponentRewrite.TrySetResult", StringComparison.Ordinal);
        int awaitRewriteIndex = content.IndexOf("WaitForRedisComponentRewriteAsync", StringComparison.Ordinal);
        int pingIndex = content.IndexOf("WaitForRedisPingAsync", StringComparison.Ordinal);

        signalIndex.ShouldBeGreaterThan(0);
        awaitRewriteIndex.ShouldBeGreaterThan(0);
        pingIndex.ShouldBeGreaterThan(awaitRewriteIndex);
        content.ShouldContain("TaskCompletionSource", Case.Sensitive);
        content.ShouldContain("Process.GetCurrentProcess().Id", Case.Sensitive);
        content.ShouldNotContain(".WithEndpoint(port: 6379", Case.Sensitive);
        content.ShouldNotContain(".WithEndpoint(port: 6380", Case.Sensitive);
    }

    private static string LocateRepoFile(string relativePath)
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, relativePath);
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate '{relativePath}' by walking up from '{AppContext.BaseDirectory}'.");
    }
}
