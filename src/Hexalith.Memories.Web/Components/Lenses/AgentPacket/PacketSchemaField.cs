// <copyright file="PacketSchemaField.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.AgentPacket;

/// <summary>A single, sanitized row of the readable Agent Packet Inspector schema view.</summary>
/// <param name="Kind">The schema field kind.</param>
/// <param name="NameKey">Localization key for the field name.</param>
/// <param name="Availability">Availability of the field value.</param>
/// <param name="SafeValue">Sanitized field value, or a documented unavailable fallback.</param>
public sealed record PacketSchemaField(
    PacketSchemaFieldKind Kind,
    string NameKey,
    LensFieldAvailability Availability,
    string SafeValue);
