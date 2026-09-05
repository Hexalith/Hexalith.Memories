// <copyright file="AccessTelemetryQualificationWorkloadResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

/// <summary>Returns only fixed-workload aggregate accounting to the host-side verifier.</summary>
/// <param name="Writer">The bounded Server-writer ordinal derived from the pod annotation.</param>
/// <param name="Attempted">The attempted fixed-workload record count.</param>
/// <param name="Acknowledged">The lifecycle-service acknowledged record count.</param>
/// <param name="Persisted">The durable transaction acknowledgement count.</param>
/// <param name="Conflicted">The record-ID conflict count.</param>
/// <param name="TransactionAcknowledgements">The exact successful transaction acknowledgement count.</param>
/// <param name="Dropped">The bounded drop count.</param>
/// <param name="Rejected">The non-conflict rejection count.</param>
/// <param name="ResultCount">The nonzero aggregate observation count.</param>
internal sealed record AccessTelemetryQualificationWorkloadResult(
    string Writer,
    long Attempted,
    long Acknowledged,
    long Persisted,
    long Conflicted,
    long TransactionAcknowledgements,
    long Dropped,
    long Rejected,
    long ResultCount);
