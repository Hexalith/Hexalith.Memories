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
    private const int MaxIngestedByLength = 200;

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

    /// <summary>Validates a memory unit identifier for safe use in Redis key construction.</summary>
    /// <param name="memoryUnitId">The memory unit identifier to validate.</param>
    /// <returns>An <see cref="ErrorResponse"/> if validation fails; otherwise <see langword="null"/>.</returns>
    public static ErrorResponse? ValidateMemoryUnitId(string memoryUnitId)
    {
        if (string.IsNullOrWhiteSpace(memoryUnitId))
        {
            return new ErrorResponse(
                "INVALID_MEMORY_UNIT_ID",
                "MemoryUnitId is required.",
                "Provide a valid memory unit identifier.");
        }

        if (memoryUnitId.Length > 200)
        {
            return new ErrorResponse(
                "INVALID_MEMORY_UNIT_ID",
                "MemoryUnitId must not exceed 200 characters.",
                "Provide a shorter identifier.");
        }

        if (!SafeCaseIdRegex().IsMatch(memoryUnitId))
        {
            return new ErrorResponse(
                "INVALID_MEMORY_UNIT_ID",
                "MemoryUnitId contains invalid characters.",
                "Only alphanumeric characters and hyphens are allowed.");
        }

        return null;
    }

    /// <summary>Validates input for deleting a memory unit from a case.</summary>
    /// <param name="tenantId">The tenant identifier from the route.</param>
    /// <param name="caseId">The case identifier from the route.</param>
    /// <param name="memoryUnitId">The memory unit identifier from the route.</param>
    /// <returns>An <see cref="ErrorResponse"/> if validation fails; otherwise <see langword="null"/>.</returns>
    public static ErrorResponse? ValidateDeleteMemoryUnit(string tenantId, string caseId, string memoryUnitId)
    {
        ErrorResponse? tenantError = ValidateTenantId(tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        ErrorResponse? caseError = ValidateCaseId(caseId);
        if (caseError is not null)
        {
            return caseError;
        }

        return ValidateMemoryUnitId(memoryUnitId);
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

    public static ErrorResponse? ValidateTenantId(string tenantId)
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

    private static readonly HashSet<string> AllowedAnnotationTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "correction",
        "clarification",
        "enrichment",
    };

    /// <summary>Validates input for creating an annotation on a memory unit.</summary>
    /// <param name="tenantId">The tenant identifier from the route.</param>
    /// <param name="caseId">The case identifier from the route.</param>
    /// <param name="targetMemoryUnitId">The target memory unit identifier from the route.</param>
    /// <param name="input">The annotation creation payload.</param>
    /// <returns>An <see cref="ErrorResponse"/> if validation fails; otherwise <see langword="null"/>.</returns>
    public static ErrorResponse? ValidateCreateAnnotation(string tenantId, string caseId, string targetMemoryUnitId, CreateAnnotationInput input)
    {
        ErrorResponse? tenantError = ValidateTenantId(tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        ErrorResponse? caseError = ValidateCaseId(caseId);
        if (caseError is not null)
        {
            return caseError;
        }

        ErrorResponse? muError = ValidateMemoryUnitId(targetMemoryUnitId);
        if (muError is not null)
        {
            return muError;
        }

        if (string.IsNullOrWhiteSpace(input.Content))
        {
            return new ErrorResponse(
                "INVALID_ANNOTATION_CONTENT",
                "Annotation content is required.",
                "Provide non-empty content for the annotation.");
        }

        if (input.Content.Length > 50000)
        {
            return new ErrorResponse(
                "INVALID_ANNOTATION_CONTENT",
                "Annotation content must not exceed 50000 characters.",
                "Reduce the content length to 50000 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(input.IngestedBy))
        {
            return new ErrorResponse(
                "INVALID_INGESTED_BY",
                "IngestedBy is required.",
                "Provide a non-empty ingestedBy value.");
        }

        if (input.IngestedBy.Length > MaxIngestedByLength)
        {
            return new ErrorResponse(
                "INVALID_INGESTED_BY",
                $"IngestedBy must not exceed {MaxIngestedByLength} characters.",
                "Provide a shorter ingestedBy value.");
        }

        if (input.AnnotationType is not null && !AllowedAnnotationTypes.Contains(input.AnnotationType))
        {
            return new ErrorResponse(
                "INVALID_ANNOTATION_TYPE",
                $"Annotation type '{input.AnnotationType}' is not recognized.",
                "Valid values: correction, clarification, enrichment.");
        }

        return null;
    }

    /// <summary>Validates that a target memory unit is not itself an annotation (prevents nested annotations).</summary>
    /// <param name="metadata">The metadata dictionary of the target memory unit.</param>
    /// <returns>An <see cref="ErrorResponse"/> if the target is an annotation; otherwise <see langword="null"/>.</returns>
    public static ErrorResponse? ValidateNotNestedAnnotation(IDictionary<string, MetadataField>? metadata)
    {
        if (metadata is not null && metadata.ContainsKey("_system.annotation_target"))
        {
            return new ErrorResponse(
                "NESTED_ANNOTATION_NOT_ALLOWED",
                "Cannot annotate an annotation. The target memory unit is itself an annotation.",
                "Annotate the original memory unit instead.");
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
