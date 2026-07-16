// <copyright file="MemoriesRoutesTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Reflection;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>
/// Story 25.3 (A21) — coverage for the single-source <see cref="MemoriesRoutes"/> route table: the template
/// constants carry the leading slash and placeholders the Server maps, and the client-facing <c>*Path</c>
/// builders return the relative, segment-escaped form the REST client sends.
/// </summary>
public class MemoriesRoutesTests
{
    [Fact]
    public void TemplateConstants_MatchTheExpectedServerTemplates()
    {
        MemoriesRoutes.ApiPrefix.ShouldBe("/api/v1");
        MemoriesRoutes.Ingest.ShouldBe("/api/v1/ingest");
        MemoriesRoutes.IngestStatus.ShouldBe("/api/v1/ingest/{instanceId}");
        MemoriesRoutes.IngestBatchStatus.ShouldBe("/api/v1/ingest/batches/{batchId}");
        MemoriesRoutes.Search.ShouldBe("/api/v1/search");
        MemoriesRoutes.Traverse.ShouldBe("/api/v1/tenants/{tenantId}/traverse");
        MemoriesRoutes.Tenants.ShouldBe("/api/v1/tenants");
        MemoriesRoutes.Tenant.ShouldBe("/api/v1/tenants/{tenantId}");
        MemoriesRoutes.Cases.ShouldBe("/api/v1/tenants/{tenantId}/cases");
        MemoriesRoutes.Case.ShouldBe("/api/v1/tenants/{tenantId}/cases/{caseId}");
        MemoriesRoutes.CaseMemoryUnit.ShouldBe("/api/v1/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}");
        MemoriesRoutes.CaseMemoryUnitBySourceUri.ShouldBe("/api/v1/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri");
        MemoriesRoutes.ConsistencyVerifyStatus.ShouldBe("/api/v1/tenants/{tenantId}/consistency/verify/{instanceId}");
        MemoriesRoutes.CaseExport.ShouldBe("/api/v1/tenants/{tenantId}/cases/{caseId}/export");
        MemoriesRoutes.TenantExport.ShouldBe("/api/v1/tenants/{tenantId}/export");
    }

