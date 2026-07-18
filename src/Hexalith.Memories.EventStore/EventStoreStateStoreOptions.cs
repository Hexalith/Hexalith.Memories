// <copyright file="EventStoreStateStoreOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Options for the Dapr-state-store-backed EventStore stores
/// (spec-infrastructure-dependency-abstraction — F6, Decision D30 and ADR-IDA-001). Lets the state
/// store component name be configured per environment; defaults to the Aspire/AppHost-provisioned
/// <c>statestore</c> component shared with the rest of the server.</summary>
public sealed class EventStoreStateStoreOptions
{
    /// <summary>Configuration section bound to this options type.</summary>
    public const string SectionName = "EventStoreIntegration:StateStore";

    /// <summary>The Dapr state store component name.</summary>
    public string StateStoreName { get; set; } = "statestore";
}
