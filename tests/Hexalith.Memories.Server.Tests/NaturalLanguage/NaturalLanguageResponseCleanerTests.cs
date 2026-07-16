// <copyright file="NaturalLanguageResponseCleanerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using Hexalith.Memories.Server.NaturalLanguage;

using Shouldly;

/// <summary>Story 9.2 Task 2.9 — tests for <c>NaturalLanguageResponseCleaner</c>. Covers Risk #7
/// (LLM returning JSON or markdown when plain text expected).</summary>
public sealed class NaturalLanguageResponseCleanerTests
{
    [Fact]
    public void StripsMarkdownCodeFences()
    {
        string raw = "```\nUser updated their shipping address to a new city.\n```";

        bool ok = NaturalLanguageResponseCleaner.TryClean(raw, out string cleaned);

        ok.ShouldBeTrue();
        cleaned.ShouldBe("User updated their shipping address to a new city.");
    }

    [Fact]
    public void StripsLanguageTaggedMarkdownFences()
    {
        string raw = "```markdown\nThe customer submitted a refund request totaling $125.\n```";

        bool ok = NaturalLanguageResponseCleaner.TryClean(raw, out string cleaned);

        ok.ShouldBeTrue();
        cleaned.ShouldBe("The customer submitted a refund request totaling $125.");
    }

    [Fact]
    public void StripsCommonPreambles_Summary()
    {
        string raw = "Summary: The user registered a new account yesterday evening.";

        bool ok = NaturalLanguageResponseCleaner.TryClean(raw, out string cleaned);

        ok.ShouldBeTrue();
        cleaned.ShouldBe("The user registered a new account yesterday evening.");
    }

    [Fact]
    public void StripsCommonPreambles_HereIsTheSummary()
    {
        string raw = "Here is the summary: A policy renewal was completed for the selected customer.";

        bool ok = NaturalLanguageResponseCleaner.TryClean(raw, out string cleaned);

        ok.ShouldBeTrue();
        cleaned.ShouldBe("A policy renewal was completed for the selected customer.");
    }

    [Fact]
    public void EmptyAfterCleanupThrows_ReturnsFalse()
    {
        string raw = "```\n   \n```";

        bool ok = NaturalLanguageResponseCleaner.TryClean(raw, out string cleaned);

        ok.ShouldBeFalse();
        cleaned.ShouldBeEmpty();
    }

    [Fact]
    public void NullResponse_ReturnsFalse()
    {
        bool ok = NaturalLanguageResponseCleaner.TryClean(null, out string cleaned);

        ok.ShouldBeFalse();
        cleaned.ShouldBeEmpty();
    }

    [Fact]
    public void WhitespaceOnlyResponse_ReturnsFalse()
    {
        bool ok = NaturalLanguageResponseCleaner.TryClean("     \n  \t  ", out string cleaned);

        ok.ShouldBeFalse();
        cleaned.ShouldBeEmpty();
    }

    [Fact]
    public void ShortResponse_BelowMinimumLength_ReturnsFalse()
    {
        bool ok = NaturalLanguageResponseCleaner.TryClean("short", out string cleaned);

        ok.ShouldBeFalse();
        cleaned.ShouldBeEmpty();
    }

    [Fact]
    public void PreservesNormalSentence_NoModification()
    {
        string raw = "The operator completed the workflow successfully.";

        bool ok = NaturalLanguageResponseCleaner.TryClean(raw, out string cleaned);

        ok.ShouldBeTrue();
        cleaned.ShouldBe("The operator completed the workflow successfully.");
    }

    [Fact]
    public void CollapsesWhitespace()
    {
        string raw = "The  user   published    a   new   comment.";

        bool ok = NaturalLanguageResponseCleaner.TryClean(raw, out string cleaned);

        ok.ShouldBeTrue();
        cleaned.ShouldBe("The user published a new comment.");
    }

    [Fact]
    public void CollapsesNewlinesWithinCleanedText()
    {
        string raw = "User modified\nthe profile\n  picture.";

        bool ok = NaturalLanguageResponseCleaner.TryClean(raw, out string cleaned);

        ok.ShouldBeTrue();
        cleaned.ShouldBe("User modified the profile picture.");
    }
}
