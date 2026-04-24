// <copyright file="TelemetryMetricsRecorder.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry;

using System.Diagnostics;

using Hexalith.Memories.Telemetry;

/// <summary>
/// Uniform metric emission helpers used by endpoint handlers. Keeps tag-key usage consistent with
/// <see cref="MemoriesMeter.MetricTagKeyPolicy"/>. Pinned at the endpoint layer so a new instrumentation
/// site cannot accidentally add a non-pinned tag.
/// </summary>
internal static class TelemetryMetricsRecorder
{
    public static void RecordSearch(string tenantIdTag, string axis, double elapsedMs)
    {
        var tags = new TagList
        {
            { "tenant_id", tenantIdTag },
            { "axis", axis },
        };
        MemoriesMeter.SearchRequests.Add(1, tags);
        MemoriesMeter.SearchDuration.Record(elapsedMs, tags);
    }

    public static void RecordIngestSuccess(string tenantIdTag, long documentCount = 1)
    {
        if (documentCount <= 0)
        {
            return;
        }

        var tags = new TagList { { "tenant_id", tenantIdTag } };
        MemoriesMeter.IngestionDocuments.Add(documentCount, tags);
    }

    public static void RecordIngestFailure(string tenantIdTag, string errorCode)
    {
        var tags = new TagList
        {
            { "tenant_id", tenantIdTag },
            { "error_code", errorCode },
        };
        MemoriesMeter.IngestionFailures.Add(1, tags);
    }

    public static void RecordNaturalLanguageDescriptionDuration(string tenantIdTag, double elapsedMs)
    {
        var tags = new TagList
        {
            { "tenant_id", tenantIdTag },
        };
        MemoriesMeter.NaturalLanguageDescriptionDuration.Record(elapsedMs, tags);
    }
}
