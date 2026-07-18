// <copyright file="AccessTelemetryRecordState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Bounded values for the lifecycle record counter.</summary>
public enum AccessTelemetryRecordState
{
    /// <summary>Accepted by the provider.</summary>
    Accepted,

    /// <summary>Rejected before admission.</summary>
    Rejected,

    /// <summary>Admitted to a Server queue.</summary>
    Enqueued,

    /// <summary>Persisted by the lifecycle authority.</summary>
    Persisted,

    /// <summary>Retried after a transient failure.</summary>
    Retried,

    /// <summary>Failed terminally.</summary>
    Failed,

    /// <summary>Dropped under a bounded policy.</summary>
    Dropped,

    /// <summary>Logically expired.</summary>
    Expired,

    /// <summary>Logically purged.</summary>
    Purged,
}
