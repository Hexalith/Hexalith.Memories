// <copyright file="InteractionTargetKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>Evidence target kind used by navigation, overlay, command, and confirmation validation.</summary>
public enum InteractionTargetKind
{
    /// <summary>The packet or search result itself.</summary>
    Packet = 0,

    /// <summary>A packet source or memory unit.</summary>
    Source,

    /// <summary>A graph path or graph node exposed by the packet.</summary>
    Graph,

    /// <summary>An activity or ingestion item.</summary>
    Activity,

    /// <summary>An operator check or backend health target.</summary>
    OperatorCheck,

    /// <summary>An MCP packet or payload inspection target.</summary>
    McpPacket,
}
