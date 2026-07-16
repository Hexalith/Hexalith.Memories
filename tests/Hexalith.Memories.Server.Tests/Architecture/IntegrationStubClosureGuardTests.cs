// <copyright file="IntegrationStubClosureGuardTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using System.IO;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>Story 26.3 source guards that prevent assertion-free integration targets from reporting success.</summary>
public static partial class IntegrationStubClosureGuardTests
{
    private static readonly string[] GenericSkipFragments =
    [
        "requires aspire fixture",
        "requires aspire apphost fixture",
        "story 6.4",
        "epic 7",
        "todo",
        "not implemented",
    ];

    [Fact]
    public static void IntegrationTests_ContainNoRunnableSkipAttributeOrNoOpPlaceholderBodies()
    {
        string repoRoot = ResolveRepoRoot();
        string integrationRoot = Path.Combine(repoRoot, "tests", "Hexalith.Memories.IntegrationTests");
        List<string> violations = [];

        foreach (string file in Directory.EnumerateFiles(integrationRoot, "*.cs", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(repoRoot, file);
            string source = File.ReadAllText(file);

            AddLineViolations(violations, relativePath, source, "RunnableSkippedFact", "uses the false-pass RunnableSkippedFact attribute/type");
            foreach ((int index, string methodName) in FindRunnableNoOpTests(source))
            {
                violations.Add($"{relativePath}:{GetLineNumber(source, index)} runnable test '{methodName}' has an assertion-free no-op body");
            }
        }

        violations.ShouldBeEmpty(
            "Integration tests must use normal facts with assertions or literal, specific xUnit skips; false-pass placeholders are forbidden.");
    }

    [Fact]
    public static void IntegrationTests_ExplicitSkipsReferenceStructuredDeferralAndEnablingCondition()
    {
        string repoRoot = ResolveRepoRoot();
        string integrationRoot = Path.Combine(repoRoot, "tests", "Hexalith.Memories.IntegrationTests");
        List<string> violations = [];

        foreach (string file in Directory.EnumerateFiles(integrationRoot, "*.cs", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(repoRoot, file);
            string source = File.ReadAllText(file);

            foreach (Match method in TestMethodRegex().Matches(source))
            {
                string attributes = method.Groups["attributes"].Value;
                if (!TestAttributeRegex().IsMatch(attributes))
                {
                    continue;
                }

                Match skip = SkipReasonRegex().Match(attributes);
                if (!skip.Success)
                {
                    continue;
                }

                string reason = skip.Groups["reason"].Value.Trim();
                int line = GetLineNumber(source, method.Index + skip.Index);
                if (reason.Length < 40 || GenericSkipFragments.Any(
                    fragment => reason.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                {
                    violations.Add($"{relativePath}:{line} has a blank/generic/stale explicit skip reason");
                    continue;
                }

                if (!StructuredDeferralIdRegex().IsMatch(reason))
                {
                    violations.Add($"{relativePath}:{line} skip reason does not reference a structured deferred-work ID");
                }

                if (!reason.Contains("Owner:", StringComparison.Ordinal) ||
                    !reason.Contains("Unskip when:", StringComparison.Ordinal))
                {
                    violations.Add($"{relativePath}:{line} skip reason must name Owner and Unskip when");
                }
            }
        }

        violations.ShouldBeEmpty(
            "Literal skips must identify a structured deferred-work entry, current owner, and stable enabling condition.");
    }

    [Theory]
    [InlineData("[Fact] public void Empty() { }", true)]
    [InlineData("[Fact] public void CommentOnly() { /* scenario */ }", true)]
    [InlineData("[Fact] public void ReturnOnly() { return; }", true)]
    [InlineData("[Fact] public Task CompletedTask() => Task.CompletedTask;", true)]
    [InlineData("[Fact] public async Task AwaitCompletedTask() { await Task.CompletedTask; }", true)]
    [InlineData("[Fact] public void FixtureDiscard() { _ = _fixture; }", true)]
    [InlineData("[Fact] public void RealAssertion() { true.ShouldBeTrue(); }", false)]
    [InlineData("[Fact(Skip = \"26.3-EXAMPLE: Owner: team. Unskip when: ready.\")] public void Deferred() { }", false)]
    public static void RunnableNoOpClassifier_RejectsAssertionFreeSuccessPaths(string source, bool expectedViolation)
        => FindRunnableNoOpTests(source).Any().ShouldBe(expectedViolation);

    [Theory]
    [InlineData("[Fact(DisplayName = \"example\", Skip = \"too short\")] public void Deferred() { }")]
    [InlineData("[Theory(Skip = \"too short\")] [InlineData(1)] public void Deferred(int value) { }")]
    public static void SkipAttributeScanner_FindsFactAndTheoryShapes(string source)
    {
        Match method = TestMethodRegex().Match(source);
        method.Success.ShouldBeTrue();
        SkipReasonRegex().IsMatch(method.Groups["attributes"].Value).ShouldBeTrue();
    }

    private static void AddLineViolations(
        ICollection<string> violations,
        string relativePath,
        string source,
        string fragment,
        string message)
    {
        int searchStart = 0;
        while (source.IndexOf(fragment, searchStart, StringComparison.Ordinal) is int index && index >= 0)
        {
            violations.Add($"{relativePath}:{GetLineNumber(source, index)} {message}");
            searchStart = index + fragment.Length;
        }
    }

    private static IEnumerable<(int Index, string MethodName)> FindRunnableNoOpTests(string source)
    {
        foreach (Match method in TestMethodRegex().Matches(source))
        {
            string attributes = method.Groups["attributes"].Value;
            if (!TestAttributeRegex().IsMatch(attributes) || SkipReasonRegex().IsMatch(attributes))
            {
                continue;
            }

            int bodyStart = method.Groups["bodyStart"].Index;
            string body = method.Groups["bodyStart"].Value == "=>"
                ? ExtractExpressionBody(source, bodyStart + 2)
                : ExtractBlockBody(source, bodyStart);
            if (IsNoOpBody(body))
            {
                yield return (method.Index, method.Groups["method"].Value);
            }
        }
    }

    private static string ExtractBlockBody(string source, int openingBrace)
    {
        int depth = 0;
        for (int index = openingBrace; index < source.Length; index++)
        {
            if (TrySkipTriviaOrLiteral(source, ref index))
            {
                continue;
            }

            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}' && --depth == 0)
            {
                return source[(openingBrace + 1)..index];
            }
        }

        return source[(openingBrace + 1)..];
    }

    private static string ExtractExpressionBody(string source, int expressionStart)
    {
        for (int index = expressionStart; index < source.Length; index++)
        {
            if (TrySkipTriviaOrLiteral(source, ref index))
            {
                continue;
            }

            if (source[index] == ';')
            {
                return source[expressionStart..index];
            }
        }

        return source[expressionStart..];
    }

    private static bool IsNoOpBody(string body)
    {
        string withoutComments = BlockCommentRegex().Replace(LineCommentRegex().Replace(body, string.Empty), string.Empty);
        string normalized = WhitespaceRegex().Replace(withoutComments, string.Empty);
        return normalized is ""
            or "return;"
            or "Task.CompletedTask;"
            or "Task.CompletedTask"
            or "returnTask.CompletedTask;"
            or "awaitTask.CompletedTask;"
            or "_=_fixture;"
            or "_=_fixture";
    }

    private static bool TrySkipTriviaOrLiteral(string source, ref int index)
    {
        if (index + 1 < source.Length && source[index] == '/' && source[index + 1] == '/')
        {
            int newline = source.IndexOf('\n', index + 2);
            index = newline < 0 ? source.Length - 1 : newline;
            return true;
        }

        if (index + 1 < source.Length && source[index] == '/' && source[index + 1] == '*')
        {
            int end = source.IndexOf("*/", index + 2, StringComparison.Ordinal);
            index = end < 0 ? source.Length - 1 : end + 1;
            return true;
        }

        int quoteIndex = index;
        while (quoteIndex < source.Length && source[quoteIndex] is '$' or '@')
        {
            quoteIndex++;
        }

        if (quoteIndex >= source.Length || source[quoteIndex] is not ('"' or '\''))
        {
            return false;
        }

        char quote = source[quoteIndex];
        bool verbatim = source.AsSpan(index, quoteIndex - index).Contains('@');
        int rawQuoteCount = quote == '"' ? CountRun(source, quoteIndex, '"') : 1;
        if (rawQuoteCount >= 3)
        {
            string terminator = new('"', rawQuoteCount);
            int rawEnd = source.IndexOf(terminator, quoteIndex + rawQuoteCount, StringComparison.Ordinal);
            index = rawEnd < 0 ? source.Length - 1 : rawEnd + rawQuoteCount - 1;
            return true;
        }

        for (int cursor = quoteIndex + 1; cursor < source.Length; cursor++)
        {
            if (verbatim && source[cursor] == '"' && cursor + 1 < source.Length && source[cursor + 1] == '"')
            {
                cursor++;
                continue;
            }

            if (!verbatim && source[cursor] == '\\')
            {
                cursor++;
                continue;
            }

            if (source[cursor] == quote)
            {
                index = cursor;
                return true;
            }
        }

        index = source.Length - 1;
        return true;
    }

    private static int CountRun(string source, int start, char value)
    {
        int count = 0;
        while (start + count < source.Length && source[start + count] == value)
        {
            count++;
        }

        return count;
    }

    private static int GetLineNumber(string source, int index)
        => source.AsSpan(0, Math.Clamp(index, 0, source.Length)).Count('\n') + 1;

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

    [GeneratedRegex(
        @"(?<attributes>(?:\s*\[[^\]]+\]\s*)+)(?:public|internal|private|protected)\s+(?:static\s+)?(?:async\s+)?(?:Task(?:<[^>]+>)?|ValueTask(?:<[^>]+>)?|void)\s+(?<method>[A-Za-z_]\w*)\s*\([^)]*\)\s*(?<bodyStart>\{|=>)",
        RegexOptions.CultureInvariant | RegexOptions.Multiline)]
    private static partial Regex TestMethodRegex();

    [GeneratedRegex(@"\[(?:Fact|Theory)\b", RegexOptions.CultureInvariant)]
    private static partial Regex TestAttributeRegex();

    [GeneratedRegex(@"\bSkip\s*=\s*""(?<reason>(?:\\.|[^""])*)""", RegexOptions.CultureInvariant)]
    private static partial Regex SkipReasonRegex();

    [GeneratedRegex(@"//[^\r\n]*", RegexOptions.CultureInvariant)]
    private static partial Regex LineCommentRegex();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex BlockCommentRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\d+\.\d+-[A-Z0-9][A-Z0-9-]+", RegexOptions.CultureInvariant)]
    private static partial Regex StructuredDeferralIdRegex();

}
