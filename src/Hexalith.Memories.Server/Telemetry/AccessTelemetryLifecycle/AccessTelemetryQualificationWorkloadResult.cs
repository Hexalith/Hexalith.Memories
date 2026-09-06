// <copyright file="AccessTelemetryQualificationWorkloadResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

/// <summary>Returns only fixed-workload aggregate accounting to the host-side verifier.</summary>
/// <param name="RunId">The verifier-generated bounded qualification run identifier.</param>
/// <param name="SegmentId">The verifier-generated canonical one-second segment identifier.</param>
/// <param name="Writer">The bounded Server-writer identity derived from the pod annotation.</param>
/// <param name="StartedUtcMs">The target-observed segment start in UTC milliseconds.</param>
/// <param name="FinishedUtcMs">The target-observed segment finish in UTC milliseconds.</param>
/// <param name="Attempted">The attempted fixed-workload record count.</param>
/// <param name="Enqueued">The records observed entering the bounded delivery queue.</param>
/// <param name="Acknowledged">The lifecycle-service acknowledged record count.</param>
/// <param name="Persisted">The durable transaction acknowledgement count.</param>
/// <param name="Conflicted">The record-ID conflict count.</param>
/// <param name="TransactionAcknowledgements">The exact successful transaction acknowledgement count.</param>
/// <param name="Dropped">The bounded drop count.</param>
/// <param name="Rejected">The non-conflict rejection count.</param>
/// <param name="RecordIds">The exact deterministic record identities emitted by this segment.</param>
/// <param name="ResultCount">The nonzero aggregate observation count.</param>
internal sealed record AccessTelemetryQualificationWorkloadResult(
    string RunId,
    string SegmentId,
    string Writer,
    long StartedUtcMs,
    long FinishedUtcMs,
    long Attempted,
    long Enqueued,
    long Acknowledged,
    long Persisted,
    long Conflicted,
    long TransactionAcknowledgements,
    long Dropped,
    long Rejected,
    IReadOnlyList<string> RecordIds,
    long ResultCount);
