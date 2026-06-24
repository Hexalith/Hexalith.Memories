// <copyright file="OperatorHealthResourceKeys.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.OperatorHealth;

/// <summary>Stable localization key conventions for the Operator Health Matrix lens (AC3).</summary>
public static class OperatorHealthResourceKeys
{
    /// <summary>Accessible label for the operator health matrix region.</summary>
    public const string RegionLabel = "Health_Region_Label";

    /// <summary>Note explaining the last-checked time is unavailable.</summary>
    public const string LastCheckedNote = "Health_LastChecked_Note";

    /// <summary>Column header / accessible prefix for the check name.</summary>
    public const string CheckLabel = "Health_Check_Label";

    /// <summary>Column header / accessible prefix for the status.</summary>
    public const string StatusLabel = "Health_Status_Label";

    /// <summary>Column header / accessible prefix for the affected capability.</summary>
    public const string CapabilityLabel = "Health_Capability_Label";

    /// <summary>Column header / accessible prefix for the evidence clue.</summary>
    public const string EvidenceLabel = "Health_Evidence_Label";

    /// <summary>Column header / accessible prefix for the next action.</summary>
    public const string NextActionLabel = "Health_NextAction_Label";

    /// <summary>Shown when no next action applies.</summary>
    public const string NextActionNone = "Health_NextAction_None";

    /// <summary>Reason shown when a next action is disabled because the scope is restrictive.</summary>
    public const string NextActionDisabled = "Health_NextAction_Disabled";

    /// <summary>Accessible label marking a trust-blocking check.</summary>
    public const string TrustBlockingLabel = "Health_TrustBlocking_Label";

    /// <summary>Builds the check label key.</summary>
    /// <param name="kind">The check kind.</param>
    /// <returns>The localization key.</returns>
    public static string Check(OperatorCheckKind kind) => $"Health_Check_{kind}";

    /// <summary>Builds the status label key.</summary>
    /// <param name="status">The check status.</param>
    /// <returns>The localization key.</returns>
    public static string Status(OperatorCheckStatus status) => $"Health_Status_{status}";
}
