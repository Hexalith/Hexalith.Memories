// <copyright file="ProductionDeploymentArtifactsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>Executable contracts for the Story 26.1 production container and Kustomize artifacts.</summary>
public sealed class ProductionDeploymentArtifactsTests
{
    [Fact]
    public void ContainerProjects_UseCentralNumericNonRootPort8080Defaults()
    {
        string root = GetRepoRoot();
        string targets = Read(root, "Directory.Build.targets");
        string server = Read(root, "src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj");
        string mcp = Read(root, "src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj");

        targets.ShouldContain("<ContainerUser>1654</ContainerUser>");
        targets.ShouldContain("<ContainerPort Include=\"8080\" Type=\"tcp\" />");
        targets.ShouldContain("<ContainerEnvironmentVariable Include=\"ASPNETCORE_HTTP_PORTS\" Value=\"8080\" />");
        server.ShouldContain("<EnableContainer>true</EnableContainer>");
        server.ShouldContain("<ContainerRepository>memories</ContainerRepository>");
        server.ShouldContain("<Content Update=\"appsettings.Development.json\" CopyToPublishDirectory=\"Never\" />");
        mcp.ShouldContain("<EnableContainer>true</EnableContainer>");
        mcp.ShouldContain("<ContainerRepository>memories-mcp</ContainerRepository>");
        mcp.ShouldContain("<Content Update=\"appsettings.Development.json\" CopyToPublishDirectory=\"Never\" />");
    }

