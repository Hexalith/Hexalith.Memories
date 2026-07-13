// <copyright file="ImportRequestValidatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Import;

using System;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Import;

using Shouldly;

/// <summary>Story 26.2 (AC1) — Docker-free coverage for import manifest validation.</summary>
public class ImportRequestValidatorTests
{
    private static ExportManifest Manifest(int schemaVersion, ExportScope scope, string tenantId, string? caseId)
        => new(schemaVersion, scope, tenantId, caseId, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

    [Fact]
    public void Validate_MatchingTenantExport_ReturnsNull()
    {
        ErrorResponse? error = ImportRequestValidator.Validate(
            Manifest(1, ExportScope.Tenant, "acme", null),
            ExportScope.Tenant,
            "acme",
            null);

        error.ShouldBeNull();
    }

    [Fact]
    public void Validate_MatchingCaseExport_ReturnsNull()
    {
        ErrorResponse? error = ImportRequestValidator.Validate(
            Manifest(1, ExportScope.Case, "acme", "case-1"),
            ExportScope.Case,
            "acme",
            "case-1");

        error.ShouldBeNull();
    }

    [Fact]
    public void Validate_UnsupportedSchemaVersion_ReturnsError()
    {
        ErrorResponse? error = ImportRequestValidator.Validate(
            Manifest(2, ExportScope.Tenant, "acme", null),
            ExportScope.Tenant,
            "acme",
            null);

        error.ShouldNotBeNull();
        error.Code.ShouldBe("IMPORT_SCHEMA_VERSION_UNSUPPORTED");
    }

    [Fact]
    public void Validate_TenantJsonPostedToCaseRoute_ReturnsScopeMismatch()
    {
        ErrorResponse? error = ImportRequestValidator.Validate(
            Manifest(1, ExportScope.Tenant, "acme", null),
            ExportScope.Case,
            "acme",
            "case-1");

        error.ShouldNotBeNull();
        error.Code.ShouldBe("IMPORT_SCOPE_MISMATCH");
    }

    [Fact]
    public void Validate_CaseJsonPostedToTenantRoute_ReturnsScopeMismatch()
    {
        ErrorResponse? error = ImportRequestValidator.Validate(
            Manifest(1, ExportScope.Case, "acme", "case-1"),
            ExportScope.Tenant,
            "acme",
            null);

        error.ShouldNotBeNull();
        error.Code.ShouldBe("IMPORT_SCOPE_MISMATCH");
    }

    [Fact]
    public void Validate_DifferentTargetTenant_ReturnsTenantMismatch()
    {
        ErrorResponse? error = ImportRequestValidator.Validate(
            Manifest(1, ExportScope.Tenant, "acme", null),
            ExportScope.Tenant,
            "other-tenant",
            null);

        error.ShouldNotBeNull();
        error.Code.ShouldBe("IMPORT_TENANT_MISMATCH");
    }

    [Fact]
    public void Validate_DifferentTargetCase_ReturnsCaseMismatch()
    {
        ErrorResponse? error = ImportRequestValidator.Validate(
            Manifest(1, ExportScope.Case, "acme", "case-1"),
            ExportScope.Case,
            "acme",
            "case-2");

        error.ShouldNotBeNull();
        error.Code.ShouldBe("IMPORT_CASE_MISMATCH");
    }
}
