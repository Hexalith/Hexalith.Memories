// <copyright file="AppHostOpenBaoConfigurationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using Shouldly;

/// <summary>Structural guards for the Story 29.1 development-only OpenBao AppHost profile.</summary>
public sealed class AppHostOpenBaoConfigurationTests
{
    [Fact]
    public void AppHost_OpenBaoImageAndNormalServerConfigurationArePinned()
    {
        string profile = ReadRepoFile("src", "Hexalith.Memories.AppHost", "OpenBaoDevelopmentProfile.cs");

        profile.ShouldContain("quay.io/openbao/openbao");
        profile.ShouldContain("2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653");
        profile.ShouldContain("storage \"inmem\" {}");
        profile.ShouldContain("listener \"tcp\"");
        profile.ShouldContain("address = \"0.0.0.0:8200\"");
        profile.ShouldContain("tls_disable = 1");
        profile.ShouldNotContain("server -dev", Case.Insensitive);
        profile.ShouldNotContain("-dev-no-store-token", Case.Insensitive);
    }

    [Fact]
    public void AppHost_OpenBaoResourceIsLoopbackStrictHealthAndSessionOnly()
    {
        string program = ReadRepoFile("src", "Hexalith.Memories.AppHost", "Program.cs");

        program.ShouldContain("AddContainer(OpenBaoDevelopmentProfile.ResourceName, OpenBaoDevelopmentProfile.Image, OpenBaoDevelopmentProfile.ImageTag)");
        program.ShouldContain("WithLifetime(ContainerLifetime.Session)");
        program.ShouldContain("ExcludeFromManifest()");
        program.ShouldContain("WithHttpEndpoint(");
        program.ShouldContain("targetPort: OpenBaoDevelopmentProfile.ContainerPort");
        program.ShouldContain("name: OpenBaoDevelopmentProfile.EndpointName");
        program.ShouldContain("isProxied: true");
        program.ShouldContain("endpoint.TargetHost = IPAddress.Loopback.ToString()");
        program.ShouldContain("endpoint.IsExternal = false");
        program.ShouldContain("WithHttpHealthCheck(");
        program.ShouldContain("path: OpenBaoDevelopmentProfile.HealthPath");
        program.ShouldContain("endpointName: OpenBaoDevelopmentProfile.EndpointName");
    }

    [Fact]
    public void AppHost_OpenBaoResourceFailsClosedOutsideDevelopmentRunMode()
    {
        string program = ReadRepoFile("src", "Hexalith.Memories.AppHost", "Program.cs");
        string profile = ReadRepoFile("src", "Hexalith.Memories.AppHost", "OpenBaoDevelopmentProfile.cs");

        program.ShouldContain("builder.ExecutionContext.IsRunMode");
        program.ShouldContain("builder.Environment.IsDevelopment()");
        program.ShouldContain("OpenBaoDevelopmentProfile.EnsureAllowed");
        profile.ShouldContain("throw new InvalidOperationException");
        profile.ShouldContain("development run mode", Case.Insensitive);
    }

    [Fact]
    public void AppHost_VaultComponentsUseDistinctProtectedParametersPrefixesAndScopes()
    {
        string program = ReadRepoFile("src", "Hexalith.Memories.AppHost", "Program.cs");

        program.ShouldContain("\"openbao-runtime-seeds\"");
        program.ShouldContain("\"openbao-access-telemetry-seeds\"");
        program.Split("secret: true", StringSplitOptions.None).Length.ShouldBeGreaterThanOrEqualTo(3);
        program.Split("type: secretstores.hashicorp.vault", StringSplitOptions.None).Length.ShouldBe(3);
        program.ShouldContain("vaultTokenMountPath");
        program.ShouldContain("vaultKVPrefix");
        program.ShouldContain("hexalith/memories/runtime");
        program.ShouldContain("hexalith/memories/access-telemetry");
        program.ShouldContain("vaultKVUsePrefix");
        program.ShouldContain("vaultValueType");
        program.ShouldContain("WriteAllTextAtomically(paths.RuntimeToken");
        program.ShouldContain("OpenBaoGenerationGate");
        program.ShouldContain("memories-access-telemetry-clock-dapr");
        program.ShouldNotContain("secretstores.local.file");
        program.ShouldNotContain("secretstores.kubernetes");
        program.ShouldNotContain("secretsFile");
        program.ShouldNotContain("secrets.json");
        program.ShouldNotContain("skipVerify");
    }

    [Fact]
    public void AppHost_ScopedTokensAndDaprKeyScopesAreFailClosed()
    {
        string initializer = ReadRepoFile("src", "Hexalith.Memories.AppHost", "OpenBaoInitializer.cs");
        string lifetimeGuard = ReadRepoFile("src", "Hexalith.Memories.AppHost", "OpenBaoSessionLifetimeGuard.cs");
        string daprConfiguration = ReadRepoFile("deploy", "dapr", "config.yaml");

        initializer.ShouldContain("create-orphan");
        initializer.ShouldContain("no_default_policy = true");
        initializer.ShouldContain("renewable = false");
        initializer.ShouldContain("168h");
        initializer.ShouldContain("revoke-self");
        initializer.ShouldNotContain("stored_shares");
        lifetimeGuard.ShouldContain("TimeSpan.FromHours(144)");
        daprConfiguration.ShouldContain("storeName: secretstore");
        daprConfiguration.ShouldContain("storeName: access-telemetry-secrets");
        daprConfiguration.ShouldContain("access-telemetry-marker-key");
        daprConfiguration.Split("defaultAccess: deny", StringSplitOptions.None).Length.ShouldBe(3);
    }

    private static string ReadRepoFile(params string[] segments)
    {
        string[] parts = new string[segments.Length + 1];
        parts[0] = ResolveRepoRoot();
        Array.Copy(segments, 0, parts, 1, segments.Length);
        string path = Path.Combine(parts);
        File.Exists(path).ShouldBeTrue($"Source file not found at {path}");
        return File.ReadAllText(path);
    }

    private static string ResolveRepoRoot()
    {
        string candidate = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        return AppContext.BaseDirectory;
    }
}
