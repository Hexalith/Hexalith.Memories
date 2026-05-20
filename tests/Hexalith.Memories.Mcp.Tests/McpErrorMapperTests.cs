// <copyright file="McpErrorMapperTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using System.Net;
using System.Text.Json;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Mcp;

using ModelContextProtocol.Protocol;

using Shouldly;

public sealed class McpErrorMapperTests
{
    private readonly McpErrorMapper _mapper = new();

    [Fact]
    public void Map_FormatsErrorWithCodeServiceMessageAndSuggestion()
    {
        var error = new ErrorResponse("TENANT_NOT_FOUND", "Tenant 'acme' was not found.", "Run memories tenant list.");
        var ex = new MemoriesRemoteException(HttpStatusCode.NotFound, error);

        CallToolResult result = _mapper.Map(ex, "search_memory");

        result.IsError.ShouldBe(true);
        result.Content.ShouldHaveSingleItem();
        var text = result.Content[0] as TextContentBlock;
        text.ShouldNotBeNull();
        text!.Text.ShouldStartWith("[TENANT_NOT_FOUND] (service=memories-server): Tenant 'acme' was not found. Run memories tenant list.");
    }

    [Fact]
    public void Map_TrimsTrailingSpace_WhenSuggestionIsEmpty()
    {
        var error = new ErrorResponse("INVALID_INPUT", "bad input", string.Empty);
        var ex = new MemoriesRemoteException(HttpStatusCode.BadRequest, error);

        CallToolResult result = _mapper.Map(ex, "search_memory");
        var text = (TextContentBlock)result.Content[0];

        text.Text.ShouldBe("[INVALID_INPUT] (service=memories-server): bad input");
        text.Text.ShouldNotEndWith(" ");
    }

    [Fact]
    public void Map_DefaultsServiceToMemoriesServer_WhenNotProvided()
    {
        var error = new ErrorResponse("X", "msg", "fix");
        var ex = new MemoriesRemoteException(HttpStatusCode.InternalServerError, error);

        CallToolResult resultNoArg = _mapper.Map(ex, "search_memory");
        CallToolResult resultBlank = _mapper.Map(ex, "search_memory", failedService: " ");

        ((TextContentBlock)resultNoArg.Content[0]).Text.ShouldContain("service=memories-server");
        ((TextContentBlock)resultBlank.Content[0]).Text.ShouldContain("service=memories-server");
    }

    [Fact]
    public void Map_AlwaysSetsIsErrorTrue()
    {
        var error = new ErrorResponse("X", "msg", string.Empty);
        var ex = new MemoriesRemoteException(HttpStatusCode.InternalServerError, error);

        CallToolResult result = _mapper.Map(ex, "search_memory");

        result.IsError.ShouldBe(true);
    }

    [Fact]
    public void Map_EmitsBothTextContentBlockAndStructuredContent()
    {
        var error = new ErrorResponse("CASE_NOT_FOUND", "no case", "list cases");
        var ex = new MemoriesRemoteException(HttpStatusCode.NotFound, error);

        CallToolResult result = _mapper.Map(ex, "get_case_info");

        result.Content[0].ShouldBeOfType<TextContentBlock>();
        result.StructuredContent.ShouldNotBeNull();
        JsonElement json = result.StructuredContent!.Value;
        json.GetProperty("code").GetString().ShouldBe("CASE_NOT_FOUND");
        json.GetProperty("service").GetString().ShouldBe("memories-server");
        json.GetProperty("tool").GetString().ShouldBe("get_case_info");
        json.GetProperty("message").GetString().ShouldBe("no case");
        json.GetProperty("suggestion").GetString().ShouldBe("list cases");
    }

    [Fact]
    public void MapGeneric_DoesNotLeakStackTrace()
    {
        Exception thrown = CreateExceptionWithStack();

        CallToolResult result = _mapper.MapGeneric(thrown, "search_memory");
        var text = (TextContentBlock)result.Content[0];

        text.Text.ShouldNotContain("at System.");
        text.Text.ShouldNotContain("---> System.");
        text.Text.ShouldNotContain("StackTrace");
        text.Text.ShouldContain("(service=memories-server)");
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("'; DROP TABLE cases;--")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("abc\0def")]
    public void MapGeneric_DoesNotEchoInputValues(string payload)
    {
        var ex = new InvalidOperationException(payload);

        CallToolResult result = _mapper.MapGeneric(ex, "search_memory");
        var text = (TextContentBlock)result.Content[0];

        text.Text.ShouldNotContain(payload);
    }

    [Fact]
    public void MapGeneric_DoesNotEchoOversizedInput()
    {
        string oversized = new string('X', 12_000);
        var ex = new InvalidOperationException(oversized);

        CallToolResult result = _mapper.MapGeneric(ex, "search_memory");
        var text = (TextContentBlock)result.Content[0];

        text.Text.ShouldNotContain(oversized);
        text.Text.Length.ShouldBeLessThan(1024);
    }

