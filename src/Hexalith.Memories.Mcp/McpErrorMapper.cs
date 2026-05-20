// <copyright file="McpErrorMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using ModelContextProtocol.Protocol;

/// <summary>
/// Story 10.1 — single translation point that converts <see cref="MemoriesRemoteException"/>
/// (and any unexpected failure) into the MCP-protocol-correct
/// <see cref="CallToolResult"/> with <see cref="CallToolResult.IsError"/> set to <c>true</c>.
/// </summary>
internal sealed partial class McpErrorMapper
{
    /// <summary>The default <c>failedService</c> identifier.</summary>
    internal const string DefaultFailedService = "memories-server";

    /// <summary>Generic error code for transport / network failures.</summary>
    internal const string NetworkErrorCode = "NETWORK_ERROR";

    /// <summary>Generic error code for unhandled tool failures.</summary>
    internal const string InternalErrorCode = "INTERNAL_ERROR";

    /// <summary>Sanitized message used in place of <c>ex.Message</c> when caller input may have leaked into it.</summary>
    internal const string SanitizedFailureMessage = "Tool execution failed before reaching the upstream service.";

    /// <summary>Error code returned when a token is not authorized for the requested tenant.</summary>
    internal const string TenantForbiddenCode = "TENANT_FORBIDDEN";

    /// <summary>Error code returned when a tenant id is malformed and unsafe to echo.</summary>
    internal const string TenantMalformedCode = "TENANT_MALFORMED";

    /// <summary>
    /// Maps a <see cref="MemoriesRemoteException"/> raised by <see cref="MemoriesClient"/> into a
    /// structured tool error result.
    /// </summary>
    /// <param name="exception">The remote exception.</param>
    /// <param name="toolName">The MCP tool that observed the failure (used in <c>StructuredContent.tool</c>).</param>
    /// <param name="failedService">The service identifier reported to the LLM client; defaults to <c>memories-server</c>.</param>
    /// <returns>A tool result with <see cref="CallToolResult.IsError"/> set.</returns>
    public CallToolResult Map(MemoriesRemoteException exception, string toolName, string? failedService = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        string service = NormalizeService(failedService);
        ErrorResponse error = exception.Error ?? new ErrorResponse(
            "UNKNOWN",
            "The server returned an error without a structured envelope.",
            "Check server logs for details.");

        return BuildErrorResult(error, service, toolName, EvidencePacketMapper.FromError(error, UnknownScope()));
    }

    /// <summary>
    /// Maps any other exception into a structured tool error result. The mapper sanitizes the
    /// message so caller-supplied input (path traversal strings, SQL fragments, oversized payloads)
    /// never leaks into the LLM-facing text. Stack traces are dropped.
    /// </summary>
    /// <param name="exception">The exception to wrap.</param>
    /// <param name="toolName">The MCP tool that observed the failure.</param>
    /// <returns>A tool result with <see cref="CallToolResult.IsError"/> set.</returns>
    public CallToolResult MapGeneric(Exception exception, string toolName)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        string code = exception is HttpRequestException or TaskCanceledException
            ? NetworkErrorCode
            : InternalErrorCode;

        var sanitized = new ErrorResponse(
            code,
            SanitizedFailureMessage,
            "Inspect the MCP server logs for diagnostic context. The original message is intentionally not echoed to the LLM.");

        return BuildErrorResult(sanitized, DefaultFailedService, toolName, EvidencePacketMapper.FromError(sanitized, UnknownScope()));
    }

    /// <summary>
    /// Builds a structured client-side validation error result without crossing the wire. Used by
    /// tool methods to short-circuit malformed parameter values (e.g. unknown <c>edge_type</c>).
    /// </summary>
    /// <param name="code">The structured error code surfaced to the LLM.</param>
    /// <param name="message">A human readable description of the failure.</param>
    /// <param name="suggestion">A recovery suggestion / hint for the LLM.</param>
    /// <param name="toolName">The MCP tool that produced the rejection.</param>
    /// <returns>A tool result with <see cref="CallToolResult.IsError"/> set.</returns>
    public CallToolResult MapValidation(string code, string message, string suggestion, string toolName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        ErrorResponse error = new(code, message, suggestion ?? string.Empty);
        return BuildErrorResult(error, toolName, toolName, EvidencePacketMapper.FromError(error, UnknownScope()));
    }

    /// <summary>
    /// Maps MCP tenant-claim authorization failures into the standard structured tool error shape.
    /// Malformed tenant identifiers are rejected with a fixed message that does not echo the input.
    /// </summary>
    /// <param name="tenantId">The requested tenant identifier.</param>
    /// <param name="toolName">The MCP tool that rejected the call.</param>
    /// <param name="reasonCode">The authorization reason code.</param>
    /// <returns>A structured authorization error.</returns>
    public CallToolResult MapAuthorization(string tenantId, string toolName, string reasonCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        string requestedTenantId = tenantId ?? string.Empty;
        if (!TenantIdRegex().IsMatch(requestedTenantId) || string.Equals(reasonCode, TenantMalformedCode, StringComparison.Ordinal))
        {
            ErrorResponse malformed = new(
                    TenantMalformedCode,
                    "The requested tenant identifier is malformed.",
                    "Use a tenant identifier containing only letters, digits, underscores, or dashes.");
            return BuildErrorResult(
                malformed,
                "mcp-auth",
                toolName,
                EvidencePacketMapper.FromError(malformed, UnknownScope()));
        }

        ErrorResponse forbidden = new(
            TenantForbiddenCode,
            $"The bearer token is not authorized for tenant '{requestedTenantId}'.",
            "Use a bearer token with a matching tenant claim, or request a tenant that is present in the token.");
        EvidencePacketScope scope = new(requestedTenantId, null, EvidencePacketIsolationStatus.Authorized, "mcp-auth");
        return BuildErrorResult(
            forbidden,
            "mcp-auth",
            toolName,
            EvidencePacketMapper.FromError(forbidden, scope));
    }

    /// <summary>Formats an <see cref="ErrorResponse"/> as the LLM-facing prose line.</summary>
    /// <param name="error">The error envelope.</param>
    /// <param name="failedService">The service identifier.</param>
    /// <returns>The formatted text.</returns>
    public static string FormatError(ErrorResponse error, string failedService)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentException.ThrowIfNullOrWhiteSpace(failedService);

        string suggestion = string.IsNullOrWhiteSpace(error.Suggestion) ? string.Empty : error.Suggestion;
        return $"[{error.Code}] (service={failedService}): {error.Message} {suggestion}".TrimEnd();
    }

    private static CallToolResult BuildErrorResult(ErrorResponse error, string service, string toolName, EvidencePacket? evidencePacket = null)
    {
        var structured = new McpErrorPayload(
            error.Code,
            service,
            toolName,
            error.Message,
            error.Suggestion ?? string.Empty,
            evidencePacket);

        JsonElement structuredElement = JsonSerializer.SerializeToElement(structured, MemoriesJsonContext.Options);

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = FormatError(error, service) }],
            StructuredContent = structuredElement,
            IsError = true,
        };
    }

    private static string NormalizeService(string? value)
        => string.IsNullOrWhiteSpace(value) ? DefaultFailedService : value;

    private static EvidencePacketScope UnknownScope()
        => new("unknown", null, EvidencePacketIsolationStatus.Unknown, "mcp-error");

    private sealed record McpErrorPayload(
        string Code,
        string Service,
        string Tool,
        string Message,
        string Suggestion,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EvidencePacket? EvidencePacket);

    [GeneratedRegex("^[A-Za-z0-9_-]{1,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex TenantIdRegex();
}
