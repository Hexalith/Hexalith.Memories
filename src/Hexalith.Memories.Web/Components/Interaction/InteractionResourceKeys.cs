// <copyright file="InteractionResourceKeys.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>
/// Stable localization key conventions shared by Story 17.3 interaction surfaces.
/// </summary>
/// <remarks>
/// Story 17.3 — the severity scale is rendered identically across forms, filters, navigation,
/// confirmations, and commands, so its labels resolve through one shared key family instead of being
/// duplicated per family.
/// </remarks>
public static class InteractionResourceKeys
{
    /// <summary>Column header / accessible prefix for a severity badge.</summary>
    public const string SeverityLabel = "Interaction_Severity_Label";

    /// <summary>Builds the severity label key for an interaction severity tier.</summary>
    /// <param name="severity">The severity tier.</param>
    /// <returns>The localization key.</returns>
    public static string Severity(InteractionSeverity severity) => $"Interaction_Severity_{severity}";

    /// <summary>Accessible label for the command surface.</summary>
    public const string CommandSurfaceLabel = "Command_Surface_Label";

    /// <summary>Accessible label for contextual navigation.</summary>
    public const string NavigationLabel = "Navigation_Label";

    /// <summary>Label for the navigation query context.</summary>
    public const string NavigationQueryLabel = "Navigation_Query_Label";

    /// <summary>Label for the preserved return path.</summary>
    public const string NavigationReturnLabel = "Navigation_Return_Label";

    /// <summary>Label for opening the target.</summary>
    public const string NavigationOpenLabel = "Navigation_Open_Label";

    /// <summary>Label for returning to the packet.</summary>
    public const string NavigationReturnActionLabel = "Navigation_ReturnAction_Label";

    /// <summary>Confirmation title key.</summary>
    public const string ConfirmationTitle = "Confirmation_Title";

    /// <summary>Confirmation tenant label key.</summary>
    public const string ConfirmationTenantLabel = "Confirmation_Tenant_Label";

    /// <summary>Confirmation case label key.</summary>
    public const string ConfirmationCaseLabel = "Confirmation_Case_Label";

    /// <summary>Confirmation object label key.</summary>
    public const string ConfirmationObjectLabel = "Confirmation_Object_Label";

    /// <summary>Confirmation consequence label key.</summary>
    public const string ConfirmationConsequenceLabel = "Confirmation_Consequence_Label";

    /// <summary>Confirmation recovery label key.</summary>
    public const string ConfirmationRecoveryLabel = "Confirmation_Recovery_Label";

    /// <summary>Builds a command label key.</summary>
    /// <param name="kind">The command kind.</param>
    /// <returns>The localization key.</returns>
    public static string Command(MemoriesCommandKind kind) => $"Command_{kind}_Label";

    /// <summary>Builds a command consequence key.</summary>
    /// <param name="kind">The command kind.</param>
    /// <returns>The localization key.</returns>
    public static string CommandConsequence(MemoriesCommandKind kind) => $"Command_{kind}_Consequence";

    /// <summary>Builds a command recovery/undo expectation key.</summary>
    /// <param name="kind">The command kind.</param>
    /// <returns>The localization key.</returns>
    public static string CommandRecovery(MemoriesCommandKind kind) => $"Command_{kind}_Recovery";

    /// <summary>Builds a disabled-reason key.</summary>
    /// <param name="reason">The validation reason.</param>
    /// <returns>The localization key.</returns>
    public static string DisabledReason(InteractionContextValidationReason reason) => $"Interaction_Disabled_{reason}";
}
