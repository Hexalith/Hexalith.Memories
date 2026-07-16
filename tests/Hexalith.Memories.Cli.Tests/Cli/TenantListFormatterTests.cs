// <copyright file="TenantListFormatterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Text.Json;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Cli.Output.Json;
using Hexalith.Memories.Contracts.V1;

using Shouldly;

public sealed class TenantListFormatterTests
{
    [Fact]
    public void Human_EmptyList_PreservesStory71NoTenantsMessage()
    {
        using var writer = new StringWriter() { NewLine = "\n" };
        new TenantListHumanFormatter().Write(Array.Empty<TenantSummary>(), writer);
        writer.ToString().ShouldBe("No tenants found.\n");
    }

    [Fact]
    public void Human_WithTenants_PreservesStory71TabSeparatedLines()
    {
        using var writer = new StringWriter() { NewLine = "\n" };
        IReadOnlyList<TenantSummary> tenants =
        [
            CreateSummary("t-1", "Tenant One"),
            CreateSummary("t-2", "Tenant Two"),
        ];

        new TenantListHumanFormatter().Write(tenants, writer);

        writer.ToString().ShouldBe(
            "t-1\tTenant One\n"
            + "t-2\tTenant Two\n");
    }

    [Fact]
    public void Json_WithTenants_EmitsSchemaVersion1Envelope()
    {
        using var writer = new StringWriter() { NewLine = "\n" };
        IReadOnlyList<TenantSummary> tenants = [CreateSummary("t-1", "Tenant One")];

        new JsonEnvelopeFormatter<IReadOnlyList<TenantSummary>>(TenantListCommand.CommandName).Write(tenants, writer);
        using JsonDocument doc = JsonDocument.Parse(writer.ToString());

        doc.RootElement.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("command").GetString().ShouldBe("tenant list");
        doc.RootElement.GetProperty("data").GetArrayLength().ShouldBe(1);
        doc.RootElement.GetProperty("data")[0].GetProperty("id").GetString().ShouldBe("t-1");
    }

    [Fact]
    public void Json_EmptyList_EmitsEmptyDataArray()
    {
        using var writer = new StringWriter() { NewLine = "\n" };
        new JsonEnvelopeFormatter<IReadOnlyList<TenantSummary>>(TenantListCommand.CommandName)
            .Write(Array.Empty<TenantSummary>(), writer);
        using JsonDocument doc = JsonDocument.Parse(writer.ToString());

        doc.RootElement.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("data").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public void Table_EmptyList_EmitsHeaderAndSeparatorOnly()
    {
        using var writer = new StringWriter() { NewLine = "\n" };
        new TenantListTableFormatter().Write(Array.Empty<TenantSummary>(), writer);

        string[] lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(2);
        lines[0].ShouldContain("TENANT ID");
        lines[0].ShouldContain("DISPLAY NAME");
        lines[1].ShouldAllBe(c => c == '-');
    }

    [Fact]
    public void Table_WithTenants_RightPadsColumns()
    {
        using var writer = new StringWriter() { NewLine = "\n" };
        IReadOnlyList<TenantSummary> tenants =
        [
            CreateSummary("t-1", "Alpha"),
            CreateSummary("longer-tenant", "Beta"),
        ];

        new TenantListTableFormatter().Write(tenants, writer);
        string[] lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Length.ShouldBe(4);
        lines[0].ShouldStartWith("TENANT ID    ");
        lines[2].ShouldStartWith("t-1          ");
        lines[3].ShouldStartWith("longer-tenant");
    }

    private static TenantSummary CreateSummary(string id, string displayName)
        => new()
        {
            Id = id,
            DisplayName = displayName,
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            IndexSizes = new TenantIndexSizes(null, null, null),
            IndexStatus = new TenantIndexStatus(IndexHealth.Unknown, IndexHealth.Unknown, IndexHealth.Unknown),
            ReindexRequired = false,
        };
}
