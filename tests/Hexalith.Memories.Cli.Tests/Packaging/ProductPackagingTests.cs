// <copyright file="ProductPackagingTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Packaging;

using System.Reflection;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>Checks product dependency boundaries and immutable container content inputs.</summary>
public sealed partial class ProductPackagingTests
{
    private const string ReviewedContainerBaseImage = "mcr.microsoft.com/dotnet/aspnet:11.0-alpine@sha256:d39b72ad1534fa0faf52b488bffb0762df7ba269d120f0338713d4dda2a41bf5";

    [Fact]
    public void BackendProjects_ShouldNotUseRedisCompatibilityPackageAsDependencyFacade()
    {
        string repoRoot = GetRepoRoot();
        string[] projectPaths =
        [
            "src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj",
            "tests/Hexalith.Memories.Benchmarks/Hexalith.Memories.Benchmarks.csproj",
        ];

        foreach (string projectPath in projectPaths)
        {
            string project = File.ReadAllText(Path.Combine(repoRoot, projectPath));
            project.ShouldNotContain(
                "Hexalith.Memories.Redis.csproj",
                Case.Sensitive);
        }
    }

    [Fact]
    public void ContainerBaseImage_IsDigestPinnedAndInheritedByEveryProject()
    {
        string repoRoot = GetRepoRoot();
        MatchCollection declarations = ContainerBaseImageRegex().Matches(File.ReadAllText(Path.Combine(repoRoot, "Directory.Build.targets")));

        declarations.Count.ShouldBe(1, "Directory.Build.targets must declare the container base image exactly once.");
        string image = declarations[0].Groups["image"].Value;
        image.ShouldMatch(
            @"^mcr\.microsoft\.com/dotnet/aspnet:[0-9][^@\s]*@sha256:[0-9a-f]{64}$",
            "the base image must name its tag and the reviewed index digest; a tag alone floats.");
        image.ShouldBe(ReviewedContainerBaseImage, "a refresh must move the reviewed index (manifest list) digest, never a platform child's.");
        string[] overrides =
        [
            .. Directory.EnumerateFiles(Path.Combine(repoRoot, "src"), "*", SearchOption.AllDirectories)
                .Append(Path.Combine(repoRoot, "Directory.Build.props"))
                .Where(static path => path.EndsWith(".csproj", StringComparison.Ordinal)
                    || path.EndsWith(".props", StringComparison.Ordinal)
                    || path.EndsWith(".targets", StringComparison.Ordinal))
                .Where(static path => File.ReadAllText(path).Contains("<ContainerBaseImage", StringComparison.Ordinal)),
        ];
        overrides.ShouldBeEmpty("container projects must inherit the digest-pinned base image.");
    }

    [GeneratedRegex(@"<ContainerBaseImage(?:\s[^>]*)?>(?<image>[^<]*)</ContainerBaseImage>")]
    private static partial Regex ContainerBaseImageRegex();

    private static string GetRepoRoot()
    {
        string? root = typeof(ProductPackagingTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(static attribute => attribute.Key == "RepoRoot")?.Value;
        root.ShouldNotBeNullOrWhiteSpace("The test project must declare its RepoRoot assembly metadata.");
        return Path.GetFullPath(root);
    }
}
