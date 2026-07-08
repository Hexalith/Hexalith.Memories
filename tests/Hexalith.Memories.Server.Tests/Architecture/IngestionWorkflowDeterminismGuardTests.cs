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

    [Fact]
    public static void WorkflowOrchestrationFiles_DoNotCaptureAmbientTraceState()
    {
        string repoRoot = ResolveRepoRoot();
        string[] workflowFiles =
        [
            Path.Combine(repoRoot, "src", "Hexalith.Memories.Server", "Workflows", "IngestionWorkflow.cs"),
            Path.Combine(repoRoot, "src", "Hexalith.Memories.Server", "Workflows", "AnnotationProjectionWorkflow.cs"),
        ];

        foreach (string workflowPath in workflowFiles)
        {
            string source = StripComments(File.ReadAllText(workflowPath));
            string fileName = Path.GetFileName(workflowPath);

            source.Contains("Activity.Current", StringComparison.Ordinal).ShouldBeFalse(
                $"{fileName} must use serialized trace context from workflow input; ambient Activity.Current is not replay-safe.");
            source.Contains("StartActivity", StringComparison.Ordinal).ShouldBeFalse(
                $"{fileName} orchestration must not emit spans directly because replay can re-execute orchestration code.");
            source.Contains("WorkflowTraceContextCapture", StringComparison.Ordinal).ShouldBeFalse(
                $"Trace context capture belongs at scheduling/activity boundaries, not inside durable workflow orchestration ({fileName}).");
        }
    }

    [Fact]
    public static void DirectUrlIngestion_CapturesTraceContextBeforeDirectWorkflowSchedule()
    {
        string repoRoot = ResolveRepoRoot();

        // Story 25.1 moved the direct URL ingestion endpoint out of Program.cs into the decomposed
        // IngestionEndpoints.cs; this guard follows the code so it keeps verifying the capture-before-schedule
        // ordering at its real home.
        string endpointsPath = Path.Combine(
            repoRoot,
            "src",
            "Hexalith.Memories.Server",
            "Endpoints",
            "IngestionEndpoints.cs");
        string source = StripComments(File.ReadAllText(endpointsPath));

        int captureIndex = source.IndexOf(
            "workflowTraceContextCapture.Apply(workflowConfigurationCapture.Apply(input))",
            StringComparison.Ordinal);
        int scheduleIndex = source.IndexOf(
            "ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input)",
            StringComparison.Ordinal);

        captureIndex.ShouldBeGreaterThanOrEqualTo(0, "The direct URL endpoint must apply workflow trace capture.");
        scheduleIndex.ShouldBeGreaterThan(captureIndex, "Trace context must be captured before direct URL workflow scheduling.");
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
