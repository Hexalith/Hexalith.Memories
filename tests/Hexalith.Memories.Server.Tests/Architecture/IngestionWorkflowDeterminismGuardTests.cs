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
    public static void IngestionWorkflowStarts_MatchTheReviewedServerInventoryExactly()
    {
        string repoRoot = ResolveRepoRoot();
        (string RelativePath, int Line, string Method, string NameExpression)[] expected =
        [
            ("src/Hexalith.Memories.Server/Activities/Cases/ScheduleAnnotationIngestionActivity.cs", 54, "direct", "nameof(IngestionWorkflow)"),
            ("src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs", 319, "direct", "nameof(IngestionWorkflow)"),
            ("src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs", 73, "direct", "nameof(IngestionWorkflow)"),
            ("src/Hexalith.Memories.Server/Workflows/AnnotationProjectionWorkflow.cs", 32, "child", "nameof(IngestionWorkflow)"),
        ];

        IReadOnlyList<(string RelativePath, int Line, string Method, string NameExpression)> detected =
            FindServerWorkflowStarts(repoRoot);
        string[] expectedKeys = expected
            .Select(site => $"{site.RelativePath}:{site.Line}|{site.Method}|{site.NameExpression}")
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] detectedKeys = detected
            .Select(site => $"{site.RelativePath}:{site.Line}|{site.Method}|{site.NameExpression}")
            .Order(StringComparer.Ordinal)
            .ToArray();
        string detectedDiagnostic = detected.Count == 0
            ? "(none)"
            : string.Join(
                Environment.NewLine,
                detected.Select(site => $"- {site.RelativePath}:{site.Line} ({site.Method}, {site.NameExpression})"));

        detectedKeys.ShouldBe(
            expectedKeys,
            "The hand-written Server IngestionWorkflow start inventory changed. "
            + "Normal top-level callers must use IIngestionWorkflowScheduler; a direct, activity-owned, or child exception requires explicit review and a bound capture/claim-check proof."
            + Environment.NewLine
            + "Detected supported starts:"
            + Environment.NewLine
            + detectedDiagnostic);
    }

    [Fact]
    public static void ReviewedWorkflowStartExceptions_PreserveCapturedStateAndClaimCheckBoundaries()
    {
        string repoRoot = ResolveRepoRoot();
        string scheduler = LoadNormalizedSource(
            repoRoot,
            "src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs");
        string endpoints = LoadNormalizedSource(
            repoRoot,
            "src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs");
        string annotationActivity = LoadNormalizedSource(
            repoRoot,
            "src/Hexalith.Memories.Server/Activities/Cases/ScheduleAnnotationIngestionActivity.cs");
        string annotationWorkflow = LoadNormalizedSource(
            repoRoot,
            "src/Hexalith.Memories.Server/Workflows/AnnotationProjectionWorkflow.cs");
        string caseService = LoadNormalizedSource(
            repoRoot,
            "src/Hexalith.Memories.Server/Cases/CaseService.cs");

        AssertOrdered(
            scheduler,
            "IngestionInput slimInput = await PrepareInputAsync( _payloadStore, instanceId, input, _workflowConfigurationCapture, _workflowTraceContextCapture, cancellationToken)",
            "ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), instanceId, slimInput, null, cancellationToken)");
        AssertOrdered(
            scheduler,
            "IngestionInput configuredInput = workflowTraceContextCapture.Apply(workflowConfigurationCapture.Apply(input));",
            "return IngestionPayloadClaimCheck.PrepareAsync(payloadStore, instanceId, configuredInput, cancellationToken);");

        AssertContains(
            endpoints,
            "SourceUri = request.Url, ContentBytes = null,");
        AssertContains(
            endpoints,
            "input = workflowTraceContextCapture.Apply(workflowConfigurationCapture.Apply(input)); string instanceId = await workflowClient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input);");

        AssertOrdered(
            annotationActivity,
            "WorkflowConfiguration = input.WorkflowConfiguration,",
            "if (ingestionInput.WorkflowConfiguration is null)",
            "ingestionInput = workflowConfigurationCapture.Apply(ingestionInput);");
        AssertContains(
            annotationActivity,
            "ingestionInput = await IngestionPayloadClaimCheck .PrepareAsync(payloadStore, input.AnnotationMemoryUnitId, ingestionInput) .ConfigureAwait(false); await workflowClient.ScheduleNewWorkflowAsync( nameof(IngestionWorkflow), instanceId: input.AnnotationMemoryUnitId, input: ingestionInput).ConfigureAwait(false);");

        AssertContains(annotationWorkflow, "ContentBytes = Encoding.UTF8.GetBytes(input.Content),");
        AssertContains(annotationWorkflow, "WorkflowConfiguration = input.WorkflowConfiguration,");
        AssertOrdered(
            annotationWorkflow,
            "CallChildWorkflowAsync<IngestionResult>( nameof(IngestionWorkflow), CreateIngestionInput(input),",
            "catch (WorkflowTaskFailedException)",
            "CallActivityAsync<bool>( nameof(CleanupGraphActivity)");
        annotationWorkflow.Contains("IngestionPayloadClaimCheck", StringComparison.Ordinal).ShouldBeFalse(
            "AnnotationProjectionWorkflow must not claim-check its already-durable parent content a second time.");

        AssertContains(
            caseService,
            "metadata, _workflowConfigurationCapture?.Capture() ?? new IngestionWorkflowConfiguration(), _workflowTraceContextCapture?.Capture())");
    }

    [Fact]
    public static void ServerEventStoreComposition_UsesTheServerSchedulerAdapter()
    {
        string repoRoot = ResolveRepoRoot();
        string host = LoadNormalizedSource(
            repoRoot,
            "src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs");
        string registration = LoadNormalizedSource(
            repoRoot,
            "src/Hexalith.Memories.Server/EventStoreIntegration/ServerEventStoreIntegrationExtensions.cs");
        string adapter = LoadNormalizedSource(
            repoRoot,
            "src/Hexalith.Memories.Server/EventStoreIntegration/EventIngestionWorkflowSchedulerAdapter.cs");

        AssertContains(host, "builder.Services.AddSingleton<IIngestionWorkflowScheduler, DaprIngestionWorkflowScheduler>();");
        AssertContains(host, "builder.Services.AddServerEventStoreIntegration(builder.Configuration);");
        AssertContains(registration, ".AddWorkflowScheduler<EventIngestionWorkflowSchedulerAdapter>()");
        AssertContains(
            registration,
            "services.TryAddSingleton<IEventIngestionWorkflowScheduler, EventIngestionWorkflowSchedulerAdapter>();");
        AssertContains(adapter, "=> _inner.ScheduleAsync(instanceId, input, cancellationToken);");
    }

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

    [Theory]
    [InlineData("client.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input);", "direct", "nameof(IngestionWorkflow)")]
    [InlineData("client.ScheduleNewWorkflowAsync(\n    \"IngestionWorkflow\",\n    input: input);", "direct", "\"IngestionWorkflow\"")]
    [InlineData("context.CallChildWorkflowAsync<IngestionResult>(\n    nameof (\n        IngestionWorkflow\n    ),\n    input);", "child", "nameof(IngestionWorkflow)")]
    [InlineData("client.ScheduleNewWorkflowAsync(workflowName: nameof(IngestionWorkflow), input: input);", "direct", "nameof(IngestionWorkflow)")]
    [InlineData("client.ScheduleNewWorkflowAsync(nameof(Hexalith.Memories.Server.Workflows.IngestionWorkflow), input: input);", "direct", "nameof(IngestionWorkflow)")]
    [InlineData("context.CallChildWorkflowAsync<IReadOnlyList<IngestionResult>>(nameof(IngestionWorkflow), input);", "child", "nameof(IngestionWorkflow)")]
    public static void WorkflowStartMatcher_RecognizesDocumentedCompileTimeForms(
        string source,
        string expectedMethod,
        string expectedNameExpression)
    {
        (string RelativePath, int Line, string Method, string NameExpression) match =
            FindWorkflowStarts(source, "Sample.cs").ShouldHaveSingleItem();

        match.RelativePath.ShouldBe("Sample.cs");
        match.Line.ShouldBe(1);
        match.Method.ShouldBe(expectedMethod);
        match.NameExpression.ShouldBe(expectedNameExpression);
    }

    [Theory]
    [InlineData("client.ScheduleNewWorkflowAsync(workflowName, input: input);")]
    [InlineData("const string WorkflowName = \"IngestionWorkflow\"; client.ScheduleNewWorkflowAsync(WorkflowName, input: input);")]
    [InlineData("ScheduleThroughWrapper(client, nameof(IngestionWorkflow), input);")]
    [InlineData("typeof(IngestionWorkflow).Name")]
    [InlineData("client.FakeScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input);")]
    [InlineData("client.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow) + suffix, input: input);")]
    [InlineData("client.ScheduleNewWorkflowAsync(\"IngestionWorkflow\".ToString(), input: input);")]
    public static void WorkflowStartMatcher_DoesNotClaimUnsupportedIndirectForms(string source)
        => FindWorkflowStarts(source, "Sample.cs").ShouldBeEmpty();

    [Fact]
    public static void WorkflowStartMatcher_IgnoresConditionalCompilationBlocks()
    {
        const string source = "#if false\nclient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input);\n#endif";

        FindWorkflowStarts(source, "Sample.cs").ShouldBeEmpty();
    }

    [Fact]
    public static void WorkflowStartMatcher_IgnoresInvocationShapedTextInsideNormalString()
    {
        const string source = "string text = \"client.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input);\";";

        FindWorkflowStarts(source, "Sample.cs").ShouldBeEmpty();
    }

    [Fact]
    public static void WorkflowStartMatcher_CommentMarkersInsideStringDoNotHideFollowingRealStart()
    {
        const string source = "string markers = \"// not a comment; /* not a comment */\";\nclient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input);";

        (string RelativePath, int Line, string Method, string NameExpression) match =
            FindWorkflowStarts(source, "Sample.cs").ShouldHaveSingleItem();

        match.Line.ShouldBe(2);
    }

    [Fact]
    public static void WorkflowStartMatcher_IgnoresActualLineAndBlockComments()
    {
        const string source = "// client.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input);\n"
            + "/* context.CallChildWorkflowAsync<IngestionResult>(nameof(IngestionWorkflow), input); */\n"
            + "client.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input);";

        (string RelativePath, int Line, string Method, string NameExpression) match =
            FindWorkflowStarts(source, "Sample.cs").ShouldHaveSingleItem();

        match.Line.ShouldBe(3);
        match.Method.ShouldBe("direct");
    }

    [Fact]
    public static void WorkflowStartMatcher_VerbatimStringWithDoubledQuotesDoesNotHideFollowingRealStart()
    {
        const string source = "string text = @\"\"\"client.FakeScheduleNewWorkflowAsync(nameof(IngestionWorkflow))\"\"\";\n"
            + "client.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input);";

        (string RelativePath, int Line, string Method, string NameExpression) match =
            FindWorkflowStarts(source, "Sample.cs").ShouldHaveSingleItem();

        match.Line.ShouldBe(2);
    }

    [Fact]
    public static void WorkflowStartMatcher_MasksInterpolatedTextAndPreservesExecutableHole()
    {
        const string source = "$\"client.FakeScheduleNewWorkflowAsync(nameof(IngestionWorkflow)) {client.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input)}\"";

        (string RelativePath, int Line, string Method, string NameExpression) match =
            FindWorkflowStarts(source, "Sample.cs").ShouldHaveSingleItem();

        match.Method.ShouldBe("direct");
        match.NameExpression.ShouldBe("nameof(IngestionWorkflow)");
    }

    private static string StripComments(string source)
        => MaskNonCode(source, maskStringAndCharacterLiterals: false);

    private static string MaskNonCodeForWorkflowInventory(string source)
        => MaskNonCode(source, maskStringAndCharacterLiterals: true);

    private static string MaskNonCode(string source, bool maskStringAndCharacterLiterals)
    {
        char[] masked = source.ToCharArray();
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == '/' && index + 1 < source.Length && source[index + 1] == '/')
            {
                int end = source.IndexOf('\n', index + 2);
                end = end < 0 ? source.Length : end;
                MaskRange(masked, index, end);
                index = end;
                continue;
            }

            if (source[index] == '/' && index + 1 < source.Length && source[index + 1] == '*')
            {
                int closing = source.IndexOf("*/", index + 2, StringComparison.Ordinal);
                int end = closing < 0 ? source.Length : closing + 2;
                MaskRange(masked, index, end);
                index = end;
                continue;
            }

            if (source[index] == '"')
            {
                int interpolationDollarCount = GetInterpolationDollarCount(source, index);
                int end;
                if (interpolationDollarCount > 0)
                {
                    end = MaskInterpolatedString(
                        source,
                        masked,
                        index,
                        interpolationDollarCount,
                        maskStringAndCharacterLiterals);
                }
                else
                {
                    end = FindStringLiteralEnd(source, index);
                    bool exactWorkflowNameLiteral = source.AsSpan(index, end - index)
                        .SequenceEqual("\"IngestionWorkflow\"".AsSpan());
                    if (maskStringAndCharacterLiterals && !exactWorkflowNameLiteral)
                    {
                        MaskRange(masked, index, end);
                    }
                }

                index = end;
                continue;
            }

            if (source[index] == '\'')
            {
                int end = FindCharacterLiteralEnd(source, index);
                if (maskStringAndCharacterLiterals)
                {
                    MaskRange(masked, index, end);
                }

                index = end;
                continue;
            }

            index++;
        }

        MaskConditionalCompilationBlocks(masked);
        return new string(masked);
    }

    private static int GetInterpolationDollarCount(string source, int quoteStart)
    {
        int current = quoteStart - 1;
        if (current >= 0 && source[current] == '@')
        {
            current--;
        }

        int dollarCount = 0;
        while (current >= 0 && source[current] == '$')
        {
            dollarCount++;
            current--;
        }

        return dollarCount;
    }

    private static int MaskInterpolatedString(
        string source,
        char[] masked,
        int start,
        int interpolationDollarCount,
        bool maskStringAndCharacterLiterals)
    {
        bool verbatim = IsVerbatimString(source, start);
        int quoteCount = CountConsecutive(source, start, '"');
        if (!verbatim && quoteCount >= 3)
        {
            return MaskRawInterpolatedString(
                source,
                masked,
                start,
                quoteCount,
                interpolationDollarCount,
                maskStringAndCharacterLiterals);
        }

        int current = start + 1;
        int literalStart = start;
        while (current < source.Length)
        {
            if (source[current] == '"')
            {
                if (verbatim && current + 1 < source.Length && source[current + 1] == '"')
                {
                    current += 2;
                    continue;
                }

                int end = current + 1;
                if (maskStringAndCharacterLiterals)
                {
                    MaskRange(masked, literalStart, end);
                }

                return end;
            }

            if (!verbatim && source[current] == '\\' && current + 1 < source.Length)
            {
                current += 2;
                continue;
            }

            if (source[current] == '{')
            {
                if (current + 1 < source.Length && source[current + 1] == '{')
                {
                    current += 2;
                    continue;
                }

                int closing = FindInterpolationHoleEnd(source, current + 1);
                if (maskStringAndCharacterLiterals)
                {
                    MaskRange(masked, literalStart, current + 1);
                }

                if (closing < 0)
                {
                    if (maskStringAndCharacterLiterals)
                    {
                        MaskRange(masked, current + 1, source.Length);
                    }

                    return source.Length;
                }

                CopyMaskedInterpolationHole(
                    source,
                    masked,
                    current + 1,
                    closing,
                    maskStringAndCharacterLiterals);
                if (maskStringAndCharacterLiterals)
                {
                    MaskRange(masked, closing, closing + 1);
                }

                current = closing + 1;
                literalStart = current;
                continue;
            }

            current++;
        }

        if (maskStringAndCharacterLiterals)
        {
            MaskRange(masked, literalStart, source.Length);
        }

        return source.Length;
    }

    private static int MaskRawInterpolatedString(
        string source,
        char[] masked,
        int start,
        int quoteCount,
        int interpolationDollarCount,
        bool maskStringAndCharacterLiterals)
    {
        int current = start + quoteCount;
        int literalStart = start;
        while (current < source.Length)
        {
            if (source[current] == '"' && CountConsecutive(source, current, '"') >= quoteCount)
            {
                int end = current + quoteCount;
                if (maskStringAndCharacterLiterals)
                {
                    MaskRange(masked, literalStart, end);
                }

                return end;
            }

            if (source[current] == '{'
                && CountConsecutive(source, current, '{') >= interpolationDollarCount)
            {
                int codeStart = current + interpolationDollarCount;
                int closing = interpolationDollarCount == 1
                    ? FindInterpolationHoleEnd(source, codeStart)
                    : FindRawInterpolationHoleEnd(source, codeStart, interpolationDollarCount);
                if (maskStringAndCharacterLiterals)
                {
                    MaskRange(masked, literalStart, codeStart);
                }

                if (closing < 0)
                {
                    if (maskStringAndCharacterLiterals)
                    {
                        MaskRange(masked, codeStart, source.Length);
                    }

                    return source.Length;
                }

                CopyMaskedInterpolationHole(
                    source,
                    masked,
                    codeStart,
                    closing,
                    maskStringAndCharacterLiterals);
                if (maskStringAndCharacterLiterals)
                {
                    MaskRange(masked, closing, closing + interpolationDollarCount);
                }

                current = closing + interpolationDollarCount;
                literalStart = current;
                continue;
            }

            current++;
        }

        if (maskStringAndCharacterLiterals)
        {
            MaskRange(masked, literalStart, source.Length);
        }

        return source.Length;
    }

    private static int FindInterpolationHoleEnd(string source, int start)
    {
        int depth = 1;
        int current = start;
        while (current < source.Length)
        {
            if (source[current] == '/' && current + 1 < source.Length && source[current + 1] == '/')
            {
                int end = source.IndexOf('\n', current + 2);
                current = end < 0 ? source.Length : end;
                continue;
            }

            if (source[current] == '/' && current + 1 < source.Length && source[current + 1] == '*')
            {
                int closing = source.IndexOf("*/", current + 2, StringComparison.Ordinal);
                current = closing < 0 ? source.Length : closing + 2;
                continue;
            }

            if (source[current] == '"')
            {
                current = FindStringLiteralEnd(source, current);
                continue;
            }

            if (source[current] == '\'')
            {
                current = FindCharacterLiteralEnd(source, current);
                continue;
            }

            if (source[current] == '{')
            {
                depth++;
            }
            else if (source[current] == '}' && --depth == 0)
            {
                return current;
            }

            current++;
        }

        return -1;
    }

    private static int FindRawInterpolationHoleEnd(string source, int start, int closingBraceCount)
    {
        int current = start;
        while (current < source.Length)
        {
            if (source[current] == '}' && CountConsecutive(source, current, '}') >= closingBraceCount)
            {
                return current;
            }

            if (source[current] == '"')
            {
                current = FindStringLiteralEnd(source, current);
                continue;
            }

            if (source[current] == '\'')
            {
                current = FindCharacterLiteralEnd(source, current);
                continue;
            }

            current++;
        }

        return -1;
    }

    private static void CopyMaskedInterpolationHole(
        string source,
        char[] masked,
        int start,
        int end,
        bool maskStringAndCharacterLiterals)
    {
        string hole = MaskNonCode(source[start..end], maskStringAndCharacterLiterals);
        hole.CopyTo(0, masked, start, hole.Length);
    }

    private static int CountConsecutive(string source, int start, char character)
    {
        int count = 0;
        while (start + count < source.Length && source[start + count] == character)
        {
            count++;
        }

        return count;
    }

    private static bool IsVerbatimString(string source, int start)
        => (start > 0 && source[start - 1] == '@')
            || (start > 1 && source[start - 2] == '@' && source[start - 1] == '$');

    private static int FindStringLiteralEnd(string source, int start)
    {
        bool verbatim = IsVerbatimString(source, start);
        int quoteCount = CountConsecutive(source, start, '"');

        if (!verbatim && quoteCount >= 3)
        {
            int index = start + quoteCount;
            while (index < source.Length)
            {
                if (source[index] != '"')
                {
                    index++;
                    continue;
                }

                int closingQuoteCount = 1;
                while (index + closingQuoteCount < source.Length && source[index + closingQuoteCount] == '"')
                {
                    closingQuoteCount++;
                }

                if (closingQuoteCount >= quoteCount)
                {
                    return index + quoteCount;
                }

                index += closingQuoteCount;
            }

            return source.Length;
        }

        int current = start + 1;
        while (current < source.Length)
        {
            if (source[current] == '"')
            {
                if (verbatim && current + 1 < source.Length && source[current + 1] == '"')
                {
                    current += 2;
                    continue;
                }

                return current + 1;
            }

            if (!verbatim && source[current] == '\\' && current + 1 < source.Length)
            {
                current += 2;
                continue;
            }

            current++;
        }

        return source.Length;
    }

    private static int FindCharacterLiteralEnd(string source, int start)
    {
        int current = start + 1;
        while (current < source.Length)
        {
            if (source[current] == '\\' && current + 1 < source.Length)
            {
                current += 2;
                continue;
            }

            if (source[current] == '\'')
            {
                return current + 1;
            }

            current++;
        }

        return source.Length;
    }

    private static void MaskRange(char[] source, int start, int end)
    {
        for (int index = start; index < end; index++)
        {
            if (source[index] is not ('\r' or '\n'))
            {
                source[index] = ' ';
            }
        }
    }

    private static void MaskConditionalCompilationBlocks(char[] source)
    {
        int conditionalDepth = 0;
        int conditionalStart = -1;
        int lineStart = 0;
        while (lineStart < source.Length)
        {
            int lineEnd = Array.IndexOf(source, '\n', lineStart);
            int nextLine = lineEnd < 0 ? source.Length : lineEnd + 1;
            int directiveStart = lineStart;
            while (directiveStart < nextLine && source[directiveStart] is ' ' or '\t' or '\r')
            {
                directiveStart++;
            }

            if (IsPreprocessorDirective(source, directiveStart, nextLine, "if"))
            {
                if (conditionalDepth == 0)
                {
                    conditionalStart = lineStart;
                }

                conditionalDepth++;
            }
            else if (conditionalDepth > 0 && IsPreprocessorDirective(source, directiveStart, nextLine, "endif"))
            {
                conditionalDepth--;
                if (conditionalDepth == 0)
                {
                    MaskRange(source, conditionalStart, nextLine);
                    conditionalStart = -1;
                }
            }

            lineStart = nextLine;
        }

        if (conditionalDepth > 0)
        {
            MaskRange(source, conditionalStart, source.Length);
        }
    }

    private static bool IsPreprocessorDirective(
        char[] source,
        int start,
        int end,
        string directive)
    {
        if (start >= end || source[start] != '#')
        {
            return false;
        }

        int current = start + 1;
        while (current < end && source[current] is ' ' or '\t')
        {
            current++;
        }

        if (current + directive.Length > end)
        {
            return false;
        }

        for (int index = 0; index < directive.Length; index++)
        {
            if (source[current + index] != directive[index])
            {
                return false;
            }
        }

        int boundary = current + directive.Length;
        return boundary >= end || source[boundary] is ' ' or '\t' or '\r' or '\n';
    }

    private static IReadOnlyList<(string RelativePath, int Line, string Method, string NameExpression)> FindServerWorkflowStarts(
        string repoRoot)
    {
        string serverRoot = Path.Combine(repoRoot, "src", "Hexalith.Memories.Server");
        List<(string RelativePath, int Line, string Method, string NameExpression)> matches = [];
        foreach (string filePath in Directory.EnumerateFiles(serverRoot, "*.cs", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(repoRoot, filePath).Replace(Path.DirectorySeparatorChar, '/');
            if (relativePath.Split('/').Any(segment => segment is "bin" or "obj"))
            {
                continue;
            }

            matches.AddRange(FindWorkflowStarts(File.ReadAllText(filePath), relativePath));
        }

        return matches;
    }

    private static IReadOnlyList<(string RelativePath, int Line, string Method, string NameExpression)> FindWorkflowStarts(
        string source,
        string relativePath)
    {
        string sourceWithoutComments = MaskNonCodeForWorkflowInventory(source);
        return WorkflowStartRegex()
            .Matches(sourceWithoutComments)
            .Select(match =>
            {
                string method = match.Groups["method"].Value.StartsWith("CallChildWorkflowAsync", StringComparison.Ordinal)
                    ? "child"
                    : "direct";
                string nameExpression = match.Groups["name"].Value.StartsWith("nameof", StringComparison.Ordinal)
                    ? "nameof(IngestionWorkflow)"
                    : "\"IngestionWorkflow\"";
                int line = sourceWithoutComments[..match.Index].Count(character => character == '\n') + 1;
                return (relativePath, line, method, nameExpression);
            })
            .ToArray();
    }

    private static string LoadNormalizedSource(string repoRoot, string relativePath)
        => NormalizeWhitespace(MaskNonCodeForWorkflowInventory(File.ReadAllText(Path.Combine(repoRoot, relativePath))));

    private static string NormalizeWhitespace(string source)
        => WhitespaceRegex().Replace(source, " ").Trim();

    private static void AssertContains(string normalizedSource, string requiredSource)
        => normalizedSource.Contains(NormalizeWhitespace(requiredSource), StringComparison.Ordinal).ShouldBeTrue(
            $"Required deterministic scheduling proof was not found: {NormalizeWhitespace(requiredSource)}");

    private static void AssertOrdered(string normalizedSource, params string[] requiredSource)
    {
        int startIndex = 0;
        foreach (string required in requiredSource)
        {
            string normalizedRequired = NormalizeWhitespace(required);
            int matchIndex = normalizedSource.IndexOf(normalizedRequired, startIndex, StringComparison.Ordinal);
            matchIndex.ShouldBeGreaterThanOrEqualTo(
                0,
                $"Required deterministic scheduling proof was missing or out of order: {normalizedRequired}");
            startIndex = matchIndex + normalizedRequired.Length;
        }
    }

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

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("(?<![A-Za-z0-9_])(?<method>ScheduleNewWorkflowAsync|CallChildWorkflowAsync\\s*<\\s*(?:[^<>]+|<[^<>]*>)+\\s*>)\\s*\\(\\s*(?:workflowName\\s*:\\s*)?(?<name>nameof\\s*\\(\\s*(?:(?:[A-Za-z_][A-Za-z0-9_]*)\\s*(?:::|\\.)\\s*)*IngestionWorkflow\\s*\\)|\"IngestionWorkflow\")(?=\\s*(?:,|\\)))", RegexOptions.CultureInvariant)]
    private static partial Regex WorkflowStartRegex();
}