    [Fact]
    public void ProductionOverlay_RendersExactSecurityPersistenceAndResourceContracts()
    {
        string root = GetRepoRoot();
        string rendered = Run(root, "kubectl", "kustomize", "deploy/kubernetes/overlays/production");

        string server = GetDocument(rendered, "Deployment", "memories");
        string mcp = GetDocument(rendered, "Deployment", "memories-mcp");
        string redis = GetDocument(rendered, "StatefulSet", "redis-stack");
        string falkordb = GetDocument(rendered, "StatefulSet", "falkordb");
        string accessTelemetryPostgresql = GetDocument(rendered, "StatefulSet", "access-telemetry-postgresql");
        string accessTelemetryPostgresqlConfig = GetDocument(rendered, "ConfigMap", "access-telemetry-postgresql-config");
        string accessTelemetryPostgresqlService = GetDocument(rendered, "Service", "access-telemetry-postgresql");
        string accessTelemetryPostgresqlPolicy = GetDocument(rendered, "NetworkPolicy", "access-telemetry-postgresql");
        string pubsub = GetDocument(rendered, "Component", "pubsub");
        string accessTelemetryStore = GetDocument(rendered, "Component", "access-telemetry-store");
        string conversation = GetDocument(rendered, "Component", "llm-openai");
        string secretStore = GetDocument(rendered, "Component", "secretstore");
        string accessTelemetrySecretStore = GetDocument(rendered, "Component", "access-telemetry-secrets");
        string configuration = GetDocument(rendered, "Configuration", "memories-config");
        string accessTelemetryConfiguration = GetDocument(rendered, "Configuration", "memories-access-telemetry-config");
        string productionConfig = GetDocumentByNamePrefix(rendered, "ConfigMap", "memories-production-config-");

        server.ShouldContain("dapr.io/app-id: memories");
        server.ShouldContain("dapr.io/sidecar-cpu-request: 250m");
        server.ShouldContain("dapr.io/sidecar-memory-limit: 512Mi");
        server.ShouldContain("cpu: 500m");
        server.ShouldContain("memory: 2Gi");
        server.ShouldContain("/ready");
        server.ShouldContain("\"status\"");
        server.ShouldContain("\"Healthy\"");
        server.ShouldContain("name: DAPR_API_TOKEN_MODE");
        server.ShouldContain("value: enabled");

        string accessTelemetryDeployment = GetDocument(rendered, "Deployment", "memories-access-telemetry");
        accessTelemetryDeployment.ShouldContain("dapr.io/volume-mounts: access-telemetry-postgresql-tls:/mnt/access-telemetry-postgresql");
        accessTelemetryDeployment.ShouldContain("secretName: access-telemetry-postgresql-tls");
        accessTelemetryDeployment.ShouldContain("key: ca.crt");

        mcp.ShouldContain("dapr.io/app-id: memories-mcp");
        mcp.ShouldContain("dapr.io/sidecar-cpu-request: 100m");
        mcp.ShouldContain("cpu: 100m");
        mcp.ShouldContain("memory: 512Mi");
        mcp.ShouldContain("/ready");
        mcp.ShouldContain("\"status\"");
        mcp.ShouldContain("\"Healthy\"");
        mcp.ShouldContain("name: DAPR_API_TOKEN_MODE");
        mcp.ShouldContain("value: enabled");

        productionConfig.ShouldContain("OIDC_AUTHORITY: https://identity.example.com");
        productionConfig.ShouldContain("OIDC_ISSUER: https://identity.example.com");
        productionConfig.ShouldContain("OIDC_AUDIENCE: hexalith-memories");
        productionConfig.ShouldContain("OIDC_TENANT_CLAIM: tenant_id");

        redis.ShouldContain("redis/redis-stack-server:7.4.0-v8@sha256:");
        redis.ShouldContain("storage: 20Gi");
        redis.ShouldContain("mountPath: /data");
        redis.ShouldContain("failureThreshold: 60");
        redis.ShouldContain("memory: 4Gi");

        falkordb.ShouldContain("falkordb/falkordb:v4.12.0@sha256:");
        falkordb.ShouldContain("storage: 10Gi");
        falkordb.ShouldContain("mountPath: /var/lib/falkordb/data");
        falkordb.ShouldContain("failureThreshold: 60");
        falkordb.ShouldContain("memory: 4Gi");

        accessTelemetryPostgresql.ShouldContain("replicas: 1");
        accessTelemetryPostgresql.ShouldContain("postgres:18.4-trixie@sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a");
        accessTelemetryPostgresql.ShouldContain("hexalith.io/availability: single-node-non-ha");
        accessTelemetryPostgresql.ShouldContain("value: /var/lib/postgresql/18/docker");
        accessTelemetryPostgresql.ShouldContain("mountPath: /var/lib/postgresql");
        accessTelemetryPostgresql.ShouldContain("storageClassName: openebs-hostpath-retain");
        accessTelemetryPostgresql.ShouldContain("storage: 400Gi");
        accessTelemetryPostgresql.ShouldContain("ssl=on");
        accessTelemetryPostgresql.ShouldContain("ssl_min_protocol_version=TLSv1.2");
        accessTelemetryPostgresqlConfig.ShouldContain("peer map=container-postgres");
        accessTelemetryPostgresqlConfig.ShouldContain("container-postgres postgres memories_admin");
        accessTelemetryPostgresql.ShouldContain("ident_file=/etc/postgresql/pg_ident.conf");
        accessTelemetryPostgresql.ShouldContain("secretName: access-telemetry-postgresql-bootstrap");
        accessTelemetryPostgresql.ShouldContain("secretName: access-telemetry-postgresql-tls");
        accessTelemetryPostgresql.ShouldContain("cpu: \"4\"");
        accessTelemetryPostgresql.ShouldContain("memory: 8Gi");
        accessTelemetryPostgresql.ShouldContain("cpu: \"8\"");
        accessTelemetryPostgresql.ShouldContain("memory: 16Gi");
        accessTelemetryPostgresqlService.ShouldContain("type: ClusterIP");
        accessTelemetryPostgresqlService.ShouldContain("port: 5432");
        accessTelemetryPostgresqlPolicy.ShouldContain("app.kubernetes.io/name: memories-access-telemetry");
        accessTelemetryPostgresqlPolicy.ShouldContain("app.kubernetes.io/name: access-telemetry-postgresql-verifier");
        accessTelemetryPostgresqlPolicy.ShouldContain("egress: []");
        string accessTelemetryNetworkPolicy = GetDocument(rendered, "NetworkPolicy", "memories-access-telemetry");
        accessTelemetryNetworkPolicy.ShouldContain("kubernetes.io/metadata.name: openbao");
        accessTelemetryNetworkPolicy.ShouldContain("port: 8200");
        accessTelemetryNetworkPolicy.ShouldContain("cidr: 169.254.25.10/32");
        accessTelemetryNetworkPolicy.ShouldContain("app.kubernetes.io/name: redis-stack");
        accessTelemetryNetworkPolicy.ShouldContain("port: 6379");
        string accessTelemetryClockNetworkPolicy = GetDocument(rendered, "NetworkPolicy", "memories-access-telemetry-clock");
        accessTelemetryClockNetworkPolicy.ShouldContain("cidr: 169.254.25.10/32");

        accessTelemetryStore.ShouldContain("type: state.postgresql");
        accessTelemetryStore.ShouldContain("version: v2");
        accessTelemetryStore.ShouldContain("name: access-telemetry-postgresql");
        accessTelemetryStore.ShouldContain("value: access_telemetry.lifecycle_");
        accessTelemetryStore.ShouldNotContain("queryIndexes");

        // Chunk-2 review patch (2026-07-21): sslRootCert is not a recognised state.postgresql/v2
        // metadata field, so Dapr silently ignored it and it guaranteed nothing. TLS verify-full is
        // carried by the OpenBao-sourced connection string, so the dead field must stay removed and
        // the secret carrier is what this guard binds instead.
        //
        // Bound structurally, not by substring: connectionString must resolve from a secretKeyRef
        // (never an inline literal, which would put a credential in the manifest), from the exact
        // secret and key the OpenBao sync populates, through the verifying secret store.
        accessTelemetryStore.ShouldNotContain("sslRootCert");
        (string connectionSecret, string connectionKey) =
            ReadComponentMetadataSecretRef(accessTelemetryStore, "connectionString");
        connectionSecret.ShouldBe("access-telemetry-postgresql");
        connectionKey.ShouldBe("connectionString");
        accessTelemetryStore.ShouldContain("secretStore: access-telemetry-secrets");

        // ACCEPTED BLOCKER - `sslmode=verify-full` / `sslrootcert` content is not statically bindable.
        // The connection string lives only in OpenBao; no producer of it exists anywhere under
        // deploy/, tools/, or src/, so a string provisioned with `sslmode=require` would leave every
        // static guard in this file green. What is bindable is bound above (secret carrier, secret
        // store, no inline literal) plus the server-side rejection of plaintext in the pg_hba guard,
        // which is what actually makes a non-TLS connection fail.
        //   Owner: Hexalith Platform Operations (OpenBao secret content), with the security reviewer
        //     for AC4 sign-off.
        //   Consequence: gate C1.12 (encryption) cannot be discharged by any test in this assembly;
        //     it requires the running-profile observation in the C1 evidence packet.
        //   Reopen trigger: the connection string becomes observable to a checked-in producer or to
        //     the C1 verifier's captured component identity; bind `sslmode=verify-full` and
        //     `sslrootcert` then and delete this blocker.

        conversation.ShouldContain("type: conversation.openai");
        conversation.ShouldContain("value: gpt-4o-mini");
        conversation.ShouldContain("value: 0s");
        conversation.ShouldContain("name: llm-secret");
        conversation.ShouldContain("key: OPENAI_API_KEY");
        conversation.ShouldContain("- memories");

        pubsub.ShouldContain("allowedTopics");
        pubsub.ShouldContain("protectedTopics");
        pubsub.ShouldContain("eventstore=memories-events;memories=");
        pubsub.ShouldContain("eventstore=;memories=memories-events");
        pubsub.ShouldContain("- eventstore");
        pubsub.ShouldContain("- memories");
        pubsub.ShouldNotContain("publishAllowedTopics");

        secretStore.ShouldContain("type: secretstores.hashicorp.vault");
        secretStore.ShouldContain("value: https://hexalith-keys.openbao.svc.cluster.local:8200");
        secretStore.ShouldContain("name: openbao-runtime-bootstrap");
        secretStore.ShouldContain("value: hexalith/memories/runtime");
        secretStore.ShouldContain("name: skipVerify");
        secretStore.ShouldContain("value: \"false\"");
        secretStore.ShouldContain("- memories");
        secretStore.ShouldContain("- eventstore");

        accessTelemetrySecretStore.ShouldContain("type: secretstores.hashicorp.vault");
        accessTelemetrySecretStore.ShouldContain("name: openbao-access-telemetry-bootstrap");
        accessTelemetrySecretStore.ShouldContain("value: hexalith/memories/access-telemetry");

        // Structural ACL contract, not loose substring presence. kustomize re-serializes with sorted keys
        // and block-style lists, so the single operation renders as
        //   - action: allow / httpVerb: / - GET / - POST / name: /api/v1/**
        // Bind allow -> exact GET/POST verbs -> the /api/v1/** path in order, and require memories-mcp to
        // be the only allowed app-id, so widening the verbs, broadening the path, or adding a second
        // policy fails this test instead of passing on incidental substring presence.
        configuration.ShouldContain("defaultAction: deny");
        configuration.ShouldContain("appId: memories-mcp");
        int allowIndex = configuration.IndexOf("action: allow", StringComparison.Ordinal);
        int getIndex = configuration.IndexOf("- GET", StringComparison.Ordinal);
        int postIndex = configuration.IndexOf("- POST", StringComparison.Ordinal);
        int nameIndex = configuration.IndexOf("name: /api/v1/**", StringComparison.Ordinal);
        allowIndex.ShouldBeGreaterThanOrEqualTo(0);
        getIndex.ShouldBeGreaterThan(allowIndex);
        postIndex.ShouldBeGreaterThan(getIndex);
        nameIndex.ShouldBeGreaterThan(postIndex);
        (configuration.Split("action: allow").Length - 1).ShouldBe(1);
        (configuration.Split("appId:").Length - 1).ShouldBe(1);
        configuration.ShouldNotContain("name: /**");
        configuration.ShouldNotContain("DELETE");
        configuration.ShouldNotContain("PUT");
        configuration.ShouldNotContain("PATCH");

        foreach (string actorConfiguration in new[] { configuration, accessTelemetryConfiguration })
        {
            actorConfiguration.ShouldContain("features:");
            actorConfiguration.ShouldContain("name: HotReload");
            actorConfiguration.ShouldContain("enabled: false");
            (actorConfiguration.Split("name: HotReload").Length - 1).ShouldBe(1);
        }

        GetDocument(rendered, "ServiceAccount", "memories").ShouldContain("name: registry-credentials");
        GetDocument(rendered, "ServiceAccount", "memories-mcp").ShouldContain("name: registry-credentials");

        rendered.ShouldNotContain("conversation.echo", Case.Insensitive);
        rendered.ShouldNotContain("Authentication__ServerUpstream", Case.Insensitive);
        (server + mcp + productionConfig).ShouldNotContain("SigningKey", Case.Insensitive);
        rendered.ShouldNotContain("kind: Secret");
    }

