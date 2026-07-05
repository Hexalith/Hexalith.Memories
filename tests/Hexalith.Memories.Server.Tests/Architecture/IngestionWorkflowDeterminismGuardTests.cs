// <copyright file="IngestionWorkflowDeterminismGuardTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using System.IO;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>Story 23.8 replay-determinism guards for ingestion workflow orchestration.</summary>
public static partial class IngestionWorkflowDeterminismGuardTests
{
    [Fact]
    public static void IngestionWorkflow_DoesNotReadMutableConfigurationSnapshots()
    {
        string repoRoot = ResolveRepoRoot();
        string workflowPath = Path.Combine(
            repoRoot,
            "src",
            "Hexalith.Memories.Server",
            "Workflows",
            "IngestionWorkflow.cs");
        string source = StripComments(File.ReadAllText(workflowPath));

        source.Contains("RetryPolicyBuilder", StringComparison.Ordinal).ShouldBeFalse(
            "IngestionWorkflow must use retry policy values captured on IngestionInput, not process-global retry state.");
        source.Contains("NaturalLanguageDescriptionOptionsSnapshot", StringComparison.Ordinal).ShouldBeFalse(
            "IngestionWorkflow must use natural-language options captured on IngestionInput, not process-global option snapshots.");
    }

    private static string StripComments(string source)
        => LineCommentRegex().Replace(source, string.Empty);

    private static string ResolveRepoRoot()
    {
        string candidate = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        return AppContext.BaseDirectory;
    }

    [GeneratedRegex("//[^\\n]*", RegexOptions.CultureInvariant)]
    private static partial Regex LineCommentRegex();
}
