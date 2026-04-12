// <copyright file="CaseValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Cases;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;

/// <summary>Validates case creation input and returns structured error responses for API callers.</summary>
internal static class CaseValidator
{
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
}
