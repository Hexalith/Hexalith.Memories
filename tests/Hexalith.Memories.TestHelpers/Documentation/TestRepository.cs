// <copyright file="TestRepository.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.TestHelpers.Documentation;

/// <summary>Finds the repository files used by source and document tests.</summary>
public static class TestRepository
{
    private const string _solutionName = "Hexalith.Memories.slnx";

    /// <summary>Gets the repository root from the test assembly or the working directory.</summary>
    public static string Root => FindRoot(AppContext.BaseDirectory) ?? FindRoot(Directory.GetCurrentDirectory())
        ?? throw new DirectoryNotFoundException($"Could not find '{_solutionName}' from the test assembly or working directory.");

    private static string? FindRoot(string startDirectory)
    {
        for (DirectoryInfo? directory = new(startDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, _solutionName)))
            {
                return directory.FullName;
            }
        }

        return null;
    }
}
