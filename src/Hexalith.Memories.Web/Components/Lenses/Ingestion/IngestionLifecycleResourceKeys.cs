// <copyright file="IngestionLifecycleResourceKeys.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.Ingestion;

/// <summary>Stable localization key conventions for the Ingestion Lifecycle Tracker lens (AC2).</summary>
public static class IngestionLifecycleResourceKeys
{
    /// <summary>Accessible label for the ingestion tracker region.</summary>
    public const string RegionLabel = "Ingestion_Region_Label";

    /// <summary>Shown when the tracker has no ingestion units.</summary>
    public const string Empty = "Ingestion_Empty";

    /// <summary>Note explaining the ingestion stage taxonomy is unavailable.</summary>
    public const string StageNote = "Ingestion_StageNote";

    /// <summary>Column header / accessible prefix for the unit identifier.</summary>
    public const string UnitLabel = "Ingestion_Unit_Label";

    /// <summary>Column header / accessible prefix for the stage.</summary>
    public const string StageLabel = "Ingestion_Stage_Label";

    /// <summary>Column header / accessible prefix for the outcome.</summary>
    public const string OutcomeLabel = "Ingestion_Outcome_Label";

    /// <summary>Column header / accessible prefix for the failure summary.</summary>
    public const string FailureLabel = "Ingestion_Failure_Label";

    /// <summary>Column header / accessible prefix for the recovery action.</summary>
    public const string RecoveryLabel = "Ingestion_Recovery_Label";

    /// <summary>Reason shown when a recovery action is disabled because the scope is restrictive.</summary>
    public const string RecoveryDisabledReason = "Ingestion_Recovery_Disabled";

    /// <summary>Shown when no recovery action applies to a unit.</summary>
    public const string RecoveryNone = "Ingestion_Recovery_None";

    /// <summary>No-failure fallback summary.</summary>
    public const string FailureNone = "Ingestion_Failure_None";

    /// <summary>Builds the outcome label key.</summary>
    /// <param name="outcome">The ingestion outcome.</param>
    /// <returns>The localization key.</returns>
    public static string Outcome(IngestionOutcome outcome) => $"Ingestion_Outcome_{outcome}";
}
