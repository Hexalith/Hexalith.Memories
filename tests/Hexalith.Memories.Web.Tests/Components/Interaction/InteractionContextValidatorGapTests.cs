// <copyright file="InteractionContextValidatorGapTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Interaction;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Interaction;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

/// <summary>
/// QA gap coverage for <see cref="InteractionContextValidator"/>: missing-tenant guards, cross-tenant and
/// cross-case packet leakage between the captured snapshot and the live packet scope, and graph/activity
/// target existence paths that the existing suite does not exercise.
/// </summary>
public sealed class InteractionContextValidatorGapTests
{
    [Fact]
    public void Validate_NullPacket_Throws()
        => Should.Throw<ArgumentNullException>(() => InteractionContextValidator.Validate(
            null!, InteractionContextTests.Snapshot(), "tenant-a", "case-a"));

    [Fact]
    public void Validate_NullSnapshot_Throws()
        => Should.Throw<ArgumentNullException>(() => InteractionContextValidator.Validate(
            EvidencePacketFixtures.CompletePacket(), null!, "tenant-a", "case-a"));

    [Theory]
    [InlineData(" ")]
    [InlineData("")]
    public void Validate_BlankSnapshotTenant_IsMissingTenant(string snapshotTenant)
    {
        InteractionContextValidationResult result = InteractionContextValidator.Validate(
            EvidencePacketFixtures.CompletePacket(),
            InteractionContextTests.Snapshot() with { TenantId = snapshotTenant },
            "tenant-a",
            "case-a");

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe(InteractionContextValidationReason.MissingTenant);
    }

    [Fact]
    public void Validate_BlankActiveTenant_IsMissingTenant()
    {
        InteractionContextValidationResult result = InteractionContextValidator.Validate(
            EvidencePacketFixtures.CompletePacket(),
            InteractionContextTests.Snapshot(),
            " ",
            "case-a");

        result.Reason.ShouldBe(InteractionContextValidationReason.MissingTenant);
    }

    [Fact]
    public void Validate_PacketTenantDiffersFromSnapshot_IsTenantChanged()
    {
        // The active tenant still matches the snapshot, but the live packet belongs to another tenant: the
        // captured target must not be reused against a foreign tenant's evidence.
        EvidencePacket foreignPacket = EvidencePacketFixtures.CompletePacket();
        foreignPacket = foreignPacket with { Scope = foreignPacket.Scope with { TenantId = "tenant-evil" } };

        InteractionContextValidationResult result = InteractionContextValidator.Validate(
            foreignPacket,
            InteractionContextTests.Snapshot(),
            "tenant-a",
            "case-a");

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe(InteractionContextValidationReason.TenantChanged);
    }

    [Fact]
    public void Validate_PacketCaseDiffersFromSnapshot_IsCaseChanged()
    {
        EvidencePacket foreignCasePacket = EvidencePacketFixtures.CompletePacket();
        foreignCasePacket = foreignCasePacket with { Scope = foreignCasePacket.Scope with { CaseId = "case-evil" } };

        InteractionContextValidationResult result = InteractionContextValidator.Validate(
            foreignCasePacket,
            InteractionContextTests.Snapshot(),
            "tenant-a",
            "case-a");

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe(InteractionContextValidationReason.CaseChanged);
    }

    [Fact]
    public void Validate_KnownGraphTarget_IsValid()
    {
        InteractionContextValidationResult result = InteractionContextValidator.Validate(
            EvidencePacketFixtures.CompletePacket(),
            InteractionContextTests.Snapshot() with { TargetKind = InteractionTargetKind.Graph, TargetId = "memory-a" },
            "tenant-a",
            "case-a");

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_UnknownGraphTarget_IsMissingTarget()
    {
        InteractionContextValidationResult result = InteractionContextValidator.Validate(
            EvidencePacketFixtures.CompletePacket(),
            InteractionContextTests.Snapshot() with { TargetKind = InteractionTargetKind.Graph, TargetId = "ghost-node" },
            "tenant-a",
            "case-a");

        result.Reason.ShouldBe(InteractionContextValidationReason.MissingTarget);
    }

    [Fact]
    public void Validate_ActivityTargetWithoutId_IsValid()
    {
        InteractionContextValidationResult result = InteractionContextValidator.Validate(
            EvidencePacketFixtures.CompletePacket(),
            InteractionContextTests.Snapshot() with { TargetKind = InteractionTargetKind.Activity, TargetId = null },
            "tenant-a",
            "case-a");

        result.IsValid.ShouldBeTrue();
    }
}
