// <copyright file="RepositoryRootLocator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

using System.Diagnostics.CodeAnalysis;

/// <summary>Locates the Hexalith.Memories repository root for AppHost-owned local infrastructure.</summary>
public static class RepositoryRootLocator
{
    private const string _markerFileName = "Hexalith.Memories.slnx";

    /// <summary>Resolves the repository root by walking up from the current directory and AppContext base directory.</summary>
    /// <param name="currentDirectory">Optional current directory override, used by tests.</param>
    /// <param name="baseDirectory">Optional base directory override, used by tests.</param>
    /// <returns>The full path to the repository root.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the repository marker cannot be found.</exception>
    public static string Resolve(string? currentDirectory = null, string? baseDirectory = null)
    {
        string resolvedCurrentDirectory = Path.GetFullPath(currentDirectory ?? Directory.GetCurrentDirectory());
        if (TryFindRoot(resolvedCurrentDirectory, out string? root))
        {
            return root;
        }

        string resolvedBaseDirectory = Path.GetFullPath(baseDirectory ?? AppContext.BaseDirectory);
        if (!string.Equals(resolvedBaseDirectory, resolvedCurrentDirectory, StringComparison.OrdinalIgnoreCase) &&
            TryFindRoot(resolvedBaseDirectory, out root))
        {
            return root;
        }

        throw new InvalidOperationException(
            $"Could not locate '{_markerFileName}' from CWD '{resolvedCurrentDirectory}' or base directory '{resolvedBaseDirectory}'. " +
            "Run AppHost or integration tests from the repository root, or set the test working directory accordingly.");
    }

    private static bool TryFindRoot(string startDirectory, [NotNullWhen(true)] out string? root)
    {
        string candidate = Path.GetFullPath(startDirectory);
        while (!string.IsNullOrWhiteSpace(candidate))
        {
            if (File.Exists(Path.Combine(candidate, _markerFileName)))
            {
                root = candidate;
                return true;
            }

            DirectoryInfo? parent = Directory.GetParent(candidate);
            if (parent is null)
            {
                break;
            }

            candidate = parent.FullName;
        }

        root = null;
        return false;
    }
}
