// <copyright file="MutationAuditLogStreamTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System.Net;
using System.Net.Http.Json;
using System.Text;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

using Microsoft.Extensions.Logging;

using Shouldly;

/// <summary>Endpoint-level audit emission coverage for Story 20.5 mutating surfaces.</summary>
[Trait("Category", "Unit")]
public sealed class MutationAuditLogStreamTests : IDisposable
{
    private readonly TelemetryWebAppFactory _factory = new();

    [Fact]
    public async Task TenantCreate_InvalidInput_EmitsTenantLifecycleAudit()
    {
        using HttpClient client = _factory.CreateClient();
        TenantProvisioningInput input = new("acme", string.Empty);

        using HttpResponseMessage response = await client.PostAsync(
            "/api/tenants",
            JsonContent.Create(input, options: MemoriesJsonContext.Options),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        AccessTelemetryEvent auditEvent = GetSingleAuditEvent();
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationTenantLifecycle);
        auditEvent.EventId.ShouldBe(7516);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.ErrorCode.ShouldBe("INVALID_INPUT");
        auditEvent.User.ShouldBe("operator-1");
        auditEvent.QueryParams!["operation"].ShouldBe("tenant-create");
    }

    [Fact]
    public async Task TenantDisplayName_InvalidInput_EmitsTenantConfigAudit()
    {
        using HttpClient client = _factory.CreateClient();
        TenantUpdateInput input = new(string.Empty);

        using HttpResponseMessage response = await client.PatchAsync(
            "/api/tenants/acme",
            JsonContent.Create(input, options: MemoriesJsonContext.Options),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        AccessTelemetryEvent auditEvent = GetSingleAuditEvent();
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationTenantConfig);
        auditEvent.EventId.ShouldBe(7517);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.ErrorCode.ShouldBe("INVALID_INPUT");
        auditEvent.User.ShouldBe("operator-1");
        auditEvent.QueryParams!["operation"].ShouldBe("display-name-update");
        auditEvent.QueryParams.Keys.ShouldNotContain("displayName");
    }

    [Fact]
    public async Task TenantEmbeddingConfig_UnknownTenant_EmitsTenantConfigAuditWithoutCredentials()
    {
        using HttpClient client = _factory.CreateClient();
        TenantEmbeddingConfig input = new(
            "google",
            "text-embedding-004",
            768,
            60,
            "secret-name",
            baseUrl: "https://example.invalid/embeddings");

        using HttpResponseMessage response = await client.PutAsync(
            "/api/tenants/unknown-tenant/embedding-config?forceReindex=true",
            JsonContent.Create(input, options: MemoriesJsonContext.Options),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        AccessTelemetryEvent auditEvent = GetSingleAuditEvent();
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationTenantConfig);
        auditEvent.EventId.ShouldBe(7517);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.ErrorCode.ShouldBe("TENANT_NOT_FOUND");
        auditEvent.TenantId.ShouldBe("__rejected__");
        auditEvent.QueryParams!["operation"].ShouldBe("embedding-config-update");
        auditEvent.QueryParams["forceReindex"].ShouldBe(true);
        auditEvent.QueryParams.Keys.ShouldNotContain("apiSecretKeyName");
        auditEvent.QueryParams.Keys.ShouldNotContain("baseUrl");
        auditEvent.QueryParams.Keys.ShouldNotContain("oidcClientId");
        auditEvent.QueryParams.Keys.ShouldNotContain("oidcTokenEndpoint");
    }