    [Fact]
    public void ServerAndMcpHosts_RegisterDaprApplicationTokenMiddleware()
    {
        // Guards the sidecar-to-app token boundary's pipeline registration. The middleware's accept/reject
        // logic is unit-tested in DaprApplicationTokenMiddlewareTests, but nothing else fails if the
        // UseMiddleware call is dropped from a host -- which would silently open the application port.
        string root = GetRepoRoot();
        string serverProgram = Read(root, "src/Hexalith.Memories.Server/Program.cs");
        string mcpProgram = Read(root, "src/Hexalith.Memories.Mcp/Program.cs");

        serverProgram.ShouldContain("UseMiddleware<DaprApplicationTokenMiddleware>");
        mcpProgram.ShouldContain("UseMiddleware<DaprApplicationTokenMiddleware>");
    }

    [Fact]
    public void ProductionOverlay_ServicesNeverTargetApplicationPort()
    {
        string root = GetRepoRoot();
        string rendered = Run(root, "kubectl", "kustomize", "deploy/kubernetes/overlays/production");

        GetDocument(rendered, "Service", "memories").ShouldContain("targetPort: 3500");
        GetDocument(rendered, "Service", "memories").ShouldNotContain("targetPort: 8080");
        GetDocument(rendered, "Service", "memories-mcp").ShouldContain("targetPort: 3500");
        GetDocument(rendered, "Service", "memories-mcp").ShouldNotContain("targetPort: 8080");
    }

