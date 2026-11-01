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
/// rule: the consuming repository itself, an available direct reference, or a sibling declared alongside the
/// consumer in the immediate workspace's <c>.gitmodules</c>. Other layouts throw with the expected locations.
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
    [InlineData(false)]
    [InlineData(true)]
    public void ResolveReferencedModuleProjectPath_WhenWorkspaceDeclaresConsumerAndSibling_ReturnsSiblingProject(bool quotedPaths)
    {
        using TemporaryLayout layout = new(workspace: true);
        DeclareWorkspace(layout, quotedPaths, "consumer", Module);
        string project = layout.CreateFile(["..", Module, .. _serverProject]);

        string path = RepositoryProjectPaths.ResolveReferencedModuleProjectPath(layout.Root, Module, _serverProject);

        path.ShouldBe(project);
    }

    [Fact]
    public void ResolveReferencedModuleProjectPath_WhenDirectReferenceAndDeclaredSiblingExist_PrefersDirectReference()
    {
        using TemporaryLayout layout = new(workspace: true);
        DeclareWorkspace(layout, false, "consumer", Module);
        string project = layout.CreateFile(["references", Module, .. _serverProject]);
        _ = layout.CreateFile(["..", Module, .. _serverProject]);

        string path = RepositoryProjectPaths.ResolveReferencedModuleProjectPath(layout.Root, Module, _serverProject);

        path.ShouldBe(project);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void ResolveReferencedModuleProjectPath_WhenConsumerOrSiblingIsUndeclared_Throws(bool declaresConsumer, bool declaresSibling)
    {
        using TemporaryLayout layout = new(workspace: true);
        DeclareWorkspace(layout, false, declaresConsumer ? "consumer" : "other-consumer", declaresSibling ? Module : Module + ".Other");
        _ = layout.CreateFile(["..", Module, .. _serverProject]);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => RepositoryProjectPaths.ResolveReferencedModuleProjectPath(layout.Root, Module, _serverProject));

        exception.Message.ShouldContain($"'{Module}'");
        exception.Message.ShouldContain(layout.PathOf(["references", Module, .. _serverProject]));
        exception.Message.ShouldNotContain("git submodule");
    }

    [Fact]
    public void ResolveReferencedModuleProjectPath_WhenDeclaredSiblingProjectIsMissing_ThrowsNamingBothExpectedLocations()
    {
        using TemporaryLayout layout = new(workspace: true);
        DeclareWorkspace(layout, false, "consumer", Module);
        _ = layout.CreateFile("..", Module, Module + ".slnx");

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => RepositoryProjectPaths.ResolveReferencedModuleProjectPath(layout.Root, Module, _serverProject));

        exception.Message.ShouldContain(layout.PathOf(["references", Module, .. _serverProject]));
        exception.Message.ShouldContain(layout.PathOf(["..", Module, .. _serverProject]));
        exception.Message.ShouldNotContain("git submodule");
    }

    [Fact]
    public void ResolveReferencedModuleProjectPath_WhenOwnProjectIsMissingWithDeclaredSibling_ThrowsInsteadOfSelectingSibling()
    {
        using TemporaryLayout layout = new(workspace: true);
        DeclareWorkspace(layout, false, "consumer", Module);
        _ = layout.CreateFile(Module + ".slnx");
        _ = layout.CreateFile(["references", Module, .. _serverProject]);
        _ = layout.CreateFile(["..", Module, .. _serverProject]);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => RepositoryProjectPaths.ResolveReferencedModuleProjectPath(layout.Root, Module, _serverProject));

        exception.Message.ShouldContain(layout.PathOf(_serverProject));
    }

    [Fact]
    public void ResolveReferencedModuleProjectPath_WhenWorkspaceFileIsMissing_ThrowsInsteadOfSelectingSibling()
    {
        using TemporaryLayout layout = new(workspace: true);
        _ = layout.CreateFile(["..", Module, .. _serverProject]);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => RepositoryProjectPaths.ResolveReferencedModuleProjectPath(layout.Root, Module, _serverProject));

        exception.Message.ShouldContain(layout.PathOf(["references", Module, .. _serverProject]));
    }

    [Theory]
    [InlineData("other-section")]
    [InlineData("commented-path")]
    [InlineData("wrong-path")]
    [InlineData("overridden-path")]
    [InlineData("empty-unquoted-path")]
    [InlineData("empty-quoted-path")]
    public void ResolveReferencedModuleProjectPath_WhenWorkspaceDoesNotDeclareExactSubmodulePaths_Throws(string declaration)
    {
        using TemporaryLayout layout = new(workspace: true);
        DeclareWorkspace(layout, false, "consumer");
        string gitmodules = layout.PathOf("..", "..", ".gitmodules");
        string invalidDeclaration = declaration switch
        {
            "other-section" => $"[other \"dependency\"]\npath = references/{Module}\n",
            "commented-path" => $"[submodule \"dependency\"]\n# path = references/{Module}\n",
            "wrong-path" => $"[submodule \"dependency\"]\npath = other/{Module}\n",
            "overridden-path" => $"[submodule \"dependency\"]\npath = references/{Module}\npath = other/{Module}\n",
            "empty-unquoted-path" => $"[submodule \"dependency\"]\npath = references/{Module}\npath =\n",
            "empty-quoted-path" => $"[submodule \"dependency\"]\npath = references/{Module}\npath = \"\"\n",
            _ => throw new ArgumentOutOfRangeException(nameof(declaration), declaration, null),
        };
        File.AppendAllText(gitmodules, invalidDeclaration);
        _ = layout.CreateFile(["..", Module, .. _serverProject]);

        _ = Should.Throw<InvalidOperationException>(
            () => RepositoryProjectPaths.ResolveReferencedModuleProjectPath(layout.Root, Module, _serverProject));
    }

    [Fact]
    public void ResolveReferencedModuleProjectPath_WhenConsumerIsNotUnderReferences_ThrowsEvenWithDeclarations()
    {
        using TemporaryLayout layout = new();
        DeclareWorkspace(layout, false, "consumer", Module);
        _ = layout.CreateFile(["..", Module, .. _serverProject]);

        _ = Should.Throw<InvalidOperationException>(
            () => RepositoryProjectPaths.ResolveReferencedModuleProjectPath(layout.Root, Module, _serverProject));
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

    private static void DeclareWorkspace(TemporaryLayout layout, bool quotedPaths, params string[] repositories)
    {
        string gitmodules = layout.CreateFile("..", "..", ".gitmodules");
        File.WriteAllText(gitmodules, string.Join(Environment.NewLine, repositories.Select(
            repository => quotedPaths
                ? $"[submodule \"{repository}\"] # workspace dependency\n\tpath = \"references/{repository}\" ; declared path\n"
                : $"[submodule \"{repository}\"]\n\tpath = references/{repository}\n")));
    }

    /// <summary>
    /// A disposable tree with optional workspace <c>references/</c> layout, allowing files in the root and in each
    /// of its parent and grandparent directories without leaving the temporary base.
    /// </summary>
    private sealed class TemporaryLayout : IDisposable
    {
        private readonly string _base = Path.Combine(Path.GetTempPath(), "hexalith-memories-repository-paths-" + Guid.NewGuid().ToString("N"));

        public TemporaryLayout(bool workspace = false)
        {
            Root = Path.Combine(_base, "a", workspace ? "references" : "b", "consumer");
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
