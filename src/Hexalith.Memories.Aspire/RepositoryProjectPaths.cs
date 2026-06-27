// <copyright file="RepositoryProjectPaths.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Aspire;

/// <summary>
/// Resolves on-disk paths to project files within a Hexalith mono-repo working tree, where the platform
/// modules (<c>Hexalith.Memories</c>, <c>Hexalith.EventStore</c>, …) are checked out as Git submodules under
/// the consuming repository root's <c>references/</c> directory.
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
    /// Builds an absolute path to a file located under the consuming repository root.
    /// </summary>
    /// <param name="path">Path segments, relative to the repository root, ending in the target file.</param>
    /// <returns>The combined path rooted at the repository root.</returns>
    public static string GetProjectPath(params string[] path)
        => Path.Combine(GetRepositoryRoot(), Path.Combine(path));

    /// <summary>
    /// Resolves the consuming repository root from the AppHost output directory, assuming the standard
    /// <c>&lt;repo-root&gt;/src/&lt;Module&gt;.AppHost/bin/&lt;config&gt;/&lt;tfm&gt;/</c> layout.
    /// </summary>
    /// <returns>The absolute path to the repository root.</returns>
    public static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
