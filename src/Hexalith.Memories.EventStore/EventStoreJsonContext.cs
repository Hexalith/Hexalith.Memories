// <copyright file="EventStoreJsonContext.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Text.Json.Serialization;

/// <summary>Source-generated JSON metadata for the EventStore package's public DTOs. Kept in the
/// EventStore project (rather than <c>Hexalith.Memories.Contracts.V1.MemoriesJsonContext</c>) because
/// Contracts must not take a project reference back to EventStore — the architecture chain is
/// EventStore → Contracts, one-way.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EventIngestionResponse))]
[JsonSerializable(typeof(TenantEventRoute))]
[JsonSerializable(typeof(TenantEventRoutingOptions))]
[JsonSerializable(typeof(TenantEventRouteResolution))]
[JsonSerializable(typeof(TenantEventRouteResolutionStatus))]
[JsonSerializable(typeof(EventIngestionOutcome))]
[JsonSerializable(typeof(EventIngestionProcessResult))]
[JsonSerializable(typeof(NormalizedCloudEventEnvelope))]
public sealed partial class EventStoreJsonContext : JsonSerializerContext;
