// <copyright file="RecoveryPacketFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Recovery;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Specimens;

internal static class RecoveryPacketFixtures
{
    public static EvidencePacket Supported() => Epic17RecoveryPacketFixtures.Supported();

    public static EvidencePacket ConflictingViaDegraded() => Epic17RecoveryPacketFixtures.ConflictingViaDegraded();

    public static EvidencePacket ConflictingViaUnavailableAxes() => Epic17RecoveryPacketFixtures.ConflictingViaUnavailableAxes();

    public static EvidencePacket DegradedBackendWithSources() => Epic17RecoveryPacketFixtures.DegradedBackendWithSources();

    public static EvidencePacket DegradedBackendNoSources() => Epic17RecoveryPacketFixtures.DegradedBackendNoSources();

    public static EvidencePacket NotIngestedYet() => Epic17RecoveryPacketFixtures.NotIngestedYet();

    public static EvidencePacket GraphGapNoSources() => Epic17RecoveryPacketFixtures.GraphGapNoSources();

    public static EvidencePacket NoMatch() => Epic17RecoveryPacketFixtures.NoMatch();

    public static EvidencePacket StaleMemory() => Epic17RecoveryPacketFixtures.StaleMemory();

    public static EvidencePacket Weak() => Epic17RecoveryPacketFixtures.Weak();

    public static EvidencePacket InsufficientFromPartial() => Epic17RecoveryPacketFixtures.InsufficientFromPartial();

    public static EvidencePacket InsufficientNoSignal() => Epic17RecoveryPacketFixtures.InsufficientNoSignal();

    public static EvidencePacket Compressed() => Epic17RecoveryPacketFixtures.Compressed();

    public static EvidencePacket Unauthorized() => Epic17RecoveryPacketFixtures.Unauthorized();

    public static EvidencePacket UnknownScope() => Epic17RecoveryPacketFixtures.UnknownScope();

    public static EvidencePacket UnknownState() => Epic17RecoveryPacketFixtures.UnknownState();

    public static EvidencePacket WeakAndCompressed() => Epic17RecoveryPacketFixtures.WeakAndCompressed();

    public static EvidencePacket StaleAndCompressed() => Epic17RecoveryPacketFixtures.StaleAndCompressed();

    public static EvidencePacket StaleDegradedWithSources() => Epic17RecoveryPacketFixtures.StaleDegradedWithSources();

    public static EvidencePacket MultiActionNoMatch() => Epic17RecoveryPacketFixtures.MultiActionNoMatch();

    public static EvidencePacket UnauthorizedWithExpandingActions() => Epic17RecoveryPacketFixtures.UnauthorizedWithExpandingActions();

    public static EvidencePacket UnauthorizedHighCount() => Epic17RecoveryPacketFixtures.UnauthorizedHighCount();

    public static EvidencePacket UnauthorizedZeroCount() => Epic17RecoveryPacketFixtures.UnauthorizedZeroCount();

    public static EvidencePacket SensitiveRecoveryAction() => Epic17RecoveryPacketFixtures.SensitiveRecoveryAction();

    public static EvidencePacket SensitiveScopeRecovery() => Epic17RecoveryPacketFixtures.SensitiveScopeRecovery();

    public static EvidencePacket MalformedButSafe() => Epic17RecoveryPacketFixtures.MalformedButSafe();
}
