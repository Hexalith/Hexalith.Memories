// <copyright file="CaseNameTemplateRendererTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using Hexalith.Memories.EventStore;

using Shouldly;

public sealed class CaseNameTemplateRendererTests
{
    [Fact]
    public void Render_DefaultTemplate_SubstitutesAggregateType()
        => CaseNameTemplateRenderer
            .Render("events:{aggregateType}", "tenant-1", "Claims")
            .ShouldBe("events:Claims");

    [Fact]
    public void Render_BothTokens_SubstituteBoth()
        => CaseNameTemplateRenderer
            .Render("{tenantId}/{aggregateType}", "tenant-1", "Claims")
            .ShouldBe("tenant-1/Claims");

    [Fact]
    public void Render_NonAllowlistedBraceToken_Throws()
        => Should.Throw<ArgumentException>(
            () => CaseNameTemplateRenderer.Render("events:{aggregateType}:{hacker}", "tenant-1", "Claims"));

    [Fact]
    public void Render_UnbalancedBracesOrFormatSpecifiers_Throw()
        => Should.Throw<ArgumentException>(
            () => CaseNameTemplateRenderer.Render("events:{aggregateType}-{0}-{tenantId:X}", "tenant-1", "Claims"));

    [Fact]
    public void Render_EmptyTemplate_Throws()
        => Should.Throw<ArgumentException>(
            () => CaseNameTemplateRenderer.Render(string.Empty, "tenant-1", "Claims"));

    [Fact]
    public void Render_WhitespaceTenantId_Throws()
        => Should.Throw<ArgumentException>(
            () => CaseNameTemplateRenderer.Render("x", "   ", "Claims"));
}
