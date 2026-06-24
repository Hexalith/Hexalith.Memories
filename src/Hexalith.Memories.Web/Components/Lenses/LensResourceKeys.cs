// <copyright file="LensResourceKeys.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses;

/// <summary>
/// Stable localization key conventions for the shared Story 17.4 lens shell.
/// </summary>
/// <remarks>
/// Every user-facing shell string resolves through a key defined here so lens titles, role labels, trust
/// labels, and the contract-boundary availability fallbacks come from localization resources instead of
/// component-side string building. Per-lens body keys live in each lens's own resource-keys class.
/// </remarks>
public static class LensResourceKeys
{
    /// <summary>Sanitized literal shown for confidence when the scope is restrictive.</summary>
    /// <remarks>A plain value (not a resx key); rendered directly so it never reveals evidence strength.</remarks>
    public const string ConfidenceUnavailableText = "unavailable";

    /// <summary>Accessible label for the shared lens shell region.</summary>
    public const string ShellLabel = "Lens_Shell_Label";

    /// <summary>Label preceding the tenant identifier.</summary>
    public const string TenantLabel = "Lens_Tenant_Label";

    /// <summary>Label preceding the case identifier.</summary>
    public const string CaseLabel = "Lens_Case_Label";

    /// <summary>Fallback shown when the case identifier is absent (tenant-wide scope).</summary>
    public const string TenantScope = "Lens_TenantScope";

    /// <summary>Label preceding the active lens name.</summary>
    public const string ActiveLensLabel = "Lens_ActiveLens_Label";

    /// <summary>Label preceding the active role.</summary>
    public const string RoleLabel = "Lens_Role_Label";

    /// <summary>Label preceding the confidence value.</summary>
    public const string ConfidenceLabel = "Lens_Confidence_Label";

    /// <summary>Label preceding the freshness value.</summary>
    public const string FreshnessLabel = "Lens_Freshness_Label";

    /// <summary>Label preceding the packet trust state.</summary>
    public const string StateLabel = "Lens_State_Label";

    /// <summary>Label preceding the affected capability.</summary>
    public const string CapabilityLabel = "Lens_Capability_Label";

    /// <summary>Label preceding the contract version.</summary>
    public const string ContractVersionLabel = "Lens_ContractVersion_Label";

    /// <summary>Label preceding the return route.</summary>
    public const string ReturnLabel = "Lens_Return_Label";

    /// <summary>Caption for the return-to-packet action.</summary>
    public const string ReturnAction = "Lens_Return_Action";

    /// <summary>Builds the lens title key.</summary>
    /// <param name="lens">The lens.</param>
    /// <returns>The localization key.</returns>
    public static string LensTitle(LensKind lens) => $"Lens_{lens}_Title";

    /// <summary>Builds the lens description key.</summary>
    /// <param name="lens">The lens.</param>
    /// <returns>The localization key.</returns>
    public static string LensDescription(LensKind lens) => $"Lens_{lens}_Description";

    /// <summary>Builds the role label key.</summary>
    /// <param name="role">The role-density profile.</param>
    /// <returns>The localization key.</returns>
    public static string Role(LensRole role) => $"Lens_Role_{role}";

    /// <summary>Builds the contract-boundary availability fallback key.</summary>
    /// <param name="availability">The field availability.</param>
    /// <returns>The localization key.</returns>
    public static string Availability(LensFieldAvailability availability) => $"Lens_Availability_{availability}";
}
