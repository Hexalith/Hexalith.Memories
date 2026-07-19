// <copyright file="OpenBaoDevelopmentProfile.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

/// <summary>Defines the disposable, development-only OpenBao server profile owned by the AppHost.</summary>
internal static class OpenBaoDevelopmentProfile
{
    /// <summary>Gets the container resource name.</summary>
    internal const string ResourceName = "openbao";

    /// <summary>Gets the OpenBao image repository.</summary>
    internal const string Image = "quay.io/openbao/openbao";

    /// <summary>Gets the exact OpenBao version and image digest used by production.</summary>
    internal const string ImageTag = "2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653";

    /// <summary>Gets the stable Aspire endpoint name.</summary>
    internal const string EndpointName = "http";

    /// <summary>Gets the OpenBao API port inside the container.</summary>
    internal const int ContainerPort = 8200;

    /// <summary>Gets the strict post-bootstrap health endpoint.</summary>
    internal const string HealthPath = "/v1/sys/health";

    /// <summary>Gets the path at which the generated configuration is mounted in the container.</summary>
    internal const string ContainerConfigurationPath = "/openbao/config/openbao.hcl";

    /// <summary>Gets the normal-server configuration for disposable local development.</summary>
    internal const string Configuration = """
        ui = false
        disable_mlock = true

        listener "tcp" {
          address = "0.0.0.0:8200"
          tls_disable = 1
        }

        storage "inmem" {}
        """;

    /// <summary>Rejects any attempt to select this disposable profile outside an explicit development run.</summary>
    /// <param name="isRunMode">Whether Aspire is evaluating the AppHost for a run operation.</param>
    /// <param name="isDevelopment">Whether the host environment is Development.</param>
    internal static void EnsureAllowed(bool isRunMode, bool isDevelopment)
    {
        if (!isRunMode || !isDevelopment)
        {
            throw new InvalidOperationException(
                "The disposable OpenBao in-memory profile is restricted to explicit Development run mode and cannot be published or selected for deployment.");
        }
    }

    /// <summary>Writes the non-secret OpenBao configuration into the AppHost-owned run directory.</summary>
    /// <param name="runDirectory">The process-unique AppHost run directory.</param>
    /// <returns>The absolute host path of the generated configuration.</returns>
    internal static string WriteConfiguration(string runDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runDirectory);

        string path = Path.Combine(runDirectory, "openbao.hcl");
        File.WriteAllText(path, Configuration + Environment.NewLine);
        return path;
    }
}
