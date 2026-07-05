// <copyright file="AgentPacketInspectorMapperTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Lenses;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Lenses;
using Hexalith.Memories.Web.Components.Lenses.AgentPacket;
using Hexalith.Memories.Web.Components.Recovery;

using Shouldly;

public sealed class AgentPacketInspectorMapperTests
{
    [Fact]
    public void Map_NullPacket_Throws()
        => Should.Throw<ArgumentNullException>(() => AgentPacketInspectorMapper.Map(null!, LensRole.AgentIntegrator));

    [Fact]
    public void Map_HappyPacket_RendersReadableSchemaWithoutStructuredError()
    {
        AgentPacketInspectorViewModel view = AgentPacketInspectorMapper.Map(
            LensPacketFixtures.Happy(),
            LensRole.AgentIntegrator);

        view.HasError.ShouldBeFalse();
        view.Restrictive.ShouldBeFalse();
        view.CountsAvailability.ShouldBe(LensFieldAvailability.Available);
        view.ToolNameAvailability.ShouldBe(LensFieldAvailability.Available);
        view.SchemaFields.Select(f => f.Kind).ShouldContain(PacketSchemaFieldKind.ToolName);
        view.SchemaFields.Select(f => f.Kind).ShouldContain(PacketSchemaFieldKind.McpSchema);
        view.SchemaFields.Single(f => f.Kind == PacketSchemaFieldKind.ToolName).SafeValue.ShouldBe("search_memory");
        view.SchemaFields.Single(f => f.Kind == PacketSchemaFieldKind.McpSchema).SafeValue
            .ShouldBe("memories.search_memory.result@v1");
        view.SafeCopyText.ShouldContain("ScopeTenant=tenant-a");
        view.SafeCopyText.ShouldContain("diagnostic=");
    }

    [Fact]
    public void Map_CompressedPacket_ShowsTokenBudgetOmittedFieldsAndExpansionHandles()
    {
        AgentPacketInspectorViewModel view = AgentPacketInspectorMapper.Map(
            LensPacketFixtures.Compressed(),
            LensRole.AgentIntegrator);

        view.HasError.ShouldBeTrue();
        view.TokenBudgetStateKey.ShouldBe(AgentPacketResourceKeys.TokenBudgetCompressed);
        view.OmittedFieldNames.ShouldContain("rankedResults");
        view.Expansions.ShouldContain(e => e.Kind == EvidencePacketRecoveryKind.IncreaseTokenBudget);
        view.SafeCopyText.ShouldContain("TokenBudget=1200 tokens");
    }

    [Fact]
    public void Map_UnauthorizedPacket_SuppressesCountsTokenBudgetAndEvidenceStrength()
    {
        AgentPacketInspectorViewModel view = AgentPacketInspectorMapper.Map(
            LensPacketFixtures.Unauthorized(),
            LensRole.AgentIntegrator);

        view.Restrictive.ShouldBeTrue();
        view.CountsAvailability.ShouldBe(LensFieldAvailability.Unauthorized);
        view.TokenBudgetAvailability.ShouldBe(LensFieldAvailability.Unauthorized);
        view.ToolNameAvailability.ShouldBe(LensFieldAvailability.Unauthorized);
        view.OmittedFieldNames.ShouldBeEmpty();
        view.Expansions.ShouldBeEmpty();

        view.SchemaFields.Single(f => f.Kind == PacketSchemaFieldKind.ResultCounts).Availability
            .ShouldBe(LensFieldAvailability.Unauthorized);
        view.SchemaFields.Single(f => f.Kind == PacketSchemaFieldKind.ToolName).Availability
            .ShouldBe(LensFieldAvailability.Unauthorized);
        view.SchemaFields.Single(f => f.Kind == PacketSchemaFieldKind.EvidenceStrength).SafeValue
            .ShouldBe("unavailable");
        view.SafeCopyText.ShouldNotContain("memory-secret");
        view.SafeCopyText.ShouldNotContain("secret-axis-evidence");
    }

    [Fact]
    public void Map_SensitivePacket_RedactsCopyTextAndSchemaValues()
    {
        AgentPacketInspectorViewModel view = AgentPacketInspectorMapper.Map(
            LensPacketFixtures.TenantCaseSensitive(),
            LensRole.AgentIntegrator);

        view.SafeCopyText.ShouldNotContain("Bearer ");
        view.SafeCopyText.ShouldNotContain("C:\\Users\\Jerome");
        view.SafeCopyText.ShouldContain("[REDACTED]");
        view.SchemaFields.Select(f => f.SafeValue).ShouldAllBe(v => !v.Contains("Bearer ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Map_SchemaMismatch_RendersSafeErrorStateWithoutThrowing()
    {
        AgentPacketInspectorViewModel view = AgentPacketInspectorMapper.Map(
            LensPacketFixtures.SchemaMismatch(),
            LensRole.AgentIntegrator);

        view.HasError.ShouldBeTrue();
        view.Severity.ShouldNotBe(RecoverySeverity.None);
        view.SafeDiagnosticCode.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Map_EmptyPacket_SignalsNoMatchWithoutRequiringRawJsonInspection()
    {
        AgentPacketInspectorViewModel view = AgentPacketInspectorMapper.Map(
            LensPacketFixtures.Empty(),
            LensRole.AgentIntegrator);

        // The error/no-match condition is conveyed by readable signals, not by forcing the user to read raw
        // JSON; the shared copy payload carries no serialized packet braces.
        view.HasError.ShouldBeTrue();
        view.SafeCopyText.ShouldNotContain("{");
        view.SafeCopyText.ShouldNotContain("}");
    }

    [Fact]
    public void Map_RedactedPacket_AnnouncesOmittedRedactedGroupsWithoutErrorState()
    {
        AgentPacketInspectorViewModel view = AgentPacketInspectorMapper.Map(
            LensPacketFixtures.Redacted(),
            LensRole.AgentIntegrator);

        // Redacted detail is announced as omitted, not silently dropped, and a redaction is not an error.
        view.HasError.ShouldBeFalse();
        view.OmittedFieldNames.ShouldContain("redactedContent");
    }
}
