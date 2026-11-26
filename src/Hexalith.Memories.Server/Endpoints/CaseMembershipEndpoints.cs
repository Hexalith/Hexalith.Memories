// <copyright file="CaseMembershipEndpoints.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Cases;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Telemetry;

using static Hexalith.Memories.Server.Endpoints.EndpointTelemetryHelpers;

/// <summary>Maps the case membership management routes.</summary>
internal static class CaseMembershipEndpoints
{
    /// <summary>Registers the add, remove, and list case member endpoints.</summary>
    internal static void MapCaseMembershipEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut(MemoriesRoutes.CaseMember, async (
            string tenantId,
            string caseId,
            string memberId,
            JsonElement requestBody,
            CaseService caseService,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                MemoriesActivitySource.CaseMember,
                AccessTelemetryLog.OperationCaseMember,
                successEventId: 7508,
                errorEventId: 7518,
                tenantId,
                caseId,
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["operation"] = "case-member-add",
                    ["memberIdPrefix"] = PrefixIdentifier(memberId, 32),
                });

            try
            {
                ErrorResponse? bodyError = TryDeserializeAddCaseMemberInput(requestBody, out AddCaseMemberInput? input);
                if (bodyError is not null)
                {
                    scope.MarkValidationError(bodyError.Code);
                    return Results.BadRequest(bodyError);
                }

                AddCaseMemberInput validatedInput = input! with { MemberId = memberId };
                ErrorResponse? error = CaseValidator.ValidateAddMember(tenantId, caseId, validatedInput);
                if (error is not null)
                {
                    scope.MarkValidationError(error.Code);
                    return Results.BadRequest(error);
                }

                Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
                if (caseResult is null)
                {
                    scope.MarkValidationError("CASE_NOT_FOUND");
                    return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
                }

                try
                {
                    (CaseMember member, bool created) = await caseService.AddMemberAsync(tenantId, caseId, validatedInput, cancellationToken);
                    return created
                        ? Results.Created(MemoriesRoutes.CaseMemberLocation(tenantId, caseId, memberId), member)
                        : Results.Ok(member);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("maximum"))
                {
                    scope.MarkValidationError("MEMBER_LIMIT_EXCEEDED");
                    return Results.BadRequest(new ErrorResponse("MEMBER_LIMIT_EXCEEDED", ex.Message, "Remove existing members before adding new ones."));
                }
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        app.MapDelete(MemoriesRoutes.CaseMember, async (
            string tenantId,
            string caseId,
            string memberId,
            CaseService caseService,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                MemoriesActivitySource.CaseMember,
                AccessTelemetryLog.OperationCaseMember,
                successEventId: 7508,
                errorEventId: 7518,
                tenantId,
                caseId,
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["operation"] = "case-member-remove",
                    ["memberIdPrefix"] = PrefixIdentifier(memberId, 32),
                });

            try
            {
                ErrorResponse? error = CaseValidator.ValidateRemoveMember(tenantId, caseId, memberId);
                if (error is not null)
                {
                    scope.MarkValidationError(error.Code);
                    return Results.BadRequest(error);
                }

                Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
                if (caseResult is null)
                {
                    scope.MarkValidationError("CASE_NOT_FOUND");
                    return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
                }

                bool removed = await caseService.RemoveMemberAsync(tenantId, caseId, memberId, cancellationToken);
                if (!removed)
                {
                    scope.MarkValidationError("MEMBER_NOT_FOUND");
                }

                return removed
                    ? Results.NoContent()
                    : Results.NotFound(new ErrorResponse("MEMBER_NOT_FOUND", $"Member '{memberId}' is not in case '{caseId}'.", "Run GET /cases/{caseId}/members to see current members."));
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        app.MapGet(MemoriesRoutes.CaseMembers, async (
            string tenantId,
            string caseId,
            CaseService caseService,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? caseIdError = CaseValidator.ValidateCaseId(caseId);
            if (caseIdError is not null)
            {
                return Results.BadRequest(caseIdError);
            }

            try
            {
                TenantIdGuard.Validate(tenantId);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
            }

            Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            if (caseResult is null)
            {
                return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
            }

            List<CaseMember> members = await caseService.ListMembersAsync(tenantId, caseId, cancellationToken);
            return Results.Ok(members);
        });
    }

    private static ErrorResponse? TryDeserializeAddCaseMemberInput(JsonElement requestBody, out AddCaseMemberInput? input)
    {
        input = null;

        if (requestBody.ValueKind != JsonValueKind.Object)
        {
            return new ErrorResponse(
                "INVALID_MEMBER_INPUT",
                "Request body must be a JSON object.",
                "Provide a JSON object with memberType set to 'user' or 'role'.");
        }

        if (!TryGetJsonPropertyIgnoreCase(requestBody, "memberType", out JsonElement memberTypeElement) ||
            memberTypeElement.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(memberTypeElement.GetString()))
        {
            return new ErrorResponse(
                "INVALID_MEMBER_TYPE",
                "MemberType is required.",
                "Provide memberType as 'user' or 'role'.");
        }

        try
        {
            input = JsonSerializer.Deserialize<AddCaseMemberInput>(requestBody.GetRawText(), MemoriesJsonContext.Options);
        }
        catch (JsonException ex)
        {
            return new ErrorResponse(
                "INVALID_MEMBER_TYPE",
                ex.Message,
                "Provide memberType as 'user' or 'role'.");
        }

        return input is null
            ? new ErrorResponse(
                "INVALID_MEMBER_INPUT",
                "Request body is required.",
                "Provide a JSON object with memberType set to 'user' or 'role'.")
            : null;
    }

    private static bool TryGetJsonPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
