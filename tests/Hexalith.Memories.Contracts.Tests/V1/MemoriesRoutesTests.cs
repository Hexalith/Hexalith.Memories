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
        MemoriesRoutes.ApiPrefix.ShouldBe("/api");
        MemoriesRoutes.Ingest.ShouldBe("/api/ingest");
        MemoriesRoutes.IngestStatus.ShouldBe("/api/ingest/{instanceId}");
        MemoriesRoutes.IngestBatchStatus.ShouldBe("/api/ingest/batches/{batchId}");
        MemoriesRoutes.Search.ShouldBe("/api/search");
        MemoriesRoutes.Traverse.ShouldBe("/api/tenants/{tenantId}/traverse");
        MemoriesRoutes.Tenants.ShouldBe("/api/tenants");
        MemoriesRoutes.Tenant.ShouldBe("/api/tenants/{tenantId}");
        MemoriesRoutes.Cases.ShouldBe("/api/tenants/{tenantId}/cases");
        MemoriesRoutes.Case.ShouldBe("/api/tenants/{tenantId}/cases/{caseId}");
        MemoriesRoutes.CaseMemoryUnit.ShouldBe("/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}");
        MemoriesRoutes.CaseMemoryUnitBySourceUri.ShouldBe("/api/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri");
        MemoriesRoutes.ConsistencyVerifyStatus.ShouldBe("/api/tenants/{tenantId}/consistency/verify/{instanceId}");
        MemoriesRoutes.CaseExport.ShouldBe("/api/tenants/{tenantId}/cases/{caseId}/export");
        MemoriesRoutes.TenantExport.ShouldBe("/api/tenants/{tenantId}/export");
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
        MemoriesRoutes.SearchPath().ShouldBe("api/search");
        MemoriesRoutes.IngestPath().ShouldBe("api/ingest");
        MemoriesRoutes.TenantsPath().ShouldBe("api/tenants");
        MemoriesRoutes.HandlersPath().ShouldBe("api/handlers");
        MemoriesRoutes.TenantPath("acme").ShouldBe("api/tenants/acme");
        MemoriesRoutes.CasesPath("acme").ShouldBe("api/tenants/acme/cases");
        MemoriesRoutes.CasePath("acme", "case-1").ShouldBe("api/tenants/acme/cases/case-1");
        MemoriesRoutes.CaseMemoryUnitPath("acme", "case-1", "mu-abc").ShouldBe("api/tenants/acme/cases/case-1/memory-units/mu-abc");
        MemoriesRoutes.CaseMemoryUnitBySourceUriPath("acme", "case-1").ShouldBe("api/tenants/acme/cases/case-1/memory-units/by-source-uri");
        MemoriesRoutes.TraversePath("acme").ShouldBe("api/tenants/acme/traverse");
        MemoriesRoutes.ConsistencyVerifyPath("acme").ShouldBe("api/tenants/acme/consistency/verify");
        MemoriesRoutes.ConsistencyVerifyStatusPath("acme", "inst-1").ShouldBe("api/tenants/acme/consistency/verify/inst-1");
        MemoriesRoutes.ConsistencyInspectPath("acme", "mu-1").ShouldBe("api/tenants/acme/consistency/inspect/mu-1");
        MemoriesRoutes.ConsistencyRepairPath("acme").ShouldBe("api/tenants/acme/consistency/repair");
        MemoriesRoutes.ConsistencyRepairStatusPath("acme", "inst-1").ShouldBe("api/tenants/acme/consistency/repair/inst-1");
        MemoriesRoutes.TenantTelemetrySummaryPath("acme").ShouldBe("api/tenants/acme/telemetry/summary");
        MemoriesRoutes.TenantHandlerMismatchesPath("acme").ShouldBe("api/tenants/acme/handlers/mismatches");
        MemoriesRoutes.CaseExportPath("acme", "case-1").ShouldBe("api/tenants/acme/cases/case-1/export");
        MemoriesRoutes.TenantExportPath("acme").ShouldBe("api/tenants/acme/export");
    }

    [Fact]
    public void PathBuilders_EscapeSegmentValues()
    {
        // Matches the client's Uri.EscapeDataString per-segment behaviour that the wire-surface tests assert
        // (e.g. te/nant -> te%2Fnant, "ca se" -> "ca%20se"). Literal path segments are left untouched.
        MemoriesRoutes.TenantPath("a/b c").ShouldBe("api/tenants/a%2Fb%20c");
        MemoriesRoutes.CaseMemoryUnitBySourceUriPath("te/nant", "ca se")
            .ShouldBe("api/tenants/te%2Fnant/cases/ca%20se/memory-units/by-source-uri");
        MemoriesRoutes.ConsistencyVerifyStatusPath("t", "inst 1").ShouldBe("api/tenants/t/consistency/verify/inst%201");
    }
}