    [Fact]
    public void EveryPublicTemplateConstant_IsAnAbsoluteApiPath()
    {
        // Completeness guard: any newly added template constant that is not an absolute /api path fails here,
        // keeping the table's server-facing invariant (leading slash, /api namespace) enforced.
        foreach (FieldInfo field in typeof(MemoriesRoutes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (!field.IsLiteral || field.FieldType != typeof(string))
            {
                continue;
            }

            var value = (string)field.GetRawConstantValue()!;
            value.StartsWith("/api", StringComparison.Ordinal).ShouldBeTrue(
                $"MemoriesRoutes.{field.Name} must be an absolute /api path template.");
        }
    }

    [Fact]
    public void PathBuilders_ReturnRelativePathsWithoutLeadingSlash()
    {
        MemoriesRoutes.SearchPath().ShouldBe("api/v1/search");
        MemoriesRoutes.IngestPath().ShouldBe("api/v1/ingest");
        MemoriesRoutes.TenantsPath().ShouldBe("api/v1/tenants");
        MemoriesRoutes.HandlersPath().ShouldBe("api/v1/handlers");
        MemoriesRoutes.TenantPath("acme").ShouldBe("api/v1/tenants/acme");
        MemoriesRoutes.CasesPath("acme").ShouldBe("api/v1/tenants/acme/cases");
        MemoriesRoutes.CasePath("acme", "case-1").ShouldBe("api/v1/tenants/acme/cases/case-1");
        MemoriesRoutes.CaseMemoryUnitPath("acme", "case-1", "mu-abc").ShouldBe("api/v1/tenants/acme/cases/case-1/memory-units/mu-abc");
        MemoriesRoutes.CaseMemoryUnitBySourceUriPath("acme", "case-1").ShouldBe("api/v1/tenants/acme/cases/case-1/memory-units/by-source-uri");
        MemoriesRoutes.TraversePath("acme").ShouldBe("api/v1/tenants/acme/traverse");
        MemoriesRoutes.ConsistencyVerifyPath("acme").ShouldBe("api/v1/tenants/acme/consistency/verify");
        MemoriesRoutes.ConsistencyVerifyStatusPath("acme", "inst-1").ShouldBe("api/v1/tenants/acme/consistency/verify/inst-1");
        MemoriesRoutes.ConsistencyInspectPath("acme", "mu-1").ShouldBe("api/v1/tenants/acme/consistency/inspect/mu-1");
        MemoriesRoutes.ConsistencyRepairPath("acme").ShouldBe("api/v1/tenants/acme/consistency/repair");
        MemoriesRoutes.ConsistencyRepairStatusPath("acme", "inst-1").ShouldBe("api/v1/tenants/acme/consistency/repair/inst-1");
        MemoriesRoutes.TenantTelemetrySummaryPath("acme").ShouldBe("api/v1/tenants/acme/telemetry/summary");
        MemoriesRoutes.TenantHandlerMismatchesPath("acme").ShouldBe("api/v1/tenants/acme/handlers/mismatches");
        MemoriesRoutes.CaseExportPath("acme", "case-1").ShouldBe("api/v1/tenants/acme/cases/case-1/export");
        MemoriesRoutes.TenantExportPath("acme").ShouldBe("api/v1/tenants/acme/export");
    }

    [Fact]
    public void PathBuilders_EscapeSegmentValues()
    {
        // Matches the client's Uri.EscapeDataString per-segment behaviour that the wire-surface tests assert
        // (e.g. te/nant -> te%2Fnant, "ca se" -> "ca%20se"). Literal path segments are left untouched.
        MemoriesRoutes.TenantPath("a/b c").ShouldBe("api/v1/tenants/a%2Fb%20c");
        MemoriesRoutes.CaseMemoryUnitBySourceUriPath("te/nant", "ca se")
            .ShouldBe("api/v1/tenants/te%2Fnant/cases/ca%20se/memory-units/by-source-uri");
        MemoriesRoutes.ConsistencyVerifyStatusPath("t", "inst 1").ShouldBe("api/v1/tenants/t/consistency/verify/inst%201");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData("..")]
    public void PathBuilders_InvalidSegmentValues_ThrowArgumentException(string caseId)
    {
        ArgumentException exception = Should.Throw<ArgumentException>(
            () => MemoriesRoutes.CaseExportPath("acme", caseId));

        exception.ParamName.ShouldBe("caseId");
    }

    [Fact]
    public void ServerLocationBuilders_ReturnAbsoluteEscapedV1Locations()
    {
        MemoriesRoutes.IngestStatusLocation("inst 1").ShouldBe("/api/v1/ingest/inst%201");
        MemoriesRoutes.IngestBatchStatusLocation("batch/1").ShouldBe("/api/v1/ingest/batches/batch%2F1");
        MemoriesRoutes.TenantProvisionStatusLocation("te/nant", "inst 1")
            .ShouldBe("/api/v1/tenants/te%2Fnant/provision-status/inst%201");
        MemoriesRoutes.TenantDeletionStatusLocation("te/nant", "inst 1")
            .ShouldBe("/api/v1/tenants/te%2Fnant/deletion-status/inst%201");
        MemoriesRoutes.CaseLocation("te/nant", "case 1")
            .ShouldBe("/api/v1/tenants/te%2Fnant/cases/case%201");
        MemoriesRoutes.CaseMemberLocation("te/nant", "case 1", "member/a")
            .ShouldBe("/api/v1/tenants/te%2Fnant/cases/case%201/members/member%2Fa");
        MemoriesRoutes.ConsistencyVerifyStatusLocation("te/nant", "inst 1")
            .ShouldBe("/api/v1/tenants/te%2Fnant/consistency/verify/inst%201");
        MemoriesRoutes.ConsistencyRepairStatusLocation("te/nant", "inst 1")
            .ShouldBe("/api/v1/tenants/te%2Fnant/consistency/repair/inst%201");
    }
}
