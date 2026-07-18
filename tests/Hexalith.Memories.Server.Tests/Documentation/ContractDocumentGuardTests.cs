// <copyright file="ContractDocumentGuardTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Documentation;

using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

/// <summary>
/// Covers the narrow Markdown and anti-corruption primitives used by published contract-document guards.
/// </summary>
public sealed class ContractDocumentGuardTests
{
    [Fact]
    public void FindLeakedToolCallMarkup_OrdinaryMarkdownHtmlAndCode_ReturnsNoDiagnostics()
    {
        const string Markdown = """
            # Contract

            Ordinary <contention>, <content-item>, <content.example>, <content:item>, and <div data-kind="invoke">HTML</div> are allowed.
            Inline code such as `<content role="sample">` and ``</invoke>`` is allowed.

            ```xml
            <parameter name="sample">
            <tool_call>
            ```
            """;

        ContractDocumentGuard.FindLeakedToolCallMarkup(Markdown).ShouldBeEmpty();
    }

    [Fact]
    public void FindLeakedToolCallMarkup_AllSupportedRawForms_ReportsMarkerAndLocation()
    {
        const string Markdown = """
            <content>
            </INVOKE>
            <parameter name="value">
            <tool_call id="call-1">
            <CONTENT role="assistant">
            </parameter>
            <invoke
            </tool_call
            """;

        IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(Markdown);

        diagnostics.Count.ShouldBe(8);
        diagnostics[0].ShouldBe("line 1, column 1: <content>");
        diagnostics[1].ShouldBe("line 2, column 1: </INVOKE>");
        diagnostics[2].ShouldContain("<parameter name=\"value\">", Case.Sensitive);
        diagnostics[3].ShouldContain("<tool_call id=\"call-1\">", Case.Sensitive);
        diagnostics[4].ShouldContain("<CONTENT role=\"assistant\">", Case.Sensitive);
        diagnostics[6].ShouldBe("line 7, column 1: <invoke");
        diagnostics[7].ShouldBe("line 8, column 1: </tool_call");
    }

    [Fact]
    public void FindLeakedToolCallMarkup_BackslashEscapedBackticks_DoNotHideRawMarker()
    {
        const string Markdown = @"\`<content>\`";

        IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(Markdown);

        diagnostics.ShouldHaveSingleItem().ShouldContain("<content>", Case.Sensitive);
    }

    [Fact]
    public void FindLeakedToolCallMarkup_FourSpaceIndentedPseudoFence_DoesNotHideRawMarker()
    {
        const string Markdown = "    ```xml\n<invoke>\n    ```";

        IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(Markdown);

        diagnostics.ShouldHaveSingleItem().ShouldContain("<invoke>", Case.Sensitive);
    }

    [Fact]
    public void FindLeakedToolCallMarkup_ValidMultilineCodeSpan_IgnoresContainedMarkerOnly()
    {
        const string Markdown = "A multiline `<parameter>\ncode sample` is allowed.\n<tool_call>";

        IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(Markdown);

        diagnostics.ShouldHaveSingleItem().ShouldBe("line 3, column 1: <tool_call>");
    }

    [Fact]
    public void GetSection_LfAndCrLf_IncludesSubordinatesAndStopsAtPeerHeading()
    {
        const string LfMarkdown = """
            # Document
            ## Contract
            owned line
            ### Detail
            subordinate line
            ## Next
            unrelated line
            """;
        string crlfMarkdown = LfMarkdown.Replace("\n", "\r\n", StringComparison.Ordinal);

        string lfSection = new MarkdownContractDocument(LfMarkdown).GetSection("Contract");
        string crlfSection = new MarkdownContractDocument(crlfMarkdown).GetSection("Contract");

        crlfSection.ShouldBe(lfSection);
        lfSection.ShouldContain("### Detail\nsubordinate line", Case.Sensitive);
        lfSection.ShouldNotContain("unrelated line", Case.Sensitive);
    }

    [Fact]
    public void GetSection_DoubledCarriageReturn_CollapsesToSingleLineBreakLikeLf()
    {
        const string LfMarkdown = """
            # Document
            ## Contract
            owned line
            ### Detail
            subordinate line
            ## Next
            unrelated line
            """;
        string doubledCrMarkdown = LfMarkdown.Replace("\n", "\r\r\n", StringComparison.Ordinal);

        string lfSection = new MarkdownContractDocument(LfMarkdown).GetSection("Contract");
        string doubledCrSection = new MarkdownContractDocument(doubledCrMarkdown).GetSection("Contract");

        doubledCrSection.ShouldBe(lfSection);
    }

    [Fact]
    public void GetTableRows_FencedTableAndHeading_AreIgnoredAndCellsAreNormalized()
    {
        const string Markdown = """
            ## Contract

            ```markdown
            ## Contract
            | Fake | Row |
            | --- | --- |
            | leaked | table |
            ```

            | Name | Value |
            | :--- | ---: |
            | alpha   | **value** |
            | code | `a|b` |
            """;

        IReadOnlyList<IReadOnlyList<string>> rows = new MarkdownContractDocument(Markdown).GetTableRows("Contract");

        rows.Count.ShouldBe(2);
        rows[0].ShouldBe(["alpha", "**value**"]);
        rows[1].ShouldBe(["code", "`a|b`"]);
    }

    [Fact]
    public void MarkdownStructure_IndentedCodeAndHtmlComments_CannotSupplyHeadingsOrTables()
    {
        const string Markdown = """
            <!--
            ## Contract
            | Fake | Comment |
            | --- | --- |
            | hidden | row |
            -->
                ## Contract
                | Fake | Indented |
                | --- | --- |
                | code | row |

            ## Contract
            | Name | Value |
            | --- | --- |
            | real | row |
            """;
        var document = new MarkdownContractDocument(Markdown);

        document.GetTableHeader("Contract").ShouldBe(["Name", "Value"]);
        document.GetTableRows("Contract").ShouldBe([new[] { "real", "row" }]);
    }

    [Fact]
    public void GetSection_MissingExactHeading_Throws()
    {
        var document = new MarkdownContractDocument("## Similar heading\ncontent");

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => document.GetSection("Exact heading"));

        exception.Message.ShouldContain("found 0", Case.Sensitive);
    }

    [Fact]
    public void GetSection_DuplicateExactHeading_Throws()
    {
        var document = new MarkdownContractDocument("## Contract\nfirst\n## Contract\nsecond");

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => document.GetSection("Contract"));

        exception.Message.ShouldContain("found 2", Case.Sensitive);
    }

    [Fact]
    public void GetTableRows_MultipleTablesInOwningSection_Throws()
    {
        const string Markdown = """
            ## Contract
            | A |
            | --- |
            | one |

            | B |
            | --- |
            | two |
            """;
        var document = new MarkdownContractDocument(Markdown);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => document.GetTableRows("Contract"));

        exception.Message.ShouldContain("found 2", Case.Sensitive);
    }

    [Fact]
    public void GetTableRows_TableExistsOnlyUnderDifferentHeading_Throws()
    {
        const string Markdown = """
            ## Contract
            Required vocabulary appears in prose only.

            ## Unrelated
            | Name | Value |
            | --- | --- |
            | required | vocabulary |
            """;
        var document = new MarkdownContractDocument(Markdown);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => document.GetTableRows("Contract"));

        exception.Message.ShouldContain("found 0", Case.Sensitive);
    }
}