    [Fact]
    public void ProductionOverlay_AccessTelemetryDeploymentsAreScaledToZero()
    {
        // Story 27.3 is not yet delivered: no memories-access-telemetry(-clock) container image is
        // published anywhere in this repo's build/release pipeline. If the disabled-patch overlay ever
        // stops scaling these Deployments to 0, applying this overlay schedules pods that can never
        // pull their image -- exactly the disposable-cluster verification failure this guards against.
        string root = GetRepoRoot();
        string rendered = Run(root, "kubectl", "kustomize", "deploy/kubernetes/overlays/production");

        GetDocument(rendered, "Deployment", "memories-access-telemetry").ShouldContain("replicas: 0");
        GetDocument(rendered, "Deployment", "memories-access-telemetry-clock").ShouldContain("replicas: 0");
    }

    [Fact]
    public void ProductionOverlay_SecretRoleIsResourceNameBound()
    {
        string root = GetRepoRoot();
        string rendered = Run(root, "kubectl", "kustomize", "deploy/kubernetes/overlays/production");
        string role = GetDocument(rendered, "Role", "memories-dapr-secret-reader");

        role.ShouldContain("resourceNames:");
        role.ShouldContain("- redis-secret");
        role.ShouldContain("- llm-secret");
        role.ShouldContain("- google-embedding-api-key");
        role.ShouldContain("- memories-embedding-client-secret");
        role.ShouldContain("- openbao-runtime-bootstrap");
        role.ShouldContain("- openbao-access-telemetry-bootstrap");
        role.ShouldContain("verbs:");
        role.ShouldContain("- get");
        role.ShouldNotContain("- list");
        role.ShouldNotContain("- watch");
    }

    [Fact]
    public void OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal()
    {
        string root = GetRepoRoot();
        string values = Read(root, "deploy/openbao/values.yaml");
        string openBaoNamespace = Read(root, "deploy/openbao/namespace.yaml");
        string serviceAccountHardening = Read(root, "deploy/openbao/service-account-hardening.yaml");
        string smokeTest = Read(root, "deploy/openbao/smoke-test.yaml");

        values.ShouldContain("fullnameOverride: hexalith-keys");
        values.ShouldContain("tlsDisable: false");
        values.ShouldContain("2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653");
        values.ShouldContain("type: ClusterIP");
        values.ShouldContain("storage \"raft\"");
        values.ShouldContain("storageClass: openebs-hostpath-retain");

        // Story 31.1 reconciled values.yaml to the deployed release, which runs three Raft voters, not
        // the single standalone voter this file used to declare. The two `size: 10Gi` declarations are
        // the `data` and `audit` volumeClaimTemplates; the StatefulSet materializes one of each per
        // replica, so the deployed platform has 2 x 3 = 6 retained 10Gi PVCs. The declaration count is
        // asserted here and the replica count immediately below, so neither half can drift alone.
        (values.Split("size: 10Gi").Length - 1).ShouldBe(2);
        values.ShouldContain("  ha:\n    enabled: true\n    replicas: 3\n");
        values.ShouldContain("    raft:\n      enabled: true");
        values.ShouldContain("      setNodeId: true");
        values.ShouldContain("  standalone:\n    enabled: false");
        values.ShouldNotContain("  standalone:\n    enabled: true");
        values.ShouldNotContain("  ha:\n    enabled: false");

        // The HA shape brings its own required surfaces: leader/standby routing, the discovery RBAC that
        // `service_registration "kubernetes"` needs, and the token-review binding. All three were
        // measured on the deployed platform while this file still declared them off.
        values.ShouldContain("    active:\n      enabled: true");
        values.ShouldContain("    standby:\n      enabled: true");
        values.ShouldContain("    serviceDiscovery:\n      enabled: true");
        values.ShouldContain("  authDelegator:\n    enabled: true");
        values.ShouldContain("service_registration \"kubernetes\" {}");
        values.ShouldContain("retry_join");

        // The application namespace is the NetworkPolicy's only justified ingress source and stays pinned.
        // The `cert-manager` source is deliberately NOT pinned: Story 31.1 measured that no cert-manager
        // `Certificate` or `Issuer` exists in namespace `openbao`, and the operations document names
        // removing that source as the limitation's reopen trigger. Asserting its presence here would have
        // made executing the documented remediation a test failure. Document/manifest agreement about it is
        // asserted instead by
        // `OpenBaoPlatformDocumentationTests.OwnedManifests_EachHaveADocumentedSectionTiedToTheirSource`,
        // so the two records cannot drift apart while the rule is either kept or removed.
        values.ShouldContain("kubernetes.io/metadata.name: hexalith-memories");

        values.ShouldContain("audit \"file\" \"persistent\"");
        values.ShouldContain("tls_min_version = \"tls12\"");
        values.ShouldContain("seal \"static\"");
        values.ShouldNotContain("tls_disable = 1");
        values.ShouldNotContain("enabled: true\n  publishNotReadyAddresses", Case.Insensitive);
        values.ShouldContain("injector:\n  enabled: false");
        values.ShouldContain("csi:\n  enabled: false");
        values.ShouldContain("ui:\n  enabled: false");

        openBaoNamespace.ShouldContain("name: openbao");
        openBaoNamespace.ShouldContain("pod-security.kubernetes.io/enforce: restricted");
        openBaoNamespace.ShouldContain("hexalith.io/platform-owner: jpiquot");
        openBaoNamespace.ShouldContain("hexalith.io/security-reviewer: murat-tea-for-jpiquot");

        serviceAccountHardening.ShouldContain("kind: ServiceAccount");
        serviceAccountHardening.ShouldContain("name: hexalith-keys");
        serviceAccountHardening.ShouldContain("automountServiceAccountToken: false");

        smokeTest.ShouldContain("kind: Job");
        smokeTest.ShouldContain("name: hexalith-keys-smoke-test");
        smokeTest.ShouldContain("automountServiceAccountToken: false");
        smokeTest.ShouldContain("runAsNonRoot: true");
        smokeTest.ShouldContain("allowPrivilegeEscalation: false");
        smokeTest.ShouldContain("- ALL");
        smokeTest.ShouldContain("value: https://hexalith-keys.openbao.svc.cluster.local:8200");
        smokeTest.ShouldContain("value: /openbao/tls/ca.crt");
        smokeTest.ShouldNotContain("tls-skip-verify");
    }

