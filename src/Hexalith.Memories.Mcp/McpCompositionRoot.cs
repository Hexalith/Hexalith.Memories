// <copyright file="McpCompositionRoot.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Mcp.Authentication;
using Hexalith.Memories.Mcp.Health;
using Hexalith.Memories.Mcp.Hosting;
using Hexalith.Memories.Mcp.Tools;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

/// <summary>
/// Story 10.1 — composition root for the MCP server. Extracted from <c>Program.cs</c> so the wiring
/// is testable in isolation and the in-process integration fixture can re-use it.
/// </summary>
internal static class McpCompositionRoot
{
    /// <summary>The DAPR app-id of the upstream Memories Server resolved through service invocation.</summary>
    internal const string MemoriesServerAppId = "memories";

    /// <summary>Environment variable overriding the upstream Memories Server DAPR app-id.</summary>
    internal const string MemoriesServerAppIdEnvVar = "MEMORIES_MCP_UPSTREAM_APP_ID";

    /// <summary>Registers every service the MCP server needs: DAPR client, MemoriesClient, MCP tools, error mapper.</summary>
    /// <param name="services">The service collection.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();

        services.AddOptions<MemoriesMcpAuthenticationOptions>()
            .BindConfiguration("Authentication:JwtBearer")
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<MemoriesMcpAuthenticationOptions>, ValidateMcpAuthenticationOptions>();
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
        services.AddTransient<IClaimsTransformation, MemoriesMcpClaimsTransformation>();
        services.AddScoped<TenantClaimAuthorizationFilter>();
        services.AddScoped<IAuthorizedTenantAccessor, AuthorizedTenantAccessor>();
        services.AddHostedService<StartupValidationHostedService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer()
            .AddMcp(_ => { });
        services.AddAuthorization();

        services.AddDaprClient();
        services.AddOptions<MemoriesClientOptions>();

        // The DAPR-supplied invoke client routes every call through the local sidecar at
        // DAPR_HTTP_ENDPOINT (defaults to http://localhost:3500) and adds the dapr-app-id header for
        // memories. The wrapping handler appends dapr-api-token when token mode is enabled.
        services.AddTransient<MemoriesMcpDaprInvocationHandler>();
        services.AddScoped<MemoriesClient>(sp =>
        {
            HttpClient invokeClient = Dapr.Client.DaprClient.CreateInvokeHttpClient(ResolveMemoriesServerAppId());
            MemoriesMcpDaprInvocationHandler.ApplyDaprApiToken(invokeClient);
            return new MemoriesClient(
                invokeClient,
                sp.GetRequiredService<IOptions<MemoriesClientOptions>>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MemoriesClient>>());
        });

        services.AddSingleton<McpErrorMapper>();

        services.AddMcpServer()
            .WithHttpTransport(options => options.Stateless = true)
            .AddAuthorizationFilters()
            .WithTools<SearchMemoryTool>()
            .WithTools<IngestContentTool>()
            .WithTools<TraverseRelationsTool>()
            .WithTools<GetCaseInfoTool>();

        services.AddSingleton<MemoriesServerUpstreamHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<DaprSidecarHealthCheck>(
                "dapr-sidecar",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["live", "ready"],
                timeout: TimeSpan.FromSeconds(3))
            .AddCheck<MemoriesServerUpstreamHealthCheck>(
                "memories-upstream",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"],
                timeout: TimeSpan.FromSeconds(6));
    }

    /// <summary>Resolves the upstream Memories Server DAPR app-id.</summary>
    /// <returns>The configured app-id, or the production default.</returns>
    internal static string ResolveMemoriesServerAppId()
    {
        string? configured = Environment.GetEnvironmentVariable(MemoriesServerAppIdEnvVar);
        return string.IsNullOrWhiteSpace(configured)
            ? MemoriesServerAppId
            : configured.Trim();
    }
}
