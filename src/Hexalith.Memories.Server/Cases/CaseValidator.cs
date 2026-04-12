// <copyright file="CaseValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Cases;

using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;

/// <summary>Validates case creation and member management input and returns structured error responses for API callers.</summary>
internal static partial class CaseValidator
{
    private const int MaxMemberIdLength = 200;

    public static ErrorResponse? ValidateCreateCase(CreateCaseInput input)
    {
        try
        {
            TenantIdGuard.Validate(input.TenantId);
        }
        catch (ArgumentException)
        {
            return new ErrorResponse(
                "INVALID_TENANT_ID",
                "TenantId contains invalid characters.",
                "Only alphanumeric and hyphens allowed.");
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return new ErrorResponse(
                "INVALID_CASE_NAME",
                "Name is required.",
                "Name is required, max 200 characters.");
        }

        if (input.Name.Length > 200)
        {
            return new ErrorResponse(
                "INVALID_CASE_NAME",
                "Name must not exceed 200 characters.",
                "Name is required, max 200 characters.");
        }

        if (input.Description is not null && input.Description.Length > 2000)
        {
            return new ErrorResponse(
                "INVALID_CASE_DESCRIPTION",
                "Description must not exceed 2000 characters.",
                "Description must not exceed 2000 characters.");
        }

        return null;
    }

    /// <summary>Validates input for adding a member to a case.</summary>
    /// <param name="tenantId">The tenant identifier from the route.</param>
    /// <param name="caseId">The case identifier from the route.</param>
    /// <param name="input">The add-member payload.</param>
    /// <returns>An <see cref="ErrorResponse"/> if validation fails; otherwise <see langword="null"/>.</returns>
    public static ErrorResponse? ValidateAddMember(string tenantId, string caseId, AddCaseMemberInput input)
    {
        ErrorResponse? tenantError = ValidateTenantId(tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        ErrorResponse? caseIdError = ValidateCaseId(caseId);
        if (caseIdError is not null)
        {
            return caseIdError;
        }

        return ValidateMemberId(input.MemberId);
    }

    /// <summary>Validates input for removing a member from a case.</summary>
    /// <param name="tenantId">The tenant identifier from the route.</param>
    /// <param name="caseId">The case identifier from the route.</param>
    /// <param name="memberId">The member identifier from the route.</param>
    /// <returns>An <see cref="ErrorResponse"/> if validation fails; otherwise <see langword="null"/>.</returns>
    public static ErrorResponse? ValidateRemoveMember(string tenantId, string caseId, string memberId)
    {
        ErrorResponse? tenantError = ValidateTenantId(tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        ErrorResponse? caseIdError = ValidateCaseId(caseId);
        if (caseIdError is not null)
        {
            return caseIdError;
        }

        return ValidateMemberId(memberId);
    }

    /// <summary>Validates a case identifier for safe use in Redis key construction.</summary>
    /// <param name="caseId">The case identifier to validate.</param>
    /// <returns>An <see cref="ErrorResponse"/> if validation fails; otherwise <see langword="null"/>.</returns>
    public static ErrorResponse? ValidateCaseId(string caseId)
    {
        if (string.IsNullOrWhiteSpace(caseId) || !SafeCaseIdRegex().IsMatch(caseId))
        {
            return new ErrorResponse(
                "INVALID_CASE_ID",
                "CaseId must be alphanumeric with hyphens only.",
                "Only alphanumeric characters and hyphens are allowed in case identifiers.");
        }

        return null;
    }

    private static ErrorResponse? ValidateTenantId(string tenantId)
    {
        try
        {
            TenantIdGuard.Validate(tenantId);
        }
        catch (ArgumentException)
        {
            return new ErrorResponse(
                "INVALID_TENANT_ID",
                "TenantId contains invalid characters.",
                "Only alphanumeric and hyphens allowed.");
        }

        return null;
    }

    private static ErrorResponse? ValidateMemberId(string memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return new ErrorResponse(
                "INVALID_MEMBER_ID",
                "MemberId is required.",
                "MemberId must be alphanumeric with hyphens, dots, and underscores only, max 200 characters.");
        }

        if (memberId.Length > MaxMemberIdLength)
        {
            return new ErrorResponse(
                "INVALID_MEMBER_ID",
                $"MemberId must not exceed {MaxMemberIdLength} characters.",
                "MemberId must be alphanumeric with hyphens, dots, and underscores only, max 200 characters.");
        }

        if (!SafeMemberIdRegex().IsMatch(memberId))
        {
            return new ErrorResponse(
                "INVALID_MEMBER_ID",
                "MemberId contains invalid characters.",
                "MemberId must be alphanumeric with hyphens, dots, and underscores only, max 200 characters.");
        }

        return null;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9\-]+$")]
    private static partial Regex SafeCaseIdRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9\-._]+$")]
    private static partial Regex SafeMemberIdRegex();
}