    [Fact]
    public void ProductionOverlay_AccessTelemetryConnectionPoolFitsPostgreSqlMaxConnections()
    {
        // Story 27.3 C1 load-probe precondition. The ADR two-writer envelope scales the lifecycle
        // deployment to two replicas, and each replica's Dapr sidecar opens its own state-store pool of
        // maxConns connections. PostgreSQL must seat every pooled connection plus the superuser reserve
        // and the evidence sessions the probe itself runs, otherwise the 500 events/s run fails on
        // connection exhaustion instead of on a real capacity limit.
        //
        // Every operand except the two documented ADR constants is read out of the rendered overlay.
        // Hardcoding the replica count bound the arithmetic to nothing: the shipped overlay is scaled
        // to zero, so a later scale-up or a default RollingUpdate surge pod would have added a third
        // pool (3 x 40 + 13 = 133 > 100) with the guard still green.
        const int c1ProbeReplicas = 2;
        const int superuserReservedConnections = 3;
        const int evidenceSessionHeadroom = 10;

        string root = GetRepoRoot();
        string rendered = Run(root, "kubectl", "kustomize", "deploy/kubernetes/overlays/production");
        string store = GetDocument(rendered, "Component", "access-telemetry-store");
        string lifecycle = GetDocument(rendered, "Deployment", "memories-access-telemetry");

        int maxConns = ReadComponentMetadataInt32(store, "maxConns");
        int maxConnections = ReadServerParameterInt32(rendered, "max_connections");

        // The reserve arithmetic above is only valid while the server keeps the defaults it is derived
        // from. This also catches an override of superuser_reserved_connections, which ends with the
        // same token, wherever in the rendered manifest it is set.
        rendered.ShouldNotContain("reserved_connections=");

        // Peak concurrent pods, not the declared replica count: a rolling update may run
        // maxSurge extra pods, each with its own sidecar pool. maxSurge must be pinned, because the
        // Kubernetes default is 25% and would round up to an extra pod at two replicas.
        int declaredReplicas = ReadIntegerField(lifecycle, "replicas");
        int maxSurge = ReadIntegerField(lifecycle, "maxSurge");
        int plannedReplicas = Math.Max(declaredReplicas, c1ProbeReplicas);
        int peakPods = plannedReplicas + maxSurge;

        ((peakPods * maxConns) + superuserReservedConnections + evidenceSessionHeadroom)
            .ShouldBeLessThanOrEqualTo(
                maxConnections,
                $"{peakPods} peak lifecycle pods (declared {declaredReplicas}, C1 probe {c1ProbeReplicas}, maxSurge {maxSurge}) x maxConns {maxConns}, plus {superuserReservedConnections} superuser-reserved and {evidenceSessionHeadroom} evidence connections, must fit max_connections {maxConnections}.");

        // maxUnavailable does not affect the peak-pod pool math above (it bounds old pods torn down,
        // not new pods added), but it is pinned here structurally so a drift is caught rather than
        // silently accepted, matching the deployment manifest's own pool-math comment.
        ReadIntegerField(lifecycle, "maxUnavailable").ShouldBe(1);
    }

