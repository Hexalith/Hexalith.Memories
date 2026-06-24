// <copyright file="InteractionContextTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Interaction;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Interaction;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

public sealed class InteractionContextTests
{
    [Fact]
    public void Traceability_HasRowsForEveryInteractionFamily()
    {
        foreach (InteractionFamily family in Enum.GetValues<InteractionFamily>())
        {
            InteractionTrace row = InteractionTraceability.For(family);

            row.ContractSources.ShouldNotBeEmpty();
            row.FrontComposerSources.ShouldNotBeEmpty();
            row.AuthorizationSource.ShouldNotBeNullOrWhiteSpace();
            row.ResourceKeys.ShouldNotBeEmpty();
            row.UnavailableFallback.ShouldNotBeNullOrWhiteSpace();
        }

        InteractionTraceability.Entries.Count.ShouldBe(Enum.GetValues<InteractionFamily>().Length);
    }

    [Fact]
    public void Validate_CurrentSourceContext_IsValid()
    {
        InteractionContextValidationResult result = InteractionContextValidator.Validate(
            EvidencePacketFixtures.CompletePacket(),
            Snapshot(),
            "tenant-a",
            "case-a");

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("tenant-b", "case-a", InteractionContextValidationReason.TenantChanged)]
    [InlineData("tenant-a", "case-b", InteractionContextValidationReason.CaseChanged)]
    public void Validate_ScopeChanges_DisableStaleTargets(string tenant, string? @case, InteractionContextValidationReason reason)
    {
        InteractionContextValidationResult result = InteractionContextValidator.Validate(
            EvidencePacketFixtures.CompletePacket(),
            Snapshot(),
            tenant,
            @case);

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe(reason);
    }

    [Fact]
    public void Validate_ContractVersionMismatch_DisablesContext()
    {
        InteractionContextValidationResult result = InteractionContextValidator.Validate(
            EvidencePacketFixtures.CompletePacket(),
            Snapshot() with { ContractVersion = "v2" },
            "tenant-a",
            "case-a");

        result.Reason.ShouldBe(InteractionContextValidationReason.ContractVersionMismatch);
        result.DisabledReasonKey.ShouldBe(InteractionResourceKeys.DisabledReason(result.Reason));
    }

    [Fact]
    public void Validate_MissingSourceTarget_DisablesContext()
    {
        InteractionContextValidationResult result = InteractionContextValidator.Validate(
            EvidencePacketFixtures.CompletePacket(),
            Snapshot() with { TargetId = "missing-source" },
            "tenant-a",
            "case-a");

        result.Reason.ShouldBe(InteractionContextValidationReason.MissingTarget);
    }

    [Fact]
    public void Validate_UnauthorizedPacket_DisablesByDefaultButAllowsTenantVerification()
    {
        InteractionContextSnapshot snapshot = Snapshot() with { TargetKind = InteractionTargetKind.Packet, TargetId = null };

        InteractionContextValidator.Validate(EvidencePacketFixtures.UnauthorizedPacket(), snapshot, "tenant-a", "case-a")
            .Reason.ShouldBe(InteractionContextValidationReason.UnauthorizedScope);
        InteractionContextValidator.Validate(
                EvidencePacketFixtures.UnauthorizedPacket(),
                snapshot,
                "tenant-a",
                "case-a",
                allowTenantVerification: true)
            .IsValid.ShouldBeTrue();
    }

    internal static InteractionContextSnapshot Snapshot()
        => new(
            "tenant-a",
            "case-a",
            "find policy context",
            EvidencePacketState.Complete,
            InteractionContextSnapshot.SupportedContractVersion,
            "memories/evidence?packet=memory-a",
            InteractionTargetKind.Source,
            "memory-a",
            []);
}
