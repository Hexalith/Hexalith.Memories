// <copyright file="IEventIngestionService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Text.Json;

/// <summary>Orchestrates a single CloudEvents subscription request: parses the envelope, resolves routing,
/// runs the preflight dedup reservation, schedules the existing ingestion workflow, and compensates if
/// scheduling fails after reservation. The implementation type (<see cref="EventIngestionService"/>) is
/// internal; the interface is public so <see cref="EventIngestionController"/> can resolve it through DI.</summary>
public interface IEventIngestionService
{
    /// <summary>Processes one subscription request body.</summary>
    /// <param name="envelopeJson">The raw CloudEvents envelope JSON read from the request body.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The typed outcome plus the <see cref="EventIngestionResponse"/> the controller will serialize.</returns>
    Task<EventIngestionProcessResult> ProcessAsync(JsonElement envelopeJson, CancellationToken cancellationToken);
}

/// <summary>Combined outcome + response produced by <see cref="IEventIngestionService.ProcessAsync"/>. The
/// controller uses <see cref="Outcome"/> to pick the HTTP status and <see cref="Response"/> as the JSON body.</summary>
/// <param name="Outcome">The typed <see cref="EventIngestionOutcome"/>.</param>
/// <param name="Response">The user-facing response envelope (safe to serialize).</param>
public sealed record EventIngestionProcessResult(
    EventIngestionOutcome Outcome,
    EventIngestionResponse Response);