    [Fact]
    public void ProductionOverlay_AccessTelemetryProfileSecurityContractsAreBound()
    {
        // Story 27.3 C1 immutability. AC4 requires an independent security reviewer to approve identity,
        // secrets, TLS, authorization, and encryption for the exact PG-ONPREM-1 profile, but until now
        // nothing failed when any of these drifted -- a silently weakened profile shipped green and would
        // have been certified. Each block below binds one property the C1 evidence packet certifies.
        string root = GetRepoRoot();
        string rendered = Run(root, "kubectl", "kustomize", "deploy/kubernetes/overlays/production");

        // The access-telemetry secret store must verify OpenBao's TLS identity, never skip it.
        string secrets = GetDocument(rendered, "Component", "access-telemetry-secrets");
        ReadComponentMetadata(secrets, "skipVerify").ShouldBe("false");
        ReadComponentMetadata(secrets, "tlsServerName").ShouldBe("hexalith-keys.openbao.svc.cluster.local");

        // The store must stay an actor state store; losing this disables reminders at runtime only.
        string store = GetDocument(rendered, "Component", "access-telemetry-store");
        ReadComponentMetadata(store, "actorStateStore").ShouldBe("true");

        // PostgreSQL must reject every plaintext path and keep the runtime role least-privileged.
        // pg_hba is first-match-wins, so presence assertions alone are not enough: a bare
        // `host ... scram-sha-256` line inserted above the reject rules accepts plaintext TCP while
        // every presence assertion still passes. Every TCP rule is therefore checked in order.
        string postgresqlConfig = GetDocument(rendered, "ConfigMap", "access-telemetry-postgresql-config");
        IReadOnlyList<string> hostBasedAuthenticationRules = ReadHostBasedAuthenticationRules(postgresqlConfig);
        hostBasedAuthenticationRules
            .Where(static rule => rule.StartsWith("host", StringComparison.OrdinalIgnoreCase))
            .ShouldBe(
                [
                    "hostssl all all 0.0.0.0/0 scram-sha-256",
                    "hostssl all all ::/0 scram-sha-256",
                    "hostnossl all all 0.0.0.0/0 reject",
                    "hostnossl all all ::/0 reject",
                ],
                "pg_hba TCP rules must appear exactly in this order: no rule may precede them, and no "
                    + "connection-type-agnostic `host` rule may exist at all.");

        // Weak authentication methods are excluded from the pg_hba rules themselves, not from the
        // whole ConfigMap: the init SQL legitimately contains `LOGIN PASSWORD`.
        foreach (string weak in new[] { "trust", "md5", "password" })
        {
            hostBasedAuthenticationRules.ShouldNotContain(
                rule => rule.Split(' ').Contains(weak, StringComparer.OrdinalIgnoreCase),
                $"No pg_hba rule may authenticate with '{weak}'.");
        }

        postgresqlConfig.ShouldContain("CREATE ROLE memories_access_telemetry_runtime LOGIN");
        postgresqlConfig.ShouldNotContain("SUPERUSER");
        postgresqlConfig.ShouldContain("REVOKE ALL ON DATABASE memories_access_telemetry FROM PUBLIC;");
        postgresqlConfig.ShouldContain("REVOKE ALL ON SCHEMA public FROM PUBLIC;");
        postgresqlConfig.ShouldContain("GRANT CONNECT ON DATABASE memories_access_telemetry TO memories_access_telemetry_runtime;");
        postgresqlConfig.ShouldContain("GRANT USAGE, CREATE ON SCHEMA access_telemetry TO memories_access_telemetry_runtime;");

        // Each workload reads exactly its own secrets, and only by get.
        foreach ((string role, string[] resourceNames) in new[]
        {
            ("memories-access-telemetry-secret-reader", new[] { "access-telemetry-marker-key", "redis-secret", "app-api-token", "dapr-api-token", "openbao-access-telemetry-bootstrap" }),
            ("memories-access-telemetry-clock-secret-reader", new[] { "access-telemetry-clock-key", "app-api-token", "dapr-api-token", "openbao-access-telemetry-bootstrap" }),
        })
        {
            string document = GetDocument(rendered, "Role", role);
            ReadYamlSequence(document, "resourceNames").ShouldBe(resourceNames, ignoreOrder: true);
            ReadYamlSequence(document, "resources").ShouldBe(["secrets"]);
            ReadYamlSequence(document, "verbs").ShouldBe(["get"]);
        }

        // The lifecycle ACL stays deny-default with exactly the four named operations. The verb
        // exclusions are case-insensitive: Dapr matches HTTP verbs case-insensitively, so a
        // case-sensitive substring check let `httpVerb: ["delete"]` through.
        string lifecycleConfiguration = GetDocument(rendered, "Configuration", "memories-access-telemetry-config");
        (lifecycleConfiguration.Split("defaultAction: deny").Length - 1).ShouldBe(3);
        (lifecycleConfiguration.Split("appId:").Length - 1).ShouldBe(2);
        (lifecycleConfiguration.Split("action: allow").Length - 1).ShouldBe(4);
        lifecycleConfiguration.ShouldContain("appId: memories\n");
        lifecycleConfiguration.ShouldContain("appId: memories-access-telemetry-inspector");
        lifecycleConfiguration.ShouldContain("name: /v1/access-telemetry/write");
        lifecycleConfiguration.ShouldContain("name: /v1/access-telemetry/heartbeat");
        lifecycleConfiguration.ShouldContain("name: /v1/access-telemetry/validate");
        lifecycleConfiguration.ShouldContain("name: /v1/access-telemetry/inspect");
        lifecycleConfiguration.ShouldNotContain("name: /**");
        lifecycleConfiguration.ShouldNotContain("DELETE", Case.Insensitive);
        lifecycleConfiguration.ShouldNotContain("PUT", Case.Insensitive);
        lifecycleConfiguration.ShouldNotContain("PATCH", Case.Insensitive);
        lifecycleConfiguration.ShouldContain("defaultAccess: deny");
        ReadYamlSequence(lifecycleConfiguration, "allowedSecrets")
            .ShouldBe(["access-telemetry-marker-key", "redis-secret", "access-telemetry-postgresql"], ignoreOrder: true);
    }

