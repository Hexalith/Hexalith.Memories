// <copyright file="TestDataRoot.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

using System.Text;

/// <summary>Creates the development-only allow-list root used by directory ingestion.</summary>
internal static class TestDataRoot
{
    /// <summary>The README written once into a new test-data directory.</summary>
    internal const string ReadmeContent =
        "# test-data\n\nDev-only allow-list root for POST /api/v1/ingest/directory. Safe to add sample files here; the endpoint is still disabled in production by default (appsettings.json AllowedDirectoryRoots=[]).\n";

    /// <summary>Ensures the test-data directory and its README exist below the repository root.</summary>
    /// <param name="repositoryRoot">The resolved repository root.</param>
    /// <returns>The full path of the test-data directory.</returns>
    /// <remarks>
    /// Several AppHosts can start together in one workspace. The README is staged in a unique temporary file
    /// and installed with a non-overwriting move, so concurrent first runs neither collide on an open file nor
    /// replace or truncate a README that another AppHost or a developer already created.
    /// </remarks>
    internal static string Ensure(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        string testData = Path.Combine(repositoryRoot, "test-data");
        _ = Directory.CreateDirectory(testData);
        string readme = Path.Combine(testData, "README.md");
        if (!File.Exists(readme))
        {
            InstallReadme(testData, readme);
        }

        return testData;
    }

    private static void InstallReadme(string testData, string readme)
    {
        string temporaryPath = Path.Combine(testData, $".README.md.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, ReadmeContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, readme, overwrite: false);
        }
        catch (IOException) when (File.Exists(readme))
        {
            // Another AppHost installed the README first; keep its copy.
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
