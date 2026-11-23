// <copyright file="GraphContentProof.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.TestHelpers.Documentation;

using System.Text.RegularExpressions;

/// <summary>Identifies the retained graph-content proof in the fast integration inventory.</summary>
public static class GraphContentProof
{
    private static readonly Lazy<(string ClassName, string MethodName)> Identity = new(ReadIdentity);

    /// <summary>Gets the complete test selector for the graph-content proof.</summary>
    public static string TestName => $"{Identity.Value.ClassName}.{Identity.Value.MethodName}";

    /// <summary>Gets the short proof citation used in the operator runbooks and verifier responses.</summary>
    public static string Citation
    {
        get
        {
            (string className, string methodName) = Identity.Value;
            return $"{className[(className.LastIndexOf('.') + 1)..]}.{methodName}";
        }
    }

    /// <summary>Gets a regex pattern that matches the complete citation as an identifier.</summary>
    public static string CitationPattern => $@"(?<![\w.]){Regex.Escape(Citation)}(?![\w.])";

    private static (string ClassName, string MethodName) ReadIdentity()
    {
        const string surface = "graph-content-isolation";
        string path = Path.Combine(TestRepository.Root, "tools", "integration-fast-required-surfaces.txt");
        string[][] rows = File.ReadLines(path)
            .Select(line => line.Split('|', StringSplitOptions.TrimEntries))
            .Where(fields => fields[0] == surface)
            .ToArray();
        if (rows.Length != 1 || rows[0].Length != 3 ||
            string.IsNullOrWhiteSpace(rows[0][1]) || string.IsNullOrWhiteSpace(rows[0][2]))
        {
            throw new InvalidDataException($"Expected one '{surface}' row with a class and method in '{path}'.");
        }

        return (rows[0][1], rows[0][2]);
    }
}