    /// <summary>
    /// Returns every pg_hba rule in file order, with runs of whitespace collapsed. pg_hba is
    /// first-match-wins, so rule order is the security property and a presence check is not.
    /// </summary>
    private static IReadOnlyList<string> ReadHostBasedAuthenticationRules(string configMap)
    {
        string[] lines = configMap.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        int start = Array.FindIndex(lines, static line => line.TrimStart().StartsWith("pg_hba.conf:", StringComparison.Ordinal));
        start.ShouldBeGreaterThanOrEqualTo(0, "pg_hba.conf is missing from the config map.");
        int blockIndent = Indent(lines[start]);

        var rules = new List<string>();
        for (int index = start + 1; index < lines.Length; index++)
        {
            // Normalize tabs before computing indent too, so a tab-indented pg_hba.conf line is
            // measured on the same basis as the space-indented block key that bounds it.
            string raw = lines[index].Replace('\t', ' ');
            if (raw.Trim().Length == 0)
            {
                continue;
            }

            // The literal block ends at the first line that dedents back to the key's own level.
            if (Indent(raw) <= blockIndent)
            {
                break;
            }

            string rule = CollapseSpaces(raw).Trim();
            if (rule.StartsWith('#'))
            {
                continue;
            }

            if (rule.StartsWith("local", StringComparison.OrdinalIgnoreCase) ||
                rule.StartsWith("host", StringComparison.OrdinalIgnoreCase))
            {
                rules.Add(rule);
            }
        }

        rules.ShouldNotBeEmpty("pg_hba.conf declared no authentication rules.");

        return rules;
    }

    private static string CollapseSpaces(string value)
    {
        while (value.Contains("  ", StringComparison.Ordinal))
        {
            value = value.Replace("  ", " ", StringComparison.Ordinal);
        }

        return value;
    }

    /// <summary>
    /// Reads a YAML block sequence bound to its own key by indentation. The previous
    /// terminator-string form ran to end-of-document whenever the terminator was absent, silently
    /// absorbing every later sequence in the manifest, and it matched only the first occurrence of
    /// the key. This form requires the key to occur exactly once and stops at the first line that
    /// dedents out of the key's block.
    /// </summary>
    private static IReadOnlyList<string> ReadYamlSequence(string document, string key)
    {
        string[] lines = document.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        int[] keyLines = [.. lines
            .Select(static (line, index) => (line, index))
            .Where(candidate => IsKeyLine(candidate.line, key))
            .Select(static candidate => candidate.index)];
        keyLines.Length.ShouldBe(1, $"Sequence '{key}' must occur exactly once; found {keyLines.Length}.");

        int keyIndex = keyLines[0];

        // A key can be the first key of a sequence item (`- allowedSecrets:`), in which case its
        // effective indent is past the `- ` marker.
        int keyIndent = Indent(lines[keyIndex]) +
            (lines[keyIndex].TrimStart().StartsWith("- ", StringComparison.Ordinal) ? 2 : 0);
        var items = new List<string>();
        for (int index = keyIndex + 1; index < lines.Length; index++)
        {
            string line = lines[index];
            if (line.Trim().Length == 0)
            {
                continue;
            }

            string trimmed = line.Trim();

            // A comment at or past the sequence's own indent does not end the block.
            if (trimmed.StartsWith('#') && Indent(line) >= keyIndent)
            {
                continue;
            }

            // A block sequence may sit at the key's own indent or deeper; anything shallower, or any
            // line that is not a sequence item, ends the block.
            if (Indent(line) < keyIndent || !trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                break;
            }

            items.Add(trimmed[2..].Trim().Trim('"'));
        }

        items.ShouldNotBeEmpty($"Sequence '{key}' is empty.");

        return items;
    }

    /// <summary>
    /// Reads one <c>spec.metadata</c> item's inline value, bound to that item. The previous form
    /// took the next <c>value:</c> anywhere after the name, so a <c>secretKeyRef</c>-backed entry
    /// silently returned a later, unrelated entry's value.
    /// </summary>
    private static string ReadComponentMetadata(string component, string metadataName)
    {
        IReadOnlyList<string> item = ReadComponentMetadataItem(component, metadataName);
        string[] values = [.. item
            .Where(static line => line.StartsWith("value:", StringComparison.Ordinal))
            .Select(static line => line["value:".Length..].Trim().Trim('"'))];
        values.Length.ShouldBe(
            1,
            $"Component metadata '{metadataName}' must carry exactly one inline value; found {values.Length}.");

        return values[0];
    }

    /// <summary>Reads the secret name and key a <c>secretKeyRef</c>-backed metadata entry resolves from.</summary>
    private static (string SecretName, string Key) ReadComponentMetadataSecretRef(string component, string metadataName)
    {
        IReadOnlyList<string> item = ReadComponentMetadataItem(component, metadataName);
        int reference = item.ToList().IndexOf("secretKeyRef:");
        reference.ShouldBeGreaterThanOrEqualTo(0, $"Component metadata '{metadataName}' is not secretKeyRef-backed.");
        item.ShouldNotContain(
            static line => line.StartsWith("value:", StringComparison.Ordinal),
            $"Component metadata '{metadataName}' must not carry an inline value beside its secretKeyRef.");

        // Read the reference's own name/key, never the item's `name: {metadataName}` line.
        IReadOnlyList<string> body = [.. item.Skip(reference + 1)];

        return (ReadScalar(body, "name:"), ReadScalar(body, "key:"));
    }