    [Fact]
    public void StructuredContent_ToolField_IsLiteralToolName()
    {
        var error = new ErrorResponse("X", "y", "z");
        var ex = new MemoriesRemoteException(HttpStatusCode.BadRequest, error);

        CallToolResult result = _mapper.Map(ex, toolName: "search_memory");

        result.StructuredContent!.Value.GetProperty("tool").GetString().ShouldBe("search_memory");
    }

    [Fact]
    public void MapGeneric_UsesNetworkErrorCode_ForHttpRequestException()
    {
        var ex = new HttpRequestException("connection refused");

        CallToolResult result = _mapper.MapGeneric(ex, "search_memory");

        result.StructuredContent!.Value.GetProperty("code").GetString().ShouldBe(McpErrorMapper.NetworkErrorCode);
    }

    [Fact]
    public void MapGeneric_UsesInternalErrorCode_ForUnknownException()
    {
        var ex = new InvalidOperationException("boom");

        CallToolResult result = _mapper.MapGeneric(ex, "search_memory");

        result.StructuredContent!.Value.GetProperty("code").GetString().ShouldBe(McpErrorMapper.InternalErrorCode);
    }

    [Fact]
    public void MapAuthorization_Mismatch_ReturnsTenantForbidden()
    {
        CallToolResult result = _mapper.MapAuthorization("tenant-a", "search_memory", McpErrorMapper.TenantForbiddenCode);

        result.IsError.ShouldBe(true);
        result.StructuredContent!.Value.GetProperty("code").GetString().ShouldBe(McpErrorMapper.TenantForbiddenCode);
        ((TextContentBlock)result.Content[0]).Text.ShouldContain("tenant-a");
    }

    [Fact]
    public void MapAuthorization_MalformedTenant_DoesNotEchoInput()
    {
        string poisoned = "tenant-a\u202E";

        CallToolResult result = _mapper.MapAuthorization(poisoned, "search_memory", McpErrorMapper.TenantMalformedCode);

        result.StructuredContent!.Value.GetProperty("code").GetString().ShouldBe(McpErrorMapper.TenantMalformedCode);
        ((TextContentBlock)result.Content[0]).Text.ShouldNotContain(poisoned);
        result.StructuredContent!.Value.GetProperty("message").GetString()!.ShouldNotContain(poisoned);
    }

    [Fact]
    public void MapAuthorization_DoesNotLeakClaimSetInResponseBody()
    {
        CallToolResult result = _mapper.MapAuthorization("tenant-a", "search_memory", McpErrorMapper.TenantForbiddenCode);
        string text = ((TextContentBlock)result.Content[0]).Text;
        string structured = result.StructuredContent!.Value.GetRawText();

        text.ShouldNotContain("email=");
        text.ShouldNotContain("groups=");
        structured.ShouldNotContain("email=");
        structured.ShouldNotContain("groups=");
    }

    [Fact]
    public void MapAuthorization_IncludesUnauthorizedEvidencePacketWithoutExpansionHandles()
    {
        CallToolResult result = _mapper.MapAuthorization("tenant-a", "search_memory", McpErrorMapper.TenantForbiddenCode);

        JsonElement packet = result.StructuredContent!.Value.GetProperty("evidencePacket");
        packet.GetProperty("state").GetString().ShouldBe("unauthorized");
        packet.GetProperty("scope").GetProperty("isolationStatus").GetString().ShouldBe("unauthorized");
        packet.GetProperty("omittedDetails").GetProperty("expansionHandles").GetArrayLength().ShouldBe(0);
        packet.GetProperty("recovery")[0].GetProperty("kind").GetString().ShouldBe("checkAuthorization");
    }

    [Fact]
    public void MapAuthorization_MalformedTenant_ProducesUnauthorizedEvidencePacket()
    {
        // Regression: a malformed tenant id must route to state: unauthorized + checkAuthorization,
        // not state: degraded + retry. The packet must not echo the unsafe input.
        string poisoned = "tenant-a\u202E";

        CallToolResult result = _mapper.MapAuthorization(poisoned, "search_memory", McpErrorMapper.TenantMalformedCode);

        JsonElement packet = result.StructuredContent!.Value.GetProperty("evidencePacket");
        packet.GetProperty("state").GetString().ShouldBe("unauthorized");
        packet.GetProperty("scope").GetProperty("isolationStatus").GetString().ShouldBe("unauthorized");
        packet.GetProperty("recovery")[0].GetProperty("kind").GetString().ShouldBe("checkAuthorization");
        packet.GetProperty("scope").GetProperty("tenantId").GetString()!.ShouldNotContain(poisoned);
    }

    private static Exception CreateExceptionWithStack()
    {
        try
        {
            throw new InvalidOperationException("inner failure");
        }
        catch (InvalidOperationException caught)
        {
            return caught;
        }
    }
}
