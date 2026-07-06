// <copyright file="EvidencePacketFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Evidence;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Specimens;

internal static class EvidencePacketFixtures
{
    public static EvidencePacket CompletePacket() => Epic17EvidencePacketFixtures.CompletePacket();

    public static EvidencePacket CompressedPacket() => Epic17EvidencePacketFixtures.CompressedPacket();

    public static EvidencePacket UnauthorizedPacket() => Epic17EvidencePacketFixtures.UnauthorizedPacket();

    public static EvidencePacket MultiSourcePacket() => Epic17EvidencePacketFixtures.MultiSourcePacket();

    public static EvidencePacket SensitivePacket() => Epic17EvidencePacketFixtures.SensitivePacket();

    public static EvidencePacket TenantCaseSensitivePacket() => Epic17EvidencePacketFixtures.TenantCaseSensitivePacket();

    public static EvidencePacket EmptyPacket() => Epic17EvidencePacketFixtures.EmptyPacket();

    public static EvidencePacket DegradedPacket() => Epic17EvidencePacketFixtures.DegradedPacket();

    public static EvidencePacket PartialPacket() => Epic17EvidencePacketFixtures.PartialPacket();

    public static EvidencePacket WeakPacket() => Epic17EvidencePacketFixtures.WeakPacket();

    public static EvidencePacket StalePacket() => Epic17EvidencePacketFixtures.StalePacket();

    public static EvidencePacket RedactedPacket() => Epic17EvidencePacketFixtures.RedactedPacket();

    public static EvidencePacket UnknownScopePacket() => Epic17EvidencePacketFixtures.UnknownScopePacket();
}
