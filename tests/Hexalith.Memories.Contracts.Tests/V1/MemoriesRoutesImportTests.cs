// <copyright file="MemoriesRoutesImportTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Story 26.2 — coverage for the additive import/restore route templates and builders.</summary>
public class MemoriesRoutesImportTests
{
    [Fact]
    public void ImportTemplates_MatchTheExpectedServerTemplates()
    {
        MemoriesRoutes.TenantImport.ShouldBe("/api/v1/tenants/{tenantId}/import");
        MemoriesRoutes.CaseImport.ShouldBe("/api/v1/tenants/{tenantId}/cases/{caseId}/import");
        MemoriesRoutes.RestoreStatus.ShouldBe("/api/v1/tenants/{tenantId}/restore/{instanceId}");
    }

    [Fact]
    public void TenantImportPath_ReturnsRelativeEscapedPath()
        => MemoriesRoutes.TenantImportPath("acme").ShouldBe("api/v1/tenants/acme/import");

    [Fact]
    public void CaseImportPath_ReturnsRelativeEscapedPath()
        => MemoriesRoutes.CaseImportPath("acme", "case 1").ShouldBe("api/v1/tenants/acme/cases/case%201/import");

    [Fact]
    public void RestoreStatusLocation_ReturnsAbsoluteEscapedPath()
        => MemoriesRoutes.RestoreStatusLocation("acme", "instance-1").ShouldBe("/api/v1/tenants/acme/restore/instance-1");

    [Fact]
    public void RestoreStatusPath_ReturnsRelativeEscapedPath()
        => MemoriesRoutes.RestoreStatusPath("acme", "instance 1").ShouldBe("api/v1/tenants/acme/restore/instance%201");

    [Fact]
    public void ImportPathBuilders_RejectDotSegments()
    {
        Should.Throw<ArgumentException>(() => MemoriesRoutes.TenantImportPath(".."));
        Should.Throw<ArgumentException>(() => MemoriesRoutes.CaseImportPath("acme", "."));
        Should.Throw<ArgumentException>(() => MemoriesRoutes.RestoreStatusPath("acme", ".."));
    }
}
