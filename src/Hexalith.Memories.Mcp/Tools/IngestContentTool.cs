// <copyright file="IngestContentTool.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tools;

using System.ComponentModel;
using System.Text;

using Hexalith.Memories.Client.Rest;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>Story 10.1 — exposes the ingestion entry point as the MCP <c>ingest_content</c> tool.</summary>
[McpServerToolType]
internal sealed class IngestContentTool
{
    /// <summary>Default MIME content type when the caller omits one.</summary>
    internal const string DefaultContentType = "text/plain";

    /// <summary>Default <c>ingestedBy</c> value when the caller omits one.</summary>
    internal const string DefaultIngestedBy = "mcp";

    /// <summary>Default source URI when the caller omits one.</summary>
    internal const string DefaultSourceUri = "mcp://content";

    private readonly MemoriesClient _client;
    private readonly McpToolExecutor _executor;

    /// <summary>Initializes a new instance of the <see cref="IngestContentTool"/> class.</summary>
    /// <param name="client">The Memories REST client (DAPR-routed).</param>
    /// <param name="executor">The shared MCP tool executor.</param>
    public IngestContentTool(
        MemoriesClient client,
        McpToolExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(executor);
        _client = client;
        _executor = executor;
    }

    /// <summary>The MCP tool method invoked by LLM agents.</summary>
    /// <param name="tenantId">Tenant identifier (required).</param>
    /// <param name="caseId">Case identifier (required).</param>
    /// <param name="content">The content payload (required).</param>
    /// <param name="sourceType">Source type discriminator (default <see cref="McpSourceType.File"/>).</param>
    /// <param name="sourceUri">Optional logical source URI; defaults to <c>mcp://content</c>.</param>
    /// <param name="contentType">Optional MIME content type; defaults to <c>text/plain</c>.</param>
    /// <param name="ingestedBy">Optional submitter identity; defaults to <c>mcp</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An MCP tool result carrying the scheduled workflow instance id.</returns>
    [McpServerTool(Name = "ingest_content")]
    [Description("Ingests a content payload into a tenant's case and returns the scheduled workflow instance id.")]
    public async Task<CallToolResult> IngestAsync(
        [Description("The tenant identifier whose case will receive the content.")]
        string tenantId,
        [Description("The case identifier within the tenant where the content will be stored.")]
        string caseId,
        [Description("The content payload — UTF-8 text for natural-language content, or base64 bytes for binary.")]
        string content,
        [Description("Source type discriminator: file (10.1 default), url (deferred to 10.2), or event (deferred to 10.2).")]
        McpSourceType sourceType = McpSourceType.File,
        [Description("Optional logical source URI recorded with the memory unit (e.g. mcp://chat/<id>); defaults to mcp://content.")]
        string? sourceUri = null,
        [Description("Optional MIME content type; defaults to text/plain.")]
        string? contentType = null,
        [Description("Identifier of the submitter (user or system); defaults to mcp.")]
        string ingestedBy = DefaultIngestedBy,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "ingest_content";

        return await _executor.RunAsync(
            tenantId,
            toolName,
            mapper =>
            {
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    return mapper.MapValidation(
                        "INVALID_INPUT",
                        "tenantId is required.",
                        "Provide a non-empty tenantId.",
                        toolName);
                }

                if (string.IsNullOrWhiteSpace(caseId))
                {
                    return mapper.MapValidation(
                        "INVALID_INPUT",
                        "caseId is required.",
                        "Provide a non-empty caseId.",
                        toolName);
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return mapper.MapValidation(
                        "INVALID_INPUT",
                        "content is required.",
                        "Provide a non-empty content payload.",
                        toolName);
                }

                return sourceType != McpSourceType.File
                    ? mapper.MapValidation(
                        "UNSUPPORTED_SOURCE_TYPE",
                        $"sourceType '{sourceType}' is not yet supported by the MCP ingest_content tool.",
                        "Use sourceType=file in Story 10.1; url and event ingestion ship in Story 10.2.",
                        toolName)
                    : null;
            },
            async (authorizedTenant, token) =>
            {
                byte[] payload = Encoding.UTF8.GetBytes(content);
                string effectiveSourceUri = string.IsNullOrWhiteSpace(sourceUri) ? DefaultSourceUri : sourceUri;
                string effectiveContentType = string.IsNullOrWhiteSpace(contentType) ? DefaultContentType : contentType;
                string effectiveIngestedBy = string.IsNullOrWhiteSpace(ingestedBy) ? DefaultIngestedBy : ingestedBy;

                // Story 18.4: MemoriesClient.IngestAsync graduated out of HXL001 — no suppression needed.
                string instanceId = await _client.IngestAsync(
                    authorizedTenant,
                    caseId,
                    effectiveSourceUri,
                    payload,
                    effectiveContentType,
                    effectiveIngestedBy,
                    metadata: null,
                    token).ConfigureAwait(false);
                return McpToolResultSerializer.Success(new IngestContentResponse(instanceId));
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The success response shape — narrow object so JSON consumers can rely on the field name.</summary>
    /// <param name="WorkflowInstanceId">The scheduled ingestion workflow instance id.</param>
    internal sealed record IngestContentResponse(string WorkflowInstanceId);
}

/// <summary>The MCP-tool-facing source-type enum. Subset of <see cref="Hexalith.Memories.Contracts.V1.SourceType"/>.</summary>
internal enum McpSourceType
{
    /// <summary>The content payload is the file body itself.</summary>
    File,

    /// <summary>The content payload is a URL whose body the server should fetch (10.2 scope).</summary>
    Url,

    /// <summary>The content payload represents a CloudEvent envelope (10.2 scope).</summary>
    Event,
}
