// <copyright file="EvidenceResourceKeys.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Evidence;

using Hexalith.Memories.Contracts.V1;

internal static class EvidenceResourceKeys
{
    public const string CockpitLabel = "Evidence_Cockpit_Label";
    public const string ScopeLabel = "Evidence_Scope_Label";
    public const string TenantLabel = "Evidence_Tenant_Label";
    public const string CaseLabel = "Evidence_Case_Label";
    public const string ScopeColumn = "Evidence_Scope_Column";
    public const string TrustLabel = "Evidence_Trust_Label";
    public const string ConfidenceColumn = "Evidence_Confidence_Column";
    public const string FreshnessColumn = "Evidence_Freshness_Column";
    public const string SourceCountLabel = "Evidence_SourceCount_Label";
    public const string HealthColumn = "Evidence_Health_Column";
    public const string TokenBudgetColumn = "Evidence_TokenBudget_Column";
    public const string ResultSection = "Evidence_Result_Section";
    public const string RecoverySection = "Evidence_Recovery_Section";
    public const string SourcesSection = "Evidence_Sources_Section";
    public const string AxesSection = "Evidence_Axes_Section";
    public const string GraphSection = "Evidence_Graph_Section";
    public const string ResultLabel = "Evidence_Result_Label";
    public const string QueryLabel = "Evidence_Query_Label";
    public const string Loading = "Evidence_Loading";
    public const string Error = "Evidence_Error";
    public const string UnknownTenant = "Evidence_UnknownTenant";
    public const string TenantScope = "Evidence_TenantScope";
    public const string NoQuery = "Evidence_NoQuery";
    public const string SummaryUnavailable = "Evidence_SummaryUnavailable";
    public const string ReasonRedacted = "Evidence_ReasonRedacted";
    public const string Unavailable = "Evidence_Unavailable";
    public const string SourceUnavailable = "Evidence_SourceUnavailable";
    public const string UnknownSourceType = "Evidence_UnknownSourceType";
    public const string SnippetUnavailable = "Evidence_SnippetUnavailable";
    public const string MemoryUnitUnavailable = "Evidence_MemoryUnitUnavailable";
    public const string AxisUnavailable = "Evidence_AxisUnavailable";
    public const string RankingReasonUnavailable = "Evidence_RankingReasonUnavailable";
    public const string NormalizationUnavailable = "Evidence_NormalizationUnavailable";
    public const string NoUnavailableAxes = "Evidence_NoUnavailableAxes";
    public const string NoCaveat = "Evidence_NoCaveat";
    public const string NodeUnavailable = "Evidence_NodeUnavailable";
    public const string NoTraversalPath = "Evidence_NoTraversalPath";
    public const string EdgeTypeUnavailable = "Evidence_EdgeTypeUnavailable";
    public const string NoGapMarkers = "Evidence_NoGapMarkers";
    public const string ScoreUnavailable = "Evidence_ScoreUnavailable";
    public const string TimestampUnavailable = "Evidence_TimestampUnavailable";
    public const string FreshnessUnavailable = "Evidence_FreshnessUnavailable";
    public const string SourcesUnavailable = "Evidence_SourcesUnavailable";
    public const string SourcesRestricted = "Evidence_SourcesRestricted";
    public const string AxesUnavailable = "Evidence_AxesUnavailable";
    public const string AxesRestricted = "Evidence_AxesRestricted";
    public const string GraphUnavailable = "Evidence_GraphUnavailable";
    public const string GraphRestricted = "Evidence_GraphRestricted";
    public const string FreshnessField = "Evidence_Freshness_Field";
    public const string TimestampField = "Evidence_Timestamp_Field";
    public const string ScoreField = "Evidence_Score_Field";
    public const string MemoryUnitField = "Evidence_MemoryUnit_Field";
    public const string NormalizedScoreField = "Evidence_NormalizedScore_Field";
    public const string RankingReasonField = "Evidence_RankingReason_Field";
    public const string NormalizationField = "Evidence_Normalization_Field";
    public const string PathField = "Evidence_Path_Field";
    public const string EdgeTypesField = "Evidence_EdgeTypes_Field";
    public const string GapMarkersField = "Evidence_GapMarkers_Field";
    public const string GraphThen = "Evidence_Graph_Then";
    public const string OrderBasis = "Evidence_OrderBasis";
    public const string PacketOrder = "Evidence_PacketOrder";
    public const string UnavailableAxes = "Evidence_UnavailableAxes";
    public const string SourceCountUnavailable = "Evidence_SourceCountUnavailable";
    public const string SourceCountOne = "Evidence_SourceCountOne";
    public const string SourceCountMany = "Evidence_SourceCountMany";
    public const string FreshnessChecked = "Evidence_FreshnessChecked";
    public const string FreshnessAge = "Evidence_FreshnessAge";
    public const string TimestampValue = "Evidence_TimestampValue";
    public const string ScoreValue = "Evidence_ScoreValue";
    public const string TokenBudgetCompressed = "Evidence_TokenBudgetCompressed";
    public const string TokenBudgetWithin = "Evidence_TokenBudgetWithin";
    public const string LoadingState = "Evidence_LoadingState";
    public const string UnauthorizedBanner = "Evidence_Banner_Unauthorized";
    public const string MissingSourceBanner = "Evidence_Banner_MissingSource";
    public const string RedactedBanner = "Evidence_Banner_Redacted";
    public const string CompressedBanner = "Evidence_Banner_Compressed";
    public const string DegradedBanner = "Evidence_Banner_Degraded";

    public static string State(EvidencePacketState state) => $"Evidence_State_{state}";

    public static string Strength(EvidencePacketEvidenceStrength strength) => $"Evidence_Strength_{strength}";

    public static string Isolation(EvidencePacketIsolationStatus isolation) => $"Evidence_Isolation_{isolation}";

    public static string Freshness(EvidencePacketFreshnessState freshness) => $"Evidence_Freshness_{freshness}";

    public static string SourceType(SourceType sourceType) => $"Evidence_SourceType_{sourceType}";
}
