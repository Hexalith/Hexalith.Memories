// <copyright file="DaprRedisComponentFilesTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using Hexalith.Memories.AppHost;

using Shouldly;

/// <summary>Executable tests for the AppHost Redis-backed Dapr component files.</summary>
[Trait("Category", "Integration")]
public sealed class DaprRedisComponentFilesTests
{
    private const string AppId = "memories-files-test";

    [Fact]
    public void Write_CreatesEachComponentWithTheEndpointAndScopes()
    {
        string directory = CreateOwnedDirectory();
        try
        {
            ComponentPaths paths = new(directory);

            paths.Write("127.0.0.1:16379");

            string stateStore = File.ReadAllText(paths.StateStore);
            stateStore.ShouldContain("name: statestore");
            stateStore.ShouldContain("value: \"127.0.0.1:16379\"");
            stateStore.ShouldContain($"- {AppId}");
            File.ReadAllText(paths.PubSub).ShouldContain("name: pubsub");
            File.ReadAllText(paths.AccessTelemetryStore).ShouldContain("name: access-telemetry-store");
            File.ReadAllText(paths.AccessTelemetryConfig).ShouldContain("name: access-telemetry-config");
            foreach (string path in paths.All)
            {
                File.ReadAllText(path).ShouldContain("127.0.0.1:16379");
            }

            Directory.GetFiles(directory).Order(StringComparer.Ordinal)
                .ShouldBe(paths.All.Order(StringComparer.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Write_RewriteReplacesFilesInsteadOfTruncatingThemInPlace()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Replacing a file that a reader holds open needs POSIX rename semantics.");
        string directory = CreateOwnedDirectory();
        try
        {
            ComponentPaths paths = new(directory);
            paths.Write("127.0.0.1:6379");
            string initial = File.ReadAllText(paths.StateStore);
            using FileStream openReader = new(
                paths.StateStore,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            paths.Write("127.0.0.1:16379");

            // A truncate-then-write rewrite would expose the new or a partial document through the open handle.
            using StreamReader reader = new(openReader);
            reader.ReadToEnd().ShouldBe(initial);
            File.ReadAllText(paths.StateStore).ShouldContain("127.0.0.1:16379");
            Directory.GetFiles(directory).Length.ShouldBe(paths.All.Length);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Write_RestrictsComponentFilesToTheOwner()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix file modes do not apply on Windows.");
        string directory = CreateOwnedDirectory();
        try
        {
            ComponentPaths paths = new(directory);

            paths.Write("127.0.0.1:16379");

            foreach (string path in paths.All)
            {
                if (!OperatingSystem.IsWindows())
                {
                    File.GetUnixFileMode(path).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateOwnedDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"memories-dapr-components-{Guid.NewGuid():N}");
        OpenBaoProtectedFileSystem.CreateDirectory(directory);
        return directory;
    }

    private sealed class ComponentPaths(string directory)
    {
        public string StateStore { get; } = Path.Combine(directory, "statestore.yaml");

        public string PubSub { get; } = Path.Combine(directory, "pubsub.yaml");

        public string AccessTelemetryStore { get; } = Path.Combine(directory, "access-telemetry-store.yaml");

        public string AccessTelemetryConfig { get; } = Path.Combine(directory, "access-telemetry-config.yaml");

        public string[] All => [StateStore, PubSub, AccessTelemetryStore, AccessTelemetryConfig];

        public void Write(string redisHost)
            => DaprRedisComponentFiles.Write(StateStore, PubSub, AccessTelemetryStore, AccessTelemetryConfig, redisHost, AppId);
    }
}
