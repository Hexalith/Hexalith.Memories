namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Server.Search;

using Shouldly;

public class RediSearchQueryEscaperTests
{
    public static TheoryData<string, string> AdversarialInputs => new()
    {
        { "$", @"\$" },
        { "=>", @"\=\>" },
        { "%", @"\%" },
        { "~", @"\~" },
        { ",", @"\," },
        { "#", @"\#" },
        { ";", @"\;" },
        { ".", @"\." },
        { "<", @"\<" },
        { ">", @"\>" },
        { "+", @"\+" },
        { "/", @"\/" },
        { @"\", @"\\" },
        { "\"quote\"", "\\\"quote\\\"" },
        { "'quote'", @"\'quote\'" },
        { "(a)[b]{c}", @"\(a\)\[b\]\{c\}" },
        { "a|b", @"a\|b" },
        { "*", @"\*" },
        { "-", @"\-" },
        { "@content:{secret}", @"\@content\:\{secret\}" },
        { "tag|union", @"tag\|union" },
        { "!!!", @"\!\!\!" },
        { "&`", @"\&\`" },
    };

    [Theory]
    [MemberData(nameof(AdversarialInputs))]
    public void EscapeText_WithRediSearchSyntax_ShouldEscapeOperators(string input, string expected)
    {
        string result = RediSearchQueryEscaper.EscapeText(input);

        result.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(AdversarialInputs))]
    public void EscapeTag_WithRediSearchSyntax_ShouldEscapeOperators(string input, string expected)
    {
        string result = RediSearchQueryEscaper.EscapeTag(input);

        result.ShouldBe(expected);
    }

    [Fact]
    public void EscapeText_WithWhitespace_ShouldPreserveFreeTextSpacing()
    {
        string result = RediSearchQueryEscaper.EscapeText("hello world");

        result.ShouldBe("hello world");
    }

    [Fact]
    public void EscapeTag_WithDialectTwoSpaces_ShouldPreserveSpaces()
    {
        string result = RediSearchQueryEscaper.EscapeTag("claim subject");

        result.ShouldBe("claim subject");
    }

    [Fact]
    public void EscapeTagComposite_WithAttributeKeyAndValue_ShouldEscapeSeparatorAndParts()
    {
        string result = RediSearchQueryEscaper.EscapeTagComposite("status@field", "Active|Archived");

        result.ShouldBe(@"status\@field\=Active\|Archived");
    }
}
