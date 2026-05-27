// <copyright file="EvidencePacketFieldMapping.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Evidence;

/// <summary>Documents the explicit Evidence Packet field source for one rendered cockpit value.</summary>
/// <param name="DisplayField">Rendered field identifier.</param>
/// <param name="ContractSource">Canonical Evidence Packet source field.</param>
/// <param name="UnavailableFallback">Fallback rendered when the source field is absent or unavailable.</param>
public sealed record EvidencePacketFieldMapping(
    string DisplayField,
    string ContractSource,
    string UnavailableFallback);
