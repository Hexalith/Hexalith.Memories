// <copyright file="AgentPacketResourceKeys.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.AgentPacket;

/// <summary>Stable localization key conventions for the Agent Packet Inspector lens (AC5).</summary>
public static class AgentPacketResourceKeys
{
    /// <summary>Accessible label for the agent packet inspector region.</summary>
    public const string RegionLabel = "Packet_Region_Label";

    /// <summary>Heading for the request summary section.</summary>
    public const string RequestSummaryLabel = "Packet_RequestSummary_Label";

    /// <summary>Label preceding the request query.</summary>
    public const string QueryLabel = "Packet_Query_Label";

    /// <summary>Label preceding the result counts.</summary>
    public const string CountsLabel = "Packet_Counts_Label";

    /// <summary>Heading for the readable schema view.</summary>
    public const string SchemaLabel = "Packet_Schema_Label";

    /// <summary>Heading for the secondary JSON view.</summary>
    public const string JsonLabel = "Packet_Json_Label";

    /// <summary>Label preceding the token budget.</summary>
    public const string TokenBudgetLabel = "Packet_TokenBudget_Label";

    /// <summary>Heading for the omitted-fields section.</summary>
    public const string OmittedFieldsLabel = "Packet_OmittedFields_Label";

    /// <summary>Shown when no fields were omitted.</summary>
    public const string OmittedFieldsNone = "Packet_OmittedFields_None";

    /// <summary>Heading for the expansion-handles section.</summary>
    public const string ExpansionHandlesLabel = "Packet_ExpansionHandles_Label";

    /// <summary>Shown when no expansion handles are present.</summary>
    public const string ExpansionHandlesNone = "Packet_ExpansionHandles_None";

    /// <summary>Heading for the structured error/state section.</summary>
    public const string ErrorLabel = "Packet_Error_Label";

    /// <summary>Label preceding the diagnostic clue.</summary>
    public const string DiagnosticLabel = "Packet_Diagnostic_Label";

    /// <summary>Caption for the copy control.</summary>
    public const string CopyLabel = "Packet_Copy_Label";

    /// <summary>Token-budget state when details were compressed.</summary>
    public const string TokenBudgetCompressed = "Packet_TokenBudget_Compressed";

    /// <summary>Token-budget state when within budget.</summary>
    public const string TokenBudgetWithin = "Packet_TokenBudget_Within";

    /// <summary>Builds the schema field name key.</summary>
    /// <param name="kind">The schema field kind.</param>
    /// <returns>The localization key.</returns>
    public static string Field(PacketSchemaFieldKind kind) => $"Packet_Field_{kind}";
}
