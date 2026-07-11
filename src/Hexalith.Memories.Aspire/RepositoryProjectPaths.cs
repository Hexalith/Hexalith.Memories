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
    /// Resolves the on-disk path to a project file inside a sibling Hexalith platform module, probing every
    /// checkout layout in the same order as the <c>$(Hexalith*Root)</c> auto-detection in
    /// <c>Directory.Build.props</c>, so the launched project path matches the build-time
    /// <c>&lt;ProjectReference&gt;</c> the AppHost uses to force a Debug build of the same project. Returns the
    /// first candidate that exists; otherwise the standalone <c>&lt;root&gt;/references/&lt;module&gt;/…</c> path.
    /// Mirrors the equivalent helper in <c>Hexalith.EventStore.Aspire</c> (duplicated on purpose to avoid a
    /// dependency on the EventStore platform package).
    /// </summary>
    /// <param name="moduleDirectory">The dependency module's directory name (e.g. <c>Hexalith.Memories</c>).</param>
    /// <param name="moduleRelativePath">Path segments inside the module, ending in the target project file.</param>
    /// <returns>The absolute path to the resolved project file.</returns>
    public static string GetReferencedModuleProjectPath(string moduleDirectory, params string[] moduleRelativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleDirectory);
        if (moduleRelativePath is null || moduleRelativePath.Length == 0)
        {
            throw new ArgumentException("At least one module-relative path segment is required.", nameof(moduleRelativePath));
        }

        string root = GetRepositoryRoot();
        string relative = Path.Combine(moduleRelativePath);
        string standalone = Path.GetFullPath(Path.Combine(root, "references", moduleDirectory, relative));
        string[] candidates =
        [
            Path.GetFullPath(Path.Combine(root, "..", relative)),
            Path.GetFullPath(Path.Combine(root, "..", "..", relative)),
            standalone,
            Path.GetFullPath(Path.Combine(root, "..", moduleDirectory, relative)),
            Path.GetFullPath(Path.Combine(root, "..", "references", moduleDirectory, relative)),
        ];

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return standalone;
    }

    /// <summary>
    /// Resolves the consuming repository root from the AppHost output directory, assuming the standard
    /// <c>&lt;repo-root&gt;/src/&lt;Module&gt;.AppHost/bin/&lt;config&gt;/&lt;tfm&gt;/</c> layout.
    /// </summary>
    /// <returns>The absolute path to the repository root.</returns>
    public static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