    /// <summary>
    /// Returns the trimmed lines of one <c>spec.metadata</c> item, from its <c>- name:</c> line up
    /// to the next item or the end of the sequence.
    /// </summary>
    private static IReadOnlyList<string> ReadComponentMetadataItem(string component, string metadataName)
    {
        string[] lines = component.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        int start = -1;
        int itemIndent = -1;
        for (int index = 0; index < lines.Length; index++)
        {
            string trimmed = lines[index].Trim();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal) &&
                string.Equals(trimmed[2..].Trim(), $"name: {metadataName}", StringComparison.Ordinal))
            {
                start.ShouldBe(-1, $"Component metadata '{metadataName}' must occur exactly once.");
                start = index;
                itemIndent = Indent(lines[index]);
            }
        }

        start.ShouldBeGreaterThanOrEqualTo(0, $"Component metadata '{metadataName}' is missing.");

        var item = new List<string> { $"name: {metadataName}" };
        for (int index = start + 1; index < lines.Length; index++)
        {
            string line = lines[index];
            if (line.Trim().Length == 0)
            {
                continue;
            }

            // A dedent, or the next `- ` item at the same indent, ends this item.
            if (Indent(line) <= itemIndent)
            {
                break;
            }

            item.Add(line.Trim());
        }

        return item;
    }

    private static string ReadScalar(IReadOnlyList<string> item, string key)
    {
        string[] values = [.. item
            .Where(line => line.StartsWith(key, StringComparison.Ordinal) && line.Length > key.Length)
            .Select(line => line[key.Length..].Trim().Trim('"'))];
        values.Length.ShouldBe(1, $"Expected exactly one '{key}' in the item; found {values.Length}.");

        return values[0];
    }

    private static int ReadComponentMetadataInt32(string component, string metadataName)
        => int.Parse(ReadComponentMetadata(component, metadataName), CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads a PostgreSQL server parameter from the whole rendered manifest, not from one document.
    /// The parameter can be set from the StatefulSet command line or from the config ConfigMap, and
    /// the previous single-document, first-occurrence form was voided by either.
    /// </summary>
    private static int ReadServerParameterInt32(string rendered, string parameterName)
    {
        MatchCollection matches = Regex.Matches(
            rendered,
            $@"(?<![A-Za-z0-9_]){Regex.Escape(parameterName)}\s*=\s*(?<value>\d+)",
            RegexOptions.None,
            TimeSpan.FromSeconds(5));
        matches.Count.ShouldBe(
            1,
            $"PostgreSQL server parameter '{parameterName}' must be set exactly once across the rendered manifest; found {matches.Count}.");

        return int.Parse(matches[0].Groups["value"].Value, CultureInfo.InvariantCulture);
    }

    private static int ReadIntegerField(string document, string key)
    {
        MatchCollection matches = Regex.Matches(
            document,
            $@"^\s*{Regex.Escape(key)}:\s*(?<value>\d+)\s*$",
            RegexOptions.Multiline,
            TimeSpan.FromSeconds(5));
        matches.Count.ShouldBe(1, $"Field '{key}' must occur exactly once; found {matches.Count}.");

        return int.Parse(matches[0].Groups["value"].Value, CultureInfo.InvariantCulture);
    }

    private static int Indent(string line) => line.Length - line.TrimStart().Length;

    private static bool IsKeyLine(string line, string key)
    {
        string trimmed = line.TrimStart();
        if (trimmed.StartsWith("- ", StringComparison.Ordinal))
        {
            trimmed = trimmed[2..].TrimStart();
        }

        return trimmed.StartsWith($"{key}:", StringComparison.Ordinal) &&
            trimmed[(key.Length + 1)..].Trim().Length == 0;
    }

    private static string GetDocument(string rendered, string kind, string name)
    {
        foreach (string document in rendered.Split("\n---", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if ((document.StartsWith($"kind: {kind}\n", StringComparison.Ordinal)
                    || document.Contains($"\nkind: {kind}\n", StringComparison.Ordinal))
                && document.Contains($"\n  name: {name}\n", StringComparison.Ordinal))
            {
                return document;
            }
        }

        throw new ShouldAssertException($"Rendered manifest did not contain {kind}/{name}.");
    }

    private static string GetDocumentByNamePrefix(string rendered, string kind, string namePrefix)
    {
        foreach (string document in rendered.Split("\n---", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if ((document.StartsWith($"kind: {kind}\n", StringComparison.Ordinal)
                    || document.Contains($"\nkind: {kind}\n", StringComparison.Ordinal))
                && document.Contains($"\n  name: {namePrefix}", StringComparison.Ordinal))
            {
                return document;
            }
        }

        throw new ShouldAssertException($"Rendered manifest did not contain {kind} named with prefix {namePrefix}.");
    }

    private static string Read(string root, string relativePath)
        // Line endings are normalized here so multi-line literal assertions (for example the
        // `  ha:\n    enabled: true\n    replicas: 3\n` block) hold regardless of how the working tree
        // materializes the file. `.gitattributes` pins `*.yaml` to LF today, which is the only reason the
        // un-normalized form worked; the sibling OpenBaoPlatformDocumentationTests already normalized, so
        // the pair would otherwise fail asymmetrically if that ever changed.
        => File.ReadAllText(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Run(string root, string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = root,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start {fileName}.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, error);
        return output;
    }

    private static string GetRepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Hexalith.Memories.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
