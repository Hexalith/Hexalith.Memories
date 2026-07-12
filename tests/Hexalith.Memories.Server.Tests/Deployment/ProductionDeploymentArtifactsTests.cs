// <copyright file="ProductionDeploymentArtifactsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.Diagnostics;

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
        server.ShouldContain("<ContainerRepository>hexalith/memories-server</ContainerRepository>");
        mcp.ShouldContain("<EnableContainer>true</EnableContainer>");
        mcp.ShouldContain("<ContainerRepository>hexalith/memories-mcp</ContainerRepository>");
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
        string pubsub = GetDocument(rendered, "Component", "pubsub");
        string conversation = GetDocument(rendered, "Component", "llm-openai");
        string secretStore = GetDocument(rendered, "Component", "secretstore");
        string configuration = GetDocument(rendered, "Configuration", "memories-config");
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

        secretStore.ShouldContain("type: secretstores.kubernetes");
        secretStore.ShouldContain("- memories");
        secretStore.ShouldContain("- eventstore");

        configuration.ShouldContain("defaultAction: deny");
        configuration.ShouldContain("appId: memories-mcp");
        configuration.ShouldContain("name: /api/v1/**");

        GetDocument(rendered, "ServiceAccount", "memories").ShouldContain("name: registry-credentials");
        GetDocument(rendered, "ServiceAccount", "memories-mcp").ShouldContain("name: registry-credentials");

        rendered.ShouldNotContain("conversation.echo", Case.Insensitive);
        rendered.ShouldNotContain("Authentication__ServerUpstream", Case.Insensitive);
        rendered.ShouldNotContain("SigningKey", Case.Insensitive);
        rendered.ShouldNotContain("kind: Secret");
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
        role.ShouldContain("verbs:");
        role.ShouldContain("- get");
        role.ShouldNotContain("- list");
        role.ShouldNotContain("- watch");
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
        => File.ReadAllText(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

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
