// <copyright file="TestDataRootTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using Hexalith.Memories.AppHost;

using Shouldly;

/// <summary>Executable tests for the AppHost test-data allow-list root.</summary>
[Trait("Category", "Integration")]
public sealed class TestDataRootTests
{
    [Fact]
    public void Ensure_NewRepository_CreatesDirectoryAndReadme()
    {
        string root = CreateRepositoryRoot();
        try
        {
            string testData = TestDataRoot.Ensure(root);

            testData.ShouldBe(Path.Combine(root, "test-data"));
            File.ReadAllText(Path.Combine(testData, "README.md")).ShouldBe(TestDataRoot.ReadmeContent);
            Directory.GetFiles(testData).Select(Path.GetFileName).ShouldBe(["README.md"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Ensure_ExistingReadme_IsPreserved()
    {
        string root = CreateRepositoryRoot();
        try
        {
            string readme = Path.Combine(root, "test-data", "README.md");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(readme)!);
            File.WriteAllText(readme, "developer notes");

            _ = TestDataRoot.Ensure(root);

            File.ReadAllText(readme).ShouldBe("developer notes");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Ensure_ConcurrentFirstRuns_InstallOneCompleteReadme()
    {
        const int callers = 32;
        string root = CreateRepositoryRoot();
        try
        {
            using Barrier start = new(callers);
            Task<string>[] runs = [.. Enumerable.Range(0, callers).Select(_ => Task.Factory.StartNew(
                () =>
                {
                    start.SignalAndWait(TimeSpan.FromSeconds(30)).ShouldBeTrue();
                    return TestDataRoot.Ensure(root);
                },
                TestContext.Current.CancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default))];

            string[] results = await Task.WhenAll(runs);

            results.Distinct(StringComparer.Ordinal).ShouldHaveSingleItem().ShouldBe(Path.Combine(root, "test-data"));
            File.ReadAllText(Path.Combine(root, "test-data", "README.md")).ShouldBe(TestDataRoot.ReadmeContent);
            Directory.GetFiles(Path.Combine(root, "test-data")).Select(Path.GetFileName).ShouldBe(["README.md"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateRepositoryRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), $"memories-test-data-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(root);
        return root;
    }
}
