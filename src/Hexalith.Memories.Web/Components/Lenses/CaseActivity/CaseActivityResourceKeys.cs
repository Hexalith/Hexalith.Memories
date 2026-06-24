// <copyright file="CaseActivityResourceKeys.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.CaseActivity;

/// <summary>Stable localization key conventions for the Case Activity Trail lens (AC1).</summary>
public static class CaseActivityResourceKeys
{
    /// <summary>Accessible label for the activity trail region.</summary>
    public const string RegionLabel = "CaseActivity_Region_Label";

    /// <summary>Shown when the trail has no source, annotation, relationship, or gap activity.</summary>
    public const string Empty = "CaseActivity_Empty";

    /// <summary>Note explaining the deterministic ordering basis (timestamps unavailable).</summary>
    public const string OrderingBasis = "CaseActivity_Ordering_Basis";

    /// <summary>Column header / accessible prefix for the activity status.</summary>
    public const string StatusLabel = "CaseActivity_Status_Label";

    /// <summary>Column header / accessible prefix for the activity link.</summary>
    public const string LinkLabel = "CaseActivity_Link_Label";

    /// <summary>Builds the activity kind label key.</summary>
    /// <param name="kind">The activity kind.</param>
    /// <returns>The localization key.</returns>
    public static string Kind(CaseActivityKind kind) => $"CaseActivity_Kind_{kind}";

    /// <summary>Builds the link status label key for a field availability.</summary>
    /// <param name="availability">The link availability.</param>
    /// <returns>The localization key.</returns>
    public static string LinkStatus(LensFieldAvailability availability) => $"CaseActivity_LinkStatus_{availability}";
}
