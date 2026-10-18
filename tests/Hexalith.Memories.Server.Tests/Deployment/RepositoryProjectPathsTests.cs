// <copyright file="RepositoryProjectPathsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.IO;

using Hexalith.Memories.Aspire;

using Shouldly;

/// <summary>
/// Platform Story 1.10 -- <c>RepositoryProjectPaths</c> resolves a Memories project with the workspace source-mapping
/// rule: the consuming repository itself when its root holds <c>Hexalith.Memories.slnx</c>, otherwise the direct
/// submodule <c>references/Hexalith.Memories</c>. Every other layout throws, naming the dependency and the expected path.
/// </summary>
public sealed class RepositoryProjectPathsTests
{
    private const string Module = "Hexalith.Memories";

    private static readonly string[] _serverProject = ["src", "Hexalith.Memories.Server", "Hexalith.Memories.Server.csproj"];

    [Fact]
    public void GetReferencedModuleProjectPath_InThisRepository_ResolvesItsOwnServerProject()
    {
        string root = RepositoryProjectPaths.GetRepositoryRoot();
        File.Exists(Path.Combine(root, Module + ".slnx")).ShouldBeTrue($"Expected the Memories repository root at {root}.");

        string path = RepositoryProjectPaths.GetReferencedModuleProjectPath(Module, _serverProject);

        path.ShouldBe(Path.Combine(root, "src", "Hexalith.Memories.Server", "Hexalith.Memories.Server.csproj"));
    }

    [Fact]
    public void ResolveReferencedModuleProjectPath_WhenRootIsTheDependency_ReturnsProjectUnderRootAndIgnoresReferencesCopy()
    {
        using TemporaryLayout layout = new();
        _ = layout.CreateFile(Module + ".slnx");
        string project = layout.CreateFile(_serverProject);
        _ = layout.CreateFile(["references", Module, .. _serverProject]);

        string path = RepositoryProjectPaths.ResolveReferencedModuleProjectPath(layout.Root, Module, _serverProject);

        path.ShouldBe(project);
    }

    [Fact]
    public void ResolveReferencedModuleProjectPath_WhenRootIsTheDependencyAndProjectIsMissing_ThrowsNamingDependencyAndExpectedPath()
    {
        using TemporaryLayout layout = new();
        _ = layout.CreateFile(Module + ".slnx");
        _ = layout.CreateFile(["references", Module, .. _serverProject]);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => RepositoryProjectPaths.ResolveReferencedModuleProjectPath(layout.Root, Module, _serverProject));

        exception.Message.ShouldContain($"'{Module}'");
        exception.Message.ShouldContain(layout.PathOf(_serverProject));
    }

    [Fact]
    public void ResolveReferencedModuleProjectPath_WhenDependencyIsMappedUnderReferences_ReturnsReferencesProject()
    {
        using TemporaryLayout layout = new();
        string project = layout.CreateFile(["references", Module, .. _serverProject]);

        string path = RepositoryProjectPaths.ResolveReferencedModuleProjectPath(layout.Root, Module, _serverProject);

        path.ShouldBe(project);
    }

    [Theory]
    [InlineData("sibling")]
    [InlineData("parent-references")]
    [InlineData("ancestor")]
    [InlineData("grand-ancestor")]
    [InlineData("root-level")]
    public void ResolveReferencedModuleProjectPath_WhenDependencyIsOnlyOutsideTheMapping_ThrowsNamingDependencyAndExpectedPath(string unmappedLayout)
    {
        using TemporaryLayout layout = new();
        string[] unmapped = unmappedLayout switch
        {
            "sibling" => ["..", Module, .. _serverProject],
            "parent-references" => ["..", "references", Module, .. _serverProject],
            "ancestor" => ["..", .. _serverProject],
            "grand-ancestor" => ["..", "..", .. _serverProject],
            "root-level" => [Module, .. _serverProject],
            _ => throw new ArgumentOutOfRangeException(nameof(unmappedLayout), unmappedLayout, null),
        };
        _ = layout.CreateFile(unmapped);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => RepositoryProjectPaths.ResolveReferencedModuleProjectPath(layout.Root, Module, _serverProject));

        exception.Message.ShouldContain($"'{Module}'");
        exception.Message.ShouldContain(layout.PathOf(["references", Module, .. _serverProject]));
    }

    /// <summary>
    /// A disposable <c>&lt;base&gt;/a/b/consumer</c> tree, so files can be placed in the consumer root and in each
    /// of its parent and grandparent directories without leaving the temporary base.
    /// </summary>
    private sealed class TemporaryLayout : IDisposable
    {
        private readonly string _base = Path.Combine(Path.GetTempPath(), "hexalith-memories-repository-paths-" + Guid.NewGuid().ToString("N"));

        public TemporaryLayout()
        {
            Root = Path.Combine(_base, "a", "b", "consumer");
            _ = Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string PathOf(params string[] segments) => Path.GetFullPath(Path.Combine([Root, .. segments]));

        public string CreateFile(params string[] segments)
        {
            string path = PathOf(segments);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Empty);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(_base))
            {
                Directory.Delete(_base, recursive: true);
            }
        }
    }
}
