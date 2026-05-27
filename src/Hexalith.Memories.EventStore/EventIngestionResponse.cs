// <copyright file="EventIngestionResponse.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Response envelope returned by the subscription endpoint. The <see cref="InstanceId"/> is
/// populated only when the event has been accepted and a workflow instance was actually scheduled — drops,
/// duplicates, and validation failures must not invent a workflow id (AC #13).</summary>
/// <param name="Status">Status string: <c>accepted</c>, <c>duplicate</c>, <c>unknown-source</c>, etc.</param>
/// <param name="InstanceId">Workflow instance id (present only when <see cref="Status"/> = <c>accepted</c>).</param>
/// <param name="WasDuplicate">Convenience flag to help downstream diagnostics; <c>true</c> iff the event was a duplicate.</param>
/// <param name="Reason">Optional human-readable reason for the response (drops, validation failures).</param>
public sealed record EventIngestionResponse(
    string Status,
    string? InstanceId,
    bool WasDuplicate,
    string? Reason)
{
    public const string StatusAccepted = "accepted";
    public const string StatusDuplicate = "duplicate";
    public const string StatusUnknownSource = "unknown-source";
    public const string StatusTenantNotFound = "tenant-not-found";
    public const string StatusTenantDeleting = "tenant-deleting";
    public const string StatusAutoCreateDisabled = "auto-create-disabled";
    public const string StatusCaseCapExceeded = "case-cap-exceeded";
    public const string StatusInvalidCloudEvent = "invalid-cloudevent";

    public static EventIngestionResponse Accepted(string instanceId)
        => new(StatusAccepted, instanceId, WasDuplicate: false, Reason: null);

    public static EventIngestionResponse Duplicate()
        => new(StatusDuplicate, InstanceId: null, WasDuplicate: true, Reason: null);

    public static EventIngestionResponse Drop(string status, string? reason = null)
        => new(status, InstanceId: null, WasDuplicate: false, reason);

    public static EventIngestionResponse Invalid(string reason)
        => new(StatusInvalidCloudEvent, InstanceId: null, WasDuplicate: false, reason);
}
