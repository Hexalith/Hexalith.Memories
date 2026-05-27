// <copyright file="CloudEventEnvelope.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Text.Json;

/// <summary>Internal CloudEvents 1.0 envelope view parsed from the subscription request body.
/// Contains only the envelope fields Story 9.1 needs; publisher-specific extension attributes
/// are read directly from the underlying <see cref="Data"/> or adjacent raw JSON when required.</summary>
/// <param name="Id">CloudEvents <c>id</c> — globally unique identifier for the event (required).</param>
/// <param name="Source">CloudEvents <c>source</c> — publisher-supplied URI-reference (required).</param>
/// <param name="Type">CloudEvents <c>type</c> — event type (required).</param>
/// <param name="Subject">CloudEvents <c>subject</c> — aggregate identifier; optional per spec.</param>
/// <param name="Time">CloudEvents <c>time</c> — publisher-supplied timestamp (ISO-8601); optional.</param>
/// <param name="DataContentType">CloudEvents <c>datacontenttype</c>; optional. Defaults to <c>application/json</c> when absent.</param>
/// <param name="Data">CloudEvents <c>data</c> — event payload. Required for Story 9.1: a missing or <see cref="JsonValueKind.Undefined"/> data field is a 400.</param>
internal sealed record CloudEventEnvelope(
    string Id,
    string Source,
    string Type,
    string? Subject,
    string? Time,
    string? DataContentType,
    JsonElement Data);
