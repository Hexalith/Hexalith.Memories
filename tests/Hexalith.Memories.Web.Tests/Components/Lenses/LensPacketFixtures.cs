// <copyright file="LensPacketFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Lenses;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Specimens;

internal static class LensPacketFixtures
{
    public static EvidencePacket Happy() => Epic17LensPacketFixtures.Happy();

    public static EvidencePacket Degraded() => Epic17LensPacketFixtures.Degraded();

    public static EvidencePacket Unauthorized() => Epic17LensPacketFixtures.Unauthorized();

    public static EvidencePacket UnknownScope() => Epic17LensPacketFixtures.UnknownScope();

    public static EvidencePacket Redacted() => Epic17LensPacketFixtures.Redacted();

    public static EvidencePacket Compressed() => Epic17LensPacketFixtures.Compressed();

    public static EvidencePacket Stale() => Epic17LensPacketFixtures.Stale();

    public static EvidencePacket Empty() => Epic17LensPacketFixtures.Empty();

    public static EvidencePacket NotIngested() => Epic17LensPacketFixtures.NotIngested();

    public static EvidencePacket SchemaMismatch() => Epic17LensPacketFixtures.SchemaMismatch();

    public static EvidencePacket CrossTenant() => Epic17LensPacketFixtures.CrossTenant();

    public static EvidencePacket MissingSource() => Epic17LensPacketFixtures.MissingSource();

    public static EvidencePacket Sensitive() => Epic17LensPacketFixtures.Sensitive();

    public static EvidencePacket TenantCaseSensitive() => Epic17LensPacketFixtures.TenantCaseSensitive();

    public static IEnumerable<EvidencePacket> All() => Epic17LensPacketFixtures.All();
}
