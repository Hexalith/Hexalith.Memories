// <copyright file="EventIngestionController.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>Subscription endpoint that receives raw CloudEvents envelopes from DAPR pub/sub.
///
/// <para>The environment-backed topic metadata attribute subscribes the route to the configured
/// <c>MEMORIES_EVENTSTORE_TOPIC</c> value on the <c>pubsub</c> component. DAPR's canonical
/// <c>/dapr/subscribe</c> discovery probe picks this up at startup via <c>MapSubscribeHandler()</c>.</para>
///
/// <para>The controller delegates all logic to <see cref="IEventIngestionService"/>, which returns the
/// typed <see cref="EventIngestionProcessResult"/> this controller translates to HTTP responses:</para>
/// <list type="bullet">
///   <item><description><see cref="EventIngestionOutcome.Accepted"/> → 200 + <see cref="EventIngestionResponse"/> with instanceId.</description></item>
///   <item><description><see cref="EventIngestionOutcome.Duplicate"/> → 200 + <c>wasDuplicate = true</c>.</description></item>
///   <item><description><see cref="EventIngestionOutcome.InvalidCloudEvent"/> → 400 + <see cref="ErrorResponse"/>. DAPR does NOT retry.</description></item>
///   <item><description><see cref="EventIngestionOutcome.UnknownSource"/> / <see cref="EventIngestionOutcome.AutoCreateDisabled"/> / <see cref="EventIngestionOutcome.CaseCapExceeded"/> → 200 + warning log (intentional drop).</description></item>
///   <item><description><see cref="EventIngestionOutcome.TenantNotFound"/> / <see cref="EventIngestionOutcome.TenantDeleting"/> / <see cref="EventIngestionOutcome.TenantProvisioning"/> / <see cref="EventIngestionOutcome.ScheduleFailed"/> → 500. DAPR retries.</description></item>
/// </list>
/// </summary>
[ApiController]
[Route("events")]
[Consumes("application/json", "application/cloudevents+json")]
public sealed class EventIngestionController : ControllerBase
{
    /// <summary>The DAPR pub/sub component name this subscription binds to. Keep in sync with the
    /// <c>metadata.name</c> of <c>deploy/dapr/components/pubsub.yaml</c> and the AppHost wiring.</summary>
    public const string PubSubName = "pubsub";

    /// <summary>The environment-variable name the subscription metadata attribute resolves at startup.
    /// Operators must set <c>MEMORIES_EVENTSTORE_TOPIC</c> to the topic name configured in
    /// <see cref="TenantEventRoutingOptions.Topic"/>.</summary>
    public const string TopicEnvVar = "MEMORIES_EVENTSTORE_TOPIC";

    private readonly IEventIngestionService _service;

    public EventIngestionController(IEventIngestionService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
    }

    /// <summary>Receives a CloudEvents envelope from DAPR pub/sub.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An HTTP response that drives DAPR's retry / drop behavior.</returns>
    [HttpPost("ingest")]
    [AllowAnonymous]
    [EnvironmentTopic(PubSubName, TopicEnvVar)]
    [ProducesResponseType(typeof(EventIngestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> OnEvent(CancellationToken cancellationToken)
    {
        JsonElement envelope;
        try
        {
            envelope = await ReadRequestBodyAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            return BadRequest(new ErrorResponse(
                Code: "INVALID_CLOUDEVENT",
                Message: ex.Message,
                Suggestion: "Ensure the request body is valid JSON and contains a CloudEvents envelope."));
        }

        JsonElement? capturedEnvelope = HttpContext.Items.TryGetValue(CloudEventEnvelopeCaptureMiddleware.CapturedEnvelopeItemKey, out object? item)
            && item is JsonElement captured
            ? captured
            : null;

        JsonElement normalizedEnvelope = CloudEventRequestNormalizer.Normalize(envelope, Request.Headers, capturedEnvelope);

        EventIngestionProcessResult result = await _service
            .ProcessAsync(normalizedEnvelope, cancellationToken)
            .ConfigureAwait(false);

        return result.Outcome switch
        {
            EventIngestionOutcome.Accepted => Ok(result.Response),
            EventIngestionOutcome.Duplicate => Ok(result.Response),
            EventIngestionOutcome.UnknownSource => Ok(result.Response),
            EventIngestionOutcome.TenantNotFound => StatusCode(StatusCodes.Status500InternalServerError, result.Response),
            EventIngestionOutcome.TenantDeleting => StatusCode(StatusCodes.Status500InternalServerError, result.Response),
            EventIngestionOutcome.AutoCreateDisabled => Ok(result.Response),
            EventIngestionOutcome.CaseCapExceeded => Ok(result.Response),
            EventIngestionOutcome.InvalidCloudEvent => BadRequest(new ErrorResponse(
                Code: "INVALID_CLOUDEVENT",
                Message: result.Response.Reason ?? "CloudEvents envelope is invalid",
                Suggestion: "Ensure id, source, type, and data are present and well-formed.")),
            EventIngestionOutcome.TenantProvisioning => StatusCode(StatusCodes.Status500InternalServerError, result.Response),
            EventIngestionOutcome.ScheduleFailed => StatusCode(StatusCodes.Status500InternalServerError, result.Response),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result.Response),
        };
    }

    private async Task<JsonElement> ReadRequestBodyAsync(CancellationToken cancellationToken)
    {
        using JsonDocument document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return document.RootElement.Clone();
    }
}
