// <copyright file="RepositoryProjectPaths.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Aspire;

/// <summary>
/// Resolves on-disk paths to project files within a Hexalith mono-repo working tree, where the platform
/// modules (<c>Hexalith.Memories</c>, <c>Hexalith.EventStore</c>, …) are either the current repository or checked
/// out as direct Git submodules under the consuming repository root's <c>references/</c> directory.
/// </summary>
/// <remarks>
/// <para>
/// A consuming domain module hosts the Memories server by referencing the cross-repo
/// <c>Hexalith.Memories.Server</c> project file (added with
/// <see cref="Aspire.Hosting.ApplicationModel.IProjectMetadata.SuppressBuild"/> set to <see langword="true"/>)
/// instead of building it, so it must resolve that path relative to the AppHost's output directory.
/// </para>
/// <para>
/// The repository root is computed from <see cref="AppContext.BaseDirectory"/> assuming the standard
/// <c>&lt;repo-root&gt;/src/&lt;Module&gt;.AppHost/bin/&lt;config&gt;/&lt;tfm&gt;/</c> layout (five levels up).
/// All Hexalith domain-module AppHosts follow this layout, which makes the resolution identical across modules.
/// This mirrors the equivalent helper in <c>Hexalith.EventStore.Aspire</c>; it is duplicated here on purpose so
/// that <c>Hexalith.Memories.Aspire</c> takes no dependency on the EventStore platform package.
/// </para>
/// </remarks>
internal static class RepositoryProjectPaths
{
    /// <summary>
    /// Resolves the on-disk path to a project file inside a Hexalith platform module the consuming repository
    /// depends on, with the same rule as the workspace source mapping. When <c>&lt;root&gt;/&lt;module&gt;.slnx</c>
    /// exists, the dependency is the consuming repository itself and the project resolves under <c>&lt;root&gt;</c>.
    /// Otherwise it resolves under the direct submodule <c>&lt;root&gt;/references/&lt;module&gt;</c>. No sibling,
    /// ancestor or root-level checkout is ever used, so the launched project path matches the build-time
    /// <c>&lt;ProjectReference&gt;</c> the AppHost uses to force a Debug build of the same project.
    /// Mirrors the equivalent helper in <c>Hexalith.EventStore.Aspire</c> (duplicated on purpose to avoid a
    /// dependency on the EventStore platform package).
    /// </summary>
    /// <param name="moduleDirectory">The dependency module's directory name (e.g. <c>Hexalith.Memories</c>).</param>
    /// <param name="moduleRelativePath">Path segments inside the module, ending in the target project file.</param>
    /// <returns>The absolute path to the resolved project file.</returns>
    /// <exception cref="InvalidOperationException">
    /// The project file does not exist at the mapped location. The message names the dependency and the expected path.
    /// </exception>
    public static string GetReferencedModuleProjectPath(string moduleDirectory, params string[] moduleRelativePath)
        => ResolveReferencedModuleProjectPath(GetRepositoryRoot(), moduleDirectory, moduleRelativePath);

    /// <summary>
    /// Applies the <see cref="GetReferencedModuleProjectPath"/> rule to an explicit consuming repository root.
    /// </summary>
    /// <param name="root">The consuming repository root.</param>
    /// <param name="moduleDirectory">The dependency module's directory name (e.g. <c>Hexalith.Memories</c>).</param>
    /// <param name="moduleRelativePath">Path segments inside the module, ending in the target project file.</param>
    /// <returns>The absolute path to the resolved project file.</returns>
    /// <exception cref="InvalidOperationException">
    /// The project file does not exist at the mapped location. The message names the dependency and the expected path.
    /// </exception>
    public static string ResolveReferencedModuleProjectPath(string root, string moduleDirectory, params string[] moduleRelativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleDirectory);
        if (moduleRelativePath is null || moduleRelativePath.Length == 0)
        {
            throw new ArgumentException("At least one module-relative path segment is required.", nameof(moduleRelativePath));
        }

        string fullRoot = Path.GetFullPath(root);
        string relative = Path.Combine(moduleRelativePath);
        bool isCurrentRepository = File.Exists(Path.Combine(fullRoot, moduleDirectory + ".slnx"));
        string expected = isCurrentRepository
            ? Path.GetFullPath(Path.Combine(fullRoot, relative))
            : Path.GetFullPath(Path.Combine(fullRoot, "references", moduleDirectory, relative));

        return File.Exists(expected)
            ? expected
            : throw new InvalidOperationException(isCurrentRepository
                ? $"Dependency '{moduleDirectory}' is the repository at '{fullRoot}', but its project '{expected}' does not exist."
                : $"Dependency '{moduleDirectory}' must be checked out as the direct submodule 'references/{moduleDirectory}' of '{fullRoot}', but its project '{expected}' does not exist. Sibling, ancestor and root-level checkouts are not used. Initialize the submodule with 'git submodule update --init -- references/{moduleDirectory}'.");
    }

    /// <summary>
    /// Resolves the consuming repository root from the AppHost output directory, assuming the standard
    /// <c>&lt;repo-root&gt;/src/&lt;Module&gt;.AppHost/bin/&lt;config&gt;/&lt;tfm&gt;/</c> layout.
    /// </summary>
    /// <returns>The absolute path to the repository root.</returns>
    public static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