    [Theory]
    [InlineData(
        "/api/tenants/unknown-tenant/provision-status/provision-other-1234567890abcdef",
        "tenant-provision-status",
        "TENANT_NOT_FOUND")]
    [InlineData(
        "/api/tenants/unknown-tenant/deletion-status/delete-other-1234567890abcdef",
        "tenant-deletion-status",
        "TENANT_NOT_FOUND")]
    public async Task TenantWorkflowStatus_UnknownTenant_EmitsTenantLifecycleAudit(
        string path,
        string operation,
        string errorCode)
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        AccessTelemetryEvent auditEvent = GetSingleAuditEvent();
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationTenantLifecycle);
        auditEvent.EventId.ShouldBe(7516);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.ErrorCode.ShouldBe(errorCode);
        auditEvent.TenantId.ShouldBe("__rejected__");
        auditEvent.QueryParams!["operation"].ShouldBe(operation);
        auditEvent.QueryParams.Keys.ShouldContain("workflowInstanceIdPrefix");
        auditEvent.QueryParams.Keys.ShouldNotContain("workflowInstanceId");
    }

    [Fact]
    public async Task TenantDelete_UnknownTenant_EmitsDeleteAudit()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.DeleteAsync(
            "/api/tenants/unknown-tenant",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        AccessTelemetryEvent auditEvent = GetSingleAuditEvent();
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationDelete);
        auditEvent.EventId.ShouldBe(7515);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.ErrorCode.ShouldBe("TENANT_NOT_FOUND");
        auditEvent.TenantId.ShouldBe("__rejected__");
        auditEvent.QueryParams!["operation"].ShouldBe("tenant-delete");
    }

    [Fact]
    public async Task CaseMemberAdd_InvalidInput_EmitsCaseMemberAudit()
    {
        using HttpClient client = _factory.CreateClient();
        using StringContent content = new("{}", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PutAsync(
            "/api/tenants/acme/cases/case-1/members/member-1",
            content,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        AccessTelemetryEvent auditEvent = GetSingleAuditEvent();
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationCaseMember);
        auditEvent.EventId.ShouldBe(7518);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.CaseId.ShouldBe("case-1");
        auditEvent.User.ShouldBe("operator-1");
        auditEvent.QueryParams!["operation"].ShouldBe("case-member-add");
        auditEvent.QueryParams.Keys.ShouldNotContain("requestBody");
    }

    [Fact]
    public async Task CaseMemberRemove_InvalidInput_EmitsCaseMemberAudit()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.DeleteAsync(
            "/api/tenants/acme/cases/bad%20case/members/member-1",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        AccessTelemetryEvent auditEvent = GetSingleAuditEvent();
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationCaseMember);
        auditEvent.EventId.ShouldBe(7518);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.CaseId.ShouldBe("bad case");
        auditEvent.User.ShouldBe("operator-1");
        auditEvent.QueryParams!["operation"].ShouldBe("case-member-remove");
        auditEvent.QueryParams.Keys.ShouldContain("memberIdPrefix");
    }

    [Fact]
    public async Task AnnotationCreate_InvalidInput_EmitsAnnotationAudit()
    {
        using HttpClient client = _factory.CreateClient();
        CreateAnnotationInput input = new("acme", "case-1", "memory-1", string.Empty, "operator-1");

        using HttpResponseMessage response = await client.PostAsync(
            "/api/tenants/acme/cases/case-1/memory-units/memory-1/annotations",
            JsonContent.Create(input, options: MemoriesJsonContext.Options),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        AccessTelemetryEvent auditEvent = GetSingleAuditEvent();
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationAnnotation);
        auditEvent.EventId.ShouldBe(7519);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.CaseId.ShouldBe("case-1");
        auditEvent.User.ShouldBe("operator-1");
        auditEvent.QueryParams!["operation"].ShouldBe("annotation-create");
        auditEvent.QueryParams.Keys.ShouldNotContain("content");
    }

    [Fact]
    public async Task MemoryUnitDelete_InvalidInput_EmitsDeleteAudit()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.DeleteAsync(
            "/api/tenants/acme/cases/bad%20case/memory-units/memory-1",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        AccessTelemetryEvent auditEvent = GetSingleAuditEvent();
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationDelete);
        auditEvent.EventId.ShouldBe(7515);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.User.ShouldBe("operator-1");
        auditEvent.QueryParams!["operation"].ShouldBe("memory-unit-delete");
    }

    [Fact]
    public async Task CaseDelete_InvalidInput_EmitsDeleteAudit()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.DeleteAsync(
            "/api/tenants/acme/cases/bad%20case",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        AccessTelemetryEvent auditEvent = GetSingleAuditEvent();
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationDelete);
        auditEvent.EventId.ShouldBe(7515);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.User.ShouldBe("operator-1");
        auditEvent.CaseId.ShouldBe("bad case");
        auditEvent.QueryParams!["operation"].ShouldBe("case-delete");
    }

    public void Dispose() => _factory.Dispose();

    private AccessTelemetryEvent GetSingleAuditEvent()
    {
        AuditLogCapture capture = _factory.AuditLogs.AccessTelemetryCaptures
            .Where(c => c.AuditEvent is not null)
            .ShouldHaveSingleItem();
        capture.Level.ShouldBe(LogLevel.Warning);
        return capture.AuditEvent!;
    }
}
