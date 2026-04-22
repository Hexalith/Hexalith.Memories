// <copyright file="EventIngestionOutcome.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Discrete outcome produced by <see cref="IEventIngestionService"/>. Each outcome maps
/// to a specific HTTP status + response body + DAPR retry posture in <c>EventIngestionController</c>.</summary>
public enum EventIngestionOutcome
{
    /// <summary>Workflow successfully scheduled; response includes the workflow instance id.</summary>
    Accepted,

    /// <summary>Duplicate of a previously-processed event id; no workflow scheduled.</summary>
    Duplicate,

    /// <summary>CloudEvents envelope missing required field (<c>id</c>/<c>source</c>/<c>type</c>/<c>data</c>).</summary>
    InvalidCloudEvent,

    /// <summary>Source matched no entry in the tenant map. Drop with 200 — no DAPR retry.</summary>
    UnknownSource,

    /// <summary>Resolved tenant does not exist. Drop with 200.</summary>
    TenantNotFound,

    /// <summary>Tenant is provisioning. Return 500 so DAPR retries until tenant becomes active.</summary>
    TenantProvisioning,

    /// <summary>Tenant is deleting (or otherwise non-operational). Drop with 200.</summary>
    TenantDeleting,

    /// <summary>Auto-create disabled and no case exists. Drop with 200.</summary>
    AutoCreateDisabled,

    /// <summary>Auto-create enabled but tenant has hit the case cap. Drop with 200.</summary>
    CaseCapExceeded,

    /// <summary>Workflow scheduling failed transiently. Preflight reservation (if any) has been released.</summary>
    ScheduleFailed,
}
